# COS-G02 — safe excavation, ropes, stress, and physical tools

## Status

Implemented as a feature-gated current-frontier slice. Existing feature-off
P01–G01 histories retain their recorded 2×3/2×4, digging, falling and starter
equipment behavior. There is no save migration. Native LÖVE and human gameplay
remain **NOT RUN** in this checkout because the `love` runtime is unavailable.

The starting source examined for this pass was owner revision `0098b5d`. Owner
work advanced the shared branch through `6fa8952`, `ae6b872`, `9abbc74`, and
`3949134` while implementation and verification were under way. Those revisions
were preserved; no reset, clean, commit, or push was made by this pass. The final
uncommitted safety-path correction remains visible in `src/jobs.lua` until the
owner decides how to manage it.

## Contract and implementation seams

`features.equipment=1` and `features.safe_excavation=1` require G01 body and
visibility; the latter requires the former. `src.equipment` owns stable bounded
pickaxe/rope-coil IDs and validates exactly one physical custody state: loose,
carried, equipped, craft, or installed rope. `src.equipment_commands` makes all
fabrication, rope, tool-drop and tool-cargo commands site-bound and validated.

`src.jobs` performs the support check immediately before a terrain mutation. It
first searches a body-valid alternate pose, then changes the still-open dig task
to a physical rope-fetch/deploy substage when a reachable coil and lane exist;
otherwise it retains the designation with **Unsafe descent — rope required**.
The task is never cancelled merely because current geometry made it unsafe. The
same module gives a no-pick dig one work unit, a soft-material pick dig two, and a
rock/ore pick dig three. `src.nav` treats a local rope as a two-cell-wide
body-aligned climb lane; `src.colonists` applies G02's bounded fall/stress rules;
`src.travel` carries stress and equipped pick identity with persistent people.

Ropes are local world records paired with their coil item. They have a stable
local ID, 1–24 cell length, and a two-column lane. A real coil is fetched before
installation; intact removal returns that same coil, while a blast removes both
rope and its embodied coil. A one-block Tool bench uses normal construction,
real multi-resource escrow, one active fabrication job, and produces a unique
tool only after its 120/60 work threshold. `src.logistics` counts loose tools in
the ordinary 24-slot craft capacity; `src.equipment` preserves the actual IDs
through load, flight and unload.

Stress is 0–100 and enters panic at 80, clearing only at 50 in safety. Damage,
critical-breath transition, unsafe-task abort and established blast danger are
the deterministic sources; quiet workers recover one point every 20 ticks.
Panic releases ordinary work and seeks a current-lit safe pose. It cannot arm a
charge or bypass support safety; an otherwise trapped emergency can take at most
an eight-cell uncontrolled drop. The existing charge is reused as the only
demolition system, with the actual field-work/fuse/evacuation route.

The player-facing seams are `src/ui/action_hud.lua`, `src/ui/crew.lua`,
`src/render.lua`, and `main.lua`: a visible Delegate panel offers rope and Tool
bench actions; the bench offers two explicit recipes; crew shows steady/panicked
stress; and the expedition view shows actual tool IDs aboard and can queue a
physical load of a loose local tool. These remain normal live panels and retain
G01 fog, camera, batch-selection and Ctrl+Q behavior.

## Follow-up placement controls

Torches now use the same real one-metal construction job on either solid floor
or an empty block adjacent to a solid terrain face/completed wall. Their mount
is derived from live support rather than a new saved orientation field, so a
destroyed backing face disables the normal light source. The Delegate HUD offers
**Unfurl rope downward** and **Unfurl rope upward**. Both fetch one coil and
install the same canonical top-to-bottom, two-cell-wide climb lane; upward
placement scans from the chosen lower anchor toward an upper route. `L` and
`Shift+L` select those two drag tools in current frontiers. New ladders are
hidden from those controls, but existing ladders and old replay commands are
preserved for compatibility.

## Acceptance evidence

