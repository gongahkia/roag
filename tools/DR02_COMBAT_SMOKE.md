# DR-02 Combat / Loadout Smoke Checklist

Run this from a fresh Campaign. This is a manual LÖVE checklist; automated
tests cover the deterministic action and persistence contracts separately.

1. Press `I`, then `Tab`; confirm the paused inventory shows Weapon A/B and
   Ability A/B plus their active markers.
2. Select a valid action with `W`/`S`, choose a slot with `A`/`D`, and press
   Enter. Confirm assignment is immediate and does not advance enemies.
3. Close inventory. Press `E`; confirm the active weapon attacks in facing.
4. Press `R`; confirm Weapon B becomes active, no attack is emitted, and
   enemies/world receive exactly one normal turn.
5. Press `Q`; confirm the active ability triggers. Press `X`; confirm Ability
   B becomes active with one normal enemy/world response.
6. Assign or activate self-destruct and confirm it requires a second `Q`.
7. Fire a ranged weapon until its magazine reads `0/capacity`; press `E` with
   compatible carried ammo. Confirm reload feedback, reduced reserve cargo,
   one enemy/world response, and no shot on that turn.
8. Repeat with too little compatible reserve; confirm a partial magazine fill.
9. Carry only a wrong ammo family; confirm an empty weapon dry-fires with
   readable feedback and consumes no wrong-family stack.
10. Break a quick-slotted component. Confirm its binding remains displayed but
    cannot fire/use; repair the same physical component and confirm it works
    without reassignment.
11. Verify bullets, shells, energy cells, and explosives occupy 1×1 inventory
    cells and visibly increase cargo mass.
12. Kill the active body with a partly loaded ranged component. Salvage it
    later and confirm its loaded magazine follows that exact physical part;
    confirm the successor has fresh quick defaults.
13. Save/reload after swapping slots, spending part of a magazine, and carrying
    reserve ammunition. Confirm slots, active indices, magazine, and cargo
    match exactly.
14. Verify Scatter Caster, Piercing Lance, ranged enemies, and bosses continue
    to use their established attack patterns.
15. Recheck DR-01: cardinal held movement, enemy bumps, faced `U`, paused
    inventory, construction behavior, transitions, and normally visible
    terrain without exploration fog.

`B` (bomb) and `F` (flare) deliberately remain temporary direct controls in
DR-02. Corpse salvage is still the existing list UI, and resource pickup still
uses faced `U` until DR-03.
