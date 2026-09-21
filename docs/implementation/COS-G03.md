# COS-G03 — minds, memories, relationships, and personal work style

## Scope

G03 is a versioned `psychology=1` feature for new frontier campaigns. It
requires the existing knowledge and safe-excavation features. Campaigns recorded
without it retain their original stress, labour, body, visibility, and save
semantics; there is no silent migration.

Each new settler receives deterministic personal state keyed only by campaign
seed and persistent person ID:

- stable courage, composure, empathy, sociability, diligence, and independence;
- mutable values for exploration, safety, cooperation, knowledge, industry, and
  preservation;
- preferences for the existing duties, a bounded background, and one personal
  ambition;
- up to 32 memories, including at most 8 core memories; and
- up to 64 directional relationships.

The state follows the existing portable person record through P04 travel and is
retained with dead historical people. It contains no local task, path, or worker
ID reference.

## Authority and reluctance

Player commands remain authoritative. G03 adds no generic order refusal and does
not cancel, retarget, or make a colonist deliberately violate G02 excavation
safety. A person who dislikes a duty or is under strain can work at a reduced,
deterministic rate. At the strongest reluctance they can lose an ordinary work
beat while correcting a small mistake. This only delays job progress: it cannot
consume extra resources, duplicate products, change a designated target, or
create an unsafe fall.

Personal preference resolves equal legal AUTO candidates after the existing
policy, priority, named order, need, hazard, rally, school, and safety rules.
It cannot override an OFF duty or a higher existing job priority.

## Experiences and social state

`src.psychology` owns compact typed memories, relationship changes, stress
reaction, slow value adaptation, ambition completion, social opportunities, and
plain-language summaries. G02 danger hooks feed it at the existing physical
transition; P05 operational study calls the discovery hook; P06 teaching and
record completion use their real finalized work transitions; P04 arrival calls
the landing/return hook. Dead witnesses are resolved locally with the existing
visibility predicate. The campaign runs bounded local social resolution after
site worker updates and before travel, followed by one campaign-wide recovery,
recall, and ambition maintenance pass.

Stress still panics at 80 and clears panic at 50. Psychology-enabled people
recover every 100 ticks by a composure-based amount. Memories may be recalled
every 300 ticks. This never creates resources or enters a physical accounting
ledger.

Initial crew relationships are directional and intentionally span friendship,
guarded acquaintance, and rivalry. They derive from compatibility and a stable
seeded relationship namespace, while `abandoned_together` is the only shared
starting memory. G03 does not fabricate hidden life histories.

## Player presentation

The live Crew panel has a third **Mind and relationships** page. It presents
mental state, current work explanation, personal style, recent/core memories,
ambition, and relationship tone in ordinary language. Numeric facets, values,
stress calculations, and relationship scores remain internal simulation data.
The panel resolves its worker by stable local ID every draw and remains live;
opening it does not pause the colony.

## Verification

`tests/g03.lua` covers deterministic generation, feature-off compatibility,
differing reactions, memory bounds and dedupe, directional social outcomes,
absolute authority with slower reluctant work, stress/recall/panic continuity,
ambition completion, strict malformed-state rejection, and clone/history
preservation. `tests/g03_gui.lua` is explicitly **MOCK UI** evidence for the
live plain-language Crew page. `tools/g03_soak.lua` invokes the real G02
excavation, P05/P06, equipment cargo, and P04 journey route with
`psychology=1` for a 20,000-tick feature-on integration trace.

Native LÖVE and human gameplay remain separate from headless and mock results.

### Final checked source

The final checked source was owner revision `bb97d93` plus the uncommitted G03
validation/line-of-sight and documentation corrections in this worktree. The
owner-managed commit was preserved; this pass did not commit or push.

| Evidence | Result |
| --- | --- |
| `luajit tests/syntax.lua` | PASS — 107 Lua files |
| `luajit tests/run.lua` | PASS — 176 groups / 121,237 assertions |
| all ten `*_gui.lua` adapters in separate `/tmp` roots | PASS — **MOCK UI** only |
| `luajit tests/maximum_size.lua` | PASS — every 512×256 map smoke case |
| `luajit tools/g03_soak.lua 9703 20000` | PASS — **HEADLESS** fresh feature-on route |
| `git diff --check` and staged diff check | PASS |

The controlled 128×80 G03 initial campaign encoded to 3,309,409 bytes with
three people, three origin memories, six directional starting ties, six physical
starter items, zero ropes, zero retained checkpoints, and 11 enabled feature
keys. The completed tick-20,000 integration trace encoded to 3,357,294 bytes
with eight retained checkpoints. It completed the real rope/tool-bench/charge
route, P05 observation/study, P06 teaching, physical tool cargo, and P04 arrival
under `psychology=1`. The arrival trace ended with stress 100 for its controlled
traveller; that is a real result of the scenario, not a claim of balanced human
play.

## Deferred work

G03 does not add factions, cultures, romance, children, governance, coercion,
generic order refusal, combat psychology, procedural dialogue, addiction,
recreation, or full personality plasticity. The next planned content direction
after human playtest feedback is industry and automation; factions and cultures
come after that.
