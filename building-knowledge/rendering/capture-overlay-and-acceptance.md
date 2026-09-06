# Capture, overlay, and acceptance

- **Lifecycle:** `active`
- **Evidence summary:** capture generation is `mechanically verified`; the full
  v13 Reference 10 matrix is historical `visually reviewed` evidence for its
  claim-scoped geometry; the complete current v17 matrix is `visually reviewed`
  only for Bloom's four annotated geometry corrections; the v2 locked day/night
  pair remains `visually reviewed` for line hierarchy and night readability
  only and is now historical because the author rejected that outline system;
  Reference 1's failed one-sector framing and corrected
  two-sector-per-axis framing are `visually reviewed`; no reconstruction is
  recorded here as `author-accepted`
- **Scope:** `tool-specific` to the current reference-site review rig; Reference
  10 camera numbers are `site-specific`
- **Last verified:** 2026-09-06 for the shared-look capture tools;
  historical site evidence below retains its original dates
- **Supersedes:** single flattering screenshots and comparisons from the wrong
  isometric quadrant
- **Superseded by:** none
- **Owning sources:**
  [`AtlasSectorReview.cs`](../../src/Tools/AtlasSectorReview.cs),
  [`world-authoring.sh`](../../tools/world-authoring.sh),
  [`bloom-grove-court-reference-10.json`](../../content/chapter_01/sites/bloom-grove-court-reference-10.json),
  [`shallows-gate-and-causeway-reference-1.json`](../../content/chapter_01/sites/shallows-gate-and-causeway-reference-1.json)

## Outcome

The September comparison page is generated from explicit raw directories and
source images with `tools/build-look-review.py`. `--scene-note NAME NOTE` identifies
the revision and limits. `shots/look-2026-09-06/review.html` now presents 23 focused
`folded-air-*-package` views: coast 13, Bloom six, alpine two and Fallen two.
All were reviewed, coast mainly in reduced matrices plus full-size play/probe
frames, and every site/alpine frame individually. Coast covers five clocks,
noon quarters, play/far, orbit and probes; the site/alpine sets have the narrower
scope listed on the page. Four package capture logs and the probe log are clean.

The preceding 70-view full material/clock/distance matrix remains available in
`review-satin-canopy.html` and retains its named evidence in the rendering ledger.
It predates the folded-petal change and must not be presented as current particle
coverage. The current coast orbit contains 180 sequential PNG/CSV/encoded frames,
30 fps, six seconds, 1600×900. Frames 0000/0045/0090/0135 were inspected individually.
No new site orbit or accelerated clock sweep was recorded. Site overlays were
generated but not inspected; structural parity and author acceptance remain open.
All four refreshed scene panels loaded in the browser. The current coast video
rendered its 0.00-second start and reached the 6.00-second endpoint; this is
sampled playback evidence, not every-frame temporal acceptance.

Every meaningful site revision is reviewed in one locked source-matching
isometric view, one calibrated overhead view when available, ordinary day and
night lighting, and four distances at all four cardinal rotations. Derived 50%
overlays and edge-difference images expose drift; they support human comparison
and never grant acceptance.

Site review additionally has five views at the locked source camera:
`site_dawn` (0.29), `site_noon` (0.50), `site_golden` (0.68),
`site_twilight` (0.80), and `site_midnight` (0.00). They use the ordinary clock
and do not replace the calibrated `reference_match_day`/`night` pair. The new
default Bloom set is 24 raw shots; historical 19-shot sets below predate these
five clock samples. Production and capture now share the view-distance haze
rule, including outline attenuation, through `Atmosphere.SetViewDistance`.

The tracked `shots/.gdignore` keeps review stills and motion sequences out of
Godot's import scan as well as its exported resources. Captures still load as
ordinary files in the local review page. A successful export alone is insufficient:
`build-linux.sh` now launches the package headlessly and requires its atlas audit
to pass before reporting readiness.

