using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Petalfell.Core;
using Petalfell.World;
using Petalfell.World.Sites;

namespace Petalfell.Tools;

/// <summary>Exercises the actual imported geometry, production attachment and physics.</summary>
public partial class SculptureSmoke : Node3D
{
	public override async void _Ready()
	{
		try
		{
			var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
			var site = ReferenceSiteDefinition.Load(
				"res://content/chapter_01/sites/fallen-colossus-reference-12.json");
			const int size = 256;
			Node3D Make(int originX, int originZ, int datum)
			{
				var data = new AtlasSectorData(0, 0, originX, originZ, size, 0,
					size, size, 128, 0, "sculpture-smoke");
				var grid = new VoxelGrid(size, 128, 7, originX, originZ);
				grid.Describe(site.Origin.X-originX, site.Origin.Z-originZ,
					datum, Palette.STONE_PALE, Palette.STONE);
				return Reference12SculptureDetail.Build(
					new AtlasSectorWindow(data, atlas, 7, grid), site, null, null);
			}
			var a = Make(10500, 4500, 44);
			AddChild(a);
			var meshes = a.FindChildren("*", "MeshInstance3D", true, false)
				.Cast<MeshInstance3D>().ToArray();
			Require(meshes.Length == 2, "The monument must have exactly two authored mesh assets");
			int triangles = 0;
			foreach (var mesh in meshes)
			{
				Require(mesh.Scale.IsEqualApprox(Vector3.One), "Imported mesh changed metre scale");
				var shape = mesh.GetNode<CollisionShape3D>("SculptureCollision/CarvedStoneSurface");
				Require(shape.Shape is ConcavePolygonShape3D, "Sculpture lost its actual surface collision");
				var faces = ((ConcavePolygonShape3D)shape.Shape).GetFaces();
				var visible = mesh.Mesh.GetFaces();
				Require(faces.Length == visible.Length, "Visible/collision triangle counts differ");
				for (int i = 0; i < faces.Length; i++)
					Require(faces[i].IsEqualApprox(visible[i]), "Collision departs from the carved mesh");
				triangles += faces.Length / 3;
				Require(mesh.Mesh.GetAabb().Position.Y >= -.001f, "Asset pivot is below its ground support");
				var byPosition = new Dictionary<Vector3, Vector3>();
				int pinned = 0, normalCount = 0;
				for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
				{
					var arrays = mesh.Mesh.SurfaceGetArrays(surface);
					var positions = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
					var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
					var packed = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
					Require(packed.Length == positions.Length, "Imported mesh lost its hull-normal channel");
					for (int i = 0; i < positions.Length; i++)
					{
						Vector2 p = packed[i] * 2f - Vector2.One;
						var n = new Vector3(p.X, p.Y, 1f-Mathf.Abs(p.X)-Mathf.Abs(p.Y));
						float fold = Mathf.Clamp(-n.Z, 0, 1);
						n.X += n.X >= 0 ? -fold : fold;
						n.Y += n.Y >= 0 ? -fold : fold;
						n = n.Normalized();
						if (packed[i].X < 0) { n = Vector3.Zero; pinned++; }
						normalCount++;
						Require(n.IsFinite() && n.Dot(normals[i]) >= -.025f,
							"Hull-normal basis/UV flip points inward from a carved face");
						if (byPosition.TryGetValue(positions[i], out Vector3 previous))
							Require(n.DistanceTo(previous) < .003f, "Hard face split tears the hull normal");
						byPosition[positions[i]] = n;
					}
				}
				Require(pinned < normalCount / 50, "Too many hull corners lost their outward extrusion");
			}
			Require(triangles is > 2000 and < 12000, "Sculpture triangle budget changed unexpectedly");
			var b = Make(10480, 4480, 51);
			Require((b.Position + new Vector3(10480, -7, 4480)).IsEqualApprox(
				a.Position + new Vector3(10500, 0, 4500)), "Window/datum change detached site anchors");
			b.Free();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			bool Hit(Vector3 from, Vector3 to) => GetWorld3D().DirectSpaceState
				.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to)).Count != 0;
			Require(!Hit(new Vector3(100, 47, 80), new Vector3(100, 47, 135)),
				"The visible gap between feet is blocked");
			Require(!Hit(new Vector3(100, 60, 80), new Vector3(100, 60, 135)),
				"The visible gap between calves is blocked");
			Require(Hit(new Vector3(94.5f, 60, 80), new Vector3(94.5f, 60, 120)),
				"West leg has no blocking collision");
			Require(Hit(new Vector3(105.5f, 60, 80), new Vector3(105.5f, 60, 120)),
				"East leg has no blocking collision");
			Require(Hit(new Vector3(125, 80, 119), new Vector3(125, 30, 119)),
				"Fallen head has no walkable surface collision");
			using var capsule = new CapsuleShape3D { Radius = .38f, Height = 1.75f };
			float Travel(Vector3 from, Vector3 motion)
			{
				var query = new PhysicsShapeQueryParameters3D
				{
					Shape = capsule, Transform = new Transform3D(Basis.Identity, from),
					Motion = motion, Margin = .01f
				};
				return GetWorld3D().DirectSpaceState.CastMotion(query)[0];
			}
			Require(Travel(new Vector3(100, 45, 80), new Vector3(0, 0, 55)) > .999f,
				"The player-sized capsule cannot travel between the feet");
			Require(Travel(new Vector3(94.5f, 60, 80), new Vector3(0, 0, 40)) < .9f,
				"The player-sized capsule passes through the west leg");
			Require(Travel(new Vector3(125, 80, 119), new Vector3(0, -50, 0)) < .9f,
				"The player-sized capsule falls through the head");
			GD.Print($"[sculpture-smoke] two authored assets, {triangles} matching collision triangles, " +
				"metre pivots, continuous outward hull normals, translated window/datum, " +
				"open foot/calf gap, blocking legs/head and player capsule sweeps passed");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PushError($"[sculpture-smoke] {ex}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool value, string message)
	{
		if (!value) throw new InvalidOperationException(message);
	}
}
