# COS-G08 — Relics, Archaeology & Deep-Space Progression

G08 is the bounded `relics=1` extension to the G07 local frontier. It requires
`environments=1`; a relic feature cannot be enabled on a legacy or otherwise
non-environment campaign. Feature-off G01–G07 histories retain their existing
seven-or-fewer destinations, local routes, and no relic/system state.

## Bounded frontier and persistence

A G08 campaign persists three stable star-system records: the existing starting
system (sites 1–7) and exactly two generated remote systems (sites 8–9 and
10–11). The remote metadata is generated once from the campaign seed and saved:
system ID/seed/name, member sites, body environment/terrain seed, and routes.
The deep routes are Home–A Prime 2400, Home–B Prime 3200, and A Prime–B Prime
2800 ticks; each remote satellite has its 700/900-tick local route. They are
hidden before a legitimate scan, and their fine-cell maps remain `nil` until a
living arrival. No more systems or destinations are generated in G08.

## Ancient sites and physical custody

There are exactly five persistent Ancient Caches: three on distinct starting
frontier sites and one in each remote system. The starting sites contain five
relics, including a distributed compatible core/lens/anchor K-3 trio; remote
caches contain the remaining four. Nine relic IDs are the whole G08 object
budget. Their descriptor, role, family, and origin are intrinsic saved facts,
while player-facing understanding remains personal knowledge.

An Ancient Cache is a visible local structure only after normal fog/light
exploration. A reachable worker needs 180 normal relic-work actions to empty it;
the cache then drops its same relic identities into local ground custody. The
single relic ledger admits exactly one authoritative state per relic: cache,
ground/stockpile, person, shuttle cargo, Analyzer, drive socket, Trade Depot,
courier, faction, or raider. Shuttle cargo consumes a normal physical cargo
slot and can be loaded/unloaded only by a reachable worker at the docked craft.
Validation cross-checks cache membership, people, Analyzer slots, drive sockets,
craft capacity, and bounded courier records so a relic cannot be duplicated by
transfer, death, save/load, or trade.

## Study, teaching, and scanning

The powered two-slot Relic Analyzer is an ordinary G04 structure (2 metal and
2 Machine Components). A physically loaded relic and reachable worker receive
the role fact at 120 powered work actions and its signature fact at 300. Power
loss pauses, rather than completes, analysis. Those facts use P05/P06 personal
knowledge records: they travel with the person, survive security allegiance
changes, can be copied/learned through a Field School, and do not become a
global technology flag.

A 300-action deep scan requires a powered Analyzer with a loaded identified Lens,
a powered local Signal Relay, and the worker's lens-role knowledge. It reveals
only the two remote system names, bodies, coarse environments, and routes. It
does not instantiate a world or reveal terrain, cache coordinates, relics,
ecology, resources, or foreign hidden state.

## Relic drive and travel

The landed shuttle receives one Relic Drive Frame through 300 work actions after
physical delivery/consumption of 4 metal and 3 Machine Components. It has three
physical sockets. A functioning trio has exactly one core, lens, and anchor of
the same family; install/remove changes only the existing relic ledger.

Deep departure uses normal manifest, body, cargo, and P04 transit semantics. It
consumes one physical cargo metal maintenance unit plus one Machine Component;
local departures (including remote primary–satellite) still consume only metal.
An incompatible activation does not depart, damages/stresses nearby people via
existing health/psychology systems, and sets a 600-tick drive cooldown without
destroying relics or charging a successful-departure cost. A compatible but
unknown trio may depart unstable, producing deterministic 600-tick passenger
pulses; any passenger who has decoded all installed relic role/signature facts
makes the same trio stable.

Remote landing is ordinary P04 arrival/founding. It preserves `personId`, health,
psychology, knowledge, education, G06 state, equipped Suit, cargo, and socketed
relic identity; the G07 profile still controls gravity, exposure, light, solar,
and wear there. There is no ending after discovery or first arrival.

## Factions, security, and UI

A relic can be physically deposited in a powered Trade Depot and dispatched on
an accepted bounded courier transfer to stable faction custody. A raider may
steal only a perceived, reachable loose relic; death drops it locally and a
successful retreat transfers it to that faction. Caches, active Analyzer slots,
and drive sockets are not omniscient raid targets.

The existing Region panel hides remote systems until scan and then groups their
coarse bodies/routes under the normal frontier surface. Cache and Analyzer
context panels show physical action/power/slots. The expedition panel shows frame
state, socket occupancy, normal relic cargo, known compatibility, deep maintenance,
and instability uncertainty. All panels remain live; they do not pause the campaign. Native
LÖVE/human evidence is separate from mock coverage.

## Executable evidence

`tests/g08.lua` covers feature compatibility, deterministic hidden systems and
all five caches/nine IDs, 180-action excavation and duplicate-custody rejection,
powered Analyzer role/signature work and P06 transfer, constrained scan, physical
frame/sockets and compatible/incompatible activation, unstable 2400-tick travel
with exact metal/component debit and codec/seek preservation, plus Depot courier
trade and perceived raider death/retreat custody.

`tests/g08_gui.lua` is MOCK-only coverage for hidden/revealed Region discipline,
site switching, visible cache/Analyzer controls, and the drive/uncertainty
expedition presentation. `tools/g08_soak.lua <seed> <ticks>` fabricates a Suit,
visits the three distributed starting caches, analyzes/scans/refits/sockets the
compatible trio, makes a stable deep trip, founds/industrializes a remote site,
recovers a remote relic, codec-round-trips, and continues to 60,000 ticks.
`tests/maximum_size.lua` instantiates all eleven maps and retains the 32 MiB
whole-save envelope.

Intentional limits: no infinite galaxy, additional player ships, orbital combat,
oxygen/pressure simulation, research-tree replacement, ancient living NPCs, or
definitive ancient-civilization explanation.
