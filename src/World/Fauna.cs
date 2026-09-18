using System;
using System.Collections.Generic;
using Godot;
using Petalfell.Core;

namespace Petalfell.World;

public enum Species : byte { Deer, Rabbit, Goat, Bird, Butterfly, Fish, Heron, Wolf }

/// <summary>
/// Ambient wildlife.
///
/// Streamed against the player the way chunks are, and for the same reason: a
/// world this size cannot afford to simulate creatures nobody is looking at,
/// and it does not need to — an animal the player has never seen has no history
/// worth preserving. A fixed population is kept alive inside a ring around the
/// traveller, culled when they walk away and re-seeded ahead of them.
///
/// Which animal appears is decided by the ground it would be standing on, so
/// fauna reinforces the provinces rather than floating free of them: deer in
/// the woods, rabbits in the meadows, goats on the high stone, fish in the
/// water. That is the whole design brief — a creature is one more way to tell
/// where you are without being told.
///
/// Every body is the same four-box plan at different proportions rather than
/// six hand-built models. At this camera distance a creature is thirty pixels
/// tall, and its silhouette and gait carry all of the recognition; modelling
/// each one separately would be a great deal of code spent below the threshold
/// where anyone could tell.
/// </summary>
public partial class Fauna : Node3D
{
	private const int Population = 16;
	public const int FishPopulation = 24;
	public const int MarshPopulation = FishPopulation + 3;
	public const float MarshSpawnRadius = 288f;
	public const float MarshRetentionRadius = 384f;
	/// <summary>
	/// Bodies alive at once. The marsh budget was sized for water and shore
	/// animals in one wetland; a herd and the pack hunting it need their own
	/// room, and only exist when the ecology is switched on.
	/// </summary>
	public const int EcologyPopulation = MarshPopulation + 12;
	private int PopulationLimit => _window == null ? Population
		: _ecology ? EcologyPopulation : MarshPopulation;
	private const int BirdPopulation = 3;
	private const float SpawnNear = 26f;
	private const float SpawnFar = 74f;
	private const float Cull = 96f;

	private readonly List<Critter> _live = new();
	private Terrain _terrain;
	private ShaderMaterial _inkLight, _inkDark;
	private Rng _rng;
	private double _retry;
	private Func<AtlasSectorWindow> _window;
	private readonly HashSet<long> _habitats = new();
	private int _seed;
	public IReadOnlyList<Critter> Live => _live;

	public void Setup(Func<AtlasSectorWindow> window, ShaderMaterial inkLight, ShaderMaterial inkDark, int seed)
	{
		_window = window;
		_inkLight = inkLight;
		_inkDark = inkDark;
		_seed = seed;
	}

	public void Setup(Terrain terrain, ShaderMaterial inkLight, ShaderMaterial inkDark, int seed)
	{
		_terrain = terrain;
		_inkLight = inkLight;
		_inkDark = inkDark;
		_rng = new Rng(seed ^ 0x5EA7);
	}

	public int LiveCount => _live.Count;

	/// <summary>
	/// Which creatures read as a group. Birds already flock in their own way and
	/// the water and marsh animals are deliberately solitary, so the herd is the
	/// grazing land animals and nothing else.
	/// </summary>
	public static bool IsHerdSpecies(Species species) =>
		species is Species.Deer or Species.Rabbit or Species.Goat;

	/// <summary>The one creature that eats the others.</summary>
	public static bool IsPredatorSpecies(Species species) => species is Species.Wolf;

	/// <summary>
	/// A species the ecology introduces cannot appear in a world that did not
	/// ask for the ecology. Everything that existed before the flag ignores it.
	/// This is a gate, not a low probability: with the flag off there is no
	/// path by which a wolf can be constructed at all.
	/// </summary>
	public static bool EcologySpeciesAllowed(Species species, bool ecologyEnabled) =>
		ecologyEnabled || !IsPredatorSpecies(species);

	private bool _ecology;
	private readonly List<Ecology.HerdNeighbour> _neighbours = new();
	private readonly List<Ecology.HuntTarget> _quarry = new();
	private Ecology.Ecosystem _ecosystem;

	/// <summary>The field that kills are reported to. Null means nothing is recorded.</summary>
	public void SetEcosystem(Ecology.Ecosystem ecosystem) => _ecosystem = ecosystem;

	/// <summary>
	/// Turn the ecology on. Off — the ordinary case — no creature is given a
	/// steering delegate and Advance runs exactly the code it ran before.
	/// </summary>
	public void SetEcologyEnabled(bool enabled) => _ecology = enabled;

	/// <summary>
	/// Herd-mates of one creature. A linear scan: there are at most 27 bodies
	/// alive at once, so any index would cost more to maintain than to skip.
	/// </summary>
	private Vector3 SteerHerd(Critter self)
	{
		_neighbours.Clear();
		foreach (var other in _live)
		{
			if (other == self || !IsInstanceValid(other) || other.Kind != self.Kind) continue;
			_neighbours.Add(new Ecology.HerdNeighbour(other.GlobalPosition, other.Heading));
		}
		return Ecology.HerdBehaviour.Steer(self.GlobalPosition, self.Heading, _neighbours);
	}

	/// <summary>
	/// What one wolf can see worth chasing. The same linear scan as the herd,
	/// for the same reason: the live population is small enough that an index
	/// would cost more to maintain than to skip.
	/// </summary>
	private Ecology.PackDecision HuntFor(Critter wolf)
	{
		_quarry.Clear();
		foreach (var other in _live)
		{
			if (!IsInstanceValid(other) || !IsHerdSpecies(other.Kind)) continue;
			float distance = wolf.GlobalPosition.DistanceTo(other.GlobalPosition);
			if (distance <= Ecology.PackBehaviour.PerceptionRadius)
				_quarry.Add(new Ecology.HuntTarget(other.GlobalPosition, distance));
		}
		return Ecology.PackBehaviour.Decide(wolf.GlobalPosition, wolf.Heading, wolf.Den,
			wolf.Stamina, _quarry, wolf.Recovering, TravellerFor(wolf));
	}

	/// <summary>
	/// The traveller as this wolf sees them, or nothing at all when the ecology
	/// is absent. Peace is the default and it is expressed here: without a
	/// grudge on this ground the threat is still handed over, and
	/// <c>PackBehaviour</c> declines it.
	/// </summary>
	private Ecology.TravellerThreat? TravellerFor(Critter wolf)
	{
		if (_ecosystem == null || _ecosystem.Vitals.Dead) return null;
		float distance = wolf.GlobalPosition.DistanceTo(_travellerAt);
		if (distance > Ecology.PackBehaviour.PerceptionRadius) return null;
		int pack = 0;
		foreach (var other in _live)
			if (other != wolf && IsInstanceValid(other) && other.Kind == Species.Wolf &&
				other.GlobalPosition.DistanceTo(wolf.GlobalPosition) <= 35f) pack++;
		return new Ecology.TravellerThreat(_travellerAt, distance,
			_ecosystem.GrudgeAt(wolf.GlobalPosition), pack);
	}

