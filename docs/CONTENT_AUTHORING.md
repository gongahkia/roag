# Content authoring from the template

1. Copy the template pack and select it with
   `JOMON_CONTENT_PACK=/path/to/pack` while authoring.
2. Keep `manifest.json` structurally valid. Set `playable` only when a world and
   all four setup option groups exist.
3. Add concrete mechanics to `systems.json` using stable IDs. Its core sections
   are `world`, `setup`, `items`, `actors`, `quests`, `routes`, and `recipes`.
   Its `activities` section contains independent empty lists for production,
   progression, preparation, chemistry, magic, circuits, vehicles, vessel,
   crises, situations, worklines, Draw, and Dice. Instantiate only systems the
   pack genuinely supports.
4. A world may add typed `features`: `base`, `maintenance_latch`, `access_gate`,
   and `objective_cache`. A latch opens a declared access ID only through a
   local interaction with its declared equipped item and, when declared, its
   stable `requires_capability_id`. A gate uses that access ID for collision.
   An objective cache names its owning operation and item.
5. `operations` is optional. Each operation explicitly references its quest,
   objective feature/item, return base, and methods. A method requires exactly
   one declared physical fact—an opened access ID or a defeated actor—and has a
   stable consequence ID. Do not put these rules in descriptions or
   `connections.json`.
6. Add display-only history and setting material to `lore.json`, non-mechanical
   relationships to `connections.json`, and optional visual/audio bindings to
   `assets.json`. Debug can bind an image resource with a pack-relative `path`
   and a `rect` string (`x,y,width,height`) under `terrain`, `features`, or
   `actors`; unresolved images retain the primitive fallback. These bindings are
   presentation-only and do not enter the mechanical fingerprint.
7. Run the automated suite under both Pygame renderers and verify a save/load
   continuation for any operation-bearing pack.

Optional setup rows may carry an engine-defined `health_bonus` integer. This is
mechanical and belongs in `systems.json`; names remain presentation. Item rows
may set `initial: false` for an item that enters inventory only through a
validated system interaction.

Never use names, descriptions, menu positions, pixel coordinates, or glyphs as
references. A system-owned route belongs in `routes`; a recipe dependency
belongs in `recipes`; an equipment rule belongs in an item definition.
`connections.json` cannot control gameplay.

`lore.json` entries use stable IDs and `{title, summary, body, tags}`.
`connections.json` rows use `{id, from, relation, to, tags}`. Both endpoints
must be declared stable IDs (a system entity or lore entry). Both are
presentation-only: changing them cannot affect RNG, commands, the mechanical
fingerprint, or saves.

A pack may optionally add a `crew` list. Each strict row is
`{id, kind, position, health, items, name, description}`. `items` are stable
item kinds and become distinct personal instances. Crew identity, health,
position, and kit are mechanical; name and description are presentation. Do
not use this domain for voting, living-member looting, memory inheritance, or
story relationships.

### Provisional neural carrier data

A crew-bearing pack may optionally add a strict `neural` section with
`record_definitions` (`[{"id": "record.example", "capability_ids": []}]`) and
`crew_initializers` (`member_id`, `carrier_item_kind_id`, and ordered
`record_definition_ids`). The carrier must be a zero-power `initial: false`
item kind and cannot also appear in that member's ordinary starting items.
New-world creation makes one deterministic installed carrier for each declared
initializer. An empty `record_definition_ids` list creates a real empty device,
not an ordinary item.

The item instance and every record use stable IDs. A record has an originating
member and a definition ID; neither is inferred from an item label or its
current custodian. Records cannot equip as gear and do not persist an event
history. A definition may grant only the current bounded vocabulary:
`capability.maintenance-service` or `capability.melee-diagonal`. These are
derived from records in the member's *installed* carrier; carried, empty, and
discarded source records grant nothing. Do not use presentation labels as a
mechanical key.

An opted-in neural section may also declare
`integration: {"site_feature_ids": ["feature.example"], "inherited_capacity": 1}`.
The site IDs must be declared local features and capacity is a nonnegative
integer. At one of those sites, `IntegrateNeuralRecordsCommand` retains a
complete capacity-limited foreign-origin selection in the active member's
installed device and empties the carried physical source. Own-origin records
remain capacity-exempt. The shared Pygame inventory detail flow exposes a
read-only selection and loss review before its separate final confirmation.
That destructive protocol is provisional: it has no copy/archive path. The
first playable currently maps Maintenance Practice to local maintenance service
and Close-Quarters Technique to one-cell diagonal melee. This is not a general
skill tree, XP system, personality model, or archive system.
