# Production atlas

## Extent and identity

Chapter 1 is one deterministic 12,288 × 9,216 block continent. Coordinates are
permanent. The atlas is divided into a 16 × 12 addressing grid of 768-block
sectors, but normal play materialises 2 × 2 sector-aligned windows rather than
persisted terrain sectors.

The grid is an allocation/streaming coordinate system. It is not visible terrain
and it does not divide provinces or sites into independent worlds.

## Authored sources

The canonical source set under `content/chapter_01/continent/` owns:

- `land.png` — land/water mass;
- `elevation.png` — broad height hierarchy;
- `water.png` — rivers, lakes, coast and wet identity;
- `region.png` — province/biome intent;
- `atlas.json` and biome catalog — dimensions, source registration and material
  profiles;
- topology JSON — domains, route graph, permanent sites and reference plans.

Generated map examples under `world-new/map/` informed these sources but do not
override them. Labels, generated roads and landmark density in those images are
not canon.

## Interpretation

The source images guide macro mass. They do not become literal vertical pixel
extrusions, hard biome cuts or perfectly traced banks.

`ProductionTerrainGuide` samples accepted values at global coordinates and
provides continuous displacement, transition and hydrology responses to the
existing low-level terrain system. The result keeps the intended mountain,
basin, river and coast positions while recovering organic local shelves,
separated layers, broken edges, gradual shores and underwater continuation.

Region ownership is categorical, but local surfaces transition through broad,
globally registered interlocking fields. Water identity remains deterministic;
its bank and bed shape are derived locally without inventing a new river route.

## Runtime window

A production window is 1,536 × 1,536 blocks with a global `VoxelGrid` origin.
Inside it:

1. the four macro sources are sampled;
2. `Planner`/`Terrain` generate local terrain and water;
3. data is described through `AtlasSectorData`/`AtlasSectorWindow`;
4. promoted sites are placed at permanent coordinates;
5. vegetation is populated after site exclusions;
6. nearby chunks and collision are streamed.

Every field/noise/material lookup uses global coordinates. Overlapping windows
must produce identical safe terrain and complete placed geometry.

### Southern lowlands

The September 10 mushroom-marsh treatment combines fen/shallows ownership with a
smooth Z5000–6800 geographic envelope. A 36/108-block field opens local shallow
channels between low grassy islets, sampled on two-block cells within the existing
macro hydrology lattice. Accepted coast/major-river registration, high northern
terrain and site addresses remain unchanged. The region-distance ecotone mixes
this response into central terrain; no hard latitude threshold changes height.
Low banks expose moss over soil and stone, with silt beds, sparse reed clumps and
lily pads. Giant mushrooms replace ordinary woodland in the southern core;
site-owned trees and architectural plans retain their separate authorship.

The September 13 pass varies near-water shoulder height through a continuous
72-block field within the same southern mask. It retains low landing tongues,
exposes more of the layered banks and does not change the marsh channel field.
The later ordinary stair pass still connects eligible land courses. Water jumps
can clear banks up to four blocks above the waterline under the tested conditions.

Unlabelled coastal water may inherit its nearest southern province within 128
blocks. This is a bounded, derived habitat classification, not a change to the
accepted region or water PNGs. All samples remain globally registered. Four shore
site registrations pin their existing vertical datums so lower natural ground
cannot shift authored stairs, monuments or water planes.

## Travel

Walking generates the neighbouring aligned window before the current edge is
unsafe. The exact current cell, surface, water column and nearby collision volume
must agree before replacement.

Shift-click map travel may choose a centred window and search near the requested
address, but the landing must have clear headroom and meaningful connected
support. Low islands can qualify through swimmable water; tiny isolated summit
chips do not.

## Terrain baseline

The accepted local vocabulary is:

- six-block registered terrace structure with broken layered edges;
- ordinary two-block courses plus deliberate stairs and monumental cliffs;
- broad connected mountain masses and separated slab layers;
- gradual bank courses and visible submerged continuation;
- translucent animated water with depth;
- biome-specific caps, substrates, geology and vegetation;
- sparse terrain-owned natural formations that preserve walkable ground.

The author accepted this terrain level on 2026-09-02. Later work may fix a local
defect or improve rendering, but must not replace the foundation with a second
compiler or literal source-boundary extrusion.

## Significant content

Topology owns site identity, location, status and connections. Only `Production`
or `Accepted` sites enter normal terrain windows. A site's complete plan must fit
its construction context. Shallows can cross the active window boundary: it is
built in a temporary bounded context containing the complete plan, then only the
active window's intersecting columns and sparse voxels are copied. Other sites
retain complete-footprint-only active-window placement. No builder may silently
truncate its authored plan.

Wilderness generation may scatter globally registered vegetation and natural
detail. It may not choose a settlement, ruin, monument, road, major stair or
story location.

## Verification

Use focused repeat/overlap checks during iteration and the bounded all-window
audit before accepting a terrain-system change:

```bash
./tools/world-authoring.sh verify-production-terrain X,Z
./tools/world-authoring.sh verify-production-playability X,Z land
./tools/world-authoring.sh verify-production-playability X,Z water
./tools/world-authoring.sh audit-production-terrain
./tools/world-authoring.sh verify-atlas-walking-handoff
```

The retired compiled-sector experiment and its historic metrics are preserved in
`reference/retired-docs/`; they are not the production workflow.
