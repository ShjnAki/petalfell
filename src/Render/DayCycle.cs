using System;
using Godot;
using Petalfell.Core;

namespace Petalfell.Render;

/// <summary>
/// The clock, and everything the sky does because of it.
///
/// One node owns the whole atmosphere at runtime — sun, moon, ambient, fog,
/// glow, sky and the shader globals — because every one of those has to agree
/// about what time it is. Split across several owners they drift, and the seam
/// shows immediately: a warm sun under a midnight sky, or fog that stays noon
/// pale while the light goes out.
///
/// THE KEY LIGHT IS ONE LIGHT. It is the sun while the sun is up and the moon
/// after it sets, swapping direction to the opposite side of the sky. Two
/// directional lights fighting over the same shadow map is a real cost for a
/// benefit nobody can see at this camera distance, and a single key means the
/// shadows never double or pop at the moment of the handover.
///
/// Shader values travel as GLOBAL uniforms rather than per-material parameters.
/// The character shader alone builds a fresh material for every box of every
/// figure in the world, so a cycle that pushed the sun direction to each one
/// would spend its whole frame chasing references that come and go with
/// streaming. Registered once, set once a frame, read by everything.
/// </summary>
public partial class DayCycle : Node
{
	public const string SunDirParam = "pf_sun_dir";
	public const string NightParam = "pf_night";
	public const string SunColourParam = "pf_sun_colour";

	/// <summary>0 midnight, 0.25 sunrise, 0.5 noon, 0.75 sunset.</summary>
	public float TimeOfDay = 0.34f;
	/// <summary>Real seconds for one full cycle.</summary>
	public float DayLength = 900f;
	public bool Paused;
	/// <summary>Authored darkness blend: 0 in daylight, 1 at full night.</summary>
	public float NightAmount { get; private set; }
	/// <summary>Player-facing multiplier over the authored day/night glow.</summary>
	public float BloomAmount { get; private set; } = 1f;
	/// <summary>
	/// Multiplier over the authored night exposure. Zero leaves the night lighting
	/// un-dimmed, one is the original exposure curve, and values above one deepen
	/// it without changing the authored colours.
	/// </summary>
	public float NightDarkness { get; private set; } = 0.45f;
	public const float MaxShadowSoftness = 12f;
	public const float DefaultShadowSoftness = 1.25f;
	internal const float ShadowDepthBias = .32f;
	internal const float SoftShadowQualityRadius = 4f;
	/// <summary>Directional filtering radius; zero is crisp, twelve is very soft.</summary>
	public float ShadowSoftness { get; private set; } = DefaultShadowSoftness;
	private bool? _hardShadowFilter;
	/// <summary>Smoothed, deterministic weather coverage. Clouds are lighting-only.</summary>
	public float CloudCover { get; private set; }
	public float RainCover { get; private set; }
	public void SetRainCover(float amount)
	{
		amount = Mathf.Clamp(amount, 0, 1);
		if (Math.Abs(amount - RainCover) < .0001f) return;
		RainCover = amount; Apply();
	}
	/// <summary>World-space directions from the world toward each celestial body.</summary>
	public Vector3 SunDirection { get; private set; } = Palette.SunDir;
	public Vector3 MoonDirection { get; private set; } = -Palette.SunDir;

	private Godot.Environment _env;
	private DirectionalLight3D _key;
	private DirectionalLight3D _fill;
	private ShaderMaterial _sky;
	private ShaderMaterial _water;
	private readonly RandomNumberGenerator _weatherRng = new();
	private float _cloudStartCover;
	private float _cloudTargetCover;
	private float _cloudSegmentTime;
	private float _cloudSegmentDuration;
	private float _cloudPatternAmplitude;
	private float _cloudPatternCyclesA;
	private float _cloudPatternCyclesB;
	private float _cloudPatternPhaseA;
	private float _cloudPatternPhaseB;
	private float _weatherTime;

	/// <summary>
	/// Register the globals before anything that reads them is compiled.
	///
	/// A shader naming a global uniform that does not exist yet fails to compile,
	/// and the failure surfaces as an untextured object rather than as an error
	/// anyone would connect to this — so this runs first thing at boot, before a
	/// single material is built.
	/// </summary>
	private static bool _registered;

