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
			CheckCameraShadowBasis();
			CheckDrift();
			CheckMirrorRegistration();
			CheckMirrorTransitions();
			CheckLocalEdits();
			CheckSurfaceFragments();
			CheckPavingPetals();
			CheckMeadowPlants();
			CheckSouthernProfiles();
			CheckWetlandReeds();
			CheckSouthernTransition();
			CheckMarshMushrooms();
			CheckMarshSurfaceDetail();
			CheckSouthernFauna();
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

	private static void CheckSouthernProfiles()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		var lookup = (int[])typeof(ProductionTerrainWindow).GetMethod("BuildProfileLookup",
			BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { atlas });
		Require(atlas.BiomeCatalog.Profiles[lookup[(int)Biome.Wetland]].Id == "fen",
			"wetland inherited the central river profile instead of fen");
		Require(atlas.BiomeCatalog.Profiles[lookup[(int)Biome.Shore]].Id == "drowned-shallows",
			"shore lost its drowned-shallows profile");
	}

	private static void CheckWetlandReeds()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		int profile = atlas.BiomeCatalog.Profiles.FindIndex(p => p.Id == "fen");
		const int size = 96;
		AtlasSectorWindow Window(int originX, int bed, bool blocked = false)
		{
			var data = new AtlasSectorData(0, 0, originX, 7200, size, 0,
				size, size, 64, 24, "wetland-reed-smoke");
			var grid = new VoxelGrid(size, 64, 7, originX, 7200);
			for (int z = 0; z < size; z++)
			for (int x = 0; x < size; x++)
			{
				int i = z * size + x;
				data.Height[i] = (ushort)bed;
				data.WaterSurface[i] = 24;
				data.Profile[i] = (byte)profile;
				grid.Describe(x, z, bed, Palette.MUD, Palette.MUD);
				if (blocked) grid.Set(x, bed, z, Palette.STONE);
			}
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		var a = Window(6000, 23);
		var b = Window(6024, 23);
		var deepWindow = Window(6000, 14);
		var coveredWindow = Window(6000, 23, true);
		int vertices = 0;
		for (int cz = 0; cz < 4; cz++)
		for (int cx = 1; cx < 4; cx++)
		{
			using var mesh = GroundDetail.BuildAtlas(a, cx, cz);
			using var next = GroundDetail.BuildAtlas(b, cx - 1, cz);
			using var deep = GroundDetail.BuildAtlas(deepWindow, cx, cz);
			using var covered = GroundDetail.BuildAtlas(coveredWindow, cx, cz);
			Require(deep == null && covered == null, "reeds grew in deep water or through a solid blocker");
			Require((mesh == null) == (next == null), "reed patch changed across window ownership");
			if (mesh == null) continue;
			var pa = mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
			var pb = next.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex].AsVector3Array();
			vertices += pa.Length;
			var wind = mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
			var nextWind = next.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
			Require(wind.SequenceEqual(nextWind), "reed wind changed across window ownership");
			var joints = new Dictionary<Vector3, Vector2>();
			Require(pa.Length == pb.Length, "reed count changed across window ownership");
			for (int i = 0; i < pa.Length; i++)
			{
				Require((pa[i] - pb[i] - new Vector3(24, 0, 0)).Length() < .001f,
					"reed geometry changed across window ownership");
				Require(pa[i].Y >= 22.9f && pa[i].Y <= 26.5f, "reeds leave their bed/water envelope");
				var motion = new Vector2(wind[i * 2], wind[i * 2 + 1]);
				Require(!joints.TryGetValue(pa[i], out var previous) || previous == motion,
					"reed head or leaf facets tear at shared wind joints");
				joints[pa[i]] = motion;
			}
		}
		Require(vertices > 0, "shallow wetland has no bed-rooted reed patches");
		GD.Print($"[southern-detail-smoke] dedicated profiles and {vertices} rooted reed vertices retain window ownership");
	}

	private static void CheckSouthernTransition()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		var a = ProductionTerrainGuide.CreateAtOrigin(atlas, 768, 5760, 6144, 20260820);
		var b = ProductionTerrainGuide.CreateAtOrigin(atlas, 768, 5784, 6144, 20260820);
		int blended = 0;
		for (int z = 24; z < 744; z += 3)
		for (int x = 48; x < 744; x += 3)
		{
			float weight = a.SouthernInfluenceAt(x, z);
			Require(weight >= 0f && weight <= 1f && float.IsFinite(weight), "invalid southern transition weight");
			Require(Math.Abs(weight - b.SouthernInfluenceAt(x - 24, z)) < .00001f,
				"southern transition depends on window origin");
			if (a.LandAt(x, z) < .99f || a.LandAt(x + 1, z) < .99f) continue;
			if (weight > .05f && weight < .95f) blended++;
			Require(Math.Abs(weight - a.SouthernInfluenceAt(x + 1, z)) < .08f,
				"central/southern terrain influence has an abrupt land boundary");
		}
		Require(blended > 100, "southern ecotone has no broad mixed land band");
		Require(ProductionTerrainGuide.LowlandHeight(44f, 1f) < 33f &&
			ProductionTerrainGuide.LowlandHeight(120f, 0f) == 120f, "southern relief does not lower selectively");
		GD.Print($"[southern-transition-smoke] {blended} mixed land samples, continuous relief and exact neighbouring influence");
	}

	private static void CheckMarshSurfaceDetail()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		int profile = atlas.BiomeCatalog.Profiles.FindIndex(p => p.Id == "fen");
		AtlasSectorWindow Window(int originX)
		{
			const int size = 96;
			var data = new AtlasSectorData(0, 0, originX, 7200, size, 0, size, size, 64, 24, "marsh-detail-smoke");
			var grid = new VoxelGrid(size, 64, 7, originX, 7200);
			for (int z = 0; z < size; z++)
			for (int x = 0; x < size; x++)
			{
				int gx = originX + x, gz = 7200 + z, i = z * size + x;
				bool dry = (gx / 8 + gz / 8) % 3 == 0;
				int height = dry ? 28 : 22;
				data.Height[i] = (ushort)height; data.Land[i] = dry ? (byte)1 : (byte)0;
				data.WaterSurface[i] = dry ? (ushort)0 : (ushort)24;
				data.Profile[i] = data.SecondaryProfile[i] = (byte)profile;
				grid.Describe(x, z, height, dry ? Palette.MOSS : Palette.MUD, Palette.STONE);
			}
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		int rootVertices = 0, padVertices = 0;
		foreach (int origin in new[] { 6000, 6288, 6576 })
		{
			var a = Window(origin); var b = Window(origin + 24);
			foreach (bool water in new[] { false, true })
			{
				using var ma = water ? GroundDetail.BuildAtlasWater(a, 2, 2) : GroundDetail.BuildAtlas(a, 2, 2);
				using var mb = water ? GroundDetail.BuildAtlasWater(b, 1, 2) : GroundDetail.BuildAtlas(b, 1, 2);
				Require((ma == null) == (mb == null), "marsh detail appeared only in one window");
				if (ma == null) continue;
				var aa = ma.SurfaceGetArrays(0); var bb = mb.SurfaceGetArrays(0);
				var pa = aa[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var pb = bb[(int)Mesh.ArrayType.Vertex].AsVector3Array();
				var wa = aa[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
				Require(pa.Length == pb.Length && wa.SequenceEqual(bb[(int)Mesh.ArrayType.Custom0].AsFloat32Array()),
					"bank or pad count/wind changed across window ownership");
				var joints = new Dictionary<Vector3, Vector2>();
				for (int i = 0; i < pa.Length; i++)
				{
					Require((pa[i] + a.GlobalOrigin - pb[i] - b.GlobalOrigin).Length() < .002f,
						"bank roots or pads moved at handoff");
					var wind = new Vector2(wa[i * 2], wa[i * 2 + 1]);
					if (joints.TryGetValue(pa[i], out var other)) Require(wind.IsEqualApprox(other), "bank root facets tear in wind");
					else joints.Add(pa[i], wind);
					if (water)
					{
						int x = Mathf.FloorToInt(pa[i].X), z = Mathf.FloorToInt(pa[i].Z);
						Require(a.Data.WaterSurface[z * a.Data.Width + x] == 24 && pa[i].Y >= 24.37f && pa[i].Y <= 24.55f,
							"floating leaf left its wet supporting cell");
						padVertices++;
					}
					else if (pa[i].Y > 25.5f && pa[i].Y < 27.9f) rootVertices++;
				}
			}
		}
		Require(rootVertices > 100 && padVertices > 20, "fixture did not exercise roots and floating leaves");
		GD.Print($"[marsh-surface-smoke] {rootVertices} hanging root / {padVertices} water-detail vertices preserve support, global placement and wind joints");
	}

	private static void CheckMarshMushrooms()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		AtlasSectorWindow Window(int originX)
		{
			const int size = 128;
			var data = new AtlasSectorData(0, 0, originX, 7200, size, 0, size, size, 64, 24, "mushroom-smoke");
			var grid = new VoxelGrid(size, 64, 7, originX, 7200);
			for (int z = 0; z < size; z++)
			for (int x = 0; x < size; x++)
			{
				int i = z * size + x;
				data.Height[i] = 26;
				data.Land[i] = 1;
				data.Profile[i] = (byte)atlas.BiomeCatalog.Profiles.FindIndex(p => p.Id == "fen");
				grid.Describe(x, z, 26, Palette.MOSS, Palette.SOIL);
			}
			Vegetation.Mushroom(grid, new Rng(11), new Noise2D(17), 6064 - originX, 26, 64, 1.1f, Palette.LEAF_ROSE);
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		var a = Window(6000);
		var b = Window(6024);
		int cells = 0;
		foreach (var voxel in a.Grid.PlacedIn(48, 48, 32))
		{
			Require(voxel.Material == b.Grid.At(voxel.X - 24, voxel.Y, voxel.Z), "mushroom changed across window ownership");
			cells++;
		}
		Require(cells > 250 && a.Grid.At(64, 27, 64) == Palette.MUSHROOM_STEM, "giant mushroom lacks its pale grounded stem");
		Require(Palette.Get(Palette.MUSHROOM_STEM).Pattern == Palette.PatternFungus,
			"mushroom stem inherited plank striping");
		for (int z = 63; z <= 65; z++)
		for (int x = 63; x <= 65; x++)
			Require(a.Grid.At(x, 31, z) == Palette.MUSHROOM_STEM, "large mushroom stem lost its thick section");
		Require(!a.Grid.SolidAt(68, 28, 64) && a.Grid.HeightAt(68, 64) > 36,
			"mushroom cap has no real open underside");
		Require(AtlasRuntimeHandoff.TryResolveExactLanding(a, 6068, 7264, out var landing, out _) && landing.SurfaceY == 26,
			"map landing under a mushroom did not use the supported ground");
		using (var detailA = GroundDetail.BuildAtlas(a, 2, 2))
		using (var detailB = GroundDetail.BuildAtlas(b, 1, 2))
		{
			Require(detailA != null && detailB != null, "mushroom fixture has no hanging detail");
			var aa = detailA.SurfaceGetArrays(0); var bb = detailB.SurfaceGetArrays(0);
			var pa = aa[(int)Mesh.ArrayType.Vertex].AsVector3Array();
			var pb = bb[(int)Mesh.ArrayType.Vertex].AsVector3Array();
			var wa = aa[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
			var wb = bb[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
			Require(pa.Length == pb.Length && wa.SequenceEqual(wb), "mushroom hanging detail changed at handoff");
			int hanging = 0;
			var joints = new Dictionary<Vector3, Vector2>();
			for (int i = 0; i < pa.Length; i++)
			{
				Require((pa[i] + a.GlobalOrigin - pb[i] - b.GlobalOrigin).Length() < .002f,
					"mushroom detail moved across window ownership");
				if (pa[i].Y < 32f) continue;
				hanging++;
				var wind = new Vector2(wa[2 * i], wa[2 * i + 1]);
				Require(wind.X >= 0f && wind.X <= .75f, "invalid gill wind weight");
				if (joints.TryGetValue(pa[i], out var other))
					Require(wind.IsEqualApprox(other), "hanging bead facets separate under wind");
				else joints.Add(pa[i], wind);
				Require(pa[i].Y < 40f && pa[i].Y > 34f, "gills escaped the cap's clear underside");
			}
			Require(hanging > 100, "fixture did not exercise hanging gills");
			GD.Print($"[mushroom-detail-smoke] {hanging} hanging vertices retain global anchors and shared wind joints");
		}
		var grammar = new ProductionTerrainGrammar(20260820);
		int wet = 0, dry = 0;
		for (int z = 7000; z < 7500; z += 6)
		for (int x = 6000; x < 6500; x += 6)
		{
			Require(!grammar.MarshWaterAt(x, z, 0f), "marsh pools escaped their southern influence");
			if (grammar.MarshWaterAt(x, z, 1f)) wet++; else dry++;
			float height = grammar.MarshHeightAt(x, z);
			Require(height >= 26f && height <= 31f, "marsh relief left its low shelf envelope");
		}
		Require(wet > 1000 && dry > 500, "marsh field did not separate shallow channels and islets");
		GD.Print($"[mushroom-marsh-smoke] {cells} mushroom cells, open underside/ground landing, {wet} wet/{dry} dry field samples");
	}

	private void CheckSouthernFauna()
	{
		var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		int profile = atlas.BiomeCatalog.Profiles.FindIndex(p => p.Id == "fen");
		AtlasSectorWindow Window(int originX, bool dry = false, int originZ = 7200, bool ocean = false)
		{
			const int size = 768;
			var data = new AtlasSectorData(0, 0, originX, originZ, size, 0, size, size, 64, 24, "fauna-smoke");
			var grid = new VoxelGrid(size, 64, 7, originX, originZ);
			for (int z = 0; z < size; z++)
			for (int x = 0; x < size; x++)
			{
				int i = z * size + x, h = dry ? 25 : ocean ? 4 : (originX + x) % 48 < 24 ? 21 : 23;
				data.Height[i] = (ushort)h;
				data.WaterSurface[i] = dry ? (ushort)0 : (ushort)24;
				data.Wetness[i] = dry ? (byte)0 : (byte)255;
				data.Profile[i] = ocean ? (byte)0 : (byte)profile;
				grid.Describe(x, z, h, Palette.MUD, Palette.MUD);
			}
			return new AtlasSectorWindow(data, atlas, 7, grid);
		}
		AtlasSectorWindow active = Window(5760);
		var fauna = new Fauna();
		AddChild(fauna);
		fauna.Setup(() => active, null, null, 20260820);
		var focus = new Vector3(6048, 25, 7296);
		for (int frame = 0; frame < 180; frame++) fauna.Advance(focus, 1.0 / 30.0);
		Require(fauna.LiveCount is > 0 and <= Fauna.MarshPopulation, "fauna exceeds bounded population");
		Require(fauna.Live.Count(c => c.Kind == Species.Heron) <= 2, "too many marsh birds");
		Require(fauna.Live.Any(c => c.Kind == Species.Fish) && fauna.Live.Any(c => c.Kind == Species.Heron),
			"southern habitats do not admit both fish and waders");
		Require(fauna.Live.Any(c => new Vector2(c.GlobalPosition.X - focus.X, c.GlobalPosition.Z - focus.Z).Length() > 96f),
			"wide-zoom wildlife did not populate beyond the former cull radius");
		Require(Fauna.MarshSpawnRadius >= 288f && Fauna.MarshRetentionRadius >= 384f, "wildlife range does not cover maximum zoom");
		var before = fauna.Live.ToDictionary(c => c.GetInstanceId(), c => c.GlobalPosition);
		active = Window(5784);
		fauna.Advance(focus + new Vector3(48, 0, 0), 0);
		Require(fauna.LiveCount == before.Count, "wide-view movement culled retained wildlife");
		foreach (var animal in fauna.Live)
		{
			Require(animal.HabitatValid(), "animal lost its habitat after handoff");
			Require(before.TryGetValue(animal.GetInstanceId(), out var position) && position == animal.GlobalPosition,
				"walking handoff reset or moved an animal");
		}
		Require(!Fauna.TryAtlasHabitat(Window(5952, true), Species.Fish, focus, out _, out _),
			"fish admitted on dry ground");
		Require(!Fauna.TryAtlasHabitat(active, Species.Heron, new Vector3(6002, 24, 7296), out _, out _),
			"wader admitted in deep water");
		fauna.Advance(new Vector3(9800, 40, 4600), .01);
		Require(fauna.LiveCount == 0, "distant map travel retained southern fauna");
		active = Window(5760, originZ: 1200, ocean: true);
		focus = new Vector3(6048, 24, 1536);
		for (int frame = 0; frame < 180; frame++) fauna.Advance(focus, 1.0 / 30.0);
		Require(fauna.LiveCount == Fauna.FishPopulation && fauna.Live.All(c => c.Kind == Species.Fish),
			"deep northern ocean did not fill its independent fish population");
		Require(fauna.Live.All(c => c.HabitatValid()), "ocean fish escaped navigable water");
		var blocked = fauna.Live[0].GlobalPosition;
		active.Grid.Set(Mathf.FloorToInt(blocked.X)-active.Data.OriginX, 23,
			Mathf.FloorToInt(blocked.Z)-active.Data.OriginZ, Palette.STONE_PALE);
		Require(!Fauna.TryAtlasHabitat(active, Species.Fish, blocked, out _, out _), "ocean fish admitted inside stone");
		fauna.Free();
		GD.Print("[southern-fauna-smoke] 24 fish including deep northern ocean, two herons maximum, 288/384-block ranges, habitat exclusion and live handoff passed");
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
				var widths = aa[(int)Mesh.ArrayType.Custom1].AsFloat32Array();
				Require(widths.Length == pa.Length*3 && widths.Zip(bb[(int)Mesh.ArrayType.Custom1].AsFloat32Array()).All(pair => Math.Abs(pair.First-pair.Second)<.0001f),
					"thin edge coverage changed at handoff");
				Require(widths.Any(v => Math.Abs(v) > .001f), "thin plant edges have no coverage metadata");
				var widthJoints = new Dictionary<Vector3,Vector3>();
				for (int vertex = 0; vertex < pa.Length; vertex++) {
					var inward = new Vector3(widths[vertex*3],widths[vertex*3+1],widths[vertex*3+2]);
					if (inward == Vector3.Zero) continue;
					Require(inward.Length() < .5f, "coverage tagged a broad decoration face");
					if (widthJoints.TryGetValue(pa[vertex],out var prior))
						Require(prior.IsEqualApprox(inward), "grass shoulder splits when widened at zoom");
					else widthJoints.Add(pa[vertex],inward);
				}
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
		day.TimeOfDay = .36f;
		day.Paused = false;
		day.DayLength = 900f;
		day._Process(0);
		Vector3 direction = key.Basis.Z;
		for (int frame = 0; frame < 240; frame++)
		{
			day._Process(1.0 / 120.0);
			Vector3 next = key.Basis.Z;
			float distance = direction.DistanceTo(next);
			Require(distance > .00001f && distance < .0001f,
				"sun direction held or jumped between rendered frames");
			direction = next;
		}
		day.Paused = true;
		Transform3D frozen = key.Transform;
		float frozenCover = day.CloudCover;
		for (int frame = 0; frame < 240; frame++) day._Process(1.0 / 120.0);
		Require(key.Transform == frozen && day.CloudCover == frozenCover,
			"frozen clock changed the directional light or weather");
		Require(key.PhysicsInterpolationMode == PhysicsInterpolationModeEnum.Off,
			"render-driven sun inherited physics interpolation");
		day.TimeOfDay += .00001f;
		day._Process(0);
		Require(key.Transform != frozen, "fine paused clock scrub was quantized away");
		day.SetShadowSoftness(0);
		Require(day.ShadowSoftness == 0f && key.ShadowBias > 0f, "crisp endpoint lost depth bias");
		float hardBias = key.ShadowBias * key.ShadowBlur;
		day.RandomizeClouds();
		Require(day.ShadowSoftness == 0f, "weather overrides crisp shadows");
		day.SetShadowSoftness(DayCycle.MaxShadowSoftness);
		Require(key.ShadowBlur == DayCycle.MaxShadowSoftness, "very blurry endpoint is clamped away");
		Require(Mathf.IsEqualApprox(hardBias, key.ShadowBias * key.ShadowBlur * 4f),
			"softness changes the effective shadow depth offset");
		var camera = new CameraRig();
		AddChild(camera);
		var menu = new DeveloperMenu();
		menu.Setup(null, null, camera, day);
		AddChild(menu);
		Label title = menu.FindChildren("*", "Label", true, false).OfType<Label>()
			.Single(label => label.Text.StartsWith("Shadow softness"));
		HSlider slider = title.GetParent().GetParent().GetChildren().OfType<HSlider>().Single();
		slider.Value = 0;
		Require(day.ShadowSoftness == 0 && day.Paused, "menu did not select crisp shadows while frozen");
		slider.Value = 50;
		Require(day.ShadowSoftness == 6 && key.ShadowBlur == 6, "menu midpoint did not update the frozen light");
		slider.Value = 100;
		Require(day.ShadowSoftness == DayCycle.MaxShadowSoftness, "menu cannot reach very blurry shadows");
		menu.Free(); camera.Free();
		GD.Print("[shadow-clock-smoke] 240 flowing/240 frozen frames, fine frozen scrub and both softness endpoints pass");
		GD.Print("[shadow-menu-smoke] frozen-clock UI: crisp / 50% / very blurry pass");
		day.QueueFree(); key.QueueFree(); fill.QueueFree();
		env.Dispose();
	}

	private void CheckCameraShadowBasis()
	{
		var camera = new CameraRig();
		AddChild(camera);
		camera.Follow(new Vector3(6400, 26, 7360), Vector3.Zero, 1);
		Basis basis = camera.GlobalBasis;
		for (int frame = 0; frame < 240; frame++)
		{
			// Real atlas coordinates and sub-voxel body/follow movement must not
			// rotate a camera whose orbit and zoom are stationary.
			camera.Follow(new Vector3(6400, 26 + MathF.Sin(frame) * .003f, 7360),
				Vector3.Zero, 1.0 / 120.0);
			Require(camera.GlobalBasis == basis, "atlas follow jitter changed the shadow camera basis");
		}
		var sun = Atmosphere.Sun(); AddChild(sun);
		foreach (float distance in new[]{50f,120f,180f,240f,360f,700f}) {
			Atmosphere.SetShadowViewDistance(sun,distance);
			Require(sun.DirectionalShadowMaxDistance >= distance*1.5f,
				"wide view loses shadows before the far visible ground");
			float stable = sun.DirectionalShadowMaxDistance;
			for (int frame=0; frame<120; frame++) Atmosphere.SetShadowViewDistance(sun,distance);
			Require(sun.DirectionalShadowMaxDistance == stable, "stationary zoom shakes shadow range");
		}
		sun.Free();
		camera.Free();
		GD.Print("[shadow-camera-smoke] 240 frames preserve exact orbit basis during sub-voxel atlas follow");
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
