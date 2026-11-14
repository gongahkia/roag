# World-builder asset and rendering audit

The existing art supports a **small, abstract settlement map** immediately at the
asset level. Urizen provides 12×12 cottage miniatures, road junctions, trees,
crops and ground textures. It does **not** currently provide a coherent
Townscaper-like kit of volumetric houses, connected roofs and construction stages.
The strongest first scene is a few cottages fronting an existing lane, with
local road changes and persistent dirt pads recording previous placements.
There is no controlled character in this recommendation.

This is an offline audit and static assembly check. No new game, simulation,
asset migration, runtime IDs or artwork were implemented. No source artwork was
moved, renamed, repaired, replaced or deduplicated. Repository baseline:
`d4efcd3c`, clean working tree at the start. README.md was never edited.

## Start with the evidence

- [Static assembly: existing lane → cottages/plot → one cottage removed](assembly-test.png).
  Both native 1× and integer 4× views are shown. [Every cell and layer](assembly-layout.json)
  identifies the original crop. Background, labels, coordinates, black-key
  compositing and tile choices are explicitly separate from artwork.
- [Miniature cottages, modern buildings and modular pieces](contact-sheets/buildings-2.png).
- [Roads, bends, junctions and bridges](contact-sheets/routes-2.png).
- [Trees, crops, minerals and dead vegetation](contact-sheets/nature.png).
- [Ground, rivers and textures](contact-sheets/terrain-2.png).
- [Kenney modular architecture components](contact-sheets/kenney-components-2.png).
- [Static inhabitants / profession variants](contact-sheets/inhabitants-1.png).
- [UI icons](contact-sheets/ui.png), [input prompts](contact-sheets/input-prompts.png),
  [fonts](contact-sheets/fonts.png), [effects/marks](contact-sheets/effects.png).
- All **28 labelled contact pages** and their audit IDs are in
  [sheet-index.json](sheet-index.json). Source overviews:
  [Urizen native](contact-sheets/urizen-source-native.png),
  [Kenney RPG 2×](contact-sheets/kenney-rpg-source-2x.png),
  [Tiny Dungeon 4×](contact-sheets/tiny-dungeon-source-4x.png).

Each contact card shows the exact crop at native resolution and a separate
integer nearest-neighbour enlargement, on dark/light alpha backings. Crops retain
original colours. A native dark drawing can genuinely be hard to read: the
sheets do not brighten or repaint it. Long prompt IDs use a deterministic
`WB-…` label alias; each frame's `label_id` and sheet index resolve it to the
complete original catalogue ID. `LIVE` means selected by the current resolver,
not that the drawing is suitable for a colony.

The three whole-sheet overviews retain the source pixels at the stated integer
scale, with a caption added above. [overview-index.json](overview-index.json)
records their source paths and display origins. These are useful where a 12px
record is only one piece of a larger, still-uncertain drawing.

## What actually exists

The comprehensive machine-readable inventory is [inventory.json.gz](inventory.json.gz),
ordinary gzip-compressed JSON following the repository's existing catalogue
convention. [inventory-index.json](inventory-index.json) exposes source files,
summary, generated visual recipes, missing references and provenance without
loading every frame. [Suitability groups](suitability-groups.json) scope the
reuse judgements. The manifest does not change the source catalogue.

