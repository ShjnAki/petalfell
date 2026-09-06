using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Petalfell.World;
using Petalfell.World.Sites;

namespace Petalfell.Tools;

/// <summary>Actual production placement, small-mesh integrity and pottery collision.</summary>
public partial class WorldbuildingSmoke : Node3D
{
	public override async void _Ready()
	{
		try
		{
			var map = MapDefinition.Load("res://content/chapter_01/map.json");
			int count=0, triangles=0;
			foreach(string id in new[]{TidekeepersLanding.SiteId,SplitWitness.SiteId})
			{
				var site=ReferenceSiteDefinition.Load($"res://content/chapter_01/sites/{id}.json");
				var bounds=AtlasRuntimeHandoff.WindowAround(map.CanonicalAtlas,site.Origin.X,site.Origin.Z,2);
				var window=ProductionTerrainWindow.Build(map,map.DefaultSeed,bounds).Window;
				var props=AuthoredSiteProps.Build(window,site); AddChild(props);
				var plan=ReferenceSiteGroundPlan.Load(site);
				Require(props.GetChildCount()==plan.Props.Count,"An authored prop was lost");
				foreach(MeshInstance3D m in props.GetChildren())
				{
					var faces=m.Mesh.GetFaces(); triangles+=faces.Length/3; count++;
					Require(m.Mesh.GetAabb().Position.Y>=-.0001f,"Prop geometry extends below its ground pivot");
					int ix=(int)MathF.Floor(m.Position.X), iz=(int)MathF.Floor(m.Position.Z);
					Require(Mathf.IsEqualApprox(m.Position.Y,window.Grid.Top[iz*window.Grid.Size+ix]),"Prop detached from current terrain");
					var edges=new Dictionary<(Vector3,Vector3),int>();
					void Edge(Vector3 a,Vector3 b)
					{
						a=a.Snapped(Vector3.One*.00001f); b=b.Snapped(Vector3.One*.00001f);
						var key=(a,b); var reverse=(b,a);
						if(edges.ContainsKey(reverse)) key=reverse;
						edges[key]=edges.GetValueOrDefault(key)+1;
					}
					double volume=0;
					for(int i=0;i<faces.Length;i+=3)
					{
						Require(faces[i].IsFinite() && faces[i+1].IsFinite() && faces[i+2].IsFinite(),"Non-finite prop vertex");
						volume-=faces[i].Dot(faces[i+1].Cross(faces[i+2]))/6d;
						Edge(faces[i],faces[i+1]); Edge(faces[i+1],faces[i+2]); Edge(faces[i+2],faces[i]);
					}
					Require(volume>0 && edges.Values.All(n=>n==2),$"Prop {m.Name} has reversed or open geometry");
				}
				await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
				await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
				foreach(MeshInstance3D m in props.GetChildren())
				{
					if(m.GetChildCount()==0) continue;
					Vector3 centre=m.GlobalPosition+Vector3.Up*.55f*m.Scale.Y;
					var hit=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(
						centre+Vector3.Right*3,centre-Vector3.Right*3));
					Require(hit.Count>0,$"Jar {m.Name} lost its blocking surface");
				}
				GD.Print($"[worldbuilding-smoke] {id}: {props.GetChildCount()} authored props, real terrain anchors, closed outward surfaces and jar collision passed");
				props.Free();
			}
			Require(triangles<20000,"Fine props exceeded the two-site triangle budget");
			GD.Print($"[worldbuilding-smoke] {count} props / {triangles} visible triangles total");
			GetTree().Quit();
		}
		catch(Exception ex) { GD.PushError($"[worldbuilding-smoke] {ex}"); GetTree().Quit(1); }
	}
	private static void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
}
