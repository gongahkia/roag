# Boss proposals for migration review

Status: all four concepts approved by the user (option A). The active migration goal authorizes implementation. These are additional encounter designs, not replacement permission for any existing Roag/Vagabonds boss content.

Common constraints: actual world tiles; clear intent and locked attack footprints; visible impact timing when the controlled member can see the threat; fixed world-role difficulty, with a stronger authored version if selected as final boss. Encounters must remain solvable by a solo character as well as a party. Numerical tuning is delegated; the mechanics below are approved at concept level.

## 1. The Bellkeeper

A burial-site spirit that hunts noise and uses resonating objects to control space. Loud actions can lure its next attack away from an ally, but every committed attack retains its marked tiles.

- **Soundseek:** marks the location of a recent loud action, then releases a delayed strike there. Moving after the mark avoids it.
- **Funeral Chime:** visible resonators emit scheduled expanding attack rings; stepping between impact bands or breaking a resonator creates an opening.
- **Broken Chorus:** destroying its resonators interrupts that chime sequence and leaves the spirit exposed during an extended recovery.

Counterplay: noise baiting, timed movement and choosing between attacking the spirit or disabling its resonators.

## 2. Sluice-Maw

A cistern-dwelling slime that turns existing water channels into weapons. Valves provide another way to fight it; effects use explicit local channels and tile states, not a new general fluid simulator.

- **Channel Surge:** marks a channel before a wave damages and pushes occupants along it.
- **Reservoir Feast:** announces a move into a connected pool to recover health; draining that pool prevents the recovery.
- **Dry Exposure:** operating valves drains selected channels, opening routes and exposing the creature to damage.

Counterplay: leave marked lanes, use terrain against the push, and time valve operations around its recovery attempt.

## 3. The Threadmother

A territorial spider that partitions a chamber with destructible webs, making separation and rescue dangerous. Its webs remain physical obstacles with readable escape routes, rather than forcing unavoidable party isolation.

- **Partition Web:** telegraphs strands between anchor points, then creates cuttable barriers.
- **Reel In:** launches a line at a marked tile and pulls an occupant toward the nest if it connects; movement or breaking the line counters it.
- **Burning Network:** fire spreads through connected strands, clearing paths but also threatening nearby allies with visible scheduled impacts.

Counterplay: cut anchors, evade the locked pull, and exploit fire while accounting for friendly fire and blocked movement.

## 4. The Ossuary Captain

An undead defender that protects its front and reshapes nearby lanes with bone barricades. Its attacks expose flanks or recovery windows, so a solo character can exploit the same positioning weaknesses as a coordinated party.

- **Shield Advance:** announces a forward lane, then charges and knocks back occupants; walls constrain the route.
- **Bone Rampart:** marks and raises breakable barricades to channel movement without permanently sealing all exits.
- **Committed Guard:** strongly protects its front and front diagonals; turning, charging and recovery create opportunities to attack exposed sides/rear.

Counterplay: bait charges, break a barricade when needed, and exploit facing and recovery instead of attacking its shield repeatedly.

## Atlas evidence and limits

The generic catalog was queried through `tools/query_assets.py`, rather than inferring identities from old game bindings. Candidate base sprites:

| Concept | Catalog candidate | Evidence |
| --- | --- | --- |
| Bellkeeper | `urizen-onebit-r42-c110` | Publisher-labelled ghost, high-confidence subject. |
| Sluice-Maw | `urizen-onebit-r42-c108` | Publisher-labelled slime, high-confidence subject. |
| Threadmother | `urizen-onebit-r15-c002` | Publisher-labelled spider, high-confidence subject. |
| Ossuary Captain | `urizen-onebit-r10-c104` or a suitable held-prop variant from that atlas series | Skeletal humanoid is a visual interpretation; exact species/profession and animation are not publisher-confirmed. |

These are metadata-grounded art candidates, not a claim of completed visual review or final binding selection. Inspect exact crops/variants through the universal atlas before final selection. Preserve neutral catalog metadata; boss identity and mechanics belong in game content. Use Vagabonds sprites exclusively, with Roag-style overlays and feedback. No new sprites, gameplay definitions or runtime mechanics have been implemented by this proposal document.
