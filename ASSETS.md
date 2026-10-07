# Runtime visual assets

ROAG has one active game-art source: the committed local Loveable Rogue collage
at `assets/art_packs/loveable_rogue.png`. `tools/extract_loveable_rogue_atlas.sh`
mechanically crops its documented native-pixel 256×256 source panel into the
runtime atlas at `assets/visual/loveable_rogue_atlas.png`.

The semantic mapping and bitmap-font rows are explicit in
`content/presentation/loveable_rogue_atlas.json`. Runtime validation rejects
missing mappings rather than substituting procedural shapes or another pack.

The repository's former alternative art packs and TTF runtime font were
removed in VIS-01. The normal game archive contains only the extracted runtime
atlas and its metadata, not the collage or authoring/import tooling.
