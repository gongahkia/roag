# Cosmonauts — decision register

This register distinguishes settled owner direction, the implementation defaults selected for the initial prompt pack, and work that remains deliberately undesigned. It is not itself an IMPLEMENT request.

## A. Owner-settled direction

### Premise and progression

The starting crew are convict-origin independent prospectors and frontier settlers. They were abandoned and are effectively independent, not presently working under a penal extraction contract or automatically being chased as fugitives. Their exact pasts and the future role of the original society remain unspecified.

They already have limited travel around the starting planet and its moons. Relics/artifacts located deep within worlds enable travel to more distant worlds. This is a capability/progression milestone, not a defined ending. The exact relic taxonomy, assembly count, activation procedure, or a distinction between interstellar and intergalactic drive tiers has not been fixed.

The core loop combines settlement production, automation, surveying, investigation, learning, teaching, and frontier expansion. Players define goals; remaining on a home world should support meaningful play. Ever-growing projects, resource requirements, and voluntarily acquired obligations can drive industry without imposing a compulsory penal quota.

### World and tone

The game is a pixelated, two-dimensional side-view colony simulator with fine-grid material simulation. Caves of Qud is the strongest procedural/worldbuilding reference; Rain World is a reference for autonomous ecology and a hostile-feeling environment. Dune and Star Trek remain lore inspirations, not a mandate to copy their named factions or canon. Deep Rock Galactic is the identified space-miner reference, not a request for its first-person combat format.

The user wants generated worlds, cultures, histories, factions, and interacting systems that continue independently of the camera. Mystery includes ancient civilizations, mechanisms, spell-like phenomena, and incomplete explanations. Procedural variation should operate through actual reusable behaviors and relationships. The game should support surprises that can make sense in hindsight rather than disconnected punishment rolls.

A finite map is now a prototype boundary only. The long-term direction includes other worlds, different environmental/physical conditions, and potentially wider procedural exploration. The present local map must not be mistaken for the whole eventual universe.

### Society and control

One player society may operate several persistent and directly manageable settlements. Founding a lunar outpost does not automatically convert it into an NPC diplomatic partner. Management can switch among locations, while individual colonists retain substantial autonomy.

The intended personal depth includes backgrounds, relationships, beliefs, preferences, ambitions, fears, disagreements, learning, and event-driven changes. Internal conflict/insurgency is within the long-term direction, but independence is not automatically triggered by geographical separation.

### Knowledge and danger

Individuals' understanding should matter. Unknown objects and writing may initially be described incompletely or remain unreadable; discovery, instruction, records, cultural understanding, and potentially lore-consistent unusual experiences can change what people comprehend. Both mechanical and historical knowledge matter. The UI must not accidentally substitute omniscient hidden labels for this system.

The environment/ecology is a major danger source. Other creatures, societies, rival miners, armed conflict, and internal unrest are also intended. Combat is not the central game identity. Deaths, irreversible errors, inaccessible resources, and failed settlements need not be recoverable, but failures must not be excuses for state corruption or unaccounted resource loss.

### Collaboration and technical constraint

The remote assistant researches and decides the high-level design with the owner, then supplies detailed Markdown implementation prompts. The local Codex instance runs Terra xhigh 5.6 and implements/tests against the actual checkout. Core game algorithms remain from-scratch Lua/LÖVE rather than adopting an off-the-shelf simulation stack.

## B. Engineering choices selected for the first implementation queue

These are proposed implementation defaults supplied in the numbered prompts, not statements that the owner previously specified their exact numbers.

| Decision | Selected approach | Reason |
|---|---|---|
| Preserve the local game | Add a campaign root and companion history, with a thin app/session adapter | Avoid a wholesale rewrite and retain tested local rules |
| Time/history | One fixed campaign tick and command order; checkpoints/branches contain the entire campaign | Avoid contradictory timelines and cross-site duplication |
| Local simulation | Three pre-generated fully simulated landing-region maps | A small honest performance/replay boundary before distant approximation |
| People | Persistent global `personId` plus site-local worker IDs; one actual body in one custody domain | Preserve local job APIs without losing identity during travel |
| Inventories | Physical piles/carry/escrow/craft cargo with one owner each | Prevent instantaneous global resource pools |
| Transport | One modest shuttle; real preparation; finite parts/food; deterministic timed routes | A playable logistics loop without premature orbital/ship-physics work |
| New state | Independently tagged campaign save; explicitly versioned features | Preserve old local saves and old histories rather than silently reinterpret them |
| Procedural seeds | Stable per-domain namespaces and a small pure-Lua campaign PRNG | Keep cosmetic and unrelated draws from perturbing terrain |
| Knowledge | Personal observations and stable fact IDs with provenance; no universal unlock | Make researchers' presence and communication consequential |
| Education | One field school with recording/teaching/self-study modes; two functional XP tracks | Implement an institution before generating many decorative professions |
| Initial content | Existing terrain/ecology and two real observed material effects | Use already executable rules to validate the new knowledge mechanics |

The numbers (three sites, three seats, 24 cargo slots, route durations, costs, study units, checkpoint count, evidence limits) are versioned prototype tuning. They can be revised deliberately after tests/play. Do not quietly change a number that affects an existing replay; update the relevant rule/version contract.

## C. Work deliberately deferred

Long-range relic activation and artifact assembly require their own concrete gameplay contract. These prompts do not introduce a fake long-range button or a completion screen.

Additional local regions, procedural expansion, streamed worlds, or approximate off-screen simulation require explicit activation/catch-up rules and measured performance. An abstraction need not reproduce the exact fine-grid trajectory; it must not be falsely advertised as doing so. Camera movement must remain causally irrelevant.

Different gravities, atmospheres, pressure, temperature, equipment, and structural support require real physical/navigation/material integration. Metadata alone is not implementation.

Rich generative ecology, culture, faction economies, diplomacy, conflict escalation, psychological memories, family/children, generational timescales, and insurgency should be built as connected executable systems, not seeded labels or a global random disaster table.

Generated scripts, partial translation, historical interpretation, false beliefs, boons/visions, scientific experimentation, artifacts/specimens, remote communication, and physical books have not all been reduced to algorithms here. P05–P06 implement a narrower personal-evidence and local-institution core that can support later tranches.

Automation chains and larger industrial requirements remain a central next gameplay pillar, not removed from the vision merely because the first six prompts prioritize reliable spatial and knowledge continuity.

## D. Suggested next design/review boundary after P06

Review actual play and performance, then select one connected loop rather than all remaining systems: for example a material-processing chain whose ecological dependencies are discoverable and teachable, followed by events/memory that affect willingness to perform its hazardous work. That selection is a future design discussion, not permission for the local agent to add those mechanics now.
