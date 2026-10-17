#!/bin/sh
# Assisted source-art coverage fixture in the real game renderer, not a legal run.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-symbols-gui-XXXXXX")}
mkdir -p "$output/app/assets" "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets/fonts" "$project_dir/assets/kenney" "$output/app/assets/"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/"
cat > "$output/app/main.lua" <<'LUA'
local Game=require('src.game')
local Camera=require('src.camera')
local Content=require('src.content')
local Terrain=require('src.terrain')
local Boons=require('src.boon_content')
local Symbols=require('src.kenney_symbols')
local TerrainArt=require('src.kenney_terrain')
local Codec=require('src.persistence')
local PNG=require('tools.art_png')
local state,camera
function Game.new_stage(seed)
    state=Game.new(seed,'core:map/scrolling_room')
    local map=Content.map_for(state)
    map.rows[5]=map.rows[5]:sub(1,2)..'bgiBmc'..map.rows[5]:sub(9)
    return state
end
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_SYMBOLS_OUTPUT'))
local before,source,ground,phase,pending,failed=nil,nil,nil,0,false,false
local function snapshot()return Codec.encode(Game.snapshot(state))end
local function fail(why)
    failed=true;io.stderr:write('Kenney symbols FAILED: '..tostring(why)..'\n')
    love.event.quit(1)
end
function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args))
        for _,id in ipairs(Boons.order)do
            Game.set_boon_stacks(state,state.player_id,id,'common',1)
        end
        before=snapshot();source=love.image.newImageData(Symbols.path)
        ground=love.image.newImageData(TerrainArt.path)
        assert(love.roagVisualPackMode()=='kenney')
    end)
    if not ok then fail(why)end
end
function love.update()end
local function compare(image,definition,x,y)
    local sx,sy=Symbols.region(definition[1]);local count=0
    for yy=0,15 do for xx=0,15 do
        local r,g,b,a=source:getPixel(sx+xx,sy+yy)
        if a==1 then
            local actual={image:getPixel(math.floor(x+xx*2+1),math.floor(y+yy*2+1))}
            local tint=definition[3]
            for i,c in ipairs({r,g,b})do
                -- Tint is quantized into vertex colors and again into the
                -- RGBA8 framebuffer: allow at most two channel byte values.
                assert(math.abs(actual[i]-c*tint[i])<=2/255+1e-6,
                    ('native icon pixel differs: tile=%d at %d,%d channel=%d expected=%.5f actual=%.5f screen=%.1f,%.1f'):format(
                        definition[1],xx,yy,i,c*tint[i],actual[i],x+xx*2+1,y+yy*2+1))
            end
            count=count+1
        end
    end end
    assert(count>8,'no meaningful source pixels compared')
end
local function check(image)
    assert(snapshot()==before,'art preview or F5 changed game snapshot/RNG')
    if phase==0 or phase>=9 then
        for i,symbol in ipairs(Terrain.order)do
            local definition=Symbols.terrain[Terrain.get(symbol).id]
            local x,y=Camera.tile_top_left(camera,i+2,5)
            compare(image,definition,x+4,y+4)
            local pixel={image:getPixel(math.floor(x+1),math.floor(y+1))}
            local sx,sy=TerrainArt.region(TerrainArt.styles.overgrown_ruins.floor[1])
            local floor={ground:getPixel(sx,sy)}
            for channel=1,3 do assert(math.abs(pixel[channel]-(definition.ground[channel]*.42+
                floor[channel]*.58))<2/255,
                'terrain ground backing mismatch')end
        end
    elseif phase<=8 then
        for line=1,7 do
            local id=Boons.order[(phase-1)*7+line]
            if id then compare(image,Symbols.boons[id],104,150+(line-1)*44)end
        end
    end
end
local function advance()
    if phase==0 then love.keypressed('i')
    elseif phase<8 then love.keypressed('right')
    elseif phase==8 then
        love.keypressed('escape')
        local read=love.filesystem.read
        love.filesystem.read=function(path,...)
            if path==Symbols.path then return 'incomplete symbol publish'end
            return read(path,...)
        end
        love.keypressed('f5');update(0)
        assert(love.roagVisualRefreshStatus():match('^rejected'))
        assert(love.roagAssetBacking(Boons.order[1]..'/icon')=='kenney')
        love.filesystem.read=read
    elseif phase==9 then
        love.keypressed('f5');update(0)
        assert(love.roagAssetBacking(Boons.order[1]..'/icon')=='kenney')
    else love.event.quit();return end
    update(0);phase=phase+1
end
function love.draw()
    if failed then return end
    local ok,why=pcall(function()
        draw();if pending then return end
        pending=true
        love.graphics.captureScreenshot(function(image)
            local good,err=pcall(function()
                PNG.write(output..'/'..(phase==0 and 'terrain' or phase<=8 and
                    ('boons-'..phase) or phase==9 and 'rejected-refresh' or 'refreshed')..'.png',image)
                check(image);advance();pending=false
            end)
            if not good then fail(err)end
        end)
    end)
    if not ok then fail(why)end
end
LUA
ROAG_SYMBOLS_OUTPUT="$output" ROAG_SPRITE_PACK=kenney ROAG_GUI_AUTOSTART=1 \
    XDG_DATA_HOME="$output/data" timeout -k 3s 35s love "$output/app" > "$output/run.log" 2>&1 || {
        cat "$output/run.log";exit 1;
    }
printf 'PASS all 52 boon/6 terrain native pixels, refresh and snapshot: %s\n' "$output"
