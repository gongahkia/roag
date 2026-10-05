# DR-05 synergy smoke checklist

Use a disposable Campaign body; these checks intentionally allow powerful,
chaotic outcomes.

1. Buy and equip Arc Relay, then assign Piercing Lance.
2. Fire Piercing Lance without a functional Shock Coil: Arc Relay reports inactive.
3. Install a Shock Coil, pierce one hostile, then a line of hostiles.
4. Repeat beside conductive water and conductive metal; confirm the actual network changes the arc.
5. Equip Kinetic Feedback, then add Kinetic Capacitor and use Hydraulic Ram/Impact Maul into a wall.
6. Arrange a crowded wall collision and confirm the secondary Force burst moves the room.
7. Equip Recycler with a ranged weapon, kill a hostile, and confirm the active magazine gains one round only up to capacity.
8. Use Scatter Caster for a multi-kill and confirm each kill may refund a round.
9. Empty a magazine with Quick Reload equipped; ATTACK reloads (does not fire) and lowers Dash recharge.
10. Equip Rupture Core and a functional Volatile Charge; break an enemy component and confirm the ordinary explosion, Force, fire, and terrain systems react.
11. Break a boss subsystem with Rupture Core; confirm no blanket boss proc immunity.
12. Open Inventory and inspect BUILD EFFECTS for active/inactive explanations.
13. Die with charms equipped; successor has no inherited reactive effects.
14. Save after a completed chain, reload, and confirm magazine/world state persists.
15. Recheck DR-01 movement/use, DR-02 swaps/reload, DR-03 salvage/pickup, DR-04 tools/terrain, and no exploration fog.

Run `luajit tools/analyze_synergies.lua` for the deterministic 500-scenario headless pass. Graphical spectacle and label readability still require a human LÖVE smoke test.
