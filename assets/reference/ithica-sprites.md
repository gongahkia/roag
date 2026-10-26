# Ithica: current sprite casting

Current Ithica lunar bindings, distinct from retained RPG preview selections. Original source IDs, crops and identity confidence remain unchanged.

The colony selection remains in `ithica-colony-sprites.json`; its bitmap font is shared. The table below covers current expedition aliases, including terrain, residents, encounters, objects and the exposure overlay. Preview-only aliases and every unused sprite retain explicit status and future ideas in catalog schema 4.

Edit `assets/ithica-casting.json` for roles/limits and `assets/ithica-expedition-bindings.json` for existing-ID selections, then regenerate. Source PNGs, IDs, rectangles and identity confidence are preserved.

**166 distinct IDs selected by Ithica, including font/UI; 14 field aliases.**

| Alias | Stable ID | Source evidence | Current role | Limits / use |
|---|---|---|---|---|
| bandit | urizen-onebit-r43-c105 | enemy / bandit / male / high | Observed movement | An uncertain humanoid silhouette. No faction, species, origin or animation is claimed. |
| boulder | urizen-onebit-r05-c020 | floor / boulders / high | Blocking surface rubble | Ground.Wall in Ithica; the legacy preview also uses this crop as nonblocking scenery. |
| cracked-wall | urizen-onebit-r02-c001 | Structural tile R02 C001 / low | Damaged compartment boundary | WFC structural boundary. Collision and sockets come from code. |
| crystal | urizen-onebit-r22-c002 | mineral / crystal; index=0 / high | Recoverable component | A readable salvage marker. Recovery yields actual carried stock, deposited only on return. |
| flagstone | urizen-onebit-r05-c004 | floor / flagged; index=0 / high | Compartment deck | WFC room floors; an observed constructed surface, with no claimed origin. |
| gate | urizen-onebit-r01-c012 | door / closed / bars / high | Sealed passage | Blocking object until a restored pump permits cycling the seal; global route validation prevents bypass. |
| gate-open | urizen-onebit-r01-c000 | door / open / high | Open passage | Same source mapping as the preview; switches presentation after the seal is opened. |
| grit | urizen-onebit-r05-c017 | floor / grit / high | Surface ground | CA terrain; explicit walkability. Author grit reused as lunar dust. |
| hazard-corruption | urizen-onebit-r09-c001 | flora / leafs / high | Interference or exposure field | The publisher leaf motif is a symbolic overlay, not a claim of lunar plants. Only eligible unprotected cells spread it; no outdoor ordinary fire. |
| resident | urizen-onebit-r00-c105 | Figure row 00, profession column 01: hammer-like hand tool / low | Suited resident or stranded person | Reuses the exact colony engineer crop. Profession, suit protection and field role are game data, not publisher identity. |
| rock-floor | urizen-onebit-r05-c009 | floor / rocks / high | Exposed rock | CA terrain variation. No inferred terrain animation. |
| stairs-up | urizen-onebit-r00-c024 | stairs / up / high | Return approach | A readable exit marker. Travel has an explicit time cost. |
| well | urizen-onebit-r25-c031 | well / high | Damaged pump | An equipment-like marker requiring a paid repair. Original source identity is retained separately. |
| wisp | urizen-onebit-r42-c110 | enemy / ghost / high | Interference | An uncertain effect-like silhouette. Distinct intent and stats; no supernatural explanation is asserted. |