| Material | Verified local extent | Important distinction |
| --- | --- | --- |
| Urizen main sheet | 2679×651 RGBA; 206×50 physical 12px cells; 5,551 occupied regions | 1px origin and 13px pitch; 200 guide cells and 4,549 blank cells are not sprites |
| Urizen free 2-bit showcase | 261×92 RGBA; 20×7 physical cells; 125 occupied regions | A limited second source, not a full replacement for the main sheet |
| Godot node icons | 653 PNG files, six visual families | 648 are 16×16 and five reset variants are 10×12; UI artwork, not world tiles |
| Input prompts | Nine 256×256 or 320×320 sheets; 786 occupied crops plus eight credit/annotation crops | Nonuniform rectangles, including wide keys; not a guessed uniform grid |
| Retained Kenney RPG review sheet | 968×526; 57×31 cells at 16px, 17px pitch, origin 0; 1,704 occupied cells | Present under docs, omitted from the canonical catalogue; many cells are components |
| Retained Tiny Dungeon review sheet | 203×186; 12×11 cells at the same 16/17 layout; all 132 cells occupied | A real source sheet under docs, not a screenshot; dungeon pieces and static figures |
| Native UI fonts | Two original font files and a 384×864 generated glyph atlas | Six font/size faces, 570 glyph metadata records; three are zero-area whitespace metrics |

The original catalogue has **11,887 regions / 7,115 occupied crops in 664 source
PNGs**. There are **7,073 distinct occupied RGBA crops** within that catalogue,
not 7,073 independent illustrated subjects. Exact duplicates, colour/style
variants, multi-cell fragments and repeated profession figures remain separate
records with relationships. They are not deduplicated or inflated by alias count.
The extended audit records **14,356 crop/glyph entries** and **669 source records**:
664 imported images, two retained review sheets, one derived font atlas and two
vector-font sources. It separately records 52 reference/derived image files.

For every crop the manifest records its source, native size, verified rectangle,
pixel hash, original ID, current roles, historical aliases, activity, available
variant/identity evidence, local provenance and contact-page references. All
11,887 original crops match the original recorded RGBA hashes. Extra review
crops use the grids documented locally; no boundaries were invented for unknown
reference screenshots. Zero-area font records are metrics, not missing art.

**No time-ordered source animation sequence was verified.** The catalogue itself
marks all region animation claims unconfirmed. Character matrices contain body,
colour, held-tool and profession variants, not a proven walk/build cycle.
Input bright/dim/pressed states and variant drawings are useful states, but do
not supply a frame order or duration. Current Adventurer idle/attack/hurt roles,
for example, all resolve to `urizen-onebit-r00-c107`; visible action motion is
procedural. Runtime movement, sparks, rings and sprite breakup are documented as
generated recipes, not counted as original sprite frames.

## Catalogue inaccuracies and gaps

1. **Current use is not the historical catalogue projection.**
   `assets/classic-bindings.json` and `src/classic_sprite_data.lua` are the active
   casting: **599 semantic roles → 237 source IDs → 67 image files**. The 362 extra
   roles are reuse, not extra drawings. The catalogue's `usage.rpg` projection
   names 319 IDs, only 180 overlapping the current 237; 57 current IDs are absent
   from it. Fifty-two currently used IDs still have the frozen import-time
   `runtime_status: unused`. Those fields need scope tags, not blind deletion.
2. **An alias does not identify the pixels.** Historical colony `solar` points
   to a glass wall (`urizen-onebit-r00-c064`); `vacuum` points to sand
   (`r05-c002`); `ice` points to a crystal (`r22-c003`); `oxygen` points to an
   uncertain machine part (`r35-c058`). They are game interpretations, not found
   solar panels, vacuum art, ice terrain or a complete oxygen plant. Godot
   `tree` is an editor hierarchy/tree symbol, not botanical world vegetation.
3. **Retired references remain, but current required sources are present.**
   Literal references point to five absent Kenney paths: `1-bit-pack/symbols.png`,
   `micro-roguelike/tiles.png`, `caves-dungeons/terrain.png`,
   `approved/tiny-dungeon.png` and `approved/roguelike-rpg.png` below
   `assets/kenney/`. Their legacy loaders/audit tests cannot load those files.
   The current game uses `CatalogVisuals`; importing the old `kenney_terrain`
   module for its pure canonical-mask geometry does not load its missing PNG.
   The two full review sheets still exist under `docs/sprite-audit/review-art/`.
   Their README's claim that approved copies are currently loaded is stale.
   `assets/unknown.png` is a deliberately missing test fixture, not missing art.
