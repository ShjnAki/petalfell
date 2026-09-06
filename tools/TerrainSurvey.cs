using System;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using Petalfell.World;

namespace Petalfell.Tools;

/// <summary>Read-only local terrain survey for permanent authored site allocation.</summary>
public partial class TerrainSurvey : Node
{
	public override void _Ready()
	{
		try
		{
			string[] args = OS.GetCmdlineUserArgs();
			if (args.Length != 2) throw new InvalidOperationException("survey requires X,Z and an output directory");
			string[] point = args[0].Split(',');
			int gx = int.Parse(point[0], CultureInfo.InvariantCulture);
			int gz = int.Parse(point[1], CultureInfo.InvariantCulture);
			string output = ProjectSettings.GlobalizePath(args[1]);
			Directory.CreateDirectory(output);
			var map = MapDefinition.Load("res://content/chapter_01/map.json");
			var bounds = AtlasRuntimeHandoff.WindowAround(map.CanonicalAtlas, gx, gz, 2);
			var window = ProductionTerrainWindow.Build(map, map.DefaultSeed, bounds).Window;
			var data = window.Data;
			const int stride = 4;
			int pixels = data.Width/stride;
			using var image = Image.CreateEmpty(pixels, pixels, false, Image.Format.Rgb8);
			var rows = new StringBuilder("x,z,top,water,cap,profile\n");
			for (int z = 0; z < data.Depth; z += stride)
			for (int x = 0; x < data.Width; x += stride)
			{
				int i = z*data.Width+x, h = window.Grid.Top[i];
				float slope = x+stride < data.Width && z+stride < data.Depth
					? Mathf.Clamp((window.Grid.Top[i+stride]-h + window.Grid.Top[i+stride*data.Width]-h)*.035f, -.28f, .28f) : 0;
				Color colour = data.WaterSurface[i] > 0 ? new Color(.22f, .42f, .68f)
					: new Color(.46f+h*.0024f, .49f+h*.0018f, .34f+h*.0030f);
				colour *= 1f+slope;
				image.SetPixel(x/stride, z/stride, colour);
				rows.AppendLine($"{data.OriginX+x},{data.OriginZ+z},{h},{data.WaterSurface[i]},{window.Grid.Cap[i]},{data.Profile[i]}");
			}
			File.WriteAllText(Path.Combine(output, "samples.csv"), rows.ToString());
			image.SavePng(Path.Combine(output, "terrain.png"));
			File.WriteAllText(Path.Combine(output, "registration.txt"),
				$"origin {data.OriginX},{data.OriginZ}; stride {stride}; size {data.Width}; focus {gx},{gz}\n");
			GD.Print($"[terrain-survey] {gx},{gz}: origin {data.OriginX},{data.OriginZ}, stride {stride}, output {output}");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PushError($"[terrain-survey] {ex}");
			GetTree().Quit(1);
		}
	}
}
