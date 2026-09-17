using Petalfell.World;

namespace Petalfell.Ecology;

/// <summary>
/// Every number the ecology runs on, with the reason it holds its value.
///
/// Two layers use this file and they are tuned against different things. The
/// field coefficients below describe whole 128-block cells and are fitted to
/// reproduce the curves of the source simulation's harness; the agent
/// parameters that arrive with the herd and pack behaviour keep the values that
/// simulation was tuned with directly. Changing one without re-running the
/// harness makes the two layers tell different stories about the same world.
/// </summary>
public static class EcologyTuning
{
	/// <summary>
	/// Cell side in blocks. 128 divides the 12,288 x 9,216 atlas exactly into
	/// 96 x 72 cells: large enough that the whole continent costs a hundred
	/// kilobytes, small enough that a cell is a valley rather than a province.
	/// </summary>
	public const int CellBlocks = 128;

	// --- Grass ------------------------------------------------------------

	/// <summary>
	/// Logistic regrowth rate, carried over from the source simulation's
	/// biomass regrowth. The logistic shape is the balance's shock absorber:
	/// fast at half load, slow near empty and slow near full.
	/// </summary>
	public const float GrassRegrowth = 0.12f;

	/// <summary>Grass eaten per prey unit per second, at full grass cover.</summary>
	public const float GrazingRate = 0.0020f;

	// --- Prey ---------------------------------------------------------------

	/// <summary>Prey growth per unit of grass eaten.</summary>
	public const float PreyBirth = 0.51f;

	/// <summary>Kills per predator per prey unit per second.</summary>
	public const float PreyPredation = 0.0016f;

	/// <summary>Prey lost per second to age and accident, predation aside.</summary>
	public const float PreyMortality = 0.00018f;

	// --- Predators ----------------------------------------------------------

	/// <summary>
	/// Predator growth per kill. Well under one: a wolf eats many deer before it
	/// raises another wolf, and that gap is what makes the cycle lag behind the
	/// prey rather than track them.
	/// </summary>
	public const float PredatorConversion = 0.20f;

	/// <summary>
	/// Predator loss per second. Deliberately slow — the source simulation found
	/// that a long-lived predator is what crosses the trough of the cycle
	/// without the species dying before the prey recover.
	/// </summary>
	public const float PredatorMortality = 0.0008f;

	// --- Space --------------------------------------------------------------

	/// <summary>
	/// Share of the gap to the neighbouring average that levels out per second.
	/// Animals walk. Without this an emptied valley could only refill by
	/// spontaneous generation, and population waves would never cross the
	/// continent. Must stay well below 1 for the stencil to remain stable.
	/// </summary>
	public const float Diffusion = 0.015f;

	// --- Rarity refuge ------------------------------------------------------

	/// <summary>
	/// Below this density a cell's prey breed faster: survivors of a crash face
	/// less competition. Ported from the source simulation, where its absence
	/// turned every deep trough into an extinction.
	/// </summary>
	public const float PreyRarityFloor = 0.6f;

	/// <summary>The same refuge for predators, at their much lower density.</summary>
	public const float PredatorRarityFloor = 0.05f;

	/// <summary>Birth multiplier applied inside the rarity refuge.</summary>
	public const float RarityBirthBoost = 2.2f;

	/// <summary>
	/// How much life a biome can carry, as a fraction of the richest ground.
	/// This is the whole reason the ecology sits on the authored geography
	/// instead of inventing a second one: herds gather where the region maps
	/// already say the land is good.
	/// </summary>
	public static float FertilityForBiome(Biome biome) => biome switch
	{
		Biome.Meadow => 1.00f,
		Biome.Plains => 0.85f,
		Biome.Forest => 0.75f,
		Biome.Sakura => 0.70f,
		Biome.Wetland => 0.50f,
		Biome.Shore => 0.30f,
		Biome.Highland => 0.25f,
		Biome.SnowyHills => 0.10f,
		_ => 0.50f,
	};
}
