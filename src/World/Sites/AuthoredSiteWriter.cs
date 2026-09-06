using System;
using System.Collections.Generic;
using System.Linq;
using Petalfell.Core;

namespace Petalfell.World.Sites;

/// <summary>Bounded voxel writing only; all composition belongs to the site's plan and blueprint.</summary>
internal sealed class AuthoredSiteWriter
{
	private readonly AtlasSectorWindow _window;
	private readonly ReferenceSiteDefinition _site;
	private readonly int _offset;
	private HashSet<ReferenceGroundPlanCell> _projection;
	private HashSet<ReferenceGroundPlanCell> _touched;
	private ReferenceGroundPlanStructure _structure;
	public ReferenceSiteGroundPlan Plan { get; }
	public int SurfaceCells { get; private set; }
	public int Voxels { get; private set; }
	public ReferenceSiteStatistics Statistics => new(SurfaceCells, Voxels);

	public AuthoredSiteWriter(AtlasSectorWindow window, ReferenceSiteDefinition site, int offset)
	{
		_window = window;
		_site = site;
		_offset = offset;
		Plan = ReferenceSiteGroundPlan.Load(site);
		if (site.RuntimePlanScale != 1 ||
		    Plan.CoordinateContract.RuntimeMirrorX != false)
			throw new InvalidOperationException("Authored voxel ranges require an explicit unmirrored metre plan");
	}

	public void Terrain()
	{
		foreach (var patch in Plan.Terrain)
		{
			if (patch.WriteMode == "preserve-atlas") continue;
			if (patch.WriteMode == "author-water")
			{
				foreach (var cell in patch.EffectiveCells)
					Water(cell.X, cell.Z, patch.SurfaceY!.Value, patch.BedY!.Value);
				continue;
			}
			if (patch.WriteMode != "author-surface")
				throw new InvalidOperationException($"Unsupported authored terrain mode '{patch.WriteMode}'");
			foreach (var cell in patch.EffectiveCells)
				Surface(cell.X, cell.Z, patch.SurfaceY!.Value, Material(patch.Material));
		}
		foreach (var stair in Plan.Structures.Where(s => s.Kind == "stair"))
			foreach (var tread in stair.Treads)
				foreach (var cell in Cells(tread.Footprint))
					Surface(cell.X, cell.Z, tread.TopY!.Value, Palette.PAVING);
		foreach (var patch in Plan.SurfacePatches)
			foreach (var cell in patch.EffectiveCells)
				// Surface wear changes material, never a stair's measured height.
				Surface(cell.X, cell.Z, Ground(cell.X,cell.Z),
					Material(patch.Material));
	}

	public void Structure(string id, Action draw)
	{
		_structure = Plan.GetStructure(id);
		_projection = _structure.ProjectionCells.ToHashSet();
		_touched = new HashSet<ReferenceGroundPlanCell>();
		draw();
		if (!_projection.SetEquals(_touched))
			throw new InvalidOperationException($"'{_site.SiteId}/{id}' did not realize its exact plan projection");
		_structure = null;
		_projection = null;
		_touched = null;
	}

	public void Foundation(byte material)
	{
		foreach (var p in _projection)
		{
			int baseY = _structure.BaseY!.Value;
			for (int y = Math.Max(-_offset, Math.Min(Ground(p.X, p.Z)-2, baseY)); y <= baseY; y++)
				Put(p.X, y, p.Z, material);
		}
	}

	public void Fill(int x0, int x1, int y0, int y1, int z0, int z1, byte material)
	{
		for (int y = y0; y <= y1; y++)
		for (int z = z0; z <= z1; z++)
		for (int x = x0; x <= x1; x++) Put(x, y, z, material);
	}

	public int Ground(int x, int z)
	{
		(int lx, int lz) = Local(x, z);
		return _window.Grid.Top[lz*_window.Grid.Size+lx]-_offset;
	}

	public int PlannedGround(int x, int z)
	{
		int top = Ground(x,z);
		var cell = new ReferenceGroundPlanCell(x,z);
		foreach (var patch in Plan.Terrain)
			if (patch.WriteMode != "preserve-atlas" && patch.EffectiveCells.Contains(cell))
				top = patch.WriteMode == "author-water" ? patch.BedY!.Value : patch.SurfaceY!.Value;
		foreach (var stair in Plan.Structures.Where(s => s.Kind == "stair"))
			foreach (var tread in stair.Treads)
				if (x >= tread.Footprint[0] && x <= tread.Footprint[2] &&
				    z >= tread.Footprint[1] && z <= tread.Footprint[3]) top = tread.TopY!.Value;
		return top;
	}

