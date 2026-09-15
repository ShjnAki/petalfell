# Dynamic rain, wet surfaces and sound

- **Lifecycle:** active
- **Evidence summary:** lifecycle and receiver ownership are mechanically verified;
  the named material, coast, grove and gate captures are visually reviewed;
  actual bus recording verifies nonzero stereo output, thunder and fades;
  no author acceptance is claimed.
- **Scope:** runtime-shared; the paving mirror covers one selected level.
- **Last verified:** 2026-09-15
- **Supersedes:** none
- **Owning sources:** [weather contract](../../docs/WEATHER.md),
  [RainField](../../src/Weather/RainField.cs),
  [RainWeather](../../src/Weather/RainWeather.cs),
  [RainAudio](../../src/Weather/RainAudio.cs),
  [paving mirror](../../src/Weather/RainPuddleReflection.cs).

## Reference observations

The three supplied images are preserved with checksums in
`world-new/look-targets/2026-09-15/`: `rain-shore.png`, `rain-grove.png`,
`rain-gate.png`. They show fine pale falling lines, small impact sparkle,
expanding water rings, disturbed reflections, cooler hazy air and broken shiny
stone films. Vegetation remains softer and less reflective. These are weather
and surface targets; existing site geometry is not redesigned to match them.

## Method

A temporary cell has a uniform inner 62% of radius and a cubic outer fringe.
Multiply its footprint by a cubic rise/hold/fall envelope. Accumulate wetness
separately, with a longer drying time. Keep the old cell until its wetness is
negligible before choosing a new location. Weather's RNG and clock belong to the
persistent runtime, not the terrain generator, day clock or window builder.

Drop receivers are indexed by a wrapping global lattice. Cache their world cell
identity and change only new fringe cells during travel. Query the highest actual
solid, including roofs, and compare it with the water surface. Preserve fall phase
when a receiver remains covered. Small rings and spray share each drop's arrival
phase; water has additional independently timed expanding waves. Use a surface
gradient to perturb both transmission and reflection, not a screen-wide rain film.

Stone uses pooled films; porous ground darkens without becoming a mirror. Reduced
roughness alone was insufficient at the locked view. A bounded second mirror
registers to visible mineral paving and contributes through the pool mask. Its
reserved camera mask excludes water and separates its clip plane. The shader uses
it only on matching horizontal surfaces and never inside either mirror camera.
Other heights keep roughness/SSR. Fade to zero before selecting another elevation.

Recorded rain uses a circular equal-power splice rather than stopping and
restarting an ambience clip. Separate thunder varies its delay, level and pitch.
Smooth local rain gain, preserve stereo, and explicitly stop playback on teardown.
Always keep source licenses and the reproducible audio preparation script.

## Evidence

| Claim | State | Evidence | Scope/limits |
|---|---|---|---|
| Rain begins, ends and chooses fresh locations; plateau, fringe and temporal changes stay continuous | mechanically verified | `verify-rain-weather`: 18 renewals over 24,000 simulated seconds, continent-spanning centres, bounded peaks, smooth samples and timestep comparison | Independent seeded weather stream; not an assertion that the whole continent rains simultaneously |
| Wetness persists after rain and dries; freezing weather stops its clock | mechanically verified | `tools/RainWeatherSmoke.cs`, dry-down and frozen-clock assertions | Manual preview deliberately sets immediate rain/wetness |
| Drops meet actual ground, water and overhead solids; travelling and window handoff preserve shared receivers | mechanically verified | 439 ground, 448 water and 13 roof receivers; central-cell equality after movement and window replacement | CPU ownership records checked because the headless renderer cannot return MultiMesh instance data |
| Wet stone differs from grass and reflects actual geometry on its registered level | visually reviewed | `shots/rain-materials/wet.png` and `dry.png`; `shots/rain-v6-gate/` noon at all four quarter rotations, twilight and play views | One dominant mineral level has a planar mirror; other heights use roughness/SSR |
| Rain, rings and spray read in actual southern terrain and authored paving | visually reviewed | `shots/rain-v6-gate/`, `shots/rain-v6-grove/`, `shots/rain-v7-coast/` play/noon/midnight and sequence frames 0000, 0060, 0179 | Does not grant whole-site geometry parity or author acceptance |
| Audio output contains stereo rain, a separate rolling thunder event and smooth fade-out without clipping | mechanically verified | `shots/rain-audio/rain-and-thunder.wav`, 19.03 s; peak .3496, RMS .0267; seamless wrap at ~4 s, separate thunder, and final fade | Real muted audio-bus capture including the loop seam, not a claim of author-approved loudness or speaker listening |
| Wet runtime stays bounded and renders interactively | mechanically verified | `shots/rain-v6-perf/look_perf_day.txt`: 1600×900, 4× MSAA, 300 frames; GPU median 19.196 ms, p95 19.328 ms; wall median 19.427 ms | RTX 3050 laptop GPU, warmed stationary gate; includes water/paving mirrors, excludes traversal costs |

