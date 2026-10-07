# Regional content packs

`jomon/content_packs/contract.json` is engine-owned. It fixes the ordered mapping
between `region.family_1` through `region.family_8` and Jomon's legacy engine
region IDs. Content packs cannot redefine that mapping.

Each pack supplies `regions.json` with one entry for every semantic region slot.
Authors may edit only the presentation strings:

- `display_name` is used for a generated region's player-facing name.
- `route_label` is used on the route chart.
- `short_description` is used for that regional route stop.

Do not add, remove, rename, or reorder semantic slots; do not add engine IDs to
this file. Region IDs, world generation, routes, save data, and mechanics remain
engine contracts for now.
