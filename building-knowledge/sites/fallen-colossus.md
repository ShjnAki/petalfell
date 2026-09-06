# Fallen Colossus (Reference 12) evidence ledger

- **Lifecycle:** `active`
- **Evidence summary:** source relations are `observed/source-measured`; the
  permanent precinct remains in production. The September 6 request replaces
  supplied meshes with from-scratch stepped carvings. Import/physics checks pass;
  v4 locked day/midnight are `visually reviewed` for the new form and material.
  Earlier imported-sculpture matrices below are historical; the site is not
  `author-accepted`
- **Scope:** `site-specific` to `fallen-colossus` / `reference-12.png`
- **Last verified:** 2026-09-06 for shared materials and outlines; placement evidence retains its earlier date
- **Supersedes:** the v6/v7 provisional hand-built sculpture; rejected attempts
  remain below
- **Superseded by:** none
- **Owning sources:**
  [`reference-12.png`](../../world-new/reference-12.png),
  [`fallen-colossus-reference-12.json`](../../content/chapter_01/sites/fallen-colossus-reference-12.json),
  [`fallen-colossus-reference-12-plan.json`](../../content/chapter_01/sites/fallen-colossus-reference-12-plan.json),
  [`Reference12FallenColossus.cs`](../../src/World/Sites/Reference12FallenColossus.cs),
  [`Reference12SculptureDetail.cs`](../../src/World/Sites/Reference12SculptureDetail.cs)

## Source constraints

The source owns a four-course stepped pedestal, two separated trunkless
colossus legs, a much larger-than-player crowned head fallen to the lower-right,
the player to the lower-left, a collapsed west beam, sparse survivors/rubble,
and blossom trees in an open meadow. There is no torso bridge. The head is a
tilted carved object, not an upright square building, and each leg reads through
foot, ankle, calf, knee and severed thigh rather than random protruding boxes.

## Evidence

