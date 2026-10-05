# PLAY-01 game-feel smoke

Run a Campaign build in LÖVE. This is a human feel check, not an automated
pass/fail substitute. Record observations with `[TOO SLOW]`, `[TOO FAST]`,
`[JANKY]`, `[SMOOTH]`, `[UNCLEAR]`, or `[GOOD]`.

## Movement and camera

1. Tap a single tile, then hold movement across open Forest space; stop and change held direction abruptly.
2. Repeat through Cave, Dungeon, and Reactor terrain.
3. Check that steps remain grid-readable, consecutive steps do not queue behind input, and the camera lags gently without wobble.

## Facing and intent

1. Face all four cardinal directions.
2. Without reading the HUD, identify facing from the sprite's front-weight, marker, weapon indicator, and cyan attack outline.
3. Swap weapons; confirm the direct footprint updates immediately.
4. Enter Build stance: confirm the weapon footprint/orientation disappears, the faced construction ghost replaces it, hostile threats remain, and exit restores the weapon preview.

## Combat clarity

1. Use basic melee, a ranged shot, Scatter Caster, Piercing Lance, heavy Force melee, and a tool.
2. Check attacker lunge/recoil, target flash/recoil, particles, shake, short hit-stop, and one damage number for every successful hit.
3. Check component break and death feedback remain readable beside damage text.
4. Trigger Arc Relay, Kinetic Feedback, and Rupture Core; every derived hit should remain individually legible.

## Threats, projectiles, and clutter

1. Fight one enemy, three enemies, a dense group, and a boss.
2. Before acting, compare cyan player outlines with red hostile threat outlines. Confirm only visible enemies show threats and a disabled ranged provider removes that threat.
3. Fire across Forest, Cave, Dungeon, and Reactor backgrounds. Confirm projectile core/outline and short tracer make the path reconstructable; scatter and piercing paths should still read.
4. Test several threats, projectiles, electricity, fire, proc chains, and damage numbers together. Record exactly where feedback becomes noise; do not tune it down without this evidence.

Graphical movement, readability, hit-stop, and clutter are **not verified by headless tests**. This document is the required LÖVE review surface.
