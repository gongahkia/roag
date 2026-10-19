# Regolith sprite reference

This reference describes **every occupied sprite region in all three downloaded packs**, including every icon variant, both generations of input prompts, the main Urizen atlas, and its free 2-bit sample. It gives each region a stable ID, source rectangle, identification basis, current-game proposal where appropriate, and an idea for future use.

**All 7,115 sprite entries are currently unused by the game.** Regolith renders character glyphs in `regolith/tui.py`; it does not load these PNGs. A proposed mapping below is a design suggestion, not an implemented renderer or a new gameplay feature.

The original [catalog.json](catalog.json) remains the file inventory: dimensions, hashes, archives, publisher links, and licenses. It did not previously enumerate the sprites inside the atlases. The new [sprites.json](sprites.json) supplies that detail.

## Open the reference

| File | Contents |
|---|---|
| [sprites.html](sprites.html) | Offline visual browser with individual sprite crops; search by name, coordinate, feature, or idea and filter by pack, mapping, and identity basis. Opens directly without a server. |
| [sprites.json](sprites.json) | Complete machine-readable reference, including blank slots, separators, annotations, exact crops, visual metrics, historical name matches, and recommended mappings. |
| [Urizen table](reference/urizen.md) | One Markdown row per occupied cell in both Urizen atlases. |
| [Godot icon table](reference/godot-node-icons.md) | One Markdown row per individual icon, including every color/style variant. |
| [Input prompt table](reference/input-prompts.md) | One Markdown row per key, controller symbol, size variant, and visual state. |
| [coverage.json](reference/coverage.json) | Counts and per-sheet coverage checks. |
| [urizen-names.json](reference/urizen-names.json) | Frozen historical author metadata and the artwork matches used to recover names. |
| [index.html](index.html) | Original whole-image browser and enlarged pack previews. |

The long Markdown tables contain **all sprite entries**, not just a shortlist. Blank atlas slots, guide bands, and source credits are enumerated in JSON rather than expanded into thousands of empty Markdown rows.

## Coverage and counting

| Pack / source | Original PNGs | Occupied sprite regions | Other regions |
|---|---:|---:|---|
| Urizen main, `urizen_onebit_tileset__v2d0.png` | 1 | 5,551 | 4,549 empty/black-only slots; 200 separator slots |
| Urizen free 2-bit sample, `urizen__2bit__free.png` | 1 | 125 | 15 empty slots |
| Godot node icons, all six variants | 653 | 653 | None |
| Input prompts, all nine sheets | 9 | 786 | 8 author/font-credit regions |
| **Total** | **664** | **7,115** | **4,772 non-sprite regions** |

There are **11,887 total records**. Of the sprite records, **1,885 have a proposed use for an existing feature**, and **5,230 remain unmapped with future-use ideas**. Actual runtime mappings: **zero**.

“Occupied sprite region” means a crop containing visible artwork. It does **not** mean a guaranteed independent object or unique design. Multi-cell machines, large buildings, banners, celestial bodies, and plants contribute several cells. Six extremely sparse cells contain only one or two foreground pixels and are explicitly flagged for review. Forty-two sprite crops repeat another crop's exact RGBA pixels and dimensions; their individual entries remain present with `identical_crop_to` links.

### Identity and mapping are separate

| Field / value | Meaning |
|---|---|
| `runtime_status: unused` | No game code currently loads or displays this bitmap. Applies even when the sprite depicts something already simulated. |
| `mapping_status: proposed` | A suggested use for a feature that exists in Regolith today. It is not an active binding. |
| `mapping_status: unmapped` | No specific existing feature is assigned. Read `future_ideas` for a possible use and any mechanics/art changes it would need. |
| `not_applicable` | Empty space, separator art, or a source credit, rather than a gameplay sprite. |
| `author_metadata_visible_rgb_match` | A historical name from the author's Urizen tile data matches this crop's visible RGB artwork. |
| `publisher_filename` | The icon name comes from its provided filename. The Regolith interpretation is still a proposal. |
| `visible_key_label_or_symbol` | A key or controller label read from the image. Controller bright/dim states are visual observations. |
| `visual_inference` / `visual_region_and_silhouette` | A working visual description or group, with unconfirmed author identity. These are deliberately marked as uncertain. |

