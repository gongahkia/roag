
### Landforms and discoveries

`topology_text.json` also owns generated landform and field-discovery presentation. The engine catalogues keep landform glyphs, encounter pools, discovery anchors, reward IDs, requirements, coordinates, and stable link/cache IDs. Pack entries may rewrite landform, structure, traveller, cache, clue, and encounter wording, but must not change the `landform_<ordinal>`, `landform:<region>:link:<role>`, or `<region>-<discovery-id>` identities selected by the engine.

Generated `generation_facts` now retain coordinate summaries for landforms and structures. They are rendered through topology templates in new history; old rendered facts remain frozen compatibility text.