	public static void RegisterGlobals()
	{
		// Guarded by a flag rather than by asking the server what already exists:
		// GlobalShaderParameterGetList is editor-only and logs a severe-performance
		// error when called from a running game.
		if (_registered) return;
		_registered = true;

		static void Add(string name, RenderingServer.GlobalShaderParameterType type, Variant value)
			=> RenderingServer.GlobalShaderParameterAdd(name, type, value);

		Add(SunDirParam, RenderingServer.GlobalShaderParameterType.Vec3, Palette.SunDir);
		Add(NightParam, RenderingServer.GlobalShaderParameterType.Float, 0f);
		Add("pf_cloud_cover", RenderingServer.GlobalShaderParameterType.Float, 0f);
		Add("pf_weather_time", RenderingServer.GlobalShaderParameterType.Float, 0f);
		Add("pf_cloud_daylight", RenderingServer.GlobalShaderParameterType.Float, 1f);
		Add("pf_reflection_plane", RenderingServer.GlobalShaderParameterType.Float, -1000000f);
		Add("pf_depth_haze", RenderingServer.GlobalShaderParameterType.Vec4,
			new Vector4(115f, 430f, 1.70f, 1f));
		Add(SunColourParam, RenderingServer.GlobalShaderParameterType.Vec3,
			new Vector3(Palette.SunColor.R, Palette.SunColor.G, Palette.SunColor.B));
		Add("pf_puddle_state", RenderingServer.GlobalShaderParameterType.Vec4, new Vector4(-1000000,0,0,0));
		Add("pf_puddle_tex", RenderingServer.GlobalShaderParameterType.Sampler2D, new GradientTexture2D());
		Add("pf_rain_time", RenderingServer.GlobalShaderParameterType.Float, 0f);
		for (int i = 0; i < Petalfell.Weather.RainField.CellCount; i++)
		{
			Add($"pf_rain_{i}", RenderingServer.GlobalShaderParameterType.Vec4, Vector4.Zero);
			Add($"pf_wet_{i}", RenderingServer.GlobalShaderParameterType.Vec4, Vector4.Zero);
		}
	}

	public void Setup(Godot.Environment env, DirectionalLight3D key, DirectionalLight3D fill,
		ShaderMaterial sky, ShaderMaterial water, int weatherSeed = 0)
	{
		_env = env;
		_key = key;
		_fill = fill;
		_sky = sky;
		_water = water;
		// Weather is reproducible for a map seed, but independent from the terrain
		// generator's random stream so changing clouds never changes the world.
		_weatherRng.Seed = ((ulong)(uint)weatherSeed << 32) ^ 0x9e3779b97f4a7c15UL;
		_cloudStartCover = _weatherRng.RandfRange(0.08f, 0.48f);
		ConfigureNextCloudSegment();
		ProcessPriority = -50;   // before anything reads the light this frame
		_appliedSky = -1f;
		_appliedSkyCloud = -1;
		// This light is driven at render frequency, never by the physics clock.
		if (_key != null) _key.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		Apply();
	}

	/// <summary>
	/// Only sky radiance is throttled. Quantizing the actual sun direction here
	/// held every cast shadow still for .44 seconds, then moved it in one jump.
	/// </summary>
	private const float Quantum = 1f / 2048f;

	private float _appliedSky = -1f;
	private int _appliedSkyCloud = -1;
	private float _appliedTime = -1f;
	private float _appliedWeatherTime = -1f;

	public override void _Process(double delta)
	{
		if (!Paused)
		{
			if (DayLength > 0.01f)
				TimeOfDay = Mathf.PosMod(TimeOfDay + (float)delta / DayLength, 1f);
			AdvanceClouds((float)delta);
			_weatherTime += (float)delta;
			RenderingServer.GlobalShaderParameterSet("pf_weather_time", _weatherTime);
		}

		if (TimeOfDay == _appliedTime && _weatherTime == _appliedWeatherTime) return;
		Apply();
	}

	/// <summary>
	/// Weather is a chain of random smooth segments. Each has a new target,
	/// duration and pair of overlapping undulations, with the pattern tapered to
	/// zero at both ends so one segment joins the next without a lighting pop.
	/// This avoids tying cloud events to a repeating point in the day cycle.
	/// </summary>
	private float ComputeCloudCover()
	{
		if (_cloudSegmentDuration <= 0.001f) return _cloudStartCover;
		float x = Mathf.Clamp(_cloudSegmentTime / _cloudSegmentDuration, 0f, 1f);
		float smooth = x * x * (3f - 2f * x);
		float envelope = Mathf.Sin(x * Mathf.Pi);
		float pattern = Mathf.Sin(x * Mathf.Tau * _cloudPatternCyclesA + _cloudPatternPhaseA) * 0.68f
			+ Mathf.Sin(x * Mathf.Tau * _cloudPatternCyclesB + _cloudPatternPhaseB) * 0.32f;
		float cover = Mathf.Lerp(_cloudStartCover, _cloudTargetCover, smooth)
			+ pattern * _cloudPatternAmplitude * envelope;
		return Mathf.Clamp(cover, 0.02f, 0.90f);
	}

