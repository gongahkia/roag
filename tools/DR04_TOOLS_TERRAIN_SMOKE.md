# DR-04 tools and terrain smoke checklist

This is a manual LÖVE pass. It complements the headless `tests/test_tools.lua`
coverage; it does not claim graphical verification on a headless host.

1. Start a fresh Campaign and confirm an **Axe** occupies carried cargo.
2. Open `I`, open the loadout panel with `Tab`, and assign the Axe to Weapon A.
3. Press `E` facing an enemy: the Axe should damage the actor, wear once, and
   not damage terrain behind that actor.
4. Press `E` facing an old-growth tree or fallen trunk. Each hit should give a
   visible impact pulse and generic crack state; it should take several hits.
5. Break the wood target, confirm physical TIMBER appears, then walk onto it:
   collection is part of the movement turn, not a second turn.
6. Obtain a **Pickaxe** from a Supply Kiosk and use it on cave stone/masonry.
7. Obtain a **Cutter** and use it on a metal machine or bulkhead.
8. Obtain a **Drill** and use it on an intended reinforced industrial target.
9. Try the wrong tool on stone/wood/metal; it should consume the committed
   swing, show no-effect feedback, and not alter integrity.
10. Try a protected transition, traversal barrier, or active reconstruction
    anchor. It must not be destructible with a tool.
11. Leave a partially damaged target, return, then save/reload and confirm its
    remaining integrity/crack state is retained.
12. Wear a tool to zero. It should stay in inventory as **BROKEN**, refuse `E`,
    and remain assignable only as an unavailable exact binding.
13. Repair that exact tool at a Repair Kiosk; confirm the physical ID, slot
    binding, and restored usability survive.
14. Drop/store/recover a damaged tool. Confirm ID and durability do not reset.
15. Die carrying a damaged tool, return with a successor, and recover it via
    the corpse spatial grid. Confirm durability is exact.
16. Visit Forest, Cave, Ruin/Dungeon, and Reactor profiles for the intended
    wood, masonry, metal, and industrial interactions.
17. Recheck DR-01 controls, DR-02 loadout/reload behavior, DR-03 spatial
    salvage and walk-over supplies, build mode, and visible terrain without
    exploration fog.
