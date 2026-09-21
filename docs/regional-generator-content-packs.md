
# Greywash, Greenwold, and Whitecairn generator text

`topology_text.json` also owns the visible text emitted while generating Greywash,
Greenwold, and Whitecairn. A writing-focused author may change the region brief,
process label, zone and landmark labels, link and cache names, and the two fixed
contact names and role labels for each region. For example, changing
`topology.greywash.zone.salt_pans` changes the displayed zone name only.

Do not change semantic keys, contact IDs, cache IDs, coordinates, tile geometry,
rewards, requirements, contact behaviour, threat archetypes, RNG stages, or
placeholder contracts. Cache slot keys use underscore-safe presentation names
where the existing mechanical cache ID contains a hyphen; the engine owns that
mapping.
