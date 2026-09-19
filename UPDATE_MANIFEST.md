# Update manifest — shared v0.3.0 to v0.4.0

This is a complete replacement tree for a separate source folder, not a patch over
unshared local edits. The original v0.3.0 ZIP was the baseline. Documentation and
old evidence have been relocated to labelled historical directories.

## Changed (34)

- `.gitignore`
- `CHANGELOG.md`
- `PACKAGE_NOTES.md`
- `PROJECT_STATE.md`
- `README.md`
- `RESEARCH.md`
- `TEST_REPORT.md`
- `UPGRADE.md`
- `config.lua`
- `docs/GENERATION.md`
- `docs/MAP_FORMAT.md`
- `main.lua`
- `src/benchmark.lua`
- `src/biomes.lua`
- `src/colonists.lua`
- `src/commands.lua`
- `src/expedition.lua`
- `src/generate.lua`
- `src/generation/benchmark.lua`
- `src/generation/frontier.lua`
- `src/generation/layouts.lua`
- `src/generation/report.lua`
- `src/history.lua`
- `src/jobs.lua`
- `src/mapfile.lua`
- `src/metrics.lua`
- `src/render.lua`
- `src/sim.lua`
- `src/structures.lua`
- `src/world.lua`
- `tests/all.lua`
- `tests/maps.lua`
- `tests/syntax.lua`
- `tools/map.lua`

## Added (66)

- `AGENTS.md`
- `HANDOFF.md`
- `Makefile`
- `docs/ARCHITECTURE.md`
- `docs/ECOLOGY_RULES.md`
- `docs/WORKFORCE.md`
- `docs/legacy-v030/CHANGELOG.md`
- `docs/legacy-v030/GENERATION.md`
- `docs/legacy-v030/MAP_FORMAT.md`
- `docs/legacy-v030/PACKAGE_NOTES.md`
- `docs/legacy-v030/PROJECT_STATE.md`
- `docs/legacy-v030/README.md`
- `docs/legacy-v030/SOURCE_SHA256.txt`
- `docs/legacy-v030/TEST_REPORT.md`
- `docs/legacy-v030/UPDATE_MANIFEST.md`
- `docs/legacy-v030/UPGRADE.md`
- `docs/prior-verification/README.md`
- `docs/prior-verification/benchmark-native-lua54.csv`
- `docs/prior-verification/benchmark-native-lua54.txt`
- `docs/prior-verification/benchmark-smoke-output.txt`
- `docs/prior-verification/core-test-output.txt`
- `docs/prior-verification/core-tests-lua54.txt`
- `docs/prior-verification/default-colony-lua54.txt`
- `docs/prior-verification/default-headless-output.txt`
- `docs/prior-verification/generation-benchmark-lua54.csv`
- `docs/prior-verification/generation-benchmark-lua54.csv.manifest.txt`
- `docs/prior-verification/gui-test-output.txt`
- `docs/prior-verification/syntax-test-output.txt`
- `docs/verification-v040/benchmark-smoke-lua54.txt`
- `docs/verification-v040/cli-comparison.json`
- `docs/verification-v040/core-lua54.txt`
- `docs/verification-v040/default-colony-lua54.txt`
- `docs/verification-v040/expansion-gui-lua54.txt`
- `docs/verification-v040/generation-lua54.csv`
- `docs/verification-v040/generation-lua54.csv.manifest.txt`
- `docs/verification-v040/generation-lua54.txt`
- `docs/verification-v040/gui-lua54.txt`
- `docs/verification-v040/map-gui-lua54.txt`
- `docs/verification-v040/maximum-size-lua54.txt`
- `docs/verification-v040/package-core-lua54.txt`
- `docs/verification-v040/soak-lua54.csv`
- `docs/verification-v040/soak-lua54.txt`
- `docs/verification-v040/syntax-lua54.txt`
- `maps/README.md`
- `maps/examples/living-chasms-19.dwmap.json`
- `maps/examples/living-labyrinth-456.dwmap.json`
- `maps/examples/living-roots-7.dwmap.json`
- `src/blasts.lua`
- `src/catalog.lua`
- `src/colony_commands.lua`
- `src/content.lua`
- `src/ecology.lua`
- `src/fieldwork.lua`
- `src/generation/frontier_v1.lua`
- `src/generation/wonders.lua`
- `src/labor.lua`
- `src/signals.lua`
- `src/ui/crew.lua`
- `src/ui/fieldnotes.lua`
- `src/ui/world_content.lua`
- `tests/data/legacy-v030-expected.txt`
- `tests/data/legacy-v030.dw`
- `tests/expansion.lua`
- `tests/expansion_gui.lua`
- `tests/maximum_size.lua`
- `tools/expansion_soak.lua`

## Unchanged (31)

- `conf.lua`
- `maps/examples/cellular-12345.dwmap.json`
- `maps/examples/faults-12345.dwmap.json`
- `maps/examples/hybrid-12345.dwmap.json`
- `maps/examples/noise-12345.dwmap.json`
- `maps/examples/vaults-12345.dwmap.json`
- `maps/examples/worms-12345.dwmap.json`
- `src/codec.lua`
- `src/generate_legacy.lua`
- `src/json.lua`
- `src/mapstore.lua`
- `src/materials.lua`
- `src/nav.lua`
- `src/noise.lua`
- `src/particles.lua`
- `src/random.lua`
- `src/storage.lua`
- `src/util.lua`
- `tests/benchmark_smoke.lua`
- `tests/data/legacy-v020-expected.txt`
- `tests/data/legacy-v020.dw`
- `tests/fixtures.lua`
- `tests/gui_smoke.lua`
- `tests/love_mock.lua`
- `tests/map_gui.lua`
- `tests/run.lua`
- `tests/suite.lua`
- `tools/benchmark.lua`
- `tools/generation_benchmark.lua`
- `tools/headless.lua`
- `tools/replay.lua`

## No longer at original path (11)

- `docs/benchmark-native-lua54.csv`
- `docs/benchmark-native-lua54.txt`
- `docs/benchmark-smoke-output.txt`
- `docs/core-test-output.txt`
- `docs/core-tests-lua54.txt`
- `docs/default-colony-lua54.txt`
- `docs/default-headless-output.txt`
- `docs/generation-benchmark-lua54.csv`
- `docs/generation-benchmark-lua54.csv.manifest.txt`
- `docs/gui-test-output.txt`
- `docs/syntax-test-output.txt`

`SOURCE_SHA256.txt` is regenerated for the final tree and deliberately excludes itself.
The complete source also appears file-by-file in the separately supplied copy/paste guide.
