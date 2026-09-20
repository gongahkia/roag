# Character presentation content packs

`characters.json` is the author-editable presentation for Jomon's fixed
shipboard people and selected household/service role labels. It is validated
against the engine-owned `jomon/content_packs/contract.json` file.

Authors may edit the non-empty string values in this shape. `build_tendency`
is the short descriptive line shown on a person's detailed record.

```json
{
  "characters": {
    "npc.ship_bartender": {
      "display_name": "Sena Quill",
      "short_description": "Keeps Jomon's common room and knows which regional casks travel safely.",
      "initial_memory": "Sena took the bar on witnessed household shares.",
      "build_tendency": "material hospitality and firm limits"
    },
    "npc.ship_merchant": {
      "display_name": "Veyra Bale",
      "role_label": "itinerant deck factor",
      "short_description": "A coast-and-river factor who visits Jomon only when a recorded route cycle and regional stock justify the mooring.",
      "initial_memory": "Veyra first heard Jomon's name in four working markets."
    }
  },
  "roles": {
    "role.household_bargemaster": {"display_label": "bargemaster"}
  },
  "household": {
    "character_creation_background": "A {ancestry} adult raised in {origin}; now Jomon's {role_label}."
  }
}
```

Every character and role slot required by the contract must be present. The
semantic keys, object field names, and the three household template
placeholders are part of that contract and must not change.

Do not edit `people.json` merely to rename these fixed people or role labels.
Its role IDs, equipment, techniques, recruit templates, and name pools are
mechanical or procedural inputs. Do not change contract engine IDs, role IDs,
stats, equipment, techniques, inventories, prices, schedules, or spawn rules
when writing character presentation.
