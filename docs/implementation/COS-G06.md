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
the exposed edge of a solid target is the sight sample, one stable raider
performs its 120 work actions, target loss/unreachability causes a bounded
retarget, and surviving raiders physically return to their ingress zone before
resolution. Terminal expeditions compact after
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

Combat events now use the existing bounded psychology memory model rather than
a second combat log: a threatened/injured colonist records the relevant danger,
a defending Guard records defense and a hostile kill, visible witnesses record a
combat death, and a public internal sabotage records saboteur/witness memories.
Those records use stable event identities and normal memory caps.

## Final acceptance evidence

`tests/g06.lua` is the focused acceptance/adversarial suite. It covers physical
equip/reload/death custody; first-body ray ordering and friendly obstruction;
Normal/Alert/Lockdown; powered-known versus unknown warning; atomic arrival or
holding; death loot versus withdrawal; one-actor external sabotage, exposed-edge
structure perception and deterministic retarget after target loss; target
loss/remote internal-sabotage rejection;
protest/cell/same-person insurgency; malformed loot/cell persistence; and a
campaign-history save/load/replay/seek/practice-branch traces through both an
approaching/warned raid and an active raid with partial internal sabotage, plus
an actual P04 armed-Guard trip retaining security, equipment, knowledge,
education, and psychology identity. The current focused result is 16 groups /
1,401 assertions.

`tests/g06_gui.lua` is explicit MOCK-only evidence for the live Crew Security
page. It exercises posture, Guard, posts, refuge, physical equip/reload,
public warning/sabotage/suspicious-gathering reports, hidden-cell discipline,
stale revision rejection, Escape, live ticking, and site isolation. It does not
claim a native LÖVE window or human playtest.

`tools/g06_soak.lua <seed> 30000` is the integrated scenario. It fabricates a
carbine, vest, and ammunition through a powered Fabricator; builds the Target
and Barricade through normal Build jobs; then verifies physical equip/reload,
training, posture movement, paid hostile approach/warning/arrival/combat,
event-hook grievance, protest, detected physical cell meeting, 120-action
internal sabotage, and unchanged-person insurgency. It codec-round-trips the
approach, active raid, detected organizing cell, and post-sabotage state before
continuing to tick 30,000 under the normal bounded state caps.

Latest deterministic trace: seed `10505`, 30,000 ticks, encoded campaign bytes
`74,064 -> 98,505 -> 118,982`, four codec round-trips, faction `2`, raid `1`,
foreign metal `40 -> 37`, one loot receipt, Guard person `1` ending at ammo `0`
and XP `36`, and insurgent person IDs `3,4` in a hostile cell. Native LÖVE and
manual play remain **NOT RUN** in this environment.
