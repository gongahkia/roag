#!/bin/sh
# Two fresh LuaJIT processes use only a temporary on-disk save directory.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
save_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-build-save-XXXXXX")
cd "$project_dir"
ROAG_BUILD_SAVE_DIR="$save_dir" luajit <<'LUA'
local G=require('src.game')
local P=require('src.persistence')
local S=require('src.scheduler')
local dir=assert(os.getenv('ROAG_BUILD_SAVE_DIR'))
local storage={}
function storage:read(name)
    local f=io.open(dir..'/'..name,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
function storage:write(name,bytes)
    local f=assert(io.open(dir..'/'..name,'wb'));f:write(bytes);f:close();return true
end
local state=G.new(951)
G.set_boon_stacks(state,state.player_id,'core:boon/emberbrand','rare',1)
G.set_boon_stacks(state,state.player_id,'core:boon/temporal_echo','uncommon',1)
local target=G.spawn_enemy(state,'core:enemy/mossbound_guard',3,2)
S.remove_for_actor(state,target.id)
assert(G.submit(state,{kind='basic_attack',dx=1,dy=0}))
assert(target.statuses and target.statuses.burning and target.statuses.burning.next_due==200)
assert(next(state.delayed_effects))
assert(P.new(storage):save(state))
assert(G.submit(state,{kind='wait'}))
local f=assert(io.open(dir..'/expected.bytes','wb'))
f:write(P.encode(G.snapshot(state)));f:close()
print('written active run with Burning and Temporal Echo to temporary disk storage')
LUA
ROAG_BUILD_SAVE_DIR="$save_dir" luajit <<'LUA'
local G=require('src.game')
local P=require('src.persistence')
local dir=assert(os.getenv('ROAG_BUILD_SAVE_DIR'))
local storage={}
function storage:read(name)
    local f=io.open(dir..'/'..name,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
function storage:write(name,bytes)
    local f=assert(io.open(dir..'/'..name,'wb'));f:write(bytes);f:close();return true
end
local state,meta=P.new(storage):load()
assert(state and meta.status=='active' and state.clock==100)
local target
for _,e in ipairs(state.entities) do
    if e.controller=='ai' then target=e;break end
end
assert(target and target.statuses and target.statuses.burning)
assert(G.submit(state,{kind='wait'}))
local f=assert(io.open(dir..'/expected.bytes','rb'))
local expected=f:read('*a');f:close()
assert(P.encode(G.snapshot(state))==expected,'fresh-process continuation diverged')
print('fresh-process disk Continue reproduced status/echo continuation exactly')
LUA
printf 'Boon disk resume passed in %s\n' "$save_dir"
