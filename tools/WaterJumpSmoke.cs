using System;
using Godot;
using Petalfell.Player;

namespace Petalfell.Tools;

/// <summary>Real controller and collision: surface launches, bank landings, roofs and input gating.</summary>
public partial class WaterJumpSmoke : Node3D
{
	public override async void _Ready()
	{
		try
		{
			foreach (string action in new[] { "move_forward", "move_back", "move_left", "move_right", "jump", "slow_walk" })
				if (!InputMap.HasAction(action)) InputMap.AddAction(action);
			foreach (int height in new[] { 1, 2, 3, 4, 6 })
				await Check(height, roof: false, disabled: false);
			await Check(3, roof: true, disabled: false);
			await Check(3, roof: false, disabled: true);
			GD.Print("[water-jump-smoke] 1–4 block banks reached; high wall, ceiling, held-key relaunch and disabled input checks passed");
			GetTree().Quit();
		}
		catch (Exception ex)
		{
			GD.PushError($"[water-jump-smoke] {ex}");
			GetTree().Quit(1);
		}
		finally
		{
			Input.ActionRelease("jump"); Input.ActionRelease("move_right");
		}
	}

	private async System.Threading.Tasks.Task Check(int height, bool roof, bool disabled)
	{
		const float water = 24f;
		var fixture = new Node3D(); AddChild(fixture);
		void Box(Vector3 centre, Vector3 size)
		{
			var body = new StaticBody3D();
			body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
			fixture.AddChild(body); body.Position = centre;
		}
		Box(new(0, 17, 0), new(80, 2, 20));
		Box(new(12, 18 + (6 + height) * .5f, 0), new(24, 6 + height, 20));
		if (roof) Box(new(-3, water + 2.5f, 0), new(6, 1, 10));
		var player = new Controller();
		player.Setup(null, (x, z) => x < 0 ? new Controller.WaterColumn(18, water) : null);
		fixture.AddChild(player); player.Position = new(-1.5f, water - .85f, 0);
		for (int i = 0; i < 20; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (!player.Swimming) throw new Exception("fixture did not start swimming");
		player.InputEnabled = !disabled;
		Input.ActionPress("move_right"); Input.ActionPress("jump");
		float apex = player.Position.Y;
		bool landed = false;
		int launches = 0;
		float previousYVelocity = 0;
		for (int i = 0; i < 180; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			apex = Mathf.Max(apex, player.Position.Y);
			if (player.Velocity.Y > 20f && previousYVelocity <= 20f) launches++;
			previousYVelocity = player.Velocity.Y;
			if (player.Position.X > .4f && player.IsOnFloor() && Math.Abs(player.Position.Y - (water + height)) < .1f)
				{ landed = true; break; }
		}
		bool reachable = height <= 4 && !roof && !disabled;
		if (landed != reachable) throw new Exception($"bank {height}, roof={roof}, disabled={disabled}: landed={landed}, position={player.Position}, apex={apex}");
		if (launches > 1 || !disabled && launches != 1) throw new Exception($"held key launched {launches} times");
		if (roof && apex > water + .30f) throw new Exception("jump crossed the solid ceiling");
		if (disabled && apex > water - .7f) throw new Exception("disabled input launched from water");
		if (!reachable && !player.Swimming) throw new Exception("blocked jump did not return to swimming");
		GD.Print($"[water-jump-smoke] bank +{height}, roof={roof}, disabled={disabled}: landed {landed}, apex {apex - water:0.00} above water, launches {launches}");
		Input.ActionRelease("jump"); Input.ActionRelease("move_right");
		fixture.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
