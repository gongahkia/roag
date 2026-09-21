# Combat action content packs

`content_packs/<pack>/action_text.json` supplies player-facing combat narration.
It may rewrite attack, guard, armour, threat-intent, telegraph, defeat, and
specialized elite-result text using only the placeholders listed in
`content_packs/contract.json`.

A writing-focused author may change the wording of those templates. It may not
change action, result, intent, actor, weapon, status, or source IDs; damage,
armour, AI, movement, target selection, rewards, cooldowns, turn costs, RNG,
and state mutation remain engine rules. In particular, threat intent display
text is not an AI key: `Threat.intent_id` is the stable identity and
`Threat.intent` is rendered presentation retained for compatibility.

For example, an alternate pack can rewrite
`intent.elite.floodgate.sluice_telegraph` as `marks fixture sluice {x},{y}`.
That changes only the warning shown to the player; the marked coordinates,
sluice effect, and deterministic hit-location seed remain the same.

## Social action text

The `social.*` entries in `action_text.json` describe courier selection, recruitment,
regional requests, negotiation, contact prompts, tavern incidents, merchant exchanges,
and claimant settlements. They may be rewritten, including only the placeholders declared
by `contract.json`. They never change contact IDs, evidence, availability, relationship
or disposition values, costs, rewards, state changes, or turn use.

## Remaining action text

The `action.*` entries describe action execution outside combat and social
interactions: equipment preparation, local movement, containers, environmental
controls, material interventions, consumables and relics, returns, and route-stop
work. Authors may rewrite only those visible messages and their declared templates.
They may not alter item kinds, inventory quantities, terrain or world state, routes,
coordinates, rewards, costs, checks, RNG, turn use, status IDs, or result identities.
Text returned directly from quests, sanctums, regional history, aftermath, worklines,
interference, vessels, preparations, and arc relics remains owned by those systems.
