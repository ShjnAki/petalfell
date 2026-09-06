using System;
using System.Collections.Generic;
using System.Linq;
using Petalfell.Core;

namespace Petalfell.World.Sites;

/// <summary>
/// A bounded range writer for explicit site-owned course schedules. There are
/// deliberately no column, arch, ruin, damage or layout generators here.
/// Ground plans own X/Z, individual courses own Y, material and every break.
/// </summary>
public static class MeasuredReferenceSite
{
	public static ReferenceSiteStatistics Build(AtlasSectorWindow window,
		ReferenceSiteDefinition site, int offset)
	{
		if (site.IsOriginalDesign || site.RuntimePlanScale != 1)
			throw new InvalidOperationException("Measured course schedules require a metre-scale reference plan");
		var writer = new AuthoredSiteWriter(window, site, offset);
		// Validate the entire schedule before mutating even one terrain column.
		foreach (var mass in writer.Plan.Structures.Where(s => s.Kind != "stair"))
		{
			if (mass.GroundAt.Count > 0 && (mass.GroundAt.Count != 2 ||
			    !mass.ProjectionCells.Contains(new ReferenceGroundPlanCell(mass.GroundAt[0],mass.GroundAt[1])) ||
			    mass.Kind != "measured-tree"))
				throw new InvalidOperationException($"{site.SiteId}/{mass.Id}: invalid source tree anchor");
			int datum = mass.GroundAt.Count == 2 ? writer.PlannedGround(mass.GroundAt[0],mass.GroundAt[1]) : 0;
			var occupied = new HashSet<ReferenceGroundPlanCell>();
			if (mass.Courses.Count == 0)
				throw new InvalidOperationException($"{site.SiteId}/{mass.Id}: missing authored volume schedule");
			foreach (var b in mass.Courses)
			{
				if (b.Count != 7 || b[0] > b[1] || b[2] > b[3] || b[4] > b[5] ||
				    b[2]+datum+offset < 0 || b[3]+datum+offset >= window.Grid.Height ||
				    b[6] < 0 || b[6] > 50)
					throw new InvalidOperationException($"{site.SiteId}/{mass.Id}: invalid course bounds/material");
				for (int z=b[4]; z<=b[5]; z++)
				for (int x=b[0]; x<=b[1]; x++)
				{
					var p = new ReferenceGroundPlanCell(x,z);
					if (!mass.ProjectionCells.Contains(p))
						throw new InvalidOperationException($"{site.SiteId}/{mass.Id}: course leaves measured projection at {x},{z}");
					if (Palette.IsSolid((byte)b[6])) occupied.Add(p);
				}
			}
			if (!occupied.SetEquals(mass.ProjectionCells))
				throw new InvalidOperationException($"{site.SiteId}/{mass.Id}: course schedule differs from top plan");
		}
		writer.Terrain();
		foreach (var mass in writer.Plan.Structures.Where(s => s.Kind != "stair"))
			writer.Structure(mass.Id, () =>
			{
				int datum = mass.GroundAt.Count == 2 ? writer.Ground(mass.GroundAt[0],mass.GroundAt[1]) : 0;
				foreach (var b in mass.Courses)
					writer.Fill(b[0],b[1],b[2]+datum,b[3]+datum,b[4],b[5],(byte)b[6]);
			});
		return writer.Statistics;
	}
}
