# Grounded stair walking

- **Lifecycle:** `active`
- **Evidence summary:** `mechanically verified` supported stair traversal; bent-leg animation rejected by the author
- **Scope:** `general` controller and character; bounded fixture evidence
- **Last verified:** 2026-09-13
- **Supersedes:** the bent-knee/planted-foot version of this entry
- **Owning sources:** `src/Player/Controller.Steps.cs`, `src/Player/Controller.cs`, `src/Player/Character.cs`, `src/Tools/AtlasSectorReview.cs`, `tools/StairWalkSmoke.cs`

## Outcome and author correction

Keep the original single-piece legs, walking cycle and movement speeds.
The subsequent author report of teleport-like glitches supersedes the earlier
pose-only evidence as a claim of smooth rendering. Low
stairs smoothly change character height while the ordinary walking animation
continues. The camera shares that eased height. Higher ledges retain automatic
jump arcs, and explicit Space jumps retain normal physics.

The author rejected the articulated version on September 13: “revert to single
legs, instead of bent ones, keep walking same”. Do not reintroduce knee/foot IK,
planted-foot poses, pelvis constraints or a special slower stair cadence. The
rejected code is archived under
`reference/retired-code/rejected-stair-ik-2026-09/` with a non-C# extension.
Earlier `v1/` and `final/` captures under `shots/stairs-2026-09-13/` show that
rejected implementation; they are not the current target.

## Evidence

| Claim | State | Scope | Evidence | Remaining uncertainty |
|---|---|---|---|---|
| Low flights stay grounded without jumping | `mechanically verified` | Six-tread .25/.5/1-block flights in both directions | `verify-stair-walk`, zero airborne frames and upward jump velocity | Exhaustive authored stairs and irregular edges |
| Existing speed works on stairs | `mechanically verified` | Slow routes, full-speed one-block manual-input flights, rotated fixtures at 6400,24,7360 | Complete routes pass without the removed 3.6 speed cap | Rapid direction changes on narrow broken slabs |
| Taller ledges, roof and input behavior remain distinct | `mechanically verified` | 1.25-/2-block ascent, 2-block descent, ceiling/input fixtures | Larger rises jump; large drops fall; headroom, Space and stopped/disabled input pass | Other collision shapes |

The revised close captures were visually inspected at
`straight-legs/step-0.50-up/frame-0022.png` and
`straight-legs/step-0.50-down/frame-0018.png`: the original straight leg
silhouettes and ordinary hip swing are restored. This is focused pose evidence,
not author acceptance. The production land check at 2692,2164 also passes,
walking 44.08 blocks across Y72..78 in 575 physics frames.

## Method

1. Keep step decisions in the controller. Only an already supported,
   non-swimming body without upward jump velocity can prepare a low step.
   Sweep upward within available headroom, forward by actual motion, then down
   onto support. Check the lower crossing again.
2. Classify actual surface contact height against `LowStepHeight` (1.05), rather
   than capsule-origin lift alone. Its rounded bottom contacts corners before
   the center reaches the tread.
3. Permit a steep capsule corner normal only during a verified crossing, then
   restore the ordinary slope angle. Clear the resulting vertical slide velocity
   on supported low steps. Short downward snaps connect descending treads; larger
   drops continue falling. Preserve the ordinary horizontal movement speed.
4. Make the manually positioned character visual root `TopLevel` with physics
   interpolation off. Turning interpolation off alone leaves the physics parent's
   interpolated transform in the rendering hierarchy. Setting the character's
   global position then composes two movement deltas. Share one interpolated
   position between character and normal follow camera.
   Ease measured step-height changes and reset on teleports. Collision stays at
   the swept supported position. Leave the original leg geometry, ordinary walk
   cycle, bob and jump animation intact.

## Render interpolation regression

