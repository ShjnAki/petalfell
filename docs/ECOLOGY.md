# Living ecology — design

A persistent Lotka–Volterra ecosystem for the production continent, ported from
a working simulation in another project, offered behind a flag.

Status: the field, the herd and the wolf are implemented behind `--ecology`;
the player layer is not. Nothing here is author-accepted, and nothing here has
been seen at the locked isometric view.

## Current state

Implemented and mechanically verified:

- the continental population field, its equations, and a harness that carries
  it ten simulated hours without extinction or explosion;
- coefficients fitted to the source simulation's measured ratio and grass level;
- herd steering on the grazing animals;
- the wolf: body, pack decisions, endurance, the chase, and the kill;
- spawning that reads the field, so a valley's population decides what appears
  in it;
- `--ecology` gating all of it, with the default world provably untouched.

Not implemented: the player's vitality, breath and hunger; death and the
campfire; the grudge and any hostility toward the traveller. Wolves currently
ignore the traveller completely, which is the intended end state for an
un-provoked player but is presently the *only* state.

Known limitation: a wolf cannot path around terrain. It steers straight at its
quarry and stops where the ground refuses it, so hunts conclude on open ground
and stall against broken ground. Evidence and the rest of the detail are in
[`building-knowledge/ecology/land-fauna-and-the-hunt-2026-09.md`](../building-knowledge/ecology/land-fauna-and-the-hunt-2026-09.md).

Run it with:

```bash
godot-mono --path . -- --terrain-focus=2692,2164 --ecology
```

---

## Why this exists

