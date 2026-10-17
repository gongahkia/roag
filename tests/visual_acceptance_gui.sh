#!/bin/sh
# Actual LÖVE rendered evidence. Normal UI plus a separately labelled spectator
# survey; callbacks are scripted, all profiles/settings/saves are isolated.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-visual-acceptance-XXXXXX")}
mkdir -p "$output_dir/app/assets" "$output_dir/app/tools" "$output_dir/data"
cp "$project_dir/main.lua" "$output_dir/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/app/conf.lua"
cp -R "$project_dir/src" "$output_dir/app/src"
cp -R "$project_dir/assets/fonts" "$project_dir/assets/generated" "$output_dir/app/assets/"
cp "$project_dir/tools/art_png.lua" "$output_dir/app/tools/art_png.lua"
cat > "$output_dir/app/main.lua" <<'LUA'
local Game=require('src.game')
local Codec=require('src.persistence')
local Biomes=require('src.biomes')
local Content=require('src.content')
local PNG=require('tools.art_png')
local tracked
local original_new=Game.new_stage
local original_restore=Game.restore
local original_submit=Game.submit
function Game.new_stage(...)
    tracked=original_new(...)
    return tracked
end
function Game.restore(...)
    tracked=original_restore(...)
    return tracked
end
function Game.submit(state,...)
    tracked=state
    return original_submit(state,...)
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_VISUAL_OUTPUT'))
local mode=assert(os.getenv('ROAG_VISUAL_MODE'))
if mode=='corrupt'then
    local read=love.filesystem.read
    love.filesystem.read=function(path,...)
        if path:match('^assets/generated/builds/.*/terrain%.png$')then return 'corrupt page' end
        return read(path,...)
    end
end
local phase,pending,failure=1,false,nil
local before,free=false,false
local function snapshot()
    assert(tracked,'fixture has not observed a run')
    return Codec.encode(Game.snapshot(tracked))
end
local function generated()
    assert(love.roagVisualPackMode()=='generated')
    assert(love.roagAssetBacking(Game.player(tracked).definition_id)=='generated')
end
local function open_settings()
    love.keypressed('escape');love.keypressed('3')
