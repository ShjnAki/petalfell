using Godot;
using Petalfell.World;

namespace Petalfell.Ecology;

/// <summary>
/// Owns the population field and carries it forward in play.
///
/// The field is the only thing in this namespace that outlives what the
/// traveller can see, so it needs exactly one owner in the scene and nothing
/// else may construct it.
/// </summary>
public partial class Ecosystem : Node
{
	/// <summary>
	/// Set once from the command line. Everything ecological is gated on this,
	/// because a contribution that changes default behaviour is not reviewable.
	/// </summary>
	public static bool Enabled { get; set; }

	/// <summary>
	/// Field seconds per tick. The dynamics are measured in minutes; ticking at
	/// frame rate would compute the same curve sixty times over.
	/// </summary>
	private const float TickSeconds = 1f;

	public EcologyField Field { get; private set; }

	/// <summary>
	/// The traveller's condition. Owned here rather than on the player because
	/// it only exists when the ecology does, and because the flag must be able
	/// to take the whole thing away without leaving a gauge behind.
	/// </summary>
	public PlayerVitals Vitals { get; } = new();

	/// <summary>Damage one wolf bite does. Three kill.</summary>
	public const float BiteDamage = 0.34f;

	/// <summary>Seconds between bites from the same wolf.</summary>
	public const float BiteCooldownSeconds = 1.5f;

	/// <summary>Vitality taken off an animal by one blow. Four bare-handed.</summary>
	public const float StrikeDamage = 0.25f;

	/// <summary>
	/// How much of the stomach one animal fills. Rather less than all of it, so
	/// a long crossing needs more than a single lucky deer.
	/// </summary>
	public const float MealFromKill = 0.45f;

	private double _pending;

	/// <summary>
	/// Build the field over an atlas.
	///
	/// A guide is made here rather than borrowed, because the runtime's guide is
	/// created inside the moving window and not kept. This one exists only to
	/// answer global biome questions, so it is given the smallest window the
	/// constructor will take — it never samples its own local allocation.
	/// </summary>
	public void Setup(WorldAtlasDefinition atlas, int worldSeed)
	{
		var guide = ProductionTerrainGuide.CreateAtOrigin(atlas, 64, 0, 0, worldSeed);
		Field = new EcologyField(atlas.Width, atlas.Depth, guide.GlobalBiomeAt, worldSeed);
	}

	public void Advance(double delta)
	{
		if (Field == null) return;
		// Vitality, breath and hunger move at frame rate; the continent does not.
		//
		// Sprinting is passed as false because this world has no sprint: the
		// traveller walks or slow-walks. Breath is therefore carried and tested
		// but nothing yet spends it, and escaping a pack is done by leaving its
		// valley rather than by outrunning it — which is the designed counterplay
		// in any case. Wiring a sprint means changing the author's movement code
		// and is deliberately left out of this contribution.
		Vitals.Advance((float)delta, sprinting: false);

		_pending += delta;
		while (_pending >= TickSeconds)
		{
			Field.Advance(TickSeconds);
			_pending -= TickSeconds;
		}
	}

	/// <summary>
	/// A creature was killed here.
	///
	/// This is the ONLY path from a body's death to the field. Unloading a
	/// creature because the traveller walked beyond the retention radius must
	/// never come through here: that is not a death, and counting it as one
	/// would empty the continent behind their footsteps, one valley per walk.
	/// </summary>
	public void ReportKill(Vector3 where)
	{
		Field?.RemovePrey(
			Field.IndexAt(Mathf.FloorToInt(where.X), Mathf.FloorToInt(where.Z)), 1f);
	}

	/// <summary>
	/// The traveller killed a wolf here. The valley's predators lose one, and
	/// the valley remembers who did it.
	/// </summary>
	public void ReportWolfKilledByTraveller(Vector3 where)
	{
		if (Field == null) return;
		int cell = Field.IndexAt(Mathf.FloorToInt(where.X), Mathf.FloorToInt(where.Z));
		Field.RemovePredator(cell, 1f);
		Field.RaiseGrudge(cell);
	}

	/// <summary>Does the ground here hold anything against the traveller?</summary>
	public bool GrudgeAt(Vector3 where) => Field != null &&
		Field.GrudgeAt(Field.IndexAt(Mathf.FloorToInt(where.X), Mathf.FloorToInt(where.Z)));
}
