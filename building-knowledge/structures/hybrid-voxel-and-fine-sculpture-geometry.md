# Hybrid voxel and fine sculpture geometry

- **Lifecycle:** `active`
- **Evidence summary:** from-scratch Reference 12 stepped carving, metre pivots,
  mesh-matching static collision and packed shared hull normals are mechanically
  checked; v4 locked day/midnight are `visually reviewed` for silhouette and stone
  readability. Earlier supplied-asset evidence below is historical. Broader
  reference-family use remains a `candidate`
- **Scope:** `site-specific` evidence for Reference 12; candidate low-level
  method for future statuary, collapsed diagonals and carved monumental forms
- **Last verified:** 2026-09-06 for shared stone and pixel-width ink; older placement evidence remains scoped below
- **Supersedes:** the rejected all-integer and provisional fractional-cuboid
  sculptures; the supplied Meshy runtime assets were replaced by explicit user
  request on September 6
- **Superseded by:** none

## Why this layer exists

Voxel style does not require every visible form to be a one-metre axis-aligned
cube. Terrain, foundations and load-bearing masses benefit from the integer
`VoxelGrid`: they stream, collide, mesh and seam with the world. A face, bent
crown, ankle, diagonally fallen beam or broken sculptural plane needs finer
units. Enlarging one integer box for every feature produces towers, masks and
forts instead of sculpture.

The current split is:

1. Keep the clearing, stairs/plinths and stone court in the strict site-owned
   voxel plan; do not bake the imported object's accidental ground into terrain.
2. Run `tools/build_fallen_colossus.py` in background Blender. It authors the
   anatomy/crown from explicit profiles and negative cuts, realizes a bounded
   0.8-unit leg / 0.7-unit head carving lattice, merges adjacent solids, resolves
   diagonal-only contacts and removes coplanar cell edges. It imports no mesh.
   Bevel only simple exposed corners: a whole-skin bevel on complex re-entrant
   step junctions created non-manifold output and is rejected. Both export
   surfaces must be closed with positive signed volume and within budget.
3. Attach each subject independently at the permanent atlas origin. Scale and
   yaw remain explicit site measurements; the import step does not choose them.
4. Override every imported `MeshInstance3D` with Petalfell's world-space
   sculpture stone. Add a restrained inverted-hull plum outline because the
   ordinary voxel ink mesh expects edge-run custom channels an arbitrary GLB
   does not contain. Expand that hull by 0.50 framebuffer pixels, independent
   of the source asset's local units and placement scale. Project the transformed
   normal using Godot's [spatial vertex built-ins](https://docs.godotengine.org/en/4.7/tutorials/shaders/shader_reference/spatial_shader.html).
5. Use each bounded static mesh's actual triangle surface for collision below
   the same transform. The previous broad boxes blocked empty areas beside the
   carving; visible voxel shells also poked through it. The current test checks
   the actual imported faces and runs rays/player-sized capsule sweeps against
   the live physics world, including the gap between the feet.
6. Rebuild the fine node with every moving atlas window that contains the
   production site. It is derived runtime geometry, never a floating preview.

This is low-level geometric vocabulary only. It must not become a reusable
statue, head, stair, arch or ruin generator. Each reference continues to own its
part dimensions, transforms, damage and silhouette.

The sculpture's UV1 carries metre-coordinate partial stone courses projected
before the fallen rotation. UV2 carries octahedral shared hull normals encoded
in Godot's basis; Blender's glTF V flip is explicitly compensated. Lighting
normals remain flat. Deep re-entrant corners with an inward average use a
negative-X sentinel and stay pinned. The import/physics smoke rejects missing
channels, wrong-basis normals, shared-corner disagreements or excessive pinning.
The half-pixel hull still obeys the world's existing distance haze.

## Evidence

