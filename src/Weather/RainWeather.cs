using System;
using Godot;
using Petalfell.Core;
using Petalfell.Render;
using Petalfell.World;

namespace Petalfell.Weather;

/// <summary>One persistent rain owner across atlas windows: field, receivers and local weather light.</summary>
public partial class RainWeather : Node3D
{
	public RainField Field { get; private set; }
	public float LocalRain { get; private set; }
	public bool Paused;
	public RainAudio Audio { get; private set; }
	public RainPuddleReflection PuddleReflection { get; private set; }
	private Func<Vector3> _focus;
	private Func<AtlasSectorWindow> _window;
	private DayCycle _day;
	private AtlasSectorWindow _lastWindow;
	private Vector2I _anchor = new(int.MinValue, int.MinValue);
	private MultiMesh _drops, _splashes;
	private const int Side = 80;
	private readonly Vector2I[] _receiverCells = new Vector2I[Side*Side];
	// CPU ownership records remain available with the headless dummy renderer,
	// whose MultiMesh getters cannot return GPU instance data.
	internal readonly Vector3[] ReceiverPositions = new Vector3[Side*Side];
	internal readonly Color[] ReceiverData = new Color[Side*Side];
	private const float Spacing = 3.2f;


	public void Setup(Func<Vector3> focus, Func<AtlasSectorWindow> window, DayCycle day, ulong seed, Camera3D camera = null)
	{
		_focus = focus; _window = window; _day = day;
		Vector3 p = focus();
		Field = new RainField(seed, new Vector2(p.X, p.Z));
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		ProcessPriority = -60;
		_drops = BuildParticles("RainStreaks", "res://shaders/rain_streak.gdshader");
		_splashes = BuildParticles("RainImpacts", "res://shaders/rain_splash.gdshader");
		AddChild(new MultiMeshInstance3D { Name = "RainSpray", Multimesh = _splashes,
			MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/rain_spray.gdshader") },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
		Audio = new RainAudio { Name = "WeatherAudio" }; AddChild(Audio);
		if (camera != null) {
			PuddleReflection=new RainPuddleReflection();
			PuddleReflection.Setup(camera,SelectPuddlePlane); AddChild(PuddleReflection);
		}
		Array.Fill(_receiverCells, new Vector2I(int.MinValue,int.MinValue));
		Publish();
	}
	private MultiMesh BuildParticles(string name, string shader)
	{
		var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseCustomData = true, Mesh = new QuadMesh { Size = Vector2.One }, InstanceCount = Side * Side };
		var mesh = new MultiMeshInstance3D { Name = name, Multimesh = mm,
			MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>(shader) },
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(new Vector3(-140, -256, -140), new Vector3(280, 600, 280)) };
		AddChild(mesh); return mm;
	}
	public override void _Process(double delta)
	{
		if (Field == null) return;
		Vector3 focus = _focus();
		if (!Paused) Field.Advance(delta, new Vector2(focus.X, focus.Z));
		LocalRain = Field.Sample(new Vector2(focus.X, focus.Z));
		_day.SetRainCover(LocalRain);
		if (Atmosphere.LastEnvironment != null)
		{
			bool wet = false;
			foreach (var cell in Field.Cells)
				wet |= cell.Wetness > .005f;
			if (Atmosphere.LastEnvironment.SsrEnabled != wet) Atmosphere.LastEnvironment.SsrEnabled = wet;
		}
		Publish();
		Audio.Advance((float)delta, LocalRain, IsSheltered(focus), Paused);
		var anchor = new Vector2I(Mathf.FloorToInt(focus.X / Spacing), Mathf.FloorToInt(focus.Z / Spacing));
		AtlasSectorWindow window = _window();
		if (_drops != null && window != null && (anchor != _anchor || window != _lastWindow))
		{
			if (window != _lastWindow) Array.Fill(_receiverCells, new Vector2I(int.MinValue,int.MinValue));
			_anchor = anchor; _lastWindow = window;
			RefreshReceivers(window, anchor);
		}
	}
	private float? SelectPuddlePlane(Camera3D camera)
		=> SelectVisiblePuddlePlane(camera, _window(), Field);