An identified sword can be **unmapped** because there is no combat system. An unnamed metal panel can have a **proposed** hull use while its exact author name remains unknown. These two questions should not be conflated.

## Urizen inspection

The publisher's [Urizen page](https://vurmux.itch.io/urizen-onebit-tileset) presents the pack as a mixture of themes and includes modular machinery as well as characters. The downloaded files have no companion tile-name manifest. Their fantasy, terrestrial, and science-fiction art needs different interpretations for a lunar colony.

### Exact atlas layout

Both local sheets use **12×12 pixel cells**, with a **one-pixel initial margin** and **one-pixel gutters**. For zero-based column `c` and row `r`:

```text
x = 1 + 13*c
y = 1 + 13*r
rectangle = [x, y, 12, 12]
```

The main sheet is **2679×651 pixels**, with **206 columns × 50 rows**. The sample is **261×92 pixels**, with **20 columns × 7 rows**. `grid` in the JSON is `[column, row]`; IDs spell out row first, for example `urizen-onebit-r09-c055`.

The main sheet's columns **25, 51, 77, and 103** are magenta separator bands. They are excluded from gameplay candidates. The sheet also has **43 visible pixels in gutters**, including faint border marks and stray pixels; their exact positions and colors are retained in `atlas_gutter_marks`. They are not counted as extra sprites or used to enlarge neighboring crops automatically. The 2-bit sheet has no visible gutter marks.

“Empty” means no nontransparent foreground pixel with any RGB channel above 8. This matters because the files mix transparent, black, and almost-black backgrounds. An alpha-only test incorrectly identifies opaque black padding as art. The threshold is explicit in the generator and JSON; original pixels are never changed.

| Main-sheet columns, inclusive | Visually inspected content | Regolith interpretation |
|---|---|---|
| 0–24 | Walls, doors, textures, vegetation, animals, food, minerals, clothing, tools, furniture, buildings, miscellaneous items | Geology and deck/hull candidates; indoor crops; tools, supplies, bunks, salvage, and decoration. Plants and animals do not imply outdoor lunar ecology. |
| 26–50 | Fantasy/medieval buildings, weapons, armor, scrolls, books, signs, containers, heraldry, effects, modular larger pictures | Mostly future props, keepsakes, reskinned tools/storage, scenario art, or UI symbols. Combat and magic are not current systems. |
| 52–76 | Metal architecture, roads/rails, vehicles, medical equipment, electronics, computers, machine parts, conduits, banknotes | Strongest machinery/HUD material; many current-feature candidates. Roads, rovers, pipe networks, and money require new mechanics. |
| 78–102 | Spacecraft and sci-fi effects, figures/poses, celestial objects, patterned tiles, status symbols, bitmap text | Crew/suit alternatives, effects, sector-map art, graphical UI, and font glyphs. Several pictures span cells. |
| 104–205 | Dense figure/profession matrix, sparse creature rows, miscellaneous figures and poses | Crew appearance candidates near the human figures; other rows remain unconfirmed body/species variants with future-use ideas. |

Each occupied cell has its own row in [reference/urizen.md](reference/urizen.md). Its JSON record also retains foreground-pixel count, dominant colors, and an exact RGBA crop hash.

### Recovered names and remaining uncertainty

The author's [historical tile data](https://github.com/vurmux/urizen/tree/28b1e60cf8fa2f15622537f6df0290e2ac890d5f/urizen/data/tilesets) supplies names for older basic, fantasy/medieval, modern, and font sheets. Those files are not laid out like the downloaded v2.0 atlas, so copying their tile numbers directly would be incorrect.

