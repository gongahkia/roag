# Unused sprite overlap and mechanic opportunities

**Historical proposal snapshot (2026-10-18):** the counts and “unused” labels
below describe the original 242-binding audit used to make review choices.
The approved features are now implemented. The current catalogue has
**289/289 resolved bindings** in both choices and includes two additional
approved source sheets. Use [the current audit](../ROAG_SPRITE_AUDIT.md) and
[regenerated catalogue](index.html) for present usage; keep this record as
context for the review decisions.

Audit of commit `3b58b1b9` (2026-10-18). This is a design inventory, not a
change to game rules or sprite bindings. It follows the runtime coverage audit
in [ROAG_SPRITE_AUDIT.md](../ROAG_SPRITE_AUDIT.md). Source indices are zero-based
cells in the [visual catalogue](index.html); the corresponding rows and current
bindings are in [tiles.csv](tiles.csv). The [interactive review page](review.html)
lets you vote on each idea and sprite pair, add comments, and export feedback.
The [feedback review](review-feedback.md) records both rounds of decisions,
corrected art readings and current approval boundaries. It supersedes the
initial candidate suggestions below where they differ.

## What “both packs” means here

The two selectable Kenney looks are **mixed Kenney** (Micro Roguelike actors and
objects, Caves & Dungeons surfaces, 1-Bit boon/terrain symbols) and
**standalone 1-Bit**. They are not two independent sprite sheets. The same
1-Bit PNG is loaded by both choices.

| Source sheet | Nonempty cells | Currently referenced by mixed | By standalone | Not referenced by either |
| --- | ---: | ---: | ---: | ---: |
| Micro Roguelike | 159 | 40 | Not loaded | 119 |
| Caves & Dungeons | 522 | 52 | Not loaded | 470 |
| Shared 1-Bit | 1,077 | 56 | 101 | **976** |

The **976** are literal shared-source overlap: any of those indices could be
mapped in either look, subject to readability. All 56 1-Bit cells currently
used by mixed Kenney are also used by standalone; standalone uses 45 more.
The 119 Micro and 470 Caves cells are *not* parallel art in standalone. Their
value for a two-look mechanic depends on finding a suitable unused 1-Bit
counterpart. The source scan below makes those matches.
All **242 active bindings** already resolve in both looks; the opportunities
below concern unused art and clearer visual identity, not broken coverage.

I inspected all three original sheets as indexed contact sheets and checked
every proposed cell against the generated CSV: all indices below have visible
pixels and no current binding in either choice. The source audit was regenerated
to a temporary directory; its HTML, JSON and CSV matched the checked-in copies
byte for byte. Motif names below are visual readings, not official per-cell
labels. Caves borders, arches and bridges are often parts of multi-cell
assemblies; a count of cells is not a count of ready-made interactables.

## Strong paired motifs

| Opportunity | Mixed Kenney unused cells | Standalone 1-Bit unused cells | Design use and important limit |
| --- | --- | --- | --- |
| **Secret crack / rubble clue** | Caves **95–102** cracked ground | **1, 3, 4, 6** fragment/debris marks | Replace the existing procedural secret crack clue with a small art overlay. Keep its current discovery rules, fog behavior and readability. The 1-Bit symbols read as debris more than literal cracks. **Visual-only first.** |
| **Attack marker and impact accent** | Micro **131** ring/reticle; **133–134** red streaks | **625–626** corner markers; **565, 570** slash/flame accents | Add a small marker at the committed attack center and an impact accent. Keep the existing dotted affected-cell outlines: they convey tactical geometry better than a sprite alone. **Visual-only first.** |
| **Weapon-specific attack accent** | Micro **70–74** weapon silhouettes | **327–329, 331–332, 334–336, 375–377, 424–434** melee, bow and staff silhouettes | Give existing sword, bow and spell attacks a recognizable small icon in the aim/impact UI. This helps distinguish current class kits without adding equipment rules. Match icons to abilities individually; this is a motif pool, not one-to-one pairs. **Visual-only first.** |
| **Trees and overgrowth** | Micro **84–87** green trees/shrubs | **49–53** tree silhouettes | Dress Overgrown Ruins and selected secret rooms before adding a new biome. These are prop candidates, not floor tiles; check Micro's 8px style beside Caves' 16px terrain at game scale. **Visual-only first.** |
| **Fungal growth / spore pod** | Caves **87–90, 116–119** mushroom clusters | **315–317** mushroom silhouettes | Dress Fungal Hollows now; a later hittable pod could briefly create existing obscuring mist. Distinguish it from the current Spore Herald enemy and bramble contact damage. |
| **Lit and unlit brazier** | Caves **506–508** flame/unlit fixtures | **738–740** candle/torch motifs | An interactable fixture could toggle a short-lived safe zone or suppress nearby mist. There is no present light-radius system, so do not promise one from art alone. |
| **Ore / fragile vein** | Caves **329–334, 358–363** colored mineral flecks | **524–525** shard/spark and **6** debris | A breakable optional node could release currency or a brief charge. Use the existing attack and reward event path; keep it distinct from resonant crystal, which already reacts to lightning. |
| **Key and optional locked vault** | Micro **120** key; Caves **314–316, 343–345** barred openings | **571–573** keys; **552, 607** barred gates | A key picked up on the same floor opens an *optional* reward room. Existing doors open freely; this would add lock state and a reachability guarantee. Caves gates need multi-cell placement or a carefully cropped single-tile icon. |
| **Pressure switch and timed gate** | Micro **104–105** button-like fixtures; same Caves gate cells above | **111–115** switch motifs; same 1-Bit gate cells above | Player or enemy weight opens a gate for a fixed number of turns. This could create tactical routing, but requires clear previews and save/restore state. Avoid making the main route depend on a timed gate. |
| **Flask pickup** | Micro **106–107, 123** flask/jar variants | **578–581, 671** bottles | A single-use on-pickup ward or cleanse is possible without an inventory screen. Current food/health drops, Alchemist attacks and 52 boons already occupy nearby design space; choose a distinct effect before adding this. |

