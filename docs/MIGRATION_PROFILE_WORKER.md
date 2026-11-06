# Native profile worker

Assigned checkout: `/home/gongahkia/coding/projects/roag-worktrees/profiles`, branch `codex/native-profile`, base `9c7c3ee4` (implementation baseline `0d5cbf3b`). The full active goal was read from `/home/gongahkia/.codex/attachments/8ba90282-fe56-4de0-bbf8-a2fd4b85d637/goal-objective.md`. Applicable supplied AGENTS instructions and migration decision/progress/preservation/tuning/boss/playtest/overview/parallel documents govern this stream. This C# checkout has no Next.js package/docs; no Next.js code is involved. The sibling Vagabonds checkout is read-only and has not been changed.

All eight aggregate acceptance gates remain open. This record distinguishes implementation, focused backend verification, actual rendered input and final integrated delivery. The root coordinator owns production campaign generation, final-role selection, shared migration records and aggregate goal status. No legacy profile or Lua runtime conversion is added.

## Preserved inventory

Pinned original source: Roag `d498fe0889050d2a23f2ff86d80db6084ed6467a`, `src/achievement_content.lua`, `src/achievements.lua`, `src/profile.lua`, `tests/meta_tests.lua`, original `main.lua` profile/save ordering. The original 22 stable IDs and four rewards are retained in handwritten `Profiles/AchievementCatalog.cs`.

| Achievement suffix | Committed native observation | Permanent boon reward |
| --- | --- | --- |
| first_guardian | Actual EliteDefeated with dead original elite | guardians_vow |
| uncovered_truth | Member discovers a real secret | prospectors_heart |
| chain_reaction | Three distinct boon IDs from one member in one causal chain | circuit_chorus |
| regents_fall | Canonical Hollow Regent victory with a conscious survivor after complete resolution | crownfire |
| first_blood | Member kills a hostile | — |
| pathfinder | Paid physical floor change | — |
| treasure_hunter | Actual chest claim | — |
| coin_purse | Actual currency collection leaves at least 50 shared gold | — |
| first_level | Member reaches level 2 | — |
| veteran | Member reaches level 5 | — |
| boon_bearer | Actual boon grant | — |
| stonebreaker | Member breaks barricade terrain | — |
| ore_seeker | Member breaks an ore deposit | — |
| gate_opener | Member opens timed gate | — |
| key_bearer | Member collects vault key | — |
| warded | Member collects ward flask | — |
| torchbearer | Member lights brazier | — |
| long_heal | Member's native SupplyCollected: healing or approved actual paid hunger restoration | — |
| pressure_survivor | Real confined pursuer arrives, root special-site hook | — |
| deeper_road | Actual member entry into physical stage 2 or deeper | — |
| sanctum_reached | Actual member entry into physical Final Sanctum | — |
| regent_awakened | Actual Regent phase-two event | — |

`first_guardian` preserves the original executed EliteDefeated condition across original biome elites despite its Thorn Warden hint. Chain Reaction extends the original single-player observation per individual member, preserving separate builds: three different companions' unrelated activations cannot combine into one person's chain. Delayed causal observations persist and prune with actual retained causal work. Food uses the already approved hunger-only conversion; stocking food does not earn its consumption badge. No new numerical tuning is introduced.

## Storage and lifecycle policy

New format `roag-native-profile/1`, compatibility boundary `roag-achievements/1`, independent of changing world/combat content hashes. Two checksummed generation records, alternating atomic replacement with flushed file and exact read-back, bounded 128 KiB input, strict IDs/fields/counters, refusal to overwrite corrupt/incompatible progress. Run IDs, permanent awards and terminal markers are copied into both generations before returning. New run reservations may skip an ID after IO failure; they never reuse one issued from a successfully replicated reservation.

Profiles default to `SAVE.profile/`; `--profile PATH` explicitly selects another directory. Playtest CLI rejects profiles outside its isolated control directory and profile symlinks. Preview factories stay ineligible unless explicitly enrolled; ordinary native startup and isolated real-input playtests enroll fresh runs. Existing saves lacking optional profile progress remain unchanged/ineligible on load; no achievements are inferred retroactively from builds or dead enemies. Existing Lua/prototype profiles are never read.

The profile freezes its reward snapshot when a fresh campaign is enrolled. Existing run chest rolls/pool do not gain rewards midway. Death commits earned achievements and a replicated terminal receipt before the world manifest. A failed death manifest cannot resurrect the earlier living state. Saving a newer campaign advances a monotonic current-run receipt; earlier worlds cannot resume. The client already rejects in-game rollback; profile receipts add stale/terminal identity safeguards. Atomic profile commits and the world manifest are separate files: if IO fails after a manifest replacement, the client reports the failure and retains the actual session for retry, without claiming that a failed write restored earlier gameplay. External deliberate editing/deletion of all profile and save data is outside these safeguards.

## Integration APIs

- `WorldSession.EnablePermanentProfile(savePath, profileDirectory = null)`: explicit fresh native campaign only, before paid actions.
- `WorldSession.FlushPermanentProfile()`: flush actual earned outcomes; ordinary successful party commands call it after the full action and scheduled consequences settle.
- `WorldSession.Load(..., profileDirectory = null)`: validates the independent continuing profile only when optional native progress exists.
- `WorldSession.RecordDungeonEntry(member, stage, sanctum = false)`: root calls on actual committed physical member arrival, using that floor's stage. The bounded stairs implementation derives the actual floor directly. Do not call for camera selection, route opening or global complex advancement.
- `WorldSession.RecordPursuerArrival(pursuer)`: root calls only after special-site arrival; it checks an actual living `core:enemy/dread_pursuer`.
- `WorldSession.RecordCampaignVictory(boss)`: root calls after settling actual selected-final-role victory in Endgame. The hook checks selected identity/dead body/living survivor and awards Regent's Fall only for Hollow Regent. Other selected final bosses do not masquerade as the Regent.

Source/profile-specific tests and evidence are being prepared; no compile/test/rendered pass or finished item-20 integration is claimed yet. Root remains responsible for the above new-world entry, special-site and selected-final settlement call sites after combining streams. Full solo/four-party campaign journeys and refreshed Linux delivery remain mandatory.
