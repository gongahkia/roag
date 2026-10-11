# Tranche 14 permanent variety

## Content and rules

| Achievement ID | Exact condition in an eligible run | Future-run boon |
| --- | --- | --- |
| `core:achievement/first_guardian` | Canonical `EliteDefeated` | `core:boon/guardians_vow`: a fully blocked hostile hit bursts in radius 1 for 1/2/3/4 base damage per Common/Uncommon/Rare/Legendary copy. |
| `core:achievement/uncovered_truth` | Canonical `SecretDiscovered` from movement or attack | `core:boon/prospectors_heart`: currency pickup heals 1/2/3/4 HP per copy, capped at effective maximum. |
| `core:achievement/chain_reaction` | Three distinct `BoonActivated` content IDs in one causal chain | `core:boon/circuit_chorus`: another boon activation sends a nearest-hostile spark within four tiles for 1/2/3/4 base damage per copy. Own activations cannot trigger it. |
| `core:achievement/regents_fall` | Canonical `Victory` while the player survives the Regent's death resolution | `core:boon/crownfire`: direct damage to a full-HP hostile applies Burning for three ticks of 1/2/3/4 base damage per copy. |

These powers are provisional. Mixed-rarity stacks add strength through the existing boon resolver. None grants permanent base stats or starting equipment. The first 20 boons remain the default pool. `src/achievement_content.lua` owns stable definitions; `src/achievements.lua` observes authoritative events and stores earned IDs, pending IDs and bounded causal-chain observations. Gameplay RNG is untouched by profile checks. The per-run pool is frozen at New Run, saved in `Game.snapshot`, and validated against chest offers on restore.

## Storage and failure behavior

The independent profile schema `roeg-profile/1` uses the inert active-run codec and two separate checksummed slots. It stores earned flags and a monotonic local run serial. A verified newer incompatible record is rejected without falling back to an older compatible profile. Invalid/truncated newest records recover the previous valid generation. Corrupt records are not silently overwritten.

At an achievement boundary, the active run is saved with a pending award before the profile write. Profile awards merge idempotently by ID and verify both generations before the run outbox is acknowledged. Run-ID reservations also verify both generations before issuing an ID. A successful profile commit clears the in-memory outbox and writes another run checkpoint; a crash between those writes merely retries an already-earned ID. At terminal Victory or Defeat with a pending award, two `pending_terminal` run generations are verified first. It is never offered as live Continue. The profile award is verified before terminal tombstones replace both active generations. On startup, and with title **R**, pending terminal recovery retries idempotently. Failed writes retain the last verified generation; full hardware power-loss durability is not promised because LÖVE has no portable fsync/atomic rename.

`ROEG_GUI_AUTOSTART=1` fixtures do not autosave milestones or earn achievements; explicit Save & Quit still uses the existing run-save path. The **B** developer boon showcase and direct fixture boon/stun/displacement helpers mark the run ineligible, and that flag persists through snapshots and stage changes. External edits to Lua state are outside the local integrity model.

## Checks

```sh
TMPDIR=/var/tmp luajit tests/run.lua
luajit tests/meta_acceptance_trace.lua
TMPDIR=/var/tmp sh tests/meta_gui_smoke.sh
TMPDIR=/var/tmp sh tests/repeat_gui_smokes.sh 3 /var/tmp/roeg-gui-repeat
```

The GUI smoke uses isolated `XDG_DATA_HOME`, two fresh LÖVE processes, a replayed sequence of legal `Game.submit` actions for the elite achievement, title **U**, Save & Quit, and a future New Run. It is callback-driven automated evidence, not a physical-keyboard playtest. Existing `finale_disk_resume.sh` and `biome_disk_resume.sh` continue checking active-run save/Continue and terminal tombstones.

The headless suite also earns Chain Reaction with ordinary seed-3 stage-two actions: it buys Opening Cut and Storm Conductor after the stage-one Emberbrand chest, then fights until all three activate in one causal chain. This guided test reads world state but only changes the run through documented actions.
