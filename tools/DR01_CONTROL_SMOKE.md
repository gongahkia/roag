# DR-01 Campaign Control Smoke

Run `love .`, start or continue a Campaign, and check the following manually.

1. `W`, `A`, `S`, and `D` move only one cardinal direction; holding two keys follows the most recently pressed key and never moves diagonally.
2. Hold a direction through several clear tiles. Each tile should resolve a turn while the sprite and camera slide rather than snap.
3. Keep holding while enemies become visible or enter range. Movement must not auto-stop for danger alone.
4. Hold into a hostile. The player should lunge/recoil without entering its cell, one turn should resolve, and holding must not spend repeated bump turns.
5. Let the blocked hostile attack. The collision recoil and the existing hit flash/shake should read as separate, connected events.
6. Compare a LIGHT body/cargo load with BURDENED, HEAVY, and OVERLOADED loads: only held movement repeat cadence should slow. Single steps and attacks remain one turn.
7. Move to establish facing, then press `E`; the current ranged attack must use that facing. The small amber edge pip marks the current direction. Arrow keys must not fire or rotate the Campaign player.
8. Face a door, corpse, storage crate, service kiosk, reconstruction station, cave connection, ruin entrance, and Reactor access in turn. `U` must affect only the object directly ahead.
9. Face a corpse and press `U`; the existing salvage panel must open. `G` must not be needed in Campaign.
10. Face ground cargo and press `U`; it remains explicit pickup for now (no walk-over auto-pickup in DR-01).
11. Open inventory while holding a direction. No movement, attack, or use action may leak through the modal; closing it must not create a delayed movement burst.
12. Open and leave build mode; confirm its existing timing/modal behavior has not changed.
13. Traverse a surface edge, descend/ascend a cave, enter a ruin, and access a Reactor. After a zone change, held movement must be reset before new stepping begins.
14. Confirm existing rusher, flanker, skirmisher, controller, and heavy enemies still act normally.
15. Confirm terrain is visible normally even before any combat LOS has revealed enemies: there is no exploration fog-of-war.

This checklist requires graphical LÖVE playtesting; it is not satisfied by the headless test suite alone.
