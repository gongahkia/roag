# P07 artwork preparation

This isolated preparation adds 16 static aliases to the existing 27 selected crops.
It supplies artwork for the three proposed encounters, their counters and hazards,
and all seven P06 hostile kinds. It does not activate encounters, replace the map's
current hostile markers, change simulation rules, or complete P07 acceptance.

The authoritative source remains the preserved `assets/catalog.json.gz` and original
PNG files. `web/content/boss-art.json` separates `authoredRole` from the copied
`source.identity` and neutral `artwork` description. It retains exact IDs, crop
rectangles, RGBA hashes, identity confidence, publisher matches where present,
classification confidence, references, original notes and uncertainties. Its
`hostiles` mapping gives the future renderer a distinct alias for every P06 kind.

All new crops are 12×12 pixels, below the exporter's 16×16 limit. Every selected crop
was opened in an enlarged, nearest-neighbor review of the exporter's derived PNG.
The final 43-crop sheet was also inspected with the founders alongside the new
hostiles. No original artwork, catalog, reference document or generator was edited.

| Local alias | Original catalog ID | Catalog artwork identity | Identity confidence |
| --- | --- | --- | --- |
| `boss-bellkeeper` | `urizen-onebit-r42-c110` | Ghost | High; publisher metadata |
| `boss-sluice-maw` | `urizen-onebit-r42-c108` | Slime | High; publisher metadata |
| `boss-threadmother` | `urizen-onebit-r15-c002` | animal / spider | High; publisher metadata |
| `feature-resonator` | `urizen-onebit-r25-c036` | bell | High; publisher metadata |
| `feature-valve` | `urizen-onebit-r00-c068` | door / valve / metal | High; publisher metadata |
| `hazard-web` | `urizen-onebit-r39-c004` | web | High; publisher metadata |
| `boss-ossuary-captain` | `urizen-onebit-r10-c104` | White skeleton-like humanoid: empty hands | Low; visual interpretation |
| `enemy-raider` | `urizen-onebit-r21-c203` | Pirate | High; publisher metadata |
| `enemy-breacher` | `urizen-onebit-r43-c108` | Bodyguard | High; publisher metadata |
| `enemy-shield` | `urizen-onebit-r05-c113` | Orange flat-headed small humanoid: rectangular shield-like object | Low; visual interpretation |
| `enemy-archer` | `urizen-onebit-r04-c119` | Green wide-eared small humanoid: curved bow-shaped object | Low; visual interpretation |
| `enemy-healer` | `urizen-onebit-r17-c104` | Monk | High; publisher metadata |
| `enemy-rusher` | `urizen-onebit-r20-c104` | Imp | High; publisher metadata |
| `hazard-fire` | `urizen-onebit-r42-c006` | fire | High; publisher metadata |
| `channel-water-h` | `urizen-onebit-r32-c004` | water / trough; orientation=H | High; publisher metadata |
| `channel-water-v` | `urizen-onebit-r32-c001` | water / trough; orientation=V | High; publisher metadata |

The confidence column concerns source identity. It does not endorse the new game
role. In particular, Monk does not establish healing, Pirate does not establish
raiding behavior, and the Captain's skeletal appearance does not confirm species.
The shield and archer retain the catalog's explicit warnings that body rows and
held props do not establish species, profession, animation or direction sets.
The web's general asset classification remains **medium** even though its web
identity is publisher-confirmed. Both confidence fields remain in the role metadata.

The Captain, shield and archer selections also occur in the preserved legacy RPG
bindings. That continuity is useful provenance, not additional identity evidence.
The other hostile roles are new local casting choices. Their crop hashes differ
from every founder crop and from one another; the final renderer must still use
hostile outlines, health/selection cues and readable role labels. Color alone is
insufficient, especially for the pale Captain and white Bellkeeper.

Keep the following boundaries when integrating these aliases:

- Use one static frame per alias. Adjacent source cells are separate candidates;
  this selection establishes no walking, attack or death sequence.
- Keep the source orientation. The water selections are explicitly horizontal and
  vertical trough segments. No corner, intersection, cap, auto-tiling mask or fluid
  flow can be inferred from them. Author channel connectivity in world data.
- The valve crop includes a doorway frame. It is suitable as a marked control
  feature, but it is not an isolated valve wheel. Open/closed state needs an explicit
  overlay or label; this selection contains no state pair.
- Bell resonance, damaged structures, burning webs, charge direction and attack
  warnings need explicit presentation cues. A static crop does not encode these
  states. Collision, footprints, blocking, damage and timing stay in simulation.
- The exporter retains its existing background-only transform: RGB channels all
  at or below 8 become transparent in derived output; other RGBA values are copied
  exactly. No recoloring, repainting, rotation or inferred multi-cell assembly was
  introduced. All original files remain protected by the preservation manifest.

The generated runtime output remains four files: `colony.png`,
`colony-atlas.json`, `manifest.json` and `CREDITS.txt`. The 43 selected crops total
29,775 generated bytes. The full catalog, reference atlas and this provenance
research stay out of the distribution. The existing compact manifest continues to
carry original IDs, crop bounds, pixel/source hashes and confidence for each alias.

Reproduce the checks from `web/` with the pinned Node version:

```sh
fnm exec --using=24.21.0 npm ci
fnm exec --using=24.21.0 npm run assets
fnm exec --using=24.21.0 npm run assets:check
fnm exec --using=24.21.0 node --test tests/assets.test.mjs
fnm exec --using=24.21.0 npm run build
```

From the repository root, run `python3 -B tools/verify_preservation.py`.
The asset tests compare every exported pixel and gutter against the original
source, verify deterministic export and freshness failures, and check all copied
identity/uncertainty fields against the compressed catalog. They also check exact
hostile coverage and prevent founder or hostile crop reuse.

Local review artifacts are under `artifacts/colony-web/P07-art/`: candidate export,
integer-scale review PNGs, the final alias/frame index, validation receipts and
source hashes. These are preparation evidence; no browser encounter or release
claim is made here. Gameplay and renderer integration remain dependent on accepted
P06 and the rest of P07.