| Claim | State | Scope | Evidence | Remaining uncertainty |
|---|---|---|---|---|
| The current head and legs contain no imported geometry and use 2,872 / 1,922 triangles with closed positive-volume surfaces | `mechanically verified` | Reference 12 | Blender build and `assets/sites/fallen-colossus-authored-audit.json`, 2026-09-06 | Counts and manifoldness do not establish artistic fidelity |
| The current stepped forms, stone courses and restrained moss read through locked daytime and midnight | `visually reviewed` | Reference 12 | `shots/worldbuilding-2026-09-06/colossus-v4/reference_match_day.png` and `site_midnight.png`, inspected at full size | Full 23-view package matrix subsequently reviewed; the court remains sparse and source parity is open |
| Fine geometry is attached in initial production play and both map/walking window replacements | `mechanically verified` | tool-specific | `Reference12SculptureDetail.cs`, `AtlasSectorReview.AttachFineSiteGeometry`, passing `dotnet build --no-restore`, 2026-08-31 | Live player-controlled handoff still needs review |
| Meshy calibration cubes, low image-floor components and baked materials are absent from the normalized head/legs assets | `mechanically verified` | import-specific | `tools/prepare_meshy_fallen_colossus.py`; head source `2abd6a29…`, legs source `0820092a…`; successful Godot reimport, 2026-08-31 | A differently generated asset needs new measured cutoffs |
| Petalfell stone and a 0.009-unit silhouette outline preserve the imported facial/crown and anatomical leg geometry over a massive site-owned voxel foundation; the legs face the source-forward axis at yaw 0 and use 1.5× their first imported review scale with matching collision | `visually reviewed` | site-specific | `/home/shikhar/godot/shots/reference-12-v25-legs-1_5x/reference_match_day.png` and `site_far_r0.png`, inspected at original size 2026-08-31 | Night response, hidden rotations and live collision still need author review |
| Hybrid sculpture is suitable for every future reference | `candidate` | reference-family | Reference 12 proves one statuary case | Needs a second distinct reference and collision/play review |
| Mineral detail blends continuously across carved planes; 0.50-pixel hull expansion removes the thick crown/leg bands produced by the old 0.009 local-unit expansion | `visually reviewed` | Reference 12 only | All six raw `shots/look-2026-09-06/refined-fallen/` frames: locked day/night, close r1/r2/r3 and far r0; passing build 2026-09-06 | Does not accept the sculpture or precinct as a source match; hidden-angle motion and compound collision remain open |

## Checks

- Build and run the strict site-plan/world audit. Fine geometry may elaborate a
  declared structure but may not create a new footprint or composition centre.
- Capture the locked view, player-scale view, far view and four rotations. Check
  silhouette first, then facial/ornamental planes, then ink density.
- Confirm the permanent atlas startup and a neighbouring-window rebuild both
  recreate the node. A review-only mesh is not an implementation.
- Run `./tools/world-authoring.sh verify-sculpture`, then walk against and onto
  the form. Automated triangle/ray/capsule checks do not constitute author play
  acceptance or a whole-route traversal review.

## Known failures

- **Scaling local-space ink with the monument:** the 13× head and 19.5× legs
  inflated the former 0.009-unit hull into broad dark seams. The current shader
  uses framebuffer pixels; original meshes, placement and collision are unchanged.
- **Large cuboid facial layers:** read as a small building or mask on a slab.
  Replaced by the author's Meshy head.
- **Metre-scale protruding kneecaps:** read as robot joints. Replaced by a
  single authored Meshy leg subject.
- **Visible voxel collision shells:** intersected the imported model as broad
  smooth slabs. Replaced by invisible compound collision.
- **Baked Meshy colour and image floor:** looked detached from Petalfell and
  introduced rectangular bases. The normalizer strips both; runtime owns stone.
- **Smooth loft-only replacement:** `colossus-v1/reference_match_day.png` showed
  smooth calves and an oval mask beside stepped voxel ruins. The current offline
  carving lattice changes the actual geometry while keeping continuous anatomy.
- **Broad cloudy moss:** `colossus-v3/reference_match_day.png` showed dark,
  blurred staining over the legs. The current UV-attached, quantized growth field
  has narrower coverage and does not stack with the old world-space moss field.
- **Meshy debris/pillars around the subjects:** explicitly removed from the
  current blockout at the author's request. The unused source is not placed.
- **Uniform 2× leg scaling:** rendered in v24 but corrected by the author before
  acceptance. The current legs and compound collision are 1.5×; the enlarged
  voxel support remains because it reads as intentional monument footing.

## Update triggers

Update this entry when compound fine collision is implemented, a second
reference uses the method, the outline/material path changes, or the author
rejects the current sculptural read.

The replacement sculpture’s 23 Linux-package views in
`shots/worldbuilding-2026-09-06/colossus-package-full/` were inspected in six
labelled reduced `qa/matrix-*.png` sheets on 2026-09-06: locked day/night, five
clock phases and close/play/wide/far quarters. The stepped anatomy, closed back
of the head and continuous silhouette remain readable across those views.
This is `visually reviewed` for that scope. Broad precinct wear and sparse
dressing remain visible source gaps; generated overlays were not inspected.

The same package’s locked daytime and close r3 images were additionally
inspected at full size: the carved face, broken crown, flat stone facets and
continuous outline remain readable. The sculpture collision smoke passed again
after the shared material gained the default-off timber branch.
