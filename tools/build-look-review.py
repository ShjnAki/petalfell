#!/usr/bin/env python3
"""Build a local review page from explicit, unmodified Godot capture sets."""
import argparse
import json
import os
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("output", type=Path)
parser.add_argument("--title", default="Light, material, atmosphere.")
parser.add_argument("--reference-label", default="Style reference")
parser.add_argument("--comparison-note", default="Compare surface finish, light and depth. The accepted atlas owns geography; a style reference may show a different setting. These images do not establish author acceptance.")
parser.add_argument("--scene", nargs=3, action="append", required=True,
                    metavar=("NAME", "CAPTURE_DIRECTORY", "REFERENCE_IMAGE"))
parser.add_argument("--scene-note", nargs=2, action="append", default=[],
                    metavar=("NAME", "NOTE"), help="State the revision and limits of a capture set")
args = parser.parse_args()
notes = dict(args.scene_note)
unknown_notes = notes.keys() - {scene[0] for scene in args.scene}
if unknown_notes:
    parser.error("Notes refer to unknown scenes: " + ", ".join(sorted(unknown_notes)))
output = args.output.resolve()
output.parent.mkdir(parents=True, exist_ok=True)

def url(path):
    return os.path.relpath(Path(path).resolve(), output.parent).replace(os.sep, "/")

scenes = []
for label, directory, reference in args.scene:
    directory = Path(directory)
    if not Path(reference).is_file():
        parser.error(f"Missing reference: {reference}")
    frames = [p for p in sorted(directory.glob("*.png"))
              if not any(token in p.stem for token in ("probe_", "overlay", "difference"))]
    if not frames:
        parser.error(f"No raw capture frames in {directory}")
    scenes.append(dict(name=label, revision=directory.name, reference=url(reference),
                       note=notes.get(label, ""),
                       frames=[dict(name=p.stem, src=url(p)) for p in frames],
                       videos=[dict(name=p.stem, src=url(p)) for p in sorted(directory.glob("*.mp4"))]))

template = r'''<!doctype html><html lang="en"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Petalfell · Look review</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#eee9e6;color:#302e3c;font:15px system-ui,sans-serif}
header{padding:25px 30px 17px;border-bottom:1px solid #cec6cf;display:flex;justify-content:space-between;gap:20px;align-items:end}
h1{font:34px Georgia,serif;margin:4px 0}p{margin:5px 0;color:#686171}.eyebrow{font-size:11px;letter-spacing:2px;text-transform:uppercase}
main{padding:20px 30px}.bar{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:14px}
button,a{font:inherit}button{border:1px solid #c8becb;background:#f9f6f3;padding:8px 14px;border-radius:5px;cursor:pointer;color:inherit}
button[aria-pressed=true]{color:#fff;background:#655771;border-color:#655771}button:focus-visible,a:focus-visible{outline:3px solid #bd7a99;outline-offset:3px}
.pair{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0;min-width:0}
figcaption{display:flex;justify-content:space-between;gap:10px;margin:6px 0 10px;color:#655d6e;font-size:12px}
.image{background:#d7d0da;display:flex;align-items:center;justify-content:center;min-height:280px;height:62vh;overflow:hidden;border-radius:5px}
img{width:100%;height:100%;object-fit:contain}a{color:#655771}#frames button{font-size:12px;padding:6px 10px}#frames{max-height:100px;overflow:auto;margin-top:17px}
details{margin-top:24px;border-top:1px solid #c8becb;padding-top:15px}video{width:min(100%,1100px);max-height:68vh;background:#111;display:block;margin:15px 0}
.note{font-size:12px;max-width:1000px;line-height:1.6;margin-top:15px}output{margin-left:12px;font-variant-numeric:tabular-nums}
@media(max-width:800px){header{align-items:start;flex-direction:column}main,header{padding:18px}.pair{grid-template-columns:1fr}.image{height:48vh}}
</style>
<header><div><div class="eyebrow">Petalfell · Art direction</div><h1>Light, material, atmosphere.</h1>
<p>Source references beside unmodified captures from the running game.</p></div><span class="eyebrow">Work in progress</span></header>
<main><nav id="scenes" class="bar" aria-label="Scene"></nav>
<section class="pair"><figure><figcaption><span>Style reference</span><a id="ref-link" target="_blank">Open original</a></figcaption>
<div class="image"><img id="ref" alt="Supplied style reference"></div></figure>
<figure><figcaption><span id="label"></span><a id="game-link" target="_blank">Open raw capture</a></figcaption>
<div class="image"><img id="game" alt="Unmodified Godot game capture"></div></figure></section>
<nav id="frames" class="bar" aria-label="Capture view"></nav>
<p class="note" id="revision"></p>
<p class="note" id="scene-note"></p>
<p class="note">Compare surface finish, light and depth. The accepted atlas owns geography; a style reference may show a different setting. These images do not establish author acceptance.</p>
<details id="motion"><summary>Motion from the game</summary><div id="videos" class="bar"></div>
<button id="play">Play motion</button><output id="time" aria-live="off">0.00 s</output><video id="video" controls muted playsinline></video>
<p class="note">Recorded with 30 fixed simulation frames per second. PNG capture and encoding time are excluded; this is not a realtime performance benchmark.</p></details>
</main><script>
const scenes=__SCENES__;let selected=0;
const $=id=>document.getElementById(id);
function button(label,action,active=false){const b=document.createElement('button');b.textContent=label;b.setAttribute('aria-pressed',active);b.onclick=action;return b}
function frame(index){const s=scenes[selected],f=s.frames[index];$('game').src=f.src;$('game-link').href=f.src;$('label').textContent=f.name.replaceAll('_',' ');[...$('frames').children].forEach((b,i)=>b.setAttribute('aria-pressed',i===index))}
function scene(index){selected=index;const s=scenes[index];[...$('scenes').children].forEach((b,i)=>b.setAttribute('aria-pressed',i===index));$('ref').src=s.reference;$('ref-link').href=s.reference;$('revision').textContent='Capture set: '+s.revision+' · '+s.frames.length+' raw views';$('scene-note').textContent=s.note;$('frames').replaceChildren(...s.frames.map((f,i)=>button(f.name.replaceAll('_',' '),()=>frame(i))));frame(Math.max(0,s.frames.findIndex(f=>f.name==='reference_match_day'||f.name==='look_noon')));$('motion').hidden=!s.videos.length;$('video').pause();$('videos').replaceChildren(...s.videos.map(v=>button(v.name,()=>{$('video').src=v.src})));if(s.videos.length)$('video').src=s.videos[0].src;$('time').textContent='0.00 s';$('play').textContent='Play motion'}
$('scenes').replaceChildren(...scenes.map((s,i)=>button(s.name,()=>scene(i))));$('play').onclick=()=>{$('video').play();$('play').textContent='Restart motion';$('video').currentTime=0};$('video').ontimeupdate=()=>{$('time').textContent=$('video').currentTime.toFixed(2)+' s'};scene(0);
</script></html>'''
import html
template = template.replace("Light, material, atmosphere.", html.escape(args.title))
template = template.replace("Style reference", html.escape(args.reference_label))
template = template.replace("Compare surface finish, light and depth. The accepted atlas owns geography; a style reference may show a different setting. These images do not establish author acceptance.", html.escape(args.comparison_note))
output.write_text(template.replace("__SCENES__", json.dumps(scenes).replace("<", "\\u003c")))
print(output)
