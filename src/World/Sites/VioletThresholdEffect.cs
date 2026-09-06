using Godot;
using Petalfell.Core;

namespace Petalfell.World.Sites;

/// <summary>Reference 5 owns this opening, light and bounded particle envelope.</summary>
public static class VioletThresholdEffect
{
	public static Node3D Build(AtlasSectorWindow window, ReferenceSiteDefinition site)
	{
		if (site?.SiteId != "violet-threshold") return null;
		var anchor = site.ToGlobal(new PlanPoint { X = 0, Z = -15 });
		int x = anchor.X-window.Data.OriginX, z = anchor.Z-window.Data.OriginZ;
		// The clear doorway's floor is the plan's crown at TopY38. Recover the
		// actual translated floor in every newly materialised atlas window.
		int offset = window.Grid.Top[z*window.Grid.Size+x]-38;
		var root = new Node3D { Name = "VioletThreshold", Position = new Vector3(
			site.Origin.X-window.Data.OriginX,offset,site.Origin.Z-window.Data.OriginZ),
			Rotation = new Vector3(0,Mathf.DegToRad(site.AxisDegrees),0) };
		var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/violet_threshold.gdshader") };
		material.SetShaderParameter("violet",Palette.ShaderRgb(Palette.PortalViolet));
		material.SetShaderParameter("rim_colour",Palette.ShaderRgb(Palette.PortalRim));
		root.AddChild(new MeshInstance3D { Name = "ActiveOpening", Mesh = new QuadMesh {
			Size = new Vector2(12.0f,12.0f) }, MaterialOverride = material,
			Position = new Vector3(.5f,43.6f,-15.45f),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
		root.AddChild(new OmniLight3D { Name = "ThresholdLight", Position = new Vector3(.5f,43,-13),
			LightColor = Palette.PortalViolet.LinearToSrgb(), LightEnergy = 3.2f,
			OmniRange = 18, OmniAttenuation = 1.5f, ShadowEnabled = true,
			LightSize = 1.4f });
		var moteMaterial = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			AlbedoColor = Palette.PortalRim.LinearToSrgb(), EmissionEnabled = true,
			Emission = Palette.PortalViolet.LinearToSrgb(), EmissionEnergyMultiplier = 2.0f };
		root.AddChild(new GpuParticles3D { Name = "ThresholdMotes", Amount = 36, Lifetime = 3.5,
			Preprocess = 3.5, Position = new Vector3(.5f,43,-15),
			VisibilityAabb = new Aabb(new Vector3(-8,-7,-4),new Vector3(16,16,8)),
			DrawPass1 = new BoxMesh { Size = Vector3.One*.07f, Material = moteMaterial },
			ProcessMaterial = new ParticleProcessMaterial { EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
				EmissionBoxExtents = new Vector3(5,5,1), Gravity = new Vector3(0,.18f,0),
				Direction = Vector3.Up, Spread = 60, InitialVelocityMin = .2f, InitialVelocityMax = .7f,
				ScaleMin = .4f, ScaleMax = 1.2f } });
		return root;
	}
}
