# Main-world content catalogues

These UTF-8 JSON files hold authored Jomon content. The Python modules keep the
rules, generated geometry, and runtime dataclasses. JSON is intentionally plain
text: it can be reviewed in diffs and needs no compression tool or dependency.

| File | Authored content |
| --- | --- |
| `people.json` | names, roles, regional context, recruits |
| `goods.json` | commodities, equipment text, discoveries, relics, passives, merchant stock |
| `actors.json` | enemy archetypes, regional actor rows, glyphs, reactions |
| `world_text.json` | Jomon map and help copy |
| `situations.json` | 24 regional situation records and afterwork samples |
| `production.json` | regional sources, stations, and hand-authored recipes |
| `chemistry.json` | reagents and ordered reaction pairs |
| `field_reports.json` | public and private regional report responses |
| `quests.json` | regional quests, rewards, and cross-region arcs |
| `spells.json` | ordered spell rows; every four rows form one skill tier |
| `skills.json` | ordered skill branches and starting role roots |
| `vessel.json` | voyage cases, hazard stations, variants, echoes, and refits |
| `aftermath.json` | revisit lines and topologies, preparations, household stories, cross-region callbacks |
| `equipment.json` | physical item specifications, loadouts, ammunition, weapons, fittings, and regional armour |

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
