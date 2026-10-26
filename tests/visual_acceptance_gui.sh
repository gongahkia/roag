#!/bin/sh
# Fixed 1-Bit pixels, automatic emergency fallback and refresh.
# Replaces the acceptance fixture for the deliberately removed Dungeon pack.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-visual-acceptance-XXXXXX")}
sh "$project_dir/tests/onebit_gui_smoke.sh" "$output/packs"
sh "$project_dir/tests/attack_preview_gui_smoke.sh" "$output/previews"
