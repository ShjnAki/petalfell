# Putting land animals and a predator into the production window

- **Lifecycle:** `active`
- **Evidence summary:** `mechanical`
- **Scope:** `tool-specific`
- **Last verified:** 2026-09-18
- **Supersedes:** none
- **Superseded by:** none
- **Owning sources:** [`docs/ECOLOGY.md`](../../docs/ECOLOGY.md),
  `src/World/Fauna.cs`, `src/Ecology/HerdBehaviour.cs`,
  `src/Ecology/PackBehaviour.cs`

## Outcome

Grazing animals and a predator can live in the moving production window,
gathering into herds and hunting each other, with kills written back into the
continental population field. It requires three habitat rules rather than one,
and the separation between them is the whole technique. It does **not** give a
predator any ability to path around terrain, so hunts conclude on open ground
and stall against broken ground.

## Evidence

| Claim | State | Scope | Evidence | Remaining uncertainty |
|---|---|---|---|---|
| Land animals and wolves spawn and persist in the production window | `mechanical` | tool-specific | Headless run at 2692,2164 with `--ecology`: census reports 5 deer, 2 rabbits, 2 goats, 2 wolves alongside 20 marsh animals | Only one address observed; northern and southern ground may differ |
| Spawn density follows the field | `mechanical` | tool-specific | The cell under the traveller read prey 2.47, predators 0.14 — the field's own computed equilibrium — while bodies appeared at matching proportions | Not yet observed after a valley has been hunted out |
| Wolves take prey, and only a kill writes to the field | `mechanical` | tool-specific | Same run: `kills 2`, goat count fell 2 → 0; the smoke asserts a kill decrements exactly one cell and that repeated kills floor at zero | Not observed across a window handoff |
| Nothing exists without the flag | `mechanical` | tool-specific | `verify-production-playability 2692,2164 land` prints no ecology line and passes; `EcologySpeciesAllowed` refuses a wolf outright | — |
| A predator cannot cross broken ground to reach prey | `mechanical` | tool-specific | Census shows a wolf holding 31–38 units from visible prey for minutes, alternating Sprint and Recover without closing | Unknown how much of the continent is passable enough for a hunt |

## Procedure

1. **Keep the existing habitat rule untouched.** `TryAtlasHabitat` encodes the
   southern wetland's own requirements — reed ground, southern latitude,
   standing water. Land animals want the opposite of most of it. Folding both
   into one function produces a chain of species exceptions inside a habitat
   rule.
2. **Write a spawn rule and a movement rule, separately.** A birthplace wants
   flat, open, dry ground; a step wants only "may I stand here, one terrace at
   most from where I am". They are different questions and the answers differ
   on most of the map.
3. Route `Legal`, `HabitatValid` and `Setup` each to the correct one. Missing
   any of the three produces a distinct failure; see below.
4. Give steering and hunting to creatures through **nullable delegates** set by
   `Fauna`. Null is the ordinary case, which is what makes a feature flag
   honest rather than merely quiet.
5. Put hysteresis on anything with a threshold and a cost.

## Checks

### Mechanical

- `./tools/world-authoring.sh verify-ecology-field` — steering, hunting
  decisions, hysteresis, and that a kill touches exactly one cell.
- `godot-mono --headless --path . -- --terrain-focus <x,z> --ecology` — prints
  `[ecology-census]` every five seconds with the live body counts, each wolf's
  intent, its distance to the nearest prey, its breath, and the field values
  under the traveller. This is the only thing that distinguishes a hunt that
  never starts from one that never concludes.
- `./tools/world-authoring.sh verify-production-playability <x,z> land` without
  the flag — proves the default world is untouched.

### Visual

Not yet done. Nothing here has been seen at the locked isometric view, at play
distance, or at night. No claim about how any of this looks is supported.

## Scope and limits

Evidence exists for one address, one session, in headless mode, with the
traveller stationary. There is no evidence for: a moving traveller, a window
handoff, a hunted-out valley recovering, night behaviour, or anything visual.

## Known failures

**Using the spawn rule for movement freezes every animal.** Wolves reported
`Stalk` continuously with the distance to prey frozen to the metre — 47 and 145
across dozens of samples. `Legal` was routing land animals through
`TryAtlasHabitat`, which is false everywhere outside the southern wetland, so
every step was refused. A creature whose every step is refused does not stand
still; it turns on the spot forever, which reads as a frozen world rather than
as an error. **The distance not changing at all was the diagnostic** — a
decision that is correct while nothing moves means the movement is blocked, not
the decision wrong.

**A threshold without hysteresis produces a twitch, not a behaviour.** Breath
pinned at exactly the recovery threshold across every sample: the wolf broke
off, regained a sliver, re-engaged, drained straight back. Any state entered on
`value <= limit` and left on `value > limit` will do this. Leave on a
noticeably higher mark.

**Running `godot-mono --headless --import` invalidates the C# assembly.** Every
smoke then fails with `Invalid call. Nonexistent function 'new' in base
'CSharpScript'`, which looks like a missing script rather than a stale build.
Run `dotnet build` again after any import.

## Update triggers

- Predator pathfinding of any kind, which would invalidate the passability
  limitation recorded here.
- The player becoming huntable, which changes what the hunt is for.
- Any change to the three habitat rules or to which caller uses which.
- The first visual review, which this entry cannot anticipate.
- Author correction.
