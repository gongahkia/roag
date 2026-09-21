# COS-G01 — physical scale, live management, fog, and torchlight

## Status

Implemented and verified with headless and mocked-UI checks. Native LÖVE rendering
and human gameplay remain **NOT RUN** in this Linux checkout because `love` is not
installed here. The isolated launcher remains the supported route for that work.

The starting revision observed for this pass was `f9f12c4`. Owner-managed commits
advanced it to `0098b5d` while work was in progress; no reset, clean, commit, or
push was made here. The current G01 changes remain unstaged/uncommitted for the
owner. The latest owner commit introduced the initial shared body helper; G01
retained its public worker-object calls while extending it for all geometry seams.

## Rules and compatibility

New frontier campaigns explicitly contain `features.body=1` and
`features.visibility=1`. Each campaign world records
`frontier.body=1`, `frontier.visibility=1`, and `body=1`. `visibility=1` is
invalid without `body=1`. Unknown versions and mismatched markers reject during
validation. Existing campaigns with neither marker still use the historical
`settler-2x3-v0` profile and old P05 sight semantics; there is no migration or
retroactive fog map.

`settler-2x4-v1` is two fine cells wide and four fine cells high, with the worker
anchor remaining the left foot. `src.body` is the one profile API for occupied
cells, eyes, hand origin, rectangle and hit testing. Navigation, support, hazards,
jobs, construction occupancy, blasts, starter/landing clearance, assembly,
school poses, rendering, selection and P04 arrival all call through it.

Ordinary Crew, Field Notes, Region, expedition, School, inspector, help and block
selection interfaces are live. Opening/closing them does not alter `app.paused`.
Space remains the explicit clock control except where an existing focused input
uses it. New-run, map-browser and historical workflows retain their established
special behavior.

Visibility-enabled worlds use these frozen values:

| Rule | Value |
| --- | ---: |
| Settler maximum sight | 20 fine cells |
| Dark vision | 3 fine cells |
| Torch light | 16 fine cells |
| Docked shuttle light | 20 fine cells |
| Torch cost | 1 metal |
| Installed-torch cap | 128 per site |

`src.visibility` implements deterministic recursive octant shadowcasting over
the current solid/wall opacity rule. Opaque boundary cells are visible, but shadow
cells beyond them are not. Air, water, steam and lava remain transparent under the
existing material model. Sources merge by maximum integer intensity. A travelling
craft is not a light source; a torch on an empty site still emits physical light,
but no living local observer means no current player visibility.

Visibility derives after structure/material updates and before ecology in
`Sim.body`. It updates compact per-row remembered material bytes and bounded
last-seen structure markers. Current masks and light masks are disposable caches.
Unseen cells render black; remembered terrain renders from its snapshot, so an
off-screen water/material change is not revealed until observed again. Templates
exclude fog memory and built lights; campaign save/checkpoint/history state keeps
it. New spatial campaign commands resolve against the current local visible mask
and reject hidden targets with `Target is not currently visible`.

Torch is a nonblocking ordinary one-block structure. Its one metal passes through
normal fetch, reservation, construction, cancellation/removal and destruction
rules. The docked shuttle gives the starting landing its first illumination;
launching it removes that source at home. P05 witness, survey and study checks use
the same per-person lit visibility predicate in visibility-enabled campaigns. A
camera position grants nothing.

## Tests and trace

| Area | Executable evidence | Result |
| --- | --- | --- |
| Body/legacy profile | `tests/g01.lua`, G01-A/C | PASS |
| FOV, source radii, memory, save validation | `tests/g01.lua`, G01-F/G/H | PASS |
| Fog command gate and one-metal construction | `tests/g01.lua`, G01-J/L | PASS |
| Dark/lit P05 effects | `tests/g01.lua`, G01-K | PASS |
| Live panels, hitboxes, explicit pause, hidden HUD | `tests/g01_gui.lua`, G01-P | PASS — MOCK UI |
| Existing core/legacy behavior | `tests/run.lua` | PASS — 155 groups / 121,121 assertions on the final source. |

| G01 acceptance | Source/test evidence | Outcome |
| --- | --- | --- |
| A — body profile | `tests/g01.lua` G01-A | PASS |
| B — body integrations | body API in nav/jobs/structures/blasts/expedition/render; P03/P04/P06 suites | PASS |
| C — replay compatibility | campaign/world marker validation; G01-C; legacy P01–P06 suites | PASS |
| D — live panels | `tests/g01_gui.lua` Crew/F4/Help clock checks | PASS — MOCK UI |
| E — stale UI | ID-bound school/expedition contracts in P04/P06 plus G01 panel redraw path | PASS — automated contract |
| F — FOV geometry | G01-F wall/boundary check and deterministic octant helper | PASS |
| G — illumination | G01-F/G radius, light-source and no-observer logic | PASS |
| H — memory truth | G01-H mutable off-screen/re-observation/save clone check | PASS |
| I — no leaks | G01-P unseen action-HUD suppression and remembered inspector path | PASS — MOCK UI |
| J — command gate | G01-J campaign command rejection | PASS |
| K — lit witnessing | G01-K plus G01 soak dark/lit receipts | PASS |
| L — torch physicality | G01-L real job/resource check | PASS |
| M — shuttle/world travel | G01 soak P04 launch/arrival and per-site fog state | PASS |
| N — unviewed simulation | established regional P02/P04 tests with per-site visibility update | PASS |
| O — history/practice | campaign clone/replay checks and G01 soak `verifyReplay` | PASS |
| P — presentation/input | G01-P and retained V01 GUI adapters | PASS — MOCK UI |
| Q — storage/performance | maximum-map 11,302,276-byte smoke; derived masks excluded | PASS — size only |
| R — integrated trace | `tools/g01_soak.lua 9501 10000` | PASS |

`luajit tools/g01_soak.lua 9501 10000` exercised a controlled 128×80
three-site modern frontier. It recorded a dark glass-reed event at tick 20 with no
personal effect evidence, then lit real events at ticks 40 and 60. A real torch
was built from metal, a field school was physically built, a 2×4 pair taught the
identity, and the expert travelled through P04. The final trace was:

```text
seed=9501 tick=10000 torch=1 school=1
expert person=1; pupil person=2
departure/arrival=5660/6060
home visible/explored=363/713
encoded history=3,288,653 bytes; checkpoints=8
expert retained operational/flora/filter/steam-to-water/v1
```

The maximum 512×256 home campaign smoke encoded 11,302,276 bytes after three
ticks, below the 32 MiB envelope. This is a size boundary sample, not an FPS or
hardware-performance claim.

## Deferred

No fuel/battery, handheld equipment slot, day/night, ambient lighting, darkness
fear/work penalties, combat/stealth, renderer rewrite, procedural culture,
industry, new physics, relic progression or remote teaching was added.
