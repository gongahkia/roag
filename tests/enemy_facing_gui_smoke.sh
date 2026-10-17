#!/bin/sh
# Assisted presentation fixtures plus genuine AI windup from legal waits.
# No physical mouse/keyboard input; all saves and screenshots are isolated.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-facing-gui-XXXXXX")}
mkdir -p "$output_dir/app/assets" "$output_dir/app/tools" "$output_dir/data"
cp "$project_dir/main.lua" "$output_dir/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/app/conf.lua"
cp -R "$project_dir/src" "$output_dir/app/src"
cp -R "$project_dir/assets/fonts" "$project_dir/assets/generated" "$output_dir/app/assets/"
cp "$project_dir/tools/art_png.lua" "$output_dir/app/tools/"
cat > "$output_dir/app/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
local Camera=require('src.camera')
local Codec=require('src.persistence')
local PNG=require('tools.art_png')
local tracked,enemy,queue,camera
local new_queue=Playback.new
function Playback.new(...) queue=new_queue(...);return queue end
local new_camera=Camera.new
function Camera.new(...) camera=new_camera(...);return camera end
function Game.new_stage(seed)
    tracked=Game.new(seed,'core:map/scrolling_room')
    enemy=Game.spawn_enemy(tracked,'core:enemy/mossbound_guard',7,3)
    return tracked
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_FACING_OUTPUT'))
local labels={'baseline','attack-left','attack-right','move-left','move-vertical','ai-locked-attack'}
local phase,pending,failure=1,false,false
local baseline,source,region,drawn,before
local function snapshot()return Codec.encode(Game.snapshot(tracked))end
local function event(kind,dx)
    return {kind=kind,source_id=enemy.id,from_x=7,from_y=3,
        floor_id=tracked.active_floor_id,target_cells={{x=7+dx,y=3}}}
end
local function reset_queue()
    queue.items={};queue.remaining=0;queue.poses={};queue.facings={}
end
local raw_draw=love.graphics.draw
love.graphics.draw=function(texture,quad,x,y,rotation,sx,sy,...)
    if region and type(quad)=='userdata' and quad.typeOf and quad:typeOf('Quad') then
        local qx,qy,qw,qh=quad:getViewport()
        if qx==region.x and qy==region.y and qw==32 and qh==32 then
            local px,py=love.graphics.transformPoint(x,y)
            drawn={x=px,y=py,sx=sx,sy=sy}
        end
    end
    return raw_draw(texture,quad,x,y,rotation,sx,sy,...)
end
local function fail(why)
    failure=true;io.stderr:write('Enemy facing GUI failure: '..tostring(why)..'\n')
    love.event.quit(1)
end
function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args))
        assert(enemy and enemy.id)
        assert(love.roagAssetBacking(enemy.definition_id)=='generated')
        local manifest=assert(love.filesystem.read('assets/generated/manifest.txt'))
        local x,y=manifest:match('asset\tcore:enemy/mossbound_guard\tactors\t(%d+)\t(%d+)')
        region={x=tonumber(x),y=tonumber(y)}
        local path=assert(manifest:match('page\tactors\t([^\t]+)'))
        source=love.image.newImageData(path)
        before=snapshot()
    end)
    if not ok then fail(why)end
end
function love.update()end
local function same(a,b)for i=1,4 do if math.abs(a[i]-b[i])>1/255 then return false end end return true end
local function check(image)
    assert(drawn,'actual enemy draw was not observed')
    if phase<=5 then assert(snapshot()==before,'presentation changed authoritative state')end
    if phase==1 then baseline=image
    elseif phase==2 or phase==3 then
        local facing=phase==2 and -1 or 1
        assert(drawn.sx*facing>0,'actual draw used the wrong horizontal scale')
        local left=drawn.x+(facing==-1 and drawn.sx*32 or 0)
        local matches=0
        -- Interior opaque sprite pixels: avoids the source ring/ground shadow.
        for y=8,23 do for x=9,22 do
            local rgba={source:getPixel(region.x+x,region.y+y)}
            if rgba[4]==1 then
                local screen_x=math.floor(left+((facing==-1 and 31-x or x)+.5)*math.abs(drawn.sx))
                local screen_y=math.floor(drawn.y+(y+.5)*drawn.sy)
                local actual={image:getPixel(screen_x,screen_y)}
                assert(same(rgba,actual),('phase %d sprite %d,%d screen %d,%d scale %.3f expected %.2f %.2f %.2f actual %.2f %.2f %.2f'):format(phase,x,y,screen_x,screen_y,drawn.sx,rgba[1],rgba[2],rgba[3],actual[1],actual[2],actual[3]))
                matches=matches+1
            end
        end end
        assert(matches>20,'insufficient real sprite pixels compared')
        if phase==2 then
            local tx,ty=Camera.tile_top_left(camera,6,3)
            for y=10,camera.tile-11 do for x=10,camera.tile-11 do
                assert(same({baseline:getPixel(math.floor(tx+x),math.floor(ty+y))},
                    {image:getPixel(math.floor(tx+x),math.floor(ty+y))}),
                    'attack warning filled the tile interior')
            end end
            local rectangles=require('src.attack_outline').rectangles(camera.tile)
            local r=rectangles[1]
            local red,green,blue=image:getPixel(math.floor(tx+r[1]+1),math.floor(ty+r[2]))
            assert(red>.8 and green>.4 and blue<.4,'dashed windup border was not rendered')
        end
    elseif phase==4 or phase==5 then assert(drawn.sx<0,'movement or vertical retention did not mirror')
    else
        assert(#Game.pending_strikes(tracked)>0,'legal waits did not create an AI attack')
        assert(drawn.sx<0,'genuine AI locked attack did not face the player')
    end
end
local function advance()
    if phase==1 then Playback.add(queue,{event('WindupStarted',-1)},tracked.player_id)
    elseif phase==2 then reset_queue();Playback.add(queue,{event('WindupStarted',1)},tracked.player_id)
    elseif phase==3 then
        reset_queue();local e=event('Moved',-1);e.x=6;e.y=3
        Playback.add(queue,{e},tracked.player_id)
    elseif phase==4 then
        update(1)
        Playback.add(queue,{{kind='Moved',source_id=enemy.id,from_x=6,from_y=3,
            x=6,y=4,floor_id=tracked.active_floor_id}},tracked.player_id)
    elseif phase==5 then
        reset_queue()
        for _=1,8 do
            love.keypressed('space');update(4)
            if #Game.pending_strikes(tracked)>0 then break end
        end
    else
        local file=assert(io.open(output..'/result.txt','w'))
        file:write('PASS: real generated atlas pixels mirrored left/right; transparent dashed interiors; movement, vertical retention and genuine AI locked windup.\n')
        file:write(table.concat({love.graphics.getRendererInfo()},' | '),'\n');file:close()
        love.event.quit();return
    end
    phase=phase+1
end
function love.draw()
    if failure then return end
    local ok,why=pcall(function()
        drawn=nil;draw()
        if not pending then
            pending=true
            love.graphics.captureScreenshot(function(image)
                local good,err=pcall(function()
                    PNG.write(output..'/'..labels[phase]..'.png',image);check(image)
                    advance();pending=false
                end)
                if not good then fail(err)end
            end)
        end
    end)
    if not ok then fail(why)end
end
LUA
ROAG_FACING_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 XDG_DATA_HOME="$output_dir/data" \
    timeout 30s love "$output_dir/app" > "$output_dir/run.log" 2>&1
cat "$output_dir/result.txt"
printf 'Enemy facing GUI smoke: 6 rendered frames in %s\n' "$output_dir"
