# Vessel content packs

`content_packs/<pack>/vessel_text.json` supplies the player-facing vessel wording
for the fixed drink measures, visible schedule labels, and tavern service wrappers.
Its `text` object is strict: every semantic key and every permitted template
placeholder is declared in `content_packs/contract.json`.

A writing-focused author may rewrite drink names, benefits and drawbacks, visible
schedule labels, bartender-service prompts, and vessel-owned drink/status results.
The engine continues to own drink IDs, stock, cost, duration, rarity,
incompatibilities, credit, inventory placement, effect IDs, schedules, actor
positions, service availability, time, and state changes. Schedule activity strings
remain engine-owned persisted keys; the pack only supplies their display labels.

Character names and roles come from `characters.json`, while item, quest, action,
and UI wording remains in their respective presentation sources. `vessel.json`
continues to own voyage and crisis mechanics as well as drink mechanical fields;
its legacy drink prose is retained for catalog compatibility and is not the runtime
presentation source.

Templates may use only the placeholders listed in the contract. Authors must not
add or remove keys, alter placeholder contracts, or change schedules, stock, prices,
workshop/refit rules, resources, RNG, or state transitions. Travel/voyage text,
ship-crisis text, and vehicle presentation are separate milestones.
