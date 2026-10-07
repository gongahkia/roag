# Hearthford topology presentation

A writing pack may edit `topology_text.json`. Its strict `text` object supplies the visible names for Hearthford's fixed zones, topology-specific landmarks, vertical links, and base containers. The engine contract in `content_packs/contract.json` lists every required key and its permitted placeholders; the current Hearthford entries are literal text and permit none.

For example, `topology.hearthford.zone.millworks` may be rewritten to rename the generated millworks without moving any tile, path, landmark, cache, or route.

Authors must not add, remove, or rename keys. They must not change Hearthford's region ID, coordinates, dimensions, levels, tiles, landmark IDs, container IDs, rewards, requirements, link endpoints, RNG stages, candidate ordering, or generation algorithms. The engine generates the layout and rewards; the pack supplies its displayed terminology.
