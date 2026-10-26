#!/bin/sh
# Standalone pack with the other art sheets deliberately absent. Isolated saves.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-onebit-gui-XXXXXX")}
mkdir -p "$output/app/assets/kenney" "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets/fonts" "$output/app/assets/"
cp -R "$project_dir/assets/kenney/1-bit-pack" "$output/app/assets/kenney/"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/"
cat > "$output/app/main.lua" <<'LUA'
local Game=require('src.game')
local Camera=require('src.camera')
local O=require('src.onebit_visuals')
local S=require('src.kenney_symbols')
local Boons=require('src.boon_content')
local Content=require('src.content')
local Terrain=require('src.terrain')
local Playback=require('src.playback')
local UI=require('src.pixel_ui')
local Codec=require('src.persistence')
local PNG=require('tools.art_png')
local mode=assert(os.getenv('ROAG_ONEBIT_MODE'))
local output=assert(os.getenv('ROAG_ONEBIT_OUTPUT'))
local state,camera,enemies,target,playback
local actor_ids,actor_page,actor_pages,checked_actors={},1,1,{}
local gallery_rows={5,7,11,12}
local boon_pages=math.ceil(#Boons.order/7)
local function install_actor_page()
    -- Keep 36 visible, walkable gallery slots, paging rather than truncating
    -- the growing actor catalogue or spawning later actors at nil coordinates.
    for n=#state.entities,1,-1 do
        local e=state.entities[n]
        if e.controller=='ai'then
            require('src.scheduler').remove_for_actor(state,e.id)
            table.remove(state.entities,n)
        end
    end
    enemies={}
    for slot=1,36 do
        local id=actor_ids[(actor_page-1)*36+slot]
        if id then
            enemies[#enemies+1]=Game.spawn_enemy(state,id,4+(slot-1)%9*2,
                gallery_rows[math.floor((slot-1)/9)+1])
        end
    end
end
local add=Playback.add
function Playback.add(queue,...)playback=queue;return add(queue,...)end
local new,restore=Game.new_stage,Game.restore
function Game.new_stage(...)
    if mode=='gallery' or mode=='effects'then
        state=Game.new(11,'core:map/scrolling_room');enemies={}
        if mode=='effects'then
            target=Game.spawn_enemy(state,'core:enemy/mossbound_guard',4,3,10000)
            target.defense=100
            return state
        end
        local map=Content.map_for(state)
        map.rows[9]=map.rows[9]:sub(1,2)..'bgiBmc'..map.rows[9]:sub(9)
        for id in pairs(O.actors)do if id:match('^core:enemy/')then actor_ids[#actor_ids+1]=id end end
        table.sort(actor_ids);actor_pages=math.ceil(#actor_ids/36)
        install_actor_page()
        Game.spawn_chest(state,'core:chest/standard',5,3)
        return state
    end
    state=new(...);return state
end
function Game.restore(...)state=restore(...);return state end
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,pending,failed=0,false,false
local source,before,drawn
local graphics_draw=love.graphics.draw
function love.graphics.draw(texture,quad,...)
    if drawn and type(quad)=='userdata' and quad.typeOf and quad:typeOf('Quad')then
        local x,y,w,h=quad:getViewport()
        if w==16 and h==16 then drawn[math.floor(y/16)*49+math.floor(x/16)]=true end
    end
    return graphics_draw(texture,quad,...)
end
local function snapshot()return Codec.encode(Game.snapshot(state))end
local function fail(why)
    failed=true;io.stderr:write('1-Bit '..mode..' FAILED: '..tostring(why)..'\n');love.event.quit(1)
end
if mode=='corrupt'then
    local read=love.filesystem.read
    love.filesystem.read=function(path,...)
        if path==O.path then return 'corrupt standalone PNG'end
        return read(path,...)
    end
end
function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args))
        if mode~='corrupt'then source=love.image.newImageData(O.path)end
        if mode=='normal'then
            before=snapshot();love.keypressed('escape');love.keypressed('3')
            assert(love.roagVisualPackMode()=='onebit')
            assert(love.roagAssetBacking(Game.player(state).definition_id)=='onebit')
        elseif mode=='gallery'then
            for _,id in ipairs(Boons.order)do Game.set_boon_stacks(state,state.player_id,id,'common',1)end
            before=snapshot()
        elseif mode=='effects'then
            love.keypressed('f');love.keypressed('d');love.keypressed('return')
            for _=1,100 do
                local event=Playback.current(playback)
                if event and event.kind=='DamageBlocked'then break end
                update(.01)
            end
            assert(Playback.current(playback).kind=='DamageBlocked','no genuine blocked-hit event')
            before=snapshot()
        else
            love.keypressed('c');update(0)
            local f=assert(io.open(output..'/expected-run.txt','rb'))
            assert(snapshot()==f:read('*a'),'fresh-process Continue differs');f:close()
            before=snapshot()
            assert(love.roagVisualPackMode()=='onebit')
            assert(love.roagAssetBacking(Game.player(state).definition_id)==
                (mode=='corrupt' and 'fallback' or 'onebit'))
        end
    end)
    if not ok then fail(why)end
