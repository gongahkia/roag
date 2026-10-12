#!/bin/sh
# Synthetic published PNG corpus; measures compilation separately from drawing.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
out=$(mktemp -d "${TMPDIR:-/tmp}/roeg-studio-e05-stress-XXXXXX")
app="$out/app"
mkdir -p "$app/assets/studio/sources" "$out/data"
cp "$project_dir/main.lua" "$project_dir/conf.lua" "$app/"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/tools" "$app/tools"
(cd "$app" && luajit - <<'LUA'
local Project=require('src.studio_project')
local PNG=require('src.studio_png')
local project=Project.new('stress_atlas','Stress atlas')
for i=1,120 do
    local id=('studio:tile/sample_%03d'):format(i)
    local a=Project.asset(id,id,'tile')
    a.width=9+i%17;a.height=7+i%13
    a.format='png';a.source=('sources/sample_%03d.png'):format(i)
    a.filter=i%5==0 and 'linear' or 'nearest'
    project.assets[#project.assets+1]=a
    local pixels={}
    for y=0,a.height-1 do for x=0,a.width-1 do
        pixels[#pixels+1]=string.char((x*13+i*3)%256,(y*19+i)%256,
            (x+y+i)%256,(x+y)%3==0 and 0 or 255)
    end end
    local f=assert(io.open('assets/studio/'..a.source,'wb'))
    f:write(PNG.encode(a.width,a.height,table.concat(pixels)));f:close()
end
local f=assert(io.open('assets/studio/manifest.txt','wb'))
f:write(Project.encode(project));f:close()
LUA
)
if [ "$(uname -s)" = Darwin ]; then
    XDG_DATA_HOME="$out/data" TMPDIR="${TMPDIR:-/tmp}" \
        /usr/bin/time -l -p timeout 40s love "$app" --atlas-build --atlas-max=128 \
        >"$out/build.log" 2>"$out/build-time.txt"
else
    XDG_DATA_HOME="$out/data" TMPDIR="${TMPDIR:-/tmp}" \
        /usr/bin/time -f 'build_wall_s=%e build_cpu_user_s=%U build_max_rss_kib=%M' \
        -o "$out/build-time.txt" timeout 40s love "$app" --atlas-build --atlas-max=128 \
        >"$out/build.log" 2>&1
fi
save_dir="$out/data/love/roeg-foundation"
if [ ! -d "$save_dir/studio/atlas-build" ]; then
    candidate=$(sed -n 's/^Atlas candidate: .* page(s), \(.*\)studio\/atlas-build\/$/\1/p' "$out/build.log")
    save_dir=${candidate%/}
fi
test -d "$save_dir/studio/atlas-build"
(cd "$app" && luajit tools/studio_atlas_publish.lua --save-dir "$save_dir" \
    --project stress_atlas >"$out/dry-run.log")
pages=$(rg -c '^page ' "$out/dry-run.log")
test "$pages" -gt 1
printf 'E05 stress: 120 PNG assets, %s pages in %s\n' "$pages" "$out"
cat "$out/build-time.txt"
