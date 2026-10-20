# Lantern Below

A playable fantasy party RPG used to explore the same artwork as Regolith. Exploration and battles are both **top-down**. Four companions travel from Hearth through Ashwood into the Sunken Abbey, recover an ember relic, and return it to town. The colony simulator remains independently playable with `./play.sh`.

![Turn-based encounter in Ashwood](images/lantern-battle.png)

## Run

Requires the same .NET 10 SDK as Regolith. From the repository:

```sh
./rpg.sh
./rpg.sh --load
./rpg.sh --demo
./rpg.sh --scene forest
./rpg.sh --scene crypt
./rpg.sh --scene atlas
```

On Windows: `dotnet run --project src/Lantern.Client --configuration Release`. The shell launcher is optional and works from any directory. Default window: 1440×900; minimum: 1024×720. `--window-size 1280x800` sets another initial size.

The three-page field guide opens before play. F1 reopens it. Closing the window or Q saves to `~/.regolith/lantern.json`. `--save PATH` selects another file. This format is separate from colony and archived Python saves.

## Controls

| Input | Action |
| --- | --- |
| WASD / arrows | Move one tile; hold to continue |
| Left click | Walk to visible terrain or approach/interact with an object |
| E / Enter outside combat | Interact with the current/adjacent object |
| Mouse wheel over map | Zoom around pointer |
| Right / middle drag | Pan |
| + / −, Page Up / Down | Zoom around party position |
| G / 0 | Follow party again / reset zoom and follow |
| 1–3 in battle | Choose a class skill |
| 4 / 5 / 6 in battle | Guard / potion / retreat |
| Click an enemy / companion | Confirm the selected targeted action |
| Tab / Right in battle | Cycle targets |
| Enter / Space in battle | Confirm selected target |
| Click a party card outside battle | Select a companion |
| H outside battle | Spend a potion on selected companion |
| T / R | Burn a torch / rest at a campsite |
| I / J | Shared pack / expedition journal |
| F1 / F2 / F4 | Guide / complete asset viewer / original colors |
| F5 or Ctrl+S / F9 | Save / load, including mid-combat |
| F11 / Q | Fullscreen / save and quit |

Menus and the atlas pause automatic enemy actions. Gameplay has no real-time survival timer: exploration advances only when moving and combat advances by turns. Repeated movement into walls does not consume light.

## Expedition route

Start in **Hearth** with 36 gold, 5 potions, 4 torches, and 6 rations. Warden Elowen explains the quest. The inn fully restores the party for 12 gold. The provisioner sells two potions, two torches, and two rations for 10 gold. Move through the eastern sign to Ashwood.

In **Ashwood**, red encounter markers show enemies before combat; they are not random invisible rolls. The abandoned chest north of the first clearing contains the iron key. A nearby shrine restores mana and composure once. A campsite farther east spends two rations to restore health, mana, and composure. The eastern steps enter the abbey. Optional fights earn levels and supplies before the boss.

In **the Sunken Abbey**, sight is occluded by walls and previously seen rooms remain mapped. The north room holds the faithful dead, the south room a cistern encounter and brazier, and the western room a strongbox. The iron key opens the eastern seal. Defeat the Hollow Abbot, interact with the ember relic behind him, and return through Ashwood to Elowen. The completed expedition remains playable.

Defeat brings everyone back to Hearth at partial health/mana, adds stress, and removes a quarter of current gold. It preserves looted chests, cleared fights, opened gates, and exploration. There is no permadeath or campaign deletion in this prototype.

## Party and battle rules

| Companion | Class | Skills |
| --- | --- | --- |
| Rowan | Warden | Longsword; shield bash with stun; party guard/composure rally |
| Vesper | Ranger | Arrow; hunter's mark for bonus damage; volley against all enemies |
| Iris | Arcanist | Free wand attack; ember with burning; chain arc against all enemies |
| Sable | Vestal | Smite; mend to heal/revive and stop bleeding; party stress relief |

