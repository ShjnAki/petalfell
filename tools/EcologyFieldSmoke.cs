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
			// The harness command wants the rows alone, so they can be piped
			// straight into a plot next to the source simulation's curves.
			string[] userArgs = OS.GetCmdlineUserArgs();
			bool csvOnly = Array.IndexOf(userArgs, "--csv-only") >= 0;
			// A longer run is how a genuine cycle is told apart from a damped
			// spiral that simply has not reached zero inside two hours.
			float harnessHours = 2f;
			foreach (string arg in userArgs)
				if (arg.StartsWith("--hours=") &&
					float.TryParse(arg[8..], System.Globalization.NumberStyles.Float,
						System.Globalization.CultureInfo.InvariantCulture, out float parsed))
					harnessHours = parsed;

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

			// Grass must regrow towards its cell's fertility and never past it.
			int meadow = -1;
			for (int i = 0; i < field.CellCount && meadow < 0; i++)
				if (field.FertilityAt(i) >= 0.99f) meadow = i;
			Assert(meadow >= 0, "the continent must contain at least one meadow cell");

			var before = field.Totals();
			for (int step = 0; step < 600; step++) field.Advance(1f);
			var after = field.Totals();

			Assert(field.ElapsedSeconds == 600f,
				$"elapsed {field.ElapsedSeconds}, expected 600");
			for (int i = 0; i < field.CellCount; i++)
			{
				Assert(field.GrassAt(i) >= 0f && field.GrassAt(i) <= field.FertilityAt(i) + 0.001f,
					$"cell {i} grass {field.GrassAt(i)} outside 0..{field.FertilityAt(i)}");
				Assert(field.PreyAt(i) >= 0f, $"cell {i} prey went negative");
				Assert(field.PredatorAt(i) >= 0f, $"cell {i} predators went negative");
			}
			Assert(after.Prey > 0f && after.Predator > 0f,
				$"ten minutes wiped the continent: prey {after.Prey:0}, predators {after.Predator:0}");

			// An event removes animals; nothing else may.
			float preyBefore = field.PreyAt(meadow);
			field.RemovePrey(meadow, 1f);
			Assert(Math.Abs(field.PreyAt(meadow) - (preyBefore - 1f)) < 0.001f,
				"RemovePrey must subtract exactly what it was given");
			field.RemovePrey(meadow, 99999f);
			Assert(field.PreyAt(meadow) == 0f, "RemovePrey must floor at zero, never go negative");

			// Empty a fertile cell completely, then let the neighbours refill it.
			var refill = new EcologyField(atlas.Width, atlas.Depth,
				guide.GlobalBiomeAt, map.DefaultSeed);
			int hole = -1;
			for (int row = 1; row < refill.Rows - 1 && hole < 0; row++)
			for (int col = 1; col < refill.Columns - 1 && hole < 0; col++)
			{
				int i = row * refill.Columns + col;
				if (refill.FertilityAt(i) >= 0.99f &&
					refill.FertilityAt(i - 1) >= 0.99f &&
					refill.FertilityAt(i + 1) >= 0.99f) hole = i;
			}
			Assert(hole >= 0, "no fertile cell with fertile neighbours was found");

			refill.RemovePrey(hole, 99999f);
			Assert(refill.PreyAt(hole) == 0f, "the cell must start empty");
			for (int step = 0; step < 1800; step++) refill.Advance(1f);
			Assert(refill.PreyAt(hole) > 0.05f,
				$"an emptied valley did not refill from its neighbours: {refill.PreyAt(hole)}");

			// Diffusion must spread, not amplify.
			Assert(refill.Totals().Prey < before.Prey * 20f,
				"diffusion is amplifying rather than spreading");

			var run = EcologyHarness.Run(
				new EcologyField(atlas.Width, atlas.Depth, guide.GlobalBiomeAt, map.DefaultSeed),
				hours: harnessHours, sampleMinutes: 5f);

			int expectedRows = (int)MathF.Round(harnessHours * 60f / 5f) + 1;
			Assert(run.Samples.Count == expectedRows,
				$"{harnessHours} h at five-minute samples is {expectedRows} rows, got {run.Samples.Count}");
			foreach (var sample in run.Samples)
			{
				Assert(sample.Prey > 0f, $"prey reached zero at {sample.Hours:0.00} h");
				Assert(sample.Predator > 0f, $"predators reached zero at {sample.Hours:0.00} h");
			}
			Assert(run.Verdict == "stable", $"{harnessHours} simulated hours ended {run.Verdict}");

			// The node must tick the field at its own rate, not once per frame.
			var ecosystem = new Ecosystem();
			ecosystem.Setup(atlas, map.DefaultSeed);
			float startPrey = ecosystem.Field.Totals().Prey;
			for (int frame = 0; frame < 600; frame++) ecosystem.Advance(1.0 / 60.0);
			Assert(MathF.Abs(ecosystem.Field.ElapsedSeconds - 10f) < 1.1f,
				$"600 frames at 60 fps is ten seconds of field time, got {ecosystem.Field.ElapsedSeconds}");
			Assert(ecosystem.Field.Totals().Prey != startPrey, "the field did not advance at all");
			Assert(!Ecosystem.Enabled, "the ecology must be off unless the flag asks for it");
			ecosystem.QueueFree();

			// Alone, a creature keeps its own heading: no neighbours, no herd.
			var alone = new System.Collections.Generic.List<HerdNeighbour>();
			var kept = HerdBehaviour.Steer(Vector3.Zero, Vector3.Forward, alone);
			Assert(kept.IsEqualApprox(Vector3.Forward),
				$"a lone creature must keep its heading, got {kept}");

			// A group off to one side pulls a creature towards it.
			var group = new System.Collections.Generic.List<HerdNeighbour>
			{
				new(new Vector3(6f, 0f, 0f), Vector3.Forward),
				new(new Vector3(7f, 0f, 1f), Vector3.Forward),
			};
			var pulled = HerdBehaviour.Steer(Vector3.Zero, Vector3.Forward, group);
			Assert(pulled.X > 0.1f, $"cohesion must pull towards the group, got {pulled}");

			// A neighbour pressed against it pushes it away, harder than cohesion pulls.
			var touching = new System.Collections.Generic.List<HerdNeighbour>
			{
				new(new Vector3(0.8f, 0f, 0f), Vector3.Forward),
			};
			var pushed = HerdBehaviour.Steer(Vector3.Zero, Vector3.Forward, touching);
			Assert(pushed.X < -0.1f, $"separation must win at touching distance, got {pushed}");

			// Beyond the herd radius a creature is scenery, not company.
			var distant = new System.Collections.Generic.List<HerdNeighbour>
			{
				new(new Vector3(40f, 0f, 0f), Vector3.Right),
			};
			var ignored = HerdBehaviour.Steer(Vector3.Zero, Vector3.Forward, distant);
			Assert(ignored.IsEqualApprox(Vector3.Forward),
				$"a creature 40 units away is not in the herd, got {ignored}");

			// The result is a heading, never a velocity.
			Assert(MathF.Abs(pulled.Length() - 1f) < 0.001f,
				$"steer must return a unit vector, got {pulled.Length()}");

			// Species that read as a group, and those that do not.
			Assert(Fauna.IsHerdSpecies(Species.Deer), "deer herd");
			Assert(Fauna.IsHerdSpecies(Species.Rabbit), "rabbits herd");
			Assert(Fauna.IsHerdSpecies(Species.Goat), "goats herd");
			Assert(!Fauna.IsHerdSpecies(Species.Fish), "fish are not a land herd");
			Assert(!Fauna.IsHerdSpecies(Species.Heron), "herons are solitary");
			Assert(!Fauna.IsHerdSpecies(Species.Butterfly), "butterflies are not a herd");

			Assert(Fauna.IsPredatorSpecies(Species.Wolf), "the wolf hunts");
			Assert(!Fauna.IsPredatorSpecies(Species.Deer), "deer do not hunt");
			Assert(!Fauna.IsHerdSpecies(Species.Wolf), "a pack is not a herd");
			// A wolf must never appear in a world that did not ask for one.
			Assert(!Fauna.EcologySpeciesAllowed(Species.Wolf, ecologyEnabled: false),
				"no wolf without the flag");
			Assert(Fauna.EcologySpeciesAllowed(Species.Wolf, ecologyEnabled: true),
				"the flag admits the wolf");
			Assert(Fauna.EcologySpeciesAllowed(Species.Deer, ecologyEnabled: false),
				"deer are not gated on the flag");

			var noPrey = new System.Collections.Generic.List<HuntTarget>();
			var idle = PackBehaviour.Decide(Vector3.Zero, Vector3.Forward, Vector3.Zero, 1f, noPrey);
			Assert(idle.Intent == PackIntent.Wander, $"no prey means no hunt, got {idle.Intent}");

			// Prey inside perception but beyond sprint range: approach at a trot.
			// Sprinting from a hundred units away empties the lungs before the chase.
			var faraway = new System.Collections.Generic.List<HuntTarget>
				{ new(new Vector3(0f, 0f, 100f), 100f) };
			var stalking = PackBehaviour.Decide(Vector3.Zero, Vector3.Forward, Vector3.Zero, 1f, faraway);
			Assert(stalking.Intent == PackIntent.Stalk, $"distant prey is stalked, got {stalking.Intent}");
			Assert(stalking.SpeedMultiplier < 2f, "a stalk is not a sprint");

			// Inside sprint range with breath left: commit.
			var close = new System.Collections.Generic.List<HuntTarget>
				{ new(new Vector3(0f, 0f, 20f), 20f) };
			var sprinting = PackBehaviour.Decide(Vector3.Zero, Vector3.Forward, Vector3.Zero, 1f, close);
			Assert(sprinting.Intent == PackIntent.Sprint, $"close prey is sprinted at, got {sprinting.Intent}");
			Assert(sprinting.SpeedMultiplier > stalking.SpeedMultiplier, "a sprint outruns a stalk");
			Assert(sprinting.Heading.Z > 0.9f, $"the sprint must aim at the prey, got {sprinting.Heading}");

			// Out of breath: break off, whatever is in front of you.
			var spent = PackBehaviour.Decide(Vector3.Zero, Vector3.Forward, Vector3.Zero, 0f, close);
			Assert(spent.Intent == PackIntent.Recover, $"an exhausted wolf breaks off, got {spent.Intent}");

			// Prey beyond perception is not prey.
			var unseen = new System.Collections.Generic.List<HuntTarget>
				{ new(new Vector3(0f, 0f, 400f), 400f) };
			Assert(PackBehaviour.Decide(Vector3.Zero, Vector3.Forward, Vector3.Zero, 1f, unseen).Intent
				== PackIntent.Wander, "a deer 400 units away has not been noticed");

			// Far from the den with nothing to chase: go home. This is also what
			// will let the traveller escape a pack by leaving its valley.
			var home = PackBehaviour.Decide(new Vector3(900f, 0f, 0f), Vector3.Right,
				Vector3.Zero, 1f, noPrey);
			Assert(home.Heading.X < -0.5f, $"beyond its range a wolf turns home, got {home.Heading}");

			// Inside the territory the recall does nothing at all.
			var ranging = PackBehaviour.Decide(new Vector3(100f, 0f, 0f), Vector3.Right,
				Vector3.Zero, 1f, noPrey);
			Assert(ranging.Heading.IsEqualApprox(Vector3.Right),
				$"inside its range a wolf keeps its heading, got {ranging.Heading}");

			if (!csvOnly) GD.Print($"[ecology-field-smoke] emptied cell {hole} refilled to " +
			         $"{refill.PreyAt(hole):0.00} prey in thirty minutes");

			if (!csvOnly) GD.Print($"[ecology-field-smoke] ten minutes: grass {before.Grass:0}->{after.Grass:0} " +
			         $"prey {before.Prey:0}->{after.Prey:0} " +
			         $"predators {before.Predator:0}->{after.Predator:0}");

			if (!csvOnly) GD.Print($"[ecology-field-smoke] atlas {atlas.Width}x{atlas.Depth}; " +
			         $"biome at 6400,7360 {far}; at 4500,1900 {north}");
			if (!csvOnly) GD.Print($"[ecology-field-smoke] {field.CellCount} cells; start " +
			         $"grass {totals.Grass:0} prey {totals.Prey:0} predators {totals.Predator:0}");
			if (csvOnly) GD.Print(run.ToCsv());
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
