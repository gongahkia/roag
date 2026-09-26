# Magic content packs

`content_packs/<pack>/magic_text.json` supplies player-facing spell names,
descriptions, targeting and casting results, restoration wording, and magic
intent text. Its semantic keys and exact template variables are fixed by
`jomon/content_packs/contract.json`.

A writing-focused editor may rewrite this wording and the validated templates.
Values such as `{spell}`, `{target}`, `{cost}`, `{range}`, `{amount}`, and
`{location}` come from the engine. Each key permits only the placeholders listed
in the contract.

Do not change spell IDs, effect IDs, target kinds, costs, reach, power, radius,
learned-spell state, enemy intent IDs, damage, statuses, resources, targeting,
or RNG. `spells.json` remains the mechanical catalog. Spell and effect display
text resolves only through `magic_text.json`; a pack rename cannot change spell
selection or combat results.

Threats retain stable `intent_id` values while `intent` stores the rendered
selected-pack wording. Old saved default magic intent wording is mapped only by
an engine-owned legacy table; unknown historical text remains an unknown intent.
Existing rendered messages in saves remain frozen.
