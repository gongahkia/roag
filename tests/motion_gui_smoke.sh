#!/bin/sh
# Callback-driven rendered start/mid/end frames for player, enemy and Dash.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-motion-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Camera=require("src.camera")
local original_new_stage=Game.new_stage
local tracked,initial_camera,latest_camera
local runs=0
function Game.new_stage(seed)
    runs=runs+1
    tracked=Game.new(seed,"core:map/scrolling_room")
    if runs==1 then Game.spawn_enemy(tracked,"core:enemy/mossbound_guard",7,3) end
    return tracked
end
local camera_update=Camera.update
function Camera.update(camera,...)
    initial_camera=initial_camera or camera.x
    camera_update(camera,...)
    latest_camera=camera.x
end
assert(love.filesystem.load("roag_main.lua"))()
local original_load,original_update,original_draw=love.load,love.update,love.draw
local circle,rectangle=love.graphics.circle,love.graphics.rectangle
local player_x,guard_x
love.graphics.circle=function(mode,x,y,radius,...)
    if mode=="fill" and radius==11 then player_x=x end
    return circle(mode,x,y,radius,...)
end
love.graphics.rectangle=function(mode,x,y,width,height,...)
    if mode=="fill" and width==20 and height==19 then guard_x=x end
    return rectangle(mode,x,y,width,height,...)
end
local output=assert(os.getenv("ROAG_MOTION_GUI_OUTPUT"))
local labels={"move_start","move_mid","move_end",
    "partial_start","partial_mid","partial_end",
    "dash_start","dash_leg1","dash_leg2","dash_end","scroll_end"}
local values={}
local phase,pending=1,false
function love.load(...)
    original_load(...)
    love.keypressed("d")
    assert(Game.player(tracked).position.x==4)
end
function love.update() original_update(0) end
local function capture(label)
    local p=Game.player(tracked).position
    values[label]={player=assert(player_x),guard=guard_x,
        logical=p.x,clock=tracked.clock,camera=latest_camera or initial_camera}
    love.graphics.captureScreenshot(function(image)
        local path=output.."/"..label..".png"
        local file=assert(io.open(path,"wb"))
        file:write(image:encode("png"):getString())
        file:close()
        if phase==1 then original_update(0.04)
        elseif phase==2 then original_update(2)
        elseif phase==3 then
            love.keypressed("x");love.keypressed("d");love.keypressed("return")
            assert(tracked.last_action.kind=="dash" and tracked.last_action.distance==1)
        elseif phase==4 then original_update(0.04)
        elseif phase==5 then original_update(2)
        elseif phase==6 then
            love.keypressed("n") -- fresh fixture, without the guard
            love.keypressed("x");love.keypressed("d");love.keypressed("return")
            assert(tracked.last_action.kind=="dash" and tracked.last_action.distance==2)
        elseif phase==7 then original_update(0.04)
        elseif phase==8 then original_update(0.04)
        elseif phase==9 then original_update(0.08)
        elseif phase==10 then
            for _=1,14 do love.keypressed("d");original_update(0.1) end
        else
            assert(values.move_start.logical==4 and
                values.move_start.player<values.move_mid.player and
                values.move_mid.player<values.move_end.player,
                "rendered Adventurer did not slide")
            assert(values.move_start.guard and values.move_mid.guard and
                values.move_end.guard and
                values.move_start.guard>values.move_mid.guard and
                values.move_mid.guard>values.move_end.guard,
                "rendered Guard did not slide")
            assert(values.partial_start.logical==5 and
                values.partial_start.player<values.partial_mid.player and
                values.partial_mid.player<values.partial_end.player,
                "partial Dash did not stop at first tile")
            assert(values.dash_start.logical==5 and
                values.dash_start.player<values.dash_leg1.player and
                values.dash_leg1.player<values.dash_leg2.player and
                values.dash_leg2.player<values.dash_end.player,
                "two-tile Dash did not traverse both legs")
            assert(values.scroll_end.camera>values.dash_end.camera,
                "camera did not follow beyond viewport edge")
            local file=assert(io.open(output.."/frames.tsv","w"))
            file:write("phase\tplayer_screen_x\tguard_screen_x\tlogical_x\tworld_time\tcamera_x\n")
            for _,name in ipairs(labels) do
                local v=values[name]
                file:write(name,"\t",v.player,"\t",tostring(v.guard),"\t",
                    v.logical,"\t",v.clock,"\t",tostring(v.camera),"\n")
            end
            file:close()
            love.event.quit()
        end
        phase=phase+1
        pending=false
    end)
end
function love.draw(...)
    player_x,guard_x=nil,nil
    original_draw(...)
    if phase>#labels or pending then return end
    pending=true
    capture(labels[phase])
end
LUA
ROAG_MOTION_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 \
    timeout 20s love "$output_dir"
for frame in move_start move_mid move_end partial_start partial_mid partial_end \
    dash_start dash_leg1 dash_leg2 dash_end scroll_end; do
    test -s "$output_dir/$frame.png"
done
test -s "$output_dir/frames.tsv"
printf 'Motion GUI smoke passed: 11 rendered frames in %s\n' "$output_dir"
