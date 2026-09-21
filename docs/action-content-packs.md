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
