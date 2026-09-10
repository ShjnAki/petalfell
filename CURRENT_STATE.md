# Current state — 2026-09-10

This is a factual snapshot, not a design proposal.

## Accepted terrain foundation

The author has accepted the current production terrain level as the starting
foundation for further world and site work.

Normal startup opens the authored 12,288 × 9,216 atlas through a moving 1,536 ×
1,536 production window. The accepted land, elevation, water and region images
drive macro geography. The established low-level generator supplies layered
voxel ground, broken terrace edges, cliffs, gradual shores, submerged terrain,
biome materials, vegetation and sparse natural formations.

Water uses the shared animated translucent shader with visible depth,
refraction, broad movement and distance filtering. It is not a flat atlas plane.

All natural sampling is globally registered. A complete 165-window audit covers
every possible normal 2 × 2 ownership window and found matching safe terrain and
overhang ownership, no severe water steps and no submerged dry boundaries.
Representative land and water collision routes pass.

Walking near a window edge prebuilds the neighbour and preserves player, camera,
day and UI state. The current handoff compares the exact terrain/water and nearby
3 × 3 × 5 collision volume, removing the prior false invisible walls near water,
terraces and placed objects. Full-map Shift-click travel builds a distant window,
resolves a supported landing and closes the map on success.

## September 10 southern lowlands

The southern fen/shallows now forms a mushroom marsh: low grassy islets above
sea24, broken by local shallow channels. A 36/108-block field and two-block local
water raster break up the old broad land shelves; existing banks, beds, stairs
and collision still realize the result. Water has a muted grey-blue marsh
response, quieter refraction/foam/glints, lily-pad patches and stronger low mist.
A continuous region-distance weight and Z5000–6800 envelope ease the response
into central terrain. The four accepted control PNGs and permanent site addresses
are unchanged. The northern 4500,1900 terrain fingerprint remains exactly
`b5dd5bafbc962634b89f74b569e1edcd98a0e9ee7c798cc7b45f31b17d896fa0`, matching the
pre-change check on this machine.

Southern wetlands now use the dedicated fen detail profile rather than the
first central-river profile listing Wetland as a compatible biome. Unlabelled
near-shore water can inherit the nearest southern province within 128 blocks.
Large pale-stemmed mushrooms have broad stepped pink/purple caps, cream
undersides and top-only light patches. They are placed on supported dry ground
outside authored precincts. Ordinary tree admission falls to 1.5% in the fully
southern core. Wet banks use moss/soil/stone layers and silt beds; small mushroom
clusters, flowers, sparse grouped reeds and understory growth use ordinary chunk
detail. Clear ground beneath caps is a valid supported map landing.

Production and site review attach bounded ambient fish, herons and butterflies
through an active-window callback. At most six animals occupy deterministic local
habitat candidates, limited to three fish, two herons and one butterfly. Spawn and
retention radii are 288/384 blocks; zoom does not change population or apply a
mesh distance cutoff. They reject inappropriate depths, dry fish spawns and solid
obstructions, preserve global positions across walking handoff, and cull after
distant map travel. This does not activate the legacy fishing, pet or item loop.

Violet Threshold's great western and eastern runestones and Twin Rites' eastern
runestone and western tall survivor now have explicit local recesses/chips and
branching moss courses. Their footprints, stairs and structural arrangement are
unchanged. Tidekeeper’s Landing, Drowned Seal Gate, Violet Threshold and Twin Rites
pin their previous datums so natural relief changes cannot shift authored
waterlines or landings. Fallen Colossus is unchanged.

The extended rendering smoke checks reed support/exclusions across twelve shared
chunks, 14,718 mixed ecotone land samples, and wildlife habitat/population/live
handoff behavior. Focused repeat/overlap checks pass at 4500,1900, 6400,7360 and
6100,6600, each including 442,368 shared terrain cells. All nine measured site
collision checks and Shallows' separate bridge check pass; original props retain
their 23-instance / 2,752-triangle integrity checks.

Water-mode controller probes now test grounded travel without requiring a
height-changing route on a deliberately flat shore. Land-mode probes still
require terrace traversal; neither mode relaxes collision, distance or swimming
checks. The mushroom-marsh coast probe at 6400,7360 walks 23.53 blocks on Y26
and swims 10.07 blocks; Tidekeeper’s approach at 6481,7519 walks 40.04 blocks
across Y25..31. This southern visual revision requires separate author review
against the supplied mushroom-marsh image and does not inherit the September 2
terrain acceptance.