	private double _census;
	private Vector3 _travellerAt;
	private double _biteCooldown;

	/// <summary>Bites the traveller has taken since this window opened.</summary>
	public int Bites { get; private set; }

	/// <summary>Animals the traveller has taken for food.</summary>
	public int Meals { get; private set; }

	/// <summary>
	/// A wolf that has closed on the traveller takes a piece out of them.
	/// Only a wolf that <c>PackBehaviour</c> would engage gets here, so an
	/// unprovoked traveller can stand in the middle of a pack untouched.
	/// </summary>
	private void ResolveBites(double delta)
	{
		_biteCooldown -= delta;
		if (_biteCooldown > 0.0 || _ecosystem == null || _ecosystem.Vitals.Dead) return;
		foreach (var wolf in _live)
		{
			if (!IsInstanceValid(wolf) || wolf.Kind != Species.Wolf) continue;
			if (wolf.GlobalPosition.DistanceTo(_travellerAt) > Ecology.PackBehaviour.KillDistance + 1f)
				continue;
			var threat = TravellerFor(wolf);
			if (!threat.HasValue ||
				!Ecology.PackBehaviour.WillEngageTraveller(wolf.GlobalPosition, wolf.Den, threat.Value))
				continue;
			_ecosystem.Vitals.Wound(Ecology.Ecosystem.BiteDamage);
			Bites++;
			_biteCooldown = Ecology.Ecosystem.BiteCooldownSeconds;
			GD.Print($"[ecology] bitten; vitality {_ecosystem.Vitals.Vitality:0.00}");
			return;
		}
	}

	/// <summary>
	/// The traveller strikes at whatever is in front of them. Four blows put a
	/// wolf down bare-handed, and the first one that lands makes this valley a
	/// place they are hunted in.
	/// </summary>
	public bool TryStrike(Vector3 from, Vector3 facing, float range = 2.5f)
	{
		if (!_ecology) return false;
		foreach (var target in _live)
		{
			if (!IsInstanceValid(target)) continue;
			bool wolf = target.Kind == Species.Wolf;
			if (!wolf && !IsHerdSpecies(target.Kind)) continue;
			var toTarget = target.GlobalPosition - from;
			toTarget.Y = 0f;
			if (toTarget.Length() > range) continue;
			if (facing.LengthSquared() > 0.0001f &&
				toTarget.Normalized().Dot(facing.Normalized()) < 0.3f) continue;
			if (!target.TakeStrike(Ecology.Ecosystem.StrikeDamage)) return true;
			if (wolf)
			{
				// Killing a wolf is the whole of how the world turns on you.
				_ecosystem?.ReportWolfKilledByTraveller(target.GlobalPosition);
				GD.Print("[ecology] a wolf is down; this valley will remember it");
			}
			else
			{
				// Hunting for the pot. Eaten where it fell rather than carried:
				// the inventory and campfire systems exist in this repository but
				// are not part of the production scene, so the ecology feeds the
				// traveller without reaching into systems nobody has switched on.
				_ecosystem?.ReportKill(target.GlobalPosition);
				_ecosystem?.Vitals.Eat(Ecology.Ecosystem.MealFromKill);
				Meals++;
				GD.Print($"[ecology] eaten where it fell; hunger " +
				         $"{_ecosystem?.Vitals.Hunger:0.00}");
			}
			_habitats.Remove(target.HabitatKey);
			_live.Remove(target);
			target.QueueFree();
			return true;
		}
		return false;
	}

	/// <summary>Prey taken by wolves since this window opened.</summary>
	public int Kills { get; private set; }

	/// <summary>
	/// What is alive around the traveller right now. A diagnostic, not a
	/// mechanism: the field is the population, and this only says how much of
	/// it currently has a body.
	/// </summary>
	private void Census(Vector3 player, double delta)
	{
		_census -= delta;
		if (_census > 0.0) return;
		_census = 5.0;
		int deer = 0, rabbit = 0, goat = 0, wolf = 0, other = 0;
		foreach (var animal in _live)
		{
			if (!IsInstanceValid(animal)) continue;
			switch (animal.Kind)
			{
				case Species.Deer: deer++; break;
				case Species.Rabbit: rabbit++; break;
				case Species.Goat: goat++; break;
				case Species.Wolf: wolf++; break;
				default: other++; break;
			}
		}
		int cell = _ecosystem?.Field?.IndexAt(Mathf.FloorToInt(player.X),
			Mathf.FloorToInt(player.Z)) ?? -1;
		string here = cell < 0 ? "no field" :
			$"cell prey {_ecosystem.Field.PreyAt(cell):0.00} " +
			$"predators {_ecosystem.Field.PredatorAt(cell):0.00} " +
			$"grass {_ecosystem.Field.GrassAt(cell):0.00}";
		// What the wolves are actually doing, which is the only way to tell a
		// hunt that never starts from one that never concludes.
		var packState = new System.Text.StringBuilder();
		foreach (var animal in _live)
		{
			if (!IsInstanceValid(animal) || animal.Kind != Species.Wolf) continue;
			float nearest = float.MaxValue;
			foreach (var quarry in _live)
				if (IsInstanceValid(quarry) && IsHerdSpecies(quarry.Kind))
					nearest = Mathf.Min(nearest,
						animal.GlobalPosition.DistanceTo(quarry.GlobalPosition));
			var decision = HuntFor(animal);
			packState.Append($" [{decision.Intent} prey@{(nearest == float.MaxValue ? -1f : nearest):0} " +
			                 $"breath {animal.Stamina:0.00}]");
		}
		string traveller = _ecosystem == null ? "" :
			$" traveller vitality {_ecosystem.Vitals.Vitality:0.00} " +
			$"breath {_ecosystem.Vitals.Breath:0.00} hunger {_ecosystem.Vitals.Hunger:0.00} " +
			$"bitten {Bites} ate {Meals} " +
			$"grudge {(_ecosystem.GrudgeAt(player) ? "yes" : "no")};";
		GD.Print($"[ecology-census] bodies deer {deer} rabbit {rabbit} goat {goat} " +
		         $"wolf {wolf} other {other}; kills {Kills};{traveller} {here};{packState}");
	}

	/// <summary>
	/// Resolve any wolf standing on top of a grazing animal.
	///
	/// Collected first and applied afterwards, so the list is never mutated
	/// while it is being walked, and so one wolf cannot take two deer in one
	/// frame by shifting the indices under itself.
	/// </summary>
	private void ResolveKills()
	{
		Critter taken = null;
		foreach (var wolf in _live)
		{
			if (!IsInstanceValid(wolf) || wolf.Kind != Species.Wolf) continue;
			foreach (var prey in _live)
			{
				if (!IsInstanceValid(prey) || !IsHerdSpecies(prey.Kind)) continue;
				if (wolf.GlobalPosition.DistanceTo(prey.GlobalPosition)
					> Ecology.PackBehaviour.KillDistance) continue;
				taken = prey;
				break;
			}
			if (taken != null) break;
		}
		if (taken == null) return;
		_ecosystem?.ReportKill(taken.GlobalPosition);
		Kills++;
		_habitats.Remove(taken.HabitatKey);
		_live.Remove(taken);
		taken.QueueFree();
	}

