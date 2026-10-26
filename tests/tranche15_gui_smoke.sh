#!/bin/sh
# Callback-driven check that simultaneous locked attacks are readable in one beat.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-t15-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
local original_new,original_add=Game.new,Playback.add
local tracked,playback
function Game.new(seed)
    tracked=original_new(seed,'core:map/test_room')
    Game.spawn_enemy(tracked,'core:enemy/mossbound_guard',3,2)
    Game.spawn_enemy(tracked,'core:enemy/mossbound_guard',2,3)
    Game.spawn_enemy(tracked,'core:enemy/ruin_skitter',3,3)
    return tracked
end
function Playback.add(queue,events,player_id)
    playback=queue
    return original_add(queue,events,player_id)
end
assert(love.filesystem.load('roag_main.lua'))()
local original_load,original_update,original_draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_T15_GUI_OUTPUT'))
local phase,pending,next_action=1,false,nil
local printed={}
local old_printf,old_print=love.graphics.printf,love.graphics.print
love.graphics.printf=function(value,...)
    printed[#printed+1]=tostring(value)
    return old_printf(value,...)
end
love.graphics.print=function(value,...)
    printed[#printed+1]=tostring(value)
    return old_print(value,...)
end
function love.load(...)
    original_load(...)
    love.keypressed('space')
    assert(tracked.clock==100)
    local group=Playback.current_group(playback)
    assert(group and #group==3 and group[1].time==0)
end
function love.update(dt)
    original_update(0)
    if next_action then
        local action=next_action;next_action=nil
        action();pending=false
    end
end
function love.draw(...)
    printed={}
    original_draw(...)
    if pending or phase>3 then return end
    if phase<3 then
        local group=Playback.current_group(playback)
        assert(group and #group==3)
        local found=false
        for _,label in ipairs(printed) do
            if label:find('3 locked attacks',1,true) then found=true end
        end
        assert(found,'grouped locked-attack HUD is absent')
        if phase==2 then
            local ignored=false
            for _,label in ipairs(printed) do
                if label:find('Input ignored during playback',1,true) then
                    ignored=true
                end
            end
            assert(ignored,'ignored key has no visible input-gate feedback')
        end
    else
        assert(Playback.current_group(playback)==nil)
    end
    pending=true
    local label=({'start','mid','after'})[phase]
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/'..label..'.tga','wb'))
        file:write(image:encode('tga'):getString());file:close()
        if phase==1 then
            next_action=function()
                local Persistence=require('src.persistence')
                local before=Persistence.encode(Game.snapshot(tracked))
                love.keypressed('space')
                assert(tracked.clock==100,'playback input gate leaked a turn')
                assert(Persistence.encode(Game.snapshot(tracked))==before,
                    'ignored input changed authoritative state')
                original_update(0.27)
            end
        elseif phase==2 then
            next_action=function() original_update(0.29) end
        else next_action=function() love.event.quit() end end
        phase=phase+1
    end)
end
LUA
ROAG_T15_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 XDG_DATA_HOME="$output_dir/data" \
    timeout -k 3s 20s love "$output_dir" > "$output_dir/love.log" 2>&1 || {
        cat "$output_dir/love.log" >&2
        printf 'Tranche 15 GUI artifacts: %s\n' "$output_dir" >&2
        exit 1
    }
for frame in start mid after; do test -s "$output_dir/$frame.tga"; done
cmp -s "$output_dir/start.tga" "$output_dir/after.tga" && {
    echo 'Windup beat did not change the rendered frame' >&2; exit 1;
}
printf 'Tranche 15 GUI smoke passed: grouped windups and input gate in %s\n' "$output_dir"