The full atlas sweep exposed Shallows' complete-footprint-only omission between
north/south windows. With the author's approval, intersecting windows now copy
only their owned columns and sparse edits from a complete, strictly validated
Shallows construction in a temporary bounded production context. This retains
all source assertions and the unchanged architecture. Focused terrain checks now
cover both axes; the previously mismatched `6255,7104` bed agrees. Shallows'
complete and clipped windows both pass bed18 / sea24 / deck87, underwater
clearance, exact landing and three physical surface probes. Temporary full-plan
construction adds preparation work in intersecting windows; no persistent terrain
cache or continent-sized allocation is introduced.

## Current sites

- **Bloom Grove Court** is a promoted production voxel transcription of
  Reference 10.
- **Fallen Colossus** is a promoted production precinct for Reference 12. Its
  head and legs are now from-scratch stepped stone GLBs built in Blender from
  authored anatomical profiles. They use 2,872 and 1,922 triangles respectively,
  Petalfell materials, partial stone courses and coherent moss. Their 17.513-unit
  head and 31.2-unit leg heights retain the preceding monument scale at the same
  site anchors. Static collision uses the actual mesh, including the open foot
  gap, instead of three oversized boxes. The original GLBs remain historical
  assets; production loads only the `fallen-colossus-authored-*` replacements.
- **Shallows Gate and Causeway** now builds in production at its existing
  6400,6980 address and earlier threefold scale. Its absolute threshold is 87,
  registered to the current sea at 24. The deck has an explicit open underside
  above tidal water. It remains
  unaccepted visual work.
- **Tidekeeper’s Landing**, at 6484,7528, is an original production shore worksite.
  Three connected stairs descend from the cut working yard through 31/29/27/25
  landings to the existing sea. A broken gauge wall, hollow catchment trough,
  capstan remnant, moorings and fallen masonry explain the former use. Four edge
  losses preserve the original ground and the remaining paving has authored
  reclamation patches. Its plan places 16 jars, coils, boards and fragments.
- **Split Witness**, at 4424,1928, is an original production northern outcrop.
  Two unequal fractured blades rise from the actual sloping ridge, with a fallen
  flake, broken wind shelter, small waymark and seven fine remnants. It performs
  no terrain surface writes; stone roots follow the existing column heights.

These original sites have explicit topology domains, design JSON, ground plans
and separate voxel blueprints. Their small shared prop meshes use the existing
palette, lighting and weathering path; pottery has exact surface collision.
No original site is compared against an unrelated reference image.

The terrain foundation is accepted; the completed visual fidelity of individual
sites is not.

## Additional reference precincts

The recovered September 6 instruction adds the large `world-new` reference sites
while retaining the current renderer and terrain foundation. Nine separate
metre-scale plans now build in production:

| Source | Site | Permanent atlas address |
|---|---|---|
| 2 | Drowned Seal Gate | 5760,7184 |
| 3 | Court of the Open Sky | 9560,5480 |
| 4 | Hollow Choir | 4212,1588 |
| 5 | Violet Threshold | 6052,8080 |
| 6 | Sanctuary of the Last Light | 4948,1420 |
| 7 | Arcade of the Leaning Pillars | 10340,5560 |
| 8 | Court of the Quiet Sign | 5120,1908 |
| 9 | Courts of the Three Crossings | 9780,6080 |
| 11 | Terrace of the Twin Rites | 6832,7340 |

Each plan owns its terrace polygons, exact treads, individual solid courses,
air cuts, rubble, surface reclamation and grounded blossom trees. The shared
`MeasuredReferenceSite` writes only these ranges. It contains no architectural
layout, ruin, arch or damage generator. Violet Threshold has one site-owned
animated opening, a local shadow-casting violet light and 36 motes, rebuilt with
the same atlas window lifecycle as the existing sculpture details.

`verify-reference-sites` exercises actual production windows, exact source
spawns, connected masonry and physics raycasts against the normal chunk
collision at every stair's central treads. Initial failures exposed floating
footings and walls blocking treads; the individual plans were corrected. Surface
reclamation now preserves each tread's actual height. See the
[reference precinct ledger](building-knowledge/sites/reference-precincts-2026-09.md)
for the latest validation and visual evidence. Production status is not author
acceptance or a claim of pixel parity.

