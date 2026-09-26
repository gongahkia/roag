# Advanced equipment content packs

`content_packs/<pack>/equipment_text.json` owns the visible wording for work
weapons, arsenal weapons, fittings, fitting results, workshop messages, and
advanced-equipment target/overlay text. Its semantic slots and exact allowed
template values are declared in `jomon/content_packs/contract.json`.

A writing-focused editor may change weapon and fitting display names,
descriptions, effect and drawback explanations, specialised strike narration,
workshop prompts and result text, and the validated templates in that file.

Do not change raw weapon kinds, raw fitting kinds, `fitting:<kind>` inventory
kinds, families, ammunition mappings, reach, damage, effects, prices,
compatibility, stock, costs, durability, slots, item shapes, state transitions,
or template-variable contracts. Those are engine/catalog mechanics. The raw
English-looking kinds are deliberate stable identities used by inventories,
saves, fitting compatibility, ammunition, and combat.

Base item labels continue through `items.json`; generic combat narration through
`action_text.json`; generic UI through `ui_text.json`; and character names
through character presentation. `equipment_text.json` only owns the advanced
vocabulary around the same mechanics. Existing rendered history remains frozen
in saves; new workshop and combat messages use the selected pack.
