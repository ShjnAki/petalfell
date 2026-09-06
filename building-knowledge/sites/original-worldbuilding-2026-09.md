# Original shore and ridge destinations

Lifecycle: active. Scope: Tidekeeper’s Landing and Split Witness only.
Evidence summary: production builds and prop integrity mechanically checked;
all 46 packaged views plus selected full-size frames visually reviewed.
Neither site is author-accepted.

The September 6 request authorized original natural/man-made destinations and
more props. These are original designs, not transcriptions of a reference image.
Their briefs, registrations and ground plans live under
`content/chapter_01/sites/`; `TidekeepersLanding.cs` and `SplitWitness.cs` own the
voxel composition. `AuthoredSiteWriter` only realizes bounded authored cells.

## Terrain reasons

The shore survey used the actual production window at 6400,7360. The selected
6484,7528 shoulder is 29 blocks high and descends toward the existing sea at 24.
The landing's 31/29/27/25 levels connect by explicit one-block stairs. Cut-away
edges return to untouched natural columns. Mooring pairs remain beside the open
route, the catchment is hollow, and debris sits below the broken wall.

The northern survey used 4500,1900. At 4424,1928 the natural top is 144, rising
about 20 blocks northward and dropping about 22 southward. The blades root in
that slope; no flat site pad is written. Unequal crowns, exposed strata and
explicit erosion cuts separate the natural outcrop from the smaller shelter and
waymark. The source cross sections are fixed, not seeded or remixed.

## Small props

The plans place 16 coastal and seven northern props. Pottery has an eight-sided
belly, real inner cavity and thick floor; broken vessels have lowered rim sectors.
Coils are closed four-sided tubes. Boards have physically unequal split ends and
local wood grain; stone and pottery fragments use distinct existing palette
colours. These meshes have no external texture dependencies or baked lighting.
Jars carry matching triangle collision. Tiny boards, coils and chips are dressing.

## Evidence and corrections

| Claim | State | Evidence | Remaining uncertainty |
|---|---|---|---|
| The two permanent addresses come from real ground | `mechanically verified` | `shots/worldbuilding-2026-09-06/survey-coast/` and `survey-north/`, CSV and terrain images inspected 2026-09-06 | Sample stride four guided allocation; runtime operates on every column |
| Existing and original plan projections remain valid | `mechanically verified` | `preview-site-plan` passed both new sites and Bloom/Fallen/Shallows plans | Plan validity alone does not prove visual quality |
| Both new sites preserve deterministic production and neighbouring-window overlap | `mechanically verified` | `verify-production-terrain 6484,7528` and `4424,1928`, 2026-09-06; repeat hashes and 442,368 safe overlap cells passed | Final coast repeat passed after the reclamation refinement; exhaustive continent traversal remains outside this check |
| Small meshes are closed, outward and physically anchored | `mechanically verified` | `verify-worldbuilding`, actual production windows, finite vertices, positive volume, paired edges and jar collision; 23 props / 2,752 triangles | Does not prove every possible approach |
| Shore layout and props explain the work areas at play distance | `visually reviewed` | `tidekeeper-v2/reference_match_day.png` and `site_play_r2.png`, full size, 2026-09-06 | Broad surfaces remain restrained; final jagged reclamation, split board grain and pottery chips were subsequently reviewed in the package |
| The unequal rock cleft reads in day and midnight on the real slope | `visually reviewed` | `split-witness-v2/reference_match_day.png` and `site_midnight.png`, full size, 2026-09-06 | Higher ridge can obscure the cleft from the rear; full packaged matrix subsequently reviewed |

Initial landing v1 was too clean and broadly paved. v2 introduces actual edge
loss, coherent work-area remnants and reclamation. The first natural crowns were
too smooth and uniform; explicit shorter columns and erosion cuts replace them.
These corrections are specific to the original designs and are not permission
to add invented damage or props to an existing measured reference site.

## Repeatable checks

Use `survey-terrain` for evidence, then author L2/L3/L4 explicitly. An original
registration must declare `isOriginalDesign`, a real `designSourcePath` and no
unrelated `referencePath`. Surface support can name a fixed terrain level or an
explicit measured natural foundation. The latter fills into each real column
without altering surrounding topography. Keep clear routes out of prop records.

Run `verify-worldbuilding`, both focused terrain checks and production playability,
then capture the Linux package with locked day/night, five clock phases and
close/play/wide/far quarters. Original sites intentionally skip reference-image
overlays; inspect their actual composition against the authored brief instead.

Final evidence, 2026-09-06:

- `tidekeeper-package-full` and `split-witness-package-full` each contain 23 raw
  Linux-package images under `shots/worldbuilding-2026-09-06/`. All 46 were
  inspected in labelled reduced `qa/matrix-1..6.png` sheets: locked day/night,
  five clock phases and four quarters at close/play/wide/far distances. Tidekeeper
  close r3 and Split Witness golden were additionally inspected at full size.
  Broken shore edges, small work-area props and the unequal ridge cleft remain
  readable. The landing retains broad quiet paving; the rear ridge masks parts
  of the cleft, as expected from the existing slope. Neither is author-accepted.
- `verify-production-playability 6481,7519 land` moved the real controller 33.65
  blocks in 592 physics frames across Y25..31, settling at Y25. The corresponding
  `4422,1940 land` test moved 29.27 blocks in 570 frames across Y120..134 and
  settled at Y120. These are automated collision-backed approaches, not a claim
  of manual exploration or exhaustive traversal.
- Final `verify-worldbuilding`, `verify-sculpture`, `verify-look-rendering` and
  the coast repeat/overlap check passed. That earlier packaged atlas audit
  reported five domains and five sites, four of them production. The subsequent
  [reference-precinct export](reference-precincts-2026-09.md) has 14 of each.
  Linux export and both actual
  packaged site capture runs completed without shader or runtime errors.