	public void Advance(Vector3 player, double delta)
	{
		if (_terrain == null && _window == null) return;

		for (int i = _live.Count - 1; i >= 0; i--)
		{
			var c = _live[i];
			if (!IsInstanceValid(c)) { _habitats.Remove(c.HabitatKey); _live.RemoveAt(i); continue; }
			float d = new Vector2(c.GlobalPosition.X - player.X, c.GlobalPosition.Z - player.Z).Length();
			if (d > (_window == null ? Cull : MarshRetentionRadius) || !c.HabitatValid())
			{
				_habitats.Remove(c.HabitatKey);
				_live.RemoveAt(i);
				c.QueueFree();
				continue;
			}
			c.Steering = _ecology && IsHerdSpecies(c.Kind) ? SteerHerd : null;
			c.Hunting = _ecology && IsPredatorSpecies(c.Kind) ? HuntFor : null;
			c.Advance(player, delta);
		}

		// Spawning is rate limited, but not to one at a time. Crossing a shoreline
		// can invalidate half the population at once, and at one animal per
		// quarter second the meadow the player is walking into stays empty for the
		// four seconds they are looking at it.
		if (_ecology)
		{
			_travellerAt = player;
			ResolveKills();
			ResolveBites(delta);
			Census(player, delta);
		}

		_retry -= delta;
		if (_live.Count >= PopulationLimit || _retry > 0.0) return;
		_retry = 0.12;
		for (int i = 0; i < 3 && _live.Count < PopulationLimit; i++) TrySpawn(player);
	}

	private void TrySpawn(Vector3 player)
	{
		if (_window != null) { TrySpawnAtlas(player); return; }
		int S = _terrain.Size;
		for (int attempt = 0; attempt < 12; attempt++)
		{
			float ang = _rng.Next() * MathF.Tau;
			float rad = _rng.Range(SpawnNear, SpawnFar);
			int x = (int)(player.X + MathF.Cos(ang) * rad);
			int z = (int)(player.Z + MathF.Sin(ang) * rad);
			if (x < 4 || z < 4 || x >= S - 4 || z >= S - 4) continue;

			int i = z * S + x;
			bool water = _terrain.Land[i] == 0;
			int level = _terrain.Level[i];
			var biome = _terrain.Plan.RegionAt(x, z).Biome;

			var species = Choose(biome, water, _terrain.Roads != null && _terrain.Roads.Clear[i] != 0);
			if (species == null) continue;
			if (species == Species.Bird && LiveBirdCount() >= BirdPopulation) continue;

			// Land animals will not stand on a road, a stair or a cliff lip.
			if (!water)
			{
				if (_terrain.StairMask[i] != 0) continue;
				if (TerrainShape.DropBelow(_terrain.Level, S, x, z) > Terrain.Step) continue;
				if (_terrain.Grid.Heights[i] > level) continue;
			}

			var kind = species.Value;
			float y = kind == Species.Fish
				? Palette.WaterLevel - _rng.Range(0.6f, 1.6f)
				: kind is Species.Bird or Species.Butterfly
					? level + _rng.Range(kind == Species.Bird ? 6f : 1.2f, kind == Species.Bird ? 13f : 2.6f)
					: level;

			var critter = new Critter();
			AddChild(critter);
			critter.Position = new Vector3(x + 0.5f, y, z + 0.5f);
			critter.Setup(kind, _terrain, _inkLight, _inkDark,
				unchecked((int)(_rng.Next() * int.MaxValue)));
			_live.Add(critter);
			return;
		}
	}

	/// <summary>
	/// Place a grazing animal or a wolf on open ground, at a density the field
	/// decides.
	///
	/// Deliberately separate from the marsh spawn below, which encodes the
	/// southern wetland's own rules and must keep working untouched when the
	/// ecology is off.
	/// </summary>
	private bool TrySpawnEcologyLand(Vector3 player, AtlasSectorWindow window)
	{
		if (_ecosystem?.Field == null) return false;
		const int spacing = 24;
		int cx = Mathf.FloorToInt(player.X / spacing), cz = Mathf.FloorToInt(player.Z / spacing);
		for (int ring = 1; ring <= (int)(MarshSpawnRadius / spacing); ring++)
		for (int dz = -ring; dz <= ring; dz++)
		for (int dx = -ring; dx <= ring; dx++)
		{
			if (Math.Abs(dx) != ring && Math.Abs(dz) != ring) continue;
			int cellX = cx + dx, cellZ = cz + dz;
			long key = ((long)cellX << 32) | (uint)cellZ | 1L << 63;
			if (_habitats.Contains(key)) continue;
			int seed = unchecked(_seed ^ cellX * 374761393 ^ cellZ * 668265263 ^ 0x1EC0);
			var rng = new Rng(seed);
			float admission = rng.Next();
			var point = new Vector3((cellX + rng.Range(.2f, .8f)) * spacing, 0f,
				(cellZ + rng.Range(.2f, .8f)) * spacing);
			float distance = new Vector2(point.X - player.X, point.Z - player.Z).Length();
			if (distance < 18f || distance > MarshSpawnRadius) continue;

			// The field decides how crowded this ground is. A valley that has
			// been hunted out stays quiet until the equation refills it.
			var field = _ecosystem.Field;
			int cell = field.IndexAt(Mathf.FloorToInt(point.X), Mathf.FloorToInt(point.Z));
			if (field.FertilityAt(cell) <= 0.01f) continue;
			float preyChance = Mathf.Clamp(field.PreyAt(cell) / 6f, 0f, .5f);
			float wolfChance = Mathf.Clamp(field.PredatorAt(cell) / 3f, 0f, .16f);
			Species species;
			if (admission < wolfChance) species = Species.Wolf;
			else if (admission < wolfChance + preyChance)
				species = rng.Chance(.5f) ? Species.Deer : rng.Chance(.6f) ? Species.Rabbit : Species.Goat;
			else continue;
			if (!EcologySpeciesAllowed(species, _ecology)) continue;

			int alive = 0;
			foreach (var animal in _live) if (animal.Kind == species) alive++;
			if (alive >= (species == Species.Wolf ? 4 : 8)) continue;
			if (!TryEcologyLandHabitat(window, point, out float ground)) continue;

			point.Y = ground;
			var born = new Critter { HabitatKey = key };
			AddChild(born);
			born.GlobalPosition = point;
			born.Setup(species, _window, _inkLight, _inkDark, seed);
			_live.Add(born);
			_habitats.Add(key);
			return true;
		}
		return false;
	}

