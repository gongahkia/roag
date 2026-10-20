# Artwork catalog: schema 5, coverage and regeneration

[catalog.json](../catalog.json) is the canonical, game-independent artwork catalog. Schema 5 adds `sprites[].asset`: descriptions, subjects, generic types, suggested layers, composition, evidence and measured foreground palettes for every region. Current Ithica casting and retained preview bindings remain separate fields. Source pixels, original IDs, crop rectangles, hashes and publisher-identity confidence are preserved. This guide retains its historical filename for existing links.

## Coverage

| Scope | Count |
| --- | ---: |
| Original PNGs / ZIP archives | 664 / 3 |
| Total indexed regions | 11,887 |
| Occupied sprite regions | 7,115 |
| Non-gameplay slots, separators and annotations | 4,772 |
| Ithica-selected IDs, including shared font/UI | 166 |
| Colony-selected IDs, including font/UI | 154 |
| Current field aliases / distinct IDs | 14 / 14 |
| Retained preview-selected IDs, including font | 191 |
| Preview-only IDs, outside current Ithica selection | 75 |
| Union of current and preview selections | 241 |
| Occupied regions unbound by either selection | 6,874 |

Occupied regions comprise **5,676 Urizen cells**, **653 Godot icon variants**, and **786 input-prompt regions**. Counts refer to crops/variants, including repeated art and fragments, rather than guaranteed unique standalone objects. Registering all 7,115 crops in the atlas does not make them gameplay bindings.

## Schema and compatibility

| Field | Meaning |
| --- | --- |
| `schema_version` | `5` |
| `images`, `archives`, `packs`, `summary` | Original inventory and provenance retained |
| `catalog_summary` | Coverage and selection counts, including current Ithica and preview-only counts |
| `game_bindings` | Preserved colony and RPG-preview alias maps; their keys retain compatibility meaning |
| `ithica_casting` | Authored current lunar casting source, limits and generator ownership |
| `sprites` | All 11,887 original region records, enriched without changing their source identity |
| `asset_taxonomy` | Generic types, layer suitability, color vocabulary and evidence/measurement rules |
| `generation_rules` | Retained preview/intermediate-generation rule metadata |
| `catalog_limits` | Identification, animation, assembly and review limits |

Schema 5 is additive to schema 4. Use `sprites[].asset` to reason about artwork independently of game mechanics. Existing `usage.colony`, `usage.rpg` and `ithica` are retained; `rpg` refers to the preview selection. Use `sprites[].ithica` for current game usage. The original import audit remains frozen in [sprites.json](../sprites.json).

## Generic artwork metadata and queries

[asset.schema.json](asset.schema.json) describes the version-1 `asset` object. [asset-semantics.json](asset-semantics.json) is the authored input for neutral visual descriptions; [asset_catalog.py](../../tools/asset_catalog.py) combines it with preserved source identities and pixel measurements. Subject claims carry their own confidence, basis and evidence reference. A visually similar figure is not silently upgraded to a confirmed goblin or a profession.

Every crop has a generic type, layer suitability, composition status, exact foreground RGBA palette, color family coverage, primary color and foreground bounds. Color measurements exclude alpha-zero and near-black pixels (all RGB channels at most 8), matching the frozen import's background rule. Color family names use documented deterministic HSV buckets; exact hex/alpha/pixel counts remain available. Layer suitability is a generic suggestion, not an implemented render layer or collision rule. Named variants and exact pixel duplicates retain all source IDs. Input variant groups distinguish keyboard keys from controller buttons with the same letter. `asset.relations.input_prompt` retains visible control/state/size labels; bright/dim does not mean pressed/released. `asset.relations.glyph` records original font families/labels and unambiguous characters/code points, using the same punctuation table as runtime font selection. The unidentified rainbow edge glyph has no asserted code point. Adjacency, animation and direction sets remain unconfirmed until separately evidenced.

Schema 5 measures and classifies all records. Visual inspection covers every occupied crop, with unresolved identities and series scope retained. The current semantic input contains 36 body-series descriptions, 98 held-prop/stance descriptions and 1,704 individual observations, including the preserved 104-crop review, all 125 occupied two-bit crops, and every uncertain Urizen crop outside the main character series. Fifty-four inspected sheets cover all 7,115 occupied crops, including every Urizen, Godot icon and input prompt region. Exceptional figures use individual descriptions rather than inheriting nearby tool/stance labels. Series descriptions are compositional observations, not individually confirmed species or professions. `asset.evidence.review_status`, `observations` and `inspection_refs` distinguish category-only coverage, publisher identity, visible controls, silhouette support, series observations and individual descriptions.

