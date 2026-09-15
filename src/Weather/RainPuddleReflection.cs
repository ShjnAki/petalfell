using System;
using Godot;
using Petalfell.Render;

namespace Petalfell.Weather;

/// <summary>One bounded wet-paving mirror, selected by actual nearby mineral support.</summary>
public partial class RainPuddleReflection : Node3D
{
	public const uint CameraMask = 0x3ffff;
	public SubViewport RenderViewport { get; private set; }
	private Camera3D _source, _mirror;
	private Func<Camera3D, float?> _select;
	private float _plane=-1000000, _blend, _probe;
	private float? _target;
	public void Setup(Camera3D source, Func<Camera3D, float?> select) { _source=source; _select=select; }
	public void SetSource(Camera3D source) { _source=source; _probe=0; }
	public override void _Ready()
	{
		ProcessPriority=101;
		RenderViewport=new SubViewport { Size=new Vector2I(8,8),HandleInputLocally=false,
			RenderTargetUpdateMode=SubViewport.UpdateMode.Disabled,Msaa3D=Viewport.Msaa.Disabled };
		AddChild(RenderViewport);
		_mirror=new Camera3D { Current=true,CullMask=CameraMask,PhysicsInterpolationMode=PhysicsInterpolationModeEnum.Off };
		RenderViewport.AddChild(_mirror);
		RenderingServer.GlobalShaderParameterSet("pf_puddle_tex",RenderViewport.GetTexture());
	}
	public override void _Process(double delta)
	{
		if (_source==null) return;
		_probe-=(float)delta;
		if(_probe<=0) { _target=_select(_source); _probe=.3f; }
		bool same=_target.HasValue && Math.Abs(_target.Value-_plane)<.01f;
		_blend=Mathf.MoveToward(_blend,same ? 1 : 0,(float)delta*2);
		if(_blend<=0 && _target.HasValue) _plane=_target.Value;
		RenderingServer.GlobalShaderParameterSet("pf_puddle_state",new Vector4(_plane,_blend,0,0));
		RenderViewport.RenderTargetUpdateMode=_blend>0 ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
		if(_blend<=0) return;
		var size=(Vector2I)(_source.GetViewport().GetVisibleRect().Size*.375f);
		size=new Vector2I(Mathf.Max(size.X,8),Mathf.Max(size.Y,8));
		if(RenderViewport.Size!=size) RenderViewport.Size=size;
		_mirror.Fov=_source.Fov; _mirror.Projection=_source.Projection; _mirror.Size=_source.Size;
		_mirror.Near=_source.Near; _mirror.Far=_source.Far; _mirror.KeepAspect=_source.KeepAspect;
		_mirror.GlobalTransform=PlanarReflection.MirrorTransform(_source.GlobalTransform,_plane);
	}
}
