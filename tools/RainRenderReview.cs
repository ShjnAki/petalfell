using System;
using System.Linq;
using Godot;
using Petalfell.Core;
using Petalfell.Render;
using Petalfell.World;
using Petalfell.Weather;

namespace Petalfell.Tools;

/// <summary>Small material/impact fixture using production shaders and weather, not a runtime world.</summary>
public partial class RainRenderReview : Node3D
{
	public override async void _Ready()
	{
		try
		{
			DayCycle.RegisterGlobals();
			var atlas = WorldAtlasDefinition.Load("res://content/chapter_01/atlas.json");
			const int size=96;
			var data=new AtlasSectorData(0,0,0,0,size,0,size,size,64,12,"rain-material-review");
			var grid=new VoxelGrid(size,64,7);
			for(int z=0;z<size;z++) for(int x=0;x<size;x++)
			{
				int h=x<48 ? 12 : 8;
				byte mat=x<24 ? Palette.GRASS : Palette.STONE_PALE;
				grid.Describe(x,z,h,mat,mat); data.Height[z*size+x]=(ushort)h;
				data.WaterSurface[z*size+x]=(ushort)(x<48 ? 0 : 12);
			}
			// Raised paving and pillars expose reflection registration, vertical wetting
			// and the actual surface on which a drop must end.
			for(int z=38;z<43;z++) for(int x=31;x<36;x++) for(int y=12;y<25;y++) { grid.Set(x,y,z,Palette.STONE_PALE); grid.Heights[z*size+x]=(short)(y+1); }
			for(int z=55;z<58;z++) for(int x=30;x<47;x++) for(int y=12;y<15;y++) { grid.Set(x,y,z,Palette.STONE_PALE); grid.Heights[z*size+x]=(short)(y+1); }
			var window=new AtlasSectorWindow(data,atlas,7,grid);
			var voxel=WorldMaterials.CreateVoxel(12);
			for(int z=0;z<size/ChunkMesher.ChunkSize;z++) for(int x=0;x<size/ChunkMesher.ChunkSize;x++)
			{
				var chunk=ChunkMesher.Build(grid,x,z);
				AddChild(new MeshInstance3D { Mesh=chunk.Surface,MaterialOverride=voxel });
			}
			var water=WorldMaterials.CreateWater(12,true);
			AddChild(new MeshInstance3D { Mesh=new PlaneMesh{Size=new Vector2(48,96)},Position=new Vector3(72,12.35f,48),
				MaterialOverride=water,Layers=PlanarReflection.WaterLayer,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off });
			AddChild(Atmosphere.Build()); var sun=Atmosphere.Sun(); var fill=Atmosphere.Fill(); AddChild(sun); AddChild(fill);
			var day=new DayCycle { TimeOfDay=.5f, Paused=true }; AddChild(day);
			day.Setup(Atmosphere.LastEnvironment,sun,fill,Atmosphere.LastSky,water);
			var focus=new Vector3(44,12,48);
			var view=new SubViewport { Size=new Vector2I(1600,900),World3D=GetViewport().World3D,
				RenderTargetUpdateMode=SubViewport.UpdateMode.Always,Msaa3D=Viewport.Msaa.Msaa4X }; AddChild(view);
			GetViewport().Disable3D=true;
			var camera=new Camera3D { Fov=32,Near=.5f,Far=600 }; view.AddChild(camera);
			camera.LookAtFromPosition(focus+new Vector3(55,55,72),focus); camera.Current=true;
			Atmosphere.SetViewDistance(Atmosphere.LastEnvironment,106);
			var mirror=new PlanarReflection(); mirror.Setup(camera,water,12.35f); AddChild(mirror);
			var rain=new RainWeather(); AddChild(rain); rain.Setup(()=>focus,()=>window,day,42,camera);
			float amount=OS.GetCmdlineUserArgs().Contains("--dry") ? 0 : .75f;
			bool cycle=OS.GetCmdlineUserArgs().Contains("--cycle");
			if(!cycle) rain.Preview(amount);
			rain.Audio.Enabled=false;
			bool distant = OS.GetCmdlineUserArgs().Contains("--distant-focus");
			if (distant) {
				camera.LookAtFromPosition(new Vector3(44,12,48)+new Vector3(55,55,72)*2,new Vector3(44,12,48));
				focus = new Vector3(220,12,220);
			}
			
			view.AddChild(WorldMaterials.CreateGrade());
			if(cycle)
			{
				rain.Paused=true;
				string dir="res://shots/rain-lifecycle"; DirAccess.MakeDirRecursiveAbsolute(dir);
				using var metadata=new System.IO.StreamWriter(ProjectSettings.GlobalizePath(dir+"/weather.csv"));
				metadata.WriteLine("seconds,rain,wetness,centre_x,centre_z");
				double previous=0;
				foreach(int seconds in new[]{0,45,90,180,360,600,900,1200})
				{
					rain.Field.Advance(seconds-previous,new Vector2(focus.X,focus.Z)); previous=seconds;
					rain._Process(0);
					for(int frame=0;frame<30;frame++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
					await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
					Capture.Save(view,dir,$"second-{seconds:D4}");
					metadata.WriteLine(FormattableString.Invariant($"{seconds},{rain.LocalRain:F5},{rain.Field.Sample(new Vector2(focus.X,focus.Z),true):F5},{rain.Field.Cells[0].Centre.X:F3},{rain.Field.Cells[0].Centre.Y:F3}"));
				}
				GetTree().Quit(); return;
			}
			for(int i=0;i<90;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
			await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
			string output="res://shots/rain-materials";
			Capture.Save(view,output,distant ? "wet-distant-focus" : amount>0 ? "wet" : "dry");
			GD.Print($"[rain-material-review] rain {rain.LocalRain:F3}, SSR {Atmosphere.LastEnvironment.SsrEnabled}");
			GetTree().Quit();
		}
		catch(Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
	}
}
