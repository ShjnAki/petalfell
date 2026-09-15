# Dynamic rain

Rain is temporary weather, never a fixed biome, region mask or terrain edit.
Three bounded storm cells have independent lifecycles and new locations on renewal.
The weather RNG is separate from world generation; a new play session can have
new weather without changing the continent. One initial storm is placed near the
traveller so a session does not depend on finding a remote active cell. Subsequent
cells can appear anywhere in the atlas, with some new events favoured near the
traveller. Moving or transporting does not relocate an existing storm.

Each cell has a 620–1,450-block radius. Its central 62% of radius is uniform,
with a randomly selected 60–80% peak; the remaining fringe fades smoothly to zero.
Overlapping cells take the strongest local value, preserving the 60–80% ceiling
and flat central coverage. Rain rises over 45–95 seconds, holds for
3–7 minutes, fades over 65–125 seconds, and rests before a newly located event.
A cell remains at its old address until residual wetness has dried. Wetting takes
roughly 28 seconds and drying roughly 125 seconds per exponential time constant.
These are independent of the day clock and never consume terrain randomness.

## Presentation

`RainWeather` persists beside the player/camera when an atlas window is replaced.
It publishes three rain and three wetness fields plus one weather clock. The same
world coordinates drive drop opacity, wet materials, water disturbance and audio.
Rain receivers query the highest solid or water surface in the active voxel window.
The fixed 80×80 instance lattice follows the traveller; previously covered cells
retain their identity, fall phase and landing point. Only newly exposed fringe
cells are rewritten while walking. All allocation stays local and bounded.

Fine falling streaks end at their receiver. Small ground/water rings and rebounding
spray share that fall phase. Water also uses independently timed, expanding impact
waves whose gradients warp both transmitted beds and mirrored banks. Grass/soil
remain comparatively rough as they darken. Exposed mineral tops collect broken
low-roughness films with clearcoat; wet opaque surfaces use screen-space reflections
at their own height. One additional 37.5%-resolution mirror registers to the dominant
visible mineral floor, supplies real reflections in broken puddle patches, and fades
before changing height. It runs only while those surfaces are wet. Other elevations
retain their roughness/SSR response. Selection casts a fixed 40×24 grid of rays
through the active camera, including capture cameras, and counts the first visible
wet mineral receivers by height. It has no player-distance cutoff; at most 768
column queries per ray keep the work bounded. SSR stays enabled while any weather
cell retains wetness, so a dry traveller can view wet paving farther away.
The existing planar water reflection keeps
its own elevation; reserved camera masks separate their clipping planes.
Rain softens the key and shifts atmospheric colour through the ordinary day cycle.

Rain ambience is a recorded stereo loop with an eight-second circular crossfade.
Separate thunder claps use irregular timing, delay, pitch and level. Audio follows
local rain with a short gain fade; an overhead solid reduces the rain bed. Sources,
licenses and modifications are in [audio credits](../assets/audio/weather/CREDITS.md).
The Linux build copies those credits beside the exported game.

## Controls and review

The tilde developer panel contains Automatic weather, Rain here, Weather volume,
and Freeze weather. Scrubbing Rain here is an immediate local preview, including
wetness; enabling Automatic weather resumes evolution without moving active cells.
Freeze time controls the day; Freeze weather controls rain, wetting and impacts.
They are deliberately independent. Weather defaults to automatic in normal play.

`--rain=0.75` forces a wet local review; `--rain=0` forces clear. Ordinary captures
remain clear unless explicitly overridden, and capture audio is muted. The material
fixture uses production shaders, rain and audio ownership:

```bash
./tools/world-authoring.sh verify-rain-weather
# GUI: use the workspace-5 launcher from the capture procedure.
godot-mono --path . --script res://tools/rain-render-review.gd
godot-mono --path . --script res://tools/rain-render-review.gd -- --dry
# Silent bus capture (no speaker playback), saved under shots/rain-audio/.
godot-mono --headless --audio-driver PulseAudio --max-fps 60 --path . \
  --script res://tools/rain-audio-review.gd
```

The September 15 source images are preserved under
`world-new/look-targets/2026-09-15/`, with a SHA-256 manifest. They govern weather
and wet-surface presentation, not new arrangements of existing authored sites.
See the [weather evidence](../building-knowledge/rendering/dynamic-rain.md) for
inspected artifacts and remaining limitations.
