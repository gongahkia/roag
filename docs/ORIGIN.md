# Origin and repository boundary

Vagabonds was extracted from the fantasy RPG retained in Ithica at commit `c87d4808293dd2d945762cf4f4a1b7058cf0313b` on 2026-10-20. The source repository was `https://github.com/gongahkia/ithica`. This repository owns an independent snapshot without importing old oversized catalog blobs or requiring Ithica's Git history. The existing Vagabonds Git history, remote, funding configuration and user-owned root AGENTS.md link were preserved.

The playable RPG client, expedition rules/tests, graphics helpers, complete asset directory, documentation images, catalog authoring inputs and asset tools were copied here. Source artwork, stable IDs, crop coordinates, original pack names, hashes, credits and mapping evidence are preserved. The catalog is tracked as lossless `assets/catalog.json.gz`. Historical `ithica-*` asset filenames and catalog fields retain their original identities intentionally; `usage.rpg` and expedition bindings describe this RPG.

There are no Git submodules, asset symlinks, project references to the sibling repository or automatic cross-repository updates. Both games can evolve independently. The colony simulation and colony client remain in Ithica. Copied expedition code retains its inherited field helpers and lunar validation adapter, without a colony runtime or normal lunar launch mode.

Old guides under `docs/history/ithica/` are provenance, not current Vagabonds documentation. Original documentation PNGs remain at their audited paths so catalog inventory/hash checks keep working.