Individual neutral names also supply conservative subject terms. Explicit “-like” or disjunctive names produce low-confidence candidate terms rather than exact identities; separately authored claims retain their declared confidence. Nouns are taken from the positive name, never from a negated uncertainty statement. Primary types describe the artwork's broad grouping: portable objects such as books, jewelry and syringes consistently use `item`, while suggested layers still allow object or interface use. These labels do not implement equipping, crafting or collision.

[urizen-silhouette-matches.json](urizen-silhouette-matches.json) records exact binary foreground-mask comparisons against the preserved author's historical tilesets at revision `28b1e60cf8fa2f15622537f6df0290e2ac890d5f`. It retains URLs, downloaded-file hashes, mask hashes and every alternative publisher label. There are 1,095 matching crops, including 81 whose original identities were not high confidence. Colors are deliberately ignored for this comparison; only terms shared by all matches become medium-confidence `publisher_silhouette_match` claims. Original identity/confidence fields are never upgraded. Shape equality alone cannot confirm a recolored sprite's publisher identity.

Coordinate review sheets are reproducible with `uv run --with pillow python tools/make_asset_review.py`. The generated manifest covers every one of the 7,115 occupied crops; its existence is not evidence that a page was inspected. Only pages explicitly recorded in the semantic input count as inspected. Original pixels are displayed at nearest-neighbor scale with coordinates. The optional historical comparison tool is `uv run --with pillow python tools/match_upstream_silhouettes.py --download`; downloading is explicit, cached under ignored `artifacts/`, and never required for normal catalog regeneration.

The full generic-catalog work remains active. Art coverage now includes explicit descriptions for uncertain motifs, 137 neutral icon-family descriptions, 786 structured prompt labels/states/sizes, and characters/code points for 146 publisher-labelled glyphs. The offline atlas now exposes those generic queries and exact exports; both native viewers share the same artwork/query model and final acceptance is being audited. The final checks must exercise canonical JSON and viewer agreement; type/subject/color/layer/composition queries with confidence; original-file, ID, crop and evidence preservation; deterministic regeneration; actual browser interaction/exports; and the native atlas. Game-specific roles must remain optional relationships rather than the basis of neutral classification. Each checkpoint records its actual scope in VERIFICATION.md and preserves a separate rollback commit.

Agent-friendly queries use the same catalog and exclude game-specific interpretations by default:

```sh
python3 tools/query_assets.py --subject ghost --subject-confidence high --format ids
python3 tools/query_assets.py --type actor --color green --sort color --limit 10
python3 tools/query_assets.py --type fragment --color cyan --facets
python3 tools/query_assets.py --search battery --format csv --output /tmp/batteries.csv
```

`--color` requires at least 5% foreground coverage by default; use `--min-color-coverage 0` to include any measured accent. `--primary-color` selects the dominant family. `--hex` selects an exact foreground RGB value. Repeat `--subject` to require multiple subject terms, and use `--subject-confidence` to set their minimum confidence. `--layer`, `--composition`, sorting and JSON/CSV/ID output are supported. `--limit 0` exports all matches; `--include-non-assets` explicitly includes blank/guide/annotation regions. Neutral JSON exports retain kind, crop hash and original identity evidence alongside `asset`; CSV includes per-subject confidence/evidence and exact color coverage as JSON-valued columns. `--full` includes all preserved and game-specific fields when needed.

Each record preserves source, crop, original identity/confidence, exact-match evidence, visual observations, duplicate/fragment metadata and import-time proposals. It also carries:

| Per-record field | Meaning |
| --- | --- |
| `ithica.status` | `bound`, `preview_only`, `unused`, or `not_applicable` |
| `ithica.usage.colony`, `.expedition` | Context-specific alias lists; `font:X` means a bitmap glyph |
| `ithica.roles`, `.limits`, `.generation` | Current lunar interpretation, adaptation limits and placement category |
| `ithica.future_ideas` | Lunar possibilities for occupied crops, including unused and preview-only art |
| `ithica.evidence` | Explicit separation between game casting and publisher identity/review |
| `usage`, `runtime_status` | Historical combined-selection view retained for compatibility |
| `assessment` | Broad family/layer, retained RPG possibilities, ideas and adaptation limits |
| `generation` | Rule references, candidate status, adjacency basis and optional focused visual review |
| `review` | Identity evidence/confidence and the actual extent of visual inspection |

