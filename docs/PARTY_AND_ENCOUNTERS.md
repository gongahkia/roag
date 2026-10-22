# Party identity and encounter design

Approved 2026-10-23 from the user's attached three-point recommendation and follow-up: implement companion specialisations, composed enemy groups and encounter-linked rewards, then research Slay the Spire, Caves of Qud and related games and thoroughly extend the encounter system. Make atomic local commits. The complete scope remains active until implementation, authoring, persistence, mouse presentation and end-to-end evidence cover it.

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


Initial implementation now includes six generated traits, sixteen class choices, three composed groups, interruption-aware prepared actions and themed persistent rewards. Atomic commits separate progression definitions, encounter definitions/runtime, shared integration and mouse presentation. The remaining research extension follows [ENCOUNTER_RESEARCH.md](ENCOUNTER_RESEARCH.md); final acceptance remains open.
