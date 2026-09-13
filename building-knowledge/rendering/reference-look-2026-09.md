# Reference look — September 2026

- **Lifecycle:** `active`
- **Evidence summary:** mechanical invariants and named intermediate renders
  are verified below; the full requested visual target remains unfinished.
- **Scope:** shared renderer, with claim-specific evidence below.
- **Last verified:** 2026-09-06
- **Supersedes:** shader-only edge rounding and weak surface detail in this session.
- **Superseded by:** none
- **Owning sources:** [look target](../../docs/LOOK_TARGET.md),
  [mesher](../../src/Core/ChunkMesher.cs), [palette](../../src/Core/Palette.cs),
  [voxel shader](../../shaders/voxel.gdshader),
  [day cycle](../../src/Render/DayCycle.cs),
  [smoke checks](../../tools/LookRenderSmoke.cs).

## Source and boundaries

The thirteen supplied images and their identities are preserved in
`world-new/look-targets/2026-09-06/`. They require light-catching edges,
distinct material grain, clustered flora, clear water, warm/cool direct light,
readable night and atmospheric depth. Embedded guide text is reference content.
The site's measured composition, the accepted macro maps and global terrain
registration continue to own the world. This work adds no authored structures,
lamp placements, new sites or changed tree anchors.

## Current method

The September 13 [shadow stability pass](shadow-stability-2026-09.md) owns the
current clock cadence, cascade settings and softness control. Earlier shadow
settings in this ledger describe their dated captures.

- Blossom lobes retain their geometry, palette and per-block values. A new
  `canopy_surface.gdshaderinc` supplies shallow jittered petal facets and
  analytic gradients on the world face plane. The retained 6.5-cell/metre,
  0.0018-relief treatment fades before becoming subpixel; its coarser first
  candidate looked like pebbles in the inspected close coast view and was
  rejected. A leaf-only normalized 0.12 diffuse wrap and 0.94 roughness soften
  the direct response while retaining shadow/cloud attenuation. All six
  revised coast and six Bloom source previews were inspected. This is surface
  refinement only; it does not supply the references' richer crown geometry.
- Existing turf, hanging fringe and green ground-detail colours use one
  global-XZ dry/lush field in `turf_surface.gdshaderinc`, with restrained
  saturation and cooler living growth. A separate connected field groups
  fringe depth. The antialiasing footprint is taken before height wrapping,
  and close clods use smooth contours. The first `sward-bloom` colour-only
  candidate stayed too yellow; all six refined `matte-turf-bloom` and five
  `matte-turf-coast` source previews were inspected. The treatment changes
  no material membership, geometry, scatter or authored wear masks. The
  look smoke check and Linux export/actual-package atlas audit pass. Current
  package review and the remaining visual gaps are recorded below.
- Production meadow grass has three blunt bent blades per clump. Two segments
  share shoulder positions and wind weights, while upward normals preserve
  ground-like brightness from either side. A 0.25 m anchor range contains the
  rest shape within its dry supporting cell. Existing admission fields and
  random draw counts remain unchanged; stems and reeds keep crossed blades.
  Flower heads use shallow five-petal cups with two facets per petal. Every
  head vertex retains the stem tip's displacement. All geometry joins the
  existing chunk mesh. Six `bent-flora-bloom` and five `bent-flora-coast` source
  previews were inspected; the previous complete `scuffed-*` matrices predate
  these shapes. The extended meadow smoke fixture passes shared-window
  position/wind equality, support/exclusion and coincident-joint checks at
  three distant origins. Current package and visual evidence is recorded below.
- Rock-pattern voxel faces now derive small pale scuffs from their existing
  face UVs and exposed-edge bits. A continuous global field interrupts coverage,
  with 0.025–0.13 m width and fine one-sixth-metre contours. Coplanar joins are
  excluded; geometry, authored masks and collision do not change. Sand uses
  a separate broad deposit tone, the shared mineral grain texture and sparse
  warped wind ridges, filtered out when subpixel. Six `scuffed-bloom` and five
  `scuffed-coast` source previews were inspected. The first broad lip bands and
  strong sand stripes were rejected; see the surface knowledge entry. All 69 raw
  package views were subsequently inspected across the four review sets below.
  The Linux export passes its actual-package atlas audit.
- Upward faces of the existing `PAVING` material shade shallow one-metre cell
  joints. A continuous wear field interrupts them; there is no minimum seam.
  Pixel-footprint filtering removes subpixel joints, and slab centres use 30%
  of the ordinary stone normal relief. This adds no geometry or new material
  placement. The initial continuous grid was rejected as ceramic-looking;
  the interrupted version has 25 inspected `worn-slab-bloom-full` source views
  and 23 inspected `worn-slab-fallen-full` package views. Three raw Bloom orbit
  quarters were also inspected. This change postdates the complete `focal-air-*`
  matrices and their package timings below.
- Convex voxel lips receive a 0.09-block physical chamfer contained inside the
  authored voxel. Coplanar seams remain flat. Each lattice corner uses its full
  eight-cell neighbourhood; complex contacts taper to zero width. Faces share
  corner facets, including across chunk boundaries. Original unrounded surface
  triangles remain the collision source.
- The existing edge graph identifies concave cracks. Convex bevels supply their
  own light response rather than an unshaded line. Intermediate attempts to
  overlay the original outline made a floating wire cage; moving it inward
  still left visible stippling and was replaced by the bevel's actual silhouette.
- Broad material variation stays globally registered. An unsigned integer hash
  replaces multiply/fract noise that lost precision at large atlas coordinates.
  Fine detail fades at 85–240 blocks and is filtered when subpixel.