A Vulkan check at 144 render fps and 60 physics ticks reproduced a .21348-block
error between the assigned presentation position and the actual rendered model
in the half-block ascent. The previous fixed-60 headless collision and pose checks
did not see this. `tools/StairWalkSmoke.cs` now samples
`GetGlobalTransformInterpolated()` after `FramePostDraw`, when Godot has composed
the hierarchy. `verify-player-motion` requires at least 20 rendered samples per
flight and no more than .005 blocks of disagreement. The corrected run passed
14 cases and 4254 rendered samples with zero measured disagreement. It includes flat walking,
slow/full-speed stairs and rotated parents at atlas coordinates.

The engine's [interpolation documentation](https://docs.godotengine.org/en/4.7/tutorials/physics/interpolation/advanced_physics_interpolation.html)
explains why disabling local interpolation does not exclude a moving parent's
transform. A top-level visual keeps its lifetime under the controller while
excluding that transform. Facing must use world directions when
`GetParentNode3D()` is null for this top-level node.

Run `./tools/world-authoring.sh verify-player-motion` through the workspace-5
silent launcher. It needs GPU rendering; headless `FramePostDraw` produces no
samples and is not a substitute. Manual-input fixtures align their test camera
with world X before pressing move-right/left, because normal input is camera
relative. Keep the camera present when checking that input path.

Fresh corrected ascent/descent sequences and before/after logs are under
`shots/stairs-2026-09-13/interpolation-fixed/`. Both `frame-0045.png` views were
inspected for the retained straight leg silhouette. Their rendered-position
checks pass with zero error. These 144-fps diagnostic runs capture after physics
frames, so their saved PNGs are sampled frames, not a constant-144-fps movie.

## Checks and limits

`dotnet build` and `./tools/world-authoring.sh verify-stair-walk` check the real
controller and character on complete flights, taller ledges, headroom and input
guards. Captures from that same fixture use fixed 30 fps through the workspace-5
silent launcher from [the capture guide](../rendering/capture-overlay-and-acceptance.md).
The original straight-leg captures are under `shots/stairs-2026-09-13/straight-legs/`; `height.csv`
records physical and presented root height. Inspect the original straight limbs,
continuous walking, ascent/descent and settling. Author review remains open.

The fixtures cover box treads .25–1 block high and 1.5 blocks deep. Capsule
size and site geometry are unchanged. The test does not establish every authored
stair's behavior or author acceptance of the revised motion.

## Collision lessons retained

- Requiring only upward-facing capsule normals produced tiny falls and hops at
  tread corners. The temporary angle exception belongs only to measured steps.
- Capsule-origin lift alone admitted a 1.25-block ledge as a low step. Classify
  actual contact surface height to keep that ledge in the auto-jump path.
- Requiring maximum theoretical lift to clear a roof rejected fitting half steps.
  Bound the sweep by actual headroom before measuring the tread.
- World-facing yaw needs conversion into a rotated parent's local basis.

## Boot ground datum — September 14 correction

The author's report of buried boot bottoms exposed a separate mesh-placement
error: the 3.2-voxel hip height plus the -3.3 boot center minus its .55 half-height
put the sole .195 world blocks below the foot origin. Place the boot center at
-2.6 instead: the sole lies .015 blocks above ground, enough for the ±.012 idle
bob. Boots overlap the trouser ends, as footwear should. Preserve their dimensions,
straight leg pivots, walking curves, shared smooth position and collision shape.
Do not compensate by raising the entire character or changing terrain heights.

`verify-stair-walk` now measures all eight corners of both actual boot meshes
at 24 stopped animation phases on ground datums 0, 24 and 72. All resting soles
must stay within -.001..+.03 of the support plane. This checks neutral/stopped
contact; it is not a foot-planting constraint during the rigid walk swing. The
September 14 check passed with .003–.027 blocks of clearance. Both captured
half-block flights retain zero render-position error; the resting ascent view
`shots/boots-2026-09-14/step-0.50-up/frame-0085.png` was inspected and shows the
full boot height above the flat landing. Author visual review remains open.

## Update triggers

Recheck after capsule, speed, step threshold, interpolation or camera-follow
changes. Author correction always updates this entry. The straight-leg walking
requirement is authoritative until the author explicitly changes it.