	private void AdvanceClouds(float delta)
	{
		_cloudSegmentTime += delta;
		if (_cloudSegmentTime < _cloudSegmentDuration) return;

		float carry = _cloudSegmentTime - _cloudSegmentDuration;
		_cloudStartCover = _cloudTargetCover;
		ConfigureNextCloudSegment();
		_cloudSegmentTime = Mathf.Min(carry, _cloudSegmentDuration);
	}

	private void ConfigureNextCloudSegment()
	{
		_cloudTargetCover = _weatherRng.RandfRange(0.04f, 0.84f);
		_cloudSegmentDuration = _weatherRng.RandfRange(55f, 155f);
		_cloudSegmentTime = 0f;
		_cloudPatternAmplitude = _weatherRng.RandfRange(0.025f, 0.13f);
		_cloudPatternCyclesA = _weatherRng.RandfRange(0.65f, 2.25f);
		_cloudPatternCyclesB = _weatherRng.RandfRange(1.6f, 4.4f);
		_cloudPatternPhaseA = _weatherRng.RandfRange(0f, Mathf.Tau);
		_cloudPatternPhaseB = _weatherRng.RandfRange(0f, Mathf.Tau);
	}

	private static float HorizonVisibility(float height)
	{
		float x = Mathf.Clamp((height + 0.045f) / 0.12f, 0f, 1f);
		return x * x * (3f - 2f * x);
	}

	/// <summary>Blend the keyframe table at the current time, wrapping midnight.</summary>
	private static Palette.SkyState Sample(float t, out float mix, out Palette.SkyState next)
	{
		var keys = Palette.Day;
		int i = keys.Length - 1;
		for (int k = 0; k < keys.Length; k++)
			if (keys[k].At <= t) i = k;

		int j = (i + 1) % keys.Length;
		float a = keys[i].At;
		float b = keys[j].At;
		// The last key wraps forward past midnight into the first.
		float span = b > a ? b - a : b + 1f - a;
		float into = t >= a ? t - a : t + 1f - a;
		mix = span < 0.0001f ? 0f : Mathf.Clamp(into / span, 0f, 1f);
		next = keys[j];
		return keys[i];
	}

