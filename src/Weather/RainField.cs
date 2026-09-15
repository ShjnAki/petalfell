using System;
using Godot;

namespace Petalfell.Weather;

/// <summary>Bounded world-space weather. Random weather never consumes terrain randomness.</summary>
public sealed class RainField : IDisposable
{
	public const int CellCount = 3;
	public const float CoreFraction = .62f;
	public sealed class Cell
	{
		public Vector2 Centre;
		public float Radius, Peak, Age, Delay, Rise, Hold, Fall, Rest, Wetness;
		public float Rain => Peak * Envelope(Age - Delay, Rise, Hold, Fall);
		public Vector4 RainUniform => new(Centre.X, Centre.Y, Radius, Rain);
		public Vector4 WetUniform => new(Centre.X, Centre.Y, Radius, Wetness);
	}
	public readonly Cell[] Cells = new Cell[CellCount];
	private readonly RandomNumberGenerator _rng = new();
	public double Time { get; private set; }
	public bool Automatic = true;
	public RainField(ulong seed, Vector2 initialFocus)
	{
		_rng.Seed = seed;
		for (int i = 0; i < CellCount; i++)
		{
			Cells[i] = new Cell();
			Renew(Cells[i], initialFocus, i == 0);
			Cells[i].Delay += i * 70;
		}
	}
	public static float Smooth(float x) { x = Mathf.Clamp(x, 0, 1); return x * x * (3 - 2 * x); }
	public static float Envelope(float t, float rise, float hold, float fall)
		=> Smooth(t / rise) * (1 - Smooth((t - rise - hold) / fall));
	public static float Footprint(Vector2 p, Vector2 centre, float radius)
		=> 1 - Smooth((p.DistanceTo(centre) / Math.Max(radius, 1) - CoreFraction) / (1 - CoreFraction));
	public float Sample(Vector2 p, bool wet = false)
	{
		float amount = 0;
		foreach (Cell c in Cells) amount = Math.Max(amount, Footprint(p,c.Centre,c.Radius) * (wet ? c.Wetness : c.Rain));
		return amount;
	}
	public void Advance(double delta, Vector2 focus)
	{
		Time += delta;
		// Substeps keep drying and renewal stable across slow frames/fast test clocks.
		while (delta > 0)
		{
			float dt = (float)Math.Min(delta, 1); delta -= dt;
			foreach (Cell c in Cells)
			{
				if (Automatic) c.Age += dt;
				float target = Mathf.Clamp(c.Rain * 1.5f, 0, 1);
				c.Wetness = Mathf.Lerp(c.Wetness, target, 1 - Mathf.Exp(-dt / (target > c.Wetness ? 28 : 125)));
				if (Automatic && c.Age > c.Delay + c.Rise + c.Hold + c.Fall + c.Rest && c.Wetness < .003f)
					Renew(c, focus, _rng.Randf() < .65f);
			}
		}
	}
	private void Renew(Cell c, Vector2 focus, bool nearby)
	{
		c.Radius = _rng.RandfRange(620, 1450);
		c.Centre = nearby ? focus + Vector2.FromAngle(_rng.Randf() * Mathf.Tau) * c.Radius * .3f
			: new Vector2(_rng.RandfRange(0, 12288), _rng.RandfRange(0, 9216));
		c.Peak = _rng.RandfRange(.60f, .80f);
		c.Age = 0; c.Wetness = 0;
		c.Delay = _rng.RandfRange(20, 80); c.Rise = _rng.RandfRange(45, 95);
		c.Hold = _rng.RandfRange(180, 420); c.Fall = _rng.RandfRange(65, 125);
		c.Rest = _rng.RandfRange(600, 900);
	}
	public void Resume()
	{
		Automatic = true;
		// Resume existing cells so neither rain nor residual wetness jumps.
		foreach (Cell c in Cells)
			if (c.Peak == 0) c.Age = c.Delay + c.Rise + c.Hold + c.Fall + c.Rest;
	}
	public void Dispose() => _rng.Dispose();
	public void Preview(Vector2 centre, float intensity, float radius = 900, float wetness = 1)
	{
		Automatic = false;
		foreach (Cell c in Cells) { c.Peak = 0; c.Wetness = 0; }
		Cell first = Cells[0]; first.Centre = centre; first.Radius = radius;
		first.Peak = Mathf.Clamp(intensity, 0, 1); first.Wetness = wetness;
		first.Age = first.Delay + first.Rise + 1;
	}
}
