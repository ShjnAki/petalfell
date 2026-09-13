# Southern marsh refinement — September 13

- **Lifecycle:** active
- **Evidence summary:** source observed; physical water jumps, detail ownership,
  support and wind continuity mechanically verified. Visual review is recorded
  below by exact artifact; no author acceptance is inferred.
- **Scope:** southern fen/shallows banks and mushrooms; production ground-flower
  density; controller surface jumps in any water column.
- **Non-scope:** relocating ruins, changing authored source compositions, new
  wildlife populations, whole-continent art acceptance or performance claims.
- **Last verified:** 2026-09-13
- **Supersedes:** the September 10 uniformly low island rims and excessive
  flower scatter; its macro geography, channel field and bounded context remain.
- **Superseded by:** none
- **Owning sources:** `src/World/Terrain.cs`, `Vegetation.cs`, `GroundDetail.cs`,
  `src/Core/Palette.cs`, `src/Player/Controller.cs`, `shaders/voxel.gdshader`,
  `detail.gdshader`, `waterdetail.gdshader`,
  `water.gdshader`, `lowland_mist.gdshader`, `tools/WaterJumpSmoke.cs` and
  `tools/LookRenderSmoke.cs`.

## Source and method

The author supplied `world-new/look-targets/2026-09-13/southern-marsh.png`;
the sibling manifest preserves its SHA-256. Observe the varied layered islands,
pale mushroom stalks, offset broad cap courses, hanging organic detail, warm
small light accents, shallow water and low mist. The explicit request to reduce
flower noise overrides the denser ground flowers in that image. Architecture
continues to follow each existing site's own plan.

1. Preserve the accepted source PNGs and the current marsh channel field. Vary
   dry bank shoulders with a globally registered 72-block field multiplied by
   the existing southern geographic/region weight; retain low landing tongues
   and the ordinary downstream stair pass. Do not apply Shore changes to north.
2. Keep giant mushrooms rooted and excluded from authored precincts. Their wider
   cap radius also expands the existing site-exclusion check. Offset only the
   upper crown within that footprint; pale patches remain on existing cap tops.
   The latest follow-up uses a two-block skirt including its cream underside and
   one small offset boss two or three blocks thick. One quadrant droops by one
   block; keep the two skirt layers connected across that step. A five-block wavelength field
   interrupts the outer rim; asymmetric corner cuts avoid identical outlines.
   Pale low feet bury into each measured supporting column. Small stems use a
   two-block section, large ones three. Dedicated `MUSHROOM_STEM` material 38 /
   `PatternFungus` 10 shades world-registered cubes without plank seams. Do not
   use architectural `PLASTER`, whose horizontal pattern caused the rejected stripes.
   Reduce candidate admission by 20%, retaining original coordinates. For each
   candidate, replay the eight neighbouring lattice draws and reject a cap
   envelope within two blocks of a lower-priority potential neighbour. Never
   query already-built mushrooms: that would make placement window-dependent.
   Potential neighbours can be unplantable; conservative clear space is intended.
3. Derive hanging gills from exposed cream cells with cap material above and a
   measured clear underside. Filter the sparse cells within the current chunk,
   then sort Z/X/Y before emitting. Vary small hanging lengths with an independent
   global draw, admitting groups through a continuous field.
4. Pin filament roots and interpolate the same wind weight onto every bead face.
   Only the last bead uses vertex alpha .5 as an opaque emission tag; all ordinary
   plants use alpha 1. The ordinary day cycle enables weak, slowly varying night
   emission. No per-mushroom light, mesh node or material is created.
5. On natural moss lips, measure adjacent support and air clearance before
   hanging short roots. Ground flowers use narrower fields, .10 admission and
   1–2 flowers per admitted cell. Loose ground petals use .035 admission and
   1–3 petals. Grass retains its existing shape and broad placement field.
6. Root reeds in the bed but put their leaves near the water surface. Rounded
   notched lily leaves stay inside a supported wet cell. Bobbing must evaluate
   `(MODEL_MATRIX * VERTEX).xz`, not local coordinates plus an unweighted origin.
7. Low mist uses a narrow height envelope and a moving field with clear gaps;
   retain bed transmission, ordinary registered reflection and bounded fog.

## Water-jump method

Replace the old held 6.5 upward assist at the surface with a 24.5 initial velocity
once per press. Skip buoyancy while rising through the surface, then return to
ordinary swimming on descent. Use normal air control, gravity and collision;
do not translate the player onto a bank. Reset coyote/buffer state at launch and
respect `InputEnabled`. Deep-water ascent can still reach the launch band.