end
local function pick(mode)
    -- Opening settings highlights the active mode. Use known relative offsets.
    local order={'automatic','classic','generated','published'}
    local current,wanted
    for i,id in ipairs(order)do
        if id==love.roagVisualPackMode()then current=i end
        if id==mode then wanted=i end
    end
    for _=1,(wanted-current+#order)%#order do love.keypressed('down')end
    love.keypressed('return');update(0)
    assert(love.roagVisualPackMode()==mode)
end
local function center_room()
    if not free then love.keypressed('f6');free=true end
    local p=Game.player(tracked).position
    local room=tracked.generated_map.rooms[2]
    local x=room.x+math.floor(room.width/2)
    local y=room.y+math.floor(room.height/2)
    local initial=snapshot()
    for _=1,math.abs(x-p.x)do love.keypressed(x>p.x and 'd' or 'a')end
    for _=1,math.abs(y-p.y)do love.keypressed(y>p.y and 's' or 'w')end
    update(2)
    assert(snapshot()==initial,'camera survey changed authoritative state')
end
local function approach_combat()
    -- Ordinary movement/wait inputs on the explicitly invulnerable spectator
    -- run. No teleport, enemy weakening or hand-inserted telegraph.
    for _=1,160 do
        if #Game.pending_strikes(tracked)>0 then return end
        local p=Game.player(tracked).position
        local target,distance
        local blocked={}
        for _,e in ipairs(tracked.entities)do
            if e.position.floor_id==tracked.active_floor_id and e.id~=tracked.player_id then
                if e.blocks_movement then blocked[e.position.x..','..e.position.y]=true end
                if e.controller=='ai'then
                    local d=math.abs(p.x-e.position.x)+math.abs(p.y-e.position.y)
                    if not distance or d<distance then target,distance=e,d end
                end
            end
        end
        assert(target,'combat survey has no enemy')
        local button='space'
        if distance>2 then
            local map=Content.map_for(tracked)
            local queue,seen,head={{p.x,p.y}}, {[p.x..','..p.y]=true},1
            while head<=#queue do
                local cell=queue[head];head=head+1
                if math.abs(cell[1]-target.position.x)+math.abs(cell[2]-target.position.y)<=1 then
                    button=assert(cell[3]);break
                end
                for _,d in ipairs({{1,0,'d'},{0,1,'s'},{-1,0,'a'},{0,-1,'w'}})do
                    local x,y=cell[1]+d[1],cell[2]+d[2]
                    local key=x..','..y
                    if not seen[key] and not blocked[key] and Content.walkable(map,x,y)then
                        seen[key]=true;queue[#queue+1]={x,y,cell[3]or d[3]}
                    end
                end
            end
        end
        love.keypressed(button);update(2)
    end
    error('no actual enemy windup reached through legal controls')
end
local function label()
    if mode=='resume'then return 'continued' end
    if mode=='corrupt'then return 'corrupt-fallback' end
    if mode=='normal'then
        return ({'settings','normal-start','normal-move','classic','generated-return'})[phase]
    end
    if phase<=#Biomes.order then return 'room-'..Biomes.order[phase]:match('/([^/]+)$')end
    if phase==#Biomes.order+7 then return 'combat-telegraph' end
    return 'class-'..Game.player(tracked).definition_id:match('/([^/]+)$')
end
local function scenario_load(...)
    load(...)
    if mode=='normal'then
        assert(love.roagVisualPackMode()=='automatic' and
            love.roagAssetBacking('core:actor/adventurer')=='generated',
            'fresh normal launch did not select bundled art')
        love.keypressed('down');love.keypressed('down');love.keypressed('return')
        love.keypressed('down');love.keypressed('down');love.keypressed('return')
        update(0)
        assert(love.roagVisualPackMode()=='generated')
    elseif mode=='resume' or mode=='corrupt'then
        love.keypressed('c');update(0)
        local file=assert(io.open(output..'/expected-run.txt','rb'))
        local expected=file:read('*a');file:close()
        assert(snapshot()==expected,'fresh Continue changed the saved run')
        if mode=='resume'then generated()
        else assert(not love.roagAssetResolved(Game.player(tracked).definition_id))end
    else generated();center_room()end
    local file=assert(io.open(output..'/renderer-'..mode..'.txt','w'))
    file:write(table.concat({love.graphics.getRendererInfo()},' | '),'\n')
    file:close()
end
function love.load(...)
    local ok,why=pcall(scenario_load,...)
    if not ok then
        failure=tostring(why)
        io.stderr:write('VISUAL LOAD FAILED: '..failure..'\n')
        love.event.quit(1)
    end
end
function love.update()end -- deterministic presentation dt is supplied below
local function advance()
    if mode=='resume' or mode=='corrupt'then love.event.quit();return
    elseif mode=='normal'then
        if phase==1 then love.keypressed('escape');love.keypressed('n');update(2);generated()
        elseif phase==2 then
            local old=tracked.clock
            local committed=false
            for _,key in ipairs({'d','s','a','w'})do
                love.keypressed(key);update(1)
                if tracked.clock>old then committed=true;break end
            end
            assert(committed,'normal legal movement failed')
            before=snapshot()
        elseif phase==3 then
            open_settings();pick('classic')
            assert(snapshot()==before,'pack switch changed run')
            love.keypressed('escape');love.keypressed('escape');update(0)
            assert(not love.roagAssetResolved(Game.player(tracked).definition_id))
        elseif phase==4 then
            open_settings();pick('generated')
            love.keypressed('escape');love.keypressed('escape');update(0)
            generated();assert(snapshot()==before)
            love.keypressed('f5');update(0);assert(snapshot()==before)
        else
            local read=love.filesystem.read
            love.filesystem.read=function(path,...)
                if path:match('^assets/generated/builds/.*/terrain%.png$')then
                    return 'incomplete refresh fixture'
                end
                return read(path,...)
            end
            love.keypressed('f5');update(0)
            assert(love.roagVisualRefreshStatus():match('^rejected'),
                'bad refresh was not rejected')
            generated();assert(snapshot()==before)
            love.filesystem.read=read
            love.keypressed('f5');update(0)
            assert(not love.roagVisualRefreshStatus():match('^rejected'))
            generated();assert(snapshot()==before)
            local file=assert(io.open(output..'/expected-run.txt','wb'))
            file:write(snapshot());file:close()
            love.keypressed('escape');love.keypressed('2') -- actual Save & Quit
            return
        end
    else
        if phase<#Biomes.order then love.keypressed('f7');center_room();generated()
        elseif phase==#Biomes.order then
            love.keypressed('f7');love.keypressed('f6');free=false;update(2)
        elseif phase<#Biomes.order+6 then love.keypressed('f9');update(2);generated()
        elseif phase==#Biomes.order+6 then
            love.keypressed('f9');update(0);approach_combat();generated()
        else love.event.quit();return end
    end
    phase=phase+1;pending=false
end
function love.draw(...)
    if failure then love.event.quit(1);return end
    local ok,why=pcall(draw,...)
    if not ok then
        failure=tostring(why);io.stderr:write('DRAW FAILED: '..failure..'\n')
        love.event.quit(1);return
    end
    if pending then return end
    pending=true
    local name=assert(label())
    love.graphics.captureScreenshot(function(image)
        local success,reason=pcall(function()
            PNG.write(output..'/'..name..'.png',image)
            advance()
        end)
        if not success then
            failure=tostring(reason)
            local file=io.open(output..'/failure-'..mode..'.txt','w')
            if file then file:write(failure);file:close()end
            io.stderr:write('VISUAL ACCEPTANCE FAILED: '..failure..'\n')
            love.event.quit(1)
        end
    end)
end
LUA
ROAG_VISUAL_OUTPUT="$output_dir" ROAG_VISUAL_MODE=normal ROAG_GENERATED_ART=0 \
    XDG_DATA_HOME="$output_dir/data/normal" timeout 45s love "$output_dir/app" >"$output_dir/normal.log" 2>&1
for mode in resume corrupt;do
    ROAG_VISUAL_OUTPUT="$output_dir" ROAG_VISUAL_MODE="$mode" \
        XDG_DATA_HOME="$output_dir/data/normal" timeout 30s love "$output_dir/app" >"$output_dir/$mode.log" 2>&1
done
ROAG_VISUAL_OUTPUT="$output_dir" ROAG_VISUAL_MODE=spectator ROAG_GENERATED_ART=1 \
    ROAG_SPECTATOR=1 XDG_DATA_HOME="$output_dir/data/spectator" \
    timeout 60s love "$output_dir/app" >"$output_dir/spectator.log" 2>&1
test -s "$output_dir/normal-start.png"
test -s "$output_dir/continued.png"
test -s "$output_dir/corrupt-fallback.png"
test -s "$output_dir/class-alchemist.png"
test -s "$output_dir/combat-telegraph.png"
printf 'Visual capture and save/fallback PASS (inspect screenshots for aesthetics): %s\n' "$output_dir"