Instead, each old 12×12 graphic was compared with the new sheet. Transparent and near-black background pixels were normalized to black; every remaining RGB pixel had to match exactly. No fuzzy, nearest-neighbor, or silhouette-only naming was used. This found **1,017 matching occupied cells**, of which **1,015 have a usable named author category**. Two matches have empty/unknown names and remain visually described. Every recovered name retains its source set, old tile ID, and original properties in JSON.

The other **4,536 main-sheet occupied cells** receive explicit visual descriptions or region/coordinate labels. They are not falsely presented as fully identified. In particular:

- Figure rows are recorded as body variants, with sampled colors. A row is not automatically a named species or a recolor of another row.
- Aligned character columns have held-prop observations where readable. A hammer-like silhouette is a possible engineer appearance, not a confirmed author profession.
- Adjacent poses are not declared to be animation frames. No frame timings or movement directions were supplied.
- Structural indices are not assumed to be adjacency bitmasks. Check corners, joins, and orientation labels visually before building an autotile table.
- Cropped parts of a large machine, banner, plant, or planet must be assembled deliberately. They are not all complete one-cell objects.
- The 125 occupied 2-bit sample cells have visual working labels, not recovered author names. The sample does not provide a complete matching replacement for every one-bit tile.

### Concrete starting mappings for this game

These are also stored in `sprites.json` under `recommended_mappings`. They cover all terrain types and current buildable modules. A different crop can be chosen later without changing the simulation model.

| Existing feature | Suggested sprite ID(s) | Interpretation / required adaptation |
|---|---|---|
| Lunar surface | `urizen-onebit-r05-c002` | Author's sand floor, desaturated to gray regolith. The cell remains vacuum. |
| Basalt | `urizen-onebit-r02-c000` | Stone silhouette distinct from constructed metal hull. |
| Water ice | `urizen-onebit-r22-c003` | Crystal silhouette recolored icy cyan and overlaid on rock/regolith. |
| Iron deposit | `urizen-onebit-r22-c000` | Geode silhouette recolored iron/rust; proposed geology, not an author-provided iron label. |
| Silicate crystal | `urizen-onebit-r22-c002` | Different mineral silhouette and violet color from water ice. |
| Habitat deck | `urizen-onebit-r01-c052` | Metal floor; existing deck connectivity still carries power. |
| Pressure hull | `urizen-onebit-r00-c052` | Metal wall; add separate damage/breach indication if needed. |
| Airlock | `urizen-onebit-r00-c068`, `urizen-onebit-r00-c071` | Valve door / open metal door as visual states of one airlock cell. |
| Solar array | `urizen-onebit-r00-c064` | The author calls this **glass wall**. Its diagonal cyan panel is a reskin candidate, not a confirmed solar-panel sprite. |
| Battery bank | `urizen-onebit-r09-c055` | Battery symbol; charge and network status need separate overlays. |
| Life support | `urizen-onebit-r35-c058` | Vent/fan-like machine part as a compact symbol; exact depicted machine is unconfirmed. |
| Hydroponics | `urizen-onebit-r24-c005` | Vegetables inside a pressurized farm module. |
| Sleeping pod | `urizen-onebit-r36-c000` | Single bed, optionally enclosed with science-fiction casing. |
| Med station | `urizen-onebit-r09-c059` | First-aid case as a compact medical facility marker. |
| Earth uplink | `urizen-onebit-r06-c055` | Radio tower with separate powered/active status. |
| Starting RTG | `urizen-onebit-r06-c056` | Power-station silhouette as a generator symbol; not an engineering drawing of an RTG. |
| Crew / stranded people | `urizen-onebit-r00-c104`, `r00-c105`, `r00-c106` in the same atlas | Neutral, hammer-like, and pick-like figures. Add visible suit cues outside and distress/escort overlays. |
| Fire | `urizen-onebit-r42-c006` | Flame overlay driven by the cell's existing fire value. |

Urizen's pixels describe appearance. Oxygen diffusion, heat transfer, decompression, power networks, mining, work orders, survivor oxygen deadlines, and escorted rescues remain properties of the simulation. Decorative pipes do not create an oxygen network; an open-looking background tile does not bypass the airlock rules.

