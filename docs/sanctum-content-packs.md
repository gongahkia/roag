# Sanctum content packs

`content_packs/<pack>/sanctum_text.json` owns the player-facing sanctum fiction:
site themes and names, witness and boss wording, cache and link labels, shrine
choices, records, and inspection text. Each `text` key is declared in
`jomon/content_packs/contract.json`; templates may use only their declared
placeholders.

The engine owns `sanctums.json`, site/region/network IDs, boss profile and
role, duty, health, glyph, rewards, encounter draws, topology, and all shrine
consequences. Sanctum links use stable IDs such as
`sanctum:hearthford:link:entry`; their editable labels do not identify or sort
traversal mechanics. Encounter events use stable IDs such as
`quiet_witnesses`, never the text shown to the player.

Authors may revise only `sanctum_text.json`. They must not alter sanctum IDs,
network IDs, boss mechanics, encounter IDs, cache IDs, link IDs, choices,
requirements, costs, or the contract placeholder list. Existing saved rendered
memories remain historical text; legacy default event phrases are mapped only
by the engine's exact compatibility table and never by active pack wording.