This work comes from a finished simulation in another project of mine —
[`fable_sim`](https://github.com/ShjnAki/fable_sim). It is a Lotka–Volterra
ecosystem: herbivores, wolves, a grass field, needs, herds, endurance hunting.
It regulates itself: herds grow until wolves catch up with them, wolves thin out
until the herds recover, and the whole thing holds a plateau for tens of minutes
with nobody touching it. Clone it, run `pnpm dev`, and leave it running. That is
the part worth judging, and it is easier to watch than to argue about.

Being precise about what it does and does not do, because the distinction
matters here: the coexistence is **metastable, not permanent**. Its own tuning
log calls it "a slowly unstable fixed point". Measured over two simulated hours
on three seeds, two hold a plateau — roughly 150 herbivores to 30 carnivores —
and one loses its herbivores after an hour. The port described below does better
on that specific point, because a coarse field with a rarity refuge and
neighbour diffusion cannot lose a species the way a few hundred agents on one
island can; ten simulated hours of it converge rather than collapse. But the
behaviour worth having came from the agent simulation, not from the field.

So: a thing that works, with its limits known, and nowhere to live. Petalfell is
a world large and quiet enough that a living ecology would mean something inside
it.

This cuts against what the documents say. `plan.md` asks for a calm, lonely,
mostly abandoned continent, and `CLAUDE.md` keeps wildlife deliberately sparse.
I have not tried to argue that away. The design instead tries to earn its place
under those constraints:

- **The world stays calm by default.** Wolves do not treat the traveller as
  prey. You can cross the entire continent, walk past packs, watch them run down
  deer, and never once be attacked.
- **The danger is something the player chooses.** Killing a wolf is what makes
  you a target — and only for that pack, only inside its own territory. They
  never leave it to pursue you. Quiet is the default state of the world, not a
  difficulty setting.
- **Nothing changes until it is switched on.** The system lives in its own
  namespace behind a flag. With the flag off, `Fauna` behaves exactly as it does
  today.
- **The fiction is left alone.** This proposes a mechanism, not a meaning. Why
  the continent emptied, and whether restoring it should be a goal, remains the
  author's to write.

The ecology also reads the authored geography instead of inventing a second one:
carrying capacity comes from the existing biome regions, so herds gather where
the maps already say the land is rich.

Two honest caveats. The source is TypeScript and this is Godot/C#, so this is a
port, not a drop-in: the mechanics and the tuning transfer, the substrate does
not. And the direction is a genuine preference of mine — I come from survival
games, and the ones I return to are contemplative *because* of the survival, not
in spite of it. Hunger is what makes a long quiet walk feel like a journey rather
than a camera move. That may be a register this world already wants; it may not.
It is the author's call.

One more thing, which only became clear late. The two projects are exact
inverses. In mine the world lives and the player is a guest who arrived late;
in Petalfell the traveller is the subject and the world is arranged around
their gaze. Put together they make something neither has alone: a beautiful
world that does not care you are in it.

That may matter more than the mechanics do. `plan.md` asks for a world that is
calm, lonely and enormous. An empty valley is not lonely — it is only empty. A
valley where a wolf runs down a deer whether or not anyone is watching is
lonely, because it tells you the place was here before you and will carry on
after. Indifference is what loneliness is made of, and it is the one thing a
staged world cannot fake.

What is on offer is a trade: this uses the author's assets, terrain, streaming
and creature system as they stand, and hands back a tuned ecology the author
would otherwise have to build and balance. If the answer is no, the branch
reverts cleanly and nothing on the production path has been touched.

---

## Decisions this design rests on

These were settled with the contributor before the design was written. They are
recorded here so a reviewer can disagree with a decision rather than with its
consequences.

| Question | Decision |
|---|---|
| Player's relation to the ecology | Both **prey and predator** and **living resource**: the player hunts, can be hunted, and the ecology feeds the existing fishing/inventory/campfire loop |
| Player survival depth | **Vitality, breath, hunger.** No thirst, no temperature |
| Trophic chain | **Wolf ported from the source project** — deer/rabbit/goat ↑ wolf, with packs, dens, endurance hunting, water refuge |
| Death | **Wake at the last lit campfire**, carried food lost, low vitality. Progression and discovered places are kept |
| Simulation architecture | **Persistent coarse field + streamed bodies as its projection** |
| Hostility | **Wolves ignore the player until they kill one.** Then the wronged pack engages the player, inside its territory only |
| Grudge scope | **The wronged pack**, durably. Other packs stay indifferent. Tunable to witnesses-only or to decay |
| Night | **No nocturnal threat layer.** Night keeps only the herd sleep behaviour from the source project |
| Narrative goal | **Out of scope.** This ships a mechanism, not a meaning |

---

## The problem the architecture has to solve

In the source project the equilibrium is *emergent*: it arises from 158 agents
living continuously on a 512 m island. In Petalfell, creatures are discarded
once the traveller passes the retention radius — 384 blocks on the production
window path, 96 on the legacy one — and `Fauna` states the reason plainly, and
the reason is good: *"an animal the player has never seen has no history worth
preserving."* The continent is 12,288 × 9,216, and the live population is
between 16 and 27 bodies.

A predator–prey cycle takes tens of minutes of simulated time. If nothing
persists, the player never sees a cycle — only wolves. The whole design turns
on this.

The answer is two layers. A coarse, cheap, persistent field carries the
population dynamics across the whole continent. The existing streamed creatures
become a *sample* of that field near the traveller, with the ported agent
behaviour, writing events back into it.

---

## 1. Module layout

A new namespace, `Petalfell.Ecology`, under `src/Ecology/`:

| File | Responsibility | Godot? |
|---|---|---|
| `EcologyField.cs` | The persistent grid and its equations. Data and mathematics, nothing else | **no** |
| `EcologyTuning.cs` | Every parameter in one place — the port of the source project's `species.ts` plus the fitted field coefficients | no |
| `EcologyHarness.cs` | Accelerated headless run, for tuning and for proving the equilibrium | no |
| `Ecosystem.cs` | The `Node` that owns the field, ticks it, and exposes queries and events | yes |
| `HerdBehaviour.cs` | Herd boids and flight, ported from `boids.ts` / `agentTick.ts` | yes |
| `PackBehaviour.cs` | Wolf state machine: hunt, sprint and stamina, pack, scavenging, bite, grudge | yes |

The boundary that matters: **`EcologyField` does not know Godot exists, and
`Fauna` does not contain a single equation.** That is what makes the equilibrium
testable headless without starting the engine — the same separation the source
project gets by keeping its `sim` package free of DOM and Three.js dependencies.

Files modified, all of them additively:

- `src/World/Fauna.cs` — reads the field when one is present, new `Species.Wolf`
  and its four-box body, reports kills back. Its current path is untouched when
  the field is absent.
- `src/Player/Vitals.cs` — new. Vitality, breath, hunger.
- `src/Player/Character.cs` — carries the vitals and their effects on movement.
- `src/World/CampfireSystem.cs` — remembers the last lit fire. Its only change.
- `src/Tools/AtlasSectorReview.cs` — wiring, behind the flag. This file is the
  production runtime host despite its name; `Main.cs` calls it for ordinary play.
- UI for the three gauges.

### The flag

`--ecology` enables the system. Without it `Ecosystem` is never constructed,
`Fauna` takes exactly the code path it takes today, and `Vitals` is inert. This
is a review requirement, not a debug convenience: a contribution that changes
default behaviour is refused on principle before anyone looks at the wolves.

---

## 2. The ecology field

Cells of 128 blocks give **96 × 72 = 6,912 cells** over the atlas. Each carries
`Grass`, `Prey`, `Predator` and a fixed `Fertility`, plus two scratch buffers
that let the diffusion sweep read one consistent state instead of smearing
populations in the direction it happens to run. Six `float` arrays,
allocated once at startup, about 162 KiB, never grown. This satisfies the standing rule that runtime allocations
stay bounded and no continent-sized voxel or height array is ever built.

Per cell, per field tick (dt ≈ 1 s of game time):

```
grass     g += ( r·g·(F − g)  −  c·N·g ) · dt
prey      N += ( e·c·N·g  −  a·N·P  −  mN·N ) · dt
predator  P += ( b·a·N·P  −  mP·P ) · dt
```

`F` is the cell's fertility. The grass logistic is the one from the source
project's `biomass.ts`, retargeted from 1 to `F` so that the land's richness
enters the equation rather than being painted on afterwards.

Three additions that the source project learned the hard way, and without which
this goes extinct:

**Fertility from the authored biome.** `F` comes from
`Plan.RegionAt(x, z).Biome` — forest and meadow rich, snowy highland poor, water
carrying no land prey. The ecology inherits the authored geography instead of
inventing a second one, which is also what satisfies the standing rule that
anything reading as a region comes from a wavelength field and never from a
per-block hash.

**Diffusion between neighbours.** A slow five-point stencil on `N` and `P`:
animals walk. This is what makes an emptied valley refill *from the valleys
around it* rather than by spontaneous generation, and it produces slow
population waves travelling across the continent — which is worth a great deal
when the game is about walking across it.

**The rarity refuge.** Ported from the source project: below a threshold a
population's birth term is boosted, because survivors of a crash face less
competition. Combined with diffusion this means a region can be emptied but the
species is never lost. Local extinction is allowed; continental extinction is
not.

### Determinism

Initial state derives from biome and `_worldSeed` through `Core/Rng`
(mulberry32, the same family as the source project's PRNG), and is never
re-rolled. The field is gameplay state, not geography: it evolves, but two runs
from the same seed and the same player actions produce the same evolution.

### Known limitation: no persistence

Petalfell writes nothing to `user://`. The field therefore lives for the length
of a session, and an over-hunted valley is repaired by restarting the game. This
is recorded rather than solved: inventing a save system inside this contribution
would be scope the author did not ask for. If the system is accepted, session
persistence is the obvious follow-up.

---

## 3. Coupling the field to the visible world

One rule, from which everything else follows: **the field is the truth, the
bodies are its projection.**

**Field → world.** `Fauna.TrySpawnAtlas` queries the cell instead of rolling
fixed per-biome probabilities. Live body count and species mix become functions
of `Prey`, `Predator` and `Grass`. A cell with high predator density produces
wolves. A valley teems or it is silent, and it is not decoration.

**World → field.** Only **events** write. A wolf killing near the player removes
a prey unit from the cell. The player hunting does the same. Culling **never**
writes: a creature unloaded because the traveller walked away is not a dead
creature, and confusing the two would empty the continent behind the traveller's footsteps.
This is the principal failure mode of this architecture and it gets its own
verification check.

This is where "living resource" lives. Emptying a basin drives `N` below its
local recovery point; deer stay gone for a long time; the wolves there thin out
or migrate by diffusion. Letting it rest repairs it. None of that is scripted —
it is the equation.

---

## 4. The wolf, and the grudge

A near-literal port of `agentTick.ts` onto `Critter`: the `decide.ts` state
machine, endurance hunting, the water refuge (deer swim at 0.62, wolves at 0.30
— this is *the* release valve that stops packs from clearing the herds),
scavenging, the territory cap, dens.

The grudge is clan state, not individual state. `PackGrudge` is raised on the
clan by the first death the player inflicts. Engagement of the player then
requires **three conditions at once**:

1. the grudge is raised on that clan;
2. the player is inside that clan's territory;
3. enough of the pack is present — the ported `humanHuntPackMin`.

Outside the territory, `homeRange` and `homingWeight` take over and they go
home. There is no pursuit, ever. A grudging pack can be escaped by leaving its
valley, which is how wolves behave and which puts the geography back in charge
of the danger.

Half of this mechanism is already written in the source project: `CARNIVORE`
carries `homeRange: 400` and `homingWeight: 0.35`, and `humanHuntPackMin` already
gates whether wolves dare take on a human at all. The port consists of not
exempting the player from the recall, and of gating the whole behaviour behind
the grudge.

---

## 5. The player layer

`src/Player/Vitals.cs`, new and self-contained: vitality, breath, hunger.

The `PLAYER` values from the source project transfer unchanged, and so does the
flight equation they were tuned around: the wolf sprints at 12 m/s and the
player at 11, so the wolf gains 1 m/s — but the wolf's wind lasts 25 s and the
player's 30. **You do not escape by speed, you escape by breath.** Caught at
10 m it is a different matter: three bites kill.

Hunger consumes meat; meat comes from hunting and from the fishing system that
already exists; cooking comes from the campfire that already exists. Nothing new
is needed on the item side — `FishingSystem`, `GlobalInventory` and
`CampfireSystem` are there and plug in.

Death wakes the player at the last lit campfire, carried food lost, vitality
low. Lighting a fire stops being decorative and becomes the act of setting a
waypoint.

---

## 6. What supplies a goal

The life and the difficulty fall out of the mechanism. The goal does not, and
this design does not pretend otherwise.

What the architecture actually produces is **a journey with consequences**.
Crossing the continent requires meat; meat requires deer; deer require grassy
valleys; and wolves contest the same valleys. Campfires become a chain of
waypoints left behind. Emptying a basin closes a route for an hour of play. That
is a goal structure that emerges from logistics without a single quest — which
sits well with a game whose own plan says the reward is the walk.

An explicit narrative goal on top — restoring regions, understanding why the
continent emptied — is a separate layer that the field would support well. It is
deliberately **not** in this contribution. It touches the fiction of the world,
which belongs to the author far more than the mechanism does.

---

## 7. Calibration

The source project's equilibrium is emergent, arising from agent-level
parameters: `sprintRange`, `staminaDrainPerSec`, herd escape chance,
`territoryMax`, the water refuge. The field runs on equation coefficients —
`a`, `b`, `mN`, `mP`, `r`, `c`, `e` — and those appear nowhere in `species.ts`.

They are therefore **fitted against the source simulation**, which is the
reference and not merely the inspiration. Raw population counts do not transfer
— the field's units are densities per 128-block cell, the source's are
individuals on a 512 m island — so the fit targets two properties that are
unit-free: the prey-to-predator ratio at plateau, and how much of the available
grass is left standing.

Measured in the source harness across three seeds: roughly **5 prey per
predator**, with **grass at 0.985**, meaning its prey are limited by wolves and
not by forage. Solving the field's equilibrium conditions for those two targets
gives the coefficients in `EcologyTuning.cs` in a single pass.

The result, over ten simulated hours: a damped oscillation. Prey swing
26,000 → 9,000, then 22,000 → 12,000, then 20,000 → 14,000, settling near
15,800 prey to 2,100 predators with grass at 94% of capacity. The predator peak
lags the prey peak by half a cycle, as it should. Continental totals settle;
individual cells do not, and player hunting will keep them from doing so.

The agent behaviour near the player keeps the original values verbatim; it is
what the player actually sees. The field is fitted so that the two layers tell
the same story instead of drifting apart. The method and its two failed first
attempts are recorded in
[`building-knowledge/ecology/field-calibration-2026-09.md`](../building-knowledge/ecology/field-calibration-2026-09.md).

---

## 8. Verification and evidence

Following the repository's existing smoke pattern
(`tools/XSmoke.cs` + `x-smoke.gd` + a `verify-x` case in `world-authoring.sh`):

- **`EcologyHarness`** — port of the source project's `runHeadless`. Two hours
  of accelerated simulation, verdict of coexistence / extinction / explosion, no
  engine required.
- **`verify-ecology`** (`tools/EcologySmoke.cs` + `ecology-smoke.gd`), covering:
  - **window crossing does not perturb the field** — walking across a window
    handoff must leave cell populations unchanged. Culling is not death;
  - **default non-hostility** — a player who has never killed crosses ten packs
    without being engaged once;
  - **no pursuit** — a grudging pack disengages at its territory boundary;
  - **bounded allocation** — the field allocates once and never grows.
- **Population curves** from `EcologyHarness` overlaid on the source harness's
  curves, as evidence that the calibration holds.
- **Captures** at the locked isometric reference view, day and night, per the
  repository's capture and acceptance workflow.

Author acceptance of the visual result is separate from all of the above and is
the author's alone, per `building-knowledge/CERTAINTY.md`.

---

## 9. Documentation obligations

Under the repository's documentation discipline, this contribution must:

- state its conflict with `plan.md` explicitly in `plan.md`, rather than
  silently contradicting it;
- add its current order and open work to `docs/ROADMAP.md`;
- record implemented facts in `CURRENT_STATE.md`;
- carry the tuning rationale into `building-knowledge/`.

On that last point: the source project's tuning comments are **translated, not
summarised**. Lines like *"the prey's refuge: a deer swims well, a wolf swims
badly"* and *"you do not escape by speed, you escape by breath"* are not
commentary — they are hard-won knowledge explaining why each constant holds the
value it holds. Losing them in translation would ship magic numbers.

---

## 10. Out of scope

- Narrative goals, and any explanation of why the continent emptied.
- Session persistence of the field.
- A nocturnal threat layer.
- Thirst, temperature, or any further survival axis.
- New wildlife beyond the wolf; the existing species keep their roles.
- Any change to terrain, sites, water, weather, rendering or the atlas.
