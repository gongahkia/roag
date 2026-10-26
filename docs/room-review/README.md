# Approved room-layout archive

[review.html](review.html) preserves the approved layout diagrams and historical
art comparisons. It is an offline design record, not the current game UI or an
art-pack selector. No further vote is pending. The game has one fixed Kenney
1-Bit look. Current authoring/build instructions are in [ROOMS.md](../ROOMS.md).
The compiler reads validated Lua definitions and Python prototypes directly;
it does not read or regenerate this HTML archive.

## Approval and implementation

The downloaded feedback approved 14 room families and all five corridor ideas.
The user approved every earlier live room in chat and asked for Broken Cloister
to have a connected, restrictive ring with breakable exits. The revised
Cloister and all other approved content are now in the game; no further review
is required. The page starts with all rooms and groups marked approved, while
still accepting later comments or changed decisions.

`tools/room_prototypes.py` authors 15 families with seven structurally distinct
biome variants: **105 new wall layouts in 15 sizes**. Run
`make room-expansion` to compile their plain Lua definitions into
`src/room_expansion.lua`. `src/rooms.lua` registers them in all biome role pools.
Each room has three safe optional structure slots; each run chooses a seeded
subset without cutting off movement routes. The pictured prop is an example.

`src/corridor_branches.lua` may add one compact side branch to a suitable
corridor on a floor. The five patterns are a hidden spur, a rejoining braid, a
switchback side path, a small hub, and a false end with a concealed loop. They
appear across all seven biomes. Their clue, key, plate, brazier, gate, optional
enemy, and reward nodes have runtime state in `src/review_features.lua`. A
completed sidequest awards five gold. The HTML shows the larger design diagrams;
the game adapts each topology to the available corridor space. A floor may have
no sidequest when its corridors lack safe space or the seeded roll declines it.

## What the previews show

- The 15 families share design intent, while each biome version changes walls
  and terrain. They are related layouts rather than 105 unrelated silhouettes.
- Enemy and chest markers are candidate positions. Generation uses a subset.
- Dashed structure slots are optional. The runtime selects a subset by seed.
- Corridor previews show topology and possible quest nodes. They are conceptual
  diagrams; actual branches are smaller, rotated to match a straight stretch
  and omitted when they would cross a room.
- All earlier room templates remain in the game. The **All live rooms** tab
  includes them and the new ones.

## Headless checks

`python3 -m tests.room_prototype_tests` checks all authored variants, and
`luajit tests/run.lua` includes corridor and sidequest progression tests.
`node tests/room_review_page_test.js` checks the offline page without opening a
browser window. Existing saved runs with room content version 2 remain accepted
when their stored maps validate against the unchanged earlier templates.