The current Linux export and its own atlas audit pass with 14 permanent domains
and 14 sites. The nine new plans and the strict Reference 1 plan/camera/vertical
audits pass. Production collision checks cover all nine source spawns, connected
foundations, 181 central stair-tread raycasts and three separate Shallows bridge
surfaces. The bridge retains bed18, sea24 and deck87, with clear space beneath
the deck and a water query that reads the bed rather than the overhead bridge.

Real-controller probes pass at the Shallows approach (34.13 blocks, Y69..87),
Violet Threshold (32.83 land blocks, Y25..34, then 14.05 swimming blocks) and
Court of the Open Sky (19.24 blocks, Y44..54). These are bounded routes, not
exhaustive exploration. Focused repeat/overlap checks cover the eastern,
northern and shore site groups, including the final Shallows geometry. Original
site props, shared rendering and walking handoff regression checks also pass.

The [reference-site gallery](shots/big-sites-2026-09-06/review.html) contains
235 current raw views: nine 23-view matrices and a 28-view Shallows matrix.
Every frame was inspected in reduced matrices, with selected frames inspected
at full size. The final exported Violet Threshold day/night pair was also
inspected at full size. This confirms rendering and structural review, not
source parity. Fine masonry, rubble density, reclamation masks and transitions
into ordinary terrain still need refinement. Gallery generation and file
completeness passed; browser control testing was blocked by the browser
connection's trust configuration.

## September 6 worldbuilding validation

The original destinations and replacement Colossus were exported to the Linux
package. Their three 23-view galleries under `shots/worldbuilding-2026-09-06/`
were inspected across locked day/night, five clock phases and four distances at
four quarter rotations. Selected close/day/golden frames were also inspected at
full size. Full reference parity and author acceptance remain open.

`verify-worldbuilding` passes for 23 props / 2,752 triangles with closed outward
surfaces, actual production ground anchors and jar collision. `verify-sculpture`
passes for 4,794 matching visible/collision triangles, continuous outward hull
normals and a player capsule traversing the open foot gap. `verify-look-rendering`
also passes its geometry, clock, reflection and bounded-detail checks.

Both new addresses pass deterministic repeat and neighbouring-window overlap
checks. Real-controller land probes moved 33.65 blocks across Y25..31 at the
landing and 29.27 blocks across Y120..134 on the northern approach. These are
bounded automated routes, not exhaustive exploration. The subsequent reference
precinct export includes these sites; its current registration count and checks
are recorded above.

## Shared rendering revision

The 2026-09-06 image set and visual criteria live in [LOOK_TARGET](docs/LOOK_TARGET.md).
The production renderer now has narrow physical bevels on exposed lips, world-space mineral
and turf variation, a separate sand pattern, denser clustered flowers and grass,
broken shoreline foam and caustic contours. The water body follows night exposure.
The bevels stay inside authored voxels. Empty concave-ink meshes are disposed
immediately. Terrain topology, collision, site layouts
and tree anchors remain unchanged. Imported sculpture outlines expand by 0.50
framebuffer pixels so their width does not grow with the monument scale.
The from-scratch sculptures additionally carry continuous hull directions in
UV2 so split flat lighting normals do not tear that outline at shared corners.
Fine mineral relief also uses the generated
`assets/materials/mineral-detail.png` data texture at two scales; its prompt is
preserved alongside it. Sculpture uses a blended triplanar sample, snow has cool
blue sides and an icy crust, and blossom/character colours have deeper midtones.
The shared texture loader ensures a mip chain even when a fresh import has none.
Snow's larger glaze sample uses a minimum six-texel footprint and antialiased
contours. Existing `MOSS_STONE` faces expose a world-space pattern of pale
substrate within their authored material cells.
Surface relief is weaker on sand, snow and blossom caps, and varies with the
existing worn field on mineral faces.
The enlarged stone texture sample now filters out fine grit before supplying
broad mineral variation; fine flake contrast is concentrated in weathered areas.
The filtered stone sample is centred on the texture's measured .660 mean with
1.80 gain and a bounded -0.28..0.22 combined broad variation.
Convex voxel outlines are omitted from the GPU mesh; concave creases retain ink.
The voxel shader adds small pale scuffs on exposed stone lips using the
existing mesher edge bits and face UVs. Their world-registered wear field leaves
coplanar joins untouched. Sand separates broad deposit tone, fine mineral grain
and sparse shallow wind ridges. The Linux package includes this revision.
The two rejected broad-band candidates remain documented as failure evidence.

