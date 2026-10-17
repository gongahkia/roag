#!/bin/sh
# Actual current-game render, settings selection, safe refresh and disk reopen.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-kenney-gui-XXXXXX")}
mkdir -p "$output/app/assets" "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets/fonts" "$project_dir/assets/generated" "$project_dir/assets/kenney" "$output/app/assets/"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/"
cat > "$output/app/main.lua" <<'LUA'
local Game=require('src.game')
local Camera=require('src.camera')
local Content=require('src.content')
local Kenney=require('src.kenney_visuals')
local Packs=require('src.visual_packs')
local PNG=require('tools.art_png')
local Codec=require('src.persistence')
local tracked,camera
local original_new,original_restore=Game.new_stage,Game.restore
function Game.new_stage(...)tracked=original_new(...);return tracked end
function Game.restore(...)tracked=original_restore(...);return tracked end
local new_camera=Camera.new
function Camera.new(...)camera=new_camera(...);return camera end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_KENNEY_OUTPUT'))
local mode=assert(os.getenv('ROAG_KENNEY_MODE'))
local phase,pending,failed=1,false,false
local before,source
local function snapshot()return Codec.encode(Game.snapshot(tracked))end
local function assert_art()
    assert(love.roagVisualPackMode()=='kenney')
    assert(love.roagAssetBacking(Game.player(tracked).definition_id)=='kenney')
end
local function fail(why)
    failed=true;io.stderr:write('KENNEY '..mode..' FAILED: '..tostring(why)..'\n')
    love.event.quit(1)
end
if mode=='corrupt'then
    local read=love.filesystem.read
    love.filesystem.read=function(path,...)
        if path=='assets/kenney/micro-roguelike/tiles.png'then return 'broken'end
        return read(path,...)
    end
end
local function pick(id)
    local current=Packs.index(love.roagVisualPackMode())
    local wanted=Packs.index(id)
    for _=1,(wanted-current+#Packs.order)%#Packs.order do love.keypressed('down')end
    love.keypressed('return');update(0)
end
local function survey()
    love.keypressed('f6')
    local p=Game.player(tracked).position
    local room=tracked.generated_map.rooms[2]
    local x,y=room.x+math.floor(room.width/2),room.y+math.floor(room.height/2)
    local initial=snapshot()
    for _=1,math.abs(x-p.x)do love.keypressed(x>p.x and 'd' or 'a')end
    for _=1,math.abs(y-p.y)do love.keypressed(y>p.y and 's' or 'w')end
    update(1);assert(snapshot()==initial)
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
function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args))
        if mode~='corrupt' then
            source=love.image.newImageData(Kenney.path)
            local walls=Kenney.wall_sheet(source)
            assert(#Kenney.wall_masks==47)
            for i=1,47 do
                for y=0,7 do for x=0,7 do
                    assert(select(4,walls:getPixel((i-1)%16*8+x,math.floor((i-1)/16)*8+y))==1)
                end end
            end
            PNG.write(output..'/wall-variants.png',walls);walls:release()
        end
        if mode=='normal'then
            assert(love.roagVisualPackMode()=='automatic')
            before=snapshot()
            love.keypressed('escape');love.keypressed('3');pick('kenney')
            assert_art();assert(snapshot()==before)
        elseif mode=='resume' or mode=='corrupt'then
            love.keypressed('c');update(0)
            local f=assert(io.open(output..'/expected-run.txt','rb'))
            assert(snapshot()==f:read('*a'),'Continue changed saved snapshot');f:close()
            if mode=='resume'then assert_art()
            else assert(love.roagAssetBacking(Game.player(tracked).definition_id)=='fallback')end
        else assert_art();survey();before=snapshot()end
    end)
    if not ok then fail(why)end
end
function love.update()end
local function label()
    if mode=='normal'then return ({'settings','entrance','classic','refreshed','saved-title'})[phase]end
    if mode=='survey' then return phase==1 and 'survey' or 'combat' end
    return mode
end
local function advance()
    if mode=='survey' and phase==1 then
        assert(snapshot()==before);love.keypressed('f6');update(1)
        approach_combat();assert_art();phase=2;return
    elseif mode~='normal'then love.event.quit();return end
    if phase==1 then love.keypressed('escape');love.keypressed('escape');update(0)
    elseif phase==2 then
        assert(snapshot()==before)
        love.keypressed('escape');love.keypressed('3');pick('classic')
        love.keypressed('escape');love.keypressed('escape');update(0)
    elseif phase==3 then
        assert(snapshot()==before)
        love.keypressed('escape');love.keypressed('3');pick('kenney')
        love.keypressed('escape');love.keypressed('escape');update(0)
        local read=love.filesystem.read
        love.filesystem.read=function(path,...)
            if path=='assets/kenney/micro-roguelike/tiles.png'then return 'incomplete'end
            return read(path,...)
        end
        love.keypressed('f5');update(0)
        assert(love.roagVisualRefreshStatus():match('^rejected'))
        assert_art();assert(snapshot()==before)
        love.filesystem.read=read
        love.keypressed('f5');update(0);assert_art();assert(snapshot()==before)
    elseif phase==4 then
        local file=assert(io.open(output..'/expected-run.txt','wb'))
        file:write(snapshot());file:close()
        love.keypressed('escape');love.keypressed('2')
    else love.event.quit();return end
    phase=phase+1
end
function love.draw()
    if failed then return end
    local ok,why=pcall(function()
        draw()
        if not pending then
            pending=true
            love.graphics.captureScreenshot(function(image)
                local good,err=pcall(function()
                    if (mode=='normal' and phase==2) or mode=='resume' then
                        local p=Game.player(tracked).position
                        local x,y=Camera.tile_top_left(camera,p.x,p.y)
                        local index=Kenney.actors[Game.player(tracked).definition_id]
                        local ox,oy=Kenney.region(index);local compared=0
                        for py=0,7 do for px=0,7 do
                            local r,g,b,a=source:getPixel(ox+px,oy+py)
                            if math.floor(r*255+.5)~=34 or math.floor(g*255+.5)~=35 or math.floor(b*255+.5)~=35 then
                                local rr,gg,bb=image:getPixel(math.floor(x+(px+.5)*camera.tile/8),
                                    math.floor(y+(py+.5)*camera.tile/8))
                                assert(math.abs(r-rr)<1/255 and math.abs(g-gg)<1/255 and
                                    math.abs(b-bb)<1/255,'native Kenney actor pixels differ in game')
                                compared=compared+1
                            end
                        end end
                        assert(compared>10,'no meaningful sprite pixel comparison')
                    end
                    PNG.write(output..'/'..label()..'.png',image)
                    advance();pending=false
                end)
                if not good then fail(err)end
            end)
        end
    end)
    if not ok then fail(why)end
end
LUA
for mode in normal resume corrupt survey;do
    if [ "$mode" = normal ];then autostart=1;else autostart=0;fi
    if [ "$mode" = survey ];then spectator=1;pack=kenney;else spectator=0;pack=;fi
    ROAG_KENNEY_OUTPUT="$output" ROAG_KENNEY_MODE="$mode" \
    ROAG_GUI_AUTOSTART="$autostart" ROAG_SPECTATOR="$spectator" ROAG_SPRITE_PACK="$pack" \
    XDG_DATA_HOME="$output/data" timeout 35s love "$output/app" > "$output/$mode.log" 2>&1
    printf 'PASS Kenney %s\n' "$mode"
done
printf 'Kenney actual-render/save/refresh smoke: %s\n' "$output"