| Claim | State | Evidence | Remaining uncertainty |
|---|---|---|---|
| From-scratch head and legs replace the supplied meshes at the existing permanent anchors | `mechanically verified` | `tools/build_fallen_colossus.py`, two authored GLBs, `Reference12SculptureDetail.cs`, build and `verify-sculpture`, 2026-09-06 | 31.2-unit legs and 17.513-unit head retain the preceding scale; whole-site fidelity remains open |
| Foot/calf gap stays open and carved surfaces block rays and a player-sized capsule | `mechanically verified` | `tools/SculptureSmoke.cs` exercises the imported mesh and the live physics space; all 4,794 collision triangles match visual faces | Controlled physics queries, not a player-controlled traversal or author acceptance |
| Stepped anatomy, crown/face recesses and stone courses remain readable at locked day and midnight | `visually reviewed` | Both full-size v4 frames in `shots/worldbuilding-2026-09-06/colossus-v4/`, 2026-09-06 | The precinct remains cleaner and sparser than the source. Full 23-view package matrix subsequently reviewed; precinct reference parity remains open |
| The airborne revision retains the sampled precinct views | `visually reviewed` | Both `folded-air-fallen-package` raw frames at full size: locked day and close r3, 2026-09-06 | A focused effects check only. Sculpture and architecture are unchanged; preceding full matrix remains historical. Overlays were generated but not inspected |
| The leaf-only surface revision retains precinct and sculpture readability through the site matrix | `visually reviewed` | All 23 package `shots/look-2026-09-06/satin-canopy-fallen-full/` views in labelled reduced matrices, plus full-size close r3, 2026-09-06 | This verifies broad readability after a shared shader change. Sparse dressing, smooth sculpture and distant colour remain below the source; composition is unchanged and not author-accepted |
| Shared turf and plant colour preserves separation around the pale precinct | `visually reviewed` | All 23 package `shots/look-2026-09-06/matte-turf-fallen-full/` views in labelled reduced matrices, plus individually inspected full-size close r0; 2026-09-06 | Quiet olive/cool growth is retained, while broad wear masks, sparse dressing and smooth sculpture remain below the source. Authored composition is unchanged; no author acceptance |
| The current plant shapes retain the precinct clearings and readable surrounding meadow | `visually reviewed` | All 23 package `shots/look-2026-09-06/bent-flora-fallen-full/` views in labelled reduced `qa/` matrices, plus individually inspected full-size close r0; 2026-09-06 | Flower cups are clearer along the meadow strips, but square wear, sparse precinct and smooth sculpture remain. Source models and authored composition are unchanged; no author acceptance |
| The current shared stone finish retains terrace and sculpture separation through the full matrix | `visually reviewed` | All 23 raw Linux-package `shots/look-2026-09-06/scuffed-fallen-full/` views, inspected 2026-09-06: locked day/night, five clocks and close/play/wide/far r0-r3 | Paving joints are strong in grazing light, broad wear remains rectangular, and imported sculpture is smoother than the source. Original meshes and transforms are unchanged; current derived comparisons were not inspected |
| The stronger warm key and cool ambient fill retain sculpture and terrace separation across the preceding lighting matrix | `visually reviewed` | All 23 raw Linux-package `shots/look-2026-09-06/directional-fallen-full/` views, inspected 2026-09-06: locked day/night, five clocks and close/play/wide/far at four quarters | Golden light exposes more of the imported surface relief, but the head remains smoother and the precinct sparser than the reference. Grazing light exaggerates paving joints. Meshes, approved 1.5x legs and authored transforms are unchanged; no author acceptance |
| The latest paving joints and fallen-petal drifts remain attached through the complete review matrix | `visually reviewed` | All 23 raw `shots/look-2026-09-06/petal-paving-fallen-full/` Linux-package views, inspected 2026-09-06: locked day/night, five clocks, close/play/wide/far r0-r3 | Grazing light emphasizes partial joints. Large warm wear rectangles, smooth sculpture and sparse precinct remain source gaps. Original meshes, 1.5x legs and transforms are unchanged; no author acceptance |
| The Linux package loads both original sculpture assets, their shared material and the imported reference image | `mechanically verified` / `visually reviewed` for the two raw frames | `shots/look-2026-09-06/package-fallen/reference_match_day.png` and `reference_match_night.png`, inspected 2026-09-06; reference comparison also completed inside the package | Asset availability and sampled appearance only. The composition and sculpture finish still differ from the source; no new geometry or acceptance |
| Current material, haze and outline attenuation remain attached through all four distances/quarters and five source-camera clock phases | `visually reviewed` | All 23 raw `shots/look-2026-09-06/focal-air-fallen/` frames from the Linux package, inspected 2026-09-06 | Foreground and middle distance are clearer with the later haze onset. Face remains smoother and precinct cleaner/sparser than source. Meshes, approved 1.5x legs and transforms are unchanged; no author acceptance |
| The current filtered mineral material and half-pixel outline remain attached through the complete distance/rotation matrix | `visually reviewed` | All 18 raw `shots/look-2026-09-06/surface-fallen/` frames: locked day/night, close/play/wide/far r0-r3, inspected 2026-09-06 | The imported face remains smoother than the block source; the precinct is cleaner and sparser. Approved 1.5x legs, meshes and transforms are unchanged. These night stills predate longer particle settling; no author acceptance |
| The site is permanently registered at `(10600,4600)`, status-gated into the map-guided production runtime and rebuilt during window handoff | `mechanically verified` | topology/source paths, passing world audit/build, and v21 production log, 2026-08-31 | Live player-controlled handoff has not been author-reviewed |
| The current plan owns one preserved atlas context, seventeen terrain records, sixteen exact wear groups, six stairs, four broken foundation traces, four rubble fields, eight fixed 2×2 pillars and four sculpture projections | `mechanically verified` | strict runtime-facing plan preview reports 26 structures; plan/world/build/diff checks pass on 2026-08-31 | Audit does not prove image similarity or traversal |
| The broad precinct uses substantial orthogonal steps and only partial third layers; ordinary atlas terrain remains visible between its central court and four detached outer slab stacks | `visually reviewed` | `/home/shikhar/godot/shots/reference-12-v25-legs-1_5x/site_far_r0.png`, inspected at original size 2026-08-31 | The composition remains cleaner and sparser than the reference and is not accepted |
| The Meshy head and legs replace baked colour with Petalfell stone and carry a 0.009-unit plum silhouette outline; the forward-facing legs use 1.5× their first imported review scale and matching 1.5× collision while the head remains unchanged | `visually reviewed` | `/home/shikhar/godot/shots/reference-12-v25-legs-1_5x/reference_match_day.png` and `site_far_r0.png`, inspected at original size 2026-08-31 | Night response, hidden rotations and live collision remain open |
| The imported head/legs are isolated from the third Meshy debris asset; surrounding architecture now comes only from the site-owned voxel plan | `mechanically verified` | only two GLB paths are loaded by `Reference12SculptureDetail`; v25 captures inspected | Further foundations must remain plan-authored rather than reusing the debris model |
| The open-meadow tree halo is applied by the production terrain path without changing boulders | `visually reviewed` | normal-start `reference_match_day.png` and `site_far_r0.png` in `/home/shikhar/godot/shots/reference-12-v26-normal-sparse-trees/`, inspected 2026-08-31 | Atlas-wide density outside the 140-block falloff was not recaptured; an older compiled-site comparison is retained only in archived documentation |
| The current site matches Reference 12 one-to-one | `candidate` | Required evidence is missing | Needs geometry/material refinement, all rotations, collision/play review and explicit author acceptance |
| Shared mineral stone remains attached to the imported planes through day/night and quarter rotations; the original local-unit outline is too thick at close range | `visually reviewed` | All 18 raw `shots/look-2026-09-06/current-fallen/` frames | Intermediate outline is superseded by the following row; this does not establish site fidelity |
| The corrected 0.50-pixel outline keeps the crown and leg edges restrained without changing the imported meshes or transforms | `visually reviewed` | All six raw `shots/look-2026-09-06/refined-fallen/` frames, 2026-09-06; locked day/night, close r1/r2/r3, far r0 | Other refined angles have not been recaptured. The site remains too clean and sparse compared with Reference 12 |

