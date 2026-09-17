# Fitting the continental population field to an agent simulation

- **Lifecycle:** `active`
- **Evidence summary:** `mechanical`
- **Scope:** `tool-specific`
- **Last verified:** 2026-09-17
- **Supersedes:** none
- **Superseded by:** none
- **Owning sources:** [`docs/ECOLOGY.md`](../../docs/ECOLOGY.md),
  `src/Ecology/EcologyTuning.cs`, `src/Ecology/EcologyHarness.cs`,
  `tools/EcologyFieldSmoke.cs`

## Outcome

A coarse Lotka–Volterra field over the whole atlas can be made to agree with a
fine agent simulation by fitting its coefficients to two measured properties of
that simulation — the prey-to-predator ratio and the standing grass level — and
never to its raw population counts, which are in different units. Doing so
produces a damped oscillation that converges on a stable interior equilibrium
over ten simulated hours. It does not reproduce the agent simulation's
trajectory, and it is not a substitute for it.

## Evidence

| Claim | State | Scope | Evidence | Remaining uncertainty |
|---|---|---|---|---|
| The field sustains both species for ten simulated hours | `mechanical` | tool-specific | `./tools/world-authoring.sh ecology-harness 10` — prey settle near 15,800, predators near 2,100, neither reaches an extinction or explosion bound | Untested beyond ten hours, and untested against player hunting pressure |
| Oscillation is damped, not sustained or growing | `mechanical` | tool-specific | Successive prey amplitudes 17,000 → 11,000 → 6,000 → 3,000 in the same run | Whether local per-cell variation stays lively once the continental total settles is not yet measured |
| Equilibrium ratio is close to the source simulation's | `mechanical` | tool-specific | Field settles near 7.5 prey per predator; source harness holds ~150 herbivores to ~30 carnivores, i.e. 5.0 | The gap is unexplained; 7.5 was accepted rather than fitted further |
| Grass stays near saturation, as in the source | `mechanical` | tool-specific | Field grass holds ~94% of total fertility; source harness reports biomass 0.985 | — |
| The source simulation is metastable, not stable | `mechanical` | external | `pnpm harness hours=2` in `fable_sim`: seed `fable-1` loses all herbivores at t=3780 s; seeds `fable-2` and `fable-3` return STABLE over two hours | Unknown how many seeds collapse; only three were run |

## Procedure

1. Run the source simulation's harness on **more than one seed**. A single run
   is not a characterisation, and the default seed is not necessarily
   representative.
2. From its output, take only two numbers: the **prey-to-predator ratio** at
   plateau, and the **standing grass fraction**. Population counts do not
   transfer — the field's units are densities per 128-block cell, the agent
   simulation's are individuals on a 512 m island.
3. Solve for the coefficients rather than searching for them. With the
   equilibrium conditions

   ```
   N* = mP / (b · a)                  prey density at equilibrium
   g* = f − c · N* / r                standing grass
   P* = (e · c · g* − mN) / a         predator density at equilibrium
   ```

   choose `N*`, `g*/f` and `N*/P*` to match the measured targets, then read the
   coefficients off. This took one pass; a blind one-at-a-time search over seven
   constants would not have.
4. Re-run the field harness and compare the two numbers from step 2.
5. Re-run at five times the tested span before believing the result. A
   two-hour run cannot distinguish a stable equilibrium from a slow spiral.

## Checks

### Mechanical

- `./tools/world-authoring.sh verify-ecology-field` — asserts a `stable`
  verdict over two simulated hours, that neither species reaches zero, and that
  an emptied cell refills from its neighbours.
- `./tools/world-authoring.sh ecology-harness 10` — prints the ten-hour curve
  as CSV for inspection. The verdict alone is not enough; read the curve.

### Visual

None. This entry covers the field only, which has no visual presence. Herd and
pack behaviour, once implemented, will need the locked isometric views.

## Scope and limits

Evidence exists for the field in isolation, unhunted, on the authored atlas
with the shipped biome fertility table. There is no evidence yet for the field
under player hunting pressure, under the herd and pack behaviour that will
write kills into it, or across a save and reload — the field is currently
session-scoped because the project has no save system.

## Known failures

**A threshold-only verdict reports a collapse as stable.** The first harness
called a run `stable` whenever both species stayed above 2% of their starting
population. A run whose predators fell monotonically from 554 to 24 over two
hours passed, because 24 is still above 2% of 554 — it was four samples from
failing and the verdict said nothing. Replaced by an additional `collapse`
state: a species whose final sample is both its own minimum and below 40% of
its start has not coexisted, it has merely not finished dying. Any verdict that
only looks at bounds will make this mistake.

**Fitting grazing pressure by intuition.** The first attempt raised
`GrazingRate` sharply on the assumption that prey should be grass-limited. The
source simulation's own biomass reading of 0.985 says the opposite: its prey
are predator-limited and the grass stays nearly full. Measure before assuming
which resource is binding.

## Update triggers

- Any change to a constant in `EcologyTuning.cs`.
- Herd or pack behaviour beginning to write events into the field, which adds
  a pressure this calibration has never seen.
- Player hunting becoming possible, for the same reason.
- A newer characterisation of the source simulation, especially one covering
  more seeds.
- Author correction.
