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
sites whose complete footprints fit, then populates vegetation.

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

## Sites

Canonical topology decides whether a site runs in production. `Production` and
`Accepted` sites are overlaid; `Planned` and `Blockout` sites do not reserve or
alter normal terrain.

Each authored plan owns its footprint, levels, voxels, meshes, surface patches,
stairs and exclusion area. `ReferenceSiteBuilder` writes the plan into the
window after natural terrain and before vegetation. Its vertical datum is
translated onto the natural surface, but its authored proportions are not
rescaled.

Original sites explicitly set `isOriginalDesign` and `designSourcePath`; reference
sites retain their image contract. Tidekeeper’s Landing and Split Witness use
site-specific voxel blueprints through `AuthoredSiteWriter`. That helper only
writes declared cells and checks exact occupied projections. It never chooses
positions, masses or damage. Preserved terrain shapes remain untouched; written
dry surfaces update voxel columns and hydrology together before placed geometry.
Structures crossing a natural slope declare `measured-natural-foundation`.

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

## Player, camera and map

The controller uses terrain/collision for land and the active window callback
for water. Manual Shift is slow walk; route-owned travel uses the same cautious
speed. A dry route cell requires headroom in both terrain and placed voxels.

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