## Procedure and current limits

- Keep the permanent terrain window and ordinary-atlas channels. The current
  court is a deliberately broad painter's stack with an enlarged central plinth,
  four detached outer lower/upper/crown stacks, explicit stairs and site-owned
  foundations/rubble; do not put the discarded Meshy floor or debris beneath it.
- Keep the source's open-meadow silhouette beyond the strict plan boundary with
  the registration-owned tree-only sparse halo: 22% density at the footprint,
  easing smoothly to ordinary Bloom density over 140 blocks. Do not enlarge the
  architectural footprint or reduce boulders/the whole biome to create this view.
- Use [hybrid sculpture geometry](../structures/hybrid-voxel-and-fine-sculpture-geometry.md)
  and `art/fallen-colossus/README.md` for the current from-scratch source,
  materials, packed outline normals and matching static collision.
- The locked camera is source-facing yaw 0, true-isometric pitch 35.26439 and
  distance 158 at 1672x941. Other rotations test completeness, not source match.
- Current collision is the imported triangle surface under the same transform;
  live walking review remains required beyond the automated rays/capsule sweeps.
- The first doubled-leg v24 capture is superseded by the author's 1.5× correction.
  Preserve the enlarged plinth from that experiment; it prevents the 1.5× feet
  from returning to the cramped v21 support.

## Rejected attempts

| Attempt | Status | Failure | Replacement |
|---|---|---|---|
| Giant axis-aligned fill layers for the head and crown | `rejected/superseded` | Read as a fort/building; could not express the fallen plane or face | Author-supplied Meshy head with invisible collision |
| Broad protruding knee blocks | `rejected/superseded` | Made the legs robotic towers | Author-supplied Meshy leg subject |
| Provisional hand-authored fractional-cuboid head and legs | `rejected/superseded` | Better than giant cubes but still materially and anatomically behind the supplied Meshy geometry | Two independently normalized author-supplied GLBs |
| Meshy baked textures, ground slabs and surrounding debris asset | `rejected/superseded` | Imported lighting/material identity and unrelated geometry instead of belonging to the site | Strip materials/floor in Blender, omit debris, apply Petalfell stone and site-owned court |
| Camera yaw 135 with a plan measured in source screen axes | `rejected/superseded` | Reversed/cancelled player/head screen relations and cropped the site | Source-facing yaw 0 with the player/head on their measured sides |
| Ordinary dense forest immediately around the monument | `rejected/superseded` | Buried the source composition | Open authored precinct and ordinary atlas vegetation beyond its exclusion |
| Uniform 2× leg scale | `rejected/superseded` | The author corrected the scale before acceptance; it dominated the court and head | 1.5× leg scale with 1.5× compound collision over the already enlarged plinth |

## Update triggers

Update immediately after geometry/material/camera changes, compound fine
collision, a complete rotation/distance capture set, or any author correction
or acceptance decision.

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
