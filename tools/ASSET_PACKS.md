# Future Asset-Pack Intake Contract

ROAG currently ships no actor sprite pack. Do not add a third-party asset
without recording its provenance first.

## Required pack manifest

Each future pack should include a small JSON or Markdown record containing:

```text
pack_id
display_name
author
source_reference
license
attribution_requirement
attribution_text
native_dimensions
files
runtime_asset_mapping
```

`files` must identify every imported file and its source-relative path.
`runtime_asset_mapping` maps ROAG presentation IDs (for example
`character.gunner`) to optional runtime asset IDs. It is presentation-only;
combat definitions, saves, and simulation must never depend on an imported
texture.

## Intake rules

1. Confirm the license permits the intended distribution before copying a file.
2. Preserve the original source reference, author, license, and required
   attribution verbatim in the pack record.
3. Keep original downloaded material separate from normalized runtime assets
   when the license permits redistribution.
4. Normalize filenames, transparent padding, pivots, frame dimensions, and
   animation names into the optional ROAG runtime manifest described in
   [VISUAL_ASSETS.md](VISUAL_ASSETS.md).
5. Use nearest-neighbour rendering. Any atlas extraction or metadata conversion
   must be deterministic and may use ordinary image tooling; no specific art
   editor is required.
6. Validate that removing the entire optional pack still leaves all actors
   playable and readable through shape fallback.

There are intentionally no pack entries or third-party files in this tranche.