	private void TrySpawnAtlas(Vector3 player)
	{
		var window = _window();
		if (window == null) return;
		if (_ecology && TrySpawnEcologyLand(player, window)) return;
		const int spacing = 24;
		int cx = Mathf.FloorToInt(player.X / spacing), cz = Mathf.FloorToInt(player.Z / spacing);
		for (int ring = 1; ring <= (int)(MarshSpawnRadius / spacing); ring++)
		for (int dz = -ring; dz <= ring; dz++)
		for (int dx = -ring; dx <= ring; dx++)
		{
			if (Math.Abs(dx) != ring && Math.Abs(dz) != ring) continue;
			int cellX = cx + dx, cellZ = cz + dz;
			long key = ((long)cellX << 32) | (uint)cellZ;
			if (_habitats.Contains(key)) continue;
			int seed = unchecked(_seed ^ cellX * 374761393 ^ cellZ * 668265263 ^ 0x5EA7);
			var rng = new Rng(seed);
			float admission = rng.Next();
			var point = new Vector3((cellX + rng.Range(.2f, .8f)) * spacing, 0f,
				(cellZ + rng.Range(.2f, .8f)) * spacing);
			float distance = new Vector2(point.X - player.X, point.Z - player.Z).Length();
			if (distance < 18f || distance > MarshSpawnRadius) continue;
			int x = Mathf.FloorToInt(point.X) - window.Data.OriginX;
			int z = Mathf.FloorToInt(point.Z) - window.Data.OriginZ;
			if (x < 2 || z < 2 || x >= window.Data.Width - 2 || z >= window.Data.Depth - 2) continue;
			int i = z * window.Data.Width + x;
			int depth = window.Data.WaterSurface[i] - window.Grid.Top[i];
			Species species = depth >= 2 ? Species.Fish : rng.Chance(.70f) ? Species.Heron : Species.Butterfly;
			float chance = species == Species.Fish ? .32f : .11f * ProductionTerrainGuide.SouthernLatitudeAt(cellZ * spacing);
			if (admission >= chance) continue;
			int count = 0;
			foreach (var animal in _live) if (animal.Kind == species) count++;
			if (count >= (species == Species.Fish ? FishPopulation : species == Species.Heron ? 2 : 1)) continue;
			if (!TryAtlasHabitat(window, species, point, out float ground, out float water)) continue;
			point.Y = species == Species.Fish ? water - .8f : species == Species.Butterfly ? ground + 1.9f : ground;
			var critter = new Critter { HabitatKey = key };
			AddChild(critter);
			critter.GlobalPosition = point;
			critter.Setup(species, _window, _inkLight, _inkDark, seed);
			_live.Add(critter);
			_habitats.Add(key);
			return;
		}
	}

	/// <summary>
	/// Dry, level, unobstructed ground for a grazing animal or a wolf.
	///
	/// Separate from <see cref="TryAtlasHabitat"/> on purpose. That one answers
	/// for the southern marsh and encodes the marsh's own requirements — reed
	/// ground, southern latitude, standing water. Land animals want the
	/// opposite of most of that, and folding both into one function would mean
	/// a chain of species exceptions inside a habitat rule.
	/// </summary>
	internal static bool TryEcologyLandHabitat(AtlasSectorWindow window, Vector3 at,
		out float ground)
	{
		ground = 0f;
		if (window == null) return false;
		var data = window.Data;
		var grid = window.Grid;
		int x = Mathf.FloorToInt(at.X) - data.OriginX, z = Mathf.FloorToInt(at.Z) - data.OriginZ;
		if (x < 2 || z < 2 || x >= data.Width - 2 || z >= data.Depth - 2) return false;

		int i = z * data.Width + x;
		if (data.WaterSurface[i] > 0) return false;

		int bed = grid.Top[i];
		byte cap = grid.Cap[i];
		if (cap is not (Palette.MOSS or Palette.MUD or Palette.SAND) &&
			!Palette.IsGrassSurface(cap)) return false;
		if (grid.MeshHeightAt(x, z) > bed) return false;

		// Level enough to stand on, and clear enough overhead to walk through.
		// A creature spawned in a crevice spends its life jittering against it.
		for (int dz = -1; dz <= 1; dz++)
		for (int dx = -1; dx <= 1; dx++)
		{
			int n = (z + dz) * data.Width + x + dx;
			if (data.WaterSurface[n] > 0) return false;
			if (Math.Abs(grid.Top[n] - bed) > 1) return false;
			for (int y = bed + 1; y <= bed + 3; y++)
				if (grid.SolidAt(x + dx, y, z + dz)) return false;
		}
		ground = bed;
		return true;
	}

	/// <summary>
	/// Can a land animal stand on this block right now?
	///
	/// Deliberately looser than <see cref="TryEcologyLandHabitat"/>. That one
	/// picks a birthplace and wants flat open ground; this one asks whether a
	/// walking creature may take one more step, and a walking creature crosses
	/// slopes, shoulders and terraces all day. Using the spawn rule for movement
	/// refused every step off perfectly level ground, which pinned wolves at
	/// eighteen units from prey they could see and never reach.
	/// </summary>
	internal static bool TryEcologyLandStep(AtlasSectorWindow window, Vector3 at,
		float fromGround, out float ground)
	{
		ground = 0f;
		if (window == null) return false;
		var data = window.Data;
		var grid = window.Grid;
		int x = Mathf.FloorToInt(at.X) - data.OriginX, z = Mathf.FloorToInt(at.Z) - data.OriginZ;
		if (x < 2 || z < 2 || x >= data.Width - 2 || z >= data.Depth - 2) return false;

		int i = z * data.Width + x;
		if (data.WaterSurface[i] > 0) return false;

		int bed = grid.Top[i];
		byte cap = grid.Cap[i];
		if (cap is not (Palette.MOSS or Palette.MUD or Palette.SAND) &&
			!Palette.IsGrassSurface(cap)) return false;
		if (grid.MeshHeightAt(x, z) > bed) return false;
		// One terrace at a time, measured against the ground being walked on
		// rather than the live height: mid-hop those differ by most of a step.
		if (MathF.Abs(bed - fromGround) > Terrain.Step + 0.5f) return false;
		for (int y = bed + 1; y <= bed + 2; y++)
			if (grid.SolidAt(x, y, z)) return false;

		ground = bed;
		return true;
	}

