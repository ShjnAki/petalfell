using Godot;

namespace Petalfell.Weather;

/// <summary>Recorded stereo rain and isolated thunder; one bounded persistent sound bed.</summary>
public partial class RainAudio : Node
{
	public float Volume = .75f;
	public bool Enabled = true;
	private AudioStreamPlayer _rain, _thunder;
	private readonly RandomNumberGenerator _rng = new();
	private float _gain, _untilThunder, _thunderGain, _thunderDelay = -1;
	public override void _Ready()
	{
		_rng.Randomize();
		var loop = GD.Load<AudioStreamOggVorbis>("res://assets/audio/weather/rain-loop.ogg");
		loop.Loop = true;
		_rain = new AudioStreamPlayer { Name = "RainBed", Stream = loop, VolumeDb = -80 };
		var thunder = GD.Load<AudioStreamOggVorbis>("res://assets/audio/weather/thunder.ogg");
		thunder.Loop = false;
		_thunder = new AudioStreamPlayer { Name = "Thunder", Stream = thunder };
		AddChild(_rain); AddChild(_thunder);
		_untilThunder = _rng.RandfRange(25, 60);
	}
	public void Advance(float dt, float rain, bool sheltered, bool paused)
	{
		if (_rain == null) return;
		float target = Enabled && !paused ? rain * Volume * (sheltered ? .32f : 1f) : 0;
		_gain = Mathf.Lerp(_gain,target,1-Mathf.Exp(-dt/1.6f));
		_rain.VolumeDb = Mathf.LinearToDb(Mathf.Max(.0001f,_gain*.70f));
		if (_gain > .0003f && !_rain.Playing) _rain.Play(_rng.RandfRange(0,60));
		else if (_gain < .0002f && _rain.Playing) _rain.Stop();
		if (!Enabled || paused) { _thunder.StreamPaused = true; return; }
		_thunder.StreamPaused = false;
		_thunder.VolumeDb = Mathf.LinearToDb(Mathf.Max(.0001f,Volume*_thunderGain));
		if (_thunderDelay >= 0)
		{
			_thunderDelay -= dt;
			if (_thunderDelay < 0 && rain > .1f)
			{
				_thunderGain = rain * _rng.RandfRange(.45f,.80f);
				_thunder.VolumeDb = Mathf.LinearToDb(Mathf.Max(.0001f,Volume*_thunderGain));
				_thunder.PitchScale = _rng.RandfRange(.76f,1.02f);
				_thunder.Play();
			}
		}
		if (rain < .35f) return;
		_untilThunder -= dt * rain;
		if (_untilThunder <= 0) CueThunder();
	}
	public override void _ExitTree()
	{
		_rain?.Stop(); _thunder?.Stop();
		_rng.Dispose();
	}
	public void CueThunder()
	{
		// Distant thunder arrives after the storm event; never an abrupt ambient-loop splice.
		_thunderDelay = _rng.RandfRange(1.5f,4.5f);
		_untilThunder = _rng.RandfRange(35,100);
	}
}