	private void Apply()
	{
		_appliedTime = TimeOfDay;
		_appliedWeatherTime = _weatherTime;
		var from = Sample(TimeOfDay, out float k, out var to);

		Color Blend(Color x, Color y) => x.Lerp(y, k);
		float Lerp(float x, float y) => Mathf.Lerp(x, y, k);

		// Where the sun is.
		//
		// The arc is built around the authored noon direction rather than a
		// vertical: Palette.SunDir is the angle the whole look was tuned at, so
		// making it the apex means midday still matches every capture ever taken
		// of this project, and dawn and dusk fall out of the same circle.
		float ang = (TimeOfDay - 0.25f) * Mathf.Tau;
		Vector3 high = Palette.SunDir;
		Vector3 east = high.Cross(Vector3.Up).Normalized();
		SunDirection = (east * Mathf.Cos(ang) + high * Mathf.Sin(ang)).Normalized();
		MoonDirection = -SunDirection;
		float sunVisibility = HorizonVisibility(SunDirection.Y);
		float moonVisibility = HorizonVisibility(MoonDirection.Y);

		// One key light: the sun by day, the moon after it goes down.
		bool daylight = SunDirection.Y > 0f;
		Vector3 keyDir = daylight ? SunDirection : MoonDirection;

		float night = Lerp(from.Night, to.Night);
		NightAmount = night;
		CloudCover = Mathf.Lerp(ComputeCloudCover(), .98f, RainCover);
		var sunColour = Blend(from.Sun, to.Sun);
		var moonColour = Palette.MoonColor;
		// The old single-key implementation moved to the moon's direction after
		// sunset but kept illuminating the terrain with the dusk sun colour. That
		// is why the nominal blue night turned the ground brown-magenta. Blend the
		// key itself toward the authored moon only after twilight is established;
		// the transition happens while the horizon key is already weak.
		float moonKey = Mathf.SmoothStep(0.48f, 0.82f, night);
		var keyColour = sunColour.Lerp(moonColour, moonKey);
		float energy = Lerp(from.SunEnergy, to.SunEnergy);
		// The palette already changes hue and authored energy through dusk. This
		// second, continuous exposure curve is what makes night actually deepen
		// instead of stopping at a differently coloured version of daytime.
		float darkness = night * NightDarkness;
		float keyExposure = Mathf.Pow(0.64f, darkness);
		float ambientExposure = Mathf.Pow(0.58f, darkness);
		// A cloud is mostly forward-scattering in daylight, so the sky fill remains
		// readable while the direct sun falls away. At night there is no bright sky
		// behind it: the same cover blocks the moon and deepens the ambient instead.
		float directCloud = daylight
			? Mathf.Lerp(1f, 0.60f, CloudCover)
			: Mathf.Lerp(1f, 0.58f, CloudCover);
		float ambientCloud = daylight
			? Mathf.Lerp(1f, 1.08f, CloudCover)
			: Mathf.Lerp(1f, 0.76f, CloudCover);

		if (_key != null)
		{
			_key.LightColor = keyColour.LinearToSrgb();
			// Fade direct light to zero before changing celestial ownership. The sky
			// and ambient retain twilight while the shadow direction crosses horizon.
			// The direct key crosses the horizon at zero energy.
			float horizonKey = Mathf.SmoothStep(0f, 0.24f, Mathf.Abs(SunDirection.Y));
			_key.LightEnergy = energy * keyExposure * directCloud * horizonKey * Mathf.Lerp(1f, .40f, RainCover);
			// Cloud transmission already dims the direct key. Retain occlusion;
			// fading it a second time floods sheltered faces with direct light.
			_key.ShadowOpacity = Lerp(from.ShadowOpacity, to.ShadowOpacity);
			bool hard = ShadowSoftness == 0f;
			if (_hardShadowFilter != hard)
			{
				RenderingServer.DirectionalSoftShadowFilterSetQuality(hard
					? RenderingServer.ShadowQuality.Hard : RenderingServer.ShadowQuality.SoftUltra);
				_hardShadowFilter = hard;
			}
			// Hard filtering has no blur kernel. Do not pass a zero ShadowBlur:
			// Godot multiplies depth bias by blur and would remove it entirely.
			// Ultra's radius is 4; Hard's is 1. Keep the effective depth offset
			// constant as the filter widens, avoiding acne and detached shadows.
			_key.ShadowBlur = hard ? 1f : Math.Max(.05f, ShadowSoftness);
			_key.ShadowBias = ShadowDepthBias / (_key.ShadowBlur * (hard ? 1f : SoftShadowQualityRadius));
			// PCSS angular-distance shadows caused noisy stippling and huge unstable
			// penumbras across cascade boundaries. Use PCF; weather changes the
			// illumination, not the user's chosen filter footprint.
			_key.LightAngularDistance = 0f;
			// A light exactly on the horizon casts shadows the length of the world
			// and the cascade cannot hold them, so the key is never allowed all
			// the way down. It goes dim instead, which is what sunset looks like.
			var aimed = keyDir;
			if (aimed.Y < 0.10f) aimed = new Vector3(aimed.X, 0.10f, aimed.Z).Normalized();
			_key.LookAtFromPosition(aimed * 120f, Vector3.Zero, Vector3.Up);
		}

		if (_fill != null)
			_fill.LightEnergy = Mathf.Lerp(0.065f, 0.055f, night)
				* (daylight ? Mathf.Lerp(1f, 1.22f, CloudCover) : Mathf.Lerp(1f, 0.55f, CloudCover));

		if (_env != null)
		{
			_env.AmbientLightColor = Blend(from.Ambient, to.Ambient);
			_env.AmbientLightEnergy = Lerp(from.AmbientEnergy, to.AmbientEnergy)
				* ambientExposure * ambientCloud * Mathf.Lerp(0.42f, 0.85f, night);
			_env.AmbientLightSkyContribution = Lerp(from.SkyMix, to.SkyMix)
				* Mathf.Lerp(1f, 0.72f, CloudCover);
			var fog = Blend(from.Fog, to.Fog).Lerp(new Color(.46f,.54f,.66f), RainCover * .42f);
			_env.FogLightColor = fog.LinearToSrgb();
			_env.FogLightEnergy = Mathf.Pow(0.68f, darkness)
				* (daylight ? Mathf.Lerp(1f, 0.92f, CloudCover) : Mathf.Lerp(1f, 0.56f, CloudCover));
			// Lanterns and lit windows have to clear the glow threshold at night
			// and must not at noon, or every pale surface in a pale world blooms.
			_env.GlowHdrThreshold = Lerp(from.GlowThreshold, to.GlowThreshold);
			ApplyBloom();
		}

		float skyStep = Mathf.Floor(TimeOfDay / Quantum);
		int skyCloudStep = Mathf.FloorToInt(CloudCover * 48f);
		if (_sky != null && (skyStep != _appliedSky || skyCloudStep != _appliedSkyCloud))
		{
			_appliedSky = skyStep;
			_appliedSkyCloud = skyCloudStep;
			_sky.SetShaderParameter("zenith", Palette.ShaderRgb(Blend(from.Zenith, to.Zenith)));
			_sky.SetShaderParameter("horizon", Palette.ShaderRgb(Blend(from.Horizon, to.Horizon)));
			_sky.SetShaderParameter("ground", Palette.ShaderRgb(Blend(from.Ground, to.Ground)));
			_sky.SetShaderParameter("sun_tint", Palette.ShaderRgb(sunColour));
			_sky.SetShaderParameter("sun_dir", SunDirection);
			_sky.SetShaderParameter("night", night);
			// The moon is opposite the sun and only drawn once it is up.
			_sky.SetShaderParameter("moon_dir", MoonDirection);
			_sky.SetShaderParameter("moon_tint", Palette.ShaderRgb(moonColour));
			_sky.SetShaderParameter("cloud_cover", CloudCover);
		}

		if (_water != null)
		{
			// The lake reflects the sky it is actually under.
			_water.SetShaderParameter("sky_low", Palette.ShaderRgb(Blend(from.Horizon, to.Horizon)));
			_water.SetShaderParameter("sky_high", Palette.ShaderRgb(Blend(from.Zenith, to.Zenith)));
			_water.SetShaderParameter("sun_colour", Palette.ShaderRgb(sunColour));
			_water.SetShaderParameter("moon_colour", Palette.ShaderRgb(moonColour));
			_water.SetShaderParameter("sun_dir", SunDirection);
			_water.SetShaderParameter("moon_dir", MoonDirection);
			_water.SetShaderParameter("sun_visibility", sunVisibility);
			_water.SetShaderParameter("moon_visibility", moonVisibility);
			_water.SetShaderParameter("cloud_cover", CloudCover);
		}

		RenderingServer.GlobalShaderParameterSet(SunDirParam, keyDir);
		RenderingServer.GlobalShaderParameterSet(NightParam, night);
		RenderingServer.GlobalShaderParameterSet("pf_cloud_cover", CloudCover);
		RenderingServer.GlobalShaderParameterSet("pf_cloud_daylight", 1f - night);
		RenderingServer.GlobalShaderParameterSet(SunColourParam,
			new Vector3(keyColour.R, keyColour.G, keyColour.B));
	}

