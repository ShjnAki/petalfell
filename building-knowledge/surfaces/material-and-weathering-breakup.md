# Material and weathering breakup

- **Lifecycle:** `active`
- **Evidence summary:** the material path and production-snow pattern/detail
  ownership are `mechanically verified`; Reference 10's current placement is
  `visually reviewed` but not `author-accepted`; Reference 12's broad authored
  ground wear is `visually reviewed` at locked/far scale; September snow
  intermediates have been reviewed and led to a mipmap/filter correction
- **Scope:** existing voxel material path is `general`; exact palette placement
  is `site-specific`
- **Last verified:** 2026-09-06 for the shared material and snow review;
  historical site-placement evidence below retains its original dates
- **Supersedes:** flat single-tone stone and per-block random/checkerboard decay
- **Superseded by:** none
- **Owning sources:** [`Palette.cs`](../../src/Core/Palette.cs),
  [`voxel.gdshader`](../../shaders/voxel.gdshader),
  [`GroundDetail.cs`](../../src/World/GroundDetail.cs),
  [`Reference10GroveCourt.cs`](../../src/World/Sites/Reference10GroveCourt.cs),
  [`bloom-grove-court-reference-10-plan.json`](../../content/chapter_01/sites/bloom-grove-court-reference-10-plan.json)

The September shared shader revision supersedes the fine-material amplitudes,
colour uploads and fade values in this entry. See
[the current rendering method](../rendering/reference-look-2026-09.md).
Authored macro placement and all site-specific constraints below still apply.
Historical capture rows do not establish the September revision.
The September loader now ensures real mip levels independently of ignored
import settings. A large unfiltered snow sample produced speckle; a sixteen-texel
minimum blurred it too far. The current six-texel sample with antialiased
contours has been inspected in all twelve `glaze-snow` frames: close/far,
five clock samples, noon quarters and performance-camera day/night. It retains
defined blue patches; snow patch contrast and temporal stability remain open.
Moss-stone substrate exposure stays strictly inside
existing material cells and does not generate or move macro patches.
The latest September preview lowers normal relief on snow/sand/blossom and
concentrates stone relief in the existing worn field. `finish-bloom`'s five
inspected views retain stone flakes and close detail, though the albedo texture
still reads too uniformly rough in places. The ground-detail normal correction
removes camera-dependent black blades; it changes lighting, not scatter fields.

Physical moss leaves are now derived from the exposed dry faces of the existing
`MOSS_STONE` cells. Read both cap profiles and sparse placed masonry; a cap that
also has an override must be processed once, with the override winning. Do not
scan a full voxel volume or the whole edit dictionary per chunk: `PlacedIn`
uses overlapping edit tiles. Leaf tips remain inside the source face in plan,
with only a small outward fold; bottom and covered faces receive no growth.
The detail joins the existing chunk mesh and has no collision or shadow pass.
All seven `growth-bloom` raw views were inspected (locked day/night, close r0–r3,
far r0). Facets read at close range and do not form detached curtains. They are
subtle at the locked/far view and do not solve missing source-specific growth.
The subsequent `patina-bloom` material revision filters the enlarged mineral
sample and gates fine flake contrast by the worn field, reducing the uniformly
crumpled wall appearance. Its lighting/capture scope is in the rendering ledger.

## Outcome