end
function love.update()end
local function compare(image,index,x,y,scale,max_y,ink,occluded)
    local sx,sy=S.region(index);local count=0
    for yy=0,max_y or 15 do for xx=0,15 do
        local r,g,b,a=source:getPixel(sx+xx,sy+yy)
        if a==1 and not (occluded and occluded(x+(xx+.5)*scale,y+(yy+.5)*scale))then
            local actual={image:getPixel(math.floor(x+(xx+.5)*scale),math.floor(y+(yy+.5)*scale))}
            local gray=r*O.ink[1]*.2126+g*O.ink[2]*.7152+b*O.ink[3]*.0722
            for c=1,3 do
                assert(math.abs(actual[c]-gray*(ink and ink[c]or 1))<=2/255+1e-6,'1-Bit sprite ink differs: '..index)
            end
            count=count+1
        end
    end end
    assert(count>5,'empty 1-Bit source region '..index)
end
local function check(image)
    if mode~='normal' or phase<4 then assert(snapshot()==before,'presentation changed the run/RNG')end
    if love.roagAssetBacking(Game.player(state).definition_id)=='onebit' then
        for _,p in ipairs({{8,8},{1015,8},{8,635},{1015,635}})do
            local r,g,b=image:getPixel(p[1],p[2])
            assert(math.abs(r-g)<=1/255+1e-6 and math.abs(g-b)<=1/255+1e-6,
                ('neutral backdrop contains color at %d,%d'):format(p[1],p[2]))
        end
    end
    if mode=='normal' and phase==1 or mode=='resume'then
        local p=Game.player(state).position;local x,y=Camera.tile_top_left(camera,p.x,p.y)
        compare(image,O.actors[Game.player(state).definition_id],x,y,camera.tile/16)
    elseif mode=='gallery'then
        if phase==0 then
            for _,e in ipairs(enemies)do
                local x,y=Camera.tile_top_left(camera,e.position.x,e.position.y)
                local dormant=e.definition_id=='core:enemy/haunted_cache' and
                    not e.ai_memory.alerted and e.hp==e.max_hp
                local palette=require('src.visual_palette')
                local tile_x,tile_y=x,y
                local size=require('src.visual_slots').enemy_size(e.definition_id,camera.tile)
                local scale=size/16
                x=x+(camera.tile-size)*.5;y=y+camera.tile-size
                local max_y=math.min(12,math.ceil((tile_y+camera.tile*28/32-y)/scale-.5)-1)
                compare(image,dormant and O.objects['core:object/idea_cache_dormant'] or
                    O.actors[e.definition_id],x,y,scale,max_y,
                    dormant and palette.chest or palette.enemy,function(px,py)
                        -- Real alarm/status/question badges may cover this
                        -- corner. Check body pixels outside that overlay and
                        -- above the HP bar rather than deleting the badge.
                        return px>=tile_x+camera.tile*.68
                            and py<=tile_y+camera.tile*.30
                    end)
                checked_actors[e.definition_id]=true
            end
            for i,mark in ipairs(Terrain.order)do
                local x,y=Camera.tile_top_left(camera,i+2,9)
                compare(image,S.terrain[Terrain.get(mark).id][1],x+4,y+4,2)
            end
        elseif phase<=boon_pages then
            for line=1,7 do
                local id=Boons.order[(phase-1)*7+line]
                if id then compare(image,S.boons[id][1],104,150+(line-1)*44,2,nil,require('src.visual_palette').gold)end
            end
        end
    elseif mode=='effects'then
        assert(drawn[require('src.idea_environment_content').symbols.cue_block_effect], 'blocked-hit sprite was not drawn')
        if phase==1 then
            assert(target.statuses.burning,'Emberbrand did not apply Burning')
            assert(drawn[O.objects['core:ui/burning_effect']], 'Burning sprite was not drawn')
        end
    end