- `assets/materials/mineral-detail.png` supplies fine neutral mineral relief.
  Its original generated pixels and prompt are preserved in the adjacent README.
  Mirrored UVs and explicit gradients avoid texture-boundary value seams. The
  texture does not own material colour, macro wear, geography or illumination.
  A second, larger sample keeps metre-sized mineral islands visible at wide
  zoom. Snow combines crust with a blue side palette; sculpture blends three
  projections so carved planes do not change texture abruptly.
  `WorldMaterials` generates the mip chain once when the disposable import has
  none. Snow's glaze uses a minimum six-texel footprint before antialiased
  contrast shaping; ordinary mineral samples retain their derivative footprint.
  Existing moss-stone cells expose pale substrate through a continuous field,
  without adding growth to any other material or changing site masks.
  Normal relief is weaker on sand, snow and blossoms. Stone also filters the
  enlarged sample before applying broad colour drift and gates fine flake
  contrast by the worn field, so intact planes are quieter than worn patches.
  The enlarged voxel-stone sample is centred on the measured .660 mean at
  1.80 gain, with combined drift clamped to -.28..+.22; the previous .70 gain
  reduced its .046 source deviation to nearly invisible tonal changes.
- Colour literals are authored in sRGB. Already-linear palette values travel to
  shader uniforms as vectors; Godot's typed light/fog properties receive sRGB.
  This avoids decoding already-linear `Color` shader arguments twice.
- One `DayCycle` owns sky, key, ambient, fog and cloud uniforms. The key fades
  through zero before changing sun/moon ownership. The 0.24-high horizon ramp
  accommodates the stronger peach keys without abrupt quantised steps. Dawn
  and evening use weaker blue ambient fill with less pale-sky contribution: at
  .27/.33/.68/.76 the direct energies are 1.65/1.85/2.05/1.50, ambient energies
  .40/.40/.38/.40 and sky mixes .22/.35/.28/.20. Noon and midnight keys retain
  their prior values; interpolation remains in the existing palette owner.
  Cloud transmission dims the direct light; shadow opacity is not faded a
  second time. Sky fill remains available under clouds.
- Current ACES exposure is 0.72 and the daytime ambient multiplier is 0.42.
  The minimum depth-haze span is 115–430 blocks, curve 1.70. Production zoom and
  review share `Atmosphere.SetViewDistance`: begin max(115, distance×0.82), end
  max(430, distance×2.0), with source scale applied for the gate review. This
  keeps the subject near the clear end of the gradient at long zoom; the old
  .55 multiplier introduced substantial haze before the focal plane.
  Voxel ink fades by the cubed haze transmittance; sculpture outline expansion
  contracts by the same amount. Fog colour remains disabled on both outlines.
  A low-density world-space fog shader adds moving, lit mist around
  water level. Its only storage is the camera-bounded froxel buffer.
- Water retains the production mesh, depth, refraction and waves, with a blue
  depth ramp, broken moving foam, caustic contours and darker night volume.
  One half-resolution planar mirror now follows a nearby visible water elevation,
  using the active window's water data. The reflection's camera mask clips
  submerged solids and omits water/ink; other elevations and vertical steps keep
  sky reflection. The lookup preserves physical shoreline registration without
  the old fixture's screen stretch. This is one bounded reflection, not a render
  target for every water height in a window.
  Height changes now fade the old registered mirror fully out before changing
  elevation, then fade the new view in; transient selections can cancel cleanly.
