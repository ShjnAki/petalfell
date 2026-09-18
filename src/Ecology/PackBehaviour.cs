using System.Collections.Generic;
using Godot;

namespace Petalfell.Ecology;

/// <summary>Something worth chasing, as the hunt sees it.</summary>
public readonly record struct HuntTarget(Vector3 Position, float Distance);

/// <summary>What a wolf has decided to do this moment.</summary>
public enum PackIntent
{
	/// <summary>Nothing in reach. Range the territory, drift home if too far out.</summary>
	Wander,

	/// <summary>Prey seen but far. Close the gap at a trot, spending nothing.</summary>
	Stalk,

	/// <summary>Close enough to commit. This is the only state that costs breath.</summary>
	Sprint,

	/// <summary>Out of breath. Break off, whatever is standing in front of you.</summary>
	Recover,
}

/// <summary>The decision and the heading that carries it out.</summary>
public readonly record struct PackDecision(PackIntent Intent, Vector3 Heading, float SpeedMultiplier);

/// <summary>
/// Hunting decisions, ported from the source project's carnivore.
///
/// The whole hunt turns on one lesson that simulation learned the hard way: a
/// wolf that sprints the moment it sees a deer arrives with nothing left. The
/// approach is a trot and the sprint only begins inside <see cref="SprintRange"/>,
/// which is far enough to cover the closing run and near enough that the lungs
/// outlast it. Widen that range and every hunt ends in exhaustion; narrow it
/// below the distance at which prey bolt and no hunt ever starts.
///
/// Stateless, like the herd steering. The stamina and the chosen target live on
/// the creature; nothing is remembered here.
/// </summary>
public static class PackBehaviour
{
	/// <summary>How far a wolf notices prey at all.</summary>
	public const float PerceptionRadius = 170f;

	/// <summary>Inside this, commit and spend the breath. Outside, walk.</summary>
	public const float SprintRange = 45f;

	/// <summary>Close enough to take it down.</summary>
	public const float KillDistance = 2f;

	/// <summary>Beyond this from its den, a wolf with nothing to chase turns back.</summary>
	public const float HomeRange = 400f;

	/// <summary>Breath below this ends the chase, whatever is in front of you.</summary>
	public const float RecoverStamina = 0.15f;

	/// <summary>
	/// Breath a winded wolf must get back before it will commit again.
	///
	/// Without this gap it re-engages the instant it crosses the lower mark,
	/// drains straight back to it, and spends the whole hunt hovering at the
	/// threshold in sprint-long twitches — visible in the census as a stamina
	/// reading pinned at exactly 0.15. A chase is a decision, not a flicker.
	/// </summary>
	public const float RecoveredStamina = 0.6f;

	/// <summary>Fraction of maximum breath spent per second of sprinting.</summary>
	public const float StaminaDrainPerSecond = 1f / 25f;

	/// <summary>Fraction recovered per second when not sprinting.</summary>
	public const float StaminaRegenPerSecond = 1f / 15f;

	private const float StalkSpeed = 1.15f;
	private const float SprintSpeed = 2.6f;
	private const float RecoverSpeed = 0.55f;

	/// <param name="wasRecovering">
	/// Whether this wolf broke off last frame. Carried by the caller rather than
	/// stored here, so the decision stays a function of its inputs.
	/// </param>
	public static PackDecision Decide(Vector3 position, Vector3 heading, Vector3 den,
		float stamina, IReadOnlyList<HuntTarget> prey, bool wasRecovering = false)
	{
		var nearest = Nearest(prey);
		bool winded = wasRecovering ? stamina < RecoveredStamina : stamina <= RecoverStamina;

		if (nearest.HasValue && !winded)
		{
			var target = nearest.Value;
			var toPrey = Flat(target.Position - position);
			if (toPrey.LengthSquared() > 0.0001f)
			{
				var aim = toPrey.Normalized();
				if (target.Distance <= SprintRange)
					return new PackDecision(PackIntent.Sprint, aim, SprintSpeed);
				if (target.Distance <= PerceptionRadius)
					return new PackDecision(PackIntent.Stalk, aim, StalkSpeed);
			}
		}

		var wandering = Homeward(position, heading, den);
		return winded
			? new PackDecision(PackIntent.Recover, wandering, RecoverSpeed)
			: new PackDecision(PackIntent.Wander, wandering, 1f);
	}

	/// <summary>
	/// The recall to the den. Inside the range it does nothing; past the edge it
	/// grows until, at twice the range out, the wolf is simply walking home.
	///
	/// This is also what keeps a territory a territory — and, once the traveller
	/// can be hunted, what lets them escape a pack by leaving its valley.
	/// </summary>
	private static Vector3 Homeward(Vector3 position, Vector3 heading, Vector3 den)
	{
		var toDen = Flat(den - position);
		float distance = toDen.Length();
		if (distance <= HomeRange || distance < 0.001f) return heading;
		float pull = Mathf.Clamp((distance - HomeRange) / HomeRange, 0f, 1f);
		var blended = heading.Lerp(toDen.Normalized(), pull);
		return blended.LengthSquared() < 0.0001f ? heading : blended.Normalized();
	}

	private static HuntTarget? Nearest(IReadOnlyList<HuntTarget> prey)
	{
		HuntTarget? best = null;
		foreach (var candidate in prey)
			if (candidate.Distance <= PerceptionRadius &&
				(!best.HasValue || candidate.Distance < best.Value.Distance))
				best = candidate;
		return best;
	}

	private static Vector3 Flat(Vector3 v) => new(v.X, 0f, v.Z);
}
