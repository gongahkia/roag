# COS-G06 — Combat, Security, Hostile Expeditions & Internal Insurgency

G06 introduces the `security=1` frontier feature.  It is deliberately opt-in:
older campaign histories retain their seven labour duties, G05 faction state,
and no security records.

Implemented state is site-local posture (`normal`, `alert`, `lockdown`), up to
eight posts, an optional refuge, bounded security events, portable Guard
designation/combat XP/ammunition/grievance state, and a campaign-level bounded
raid allocator.  Frontier Carbine, Shock Baton, Protective Vest and Ammunition
are fabricator recipes; Training Targets and Barricades are normal structures.
Barricades are walkable and only their lower two fine cells stop a ranged ray.

Combat has no hit RNG.  A carbine consumes one pouch round and uses an integer
ray: solid terrain, solid structures, barricade cover, then the first body.
Existing friendly bodies prevent intentional shots.  The implementation keeps
equipment as a unique existing equipment item; it can be loose, carried,
equipped in a distinct tool/weapon/armor slot, industrial cargo, or shuttle
cargo.  Ammunition is a physical stack until explicitly moved into a twelve
round personal pouch.

Hostile G05 factions evaluate at 1,000-tick boundaries, pay food and metal,
and create an 800-tick approaching expedition.  A known faction with a powered
relay gets a 300-tick warning.  Raiders materialize atomically from valid body
poses, retain raid-owned equipment until death, and create a one-time loot
receipt when their equipment becomes loose player-recoverable property.

Grievance is bounded to eight visible typed causes.  The current implementation
records stable-period decay and combat injury, supports bounded nonviolent
protests, then a deterministic organizing cell and infrastructure sabotage for
severe stressed dissidents.  Sabotage disables the exact structure lifetime for
600 ticks without deleting its buffers; insurgents retain their original
person IDs and security/personality state.

Focused coverage is in `tests/g06.lua`; the long-running deterministic state
smoke is `tools/g06_soak.lua <seed> <ticks>`.
