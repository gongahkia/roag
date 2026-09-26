# Character presentation content packs

`characters.json` is the author-editable presentation for Jomon's fixed
shipboard people, the fixed starting second contact, and selected
household/service role labels. It is validated
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
      "short_description": "A coast-and-river factor who visits Jomon only when a recorded route cycle and regional stock justify the mooring.",
      "initial_memory": "Veyra first heard Jomon's name in four working markets.",
      "build_tendency": "bounded tools, witnessed exchange, and regional shortages"
    },
    "npc.hearthford_second_contact": {
      "display_name": "Tomas Reed",
      "role_label": "millwright speaker"
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

The generated six-adult household keeps its existing name pools, mechanical
role IDs, equipment, techniques, and deterministic generation order in
`people.json`. Its authored role labels and character-creation background
template are in this file. The fixed second contact's name also remains a
possible procedural contact name in `people.json`; this file is the canonical
presentation for that one static contact slot.