Every occupied crop has future-use proposals; non-gameplay regions remain marked as such. The new lunar proposals use evidence-scoped family/category rules and existing observations. **This is exhaustive per-entry coverage, not a new manual identification of all 7,115 motifs.**

## Offline artwork atlas

Open [atlas.html](../atlas.html) directly as a file. It embeds the original images and metadata and makes no network requests. The main controls filter by pack, generic type, color, exact subject terms with minimum claim confidence, suggested layer and composition. Text search requires all space-separated words; comma-separated subject terms must all match. Sort by ID, name, type, primary color, dimensions or visual confidence, with descending order available.

Advanced controls offer minimum foreground color coverage (5% by default), primary-color-only matching, exact foreground RGB, original source identity confidence and optional game relationships/search. Clicking a palette swatch applies exact RGB. Game casting/proposals start folded and stay out of neutral text search unless explicitly enabled. Source identity confidence and per-subject confidence are separate.

Selection URLs preserve filters, sort, page and stable ID. Each sprite exports neutral metadata with source identity/crop hashes and an original crop PNG; filtered selections export all matching records, not merely the visible page. Downloads preserve every crop's frozen RGBA, including invisible RGB and partial transparency that browser canvas round trips can normalize. Whole-image icons retain the original PNG bytes. The generator encodes cropped-sheet PNG downloads with Python's standard library; original files are never rewritten.

The scene lab retains exact-ID layer recipes and rendered PNG export. Palette tinting only changes the preview; crop downloads retain source pixels. An empty query clears the selection and disables its exports.

Optional real-browser verification is separate from standard-library regeneration:

```sh
uv run --with playwright==1.64.0 playwright install chromium
uv run --with playwright==1.64.0 python tools/verify_asset_atlas.py
```

An existing Chromium can be selected with `--browser /path/to/chromium`. Reports, downloads and desktop/mobile screenshots go to ignored `artifacts/atlas-verification/`. The persistent generic unit suite also decodes every embedded crop PNG and verifies all 7,115 frozen RGBA hashes. Linux CI configures both that suite and the browser check; actual executed results are recorded in [VERIFICATION.md](../../docs/VERIFICATION.md).

## Native artwork atlas

F2 opens the native atlas in either the normal game or retained preview. It uses the same `asset` descriptors as the offline atlas. F3 cycles pack, F6/Tab type, F8 measured color, F10 sort and F7 optional game usage. Shift reverses cycling. The second row also cycles suggested layer and composition. F4 only changes the preview palette, F9 resets filters, and F1 opens query help. Arrows/pages browse; the wheel over the detail panel scrolls its evidence. Esc clears typed text, then closes.

Free words are ANDed over neutral metadata. Typed filters include:

```text
subject:ghost confidence:high
color:green type:actor
layer:object composition:fragment_candidate
hex:#feffff
primary:blue sort:name desc:true
type:input_prompt color:white coverage:0.3
source:high
```

`confidence:` sets minimum subject-claim confidence; `source:` and `visual:` match original identity and visual classification confidence separately. Repeated subjects/colors require every term; comma lists work as well. The color control uses 5% foreground coverage; typed `coverage:` uses a 0–1 fraction. A typed `sort:` takes precedence over the sort button. `game:true` or the Game notes control opts into casting/proposal text and details. Invalid color/hex/confidence/coverage syntax shows an error with no results. Filtering changes no campaign state.

Both native readers use [ArtworkCatalog.cs](../../src/Ithica.Client/ArtworkCatalog.cs). Their graphical smoke reports include full ID/order results for representative queries. Compare executed reports with the canonical JSON using:

```sh
python3 tools/verify_native_atlas.py artifacts/native-atlas/current.json artifacts/native-atlas/preview.json
```

This verifies actual native results against the independent Python query path, including all IDs, colors, subjects/confidence, fragments/layers/composition, exact RGB, source confidence, optional game notes and invalid queries. Reports are ignored developer artifacts, not player saves.

## Current lunar use and retained preview use