## Godot node icons

All **653 individual files** are listed, with their original filename identity and a specific use or future idea. These are UI/node icons rather than a complete map tileset. The six source styles are:

| Variant | Files | Suggested treatment |
|---|---:|---|
| `color` | 12 | Colorful status illustrations and damage/health badges. Some semantic families occur only here. |
| `control` | 128 | Alternative UI palette. |
| `node` | 128 | Alternative UI palette. |
| `node_2D` | 129 | Alternative UI palette, plus the extra `icon_character_2` file. |
| `node_3D` | 128 | Alternative UI palette. |
| `white` | 128 | Suggested starting HUD set, recolorable to the game's status colors. |

Most files are 16×16. The five `icon_reset_2.png` variants are **10×12**; their rectangles preserve that size. Variants remain individually documented, even when they share a filename/semantic family. Their colors do not automatically mean selected, disabled, or hovered.

Useful initial white icons are `shovel` for mine/salvage, `hammer` for build, `liquid` for water, `thunder` for power, `gem` for crystal, `heart` for health, `character` for crew, `interrogation` for distress/help, `signal` for communications, `magnifier` for zoom, `save`, `paused`, `play`, and `time`. Their complete paths and uses are in the per-icon table.

Unmapped icons also have concrete ideas: `robot` for a future maintenance drone, `wheel` for a rover/hauler, `brush` for cosmetic room painting, `audio`/`sound` for future audio settings, and `coin`/`money_bag` for a future trade system. None of those mechanics is implied to exist already.

## Keyboard and controller prompts

All prompt rectangles were checked against the original sheets. **Every nontransparent source pixel falls inside a documented control/device rectangle or a separately classified credit region.** This avoids silently missing small controller buttons or splitting a long key into several false sprites.

| Sheet | Prompt sprites | Notes |
|---|---:|---|
| New Keyboard Black | 96 | Raised/flat keys and keyboard/mouse device symbols. |
| New Keyboard Black Corn | 96 | Alternate font and different modifier-key bounds; font credit recorded separately. |
| New Keyboard White | 96 | Device symbols are lower on the sheet rather than at the upper right. |
| New Keyboard White Corn | 96 | Alternate font; font credit recorded separately. |
| Legacy Keyboard Buttons | 96 | Separate older release, with its own source path. |
| New PS4 | 81 | Large, compact, and tiny symbols; face buttons, Options/Share, D-pad, shoulders/triggers, L/R sticks. |
| New Xbox 360 | 81 | Three size families; A/B/X/Y, Start/Back-style triangles, D-pad, shoulders/triggers, sticks. |
| New Xbox One | 87 | Three size families, including View/Menu/blank-button alternatives. |
| Legacy Xbox Buttons | 57 | Bright/dark style groups, older button art, and controller silhouette. |

### Current keyboard uses

Key labels are keycap identities, not case-sensitive action strings. For example, the `L` keycap can illustrate lowercase `l` movement; `Shift` + `L` illustrates loading a save. The game code was checked when assigning these proposals:

- Arrows and `H/J/K/L`: cursor movement. Shifted arrows and `H/J/K` provide larger steps; uppercase `L` loads instead.
- `D`, `B`, `P`, `X`, `V`, Enter, Esc: mine/salvage, build, survey, cancel work, region selection, confirm/interact/rescue, cancel/inspect.
- `R`: cycle **on-map** stranded-survivor signals. It does not open a separate expeditions tab.
- `G`: return to the habitat. `C`: crew panel. `F`: follow a colonist. `E`: event log. `O`: environmental overlays.
- Space: pause/resume. `1/2/3`: simulation speed. `1`–`9` also select menu entries; `0` selects the tenth build item or resets map zoom outside that menu.
- `S`: save. Shift+`L`: load. `T`: tutorial. `N`/`P`: next/previous tutorial page; `N` restarts on the game-over screen. `Q`: quit.
- Mouse symbol: direct survivor selection/interactions and pointer-centered wheel zoom.