The real-controller fixture in `verify-water-jump` has a bed, bank and optional
roof. Banks 1–4 blocks above the water are reached; a 6-block wall and a roof
are not passed through. Holding Space produces one launch; disabled input
produces none. The measured full-jump apex is 4.53 blocks above water.

## Checks and evidence

| Claim | State | Scope | Evidence | Limit |
|---|---|---|---|---|
| Higher water exits use actual collision and preserve blocked geometry | mechanically verified | controller | `verify-water-jump`, `/tmp/petalfell-water-jump.log` | Controlled fixtures, not every natural bank |
| Hanging cap detail retains exact global mesh/wind ownership | mechanically verified | tool-specific | `verify-look-rendering`, 1,760 hanging vertices after the latest mushroom follow-up; `/tmp/petalfell-mushroom-v3-look.log` | Does not establish artistic quality |
| Bank roots and water leaves retain support and window ownership | mechanically verified | tool-specific | Same check, 4,484 root / 3,504 water-detail vertices at three atlas origins | Shader temporal behavior additionally requires capture review |
| Land and swimming work in production | mechanically verified | bounded routes | 2692,2164 land: 44.15 blocks; 6400,7360 water: 22.54 grounded + 14.05 swimming, `/tmp/petalfell-marsh-{land,water}-final.log` | The water test separately locates a larger bank near its valid small-islet spawn |
| Northern terrain remains unchanged | mechanically verified | focused northern window | `verify-production-terrain 4500,1900`, fingerprint `b5dd5bafbc962634b89f74b569e1edcd98a0e9ee7c798cc7b45f31b17d896fa0` | Flower reduction is intentionally shared; this fingerprint describes terrain/placed geometry |
| Source-owned stairs and Shallows water/deck separation survive | mechanically verified | site-specific | `verify-reference-sites`, `/tmp/petalfell-marsh-sites-final.log` | Source likeness and exhaustive traversal remain separate |
| Complete terrain ownership remained continuous before the mushroom follow-up | mechanically verified | full atlas, earlier revision | 165 windows / 3,072 global chunks / 10,560 fingerprints; 5,984 safe terrain and 14,208 overhang comparisons; manifest `a4347626a528b54b`, `/tmp/petalfell-marsh-all-windows.log` | Predates the final cap shape and density; excludes fine-detail shaders and artistic quality |
| Final mushroom placement is repeatable through both neighbour axes | mechanically verified | coast and fen windows | `verify-production-terrain 6400,7360` and `5107,6620`; `/tmp/petalfell-mushroom-v3-{coast,fen}-data.log` | Focused repeats/overlaps, not a second full-atlas audit |
| Transition and walking handoff remain supported | mechanically verified | focused / tool-specific | `verify-production-terrain 6100,6600` repeat + both neighbour axes; `verify-atlas-walking-handoff` 4 cardinal / 4 corner / 1 partial-outer transitions | Not prolonged manual traversal |

## Visual review — September 13

[Local comparison gallery](../../shots/marsh-2026-09-13/review.html) pairs the
unchanged sources with raw Godot captures. The first completed sets were
`shots/marsh-2026-09-13/{coast,fen,threshold,transition}-final/`: 28 raw stills,
plus 180 fixed-step coast frames at 30 frames/second, encoded as
`coast-final/motion.mp4`. The `before/` set predates this refinement; `v1/` and
`coast/` are intermediate and must not be presented as final. The later
`mushroom-v2-*` sets below supersede all these mushroom shapes/densities.

- **Coast, 6400,7360:** three distances, four quarter orientations and dawn,
  noon, sunset and midnight show layered shoulders, sparse ground flowers,
  mixed pink/lilac cap courses, hanging roots and cream gills. Night tips emit
  small warm points without introducing per-mushroom light pools. Water beds
  remain visible. Motion frames 0, 90 and 179 were inspected for changing water,
  plant positions and low atmosphere; this is not a performance benchmark.
- **Fen, 5107,6620:** wide and rotated views show broad channels beside mushroom
  groves. The close `atlas_play` does not frame the player, so it does not verify
  player readability. `look_noon_r2` exposes cap shapes and hanging undersides.
- **Violet Threshold:** source-matched day/night and four quarter orientations
  show the unchanged portal/stair precinct within the revised marsh. Its own
  Reference 5 remains the architectural source. No complete-site likeness claim.
- **Transition, 6100,6100:** wide and night views retain a gradual mix of wooded
  and mushroom shoreline. The close view is partially obstructed by a stem;
  camera obstruction at this address remains a separate limitation.

