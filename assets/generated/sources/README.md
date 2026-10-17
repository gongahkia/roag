# Dungeon art reference sources

Generated with the built-in imagegen tool on 2026-10-17. These originals remain
independent of the runtime atlas. No imported personal artwork was modified.

The following is the prompt set used for these three reference sheets. The
compiler enforces precise raster constraints that image generation alone did
not meet: equal cell slicing, silhouette crop and baseline, exact four-color
quantization, opaque terrain, matching opposite edges and lossless PNG output.

## terrain-v2.png

Raw static pixel-art terrain texture sheet for a top-down 8-bit dungeon game.
One exact four-column by two-row grid, no gutters, labels or outside border.
Every cell is a fully opaque square texture extending to every edge and
designed to repeat seamlessly. No isolated objects, transparent margins or
perspective. Row one: quiet dark worn stone floor; alternative quiet dark stone
floor; medium-value chunky masonry wall blocks; alternative masonry blocks.
Row two: dark calm water with small horizontal ripples; alternative ripples;
dark dirt with tiny pebbles; quiet mossy ground. Strict 16×16 logical pixel
appearance, crisp square pixels, maximum colors #101820, #28383C, #607466,
#B8C48A. Floors primarily the first two dark colors; walls mainly the second
and third with sparse pale highlights. No gradients, blur or antialiasing.
Flat, simple, blocky original 8-bit action-adventure appearance. Wide 2:1 sheet.

## classes-v2.png

Six upright playable-character sprites in an exact three-column by two-row
grid. Transparent background, no grid lines, text, scenery or shadows. Equal
scale and baseline, front view with a small right-facing weapon, two visible
feet. Stocky humanoid heroes with a readable head, torso, arms and legs; simple
8-bit action-adventure pixel art. Row one: tunic-wearing Adventurer with sword
and round shield; hooded Ranger with bow; Arcanist with pointed wizard hat and
staff. Row two: Berserker with broad axe; helmeted Sentinel with rectangular
shield; hooded Alchemist with potion flask. Four opaque colors plus alpha:
#101820, #52664C, #A3B66C, #E4E6AE. Dark outline, pale face and hands, midtone
clothes, bright equipment highlight, consistent proportions and transparent
space between cells. No crawling poses, monsters, gradients or glow. 3:2 sheet.

## enemies-v2.png

Twenty-five distinct enemies in an exact five-column by five-row grid, one
complete centered sprite per cell, matching scale and baseline. Transparent
background, no labels, grid, scenery or ground shadows. Chunky low-resolution
pixel art, readable silhouettes and thick dark outlines. Same four colors as
the heroes. No gradients, glow, antialiasing or cropped weapons. Square sheet.

1. Moss-covered guard and round shield; thorn-spitting bulb; ruin beetle;
   mushroom herald and horn; canal warrior and lance.
2. Hooded cinder healer and staff; bell-carrying cultist; horned glass beast;
   crystal bombardier and bomb; vault spider.
3. Mire witch and roots; chanting horned goblin; shadow-cloaked strider;
   forge wizard, hammer and flame; moss ogre with large fists.
4. Tower-shield fortress knight; antlered Thorn Warden with blade; mushroom
   queen Mycelial Matriarch; bulky Canal Lockmaster and trident; horned Ash
   Marshal and axe.
5. Crystal golem Prism Curator; robed Index Keeper and open book; furnace
   golem Ember Crucible; hooded scythe-bearing Extraction Pursuer; crowned
   skeletal Hollow Regent, robe and sword.

## Rebuild

From the game root, run `sh tools/build_generated_art.sh` for a dry run and
`sh tools/build_generated_art.sh --apply` to activate the validated generation.
This uses LÖVE 11.5 ImageData and zlib with the small PNG writer in
`tools/art_png.lua`; there are no new image-tool dependencies. The compiler
retains stable IDs, packs untrimmed 32×32 output cells, and keeps old builds.
The source sheets are references, not directly sampled by the game.
