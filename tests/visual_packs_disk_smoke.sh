#!/bin/sh
# Separate LuaJIT processes; no LÖVE or live player data.
set -eu
cd "$(dirname "$0")/.."
output_dir=$(mktemp -d /var/tmp/roag-fixed-look-disk-XXXXXX)
printf '%s' 'retired classic preference' > "$output_dir/visual-pack-a.roag"
printf '%s' 'retired mixed preference' > "$output_dir/visual-pack-b.roag"
for requested in classic kenney onebit; do
 ROAG_SPRITE_PACK="$requested" ROAG_FIXED_LOOK_DIR="$output_dir" luajit - <<'LUA'
local P=require('src.visual_packs');local G=require('src.game');local C=require('src.persistence')
local root=assert(os.getenv('ROAG_FIXED_LOOK_DIR'));local before=C.encode(G.snapshot(G.new_stage(17)))
assert(P.default=='onebit' and not P.save and not P.inspect)
local r=P.build(function()error('retired manifest access')end,function()error('loose art access')end,
 function()return {style='onebit',resolve=function()return {source='onebit'}end},'loaded'end)
assert(r:resolve('core:actor/adventurer').source=='onebit')
assert(C.encode(G.snapshot(G.new_stage(17)))==before)
for _,v in ipairs({{'a','retired classic preference'},{'b','retired mixed preference'}})do
 local f=assert(io.open(root..'/visual-pack-'..v[1]..'.roag','rb'));assert(f:read('*a')==v[2]);f:close()
end
print('PASS fixed look ignores '..os.getenv('ROAG_SPRITE_PACK')..'; old preferences and deterministic run unchanged')
LUA
done
printf 'Fixed-look process artifacts: %s\n' "$output_dir"