**Visual state: observed, not author-accepted.** The flower field is quieter and
the cap/bank silhouette has more variation. The supplied image still has richer
soft lighting, more irregular island composition and denser authored architectural
detail. These captures establish a refinement, not complete reference parity.

### Follow-up: thinner caps and breathing room

The author asked for closer reference likeness and slightly reduced mushroom
frequency. Production fen counts changed 913 → 701 (23% fewer); coast counts
777 → 615 (21% fewer). Macro terrain and retained candidate addresses are fixed.
`mushroom-v2-fen/` contains six fresh stills: wide/far, midnight and three noon
quarters. All six were inspected individually. `look_noon_r2` now shows the
player between distinct caps where the earlier large pink crown filled the gap.
Broad shallow skirts, stepped uneven rims, cream undersides and low stem feet
read clearly across the rotations. Night retains small hanging spore accents.
The far view still reads as a grove, with individual crowns separated.

`mushroom-v2-coast/` contains four fresh play/wide/noon/midnight stills. Noon and
midnight were inspected individually; the hanging tips remain readable on the
thinner silhouette and the open marsh retains quiet flower coverage. The older
motion sequence documents unchanged water/wind/atmosphere only, not final caps.
Five `mushroom-v2-threshold/` stills refresh the locked day/night pair and three
other play rotations; two `mushroom-v2-transition/` stills refresh wide/midnight.
These seven were inspected in the labelled context matrix: the existing site
stays clear, and the transition retains its mixed shoreline growth. The current
gallery contains 17 raw final stills and separately labelled earlier comparisons.
All four final capture logs have no error/warning entries. Build and whitespace
checks pass; no new full-atlas performance or visual acceptance claim is made.

## Latest mushroom revision: thin umbrella and square-shaded stem

`mushroom-v3-*` supersedes v2's shape/material evidence. Fen noon r1/r2/r3,
far and midnight were inspected individually, as were coast noon and midnight.
The two-block main cap, small two-/three-block boss and lowered quadrant read
across rotations. Three-block pale stems show square shade patches instead of
horizontal plaster lines. Hanging undersides and night spore tips remain visible.
The same candidate density and spacing are retained; the latest fen/coast checks
still generate 701/615 large mushrooms respectively.

`verify-look-rendering` now also checks the large stem's 3×3 section and dedicated
fungus pattern, in addition to its actual open underside, supported landing and
window-identical geometry/detail. The fixture contains 585 mushroom cells and
1,760 hanging vertices. Both focused production windows pass repeated builds,
east/west and north/south comparisons after this edit. The build has zero
warnings/errors. These remain mechanical checks and observed visual evidence;
only the author can accept the result.
The remaining fen wide, coast play/wide, five Threshold and two transition views
were inspected in `mushroom-v3-context-matrix.jpg`. The 17 fresh raw stills cover
four locations; all four capture logs are clean. The comparison gallery now
presents v3 as current and labels v2 as superseded.

## Known failures and corrections

- Uniform two-block near-water rims made every small island read as a flat
  wafer. The 72-block shoulder field exposes more courses without re-rolling
  channels or macro geography.
- Tall cap walls and repeated concentric tiers made neighbouring mushrooms read
  as packed blocks. Thin the skirt, offset shallow upper slabs and cut coherent
  rim sections; reduce population and check cap envelopes rather than only stems.
- The author then requested a thinner skirt with a small thick top and thicker
  square-shaded stems. Two broad upper tiers still read as a stepped stack;
  replace them with one small boss. Plaster made pale stems striped: a dedicated
  fungus pattern supplies square tones while preserving architectural materials.
- Merely adding detail increased noise. Reduce flower/petal coverage first and
  use coherent groups and empty space; do not replace removed flowers with dense
  straight reeds.
- Hanging meshes first emitted identical cells in different orders because
  local 32-block edit tiles do not align with 24-block render chunks after a
  window move. Sorting only the bounded cream subset fixes exact mesh ordering.
- An old water smoke assumed a valid dry spawn always belonged to a 24-cell land
  route. A small swimmable islet violates that assumption without being unsafe.
  Verify the real spawn, then select the nearest qualifying bank within 72 blocks
  for the separate land-distance test. Keep the full physics/distance assertions.
- Local water-detail phase plus an unweighted global origin changed bobbing at
  handoff. Evaluate the full weighted phase in global coordinates.

## Update triggers

Recheck source, views and evidence when changing southern influence, bank height,
mushroom footprint, fine-detail growth/support, phase ownership, emission tags,
flower coverage, mist, water absorption, jump impulse or swim-state transitions.
Only the author can accept the resulting look.
