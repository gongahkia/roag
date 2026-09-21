# Quest content packs

`content_packs/default/quests.json` owns the player-facing title, lead, regional
choice label and availability wording for each contracted regional quest. It also
owns cross-region arc titles and the visible names and descriptions of their
physical evidence records. A writer may rewrite those strings and nothing else.

For example, a safe change is:

```json
"quest.regional.hearthford": {
  "title": "A Different Mill Claim",
  "lead": "A witness points to the cache below the road.",
  "choices": {"l": {"label": "Publish the settlement", "requirement": ""}, "r": {"label": "Back the private repair", "requirement": ""}}
}
```

Do not add, remove, or rename quest keys or choice keys. Do not edit engine IDs,
cache IDs, evidence IDs, requirements, rewards, branch outcomes, or catalog
ordering. Those values remain in the engine-owned contract and mechanics
catalog. Evidence keeps its stable `consumable:` item kind even when its visible
name changes.

Rumour notices continue to use the engine-owned `rumour` notice kind. The UI
pack owns its visible category label; quest leads supply only their body text.
Persisted messages and memories remain rendered legacy strings for save
compatibility and are not rewritten on load.
