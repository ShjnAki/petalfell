# Shadow motion and stability — September 2026

- **Lifecycle:** active
- **Evidence:** mechanically verified; focused visual review below; author acceptance open
- **Scope:** shared directional renderer and atlas camera
- **Last verified:** 2026-09-15
- **Supersedes:** quantized direct-light updates and the restricted 0.5–6 blur control
- **Owning sources:** [DayCycle](../../src/Render/DayCycle.cs),
  [Atmosphere](../../src/Render/Atmosphere.cs),
  [CameraRig](../../src/Render/CameraRig.cs),
  [PlanarReflection](../../src/Render/PlanarReflection.cs),
  [DeveloperMenu](../../src/Tools/DeveloperMenu.cs),
  [look checks](../../tools/LookRenderSmoke.cs)

## Mechanism

The old 1/2048-day bucket held the sun in place for about 0.439 seconds at the
900-second day length. Only the sky material now uses that throttle. The direct
key, cloud lighting and water illumination update every rendered frame. When
the clock is frozen, unchanged time/weather skip lighting writes altogether.
Both directional lights use LightOnly sky mode; the authored sky gets its own
explicit parameters.

The sun and reflection camera are driven in `_Process` and disable physics
interpolation. The gameplay camera already did so; its orientation now comes
directly from yaw/pitch, avoiding small basis changes when LookAt subtracts
rounded world positions thousands of blocks from the origin.

An 8192 directional atlas and blended cascade splits 0.20/0.45/0.72 place more
resolution around the long-lens subject. Range now grows with camera distance as described in the September 15 correction below.
The developer control is 0–100%, mapping to radius 0–12 with default 1.25.
Zero selects Hard filtering with zero angular distance. Positive values use
Ultra PCF. Cloud coverage changes illumination but cannot change this radius.

**Rejected endpoint:** setting ShadowBlur directly to zero exposed dense
self-shadow stripes. Godot multiplies depth bias by the blur and quality radius
as well as using it for filtering. Hard filtering keeps a nonzero internal blur
value (ignored by its single sample); bias compensates for blur and the current
Godot quality radius (Hard 1, Ultra 4). This preserves an effective 0.32 depth
bias coefficient across the control. Recheck these engine multipliers after a
Godot upgrade. See the engine's
[light upload](https://github.com/godotengine/godot/blob/master/servers/rendering/renderer_rd/storage_rd/light_storage.cpp)
and [quality setup](https://github.com/godotengine/godot/blob/master/servers/rendering/renderer_rd/renderer_scene_render_rd.cpp).

## Verification and limits

`dotnet build`, `verify-look-rendering` and `verify-camera-auto-zoom` cover the
change. The look check samples 240 flowing and 240 frozen frames, fine paused
scrubbing, both endpoints, effective bias and the actual menu at 0/50/100%.
Another 240-frame test follows sub-voxel vertical motion at atlas 6400,7360
while requiring an exactly unchanged camera basis.

Focused GPU evidence lives in `shots/shadows-2026-09-13/` at atlas 5107,6620.
Sequences contain 90 PNG frames and light/camera CSV metadata at fixed 30 fps.
`before/shadow_flow` has only eight unique light directions; `after/shadow_flow`
has 90. Both locked frozen sequences retain one light direction and camera pose.
Initial frozen image differences showed moving water and plants, not a shifting
terrain shadow. The author reports shaking everywhere even while everything
appears still; this observation must not be dismissed on the strength of a locked-camera test.
The `shadow_frozen_follow` probe therefore runs the ordinary interpolated-player
Follow path as well. Its light and camera basis remain exactly fixed; camera
position makes one final 0.00049-block settling step on each horizontal axis,
then remains fixed for 89 frames. Static cap/ground shadow regions average
under one 8-bit colour level of frame difference. The final hard/default/maximum
stills were inspected full-size: the zero-bias stripes are gone, default edges
remain defined, and maximum softness deliberately spreads the cast edge widely.
Extreme PCF softness retains some spatial grain.

The additional `shadow_gameplay` probe records the actual game window at atlas
6400,7360, 75-block zoom, with normal player physics and 90 render frames/sec
against 60 physics ticks. It emits 180 frames and metadata. Run it with
`--fixed-fps 90 -- --terrain-focus 6400,7360 --shots res://shots/shadows-2026-09-13/gameplay --only shadow_gameplay`
through the approved silent workspace-5 launcher. Other shadow probes require
`--fixed-fps 30`. These captures are visual evidence, not real-time FPS tests.
The completed gameplay recording has exactly one light direction, camera
position and camera basis across all 180 frames. The inspected stationary ground
shadow edge averages 0.52 of an 8-bit channel level of change across nine sampled
frames; the amplified full-frame difference shows animated water/plants without
a displaced terrain shadow boundary. Raw metadata totals are in
`shots/shadows-2026-09-13/metadata-check.json`.
`matrix/` adds four noon quarter rotations and night at 170 blocks, inspected
in a six-frame contact sheet. Its 75-block `atlas_play` image frames the water
bed, so the actual gameplay capture above supplies the useful near-ground
shadow check. No geometry or site composition was changed by this pass.
The [local gallery](../../shots/shadows-2026-09-13/review.html) contains the
before/after flowing clips and the frozen-follow clip; all are encoded directly
from their 30 fps PNG sequences.

Water, grass, mist and wildlife intentionally continue animating when only the
day clock is frozen. Their movement is separate from stationary terrain shadows.
The exact original everywhere-shaking symptom was not independently reproduced;
author confirmation in ordinary play remains open.

## September 15 zoom consistency correction

The fixed 260-block production shadow range could end within the visible scene
at maximum zoom. Production, interactive review and captures now share
`Atmosphere.SetShadowViewDistance`: max(260 × site scale, 2 × camera distance).
The 240-block developer maximum therefore has 480-block shadow coverage. There
is no range quantization, and an unchanged zoom does not rewrite the light.
The look check covers six distances through 700 and repeated frozen calls.

Thin grass/stem/reed/vine edges also carry a midpoint offset in CUSTOM1. The
vertex shader preserves up to 0.9 pixels of coverage with a maximum 3× width;
it does not add plants or move their centres. Joined grass shoulders share the
same offset and wind, mechanically checked through window handoff. Broad
mineral faces and flower heads keep their physical dimensions. The detail mesh
has a half-block cull margin for wind and this small width expansion.

This addresses subpixel loss of thin decorations, not a removed distance LOD:
the production detail shader and merged meshes had no explicit distance cutoff.

`shots/zoom-consistency/` records the same coast at 75 and 240 blocks and all
four quarter rotations at 240. Close and wide were inspected at full size; the
remaining quarters in a labelled 800-pixel-wide matrix. Cast mushroom shadows,
reeds, grass and hanging vegetation remain visible. Production reported 25
loaded animals throughout the wide rotations. Capture logs are clean; build,
weather and extended look checks pass. These stills do not claim exhaustive
motion review or author acceptance.
