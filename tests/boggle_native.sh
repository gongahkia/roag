#!/bin/sh
# Run after boggle_gui.sh created a disposable app/data fixture at $1.
# Native xdotool input; observer only records state/metrics and supplies SDL quit.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
fixture=$(CDPATH= cd "${1:?Pass the boggle_gui.sh fixture directory}" && pwd)
output="$fixture/native"
mkdir -p "$output"
cp -R "$fixture/app" "$output/app"
cp -R "$fixture/data" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir"/src/*.lua "$output/app/src/"
cp "$project_dir/tests/boggle_native_observer.lua" "$output/app/main.lua"
export ROAG_BOGGLE_NATIVE_OUTPUT="$output" XDG_DATA_HOME="$output/data" ROAG_GUI_AUTOSTART=0 ROAG_SPECTATOR=0
love "$output/app" --boggle-trial --trial-seed=202 &
game_pid=$!
trap 'kill "$game_pid" 2>/dev/null || true' EXIT HUP INT TERM
python3 "$project_dir/tests/boggle_native.py"
wait "$game_pid"
trap - EXIT HUP INT TERM
python3 - "$fixture" "$output" <<'PY'
import hashlib,json,sys
from pathlib import Path
fixture,out=map(Path,sys.argv[1:]);data=out/'data'
after={str(p.relative_to(data)):hashlib.sha256(p.read_bytes()).hexdigest() for p in data.rglob('*') if p.is_file()}
assert after==json.loads((fixture/'storage-before.json').read_text()),'Normal fixture files changed'
print((out/'native-results.txt').read_text())
print('PASS native storage isolation; metrics:',out/'native-metrics.roag')
PY
