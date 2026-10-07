# Preparation content packs

`content_packs/<pack>/preparation_text.json` owns the displayed names,
descriptions, conditions, use results, records, and field-kit wording for
preparations. The engine-owned contract in `jomon/content_packs/contract.json`
lists every semantic slot and its allowed template values.

A writing-focused editor may change only the wording in this file and its
validated templates. Do not change `preparation.<mode>` IDs, preparation item
kinds, topology relationships, modes, consumed items, effects, quantities,
state markers, timing, RNG, or template-variable contracts. These values remain
engine mechanics. Old bundled-default preparation labels are accepted only by
the compatibility mapping while old saves load; selected-pack text is never
used to recover an identity.