Every class also guards, uses shared potions, or retreats. Skills validate targets and mana before spending resources. Enemies display their next intent and target. Initiative uses speed with small seeded tie variation each round. Turn order is visible above the battlefield. Battles use distinct map positions, but there is no tactical walking/range/flanking system: position is presentation, and single-target attacks may target any living enemy.

Guard halves incoming damage until the unit's next turn. Burn and bleed deal damage at turn start. Stun skips a turn. Mark increases incoming attack damage. Stress at 80 or more weakens attacks, and low light increases enemy-induced stress. Torches restore 45 light; dark exploration reduces sight. Defeated party members can be revived by mend, a potion, camp recovery, or resting. Victory awards gold, XP, and a potion. Levels increase health, mana, and attack strength.

## Explore the sprites

Press **F2** inside the game, or run `./rpg.sh --scene atlas`. Type to search IDs, names, categories, interpretations, or ideas. F3 cycles packs, F6/Tab cycles families, and F7 cycles usage. F4 switches original/masked artwork. Arrows select entries; Page Up/Down or the wheel pages through the grid. Scroll over the detail panel to read long notes. Esc clears search, then returns to play; F2 also returns directly.

The native viewer has all **7,115 occupied regions** from Urizen, Godot node icons, and keyboard/controller prompts. Merely registering or viewing an image is not counted as a gameplay binding. Some entries are fragments, uncertain figures, or sparse marks, rather than independently usable objects. Controller art is available for inspection; controller input is not implemented.

Open [the offline atlas](../assets/atlas.html) for more filters and a **scene lab**. Pin sprites to a palette, paint separate ground/prop/actor layers, erase with the right mouse button, and export JSON recipes or PNG previews. The HTML embeds its data and original images, so it works directly from disk without a server or canvas cross-origin exceptions. Recipes retain exact stable IDs rather than copying/repacking source artwork.

## Code, catalog, and checks

`Lantern.Core` owns maps, collision, line of sight, party state, interaction, turn resolution, loot, quest progression, seeded random state, and atomic save files. It has no MonoGame dependency. `Lantern.Client` owns input, interpolation, top-down rendering, battle/party UI, and the native asset viewer. Both clients use `Regolith.Graphics` for sprite decoding, bitmap text, palette masks, and camera transforms; the two gameplay cores are independent.

The [binding table](../assets/reference/rpg-bindings.md) lists every RPG alias and source identity. [Catalog v2](../assets/reference/CATALOG_V2.md) explains exhaustive coverage, usage, evidence, and uncertainty. The original artwork, crop IDs, colony selection, and import audit are unchanged.

```sh
./tools/dotnet.sh build Regolith.sln --configuration Release
./tools/dotnet.sh test Regolith.sln --configuration Release --no-build
./rpg.sh --headless
./rpg.sh --smoke-test --screenshot /tmp/lantern-smoke.png
python3 tools/build_game_catalog.py --check
```

The headless driver completes a real expedition through movement, interactions, paid skills, camp costs, boss combat, and return-home progression. It does not grant items or victory through debug state. Core tests cover this on several seeds, plus collision, visibility, gate/chest rules, battle validation, guard, stress, healing/revival, burning, defeat, retreat, and exact save continuation. The native smoke opens a real window and checks the viewer, transition, combat, zoom, saves, and full quest before exiting without touching the player's save.

Capture other views with `--scene town|forest|crypt|battle|victory|atlas --screenshot PATH --frames 12`. Local distribution: `./tools/dotnet.sh publish src/Lantern.Client --configuration Release --self-contained false --output artifacts/lantern`. The output needs .NET 10 and includes all source images required by its viewer, the lightweight atlas index, runtime manifest, and credits. The larger audit/catalog stays in the repository.

This is an authored three-area vertical slice, with five encounters and one finite quest. Equipment is class-defined; item hauling, gear swapping, dialogue branches, shops with individual wares, procedural campaigns, directional sprite animations, tactical movement, and gamepad controls are future work. Maps use single-cell sprite interpretations, not an inferred autotile/animation system. Native graphics have been exercised on macOS Intel; other desktop platforms still need graphical verification.
