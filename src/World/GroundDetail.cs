using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Petalfell.Core;

namespace Petalfell.World;

/// <summary>
/// Sub-voxel ground detail — grass tufts, flowers, pebbles, reeds, lichen,
/// fallen petals.
///
/// The reference images are covered in tiny handcrafted marks. None of it is
/// legible as an object from the play camera; all of it is what makes a grass
/// shelf read as *made* rather than as fill. A voxel is a metre here, so this
/// layer cannot be blocks: it is one merged mesh per chunk of small blades,
/// folded petals and sub-voxel boxes.
///
/// Four decisions carry the whole look, and getting any of them wrong turns the
/// meadow into something else:
///
///   * Meadow grass has three bent blades with a shared rooted clump. The
///     crossed-pair primitive remains available for stems and reeds.
///   * Blade normals point straight UP, not out of the quad. Lit like the
///     ground they grow from, blades stay inside the high-key band; lit as
///     vertical surfaces they turn into dark slivers and a shelf reads as
///     gravel.
///   * Blade ends stay blunt. Pointed straight silhouettes read as tiny conifers.
///   * Detail follows broad meadow fields, with several blades or flowers per
///     occupied column and quiet gaps between patches.
///
/// Wind lives in the vertex shader, driven by a per-vertex sway weight (0 at
/// the root, 1 at the tip) and a per-clump phase, so a whole meadow ripples
/// rather than pulsing in lockstep.
/// </summary>
public static class GroundDetail
{
	/// <summary>World seed, so the scatter fields agree with the terrain's.</summary>
	public static int Seed;

	private static Noise2D _meadow, _flowers, _waterDrift, _fragments, _pavingDrift;

	/// <summary>Face brightness ramp for sub-voxel boxes, matching the voxel shader's.</summary>
	private const float ShadeTop = 1.0f, ShadeSide = 0.88f, ShadeSideZ = 0.82f, ShadeBottom = 0.7f;

	/// <summary>
	/// A deterministic per-column draw sequence. Seeded from the column itself
	/// rather than shared, so any chunk can be built in any order and a shelf
	/// scatters identically every time it streams back in.
	/// </summary>
	private struct Draw
	{
		private uint _s;

		public Draw(int x, int z, int salt)
		{
			unchecked
			{
				uint h = (uint)(x * 374761393) + (uint)(z * 668265263) + (uint)(salt * 1442695040);
				h = (h ^ (h >> 13)) * 1274126177u;
				_s = h ^ (h >> 16);
			}
		}

		public float Next()
		{
			unchecked
			{
				_s += 0x6D2B79F5u;
				uint t = _s;
				t = (uint)((t ^ (t >> 15)) * (t | 1u));
				t ^= t + (uint)((t ^ (t >> 7)) * (t | 61u));
				return ((t ^ (t >> 14)) & 0xFFFFFF) / 16777216f;
			}
		}

		public float Range(float a, float b) => a + Next() * (b - a);
		public int Int(int a, int b) => a + (int)(Next() * (b - a + 1));
		public bool Chance(float p) => Next() < p;
		public T Pick<T>(T[] items) => items[Math.Min(items.Length - 1, (int)(Next() * items.Length))];
	}

	private sealed class Field
	{
		public readonly List<Vector3> Pos = new(4096);
		public readonly List<Vector3> Nrm = new(4096);
		public readonly List<Color> Col = new(4096);
		public readonly List<float> Det = new(8192);   // (sway, phase) per vertex
		public readonly List<int> Idx = new(6144);
		private readonly List<(int index, Vector3 inward)> _thinEdges = new();

		public bool Empty => Pos.Count == 0;

		/// <summary>One quad with separately weighted bottom and top edges.</summary>
		public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n,
			Color bottom, Color top, float sway, float phase, float bottomSway = 0f, bool preserveWidth = false)
		{
			int i = Pos.Count;
			Pos.Add(a); Pos.Add(b); Pos.Add(c); Pos.Add(d);
			if (preserveWidth) {
				_thinEdges.Add((i, (b-a)*.5f)); _thinEdges.Add((i+1, (a-b)*.5f));
				_thinEdges.Add((i+2, (d-c)*.5f)); _thinEdges.Add((i+3, (c-d)*.5f));
			}
			for (int k = 0; k < 4; k++) Nrm.Add(n);
			Col.Add(bottom); Col.Add(bottom); Col.Add(top); Col.Add(top);
			Det.Add(bottomSway); Det.Add(phase);
			Det.Add(bottomSway); Det.Add(phase);
			Det.Add(sway); Det.Add(phase);
			Det.Add(sway); Det.Add(phase);
			Idx.Add(i); Idx.Add(i + 1); Idx.Add(i + 2);
			Idx.Add(i); Idx.Add(i + 2); Idx.Add(i + 3);
		}

		/// <summary>Three blunt bent leaves, rooted together and lit like their turf.</summary>
		public void Grass(float x, float y, float z, float w, float h,
			Color bottom, Color top, float phase, float leanX, float leanZ)
		{
			var centre = new Vector3(x, y, z);
			var lean = new Vector3(leanX, 0f, leanZ) * 0.2f;
			for (int blade = 0; blade < 3; blade++)
			{
				float angle = phase + blade * Mathf.Tau / 3f;
				var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
				var across = new Vector3(-outward.Z, 0f, outward.X) * (w * 0.24f);
				float stature = 0.72f + blade * 0.14f;
				var root = centre + outward * 0.025f;
				var shoulder = root + Vector3.Up * (h * stature * 0.58f) + outward * 0.035f;
				var tip = root + Vector3.Up * (h * stature * 0.92f) + outward * 0.15f + lean;
				Color middle = bottom.Lerp(top, 0.58f);
				float shoulderSway = stature * 0.55f;
				Quad(root - across * 0.8f, root + across * 0.8f,
					shoulder + across, shoulder - across, Vector3.Up,
					bottom, middle, shoulderSway, phase, preserveWidth: true);
				// Duplicated shoulder vertices use identical weights in both segments.
				Quad(shoulder - across, shoulder + across,
					tip + across * 0.5f, tip - across * 0.5f, Vector3.Up,
					middle, top, stature, phase, shoulderSway, preserveWidth: true);
			}
		}

		/// <summary>A crossed pair of vertical blades — the workhorse.</summary>
		public void Tuft(float x, float y, float z, float w, float h,
			Color bottom, Color top, float phase, float leanX = 0f, float leanZ = 0f, float sway = 1f)
		{
			float hw = w * 0.5f;
			for (int k = 0; k < 2; k++)
			{
				float ax = k == 0 ? hw : 0f;
				float az = k == 0 ? 0f : hw;
				float tx = x + leanX, tz = z + leanZ;
				Quad(
					new Vector3(x - ax, y, z - az),
					new Vector3(x + ax, y, z + az),
					new Vector3(tx + ax * 0.62f, y + h, tz + az * 0.62f),
					new Vector3(tx - ax * 0.62f, y + h, tz - az * 0.62f),
					Vector3.Up, bottom, top, sway, phase, preserveWidth: true);
			}
		}

		/// <summary>A sub-voxel box: pebbles, flower heads, lichen, tiny fungus caps.</summary>
		public void Box(float cx, float y, float cz, float sx, float sy, float sz,
			Color color, float sway = 0f, float phase = 0f)
		{
			float x0 = cx - sx * 0.5f, x1 = cx + sx * 0.5f;
			float y0 = y, y1 = y + sy;
			float z0 = cz - sz * 0.5f, z1 = cz + sz * 0.5f;

			Color Shade(float m) => new(color.R * m, color.G * m, color.B * m);
			var top = Shade(ShadeTop);
			var bot = Shade(ShadeBottom);
			var sxc = Shade(ShadeSide);
			var szc = Shade(ShadeSideZ);

			Quad(new(x0, y1, z0), new(x0, y1, z1), new(x1, y1, z1), new(x1, y1, z0), Vector3.Up, top, top, sway, phase);
			Quad(new(x0, y0, z1), new(x0, y0, z0), new(x1, y0, z0), new(x1, y0, z1), Vector3.Down, bot, bot, 0f, phase);
			Quad(new(x1, y0, z0), new(x1, y0, z1), new(x1, y1, z1), new(x1, y1, z0), Vector3.Right, sxc, sxc, sway, phase);
			Quad(new(x0, y0, z1), new(x0, y0, z0), new(x0, y1, z0), new(x0, y1, z1), Vector3.Left, sxc, sxc, sway, phase);
			Quad(new(x1, y0, z1), new(x0, y0, z1), new(x0, y1, z1), new(x1, y1, z1), Vector3.Back, szc, szc, sway, phase);
			Quad(new(x0, y0, z0), new(x1, y0, z0), new(x1, y1, z0), new(x0, y1, z0), Vector3.Forward, szc, szc, 0f, phase);
		}

