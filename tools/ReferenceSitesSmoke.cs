using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Petalfell.Core;
using Petalfell.World;

namespace Petalfell.Tools;

/// <summary>Production geometry, connected masonry and real stair collision.</summary>
public partial class ReferenceSitesSmoke : Node3D
{
	private readonly List<string> _errors = new();
	public override async void _Ready()
	{
		try
		{
			var map = MapDefinition.Load("res://content/chapter_01/map.json");
			var requested = OS.GetCmdlineUserArgs().FirstOrDefault(s => s.StartsWith("--site="))?[7..];
			int tested = 0;
			foreach (var canonical in map.CanonicalAtlas.Topology.Sites.Where(s =>
			         s.ReferencePlan?.BuilderId.Contains("-measured-v1") == true &&
			         (requested == null || s.Id == requested)))
			{
				tested++;
				var site = canonical.ReferencePlan;
				var plan = ReferenceSiteGroundPlan.Load(site);
				var bounds = AtlasRuntimeHandoff.WindowAround(map.CanonicalAtlas,site.Origin.X,site.Origin.Z,2);
				var window = ProductionTerrainWindow.Build(map,map.DefaultSeed,bounds).Window;
				window.Data.Validate(map.CanonicalAtlas.BiomeCatalog.Profiles.Count);
				int ox=site.Origin.X-window.Data.OriginX, oz=site.Origin.Z-window.Data.OriginZ;
				var stairCells = plan.Structures.Where(s => s.Kind == "stair").SelectMany(s => s.ProjectionCells).ToHashSet();
				var datum = plan.Terrain.First(t => t.WriteMode == "author-surface" &&
					t.EffectiveCells.Any(p => !stairCells.Contains(p)));
				var dc = datum.EffectiveCells.First(p => !stairCells.Contains(p));
				int offset = window.Grid.Top[(oz+dc.Z)*window.Grid.Size+ox+dc.X]-datum.SurfaceY!.Value;
				int before = _errors.Count;
				if (!AtlasRuntimeHandoff.TryResolveLanding(window,site.Origin.X+site.PlayerSpawn.X,
				    site.Origin.Z+site.PlayerSpawn.Z,out var landing,out string reason) || !landing.ExactCell)
					_errors.Add($"{site.SiteId}: source spawn displaced: {reason}, radius {landing.SearchRadius}");

				var occupied = new HashSet<Vector3I>();
				var owners = new Dictionary<Vector3I,string>();
				foreach (var mass in plan.Structures.Where(s => s.Kind != "stair"))
				{
					int anchor = mass.GroundAt.Count == 2 ? window.Grid.Top[(oz+mass.GroundAt[1])*window.Grid.Size+ox+mass.GroundAt[0]]-offset : 0;
					foreach (var b in mass.Courses)
					for (int z=b[4]; z<=b[5]; z++) for (int x=b[0]; x<=b[1]; x++)
					for (int y=b[2]+anchor+offset; y<=b[3]+anchor+offset; y++)
					{
						var p = new Vector3I(x+ox,y,z+oz);
						if (Palette.IsSolid((byte)b[6])) { occupied.Add(p); owners[p]=mass.Id; }
						else { occupied.Remove(p); owners.Remove(p); }
					}
				}
				var remaining = new HashSet<Vector3I>(occupied);
				var directions = new[]{Vector3I.Left,Vector3I.Right,Vector3I.Up,Vector3I.Down,Vector3I.Forward,Vector3I.Back};
				while (remaining.Count > 0)
				{
					var first = remaining.First(); var queue = new Queue<Vector3I>();
					queue.Enqueue(first); remaining.Remove(first); bool supported=false; int volume=0;
					while (queue.Count > 0)
					{
						var p=queue.Dequeue(); volume++;
						if (p.Y < window.Grid.Top[p.Z*window.Grid.Size+p.X]) supported=true;
						foreach (var d in directions)
						{
							var q=p+d;
							if (remaining.Remove(q)) queue.Enqueue(q);
							else if (!occupied.Contains(q) && Palette.IsSolid(window.Grid.At(q.X,q.Y,q.Z))) supported=true;
						}
					}
					if (!supported) _errors.Add($"{site.SiteId}/{owners[first]}: unsupported {volume}-voxel component near {first-new Vector3I(ox,offset,oz)}");
				}

				var probes = new List<(string Id,int X,int Y,int Z)>();
				foreach (var stair in plan.Structures.Where(s => s.Kind == "stair"))
				foreach (var tread in stair.Treads)
				{
					var f=tread.Footprint;
					int x=(f[0]+f[2])/2+ox,z=(f[1]+f[3])/2+oz,y=tread.TopY!.Value+offset;
					probes.Add((stair.Id,x,y,z));
					if (!Palette.IsSolid(window.Grid.At(x,y-1,z)) ||
					    Palette.IsSolid(window.Grid.At(x,y,z)) || Palette.IsSolid(window.Grid.At(x,y+1,z)))
						_errors.Add($"{site.SiteId}/{stair.Id}: blocked/missing central tread at {x-ox},{y-offset},{z-oz}");
				}
				var collision = new Node3D(); AddChild(collision);
				foreach (var chunk in probes.Select(p => (p.X/ChunkMesher.ChunkSize,p.Z/ChunkMesher.ChunkSize)).Distinct())
				{
					var data=ChunkMesher.Build(window.Grid,chunk.Item1,chunk.Item2);
					var shape=new ConcavePolygonShape3D(); shape.SetFaces(data.CollisionFaces);
					var body=new StaticBody3D(); body.AddChild(new CollisionShape3D { Shape=shape }); collision.AddChild(body);
				}
				await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
				await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
				foreach (var p in probes)
				{
					var from=new Vector3(p.X+.5f,p.Y+.35f,p.Z+.5f);
					var hit=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from,from-Vector3.Up*.8f));
					if (hit.Count==0 || MathF.Abs(((Vector3)hit["position"]).Y-p.Y)>.02f)
						_errors.Add($"{site.SiteId}/{p.Id}: collision does not meet tread {p.X-ox},{p.Y-offset},{p.Z-oz}");
				}
				collision.Free();
				GD.Print($"[reference-sites-smoke] {site.SiteId}: {occupied.Count} final authored voxels, {probes.Count} physical tread probes, {_errors.Count-before} issue(s)");
				GC.Collect();
			}
			if (requested == null || requested == "shallows-gate-and-causeway")
			{
				tested++;
				await CheckShallowsBridge(map);
				await CheckShallowsBridge(map, clipped: true);
			}
			if (tested == 0) _errors.Add("No measured reference site matched " + requested);
			foreach (string error in _errors) GD.PushError("[reference-sites-smoke] "+error);
			GetTree().Quit(_errors.Count==0?0:1);
		}
		catch (Exception ex) { GD.PushError("[reference-sites-smoke] "+ex); GetTree().Quit(1); }
	}

	private async System.Threading.Tasks.Task CheckShallowsBridge(MapDefinition map, bool clipped = false)
	{
		var bounds = AtlasRuntimeHandoff.WindowAround(map.CanonicalAtlas,6400,clipped ? 7360 : 6980,2);
		var window = ProductionTerrainWindow.Build(map,map.DefaultSeed,bounds).Window;
		window.Data.Validate(map.CanonicalAtlas.BiomeCatalog.Profiles.Count);
		int x=6400-window.Data.OriginX,z=7040-window.Data.OriginZ,index=z*window.Data.Width+x;
		if (window.Data.Height[index]!=18 || window.Data.WaterSurface[index]!=24 ||
		    window.Grid.HeightAt(x,z)!=87 || window.Data.Land[index]!=0)
			_errors.Add("Shallows: expected separate bed 18, water 24, and solid deck 87");
		if (!window.TryWaterColumnAtGlobal(6400,7040,out float bed,out float water) ||
		    bed!=18 || MathF.Abs(water-24.35f)>.01f)
			_errors.Add("Shallows: controller water query did not retain the submerged bed");
		if (!AtlasRuntimeHandoff.TryResolveLanding(window,6400,7040,out var landing,out var rejection) ||
		    !landing.ExactCell || landing.SurfaceY!=87)
			_errors.Add("Shallows: bridge travel displaced: "+rejection);
		for (int y=25;y<84;y++)
			if (window.Grid.SolidAt(x,y,z)) _errors.Add("Shallows: solid obstruction below deck at "+y);
		var collision=new Node3D();AddChild(collision);
		var mesh=ChunkMesher.Build(window.Grid,x/ChunkMesher.ChunkSize,z/ChunkMesher.ChunkSize);
		var shape=new ConcavePolygonShape3D();shape.SetFaces(mesh.CollisionFaces);
		var body=new StaticBody3D();body.AddChild(new CollisionShape3D { Shape=shape });collision.AddChild(body);
		await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
		foreach (var ray in new[]{(From:88f,To:86f,Y:87f),(From:25f,To:86f,Y:84f),(From:24f,To:17f,Y:18f)})
		{
			var hit=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(
				new Vector3(x+.5f,ray.From,z+.5f),new Vector3(x+.5f,ray.To,z+.5f)));
			if (hit.Count==0 || MathF.Abs(((Vector3)hit["position"]).Y-ray.Y)>.02f)
				_errors.Add("Shallows: physical bridge/bed surface missing at "+ray.Y);
		}
		collision.Free();
		GD.Print($"[reference-sites-smoke] Shallows bridge ({(clipped ? "clipped" : "complete")} window): bed/water/deck separation, exact travel, underside clearance and 3 physical surfaces checked");
	}
}
