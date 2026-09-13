# Architecture

This document defines production boundaries. Implementation details belong in
source comments; current evidence belongs in [`CURRENT_STATE.md`](CURRENT_STATE.md).

## Runtime composition

Normal startup has one path:

```text
Main
 ├─ WorldAuthoring (headless commands; exits)
 └─ production runtime
     ├─ accepted atlas sources
     ├─ bounded ProductionTerrainWindow
     ├─ promoted authored sites
     ├─ AtlasSectorWindow / VoxelGrid
     ├─ ChunkStreamer + collision
     ├─ water / materials / ink / atmosphere
     ├─ player + camera + developer controls
     └─ AtlasRuntimeHandoff + AtlasWorldMap
```

`Main` does not assemble an alternate world. Retired implementations are stored
outside the compilation tree under `reference/retired-code/`.

## World ownership

The atlas is 12,288 × 9,216 blocks. Four accepted images own land, elevation,
hydrology and region intent. Topology JSON owns domains, permanent site origins,
connections, status and reference-plan paths.

The images are macro controls, not block masks. `ProductionTerrainGuide` samples
them in global coordinates and supplies the existing local terrain grammar.
`Planner` and `Terrain` create shelves, broken terraces, banks, submerged beds,
materials and natural detail inside a bounded window. All randomness is a pure
function of world seed plus absolute atlas coordinates.

## Bounded windows

`ProductionTerrainWindow` owns a sector-aligned 2 × 2 window (1,536 blocks
square). It creates a local `VoxelGrid` with a global origin, applies production
sites, then populates vegetation. Ordinary site builders require complete
footprints. Shallows additionally supports clipped window ownership: its unchanged
complete blueprint is validated and built in one temporary 2 × 2 production
context, then only intersecting columns and sparse edits (including AIR) are
copied into the active window. The temporary context does not recursively build
sites or populate wilderness and is not retained. The strict source builder and
its full-footprint checks remain intact; this is not clipped authoring.

`AtlasRuntimeHandoff` chooses adjacent windows for walking and centred windows
for map travel. A replacement is built completely before it is installed.
Walking preserves the exact player transform and requires matching terrain,
water and nearby collision ownership; map travel may search for a supported
landing. Neither path changes canonical geography.

## Terrain and water

`Terrain` remains the low-level source of the accepted block language:

- six-block registered terraces and broken layered edges;
- deterministic stairs selected in atlas space;
- gradual bank courses and underwater continuation;
- translucent animated water with depth/refraction;
- biome cap, substrate and deep-column materials;
- sparse terrain-owned natural formations.

Production changes only its macro inputs. Do not replace these primitives with
literal map-pixel extrusion or a second terrain generator.

Water identity comes from accepted hydrology after the production displacement
and shore response. `AtlasSectorWindow` builds visible water geometry from that
data; the shared shader owns movement, translucency and depth. Collision and the
controller query the same window water columns.

### Southern lowland response

Southern terrain uses a continuous fen/shallows influence from the accepted region
map, multiplied by a smooth global-Z 5000–6800 envelope. Low mapped elevations
compress toward sea-level shelves; the high-elevation response fades out across
source elevation .64–.76. The existing shelf, stair and bank primitives remain.
A globally registered 36/108-block marsh field opens subordinate shallow channels
between low grassy islets. These local pools use a two-block raster inside the
existing six-block macro hydrology cells, then the same bank, bed and collision
pipeline. Near-water shelves rise gradually from sea24; wet banks expose moss,
soil and stone courses. Accepted source PNGs and major river/coast registration
are not rewritten. This is the author's mushroom-marsh revision, not the prior
tree-heavy coast treatment.