## Plausible matches that need a closer design pass

| Opportunity | Candidate cells | Why it is lower confidence or higher cost |
| --- | --- | --- |
| **Volatile object** | Micro **89** round vessel; 1-Bit **337–340, 570** round/flame vessels | The shapes plausibly read as bombs, but their exact intent is ambiguous at 8–16px. A neutral, attack-triggered delayed blast could affect both factions, reusing combat events. Verify the silhouettes at the game's 40px tile before committing. |
| **Hooded NPC or enemy** | Micro **8** hooded body; 1-Bit **79** hooded figure | This is a genuine unused body pair. Micro's main actor area has only **one** unused complete character body, so it does not support a broad new class/enemy roster by itself. The NPC's gameplay role must earn its place first. |
| **Water crossing** | Caves **445–446, 474–475, 503–504** wood bridge pieces; 1-Bit **70, 245, 294** ladder/grate motifs | The 1-Bit art is a visual analogy, not a matching bridge. Caves art is an assembly. Walkable crossings over currently impassable water change generation connectivity and pathfinding. |
| **Pillar or cover** | Caves **145–149, 174–178** column pieces; 1-Bit **640** structural motif | The 1-Bit match is weak. A blocking pillar also overlaps the breakable barricade unless its projectile/movement behavior differs. Use as decorative architecture first, or defer. |

## Surplus that should not drive a mechanic yet

The 976 shared 1-Bit cells include glyphs, UI frames, card/dice imagery,
modern/space objects, near-duplicate silhouettes and fragments of larger
structures. The 470 unused Caves cells include many border variants and
multi-cell assemblies. Counts alone exaggerate the number of distinct game
ideas. Additional Micro weapons could support a true equipment system, but
the present class abilities and 52 boons already express weapon and build
choices. A new item inventory needs a separate decision about progression,
controls and balance. Likewise, extra floor art cannot by itself make another
biome mechanically distinct; the seven existing biomes have their own routes,
encounters and terrain rules, while 1-Bit currently uses one base material
set for all seven.

## Original recommendation (superseded where the review differs)

1. **Use existing mechanics more clearly first:** secret crack art, attack
   accents, trees and fungal/ore decoration. This consumes good matches with no new
   balance or persistence rules.
2. **Prototype one small combat object:** a spore pod or volatile vessel in an
   optional room. Give it one clear telegraph, one event-driven effect and
   fixed deterministic placement. Test both factions and save/restore.
3. **Then consider an optional key vault.** It has the clearest complementary
   item and gate art, but requires generated-floor reachability, lock state,
   reward economy and Classic fallback visuals.
4. **Defer a new biome, class or inventory system.** Seven biomes and six
   classes already exist; standalone 1-Bit currently shares one base terrain
   material across every biome. Extra art alone would not establish a distinct
   route, combat niche or readable biome identity.

Any implemented mechanic needs stable IDs and bindings in
`src/visual_slots.lua`, pack mappings in `src/kenney_visuals.lua` and
`src/onebit_visuals.lua` (or their terrain/symbol modules), simulation and
placement in `src/game.lua`/`src/stage.lua` or `src/terrain.lua`, a Classic
built-in fallback, and updated coverage tests. The original PNG sheets should
remain unchanged. This document is a candidate map, not evidence that a
mechanic is already implemented or balanced.
