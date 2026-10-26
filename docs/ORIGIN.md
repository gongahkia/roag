# Origin and repository boundary

Roag was extracted from the fantasy RPG retained in Ithica at commit `c87d4808293dd2d945762cf4f4a1b7058cf0313b` on 2026-10-20. The source repository was `https://github.com/gongahkia/ithica`. This repository owns an independent snapshot without importing old oversized catalog blobs or requiring Ithica's Git history. The existing Roag Git history, remote, funding configuration and user-owned root AGENTS.md link were preserved.

The playable RPG client, expedition rules/tests, graphics helpers, complete asset directory, documentation images, catalog authoring inputs and asset tools were copied here. Source artwork, stable IDs, crop coordinates, original pack names, hashes, credits and mapping evidence are preserved. The catalog is tracked as lossless `assets/catalog.json.gz`. Historical `ithica-*` asset filenames and catalog fields retain their original identities intentionally; `usage.rpg` and expedition bindings describe this RPG.

There are no Git submodules, asset symlinks, project references to the sibling repository or automatic cross-repository updates. Both games can evolve independently. The colony simulation and colony client remain in Ithica. Copied expedition code retains its inherited field helpers and lunar validation adapter, without a colony runtime or normal lunar launch mode.

Old guides under `docs/history/ithica/` are provenance, not current Roag documentation. Original documentation PNGs remain at their audited paths so catalog inventory/hash checks keep working.

## Replayed history and integration reference

On 2026-10-22 the user replayed Ithica history into this repository. `070558d1332010b54cd7da080fb23bfa4f24cd17` records its completed extraction/workforce checkpoint; `6743eba44a347a1a07624b30a90e97e902246673` restores the exact Roag tree previously recorded at `1e27b35`. The original sibling checkout was removed. An independent, detached, remote-free copy at the pinned Ithica commit is ignored under `artifacts/references/ithica`; all 849 files were verified against their Git blobs, executable modes and a complete object integrity check. The recorded commit is sufficient to recreate the reference.

The user subsequently authorized one fantasy Roag game incorporating selected mechanics, with no lunar lore or runtime dependency on the reference copy. INTEGRATION.md records that direction; earlier statements about independent product development above describe the historical extraction. Original source identities, artwork and credits remain preserved.
