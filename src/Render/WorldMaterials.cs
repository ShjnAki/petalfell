using Godot;
using Petalfell.Core;

namespace Petalfell.Render;

/// <summary>
/// Shared construction of the materials that define the finished world look.
/// Runtime sector review must use the same shaders and starting parameters as
/// ordinary play or it is only a second preview renderer with different truths.
/// </summary>
public static class WorldMaterials
{
	public readonly record struct InkSet(ShaderMaterial Light, ShaderMaterial Dark);
	private static Texture2D _mineralDetail;

	private static Texture2D MineralDetail()
	{
		if (_mineralDetail != null) return _mineralDetail;
		var imported = GD.Load<Texture2D>("res://assets/materials/mineral-detail.png");
		using var pixels = imported.GetImage();
		// Texture imports are disposable/ignored in this repository. Do not depend
		// on an editor having detected 3D use and enabled mipmaps on this machine.
		if (pixels.HasMipmaps()) return _mineralDetail = imported;
		if (pixels.IsCompressed() && pixels.Decompress() != Error.Ok)
			throw new System.InvalidOperationException("Cannot decompress the mineral detail source.");
		if (pixels.GenerateMipmaps() != Error.Ok)
			throw new System.InvalidOperationException("Cannot generate mineral detail mipmaps.");
		_mineralDetail = ImageTexture.CreateFromImage(pixels);
		GD.Print($"[world-materials] mineral detail {pixels.GetWidth()}x{pixels.GetHeight()}, {pixels.GetMipmapCount()} mip levels");
		return _mineralDetail;
	}

	public static InkSet CreateInk(float waterLevel, int priorityOffset = 0)
	{
		var shader = GD.Load<Shader>("res://shaders/ink.gdshader");
		ShaderMaterial Ink(int priority, int pass)
		{
			var material = new ShaderMaterial { Shader = shader, RenderPriority = priority };
			material.SetShaderParameter("ink_dark", Palette.InkDark);
			material.SetShaderParameter("ink_light", Palette.InkLight);
			material.SetShaderParameter("core_width", Palette.InkWidth);
			material.SetShaderParameter("ink_pass", pass);
			material.SetShaderParameter("water_level", waterLevel);
			return material;
		}

		var light = Ink(1 + priorityOffset, 0);
		var dark = Ink(2 + priorityOffset, 2);
		light.NextPass = Ink(3 + priorityOffset, 1);
		return new InkSet(light, dark);
	}

	public static ShaderMaterial CreateVoxel(float waterLevel)
	{
		var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/voxel.gdshader") };
		material.SetShaderParameter("mineral_detail", MineralDetail());
		var moss = Palette.Get(Palette.MOSS_STONE);
		var stone = Palette.Get(Palette.STONE_PALE);
		static Vector3 Ratio(Color substrate, Color coating) =>
			new(substrate.R / coating.R, substrate.G / coating.G, substrate.B / coating.B);
		material.SetShaderParameter("moss_substrate_top_ratio", Ratio(stone.Top, moss.Top));
		material.SetShaderParameter("moss_substrate_side_ratio", Ratio(stone.Side, moss.Side));
		material.SetShaderParameter("sun_dir", Palette.SunDir);
		material.SetShaderParameter("plane_y", waterLevel);
		return material;
	}

	public static ShaderMaterial CreateDetail() =>
		new() { Shader = GD.Load<Shader>("res://shaders/detail.gdshader") };

	public static ShaderMaterial CreateSculptureStone(Color colour,
		float weathering = 0.62f, float mossMix = 0f)
	{
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://shaders/sculpture.gdshader"),
		};
		material.SetShaderParameter("base_colour", Palette.ShaderRgba(colour));
		material.SetShaderParameter("mineral_detail", MineralDetail());
		material.SetShaderParameter("weathering", weathering);
		material.SetShaderParameter("moss_mix", mossMix);
		material.SetShaderParameter("moss_colour", Palette.ShaderRgba(Palette.Get(Palette.MOSS_STONE).Top));
		return material;
	}

	public static ShaderMaterial CreateSculptureOutline(float widthPixels = 0.50f)
	{
		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://shaders/sculpture_outline.gdshader"),
			RenderPriority = 1,
		};
		material.SetShaderParameter("ink_colour", Palette.InkDark);
		material.SetShaderParameter("outline_pixels", widthPixels);
		return material;
	}

	public static ShaderMaterial CreateWaterDetail() =>
		new() { Shader = GD.Load<Shader>("res://shaders/waterdetail.gdshader") };

	public static ShaderMaterial CreateWater(float waterLevel, bool surfaceFromMesh = false,
		bool reflectionAvailable = true)
	{
		var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/water.gdshader") };
		material.SetShaderParameter("shoal", Palette.ShaderRgb(Palette.WaterShoal));
		material.SetShaderParameter("shallow", Palette.ShaderRgb(Palette.WaterShallow));
		material.SetShaderParameter("deep", Palette.ShaderRgb(Palette.WaterDeep));
		material.SetShaderParameter("warm", Palette.ShaderRgb(Palette.WaterWarm));
		material.SetShaderParameter("marsh_shoal", Palette.ShaderRgb(Palette.WaterMarshShoal));
		material.SetShaderParameter("marsh_deep", Palette.ShaderRgb(Palette.WaterMarshDeep));
		material.SetShaderParameter("sheen", Palette.ShaderRgb(Palette.WaterSheen));
		material.SetShaderParameter("sky_low", Palette.ShaderRgb(Palette.SkyHorizon));
		material.SetShaderParameter("sky_high", Palette.ShaderRgb(Palette.SkyZenith));
		material.SetShaderParameter("sun_colour", Palette.ShaderRgb(Palette.SunColor));
		material.SetShaderParameter("sun_dir", Palette.SunDir);
		material.SetShaderParameter("plane_y", waterLevel);
		material.SetShaderParameter("surface_from_mesh", surfaceFromMesh);
		// Atlas water differs only in where its surface position comes from. Its
		// multi-height tops and step curtains still use the legacy absorption,
		// refraction, caustics and moving-sheet response; overriding those values
		// here once collapsed every column into one opaque periwinkle stop.
		// Start with sky until the bounded atlas reflection controller has selected
		// a visible water elevation and supplied its mirrored viewport.
		if (!reflectionAvailable) material.SetShaderParameter("reflect_mix", 0f);
		return material;
	}

	public static CanvasLayer CreateGrade()
	{
		var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/grade.gdshader") };
		material.SetShaderParameter("lift", Palette.GradeLift);
		material.SetShaderParameter("gamma_", Palette.GradeGamma);
		material.SetShaderParameter("gain", Palette.GradeGain);
		material.SetShaderParameter("saturation", Palette.GradeSaturation);
		material.SetShaderParameter("contrast", Palette.GradeContrast);
		material.SetShaderParameter("vignette", Palette.GradeVignette);

		var rect = new ColorRect
		{
			Material = material,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		var layer = new CanvasLayer { Name = "Grade", Layer = 100 };
		layer.AddChild(rect);
		return layer;
	}
}
