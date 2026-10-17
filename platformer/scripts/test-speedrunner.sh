#!/usr/bin/env bash
set -euo pipefail
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
if [[ ! -f "$repo_dir/data.zip" ]]; then
  "$repo_dir/scripts/fetch-assets.sh"
fi
python3 "$repo_dir/scripts/prepare-sprites.py"
"$repo_dir/scripts/godot.sh" --headless --path "$repo_dir/speedrunner" --editor --import >/dev/null
for test in test_runner test_courses test_workshop test_identity test_solo test_bots; do
  "$repo_dir/scripts/godot.sh" --headless --path "$repo_dir/speedrunner" \
    --fixed-fps 120 --script "res://tests/${test}.gd" -- --testing
done