`DayCycle` drives direct sunlight/moonlight, sky, haze and global moving cloud
shade. Its direct key fades to zero at sun/moon handover across an absolute
solar-height ramp of 0.24. Dawn/evening keys use stronger peach light and
weaker blue ambient fill with less pale-sky contribution; noon and midnight
keys are unchanged. The 4,097-sample clock check passes with this ramp. Terrain, ground detail,
characters and sculpture use the same cloud field. Linear palette colours are
uploaded as numeric shader vectors; typed sRGB light/fog properties are encoded
at their boundary to avoid the previous double conversion.
The current ACES exposure is 0.72 and daytime ambient multiplier 0.42. Depth
haze has a 115–430-block minimum span and a 1.70 curve; shared view-distance
scaling extends that span for long-lens overviews in production and review.
Unshaded voxel ink fades in that same haze; sculpture outline width contracts
with it. Both retain their fog-disabled colour response.
One world-registered fog shader
adds drifting water-level mist to the camera-bounded volumetric buffer. It does
not allocate atlas-sized data or add density at mountain height.

Production attaches `AmbientDrift` outside the replaceable window: 32 falling
pieces and 18 fireflies in two MultiMeshes, sampled against the active window.
Their positions remain global across a handoff. Falling pieces now use an eight-facet cupped
lamina instead of a 12-triangle unlit box. A tapered root and notched tip supply
the outline; the material receives scene light and cloud shadows, with a 0.24
diffuse wrap. Authored sRGB pigment is converted once at spawn. The original
pool, random sequence, fall/spin/landing and day/night fade are retained.
Existing ground detail remains
one merged mesh per streamed chunk. Flower petals and centres now move with
their stem tips, with two small leaves attached along each stem. Ground-detail
normals retain their authored direction on both faces, preventing reverse-view
grass from darkening. Fireflies billboard against each rendering camera.
Production grass now has three blunt bent blades per clump, with matched wind
weights at the shoulder of each two-segment blade. Flower heads have shallow
five-petal cups. The meadow/flower admission fields, draw sequence and chunk
mesh ownership are unchanged. Six `bent-flora-bloom` and five
`bent-flora-coast` source previews were inspected on 2026-09-06; the close Bloom
views show fuller clumps and raised petals, while the four noon coast quarters
retain upward grass lighting. The complete current package review is recorded
below; `scuffed-*` captures remain evidence for the preceding plant shapes.
Dry exposed faces of existing `MOSS_STONE` cap/placed cells carry small folded
leaf facets in the same chunk detail mesh. The tile-bounded sparse query retains
AIR overrides and processes rewritten cells once; no new growth mask is created.
Dry snow, scree, sand and soil now carry low six-sided mineral fragments in
32-block deposit fields, with more accumulation on exposed terrace lips.
Clusters contain one to three pieces, stay within their supporting cell and
join the existing detail mesh. They add no collision or new site rubble.

Upward `PAVING` faces shade shallow cell joints only in a continuous worn
field, with quieter slab-centre normal relief. This does not alter voxels,
collision, macro wear masks or natural rock. The rejected continuous grid and
intermediate review evidence are recorded in the surface knowledge entry.

Existing turf, mesher-supplied fringe and green ground-detail colours now share
continuous global-XZ dry/lush fields and a restrained colour response through
`turf_surface.gdshaderinc`. A separate connected field varies hanging fringe
depth; fine clods are smooth rather than square-sampled. Fringe antialiasing
uses the world-height footprint before wrapping. All six `matte-turf-bloom`
and five `matte-turf-coast` source previews were inspected. The current package
matrix below includes this shared colour revision.

Blossom materials replace rectangular fine noise with jittered, overlapping
petal facets at 6.5 cells/metre, with 0.0018 analytic relief and pixel-footprint
filtering from 0.025 to 0.095 metres. Existing per-block values and palette
colours remain. A leaf-only normalized 0.12 diffuse wrap retains shadow/cloud
attenuation. Six revised coast and six Bloom source previews were inspected;
the first deeper, coarser canopy candidate was rejected as pebble-like.

