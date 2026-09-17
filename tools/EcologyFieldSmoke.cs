using System;
using Godot;
using Petalfell.Ecology;
using Petalfell.World;

namespace Petalfell.Tools;

/// <summary>
/// Headless checks for the continental ecology field. The field carries no
/// Godot dependency, so this smoke exists only to load the authored atlas and
/// run assertions against it — the engine is the test runner, not a dependency
/// of the thing under test.
/// </summary>
public partial class EcologyFieldSmoke : Node
{
	public override void _Ready()
	{
		try
		{
			var map = MapDefinition.Load("res://content/chapter_01/map.json");
			var atlas = map.CanonicalAtlas;
			var guide = ProductionTerrainGuide.CreateAtOrigin(atlas, 64, 0, 0, map.DefaultSeed);

			Assert(atlas.Width == 12288, $"atlas width {atlas.Width}, expected 12288");
			Assert(atlas.Depth == 9216, $"atlas depth {atlas.Depth}, expected 9216");

			// The guide must answer in global coordinates far outside its own
			// 64-block window, because the field samples the whole continent from
			// a single small instance.
			var far = guide.GlobalBiomeAt(6400, 7360);
			var north = guide.GlobalBiomeAt(4500, 1900);
			Assert(Enum.IsDefined(far), $"southern sample returned {far}");
			Assert(Enum.IsDefined(north), $"northern sample returned {north}");
			Assert(far != north, "southern marsh and northern highland returned the same biome");

			// Fertility must be ordered by how much a biome can feed, and water
			// or bare rock must feed nothing at all.
			Assert(EcologyTuning.FertilityForBiome(Biome.Meadow) >
				EcologyTuning.FertilityForBiome(Biome.Forest),
				"meadow must out-feed forest");
			Assert(EcologyTuning.FertilityForBiome(Biome.Forest) >
				EcologyTuning.FertilityForBiome(Biome.Highland),
				"forest must out-feed highland");
			Assert(EcologyTuning.FertilityForBiome(Biome.SnowyHills) <= 0.15f,
				"snowy hills must be near barren");
			foreach (Biome biome in Enum.GetValues<Biome>())
			{
				float f = EcologyTuning.FertilityForBiome(biome);
				Assert(f >= 0f && f <= 1f, $"fertility for {biome} out of range: {f}");
			}
			Assert(EcologyTuning.CellBlocks == 128, "cell size must be 128 blocks");

			var field = new EcologyField(atlas.Width, atlas.Depth,
				guide.GlobalBiomeAt, map.DefaultSeed);

			Assert(field.Columns == 96, $"columns {field.Columns}, expected 96");
			Assert(field.Rows == 72, $"rows {field.Rows}, expected 72");
			Assert(field.CellCount == 6912, $"cells {field.CellCount}, expected 6912");

			// Indexing must address the whole atlas and clamp outside it rather
			// than throwing: callers hand in live player positions.
			Assert(field.IndexAt(0, 0) == 0, "origin must be cell 0");
			Assert(field.IndexAt(atlas.Width - 1, atlas.Depth - 1) == 6911,
				"far corner must be the last cell");
			Assert(field.IndexAt(-500, -500) == 0, "negative coordinates must clamp");
			Assert(field.IndexAt(99999, 99999) == 6911, "large coordinates must clamp");

			// Rebuilding from the same seed must reproduce the same world.
			var twin = new EcologyField(atlas.Width, atlas.Depth,
				guide.GlobalBiomeAt, map.DefaultSeed);
			for (int i = 0; i < field.CellCount; i++)
				Assert(field.PreyAt(i) == twin.PreyAt(i),
					$"cell {i} differs between two builds from the same seed");

			var totals = field.Totals();
			Assert(totals.Prey > 0f, "the continent must start with prey");
			Assert(totals.Predator > 0f, "the continent must start with predators");
			Assert(totals.Prey > totals.Predator * 5f,
				$"prey {totals.Prey:0} must far outnumber predators {totals.Predator:0}");

			GD.Print($"[ecology-field-smoke] atlas {atlas.Width}x{atlas.Depth}; " +
			         $"biome at 6400,7360 {far}; at 4500,1900 {north}");
			GD.Print($"[ecology-field-smoke] {field.CellCount} cells; start " +
			         $"grass {totals.Grass:0} prey {totals.Prey:0} predators {totals.Predator:0}");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PushError($"[ecology-field-smoke] {ex}");
			GetTree().Quit(1);
		}
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition) throw new InvalidOperationException(message);
	}
}
