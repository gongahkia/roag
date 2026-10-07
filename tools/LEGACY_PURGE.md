# VIS-01 legacy purge

## Removed

VIS-01 removed the former multi-pack art path:

- `src/rendering/art_packs.lua` and its selectable-pack catalog;
- `src/rendering/presentation_assets.lua`, optional sprite manifest fallback,
  and the ART-01 compatibility path;
- `src/rendering/actor_glyphs.lua` and `src/rendering/world_glyphs.lua`;
- art-pack persistence/config/catalog/export modules;
- `sprite_editor/`, `sprite_map.lua`, its mapping JSON, and its tests;
- all non-selected bundled art packs and the retired BigBlue TTF font;
- old pack configuration/content/tests/docs and the `sprite-editor` Make/Compose entry.

The removed code either selected unrelated packs, offered a dormant mapping
workflow, or silently fell back to procedural art. Git history is the archive.

## Retained compatibility

| Path | Why it remains |
|---|---|
| Campaign/Open World Sandbox | Still title-accessible as an explicitly optional supported compatibility mode. It uses the same Loveable Rogue renderer; it does not retain another renderer. |
| `src/ui/cursor_manager.lua` + Kenney cursor files | Editor/application pointer affordances, not game-world artwork or an actor sprite pack. Studio and level tools use it. |
| Legacy-named content files under `content/*/legacy.lua` | They remain active authoritative content for the supported Campaign/Sandbox and shared registry. The name describes origin, not dead code. |
| Existing save persistence modules | Current save boundaries remain needed; VIS-01 did not alter simulation/save formats. |

No active world/actor visual fallback remains. A missing Loveable Rogue binding
is a validation/runtime error during development.
