# Native tactical content

`boons.json` preserves the 103 registered definitions from Roag revision `d498fe0889050d2a23f2ff86d80db6084ed6467a`, after its ordered expansion merges. It is embedded as `Roag.Tactics.Boons.json` and loaded by the handwritten `BoonCatalog` into immutable typed definitions. It contains content data, not translated Lua code or a dynamic execution runtime.

The original `art` fields are deliberately excluded: final sprites must be selected through the Vagabonds universal atlas. Original indices and source hashes remain in `docs/migration/original-boon-inventory.json` as reference evidence. No final boon art selection is claimed here.

The schema retains ordered pools, four rarity tiers, class eligibility, incompatibilities, condition trees, effects, modifiers, weapon forms and preview metadata. Loading rejects unknown fields/operations and invalid declarations. The native event/effect resolver and content-authoring CLI integration are still being implemented; successful catalog loading is not proof that every boon executes in the client.

Overflow Chalice's food conversion remains pending user clarification. Its original declaration is retained for preservation; no hunger-overflow or healing-only adaptation has been selected. See `docs/MIGRATION_PROGRESS.md` for current evidence and unfinished work.