| Check | Executable evidence | Outcome |
| --- | --- | --- |
| G02-A/C — support mutation/no-coil block | `tests/g02.lua`, first group | PASS |
| G02-B/D — rope lifecycle and climb lane | `tests/g02.lua`, automatic-rope and rope groups; `tools/g02_soak.lua` | PASS — real coil, automatic attachment, 2-column lane and recovery |
| G02-E — pick work units | `tests/g02.lua`, pick group | PASS |
| G02-F/H/I — custody, cargo and accounting | `tests/g02.lua`, custody/cargo group; campaign validator | PASS |
| G02-G — fabrication/escrow/output | `tests/g02.lua`, fabrication group | PASS |
| G02-J/K — panic and fall bands | `tests/g02.lua`, panic/fall group | PASS |
| G02-L — needs/hazard precedence | existing jobs/fieldwork/travel core regressions | PASS — focused seam coverage |
| G02-M — established charge reuse | `tests/g02.lua`, charge group; soak charge arm/fuse | PASS |
| G02-N — fog and P05 boundary | G01 visibility/knowledge regressions and G02 soak | PASS |
| G02-O/P — save, branch and old history | `tests/g02.lua`, legacy group; history replay in soak | PASS |
| G02-Q — controls and live panels | `tests/g02_gui.lua` | PASS — MOCK UI |
| G02-R — integrated excavation/travel | `tools/g02_soak.lua 9602 15000` | PASS — HEADLESS |

Aggregate totals are supplementary to the named cases above.

## Integrated trace

`tools/g02_soak.lua 9602 15000` uses a labelled 128×80 controlled frontier. It
arranges a clear shaft, real initial metal and a specimen, but grants no rope,
tool, fact, school, record, tuition, cargo or travel completion. The executed
sequence builds a real torch and rope, equips pickaxe `#1`, mines a lit face,
constructs a Tool bench, fabricates a replacement rope coil, completes P05
survey/study, records and teaches at a 2×4 field school, physically loads a loose
coil, arms the established charge, then launches the expert/miner expedition.
The
equipped pick and operational fact are checked after P04 arrival. A save/reload
continuation and history replay are then compared at tick 15,000.

The trace's final output records rope ID/length, bench ID, arm status, pick ID,
craft-tool count, departure/arrival ticks, encoded bytes and checkpoint count.
It is a headless fixture route, not a native or human playthrough.

## Final verification

On the final uncommitted source state, `luajit tests/syntax.lua` compiled 103 Lua
files; `luajit tests/run.lua` passed **164 groups / 121,162 assertions**; and all
nine `*_gui.lua` adapters passed as **MOCK UI**. `luajit tests/maximum_size.lua`
passed each 512×256 terrain generator's exact map round-trip and two-tick
accounting boundary. The pre-existing G01 maximum visibility-enabled save result
was 11,302,276 bytes, below the unchanged 32 MiB envelope; the fresh 128×80 G02
frontier was 3,279,451 bytes at tick zero (six loose starter items and no ropes)
and 3,308,579 bytes with eight checkpoints at tick 15,000. These are
encoded-size and headless CPU checks, not native-frame or general performance
claims.

The fresh G02 trace reported:

```text
seed=9602 tick=15000 rope=1/5 bench=40 chargeArmed=true
pick=1 cargoTools=1 departure/arrival=11556/15000
bytes=3308579 checkpoints=8
expert=1 miner=2 pupil=2 fact=operational/flora/filter/steam-to-water/v1
```

`git diff --check` and `git diff --cached --check` passed. The remaining worktree
delta is limited to the automatic-rope safety correction, its focused regression,
the integrated trace/report, and this handoff; it contains no generated save,
playtest root, real player save, font bundle, commit, or push.

## Limits

G02 does not add durability, quality, repair, an inventory grid, powered drills,
weapons, a generic crafting economy, cave-in/structural simulation, broad
psychology, medical injury, combat, new planetary physics, or another explosive.
The player guide contains the finite native human checks; no mock result has been
labelled human gameplay.
