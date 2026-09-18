using System.Collections.Generic;
using Godot;

namespace Petalfell.Ecology;

/// <summary>One herd member as the steering sees it.</summary>
public readonly record struct HerdNeighbour(Vector3 Position, Vector3 Heading);

/// <summary>
/// Classic three-rule flocking, ported from the source project's boids.
///
/// Stateless on purpose. A herd is not an object that exists somewhere — it is
/// what a field of individually-steering animals looks like from outside, and
/// modelling it as a thing would mean deciding who joins and who leaves.
/// </summary>
public static class HerdBehaviour
{
	/// <summary>Beyond this, a creature is scenery rather than company.</summary>
	public const float HerdRadius = 8f;

	/// <summary>Personal space. Inside it, getting away beats staying together.</summary>
	private const float PersonalSpace = 2.2f;

	private const float SeparationWeight = 1.2f;
	private const float AlignmentWeight = 0.4f;
	private const float CohesionWeight = 0.35f;

	/// <summary>
	/// How much of the creature's own intent survives. Steering that overrode
	/// the wander entirely would produce a single organism sliding across a
	/// meadow; the herd should bend a walk, not replace it.
	/// </summary>
	private const float OwnHeadingWeight = 1.0f;

	public static Vector3 Steer(Vector3 position, Vector3 heading,
		IReadOnlyList<HerdNeighbour> neighbours)
	{
		Vector3 separation = Vector3.Zero, alignment = Vector3.Zero, centre = Vector3.Zero;
		int counted = 0;

		foreach (var neighbour in neighbours)
		{
			var offset = new Vector3(neighbour.Position.X - position.X, 0f,
				neighbour.Position.Z - position.Z);
			float distance = offset.Length();
			if (distance > HerdRadius || distance < 0.001f) continue;
			counted++;
			centre += new Vector3(neighbour.Position.X, 0f, neighbour.Position.Z);
			alignment += neighbour.Heading;
			// Crowding rises sharply as the gap closes, so a herd packs loosely
			// and then refuses to pack further.
			if (distance < PersonalSpace)
				separation -= offset.Normalized() * ((PersonalSpace - distance) / PersonalSpace);
		}

		if (counted == 0) return heading;

		var flat = new Vector3(position.X, 0f, position.Z);
		var toCentre = centre / counted - flat;
		alignment.Y = 0f;

		var steer = heading * OwnHeadingWeight
			+ separation * SeparationWeight
			+ Normalised(alignment) * AlignmentWeight
			+ Normalised(toCentre) * CohesionWeight;
		return steer.LengthSquared() < 0.0001f ? heading : steer.Normalized();
	}

	private static Vector3 Normalised(Vector3 v) =>
		v.LengthSquared() < 0.0001f ? Vector3.Zero : v.Normalized();
}