Unlabelled water pixels inherit the nearest southern province for at most 128
blocks, using a bounded distance patch with an expanded dependency margin. This
lets adjacent reeds, water dressing and fauna share a shore identity rather than
falling back to central meadow. Northern water ownership remains unchanged.
Southern Wetland selects the dedicated fen detail profile; the historical
northern profile lookup remains unchanged. A separate coordinate-seeded mushroom
pass runs before ordinary trees on an 18-block candidate lattice. It requires dry
root support and excludes authored precincts. Admission is .40–.72 through the
grove field, multiplied by southern influence. A bounded eight-neighbour query
replays candidate positions, admission and scale, then global priority rejects
cap envelopes closer than two blocks. It queries potential candidates, including
unplantable ones, to keep spacing independent of local generation order. The
original lattice and retained candidate coordinates do not move. Pale stalks, cream undersides and
layered spotted caps are voxel geometry, not recoloured tree crowns; the normal
mesher supplies both visible faces and collision. Ordinary tree admission falls
to 1.5% of its former rate in the fully southern core, blending through the same
influence. Small mushroom clusters and understory plants share chunk detail.
Clear ground beneath southern caps remains eligible for map landings.

The September 13 refinement adds a continuous 72-block bank-shoulder field within
that existing southern influence, without moving channels. Offset mushroom crowns
retain the same rooted construction and exclusion rules. The later mushroom
follow-up uses a two-block skirt (including the cream underside), a small thick
offset boss, one drooping quadrant, field-notched rim and low rooted pale feet.
Stems have two-/three-block sections and use `MUSHROOM_STEM` (38) with
`PatternFungus` (10). The voxel shader shades each world-registered cube with a
square tonal patch and no plank seams; architectural plaster is unchanged.
`GroundDetail` adds
cream-underside filaments and natural moss-bank roots inside ordinary chunk
meshes. Sparse cream cells are sorted in Z/X/Y order before emission, independent
of local edit-tile bucketing. Root and bead facets share their filament's wind
weights. Opaque detail vertex alpha .5 tags only terminal spore beads for weak
night emission; other detail uses alpha 1 and no emission. This adds no light,
node, material or draw per mushroom. Rounded notched lily pads retain per-column
water support; their shader samples bobbing phase in global XZ.

Southern sea-level water blends toward palette-authored marsh colours with
reduced refraction, foam and glints, retaining bed transmission and registered
reflections. The response fades by latitude and depth; low-altitude southern mist
uses the existing bounded fog buffer. No new per-pond renderer is allocated.

## Sites

Canonical topology decides whether a site runs in production. `Production` and
`Accepted` sites are overlaid; `Planned` and `Blockout` sites do not reserve or
alter normal terrain.

Each authored plan owns its footprint, levels, voxels, meshes, surface patches,
stairs and exclusion area. `ReferenceSiteBuilder` writes the plan into the
window after natural terrain and before vegetation. Its vertical datum is
translated onto the natural surface, but its authored proportions are not
rescaled.

A site with explicit `verticalDatumY` retains that absolute level instead.
Shallows uses datum87 with the preserved author-directed 3x source transform,
which places its measured sea at the production sea24.

Original sites explicitly set `isOriginalDesign` and `designSourcePath`; reference
sites retain their image contract. Tidekeeper’s Landing and Split Witness use
site-specific voxel blueprints through `AuthoredSiteWriter`. That helper only
writes declared cells and checks exact occupied projections. It never chooses
positions, masses or damage. Preserved terrain shapes remain untouched; written
dry surfaces update voxel columns and hydrology together before placed geometry.
Structures crossing a natural slope declare `measured-natural-foundation`.

References 2–9 and 11 use `MeasuredReferenceSite`: each plan stores its own
inclusive XYZ/material courses and air cuts. `MeasuredCourseSchedule` checks
the final solid projection, material IDs and bounded course volume before
construction. Only explicit tree records may use a ground-relative anchor.
Reclamation repaints the current tread/terrace height. It cannot restore a
parent terrace through a staircase. No helper chooses the site's composition.

Overhead bridge decks remain sparse solids above wet terrain. Atlas hydrology
retains the actual submerged bed; landing support follows an exposed solid
deck when present. The controller's water callback finds solid ground at or
below the waterline, so a high deck cannot disable swimming underneath it.

