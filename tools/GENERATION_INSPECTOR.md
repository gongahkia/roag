# Generation Inspector

The inspector constructs an isolated initial floor from the normal authoritative
stage pipeline. It never loads, overwrites, or autosaves `active_run.json`.

```sh
love . --generation-inspector
```

Controls:

- `[` / `]`: choose a generated stage/biome
- type digits, `Backspace`, `Enter`: set and regenerate the seed
- `R`: regenerate the current seed; `N`: advance to the next seed
- mouse wheel: zoom; middle-mouse drag, arrow keys, or `WASD`: pan; `F`: fit
- left click: pin a cell; hover shows the current coordinate
- `1` terrain, `2` connectivity, `3` actors, `4` objects, `5` hazards,
  `6` liquids, `7` gas, `8` power, `9` objectives, `C` conductivity,
  `T` authored dungeon room chunks/connectors, `M` placement provenance
- `H`: toggle the compact help panel; `Esc`: leave inspector mode

The initial game has no retained hidden spawner/activation-region entities.
The inspector consequently shows the actual initial actors/objectives and
placement provenance/derived stream labels, not invented future spawners.
The normal exit is created at runtime after objectives are complete, so initial
reports validate required target reachability and label the exit accordingly.

## Batch diagnostics

```sh
luajit tools/analyze_generation.lua --stage cave --seed 1000 --count 500
luajit tools/analyze_generation.lua --stage dungeon --seed 2000 --count 100 --json /tmp/dungeon-report.json
```

The command reports structural failures with exact seeds, ranges for key
metrics, material/object distributions, selected outlier seeds, and—on the
template dungeon—room counts, template usage, rotations, and connector-pattern
usage. Its JSON output uses the independent diagnostic envelope
`roag.generation_report` v1. Use a failed or outlier seed directly in the
inspector for visual diagnosis.