During terrain capture, the capture loop owns the camera and shared haze. Normal
playable Follow is suspended; `atlas_follow` invokes Follow explicitly inside the
capture loop. Each still checks the expected haze span after settling and logs
camera distance plus the resulting span, catching late writes from another camera.

## Evidence

| Claim | State | Scope | Evidence | Remaining uncertainty |
|---|---|---|---|---|
| The historical Reference 10 capture emitted 19 raw shots: locked day/night, calibrated top day, and 4 distances x 4 rotations, plus four derived comparison images | `mechanically verified` | historical/tool-specific | Complete file list in `/home/shikhar/godot/shots/reference-10-plan-v13-full/`; the September rig adds five source-camera clock samples | File production alone does not prove anyone inspected them |
| The locked camera uses source resolution 1672x941, yaw 135, pitch 35.26439, and current distance 190 | `mechanically verified` | site-specific/tool-specific | site `referenceView`; capture viewport construction | These parameters can become stale if the source calibration changes |
| v11 was inspected at the locked angle, overhead, all distances, all rotations, and day/night | `visually reviewed` | site-specific | `/home/shikhar/godot/shots/reference-10-plan-v11-full/`, 2026-08-30 | Later slab edits are not covered; geometry/material match remains open |
| The corrected central occupied footprint, restored slab boundary, open channels, supported reverse faces, modest far extent, and unchanged square shafts were inspected across v13 | `visually reviewed` | site-specific | Historical pre-lighting-change evidence: all 19 raw captures and four derived comparisons in `/home/shikhar/godot/shots/reference-10-plan-v13-full/`, 2026-08-30 | These claims do not establish the current render, whole-site fidelity, collision/playability, or author acceptance |
| The rejected internal-softness renderer was inspected at locked day/night samples before the author rejected its outline result | `visually reviewed` | historical/site-specific | `/home/shikhar/godot/shots/reference-10-ink-parity-v2/reference_match_day.png` and `reference_match_night.png`, 2026-08-30 | Historical failure evidence only; it does not describe the restored legacy ink now active |
| The current Bloom v17 capture set contains all 19 raw shots and all four derived comparisons | `mechanically verified` | tool-specific | Complete file set in `/home/shikhar/godot/shots/reference-10-callouts-current-v17/`, 2026-08-30: locked day/night, calibrated top, close/play/wide/far at r0-r3, source overlay/edge difference, and top overlay/edge difference | Completeness alone does not prove inspection or visual correctness |
| Across the complete current Bloom v17 matrix, the stair-side bump family stays absent, the south-west passage stays open, the four 2x2 shafts visibly originate from their connected foundations, and the southern threshold reads as two rises | `visually reviewed` | site-specific | All 19 raw shots and four derived comparisons in `/home/shikhar/godot/shots/reference-10-callouts-current-v17/`, inspected 2026-08-30 at locked day/night, calibrated top, close/play/wide/far, and r0-r3 | Only these four annotated geometry claims were promoted; whole-site parity, playability, and author acceptance remain unproven |
| A site whose footprint fits one sector still receives a real 2x2 sector context for the fixed 14-chunk capture circle | `mechanically verified` | tool-specific | `SiteSectorBounds`; `dotnet build --no-restore`, 2026-08-30 | This code invariant alone does not prove the registered subject is in frame |
| One-sector framing clamped Reference 1 away from the site, while the 2x2 context put its player, causeway and gate into the locked and overhead frames | `visually reviewed` | tool-specific | Empty registered frames in `/home/shikhar/godot/shots/reference-1-blockout-v1/` compared with `reference_match_day.png` and `reference_top_day.png` in `/home/shikhar/godot/shots/reference-1-blockout-v2/`, 2026-08-30 | Correct framing exposes the blockout but does not establish its geometry or visual fidelity |

## Procedure

1. Finish the mechanical plan audit and build before launching a visual review.
2. Ensure the registered focus remains inside the capture stream margin. Even if
   the footprint fits one sector, compose at least two real sectors per axis for
   the fixed site capture circle; a clamped focus can produce a valid but empty
   screenshot. Capture the locked isometric and top view first. If large topology
   is wrong, return to the plan before spending time on the full matrix.
