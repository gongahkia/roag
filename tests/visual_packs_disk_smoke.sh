#!/bin/sh
# Two separate LuaJIT processes, no LÖVE window or player save directory.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-visual-pack-XXXXXX")
cd "$project_dir"
export ROAG_PACK_SMOKE_DIR="$output_dir"

luajit - <<'LUA'
local Packs=require('src.visual_packs')
local root=assert(os.getenv('ROAG_PACK_SMOKE_DIR'))
local store={}
function store:read(name)
    local file=io.open(root..'/'..name,'rb')
    if not file then return nil end
    local bytes=file:read('*a');file:close();return bytes
end
function store:write(name,bytes)
    local file=assert(io.open(root..'/'..name,'wb'))
    assert(file:write(bytes));assert(file:close());return true
end
assert(Packs.save(store,'kenney'))
assert(Packs.save(store,'classic'))
assert(Packs.inspect(store).mode=='classic')
LUA

luajit - <<'LUA'
local Packs=require('src.visual_packs')
local root=assert(os.getenv('ROAG_PACK_SMOKE_DIR'))
local store={}
function store:read(name)
    local file=io.open(root..'/'..name,'rb')
    if not file then return nil end
    local bytes=file:read('*a');file:close();return bytes
end
local found=Packs.inspect(store)
assert(found.status=='loaded' and found.mode=='classic')
local file=assert(io.open(root..'/'..Packs.files[2],'wb'))
assert(file:write('interrupted write'));assert(file:close())
found=Packs.inspect(store)
assert(found.status=='recovered' and found.mode=='kenney')
print('Sprite-pack fresh-process persistence PASS: '..root)
LUA
