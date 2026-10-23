# Party identity and encounter design

Approved 2026-10-23 from the user's attached three-point recommendation and follow-up: implement companion specialisations, composed enemy groups and encounter-linked rewards, then research Slay the Spire, Caves of Qud and related games and thoroughly extend the encounter system. Make atomic local commits. The complete scope is implemented and verified; the requirement review below records its evidence and remaining playtest limits.

## Required outcomes

- Generated people gain persistent personal combat traits; each of the four classes offers meaningful authored development choices. Choices must alter real combat, be readable before commitment and reject invalid requests without costs or state changes.
- Encounters contain deliberately compatible enemy roles, including support with attackers, interruptible preparation and attrition. Plans remain visible and deterministic. Groups must appear in ordinary generated worlds, not only previews.
- Rewards reflect the encounter's identity and risk, use thematic equipment pools and include distinctive boss rewards. Ownership and one-time claiming survive retreat, saves and loading; independent loot randomness cannot reroll outcomes.
- A reproducible fresh-expedition audit measures recruitment, travel/discovery access, resources, fighting and recovery across seeds. Report structural and scripted evidence honestly; automated success is not proof of enjoyment.
- After the initial implementation, primary-source research on Slay the Spire, Caves of Qud and comparable games informs a further encounter-generation pass. Record source-backed findings, concrete adopted rules and deliberate differences appropriate to this open world.
- Preserve free access to dungeons and the final threat, fixed world dangers rather than party-scaled difficulty, generated companions, mouse controls, turn-based party combat, shared simulation rules, collision/privacy, separate RNG streams and safe incompatible-save rejection.
- All new content families have stable IDs, validation, references, safe editing/removal, schema, scaffolds, runtime previews and authoring documentation. Review and improve agent tools when repeated tasks reveal friction.
- Verify real Core outcomes and exact continuation, native mouse flows and minimum-size rendering. Update project state and verification evidence, review every requirement, and close the durable task only with fresh complete evidence.

## Sequence

1. Capture baseline expedition evidence and establish content/runtime contracts.
2. Implement personal combat identity and specialisation choices with presentation and tests.
3. Implement composed encounters and themed, persistent rewards in normal generation.
4. Research encounter design and extend population selection, composition, behavior and counterplay based on findings.
5. Dogfood authoring tools, audit generation and complete integrated automated/native/visual acceptance.

The baseline is `b9cd63f`. It passed 477 .NET tests and 147 native checks before these changes; those counts do not verify this milestone. New gameplay content will change the compatibility hash. Diagnostics use isolated slots and never rewrite player saves.

## Evidence and research

Implementation findings, source links, adopted design rules and measured results will be recorded as work progresses. The tracked task specification is `devtools/tasks/party-and-encounters.json`.


The initial three-group implementation was followed by the primary-source [research pass](ENCOUNTER_RESEARCH.md), then expanded contextual populations and saved behavior cadence. Local commits separate progression, groups/rewards, mouse presentation, population generation, cadence, validation, agent tools and invariant fixes.

## Requirement review

| Requirement | Implemented result | Evidence |
| --- | --- | --- |
| Pacing | Real fresh-world recruitment, discoveries, fights and recovery across four seeds; bounded-controller gaps remain explicit. | Preserved pre-change baseline and `audit-expedition`; [audit limits](EXPEDITION_PACING_AUDIT.md). |
| Companion identity | Six generated innate traits and sixteen choices, two alternatives at levels 2 and 4 for every class; actual combat effects and explicit permanent commitment. | Progression/content/no-cost/save tests,runtime previews for every new definition, `world-progression` mouse scenario. |
| Enemy groups | Nine compatible role compositions; one group per underground floor, context eligibility/weights, local no-repeat and connected lazy placement. | 72 authored contexts and 576 placed groups across seeds 42, 1337, -42 and 2026; forward/reverse visitation, IDs, terrain/RNG purity and saved activation tests. |
| Counterplay | Wounded-ally support, marked prey, limited mana, interruptible preparation and saved last-action non-repeat rules. | Real-resolver tests and paired seed 42/1337 audits: charge interruption, guard mitigation and healer-priority measurements. |
| Rewards | 22 thematic profiles, eight guardian signatures, themed caches, independent generated gear and one-time source grants. | Retreat/partial-group/reload/capacity/equipment tests and actual three-item guardian reward capture. |
| Research extension | Qud contextual populations, Spire readability, Crawl bands/habitat/meaningful decisions and Darkest Dungeon resource pressure informed explicit local adaptations. | Cited primary sources, documented limits and implemented context/roles/behavior/loot rules in [research notes](ENCOUNTER_RESEARCH.md). |
| Invariants | Free dungeon/final-threat access, fixed enemy strength and membership, generated party, mouse input, frozen combat clock, shared needs, collision/privacy and safe saves. | Full regressions; party-size/level membership and visible-only threat tests; exact native continuation. |
| Authoring and agent loop | Stable IDs, references, role/coverage/applicability validation, scaffolds/schema/previews, last-innate removal protection, three managed audits and explicit native scene impact guidance. | 53 runtime previews; temporary scaffold/validate/inspect/remove exercise; reference refusal, CLI tests and 17 workflow tests. |
| Acceptance | Fresh full managed run includes every requested profile and all eleven native scenes. | Run `20261023-013614-8663deaa` passed with 650 .NET tests, 17 workflow tests, 38 catalog tests and 187 native checks. Final captures were inspected; see [verification](VERIFICATION.md). |

The first final run was intentionally cancelled after review exposed inherited party-size scaling in ambient encounters. A shared pure opponent query now fixes that rule and feeds visible hover estimates. Two old single-target feedback fixtures now isolate their foe explicitly. A later authoring review also protected the last innate definition from deletion. These fixes have focused regression evidence and are included in the fresh final run.
