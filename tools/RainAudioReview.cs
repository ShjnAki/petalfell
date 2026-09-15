using System;
using System.Collections.Generic;
using Godot;
using Petalfell.Weather;

namespace Petalfell.Tools;

/// <summary>Record the real audio bus silently, including rain's fade and a delayed thunder event.</summary>
public partial class RainAudioReview : Node
{
	private RainAudio _audio;
	private AudioEffectCapture _capture;
	private readonly List<float> _samples=new();
	private int _bus;
	private ulong _start;
	private bool _cue;
	public override void _Ready()
	{
		_bus=AudioServer.BusCount; AudioServer.AddBus(); AudioServer.SetBusName(_bus,"WeatherReview");
		_capture=new AudioEffectCapture { BufferLength=2 };
		AudioServer.AddBusEffect(_bus,_capture); AudioServer.SetBusMute(_bus,true);
		_audio=new RainAudio(); AddChild(_audio);
		foreach(Node child in _audio.GetChildren()) if(child is AudioStreamPlayer player) player.Bus="WeatherReview";
		// Cross the prepared loop seam during this short recording.
		_audio.GetNode<AudioStreamPlayer>("RainBed").Play(76);
		_start=Time.GetTicksMsec();
	}
	public override void _Process(double delta)
	{
		float elapsed=(Time.GetTicksMsec()-_start)/1000f;
		_audio.Advance((float)delta,elapsed<15 ? .75f : 0,false,false);
		if(!_cue && elapsed>4) { _audio.CueThunder(); _cue=true; }
		foreach(Vector2 sample in _capture.GetBuffer(_capture.GetFramesAvailable()))
		{ _samples.Add(sample.X); _samples.Add(sample.Y); }
		if(elapsed<19) return;
		float peak=0; double square=0; var data=new byte[_samples.Count*2];
		for(int i=0;i<_samples.Count;i++)
		{
			float value=_samples[i]; peak=Math.Max(peak,Math.Abs(value)); square+=value*value;
			short pcm=(short)(Mathf.Clamp(value,-1,1)*32767);
			data[i*2]=(byte)(pcm&255); data[i*2+1]=(byte)((pcm>>8)&255);
		}
		double rms=Math.Sqrt(square/Math.Max(1,_samples.Count));
		if(peak<.01 || peak>=1 || _samples.Count<AudioServer.GetMixRate()*20)
		{ GD.PushError($"[rain-audio-review] invalid capture: peak {peak} RMS {rms} samples {_samples.Count}"); GetTree().Quit(1); return; }
		DirAccess.MakeDirRecursiveAbsolute("res://shots/rain-audio");
		using var wav=new AudioStreamWav { Format=AudioStreamWav.FormatEnum.Format16Bits,Stereo=true,
			MixRate=(int)AudioServer.GetMixRate(),Data=data };
		wav.SaveToWav("res://shots/rain-audio/rain-and-thunder.wav");
		GD.Print($"[rain-audio-review] recorded {_samples.Count/(2*AudioServer.GetMixRate()):F2}s stereo, peak {peak:F4}, RMS {rms:F4}; fade-in, delayed thunder, clear-weather fade-out");
		GetTree().Quit();
	}
	public override void _ExitTree() { AudioServer.RemoveBus(_bus); }
}