4. **Documentation and index scope drift.** `assets/SPRITE_GUIDE.md` links
   `catalog.json`, while the actual canonical file is `catalog.json.gz`.
   The main catalogue omits the two retained source review sheets and the newer
   rendered dice evidence from its media accounting. Screenshots/previews are
   reference evidence, not a second supply of independent source sprites.
5. **Classification is often broader than a usable asset.** “Building”,
   “multistory”, “effect” and “fragment” do not establish footprint, pivot,
   connection ports or animation. A 12px house pictogram, an elevation facade,
   a roof corner and a building-shaped UI icon need different categories.
   Every original crop is in bounds; the uncertainty is often meaning and
   assembly, rather than a corrupt rectangle.

The inventory retains all 81 literal missing-reference occurrences, grouped by
file/line, rather than silently redirecting them. No missing art or obsolete test
was repaired. Local provenance is recorded without a new online licence claim:
`assets/CREDITS.txt` records CC0 declarations for Urizen, Godot icons and input
prompts. The three retained ZIPs contain no licence files. The Kenney review
sheets have local CC0 licence texts alongside them. BigBlue TerminalPlus has a
local CC BY-SA 4.0 notice; Source Code Pro has an SIL OFL notice. Reference logos
and screenshots whose independent reuse rights are unclear remain reference-only.

## Suitability for placement-driven world building

These classifications concern specific representations, not complete systems.
An art asset can be ready while its placement rule still needs implementation.

| Function | Assessment | Evidence and limits |
| --- | --- | --- |
| Ground / water / shore | **Ready** ground; **composition** for boundaries | Dark Urizen grass/dirt/stone textures and named river/ocean variants. Kenney has larger modular terrain sets. Neither source index is an automatic adjacency bitmask. |
| Vegetation / resources | **Ready** as static objects | Trees, shrubs, mushrooms, crops, crystals and geodes are visible. Species/colour variants are not verified growth stages; resource depletion needs an explicit representation. |
| Paths / roads / bridges | **Composition** | Urizen road H/V, four elbows, four T joins and cross; bridges H/V; rail/street families add further connections. No isolated or single-ended tile was verified in the 13-cell dirt-road family. |
| Buildings | **Ready** as miniature map symbols | `r33-c000`/`001` cottages and nearby granary/farm/warehouse miniatures read as a settlement at 12px. These are front/elevation-like pictograms, not complete overhead roofs at room scale. |
| Walls / roofs / doors / foundations | **Composition / adaptable** | Urizen wall and door symbols, and Kenney's 16px modular architectural pieces, can be assembled. Footprints, anchors and joins are not authored as complete building prefabs. Mixing 12px Urizen and 16px Kenney needs a deliberate scale/palette decision. |
| Construction / development stages | **Additional art needed** for literal stages | No matched foundation → scaffold → completed version of the same cottage was verified. Dirt pads can symbolise disturbed land; do not call different houses construction frames. |
| People / animals | **Ready** as static ambience; **uncertain** as animation sets | Existing humanoids, dogs, cats and other animals can decorate the map. No complete worker walk/build cycle was established, and the first scene needs no workers or controlled character. |
| Props / storage / signs | **Ready / composition** | Barrels, beds, containers, signs, well/anvil and fences exist. Some large machinery is split into uncertain cell fragments; a “warehouse” icon is not a storage-interior kit. |
| Ruins / abandonment / history | **Composition** | Crumbled brick `r03-c006`, dead tree `r10-c005`, graves and dirt textures exist. No matching ruined state for every cottage was found. Persistent pads/access stubs are the safest first trace. |
| Placement / environmental effects | **Adaptable** rendering; **uncertain** sprite sequences | Existing outlines, alpha ghosts, flashes and rings can acknowledge placement. Static magic/blood/electric motifs are not a verified environmental animation cycle and need not be imported into this concept. |
| UI / fonts | **Ready** | Hammer, hand, grid, magnifier and clear/delete icons; keyboard/controller prompts; two licensed fonts and their verified glyph cache. |
| Sculptural / isometric settlement | **Additional art needed** | No coherent joined 3D roof/storey/side-wall kit was found. A convincing Townscaper-like massing scene is not supported by simply scaling these symbols. |