	public void Put(int x, int y, int z, byte material)
	{
		var cell = new ReferenceGroundPlanCell(x, z);
		if (_projection == null || !_projection.Contains(cell))
			throw new InvalidOperationException($"'{_site.SiteId}/{_structure?.Id}' voxel leaves its declared projection at {x},{z}");
		(int lx, int lz) = Local(x, z);
		int ly = y+_offset;
		var grid = _window.Grid;
		if (!grid.InBounds(lx, ly, lz)) throw new InvalidOperationException("Authored voxel leaves the window/height bound");
		grid.Set(lx, ly, lz, material);
		if (Palette.IsSolid(material))
		{
			_touched.Add(cell);
			int index = lz*grid.Size+lx;
			grid.Heights[index] = (short)Math.Max(grid.Heights[index], ly+1);
		}
		Voxels++;
	}

	private void Surface(int x, int z, int top, byte cap)
	{
		(int lx, int lz) = Local(x, z);
		int y = top+_offset;
		if (y <= global::Petalfell.World.Terrain.Sea || !_window.Grid.InBounds(lx, y, lz))
			throw new InvalidOperationException("A dry authored landing must stay above the existing sea and inside the window");
		var data = _window.Data;
		int i = lz*data.Width+lx;
		bool natural = !_site.IsOriginalDesign && cap is
			Palette.GRASS_STONE or Palette.SNOW or Palette.SAND;
		_window.Grid.RedescribeUnedited(lx, lz, y, cap,
			natural ? Palette.SOIL : Palette.STONE, Palette.STONE_PALE);
		data.Height[i] = (ushort)y;
		data.WaterSurface[i] = 0;
		data.Land[i] = 1;
		data.Water[i] = 0;
		data.Hydrology[i] = 0;
		data.Wetness[i] = 0;
		data.Surface[i] = (byte)AtlasTerrainSurface.Cap;
		SurfaceCells++;
	}

	private void Water(int x, int z, int surface, int bed)
	{
		(int lx, int lz) = Local(x,z);
		int top = surface+_offset, bottom = bed+_offset;
		if (bottom < 1 || bottom >= top || top >= _window.Grid.Height)
			throw new InvalidOperationException("Authored water leaves its finite bed/surface envelope");
		int i = lz*_window.Data.Width+lx;
		_window.Grid.RedescribeUnedited(lx,lz,bottom,Palette.SAND,Palette.STONE,Palette.STONE_PALE);
		var data = _window.Data;
		data.Height[i]=(ushort)bottom;
		data.WaterSurface[i]=(ushort)top;
		data.Water[i]=255; data.Land[i]=0; data.Hydrology[i]=3; data.Wetness[i]=255;
		data.Surface[i]=(byte)AtlasTerrainSurface.Underwater;
		SurfaceCells++;
	}

	private (int, int) Local(int x, int z)
	{
		if (x < _site.FootprintMin.X || x > _site.FootprintMax.X ||
		    z < _site.FootprintMin.Z || z > _site.FootprintMax.Z)
			throw new InvalidOperationException("Authored cell leaves the site's footprint");
		var p = _site.ToGlobal(new PlanPoint { X = x, Z = z });
		return (p.X-_window.Data.OriginX, p.Z-_window.Data.OriginZ);
	}

	private static IEnumerable<ReferenceGroundPlanCell> Cells(List<int> r)
	{
		for (int z = r[1]; z <= r[3]; z++)
		for (int x = r[0]; x <= r[2]; x++) yield return new ReferenceGroundPlanCell(x, z);
	}

	private static byte Material(string id) => id switch
	{
		"worn-paving" => Palette.PAVING,
		"warm-stone" => Palette.STONE_WARM,
		"pale-stone" => Palette.STONE_PALE,
		"moss-stone" => Palette.MOSS_STONE,
		"reclaimed-meadow" => Palette.GRASS_STONE,
		"snow" => Palette.SNOW,
		"sand" => Palette.SAND,
		_ => throw new InvalidOperationException($"Unknown authored-site material '{id}'")
	};
}
