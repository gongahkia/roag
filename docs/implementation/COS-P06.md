# COS-P06 — field schools, preserved records, and personal learning

## Status and checkout

**Status: implementation and automated headless/mock verification completed.**
The remaining evidence gap is a real LÖVE-window playtest; it was not run.

The working HEAD changed from `fb3804b` to `df5b2eb` through owner-side commits
while this pass was in progress. I did not commit, push, reset, clean, install a
dependency, access a player save or alter unrelated assets. The uncommitted work
is this handoff/contract update and the final education-soak verification
refinement. Recheck `git status` before a follow-on change.

## Prerequisites and implementation

The fresh registered suite covers P05-A/B actual ecology/witness geometry, P05-C/D/M
actor-owned survey/study, P05-F/I/O transport and persistence, P05-G/N/P
capability/history presentation, and P05-Q's controlled no-grant trace. The fresh
P05 soak used seed 9051, effect ticks 20 and 40, study completion at tick 510,
Moon I founding at tick 1650, two facts, eight checkpoints and 3,196,934 encoded
bytes. The P04 regression soak passed at tick 10,000 with ten receipts, eight
checkpoints and 3,157,886 bytes.

`education=1` is feature-gated by `knowledge=1`; it is selected only for newly
created frontier campaigns. `src.education` owns validated version-1 campaign,
world, school and personal state. Initial `fieldworkXP` and `teachingXP` are
independent deterministic 0..149 values from the specified per-person RNG
namespaces. The seed-12345 vectors are 43/98, 63/126 and 83/101 for people 1..3.

`field_school` is an ordinary built structure whose exact recipe is four stone and
two metal. Each installation owns a monotonic school ID, policy revision, at most
16 records, one local recording draft and one session. `school_policy` is bound to
site, structure slot, school ID and expected revision; stale/rebuilt policies
reject before changing tasks, records or materials. The three modes are real
Field-duty work: 120-action recording, atomic attended live teaching, and local
record study. Tuition is personal at 200 normalized units; recording work remains
at the installed school. Facts learned through communication use `taught`,
`record` or `mixed` provenance without acquiring another person's eyewitness
observations.

Sessions are offered before worker iteration and finalized after it, in session-ID
order. A lesson reserves a complete reachable pair or nobody. Reassignment or a
normal job release releases both matching session tasks. P05 analysis and P06 work
share a serialized once-per-person-per-tick guard. The P04 portable adapter carries
the personal education table and rejects malformed values; school/session/task
references remain local.

The current UI adds Field school to education-enabled build choices and a School
panel with enabled/mode/topic/priority controls, current local source details,
records and expertise. It uses the existing shared 13px Cozette font; no asset or
font-instance replacement was made.

One narrow P04 scheduling repair is included in the owner-side P06 commits:
logistics and school pose selection do not claim a tile already claimed by another
work task. This was found by the education trace when expedition assembly was
blocked by a competing pose claim. The P04 16-group regression and its 10,000-tick
soak pass after the repair.

## Acceptance evidence

| Cases | Evidence | Outcome |
| --- | --- | --- |
| P06-A | construction/resource accounting | PASS |
| P06-B | local source and Field-duty policy | PASS |
| P06-C | 120-action immutable record | PASS |
| P06-D | paired attendance/interruption release | PASS |
| P06-E | pre-XP arithmetic and ten-tick schedule | PASS |
| P06-F | record study, source loss and retained tuition | PASS |
| P06-G | communicated method/no copied observations | PASS |
| P06-H | portable-state clone test plus actual P04 flight of education XP/facts and unfinished tuition | PASS |
| P06-I | save/load/replay/seek/branch partial work | PASS |
| P06-J | unviewed-state simulation | PASS |
| P06-K | no-pupil atomic reservation | PASS |
| P06-L | stale policy revision | PASS |
| P06-M | learner-owned tuition/local draft | PASS |
| P06-N | repeated-finalizer same-tick guard | PASS |
| P06-O | malformed expertise/duplicate-record bounds | PASS |
| P06-P | `tests/education_gui.lua` mocked policy UI | PASS, mock-only |
| P06-Q | physical record then teaching, integrated scenario | PASS |
| P06-R | exact initialization vectors/feature-off boundary | PASS |
| P06-S | repeated policy churn/allocation bound | PASS |

`tests/education.lua` reports 19 groups / 53 assertions. P06-H creates a real
record, earns partial record-study tuition, releases the local school task, then
launches that learner through the actual P04 path and checks the transit passenger
record. Expertise/facts are also observed in the long integrated flight.

## Numerical integrated trace

`luajit tools/education_soak.lua 9051 20000` passed using an explicit controlled
128x80 home fixture, three 192x112 campaign maps and pre-history real material
supplies. It is not an untouched generated landing. A witnessed real glass-reed
`steam -> water` effect occurs at ticks 20 and 40. A (person 1) surveys and
completes actual study at tick 580. Real haulers install school 1 using its
stone/metal recipe; A records identification and operation. B (person 2) receives
the operational fact by live teaching at tick 4370 and remains home when A flies.

The P04 departure/arrival are ticks 7022/7421. A retains the operation on Moon I;
B retains its fact at home. The two physical home records remain in school 1.
After C (person 3) is released from the actual rally that kept it out of Field
selection, C studies those surviving records without a teacher and acquires the
operation at tick 9670 with method `record`. The trace compares an 80-tick partial
record-study save/reload continuation against its uninterrupted canonical state,
then explicitly replay-verifies the full command log.

At tick 20,000 it retained two school records and eight in-memory checkpoints. The
four recorded checkpoint texts were 3,199,374 / 3,201,343 / 3,203,114 /
3,205,267 bytes; final encoded history was 3,206,041 bytes, below the 32 MiB
envelope. Lua heap at final sampling was 114,374 KiB. This is one diagnostic sample,
not a stable CPU/memory overhead estimate.

## Commands and limitations

```text
luajit tests/syntax.lua                         PASS, 91 Lua files
luajit tests/run.lua                            PASS, 150 groups; 121,079 assertions
luajit tests/benchmark_smoke.lua                PASS, benchmark adapter only
luajit tools/headless.lua 12345 frontier 2000   PASS, 2.1436 CPU seconds
luajit tools/expansion_soak.lua                 PASS, six legacy accounting/replay traces
luajit tests/maximum_size.lua                   PASS, 11 maximum-dimension smokes
luajit tools/travel_soak.lua 73421 10000        PASS, P04 production trace
luajit tools/knowledge_soak.lua 9051 10000      PASS, P05 controlled trace
luajit tools/education_soak.lua 9051 20000      PASS, trace above
luajit tests/region_gui.lua <fresh /tmp>        PASS, mocked
luajit tests/logistics_gui.lua <fresh /tmp>     PASS, mocked
luajit tests/travel_gui.lua <fresh /tmp>        PASS, mocked
luajit tests/knowledge_gui.lua <fresh /tmp>     PASS, mocked
luajit tests/education_gui.lua <fresh /tmp>     PASS, mocked
git diff --check                                PASS
git diff --cached --check                       PASS
```

No real LÖVE window, SDL input, GPU rendering, click-target alignment, or
human-play save workflow was run. With confirmed disposable LÖVE storage and a
usable display, start a New frontier campaign, build a Field school with four
stone/two metal, select it, set Record, then Teach or Study record, and verify
that a travel departure releases school work while personal facts/XP survive. Do
not point that check at a real player save.

P06 stops here. It does not authorize P07, schools as remote libraries, generated
cultures/languages, new ecology/physics, social simulation or long-range travel.
