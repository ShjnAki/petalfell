using Petalfell.Core;

namespace Petalfell.World.Sites;

/// <summary>The original tidekeeper site: every surviving course is authored here and in its plan.</summary>
public static class TidekeepersLanding
{
	public const string BuilderId = "tidekeepers-landing-v1";
	public const string SiteId = "tidekeepers-landing";

	public static ReferenceSiteStatistics Build(AtlasSectorWindow window,
		ReferenceSiteDefinition site, int verticalOffset)
	{
		var w = new AuthoredSiteWriter(window, site, verticalOffset);
		w.Terrain();
		w.Structure("gauge-wall", () =>
		{
			w.Foundation(Palette.STONE);
			w.Fill(14,17,30,31,-17,15,Palette.STONE);
			w.Fill(15,16,32,38,-16,11,Palette.STONE_PALE);
			w.Fill(14,17,32,42,-17,-12,Palette.STONE_PALE);
			w.Fill(15,17,39,40,5,11,Palette.STONE_WARM);
			w.Fill(14,17,32,32,-17,15,Palette.STONE_WARM);
			w.Fill(15,16,37,37,-11,11,Palette.STONE_WARM);
			w.Fill(15,16,36,38,-3,2,Palette.AIR);
			w.Fill(15,16,34,35,-1,1,Palette.AIR);
			w.Fill(14,15,41,42,-13,-12,Palette.AIR);
			w.Fill(14,14,31,35,-17,-15,Palette.MOSS_STONE);
			w.Fill(15,15,38,38,7,10,Palette.MOSS_STONE);
			// Pale ticks survive on the route-facing edge of the tall gauge.
			for (int y = 33; y <= 39; y += 2)
				w.Fill(14,14,y,y,-16,-14,Palette.STONE_WARM);
		});
		w.Structure("dry-catchment-trough", () =>
		{
			w.Foundation(Palette.STONE);
			w.Fill(-20,-19,32,34,-24,-14,Palette.STONE_WARM);
			w.Fill(-18,-12,32,34,-24,-23,Palette.STONE_WARM);
			w.Fill(-18,-12,32,34,-15,-14,Palette.STONE_WARM);
			w.Fill(-13,-12,32,34,-22,-16,Palette.STONE_WARM);
			w.Fill(-20,-19,35,35,-23,-17,Palette.STONE_PALE);
			w.Fill(-18,-13,35,35,-24,-23,Palette.STONE_PALE);
			w.Fill(-13,-12,34,34,-20,-18,Palette.AIR);
			w.Fill(-18,-16,31,31,-22,-20,Palette.MOSS_STONE);
		});
		w.Structure("hauling-stone", () =>
		{
			w.Foundation(Palette.STONE_WARM);
			w.Fill(-6,-4,32,32,-19,-17,Palette.STONE_PALE);
			w.Fill(-5,-5,33,35,-18,-18,Palette.BEAM);
			w.Fill(-7,-3,34,34,-18,-18,Palette.PLANK_PALE);
			w.Put(-6,31,-20,Palette.MOSS_STONE);
		});
		w.Structure("upper-moorings", () =>
		{
			w.Foundation(Palette.STONE_WARM);
			w.Fill(-13,-12,30,31,6,7,Palette.STONE_PALE);
			w.Fill(10,11,30,30,6,7,Palette.STONE_PALE);
			w.Put(10,31,6,Palette.MOSS_STONE);
		});
		w.Structure("lower-moorings", () =>
		{
			w.Foundation(Palette.STONE);
			w.Fill(-7,-6,26,27,33,34,Palette.MOSS_STONE);
			w.Fill(6,7,26,26,33,34,Palette.STONE_WARM);
			w.Put(6,27,33,Palette.STONE_PALE);
		});
		w.Structure("wall-fall", () =>
		{
			w.Foundation(Palette.STONE);
			w.Fill(21,23,28,29,5,7,Palette.STONE_PALE);
			w.Fill(21,22,30,30,5,6,Palette.STONE_WARM);
			w.Fill(24,26,28,28,9,10,Palette.STONE_WARM);
			w.Fill(20,21,28,29,12,14,Palette.STONE_PALE);
			w.Fill(26,28,28,28,15,16,Palette.MOSS_STONE);
		});
		return w.Statistics;
	}
}