[ithica-sprites.md](ithica-sprites.md) lists all 14 current field aliases and their limits. [ithica-casting.json](../ithica-casting.json) is the authored source. Examples:

- The boulder crop is blocking surface rubble in Ithica; the legacy preview can use the same crop as nonblocking scenery. Collision belongs to the map's explicit ground/object data.
- A crystal crop marks actual recoverable components. Rewards enter carried cargo and reach home inventory on return.
- A leaf-like crop is a symbolic interference/exposure overlay; it does not establish outdoor lunar vegetation or supernatural lore.
- Resident and unidentified figures retain their existing source evidence. Field roles, suit protection and enemy behavior are game data.
- Ordinary vegetation belongs only in a suitable enclosed habitat. Ordinary spreading fire requires oxygen; lunar sites do not inherit the fantasy preview's outdoor fire or spores.

[rpg-bindings.md](rpg-bindings.md) retains the full preview binding inventory. Fantasy companions, monsters and scenery there are identified as preview casting. Their descriptions do not create lunar mechanics or proper names.

The generator preserves authored WFC sockets on 7×7 room chunks, CA terrain rules and global route/quest checks. Its intermediate/preview rules reference **43 IDs**. The current lunar generator recasts terrain, objects and constraints explicitly; atlas indices never imply sockets, animation or collision.

[urizen-generation-review.json](urizen-generation-review.json) retains **104 individually inspected crop observations**, shown in [urizen-generation.md](urizen-generation.md). Observations do not upgrade uncertain publisher identities. Current lunar roles are listed separately from those retained preview decisions.

## Authoring and derived files

| Task | File |
| --- | --- |
| Complete source of truth | [catalog.json](../catalog.json), `sprites[].asset` plus preserved source evidence; game usage is separate |
| Generic visual authoring / contract | [asset-semantics.json](asset-semantics.json) / [asset.schema.json](asset.schema.json) |
| Historical silhouette evidence | [urizen-silhouette-matches.json](urizen-silhouette-matches.json); original RGB identity remains separate |
| Current lunar casting and family ideas | [ithica-casting.json](../ithica-casting.json) |
| Field/preview alias-to-ID selection | [ithica-expedition-bindings.json](../ithica-expedition-bindings.json) |
| Preview/intermediate generation rules | [ithica-generation.json](../ithica-generation.json) |
| Colony selection | [ithica-colony-sprites.json](../ithica-colony-sprites.json), generated by the colony builder |
| Generated field/viewer manifest | [ithica-expedition-sprites.json](../ithica-expedition-sprites.json) |
| Current field casting table | [ithica-sprites.md](ithica-sprites.md) |
| Native viewer metadata / offline browser | [atlas-index.json](../atlas-index.json) / [atlas.html](../atlas.html) |
| Frozen import audit / original browser | [sprites.json](../sprites.json) / [sprites.html](../sprites.html) |

Colony and field aliases are separate contexts and can intentionally refer to different crops, such as `wall`. Combining dictionaries by overwrite would corrupt those mappings. Original publisher filenames and credits remain intact during the Ithica rebrand.

The native viewer loads source textures lazily. The offline atlas embeds original PNG bytes for file-based inspection and scene-lab export without networking. Exported recipes contain exact IDs by layer; runtime recipe import is not implemented.

## Regenerate and verify

Python 3 standard library only; Python is not a game runtime dependency:

```sh
python3 tools/build_runtime_assets.py
python3 tools/build_game_catalog.py
python3 tools/build_game_catalog.py --check
```

The catalog builder reads the frozen audit, original inventory retained in the existing catalog, neutral visual authoring, original PNG pixels, colony manifest, field/preview bindings, current casting, generation rules, focused review and HTML template. It deterministically regenerates schema 5, the viewer index, field manifest, Markdown tables and offline HTML. **Keep the existing catalog's original inventory; it is also an input.** `--check` verifies freshness and all 664 original PNG / 3 archive hashes. The standard-library decoder also verifies every crop against its frozen RGBA hash. CI runs the freshness check on Linux. Run `python3 -m unittest discover -s tests/catalog -v` for the neutral metadata, pixel decoder and query contracts.

Edit authoring inputs, then regenerate derived files. Do not hand-edit generated records/tables or transfer coordinates blindly after an intentional sheet change. The optional original-audit development tool uses Pillow; its use would require a deliberate audit migration. No generator recolors or repacks source PNGs.