	/// <summary>Applies immediately even while the developer clock is frozen.</summary>
	public void SetBloomAmount(float amount)
	{
		BloomAmount = Mathf.Clamp(amount, 0f, 2.5f);
		ApplyBloom();
	}

	/// <summary>Applies immediately even while the developer clock is frozen.</summary>
	public void SetNightDarkness(float amount)
	{
		NightDarkness = Mathf.Clamp(amount, 0f, 2.5f);
		Apply();
	}

	/// <summary>Applies immediately while frozen, including an unfiltered endpoint.</summary>
	public void SetShadowSoftness(float amount)
	{
		ShadowSoftness = Mathf.Clamp(amount, 0f, MaxShadowSoftness);
		Apply();
	}

	/// <summary>Immediately rolls a new weather pattern for the developer overlay.</summary>
	public void RandomizeClouds()
	{
		_cloudStartCover = _weatherRng.RandfRange(0.04f, 0.84f);
		ConfigureNextCloudSegment();
		_appliedSkyCloud = -1;
		Apply();
	}

	private void ApplyBloom()
	{
		if (_env == null) return;
		_env.GlowIntensity = Mathf.Lerp(0.78f, 1.08f, NightAmount) * BloomAmount;
	}

	/// <summary>Clock reading, for the developer overlay.</summary>
	public string Clock()
	{
		float hours = TimeOfDay * 24f;
		int h = Mathf.FloorToInt(hours);
		int m = Mathf.FloorToInt((hours - h) * 60f);
		return $"{h:00}:{m:00}";
	}
}
