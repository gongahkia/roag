# Goal 08 · Verified living-world artwork review

The retained Urizen sheet supports two visibly distinct fantasy peoples alongside
humans: the publisher-labelled **imp** and **genie** drawings. This goal casts them
as peaceful **Imps** and **Djinn**, without importing enemy AI or combat. A figure
carrying a bag supports a travelling merchant; a hooded staff-bearing figure
supports a wandering storyteller. These visitor professions are game casting,
not verified author labels. Existing human drawings and colours remain intact.

The working baseline is `474a32a7c1593224a40a0accfc28399298a0b2a6` on main.
Its settlement application and Pages workflow match `release/settlement-goal-07`.
The original audit's current-use count is still correct: **599 semantic roles,
237 catalogue IDs, 67 source PNGs**, verified from `assets/classic-bindings.json`
and the inventory's source mappings. Historical aliases are not used as evidence
that a drawing depicts a new species or has animation frames.

## Visual inspection and evidence

Reviewed the actual 2679×651 Urizen source, the existing labelled inhabitants,
nature, props, terrain/routes and retained Tiny Dungeon sheets, the original
asset audit, publisher-match metadata and local credits. The additional
[40-crop contact sheet](evidence/goal-08/sprite-review.png) examines **all 29 neutral
figure rows** plus visiting, vegetation, animal and camp/prop candidates at 1×
and 6×. Its [HTML source](evidence/goal-08/sprite-review.html) and
[recorded crop metadata](evidence/goal-08/review-crops.json) are reproducible with:

```sh
python3 -B apps/settlement/tool/goal08_asset_review.py
```

Each rectangle comes from the existing inventory; source pixels are shown without
recolouring. Black-key masking and semantic palettes are separate runtime
adaptations. Original source/catalogue bytes are untouched. The original audit's
11,887 catalogue regions are not 11,887 independently usable characters; many
are aliases, colour/profession variations, fragments, blank cells or interfaces.

## Selected content: source, interpretation and implementation

All entries below are **12×12**, from the unchanged
`assets/urizen/urizen_onebit_tileset__v2d0.png`. They fit the current 12-pixel
miniature buildings and nine-world-pixel inhabitants. No source walk/seasonal
animation sequence was verified. Motion remains the existing procedural travel,
facing, lift and gestures; static variants are not presented as animation frames.

| Role | Source ID / exact rectangle | What the image supports | Confidence / adaptation |
| --- | --- | --- | --- |
| Existing humans | `r00–03-c104`, `[1353,1/14/27/40,12,12]` | Four existing humanoid outlines, hair/hood/cap variants | Existing approved cast; same pixels/palettes and identity assignment retained |
| Imp resident | `urizen-onebit-r20-c104`, `[1353,261,12,12]` | Horned, compact figure with a protruding lower/tail silhouette | Publisher match **enemy / imp**, high confidence; peaceful resident is new semantic casting |
| Djinn resident | `urizen-onebit-r18-c104`, `[1353,235,12,12]` | Broad crest/head and distinctly curled/tapered lower outline | Publisher match **enemy / genie**, high confidence; peaceful Djinn is new casting |
| Merchant visitor | `urizen-onebit-r00-c198`, `[2575,1,12,12]` | Humanoid with a large bag-like side prop | Profession/author name unconfirmed; merchant role inferred from visible pack |
| Storyteller visitor | `urizen-onebit-r02-c187`, `[2432,27,12,12]` | Hooded figure with raised staff-like prop | Storyteller/wanderer is authored casting; no claim of an instrument or bard animation |
| Winter branches | `urizen-onebit-r10-c005`, `[66,131,12,12]` | Publisher-labelled dead tree, exposed branches | Reused visually as a leafless winter tree; does not change forest state or kill a planted tree |
| Spring flowers | `urizen-onebit-r10-c001`, `[14,131,12,12]` | Flower stems | Cosmetic seasonal detail, no free harvest/resource effect |
| Autumn leaf | `urizen-onebit-r09-c001`, `[14,118,12,12]` | Publisher-labelled leaves | Restrained seasonal ground detail and tint, no whole-scene colour overlay |
| Merchant stop prop | `urizen-onebit-r47-c006`, `[79,612,12,12]` | Bag/container | Small composition at the actual Market activity, not a new stockpile |
| Story-circle prop | `urizen-onebit-r42-c003`, `[40,547,12,12]` | Upright furniture torch | Visible only for real storytelling; not an invented lantern sprite or fire-hazard mechanic |

Imps and Djinn have independent source silhouettes, not recolours of human
bodies. Their original black-key masks and luminance are retained with compatible
copper/red and blue/lilac palette variations. Human skin/hair/coat treatment is
unchanged. Visitors use their own pack/staff drawings and muted gold/lilac casts.
The source's enemy classification does not require hostile behaviour in this
simulation. Conversely, a green human-like row alone would not establish a
verified goblin people.