3. Use the source image's exact resolution and locked quadrant. Reference 10's
   current lock is yaw 135 degrees, pitch 35.26439 degrees, distance 190.
4. Generate the full matrix after topology survives the first comparison:
   close 62, play 96, wide 154, and far 240; each at r0/r1/r2/r3 in 90-degree
   increments. Include locked day/night and top day.
5. Inspect raw source and render side by side before the 50% overlay. Then use
   the overlay for alignment and edge difference for silhouette diagnostics.
6. Record every inspected artifact and separate findings by geometry, material,
   light, scale, and hidden-face completeness. Do not use colour mismatch to
   move geometry until edge/top evidence agrees.
7. After any geometry, material, camera, shader, or lighting change, recapture
   every view affected by that change. Old captures become historical evidence.
8. Record `author-accepted` only after the author explicitly accepts a named
   revision/artifact set and scope. A continued instruction or positive comment
   about one detail is not whole-site acceptance.

## Checks

### Mechanical

```bash
./tools/world-authoring.sh capture-site bloom-grove-court \
  res://../shots/reference-10-review
```

On the author's current Hyprland workstation, every Godot GUI launch must be
silent on workspace 5. Use the approved launcher shape rather than opening a
window on the active workspace:

```bash
env -u LD_LIBRARY_PATH hyprctl dispatch \
  'hl.dsp.exec_cmd("env -u LD_LIBRARY_PATH PATH=/run/current-system/sw/bin:/usr/bin:/bin XDG_DATA_HOME=/tmp/petalfell-capture /home/shikhar/godot/petalfell/tools/world-authoring.sh capture-site bloom-grove-court res://../shots/reference-10-review", { workspace = "5 silent" })'
```

The explicit `PATH` is required because a compositor-launched command does not
necessarily inherit the interactive shell path that contains `godot-mono`.
Confirm the process exits and that the expected raw/derived files have fresh
timestamps. The dispatcher returning `ok` proves only that it accepted the
launch request; it does not prove Godot started. This remains mechanical
evidence only.

### Visual

- `reference_match_day`: source-side relationships, outer silhouette, primary
  stair, arch/opening, counts, player-relative scale, and lighting direction.
- `reference_overlay_50` and edge difference: calibration and corresponding
  edges, not beauty or acceptance thresholds.
- `reference_top_day` and top overlay: footprint, empty channels, stair
  direction, tree anchors, and unintended bridges.
- close/play r0-r3: block thickness, shaft section, damage, paving, collision
  scale, and hidden backs.
- wide/far r0-r3: hierarchy, rhythm, density, terrain integration, and distant
  readability.
- locked night: shadow modelling and material separation; day and night must use
  the ordinary `DayCycle`, not a presentation-only rig.

## Scope and limits

### September 6 shared look checks

The production `look_*` stills use distance 170, yaw 45, pitch 38 at dawn .27,
noon .50, sunset .72, twilight .80 and midnight .00. Noon r1/r2/r3 rotate by
90 degrees. These use ordinary production lighting and materials. Terrain
capture logs additionally report actual cap material IDs within 100 blocks and
the nearest snow sample; a province label alone is not material evidence.
Each still now waits at least 24 frames and 2.5 simulation seconds after a clock
or camera change. The earlier 24-frame-only wait left daytime petals in some
night captures on a fast GPU. Those captures remain geometry/material evidence,
but do not establish settled night-particle appearance.

Optional `probe_no_fog`, `probe_no_grade`, and `probe_no_post` use that noon
camera while isolating post-process contributions. They run only when explicitly
named with `--only` and are diagnostic evidence, not beauty/acceptance views.

