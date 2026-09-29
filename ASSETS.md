# Asset credits

The playable LÖVE port uses the **[Kenney 1-Bit Pack](https://kenney.nl/assets/1-bit-pack)** for its tile and entity sprites. It is CC0; the supplied `assets/kenney/License.txt` is retained.

Sound effects are selected from Kenney's **[50 RPG Sound Effects on OpenGameArt](https://opengameart.org/content/50-rpg-sound-effects)**: footsteps, knife draw/slice, chop, coin handling, metal impact, doors, and UI book flip. This pack is CC0; its original `assets/sounds/license.txt` is retained.

The UI uses **[BigBlue Terminal](https://github.com/ryanoasis/nerd-fonts/releases/tag/v3.5.1)** (`BigBlueTermPlusNerdFontMono-Regular.ttf`) at native pixel-friendly sizing. It is retained with its upstream `assets/fonts/LICENSE.TXT`; the upstream BigBlue Terminal license is CC BY-SA 4.0.

The live game uses verified tiles from the supplied Kenney sheet. The player is the adventurer at column 25, row 1; targets, ammo, light sources, doors, projectiles, explosives, enemy variants, and the boss are likewise mapped explicitly in the `sprite` table in `main.lua`. The mapping was checked against a labelled rendering of the 49 × 22 sheet, avoiding the earlier grave-marker mismatch.
