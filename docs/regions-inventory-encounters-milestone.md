# Regions, inventory, and encounter-variety milestone

## Verified baseline

Work began from clean commit `f1c397f033e2895ca35e5f0b0dedb9d8bba8a53e`.
The baseline has one 96x54 four-level Hearthford region, a compact separate
Jomon map, six household adults, six weapons, eight secondary items, twelve
passive discoveries, two relics, eight persistent containers, seven placed
threats, and save format 3. Its 38 standard-library tests, compilation, and
whitespace checks pass. The prior owner assessment is authoritative: finite
supplies work, but the game still needs regions that feel geographically
different, treasure that teaches its own visual language, more physical build
choices, and much better enemy navigation and plans.

## Bounded implementation plan

This tranche deliberately extends the existing direct architecture. It does
not introduce a universal item language, planner, encounter DSL, biome engine,
anatomy simulator, economy, or naval combat mode.

1. **Document and baseline.** Lock the counts, product boundaries, save-format
   decision, commands, and owner evidence here and in the short tracker.
2. **Physical inventory and protection.** Add authored item definitions,
   stable item instances, a rotatable courier grid, a larger bounded Jomon
   locker, readied and six armour slots, safe transfers, protection by damage
   kind, contextual injuries, load bands, and deterministic terrain statuses.
   Increment saves to format 4 and migrate format 3 without deleting overflow.
3. **Physical tavern and recruits.** Expand the walkable aboard map with a bar,
   tables, visible people, direct conversation/switching, equipment access,
   six persistent regional visitors, and a berth limit. Retire menu-only
   courier selection while retaining zero-time conversation and inspection.
4. **Bounded enemy decisions.** Fix blocked pursuit with a short deterministic
   path search. Give each actor a current goal, limited sight and hearing,
   last-known position, morale, finite resources, allies, and a small scored
   set of direct legal actions. Repeated no-progress prose is suppressed.
5. **Long-range and mixed encounters.** Add six or more weapon families,
   telegraphed ranged lanes, cover/elevation/weather/smoke/projectile checks,
   finite ammunition/reloads, complementary group roles, readable alerts,
   theft, retreat, and a 100-seed offline composition audit.
6. **Three dedicated regional generators.** Add a tidal coast, open forest,
   and limestone uplands. Each uses its own small geography method, three or
   more useful levels, five or more landmarks, six or more visible persistent
   containers, two contacts, two possible recruits, an authored objective,
   region-specific environmental clock, encounters, market effects, and
   reachability repair/validation. Hearthford remains supported.
7. **Travel and voyages.** Add gangplank destination choice, recorded travel
   time, persistent active-region state, and sporadic deterministic raider,
   deep-water creature, and original material/omen voyage situations reusing
   the ordinary action and cargo rules.
8. **Integration and play.** Preserve all regional exploration and local
   changes, audit at least 100 seeds, run focused and full automated checks,
   and conduct eight substantial real-PTY expeditions with different couriers
   and builds. Balance from observed routes and outcomes.
9. **Assessment.** Record exact counts, checks, PTY paths, failures and
   limitations, then update the active documentation in a final local commit.

## Design constraints carried into implementation

Items occupy exactly one known place: body slot, readied slot, courier grid,
ground/container, Jomon locker, or explicit lost/destroyed state. Objective
evidence remains a small separate record. Inventory inspection and cursor
movement take no time; a confirmed threatened repack/equip/drop/consume action
takes one turn.

Enemy intelligence uses current perceptions rather than hidden courier state.
The selected goal and next intent remain readable. Ranged awareness always
provides a preparation cue or escape/cover opportunity before a severe shot.
Regional pressure changes composition and goals, not only health or damage.
Idle terminal time never advances tides, weather, patrols, objectives, travel,
statuses, or encounters.

## Explicit exclusions

Several complete regions beyond the requested four, infinite terrain, full
fluid/weather/structural simulation, complete anatomical tissues, layered
clothing, generic planning or scripting languages, broad factions or distant
people, offline catch-up, crafting, real-time play, graphical tiles, servers,
accounts, telemetry, networking, multiplayer, generic magic, and unbounded
onboard incidents remain out of scope.

## Verification record

This section will be replaced with exact automated results, encounter-audit
output, and only the real PTY paths actually exercised after all phases are
integrated. Until then, the milestone is in progress and not assessed as
playable.
