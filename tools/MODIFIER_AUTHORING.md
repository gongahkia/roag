# Expedition modifier authoring

Normal Expedition modifiers are strict JSON files in `content/expedition/modifiers/`; one file is one modifier. The manifest determines deterministic load order. Runtime state persists only `modifier ID → stack count`; definitions are reloaded from content.

## Add a normal modifier

1. Open **ROAG Studio → Modifier Workbench**, choose New or Duplicate, then save after validation; or add a JSON file and its filename to `manifest.json`.
2. Give it a stable `expedition.passive.*` ID, mechanical name/description, category/tags, and disabled pool state until reviewed.
3. Add `static_effects` for stats or a hook with conditions/effects for reactions.
4. Run `luajit tools/analyze_modifiers.lua` and `luajit tests/run.lua`.

Normal content requires **no gameplay-code changes** when it uses a registered primitive.

## Schema v1

```json
{
  "schema_version": 1,
  "id": "expedition.passive.ballistic_lens",
  "name": "BALLISTIC LENS",
  "description": "+1 projectile damage per stack.",
  "category": "damage",
  "tags": ["projectile", "damage"],
  "pool": { "enabled": true, "weight": 1 },
  "static_effects": [
    { "kind": "modify_stat", "stat": "projectile_damage", "value": { "kind": "linear", "base": 1, "per_stack": 1 } }
  ],
  "hooks": []
}
```

Static effects apply continuously. Hooks run only on their listed trigger. Hook conditions are ANDed; effect order is definition order, while definitions resolve by stable ID.

```json
{
  "schema_version": 1,
  "id": "expedition.passive.spark_fixture",
  "name": "SPARK FIXTURE",
  "description": "Projectile hits ignite their target.",
  "category": "explosive",
  "tags": ["projectile", "fire"],
  "pool": { "enabled": false, "weight": 1 },
  "static_effects": [],
  "hooks": [{
    "trigger": "on_hit",
    "conditions": [{ "kind": "attack_has_tag", "tag": "projectile" }],
    "effects": [{ "kind": "ignite", "radius": { "kind": "linear", "base": 0, "per_stack": 1 } }]
  }]
}
```

Multiple effects may compose in one hook:

```json
{
  "trigger": "on_component_break",
  "conditions": [],
  "effects": [
    { "kind": "small_explosion", "radius": { "kind": "linear", "base": 1, "per_stack": 1 }, "damage": { "kind": "constant", "value": 1 } },
    { "kind": "ignite", "radius": { "kind": "constant", "value": 1 } }
  ]
}
```

## Stack expressions

- `constant`: same `value` at all positive stacks.
- `linear`: `base + per_stack * (stacks - 1)`.
- `geometric`: `base * multiplier_per_stack^(stacks - 1)`.
- `every_n`: `base + per_step * floor((stacks - 1) / n)`.
- `thresholds`: sorted `{stacks, value}` breakpoints.

Expressions can set `min`/`max` only for real safety bounds. The editor previews stacks 1, 2, 3, 5 and 10.

## Registries and simulator

The Workbench gets triggers, conditions, effect fields and expression fields from runtime registries. Its simulator uses the same parser, condition evaluation and effect compiler as Expedition, but a temporary fixture state; it cannot mutate an active run or meta profile. It shows action → trigger → modifier → effect trace nodes.

## When data is insufficient

1. implement one generic Session/world primitive;
2. register its stable ID and field metadata in `src/expedition/modifiers.lua`;
3. validate fields and add runtime tests;
4. author any number of JSON modifiers with it.

Do not put Lua callbacks or source snippets in modifier JSON.
