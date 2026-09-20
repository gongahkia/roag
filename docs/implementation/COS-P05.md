# COS-P05 — personal observation, identification, and field study

## Status

Completed locally without a commit or push. The work started from
`7a36f499bbeeb9ea65c5fa2138a4f1c4569ebe8e`; all changes in this tranche remain
in the working tree. P04's reported implementation was preserved. No real LÖVE
window session was run.

## Implemented contract

`knowledge=1` is an explicit campaign feature. It creates
`campaign.knowledge={version=1,rulesVersion=1,registryVersion=1,nextHistoryId=1,history={}}`
and sets `world.frontier.knowledge=1`. Every knowledge-enabled person has
`worker.frontier.knowledge` with bounded raw observations, learned facts, personal
studies, and the last actual study-action tick. The P04 portable-person adapter
deep-copies and validates that extension through departure, transit, arrival, and
return. Earlier feature sets do not gain the marker, records, or altered survey
semantics.

The fact registry has version 1 identification facts for stable existing
`flora`, `fauna`, and `sites` catalog keys, plus exactly these operational facts:

```text
operational/flora/filter/steam-to-water/v1
operational/flora/thorn/sand-to-rock/v1
```

At the start of local ecology, after earlier material/structure stages and before
ecological mutations, living local workers are scanned in person-ID order against
stable category/entity-ID encounter order. The scan uses the existing clear-line
geometry and an eight-cell Manhattan range. The actual glass-reed and iron-thorn
success branches synchronously determine witnesses before writing the target cell;
only a successful `steam -> water` or `sand -> rock` mutation credits a bounded
typed sample. The hook consumes no RNG and makes no physical/accounting change.

Feature-on survey grants the acting person only. `field.kind='study'` uses the
existing field duty, pathing, task release, needs and named-owner machinery. It
derives its topic from the target rather than accepting a fact ID. The same worker
must have the relevant identification and two distinct-tick effect observations of
the exact specimen. Each actual eligible action contributes one unit, no more than
once per person/tick; completion at 120 creates the fact. Progress is person-owned,
is retained after cancellation/interruption/destruction, and cannot be taken by a
second worker. A matching replacement specimen still needs new firsthand evidence.

The personal caps are 64 observations, 64 facts, 64 studies, and four retained
samples/provenance entries. The history ring is 128 acquisition entries. Raw
observations evict least-recently-seen records by stable source identity; learned
facts and self-contained study evidence are not evicted.

F4 now presents observer-specific personal notes for a knowledge campaign;
Left/Right cycles living local observers. The visible **Study** tool submits the
ordinary generic field order. Inspector labels stay neutral without that observer's
identification. Historical records are informational and never satisfy study
eligibility. All touched UI uses the shared Cozette instances already provided by
`src/render.lua`.

## Acceptance coverage

`tests/knowledge.lua` registers P05-A through P05-O and P05-Q; each group uses the
real ecology, commands, jobs, history, or travel adapter named in its title.
`tests/knowledge_gui.lua` is P05-P, a separate mocked LÖVE input/render/storage
test. Fresh results:

| Cases | Evidence |
| --- | --- |
| P05-A/B | near/far/dead witness checks; actual glass-reed and iron-thorn mutation hooks |
| P05-C/D | actor-only survey, two distinct receipts, 120 real fieldwork actions |
| P05-E/M | cancellation, handover, death, destroyed source, and rebinding boundaries |
| P05-F | physical loading, assembly, production-duration flight, fresh local ID, retained expert fact |
| P05-G/N | neutral labels, bounded acquisition history, dead expert excluded from live expertise |
| P05-H/L | deterministic observation eviction and person/source identity isolation |
| P05-I/O | save/load/replay/branch, strict personal schema validation, feature-off isolation |
| P05-J/K | material/ledger invariance and event-time witness geometry |
| P05-P | mocked F4 observer switch, shared-font field notes, Study control, modal isolation |
| P05-Q | no-grant evidence → survey → study trace with encoded validated state |

## Commands run

```text
luajit tests/syntax.lua
luajit tests/run.lua
luajit tests/knowledge_gui.lua <fresh disposable directory>
luajit tools/knowledge_soak.lua 9051 10000
git diff --check
```

The P05-focused suite passed 16 headless groups / 52 assertions. The mocked GUI
case passed separately. The 10,000-tick trace passed with seed 9051: effects at
ticks 20 and 40, study completion at tick 510, Moon I founding at tick 1650, eight
retained checkpoints, two retained personal facts, and a 3,196,934-byte campaign
history. It creates controlled test ecology before its history starts, then verifies
the full recorded survey/study/transport continuation; this is a fixture, not an
ordinary generated landing claim.

## Boundaries and remaining verification

No school, teaching, transferable record, XP, global research unlock, language,
culture, new reaction, planetary physics, or P06 work was added. The GUI checks
use the repository's mock graphics/input/storage adapter. They do not verify an
actual desktop LÖVE window, SDL input, GPU rendering, Cozette bitmap layout at all
window sizes, or a player-save workflow. CPU timing and heap measurements remain
separate diagnostic work; this tranche does not claim a stable overhead percentage.
