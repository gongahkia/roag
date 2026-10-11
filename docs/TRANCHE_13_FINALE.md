# Tranche 13 finale contract

Three full biome stages retain their generated floors, doors, secrets, pressure,
elite rewards and extraction pursuer. The third unlocked exit offers one cached,
zero-time route: **Enter the Final Sanctum**. It creates an authored 23×17 chamber
on `core:floor/final_sanctum`. The three retired floors stay frozen. Their AI
turns, attacks, status ticks and delayed boon effects remain in those frozen
snapshots; none are migrated or applied to the new floor. The player's build,
health, stats, XP, currency and gameplay RNG continue into the chamber.

The Hollow Regent is one ordinary damageable enemy using the shared scheduler,
AI, telegraph, movement and damage pipeline. It cycles through a cardinal
piercing lane, a near fan and a locked 3×3 sigil. If a pattern has no valid
targeting geometry, it uses the sigil or moves along the normal pathfinder.
Below half HP it gains 25 speed for future actions; already committed cells
and due times stay fixed. These HP, speed, windup and damage values are
provisional balance data in `src/content.lua`.

A canonical `EntityKilled` record for the Regent marks its defeat once. The
action's synchronous effects and scheduled work through the next player-ready
boundary finish before terminal Victory is installed. If player and Regent
both die during that resolution, the player death wins the terminal result.
No Regent XP, currency or chest is added after the terminal blow.

`roeg-run/10` and `roeg-content/8` intentionally reject older active saves.
The two-generation persistence envelope is unchanged. A finale Save & Quit
stores the concrete chamber, boss phase/HP, locked attacks, delayed effects,
fog, player build and frozen stages. Victory and defeat write terminal records
to both slots, preventing an older active backup from becoming Continue.

Verification commands:

```sh
luajit tests/run.lua
luajit tests/finale_playthrough.lua 3 /tmp/roeg-finale-seed-3.trace
luajit tests/profile_finale.lua 3
luajit tests/finale_corpus.lua
sh tests/finale_gui_smoke.sh
sh tests/finale_disk_resume.sh
```

The seed-3 driver reads game state to choose moves but changes it only through
ordinary `Game.submit` actions. GUI finale and disk fixtures prepare later
states for focused rendering and save checks; they are not unassisted runs.
Physical keyboard feel and native GPU frame rate remain separate playtest work.