`canopy_surface.gdshaderinc` replaces the leaf pattern's rectangular grain with
jittered, rotated overlapping petal facets at 6.5 cells/metre. Analytic gradients
supply 0.0018 relief; albedo contrast is 0.085 before the shared pattern gain.
Both fade from a 0.025 to 0.095 metre pixel footprint, with no petal evaluation
once fully unresolved. Broad smooth variation and existing per-block values
keep the crown's lobes readable. Leaf-only normalized diffuse wrap is 0.12,
with roughness 0.94 and ordinary cloud/shadow attenuation. No palette conversion,
geometry, anchors, allocations or material membership change. All six refined
`satin-canopy-coast` and six `satin-canopy-bloom` source previews were inspected;
the close surface is quieter and the sampled distant crowns keep their forms.
The coarser first candidate is recorded under Known failures.
The retained canopy finish is exported in `satin-canopy-*-full`: 70 package
views reviewed in reduced matrices, with individual fine-detail frames and
four coast orbit samples listed in the rendering ledger. Both sites cover all
four distances/quarters and five clocks; coast and alpine cover five clocks,
four noon quarters and play/far. The Linux atlas audit and rendering smoke pass.
The coast probe retains 121 draws/154,674 primitives, with GPU median about
0.35 ms higher by day and 0.32 ms at night than the preceding turf run. This is
one warmed comparison, not an isolated repeated benchmark. Simple canopy
geometry, sparse dressing and source composition gaps remain open.

`turf_surface.gdshaderinc` shares continuous global-XZ
dry/lush colour between existing turf, its mesher-supplied fringe and green
plant vertex colours. The earlier fringe bypassed the turf colour treatment,
producing a brighter, more uniform green band. A separate three-metre field now
groups deep fringe coverage, with small torn contours inside those patches.
Close turf uses soft clod contours instead of quantised square samples. The
first `sward-bloom` candidate was inspected in all six source previews: its
colour ratios alone left the turf too yellow and the ledges too bright. The
retained treatment applies the same restrained saturation/luminance response
to turf, fringe and green plant colours. All six `matte-turf-bloom` and five
`matte-turf-coast` source previews were inspected: olive and cool growth remain
distinct at locked/far distance, and the reverse-view blades sit against their
ground without the preceding bright green contrast. The fringe footprint is
measured before the integer-height wrap to avoid seam-inflated filtering.
This changes no material placement, site wear mask or vegetation anchors.
The exported `matte-turf-*-full` sets contain 70 raw views across Bloom, Fallen,
coast and the mixed alpine boundary. All were reviewed in labelled reduced
contact sheets, with individual fine-detail views and four Bloom orbit samples
recorded in the rendering ledger. The quieter colours persist through these
clocks, distances and quarters. Broad cap/substrate bands, square site wear
masks and sparse ground dressing remain below the references. The smoke check,
export and actual-package atlas audit pass; no author acceptance is implied.

The September exposed-lip revision uses the mesher's existing UV/edge data to
apply interrupted pale mineral scuffs only at real exposed stone edges. The
refined 0.025–0.13 m marks have fine stepped contours and footprint filtering.
All six `scuffed-bloom` source previews were inspected (locked day/night,
close r0/r2, golden hour, far r0): pillar edges no longer carry the first
candidate's broad pale streaks. Sand's narrower warped-ripple patches, mineral
grain and independent broad deposit tone were inspected in all five
`scuffed-coast` source previews. The ridges no longer span beaches as visible
parallel bands. The Linux export passes its packaged atlas audit. All 69 raw stills in
`scuffed-bloom-full` (25), `scuffed-fallen-full` (23), `scuffed-coast-full` (11)
and `scuffed-snow-full` (10) were subsequently inspected, including five clocks
and four quarters at each focus. Both sites cover all four distances. Three raw
quarter-turn samples from each new Bloom/coast orbit were also inspected. The
marks stay attached at these sampled angles, and broad sand bands remain absent.
Paving masks, sparse masonry/shore detail and broad pale alpine shelves remain
below the references; these checks do not establish complete temporal quality.

