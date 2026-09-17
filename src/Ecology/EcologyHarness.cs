using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Petalfell.Ecology;

/// <summary>One reading of the whole continent.</summary>
public readonly record struct EcologySample(float Hours, float Grass, float Prey, float Predator);

/// <summary>An accelerated run and the verdict it earned.</summary>
public sealed record EcologyRun(string Verdict, IReadOnlyList<EcologySample> Samples)
{
	/// <summary>
	/// Rows for plotting against the source simulation's curves. Invariant
	/// culture, because a decimal comma would silently corrupt the comparison.
	/// </summary>
	public string ToCsv()
	{
		var text = new StringBuilder("hours,grass,prey,predator\n");
		foreach (var sample in Samples)
			text.Append(string.Format(CultureInfo.InvariantCulture,
				"{0:0.000},{1:0.00},{2:0.00},{3:0.00}\n",
				sample.Hours, sample.Grass, sample.Prey, sample.Predator));
		return text.ToString();
	}
}

/// <summary>
/// Runs the field far faster than play, so that an equilibrium meant to be felt
/// over hours can be judged in seconds. This is the port of the source
/// simulation's headless runner, and it is the only thing that can honestly
/// answer whether the coefficients hold.
/// </summary>
public static class EcologyHarness
{
	/// <summary>Below this share of the starting population, the run is an extinction.</summary>
	private const float ExtinctionShare = 0.02f;

	/// <summary>Above this multiple of the starting population, the run is an explosion.</summary>
	private const float ExplosionMultiple = 25f;

	/// <summary>
	/// A species still at its lowest point when the run ends, and below this
	/// share of where it started, is on its way out whether or not it has
	/// crossed the extinction threshold yet.
	/// </summary>
	private const float CollapseShare = 0.4f;

	public static EcologyRun Run(EcologyField field, float hours, float sampleMinutes)
	{
		if (field == null) throw new ArgumentNullException(nameof(field));
		if (hours <= 0f) throw new ArgumentOutOfRangeException(nameof(hours));
		if (sampleMinutes <= 0f) throw new ArgumentOutOfRangeException(nameof(sampleMinutes));

		var start = field.Totals();
		var samples = new List<EcologySample> { Read(field, 0f) };
		string verdict = "stable";

		int blocks = (int)MathF.Round(hours * 60f / sampleMinutes);
		int secondsPerBlock = (int)MathF.Round(sampleMinutes * 60f);
		for (int block = 1; block <= blocks; block++)
		{
			for (int second = 0; second < secondsPerBlock; second++) field.Advance(1f);
			var sample = Read(field, block * sampleMinutes / 60f);
			samples.Add(sample);

			if (verdict != "stable") continue;
			if (sample.Prey < start.Prey * ExtinctionShare ||
				sample.Predator < start.Predator * ExtinctionShare) verdict = "extinction";
			else if (sample.Prey > start.Prey * ExplosionMultiple ||
				sample.Predator > start.Predator * ExplosionMultiple) verdict = "explosion";
		}

		// A threshold alone calls a slide "stable" right up until the moment it
		// isn't. Coexistence means the curve turned round at some point, so a
		// species that ends the run at its own lowest value has not coexisted —
		// it has simply not finished dying yet.
		if (verdict == "stable" &&
			(StillFalling(samples, start.Prey, s => s.Prey) ||
			 StillFalling(samples, start.Predator, s => s.Predator)))
			verdict = "collapse";

		return new EcologyRun(verdict, samples);
	}

	private static bool StillFalling(IReadOnlyList<EcologySample> samples, float start,
		Func<EcologySample, float> read)
	{
		float last = read(samples[^1]);
		if (last >= start * CollapseShare) return false;
		foreach (var sample in samples)
			if (read(sample) < last) return false;
		return true;
	}

	private static EcologySample Read(EcologyField field, float hours)
	{
		var totals = field.Totals();
		return new EcologySample(hours, totals.Grass, totals.Prey, totals.Predator);
	}
}
