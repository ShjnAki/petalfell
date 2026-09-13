using System;
using Godot;

namespace Petalfell.Player;

public partial class Controller
{
	/// <summary>One voxel tread is a walking step; two-block terraces still hop.</summary>
	public const float LowStepHeight = 1.05f;
	public int LowStepCount { get; private set; }
	public float LastLowStepRise { get; private set; }
	private bool _stepSmoothing, _presentationInitialized;
	private float _presentationY;
	private Vector3 _lastPresentationSource;
	private readonly KinematicCollision3D _stepHit = new();
	private bool _lowStepPrepared;

	private void PrepareLowStep(ref Vector3 velocity, float dt)
	{
		_lowStepPrepared = false;
		Vector3 flat = new(velocity.X, 0, velocity.Z);
		if (flat.LengthSquared() < .001f) return;
		Vector3 motion = flat * dt;
		Transform3D from = GlobalTransform;
		if (FindLowStep(from, motion, out float rise))
		{
			// The entire up/forward/down capsule path has been swept. MoveAndSlide
			// owns this frame's horizontal travel and establishes the new support.
			GlobalPosition += Vector3.Up * rise;
			_lowStepPrepared = true;
		}
		velocity.X = flat.X;
		velocity.Z = flat.Z;
	}

	private bool FindLowStep(Transform3D from, Vector3 motion, out float rise)
	{
		rise = 0f;
		if (!TestMove(from, motion, _stepHit)) return false;
		if (_stepHit.GetNormal().Y > .99f) return false;
		Vector3 lift = Vector3.Up * (LowStepHeight + .04f);
		if (TestMove(from, lift, _stepHit))
		{
			// Use the available headroom, not the maximum theoretical step lift.
			// A low roof can permit a half tread without permitting a whole one.
			lift.Y = Mathf.Max(0, _stepHit.GetTravel().Y - .005f);
			if (lift.Y < .01f) return false;
		}
		Transform3D raised = from;
		raised.Origin += lift;
		if (TestMove(raised, motion)) return false;
		raised.Origin += motion;
		if (!TestMove(raised, -lift - Vector3.Up * .04f, _stepHit) ||
			_stepHit.GetNormal().Y < .09f) return false;
		// The capsule can touch a high corner while its feet are lower than the
		// actual tread. Classify the surface itself, not that rounded contact pose.
		if (_stepHit.GetPosition().Y - from.Origin.Y > LowStepHeight + .005f) return false;
		rise = raised.Origin.Y + _stepHit.GetTravel().Y - from.Origin.Y;
		if (rise <= .001f || rise > LowStepHeight) return false;
		// The broad elevated sweep can clear a second riser. The actual lower
		// crossing must also be clear, otherwise we would cut through that riser.
		raised = from;
		raised.Origin.Y += rise;
		return !TestMove(raised, motion, _stepHit) || _stepHit.GetNormal().Y > .09f;
	}

	private void SnapLowStepDown()
	{
		if (IsOnFloor() || Velocity.Y > 0f) return;
		if (!TestMove(GlobalTransform, Vector3.Down * LowStepHeight, _stepHit) ||
			_stepHit.GetNormal().Y < .09f) return;
		float oldSnap = FloorSnapLength;
		float oldAngle = FloorMaxAngle;
		FloorSnapLength = LowStepHeight;
		FloorMaxAngle = Mathf.DegToRad(85f);
		ApplyFloorSnap();
		FloorSnapLength = oldSnap;
		FloorMaxAngle = oldAngle;
	}

	private void RecordLowStep(float previousY, float rise)
	{
		if (!_presentationInitialized)
		{
			_presentationY = previousY;
			_presentationInitialized = true;
		}
		_stepSmoothing = true;
		LastLowStepRise = rise;
		LowStepCount++;
	}

	/// <summary>One render-frame position shared by the model and follow camera.</summary>
	public Vector3 AdvancePresentation(double delta)
	{
		Vector3 source = GetGlobalTransformInterpolated().Origin;
		if (!_presentationInitialized || source.DistanceSquaredTo(_lastPresentationSource) > 16f)
		{
			_presentationY = source.Y;
			_stepSmoothing = false;
			_presentationInitialized = true;
		}
		_lastPresentationSource = source;
		if (_stepSmoothing)
		{
			_presentationY = Mathf.Lerp(_presentationY, source.Y, 1f - Mathf.Exp(-14f * (float)delta));
			if (Math.Abs(_presentationY - source.Y) < .001f)
			{
				_presentationY = source.Y;
				_stepSmoothing = false;
			}
		}
		else _presentationY = source.Y;
		return new Vector3(source.X, _presentationY, source.Z);
	}
}
