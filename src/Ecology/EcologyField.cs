using System;
using Petalfell.Core;
using Petalfell.World;

namespace Petalfell.Ecology;

/// <summary>Continental totals, for the harness and for diagnostics.</summary>
public readonly record struct EcologyTotals(float Grass, float Prey, float Predator);

/// <summary>
/// The persistent population field.
///
/// A predator-prey cycle takes tens of minutes. The traveller's creatures are
/// discarded within a few hundred blocks, so no cycle could ever be witnessed
/// if the population lived only in the bodies on screen. It lives here instead:
/// one coarse cell per 128 blocks, carrying grass, prey and predators across
/// the whole continent whether anyone is looking or not. The bodies near the
/// traveller are a sample of this, never the other way round.
///
/// Deliberately free of Godot: the equilibrium has to be provable without
/// starting an engine.
/// </summary>
public sealed class EcologyField
{
	public int Columns { get; }
	public int Rows { get; }
	public int CellCount => Columns * Rows;
	public float ElapsedSeconds { get; private set; }

	private readonly float[] _grass, _prey, _predator, _fertility;
	private readonly float[] _preyScratch, _predatorScratch;

	/// <summary>
	/// Build the field over an atlas. Fertility is sampled once per cell from
	/// the authored biome at the cell's centre; the starting densities are drawn
	/// deterministically from the world seed and never re-rolled, so two runs of
	/// the same world begin the same way.
	/// </summary>
	public EcologyField(int atlasWidth, int atlasDepth, Func<int, int, Biome> biomeAt, int seed)
	{
		if (biomeAt == null) throw new ArgumentNullException(nameof(biomeAt));
		Columns = Math.Max(1, atlasWidth / EcologyTuning.CellBlocks);
		Rows = Math.Max(1, atlasDepth / EcologyTuning.CellBlocks);

		int count = Columns * Rows;
		_grass = new float[count];
		_prey = new float[count];
		_predator = new float[count];
		_fertility = new float[count];
		_preyScratch = new float[count];
		_predatorScratch = new float[count];

		var rng = new Rng(seed ^ 0xEC0107);
		for (int row = 0; row < Rows; row++)
		for (int col = 0; col < Columns; col++)
		{
			int i = row * Columns + col;
			int centreX = col * EcologyTuning.CellBlocks + EcologyTuning.CellBlocks / 2;
			int centreZ = row * EcologyTuning.CellBlocks + EcologyTuning.CellBlocks / 2;
			float fertility = EcologyTuning.FertilityForBiome(biomeAt(centreX, centreZ));
			_fertility[i] = fertility;
			_grass[i] = fertility * rng.Range(0.35f, 0.75f);
			_prey[i] = fertility * rng.Range(1.5f, 3.5f);
			_predator[i] = fertility * rng.Range(0.05f, 0.18f);
		}
	}

	/// <summary>
	/// The cell holding a global atlas address. Clamped rather than checked:
	/// callers hand in live player positions, and an address a block outside the
	/// atlas is a rounding artefact, not a programming error.
	/// </summary>
	public int IndexAt(int globalX, int globalZ)
	{
		// Integer division truncates towards zero, so a negative address would
		// land in column 0 by accident rather than by intent. Say it outright.
		int col = globalX < 0 ? 0 : Math.Min(globalX / EcologyTuning.CellBlocks, Columns - 1);
		int row = globalZ < 0 ? 0 : Math.Min(globalZ / EcologyTuning.CellBlocks, Rows - 1);
		return row * Columns + col;
	}

	public float GrassAt(int index) => _grass[index];
	public float PreyAt(int index) => _prey[index];
	public float PredatorAt(int index) => _predator[index];
	public float FertilityAt(int index) => _fertility[index];

	public EcologyTotals Totals()
	{
		float grass = 0f, prey = 0f, predator = 0f;
		for (int i = 0; i < _grass.Length; i++)
		{
			grass += _grass[i];
			prey += _prey[i];
			predator += _predator[i];
		}
		return new EcologyTotals(grass, prey, predator);
	}
}
