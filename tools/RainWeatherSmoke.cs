using System;
using Godot;
using Petalfell.Weather;
using Petalfell.Render;
using Petalfell.Core;
using Petalfell.World;

namespace Petalfell.Tools;

public partial class RainWeatherSmoke : Node
{
	private static void Require(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); }
	public override async void _Ready()
	{
		try
		{
			var centre = new Vector2(6400,7360);
			using var field = new RainField(42,centre);
			field.Preview(centre,.7f,1000);
			Require(Math.Abs(field.Sample(centre)-.7f)<.00001f,"core intensity");
			Require(Math.Abs(field.Sample(centre+new Vector2(610,0))-.7f)<.00001f,"core is not flat");
			Require(field.Sample(centre+new Vector2(810,0)) is > .34f and < .36f,"smooth fringe midpoint");
			Require(field.Sample(centre+new Vector2(1000,0)) == 0,"rain leaks beyond radius");
			float prior=.7f;
			for (int x=0;x<=1100;x++)
			{
				float rain=field.Sample(centre+new Vector2(x,0));
				Require(rain <= prior+.00001f && prior-rain<.003f,"discontinuous rain edge"); prior=rain;
			}
			using var a=new RainField(761,centre); using var b=new RainField(761,centre);
			a.Advance(200,centre);
			for(int i=0;i<2000;i++) b.Advance(.1,centre);
			Require(Math.Abs(a.Sample(centre)-b.Sample(centre))<.0001f,"frame dependent transition");
			Require(Math.Abs(a.Sample(centre,true)-b.Sample(centre,true))<.003f,"frame dependent wetting");
			Vector2 start=a.Cells[0].Centre;
			bool stopped=false, restarted=false, moved=false;
			float last=a.Sample(centre); int renewals=0;
			var min=new Vector2(20000,20000); var max=Vector2.Zero;
			for(int i=0;i<24000;i++)
			{
				a.Advance(1,centre);
				float rain=a.Sample(centre);
				Require(Math.Abs(rain-last)<.04f,"storm starts/stops abruptly"); last=rain;
				if(rain<.001f) stopped=true;
				if(stopped && rain>.1f) restarted=true;
				if(a.Cells[0].Centre!=start) { moved=true; renewals++; start=a.Cells[0].Centre; }
				foreach(var c in a.Cells)
				{
					min=min.Min(c.Centre); max=max.Max(c.Centre);
					Require(c.Peak>=.6f && c.Peak<=.8f,"automatic peak out of range");
					Require(float.IsFinite(c.Wetness) && c.Wetness>=0 && c.Wetness<=1,"invalid wetness");
				}
			}
			Require(stopped && restarted && moved && renewals>5,"storms remain fixed or fail to renew");
			Require(max.X-min.X>8000 && max.Y-min.Y>6000,"storm centres restricted to fixed region");
			foreach(var c in field.Cells) { c.Centre=centre; c.Radius=1000; c.Peak=.7f; c.Age=c.Delay+c.Rise+1; }
			Require(Math.Abs(field.Sample(centre)-.7f)<.00001f,"overlap exceeded storm peak");
			field.Preview(centre,0,1000,1); field.Advance(30,centre);
			Require(field.Sample(centre)==0 && field.Sample(centre,true)>.7f,"surface dries instantly");
			field.Advance(1200,centre);
			Require(field.Sample(centre,true)<.001f,"surface never dries");
			DayCycle.RegisterGlobals();
			CheckReceivers();
			var audio=new RainAudio { Enabled=false }; AddChild(audio);
			var bed=audio.GetNode<AudioStreamPlayer>("RainBed");
			var thunder=audio.GetNode<AudioStreamPlayer>("Thunder");
			Require(bed.Stream is AudioStreamOggVorbis { Loop:true } && bed.Stream.GetLength()>60,"invalid rain loop");
			Require(thunder.Stream is AudioStreamOggVorbis { Loop:false } && thunder.Stream.GetLength()>5,"invalid thunder one-shot");
			audio.Advance(1,0,false,false);
			Require(!bed.Playing && !thunder.Playing,"clear/muted audio started");
			GD.Print($"[rain-weather] PASS: flat core, smooth edges, temporal continuity, drying, {renewals} relocations, continent coverage, independent clock, rain/thunder asset configuration and clear/muted state");
			audio.Free();
			await ToSignal(GetTree().CreateTimer(.12),SceneTreeTimer.SignalName.Timeout);
			GetTree().Quit();
		}
		catch(Exception ex) { GD.PushError($"[rain-weather] {ex}"); GetTree().Quit(1); }
	}
	private void CheckReceivers()
	{
		var atlas=WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
		AtlasSectorWindow Window(int origin)
		{
			const int size=96;
			var data=new AtlasSectorData(0,0,origin,0,size,0,size,size,64,24,"weather-receiver-test");
			var grid=new VoxelGrid(size,64,7,origin,0);
			for(int z=0;z<size;z++) for(int x=0;x<size;x++)
			{
				bool water=x+origin>=48; int h=water ? 16 : 20;
				grid.Describe(x,z,h,Palette.STONE_PALE,Palette.STONE_PALE);
				data.Height[z*size+x]=(ushort)h; data.WaterSurface[z*size+x]=(ushort)(water?24:0);
				if(x+origin is >=40 and <50 && z is >=40 and <50)
				{ grid.Set(x,35,z,Palette.STONE_PALE); grid.RaiseOverhangCeiling(x,z,36); }
			}
			return new AtlasSectorWindow(data,atlas,7,grid);
		}
		var current=Window(0); var focus=new Vector3(48,24,48);
		var day=new DayCycle(); AddChild(day);
		var rain=new RainWeather(); AddChild(rain); rain.Setup(()=>focus,()=>current,day,9); rain.Audio.Enabled=false;
		rain.Preview(.75f); rain._Process(0);
		var mm=rain.GetNode<MultiMeshInstance3D>("RainStreaks").Multimesh;
		Require(mm.InstanceCount==6400,"unbounded receiver count");
		var before=new Vector3[6400]; var valid=new bool[6400]; int roof=0,waterCount=0,ground=0;
		for(int i=0;i<6400;i++)
		{
			before[i]=rain.ReceiverPositions[i]; valid[i]=rain.ReceiverData[i].G>.5f;
			if(!valid[i]) continue;
			var p=before[i]; int x=Mathf.FloorToInt(p.X),z=Mathf.FloorToInt(p.Z);
			float expected;
			if(x is >=40 and <50 && z is >=40 and <50) { expected=36.025f; roof++; }
			else if(x>=48) { expected=24.375f; waterCount++; }
			else { expected=20.025f; ground++; }
			Require(Math.Abs(p.Y-expected)<.001,"drop ends below/above its actual receiver");
		}
		Require(roof>0 && waterCount>0 && ground>0,"receiver fixture missed a surface kind");
		focus.X+=3.2f; rain._Process(0);
		for(int i=0;i<6400;i++) if(valid[i]) Require(rain.ReceiverPositions[i]==before[i],"walking reset a central rain receiver");
		current=Window(24); rain._Process(0);
		for(int i=0;i<6400;i++) if(valid[i] && before[i].X>=24)
			Require(rain.ReceiverPositions[i]==before[i],"window handoff displaced a shared receiver");
		var viewport = new SubViewport { Size = new Vector2I(1600,900) }; AddChild(viewport);
		var camera = new Camera3D { Far = 2000, Fov = 32 }; viewport.AddChild(camera);
		var target = new Vector3(34,20,48);
		foreach (float distance in new[]{100f,220f,400f})
		foreach (float yaw in new[]{0f,Mathf.Pi/2,Mathf.Pi,3*Mathf.Pi/2})
		{
			camera.LookAtFromPosition(target + new Vector3(0,.8f,1).Normalized().Rotated(Vector3.Up,yaw)*distance,target);
			Require(RainWeather.SelectVisiblePuddlePlane(camera,current,rain.Field)==20,
				$"visible wet paving lost at distance {distance}, yaw {yaw}");
		}
		rain.Field.Preview(new Vector2(5000,5000),.7f,100,1);
		Require(RainWeather.SelectVisiblePuddlePlane(camera,current,rain.Field)==null,"dry visible paving enabled wet mirror");
		viewport.Free();
		GD.Print("[rain-weather] visible paving PASS: three zoom distances/four rotations, dry view rejection");
		rain.Paused=true; double clock=rain.Field.Time;
		rain._Process(5); Require(rain.Field.Time==clock,"frozen weather advanced its clock");
		rain.Free(); day.Free();
		GD.Print($"[rain-weather] receiver PASS: {ground} ground/{waterCount} water/{roof} roof, walking and window handoff");
	}

}
