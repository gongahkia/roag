# Material and inspection content packs

`content_packs/<pack>/material_text.json` owns material display names, hazard and
status wording, material-event narration, field-inspection text, and material
handling feedback. The engine-owned contract in `jomon/content_packs/contract.json`
lists each semantic slot and its allowed template values.

A writing-focused editor may change the wording in this file and its validated
templates. It may not change material, coating, fluid, hazard, verb, reaction,
or status IDs; physical quantities; fire, water, spread, collapse, exposure, or
reaction rules; geometry; costs; timing; RNG; state mutation; or template
contracts. Chemistry reagent and reaction names remain in `chemistry_text.json`;
item, preparation, topology, combat, and UI display values remain in their
respective presentation surfaces.