		/// <summary>A low mineral plate with two broken corners and six lit bevels.</summary>
		public void Chip(float x, float y, float z, float width, float height, float depth,
			float angle, float cut, Color color)
		{
			float hx = width * 0.5f, hz = depth * 0.5f;
			Span<Vector2> profile = stackalloc Vector2[6]
			{
				new(-hx + width * cut, -hz), new(hx, -hz), new(hx, hz - depth * cut),
				new(hx - width * cut, hz), new(-hx, hz), new(-hx, -hz + depth * cut),
			};
			Span<Vector3> rim = stackalloc Vector3[6];
			Span<Vector3> top = stackalloc Vector3[6];
			float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
			for (int k = 0; k < 6; k++)
			{
				var p = profile[k];
				var offset = new Vector3(p.X * c - p.Y * s, 0f, p.X * s + p.Y * c);
				rim[k] = new Vector3(x, y + 0.012f, z) + offset;
				top[k] = new Vector3(x, y + height, z) + offset * 0.72f;
			}
			for (int k = 0; k < 6; k++)
			{
				int next = (k + 1) % 6;
				var normal = (rim[k] - rim[next]).Cross(top[k] - rim[next]).Normalized();
				Quad(rim[next], rim[k], top[k], top[next], normal, color, color, 0f, 0f);
			}
			// All facets face upward; a buried base would only add invisible faces.
			for (int k = 1; k < 5; k++)
			{
				int first = Pos.Count;
				Pos.Add(top[0]); Pos.Add(top[k + 1]); Pos.Add(top[k]);
				for (int j = 0; j < 3; j++)
				{
					Nrm.Add(Vector3.Up); Col.Add(color); Det.Add(0f); Det.Add(0f);
					Idx.Add(first + j);
				}
			}
		}

		/// <summary>
		/// A strand hanging DOWN a wall face — the vine primitive.
		///
		/// Everything about it is the tuft inverted. The moving end is the bottom,
		/// so the corner order puts the hanging tip in the last pair, which is
		/// where <see cref="Quad"/> applies sway. The plane holds the wall's
		/// tangent and up, so the strand lies flat against the masonry rather than
		/// standing off it.
		///
		/// The normal is mostly UP with a lean out of the wall. Straight out, and
		/// a curtain of vine is lit as a vertical surface and goes to dark slivers
		/// for the same reason blades do; straight up, and it stops reading as
		/// attached to anything.
		/// </summary>
		public void Drape(float x, float y, float z, float fx, float fz,
			float length, float width, Color attach, Color tip, float phase)
		{
			float tx = -fz, tz = fx;             // along the wall
			float hw = width * 0.5f;
			var n = new Vector3(fx * 0.45f, 0.89f, fz * 0.45f).Normalized();
			float bottom = y - length;
			// A vine hangs, so it drifts away from the wall as it falls.
			float swing = 0.10f + length * 0.06f;

			Quad(
				new Vector3(x - tx * hw, y, z - tz * hw),
				new Vector3(x + tx * hw, y, z + tz * hw),
				new Vector3(x + tx * hw * 0.7f + fx * swing, bottom, z + tz * hw * 0.7f + fz * swing),
				new Vector3(x - tx * hw * 0.7f + fx * swing, bottom, z - tz * hw * 0.7f + fz * swing),
				n, attach, tip, 1f, phase, preserveWidth: true);
		}