Blossom-region ground detail now adds small fallen-petal drifts to existing dry,
uncovered `PAVING` caps. An 18-block continuous field owns their distribution;
an independent draw places at most seven flat petals inside each supporting
cell, clear of bevel lips. They join the existing chunk detail mesh.

Bloom's authored surface plan restores 24 pale paving cells in four lower-court
gaps, including beneath the traveller. Its arch builder replaces the earlier
short moss marks with three source-specific branching patches. Evaluation of
the before/after writes retains all 681 occupied arch cells; 43 material values
change. Source/runtime plan audits, world audit and a zero-warning build pass.

`verify-look-rendering` checks coplanar chunk seams, all 255 nonempty local
voxel junction configurations, unchanged collision faces,
4,097 clock samples including the horizon transition, particle continuity,
folded-petal triangle budget/winding/landing bounds and single colour conversion,
mirror registration at three heights/four quarters, reflection height fades,
and sparse edit ownership across 600 clipped chunk/tile query windows.
The fragment fixture checks identical shared-interior geometry after window
replacement, dry support around steps/water/blockers, normal direction and the
three-fragment-per-cell vertex bound at three distant atlas origins. The paving
petal fixture also checks shared-interior window continuity at three distant
origins, dry support, non-paving/blocker exclusions and the seven-petal bound.
The meadow fixture checks grass and flower geometry/wind continuity across
replacement windows at three distant origins, dry grass support around steps,
water and blockers, upward grass lighting normals, and identical wind weights
at coincident facet joints. The extended smoke check passes on 2026-09-06.
Land and water playability checks passed after the collision/render split
(2692,2164: 44.15 blocks; 6400,7360: 34.61 land + 10.06 swimming).
The visual revision is not author-accepted. Named capture evidence and remaining
gaps are recorded in [rendering knowledge](building-knowledge/rendering/reference-look-2026-09.md).
The local comparison page is generated by `tools/build-look-review.py` from
explicit raw captures and supplied references. Its current `folded-air-*-package`
sets contain 23 Linux-package views: coast 13 (five clocks, four noon quarters,
play/far, orbit still and two probe stills), Bloom six (locked day/night and
four close quarters), alpine two (play/noon at 4490,1882), and Fallen two
(locked day and close r3). All were reviewed on 2026-09-06: coast in reduced
matrices plus full-size play/probe views; every Bloom/alpine/Fallen view at full
size. Nine source coast previews were also inspected. These are focused effects
checks. The preceding 70-view full material/clock/distance matrix is preserved
in `shots/look-2026-09-06/review-satin-canopy.html`; it predates the airborne change.

The current coast orbit has 180 sequential PNG/CSV/encoded frames, six seconds
at 30 fps, 1600×900. Frames 0000/0045/0090/0135 were inspected at full size.
This is sampled motion evidence. No new site orbit, accelerated clock sequence
or traversal test accompanies this effect revision. Site overlays were generated
but not inspected. Simple crowns, broad wear masks, sparse dressing and source
composition remain open visual gaps. Rejected face/corner canopy-bud experiments
were removed; the retained canopy geometry is unchanged.

The export, actual-package atlas audit and extended rendering smoke pass.
All four package capture logs and the separate probe log are free of shader or
runtime warnings/errors. The coast probe at 6400,7360 has 300 sequential positive
finite samples per clock at 1600×900/4× MSAA. Combined main/mirror GPU median/p95
is 8.414/8.642 ms day and 8.383/8.615 ms night, using sorted indices 150/285.
The main view retains 121 draws and has 154,546 primitives, 128 fewer than the
preceding unlit-box revision. These single warmed stationary runs do not establish
an isolated speedup, traversal performance or gameplay FPS.
The refreshed comparison loads all four scene panels; browser playback rendered
the coast orbit's start and six-second endpoint.

One half-resolution planar mirror now follows nearby visible water. Submerged
geometry is clipped only in the mirror pass; water at other elevations and
vertical water-step faces retain sky reflection. Plane selection reads the
active window, and the mirror disables when no nearby visible water is found.
Height changes fade through sky reflection before moving the mirror plane.

Opt-in `look_orbit` and `look_orbit_night` record a 180-frame camera turn with
clock, yaw and selected reflection-height metadata. Stills settle for at least
2.5 seconds of simulation and 24 frames after a clock/camera change, allowing
the ordinary particle and reflection fades to finish on fast GPUs.