	internal static bool TryAtlasHabitat(AtlasSectorWindow window, Species species, Vector3 at,
		out float ground, out float water)
	{
		ground = water = 0f;
		if (window == null) return false;
		var data = window.Data;
		var grid = window.Grid;
		int x = Mathf.FloorToInt(at.X) - data.OriginX, z = Mathf.FloorToInt(at.Z) - data.OriginZ;
		if (x < 2 || z < 2 || x >= data.Width - 2 || z >= data.Depth - 2) return false;
		string detail = window.GroundDetailSetAt(x, z);
		if (species != Species.Fish && (ProductionTerrainGuide.SouthernLatitudeAt(at.Z) <= 0f ||
			detail is not ("reed-root-moss" or "sand-reed-petal"))) return false;
		int i = z * data.Width + x, bed = grid.Top[i];
		water = data.WaterSurface[i] > 0 ? data.WaterSurface[i] + .35f : 0f;
		ground = bed;
		float depth = water - bed;
		if (species == Species.Fish)
		{
			if (water <= 0f || depth < 1.8f) return false;
			for (int dz = -1; dz <= 1; dz++)
			for (int dx = -1; dx <= 1; dx++)
				if (data.WaterSurface[(z + dz) * data.Width + x + dx] != data.WaterSurface[i] ||
					grid.SolidAt(x + dx, Mathf.FloorToInt(water - .8f), z + dz)) return false;
			return true;
		}
		if (species == Species.Heron && (depth > 1.8f || water == 0f && data.Wetness[i] < 160)) return false;
		if (water == 0f && grid.Cap[i] is not (Palette.MOSS or Palette.MUD or Palette.SAND) &&
			!Palette.IsGrassSurface(grid.Cap[i])) return false;
		if (bed > Terrain.Sea + 10 || grid.MeshHeightAt(x, z) > bed) return false;
		if (species == Species.Butterfly) ground = Math.Max(bed, water);
		for (int dz = -1; dz <= 1; dz++)
		for (int dx = -1; dx <= 1; dx++)
		{
			if (Math.Abs(grid.Top[(z + dz) * data.Width + x + dx] - bed) > 1) return false;
			for (int y = Mathf.FloorToInt(ground); y <= MathF.Ceiling(ground + 3f); y++)
				if (grid.SolidAt(x + dx, y, z + dz)) return false;
		}
		return true;
	}

	private int LiveBirdCount()
	{
		int count = 0;
		foreach (var critter in _live)
			if (IsInstanceValid(critter) && critter.Kind == Species.Bird) count++;
		return count;
	}

	private Species? Choose(Biome biome, bool water, bool nearRoad)
	{
		if (water) return _rng.Chance(0.75f) ? Species.Fish : null;
		// Creatures give a road a wide berth. It is where people are.
		if (nearRoad && _rng.Chance(0.7f)) return null;

		return biome switch
		{
			Biome.Forest => _rng.Chance(0.55f) ? Species.Deer
				: _rng.Chance(0.6f) ? Species.Bird : Species.Rabbit,
			Biome.Meadow => _rng.Chance(0.4f) ? Species.Rabbit
				: _rng.Chance(0.5f) ? Species.Butterfly : Species.Deer,
			Biome.Sakura => _rng.Chance(0.6f) ? Species.Butterfly : Species.Bird,
			Biome.Plains => _rng.Chance(0.5f) ? Species.Rabbit : Species.Bird,
			Biome.Highland => _rng.Chance(0.6f) ? Species.Goat : Species.Bird,
			Biome.SnowyHills => _rng.Chance(0.7f) ? Species.Goat : null,
			Biome.Shore => _rng.Chance(0.7f) ? Species.Bird : null,
			Biome.Wetland => _rng.Chance(0.5f) ? Species.Bird : Species.Butterfly,
			_ => Species.Bird,
		};
	}
}

/// <summary>One animal: a shared body plan, its own proportions, its own gait.</summary>
public partial class Critter : Node3D
{
	/// <summary>World units per creature voxel. Matches the traveller's scale.</summary>
	private const float S = 0.30f;