- Ground details remain merged per chunk. Grass, flowers and petals cluster in
  broad fields. `AmbientDrift` owns two fixed MultiMeshes: 32 falling pieces and
  18 fireflies, resolving ground from the currently active window.
  Falling pieces use one eight-facet cupped lamina with a tapered root and
  notched tip, preserving the instance dimensions, spin, landing, random draw
  sequence and lifetime fade. `airborne_petal.gdshader` receives the shared
  cloud/shadow light with 0.24 diffuse wrap and clips the mirror plane.
  `AmbientDrift` converts authored sRGB pigment exactly once at spawn before
  uploading linear MultiMesh COLOR. The old unshaded box path left vertex
  colour conversion disabled; see Godot's
  [vertex colour encoding contract](https://docs.godotengine.org/en/4.7/classes/class_basematerial3d.html#class-basematerial3d-property-vertex-color-is-srgb).
  The extended `CheckDrift` checks the single surface, triangle budget, winding,
  cup/landing bounds, colour conversion and unchanged global particle positions.
  Existing moss-stone cap/placed faces also carry four-facet leaf clusters.
  Growth uses only exposed dry faces, stays within the source cell in plan,
  and adds no new mask, structure, collision, draw or shadow pass. Sparse cell
  enumeration uses only overlapping edit tiles and skips the cap overlap.
  Flower heads share the stem tip's wind phase/weight. Firefly quads billboard
  in the vertex shader for the camera rendering each pass.
  Two small stem leaves follow their attachment height's sway. The detail
  shader preserves the authored world normal before back-face handling; the
  upward normals used for grass must not flip with camera orientation.
  Natural snow, scree, sand and soil also supply one-to-three low mineral
  fragments through a 32-block deposit field. Broken corners and six bevels
  provide physical facets; rotated bounds remain inside the dry support cell.
  Their independent draw preserves the other scatter streams. Paving, masonry,
  rubble and authored moss locations are excluded. Separately, profiles whose
  existing detail set includes petals admit flat fallen-petal drifts on dry,
  uncovered `PAVING` caps. An 18-block continuous field and independent draw
  place at most seven petals/28 vertices inside each cell, clear of the bevel.
  They use existing petal colours and the same chunk detail mesh. They add no
  growth mask, rubble, collision, draw or shadow pass.

## Evidence

The current `review.html` presents the focused `folded-air-*-package` revision:
13 coast views, six Bloom views, two alpine views and two Fallen views. All 23
were inspected: coast in reduced matrices plus full-size play/probe frames;
all site/alpine views individually. Nine source coast previews were also reviewed.
The current coast orbit has 180 sequential PNG/CSV/encoded frames, 30 fps,
six seconds, 1600×900; frames 0000/0045/0090/0135 were inspected. No new site orbit,
clock sweep, traversal or every-frame visual acceptance is claimed. Current site
overlays were generated but not inspected. This effects change retains the
existing canopy, architecture and terrain. The face-stud and crenellated-corner
canopy experiments were rejected in agent review and removed; see the surface
method's Known failures.
All four current browser panels loaded, and coast motion rendered its start and
six-second endpoint. The app's separate open-panel request returned `queued`.


The preceding full comparison, preserved in `review-satin-canopy.html`, presents `satin-canopy-bloom-full` (24 raw views),
`satin-canopy-fallen-full` (23), `satin-canopy-coast-full` (13) and
`satin-canopy-snow-full` (10): 70 Linux-package views with the refined canopy
surface. All were inspected in labelled reduced matrices and/or individually
on 2026-09-06. Individual full-size review covers all six revised coast and six
Bloom source previews, package Bloom dawn, Fallen close r3, alpine play, coast
play, both performance stills and coast orbit frames 0000/0045/0090/0135.
Both sites cover five clocks and four distances/quarters; terrain covers five
clocks, four noon quarters and play/far. Alpine remains the mixed snow/scree
boundary at 4490,1882. All four capture logs and the separate coast probe log
complete without shader/runtime warnings or errors.

The coast orbit contains 180 sequential PNG/CSV/encoded frames at 30 fps,
six seconds and 1600×900. Its four sampled quarters keep the shallow surface
attached to the existing lobes. This is sampled appearance, not every-frame
acceptance or traversal. There is no new Bloom orbit or accelerated clock sweep
for the canopy revision. Site overlays were generated but not inspected.

All preceding sets below, including `matte-turf-*`, are historical evidence for
their named revisions. Reduced matrices establish broad readability, with fine
surface evidence limited to the individual frames above. Simple crowns, broad
square wear, sparse dressing, smooth sculpture and pale distance remain below
the references. No whole-site parity or author acceptance is claimed.

| Claim | State | Scope | Evidence | Remaining uncertainty |
|---|---|---|---|---|
| Folded airborne pieces retain light-responsive pigment instead of bright rectangular flecks | `visually reviewed` | focused effects revision | All 23 `folded-air-*-package` views and nine source coast previews; individual/matrix scope above, 2026-09-06 | Small distant particles are unresolved. This does not close canopy, dressing, architecture or lighting parity |
| The new particle mesh retains bounded runtime ownership and global continuity | `mechanically verified` | `AmbientDrift` | Extended `CheckDrift`: single surface, eight triangles, cup/landing bounds, normal winding, one sRGB conversion, two draws/50 instances and global handoff. `/tmp/petalfell-folded-air-smoke.log` passes | Headless inspects simulation and mesh data; captures cover actual GPU uploads |
| The folded-petal build exports and runs with similar sampled stationary GPU cost | `mechanically verified` | Linux package, RTX 3050 Laptop, 1600×900/4× MSAA, coast 6400,7360 | Export/actual-package atlas audit pass. Four capture logs plus probe log clean. Two 300-frame CSVs: GPU median/p95 8.414/8.642 ms day, 8.383/8.615 ms night; sorted indices 150/285. Main view 121 draws/154,546 primitives | 128 fewer primitives, but single warmed runs do not establish an isolated speedup or traversal/gameplay FPS |
| Shallow canopy facets preserve block forms across the sampled clocks and cameras | `visually reviewed` | Bloom, Fallen, coast and mixed alpine boundary | All 70 `satin-canopy-*-full` package stills in reduced matrices; individual previews/package/orbit samples listed above, 2026-09-06. All four browser panels loaded and coast playback rendered 0.00/6.00-second endpoints | The coarse first candidate was rejected. This surface refinement does not supply richer crown geometry, source composition, missing dressing or every-frame temporal acceptance |
| The canopy surface retains geometry counts with a modest stationary GPU cost increase | `mechanically verified` | Linux package, RTX 3050 Laptop, 1600×900/4× MSAA, coast 6400,7360 | `satin-canopy-coast-full/look_perf_day.csv` and `look_perf_night.csv`: 300 positive finite sequential samples each. GPU median/p95 8.442/8.790 ms day, 8.435/8.669 ms night, using sorted indices 150/285; 121 draws/154,674 primitives. Both stills inspected | Median is about 0.35/0.32 ms above the preceding turf run. Single warmed runs are not an isolated repeated A/B, traversal or gameplay-FPS benchmark |
| The package includes the canopy revision and its coast orbit is complete | `mechanically verified` | export/capture | `/tmp/petalfell-satin-canopy-linux-build.log`: export and actual-package atlas audit pass. Rendering smoke passes. Four capture logs and separate probe log have no shader/runtime warnings/errors. Coast orbit has 180 sequential PNG/CSV/encoded frames, 30 fps, six seconds | Build and capture completion do not establish visual parity or author acceptance |
| Shared turf/fringe/plant colour remains coherent across the sampled clocks and cameras | `visually reviewed` | Bloom, Fallen, coast and mixed alpine boundary | All 70 package `matte-turf-*-full` views in reduced matrices; eleven source previews, individual package views and four raw Bloom orbit samples listed above; 2026-09-06. The current comparison loads all four scenes and Bloom playback renders 0.00/6.00-second endpoints | Quieter olive/cool growth is retained. Broad cap/substrate bands, square wear masks, sparse dressing and pale distance remain below the references; sampled review does not establish every pixel or temporal frame |
| The turf shader retains geometry counts with bounded stationary coast timings | `mechanically verified` | Linux package, RTX 3050 Laptop, 1600×900/4× MSAA, coast 6400,7360 | `matte-turf-coast-full/look_perf_day.csv` and `look_perf_night.csv`: 300 sequential positive samples each. Probe median/p95 (sorted indices 150/285) 8.094/8.703 ms day, 8.119/8.282 ms night; 121 draws/154,674 primitives. Both stills inspected | Median is about 0.1 ms above the preceding bent-flora run, with a larger day tail. This is one warmed stationary run, not an isolated repeated A/B, traversal or gameplay-FPS benchmark |
| The preceding Linux package and Bloom orbit contain the retained turf revision | `mechanically verified` | export/capture | `/tmp/petalfell-matte-turf-linux-build.log`: export and actual-package atlas audit pass; all four capture logs complete without runtime warnings/errors. Bloom has 180 sequential PNG/CSV/encoded frames, 30 fps, 6 s | Completion and package availability do not establish source parity or author acceptance |
| Bent meadow blades and cupped flower heads remain globally attached and supported | `mechanically verified` | shared plant mesh | `CheckMeadowPlants` in `./tools/world-authoring.sh verify-look-rendering`, 2026-09-06: three distant overlapping windows; identical world positions/wind arrays; dry grass support around steps, water and blockers; coincident facet wind equality and upward grass normals. Build passes with zero warnings/errors | Invariants do not establish visual parity or live traversal |
| Fuller clumps and raised flower petals remain readable through the preceding turf camera sets | `visually reviewed` | Bloom, Fallen, coast and alpine boundary | 70 package views reviewed as described above; individual close/preview/orbit samples named above, 2026-09-06. Current review page loads all four scenes and Bloom playback reaches 6.00 seconds | Square paving masks, sparse shore/masonry dressing, smooth sculpture and distant palette remain below the references. Contact sheets do not prove every fine pixel or temporal frame |
| The preceding plant revision adds geometry without adding coast draws | `mechanically verified` | Linux package, RTX 3050 Laptop, warmed coast 6400,7360 | `bent-flora-coast-full/look_perf_day.csv` and `look_perf_night.csv`, 300 sequential positive samples each at 1600×900/4× MSAA. Main/mirror GPU median/p95 7.999/8.145 ms day and 8.020/8.237 ms night; 121 main draws/154,674 primitives. Both stills inspected | Adds 5,452 visible primitives and about 0.2 ms median over the preceding scuffed run; not an isolated repeated A/B or traversal benchmark |
| The preceding Linux package includes the plant shapes and the recorded orbit is complete | `mechanically verified` | export/capture | `/tmp/petalfell-bent-flora-linux-build.log`: export and actual-package atlas audit pass. Bloom orbit has 180 sequential PNG/CSV/ffprobe frames, 30 fps, 6 s; four current capture jobs complete without runtime errors | Export/capture completion does not establish source parity or author acceptance |
| The preceding exposed-lip/sand revision has bounded stationary coast GPU timings | `mechanically verified` | Linux package, RTX 3050 Laptop, warmed coast 6400,7360 | `scuffed-coast-perf/look_perf_day.csv` and `look_perf_night.csv`: 300 positive sequential samples each, 1600×900/4× MSAA. Combined main/mirror GPU median/p95 7.813/8.186 ms day, 7.810/8.266 ms night; 121 main draws/149,222 primitives. Both probe stills inspected | About 0.30/0.28 ms higher median than the earlier focal-air probe; this includes intervening changes and is not an isolated shader A/B. No traversal or gameplay-FPS claim |
| The preceding material comparison loads all four sets and plays both orbits to their ends | `mechanically verified` / `visually reviewed` for sampled playback | local page | CUA inspected all four `scuffed-*-full` scene panels; Bloom/coast showed rendered 0.00-second starts and 6.00-second ends on 2026-09-06 | Sampled playback is not complete temporal acceptance or realtime performance |
| Interrupted exposed-edge scuffs and fine sand grain remain coherent through the four material review sets | `visually reviewed` | two sites, coast and mixed alpine boundary | All 69 raw `scuffed-*-full` Linux-package stills and three raw quarter-turn samples per Bloom/coast orbit, inspected 2026-09-06 | Broad paving masks, sparse masonry and shore detail, smooth sculpture and pale distant shelves remain below the sources. No whole-world, temporal or author-acceptance claim |
| The Linux export includes the refined exposed-lip and sand shader | `mechanically verified` | package availability | `/tmp/petalfell-scuffed-linux-build.log`: actual-package atlas audit passes and export exits 0; all four new capture logs complete without shader/runtime errors | Successful compilation/capture is not aesthetic parity |
| Both preceding material orbit encodings contain every requested frame and matching metadata | `mechanically verified` | capture artifacts | `scuffed-bloom-full/bloom-orbit.mp4` and `scuffed-coast-full/coast-orbit.mp4`: 180 PNG/CSV/ffprobe frames each, 30 fps, 6 s; yaw 45..403, coast mirror 24.35 throughout | Camera sampling does not establish live traversal, realtime GPU cost or every-frame quality |
| The preceding lighting comparison loads all four scene sets and plays all three recordings to their ends | `mechanically verified` / `visually reviewed` for sampled playback | local review page | CUA checked all four `directional-*-full` scene panels and rendered 0.00-second starts followed by 6.00/6.00/12.00-second ends for Bloom/coast/snow on 2026-09-06 | Playback proves clip availability and sampled appearance; every-frame quality and realtime performance remain separate |
| Stronger peach keys and weaker blue fill preserve material and shadow separation across all four preceding lighting capture sets | `visually reviewed` | two sites, one coast focus and one snow/scree boundary | All 70 raw `directional-*-full` Linux-package stills, inspected 2026-09-06; five clocks and four quarters per scene, full site distances, terrain play/far views | Broad square paving wear, sparse masonry/shore detail, smooth sculpture and pale distant colour remain. The snow focus includes substantial scree; no whole-biome or reference-parity claim |
| The preceding Linux export includes the stronger lighting keys and continuous horizon handover | `mechanically verified` | Linux package and clock invariant | `tools/build-linux.sh` exits 0 after its actual-package atlas audit (`/tmp/petalfell-directional-linux-build.log`); all `verify-look-rendering` checks pass with the .24 ramp (`/tmp/petalfell-directional-clock-smoke-v2.log`) | Mechanical success does not establish aesthetic parity |
| Preceding lighting orbit/cycle recordings contain every requested encoded frame and matching metadata | `mechanically verified` / `visually reviewed` for named raw samples | preceding Linux Bloom, coast and snow/scree boundary | `directional-bloom-full/bloom-orbit.mp4` and `directional-coast-full/coast-orbit.mp4`: 180 PNG/CSV/encoded frames each; `directional-snow-full/snow-cycle.mp4`: 360. All 30 fps, 6/6/12 seconds; Bloom 1672×942 with one bottom padding row, terrain 1600×900. Raw orbit quarters 0045/0090/0135 for both scenes and snow 0088/0105/0180/0268/0285/0359 inspected | These are sampled temporal checks, not every-frame visual acceptance, live traversal or realtime performance |
| The preceding Bloom orbit records and plays the source-owned surface correction | `mechanically verified` / `visually reviewed` for sampled playback | package Bloom only | `source-wear-bloom-full/bloom-orbit.mp4`: matching 180 PNG/CSV/encoded frames, 6 s at 30 fps, 1672×942 (one bottom padding row). CUA showed rendered 0.00/6.00-second endpoints; three raw orbit quarters inspected | Sampled appearance and playback do not establish every-frame quality, traversal or realtime performance |
| Bloom's pale standing area and branching arch moss remain readable across clocks and camera quarters | `visually reviewed` | Bloom-authored surface masks only | All 25 raw `source-wear-bloom-full/` Linux-package stills and four derived comparisons, inspected 2026-09-06. Both plan audits, world audit, build and package atlas audit pass; arch occupancy retains 681 cells | Forty-three arch material values and 24 floor cells change. Other rectangular wear patches, sparse masonry and atmospheric depth still differ from the reference |
| Interrupted joints and small petal drifts remain on the existing paving through both complete site matrices | `visually reviewed` | Bloom and Fallen paving | All 25 raw `petal-paving-bloom-full/` and 23 raw `petal-paving-fallen-full/` Linux-package stills; Bloom orbit samples 0045/0090/0135, inspected 2026-09-06. Both package capture logs complete without shader/runtime errors | Joints are stronger under grazing light. Broad square wear masks, simplified masonry and sparse precincts still differ from the sources; no author acceptance |
| Paving petals retain dry support and world registration at window replacement | `mechanically verified` | shared interior at three distant atlas origins | `CheckPavingPetals`: matched global vertices, stepped PAVING caps, water/non-paving/blocker exclusions, bevel margin and at most seven petals/28 vertices per cell. Built `verify-look-rendering` passes, marker includes fragment/petal handoff | Fixture proves support and deterministic placement, not visual density or traversal performance |
| The preceding package includes the paving changes, Bloom mask correction and original atlas inputs | `mechanically verified` | Linux export | `tools/build-linux.sh` exits 0 after its actual-package headless atlas audit; current Bloom and retained Fallen captures load imported assets and produce source comparison outputs | Bloom derived comparisons were inspected; Fallen comparisons were not. Neither establishes structural parity |
| Earlier Linux coast rendering has bounded GPU timings with fragments, mineral gain and haze onset | `mechanically verified` | warmed stationary coast camera | `focal-air-coast-perf/look_perf_day.csv` and `look_perf_night.csv`, 300 positive samples each at 1600×900/4× MSAA; GPU median/p95 7.509/7.755 ms day, 7.526/7.773 ms night. 121 main draws/149,222 primitives; GPU includes the active mirror. Both stills inspected | No traversal, moving-window, whole-world or gameplay-FPS claim |
| Earlier focal-air recordings have complete metadata and play to their ends | `mechanically verified` / `visually reviewed` for sampled playback | earlier focal-air Linux coast and alpine boundary | `focal-air-coast/coast-orbit.mp4`: 180 frames/6 s; `focal-air-snow/snow-cycle.mp4`: 360 frames/12 s; matching PNG/CSV counts and ffprobe 1600×900/30 fps. CUA showed rendered starts and 6.00/12.00-second ends | Recorded capture is not realtime performance; every frame was not inspected |
| Earlier focal-air fragments, water beds and snow glaze remain attached in sampled motion | `visually reviewed` | nine raw motion samples | Coast orbit 0045/0090/0135; alpine cycle 0088/0105/0180/0268/0285/0359, 2026-09-06. Coast mirror remains 24.35; the dry alpine cycle has no mirror | Full temporal quality and travel between different water heights remain open |
| Natural fragments remain deterministic and supported across window replacement | `mechanically verified` | shared interior chunks at three distant atlas origins | `CheckSurfaceFragments`: stepped SOIL caps, water and blockers; identical global vertices in shared interior chunks, unit upward normals, dry support, at most three fragments/108 vertices per cell. Full `verify-look-rendering` passed | This fixture isolates geometry and ownership; it does not establish visual density or traversal cost |
| The later haze onset keeps foreground and middle-distance material separation clearer | `visually reviewed` | both complete site matrices and coast/snow boundary | All 69 raw `focal-air-*` stills, compared with the preceding `air-*` / `package-*` sets | Distant colour remains pale; the accepted atlas cannot reproduce the illustrative canyon backdrop |
| The earlier packaged Bloom scene has positive, bounded GPU timings at a warmed stationary camera | `mechanically verified` | Bloom performance camera only | `package-bloom-perf/look_perf_day.csv` and `look_perf_night.csv`, 300 frames each at 1600×900/4× MSAA on RTX 3050; GPU median/p95 6.843/7.613 ms day and 6.780/7.005 ms night; 156 main draws/403,710 primitives. Both accompanying stills inspected | This is a different location from the older coast probe; no direct regression comparison, traversal or gameplay-FPS claim |
| Camera-distance haze preserves the foreground at play/far zoom; coast beds and snow/scree separation remain readable at five clocks and four noon quarters | `visually reviewed` | earlier focal-air package coast and snow boundary | All 11 raw `focal-air-coast/` and 11 raw `focal-air-snow/` frames, 2026-09-06; every still passes the capture haze-span guard | Small facets remain attached to dry caps, but broad shores and exposed rock still look too clean and sparse. Snow view includes substantial scree. No whole-biome or author acceptance |
| Earlier package motion records complete sequences and plays to the end | `mechanically verified` / `visually reviewed` for sampled playback | capture/browser only | `package-coast/day-cycle.mp4`: 360 frames/12 s; `package-snow/snow-orbit.mp4`: 180 frames/6 s; both 1600×900/30 fps with matching PNG/CSV counts. CUA showed rendered starts/ends and 12.00/6.00 s | No realtime performance claim or assertion that every frame was inspected |
| Coast beds remain visible around horizon transitions/noon/midnight; snow glaze stays attached at the sampled orbit quarters | `visually reviewed` | nine raw motion samples | Coast cycle frames 0088/0105/0180/0268/0285/0359 and snow orbit frames 0045/0090/0135 in the package sets, 2026-09-06. Coast mirror remains 24.35 throughout; dry snow orbit has no mirror | Complete temporal quality and live transitions between water elevations remain open |
| Exported Fallen sculpture, shared materials and reference-image comparison load successfully | `mechanically verified` / `visually reviewed` for two raw frames | package Fallen only | Both raw `package-fallen/reference_match_day.png` and `reference_match_night.png`, plus successful imported-source comparison output, 2026-09-06 | This proves package asset availability and sampled appearance; source geometry mismatch and smooth sculpture finish remain |
| The packaged coast remains traversable outside the checkout working directory | `mechanically verified` | focused land/swim smoke | Exported `--verify-production-playability 6400,7360 water` launched from `/tmp`, 2026-09-06: 34.61 land blocks/550 frames and 10.06 swimming blocks/203 frames at water surface 24 | Automated input, not manual/controller or long-route validation |
| Filtered stone, attached leaf growth, restrained outlines and changing warm/cool light remain readable across both complete site matrices | `visually reviewed` | Bloom and Fallen only | All 24 raw `focal-air-bloom/` and all 23 raw `focal-air-fallen/` frames, 2026-09-06: locked day/night, five locked-camera clock samples, close/play/wide/far r0-r3; Bloom additionally top day | Bloom paving and wall growth remain too clean/square; Fallen face remains smoother and precinct sparser than source. Distant colour is pale. No new architecture or author acceptance |
| The earlier 90–370 minimum haze and matching outline attenuation removed isolated dark ink in the washed rear field | `visually reviewed` | historical site matrices | `air-bloom-full/` and `air-fallen-full/`, compared with rejected `haze-bloom/` samples | Atmospheric depth remains provisional; the lower accepted atlas cannot reproduce the illustrative canyon backdrop |
| Original atlas controls survive export and produce identical coast terrain | `mechanically verified` | source/package parity | Source and exported `--verify-production-terrain 6400,7360`, 2026-09-06, repeat digest `350d5f3db0cea866773f494a1491d5d9796a7e3869b9b4d9931bb65fc04a4068`; matching 442,368 safe overlap cells and 11,743 placed voxels | This is focused data parity, not a continent-wide performance or visual claim |
| Coast ground detail stays lit consistently across four noon directions and sampled night-orbit quarters; the water retains readable beds at all five clock samples | `visually reviewed` | coast only | All ten raw `finish-coast/` stills and orbit frames 0045/0090/0135, 2026-09-06 | The shoreline remains cleaner and emptier than the reference. Multiple-elevation travel remains unreviewed |
| The corrected night orbit records and plays for six seconds | `mechanically verified` / `visually reviewed` for sampled playback | capture/browser only | `finish-coast/night-orbit.mp4`: ffprobe 1600x900, 30 fps, 180 frames; matching 180 CSV/PNG rows, yaw 45..403, water 24.35 throughout; CUA rendered start/end and showed 0.00 then 6.00 s | This does not establish realtime performance or absence of every temporal artifact |
| The latest snow relief retains blue glaze and separation from lavender scree at close/far and noon/midnight | `visually reviewed` | four-view snow preview | All four raw `finish-snow/` frames, 2026-09-06 | Patch contrast and snow motion remain provisional; this is a mixed snow/scree view, not the complete biome |
| Quieter relief retains mineral flakes, attached flower leaves and light grass in the two close quarters; locked day/night and far forms remain legible | `visually reviewed` | Bloom five-view preview | `finish-bloom/reference_match_day`, `reference_match_night`, `site_close_r0`, `site_close_r2`, `site_far_r0`, 2026-09-06 | Stone albedo can still look crumpled. Rectangular wear and sparse masonry growth remain unresolved; this is not a complete new matrix |
| Restoring authored normals keeps the same meadow light on both sides | `visually reviewed` | coast only | All five raw `grass-normals-coast/` frames, noon r0-r3 and midnight, 2026-09-06 | Close flower leaves and the subsequent material-specific relief pass need fresh views |
| Water-height changes pass through zero reflection weight before moving the mirror; cancellation and loss of water recover continuously | `mechanically verified` | simulation invariant | `CheckMirrorTransitions` in `verify-look-rendering`, 2026-09-06; latest built smoke run passed without resource-leak warnings | Live travel across multiple water elevations remains unreviewed |
| Longer settling removes residual daytime petals from the night coast; the night orbit has clear beds and restrained luminous points at the four sampled quarters | `visually reviewed` | coast only | Three raw `settled-coast/` stills and orbit frames 0000/0045/0090/0135, 2026-09-06; 180-frame capture completed without shader errors | The reverse quarters exposed flipped grass shading, corrected afterward; these frames are not evidence for that correction |
| Filtered mineral finish and the half-pixel imported outline remain attached through the full Fallen material matrix | `visually reviewed` | Fallen only | All 18 raw `surface-fallen/` frames, 2026-09-06 | Cleaner precinct and smoother imported face still differ from the source; night particles predate the longer settling wait |
| Physical bevels are closed and outward-facing for all 255 nonempty 2×2×2 occupancy configurations across a chunk seam; collision retains the exact exposed voxel triangles | `mechanically verified` | tool-specific | `./tools/world-authoring.sh verify-look-rendering`, 2026-09-06; `CheckEdges` and `CheckBevelJunctions` | This local combinatorial check does not establish aesthetic quality or whole-world performance |
| All 4,097 clock samples preserve ambient light and bounded key-energy changes; sun/moon switches occur at zero direct energy | `mechanically verified` | tool-specific | `CheckClock`; the previous .18 ramp failed with the stronger peach keys at clock .26757812; widening to .24 passes all 4,097 samples without changing the invariant on 2026-09-06 | Still frames do not prove temporal sky/shadow quality |
| Window replacement does not move simulated airborne pieces and the pool is bounded to 50 instances/two draws | `mechanically verified` | tool-specific | `CheckDrift`, 2026-09-06 | Headless dummy rendering does not retain GPU MultiMesh transforms; this check inspects simulation positions |
| Physical bevels visibly soften pillar and wall corners; moving original convex ink onto the bevel still produces stippled outlines | `visually reviewed` | site-specific | `shots/look-2026-09-06/bevel-ink-bloom/atlas_play.png` and `look_noon.png`, 2026-09-06, production focus 9800,4600, yaw 45 | Intermediate ink has since been replaced; new texture and concave-only result require fresh review |
| Earlier light balancing gives clearer cast shadows and readable deep night | `visually reviewed` | site-specific | `shots/look-2026-09-06/v6-bloom/look_noon.png`, `look_sunset.png`, `look_midnight.png`, 2026-09-06 | Historical intermediate evidence only; the full reference finish is still missing |
| Close mineral detail reads on pillar and slab surfaces; the corresponding wide locked frame remains overly pale | `visually reviewed` | site-specific, intermediate | `mineral-bloom-site/reference_match_day.png`, `site_close_r0.png`, compared with target `11-bloom-dawn.png` | Led to the exposure/fill/haze revision; this is not current lighting evidence |
| Disabling fog improves middle-distance separation substantially; disabling the grade alone has a small effect | `visually reviewed` | diagnostic, intermediate | All four raw `post-probes/` frames at 9800,4600, noon, 170/45/38 | Diagnostic views must never be presented as the production result |
| Deeper blossoms and reduced fill/exposure improve separation through noon/sunset/midnight | `visually reviewed` | site-specific, intermediate | All four `value-bloom/` frames | The traveller was subsequently softened toward teal; large mineral islands and mist postdate this set |
| Snow sides are visibly blue beside lavender scree; wide shelves still lack reference-level variation | `visually reviewed` | site-specific | `frost-alpine/atlas_play.png`, `look_noon.png`, `look_midnight.png`; cap-count log confirms snow 14 and scree 18 in this frame | Close camera is on scree. A snow-only close view and current larger mineral sample still require review |
| Coast depth and shore remain legible through day/night; night transmission is too saturated | `visually reviewed` | site-specific, intermediate | All four `value-coast/` frames | Night transmission/chroma have since been reduced; needs fresh review |
| Lowland mist compiles and catches light while keeping foreground grass clear | `visually reviewed` | site-specific | `mist-coast/look_noon.png` and `look_sunset.png` | Motion and frame cost remain to be checked; this set predates darker night water |
| Land and water traversal still work after separating bevel rendering from voxel collision | `mechanically verified` | tool-specific | `verify-production-playability 2692,2164 land` and `6400,7360 water`, 2026-09-06: 44.15 blocks land, 34.61 land/10.06 swim | Does not cover every site or manual traversal |
| Current Bloom stone detail and physical edges remain coherent at close/play/wide/far distances and all four quarters | `visually reviewed` | site-specific | All 19 raw `current-bloom/` frames, including locked day/night and top | Paving remains too uniform, moss too rectangular and flora too simple; no author acceptance |
| Night water is darker and less saturated while retaining visible submerged shelves | `visually reviewed` | coast only | `current-coast/look_noon.png` and `look_midnight.png` | Water still lacks geometry reflections; wider biome evidence remains open |
| The running game produced a complete 360-frame clock sweep and 180-frame coast motion sequence at 1600×900 | `mechanically verified` | capture tools | `current-cycle/day-cycle.mp4` is 12 s/360 frames; `current-coast/motion.mp4` is 6 s/180 frames; matching CSV/PNG counts and ffprobe metadata | Fixed simulation rate is not realtime performance. The clock clip later played to 12.00 s in the CUA browser; start/end and six raw samples were inspected, not every motion frame |
| The clock sweep preserves readable terrain at sampled midnight, horizon transitions and noon | `visually reviewed` | Bloom samples only | Raw `current-cycle/look_cycle/frame-0000`, `0088`, `0105`, `0180`, `0268`, `0285` PNGs | Six inspected frames do not establish flicker-free playback or all-weather behavior |
| Half-pixel imported-sculpture ink removes the broad bands around crown teeth and legs | `visually reviewed` | Fallen Colossus only | All six raw `refined-fallen/` frames; source mesh/transform unchanged | See the site ledger for unrecaptured refined angles and source-fidelity gaps |
| The unfiltered large snow glaze survives at distance but creates high-frequency speckle | `visually reviewed` | intermediate snow only | All 11 raw `current-snow/` frames and five `refined-snow/` frames at 4490,1882 | Superseded by explicit mip generation and the separate glaze filter |
| Explicit mip generation removes snow's far speckle, but a sixteen-texel minimum footprint makes the glaze too airbrushed | `visually reviewed` | intermediate snow only | All five raw `mipped-snow/` frames | Led to the six-texel minimum and narrower, antialiased contour; that revision needs fresh review |
| Offscreen coast rendering has positive GPU timestamps and 121 visible draws/146,830 primitives | `mechanically verified` | warmed stationary coast only | `perf-coast/look_perf_day.csv` and `look_perf_night.csv`, 300 frames each at 1600×900/4× MSAA; GPU median/p95 6.186/6.444 ms day and 6.205/6.855 ms night | No traversal or window-handoff benchmark; hidden ordinary-window FPS was discarded as insufficient evidence |
| Moss-stone now shows irregular pale substrate within the original green material cells | `visually reviewed` | Bloom sampled views | `moss-bloom/reference_match_day`, `site_close_r0`, `site_far_r0` PNGs | These samples do not establish the full material matrix or source fidelity |
| Snow's six-texel glaze filter restores defined blue patches without the original fine speckle | `visually reviewed` | snow only | All twelve raw `glaze-snow/` frames: play/far, five clock samples, noon r1/r2/r3 and performance-camera day/night | Patch contrast remains provisional; stills do not establish temporal stability |
| The combined coast main/mirror GPU probe adds about 1.3 ms over the pre-mirror baseline | `mechanically verified` | warmed stationary coast only | `reflection-coast/look_perf_day.csv` and `look_perf_night.csv`, 300 frames each; combined median/p95 7.486/7.651 ms day, 7.517/7.821 ms night | Traversal and window-handoff costs remain unmeasured |
| The mirror's raw image contains inverted shoreline/character geometry and excludes the submerged bed | `visually reviewed` | coast diagnostic only | Both `reflection-probe/probe_reflection` PNGs, plane 24.35 at 800×450; noon/midnight and noon r1/r2/r3 in `reflection-coast/` | These low banks have physically short reflections; taller banks, plane switching and camera motion remain open |
| Surface points remain registered in the mirror across three water heights and four camera quarters at distant atlas coordinates | `mechanically verified` | transform invariant | `CheckMirrorRegistration` in `verify-look-rendering`, 2026-09-06 | Shader clipping and live multi-elevation transitions need rendered evidence |
| Current moss substrate and filtered stone remain attached through the complete Bloom material matrix | `visually reviewed` | Bloom only | All 19 raw `surface-bloom/` frames: locked day/night, top, close/play/wide/far at all quarters | Square ground wear and sparse wall growth remain prominent; near stone can still read too uniformly rough. Night particles in this set had insufficient settling time |
| The composited coast retains clear beds and short bank reflections through play, wide, quarter and day/night views | `visually reviewed` | coast only | All nine raw `reflection-coast/` frames | Night particle appearance predates the longer settling wait; taller banks and multiple water heights remain open |
| The coast mirror remains at 24.35 during a complete camera orbit; four raw quarter frames show registered shoreline and clear beds | `mechanically verified` / `visually reviewed` for the four samples | coast only | 180 PNGs and CSV rows in `coast-orbit/look_orbit/`, yaw 45..403; raw frames 0000/0045/0090/0135 inspected | Full continuous orbit playback and live multi-elevation travel remain open |
| The current water-motion clip plays through its six-second duration in the local review page | `visually reviewed` | browser playback only | `mirror-motion/water-reflections.mp4`, 180 frames, 1600×900, 30 fps; CUA browser showed 0.00 and 6.00 s with rendered start/end frames | This verifies playback and sampled appearance, not every intermediate frame or realtime game performance |

## Failures to avoid

- `haze-bloom` used 75–320/1.45 and washed the rear field too strongly. Six of
  seven views were inspected; the close view was not. The first `air-bloom`
  attempt also had a shader compile error and is invalid outline evidence.
  Shader built-ins must be used in the entry point and passed into the include
  helper. Only the later `air-*-full` matrices establish the fixed outline pass.
- The first Linux export completed but could not start: raw PNG existence checks
  rejected imported references, and CPU atlas loads assumed host filesystem
  paths. Preserve the four data PNGs as Keep File, use FileAccess for their bytes
  and headers, and resolve imported display references with ResourceLoader.
  The build now audits the actual package before reporting it ready.
- `export-coast/atlas_far.png` exposed a capture ownership bug: playable Follow
  rewrote the named shot's haze with the dormant player zoom every frame. The
  capture now owns Follow while active and checks its haze after settling.
  Those first export sets are intermediate evidence, not current haze validation.

- A double-sided grass shader can flip its deliberately upward normals on
  back faces. The night orbit exposed camera-dependent black tufts despite
  correct mesh normals. Carry the authored world normal into the fragment
  stage and restore it in view space; changing palette or density cannot fix it.

- Screen derivatives of a discontinuous pattern made diagonal triangle scars.
  Use world-plane height samples for relief.
- Restoring fog on unshaded ink produced bright halos; keep its fog disabled.
- A stronger quantised noise field looked like a printed grid. Fine mineral
  flakes must have irregular contours and remain subordinate to large forms.
- Colour and texture edits were too weak while colour uploads decoded twice.
  Verify the upload path before compensating by increasing palette saturation.
- A province label is not proof of the material visible in a frame. Terrain
  capture logs now report actual nearby cap IDs and the nearest snow sample.
- Capture generation, visual inspection, and author acceptance are separate.
  Nothing in this entry establishes whole-site or whole-game acceptance.
- A mip-filtering sampler cannot filter a texture imported without mipmaps.
  The initial generated asset had `mipmaps/generate=false`; earlier claims of
  mip filtering were source intent, not the runtime state. Explicit shared
  generation now logs the actual ten-level chain. `filtered-snow/` predates that
  fix and must not be treated as successful filtering evidence.
- Waiting only 24 frames after changing the clock is insufficient for particle
  fades on a fast GPU. Current stills also wait 2.5 simulation seconds; prior
  night stills can contain residual daytime petals and do not prove the settled
  effect. Runtime uses smooth fades during the ordinary 900-second day.

## Remaining target gaps

Material finish, vegetation massing, source-specific wear and architectural
fidelity, horizon/lowland haze, water reflections and special effects need
further source comparison. The current macro world is lower around Bloom than
the illustrative canyon backdrop; do not fake that backdrop in a capture or
move the accepted atlas to hide the difference. Recheck cold shelves, coast,
Bloom and Fallen Colossus at multiple distances and four rotations, with
dawn/noon/sunset/twilight/midnight and live motion before claiming completion.