Bloom's source-owned correction restores four pale lower-court paving gaps,
including the traveller's standing area, using 24 explicit plan cells. The
calibrated overhead source locates the cells; the locked isometric confirms the
pale court beneath the traveller. The arch now has three distinct moss shapes:
a lower-left fork, a lintel patch with short tails, and a taller right-jamb
streak continuing into the opening. These are authored material replacements,
not a shared procedural mask. Direct evaluation of the old/new arch writes
finds the same 681 occupied cells and 43 material changes; the opening and crown
bites are unchanged. All six `source-wear-bloom` preview views and all 25 raw
`source-wear-bloom-full` Linux-package views were inspected, including five
clocks and four distances/quarters. All four derived comparisons and three raw
orbit quarters were inspected separately. The package passes its atlas audit.
The repair addresses these specific mismatches; other square floor patches and
the sparse masonry finish still require source comparison.

Paving-joint candidate: the existing `PAVING` material (19) can shade shallow
one-metre cell joints on upward faces without changing voxel/collision data or
applying masonry to natural rock. The first `slab-bloom` five-view preview was
rejected during agent review: a nonzero minimum seam and strong normal shoulder
made every cell read as a new ceramic tile, especially at close/reverse views.
The revised candidate has no minimum seam, uses a continuous wear field to
interrupt joints, and reduces both shoulder slope and the slab-centre bump.
All 25 raw `worn-slab-bloom-full` source views and 23 raw
`worn-slab-fallen-full` Linux-package views were inspected, including five
clocks and all four distances/quarters. Bloom orbit samples 0045/0090/0135 keep
the interrupted joints attached. These establish the sampled surface response,
not complete motion quality or source parity. Broad wear masks and site massing
remain too regular.

Fallen-petal drifts reuse existing blossom profile membership and dry, uncovered
`PAVING` support. An 18-block continuous field owns the patches; an independent
draw distributes at most seven flat diamonds within each cell, outside the
bevel margin. They reuse the existing palette and chunk detail mesh, with no
collision, shadow pass or new material mask. The five `petal-paving-bloom`
preview views were inspected: patches read on open/reverse paving and are subtle
at far distance. The subsequent full package matrices contain 25 Bloom and 23
Fallen raw views, all inspected, plus three raw Bloom orbit quarters. Small
drifts remain attached at the sampled angles and clocks. `CheckPavingPetals` passes
shared-interior continuity at three distant origins, stepped dry support,
water/non-paving/blocker exclusions and the 28-vertex-per-cell bound. Account
for ArrayMesh UNORM8 colour packing when identifying the petals in mesh checks.

The preceding fragment/material/haze revision was inspected in all 69 raw
`focal-air-bloom`, `focal-air-fallen`, `focal-air-coast` and `focal-air-snow`
views on 2026-09-06. Small natural facets stay on dry shelves through the sampled
clocks and camera quarters. The stronger mineral islands clarify stone colour,
but large paving remains too smooth/regular and broad shores too sparse.
This is shared-surface evidence, not site composition or author acceptance.


Natural snow, scree, sand and soil receive small six-sided fragments through a
32-block deposit field. The field, rather than independent per-cell noise, owns
where patches accumulate; an independent global-coordinate draw only places
pieces inside those patches. Terrace lips receive a modest density increase.
The rotated bounding rectangle stays inside its dry, exposed supporting cell.
Paving, stone masonry, rubble and moss masks are excluded. One cluster has at
most three fragments/108 vertices in the ordinary chunk detail mesh, with no
collision, extra material or shadow pass. The smoke fixture compares shared
interior chunks: an outer window boundary lacks the neighbour apron and is
outside the production meshable margin.

All seven `chips-snow` and all ten `chips-coast` raw views were inspected.
Fragments remain attached through noon quarters, day/night and close/far views;
they are small material accents and do not supply the reference's larger rubble
masses. These sets predate the subsequent mineral-contrast and focal-air work.

Reference-like stone breakup is two-scale. Explicit authored cells and courses
carry the broad pale/cool/warm/moss pattern that must survive far zoom; the
existing world-space `PatternRock` shader adds restrained fine weathering at
near range. Production snow adds its own bounded drift: its fine crust still
fades, while one subtle low-frequency drift remains at distance so an alpine
shelf does not collapse to a white card. The shader never chooses the layout,
damage, moss islands, or architectural courses.

