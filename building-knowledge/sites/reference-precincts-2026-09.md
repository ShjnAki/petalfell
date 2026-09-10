# September reference precincts

- **Lifecycle:** active
- **Scope:** References 2–9 and 11 only; the shared course writer is a bounded
  storage primitive, not a composition method.
- **Evidence summary:** each source image was inspected at original size and
  compared with its own plan. All final clock/distance/rotation matrices were
  inspected at reduced size, with selected frames and the exported portal
  day/night pair inspected at full size. Plans remain candidate transcriptions, not accepted
  replicas. Production placement does not imply author acceptance.
- **Last verified:** 2026-09-06, during the recovered reference-site instruction.
- **Supersedes:** no existing reference site. Bloom Grove Court and Fallen
  Colossus retain their existing builders and authored sculpture.
- **Superseded by:** none.
- **Sources:** `world-new/reference-{2,3,4,5,6,7,8,9,11}.png`, corresponding
  `content/chapter_01/sites/*-plan.json`, and `shots/big-sites-2026-09-06/`.

## Individual source distinctions

| Reference / permanent site | Source-owned composition | Registration and limitations |
|---|---|---|
| 2 — Drowned Seal Gate | Sealed central door, asymmetric cliff-wall wings, long eastern stair, two independent ruined jetties and their water piers | 5760,7184; ordinary datum27, sea24. Initial render overstates clean solid crowns; later explicit crown losses and violet door panels address that reading. |
| 3 — Court of the Open Sky | Six separate open arch remnants around a two-stage altar; two upper stair approaches and a dry sunken southern room | 9560,5480; datum40. A low return crossed the eastern stair and a decorative carpet raised the altar treads; both were corrected in the plan. |
| 4 — Hollow Choir | Three broad stair flights; closed stepped apse with open front, hollow western watchshaft, independent eastern choir remnants | 4212,1588; datum176. The shaft has four walls and a real central void, not a filled tower. Snow and stone occupy separate measured surface patches. |
| 5 — Violet Threshold | Small shore portal precinct, paired unequal runestones, short upper stairs, separate lower quay and drowned pier | 6052,8080; natural datum17, sea24. Added the missing lowest tidal ascent after the supported-landing check displaced the source spawn. The active opening is a site-owned shader/light effect, not a new transport mechanic. |
| 6 — Sanctuary of the Last Light | Two-stage snow altar, four unequal carved supports and a broken curved rear arch; thin western obelisk and detached eastern chapel | 4948,1420; datum184. The chapel's first two treads initially overlapped at different levels; they now have separate footprints. |
| 7 — Arcade of the Leaning Pillars | L-shaped pointed arcades, low square altar, dry excavations and two individually offset leaning survivors | 10340,5560; datum36. The shafts are stepped voxel forms; fine angled stone carving is still a fidelity limit. The foreground tree was moved clear of the sole approach. |
| 8 — Court of the Quiet Sign | Low snow dais, small inscribed central tablet, broken perimeter and four unequal waystones | 5120,1908; datum138. Keep this low and open; do not replace it with a tall temple. Reference animals are not part of the architectural transcription. |
| 9 — Courts of the Three Crossings | Three spatially separate pavilions with rose collars, scattered stelae, offering mounds, broken connecting walls and a thin bridge over a dry cleft | 9780,6080; datum40. Do not close the three courts into one precinct or fill the bridge's open underside. |
| 11 — Terrace of the Twin Rites | Broad stepped altar, western chapel, four rear arch remnants, uneven survivors, eastern runestone and lower shore flight | 6832,7340; datum36. Extended four floating footings into their own ground; moved the eastern stele off the altar approach. |

The plans record source pixel landmarks, metre-scale interpretation, footprint,
view, terrain levels, exact treads, individually named courses and local tree
anchors. Visible-source interpretation is still candidate where the single
perspective view cannot determine hidden depth or exact dimensions.

## September 10 southern stonework

The author selected refinement of existing southern stonework, not new sculptures
or changes to Fallen Colossus. Violet Threshold's `west-great-rune-stone` and
`eastern-rune-stone`, and Twin Rites' `east-runestone` and
`western-tall-survivor`, retain their registered positions and occupied top-plan
projections. Their own course lists add shallow face incisions, limited crown
chips and branching basal moss. The source images remain the composition
contract; these are candidate detail refinements, not an accepted reconstruction.

Moss courses must use palette material 9 (`MOSS_STONE`), not material 12
(`STONE_WARM`). The first review exposed a warm-stone assignment where growth
was intended; only the newly authored growth records were corrected. Existing
warm-stone courses remain unchanged. Fine moss leaves come from the ordinary
material-masked detail pass, never from an independently scattered site overlay.

The lowered southern natural terrain does not move measured water or stairs:
Violet Threshold, Twin Rites, Drowned Seal Gate and Tidekeeper’s Landing now pin
their preceding absolute registrations. No footprint, stair run, source camera,
site address or Colossus asset is changed. Both edited sites pass exact-spawn,
connected-solid and physical-tread checks (16 and 34 treads respectively).

