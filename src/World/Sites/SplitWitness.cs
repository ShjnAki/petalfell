using System;
using Petalfell.Core;

namespace Petalfell.World.Sites;

/// <summary>Original surveyed outcrop. These explicit cross sections own its silhouette.</summary>
public static class SplitWitness
{
	public const string BuilderId = "split-witness-v1";
	public const string SiteId = "split-witness";

	public static ReferenceSiteStatistics Build(AtlasSectorWindow window,
		ReferenceSiteDefinition site, int verticalOffset)
	{
		var w = new AuthoredSiteWriter(window, site, verticalOffset);
		int[] westRelief = { 2,0,1,0,2,1,0,1,3,1,0,2,1,0,2 };
		int[] eastRelief = { 1,0,2,1,0,1,3,0,1,0,2,1,0,2 };
		w.Terrain(); // Preserve every atlas column outside the occupied stone roots.
		w.Structure("west-blade", () =>
		{
			w.Foundation(Palette.STONE);
			foreach (var p in w.Plan.GetStructure("west-blade").ProjectionCells)
			{
				int crest = p.Z switch { < -10 => 169, < -4 => 175, < 2 => 178, < 7 => 171, _ => 161 };
				int shoulder = Math.Abs(p.X+10);
				int top = crest - (shoulder > 5 ? 9 : shoulder > 3 ? 4 : 0) - westRelief[p.X+18];
				for (int y = 145; y <= top; y++) w.Put(p.X,y,p.Z,Stratum(y));
				if (p.Z < -3 && p.X <= -9) w.Put(p.X,top,p.Z,Palette.SNOW);
			}
			// Authored erosion cuts expose ledges and short cracks within the blade.
			w.Fill(-17,-16,153,155,-8,-6,Palette.AIR);
			w.Fill(-18,-17,160,165,-2,0,Palette.AIR);
			w.Fill(-6,-5,152,154,-8,-5,Palette.AIR);
			w.Fill(-5,-4,164,168,-3,-1,Palette.AIR);
			w.Fill(-13,-11,146,148,9,10,Palette.AIR);
		});
		w.Structure("east-blade", () =>
		{
			w.Foundation(Palette.STONE);
			foreach (var p in w.Plan.GetStructure("east-blade").ProjectionCells)
			{
				int crest = p.Z switch { < -6 => 164, < 0 => 171, < 6 => 172, < 11 => 163, _ => 154 };
				int shoulder = Math.Abs(p.X-9);
				int top = crest - (shoulder > 5 ? 8 : shoulder > 3 ? 3 : 0) - eastRelief[p.X-3];
				for (int y = 145; y <= top; y++) w.Put(p.X,y,p.Z,Stratum(y));
				if (p.Z < 0 && p.X < 11) w.Put(p.X,top,p.Z,Palette.SNOW);
			}
			w.Fill(3,4,153,155,1,3,Palette.AIR);
			w.Fill(15,16,159,162,0,2,Palette.AIR);
			w.Fill(13,14,148,150,8,10,Palette.AIR);
			w.Fill(6,8,147,148,12,13,Palette.AIR);
		});
		w.Structure("fallen-flake", () =>
		{
			w.Foundation(Palette.STONE);
			foreach (var p in w.Plan.GetStructure("fallen-flake").ProjectionCells)
			{
				int top = 146 - (p.Z-24)/3 - (p.X < 8 ? 2 : 0);
				for (int y = 135; y <= top; y++)
					w.Put(p.X,y,p.Z,y >= top-1 ? Palette.STONE_PALE : Palette.STONE);
			}
		});
		w.Structure("wind-shelter", () =>
		{
			w.Foundation(Palette.STONE);
			foreach (var p in w.Plan.GetStructure("wind-shelter").ProjectionCells)
			{
				int top = p.X < -23 ? 138 : p.X < -20 ? 136 : 134;
				for (int y = 134; y <= top; y++) w.Put(p.X,y,p.Z,Palette.STONE_PALE);
			}
		});
		w.Structure("three-stone-waymark", () =>
		{
			w.Foundation(Palette.STONE);
			w.Fill(-11,-8,133,133,23,26,Palette.STONE_PALE);
			w.Fill(-10,-9,134,135,24,25,Palette.STONE_WARM);
			w.Put(-10,136,24,Palette.STONE_PALE);
		});
		return w.Statistics;
	}

	private static byte Stratum(int y) => y switch
	{
		< 149 => Palette.STONE,
		< 152 => Palette.SCREE,
		< 160 => Palette.STONE_PALE,
		< 162 => Palette.STONE,
		< 169 => Palette.STONE_PALE,
		< 171 => Palette.SCREE,
		_ => Palette.STONE_PALE,
	};
}