The inspected art therefore best supports an **abstract overhead board populated
with small elevation-like landmarks**. Kenney can support a more literal
room-scale top-down composition, but its building assemblies need authoring and
are a different visual family. Neither observation fixes the game's setting.
The static hamlet is one small coherent recommendation, not a commitment to a
rural economy or a generation algorithm.

## What the existing renderer can reuse

- `main.lua:load_native_visuals` and `src/catalog_visuals.lua` load verified exact
  crops, use nearest filtering and share source textures/aliases. The active
  Urizen object path removes near-black RGB (`<=8`) and turns remaining pixels
  into a white alpha mask for semantic tinting. **The contact sheets show native
  colours; the current game does not preserve those object colours by default.**
  The assembly test uses a specifically identified colour-preserving black-key
  compositing adaptation; no source bytes changed. Native grass/road art is dark.
- Current ground surfaces are generated opaque/dimmed copies with derived edges.
  `CatalogVisuals:terrain` canonicalises 8-neighbour masks into 47 cases and
  caches them. These are procedural borders over a source crop, **not 47 authored
  neighbouring atlas drawings**. A new road-connection lookup would still need
  explicit named ports, not an inferred row-to-mask conversion.
- Ground, environmental details, objects, actors and overlays already have
  separate draw passes. `draw_visual` supports alpha, scaling and facing flips;
  camera tile/screen conversion and clipping can be reused without a controlled
  character. The current camera follows a supplied point, so a builder would
  supply its own focus. This is capability, not an implemented builder camera.
- Existing top/bottom vault/gate drawing demonstrates manual multi-cell
  composition. There is no general building-prefab placement/ownership/footprint
  editor to assume already exists. Each new composition needs explicit offsets,
  layering, anchor and collision/occupancy rules.
- `draw_preview` demonstrates legal-target outlines and ghosts; it currently
  validates combat commands, not building placement. `visual_juice.lua` provides
  cosmetic motion, rings and sprite breakup without changing simulation state.
  These are reusable presentation treatments, not a new colony simulation.

## Proposed catalogue organisation — no migration applied

[The machine-readable taxonomy proposal](taxonomy-proposal.json) separates six
layers: **source file → frame → appearance variant → semantic binding → assembly
→ verified animation sequence**. Preserve original paths, hashes and legacy IDs.
Keep gameplay aliases in scoped binding maps rather than renaming the drawing.

Use a small set of world categories: terrain/shore, vegetation/resource,
road/bridge/rail, miniature building, modular architecture, prop/storage/sign,
visible history, static inhabitant and placement effect. Keep UI icons, input
prompts and typography outside world architecture. Existing publisher identity,
rectangles, measured palette and confidence fields are useful; inherited
colony/RPG casting remains useful as explicitly historical context.

Add `view`, native pixel scale, representational scale, `whole/component/fragment`,
anchor, logical footprint, layer and connection-port tags. Only author ports or
assembly offsets after checking the actual joins. Add construction/growth/ruin
relationships as **unknown** until there is evidence for the same subject's
states. Animation must name verified ordered frames and timing; do not derive it
from contiguous cells, aliases or colour rows. Distinguish current, historical,
unbound and missing-reference activity. Pixel/mask equivalence should support
search and variant grouping, not physical source deduplication.

## One recommended first scene: a lane with remembered cottage lots

