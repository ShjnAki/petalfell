using System;
using System.Collections.Generic;
using Godot;
using Petalfell.Core;
using Petalfell.Render;

namespace Petalfell.World.Sites;

/// <summary>Small shared meshes, placed only by explicit per-site prop records.</summary>
public static class AuthoredSiteProps
{
	public static Node3D Build(AtlasSectorWindow window, ReferenceSiteDefinition site)
	{
		if (site?.IsOriginalDesign != true) return null;
		var plan = ReferenceSiteGroundPlan.Load(site);
		var root = new Node3D { Name = "AuthoredSiteProps" };
		var meshes = new Dictionary<string, Mesh>();
		var materials = new Dictionary<string, Material>();
		foreach (var prop in plan.Props)
		{
			if (!meshes.TryGetValue(prop.Kind, out var mesh))
				meshes[prop.Kind] = mesh = MakeMesh(prop.Kind);
			if (!materials.TryGetValue(prop.Kind, out var material))
			{
				byte cap = prop.Kind switch { "jar" or "broken-jar" or "pottery-shard" => Palette.SOIL,
					"rope-coil" => Palette.PLANK_PALE, "plank" => Palette.BEAM, _ => Palette.STONE_PALE };
				var finish = WorldMaterials.CreateSculptureStone(Palette.Get(cap).Top,.30f);
				finish.SetShaderParameter("weathered_wood",prop.Kind == "plank");
				materials[prop.Kind] = material = finish;
			}
			// Translation uses the runtime terrain column, so a moved atlas window
			// cannot leave a pot floating at an old local address or cached datum.
			var local = new Vector3(prop.X,0,prop.Z).Rotated(Vector3.Up,Mathf.DegToRad(site.AxisDegrees));
			float x = local.X + site.Origin.X-window.Data.OriginX;
			float z = local.Z + site.Origin.Z-window.Data.OriginZ;
			int i = (int)MathF.Floor(z)*window.Grid.Size+(int)MathF.Floor(x);
			var instance = new MeshInstance3D { Name = prop.Id, Mesh = mesh, MaterialOverride = material,
				Position = new Vector3(x,window.Grid.Top[i],z), Scale = Vector3.One*prop.Scale,
				Rotation = new Vector3(0,Mathf.DegToRad(site.AxisDegrees+prop.YawDegrees),0),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.On };
			if (prop.Kind is "jar" or "broken-jar")
			{
				var body = new StaticBody3D { Name = "JarCollision" };
				body.AddChild(new CollisionShape3D { Shape = mesh.CreateTrimeshShape() });
				instance.AddChild(body);
			}
			root.AddChild(instance);
		}
		return root;
	}

	private static Mesh MakeMesh(string kind)
	{
		if (kind == "plank") return SplitPlank();
		if (kind == "stone-shard") return Box(new Vector3(.6f,.25f,.9f));
		if (kind == "pottery-shard") return Box(new Vector3(.45f,.10f,.65f));
		var b = new Facets();
		if (kind == "rope-coil")
		{
			const int count = 64;
			Vector3 Point(int i, int j)
			{
				float a = i*Mathf.Pi*5/count, r = .28f + a*.045f;
				float c = j*Mathf.Tau/4;
				return new Vector3(Mathf.Cos(a)*(r+Mathf.Cos(c)*.072f),
					.085f+Mathf.Sin(c)*.072f,Mathf.Sin(a)*(r+Mathf.Cos(c)*.072f));
			}
			for (int i=0;i<count;i++) for (int j=0;j<4;j++)
				b.Quad(Point(i,j),Point(i+1,j),Point(i+1,j+1),Point(i,j+1));
			b.Quad(Point(0,3),Point(0,2),Point(0,1),Point(0,0));
			b.Quad(Point(count,0),Point(count,1),Point(count,2),Point(count,3));
		}
		else
		{
			// Closed pottery wall: outer belly, rim, inner cavity, thick floor.
			Vector2[] profile = { new(0,0),new(.35f,0),new(.48f,.10f),new(.70f,.55f),
				new(.60f,1),new(.40f,1.25f),new(.42f,1.34f),new(.30f,1.34f),
				new(.28f,1.22f),new(.47f,.94f),new(.55f,.55f),new(.30f,.16f),new(0,.16f) };
			Vector3 Point(int i,int j)
			{
				float a = i*Mathf.Tau/8;
				float h = profile[j].Y;
				if (kind == "broken-jar" && (i%8 is 2 or 3) && j is >= 5 and <= 8) h -= .30f;
				return new Vector3(Mathf.Cos(a)*profile[j].X,h,Mathf.Sin(a)*profile[j].X);
			}
			for(int i=0;i<8;i++) for(int j=0;j<profile.Length-1;j++)
				b.Quad(Point(i,j),Point(i+1,j),Point(i+1,j+1),Point(i,j+1));
		}
		return b.Mesh();
	}

