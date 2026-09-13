using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Petalfell.Player;
using Petalfell.Render;

namespace Petalfell.Tools;

/// <summary>Real capsule sweeps through complete flights, with the production leg rig.</summary>
public partial class StairWalkSmoke : Node3D
{
	private Controller _player;
	private Character _character;
	private string _shots;
	private Camera3D _camera;
	private Vector3 _presented;
	private float _renderError;
	private int _renderSamples;
	private bool _checkRendering, _measureRendering, _axisAlignedInput;
	private void CheckRenderedPosition()
	{
		if (_player == null || !GodotObject.IsInstanceValid(_player) || !_measureRendering) return;
		// Read the transform after Godot has composed inherited interpolation.
		// Checking GlobalPosition in _Process alone misses the rendered jump.
		_renderError = Math.Max(_renderError,
			_character.GetGlobalTransformInterpolated().Origin.DistanceTo(_presented));
		_renderSamples++;
	}
	public override void _ExitTree() => RenderingServer.FramePostDraw -= CheckRenderedPosition;

	public override void _Process(double delta)
	{
		if (_player == null || !GodotObject.IsInstanceValid(_player)) return;
		_presented = _player.AdvancePresentation(delta);
		_character.GlobalPosition = _presented;
		_character.Animate(_player.Velocity, _player.Facing, _player.IsOnFloor(), _player.Swimming, delta);
		if (_camera != null)
		{
			Vector3 focus = _character.GlobalPosition + Vector3.Up * 1.5f;
			_camera.GlobalPosition = focus + new Vector3(_axisAlignedInput ? 0 : 6, 4, 9);
			_camera.LookAt(focus);
		}
	}

