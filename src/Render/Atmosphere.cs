using Godot;
using Petalfell.Core;

namespace Petalfell.Render;

/// <summary>
/// Lighting, sky and air.
///
/// Distance dissolving is the single effect that does most of the "this is a
/// miniature" work in the reference images: foreground crisp, midground soft,
/// background nearly sky. Fog colour, the sky's horizon band and the glow
/// threshold all have to agree, or the seam shows as a ring around the world.
///
/// Shadows are high quality and slightly darker than a washed-out pastel would
/// suggest — important characters and structures have to stay grounded at every
/// intended camera distance.
/// </summary>
public static class Atmosphere
{
	/// <summary>Handed to DayCycle, which owns these once the world is running.</summary>
	public static ShaderMaterial LastSky { get; private set; }
	public static Godot.Environment LastEnvironment { get; private set; }

	public static WorldEnvironment Build()
	{
		var env = new Godot.Environment();

		var sky = new Sky();
		var skyMat = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://shaders/sky.gdshader"),
		};
		skyMat.SetShaderParameter("zenith", Palette.ShaderRgb(Palette.SkyZenith));
		skyMat.SetShaderParameter("horizon", Palette.ShaderRgb(Palette.SkyHorizon));
		skyMat.SetShaderParameter("ground", Palette.ShaderRgb(Palette.SkyGround));
		skyMat.SetShaderParameter("sun_tint", Palette.ShaderRgb(Palette.SunTint));
		skyMat.SetShaderParameter("sun_dir", Palette.SunDir);
		sky.SkyMaterial = skyMat;
		// A changing sky uses a realtime radiance map; never rebake quality GI per frame.
		sky.ProcessMode = Sky.ProcessModeEnum.Realtime;
		sky.RadianceSize = Sky.RadianceSizeEnum.Size256;

		env.BackgroundMode = Godot.Environment.BGMode.Sky;
		env.Sky = sky;
		env.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
		env.AmbientLightColor = new Color(0.92f, 0.935f, 1.0f);
		env.AmbientLightSkyContribution = 0.78f;
		env.AmbientLightEnergy = 0.42f;

		// Leave the play subject clear, then merge distant shelves into the sky.
		env.FogEnabled = true;
		env.FogMode = Godot.Environment.FogModeEnum.Depth;
		env.FogLightColor = Palette.SkyHaze.LinearToSrgb();
		env.FogLightEnergy = 1.0f;
		env.FogSunScatter = 0.12f;
		env.FogDepthBegin = 115f;
		env.FogDepthEnd = 430f;
		env.FogDepthCurve = 1.70f;
		env.FogDensity = 1.0f;
		env.FogHeightDensity = 0.004f;
		env.FogHeight = Palette.WaterLevel + 10f;
		// Depth haze handles distant silhouettes; a separate low-density froxel
		// layer catches actual light in water-level hollows without veiling hills.
		env.VolumetricFogEnabled = true;
		env.VolumetricFogDensity = 0f;
		env.VolumetricFogLength = 360f;
		env.VolumetricFogAlbedo = new Color(0.90f, 0.93f, 1f);
		env.VolumetricFogTemporalReprojectionEnabled = true;

		// Thresholded bloom preserves matte surfaces and gives emitters a halo.
		env.GlowEnabled = true;
		env.GlowNormalized = true;
		env.GlowIntensity = 0.78f;
		env.GlowStrength = 1.28f;
		env.GlowBloom = 0f;
		env.GlowHdrThreshold = 1.05f;
		env.GlowHdrScale = 1.05f;
		env.GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Screen;
		env.SetGlowLevel(0, 0.04f);
		env.SetGlowLevel(1, 0.15f);
		env.SetGlowLevel(2, 0.38f);
		env.SetGlowLevel(3, 1.00f);
		env.SetGlowLevel(4, 0.82f);
		env.SetGlowLevel(5, 0.50f);
		env.SetGlowLevel(6, 0.25f);

		// Exposure is tuned after correcting the previous double colour conversion.
		env.TonemapMode = Godot.Environment.ToneMapper.Aces;
		env.TonemapExposure = 0.72f;
		env.TonemapWhite = 3.1f;

		// Local contact shading complements the mesher's baked corner occlusion.
		env.SsaoEnabled = true;
		env.SsaoRadius = 2.0f;
		env.SsaoIntensity = 0.92f;
		env.SsaoPower = 1.4f;
		env.SsaoLightAffect = 0.25f;
		env.SsaoHorizon = 0.10f;