	private static ArrayMesh Box(Vector3 size)
	{
		var b = new Facets();
		var lo = new Vector3(-size.X*.5f,0,-size.Z*.5f);
		var hi = lo+size;
		Vector3 P(int x,int y,int z) => new(x==0?lo.X:hi.X,y==0?lo.Y:hi.Y,z==0?lo.Z:hi.Z);
		b.Quad(P(0,0,0),P(1,0,0),P(1,1,0),P(0,1,0));
		b.Quad(P(1,0,1),P(0,0,1),P(0,1,1),P(1,1,1));
		b.Quad(P(0,0,1),P(0,0,0),P(0,1,0),P(0,1,1));
		b.Quad(P(1,0,0),P(1,0,1),P(1,1,1),P(1,1,0));
		b.Quad(P(0,1,0),P(1,1,0),P(1,1,1),P(0,1,1));
		b.Quad(P(0,0,1),P(1,0,1),P(1,0,0),P(0,0,0));
		return b.Mesh();
	}

	private static ArrayMesh SplitPlank()
	{
		// Three joined slivers form an uneven broken end and a narrow open split.
		// Each closed sliver has its own physical top; there is no painted cutout.
		var b = new Facets();
		foreach(var span in new[]{(-.36f,-.10f,-1.60f,1.60f),(-.10f,.04f,-1.37f,1.28f),(.04f,.36f,-1.47f,1.54f)})
		{
			float x0=span.Item1,x1=span.Item2,z0=span.Item3,z1=span.Item4;
			Vector3 P(float x,float y,float z)=>new(x,y,z);
			b.Quad(P(x0,0,z0),P(x1,0,z0),P(x1,.16f,z0),P(x0,.16f,z0));
			b.Quad(P(x1,0,z1),P(x0,0,z1),P(x0,.16f,z1),P(x1,.16f,z1));
			b.Quad(P(x0,0,z1),P(x0,0,z0),P(x0,.16f,z0),P(x0,.16f,z1));
			b.Quad(P(x1,0,z0),P(x1,0,z1),P(x1,.16f,z1),P(x1,.16f,z0));
			b.Quad(P(x0,.16f,z0),P(x1,.16f,z0),P(x1,.16f,z1),P(x0,.16f,z1));
			b.Quad(P(x0,0,z1),P(x1,0,z1),P(x1,0,z0),P(x0,0,z0));
		}
		return b.Mesh();
	}

	private sealed class Facets
	{
		private readonly List<Vector3> _vertices = new(), _normals = new();
		public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d) { Triangle(a,b,c); Triangle(a,c,d); }
		private void Triangle(Vector3 a,Vector3 b,Vector3 c)
		{
			// Clockwise face order is front-facing in Godot.
			Vector3 n=(c-a).Cross(b-a);
			if(n.LengthSquared()<.0000001f) return;
			n=n.Normalized();
			_vertices.AddRange(new[]{a,b,c}); _normals.AddRange(new[]{n,n,n});
		}
		public ArrayMesh Mesh()
		{
			var arrays = new Godot.Collections.Array(); arrays.Resize((int)Godot.Mesh.ArrayType.Max);
			arrays[(int)Godot.Mesh.ArrayType.Vertex]=_vertices.ToArray();
			arrays[(int)Godot.Mesh.ArrayType.Normal]=_normals.ToArray();
			var mesh=new ArrayMesh(); mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays); return mesh;
		}
	}
}