Build only a tiny placement test: an 11×7 miniature map with an existing
horizontal lane. Let the player place a cottage in one of three north-side lots
or a small crop plot south of the lane, and remove a cottage. No worker system,
controlled avatar, economy, technology tree or generation framework is needed.

All nine exact artwork IDs used in [the assembly](assembly-test.png) come from
`assets/urizen/urizen_onebit_tileset__v2d0.png`:

| Exact audit/catalogue ID | Rectangle `[x,y,w,h]` | Use |
| --- | --- | --- |
| `urizen-onebit-r05-c015` | `[196,66,12,12]` | Grass ground |
| `urizen-onebit-r05-c016` | `[209,66,12,12]` | Second grass texture, not a growth stage |
| `urizen-onebit-r05-c001` | `[14,66,12,12]` | Persistent dirt pad / disturbed land |
| `urizen-onebit-r11-c002` | `[27,144,12,12]` | Existing horizontal road |
| `urizen-onebit-r11-c011` | `[144,144,12,12]` | N/E/W access junction beneath a placed cottage |
| `urizen-onebit-r08-c001` | `[14,105,12,12]` | Static oak-like tree decoration |
| `urizen-onebit-r33-c000` | `[1,430,12,12]` | Cottage miniature |
| `urizen-onebit-r33-c001` | `[14,430,12,12]` | Alternate cottage drawing, not an upgrade stage |
| `urizen-onebit-r24-c005` | `[66,313,12,12]` | Vegetable plot |

A cottage placement replaces its lot's grass with a dirt pad and changes the
road immediately south from H to N/E/W. The cottage graphic faces the lane.
After removal, keep the pad and access junction: earlier actions remain in the
composition. Crop plots overlay dirt using the existing vegetable glyph. These
are proposed local reactions; the picture does not execute them. The lane runs
off both board edges, avoiding an invented road endcap. The source road family's
missing isolated/end states can be deferred without changing this small test.

**Additional artwork required for this miniature first scene: none.** Rendering
work would include ordinary layering, colour-preserving black-key/tint choice,
local junction selection, stable placement previews and a small footprint rule.
If the aim becomes physically joined buildings, arbitrary free road ends or
literal staged construction, those are real art/assembly gaps rather than assets
already supplied by this audit. Do not scale the miniature claim into a full
Townscaper-like building kit.

## Verification and preservation

```sh
python3 -B docs/world-builder/verify_inventory.py
python3 -B tools/build_classic_assets.py --check
```

The audit verifier checks **669 sources, 14,356 exact crops, 28 pages with 877
labelled appearances**, source and cropped RGBA hashes, in-bounds/disjoint cards,
label-to-ID agreement, original opaque pixels at native and integer-enlarged
sizes, all nine assembly IDs and README hash. No incorrect original crop was
found. Long prompt labels initially overlapped; they were corrected to stable
short label aliases and the corrected sheets were inspected. Whole-sheet and
selected detailed views were inspected alongside a montage of the remaining
pages. This is not a fresh manual species/object identification of every cell.

The existing catalogue test command was also run before the full audit output:
`python3 -B -m unittest discover -s tests/catalog -v` produced **47 passes and
one failure**. The media-accounting assertion omits the already existing dice
PNG/GIF evidence. [The captured baseline log](catalog-tests-baseline.log) records
that failure; it was not repaired. The active asset check passes **599 bindings
from 67 unchanged PNGs**. No broad gameplay regression run or live game launch
was needed for this offline audit.

[Preservation evidence](preservation.json) records initial/final README equality,
source-asset hashes, unchanged code/catalogue files and normal-player-storage
fingerprints. All new deliverables are under `docs/world-builder/`; temporary
render helpers lived under `/tmp`. No downloads, replacement art, source
reorganisation or normal-storage writes were performed.

Initial and final README.md SHA-256:

```text
3b012aeb7f8e0ea8806989b96c9cb694831d714c24d84afb075c1109eb44b40a
```