## Transition and motion review

The material lifecycle emits eight stills and `shots/rain-lifecycle/weather.csv`.
The 0, 90, 180, 600 and 1,200-second states were inspected: clear, building rain,
steady wet rain, stopped rain with residual dampness, and dry after renewal.
Rain reaches .660, then is zero at 600 seconds while wetness remains .180;
wetness falls to .016 at 900 seconds and zero by renewal at 1,200 seconds.
The first cell changes centre from (-72.561,-133.226) to (5891.726,3539.665).
This is accelerated lifecycle evidence, not a realtime performance measurement.

The final coast sequence has 180 frames at 30 simulation frames/second and a
six-second `rain-motion.mp4`. CSV rain-time increments remain .03333–.03334 s;
local rain remains .75. Frames 0000, 0060 and 0179 were inspected alongside the
play, noon and midnight stills. Rings change radius/position, spray remains tiny,
and the existing banks and player retain their positions. This is sampled
frame inspection, not a claim that every video frame was watched.

`dotnet build`, `verify-rain-weather`, and the existing `verify-look-rendering`
pass. Audio playback is checked separately with the real muted PulseAudio bus;
headless dummy audio only validates stream configuration and silence, because
its unmixed playback queue can report false teardown leaks.

## September 15 distance correction

The initial 40-block player-centred histogram made wide-view paving reflections
appear only after approaching. It is replaced by 40×24 screen rays from the current
source camera, voting on first visible wet mineral receivers. Each ray is bounded
to 768 column queries. Wetness is checked at the receiver, not at the traveller.
SSR is enabled whenever residual wetness exists, independent of player position.

`verify-rain-weather` now checks visible paving at distances 100, 220 and 400,
at all four quarter rotations, and rejects a dry view. The inspected GPU fixture
`shots/rain-materials/wet-distant-focus.png` doubles camera distance and moves the
traveller focus to (220,12,220), outside the old search area; the visible stone
still reflects the pillar and slab. This replaces the original proximity-based
selection evidence. The earlier stationary GPU timing above predates this change
and is not a performance measurement of the new selector.

## Known failures and safeguards

- Explicit view-space placement of rain quads broke the scene in the first pass.
  Keep this translation-only instance lattice in local coordinates and let Godot
  perform its usual world/view transform. The discarded white diagnostic captures
  do not count as visual evidence.
- Equal bright rings in every small cell read as a bubble pattern. Lower their
  spatial density, vary expansion radius, and give crests directional brightness.
- Lowering roughness without a usable reflected source barely changed paving.
  Verify a reflected object, not merely a changed colour or roughness number.
- Never apply the water mirror to a different floor elevation. A convincing
  colour patch can still be a geometrically misplaced reflection.
- C# build success must be checked before every GPU launch. A failed diagnostic
  build can silently leave the preceding DLL available to Godot.
- GPU frame readback and PNG compression are not a performance measurement.
  Use the warmed benchmark with all active reflection viewports counted.

## Remaining limits and review

Rain and wetting are local world fields; the material wetness shader does not yet
model the full history of shelter or evaporation per voxel. Drop landing points
and sheltered audio do use current overhead solids. Screen-space reflections
cannot recover occluded/offscreen geometry; the paving mirror supplies one nearby
level only. Author review of sound balance and source fidelity remains separate
from these mechanical and visual checks.

Update this entry whenever field shape, timing, rain geometry, material response,
reflection ownership, sound sources or controls change. Retain rejected methods
only where they prevent a likely repeat failure.