Tab, Ctrl, Alt, and letters without a current binding remain unmapped, with future shortcut/keybinding ideas. The downloaded keyboards do **not** include dedicated keycaps for every supported control: `+`, `-`, `=`, `_`, `.`, `?`, Page Up/Down, and a specific wheel-scroll graphic would need text or additional art. The mouse symbol alone does not distinguish left click from wheel movement.

The keyboard layout is not a uniform tile atlas. For example, Enter is **42×16**, the raised Space key is **80×16**, and some flat Space variants are **64×16**. Corn-font Ctrl/Alt bounds differ from the standard font. The source's shifted/shortened flat-state modifier row is preserved rather than inferred from the raised-state positions.

### Controller ideas

**The game currently has no gamepad input.** Every controller sprite therefore remains unmapped. Its entry suggests a possible future binding: A/Cross to confirm or rescue; B/Circle to cancel; X/Square for a context tool; Y/Triangle for build/overlays; D-pad for cursor/menu navigation; triggers for zoom; shoulders for crew/distress cycling; sticks for pan/follow; Menu/Options for pause/help. These are design ideas requiring implementation.

Bright/dim and raised/flat describe observed artwork, not guaranteed publisher-defined input states. Neutral and direction-marked D-pads are separate sprites. An L/R stick glyph identifies the stick, not an analog movement direction.

## JSON examples and reuse

The top-level `records` array includes both sprites and non-sprite regions. Filter on `kind == "sprite"` for usable artwork candidates. `source` is relative to `assets/`, `rect` is `[x, y, width, height]`, and coordinates start at the top left with zero-based indices.

```python
import json
from pathlib import Path

reference = json.loads(Path("assets/sprites.json").read_text())
sprites = [r for r in reference["records"] if r["kind"] == "sprite"]
unmapped = [r for r in sprites if r["mapping_status"] == "unmapped"]
airlock = [r for r in sprites if any("airlock" in use.lower()
                                   for use in r["current_game_uses"])]
```

`identity.upstream_matches`, when present, retains author properties such as `group1`, `group2`, `group3`, `orientation`, and `index`. The original spellings are preserved, even where dated or unusual. `identical_crop_to` means exact pixel/dimension repetition, not that two differently colored graphics are interchangeable. The visual browser embeds its data and crops the unchanged originals using nearest-neighbor scaling.

## Regeneration and verification

[tools/build_sprite_reference.py](../tools/build_sprite_reference.py) rebuilds the sprite JSON, three complete Markdown tables, coverage report, and offline browser. It reads local artwork, the original catalog, [the frozen name matches](reference/urizen-names.json), and [the browser template](reference/browser-template.html). It makes no network requests. Pillow is needed only to regenerate this documentation; the game still uses the standard library.

```sh
# Use a development Python environment with Pillow installed.
python tools/build_sprite_reference.py
```

The generator verifies that all 664 original images are represented, their original SHA-256 hashes remain unchanged, IDs are unique, crops stay within image bounds, every sprite has an unused/mapping status and future idea, all expected Urizen slots are present, and the prompt sheets have zero uncovered nontransparent pixels. [coverage.json](reference/coverage.json) records the resulting counts.

## Sources and credits

Publisher/source references are recorded in [CREDITS.txt](CREDITS.txt) and [catalog.json](catalog.json): [pixel-boy's Godot icons](https://pixel-boy.itch.io/icon-godot-node), [Hyohnoo's input prompts](https://hyohnoo.itch.io/keyboard-controller-keys), and [vurmux's Urizen artwork](https://vurmux.itch.io/urizen-onebit-tileset). Historical Urizen tile metadata is pinned to revision `28b1e60cf8fa2f15622537f6df0290e2ac890d5f`; the [tile-data directory's own license](https://github.com/vurmux/urizen/blob/28b1e60cf8fa2f15622537f6df0290e2ac890d5f/urizen/data/tilesets/urizen-onebit-basic/LICENSE) is CC0. This reference imports tile labels and properties, not the dungeon-generation library's code.
