# Tranche 09 acceptance and profiling notes

## Evidence classes and replay

`luajit tests/acceptance_trace.lua /tmp/roag-acceptance.tsv` writes the
seed-3 guided trace. The driver observes the current game state and submits
ordinary `Game.submit` intents only. It does not reposition an actor, change
health/stats/currency, clear schedules, or change enemy behavior. This is a
guided automated strategy, not an unassisted run or a human playtest.

The tested path kills ordinary enemies for XP/currency, buys the first cached
paid-chest offer (Emberbrand in the expanded Tranche 11 pool), ignites a real target, travels to
the elite, triggers phase two and defeats it, claims the free chest, confirms a
prototype route, and kills an ordinary enemy on stage two. The full action and
event sequence is replayed from the same seed and compared to an identical
snapshot. A second test serializes the resulting state through the inert save
codec, restores a new game object, and compares subsequent actions/events.

`luajit tests/attempt_full_run.lua /tmp/roag-full-attempt.tsv` attempts to
continue that same legal-action strategy through stages two and three. It
prints the first failure rather than assigning a successful outcome. Reaching
the final prototype endpoint with this strategy is **not** an automated
acceptance claim. Existing prepared stage GUI tests use direct state setup;
they are valuable UI integration evidence, not a legal survival playthrough.
In the measured seed-3 attempt, the stage-two elite died after action 719
with 4 HP remaining; the route entered stage three. The guide died during
the stage-three elite fight at action 1048. It did not demonstrate the third
exit. This is evidence that the particular strategy failed, not a proven
map softlock or a human balance verdict.

Tranche 11 changed seed 3's paid chest choice to earned Emberbrand. On the
current build, the same legal-action guide reached the explicit non-victory
prototype endpoint in 965 actions with 1 HP. This is a guided automated run,
not a human playtest or a guarantee for other seeds/builds.

Independent ordinary-action checks use seed 9 for the movement sigil, seed 65
for an empty-tile Sword Strike on the cracked wall, seed 13 for a cardinal
door bump/open/enter, and seed 1 for terminal death and rejected later input.

## Reproducible measurement commands

```sh
luajit tests/profile_headless.lua 9
sh tests/profile_gui.sh
tests/repeat_gui_smokes.sh 5
```

The headless benchmark times `Game.submit` with `os.clock` and reports CPU
milliseconds per action, event counts and sampled Lua heap. The normal run
uses seed 21. The crowd scenarios add 15, 40 or 90 valid synthetic Skitters
through the public spawn API, beyond the normal population cap for stress
purposes. The high-stack fixture equips 40 Legendary copies and triggers a
real Sweep/proc chain. Peak internal effect-queue length is unavailable with
current non-invasive instrumentation; it is labeled as such. The benchmark
does not assert hardware-specific latency thresholds.

The GUI benchmark uses a fresh temporary LÖVE directory and two callback
scenes: a generated fog/minimap/scroll view and a prepared boon demo.
`F3` toggles the same presentation-only diagnostic overlay in ordinary play.
Frame intervals are observed between draw callbacks; `update` and `draw` CPU
durations exclude GPU completion and are reported separately. LÖVE's FPS,
draw-call and texture-memory stats are sampled when available. These
callback-driven samples do not establish performance on a normal laptop.
The normal game does not collect timing samples until F3 or
`ROAG_DIAGNOSTICS=1` enables them.

One matched isolated LuaJIT 2.1/x86_64 seed-21 run (`9` actions for normal,
`3` each for the synthetic crowds) gave the following CPU action p50 values:

| Scenario | Actors | Before AI occupancy change | After | After p95 | Peak sampled Lua heap |
| --- | ---: | ---: | ---: | ---: | ---: |
| Generated wait | 17 | 6.35 ms | 4.51 ms | 12.78 ms | 3,186 KiB |
| +15 synthetic enemies | 32 | 31.03 ms | 14.09 ms | 18.71 ms | 3,628 KiB |
| +40 synthetic enemies | 57 | 53.67 ms | 17.76 ms | 26.64 ms | 3,763 KiB |
| +90 synthetic enemies | 107 | 155.15 ms | 26.58 ms | 27.95 ms | 4,423 KiB |

These very short CPU samples are noisy and the 107-actor case is far above
the ordinary 18-normal-enemy pressure cap. The 40-copy Legendary fixture
produced 118 events and 32 activation/due events in three actions; its
isolated action p95 was 1.93 ms with a sampled 2,064 KiB peak. A second run
under other system load measured a 62.86 ms median for the 107-actor case,
so the table is evidence of a hot-path improvement, not a stable percentile
guarantee. The source files under `/tmp/roag-tranche09-profile-before.tsv`
and `/tmp/roag-tranche09-profile-after.tsv` record the matched run.
In a longer post-change run (`luajit tests/profile_headless.lua 30`), the
normal 17-actor stage had 30 actions at p50/p95/max 5.06/6.65/8.76 ms;
the 107-actor synthetic crowd had 10 actions at 21.01/31.81/31.81 ms;
and the first 30 inputs of the seed-3 legal route had 3.35/5.87/7.50 ms.
The raw output is `/tmp/roag-tranche09-profile-final30.tsv` in this work
session; rerun the command for portable evidence on another machine.

