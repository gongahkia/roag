# ROAG Studio

ROAG Studio is the repository-local authoring workspace. It deliberately keeps
all tools separate from normal gameplay and active-run persistence.

```console
love studio
```

The home screen links to the focused tools:

- **Screen Composer** edits `content/screens/legacy.json`: validated,
  serializable title-facing copy, layout type, and palette token.
- **Visual Atlas Contract** is the fixed, validated Loveable Rogue mapping at
  `content/presentation/loveable_rogue_atlas.json`. It is deliberately not an
  arbitrary pack selector or sprite-painting surface.
- **Room Workbench** (`love level_editor --room-editor`) edits shared Dungeon
  and Reactor templates.
- **Generation Inspector** (`love level_editor`) previews deterministic floors
  without opening or modifying a run.

It is a substantial foundation for ROAG's own 2D content workflow, rather than
a misleading claim to already be a general-purpose Godot/GameMaker replacement:
there is intentionally no arbitrary scripting VM, physics editor, scene graph,
or export pipeline. The shared seams are JSON screen definitions, the fixed
visual atlas contract, and the existing validated room schema.