Optional `look_motion` records 180 frames at play distance 86 with the ordinary
900-second day; `look_cycle` records 360 frames at distance 170 with a 12-second
accelerated day. Launch Godot with `--fixed-fps 30` **before** `--`. Frame PNGs
and a CSV of actual clock/cloud/key values are written below the shot directory.
`look_orbit` and `look_orbit_night` each record 180 frames at distance 86/pitch 31,
turning from yaw 45 through a full circle at morning .34 or midnight .00.
Their CSV also records camera yaw and the currently visible reflection plane.
Wind, particles, water, fog and DayCycle advance inside the engine. Readback and
PNG compression make this unsuitable as a realtime FPS measurement.

Optional `look_perf_day`/`look_perf_night` use distance 75/yaw 45/pitch 31 and
the same renderer at noon/midnight. Run **without** `--fixed-fps`. The capture
viewport always renders on the GPU even on an inactive workspace; the duplicate
window 3D pass is disabled. After saving its still and warming 120 frames, the
probe measures 300 frames without image readback, then writes CSV and summary
files. Positive GPU timestamps and visible draw counts are required. Timings
describe warmed stationary rendering, not traversal, UI or streaming/handoff.
With the atlas water mirror enabled, GPU/render-CPU columns sum the main and
active mirror viewports; visible draw/primitive columns describe the main view.
`probe_reflection` saves both the composited frame and the raw half-resolution
mirror, with its chosen elevation in the log. These are diagnostic artifacts.
Godot's viewport timing APIs report milliseconds; the documented source is
[RenderingServer](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html#class-renderingserver-method-viewport-get-measured-render-time-gpu).

For these GPU captures on the current Hyprland machine, the tested launcher is:

```bash
env -u LD_LIBRARY_PATH hyprctl dispatch \
  'hl.dsp.exec_cmd("env -u LD_LIBRARY_PATH PATH=/run/current-system/sw/bin:/usr/bin:/bin XDG_DATA_HOME=/tmp/petalfell-capture timeout 300s godot-mono --path /home/shikhar/godot/petalfell --display-driver x11 --disable-vsync --fixed-fps 30 -- --terrain-focus 6400,7360 --shots res://shots/look-review --only look_noon,look_midnight,look_motion > /tmp/petalfell-look-review.log 2>&1", { workspace = "5 silent" })'
```

Only one GPU review process runs at a time. Check the build's successful exit
before launching, then inspect the fresh log/files. Encode a sequence with:

```bash
ffmpeg -framerate 30 -i shots/look-review/look_motion/frame-%04d.png \
  -c:v libx264 -crf 18 -pix_fmt yuv420p shots/look-review/motion.mp4
```

Colour RMSE and mean edge delta have no acceptance threshold. They are useful
only when calibration and compared content are stable. A top view cannot prove
vertical silhouette; the locked view cannot expose every hidden face; four
rotations do not prove playability or collision unless the playable runtime is
tested separately. `--review-site` capture mode is nonplayable.

## Known failures

- One hero screenshot concealed hollow or malformed reverse geometry. The full
  four-rotation matrix is mandatory.
- A correct-looking different isometric angle reversed source ordering. Only the
  locked source quadrant is the acceptance comparison.
- Captures were previously described as accepted merely because they existed.
  Generation, inspection, and author acceptance are now separate evidence states.
- A workspace dispatcher returned `ok` while producing no process or files when
  its environment could not resolve `godot-mono`. Set the explicit system
  `PATH` in the dispatched command and verify fresh output files before waiting
  for or inspecting a capture.
- Late-night samples collapsed pale materials into navy silhouettes. Reference
  10 currently uses day-cycle time 0.80 for the locked night comparison; any
  change needs renewed visual evidence.
- Reference 1 initially emitted a complete, apparently valid matrix of water and
  distant terrain because its one-sector mosaic put the authored origin outside
  the 14-chunk meshable focus band. Do not move the calibrated camera to hide this
  symptom. Add real neighbouring sectors, recapture, and verify that the player
  and source landmarks occupy the registered pixels.

## Update triggers

Update for shot names/counts, distances, rotations, viewport resolution, camera
focus/yaw/pitch, day/night samples, overlay/difference algorithms, review-mode
playability, workspace-launch requirements, or acceptance criteria. Replace all
affected visual evidence rows after a new capture revision.
