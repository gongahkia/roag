# Connected biome floors — expansion checkpoint

Normal **New Run** now creates two connected 2D floors per biome stage. Move
cardinally onto the cyan stair marker to change floors. The move costs one
ordinary recovery; there is no extra stair action or automatic bounce. The HUD
shows `Floor 1/2` or `Floor 2/2`, and the minimap shows only discovered stairs
on the active floor. The stage miniboss and extraction exit are on floor 2.

The biome budget is shared across the two floors: six rooms on each, five paid
chests (two upstairs, three downstairs), two optional ordinary secrets (one of
each existing mechanism), and one miniboss. Both floors are concrete saved
maps. The inactive floor retains its enemies, fog, rewards, statuses, locked
attacks and delayed effects. Floor-local scheduled work is rebased by exactly
the simulated time spent away when the player returns; the stage pressure and
extraction deadline stay on the continuous stage clock. The invulnerable
pursuer relocates deterministically after a stair move without duplicating.
Floor transitions checkpoint the active run at a stable player decision.

Studio E03 has a **Stair** marker tool in the second toolbar row, also selected
with **Y**. Click a walkable, connected tile in a room to add or replace its
single optional stair anchor; click the same tile again to remove it. Validation
rejects blocked, disconnected, editable or duplicate stair anchors. The marker
rotates and mirrors with the room. A selected authored terminal room on floor 1
or start room on floor 2 can supply the generated stair position; otherwise
generation uses the existing reserved elite/start marker. E03 source encoding
remains `roeg-studio-world/1`, so older projects without this optional marker
continue to load. The published world pack retains stable room and tile IDs.

The legacy `Game.new_stage(seed)` constructor remains available to existing
single-floor headless fixtures and old saved runs. Normal frontend New Run
passes `{multifloor=true}`; route handoffs inherit that mode. A fixture or
headless driver can opt in with `Game.new_stage(seed,{multifloor=true})`.
`Game.new_generated(seed)` remains the old stage-free constructor.

Run focused checks with:

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp sh tests/expansion_floor_gui_smoke.sh
TMPDIR=/var/tmp sh tests/expansion_floor_disk_resume.sh
```

The GUI and disk scripts use prepared stair approaches, isolated temporary
LÖVE save directories and real callback/render/save paths. The headless suite
also includes a seed-3 **ordinary-action** movement trace from entrance to
floor 2. These are automated checks, not physical-keyboard playtesting.

The first compact layout retains 104×68 tiles per floor and twelve total rooms.
In sampled seeds 1, 3, 7 and 29, shortest critical-path distances were
respectively 179, 211, 194 and 176 tiles across both floors, versus 156, 165,
151 and 165 on the legacy one-floor map. This is a pacing concern, not evidence
of a broken route. A tighter spacing attempt failed corridor/room protection
validation and was reverted. Reassess stage travel with more legal-action data
before reducing room separation or changing combat speed.