`apps/settlement/tool/assets.py` exports exact crops, portrait variants and a
single extended **world-actors atlas**. Existing 32 human atlas frames retain
their original pixel positions and feet; the original atlas file remains
unchanged. New actor frames occupy separate rows after its shadow row. The app
manifest distinguishes source frames/rectangles, palette variants, atlas frames,
semantic people/visitor roles, and actual permanent versus temporary entities.
The small `living_world` section is the implementation manifest; this is not a
migration of the entire historical catalogue.

## Ranked wider catalogue opportunities

| Rank / group | Verified material | Plausible role / suitability | Decision for this goal |
| --- | --- | --- | --- |
| 1 · Fantasy residents | Imp `r20-c104`, genie `r18-c104`; other neutral rows have different masks but uncertain identities | Small coherent fantasy populations with real work/social preferences | Implement the two high-confidence source identities |
| 2 · Visitors | Bag carrier `r00-c198`, staff carrier `r02-c187`; many held-tool figures | Distinct temporary guests, actual road travel and venue activity | Merchant and storyteller; reject unverified musical-instrument claims |
| 3 · Wildlife | Cat `r14-c001` `[14,183,12,12]`, hawk `r14-c006` `[79,183,12,12]`, rabbit `r16-c003` `[40,209,12,12]`; dog, horse, cattle, birds also verified | Wildlife, pets or farm ambience at compatible scale | Ranked for future ambient activity; no invented animal economy or extra AI subsystem now |
| 4 · Seasonal vegetation | Oak `r08-c001`, willow `r08-c002`, spruce `r08-c003`, leaves, flowers and bare tree | Seasonal foliage and ground accents, without mechanically regenerating terrain | Use branches/flowers/leaves; keep planting catalogue and saved vegetation identity |
| 5 · Visit/festival props | Bag, torch, bell `r25-c036` `[469,326,12,12]`, long tables `r35-c000..012`, chest family `r19-c026..033` | Make a visit/gathering's location visible | Use pack/torch compositions; defer furniture-placement/resource systems |
| 6 · Camps | Tent walls `r21-c043..050`, e.g. `[560,274,12,12]`, `[573,274,12,12]`, `[586,274,12,12]` | Temporary camp assembled from multiple pieces | Promising, but one piece is not a complete tent; defer footprint/port assembly |
| 7 · Defensive infrastructure | Urizen masonry/walls/gates; retained Kenney modular pieces; existing road/bridge ports | Fences, gates, walls and watch posts | Future Goals 09/10; assembly, collision and ownership still need design |
| 8 · Hostiles | Skeleton `r10-c104`, shadow-like `r13-c104`, horned/red rows, monk `r17-c104`, wraith-like `r26-c104`, caster-like `r28-c104` | Distinct future encounters | No hostile AI, damage, raids or deletion of Lua enemies in this goal |
| 9 · Unusual buildings | Urizen miniature granary/farm/warehouse family, shrines, monuments; retained 16px architecture | New community/industry landmarks | Keep all current buildings; review full footprint before a future addition |
| 10 · Environmental incidents | Static fire/bonfire `r42-c000/002/006`, broken columns `r03-c027`, dead vegetation | Incident markers and aftermath | No verified flame animation order; destructive storms, fire/injury and hazards deferred |
| 11 · Weather motifs | No verified coherent rain/snow animation sequence in selected sheet | Existing procedural rain, restrained snow/wind, static frost and UI labels | Do not call crystal/glacier aliases snowfall; keep effects cosmetic and reduced-motion aware |

## Rejected or limited alternatives

- Figure row 04 is green but its author species name is unknown. Row 27 is an
  unusual green, crown/branch-like silhouette; a “Rootkin” interpretation is
  possible but less certain than the verified genie. Neither is promoted into
  a documented publisher goblin/plant race.
- The original human hair/hood/cap variants remain human appearances, not separate
  peoples. Held-item columns do not prove professions, individual personalities
  or time-ordered walk cycles.
- Retained Tiny Dungeon and Kenney review drawings are real 16px sources with
  local provenance, but their scale, coloured interiors and modular perspective
  do not match this miniature 12px outdoor cast as directly. They remain review
  evidence; no unknown screenshots or missing Kenney paths are imported.
- Large machinery, unusual buildings, tent panels and table fragments require
  composition rules before they can become complete one-cell entities. No guessed
  atlas coordinates or new sprite pack is used.

## Provenance and preservation

The local `assets/CREDITS.txt` records vurmux's CC0 declaration for Urizen, including
its upstream tileset metadata revision. The two high-confidence identity matches
are recorded in the unchanged `assets/reference/urizen-names.json`/inventory.
Godot UI icons retain their existing pixel-boy CC0 credit; the unchanged Source
Code Pro font retains Adobe's SIL OFL licence. This review does not invent an
independent licence claim for reference logos/screenshots or uncertain artwork.

The renderer only reads new exported derivatives. Original sprite sheets,
canonical catalogue records, all pre-existing app PNGs, every README and the Lua
runtime are protected by the Goal 08 baseline fingerprints. Changes and final
runtime evidence are reported in [GOAL_08.md](GOAL_08.md).
