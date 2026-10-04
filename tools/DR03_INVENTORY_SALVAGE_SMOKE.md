# DR-03 Inventory and Salvage Smoke Checklist

Run a Campaign build in LÖVE. This is a manual visual/interaction checklist;
the headless suite covers the deterministic transfer and persistence rules.

1. Face an ordinary corpse and press `U`; confirm the old text list is replaced by FALLEN CARGO and YOUR INVENTORY spatial grids.
2. Drag a one-cell corpse component into an empty player cell.
3. Drag an irregular component, rotate with `R`, and place it only where its full footprint fits.
4. Release an item over occupied/out-of-bounds cells; confirm the corpse and player inventories remain unchanged.
5. Recover a large boss component and verify it is shown by the same grid, not an anatomical body layout.
6. Die with components, construction material, and ammunition; return with a successor and recover selected player-corpse cargo.
7. Recover a partially loaded ranged component; after reconstruction, verify its magazine count remains unchanged.
8. Take only part of a corpse, close it, leave the zone, save/reload, and return; only unsalvaged cargo should remain.
9. Walk onto TIMBER, MASONRY, METAL, BULLETS, SHELLS, ENERGY CELLS, and EXPLOSIVES; each should collect with the movement action and a concise `+N` message.
10. Fill the inventory so a whole supply stack cannot fit; walk over it and confirm the complete stack remains on the ground.
11. Walk onto a loose component; confirm it remains on the ground. Step beside it, face it, and use `U` to collect it.
12. Open inventory, select a stack/component, then use `Delete`/`Backspace` or click DROP SELECTED. Confirm it appears at the current cell without spending a turn.
13. Close inventory without moving; confirm a dropped resource does not instantly re-collect. Leave and re-enter to collect it.
14. Pick up and drop enough mass to change encumbrance; confirm subsequent held movement cadence changes while attacks remain crisp.
15. Verify `E`/`R` weapon slots, `Q`/`X` ability slots, physical ammo/reload, directional `U`, enemy bump behavior, inventory pause, and build-mode behavior still work.

Do not treat this checklist as completed on a headless environment: mouse drag,
layout readability, and feedback need a human LÖVE pass.
