using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Petalfell.Core;
using Petalfell.Render;
using Petalfell.World;

namespace Petalfell.Tools;

/// <summary>Geometry boundaries, horizon continuity and particle window ownership.</summary>
public partial class LookRenderSmoke : Node
{
	public override void _Ready()
	{
		try
		{
			CheckEdges();
			CheckBevelJunctions();
			CheckClock();
			CheckDrift();
			CheckMirrorRegistration();
			CheckMirrorTransitions();
			CheckLocalEdits();
			CheckSurfaceFragments();
			CheckPavingPetals();
			CheckMeadowPlants();
			GD.Print("[look-render-smoke] convex lips/coplanar chunk seam, 255 watertight junctions, unchanged box collision, " +
				"4097 clock samples/horizon fade, bounded global particle handoff, mirror registration/height fades, local edit ownership, supported fragment/petal/plant handoff and plant wind seams passed");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PushError($"[look-render-smoke] {ex}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool value, string message)
	{
		if (!value) throw new InvalidOperationException(message);
	}

	private static void CheckLocalEdits()
	{
		// Chunk width 24 and edit-tile width 32 do not coincide. Test every
		// clipped window around their boundaries against an independent oracle.
		var grid = new VoxelGrid(73, 16);
		var expected = new Dictionary<Vector3I, byte>();
		for (int z = 0; z < grid.Size; z += 3)
		for (int x = 0; x < grid.Size; x += 5)
		{
			var p = new Vector3I(x, (x + z) % 13, z);
			grid.Set(p.X, p.Y, p.Z, Palette.STONE);
			byte material = (x + z) % 2 == 0 ? Palette.MOSS_STONE : Palette.AIR;
			grid.Set(p.X, p.Y, p.Z, material);
			expected[p] = material;
		}
		foreach (int width in new[] { 0, 1, 24, 32, 48, 100 })
		foreach (int z0 in new[] { -40, -1, 0, 23, 31, 32, 47, 71, 73, 100 })
		foreach (int x0 in new[] { -40, -1, 0, 23, 31, 32, 47, 71, 73, 100 })
		{
			var actual = grid.PlacedIn(x0, z0, width).ToArray();
			var wanted = expected.Where(p => p.Key.X >= x0 && p.Key.X < x0 + width &&
				p.Key.Z >= z0 && p.Key.Z < z0 + width).ToDictionary(p => p.Key, p => p.Value);
			Require(actual.Length == wanted.Count, "local edit query duplicated/lost boundary cells");
			foreach (var cell in actual)
				Require(wanted.TryGetValue(new Vector3I(cell.X, cell.Y, cell.Z), out byte value) &&
					value == cell.Material, "local edit query lost overwrite/AIR ownership");
		}
	}

	private static void CheckSurfaceFragments()
	{
		// Soil has no other detail vocabulary. Use it to isolate mineral plates
		// and verify real mesh support across steps, water, blockers and handoff.
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		const int size = 96, originZ = 3840;
		AtlasSectorWindow Window(int originX)
		{
			var data = new AtlasSectorData(0, 0, originX, originZ, size, 0,
				size, size, 16, 0, "fragment-smoke");
			var grid = new VoxelGrid(size, 16, 7, originX, originZ);
			for (int z = 0; z < size; z++)
			for (int x = 0; x < size; x++)
			{
				int gx = originX + x, gz = originZ + z, i = z * size + x;
				int h = 5 + (gx / 7 + gz / 11) % 3;
				data.Height[i] = (ushort)h;
				data.WaterSurface[i] = gz % 13 == 0 ? (ushort)8 : (ushort)0;
				grid.Describe(x, z, h, Palette.SOIL, Palette.SOIL);
				if (gx % 17 == 0) grid.Set(x, h, z, Palette.TRUNK);
			}
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		int vertices = 0;
		foreach (int origin in new[] { 4368, 9360, 10560 })
		{
			var a = Window(origin);
			var b = Window(origin + ChunkMesher.ChunkSize);
			// Compare the shared interior, where both windows own the neighbour
			// apron used to classify terrace lips. The clipped outermost cells
			// are outside the production window's meshable stream margin.
			for (int z = 1; z < 3; z++)
			{
				using var meshA = GroundDetail.BuildAtlas(a, 2, z);
				using var meshB = GroundDetail.BuildAtlas(b, 1, z);
				Require((meshA == null) == (meshB == null), "handoff changed empty fragment mesh");
				if (meshA == null) continue;
				var aa = meshA.SurfaceGetArrays(0);
				var bb = meshB.SurfaceGetArrays(0);
				var pa = aa[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var pb = bb[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var normals = aa[(int)Mesh.ArrayType.Normal].AsVector3Array();
				Require(pa.Length == pb.Length && pa.Length <= ChunkMesher.ChunkSize * ChunkMesher.ChunkSize * 108,
					"fragment count changed at handoff or exceeded its cell bound");
				vertices += pa.Length;
				for (int i = 0; i < pa.Length; i++)
				{
					Require((pa[i] + a.GlobalOrigin - pb[i] - b.GlobalOrigin).Length() < 0.002f,
						"fragment moved during a window replacement");
					int x = Mathf.FloorToInt(pa[i].X), cz = Mathf.FloorToInt(pa[i].Z);
					int h = a.Grid.HeightAt(x, cz);
					Require(a.Grid.At(x, h - 1, cz) == Palette.SOIL &&
						a.Data.WaterSurface[cz * size + x] < h &&
						pa[i].Y >= h + 0.01f && pa[i].Y <= h + 0.111f,
						"fragment crossed its supported dry cap or grew onto a blocker");
					Require(normals[i].Y > 0f && Mathf.Abs(normals[i].Length() - 1f) < 0.001f,
						"fragment has an inverted or invalid facet normal");
				}
			}
		}
		Require(vertices > 100, "fragment fixture did not exercise geometry");
	}

	private static void CheckPavingPetals()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		int profile = atlas.BiomeCatalog.Profiles.FindIndex(p => p.GroundDetailSetId == "petal-grass-flower");
		Require(profile >= 0, "missing blossom profile for paving litter");
		const int size = 96, originZ = 4600;
		AtlasSectorWindow Window(int originX)
		{
			var data = new AtlasSectorData(0, 0, originX, originZ, size, 0,
				size, size, 16, 0, "paving-petal-smoke");
			var grid = new VoxelGrid(size, 16, 7, originX, originZ);
			for (int z = 0; z < size; z++)
			for (int x = 0; x < size; x++)
			{
				int gx = originX + x, gz = originZ + z, i = z * size + x;
				int h = 5 + (gx / 7 + gz / 11) % 3;
				data.Height[i] = (ushort)h;
				data.Profile[i] = data.SecondaryProfile[i] = (byte)profile;
				data.WaterSurface[i] = gz % 13 == 0 ? (ushort)8 : (ushort)0;
				// Interleaved non-paving and blocked columns must never carry litter.
				grid.Describe(x, z, h, gx % 9 == 0 ? Palette.STONE_PALE : Palette.PAVING, Palette.STONE);
				if (gx % 17 == 0) grid.Set(x, h, z, Palette.TRUNK);
			}
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		int petals = 0;
		foreach (int origin in new[] { 4368, 9360, 10560 })
		{
			var a = Window(origin);
			var b = Window(origin + ChunkMesher.ChunkSize);
			for (int z = 1; z < 3; z++)
			{
				using var meshA = GroundDetail.BuildAtlas(a, 2, z);
				using var meshB = GroundDetail.BuildAtlas(b, 1, z);
				Require((meshA == null) == (meshB == null), "handoff changed empty paving detail");
				if (meshA == null) continue;
				var aa = meshA.SurfaceGetArrays(0);
				var bb = meshB.SurfaceGetArrays(0);
				var pa = aa[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var pb = bb[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var colors = aa[(int)Mesh.ArrayType.Color].AsColorArray();
				Require(pa.Length == pb.Length, "paving detail count changed at handoff");
				var counts = new Dictionary<Vector2I, int>();
				for (int i = 0; i < pa.Length; i++)
				{
					Require((pa[i] + a.GlobalOrigin - pb[i] - b.GlobalOrigin).Length() < 0.002f,
						"paving detail moved at window replacement");
					// ArrayMesh packs vertex colour to UNORM8; allow one channel step
					// when identifying the pink litter among the existing green lichen.
					if (!Palette.PetalColors.Any(c => Mathf.Abs(c.R - colors[i].R) <= 1f / 255f &&
						Mathf.Abs(c.G - colors[i].G) <= 1f / 255f &&
						Mathf.Abs(c.B - colors[i].B) <= 1f / 255f)) continue;
					petals++;
					int x = Mathf.FloorToInt(pa[i].X), cz = Mathf.FloorToInt(pa[i].Z);
					int h = a.Grid.HeightAt(x, cz);
					Require(a.Grid.At(x, h - 1, cz) == Palette.PAVING &&
						a.Grid.At(x, h, cz) == Palette.AIR && a.Data.WaterSurface[cz * size + x] < h &&
						Mathf.Abs(pa[i].Y - h - 0.018f) < 0.001f &&
						pa[i].X - x > 0.079f && pa[i].X - x < 0.921f &&
						pa[i].Z - cz > 0.079f && pa[i].Z - cz < 0.921f,
						"petal escaped its dry paving support or crossed a lip");
					var cell = new Vector2I(x, cz);
					counts[cell] = counts.GetValueOrDefault(cell) + 1;
				}
				Require(counts.Values.All(n => n <= 28), "petal drift exceeded seven quads per cell");
			}
		}
		Require(petals > 100, "paving fixture did not exercise petal geometry");
	}

	private static void CheckMeadowPlants()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		int profile = atlas.BiomeCatalog.Profiles.FindIndex(p => p.GroundDetailSetId == "petal-grass-flower");
		Require(profile >= 0, "missing blossom profile for meadow fixture");
		const int size = 96, originZ = 4600;
		AtlasSectorWindow Window(int originX)
		{
			var data = new AtlasSectorData(0, 0, originX, originZ, size, 0,
				size, size, 16, 0, "meadow-plant-smoke");
			var grid = new VoxelGrid(size, 16, 7, originX, originZ);
			for (int z = 0; z < size; z++)
			for (int x = 0; x < size; x++)
			{
				int gx = originX + x, gz = originZ + z, i = z * size + x;
				int h = 5 + (gx / 7 + gz / 11) % 3;
				data.Height[i] = (ushort)h;
				data.Profile[i] = data.SecondaryProfile[i] = (byte)profile;
				data.WaterSurface[i] = gz % 13 == 0 ? (ushort)8 : (ushort)0;
				grid.Describe(x, z, h, gx % 9 == 0 ? Palette.PAVING : Palette.GRASS, Palette.STONE);
				if (gx % 17 == 0) grid.Set(x, h, z, Palette.TRUNK);
			}
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		int grassTips = 0, flowerVertices = 0, sharedVertices = 0;
		foreach (int origin in new[] { 4368, 9360, 10560 })
		{
			var a = Window(origin);
			var b = Window(origin + ChunkMesher.ChunkSize);
			for (int z = 1; z < 3; z++)
			{
				using var meshA = GroundDetail.BuildAtlas(a, 2, z);
				using var meshB = GroundDetail.BuildAtlas(b, 1, z);
				Require(meshA != null && meshB != null, "meadow fixture produced no detail");
				var aa = meshA.SurfaceGetArrays(0);
				var bb = meshB.SurfaceGetArrays(0);
				var pa = aa[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var pb = bb[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var na = aa[(int)Mesh.ArrayType.Normal].AsVector3Array();
				var da = aa[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
				var db = bb[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
				Require(pa.Length == pb.Length && da.SequenceEqual(db), "plant shape/wind changed at handoff");
				var joints = new Dictionary<Vector3, Vector2>();
				for (int i = 0; i < pa.Length; i++)
				{
					Require((pa[i] + a.GlobalOrigin - pb[i] - b.GlobalOrigin).Length() < 0.002f,
						"meadow detail moved at window replacement");
					float weight = da[i * 2], phase = da[i * 2 + 1];
					var wind = new Vector2(weight, phase);
					if (joints.TryGetValue(pa[i], out var other))
					{
						Require(wind.IsEqualApprox(other), "coincident plant facets separate under wind");
						if (weight > 0f) sharedVertices++;
					}
					else joints.Add(pa[i], wind);
					if (weight <= 0f) continue;
					int x = Mathf.FloorToInt(pa[i].X), cz = Mathf.FloorToInt(pa[i].Z);
					int h = a.Grid.HeightAt(x, cz);
					Require(a.Grid.At(x, h - 1, cz) == Palette.GRASS &&
						a.Grid.At(x, h, cz) == Palette.AIR && a.Data.WaterSurface[cz * size + x] < h &&
						pa[i].Y >= h - 0.031f && pa[i].Y <= h + 0.61f,
						"plant escaped its dry supporting grass cell or grew through a blocker");
					Require(weight <= 1f && float.IsFinite(phase), "invalid plant wind attribute");
					Require(Mathf.Abs(na[i].Length() - 1f) < 0.003f, "invalid plant normal");
					if (weight > 0.5f)
					{
						Require(na[i].Y > 0.99f, "grass lost its upward lighting normal");
						grassTips++;
					}
					else if (weight == 0.5f) flowerVertices++;
				}
			}
		}
		Require(grassTips > 100 && flowerVertices > 100 && sharedVertices > 100,
			"meadow fixture did not exercise grass, flower heads and animated facet seams");
	}

	private static void CheckMirrorRegistration()
	{
		// Surface points must register at the same pixels, with X inverted to
		// preserve camera handedness. Exercise distant atlas coordinates, four
		// camera quarters and distinct lake heights, rather than a single origin.
		foreach (float height in new[] { 24.35f, 68.35f, 120.35f })
		for (int quarter = 0; quarter < 4; quarter++)
		{
			var centre = new Vector3(9800, height, 4600);
			var basis = Basis.FromEuler(new Vector3(-0.54f, quarter * Mathf.Pi * 0.5f + .3f, 0));
			var source = new Transform3D(basis, centre + basis.Z * 75f);
			var mirror = PlanarReflection.MirrorTransform(source, height);
			Require(mirror.Basis.Determinant() > .99f, "mirror camera reversed winding");
			foreach (var offset in new[] { Vector3.Zero, new Vector3(6, 0, -11), new Vector3(-9, 0, 4) })
			{
				Vector3 a = source.AffineInverse() * (centre + offset);
				Vector3 b = mirror.AffineInverse() * (centre + offset);
				Require((b - new Vector3(-a.X, a.Y, a.Z)).Length() < .003f,
					"mirror lookup moved a point on the water plane");
			}
		}
	}

	private static void CheckMirrorTransitions()
	{
		using var mirror = new PlanarReflection();
		mirror.Setup(null, null, 24.35f);
		const float dt = 1f / 30f;
		for (int i = 0; i < 30; i++) mirror.AdvancePlane(24.35f, dt);
		Require(Math.Abs(mirror.ReflectionWeight - .72f) < .001f, "mirror never reached its steady weight");
		bool crossedSky = false, reachedNewPlane = false;
		for (int i = 0; i < 60; i++)
		{
			float before = mirror.ReflectionWeight;
			mirror.AdvancePlane(68.35f, dt);
			Require(Math.Abs(mirror.ReflectionWeight - before) <= dt * 2f + .00001f,
				"water-height switch caused an abrupt reflection opacity step");
			if (!mirror.CurrentPlane.HasValue) crossedSky = true;
			else if (Math.Abs(mirror.CurrentPlane.Value - 68.35f) < .001f)
			{
				Require(crossedSky, "mirror jumped to another elevation while visible");
				reachedNewPlane = true;
			}
		}
		Require(reachedNewPlane && mirror.ReflectionWeight > .71f, "new water plane never faded in");
		// A brief view of another river level must be cancellable without moving
		// the old reflection or leaving the fade stuck halfway through.
		for (int i = 0; i < 3; i++) mirror.AdvancePlane(120.35f, dt);
		for (int i = 0; i < 30; i++) mirror.AdvancePlane(68.35f, dt);
		Require(mirror.CurrentPlane == 68.35f && mirror.ReflectionWeight > .71f,
			"cancelled plane selection did not recover the registered mirror");
		for (int i = 0; i < 30; i++) mirror.AdvancePlane(null, dt);
		Require(!mirror.CurrentPlane.HasValue && mirror.ReflectionWeight == 0f,
			"dry terrain retained an active reflection");
		mirror.Free();
	}

	private static void CheckEdges()
	{
		int seam = ChunkMesher.ChunkSize;
		var grid = new VoxelGrid(seam * 2, 16, 7, 9216, 3840);
		grid.Set(seam - 1, 4, 7, Palette.STONE);
		grid.Set(seam, 4, 7, Palette.STONE);
		grid.Heights[7 * grid.Size + seam - 1] = 5;
		grid.Heights[7 * grid.Size + seam] = 5;
		for (int ci = 0; ci < 2; ci++)
		{
			var data = ChunkMesher.Build(grid, ci, 0);
			Require(data.CollisionFaces.Length == 30, "two joined voxels gained/lost collision faces");
			var arrays = data.Surface.SurfaceGetArrays(0);
			var positions = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
			var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
			var edges = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
			int topVertices = 0;
			for (int v = 0; v < positions.Length; v++)
			{
				Require(positions[v].X >= seam - 1 && positions[v].X <= seam + 1,
					"bevel escaped the authored voxel bounds");
				if (normals[v].Y < 0.99f) continue; // GPU normal packing is not bit-exact.
				topVertices++;
				Require((int)edges[v].X == (ci == 0 ? 7 : 11), "coplanar chunk seam received a bevel");
			}
			Require(topVertices == 4, "expected exactly one flat top quad per chunk");
			Require(data.CollisionFaces.All(p => p == p.Round()), "bevel entered collision geometry");
			data.Surface.Dispose();
			data.Ink?.Dispose();
		}
	}

	private static void CheckBevelJunctions()
	{
		// Every occupancy of a 2x2x2 neighbourhood, with the shared vertex on a
		// real chunk border. This catches tapered junction cracks that an isolated
		// prettified cube would miss, including diagonal-touch non-manifolds.
		for (int mask = 1; mask < 256; mask++)
		{
			int seam = ChunkMesher.ChunkSize;
			var grid = new VoxelGrid(seam * 2, 16, 7, 9216, 3840);
			var occupied = new HashSet<Vector3I>();
			for (int bit = 0; bit < 8; bit++)
			{
				if ((mask & (1 << bit)) == 0) continue;
				var at = new Vector3I(seam - 1 + (bit & 1), 4 + ((bit >> 1) & 1), 7 + ((bit >> 2) & 1));
				occupied.Add(at); grid.Set(at.X, at.Y, at.Z, Palette.STONE);
				int index = at.Z * grid.Size + at.X;
				grid.Heights[index] = (short)Math.Max(grid.Heights[index], at.Y + 1);
			}
			var edges = new Dictionary<(Vector3I, Vector3I), int>();
			int collisionVertices = 0;
			static int Compare(Vector3I a, Vector3I b) => a.X != b.X ? a.X.CompareTo(b.X)
				: a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.Z.CompareTo(b.Z);
			void Edge(Vector3 a, Vector3 b)
			{
				var qa = (Vector3I)(a * 10000f).Round();
				var qb = (Vector3I)(b * 10000f).Round();
				var pair = Compare(qa, qb) < 0 ? (qa, qb) : (qb, qa);
				edges[pair] = edges.GetValueOrDefault(pair) + 1;
			}
			for (int ci = 0; ci < 2; ci++)
			{
				var data = ChunkMesher.Build(grid, ci, 0);
				if (data.Empty) continue;
				collisionVertices += data.CollisionFaces.Length;
				Require(data.CollisionFaces.All(p => p == p.Round()), $"collision bevel in occupancy {mask}");
				var arrays = data.Surface.SurfaceGetArrays(0);
				var points = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
				var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
				for (int i = 0; i < indices.Length; i += 3)
				{
					Vector3 a = points[indices[i]], b = points[indices[i+1]], c = points[indices[i+2]];
					Vector3 cross = (b-a).Cross(c-a);
					Require(cross.LengthSquared() > 1e-12f && cross.Dot(normals[indices[i]]) < 0,
						$"degenerate/reversed bevel in occupancy {mask}");
					Edge(a,b); Edge(b,c); Edge(c,a);
				}
				data.Surface.Dispose(); data.Ink?.Dispose();
			}
			int exposed = 0;
			foreach (Vector3I at in occupied)
			foreach (Vector3I direction in new[] {Vector3I.Left, Vector3I.Right, Vector3I.Up,
				Vector3I.Down, Vector3I.Forward, Vector3I.Back})
				if (!occupied.Contains(at + direction)) exposed++;
			Require(collisionVertices == exposed * 6, $"collision changed in occupancy {mask}");
			Require(edges.Values.All(count => count % 2 == 0), $"open bevel edge in occupancy {mask}");
		}
	}

	private void CheckClock()
	{
		DayCycle.RegisterGlobals();
		var env = new Godot.Environment();
		var key = new DirectionalLight3D();
		var fill = new DirectionalLight3D();
		AddChild(key); AddChild(fill);
		var day = new DayCycle { Paused = true };
		AddChild(day);
		day.Setup(env, key, fill, null, null, 7);
		float previous = -1f;
		for (int sample = 0; sample <= 4096; sample++)
		{
			day.TimeOfDay = (sample % 4096) / 4096f;
			day._Process(0);
			Require(float.IsFinite(key.LightEnergy) && key.LightEnergy >= 0f, "invalid key energy");
			Require(env.AmbientLightEnergy > 0.10f, "clock extinguished ambient light");
			if (previous >= 0f) Require(Mathf.Abs(key.LightEnergy - previous) < 0.03f,
				$"direct light popped at {day.TimeOfDay}");
			if (sample is 1024 or 3072) Require(key.LightEnergy < 0.001f,
				"sun/moon ownership changed with a visible direct key");
			previous = key.LightEnergy;
		}
		day.QueueFree(); key.QueueFree(); fill.QueueFree();
		env.Dispose();
	}

	private void CheckDrift()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		int profile = atlas.BiomeCatalog.Profiles.FindIndex(p => p.GroundDetailSetId == "petal-grass-flower");
		Require(profile >= 0, "missing blossom profile");
		AtlasSectorWindow Window(int originX)
		{
			var data = new AtlasSectorData(0, 0, originX, 3840, 96, 0, 96, 96, 16, 0, "look-smoke");
			var grid = new VoxelGrid(96, 16, 7, originX, 3840);
			for (int z = 0; z < 96; z++)
			for (int x = 0; x < 96; x++)
			{
				int i = z * 96 + x;
				data.Height[i] = 5; data.Land[i] = 1;
				data.Profile[i] = data.SecondaryProfile[i] = (byte)profile;
				grid.Describe(x, z, 5, Palette.GRASS, Palette.SOIL);
			}
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		AtlasSectorWindow current = Window(9216);
		var at = new Vector3(9264, 5, 3888);
		var drift = new AmbientDrift();
		AddChild(drift);
		drift.Setup(() => current, 7, at);
		drift.Advance(at, 0, 0);
		var leaves = drift.GetNode<MultiMeshInstance3D>("FallingLeaves").Multimesh;
		var fireflies = drift.GetNode<MultiMeshInstance3D>("NightFireflies").Multimesh;
		Require(leaves.InstanceCount == 32 && fireflies.InstanceCount == 18 && drift.GetChildCount() == 2,
			"airborne detail exceeded its two-draw/50-instance bound");
		Require(leaves.Mesh is ArrayMesh && leaves.Mesh.GetSurfaceCount() == 1,
			"falling lamina lost its shared single mesh");
		var arrays = leaves.Mesh.SurfaceGetArrays(0);
		var points = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
		var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
		Require(points.Length <= 36 && points.Length >= 12 && points.Length % 3 == 0,
			"falling lamina exceeded the old box triangle budget");
		Require(points.All(p => Math.Abs(p.X) <= .5f && Math.Abs(p.Z) <= .5f && p.Y >= 0 && p.Y <= 1),
			"falling mesh escaped the existing landing/instance scale bounds");
		Require(points.Max(p => p.Y) - points.Min(p => p.Y) > .5f, "falling petal lost its physical cup");
		for (int i = 0; i < points.Length; i += 3)
		{
			Vector3 cross = (points[i + 1] - points[i]).Cross(points[i + 2] - points[i]);
			Require(cross.LengthSquared() > 1e-8f && cross.Dot(normals[i]) < 0 && normals[i].Y > 0,
				"degenerate or reversed falling petal facet");
		}
		// Headless's dummy rendering server does not retain MultiMesh transforms.
		// Inspect simulation positions; GPU upload is covered by real captures.
		Vector3[] Positions() => ((IEnumerable)typeof(AmbientDrift)
			.GetField("_falling", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(drift)!)
			.Cast<object>().Select(piece => (Vector3)piece.GetType().GetField("Position")!
			.GetValue(piece)!).ToArray();
		var before = Positions();
		var sourceColors = Palette.AirLeafColors.Concat(Palette.AirPetalColors)
			.Select(c => c.SrgbToLinear()).ToArray();
		foreach (object piece in (IEnumerable)typeof(AmbientDrift)
			.GetField("_falling", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(drift)!)
			Require(sourceColors.Contains((Color)piece.GetType().GetField("Color")!.GetValue(piece)!),
				"airborne pigment was not converted from sRGB exactly once");
		current = Window(9232);
		drift.Advance(at, 0, 0);
		var after = Positions();
		for (int i = 0; i < before.Length; i++)
		{
			Require(after[i] == before[i], "walking handoff moved an airborne petal");
			Require(before[i].X > 9216, "particle uses local instead of global coordinates");
		}
		drift.QueueFree();
	}
}
