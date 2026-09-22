# COS-G07 — Planetary Environments & Wider Frontier

G07 is the opt-in `environments=1` local-system extension. It requires the
current region, travel, logistics, industry, equipment, and security frontier
features. Feature-off campaigns remain the exact three-destination model: they
have no environment records, exposure, extra routes, solar/wear modifier, or
lazy sites.

## Bounded frontier and persistence

A G07 campaign has exactly seven persisted regional identities. Home Planet,
Moon I, and Moon II retain their IDs and direct durations (400, 600, and 800
ticks). Four deterministic frontier records use stable site/body IDs 4–7,
independent generated names, a selected environment archetype, profile, terrain
seed, recipe, visited flag, and direct Home routes of 900/1100/1300/1500 ticks.
Each additionally has one fixed direct Moon I/II link. The selection is without
replacement from six archetypes and deterministically guarantees low gravity,
high gravity, a non-breathable body, and thermal variety.

The first three worlds are still created for a new G07 campaign. Frontier 4–7
have `world=nil` until a living party actually arrives. `Campaign.instantiate`
creates the resolved saved seed/recipe only once at first arrival; it neither
rerolls nor runs a hidden colony. There are never more than seven sites. The
normal campaign codec owns body metadata, instantiated maps, routes, exposure,
and equipment state, so save/load and campaign history retain a single canonical
world identity.

## Mechanical environment

Each generated profile is integer data: gravity percent, atmosphere,
thermal/radiation severity, solar percent, machine-wear percent, and terrain
archetype. Home is the 100% breathable baseline; Moon I and Moon II receive
their stated low-gravity thin/cold profiles only in G07 campaigns.

Gravity changes the existing body navigation/fall contracts rather than adding
physics: safe level jumps allow 3 fine cells at <=60%, 2 through 110%, and 1
above 110%; existing fall damage is integer-scaled by gravity percent. Vacuum
suppresses ordinary torch illumination while powered Electric Lamps continue to
provide G01-compatible local light.

Living settlers and physical raiders have bounded atmosphere/thermal/radiation
exposure (0–100), updated every ten ticks. Zero effective exposure recovers by
two; 50 warns, 70 releases ordinary work for reachable safety/suit acquisition,
and 90 applies one ordinary HP damage per update. Shuttle passengers recover.
The Frontier Suit is an independently equipped physical item (not combat armor):
it eliminates atmosphere exposure and lowers thermal/radiation severity by one.
The powered Environmental Regulator has a 24-cell local thermal/radiation radius
but never supplies atmosphere. Guard work remains interruptible by immediate
environmental safety.

Small Solar Arrays use deterministic percent/remainder output; Fabricators and
Mining Rigs use the matching deterministic wear/remainder rule. Dust/airless
world generation passes the selected archetype into the existing generator,
altering rock/sand/ice/water/ore distributions. Existing flora/fauna are then
deterministically thinned for sparse/arid profiles instead of granting remote
ecological knowledge or creating a new species catalogue.

## Travel, security, and UI

Frontier Suit custody uses the existing equipment item model and its separate
environment slot. It travels with the same `personId`; craft accounting includes
equipped/carry items. Frontier routes keep normal cabin needs and consume the
one existing cargo-metal maintenance unit per departure. A non-breathable
destination warns in Expedition UI when passengers lack equipped or loaded
suits, without blocking a deliberate dangerous launch.

G05 and G06 operate on any instantiated owned site. A hostile raid against a
non-breathable target charges the normal one food plus one metal per raider and
one additional metal per required suit; each materialized raider carries a
physical Frontier Suit that drops on death or leaves on withdrawal. The normal
Region panel lists only coarse survey data (name, ownership, route, gravity,
atmosphere, thermal/radiation/solar), while the Crew Security panel lists suit
and bounded exposure. It does not reveal terrain/resource/ecology locations.

## Executable evidence

`tests/g07.lua` covers gate/legacy routes, deterministic seven-site generation,
profiles/names/route diversity, lazy creation, gravity, suit/exposure/health,
regulator/vacuum-light/solar/wear, environment terrain and ecology signatures,
actual 900-tick frontier founding with identity/cargo/history replay/seek, and
hazardous G05 raid debit/suit provisioning.

The final cross-system closure cases are deliberately compact rather than a
second environment subsystem. G07-I starts an ordinary build task in vacuum,
crosses the live danger threshold, releases that task, follows normal
fine-cell movement to a loose Frontier Suit, verifies unique custody, and
recovers. Its companion Alert case shows a Guard leaving an assigned defense
post for a reachable powered regulator while the site remains Alert. G07-J
does the same for a G06 protestor and runs the production 120-action
same-person sabotage transition, proving exposure and an equipped Suit persist
when the person becomes an insurgent. G07-E explicitly proves that a powered
regulator in vacuum leaves atmosphere exposure active and that power loss ends
only its live thermal/radiation cover.

G07-K carries one real G04 Machine Component through the established
Home → craft → transit → owned frontier-site unload path, proving source,
transit, and destination custody conservation. It then powers a real Signal
Relay at that same frontier outpost, discovers a faction, and uses the ordinary
site-bound G05 contact command and audience; site selection is not limited to
the original three bodies. The associated audit checked `logistics`, `travel`,
`factions`, `security`, Region rendering, and header site switching for an
operational exactly-three-site assumption. Their relevant iterations resolve
the campaign's dynamic site collection; remaining literal Home/Moon route
definitions are the intentionally preserved legacy routes.

`tests/g07_gui.lua` is MOCK-only evidence for the seven-body Region surface,
fog-safe summaries, live ticking, Crew exposure/suit text, expedition hazard
warning, and regulator inspector. Its closure fixture additionally clicks a
normal header site button to switch Home → owned site 4, checks the selected
frontier Crew/Security exposure and Lockdown data (rather than Home's), and
proves live ticking remains unpaused. It is not native LÖVE or human-play
evidence.

`tools/g07_soak.lua <seed> <ticks>` fabricates a suit, runs normal preparation,
loading, departure, 900-tick arrival and founding, unloads cargo, powers a local
regulator/solar work area, performs a codec round-trip, then continues the
multi-site campaign. The focused history test supplies replay/seek proof because
the soak keeps its long continuation on the direct campaign clock. The G07
maximum-size extension materializes all seven permitted maps and checks the
32 MiB full campaign envelope.

Native LÖVE/manual play is a separate verification category and is not claimed
by the mock or headless scenarios. G07 intentionally does not add oxygen
consumables, sealed rooms, weather fluids, relics, deep-space travel, new star
systems, offensive expeditions, or G08 archaeology.
