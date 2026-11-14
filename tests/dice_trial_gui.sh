#!/bin/sh
# A disposable copy and storage root for real native input, without system installs.
set -eu
project_dir=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
output=${ROAG_DICE_GUI_OUTPUT:-/tmp/roag-dice-gui}
mkdir -p "$output/app" "$output/data/love/roag-foundation"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/tests/dice_trial_gui.lua" "$output/app/main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
ln -sfn "$project_dir/src" "$output/app/src"
ln -sfn "$project_dir/assets" "$output/app/assets"
ln -sfn "$project_dir/content" "$output/app/content"
ln -sfn "$project_dir/tests" "$output/app/tests"
(cd "$project_dir"; ROAG_DICE_FIXTURE="$output/data/love/roag-foundation" luajit - <<'LUA'
local root=assert(os.getenv('ROAG_DICE_FIXTURE'))
local store={read=function(_,n)local f=io.open(root..'/'..n,'rb');if f then local b=f:read('*a');f:close();return b end end,
 write=function(_,n,b)local f=assert(io.open(root..'/'..n,'wb'));f:write(b);f:close();return true end}
assert(require('src.persistence').new(store):replace(require('src.game').new(987)))
assert(require('src.profile').new(store):save(require('src.profile').fresh()))
assert(require('src.presentation_settings').save(store,require('src.presentation_settings').defaults()))
LUA
)
python3 - "$output" <<'PYTHON'
import hashlib,json,sys
from pathlib import Path
r=Path(sys.argv[1]);d=r/'data/love/roag-foundation'
(r/'storage-before.json').write_text(json.dumps({p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in d.iterdir() if p.is_file()},sort_keys=True))
PYTHON
export ROAG_DICE_GUI_OUTPUT="$output" XDG_DATA_HOME="$output/data"
exec "${LOVE_BIN:-love}" "$output/app" --dice-trial --trial-seed=202