Visual evidence on September 10: `/tmp/petalfell-south-threshold-v2/` and
`/tmp/petalfell-south-twin-rites-final/` each contain 23 raw views. All clock and
four-distance/four-quarter views were inspected in the labelled
`/tmp/petalfell-threshold-{lighting,quarters}-v2.png` and
`/tmp/petalfell-twin-rites-{lighting,quarters}.png` matrices; both sites' play-r3
views were also inspected at full size. Recesses and basal moss remain confined
to the named stones, and the original approaches and silhouette remain readable.
The matrices establish stonework review, not exact source parity or acceptance;
broad terrain skirts and coarse masonry remain limitations. The coast was
recaptured separately after the subsequent Shallows boundary-loading fix.

## Construction and checks

`MeasuredReferenceSite` writes inclusive ranges from each site's JSON. There is
no column, arch, shrine, ruin, scatter or layout generator. The final solid
projection after all air cuts must equal the ground plan. Per-mass course visits
are capped at 500,000 before volume allocation; material IDs and inclusive XYZ
bounds are checked. Ground-relative courses are restricted to explicitly placed
trees, with their post-terrace anchor checked before writing.

`verify-reference-sites` builds actual production windows. It checks exact
supported source spawns, six-connected solid components meeting the terrain,
central tread clearance, and physics raycasts against the normal chunk mesher's
collision faces. The first pass caught problems that a 2D plan audit missed:

- a surface repaint used the parent terrace's height and erased several treads;
- a low wall and a runestone footing blocked two stair approaches;
- six elevated fragments had no contact with the actual underlying terrain;
- an added foreground tree blocked a stair;
- two chapel treads overlapped at unequal levels.

Reclamation now changes the material at the current tread height. Footings,
walls, trees and the overlapping tread were corrected in their owning plans.
Do not weaken collision checks to make a capture pass.

## Evidence and remaining review

Initial views are under `seal-v1`, `open-sky-v1`, `choir-v1`, `threshold-v1`,
`sanctuary-v1`, `arcade-v1`, `quiet-sign-v1`, `crossings-v1` and `twin-rites-v1`.
They are iteration evidence and may precede corrected geometry. The first
portal frame exposed an unshaded output error: emission alone left a dark oval;
the shader now writes its radiance through the unshaded albedo output.

The current evidence is `shots/big-sites-2026-09-06/review.html`, generated by
`python3 -B tools/build-reference-sites-review.py`. It checks completeness of
235 raw views before generating the gallery. Current directories are
`seal-final`, `open-sky-final`, `choir-final`, `threshold-final`,
`sanctuary-final`, `arcade-final`, `quiet-sign-final`, `crossings-final` and
`twin-rites-final`, each with 23 raw views. `shallows-final-v2` contributes 28;
the preceding `shallows-final` roof candidate is superseded.

Each 23-view set covers locked day/night, five clock phases, and four distances
at four quarter rotations. Every frame was inspected in reduced contact sheets;
selected frames were inspected individually at full size. Two additional
`threshold-package` day/night frames from the final exported game were inspected
at full size. The sites retain distinguishable source arrangements, openings,
separate jetties, changing levels, shared materials and day-cycle lighting.
Coarse masonry, fine broken profiles, sparse rubble, square reclamation patches
and abrupt transitions into ordinary terrain remain visible limitations.
Nothing here is author-accepted or an exact visual replica.

Validation completed on September 6:

- `dotnet build --no-restore`: zero warnings/errors. Linux export and the
  exported binary's atlas audit pass with 14 domains and 14 sites.
- All nine measured top plans pass. Their larger previews now fit the drawing
  area without covering the header/legend. Reference 1 also passes its strict
  plan, camera and vertical audits.
- `verify-reference-sites`: all nine exact source spawns, final connected
  foundations and 181 central tread collision probes pass. The focused final
  Reference 1 rerun passes separate bed/water/deck, exact travel, underside
  clearance, controller water-query and three collision-surface checks.
- Real-controller Shallows approach: 34.13 blocks across Y69..87 in 526 physics
  frames. Violet Threshold: 32.83 land blocks across Y25..34 in 463 frames,
  then 14.05 swimming blocks in 125 frames at sea24. Open Sky: 19.24 blocks
  across Y44..54 in 571 frames. These are bounded routes, not exhaustive play.
- Deterministic repeat and neighbouring-window comparisons pass at
  9560,5480; 5200,1700; 6052,8080; 4212,1588; 10340,5560; and 6400,6980.
  Each comparison covers 442,368 safe overlap cells. A requested map centre
  may resolve to nearby safe ground; the separate site test checks each authored
  source spawn exactly.
- Existing original-site prop checks pass for 23 props / 2,752 triangles.
  The shared rendering check and walking handoff regression pass. Existing
  original sites retain their previous substrate and prop geometry.

Passing logs are copied to `shots/big-sites-2026-09-06/evidence/`; its README
distinguishes the full measured-site run from the final focused bridge rerun.
The gallery's files and generation were checked. Browser interaction testing
was blocked by the browser connection's trust configuration; do not describe
its scene controls as live-verified in this run.

Two material lessons accompany this construction: rose/amethyst masonry now
uses dedicated mineral palette entries rather than blossom or bark IDs, so
stone receives the existing mineral shader; the previous palette entries and
tree materials remain unchanged. Terrain repaint must preserve actual tread
height, and original-site substrate must retain its preceding behavior.