## Evidence

| Claim | State | Scope | Evidence | Remaining uncertainty |
|---|---|---|---|---|
| `STONE`, `STONE_PALE`, `STONE_WARM`, `PAVING`, rubble, and `MOSS_STONE` use the existing rock-pattern material path | `mechanically verified` | general/tool-specific | Palette definitions and `voxel.gdshader`; Reference 10 material mapping in `WriteAuthoredSurfaceWear` | Does not establish that current colours exactly match the source grade |
| Fine pattern is world-space and fades from 85 to 240 world units (September revision), so far-read breakup must be authored at block scale | `mechanically verified` | general/tool-specific | `voxel.gdshader` pattern projection and fade uniforms | Runtime grade/fog/light can alter perceived contrast |
| Production snow separates fading close crust from a persistent low-frequency world-XZ drift, and physical traces/stones are admitted by broad deterministic fields rather than an atlas-wide per-cell scatter | `mechanically verified` | tool-specific | `Palette.PatternSnow`, the bounded 30–90 m snow branch in `voxel.gdshader`, and the 72/96-block fields plus support test in `GroundDetail.BuildAtlas`; `dotnet build --no-restore`, `git diff --check`, and headless Godot editor import passed 2026-08-30 | No current capture has been inspected for contrast, trace density, edge popping, or shimmer; these values remain visually provisional |
| Explicit paving islands and vertical stone accents remain visible across Reference 10 review distances | `visually reviewed` | site-specific | v11 close/play/wide/far day matrix and `reference_match_night.png` inspected 2026-08-30 | Current result is cleaner and less nuanced than the source; not accepted |
| Sixteen exact warm/cool/moss/worn groups remain legible across Reference 12's expanded central and outer slab field without moving any terrain edge | `visually reviewed` | Reference 12 site-specific | `/home/shikhar/godot/shots/reference-12-v25-legs-1_5x/reference_match_day.png` and `site_far_r0.png`, inspected 2026-08-31 | The current contrast is restrained and whole-site material parity remains open |

## Procedure

1. Separate macro placement from micro shading. Trace broad paving wear, warm
   stains, cool stones, and moss islands into exact plan cells or explicit
   structure courses.
2. Use `STONE_PALE` as the principal ruin fabric only where the source supports
   it. Add `STONE`, `STONE_WARM`, `PAVING`, and `MOSS_STONE` as coherent patches,
   courses, or face streaks—not evenly spaced decoration.
3. Keep surface-patch cells exact and non-overlapping. A deterministic runtime
   hash must not move their edges or convert a source stain into confetti.
4. Keep vertical breakup attached to the unique mass: base course, weathered
   face, damaged crown, or local moss streak. Do not apply one repeating course
   schedule to every pillar.
5. Let all stone variants use the shared `PatternRock` shader for fine organic
   variation. Do not add a site-specific bitmap or a second random material
   layout unless new visual evidence demonstrates a missing frequency band.
6. Preserve natural cap/sub/deep materials on reclaimed terrain. The green lip,
   warm soil seam, and regional cliff tone are part of terrain integration;
   only paved surfaces need pale masonry below them.
7. Give snow its own frequency split. Fade fine crust with other near patterns,
   but retain only a subtle, low-frequency world-space drift for mid/far read.
   Gather metre-scale wind traces in wavelength-scale fields and reject traces
   whose full length lacks a same-height snow cap. Gate exposed stones through a
   second broad scoured field; never restore an independent per-cell snow chance.
8. Judge material under the ordinary day cycle at locked day, night, and close
   play distance. Fix geometry and camera before tuning colour against an
   overlay.

## Checks

### Mechanical

Audit surface patches for ownership and overlap, build, and inspect palette
definitions to ensure each selected material still uses the expected pattern.
Mechanical checks prove deterministic assignment and shader selection, not
perceptual similarity.