`AuthoredSiteProps` attaches only the plan's explicit small-prop records, with a
64-instance per-site limit. Meshes and finishes are shared within a site, grounded
against the active window and rebuilt on window replacement. Closed faceted jars,
broken rims, rope coils, split boards and fragments use existing palette colours
and the sculpture finish; timber adds object-space grain. Jar collision follows
its actual hollow mesh. Normal production and review use the same attachment.

Fine sculpture GLBs are prepared deterministically and assigned Petalfell
stone/ink. Fallen Colossus now uses from-scratch metre-scale stepped carvings
from `tools/build_fallen_colossus.py`, with static triangle collision taken from
the same mesh under the same transform. No broad invisible collision boxes remain.
They supplement site-owned voxels; they do not replace the measured site plan.
The sculpture outline expands in framebuffer pixels. Its authored UV2 channel
contains a shared octahedral hull normal at hard face splits, while ordinary
flat normals continue to light the stone. Re-entrant corners that have no outward
average are pinned. UV1 carries metre-coordinate stone courses projected before
the head's fallen rotation; UV2 must not be regenerated as a lightmap unwrap.

Violet Threshold owns one animated opening, one local light and 36 GPU motes.
`VioletThresholdEffect` attaches through the same fine-site lifecycle used by
review, production startup and window replacement. Rose and amethyst mineral
accents use the common rock shader; the active opening alone uses an unshaded
local shader. It adds no transport behavior.

## Rendering

`ChunkStreamer` materialises only nearby chunks. `ChunkMesher` reads the voxel
grid, ground detail and overhang ceiling, then optionally builds collision.

One material pipeline is shared across terrain and sites:

- `voxel.gdshader` — world-space colour breakup and material response;
- `ink.gdshader` — analytic concave voxel creases and existing box-prop edge passes;
- `water.gdshader` — animated translucent depth/refraction;
- `cloud_light.gdshaderinc` — a global cloud field modulating direct light only;
- `mineral_surface.gdshaderinc` — shared scalar mineral texture sampling;
- `turf_surface.gdshaderinc` — continuous dry/lush colour shared by existing turf, fringe and green plants;
- `canopy_surface.gdshaderinc` — filtered shallow petal facets inside the existing blossom material;
- `lowland_mist.gdshader` — globally registered water-level banks in the bounded fog buffer;
- `PlanarReflection` — one half-resolution mirror following a nearby visible water elevation;
- `DayCycle`/`Atmosphere` — ordinary day and night lighting, horizon handover and weather;
- `DeveloperMenu` — review-only live parameters.

Direct light and weather advance at render frequency; only sky-material
radiance updates use the 1/2048-day bucket. The frozen clock skips unchanged
lighting writes. Render-driven sun, gameplay camera and reflection camera opt
out of physics interpolation. Camera orientation comes directly from orbit
angles, avoiding precision loss from subtracting large atlas coordinates.
Directional shadows use an 8192 atlas, four blended ranges and a developer
softness control. Zero selects hard filtering; positive values use Ultra PCF.
Depth bias compensates for the renderer's blur/radius multiplier, keeping the
surface offset constant across the control. See the
[shadow stability record](building-knowledge/rendering/shadow-stability-2026-09.md).

The shared mineral texture has an explicit mip chain, generated once if the
disposable import lacks it. Voxel and sculpture materials share that bounded
texture. Moss substrate breakup is confined to existing `MOSS_STONE` cells;
the shader cannot place growth outside the authored material mask.
Blossom surfaces retain their per-block values and add world-plane petal
reflectance/analytic relief, filtered before becoming subpixel. Only leaf
materials enable a small normalized diffuse wrap in the shared light function;
shadow and cloud attenuation still apply. This adds no geometry, allocations,
texture, material placement or vegetation anchors.
Only upward `PAVING` faces receive shallow, field-interrupted cell joints;
their screen footprint filters subpixel lines and their normal grain is quieter.
Rock-pattern surfaces use the mesher's existing face UVs and exposed-edge bits
to confine small pale scuffs to real lips. Continuous world fields interrupt
their coverage; coplanar joins cannot create scuff seams. Sand separately has
broad deposit tone, filtered mineral grain and sparse shallow wind ridges.
Existing turf caps and their mesher-supplied hanging fringe share global XZ
colour fields. The fringe retains its palette identity while receiving the same
surface treatment; a separate connected field varies its depth. Green vertex
colours in the ground-detail mesh share the turf tint. Flower and mineral colours
keep their original palette. These fields do not change material membership,
growth placement, terrain topology or authored wear masks.

