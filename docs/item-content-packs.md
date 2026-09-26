# Base item presentation content packs

`jomon/content_packs/default/items.json` is the writing-only presentation layer
for the ordinary base goods and equipment covered by the engine contract. Each
record is named by an engine-owned semantic slot and contains only the fields
listed for that slot in `jomon/content_packs/contract.json`.

```json
{
  "items": {
    "item.equipment_002": {
      "display_name": "Ash spear",
      "description": "Braces and attacks at reach.",
      "short_description": "reach-two attack and positional control"
    },
    "item.goods_014": {
      "display_name": "Willow Dressing",
      "description": "A bitter wrap that treats one expedition injury."
    }
  }
}
```

Authors may revise only the supplied presentation values: `display_name`,
`description`, and, where present in the contract, `short_description`.
They must not add or remove slots, rename semantic IDs, or add engine IDs,
stats, costs, dimensions, equipment slots, effects, recipes, requirements,
stock rules, or numeric fields.

The item keys in `goods.json` and `equipment.json` remain the mechanical and
save identities. They determine item shape, weight, combat values, tags,
merchant stock, recipe inputs, and ordering. The selected pack supplies the
text rendered for the contracted base items. Systems outside this bounded
base catalog, such as work weapons, expanded weapons, preparations, and arc
relics, retain their existing rendering until their dedicated content slices.