		// Wet stone uses the opaque depth/normal buffer, so reflection stays on
		// each slab's actual elevation. Weather enables this only while wet.
		env.SsrMaxSteps = 48;
		env.SsrDepthTolerance = .5f;
		env.SsrFadeIn = .15f;
		env.SsrFadeOut = 2f;

		env.AdjustmentEnabled = false;   // the canvas grade owns display space

		LastSky = skyMat;
		LastEnvironment = env;
		var atmosphere = new WorldEnvironment { Environment = env, Name = "Atmosphere" };
		var mistMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/lowland_mist.gdshader") };
		mistMaterial.SetShaderParameter("water_level", Palette.WaterLevel);
		atmosphere.AddChild(new FogVolume
		{
			Name = "LowlandMist",
			Shape = RenderingServer.FogVolumeShape.World,
			Material = mistMaterial,
		});
		return atmosphere;
	}

	/// <summary>
	/// Preserve a clear foreground and a visible depth gradient as the long lens
	/// dollies out. Production and review share this rule, so an overview does not
	/// get a different atmosphere or disappear behind a fixed far fog plane.
	/// </summary>
	public static void SetViewDistance(Godot.Environment environment, float distance, float scale = 1f)
	{
		// Keep the focal plane in clear air. Beginning halfway to the subject
		// bleached its material values whenever the player zoomed out.
		float begin = Mathf.Max(115f * scale, distance * 0.82f);
		float end = Mathf.Max(430f * scale, distance * 2.0f);
		if (Mathf.Abs(environment.FogDepthBegin - begin) > 0.05f) environment.FogDepthBegin = begin;
		if (Mathf.Abs(environment.FogDepthEnd - end) > 0.05f) environment.FogDepthEnd = end;
		// Unshaded outline passes fade in air rather than receiving bright fog
		// colour. They need the same span as their owning surface at every zoom.
		RenderingServer.GlobalShaderParameterSet("pf_depth_haze", new Vector4(begin, end,
			environment.FogDepthCurve, environment.FogEnabled ? environment.FogDensity : 0f));
	}

	public static float ShadowDistanceForView(float distance, float scale = 1f)
		=> Mathf.Max(260f * scale, distance * 2f);

	public static void SetShadowViewDistance(DirectionalLight3D sun, float distance, float scale = 1f)
	{
		if (sun == null) return;
		float range = ShadowDistanceForView(distance, scale);
		if (sun.DirectionalShadowMaxDistance != range) sun.DirectionalShadowMaxDistance = range;
	}

	public static DirectionalLight3D Sun()
	{
		var sun = new DirectionalLight3D
		{
			Name = "Sun",
			PhysicsInterpolationMode = Node.PhysicsInterpolationModeEnum.Off,
			SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly,
			LightColor = Palette.SunColor.LinearToSrgb(),
			LightEnergy = 0.98f,
			ShadowEnabled = true,
			ShadowOpacity = 0.60f,
			ShadowBias = DayCycle.ShadowDepthBias / (DayCycle.DefaultShadowSoftness * DayCycle.SoftShadowQualityRadius),
			ShadowNormalBias = 0.65f,
			ShadowBlur = DayCycle.DefaultShadowSoftness,
			// Filtered cascades avoid the grain from large PCSS penumbras.
			LightAngularDistance = 0f,
			DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits,
			DirectionalShadowBlendSplits = true,
			DirectionalShadowMaxDistance = 260f,
			// The long lens sits 50–240 blocks behind its subject. Tiny near
			// splits wasted most of the map on air in front of the camera.
			DirectionalShadowSplit1 = 0.20f,
			DirectionalShadowSplit2 = 0.45f,
			DirectionalShadowSplit3 = 0.72f,
		};
		sun.LookAtFromPosition(Palette.SunDir * 100f, Vector3.Zero, Vector3.Up);
		return sun;
	}

	/// <summary>
	/// A weak cool fill preserves material colour beneath the changing key.
	/// </summary>
	public static DirectionalLight3D Fill()
	{
		var fill = new DirectionalLight3D
		{
			Name = "Fill",
			SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly,
			LightColor = Palette.FillColor.LinearToSrgb(),
			LightEnergy = 0.10f,
			ShadowEnabled = false,
			LightSpecular = 0f,
		};
		fill.LookAtFromPosition(new Vector3(0.62f, 0.45f, 0.65f) * 100f, Vector3.Zero, Vector3.Up);
		return fill;
	}
}