The reflection camera omits reserved water layer 20. Its distinct cull mask also
identifies the reflection-only submerged clipping in solid shaders; ordinary
cameras keep water layer 20. Other water elevations and step curtains use the
live sky. Plane selection reads at most 289 active-window columns five times a
second. A changed height fades the registered mirror out before moving its
plane, then fades the new reflection in; a dry view fades out and disables it.
It never changes hydrology or
allocates a render target per river/lake. Capture review uses this same mirror
with its capture camera; performance probes sum both active viewport timings.

Authored sRGB colours are converted to linear exactly once. Shader noise and CPU
noise must remain globally registered so moving-window ownership is invisible.
`Palette.ShaderRgb`/`ShaderRgba` upload already-linear colours as numeric vectors,
not typed `Color` variants that Godot would decode again. CPU-interpolated light
and fog colours are encoded for Godot's sRGB properties.

The surface mesh carries face UVs and exposed-lip bits in UV2. The mesher adds narrow physical chamfers
inside exposed convex lips, with common corner decisions across chunk aprons.
Coplanar seams remain flat and collision retains original voxel triangles.
Convex voxel strips are omitted from the ink mesh; their physical bevel supplies
the highlight and silhouette. `AmbientDrift` lives outside window content and resolves the active
window through a callback, bounding airborne detail to two draws/50 instances.
Falling pieces share an eight-facet cupped lamina, with biome width/length,
existing spin and ground-aware landing. `airborne_petal.gdshader` uses the shared
cloud/shadow light function and mirror-plane clipping; alpha only fades lifetime.
Their authored sRGB colours are decoded once at spawn for linear MultiMesh COLOR.
Firefly billboards face each rendering camera in their vertex shader, including
capture and mirror cameras. Flower heads share the stem tip's wind weight so
petals and centres remain attached while swaying; stem leaves use their attachment
height's weight. Ground detail preserves authored world normals before back-face
handling so its upward-lit blades remain consistent through camera rotations.
Production meadow grass uses three blunt bent blades per clump. Each blade has
two segments with identical displacement at the shared shoulder. Clump anchors
stay within the supporting grass cell; existing meadow admission fields and
random draw counts are unchanged. Stems and reeds retain the crossed primitive.
Flower heads form shallow five-petal cups, with two facets per petal and one
shared head displacement. Both shapes join the existing chunk detail mesh.
Existing moss-stone cap cells and sparse placed cells also supply folded leaf
facets on their exposed, dry faces. `VoxelGrid.PlacedIn` visits only overlapping
edit tiles; cap/overlay overlap is processed once. This detail stays within the
authored mask, merges into the ordinary chunk detail mesh and adds no collision.
Dry snow, scree, sand and soil caps supply low faceted mineral fragments through
a globally registered 32-block deposit field. Each cluster remains inside one
supporting cell; it cannot bridge a step or grow on water/blocked caps. This
natural detail uses an independent random stream and the existing chunk mesh,
with no site masonry placement, collision, extra draw or shadow pass.
Blossom profiles also allow small fallen-petal drifts on exposed dry `PAVING`.
An 18-block field and independent coordinate draw place at most seven flat
petals inside each cell, clear of its bevel; these join the existing detail mesh.
`Atmosphere.SetViewDistance` owns the depth-haze span for both production camera
zoom and review cameras; capture no longer carries a separate haze formula.

