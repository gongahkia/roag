# ROAG manual UX smoke checklist

Run this with LÖVE after any presentation change. Headless tests cover the
view-model contracts; this checklist covers readability, hierarchy, clipping,
and the actual player journey.

1. Start with a fresh profile. Confirm the one-time body/salvage/rebuild
   briefing appears before the first run, then does not appear again.
2. Open **HOW TO PLAY** from the title screen. Confirm every listed control
   matches current input and the text fits at the supported window size.
3. On a normal floor, verify the HUD distinguishes HP current/max, OBJECTIVE,
   SCRAP, cargo status, charm occupancy, resources, curse, and non-normal
   locomotion without covering the playfield.
4. Stand next to a powered door, service kiosk, discovery cache, reinforced
   barrier, maintenance hatch, and corpse across representative runs. Confirm
   the context panel names the existing key and gives a readable unavailable
   reason where needed.
5. Take component damage and break a capability provider. Confirm condition,
   locomotion, and capability-loss feedback appears once per transition rather
   than once per integrity point. Check an enemy capability loss is readable.
6. Open inventory and body abilities. Confirm the body list, condition,
   footprint, rotation, mass, ability/provider, ammo cost, and wear are clear
   without displaying physical IDs.
7. Salvage a corpse. Confirm the panel shows slot, integrity, condition,
   mass, footprint, ability, current-slot comparison, and FITS/NO SPACE before
   transfer. Fill inventory and verify salvage remains atomic.
8. Complete reconstruction. Confirm selected inventory part, destination slot,
   compatibility/failure reason, and the distinction from repair are clear.
9. Visit each service. Confirm SCRAP, price, stock, affordability, repair
   integrity, component detail, sale value, charm effect, and charm capacity
   are readable without relying on colour alone.
10. Choose a curse and a route. Confirm exact curse scope/effect, boss/service
    route hierarchy, locked-edge requirement, and that discoveries remain
    undisclosed on the route map.
11. Fight each boss family. Confirm boss HP, named physical subsystems,
    locomotion, and telegraph/provider connection are readable. Break a
    telegraphed provider and confirm cancellation feedback.
12. Trigger faction combat and a reinforcement source. Confirm hostile actor
    colours/readability, source DORMANT/ARMED/SPENT state, countdown, and the
    visible source of arrivals.
13. Claim one unknown discovery and one known discovery. Confirm DATA versus
    SCRAP feedback is unambiguous. Inspect research history and verify unknown
    entries remain `???`.
14. Die, open the Fallen archive, and inspect an entry. Confirm the body,
    biome, charms, and recurrence status are readable without semantic IDs.
15. Visit Forest, Cave, Dungeon, and Reactor. Confirm all physical landmarks
    (trees, logs, boulders, stalagmites, rubble, machinery, cables, fixtures)
    render from the selected art pack rather than renderer-made placeholder
    geometry. Repeat after changing to at least one non-default art pack.
