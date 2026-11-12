# Classic roguelike artwork mapping

`assets/classic-bindings.json` explicitly maps all 599 semantic IDs in the frozen
`docs/sprite-audit/inventory.json` to the current preserved catalog. It contains
no implicit fallback, generated coordinate, or `aliasOf` reference. Each entry
has a catalog sprite ID and notes separating the source identification from
ROAG's local casting. The catalog and original PNG files are unchanged.
An optional boolean `flipX` corrects a crop's default facing in the local
binding. The renderer applies this correction before movement/attack facing;
portraits and static attack/hurt aliases use the same correction.

The mapping uses 237 distinct crops from 67 source PNGs: Urizen's main sheet,
65 Godot white icon PNGs, and one input-prompt sheet. Every selected crop was
decoded and matched against its catalog `rgba_sha256`; all 237 hashes match.
The catalog gzip SHA-256 is
`ed7b5cbbdfb21b82092197035694f3454582a8822df783e31171a8ec56beede3`.
The source names, rectangles, licenses and identity evidence remain in the
catalog and are copied by the runtime exporter rather than duplicated here.

| Semantic group | Explicit bindings |
| --- | ---: |
| Fifteen classes, including attack/hurt aliases | 45 |
| Fifty classic enemy forms, including attack/hurt aliases | 150 |
| 103 boon icons and detail aliases | 206 |
| Chests and drops | 5 |
| Objects, scenery and interactions | 84 |
| Terrain bases and mechanical markers | 47 |
| Interface, effects and status cues | 62 |
| **Total** | **599** |

Fourteen class and all fifty classic enemy castings reuse
`catalog.game_bindings.rpg`. Shade instead uses `urizen-onebit-r05-c108`, a
visually reviewed compact masked-looking figure replacing its broad previous
silhouette. This is a local casting override; the catalog's low-confidence
source identity (figure with a thin pole-like implement) remains unchanged.
Berserker, Alchemist, Hookrunner, Trapper, Duelist, Geomancer, Spiritbinder,
Tinkerer, Stormcaller and Wayfinder are mirrored to correct their default
orientation. The replacement Shade is also normalized to face right. These
corrections apply to each base, attack and hurt binding.
The four additional colony-era boss castings are
not new requirements of the restored classic game. A class or enemy name is a
local gameplay assignment, not a newly verified publisher identity. Most of
the character-sheet assignments retain low identification confidence: a visible
blade, vessel, pole or shield-like prop does not prove a character's profession
or species. For example, the Ember Moth uses the publisher-identified bee as a
winged insect substitute, and the Gulp Toad's figure remains an unverified local
creature casting.

Ninety-nine boon motifs reuse the RPG bindings unchanged. Emberbrand,
Crownfire, Cinder Stopper and Passing Ember instead use the publisher-identified
Urizen fire crop, `urizen-onebit-r42-c006`. The previous Godot `glow` mapping is
an editor symbol resembling a pause control, so fire communicates these four
roles more directly. This is a local mapping override, not a catalog correction.
The remaining editor icons are symbolic UI motifs: their filenames identify the
artwork, while boon descriptions explain the game rules.

All 130 attack/hurt bindings explicitly repeat their actor's base crop. All 103
boon detail bindings explicitly repeat their icon. These are static aliases,
not animation frames. Neighboring atlas cells are not treated as movement,
attack, hurt, open/closed, or environmental animation sequences.

The terrain mappings select genuine floor and wall motifs. In particular, the
catalog's generic RPG `wall` is identified as a stone window; the restored game's
plain wall bases instead use stone, rough dungeon, cave, fortified brick, glass
or metal wall crops appropriate to each biome. Water bases use the identified
horizontal river piece. A renderer may derive the 47 canonical wall/water masks,
but those masks are new compositions: the source does not provide a verified
47-mask bank set, and its neighboring cells must not be inferred as one.

Some classic multi-cell scenery has only representative counterparts:

- Column top/middle/base roles deliberately repeat a complete antique or broken
  column motif. They are not verified component slices of a larger column.
- Two-cell gate and vault roles repeat complete door motifs; the labels do not
  turn those crops into verified top/bottom door pieces.
- Rail roles use publisher-identified directional railway pieces. Their corners
  and repeated vertical pieces are an explicit local assembly.
- Metal hatch variants stand in for raised/depressed pressure plates. They are
  distinct symbols, not an identified state animation.
- A cabinet represents an empty shelf; parquet represents the carpet floor;
  a statue marks a chapel alcove; waterlily represents bog vegetation; a vertical
  chain stands in for a grappling hook. These substitutions are recorded locally.
- The unlit brazier is a vessel/stand silhouette of uncertain identity; its lit
  state uses an actual bonfire rather than assuming the next cell is its flame.

Fate values 1–8 and charge values 0–3 use publisher-identified Urizen numeral
glyphs, keeping their values distinct. The four directional force markers use
identified flat arrow keycaps from the current input-prompt pack. They retain the
keycap border and are UI symbols cast as map cues, not claimed ground textures.

The existing Urizen character, terrain/nature and equipment preview sheets and
Godot white-icon preview were inspected, followed by enlarged crops of all 65
actor identities and all selected non-actor motifs. Of the 237 unique crops,
176 retain high source-identification confidence and 61 retain low confidence.
Those are identification confidence levels, not approval scores for ROAG's
casting. Several roles share deliberately repeated motifs; complete coverage
does not mean 599 bespoke illustrations.

This document describes the authored mapping and source-pixel verification.
It does not claim LÖVE rendering, gameplay testing, visual approval of the final
compositor, or animation coverage. Historical Kenney audit pages remain frozen
records of the old appearance and must not be regenerated to imply that they
approved these replacement castings.

## Restored runtime handling

The catalogue PNGs remain unchanged. Runtime copies key RGB channels at or below 8
to transparent for Urizen body/icon textures, then use a white foreground alpha
mask under the classic semantic-ink palette. This keeps dark red casts readable.
Opaque terrain surfaces sample the original RGB crop, apply subdued lighting and
derived cardinal/concave borders. These borders do not claim native autotile data.
Complete column/door casts draw once at their owning cell; historical multipart
role aliases are not stacked into repeated complete objects.