		/// <summary>A tiny leaf or petal lying flat on the ground, rotated in plan.</summary>
		public void Fleck(float x, float y, float z, float length, float width, float rot, Color color)
		{
			var along = new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)) * (length * 0.5f);
			var across = new Vector2(-along.Y, along.X).Normalized() * (width * 0.5f);
			Quad(
				new(x - along.X, y, z - along.Y),
				new(x + across.X, y, z + across.Y),
				new(x + along.X, y, z + along.Y),
				new(x - across.X, y, z - across.Y),
				Vector3.Up, color, color, 0f, 0f);
		}

		/// <summary>A folded four-facet leaf, attached to an authored moss face.</summary>
		public void MossLeaf(Vector3 root, Vector3 along, Vector3 across, Vector3 outward,
			float length, float width, Color color)
		{
			var middle = root + along * (length * 0.48f);
			var ridge = middle + outward * 0.055f;
			var tip = root + along * length + outward * 0.028f;
			var left = middle + across * (width * 0.5f);
			var right = middle - across * (width * 0.5f);
			Facet(root, left, ridge); Facet(left, tip, ridge);
			Facet(tip, right, ridge); Facet(right, root, ridge);

			void Facet(Vector3 a, Vector3 b, Vector3 c)
			{
				var normal = (b - a).Cross(c - a).Normalized();
				if (normal.Dot(outward) < 0f) normal = -normal;
				// Wall leaves tip toward the sky, retaining the pale growth palette
				// while their facets still respond differently to the moving key.
				normal = (normal + Vector3.Up * 0.35f).Normalized();
				int first = Pos.Count;
				Pos.Add(a); Pos.Add(b); Pos.Add(c);
				for (int i = 0; i < 3; i++)
				{
					Nrm.Add(normal); Col.Add(color); Det.Add(0f); Det.Add(0f);
					Idx.Add(first + i);
				}
			}
		}

		public void LilyPad(float x, float y, float z, float radius, float angle, Color colour)
		{
			// One missing wedge gives a readable leaf notch, with a shallow folded rim.
			for (int k = 0; k < 9; k++)
			{
				float a = angle + (k + .35f) * Mathf.Tau / 10f;
				float b = angle + (k + 1.35f) * Mathf.Tau / 10f;
				int i = Pos.Count;
				Pos.Add(new(x, y, z));
				Pos.Add(new(x + Mathf.Cos(b) * radius, y + .018f, z + Mathf.Sin(b) * radius));
				Pos.Add(new(x + Mathf.Cos(a) * radius, y + .018f, z + Mathf.Sin(a) * radius));
				for (int n = 0; n < 3; n++)
				{
					Nrm.Add(Vector3.Up); Col.Add(colour); Det.Add(0f); Det.Add(0f); Idx.Add(i + n);
				}
			}
		}

		public ArrayMesh Build()
		{
			var arrays = new Godot.Collections.Array();
			arrays.Resize((int)Mesh.ArrayType.Max);
			arrays[(int)Mesh.ArrayType.Vertex] = Pos.ToArray();
			arrays[(int)Mesh.ArrayType.Normal] = Nrm.ToArray();
			arrays[(int)Mesh.ArrayType.Color] = Col.ToArray();
			arrays[(int)Mesh.ArrayType.Custom0] = Det.ToArray();
			arrays[(int)Mesh.ArrayType.Index] = Idx.ToArray();
			var widths = new float[Pos.Count * 3];
			foreach (var (index, inward) in _thinEdges) {
				widths[index*3] = inward.X; widths[index*3+1] = inward.Y; widths[index*3+2] = inward.Z;
			}
			arrays[(int)Mesh.ArrayType.Custom1] = widths;

			// Two floats per vertex in CUSTOM0: sway weight and clump phase.
			ulong fmt = ((ulong)Mesh.ArrayCustomFormat.RgFloat << 13) |
				((ulong)Mesh.ArrayCustomFormat.RgbFloat << 16);

			var mesh = new ArrayMesh();
			mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null,
				(Mesh.ArrayFormat)fmt);
			return mesh;
		}

		public void Flower(float x, float y, float z, float height, Color petals, float phase)
		{
			Tuft(x, y - 0.025f, z, 0.055f, height + 0.025f, TuftBase, ReedTip, phase, sway: 0.5f);
			// Two small leaves give the flower a stem silhouette at walking distance.
			// They share the stem's displacement at their attachment height.
			for (int leaf = 0; leaf < 2; leaf++)
			{
				float level = 0.32f + leaf * 0.22f;
				float angle = phase + leaf * Mathf.Pi;
				var along = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
				var across = new Vector3(-along.Z, 0f, along.X) * 0.048f;
				var root = new Vector3(x, y + height * level, z);
				var middle = root + along * 0.085f + Vector3.Up * 0.025f;
				int start = Det.Count;
				Quad(root, middle + across, root + along * 0.17f + Vector3.Up * 0.035f,
					middle - across, Vector3.Up, TuftBase, ReedTip, 0f, phase);
				float weight = 0.5f * (height * level + 0.025f) / (height + 0.025f);
				for (int i = start; i < Det.Count; i += 2) Det[i] = weight;
			}
			// The head is attached to the stem tip. Give every head vertex the same
			// wind weight; applying the blade's root/tip gradient would tear it apart.
			int headStart = Det.Count;
			// A low cup catches light on two broad facets per petal, keeping the
			// five-part flower silhouette visible from both low and overhead views.
			for (int petal = 0; petal < 5; petal++)
			{
				float angle = phase + petal * Mathf.Tau / 5f;
				var along = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
				var across = new Vector3(-along.Z, 0f, along.X);
				var root = new Vector3(x, y + height, z) + along * 0.02f;
				var shoulder = root + along * 0.08f + Vector3.Up * 0.018f;
				var tip = root + along * 0.17f + Vector3.Up * 0.065f;
				var innerNormal = (Vector3.Up - along * 0.225f).Normalized();
				var outerNormal = (Vector3.Up - along * 0.52f).Normalized();
				Quad(root - across * 0.019f, root + across * 0.019f,
					shoulder + across * 0.063f, shoulder - across * 0.063f,
					innerNormal, petals.Darkened(0.06f), petals, 0f, phase);
				Quad(shoulder - across * 0.063f, shoulder + across * 0.063f,
					tip + across * 0.027f, tip - across * 0.027f,
					outerNormal, petals, petals.Lightened(0.035f), 0f, phase);
			}
			Box(x, y + height - 0.01f, z, 0.065f, 0.035f, 0.065f, FlowerHeart);
			for (int i = headStart; i < Det.Count; i += 2)
			{
				Det[i] = 0.5f;
				Det[i + 1] = phase;
			}
		}
	}

	private static Color Srgb(uint hex) => new Color(
		((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f).SrgbToLinear();

	private static readonly Color[] FlowerTops =
	{
		Srgb(0xf6c3d4), Srgb(0xf2dcc0), Srgb(0xe2cdf3), Srgb(0xfaeaf0), Srgb(0xf3b9c6),
		Srgb(0x82bdde), Srgb(0xefca68), Srgb(0xf5eddb),
	};
	private static readonly Color[] PebbleTops =
	{
		Srgb(0xd6d0e2), Srgb(0xcfc7db), Srgb(0xded7e6),
	};
	private static readonly Color[] PadColors =
	{
		Srgb(0x93ad72), Srgb(0xa3b97f), Srgb(0x87a267),
	};
	private static readonly Color TuftBase = Srgb(0x8f9e66);
	private static readonly Color ReedBase = Srgb(0x9aa877);
	private static readonly Color ReedTip = Srgb(0xc3cf9c);
	private static readonly Color Lichen = Srgb(0xa8b782);
	private static readonly Color FlowerHeart = Srgb(0xf4cd71);

	// Reclamation greens (plan.md §11a.4). Deliberately deeper and less pastel
	// than the meadow's: growth taking a building back is the one green in this
	// world allowed to look vigorous, and against pale masonry it has to carry
	// enough weight to read as mass rather than as a stain.
	// Deeper than the meadow, but only just.
	//
	// The first pass used a genuine forest green (0x6b855a) on the reasoning that
	// growth taking a building back should look vigorous. Against pastel masonry
	// it rendered as black tar streaked down the walls: everything else in this
	// world sits between 0.6 and 0.9 linear and that green sits at 0.15. In a
	// palette this high-key, "darker than the darkest thing present" is not
	// contrast, it is a hole in the picture.
	private static readonly Color VineAttach = Srgb(0x87a067);
	private static readonly Color VineTip = Srgb(0xbccf96);
	private static readonly Color[] ThicketBase =
	{
		Srgb(0x8aa26a), Srgb(0x93a973), Srgb(0x829a63),
	};
	private static readonly Color[] ThicketTip =
	{
		Srgb(0xbdcf9a), Srgb(0xc6d6a6), Srgb(0xb2c68d),
	};
	private static readonly Color MossCushionSide = Srgb(0x86a05f);
	private static readonly Color MossCushionTop = Srgb(0x9db572);
	private static readonly Color[] RubbleTones =
	{
		Srgb(0xb0a8bb), Srgb(0xa39aae), Srgb(0xbcb4c6),
	};
	private static readonly Color SaplingTrunk = Srgb(0x7d5a68);

	/// <summary>
	/// What floats on the water: fallen blossom and the occasional lily pad.
	///
	/// Built as its own mesh because it needs a material that writes depth — see
	/// waterdetail.gdshader for why — and because it is scattered over water
	/// columns, which the ground pass skips outright.
	/// </summary>
	public static ArrayMesh BuildWater(Terrain terrain, int ci, int ck)
	{
		int cs = ChunkMesher.ChunkSize;
		int S = terrain.Size;
		int x0 = ci * cs, z0 = ck * cs;
		int x1 = Math.Min(S, x0 + cs), z1 = Math.Min(S, z0 + cs);
		float surface = Palette.WaterLevel;

		var f = new Field();

		for (int z = z0; z < z1; z++)
		for (int x = x0; x < x1; x++)
		{
			int i = z * S + x;
			int bed = terrain.Level[i];
			if (bed > Terrain.Sea) continue;                  // dry land
			// Nothing floats where the water is a film over the bed; that band
			// belongs to the shoreline and reads as debris stranded on mud.
			if (surface - bed < 0.8f) continue;

			var rng = new Draw(x, z, 0x0FA7);
			float fx = x + 0.5f, fz = z + 0.5f;

			// Pads gather; single petals drift. A pad carries a flower often
			// enough to be a surprise and rarely enough to stay one.
			if (rng.Chance(0.005f))
			{
				var pad = rng.Pick(PadColors);
				int n = rng.Int(1, 3);
				for (int k = 0; k < n; k++)
				{
					float ox = rng.Range(-0.34f, 0.34f), oz = rng.Range(-0.34f, 0.34f);
					f.Fleck(fx + ox, surface + 0.03f, fz + oz,
						rng.Range(0.46f, 0.72f), rng.Range(0.42f, 0.66f), rng.Next() * 1.57f, pad);
					if (k == 0 && rng.Chance(0.30f))
					{
						f.Box(fx + ox, surface + 0.05f, fz + oz, 0.20f, 0.14f, 0.20f,
							rng.Pick(FlowerTops));
					}
				}
				continue;
			}

			// Punctuation, not a carpet. A percent or so of columns puts a couple
			// of dozen petals across a whole lake, which is what the reference
			// actually holds; anything denser reads as debris.
			if (rng.Chance(0.011f))
			{
				int n = rng.Int(1, 2);
				for (int k = 0; k < n; k++)
				{
					f.Fleck(fx + rng.Range(-0.38f, 0.38f), surface + 0.02f,
						fz + rng.Range(-0.38f, 0.38f),
						rng.Range(0.26f, 0.42f), rng.Range(0.22f, 0.36f),
						rng.Next() * 1.57f, rng.Pick(Palette.PetalColors));
				}
			}
		}

		return f.Empty ? null : f.Build();
	}

	/// <summary>
	/// Floating detail for a compiled production-atlas window. Water height is
	/// per column in the atlas, so every petal is placed on the same stepped
	/// surface that <see cref="AtlasSectorWindow.BuildWater"/> materialises.
	/// Random draws use global coordinates, making overlap and rebuild order
	/// irrelevant at sector seams.
	/// </summary>
	public static ArrayMesh BuildAtlasWater(AtlasSectorWindow window, int ci, int ck)
	{
		_waterDrift ??= new Noise2D(Seed + 73);
		AtlasSectorData data = window.Data;
		int cs = ChunkMesher.ChunkSize;
		int x0 = ci * cs, z0 = ck * cs;
		int x1 = Math.Min(data.Width, x0 + cs), z1 = Math.Min(data.Depth, z0 + cs);
		var f = new Field();

		for (int z = z0; z < z1; z++)
		for (int x = x0; x < x1; x++)
		{
			int index = z * data.Width + x;
			int surface = data.WaterSurface[index];
			if (surface == 0 || surface - data.Height[index] < 2 ||
				window.Grid.SolidAt(x, surface, z) || window.Grid.SolidAt(x, surface + 1, z)) continue;

			int gx = data.OriginX + x, gz = data.OriginZ + z;
			var rng = new Draw(gx, gz, 0x0FA7);
			float fx = x + 0.5f, fz = z + 0.5f, y = surface + 0.38f;
			string detailSet = window.GroundDetailSetAt(x, z);
			float marsh = detailSet is "reed-root-moss" or "sand-reed-petal"
				? ProductionTerrainGuide.SouthernLatitudeAt(gz) : 0f;
			bool permitsPads = (detailSet.Contains("reed", StringComparison.Ordinal) ||
				detailSet.Contains("moss", StringComparison.Ordinal)) && (marsh <= 0f || surface - data.Height[index] <= 8);
			bool permitsPetals = detailSet.Contains("petal", StringComparison.Ordinal) ||
				detailSet.Contains("flower", StringComparison.Ordinal);
			if (!permitsPads && !permitsPetals) continue;
			// Independent per-cell chances became evenly spaced confetti over broad
			// atlas water. First admit only coherent drift patches, then let the local
			// draw vary instances inside them; absolute coordinates preserve chunk and
			// seam determinism.
			float drift = _waterDrift.Fbm01(gx / 42f, gz / 42f, 3);
			float threshold = Rng.Lerp(.64f, .50f, marsh);
			if (drift < threshold) continue;
			float clump = Rng.Smoothstep(threshold, 0.88f, drift);
			float padChance = permitsPads ? (0.003f + clump * 0.022f) * Rng.Lerp(1f, 5f, marsh) : 0f;
			if (rng.Chance(padChance))
			{
				var pad = rng.Pick(PadColors);
				int count = rng.Int(1, 3);
				for (int k = 0; k < count; k++)
				{
					float ox = rng.Range(-0.34f, 0.34f), oz = rng.Range(-0.34f, 0.34f);
					f.LilyPad(fx + ox * .55f, y, fz + oz * .55f, rng.Range(.20f, .32f),
						rng.Next() * Mathf.Tau, pad);
					if (k == 0 && rng.Chance(0.12f))
						f.Box(fx + ox * .55f, y + 0.02f, fz + oz * .55f, 0.20f, 0.14f, 0.20f,
							rng.Pick(FlowerTops));
				}
				continue;
			}

			float petalChance = permitsPetals ? (0.005f + clump * 0.035f) * Rng.Lerp(1f, .25f, marsh) : 0f;
			if (!rng.Chance(petalChance)) continue;
			int petals = rng.Int(1, 2);
			for (int k = 0; k < petals; k++)
				f.Fleck(fx + rng.Range(-0.38f, 0.38f), y,
					fz + rng.Range(-0.38f, 0.38f), rng.Range(0.26f, 0.42f),
					rng.Range(0.22f, 0.36f), rng.Next() * 1.57f,
					rng.Pick(Palette.PetalColors));
		}

		return f.Empty ? null : f.Build();
	}

	/// <summary>
	/// Everything the land has put back on a ruin in this chunk.
	///
	/// The sprigs were decided once, during world construction, by
	/// <see cref="Reclaim.Overgrow"/> — this only turns them into geometry. They
	/// go into the SAME field as the meadow's tufts on purpose: one mesh, one
	/// material, one wind shader, and not a line of new plumbing in the streamer.
	/// </summary>
	private static void Sprigs(Field f, int ci, int ck)
	{
		var list = Reclaim.In(ci, ck);
		if (list == null) return;

		foreach (var s in list)
		{
			// Tone is a per-instance draw, so neighbouring growth of the same kind
			// never comes out the same colour. Without it a wall of vine reads as
			// one flat green shape.
			float t = s.Tone;

			switch (s.Kind)
			{
				case Growth.Vine:
				{
					// Two or three strands off one attachment, at slightly
					// different lengths. A single quad reads as a hanging rag.
					// Narrow strands at unequal lengths. Wide ones read as a hanging
					// rag whatever colour they are.
					int strands = t > 0.45f ? 3 : 2;
					for (int k = 0; k < strands; k++)
					{
						float off = (k - (strands - 1) * 0.5f) * 0.26f;
						float len = s.Size * (0.58f + ((k * 37 + (int)(t * 91)) % 10) * 0.048f);
						f.Drape(
							s.X + -s.Fz * off, s.Y, s.Z + s.Fx * off,
							s.Fx, s.Fz, len, 0.12f + t * 0.09f,
							VineAttach, VineTip, s.Phase + k * 0.7f);
					}
					break;
				}

				case Growth.Fern:
				{
					// Fronds arch OUT of the wall they shelter under, which is the
					// only thing distinguishing a fern from a tuft at this size.
					var b = ThicketBase[(int)(t * 2.99f)];
					var tip = ThicketTip[(int)(t * 2.99f)];
					int n = 3 + (int)(t * 2f);
					for (int k = 0; k < n; k++)
					{
						float spread = (k / MathF.Max(1f, n - 1f) - 0.5f) * 0.5f;
						f.Tuft(
							s.X + -s.Fz * spread, s.Y - 0.04f, s.Z + s.Fx * spread,
							0.13f + t * 0.06f, s.Size * (0.7f + k % 3 * 0.15f),
							b, tip, s.Phase + k * 0.9f,
							s.Fx * (0.16f + t * 0.12f), s.Fz * (0.16f + t * 0.12f));
					}
					break;
				}

				case Growth.Thicket:
				{
					var b = ThicketBase[(int)(t * 2.99f)];
					var tip = ThicketTip[(int)(t * 2.99f)];
					// A woody heart with blades over it. The boxes are what stop a
					// thicket reading as tall grass — scrub has mass.
					f.Box(s.X, s.Y - 0.04f, s.Z, s.Size * 0.55f, s.Size * 0.34f, s.Size * 0.5f, b);
					int n = 4 + (int)(t * 3f);
					for (int k = 0; k < n; k++)
					{
						float a = (k / (float)n) * 6.283f + t * 3f;
						float r = s.Size * (0.14f + (k % 3) * 0.10f);
						f.Tuft(
							s.X + MathF.Cos(a) * r, s.Y - 0.05f, s.Z + MathF.Sin(a) * r,
							0.15f + t * 0.09f, s.Size * (0.55f + (k % 4) * 0.14f),
							b, tip, s.Phase + k * 0.8f,
							MathF.Cos(a) * 0.14f, MathF.Sin(a) * 0.14f);
					}
					break;
				}

				case Growth.Sapling:
				{
					f.Box(s.X, s.Y - 0.05f, s.Z, 0.13f, s.Size, 0.13f, SaplingTrunk, 0.35f, s.Phase);
					var tip = ThicketTip[(int)(t * 2.99f)];
					var b = ThicketBase[(int)(t * 2.99f)];
					for (int k = 0; k < 4; k++)
					{
						float a = k * 1.571f + t;
						f.Tuft(
							s.X + MathF.Cos(a) * 0.12f, s.Y + s.Size * 0.55f, s.Z + MathF.Sin(a) * 0.12f,
							0.22f, s.Size * 0.55f, b, tip, s.Phase + k,
							MathF.Cos(a) * 0.20f, MathF.Sin(a) * 0.20f);
					}
					break;
				}

				case Growth.Moss:
				{
					f.Box(s.X, s.Y - 0.06f, s.Z, s.Size, 0.10f + t * 0.09f, s.Size * 0.9f, MossCushionTop);
					if (t > 0.6f)
						f.Tuft(s.X, s.Y + 0.02f, s.Z, 0.10f, 0.10f + t * 0.10f,
							MossCushionSide, MossCushionTop, s.Phase, 0f, 0f, 0.5f);
					break;
				}

				case Growth.Rubble:
				{
					f.Box(s.X, s.Y - 0.05f, s.Z, s.Size, s.Size * (0.34f + t * 0.3f), s.Size * 0.86f,
						RubbleTones[(int)(t * 2.99f)]);
					break;
				}
			}
		}
	}

	public static ArrayMesh Build(Terrain terrain, int ci, int ck)
	{
		_meadow ??= new Noise2D(Seed + 71);
		_flowers ??= new Noise2D(Seed + 72);

		int cs = ChunkMesher.ChunkSize;
		int S = terrain.Size;
		int x0 = ci * cs, z0 = ck * cs;
		int x1 = Math.Min(S, x0 + cs), z1 = Math.Min(S, z0 + cs);

		var f = new Field();

		for (int z = z0; z < z1; z++)
		for (int x = x0; x < x1; x++)
		{
			int i = z * S + x;
			if (terrain.Land[i] == 0) continue;
			int h = terrain.Level[i];
			byte cap = terrain.Grid.At(x, h - 1, z);
			// Nothing sprouts under a canopy or inside a trunk.
			if (terrain.Grid.At(x, h, z) != Palette.AIR) continue;

			float y = h;
			float fx = x + 0.5f, fz = z + 0.5f;
			var rng = new Draw(x, z, 0x5EED);

			bool grassy = Palette.IsGrassSurface(cap) || cap is Palette.MOSS or Palette.BLOSSOM_DRIFT;
			bool muddy = cap == Palette.MUD;
			// Snow and scree carry their own marks, not grass ones.
			bool snowy = cap == Palette.SNOW;
			bool scree = cap == Palette.SCREE;
			bool sandy = cap == Palette.SAND;
			bool stony = cap is Palette.STONE or Palette.STONE_PALE or Palette.STONE_WARM;

			// Rushes stand in the SHALLOWS, so the bed has to be near the surface
			// — not merely below it. Without the lower bound the lake basin
			// qualifies all the way to its deepest point and the open water fills
			// with reeds standing ten blocks under.
			if ((sandy || grassy || muddy) && terrain.Wet[i] == 1 &&
			    h <= Terrain.Sea + 2 && h >= Terrain.Sea - 2)
			{
				if (rng.Chance(0.15f))
				{
					int n = rng.Int(2, 4);
					for (int k = 0; k < n; k++)
					{
						f.Tuft(fx + rng.Range(-0.30f, 0.30f), y - 0.03f, fz + rng.Range(-0.30f, 0.30f),
							rng.Range(0.10f, 0.17f), rng.Range(0.38f, 0.76f),
							ReedBase, ReedTip, rng.Next() * 6.28f,
							rng.Range(-0.18f, 0.18f), rng.Range(-0.18f, 0.18f));
					}
				}
				continue;
			}

			if (grassy)
			{
				// The base is a deeper sage than any shelf, the tip a little
					// lighter than the one it grows from. Taken straight off the
					// block's own top colour the whole blade sits inside the ground
					// tone and the scatter disappears — the marks only read because
					// the root end is decisively darker than what surrounds it.
					var block = Palette.Get(cap);
					var dark = TuftBase;
					var lite = new Color(block.Top.R * 1.08f, block.Top.G * 1.08f, block.Top.B * 1.08f);

				// Clumps, not a dusting: a few percent of columns, each carrying
				// one to three blades.
				float lush = _meadow.Fbm01(x * 0.045f, z * 0.045f, 3);
				if (rng.Chance(0.018f + lush * 0.055f))
				{
					int n = rng.Int(1, 3);
					for (int k = 0; k < n; k++)
					{
						f.Tuft(fx + rng.Range(-0.32f, 0.32f), y - 0.03f, fz + rng.Range(-0.32f, 0.32f),
							rng.Range(0.16f, 0.30f), rng.Range(0.26f, 0.52f),
							dark, lite, rng.Next() * 6.28f,
							rng.Range(-0.14f, 0.14f), rng.Range(-0.14f, 0.14f));
					}
				}

				// Flowers gather into drifts rather than dusting evenly.
				float ff = _flowers.Fbm01(x * 0.028f, z * 0.028f, 3);
				if (ff > 0.66f && rng.Chance((ff - 0.66f) * 0.30f))
				{
					var col = rng.Pick(FlowerTops);
					int n = rng.Int(1, 3);
					for (int k = 0; k < n; k++)
					{
						float ox = rng.Range(-0.30f, 0.30f), oz = rng.Range(-0.30f, 0.30f);
						float sh = rng.Range(0.24f, 0.42f);
						// A bare stem plus a head: the head is the note of colour,
						// the stem exists only so it is not floating.
						f.Tuft(fx + ox, y - 0.03f, fz + oz, 0.07f, sh, dark, dark, 0f, 0f, 0f, 0.6f);
						f.Box(fx + ox, y + sh - 0.04f, fz + oz, 0.20f, 0.16f, 0.20f,
							col, 0.9f, rng.Next() * 6.28f);
					}
				}

				// Pebbles and clods. Never animated — they are the still notes.
				if (rng.Chance(0.0055f))
				{
					f.Box(fx + rng.Range(-0.25f, 0.25f), y - 0.05f, fz + rng.Range(-0.25f, 0.25f),
						rng.Range(0.24f, 0.46f), rng.Range(0.14f, 0.28f), rng.Range(0.24f, 0.46f),
						rng.Pick(PebbleTops));
				}

				// Ground flecks are tiny and scarce. Most are leaves; flower petals are
				// a rarer biome note rather than a uniform confetti scatter.
				var biome = terrain.Plan.RegionAt(fx, fz).Biome;
				float leafChance = biome switch
				{
					Biome.Forest => 0.008f,
					Biome.Sakura => 0.007f,
					Biome.Meadow => 0.005f,
					Biome.Plains => 0.003f,
					_ => 0.002f,
				};
				if (rng.Chance(leafChance))
				{
					f.Fleck(fx + rng.Range(-0.36f, 0.36f), y + 0.02f,
						fz + rng.Range(-0.36f, 0.36f), rng.Range(0.10f, 0.18f),
						rng.Range(0.045f, 0.075f), rng.Next() * Mathf.Pi,
						rng.Pick(Palette.FallenLeafColors));
				}

				float petalChance = biome switch
				{
					Biome.Sakura => 0.006f,
					Biome.Meadow => 0.0025f,
					Biome.Forest => 0.0015f,
					_ => 0f,
				};
				if (rng.Chance(petalChance))
				{
					f.Fleck(fx + rng.Range(-0.36f, 0.36f), y + 0.022f,
						fz + rng.Range(-0.36f, 0.36f), rng.Range(0.08f, 0.14f),
						rng.Range(0.04f, 0.07f), rng.Next() * Mathf.Pi,
						rng.Pick(Palette.PetalColors));
				}
				continue;
			}

			// Reeds and clods on open mud, wherever it is not already flooded.
			if (muddy)
			{
				if (rng.Chance(0.06f))
				{
					int n = rng.Int(1, 3);
					for (int k = 0; k < n; k++)
					{
						f.Tuft(fx + rng.Range(-0.30f, 0.30f), y - 0.03f, fz + rng.Range(-0.30f, 0.30f),
							rng.Range(0.09f, 0.15f), rng.Range(0.30f, 0.62f),
							ReedBase, ReedTip, rng.Next() * 6.28f,
							rng.Range(-0.20f, 0.20f), rng.Range(-0.20f, 0.20f));
					}
				}
				continue;
			}

			// Wind-scoured snow keeps only the odd exposed stone.
			if (snowy)
			{
				if (rng.Chance(0.004f))
				{
					f.Box(fx + rng.Range(-0.25f, 0.25f), y - 0.05f, fz + rng.Range(-0.25f, 0.25f),
						rng.Range(0.22f, 0.40f), rng.Range(0.12f, 0.24f), rng.Range(0.22f, 0.40f),
						rng.Pick(PebbleTops));
				}
				continue;
			}

			// Lichen flecks on bare stone and scree.
			if ((stony || scree) && rng.Chance(0.012f))
			{
				f.Box(fx + rng.Range(-0.25f, 0.25f), y - 0.06f, fz + rng.Range(-0.25f, 0.25f),
					rng.Range(0.22f, 0.40f), rng.Range(0.10f, 0.20f), rng.Range(0.22f, 0.40f),
					Lichen);
			}
		}

		// Reclamation last, and outside the column walk: a vine hangs at an
		// arbitrary height on a wall face, which the per-column pass above has no
		// way to reach — it only ever looks at the top of each column.
		Sprigs(f, ci, ck);

		return f.Empty ? null : f.Build();
	}

	/// <summary>Reject a long snow mark before any part can bridge a terrace edge.</summary>
	private static bool SupportsSnowTrace(VoxelGrid grid, int expectedHeight,
		Vector2 center, Vector2 along, float length)
	{
		// A metre-scale fleck can cross two or three voxel columns. The earlier
		// sub-block marks could assume their source cell supported every corner; a
		// longer mark cannot, or it bridges a terrace break and floats over the
		// cliff. Five centreline samples are enough for this narrow diamond.
		for (int i = -2; i <= 2; i++)
		{
			float t = i * 0.245f * length;
			int x = (int)MathF.Floor(center.X + along.X * t);
			int z = (int)MathF.Floor(center.Y + along.Y * t);
			if (x < 0 || z < 0 || x >= grid.Size || z >= grid.Size ||
				grid.HeightAt(x, z) != expectedHeight ||
				grid.At(x, expectedHeight - 1, z) != Palette.SNOW)
				return false;
		}
		return true;
	}

	private static readonly Vector3I[] MossFaces =
	{
		Vector3I.Right, Vector3I.Left, Vector3I.Up, Vector3I.Back, Vector3I.Forward,
	};
	private static readonly Vector3I[] BankFaces =
	{
		Vector3I.Left, Vector3I.Right, Vector3I.Forward, Vector3I.Back,
	};

	/// <summary>
	/// Add physical growth only to existing moss-stone cells. Cap profiles and
	/// sparse masonry are separate storage paths; process their overlap once.
	/// No region, structure, mask or collision is authored here.
	/// </summary>
	private static void MasonryGrowth(Field field, VoxelGrid grid, AtlasSectorData data,
		int x0, int z0, int x1, int z1)
	{
		for (int z = z0; z < z1; z++)
		for (int x = x0; x < x1; x++)
		{
			int y = grid.Top[z * grid.Size + x] - 1;
			if (y >= 0 && grid.At(x, y, z) == Palette.MOSS_STONE) GrowCell(x, y, z);
		}
		foreach (var cell in grid.PlacedIn(x0, z0, ChunkMesher.ChunkSize))
		{
			if (cell.X >= x1 || cell.Z >= z1 || cell.Material != Palette.MOSS_STONE ||
				cell.Y == grid.Top[cell.Z * grid.Size + cell.X] - 1) continue;
			GrowCell(cell.X, cell.Y, cell.Z);
		}

		void GrowCell(int x, int y, int z)
		{
			int water = data.WaterSurface[z * data.Width + x];
			if (water > 0 && y <= water) return;
			for (int face = 0; face < MossFaces.Length; face++)
			{
				Vector3I step = MossFaces[face];
				if (grid.At(x + step.X, y + step.Y, z + step.Z) != Palette.AIR) continue;
				Vector3 normal = step;
				var u = step.Y != 0 ? Vector3.Right : normal.Cross(Vector3.Up);
				var v = step.Y != 0 ? Vector3.Back : Vector3.Up;
				var center = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) + normal * 0.518f;
				var rng = new Draw(x + grid.OriginX, z + grid.OriginZ, y * 37 + face * 101 + 0xB07);
				// The material owns the patch; the draw only varies leaf shape inside
				// this face. Every tip stays inside its in-plane bounds, clear of lips.
				int count = rng.Int(4, 7);
				for (int leaf = 0; leaf < count; leaf++)
				{
					float angle = rng.Range(0f, Mathf.Tau);
					var along = u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
					var across = -u * Mathf.Sin(angle) + v * Mathf.Cos(angle);
					var root = center + u * rng.Range(-0.16f, 0.16f) + v * rng.Range(-0.16f, 0.16f);
					field.MossLeaf(root, along, across, normal, rng.Range(0.19f, 0.29f),
						rng.Range(0.12f, 0.20f), VineAttach.Lerp(VineTip, rng.Range(0.25f, 0.80f)));
				}
			}
		}
	}

	private static void SmallMushrooms(Field field, int gx, int gz, float x, float y, float z)
	{
		var rng = new Draw(gx, gz, 0xF061);
		float patch = _meadow.Fbm01(gx / 22f + 47f, gz / 22f, 2);
		if (!rng.Chance(ProductionTerrainGuide.SouthernLatitudeAt(gz) * .08f *
			Rng.Smoothstep(.40f, .66f, patch))) return;
		Color stem = Palette.Get(Palette.PLASTER).Top;
		Color cap = Palette.Get(patch > .54f ? Palette.LEAF_LILAC : Palette.LEAF_ROSE).Top;
		int count = rng.Int(2, 4);
		for (int i = 0; i < count; i++)
		{
			float px = x + rng.Range(-.18f, .18f), pz = z + rng.Range(-.18f, .18f);
			float height = rng.Range(.18f, .40f), width = rng.Range(.28f, .50f);
			field.Box(px, y - .02f, pz, .075f, height, .075f, stem);
			field.Box(px, y + height - .04f, pz, width, .10f, width, stem);
			field.Box(px, y + height + .02f, pz, width * .84f, .11f, width * .84f, cap);
		}
	}

	private static bool ReedClearance(VoxelGrid grid, int x, int z, int bed, int surface)
	{
		for (int y = bed; y <= surface + 2; y++)
			if (grid.At(x, y, z) != Palette.AIR) return false;
		return true;
	}

	/// <summary>Fine hanging gills on exposed cream undersides, owned by the same chunk as their cap cell.</summary>
	private static void MushroomGills(Field field, AtlasSectorWindow window, int x0, int z0)
	{
		var grid = window.Grid;
		// Sparse tiles use local indices; sort the bounded cream subset so a moved
		// window emits the same mesh order as well as the same visible geometry.
		foreach (var cell in grid.PlacedIn(x0, z0, ChunkMesher.ChunkSize)
			.Where(c => c.Material == Palette.LEAF_CREAM).OrderBy(c => c.Z).ThenBy(c => c.X).ThenBy(c => c.Y))
		{
			if (cell.Material != Palette.LEAF_CREAM) continue;
			int x = cell.X, z = cell.Z, y = cell.Y;
			string profile = window.GroundDetailSetAt(x, z);
			if (profile is not ("reed-root-moss" or "sand-reed-petal")) continue;
			int gx = grid.OriginX + x, gz = grid.OriginZ + z;
			float south = ProductionTerrainGuide.SouthernLatitudeAt(gz);
			if (south <= 0f || y < grid.Top[z * grid.Size + x] + 6 ||
				grid.At(x, y + 1, z) is not (>= Palette.LEAF_PINK and <= Palette.LEAF_ROSE) ||
				grid.At(x, y - 1, z) != Palette.AIR) continue;
			// Keep clear lanes underneath and let whole reaches of a cap stay quiet.
			float patch = _meadow.Fbm01(gx / 9f + 13f, gz / 9f - 23f, 2);
			var rng = new Draw(gx, gz, 0x6111 + y);
			if (!rng.Chance(south * Rng.Smoothstep(.30f, .65f, patch) * .42f)) continue;
			float length = rng.Range(1.2f, 3.2f);
			bool clear = true;
			for (int h = y - 1; h >= y - Mathf.CeilToInt(length) - 1; h--)
				if (grid.At(x, h, z) != Palette.AIR) { clear = false; break; }
			if (!clear) continue;
			float px = x + .5f + rng.Range(-.18f, .18f), pz = z + .5f + rng.Range(-.18f, .18f);
			float phase = rng.Range(0f, Mathf.Tau);
			Color cream = Palette.Get(Palette.LEAF_CREAM).Top;
			Color blush = cream.Lerp(Palette.Get(Palette.LEAF_BLUSH).Top, .3f);
			// Negative height pins the top and gives the hanging tip the wind weight.
			field.Tuft(px, y + .025f, pz, .09f, -length, cream, blush, phase, sway: .75f);
			for (int bead = 1; bead <= 3; bead++)
			{
				float t = bead / 3f;
				int start = field.Det.Count;
				field.Box(px, y + .025f - length * t, pz, .16f, .22f, .16f,
					bead == 3 ? cream.Lerp(FlowerHeart, .24f) : blush, phase: phase);
				// Every face shares the filament's interpolated displacement at this bead.
				for (int i = start; i < field.Det.Count; i += 2)
				{
					field.Det[i] = .75f * Mathf.Clamp((y + .025f - field.Pos[i / 2].Y) / length, 0f, 1f);
					// The opaque detail shader reserves vertex alpha .5 for a spore tip.
					// This is an emission tag, not transparency or another material/draw.
					if (bead == 3)
					{
						Color tint = field.Col[i / 2]; tint.A = .5f; field.Col[i / 2] = tint;
					}
				}
			}
		}
	}

	private static void MarshBankRoots(Field field, VoxelGrid grid, int gx, int gz, int x, int z, int height)
	{
		float patch = _meadow.Fbm01(gx / 14f - 37f, gz / 14f + 8f, 2);
		var rng = new Draw(gx, gz, 0xB411);
		if (!rng.Chance(ProductionTerrainGuide.SouthernLatitudeAt(gz) *
			Rng.Smoothstep(.38f, .65f, patch) * .38f)) return;
		foreach (var step in BankFaces)
		{
			int nx = x + step.X, nz = z + step.Z;
			if (nx < 0 || nz < 0 || nx >= grid.Size || nz >= grid.Size) continue;
			int drop = height - grid.Top[nz * grid.Size + nx];
			if (drop < 2 || grid.At(nx, height - 1, nz) != Palette.AIR) continue;
			float length = Math.Min(drop - .5f, rng.Range(.8f, 2.3f));
			bool clear = true;
			for (int y = height - 1; y >= height - Mathf.CeilToInt(length); y--)
				if (grid.At(nx, y, nz) != Palette.AIR) { clear = false; break; }
			if (!clear) continue;
			float px = x + .5f + step.X * .56f, pz = z + .5f + step.Z * .56f;
			float phase = rng.Range(0f, Mathf.Tau);
			field.Tuft(px, height - .08f, pz, .12f, -length, VineAttach, VineTip, phase, sway: .50f);
			for (int leaf = 1; leaf <= 3; leaf++)
			{
				float t = leaf / 3f;
				int start = field.Det.Count;
				field.Box(px, height - .08f - length * t, pz, .24f, .17f, .20f, VineAttach);
				for (int i = start; i < field.Det.Count; i += 2)
				{
					field.Det[i] = .50f * Mathf.Clamp((height - .08f - field.Pos[i / 2].Y) / length, 0f, 1f);
					field.Det[i + 1] = phase;
				}
			}
		}
	}

	private static void WetlandReeds(Field field, int gx, int gz, float x, float z, int bed, int surface)
	{
		float patch = _meadow.Fbm01(gx / 38f + 17f, gz / 38f - 29f, 3);
		float density = Rng.Smoothstep(.48f, .72f, patch) * .028f *
			ProductionTerrainGuide.SouthernLatitudeAt(gz);
		var rng = new Draw(gx, gz, 0x7EED);
		if (!rng.Chance(density)) return;
		int count = rng.Int(2, 3);
		for (int k = 0; k < count; k++)
		{
			float px = x + rng.Range(-.12f, .12f), pz = z + rng.Range(-.12f, .12f);
			float root = bed - .025f, height = surface - root + rng.Range(.65f, 1.10f);
			float phase = rng.Next() * Mathf.Tau;
			field.Grass(px, surface - .06f, pz, .25f, (root + height - surface) * .85f, VineAttach, VineTip,
				phase, rng.Range(-.18f, .18f), rng.Range(-.18f, .18f));
			field.Tuft(px, root, pz, .055f, height, ReedBase, ReedTip, phase, sway: .65f);
			int headStart = field.Det.Count;
			field.Box(px, root + height - .12f, pz, .12f, .24f, .12f,
				k % 2 == 0 ? FlowerTops[0] : ReedTip, sway: .65f, phase: phase);
			for (int i = headStart; i < field.Det.Count; i += 2) field.Det[i] = .65f;
		}
	}

	/// <summary>
	/// Sub-voxel detail for a compiled atlas window. The authored biome profile
	/// chooses the vocabulary; the visible cap and water depth decide what can
	/// physically grow at a column. Broad fields create clumps while the global
	/// coordinate draw supplies deterministic variation inside them.
	/// </summary>
	public static ArrayMesh BuildAtlas(AtlasSectorWindow window, int ci, int ck)
	{
		_meadow ??= new Noise2D(Seed + 71);
		_flowers ??= new Noise2D(Seed + 72);
		_fragments ??= new Noise2D(Seed + 74);
		_pavingDrift ??= new Noise2D(Seed + 75);

		AtlasSectorData data = window.Data;
		VoxelGrid grid = window.Grid;
		int cs = ChunkMesher.ChunkSize;
		int x0 = ci * cs, z0 = ck * cs;
		int x1 = Math.Min(data.Width, x0 + cs), z1 = Math.Min(data.Depth, z0 + cs);
		var f = new Field();

		for (int z = z0; z < z1; z++)
		for (int x = x0; x < x1; x++)
		{
			int index = z * data.Width + x;
			int h = grid.HeightAt(x, z);
			int natural = grid.Top[index];
			if (data.OriginZ + z > 5000 && natural < h && grid.At(x, h - 1, z) is >= Palette.LEAF_PINK and <= Palette.LEAF_ROSE &&
				grid.At(x, natural, z) == Palette.AIR && grid.At(x, natural + 1, z) == Palette.AIR)
				h = natural;
			if (h <= 0 || h >= grid.Height || grid.At(x, h, z) != Palette.AIR) continue;
			int water = data.WaterSurface[index];
			if (water > 0 && h <= water)
			{
				byte bed = grid.At(x, h - 1, z);
				if (water - h <= 3 && bed is Palette.MUD or Palette.SAND or Palette.MOSS &&
					window.GroundDetailSetAt(x, z).Contains("reed", StringComparison.Ordinal) &&
					ReedClearance(grid, x, z, h, water))
					WetlandReeds(f, data.OriginX + x, data.OriginZ + z, x + .5f, z + .5f, h, water);
				continue;
			}

			byte cap = grid.At(x, h - 1, z);
			bool grassy = Palette.IsGrassSurface(cap) || cap is Palette.MOSS or Palette.BLOSSOM_DRIFT;
			bool muddy = cap == Palette.MUD;
			bool snowy = cap == Palette.SNOW;
			bool scree = cap == Palette.SCREE;
			bool sandy = cap == Palette.SAND;
			bool earthy = cap == Palette.SOIL;
			bool stony = cap is Palette.STONE or Palette.STONE_PALE or Palette.STONE_WARM or
				Palette.MOSS_STONE or Palette.PAVING;
			if (!grassy && !muddy && !snowy && !scree && !sandy && !earthy && !stony) continue;
			// Material-scale plates are natural cap detail. Paving, masonry, rubble
			// and the authored moss mask retain their site-owned surface vocabulary.
			if (snowy || scree || sandy || earthy)
				SurfaceFragments(f, grid, data, x, z, h, cap);

			int gx = data.OriginX + x, gz = data.OriginZ + z;
			var rng = new Draw(gx, gz, 0x5EED);
			float y = h, fx = x + 0.5f, fz = z + 0.5f;
			string detailSet = window.GroundDetailSetAt(x, z);
			if (grassy && detailSet is "reed-root-moss" or "sand-reed-petal")
			{
				SmallMushrooms(f, gx, gz, fx, y, fz);
				// Only natural moss banks; authored paving and masonry use their own masks.
				if (cap == Palette.MOSS && h == natural)
					MarshBankRoots(f, grid, gx, gz, x, z, h);
			}
			if ((grassy || muddy) && h <= Terrain.Sea + 6 && data.Wetness[index] >= 160 &&
				detailSet is "reed-root-moss" or "sand-reed-petal")
				WetlandReeds(f, gx, gz, fx, fz, h, h);
			if (cap == Palette.PAVING && detailSet.Contains("petal", StringComparison.Ordinal))
				PavingPetals(f, gx, gz, fx, y, fz);

			// The atlas stores actual water height, so a shallow is a depth test
			// rather than a comparison with one legacy global sea plane.
			int depth = water > 0 ? water - data.Height[index] : 0;
			if ((sandy || grassy || muddy) && water > 0 && depth <= 3)
			{
				if (rng.Chance(detailSet.Contains("reed", StringComparison.Ordinal) ? 0.16f : 0.07f))
				{
					int count = rng.Int(2, 4);
					for (int k = 0; k < count; k++)
						f.Tuft(fx + rng.Range(-0.30f, 0.30f), water + 0.31f,
							fz + rng.Range(-0.30f, 0.30f), rng.Range(0.10f, 0.17f),
							rng.Range(0.38f, 0.76f), ReedBase, ReedTip,
							rng.Next() * 6.28f, rng.Range(-0.18f, 0.18f), rng.Range(-0.18f, 0.18f));
				}
				continue;
			}

			if (grassy)
			{
				var block = Palette.Get(cap);
				var light = new Color(block.Top.R * 1.08f, block.Top.G * 1.08f, block.Top.B * 1.08f);
				float lush = _meadow.Fbm01(gx * 0.045f, gz * 0.045f, 3);
				float meadowBand = Rng.Smoothstep(0.40f, 0.70f, lush);
				float density = detailSet is "talus-and-blanks" or "snow-windtrace"
					? 0.010f + lush * 0.025f
					: 0.04f + meadowBand * 0.42f;
				if (rng.Chance(density))
				{
					int count = rng.Int(2, 5);
					for (int k = 0; k < count; k++)
						f.Grass(fx + rng.Range(-0.25f, 0.25f), y - 0.03f,
							fz + rng.Range(-0.25f, 0.25f), rng.Range(0.16f, 0.30f),
							rng.Range(0.30f, 0.60f), TuftBase, light, rng.Next() * 6.28f,
							rng.Range(-0.14f, 0.14f), rng.Range(-0.14f, 0.14f));
				}

				bool flowers = detailSet.Contains("flower", StringComparison.Ordinal) ||
					detailSet.Contains("petal", StringComparison.Ordinal) ||
					detailSet == "reed-root-moss" && ProductionTerrainGuide.SouthernLatitudeAt(gz) > 0f;
				float flowerField = _flowers.Fbm01(gx * 0.028f, gz * 0.028f, 3);
				float flowerBand = Rng.Smoothstep(0.51f, 0.74f, flowerField);
				if (flowers && rng.Chance(flowerBand * 0.10f *
					(detailSet == "reed-root-moss" ? ProductionTerrainGuide.SouthernLatitudeAt(gz) : 1f)))
				{
					Color petals = rng.Pick(FlowerTops);
					int count = rng.Int(1, 2);
					for (int k = 0; k < count; k++)
						f.Flower(fx + rng.Range(-0.30f, 0.30f), y,
							fz + rng.Range(-0.30f, 0.30f), rng.Range(0.20f, 0.45f),
							petals, rng.Next() * Mathf.Tau);
				}
				if (detailSet.Contains("petal", StringComparison.Ordinal) &&
					rng.Chance(meadowBand * 0.035f))
				{
					int count = rng.Int(1, 3);
					for (int k = 0; k < count; k++)
						f.Fleck(fx + rng.Range(-0.34f, 0.34f), y + 0.018f,
							fz + rng.Range(-0.34f, 0.34f), rng.Range(0.09f, 0.17f),
							rng.Range(0.05f, 0.09f), rng.Next() * Mathf.Pi,
							rng.Pick(Palette.PetalColors));
				}
				if (rng.Chance(0.0055f))
					f.Box(fx + rng.Range(-0.25f, 0.25f), y - 0.05f,
						fz + rng.Range(-0.25f, 0.25f), rng.Range(0.24f, 0.46f),
						rng.Range(0.14f, 0.28f), rng.Range(0.24f, 0.46f), rng.Pick(PebbleTops));
				continue;
			}

			if (muddy)
			{
				if (rng.Chance(detailSet.Contains("reed", StringComparison.Ordinal) ? 0.07f : 0.025f))
					f.Tuft(fx + rng.Range(-0.30f, 0.30f), y - 0.03f,
						fz + rng.Range(-0.30f, 0.30f), rng.Range(0.09f, 0.15f),
						rng.Range(0.30f, 0.62f), ReedBase, ReedTip, rng.Next() * 6.28f,
						rng.Range(-0.20f, 0.20f), rng.Range(-0.20f, 0.20f));
				continue;
			}

			if (sandy)
			{
				if (rng.Chance(0.0045f))
					f.Box(fx + rng.Range(-0.28f, 0.28f), y - 0.05f,
						fz + rng.Range(-0.28f, 0.28f), rng.Range(0.20f, 0.40f),
						rng.Range(0.10f, 0.22f), rng.Range(0.20f, 0.40f), rng.Pick(PebbleTops));
				if (detailSet.Contains("petal", StringComparison.Ordinal) && rng.Chance(0.0025f))
					f.Fleck(fx + rng.Range(-0.36f, 0.36f), y + 0.02f,
						fz + rng.Range(-0.36f, 0.36f), rng.Range(0.08f, 0.14f),
						rng.Range(0.04f, 0.07f), rng.Next() * Mathf.Pi,
						rng.Pick(Palette.PetalColors));
				continue;
			}

			if (snowy)
			{
				// A snow shelf cannot rely on turf, flowers or colour patches to show
				// scale. The first atlas pass therefore left hundreds of metres of cap
				// as one unmarked white card. Wind traces are sparse *inside* a broad
				// coherent field: the field makes drifts gather, while the keyed column
				// draw only decides a few metre-scale parallel strokes in that region.
				// Using the hash alone here would turn the whole cold shelf into even
				// confetti, the same failure class as the old water-detail scatter.
				float driftField = _meadow.Fbm01(gx / 72f, gz / 72f, 3);
				float driftBand = Rng.Smoothstep(0.60f, 0.82f, driftField);
				// Fewer but longer groups survive the long-lens atlas framing. The former
				// one-to-two-metre marks became isolated pixels at the wide/far review
				// distances even though their count was already high enough.
				float traceChance = driftBand * (0.0015f + driftBand * 0.0045f);
				if (rng.Chance(traceChance))
				{
					Color snow = Palette.Get(Palette.SNOW).Top;
					var trace = new Color(snow.R * 0.89f, snow.G * 0.92f,
						snow.B * 1.02f, 1f);
					int count = rng.Int(3, 5);
					float directionField = _flowers.Fbm01(gx / 180f + 17.4f,
						gz / 180f - 9.6f, 2);
					float rotation = -0.38f + (directionField - 0.5f) * 0.34f +
						rng.Range(-0.045f, 0.045f);
					var along = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));
					var across = new Vector2(-along.Y, along.X);
					float spacing = rng.Range(0.18f, 0.30f);
					for (int k = 0; k < count; k++)
					{
						float lane = (k - (count - 1) * 0.5f) * spacing;
						Vector2 offset = across * lane + along * rng.Range(-0.22f, 0.22f);
						var centre = new Vector2(fx + offset.X, fz + offset.Y);
						float length = rng.Range(2.40f, 5.80f);
						if (!SupportsSnowTrace(grid, h, centre, along, length)) continue;
						f.Fleck(centre.X, y + 0.018f, centre.Y, length,
							rng.Range(0.10f, 0.19f), rotation, trace);
					}
				}
				// Exposed stone belongs to broad wind-scoured patches. A naked per-cell
				// chance produced equally spaced pepper over every snow shelf even when
				// the individual stones were rare.
				float exposureField = _flowers.Fbm01(gx / 96f + 23.7f,
					gz / 96f - 11.2f, 3);
				float exposure = Rng.Smoothstep(0.66f, 0.84f, exposureField) *
					(1f - Rng.Smoothstep(0.47f, 0.65f, driftField));
				if (rng.Chance(exposure * 0.010f))
				{
					int stones = rng.Int(1, 2);
					for (int k = 0; k < stones; k++)
						f.Box(fx + rng.Range(-0.34f, 0.34f), y - 0.05f,
							fz + rng.Range(-0.34f, 0.34f), rng.Range(0.24f, 0.46f),
							rng.Range(0.12f, 0.24f), rng.Range(0.24f, 0.46f), rng.Pick(PebbleTops));
				}
				continue;
			}

			if (stony || scree)
			{
				if (detailSet == "snow-windtrace")
				{
					// Snow-scoured cap patches are frost-bare, not lichen lawns. The
					// former uniform 1% lichen roll printed green dots across every
					// exposed region at far zoom. Admit a few pale shards only inside
					// coherent talus fields, and keep each occurrence as a small group.
					float talusField = _flowers.Fbm01(gx / 78f + 41.3f,
						gz / 78f - 26.8f, 3);
					float talus = Rng.Smoothstep(0.68f, 0.84f, talusField);
					if (rng.Chance(talus * 0.0035f))
					{
						int count = rng.Int(2, 4);
						for (int k = 0; k < count; k++)
							f.Box(fx + rng.Range(-0.38f, 0.38f), y - 0.06f,
								fz + rng.Range(-0.38f, 0.38f), rng.Range(0.20f, 0.38f),
								rng.Range(0.10f, 0.20f), rng.Range(0.20f, 0.38f),
								rng.Pick(PebbleTops));
					}
				}
				else
				{
					float stoneChance = detailSet is "scree-and-heather" or "fern-moss-flower"
						? 0.018f : 0.010f;
					if (rng.Chance(stoneChance))
						f.Box(fx + rng.Range(-0.25f, 0.25f), y - 0.06f,
							fz + rng.Range(-0.25f, 0.25f), rng.Range(0.22f, 0.40f),
							rng.Range(0.10f, 0.20f), rng.Range(0.22f, 0.40f), Lichen);
				}
			}
		}

		MushroomGills(f, window, x0, z0);
		MasonryGrowth(f, grid, data, x0, z0, x1, z1);
		return f.Empty ? null : f.Build();
	}

	/// <summary>Wind litter on existing dry blossom-region paving, never new growth or damage.</summary>
	private static void PavingPetals(Field field, int gx, int gz, float x, float y, float z)
	{
		// A wavelength field admits entire drift patches; the independent draw only
		// distributes tiny petals inside them and cannot perturb existing detail.
		float drift = _pavingDrift.Fbm01(gx / 18f, gz / 18f, 3);
		float band = Rng.Smoothstep(0.43f, 0.68f, drift);
		var rng = new Draw(gx, gz, 0x9E7A);
		if (!rng.Chance(band * 0.48f)) return;
		int count = rng.Int(3, 7);
		float angle = -0.38f + (drift - 0.5f) * 0.7f;
		for (int k = 0; k < count; k++)
			field.Fleck(x + rng.Range(-0.30f, 0.30f), y + 0.018f,
				z + rng.Range(-0.30f, 0.30f), rng.Range(0.12f, 0.24f),
				rng.Range(0.065f, 0.12f), angle + rng.Range(-0.65f, 0.65f),
				rng.Pick(Palette.PetalColors));
	}

	private static void SurfaceFragments(Field field, VoxelGrid grid, AtlasSectorData data,
		int x, int z, int height, byte cap)
	{
		int gx = x + data.OriginX, gz = z + data.OriginZ;
		float deposit = _fragments.Fbm01(gx / 32f, gz / 32f, 3);
		float band = Rng.Smoothstep(0.39f, 0.69f, deposit);
		if (band < 0.10f) return;
		bool lip = grid.At(x - 1, height - 1, z) == Palette.AIR ||
			grid.At(x + 1, height - 1, z) == Palette.AIR ||
			grid.At(x, height - 1, z - 1) == Palette.AIR ||
			grid.At(x, height - 1, z + 1) == Palette.AIR;
		var rng = new Draw(gx, gz, 0xC41F);
		float chance = band * band * 0.16f + (lip ? band * 0.18f : 0f);
		if (!rng.Chance(chance)) return;
		bool snow = cap == Palette.SNOW;
		bool sand = cap == Palette.SAND;
		Color ground = Palette.Get(cap).Top;
		int count = rng.Int(1, 3);
		for (int k = 0; k < count; k++)
		{
			float size = k == 0 ? rng.Range(0.35f, 0.65f) : rng.Range(0.14f, 0.29f);
			float depth = size * rng.Range(0.55f, 0.85f);
			float angle = rng.Int(0, 3) * Mathf.Pi * 0.5f + rng.Range(-0.16f, 0.16f);
			float c = Mathf.Abs(Mathf.Cos(angle)), s = Mathf.Abs(Mathf.Sin(angle));
			// Clamp the rotated footprint, not just its centre. No chip bridges a
			// terrace break or crosses ownership at a chunk/window boundary.
			float rx = (size * c + depth * s) * 0.5f;
			float rz = (size * s + depth * c) * 0.5f;
			float px = x + 0.5f + rng.Range(-0.47f + rx, 0.47f - rx);
			float pz = z + 0.5f + rng.Range(-0.47f + rz, 0.47f - rz);
			Color mineral = snow ? ground : ground.Lerp(rng.Pick(PebbleTops), sand ? 0.55f : 0.32f);
			float tone = rng.Range(snow ? 0.96f : 0.82f, snow ? 1.08f : 1.04f);
			mineral = new Color(mineral.R * tone, mineral.G * tone, mineral.B * tone);
			field.Chip(px, height, pz, size, rng.Range(0.045f, snow ? 0.13f : 0.11f),
				depth, angle, rng.Range(0.14f, 0.30f), mineral);
		}
	}
}
