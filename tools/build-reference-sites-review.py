#!/usr/bin/env python3
"""Build the September site gallery from complete, explicit raw capture sets."""
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
CAPTURES = ROOT / "shots/big-sites-2026-09-06"
SETS = [
    (1, "shallows-final-v2", "shallows-gate-and-causeway-reference-1"),
    (2, "seal-final", "drowned-seal-gate"),
    (3, "open-sky-final", "court-of-the-open-sky"),
    (4, "choir-final", "hollow-choir"),
    (5, "threshold-final", "violet-threshold"),
    (6, "sanctuary-final", "sanctuary-of-the-last-light"),
    (7, "arcade-final", "arcade-of-the-leaning-pillars"),
    (8, "quiet-sign-final", "court-of-the-quiet-sign"),
    (9, "crossings-final", "courts-of-the-three-crossings"),
    (11, "twin-rites-final", "terrace-of-the-twin-rites"),
]
expected = {"reference_match_day", "reference_match_night"}
expected |= {"site_" + clock for clock in ("dawn", "noon", "golden", "twilight", "midnight")}
expected |= {f"site_{distance}_r{quarter}" for distance in ("close", "play", "wide", "far") for quarter in range(4)}
output = Path(sys.argv[1]) if len(sys.argv) > 1 else CAPTURES / "review.html"
command = [sys.executable, str(ROOT / "tools/build-look-review.py"), str(output),
           "--title", "Places from the references.", "--reference-label", "Structural reference",
           "--comparison-note", "Compare the arrangement, openings, levels, material and scale. Each site uses the current production terrain and shared day cycle. These are candidate transcriptions; fine profiles, rubble density and terrain transitions still differ from the sources. Visual review does not establish author acceptance."]
total = 0
for number, directory, registration in SETS:
    site = json.loads((ROOT / f"content/chapter_01/sites/{registration}.json").read_text())
    required = set(expected)
    if number == 1:
        required |= {"reference_top_day"} | {f"site_detail_r{i}" for i in range(4)}
    present = {p.stem for p in (CAPTURES / directory).glob("*.png")}
    if required - present:
        raise SystemExit(f"Incomplete capture set {directory}: {sorted(required-present)}")
    total += len(required)
    label = f"{number} · {site['name']}" if 'name' in site else f"{number} · {site['siteId'].replace('-', ' ').title()}"
    address = site['origin']
    note = f"Permanent address {address['x']},{address['z']} · {len(required)} raw views · production geometry and collision checked. "
    if number == 1:
        note += "Preserved 3x source scale. Bed18 / sea24 / deck87. The top view has its own reference-1-top.png source."
    elif number == 5:
        note += "The opening is an animated visual effect; it does not teleport the player."
    else:
        note += "Each structure and ground intervention is owned by this site's measured plan."
    command += ["--scene", label, str(CAPTURES / directory), str(ROOT / f"world-new/reference-{number}.png"), "--scene-note", label, note]
subprocess.run(command, check=True)
print(f"Validated {total} raw views across {len(SETS)} reference sites.")
