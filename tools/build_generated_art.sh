#!/bin/sh
# Reproducible imagegen-sheet compiler. Default dry run; --apply activates last.
set -eu
case ${1:-} in ''|--apply) ;; *) printf 'Usage: sh tools/build_generated_art.sh [--apply]\n' >&2;exit 2;; esac
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
work=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-art-build-XXXXXX")
cat > "$work/conf.lua" <<'LUA'
function love.conf(t)
    t.identity='roag-art-build';t.window=false;t.modules.graphics=false
    t.modules.audio=false;t.modules.joystick=false;t.modules.physics=false
end
LUA
cat > "$work/main.lua" <<'LUA'
function love.load()
    local root=assert(os.getenv('ROAG_ART_ROOT'))
    package.path=root..'/?.lua;'..package.path
    local ok,why=pcall(require('tools.build_generated_art').run,root)
    if not ok then io.stderr:write(tostring(why)..'\n');love.event.quit(1)
    else love.event.quit()end
end
LUA
ROAG_ART_ROOT="$project_dir" ROAG_ART_RESULT="$work/result.txt" \
    XDG_DATA_HOME="$work/data" timeout 45s love "$work"
{
    read -r build_dir
    read -r fingerprint
} < "$work/result.txt"
case $fingerprint in ''|*[!0-9a-f]*) printf 'Invalid build fingerprint\n' >&2;exit 1;; esac
test "${#fingerprint}" -eq 16
printf 'Plan: four PNG pages -> assets/generated/builds/%s/; activate matching manifest last\n' "$fingerprint"
if [ "${1:-}" = --apply ];then
    target="$project_dir/assets/generated/builds/$fingerprint"
    mkdir -p "$target"
    for name in actors backgrounds icons terrain;do
        if [ -e "$target/$name.png" ];then
            cmp "$build_dir/$name.png" "$target/$name.png" || {
                printf 'Refusing to overwrite a conflicting immutable page: %s\n' "$target/$name.png" >&2
                exit 1
            }
        else
            cp "$build_dir/$name.png" "$target/$name.png"
        fi
        cmp "$build_dir/$name.png" "$target/$name.png"
    done
    if ! cmp -s "$build_dir/manifest.txt" "$project_dir/assets/generated/manifest.txt";then
        cp "$build_dir/manifest.txt" "$project_dir/assets/generated/manifest.txt"
    fi
    printf 'Applied %s; previous build and raw sources preserved.\n' "$fingerprint"
else
    printf 'DRY RUN: checkout untouched; inspect build at %s\n' "$build_dir"
fi
