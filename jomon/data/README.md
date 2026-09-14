# Main-world content catalogues

These UTF-8 JSON files hold authored Jomon content. The Python modules keep the
rules, generated geometry, and runtime dataclasses. JSON is intentionally plain
text: it can be reviewed in diffs and needs no compression tool or dependency.

| File | Authored content |
| --- | --- |
| `people.json` | names, roles, regional context, recruits |
| `goods.json` | commodities, equipment text, discoveries, relics, passives, merchant stock |
| `actors.json` | enemy archetypes, regional and elite actor rows, glyphs, and reactions |
| `world_text.json` | Jomon map and help copy |
| `geography.json` | regional side-route anchors, terrain accents, hidden field caches, and frontier identities, contacts, discoveries, and relic sources |
| `terrain_variation.json` | regional field pockets, side-structure names, field travellers, and bounded local encounter pools |
| `situations.json` | 24 regional situation records and afterwork samples |
| `production.json` | regional sources, stations, and hand-authored recipes, including circuit parts |
| `circuits.json` | electrical fitting behaviors, glyphs, physical descriptions, and authored installations |
| `chemistry.json` | reagents and ordered reaction pairs |
| `field_reports.json` | public and private regional report responses |
| `quests.json` | regional quests, rewards, and cross-region arcs |
| `spells.json` | ordered spell rows; every four rows form one skill tier |
| `skills.json` | ordered skill branches and starting role roots |
| `vessel.json` | voyage cases, hazard stations, variants, echoes, refits, and authored route nodes and legs |
| `aftermath.json` | revisit lines and topologies, preparations, household stories, cross-region callbacks |
| `equipment.json` | physical item specifications, loadouts, ammunition, weapons, fittings, and regional armour |
| `visuals.json` | Jomon deck/tavern ASCII maps, entity and terrain glyphs, route marks, card frames, dice faces |
| `vehicles.json` | small-vehicle names, glyphs, interiors, limits, and harbour dimensions/moorings |

Arsenal weapon rows own their physical `shape` and `weight`; the inventory
adapter uses those exact values. Working weapons already have their own shape
and weight. Choose these per-weapon values with pack and load balance in mind.

Keep existing IDs stable: saves reference them. Array order matters for
situations, reactions, recipes, spells, and skill branches. Weapon-family
recipes, spell tiers, skill prerequisites, and encounter behavior are still
derived in Python. Route geometry, equipment effects, and aftermath reducers
also remain Python rules. The catalogues load at import, not during play.

After editing content, run:

```console
python -m unittest tests.test_catalog
python -m jomon.verification content
python -m unittest discover -s tests
```

The loader rejects duplicate keys, non-finite numbers, and missing sections.
Module adapters reject malformed row shapes; the content audit checks gameplay
references. Invalid JSON stops startup with a named error.

Visual sources are package-local: Jomon's actor-specific hostile glyphs are in
`actors.json`; the Dullest Dungeon's original portraits, card marks, and title
remain in `jomon/dumbest_dungeon/data/art.json`, while its office-fantasy portraits
and competitive-map symbols are in `jomon/dumbest_dungeon/data/office_visuals.json`.
Generated regional terrain and procedurally drawn UI borders remain rendering
rules rather than authored entity assets.
