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
}