	public override async void _Ready()
	{
		try
		{
			RenderingServer.FramePostDraw += CheckRenderedPosition;
			_checkRendering = Array.IndexOf(OS.GetCmdlineUserArgs(), "--motion-check") >= 0;
			DayCycle.RegisterGlobals();
			(_shots, _) = Capture.ParseArgs();
			CheckRestingBoots();
			foreach (string action in new[] { "move_forward", "move_back", "move_left", "move_right", "jump", "slow_walk" })
				if (!InputMap.HasAction(action)) InputMap.AddAction(action);
			if (_shots != null || _checkRendering)
			{
				AddChild(Atmosphere.Build());
				Atmosphere.LastEnvironment.FogEnabled = false;
				Atmosphere.LastEnvironment.VolumetricFogEnabled = false;
				AddChild(Atmosphere.Sun()); AddChild(Atmosphere.Fill());
				_camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 8f, Current = true };
				AddChild(_camera);
			}
			if (_shots != null)
			{
				await Flight(.5f, false);
				await Flight(.5f, true);
				GetTree().Quit();
				return;
			}
			if (_checkRendering) await Flight(0f, false);
			foreach (float rise in new[] { .25f, .5f, 1f })
			{
				await Flight(rise, false);
				await Flight(rise, true);
			}
			await Flight(1f, false, fullSpeed: true);
			await Flight(1f, true, fullSpeed: true);
			await Flight(2f, false);
			await Flight(1.25f, false);
			await Flight(2f, true);
			await Flight(.5f, false, Mathf.Pi / 4f, new Vector3(6400, 24, 7360));
			await Flight(1f, true, Mathf.Pi / 4f, new Vector3(6400, 24, 7360));
			await Guard(2.50f, manual: false, disabled: false);
			await Guard(1.95f, manual: false, disabled: false);
			await Guard(null, manual: true, disabled: false);
			await Guard(null, manual: true, disabled: true);
			GD.Print("[stair-walk-smoke] complete low flights up/down and taller auto-hop passed");
			GetTree().Quit();
		}
		catch (Exception error)
		{
			GD.PushError($"[stair-walk-smoke] {error}");
			GetTree().Quit(1);
		}
	}

	private void CheckRestingBoots()
	{
		var character = new Character(); AddChild(character);
		var ink = WorldMaterials.CreateInk(24); character.Setup(ink.Light, ink.Dark);
		// Check actual mesh corners after animation at several ground datums and
		// stopped phases. This catches buried soles without changing collision.
		float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
		foreach (float ground in new[] { 0f, 24f, 72f })
		{
			character.GlobalPosition = new Vector3(6400, ground, 7360);
			for (int phase = 0; phase < 24; phase++)
			{
				character.Animate(Vector3.Right * Controller.MaxSpeed, Vector3.Right, true, false, .10);
				for (int settle = 0; settle < 90; settle++)
					character.Animate(Vector3.Zero, Vector3.Right, true, false, 1.0 / 60);
				foreach (string name in new[] { "LeftBoot", "RightBoot" })
				{
					var boot = (MeshInstance3D)character.FindChild(name, recursive: true, owned: false);
					Aabb bounds = boot.GetAabb();
					float bottom = float.PositiveInfinity;
					for (int corner = 0; corner < 8; corner++)
						bottom = Math.Min(bottom, boot.ToGlobal(bounds.GetEndpoint(corner)).Y - ground);
					minimum = Math.Min(minimum, bottom); maximum = Math.Max(maximum, bottom);
				}
			}
		}
		character.Free();
		if (minimum < -.001f || maximum > .03f)
			throw new Exception($"resting boots miss ground: {minimum}..{maximum}");
		GD.Print($"[boot-ground-smoke] 144 resting soles at Y0/24/72: {minimum:0.000}..{maximum:0.000} above support");
	}

	private async Task Guard(float? roofBottom, bool manual, bool disabled)
	{
		_axisAlignedInput = true;
		var fixture = new Node3D(); AddChild(fixture);
		Box(fixture, new Vector3(-5, -.5f, 0), new Vector3(10, 1, 6));
		Box(fixture, new Vector3(10, .25f, 0), new Vector3(20, .5f, 6));
		if (roofBottom.HasValue)
			Box(fixture, new Vector3(1, roofBottom.Value + .25f, 0), new Vector3(12, .5f, 6));
		_player = new Controller();
		_player.Setup(null, (_, _) => null);
		fixture.AddChild(_player); _player.Position = new Vector3(-1.5f, 0, 0);
		_character = new Character(); _player.AddChild(_character);
		var ink = WorldMaterials.CreateInk(24); _character.Setup(ink.Light, ink.Dark);
		for (int i = 0; i < 20; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		_player.InputEnabled = !disabled;
		Input.ActionPress("move_right"); Input.ActionPress("slow_walk");
		if (manual) Input.ActionPress("jump");
		float maxY = 0, up = 0;
		for (int i = 0; i < 100; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			maxY = Math.Max(maxY, _player.Position.Y);
			up = Math.Max(up, _player.Velocity.Y);
			if (i == 6) Input.ActionRelease("jump");
		}
		bool clear = !disabled && (!roofBottom.HasValue || roofBottom > 2.25f);
		if ((_player.Position.X > .4f) != clear) throw new Exception($"roof/input guard failed: roof={roofBottom}, disabled={disabled}, at={_player.Position}");
		if (roofBottom.HasValue && maxY + 1.75f > roofBottom.Value + .015f) throw new Exception("step penetrated ceiling");
		if (manual && !disabled && up < 15f) throw new Exception("low-step assist swallowed manual jump");
		if (disabled && up > 0) throw new Exception("input gate allowed manual jump");
		// Stopping on a tread must not retain a forced crossing vector.
		_player.StopTravel(); _player.InputEnabled = false;
		Vector3 stopped = _player.Position;
		for (int i = 0; i < 30; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (Math.Abs(_player.Position.X - stopped.X) > .02f) throw new Exception("step kept travelling after StopTravel");
		GD.Print($"[stair-guard-smoke] roof {roofBottom}, manual={manual}, disabled={disabled}: clear={clear}, up={up:0.00}; stop retained position");
		Input.ActionRelease("move_right"); Input.ActionRelease("slow_walk"); Input.ActionRelease("jump");
		_measureRendering = false;
		_player = null; _character = null;
		fixture.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private async Task Flight(float rise, bool descending, float yaw = 0, Vector3 origin = default, bool fullSpeed = false)
	{
		_axisAlignedInput = fullSpeed;
		const int treads = 6;
		const float run = 1.5f;
		var fixture = new Node3D(); AddChild(fixture);
		fixture.Position = origin; fixture.Rotation = new Vector3(0, yaw, 0);
		Box(fixture, new Vector3(5, -.5f, 0), new Vector3(30, 1, 6));
		for (int i = 0; rise > 0 && i < treads; i++)
			Box(fixture, new Vector3((i + .5f) * run, rise * (i + 1) * .5f, 0),
				new Vector3(run, rise * (i + 1), 4));
		if (rise > 0) Box(fixture, new Vector3(12, rise * treads * .5f, 0), new Vector3(6, rise * treads, 4));
		_player = new Controller { RouteSpeed = Controller.SlowWalkSpeed };
		_player.Setup(null, (_, _) => null);
		fixture.AddChild(_player);
		_player.Position = descending ? new Vector3(10, rise * treads, 0) : new Vector3(-1.5f, 0, 0);
		_player.ResetPhysicsInterpolation();
		_character = new Character();
		_player.AddChild(_character);
		var ink = WorldMaterials.CreateInk(24);
		_character.Setup(ink.Light, ink.Dark);
		for (int i = 0; i < 20; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		var route = new List<Vector3>();
		if (descending)
		{
			for (int i = treads - 1; i >= 0; i--) route.Add(new Vector3((i + .5f) * run, rise * (i + 1), 0));
			route.Add(new Vector3(-1.5f, 0, 0));
		}
		else
		{
			for (int i = 0; i < treads; i++) route.Add(new Vector3((i + .5f) * run, rise * (i + 1), 0));
			route.Add(new Vector3(10, rise * treads, 0));
		}
		_renderError = 0; _renderSamples = 0; _measureRendering = true;
		_player.SetRoute(route.ConvertAll(fixture.ToGlobal));
		if (fullSpeed) Input.ActionPress(descending ? "move_left" : "move_right");
		float upward = 0, visualDelta = 0;
		string clip = $"{_shots}/step-{rise:0.00}-{(descending ? "down" : "up")}";
		StreamWriter metadata = null;
		int captured = 0;
		if (_shots != null)
		{
			DirAccess.MakeDirRecursiveAbsolute(clip);
			metadata = new StreamWriter(ProjectSettings.GlobalizePath($"{clip}/height.csv"));
			metadata.WriteLine("frame,physics_y,visual_y");
		}
		async Task Frame()
		{
			await RenderingServer.Singleton.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			Capture.Save(GetViewport(), clip, $"frame-{captured++:D4}", quiet: true);
			metadata.WriteLine(FormattableString.Invariant($"{captured},{_player.GlobalPosition.Y},{_character.GlobalPosition.Y}"));
		}
		float lastVisual = _character.GlobalPosition.Y;
		int airFrames = 0;
		for (int frame = 0; frame < 900 && (fullSpeed ? (descending ? _player.Position.X > -1f : _player.Position.X < 10f) : _player.Route != null); frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			upward = Math.Max(upward, _player.Velocity.Y);
			if (!_player.IsOnFloor()) airFrames++;
			visualDelta = Math.Max(visualDelta, Math.Abs(_character.GlobalPosition.Y - lastVisual));
			lastVisual = _character.GlobalPosition.Y;
			if (_shots != null) await Frame();
		}
		if (_shots != null)
		{
			for (int i = 0; i < 24; i++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				await Frame();
			}
			metadata.Dispose();
			GD.Print($"[stair-capture] {clip}: {captured} frames");
		}
		if (fullSpeed)
		{
			Input.ActionRelease("move_left"); Input.ActionRelease("move_right");
			if (descending ? _player.Position.X > -1f : _player.Position.X < 10f) throw new Exception("full-speed flight did not finish");
		}
		if (!fullSpeed && _player.Route != null) throw new Exception($"flight {rise}/{descending} stuck at {_player.Position}, waypoint {_player.RouteIndex}, hops={upward}");
		if (rise <= 1f && (upward > .1f || airFrames > 2))
			throw new Exception($"low flight {rise}/{descending} hopped: upward={upward}, air={airFrames}");
		if (rise > 1f && !descending && upward < 10f) throw new Exception("tall ledge no longer auto-jumps");
		if (rise == 2f && descending && airFrames < 10) throw new Exception("large drop was snapped like a low tread");
		if (rise > 0 && rise <= 1f && _player.LowStepCount < treads) throw new Exception("flight did not register each tread");
		GD.Print($"[stair-walk-smoke] rise {rise}, down={descending}, fullSpeed={fullSpeed}: steps {_player.LowStepCount}, air {airFrames}, max upward {upward:0.00}, visual delta {visualDelta:0.000}");
		GD.Print($"[stair-render-smoke] {rise}/{descending}: {_renderSamples} rendered samples, error {_renderError:0.0000}");
		if (_checkRendering && (_renderSamples < 20 || _renderError > .005f))
			throw new Exception($"rendered character diverged from presentation: samples={_renderSamples}, error={_renderError}");
		_measureRendering = false;
		_player = null; _character = null;
		fixture.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private void Box(Node3D parent, Vector3 at, Vector3 size)
	{
		var body = new StaticBody3D();
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
		parent.AddChild(body); body.Position = at;
		if (_shots != null || _checkRendering) body.AddChild(new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(.65f, .64f, .58f), Roughness = .9f },
		});
	}
}
