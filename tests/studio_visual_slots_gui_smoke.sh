#!/bin/sh
# Native draw-path check in a copied app with a disposable LÖVE identity.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-visual-slots-XXXXXX")
app="$output/app"
mkdir -p "$app/assets/studio/sources" "$output/data"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
APP="$app" luajit - <<'LUA'
local Project=require('src.studio_project')
local PNG=require('src.studio_png')
local app=assert(os.getenv('APP'))
local project=Project.new('visual_slots_gui','Visual slots GUI')
local slots={
    {'player','core:actor/adventurer',{220,40,40,255}},
    {'attack','core:actor/adventurer/attack',{40,220,40,255}},
    {'hurt','core:actor/adventurer/hurt',{40,40,220,255}},
    {'enemy','core:enemy/mossbound_guard',{190,100,50,255}},
    {'enemy_attack','core:enemy/mossbound_guard/attack',{190,180,50,255}},
    {'enemy_hurt','core:enemy/mossbound_guard/hurt',{130,50,190,255}},
    {'wall','core:terrain/overgrown_ruins/wall/base',{180,50,180,255}},
    {'background','core:background/overgrown_ruins',{35,50,90,255}},
    {'currency','core:drop/currency',{240,210,20,255}},
    {'currency_detail','core:drop/currency/detail',{250,190,35,255}},
    {'boon_icon','core:boon/storm_conductor/icon',{60,190,240,255}},
    {'boon_detail','core:boon/storm_conductor/detail',{90,220,250,255}},
}
for _,slot in ipairs(slots) do
    local name,binding,color=slot[1],slot[2],slot[3]
    local a=Project.asset('studio:actor/'..name,name,'actor')
    a.binding=binding;a.source='sources/'..name..'.png'
    a.format='png';a.width=2;a.height=2
    project.assets[#project.assets+1]=a
    local file=assert(io.open(app..'/assets/studio/'..a.source,'wb'))
    file:write(PNG.encode(2,2,string.rep(string.char(unpack(color)),4)))
    file:close()
end
local file=assert(io.open(app..'/assets/studio/manifest.txt','wb'))
file:write(Project.encode(project));file:close()
LUA
cat > "$app/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
local VisualAssets=require('src.visual_assets')
local state,queue,enemy
local old_new=Game.new_stage
function Game.new_stage(seed)
    state=Game.new(seed,'core:map/test_room')
    local position=Game.player(state).position
    enemy=Game.spawn_enemy(state,'core:enemy/mossbound_guard',
        position.x+3,position.y)
    state.drops[#state.drops+1]={id=777,kind='currency',amount=1,
        position={floor_id=position.floor_id,x=position.x+1,y=position.y},
        collected=false}
    return state
end
local old_queue=Playback.new
function Playback.new()
    queue=old_queue();return queue
end
local old_resolve=VisualAssets.resolve
local seen={}
function VisualAssets:resolve(binding)
    local handle=old_resolve(self,binding)
    if handle then seen[binding]=(seen[binding] or 0)+1 end
    return handle
end
assert(love.filesystem.load('roag_main.lua'))()
local original_load,original_update,original_draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_VISUAL_OUTPUT'))
local phase=1
function love.load(...)
    original_load(...)
    assert(state and queue and love.roagAssetStatus()=='loaded')
    Game.set_boon_stacks(state,state.player_id,
        'core:boon/storm_conductor','common',1)
    assert(love.roagAssetResolved('core:actor/adventurer/attack'))
    assert(love.roagAssetResolved('core:actor/adventurer/hurt'))
    assert(love.roagAssetResolved('core:boon/storm_conductor/icon'))
end
function love.update(_) original_update(0) end
function love.draw(...)
    seen={}
    original_draw(...)
    local required=phase==1 and 'core:actor/adventurer' or
        phase==2 and 'core:actor/adventurer/attack' or
        phase==3 and 'core:actor/adventurer/hurt' or
        phase==6 and 'core:enemy/mossbound_guard/attack' or
        phase==7 and 'core:enemy/mossbound_guard/hurt' or nil
    if required then
        assert(seen[required],required..' was not drawn in phase '..phase)
    end
    assert(seen['core:background/overgrown_ruins'],'gameplay backdrop not drawn')
    if phase==1 then
        assert(seen['core:terrain/overgrown_ruins/wall/base'],
            'automatic wall terrain not drawn')
    end
    if phase<5 then
        assert(seen['core:drop/currency'],'floor currency sprite not drawn')
    end
    if phase==1 then
        assert(seen['core:enemy/mossbound_guard'],
            'base enemy sprite not drawn')
    end
    assert(seen['core:boon/storm_conductor/icon'],
        'owned boon icon not drawn in phase '..phase..' stacks='..
        tostring(Game.player(state).boons['core:boon/storm_conductor'] and
        Game.player(state).boons['core:boon/storm_conductor'].common))
    if phase==4 then
        assert(seen['core:boon/storm_conductor/detail'],
            'clicked owned boon detail was not drawn')
    elseif phase==5 then
        assert(seen['core:drop/currency/detail'],
            'currency pickup detail was not drawn')
        assert(state.drops[1].collected,'currency was not collected')
    end
    local path=output..'/phase'..phase..'.tga'
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(path,'wb'))
        file:write(image:encode('tga'):getString());file:close()
    end)
    if phase==1 then
        Playback.add(queue,{{kind='AttackPerformed',source_id=state.player_id,
            floor_id=state.active_floor_id,time=0,x=2,y=2,
            target_cells={{x=3,y=2}}}},state.player_id)
    elseif phase==2 then
        queue.items={};queue.remaining=0
        Playback.add(queue,{{kind='DamageTaken',target_id=state.player_id,
            actual_loss=1,floor_id=state.active_floor_id,time=0,x=3,y=2}},
            state.player_id)
    elseif phase==3 then
        queue.items={};queue.remaining=0
        love.mousepressed(747,355,1)
    elseif phase==4 then
        love.mousepressed(747,355,1)
        love.keypressed('d')
    elseif phase==5 then
        queue.items={};queue.remaining=0
        Playback.add(queue,{{kind='AttackPerformed',source_id=enemy.id,
            floor_id=state.active_floor_id,time=0,target_cells={}}},
            state.player_id)
    elseif phase==6 then
        queue.items={};queue.remaining=0
        Playback.add(queue,{{kind='DamageTaken',target_id=enemy.id,
            actual_loss=1,floor_id=state.active_floor_id,time=0}},
            state.player_id)
    else love.event.quit() end
    phase=phase+1
end
LUA
ROAG_GUI_AUTOSTART=1 ROAG_VISUAL_OUTPUT="$output" \
    XDG_DATA_HOME="$output/data" timeout 25s love "$app" >"$output/game.log" 2>&1
for phase in 1 2 3 4 5 6 7; do test -s "$output/phase$phase.tga"; done
printf 'Visual slots GUI passed: %s\n' "$output"