end
local function advance()
    if mode=='effects'then
        if phase==0 then
            for _=1,20 do update(.5)end
            Game.set_boon_stacks(state,state.player_id,'core:boon/emberbrand','common',1)
            -- A blocked contact still legitimately applies Emberbrand.
            love.keypressed('f');love.keypressed('d');love.keypressed('return')
            for _=1,100 do
                local event=Playback.current(playback)
                if event and event.kind=='DamageBlocked'then break end
                update(.01)
            end
            assert(Playback.current(playback).kind=='DamageBlocked')
            before=snapshot()
        else love.event.quit();return end
    elseif mode=='gallery'then
        if phase==0 and actor_page<actor_pages then
            actor_page=actor_page+1;install_actor_page();update(0)
            before=snapshot();return
        elseif phase==0 then
            for _,id in ipairs(actor_ids)do assert(checked_actors[id],'unrendered actor '..id)end
            love.keypressed('i')
        elseif phase<boon_pages then love.keypressed('right')
        else
            print(('PASS gallery checked every actor (%d) and boon (%d) over %d actor / %d boon pages')
                :format(#actor_ids,#Boons.order,actor_pages,boon_pages))
            love.event.quit();return
        end
    elseif mode~='normal'then love.event.quit();return
    elseif phase==0 then love.keypressed('escape');love.keypressed('escape');update(0)
    elseif phase==1 then
        local read=love.filesystem.read
        love.filesystem.read=function(path,...)if path==O.path then return 'broken'end;return read(path,...)end
        love.keypressed('f5');update(0)
        assert(love.roagVisualRefreshStatus():match('^rejected'))
        assert(love.roagAssetBacking(Game.player(state).definition_id)=='onebit')
        love.filesystem.read=read
        love.keypressed('f5');update(0)
        love.keypressed('f');love.keypressed('d')
    elseif phase==2 then love.keypressed('escape');love.keypressed('i')
    elseif phase==3 then
        love.keypressed('escape')
        local f=assert(io.open(output..'/expected-run.txt','wb'));f:write(snapshot());f:close()
        love.keypressed('escape');love.keypressed('2')
    else love.event.quit();return end
    update(0);phase=phase+1
end
function love.draw()
    if failed then return end
    local ok,why=pcall(function()
        drawn={};draw();if pending then return end;pending=true
        love.graphics.captureScreenshot(function(image)
            local good,err=pcall(function()
                local page=mode=='gallery' and phase==0 and ('-actors-'..actor_page)or ''
                PNG.write(output..'/'..mode..'-'..phase..page..'.png',image)
                check(image);advance();pending=false
            end)
            if not good then fail(err)end
        end)
    end)
    if not ok then fail(why)end
end
LUA
for mode in normal resume corrupt gallery effects;do
    if [ "$mode" = normal ] || [ "$mode" = gallery ] || [ "$mode" = effects ];then autostart=1;else autostart=0;fi
    ROAG_ONEBIT_MODE="$mode" ROAG_ONEBIT_OUTPUT="$output" ROAG_GUI_AUTOSTART="$autostart" \
      XDG_DATA_HOME="$output/data" \
      timeout -k 3s 40s love "$output/app" > "$output/$mode.log" 2>&1 || {
        cat "$output/$mode.log"
        printf 'Retained failed 1-Bit artifacts: %s\n' "$output"
        exit 1
    }
    printf 'PASS standalone 1-Bit %s\n' "$mode"
done
printf 'Fixed 1-Bit native pixels, settings, save/Continue, corruption and gallery: %s\n' "$output"
