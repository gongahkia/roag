# Tranche 10 movement presentation

The simulation still commits complete actions before playback. `Moved` events
carry immutable start/end tiles and Dash's traversed path; `Displaced` names its
target and start/end. Playback never reconstructs earlier movement from the
actor's final authoritative position. One ordered movement beat can contain
several actors and timestamps. Each actor follows its own event order, and all
actors in that beat move concurrently. Telegraph, impact, damage and death
events keep their existing ordered playback slots. This compresses mundane
movement between combat events without suggesting an extra player decision.

An ordinary segment takes **80 ms** with smoothstep easing and exact endpoint
snap. A beat with many segments lasts at most **320 ms**; longer paths retain
their waypoints and run faster within that cap. Arbitrary displacement with no
traversed path snaps instead of drawing a route through walls. Movement on a
different floor resets with the playback queue. Saving/continuing starts clean
presentation at the already committed logical tile. No motion state enters the
save, affects RNG, or advances world time. The existing input gate accepts the
next action after visual playback; there is no buffered action queue.

The camera follows the interpolated player tile and uses one rounded screen
offset for tiles and tokens. Enemy tokens are drawn only when both the visual
tile and their final authoritative tile are currently visible. Hidden motion
cannot expose an undiscovered route. The minimap continues to use authoritative
exploration, never interpolated positions.

## Corridor finding

Seed 1 includes five corridors of at least 24 tiles, with side alcoves. The
Thornspitter's authored firing range is seven cardinal tiles; projectile walls
stop its lane. In a clear seven-tile test lane, its locked windup committed at
world time 0 and struck at time 100, before the next player decision at 100.
The new historical windup label shows commitment and due times. This is a
real strict-scheduler tactical hazard, especially in a narrow corridor. No
range, windup, enemy speed or corridor geometry was changed from one anecdote.
Human playtesting should assess whether alcoves and line-of-sight breaks are
frequent enough near actual Thornspitter encounters.

## Manual feel check

1. Run `love .`, Continue or start a normal run, and move with WASD/arrows.
   Watch player and nearby enemy tokens glide one tile without blurring.
2. Use **X**, choose a cardinal direction and Enter. Try a full Dash and a
   second-tile obstruction; watch the actual traversed tiles.
3. Walk through a long corridor to scroll the camera. Check that floor edges,
   fog, minimap and player token stay aligned.
4. Approach a Thornspitter lane. Check whether its locked tiles and world-time
   warning are understandable, including when the strike precedes the next
   action. Try using a wall or alcove for cover.
5. Save & Quit during playback, Continue, and check that the player appears on
   the logical tile and that world time did not change on load.

Tranche 11 expanded boon/build synergies. Future content priorities are genuinely
different biomes and encounters, then final boss and permanent unlocks.