Opt-in `look_perf_day`/`look_perf_night` probes record 300 warmed frames of a
rendered offscreen viewport, with the window's duplicate 3D pass disabled and no
image readback during measurement. Main and active mirror GPU timings are
summed. Historical measurements remain in the rendering knowledge ledger;
traversal and moving-window cost remain unmeasured.

## Linux package

The four atlas control images use tracked Keep File import settings. CPU image
and PNG-header reads use Godot FileAccess inside the PCK; display-reference
checks recognize imported resources. The source and Linux package produced the
same terrain fingerprint at 6400,7360 after this correction. The build command
now requires a successful headless atlas audit from the exported binary.
`shots/.gdignore` excludes review images from Godot's import scan.
The package passed the coast land/swimming smoke from a working directory
outside the repository (34.61 land and 10.06 swimming blocks). Its locked
Fallen day/night captures load both original sculpture assets, the shared
material and imported comparison source. These two raw frames were inspected.
Terrain captures own their camera and haze while active; each still checks the
expected haze span after settling, preventing playable Follow from overwriting it.

## Controls and review

- W/A/S/D or arrows: move
- Shift: slow walk
- Space: jump
- wheel: zoom
- K: linear auto-zoom to maximum
- Q/E: orbit in review modes
- M: production atlas map
- Shift-click map: travel and close map after success
- tilde: developer settings

Camera obstruction does not alter zoom. The earlier obstruction pull-in/recovery
behavior was removed.

## Source organization

`src/Main.cs` now only dispatches headless authoring commands and the production
runtime. The retired circular-world assembly, local map and summit monument are
preserved under `reference/retired-code/legacy-world/` with non-compiling file
extensions. They no longer have runtime flags.

The active low-level terrain, water, render, voxel, vegetation, player and camera
classes remain in `src/` because production uses them. The extracted local shelf
field is now named `ProductionTerrainGrammar`; its stable noise salt strings were
kept unchanged so the rename does not move accepted terrain.

The old 3,456-world topology is archived under `reference/retired-data/` and no
longer loads or audits at startup. The atlas declares only the four source layers
the runtime actually consumes; speculative culture, abandonment and wilderness
raster placeholders were removed.

## Verified commands

The current tree builds with zero warnings/errors. The production checks include:

- repeatable focused terrain generation and overlap comparison;
- all-window terrain ownership audit;
- scripted land and water traversal;
- walking-window handoff planning and collision continuity;
- map transport/open-state behavior;
- fixed camera distance and K auto-zoom.

These checks establish mechanics. The terrain-level author acceptance is the
author's explicit decision; future site and final lighting acceptance remain
separate.

## Open work

1. Refine the source-specific masonry, ground transitions and detail of the
   implemented reference precincts, including Reference 1's heavy massing.
2. Continue comparing Bloom and Fallen Colossus against their references; obtain
   author review of individual sites before recording visual acceptance.
3. Traverse the accepted atlas for localized collision/route issues.
4. Continue reference comparison of materials, lighting and atmosphere, including
   source-specific wear and shape fidelity.
5. Build gameplay/story content on the accepted world foundation.

## September 7 Omarchy build migration

`tools/setup-nuget.sh` discovers the installed Arch/official Godot Mono
`GodotSharp/Tools/nupkgs` feed. The generated configuration clears inherited
fallback folders and uses the bundled Godot SDK plus nuget.org for other .NET
dependencies. Linux exports use the matching .NET templates in Godot's standard
user data directory; missing templates produce an actionable error. The package
launcher uses the system's normal library resolution.

On September 7, a clean Godot.NET.Sdk 4.7.2 / net8.0 build passed with zero
warnings/errors using Arch Godot Mono 4.7.2. The production atlas audit passed.
Headless controller checks passed at 2692,2164 (44.15 land blocks) and 6400,7360
(34.61 land blocks plus 10.06 swimming blocks). The movement checks used temporary
XDG data directories so sandboxed execution could write logs. No graphical
review was performed during this migration.

The copied editor executable override was cleared, and old compiled caches and
Linux output were moved outside the project to a temporary backup before the
clean build. The current machine has no matching .NET export templates installed;
`tools/build-linux.sh` correctly stops at that prerequisite, so a fresh standalone
package has not been verified. Historical capture logs remain unchanged.