	internal static float? SelectVisiblePuddlePlane(Camera3D camera, AtlasSectorWindow w, RainField field)
	{
		if (w == null || camera == null) return null;
		// Fixed screen-space samples cover the lens at every zoom. Each ray stops
		// at its first terrain/water receiver; player proximity never gates paving.
		var levels = new int[w.Grid.Height + 1];
		Vector2 size = camera.GetViewport().GetVisibleRect().Size;
		for (int sy = 0; sy < 24; sy++) for (int sx = 0; sx < 40; sx++)
		{
			Vector2 pixel = size * new Vector2((sx + .5f) / 40, (sy + .5f) / 24);
			Vector3 origin = camera.ProjectRayOrigin(pixel), ray = camera.ProjectRayNormal(pixel);
			if (ray.Y >= -.01f) continue;
			float begin = Math.Max(camera.Near, (w.Grid.Height - origin.Y) / ray.Y);
			float end = Math.Min(camera.Far, -origin.Y / ray.Y);
			// At most 768 queries per ray, independent of world size and zoom.
			float step = Math.Max(2f, (end - begin) / 767f);
			for (int probe = 0; probe < 768; probe++)
			{
				float distance = begin + probe * step;
				if (distance > end) break;
				Vector3 p = origin + ray * distance;
				int x = Mathf.FloorToInt(p.X) - w.Data.OriginX, z = Mathf.FloorToInt(p.Z) - w.Data.OriginZ;
				if (x < 0 || z < 0 || x >= w.Data.Width || z >= w.Data.Depth) continue;
				int h = w.Grid.MeshHeightAt(x,z);
				int water = w.Data.WaterSurface[z*w.Data.Width+x];
				if (p.Y > Math.Max(h,water)) continue;
				if (h > 0 && h < levels.Length && water <= h &&
					Palette.Get(w.Grid.At(x,h-1,z)).Pattern == Palette.PatternRock &&
					field.Sample(new Vector2(p.X,p.Z),true) >= .03f) levels[h]++;
				break;
			}
		}
		int selected = 0;
		for (int h = 1; h < levels.Length; h++) if (levels[h] > levels[selected]) selected = h;
		return levels[selected] >= 2 ? selected : null;
	}
	private bool IsSheltered(Vector3 p)
	{
		var w = _window();
		if (w == null) return false;
		int x = Mathf.FloorToInt(p.X)-w.Data.OriginX, z = Mathf.FloorToInt(p.Z)-w.Data.OriginZ;
		return x >= 0 && z >= 0 && x < w.Data.Width && z < w.Data.Depth && w.Grid.MeshHeightAt(x,z) > p.Y + 7;
	}
	private void Publish()
	{
		RenderingServer.GlobalShaderParameterSet("pf_rain_time", (float)Field.Time);
		for (int i = 0; i < RainField.CellCount; i++)
		{
			RenderingServer.GlobalShaderParameterSet($"pf_rain_{i}", Field.Cells[i].RainUniform);
			RenderingServer.GlobalShaderParameterSet($"pf_wet_{i}", Field.Cells[i].WetUniform);
		}
	}
	private static float Hash(int x, int z, uint salt)
	{
		uint h = unchecked((uint)x * 374761393u + (uint)z * 668265263u + salt);
		h = (h ^ (h >> 13)) * 1274126177u;
		return ((h ^ (h >> 16)) & 16777215u) / 16777216f;
	}
	private void RefreshReceivers(AtlasSectorWindow w, Vector2I anchor)
	{
		// Instance index is a wrapping world lattice; moving the coverage only
		// relocates the offscreen fringe, never resets drops in the visible centre.
		for (int z = anchor.Y - Side/2; z < anchor.Y + Side/2; z++)
		for (int x = anchor.X - Side/2; x < anchor.X + Side/2; x++)
		{
			int slot = Mathf.PosMod(z, Side) * Side + Mathf.PosMod(x, Side);
			var cell = new Vector2I(x,z);
			if (_receiverCells[slot] == cell) continue;
			_receiverCells[slot] = cell;
			float px = (x + .08f + Hash(x,z,31)*.84f)*Spacing;
			float pz = (z + .08f + Hash(x,z,89)*.84f)*Spacing;
			int lx = Mathf.FloorToInt(px) - w.Data.OriginX, lz = Mathf.FloorToInt(pz) - w.Data.OriginZ;
			bool valid = lx >= 0 && lz >= 0 && lx < w.Data.Width && lz < w.Data.Depth;
			float level = -500;
			bool water = false; bool grass = false;
			if (valid)
			{
				int h = w.Grid.MeshHeightAt(lx,lz);
				while (h > 0 && !w.Grid.SolidAt(lx,h-1,lz)) h--;
				float surface = w.Data.WaterSurface[lz*w.Data.Width+lx];
				water = surface > h;
				level = water ? surface + .35f : h;
				grass = h > 0 && Palette.IsGrassSurface(w.Grid.At(lx,h-1,lz));
			}
			var transform = new Transform3D(Basis.Identity, new Vector3(px, level + .025f, pz));
			var data = new Color(Hash(x,z,131), valid ? 1 : 0, water ? 1 : grass ? .5f : 0, Hash(x,z,751));
			ReceiverPositions[slot] = transform.Origin; ReceiverData[slot] = data;
			_drops.SetInstanceTransform(slot, transform); _drops.SetInstanceCustomData(slot, data);
			_splashes.SetInstanceTransform(slot, transform); _splashes.SetInstanceCustomData(slot, data);
		}
		// Bounds follow global positions stored directly in the instance transforms.
		foreach (Node child in GetChildren()) if (child is MultiMeshInstance3D mesh)
			mesh.CustomAabb = new Aabb(new Vector3(anchor.X*Spacing-140, -10, anchor.Y*Spacing-140), new Vector3(280, w.Grid.Height + 64, 280));
	}
	public override void _ExitTree() => Field?.Dispose();
	public void Preview(float amount, float radius = 900)
	{
		Vector3 p = _focus(); Field.Preview(new Vector2(p.X,p.Z),amount,radius,amount > 0 ? 1 : 0); Publish();
	}
}
