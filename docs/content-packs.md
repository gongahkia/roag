
### Landforms and discoveries

`topology_text.json` also owns generated landform and field-discovery presentation. The engine catalogues keep landform glyphs, encounter pools, discovery anchors, reward IDs, requirements, coordinates, and stable link/cache IDs. Pack entries may rewrite landform, structure, traveller, cache, clue, and encounter wording, but must not change the `landform_<ordinal>`, `landform:<region>:link:<role>`, or `<region>-<discovery-id>` identities selected by the engine.

Generated `generation_facts` now retain coordinate summaries for landforms and structures. They are rendered through topology templates in new history; old rendered facts remain frozen compatibility text.

Legacy landform links without an ID are recovered from their stable region, endpoints, and vertical role. This recovery never reads selected-pack labels.

### Situations

`situation_text.json` owns the titles, groups, duties, material descriptions, answer labels, consequences, records, reports, and overlay wording for regional micro-sites. `situations.json` retains only stable situation identity, region/band/anchor placement, and material-effect IDs. Situation choices use the stable `tool`, `material_rope`, `material_oil`, and `account` outcome identities; rendered outcomes and memories remain frozen history.