LÖVE 11.5 used the WSLg display and Mesa **llvmpipe software OpenGL** for
the measured GUI scenes. In 179 sampled draw intervals per scene, the
generated fog/map/scroll view measured p50/p95/p99 of 5.36/10.89/20.82 ms;
the prepared boon-hit scene measured 7.43/11.08/14.21 ms and observed three
`BoonActivated` events. The measured Lua draw CPU p95 values were 1.68 and
1.62 ms respectively. No ordinary laptop GPU/FPS target is inferred.

The persistence GUI smoke uses two separate LÖVE processes sharing only an
isolated test save directory. In addition to checking displayed time and
Continue, it now compares the complete inert-encoded authoritative snapshot
across processes and compares the result of the next Wait against a saved
control continuation. Headless persistence regressions separately cover
rotating-slot corruption fallback, failed writes, valid-newer incompatibility,
terminal tombstones, pending attacks/echoes, reward caches and retired stages.
The GUI smoke does not overwrite a user's platform save directory.

## Narrow dependency and state boundary map

- `src/game.lua` owns authoritative intent validation, immediate player
  actions, damage batching, ordered gameplay events and snapshot/restore. It
  has no LÖVE calls; `main.lua` calls into it and owns menus, camera and HUD.
- `src/scheduler.lua` orders committed effects before new actor decisions and
  preserves timestamp/sequence ties. Stage deadlines and delayed boons use
  that same schedule; no frame delta enters it.
- `src/boons.lua` owns subscription filtering, causal ancestry, pair
  correlation and the iterative effect queue. Its per-action context is
  cleared after resolution; pending delayed payloads stay plain data in
  authoritative state.
- `src/generation.lua` builds a concrete floor from validated room content;
  `src/world.lua` handles door/secret events; `src/visibility.lua` derives
  FOV from that floor. The saved concrete tilemap takes precedence over seed
  regeneration. Generation, pressure and loot streams are separately derived.
- `src/stage.lua` owns elite activation/death, pressure, deadline, pursuer and
  route lifecycle. `game.lua` carries run-level stats to a new stage and
  stores retired floor state frozen, so old pending effects cannot act there.
- `src/persistence.lua` is an inert plain-data codec plus two-generation
  checksum/compatibility service; `src/love_storage.lua` is its LÖVE adapter.
  The renderer has no authority to alter loaded simulation outcomes.
- `src/playback.lua` and `main.lua` render ordered events, FOV-filtered
  entities and known telegraphs. The F3 diagnostics reads these outputs only.

The one measured hot path was AI BFS repeatedly rescanning all entities for
each candidate tile. `src/ai.lua` now builds a local occupancy set once per
decision and discards it afterward. It does not cache across turns, avoiding
stale door or actor positions. Existing AI, timing and replay tests verify
the same authoritative behavior.

## Human playtest checklist

1. Start a new normal run in `love .`; record seed and actual display/GPU.
2. Try WASD movement, diagonal F/R aim, Dash into an obstacle and Wait. Check
   that aiming/canceling/menu/Tab/F3 does not change world time.
3. Watch a fast enemy's locked target and time-to-impact. Confirm it may
   strike before another player action when its speed allows.
4. Kill an enemy, step onto its drop, buy a chest, cancel/reopen its offers,
   then choose one boon. Check clarity of proc messages during a cascade.
5. Open a door with a bump; discover both a cracked-wall and floor-sigil
   secret without a search key. Check fog and minimap secrecy.
6. Fight the Thorn Warden, observe both attack shapes and phase two, take or
   decline its free chest, and decide whether the 1,500-unit grace feels fair.
7. Allow the pursuer to arrive; test whether the exit remains discoverable
   and usable. Continue through a route to the next prototype stage.
8. Save & Quit during an active run, fully close LÖVE, reopen and Continue.
   Compare HP, time, map memory, offers, boons, pressure/deadline and feel.
   At a third-stage exit, verify the endpoint says final boss pending rather
   than victory.

Three potential game-feel frictions need human feedback: the seed-3 guide
takes hundreds of committed actions to reach the elite; fast telegraphs can
resolve before another player choice; and densely chained boon messages may
be hard to read. No balance value was changed from measurements alone.

During repeated GUI verification, one exploration harness run reached its
second assertion phase with the player short of the secret tile. The harness
had assumed a fixed presentation update would always clear input gating.
It now reads the playback gate, waits until that gate is clear, and asserts
each intended route action committed. This is a callback-harness timing fix,
not a change to scheduler or gameplay. The earlier LÖVE segmentation fault
has no identified cause; clean repetitions cannot establish it was fixed.

A later basic GUI run reset to seed 1 between capture phases despite that
script containing no reset input; the source of that external key event was
not captured. The basic callback harness now accepts only its own scripted
keys, preserving all action/time assertions. This also exposed a real F3
toggle defect: `diagnostics and nil or new()` always returned `new()` in Lua,
so F3 could turn diagnostics on but never off. The toggle now branches
explicitly, and the GUI harness checks both zero authoritative-state change
and that the overlay is off after the second press. These observations do
not explain the older segfault.
