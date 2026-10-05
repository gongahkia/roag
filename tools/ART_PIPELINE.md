# ROAG Aseprite Art Pipeline

ROAG production sprites are authored as exact native pixels in Aseprite. Image
generation is allowed only for visual reference; never crop or ship a generated
sprite sheet.

## Current ART-01 state

`character.gunner` is the first production asset in `art/assets.json`. Its
editable Aseprite source, deterministic runtime sheet/metadata, and contact
sheet preview demonstrate the full pipeline. Other actors deliberately remain
on placeholder presentation until a later human review approves this workflow.

## Layout

```text
art/                     development-only sources, palette, references, previews
art/source/              editable .aseprite files
art/reference/           non-runtime concept references
art/previews/            generated contact sheets and inspection previews
assets/sprites/          packaged PNG/JSON runtime exports only
```

`art/` is excluded from `roag.love`. The game reads only
`assets/sprites/manifest.json`, runtime PNG sheets, and Aseprite-exported JSON.

## Everyday workflow

1. Read `art/STYLE.md` and the relevant `art/reference/...` reduction brief.
2. In Aseprite create the native 24 × 24 source canvas with transparent
   background; use `art/palettes/roag-base.json`.
3. Use layers such as `shadow`, `body`, `equipment`, and `highlights`.
4. Create `idle`, `move`, `attack`, and `hurt` tags. Keep pivot `(12, 21)` and
   body/feet stable; let PLAY-01 own movement and recoil.
5. Inspect a nearest-neighbour 8× render, correct pixels, and repeat. Do at
   least three inspect → edit iterations before declaring an asset ready.
6. Add or update the production manifest entry, then export and validate:

```console
ASEPRITE_BIN=/path/to/aseprite ./tools/export_art.sh
make art-validate
make package
```

`ASEPRITE_BIN` is authoritative and may point at a usable Windows executable
from WSL. Unlike gameplay validation, export is intentionally a host command:
Aseprite is licensed external authoring software and is not embedded in Docker
or in `roag.love`.

The exporter runs the source-pixel validator through Aseprite, emits a
horizontal PNG sheet plus standard `json-array` Aseprite metadata, canonicalizes
metadata key order, and rebuilds `assets/sprites/manifest.json`. Running it
twice without source changes must produce equivalent runtime data.

## Validation

`make art-validate` runs in the ROAG Docker image and validates the manifest,
palette, production metadata, pivots, frame rectangles/durations, duplicate
tags, and required tags. The Aseprite-side validator additionally rejects
non-opaque body pixels and colours outside the declared palette.

Packaging runs structural art validation before creating `roag.love`; malformed
committed runtime metadata cannot silently ship.

## Aseprite MCP

Preferred MCP: `Vollkorn-Games/aseprite-mcp`, installed outside this repository
under a user-local MCP directory. It uses Aseprite batch mode plus controlled
Lua operations, supports layers, frames, tags, palettes, pixel-grid drawing,
export, and nearest-neighbour inspection renders. Its stdio registration is
global Codex configuration, never repository configuration.

Verify after a Codex restart:

```console
codex mcp list
codex mcp get aseprite-mcp --json
```

Then run a disposable 8 × 8 create → draw → render → pixel-edit → save → export
smoke test through MCP before changing production art. Delete every smoke file.

## Adding the next character

Copy the Gunner manifest shape, keep a semantic `character.<id>` asset ID, add
a `runtime_binding.character_id`, and use the same source/export/validation
flow. Runtime animation selection is metadata-driven—never add hard-coded
sheet rectangles to the renderer.
