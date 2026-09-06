using System;
using System.Collections.Generic;
using System.Linq;
using Petalfell.Core;

namespace Petalfell.World.Sites;

/// <summary>Checks authored volumes, including air cuts, without designing geometry.</summary>
public static class MeasuredCourseSchedule
{
	public static IEnumerable<string> Audit(ReferenceGroundPlanStructure mass,
		IReadOnlySet<ReferenceGroundPlanCell> projection)
	{
		var errors = new List<string>();
		if (mass.Kind is not ("measured-masonry" or "measured-tree")) return errors;
		string label = $"{mass.Id}: ";
		if (mass.Courses == null || mass.Courses.Count == 0)
			return new[] { label + "missing measured courses" };
		if (mass.GroundAt == null || (mass.GroundAt.Count != 0 &&
		    (mass.GroundAt.Count != 2 || mass.Kind != "measured-tree" ||
		     !projection.Contains(new(mass.GroundAt[0], mass.GroundAt[1])))))
			errors.Add(label + "invalid grounded tree anchor");
		long visits = 0;
		var occupied = new HashSet<(int X, int Y, int Z)>();
		foreach (var b in mass.Courses)
		{
			if (b == null || b.Count != 7 || b[0] > b[1] || b[2] > b[3] || b[4] > b[5] ||
			    b[2] < -256 || b[3] > 255 || !(b[6] is >= 0 and <= 37 or 40 or 41 or 42 or 50))
			{ errors.Add(label + "invalid inclusive course bounds or palette material"); continue; }
			visits += ((long)b[1]-b[0]+1)*((long)b[3]-b[2]+1)*((long)b[5]-b[4]+1);
			if (visits > 500_000)
			{ errors.Add(label + "exceeds the bounded 500000 voxel course budget"); break; }
			bool solid = Palette.IsSolid((byte)b[6]);
			for (int z=b[4]; z<=b[5]; z++)
			for (int x=b[0]; x<=b[1]; x++)
			{
				if (!projection.Contains(new(x,z)))
				{ errors.Add(label + $"course leaves the top plan at {x},{z}"); continue; }
				for (int y=b[2]; y<=b[3]; y++)
					if (solid) occupied.Add((x,y,z)); else occupied.Remove((x,y,z));
			}
		}
		var finalProjection = occupied.Select(p => new ReferenceGroundPlanCell(p.X,p.Z)).ToHashSet();
		if (!finalProjection.SetEquals(projection))
			errors.Add(label + "final occupied volume after air cuts differs from the top plan");
		return errors.Distinct();
	}
}