Southern reeds are emitted before the submerged-cell rejection in `GroundDetail`.
They root on the actual bed, have bent leaves and shared wind weights at their
seed heads, and are restricted to shallow, unobstructed reed-profile cells.
Dry wetland herbs and bank reeds use the existing merged chunk detail mesh.

`Fauna` has a production-window callback alongside its historical terrain API.
Production attaches one `SouthernWildlife` node outside replaceable window
content. At most six animals (three fish, two herons, one butterfly) occupy
coordinate-seeded habitat candidates. The production spawn radius is 288 blocks
and retention radius is 384 blocks, with no mesh distance cutoff; camera zoom
neither increases population nor removes existing animals. Fish require clear
submerged space, herons require
shallow water or wet banks, and all species reject unsuitable profiles and placed
obstructions. Animals retain global transforms across walking replacement and
are culled after distant travel. Their materials use the shared character light,
cloud and mirror clipping path, with one sRGB conversion. No legacy inventory,
fishing, pet or settlement assembly is activated.

## Player, camera and map

The controller uses terrain/collision for land and the active window callback
for water. Manual Shift is slow walk; route-owned travel uses the same cautious
speed. A dry route cell requires headroom in both terrain and placed voxels.

Space launches a surface water jump with `WaterJumpVel` once per press. A rising
water jump bypasses buoyancy until descent returns it to the active water surface;
gravity and `MoveAndSlide` retain ownership throughout. Launching consumes jump
buffer/coyote state and respects the input gate. No bank teleport or collision
bypass is used. `verify-water-jump` checks physical bank, wall and ceiling cases.

`Controller.Steps.cs` owns supported up/forward/down capsule sweeps for treads
up to 1.05 blocks and short descending floor snaps. Actual contact height separates
low treads from taller auto-jump ledges; available headroom bounds the lift.
A verified corner crossing temporarily permits its steep capsule contact normal,
then restores the ordinary slope limit. Walking speed remains owned by ordinary
movement input and route settings. Manual jumps and swimming retain their paths.

The character retains single-piece legs and its ordinary walking animation.
Boot placement owns sole alignment to the controller's feet origin; it does not
move the collision capsule or add a height offset to the whole character.
Its visual root is `TopLevel` with physics interpolation off, so the manually
presented world transform is not composed with an interpolated physics parent.
`Controller.AdvancePresentation` supplies one interpolated position with eased
step height to both the character and normal follow camera in `AtlasSectorReview`;
teleports reset its vertical history. Boundary corrections run before sampling
that frame's shared pose. Stair motion has no separate leg or pelvis
solver.
`verify-stair-walk` exercises the production controller and rig on complete
quarter-, half- and one-block flights, full-speed input, rotated fixtures, taller ledges, roofs,
manual jumping and stopped/disabled input. `verify-player-motion` runs the same
fixture on the GPU at 144 render fps against 60 physics ticks, checking the
post-draw character transform against the shared presentation position.

Camera distance is player-owned. Nearby geometry may occlude the traveller but
must not change zoom. Wheel input changes distance; `K` linearly moves to maximum
at the developer-configured speed.

`AtlasWorldMap` renders accepted macro sources and canonical topology. A
successful Shift-click transport closes it after committing the replacement;
failed transport leaves it open.

## Data boundaries

- Authored: atlas images, topology, reference plans, site meshes and settings.
- Derived in memory: terrain windows, collision, dressing and map textures.
- Historical: anything under `reference/`; never loaded by runtime.

Generators never rewrite authored data. Runtime correctness must not depend on a
persisted terrain cache.

The four macro control PNGs retain their original bytes in exports through
tracked `Keep File` import settings. CPU decoding and encoding validation use
Godot `FileAccess` for both disk and PCK paths. Display references instead accept
imported-resource remaps; their texture representation never supplies geography.

## Verification boundary

Build success proves compilation. Headless terrain checks prove deterministic
data, overlap and scripted collision behavior. Captures prove only that an image
was rendered until someone inspects it. Only the author can accept visual work.
