#!/bin/sh
# Synthetic high-stack chain rendered through the real game frontend.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-t15-chain-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
local Persistence=require('src.persistence')
local original_new,original_add=Game.new,Playback.add
local tracked,playback,first_item
function Game.new(seed)
    tracked=original_new(21)
    for _,p in ipairs({{3,2},{3,3},{2,3},{4,2},{5,2}}) do
        Game.spawn_enemy(tracked,'core:enemy/ruin_skitter',p[1],p[2],100)
    end
    return tracked
end
function Playback.add(queue,events,player_id)
    playback=queue
    return original_add(queue,events,player_id)
end
assert(love.filesystem.load('roag_main.lua'))()
local original_load,original_update,original_draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_T15_CHAIN_OUTPUT'))
local phase,pending,next_action=1,false,nil
local labels={}
local old_printf,old_print=love.graphics.printf,love.graphics.print
love.graphics.printf=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_printf(value,...)
end
love.graphics.print=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_print(value,...)
end
function love.load(...)
    original_load(...)
    -- Autostart deliberately clears showcase stacks, so install this synthetic
    -- stress build only after the normal frontend initialization.
    for _,name in ipairs({'storm_conductor','detonation_bloom',
        'resonant_wounds','temporal_echo','static_footsteps'}) do
        Game.set_boon_stacks(tracked,tracked.player_id,'core:boon/'..name,
            'legendary',8)
    end
    love.keypressed('r');love.keypressed('d');love.keypressed('return')
    assert(tracked.clock==150)
    original_update(0.22)
    first_item=playback.items[1]
    local group=assert(Playback.current_group(playback));assert(#group>=10)
    local found=false;local duration=0
    for _,event in ipairs(group)do if event.kind=='BoonActivated' and
        event.boon_id=='core:boon/temporal_echo'then found=true end end
    for _,item in ipairs(playback.items)do duration=duration+item.duration end
    assert(found and duration<3,'dense causal feedback became an unbounded presentation backlog')
end
function love.update(dt)
    original_update(0)
    if next_action then
        local action=next_action;next_action=nil
        action();pending=false
    end
end
function love.draw(...)
    labels={}
    original_draw(...)
    if pending or phase>2 then return end
    if phase==1 then
        local activated,boon=false,false
        for _,label in ipairs(labels) do
            if label:find('BOON CHAIN',1,true) then activated=true end
            if label:find('procs',1,true) then boon=true end
        end
        assert(activated and boon,'dense boon beat did not render its grouped HUD')
    else
        assert(playback.items[1]~=first_item)
    end
    pending=true
    local label=phase==1 and 'boon' or 'later'
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/'..label..'.tga','wb'))
        file:write(image:encode('tga'):getString());file:close()
        if phase==1 then
            next_action=function()
                local before=Persistence.encode(Game.snapshot(tracked))
                love.keypressed('space')
                assert(Persistence.encode(Game.snapshot(tracked))==before,
                    'dense playback accepted hidden input')
                original_update(0.37)
            end
        else next_action=function() love.event.quit() end end
        phase=phase+1
    end)
end
LUA
ROAG_T15_CHAIN_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 XDG_DATA_HOME="$output_dir/data" \
    timeout -k 3s 20s love "$output_dir" > "$output_dir/love.log" 2>&1 || {
        cat "$output_dir/love.log" >&2
        printf 'Tranche 15 chain artifacts: %s\n' "$output_dir" >&2
        exit 1
    }
test -s "$output_dir/boon.tga"
test -s "$output_dir/later.tga"
cmp -s "$output_dir/boon.tga" "$output_dir/later.tga" && {
    echo 'Boon cascade did not change rendered frame' >&2; exit 1;
}
printf 'Tranche 15 chain GUI smoke passed: %s\n' "$output_dir"