### Visual

At close/play distance, inspect whether fine weathering breaks flat faces
without becoming a printed grid. At wide/far distance, ignore the faded shader
and check whether authored macro patches still describe age and material. At
night, verify that pale, cool, warm, and mossed masses remain separable without
turning the scene into uniformly dark violet. Compare geometry in an edge view
before blaming material for a silhouette mismatch. For snow, orbit and compare
successive fixed frames: the broad drift should remain legible at wide/far range
without crawling, while physical traces should form sparse parallel groups,
stay supported by one shelf, and disappear as groups rather than even pepper.

## Scope and limits

The current shader is a shared world material, not a Reference 10-specific
solution. Its presence does not prove exact source texture. Changing its global
strength or fade affects every terrain and structure using it and needs broader
regression captures. The specific patch coordinates and courses must never be
copied to another site.

## Known failures

- **Rejected in agent review, 2026-09-06:** render-only cuboid buds on exposed
  leaf faces read as studs in `blossom-lobes-coast/atlas_play.png`. Moving taller
  buds to convex top corners made the crowns crenellated in
  `corner-blossoms-coast/atlas_play.png`. Both full-size frames were inspected;
  both mesher experiments were removed. Neither has author acceptance or a
  retained implementation. Adding regular bumps to a broad cuboid does not
  produce the references' layered crown composition.


- The first `petal-canopy-coast` preview used overlapping ellipse relief at
  3.6 cells/metre and 0.010 height gain. Full-size `atlas_play`, noon and
  midnight were inspected: the close canopy looked covered in pebbles. That
  amplitude is rejected. The follow-up uses 6.5 cells/metre, 0.0018 relief,
  half the colour contrast and an earlier pixel-footprint fade. Crown geometry
  and per-block value separation must remain stronger than the surface marks.
- The first September sand-ripple preview (`lip-weather-coast`, all five raw
  views inspected) left broad parallel ridges visible across the beach, including
  at night. Its 0.075 normal amplitude and broad patch coverage read as fabric.
  The next candidate uses smaller, more warped ridges in narrower deposit
  patches, a 0.030 normal amplitude and earlier footprint filtering. Fine grain
  and broad sand-tone drift remain separate; all five revised `scuffed-coast`
  previews were subsequently inspected.
- The first September exposed-lip patina used continuous pale bands as wide as
  0.24 m. `lip-weather-bloom/site_close_r0`, `site_close_r2` and `site_golden`
  were inspected: long pillar edges looked streaked, so that version was
  rejected during agent review. The revised candidate narrows marks to 0.13 m,
  interrupts them with the continuous wear field and steps their fine contour
  at one-sixth metre. All six revised `scuffed-bloom` previews were subsequently
  inspected; neither variant changes geometry, masks or collision.
- Flat pale blocks looked unfinished even when topology improved. Explicit
  macro palette islands plus the existing rock shader provide two-scale breakup.
- Per-block random moss/weathering produced confetti. Coherent source-authored
  patches replace random selection.
- Repeated colour bands on every survivor exposed a reusable pattern. Variation
  is now tied to each unique structure.
- Increasing shader detail at far range caused shimmer and still could not fix
  composition. The shader fades; macro authored blocks own distant readability.
- Sharing the turf pattern left production snow as one white card after the
  generic 40–130 m fade. Snow now retains only a smooth 30–90 m world-space
  drift; its higher-frequency crust still fades.
- Independent per-column snow stones and sub-metre scratches read as far-field
  confetti or vanished entirely. Broad scoured/drift fields now gate supported
  metre-scale groups; their actual rendered density remains to be reviewed.

## Update triggers

Update for palette colour/pattern changes, shader strength/frequency/fade
changes, material-to-plan mapping changes, new source-authored patch categories,
day-cycle/grade changes that alter readability, or author corrections to the
material match. Global shader changes require evidence beyond one site.
