# Production content packs

`content_packs/<pack>/production_text.json` contains the player-facing text for
physical gathering, fabrication, witnessed work orders, regional stock notices,
and the craft catalog. Its keys are fixed engine-owned semantic slots declared in
`jomon/content_packs/contract.json`.

A writing-focused editor may change recipe labels, station labels, gathering and
fabrication narration, delegation and completion records, status explanations,
and the validated templates in that file. Template values such as `{recipe}`,
`{station}`, `{quantity}`, `{item}`, `{worker}`, `{region}`, and `{stock}` are
supplied by the engine; the exact accepted set is declared per key in the
contract.

Do not change recipe IDs, station IDs, input or output kinds, quantities, flask
contents, regional source IDs, stock, work-order fields, availability checks,
or timing. Those values remain mechanical catalog and state data. Item names are
resolved through item presentation, reagents through chemistry presentation, and
regions and contacts through their existing presentation surfaces. A production
pack changes only the rendered description of the same work.

Work orders persist their stable recipe, region, day, and worker IDs. Existing
rendered production records remain frozen in old saves; later records use the
selected pack's rendered text.

Masterwork status is a stable item flag. The legacy default `masterwork:` provenance
prefix is recognized only when loading old saves; a pack’s provenance wording does
not grant or remove the masterwork combat effect.
