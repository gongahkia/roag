#!/bin/sh
# Real player preview callbacks; isolated saves and framebuffer comparisons.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-preview-gui-XXXXXX")}
mkdir -p "$output/app/assets" "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets/fonts" "$project_dir/assets/kenney" "$output/app/assets/"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/"
cat > "$output/app/main.lua" <<'LUA'
local Game=require('src.game')
local Camera=require('src.camera')
local Codec=require('src.persistence')
local Outline=require('src.attack_outline')
local PNG=require('tools.art_png')
local state,camera
function Game.new_stage(seed)
    state=Game.new(seed,'core:map/scrolling_room');return state
end
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_PREVIEW_OUTPUT'))
local cases={
    {'sword','f','basic_attack','d',1,0},
    {'sweep','r','sweeping_slash','d',1,0},
    {'dash','x','dash','d',1,0},
    {'blocked-dash','x','dash','w',0,-1},
}
local zooms={40}
local zoom,phase,pending,failed=1,0,false,false
local baseline,before
local function snapshot()return Codec.encode(Game.snapshot(state))end
local function fail(why)
    failed=true;io.stderr:write('Attack preview FAILED: '..tostring(why)..'\n')
    love.event.quit(1)
end
function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args));before=snapshot()
        assert(love.roagVisualPackMode()=='kenney')
        assert(camera.tile==40,'fixed camera size changed')
        for _,key in ipairs({'=','+','kp+','-','kp-'})do love.keypressed(key)end
        love.wheelmoved(0,3);love.wheelmoved(0,-3)
        assert(camera.tile==40,'removed zoom controls changed the camera')
        update(0)
    end)
    if not ok then fail(why)end
end
function love.update()end
local function check(image)
    assert(snapshot()==before,'preview/cancel/zoom changed authoritative state')
    if phase==0 then baseline=image;return end
    local c=cases[phase]
    local preview=Game.preview(state,c[3],c[5],c[6])
    assert(#preview.cells>0)
    local blocked=false
    for _,cell in ipairs(preview.cells)do
        local x,y=Camera.tile_top_left(camera,cell.x,cell.y)
        for yy=8,camera.tile-9 do for xx=8,camera.tile-9 do
            local old={baseline:getPixel(math.floor(x+xx),math.floor(y+yy))}
            local now={image:getPixel(math.floor(x+xx),math.floor(y+yy))}
            for channel=1,4 do assert(math.abs(old[channel]-now[channel])<1/255,
                c[1]..' preview filled the tile interior')end
        end end
        local color=c[3]=='dash' and (cell.status=='open' and {.22,.85,.55} or
            cell.status=='unreachable' and {.55,.55,.55} or {1,.27,.28}) or
            c[3]=='sweeping_slash' and {1,.56,.17} or {1,.86,.30}
        local rect=Outline.rectangles(camera.tile)[1]
        local pixel={image:getPixel(math.floor(x+rect[1]+1),math.floor(y+rect[2]))}
        for channel=1,3 do assert(math.abs(pixel[channel]-color[channel])<.04,
            c[1]..' dotted border/status color missing')end
        if cell.status=='wall' then blocked=true end
    end
    if c[1]=='blocked-dash'then assert(blocked,'blocked fixture did not reach a wall')end
end
local function advance()
    if phase>0 then love.keypressed('escape')end
    phase=phase+1
    if phase>#cases then
        zoom=zoom+1;phase=0
        if zoom>#zooms then love.event.quit();return end
    else
        local c=cases[phase];love.keypressed(c[2]);love.keypressed(c[4])
    end
    update(0)
end
function love.draw()
    if failed then return end
    local ok,why=pcall(function()
        draw()
        if pending then return end
        pending=true
        love.graphics.captureScreenshot(function(image)
            local good,err=pcall(function()
                PNG.write(output..'/'..zooms[zoom]..'-'..
                    (phase==0 and 'baseline' or cases[phase][1])..'.png',image)
                check(image);advance();pending=false
            end)
            if not good then fail(err)end
        end)
    end)
    if not ok then fail(why)end
end
LUA
ROAG_PREVIEW_OUTPUT="$output" ROAG_SPRITE_PACK=kenney ROAG_GUI_AUTOSTART=1 \
    XDG_DATA_HOME="$output/data" timeout -k 3s 35s love "$output/app" > "$output/run.log" 2>&1
printf 'PASS: dotted Sword/Sweep/Dash/blocked previews, clear interiors, exact snapshot, fixed camera: %s\n' "$output"
