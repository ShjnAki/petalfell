using System;

namespace Petalfell.Ecology;

/// <summary>
/// The traveller's vitality, breath and hunger.
///
/// Three numbers, no more. The source project's player carried the same
/// vitality and breath; hunger is the one addition, and it exists to make the
/// meat the hunt produces worth carrying. Thirst and warmth were considered
/// and left out: a fourth gauge turns a walk into an inventory screen.
///
/// Free of Godot so the rules can be proved without an engine, and because
/// they are rules rather than presentation.
/// </summary>
public sealed class PlayerVitals
{
	/// <summary>
	/// THE EQUATION OF THE ESCAPE, carried over from the source project.
	///
	/// A wolf sprints at 12 and the traveller at 11, so the wolf gains a metre
	/// a second. From its 45-unit engagement range it would need some forty
	/// seconds to reach you — but its breath lasts twenty-five and yours lasts
	/// thirty. IT GIVES UP FIRST. You do not escape by speed, you escape by
	/// breath. Caught at ten paces is another matter: three bites kill.
	///
	/// Changing either number without the other silently decides whether being
	/// hunted is survivable.
	/// </summary>
	public const float BreathSeconds = 30f;

	private const float BreathRegenSeconds = 12f;

	/// <summary>Vitality regained per second, once the bleeding has stopped.</summary>
	private const float VitalityRegenPerSecond = 0.02f;

	/// <summary>Seconds without a wound before healing starts. Never mid-fight.</summary>
	private const float HealingDelaySeconds = 8f;

	/// <summary>
	/// Hunger spent per second. Empty in about forty minutes of play — slow
	/// enough that a walk is not an errand, quick enough that a long journey
	/// has to be provisioned.
	/// </summary>
	private const float HungerPerSecond = 1f / 2400f;

	/// <summary>Below this, wounds stop closing. Starving is not resting.</summary>
	public const float StarvingBelow = 0.15f;

	/// <summary>Vitality lost per second on an empty stomach.</summary>
	private const float StarvationDamagePerSecond = 0.01f;

	public float Vitality { get; private set; } = 1f;
	public float Breath { get; private set; } = 1f;
	public float Hunger { get; private set; } = 1f;
	public bool Dead => Vitality <= 0f;

	private float _sinceWounded = HealingDelaySeconds;

	/// <summary>Can the traveller still put on a burst of speed?</summary>
	public bool CanSprint => Breath > 0f && !Dead;

	public void Advance(float dtSeconds, bool sprinting)
	{
		if (dtSeconds <= 0f || Dead) return;

		Breath = Math.Clamp(Breath + dtSeconds *
			(sprinting ? -1f / BreathSeconds : 1f / BreathRegenSeconds), 0f, 1f);

		Hunger = Math.Max(0f, Hunger - HungerPerSecond * dtSeconds);

		_sinceWounded += dtSeconds;
		if (Hunger <= 0f) Wound(StarvationDamagePerSecond * dtSeconds, healingInterrupted: false);
		else if (_sinceWounded >= HealingDelaySeconds && Hunger > StarvingBelow)
			Vitality = Math.Min(1f, Vitality + VitalityRegenPerSecond * dtSeconds);
	}

	/// <summary>
	/// Take a wound. Starvation passes <c>healingInterrupted: false</c>: going
	/// hungry should not also reset the clock on every scar you are carrying.
	/// </summary>
	public void Wound(float amount, bool healingInterrupted = true)
	{
		if (amount <= 0f || Dead) return;
		Vitality = Math.Max(0f, Vitality - amount);
		if (healingInterrupted) _sinceWounded = 0f;
	}

	/// <summary>Eat. Meat fills more of the stomach than a fish does.</summary>
	public void Eat(float nourishment) =>
		Hunger = Math.Clamp(Hunger + nourishment, 0f, 1f);

	/// <summary>
	/// Wake at the last lit fire: alive, badly weakened, and hungry. The journey
	/// is kept; what was being carried is not, and that is handled by the caller
	/// because an inventory is not a vital sign.
	/// </summary>
	public void ReviveAtCampfire()
	{
		Vitality = 0.35f;
		Breath = 0.5f;
		Hunger = Math.Max(Hunger, 0.25f);
		_sinceWounded = 0f;
	}
}
