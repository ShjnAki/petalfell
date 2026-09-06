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
		if (!site.IsOriginalDesign || site.RuntimePlanScale != 1 ||
		    Plan.CoordinateContract.RuntimeMirrorX != false)
			throw new InvalidOperationException("Original voxel blueprints require an explicit unmirrored metre plan");
	}

	public void Terrain()
	{
		foreach (var patch in Plan.Terrain)
		{
			if (patch.WriteMode == "preserve-atlas") continue;
			if (patch.WriteMode != "author-surface")
				throw new InvalidOperationException("These original landmarks do not author new water bodies");
			foreach (var cell in patch.EffectiveCells)
				Surface(cell.X, cell.Z, patch.SurfaceY!.Value, Material(patch.Material));
		}
		foreach (var stair in Plan.Structures.Where(s => s.Kind == "stair"))
			foreach (var tread in stair.Treads)
				foreach (var cell in Cells(tread.Footprint))
					Surface(cell.X, cell.Z, tread.TopY!.Value, Palette.PAVING);
		foreach (var patch in Plan.SurfacePatches)
			foreach (var cell in patch.EffectiveCells)
				Surface(cell.X, cell.Z, Plan.GetTerrain(patch.TerrainId).SurfaceY!.Value,
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
		_window.Grid.RedescribeUnedited(lx, lz, y, cap, Palette.STONE, Palette.STONE_PALE);
		data.Height[i] = (ushort)y;
		data.WaterSurface[i] = 0;
		data.Land[i] = 1;
		data.Water[i] = 0;
		data.Hydrology[i] = 0;
		data.Wetness[i] = 0;
		data.Surface[i] = (byte)AtlasTerrainSurface.Cap;
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
		_ => throw new InvalidOperationException($"Unknown original-site material '{id}'")
	};
}
