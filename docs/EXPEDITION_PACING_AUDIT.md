# Fresh-expedition pacing audit

`./dev audit-expedition` runs a bounded, graphics-free command policy against untouched new worlds. It measures recruitment, local discoveries, ordinary encounters, recovery and a dungeon approach using real `WorldSession` commands. It does not edit terrain, move actors directly, grant supplies, use a player save or replace human playtesting.

```sh
./dev audit-expedition --seeds 42,1337,-42,2026 --steps 900 --output artifacts/expedition-audit/current.json
./dev audit-expedition --seed 40 --samples 4 --steps 900 --output artifacts/expedition-audit/consecutive.json
```

The Tools entry point handles output paths and replacement consistently with the other audits. The reusable Core entry point is `ExpeditionPacingAudit.Run(catalog, seeds, stepBudget)`. Limits are 1–16 distinct seeds and 1–5000 successful movement steps per world.

## Reading evidence

`passed` means the observed state invariants held: the clock did not go backwards, inventories stayed nonnegative, actor resources remained valid and party members kept separate positions. **It does not mean every expedition goal completed.** Check each world's `coverageGaps` and each leg's `outcome`, `initialDistance`, `remainingDistance`, `steps` and before/after snapshots.

Snapshots include the leader position, elapsed simulation ticks, successful movement steps, food, medicine, gold and every party member's class, life state, health, mana, wounds, hunger and fatigue. Combat records retain enemy archetypes, executed actions, rounds reached, victory/wipe/unresolved status and the actual resolver's reward. Recovery records retain attempted commands and their real costs/results. An unsuccessful camp attempt consumes no resources.

The controller uses generated coordinates to select goals, including undiscovered goals. It routes only through observed ground and observed unoccupied cells, with a 25×19 surface view following the leader. A successful tile advances 140 ms through the real simulation; decisions take no time. It may reverse leadership using the existing command when companions block a corridor. It does not dig, build bridges, shop, choose equipment, develop companions or optimise combat. Combat uses the first class ability against the lowest-health enemy, with limits of four encounters and 200 actions each. A known camp can be revisited after combat; the camp approach also has its own movement bound.

Consequently, a failed dungeon approach can reflect the controller, local obstacles, party formation or its budget. It is not proof that a dungeon is inaccessible. Manhattan distances are lower bounds; actual audited movement includes detours. Successful first-ability combat is evidence about that encounter sample, not a recommendation to balance every encounter around that policy. Compare reports using the same seeds, movement budget and audit implementation, and retain both gameplay content hashes.

## Initial observation before gameplay-depth changes

The initial preserved-runtime sample used seeds 42, 1337, −42 and 2026, with a 900-step budget and gameplay hash `6D6DD0DD0D05320998C5243CF3CFEC4EFF0EE5DEDAE342F41D3C4CAC1768BCBD`. Each recruited three companions using nine movement steps. The nearest generated discoveries were 59–62 Manhattan tiles from the start; nearest dungeons were 198–422 tiles away.

Four ordinary encounters were won using the stated first-ability policy in 4–10 actions over 1–2 rounds, with total party health losses of 2–13. Each awarded 12 gold, 3 food and 12 experience. Two camp rests completed, each costing four food and 300 simulation ticks; the other two had explicit recovery coverage gaps. No dungeon entry completed within this controller and budget. A separate 1600-step sample also left dungeon coverage gaps, so those routes require targeted inspection or manual play rather than treating the reported walking duration as an intended travel time.

Raw observations are retained locally under `artifacts/expedition-audit/baseline.json` and `baseline-extended.json`. The baseline used a preserved pre-change Core assembly, SHA-256 `b5ac9fd11d98532ab5d09e764208911420b3f725328ed5d901ea5f6e318b6a40`, from checkout `2817d53`. An isolated harness compiled the audit against that assembly so concurrent implementation could not silently change the baseline. These ignored artifacts are evidence from this checkout; a clean checkout reproduces the current catalog/runtime, not that archived binary.

Three short tests cover deterministic fresh-world execution and real recruitment/time, honest reporting of uncovered goals under a tiny budget, and explicit seed/budget limits. Longer representative runs belong in milestone evidence rather than mandatory per-test multi-minute simulations.

## Completed gameplay pass comparison

The final-source four-seed sample, retained in managed run `20261023-013614-8663deaa/audit-expedition.json`, uses the same 900-step policy as the baseline. Each world again recruited a four-person party in nine steps. The sampled fights now distinguish composed situations from solitary creatures:

| Seed | Before: actions / party HP lost | After: actions / party HP lost | New gold / food / XP |
| --- | --- | --- | --- |
| 42 | 10 / 4 | 17 / 24 | 16 / 2 / 20 |
| 1337 | 6 / 6 | 6 / 6 | 3 / 0 / 8 |
| −42 | 4 / 2 | 4 / 2 | 2 / 2 / 6 |
| 2026 | 10 / 13 | 12 / 22 | 14 / 2 / 16 |

All four sampled fights were victories. The two changed compositions demanded more actions and health than their earlier counterparts; solitary creature fights retained their sampled action counts while granting distinct rewards. This is a measured consequence of the changes, not proof that every composition is balanced or that basic attacks are always sufficient.

Two camp rests completed and two recovery gaps remained. Seed 1337 failed to approach a serviceable camp; seed −42 had not observed one. No dungeon entry completed within this controller and budget. The separate population audit explicitly activates 576 floors across four seeds and establishes usable placement there; it does not fill this fresh-expedition route coverage gap. Human playtesting should therefore examine long journeys, supplies and recovery before changing world size, encounter weights or travel speeds.
