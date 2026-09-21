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

Equip and reload commands are intentions, not remote inventory edits. The
chosen local person must reach the exact loose item or ammunition pile through
the normal task planner; interruption leaves its original custody intact.
Death drops equipped/carry equipment and pouch rounds once at the body. The
same personal security state and equipped item references travel with the
person through the existing P04 portability path.

Hostile G05 factions evaluate at 1,000-tick boundaries, pay food and metal,
and create an 800-tick approaching expedition.  A known faction with a powered
relay gets a 300-tick warning.  Raiders materialize atomically from valid body
poses, retain raid-owned equipment until death, and create a one-time loot
receipt when their equipment becomes loose player-recoverable property.
Assault and sabotage actors use their own lamp-limited perception and ordinary
body navigation. A sabotage target is selected only after it is perceived;
target loss causes a bounded retarget, and surviving raiders physically return
to their ingress zone before resolution. Terminal expeditions compact after
their bounded incident/receipt records have been retained.

Grievance is bounded to eight visible typed causes.  The current implementation
records stable-period decay and combat injury, supports bounded nonviolent
protests, then a deterministic organizing cell and infrastructure sabotage for
severe stressed dissidents.  Sabotage disables the exact structure lifetime for
600 ticks without deleting its buffers; insurgents retain their original
person IDs and security/personality state.

The normal Crew surface has a fourth Security page for posture, live Guard
status, XP/readiness, public warnings/events, perceived hostiles, and policy
controls. It issues the same site-bound commands used by tests. Posts and
refuges must be currently visible, and stale policy revisions reject. No
secret-cell membership, target, or plan is exposed before a public outcome;
only a detected suspicious gathering becomes a public notice.

Focused coverage is in `tests/g06.lua`; the long-running deterministic state
smoke is `tools/g06_soak.lua <seed> <ticks>`.