	private readonly struct Tone
	{
		public readonly Color Linear;
		public readonly bool Pale;

		public Tone(uint hex)
		{
			var s = new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);
			Linear = s.SrgbToLinear();
			Pale = s.R * 0.2126f + s.G * 0.7152f + s.B * 0.0722f >= Palette.LightFaceLuma;
		}
	}

	private Species _kind;
	public Species Kind => _kind;
	private Terrain _terrain;
	private ShaderMaterial _inkLight, _inkDark;
	private Rng _rng;

	private Node3D _body, _head;
	private readonly List<Node3D> _limbs = new();
	private float _phase;
	private float _yaw;
	private Vector3 _heading = Vector3.Forward;
	private float _speed;
	private double _decide;
	private float _startle;
	private float _spookCooldown;
	/// <summary>Vertical state. Land animals leave the ground to change terrace.</summary>
	private float _vy;
	private bool _airborne;
	private float _groundY;
	private Func<AtlasSectorWindow> _window;
	private float _waterSurface = Palette.WaterLevel;
	public long HabitatKey { get; set; }

	/// <summary>
	/// Optional steering, supplied by the ecology. Null in ordinary play, which
	/// is what keeps the flag honest: no delegate, no behaviour change.
	/// </summary>
	public Func<Critter, Vector3> Steering { get; set; }

	public Vector3 Heading => _heading;

	/// <summary>
	/// Optional hunting, supplied by the ecology. Null in ordinary play and for
	/// every creature that is not a predator.
	/// </summary>
	public Func<Critter, Ecology.PackDecision> Hunting { get; set; }

	/// <summary>
	/// Where this creature was born, and where a predator returns when it has
	/// ranged too far. Set once at spawn; a den does not move.
	/// </summary>
	public Vector3 Den { get; private set; }

	/// <summary>Breath, 0 to 1. Only sprinting spends it.</summary>
	public float Stamina { get; private set; } = 1f;

	/// <summary>Broke off last frame, and will not commit again until recovered.</summary>
	public bool Recovering { get; private set; }

	private float _vitality = 1f;

	/// <summary>Take a blow. False once it has taken the last one.</summary>
	public bool TakeStrike(float amount)
	{
		_vitality -= amount;
		// A struck animal bolts whether or not it survives the blow.
		_startle = 1f;
		return _vitality > 0f;
	}

	public bool HabitatValid()
	{
		if (_window == null) return true;
		// Land animals answer to the land rule; the marsh animals to the marsh's.
		return Fauna.IsHerdSpecies(_kind) || Fauna.IsPredatorSpecies(_kind)
			? Fauna.TryEcologyLandStep(_window(), GlobalPosition, _groundY, out _)
			: Fauna.TryAtlasHabitat(_window(), _kind, GlobalPosition, out _, out _);
	}

	public void Setup(Species kind, Func<AtlasSectorWindow> window, ShaderMaterial inkLight,
		ShaderMaterial inkDark, int seed)
	{
		_kind = kind;
		_window = window;
		_inkLight = inkLight;
		_inkDark = inkDark;
		_rng = new Rng(seed);
		_phase = _rng.Next() * 6f;
		_yaw = _rng.Next() * Mathf.Tau;
		bool land = Fauna.IsHerdSpecies(kind) || Fauna.IsPredatorSpecies(kind);
		bool placed = land
			? Fauna.TryEcologyLandHabitat(window(), GlobalPosition, out _groundY)
			: Fauna.TryAtlasHabitat(window(), kind, GlobalPosition, out _groundY, out _waterSurface);
		if (!placed)
			throw new InvalidOperationException("Wildlife spawn has no matching habitat");
		Den = GlobalPosition;
		Build();
	}

	public void Setup(Species kind, Terrain terrain, ShaderMaterial inkLight,
		ShaderMaterial inkDark, int seed)
	{
		_kind = kind;
		_terrain = terrain;
		_inkLight = inkLight;
		_inkDark = inkDark;
		_rng = new Rng(seed);
		_phase = _rng.Next() * 6f;
		_yaw = _rng.Next() * Mathf.Tau;

		// Seed the ground height from the terrain, not from the first successful
		// Legal() call.
		//
		// This was a chicken and egg that dropped every land animal through the
		// floor. Legal() decides whether a square is reachable by comparing it to
		// _groundY, and _groundY was only ever assigned when Legal() succeeded —
		// so on a creature that started at zero, every test read "that ledge is
		// twenty-six blocks up, unreachable", nothing ever set the field, and
		// Vertical() saw a twenty-six block drop below it and started falling.
		// They appeared for a second and sank.
		int S = terrain.Size;
		int gx = Mathf.Clamp((int)GlobalPosition.X, 0, S - 1);
		int gz = Mathf.Clamp((int)GlobalPosition.Z, 0, S - 1);
		_groundY = terrain.Level[gz * S + gx];

		Den = GlobalPosition;
		Build();
	}

	/* ----------------------------------------------------------------
	 * Bodies
	 * ---------------------------------------------------------------- */
	private void Build()
	{
		_body = new Node3D();
		AddChild(_body);

		switch (_kind)
		{
			case Species.Deer: Quadruped(new Tone(0xc59a86), new Tone(0xf1e2d6), 3.0f, 2.6f, 5.4f, 3.1f, 0.9f); break;
			case Species.Goat: Quadruped(new Tone(0xe8e2ea), new Tone(0xb9aec0), 2.7f, 2.4f, 4.4f, 2.4f, 0.8f); break;
			case Species.Rabbit: Quadruped(new Tone(0xe3d5cf), new Tone(0xf4ece6), 1.9f, 1.7f, 2.7f, 1.1f, 0.6f); break;
			case Species.Bird: Flyer(new Tone(0xdfe6f2), new Tone(0xb9c2d8), 0.55f); break;
			case Species.Butterfly: Flyer(new Tone(0xf8ccda), new Tone(0xdccef1), 0.34f); break;
			case Species.Fish: Swimmer(new Tone(0xa9c2d8)); break;
			// Longer, lower and narrower than the deer it hunts, with a pale
			// underside. At thirty pixels tall the silhouette is the whole of the
			// recognition, so the proportions carry it rather than the colour.
			case Species.Wolf: Quadruped(new Tone(0x6d6b78), new Tone(0xc8c3cc), 2.6f, 2.4f, 5.8f, 2.4f, 1.05f); break;
			case Species.Heron: Wader(); break;
		}
	}

	private void Quadruped(Tone coat, Tone belly, float w, float h, float d, float legY, float headScale)
	{
		float legLen = legY;
		Box(_body, w, h, d, coat, new Vector3(0, (legLen + h * 0.5f) * S, 0));
		Box(_body, w * 0.86f, h * 0.34f, d * 0.9f, belly,
			new Vector3(0, (legLen + h * 0.18f) * S, 0), outlined: false);

		// Nose toward +Z.
		//
		// The whole menagerie was walking backwards. Yaw is derived with
		// Atan2(heading.x, heading.z), which turns +Z to face the heading — the
		// convention Character.cs is built to — but these bodies had the head at
		// NEGATIVE Z, so every animal presented its tail to wherever it was going.
		// Fixed in the model rather than by adding a half-turn to the yaw, so the
		// project keeps one facing convention instead of two.
		_head = Pivot(_body, new Vector3(0, (legLen + h * 0.85f) * S, d * 0.42f * S));
		Box(_head, w * 0.72f * headScale, h * 0.72f * headScale, w * 0.8f * headScale,
			coat, new Vector3(0, 0, w * 0.3f * headScale * S));
		if (_kind == Species.Rabbit)
		{
			// Ears. The one piece of species-specific modelling in here, because a
			// rabbit without them is just a small deer.
			Box(_head, 0.4f, 1.7f, 0.4f, coat, new Vector3(-0.45f * S, 1.2f * S, 0f), outlined: false);
			Box(_head, 0.4f, 1.7f, 0.4f, coat, new Vector3(0.45f * S, 1.2f * S, 0f), outlined: false);
		}
		else if (_kind == Species.Goat)
		{
			Box(_head, 0.35f, 0.9f, 0.35f, new Tone(0x8a6270), new Vector3(-0.4f * S, 0.9f * S, 0f), outlined: false);
			Box(_head, 0.35f, 0.9f, 0.35f, new Tone(0x8a6270), new Vector3(0.4f * S, 0.9f * S, 0f), outlined: false);
		}

		float lx = w * 0.32f, lz = d * 0.32f;
		for (int i = 0; i < 4; i++)
		{
			float sx = (i & 1) == 0 ? -lx : lx;
			float sz = i < 2 ? -lz : lz;
			var pivot = Pivot(_body, new Vector3(sx * S, legLen * S, sz * S));
			Box(pivot, 0.8f, legLen, 0.8f, coat, new Vector3(0, -legLen * 0.5f * S, 0), outlined: false);
			_limbs.Add(pivot);
		}
	}

	private void Flyer(Tone body, Tone wing, float scale)
	{
		Box(_body, 1.1f * scale, 1.0f * scale, 2.0f * scale, body, Vector3.Zero);
		for (int i = 0; i < 2; i++)
		{
			var pivot = Pivot(_body, new Vector3((i == 0 ? -0.5f : 0.5f) * scale * S, 0.2f * scale * S, 0));
			Box(pivot, 2.4f * scale, 0.22f * scale, 1.5f * scale, wing,
				new Vector3((i == 0 ? -1.2f : 1.2f) * scale * S, 0, 0), outlined: false);
			_limbs.Add(pivot);
		}
	}

	private void Wader()
	{
		var pale = new Tone(0xe5dedb);
		var wing = new Tone(0xaeb9cc);
		var leg = new Tone(0x8c8e84);
		Box(_body, 1.4f, 1.5f, 3.0f, pale, new Vector3(0, 1.25f, 0));
		Box(_body, 1.55f, .65f, 2.5f, wing, new Vector3(0, 1.45f, -.08f), outlined: false);
		_head = Pivot(_body, new Vector3(0, 1.42f, .32f));
		Box(_head, .65f, 2.1f, .75f, pale, new Vector3(0, .25f, .08f));
		Box(_head, .9f, .85f, 1.1f, pale, new Vector3(0, .61f, .15f));
		Box(_head, .3f, .3f, 1.7f, new Tone(0xd4b793), new Vector3(0, .58f, .50f), outlined: false);
		for (int side = -1; side <= 1; side += 2)
		{
			var pivot = Pivot(_body, new Vector3(side * .12f, 1.14f, 0));
			Box(pivot, .24f, 3.65f, .24f, leg, new Vector3(0, -.55f, 0), outlined: false);
			Box(pivot, .42f, .16f, 1.0f, leg, new Vector3(0, -1.11f, .08f), outlined: false);
			_limbs.Add(pivot);
		}
	}

	private void Swimmer(Tone tone)
	{
		Box(_body, 0.75f, 1.0f, 2.2f, tone, Vector3.Zero);
		// Tail behind, which is now -Z. See the note in Quadruped.
		var tail = Pivot(_body, new Vector3(0, 0, -1.1f * S));
		Box(tail, 0.2f, 1.1f, 1.0f, tone, new Vector3(0, 0, -0.5f * S), outlined: false);
		_limbs.Add(tail);
	}

	private Node3D Pivot(Node3D parent, Vector3 at)
	{
		var n = new Node3D { Position = at };
		parent.AddChild(n);
		return n;
	}

	/// <summary>See Character.Box — the same silhouette-only rule applies.</summary>
	private void Box(Node3D parent, float w, float h, float d, Tone tone, Vector3 at,
		bool outlined = true)
	{
		float wx = w * S, wy = h * S, wz = d * S;
		var mesh = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(wx, wy, wz) },
			Position = at,
		};
		var mat = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/character.gdshader") };
		mat.SetShaderParameter("albedo", Palette.ShaderRgba(tone.Linear));
		mat.SetShaderParameter("sun_dir", Palette.SunDir);
		mesh.MaterialOverride = mat;
		parent.AddChild(mesh);

		if (!outlined) return;

		var ink = new MeshInstance3D
		{
			Mesh = InkBuilder.Box(wx, wy, wz, tone.Pale),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(new Vector3(-wx, -wy, -wz), new Vector3(wx * 2, wy * 2, wz * 2)),
		};
		ink.SetSurfaceOverrideMaterial(0, tone.Pale ? _inkLight : _inkDark);
		mesh.AddChild(ink);
	}

	/* ----------------------------------------------------------------
	 * Behaviour
	 * ---------------------------------------------------------------- */
	private float Cruise => _kind switch
	{
		Species.Deer => 3.4f,
		Species.Goat => 2.2f,
		Species.Rabbit => 3.0f,
		Species.Bird => 7.5f,
		Species.Butterfly => 1.8f,
		Species.Fish => 2.6f,
		Species.Heron => .85f,
		Species.Wolf => 3.8f,
		_ => 2f,
	};

	public void Advance(Vector3 player, double delta)
	{
		float dt = (float)delta;
		var here = GlobalPosition;

		// Flight, at TOUCHING distance and once.
		//
		// The first version bolted at seven to thirteen units and re-triggered
		// every frame the player stayed inside that ring, so a meadow emptied
		// ahead of you and nothing ever settled — which is the opposite of a world
		// that is supposed to feel alive and indifferent. An animal should let you
		// walk up to it, move off a little when you are almost on top of it, and
		// then go back to what it was doing.
		float near = new Vector2(player.X - here.X, player.Z - here.Z).Length();
		float radius = _kind is Species.Bird or Species.Butterfly ? 3.4f : 2.6f;
		_spookCooldown = Mathf.Max(0f, _spookCooldown - dt);
		if (near < radius && _spookCooldown <= 0f)
		{
			_startle = 1f;
			// A refractory period, so standing next to something does not hold it
			// in a permanent panic.
			_spookCooldown = 7f;
			var away = new Vector3(here.X - player.X, 0, here.Z - player.Z);
			if (away.LengthSquared() > 0.01f) _heading = away.Normalized();
			_decide = 0.9f;
		}
		// Short. A second and a half of trotting is a few units of ground, which
		// is "moved off a bit" rather than "fled the province".
		_startle = Mathf.Max(0f, _startle - dt * 1.5f);

		_decide -= delta;
		if (_decide <= 0.0)
		{
			_decide = _rng.Range(1.6f, 4.5f);
			// Idling is as much of the behaviour as moving. A meadow where every
			// rabbit is permanently in transit looks like a screensaver.
			bool rest = _kind != Species.Bird && _rng.Chance(0.35f);
			_speed = rest ? 0f : Cruise * _rng.Range(0.55f, 1.05f);
			float turn = _rng.Bell() * 1.5f;
			_heading = _heading.Rotated(Vector3.Up, turn);
		}

		// The herd bends the wander rather than replacing it, and never while
		// startled — a bolting animal is not thinking about its neighbours.
		if (Steering != null && _startle <= 0.01f && _speed > 0.01f)
		{
			var steered = Steering(this);
			if (steered.LengthSquared() > 0.0001f)
				_heading = _heading.Lerp(steered, 1f - Mathf.Exp(-2.5f * dt)).Normalized();
		}

		// A hunt overrides the wander outright. A wolf on a deer is not idling
		// in its general direction, and a resting wolf that sees one gets up.
		float pace = 1f;
		if (Hunting != null && _startle <= 0.01f)
		{
			var decision = Hunting(this);
			if (decision.Heading.LengthSquared() > 0.0001f)
				_heading = _heading.Lerp(decision.Heading, 1f - Mathf.Exp(-6f * dt)).Normalized();
			pace = decision.SpeedMultiplier;
			bool sprinting = decision.Intent == Ecology.PackIntent.Sprint;
			Stamina = Mathf.Clamp(Stamina + dt * (sprinting
				? -Ecology.PackBehaviour.StaminaDrainPerSecond
				: Ecology.PackBehaviour.StaminaRegenPerSecond), 0f, 1f);
			Recovering = decision.Intent == Ecology.PackIntent.Recover;
			if (decision.Intent != Ecology.PackIntent.Wander) _speed = Cruise;
		}

		float speed = (_speed + _startle * Cruise * 1.4f) * pace;
		if (speed > 0.01f)
		{
			var step = _heading * speed * dt;
			var want = here + step;
			if (!Legal(want, out float ground))
			{
				// Turn rather than stop. A creature stuck against a cliff jittering
				// in place is more distracting than one that simply walks away.
				_heading = _heading.Rotated(Vector3.Up, 2.2f);
			}
			else
			{
				want.Y = here.Y;
				GlobalPosition = want;
				_groundY = ground;
			}

			_yaw = Mathf.LerpAngle(_yaw, Mathf.Atan2(_heading.X, _heading.Z), 1f - Mathf.Exp(-7f * dt));
		}
		else if (Legal(here, out float standing)) _groundY = standing;

		Vertical(dt);
		Rotation = new Vector3(0, _yaw, 0);

		// One phase accumulator drives the gait, exactly as the traveller's does:
		// a walk and a bolt are the same curve at different rates.
		float cadence = Mathf.Clamp(speed / Cruise, 0f, 1.6f);
		_phase += dt * (_kind switch
		{
			Species.Butterfly => 17f,
			Species.Bird => 11f + cadence * 6f,
			Species.Fish => 5f + cadence * 4f,
			_ => 2.5f + cadence * 9f,
		});

		Gait(cadence, dt);
	}

	/// <summary>
	/// Height, and how a creature changes terrace.
	///
	/// Snapping straight to whatever the ground turned out to be is what the
	/// first version did, and on a world built entirely out of two-block terraces
	/// that means every animal teleports up and down all day. A terrace is a step
	/// a creature has to LEAVE THE GROUND to take, so it does: an upward change
	/// launches a hop with enough speed to clear it, a downward one just walks off
	/// the edge, and gravity handles both. The gait tucks the legs while the feet
	/// are off the ground, which is the whole reason the hop reads as a hop rather
	/// than as a smoothed slide.
	///
	/// Swimmers and flyers never touch this: they have no feet on anything.
	/// </summary>
	private void Vertical(float dt)
	{
		var at = GlobalPosition;

		if (_kind is Species.Fish or Species.Bird or Species.Butterfly)
		{
			float want = _kind switch
			{
				Species.Fish => _window == null ? Palette.WaterLevel - 1.1f :
					Math.Clamp(_waterSurface - .8f, _groundY + .4f, _waterSurface - .4f),
				Species.Bird => _groundY + 10f,
				_ => _groundY + 1.9f,
			};
			float bob = _kind switch
			{
				Species.Bird => Mathf.Sin(_phase * 0.7f) * 0.10f,
				Species.Butterfly => Mathf.Sin(_phase * 0.5f) * 0.16f,
				_ => 0f,
			};
			float rate = _kind == Species.Fish ? 2f : 1.4f;
			GlobalPosition = new Vector3(at.X,
				Mathf.Lerp(at.Y, want + bob, 1f - Mathf.Exp(-rate * dt)), at.Z);
			return;
		}

		const float Gravity = 34f;
		float foot = at.Y;

		// A creature that has somehow ended up well under the terrain is put back
		// on it rather than left to fall for ever. Nothing should reach this, but
		// an animal quietly sinking out of the world is both the worst-looking
		// possible failure and the hardest to notice in code.
		if (foot < _groundY - 24f)
		{
			GlobalPosition = new Vector3(at.X, _groundY, at.Z);
			_vy = 0f;
			_airborne = false;
			return;
		}

		if (!_airborne)
		{
			float rise = _groundY - foot;
			if (rise > 0.35f)
			{
				// Enough to clear the step with a little over, which is what makes
				// the arc visible rather than a scramble.
				_vy = Mathf.Sqrt(2f * Gravity * (rise + 0.45f));
				_airborne = true;
			}
			else if (rise < -0.35f)
			{
				// Walked off a lip. No push, just let go.
				_vy = 0f;
				_airborne = true;
			}
			else
			{
				GlobalPosition = new Vector3(at.X, _groundY, at.Z);
				return;
			}
		}

		_vy -= Gravity * dt;
		float y = foot + _vy * dt;
		if (_vy <= 0f && y <= _groundY)
		{
			y = _groundY;
			_vy = 0f;
			_airborne = false;
		}
		GlobalPosition = new Vector3(at.X, y, at.Z);
	}

	private void Gait(float cadence, float dt)
	{
		switch (_kind)
		{
			case Species.Bird:
			case Species.Butterfly:
			{
				// Wings beat whether or not the creature is going anywhere: it is
				// holding itself up either way.
				float beat = Mathf.Sin(_phase) * (_kind == Species.Butterfly ? 1.15f : 0.72f);
				if (_limbs.Count >= 2)
				{
					_limbs[0].Rotation = new Vector3(0, 0, -beat);
					_limbs[1].Rotation = new Vector3(0, 0, beat);
				}
				_body.Position = new Vector3(0, Mathf.Sin(_phase * 2f) * 0.02f, 0);
				break;
			}
			case Species.Heron:
			{
				for (int i = 0; i < _limbs.Count; i++)
					_limbs[i].Rotation = new Vector3(Mathf.Sin(_phase + i * Mathf.Pi) * cadence * .24f, 0, 0);
				if (_head != null) _head.Rotation = new Vector3(Mathf.Sin(_phase * .35f) * .10f, 0, 0);
				break;
			}
			case Species.Fish:
			{
				if (_limbs.Count >= 1)
					_limbs[0].Rotation = new Vector3(0, Mathf.Sin(_phase) * 0.55f, 0);
				break;
			}
			default:
			{
				// Off the ground: legs tucked, front reaching. Without this the
				// animal runs on air through the whole arc and the hop reads as a
				// glitch rather than as a jump.
				if (_airborne)
				{
					float t = Mathf.Clamp(_vy / 6f, -1f, 1f);
					for (int i = 0; i < _limbs.Count; i++)
					{
						bool front = i < 2;
						_limbs[i].Rotation = new Vector3(front ? 0.85f + t * 0.4f : -0.7f - t * 0.3f, 0, 0);
					}
					_body.Position = new Vector3(0, 0, 0);
					if (_head != null) _head.Rotation = new Vector3(-t * 0.25f, 0, 0);
					break;
				}

				// Diagonal pairs, which is what a quadruped actually does and reads
				// correctly even at this size.
				float swing = Mathf.Sin(_phase) * cadence * 0.75f;
				for (int i = 0; i < _limbs.Count; i++)
				{
					bool lead = i == 0 || i == 3;
					_limbs[i].Rotation = new Vector3(lead ? swing : -swing, 0, 0);
				}
				// Rabbits hop rather than walk: the body rises with the stride
				// instead of staying level over it.
				float bob = _kind == Species.Rabbit
					? Mathf.Abs(Mathf.Sin(_phase)) * cadence * 0.22f
					: Mathf.Abs(Mathf.Sin(_phase * 2f)) * cadence * 0.05f;
				_body.Position = new Vector3(0, bob, 0);
				if (_head != null)
					_head.Rotation = new Vector3(Mathf.Sin(_phase * 0.5f) * 0.06f * cadence, 0, 0);
				break;
			}
		}
	}

	/// <summary>Is this somewhere the creature can be, and how high is the ground?</summary>
	private bool Legal(Vector3 at, out float ground)
	{
		if (_window != null)
		{
			// Land animals answer to the land rule. Sending them through the
			// marsh rule refuses every step outside the southern wetland, and a
			// creature whose every step is refused does not stand still — it
			// turns on the spot forever, which looks like a frozen world.
			if (Fauna.IsHerdSpecies(_kind) || Fauna.IsPredatorSpecies(_kind))
				return Fauna.TryEcologyLandStep(_window(), at, _groundY, out ground);
			if (!Fauna.TryAtlasHabitat(_window(), _kind, at, out ground, out float surface)) return false;
			if (_kind == Species.Heron && Math.Abs(ground - _groundY) > .35f) return false;
			_waterSurface = surface;
			return true;
		}
		int S = _terrain.Size;
		int x = (int)MathF.Floor(at.X), z = (int)MathF.Floor(at.Z);
		ground = at.Y;
		if (x < 2 || z < 2 || x >= S - 2 || z >= S - 2) return false;

		int i = z * S + x;
		bool water = _terrain.Land[i] == 0;
		if (_kind == Species.Fish) return water;

		if (_kind is Species.Bird or Species.Butterfly)
		{
			ground = MathF.Max(_terrain.Level[i], Palette.WaterLevel);
			return true;
		}

		if (water) return false;
		int level = _terrain.Level[i];
		// Measured against the ground the creature is walking on, not against its
		// current height. Mid-hop those differ by most of a terrace, and testing
		// the live Y made a creature reject the very ledge it was jumping onto.
		if (MathF.Abs(level - _groundY) > Terrain.Step + 0.5f) return false;
		if (_terrain.Grid.Heights[i] > level) return false;
		ground = level;
		return true;
	}
}
