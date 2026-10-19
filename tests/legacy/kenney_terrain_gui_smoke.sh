#!/bin/sh
# Real procedural biome captures, isolated spectator camera and native pixels.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-caves-gui-XXXXXX")}
mkdir -p "$output/app/assets" "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets/fonts" "$project_dir/assets/kenney" "$output/app/assets/"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/"
cat > "$output/app/main.lua" <<'LUA'
local Game=require('src.game')
local Camera=require('src.camera')
local Spectator=require('src.spectator')
local Biomes=require('src.biomes')
local Content=require('src.content')
local Terrain=require('src.kenney_terrain')
local Codec=require('src.persistence')
local PNG=require('tools.art_png')
local state,camera
local new=Spectator.new
function Spectator.new(...)state=new(...);return state end
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,pending,failed=1,false,false
local before,source,surfaces,terrain,focus_x,focus_y
local output=assert(os.getenv('ROAG_CAVES_OUTPUT'))
local function snapshot()return Codec.encode(Game.snapshot(state))end
local function fail(why)
    failed=true;io.stderr:write('Caves GUI FAILED: '..tostring(why)..'\n')
    love.event.quit(1)
end
local function pan()
    local map=Content.map_for(state);local room=map.rooms[2]
    local x,y=room.x+math.floor(room.width/2),room.y+math.floor(room.height/2)
    if map.biome_id==Biomes.order[3]then
        for yy=8,map.height-8 do
            local xx=map.rows[yy]:find('~~~',8,true)
            if xx and xx<map.width-8 then x,y=xx+1,yy;break end
        end
    end
    local p=Game.player(state).position
    focus_x,focus_y=x,y
    local initial=snapshot()
    for _=1,math.abs(x-p.x)do love.keypressed(x>p.x and 'd' or 'a')end
    for _=1,math.abs(y-p.y)do love.keypressed(y>p.y and 's' or 'w')end
    update(2);assert(snapshot()==initial,'camera panning changed the run')
    before=snapshot()
end
function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args));love.keypressed('f6')
        source=love.image.newImageData(Terrain.path)
        surfaces=Terrain.surface_sheet(source)
        terrain=Terrain.new(function(path)return love.filesystem.read(path)end,
            function()return {getWidth=function()return 492 end,getHeight=function()return 305 end}end,
            function(x,y)return {x=x,y=y}end)
        pan()
    end)
    if not ok then fail(why)end
end
function love.update()end
local function check(image)
    assert(snapshot()==before,'render/refresh changed snapshot or RNG')
    local map=Content.map_for(state)
    local slug=map.biome_id:match('/([^/]+)$');local style=Terrain.styles[slug]
    local occupied={}
    for _,e in ipairs(state.entities)do
        if e.position then occupied[e.position.x..','..e.position.y]=true end
    end
    for _,s in ipairs(map.placements.stairs or {})do occupied[s.x..','..s.y]=true end
    if map.elite then occupied[map.elite.x..','..map.elite.y]=true end
    local compared,water_compared=0,0
    for y=1,map.height do for x=1,map.width do
        local px,py=Camera.tile_top_left(camera,x,y)
        local visual=map.visual_rows and map.visual_rows[y]:sub(x,x)
        if compared<3 and map.rows[y]:sub(x,x)=='.' and not occupied[x..','..y]
            and visual~='p' and visual~='k' and px>=210 and px+40<=990 and py>=80 and py+40<=440 then
            local index=style.floor[(x*3+y*5)%#style.floor+1]
            local sx,sy=Terrain.region(index)
            for yy=0,15 do for xx=0,15 do
                local expected={source:getPixel(sx+xx,sy+yy)}
                assert(expected[4]==1,'selected ground tile has transparent holes')
                local actual={image:getPixel(math.floor(px+(xx+.5)*40/16),
                    math.floor(py+(yy+.5)*40/16))}
                for c=1,3 do assert(math.abs(expected[c]-actual[c])<1.1/255,
                    ('native terrain mismatch %s floor=%d cell=%d,%d'):format(slug,index,x,y))end
            end end
            compared=compared+1
        end
        if water_compared<2 and map.rows[y]:sub(x,x)=='~' and
            (x~=focus_x or y~=focus_y) and px>=210 and px+40<=990 and py>=80 and py+40<=440 then
            local h=terrain:resolve(map,x,y,'water',function()return true end)
            for yy=0,15 do for xx=0,15 do
                local expected={surfaces:getPixel(h.offset_x+xx,h.offset_y+yy)}
                if expected[4]==1 then
                    local actual={image:getPixel(math.floor(px+(xx+.5)*40/16),
                        math.floor(py+(yy+.5)*40/16))}
                    for c=1,3 do assert(math.abs(expected[c]-actual[c])<1.1/255,
                        'native shoreline pixels differ')end
                end
            end end
            water_compared=water_compared+1
        end
    end end
    assert(compared>=1,'no unobstructed ground pixels tested in '..slug)
    if map.biome_id==Biomes.order[3]then assert(water_compared>=1,'aqueduct water was not sampled')end
end
local read=love.filesystem.read
local function advance()
    if phase<7 then love.keypressed('f7');pan()
    elseif phase==7 then
        love.filesystem.read=function(path,...)
            if path==Terrain.path then return 'interrupted terrain publish'end
            return read(path,...)
        end
        love.keypressed('f5');update(0)
        assert(love.roagVisualRefreshStatus():match('^rejected'))
        love.filesystem.read=read
    elseif phase==8 then love.keypressed('f5');update(0)
    else love.event.quit();return end
    phase=phase+1
end
function love.draw()
    if failed then return end
    local ok,why=pcall(function()
        draw();if pending then return end
        pending=true
        love.graphics.captureScreenshot(function(image)
            local good,err=pcall(function()
                local slug=phase<=7 and Content.map_for(state).biome_id:match('/([^/]+)$') or
                    phase==8 and 'rejected-refresh' or 'refreshed'
                PNG.write(output..'/'..slug..'.png',image)
                check(image);advance();pending=false
            end)
            if not good then fail(err)end
        end)
    end)
    if not ok then fail(why)end
end
LUA
ROAG_CAVES_OUTPUT="$output" ROAG_SPRITE_PACK=kenney ROAG_SPECTATOR=1 \
    XDG_DATA_HOME="$output/data" timeout -k 3s 45s love "$output/app" > "$output/run.log" 2>&1 || {
        cat "$output/run.log";exit 1;
    }
printf 'PASS seven generated biomes, native floor pixels, camera/F5 state isolation: %s\n' "$output"
