# Legendary and relic presentation

A writing pack may edit `legendary_text.json`. Its strict `text` object supplies the words used for generated legendary objects and the four stable arc relics. The engine contract in `content_packs/contract.json` lists every required text key and the exact placeholders allowed for each template.

For example, a pack can change `legendary.object.name` from `"{maker}'s {noun}"` to another phrase using exactly `{maker}` and `{noun}`. It may also rewrite the provenance template and an arc relic's display name or discovery/result prose.

Authors must not add, remove, or rename text keys or placeholders. They must not change legendary or relic IDs, inventory kinds, object stats, effects, rewards, cache placement, ownership, deterministic selection, RNG stages, arc choices, or save identities. The engine selects the object and applies its mechanics; the pack supplies only the displayed language.
