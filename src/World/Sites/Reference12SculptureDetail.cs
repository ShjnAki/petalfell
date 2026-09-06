using Godot;
using Petalfell.Core;
using Petalfell.Render;

namespace Petalfell.World.Sites;

/// <summary>
/// Places the from-scratch Reference 12 sculptures at the permanent site anchors.
/// The authored metre-scale meshes own their carved forms and exact static
/// collision; the voxel site continues to own the court and stepped footing.
/// </summary>
public static class Reference12SculptureDetail
{
	public const string SiteId = "fallen-colossus";
	public const string HeadPath = "res://assets/sites/fallen-colossus-authored-head.glb";
	public const string LegsPath = "res://assets/sites/fallen-colossus-authored-legs.glb";

	public static Node3D Build(AtlasSectorWindow window, ReferenceSiteDefinition site,
		ShaderMaterial inkLight, ShaderMaterial inkDark)
	{
		if (window == null || site?.SiteId != SiteId) return null;
		var root = new Node3D { Name = "FallenColossusAuthoredSculptures" };

		int localX = site.Origin.X - window.Data.OriginX;
		int localZ = site.Origin.Z - window.Data.OriginZ;
		// The voxel blueprint's centre is the Y=44 statue dais. Recovering its
		// translation keeps the external meshes attached to both compiled review
		// terrain and the fast map-guided production terrain.
		int verticalOffset = window.Grid.Top[localZ * window.Grid.Size + localX] - 44;
		root.Position = new Vector3(localX, verticalOffset, localZ);
		root.Rotation = new Vector3(0f, Mathf.DegToRad(site.AxisDegrees), 0f);

		// Both assets are authored in block/metre units with bottom-centred pivots.
		// Their 31.2-unit leg and 17.51-unit head heights retain the monument scale;
		// the head's fallen pitch is authored in the mesh, its site yaw stays here.
		ShaderMaterial stone = WorldMaterials.CreateSculptureStone(
			Palette.Get(Palette.STONE_PALE).Top, 0.78f, 0.66f);
		stone.SetShaderParameter("carved_courses", true);
		ShaderMaterial outline = WorldMaterials.CreateSculptureOutline();
		outline.SetShaderParameter("authored_outline_normals", true);
		stone.NextPass = outline;
		root.AddChild(Place(HeadPath, "FallenHead", new Vector3(25f, 40f, 19f),
			-45f, stone));
		root.AddChild(Place(LegsPath, "TrunklessLegs", new Vector3(0f, 44f, 0f),
			0f, stone));
		return root;
	}

	private static Node3D Place(string path, string name, Vector3 position,
		float yawDegrees, Material material)
	{
		PackedScene scene = GD.Load<PackedScene>(path) ??
			throw new System.InvalidOperationException(
				$"Reference 12 authored asset '{path}' was not imported");
		Node3D model = scene.Instantiate<Node3D>();
		model.Name = name;
		model.Position = position;
		model.Rotation = new Vector3(0f, Mathf.DegToRad(yawDegrees), 0f);
		foreach (Node child in model.FindChildren("*", "MeshInstance3D", true, false))
			if (child is MeshInstance3D mesh)
			{
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
				mesh.MaterialOverride = material;
				// A bounded static triangle shape follows the same transform as the
				// visible mesh, leaving the foot gap and carved silhouette traversable.
				// No separate oversized boxes can block apparently empty ground.
				var body = new StaticBody3D { Name = "SculptureCollision" };
				body.AddChild(new CollisionShape3D
				{
					Name = "CarvedStoneSurface", Shape = mesh.Mesh.CreateTrimeshShape()
				});
				mesh.AddChild(body);
			}
		return model;
	}
}
