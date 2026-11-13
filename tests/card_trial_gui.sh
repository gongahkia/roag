#!/bin/sh
# Real game callbacks, source crops and disk-isolation check; disposable app/data.
set -eu
readme_hash=$(sha256sum "$(dirname "$0")/../README.md" | cut -d ' ' -f 1)
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/tmp}/roag-card-gui-XXXXXX")}
mkdir -p "$output/app/src" "$output/app/assets" "$output/app/tests" "$output/data/love/roag-foundation"
output=$(CDPATH= cd "$output" && pwd)
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp "$project_dir"/src/*.lua "$output/app/src/"
cp "$project_dir/tests/card_trial_gui.lua" "$output/app/main.lua"
cp "$project_dir/tests/card_trial_policy.lua" "$output/app/tests/"
cp -R "$project_dir/assets/fonts" "$output/app/assets/"
(cd "$project_dir"; luajit - <<'LUA'
for path in pairs(require('src.classic_sprite_data').sources)do print(path)end
LUA
) > "$output/sources.txt"
while IFS= read -r path; do
    mkdir -p "$(dirname "$output/app/$path")"
    cp "$project_dir/$path" "$output/app/$path"
done < "$output/sources.txt"
(cd "$project_dir"; ROAG_CARD_FIXTURE_DATA="$output/data/love/roag-foundation" luajit - <<'LUA'
local root=assert(os.getenv('ROAG_CARD_FIXTURE_DATA'))
local store={read=function(_,n)local f=io.open(root..'/'..n,'rb');if f then local b=f:read('*a');f:close();return b end end,
 write=function(_,n,b)local f=assert(io.open(root..'/'..n,'wb'));f:write(b);f:close();return true end}
assert(require('src.persistence').new(store):replace(require('src.game').new(987)))
assert(require('src.profile').new(store):save(require('src.profile').fresh()))
assert(require('src.presentation_settings').save(store,require('src.presentation_settings').defaults()))
LUA
)
python3 - "$output" <<'PY'
import hashlib,json,sys
from pathlib import Path
r=Path(sys.argv[1]);d=r/'data'
(r/'storage-before.json').write_text(json.dumps({str(p.relative_to(d)):hashlib.sha256(p.read_bytes()).hexdigest() for p in d.rglob('*') if p.is_file()},sort_keys=True))
PY
ROAG_CARD_GUI_OUTPUT="$output" XDG_DATA_HOME="$output/data" ROAG_GUI_AUTOSTART=0 ROAG_SPECTATOR=0 \
    timeout -k 3s 420s love "$output/app"
cp "$project_dir/tests/card_trial_preview_gui.lua" "$output/app/main.lua"
ROAG_CARD_GUI_OUTPUT="$output" XDG_DATA_HOME="$output/data" ROAG_GUI_AUTOSTART=0 ROAG_SPECTATOR=0 \
    timeout -k 3s 50s love "$output/app" --card-trial --trial-seed=202
python3 - "$output" <<'PY'
import hashlib,json,sys
from pathlib import Path
r=Path(sys.argv[1]);d=r/'data'
after={str(p.relative_to(d)):hashlib.sha256(p.read_bytes()).hexdigest() for p in d.rglob('*') if p.is_file()}
assert json.loads((r/'storage-before.json').read_text())==after,'Normal saves/profile/settings changed'
assert (r/'gui-results.txt').is_file(),'missing GUI completion'
assert (r/'preview-results.txt').is_file(),'missing preview fixture completion'
print((r/'gui-results.txt').read_text())
print((r/'preview-results.txt').read_text())
print('PASS normal active run, profile and presentation files unchanged')
print('Screenshots and action receipts:',r)
PY
test "$readme_hash" = "$(sha256sum "$project_dir/README.md" | cut -d ' ' -f 1)"
printf '%s\n' 'PASS README hash unchanged'

