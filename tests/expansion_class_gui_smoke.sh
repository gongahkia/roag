#!/bin/sh
# Four isolated title -> class choice -> New Run -> attack callback sessions.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-class-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require("src.game")
local Classes=require("src.classes")
local chosen=assert(tonumber(os.getenv("ROAG_CLASS_INDEX")))
local target=Classes.order[chosen]
local tracked,last_attack
local old_new,old_submit=Game.new_stage,Game.submit
function Game.new_stage(seed,options)
    tracked=old_new(seed,options)
    return tracked
end
function Game.submit(state,intent)
    local ok,reason,events,preview=old_submit(state,intent)
    for _,event in ipairs(events or {}) do
        if event.kind=="AttackPerformed" and event.source_id==state.player_id then
            last_attack=event
        end
    end
    return ok,reason,events,preview
end
assert(love.filesystem.load("roag_main.lua"))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local old_print,old_printf=love.graphics.print,love.graphics.printf
local labels={}
love.graphics.print=function(v,...)
    labels[#labels+1]=tostring(v);return old_print(v,...)
end
love.graphics.printf=function(v,...)
    labels[#labels+1]=tostring(v);return old_printf(v,...)
end
local out=assert(os.getenv("ROAG_CLASS_OUTPUT"))
local phase,pending=1,false
local function has(fragment)
    for _,label in ipairs(labels) do
        if label:find(fragment,1,true) then return true end
    end
end
function love.load(...)
    old_load(...)
    love.keypressed(tostring(chosen))
end
function love.draw(...)
    labels={};old_draw(...)
    if phase>2 or pending then return end
    if phase==1 then
        assert(has('1–6  CHOOSE CLASS') and
            has(Classes.get(target).name:upper()),"title class choice missing")
    else
        assert(tracked and Game.player(tracked).definition_id==target
            and tracked.floor_order and tracked.clock>0,
            "class New Run did not create a normal paired stage")
        assert(has(Classes.get(target).name:upper()..'  ·  LV')
            and last_attack and last_attack.ability_id==Classes.get(target).basic_id,
            "class ability/HUD did not render")
    end
    pending=true
    love.graphics.captureScreenshot(function(image)
        local path=out.."/class-"..chosen.."-"..phase..".png"
        local file=assert(io.open(path,"wb"))
        file:write(image:encode("png"):getString());file:close()
        if phase==1 then
            love.keypressed("n")
            assert(tracked and Game.player(tracked).definition_id==target)
            love.keypressed("f")
            love.keypressed("d")
            love.keypressed("return")
            old_update(10)
        else love.event.quit() end
        phase=phase+1;pending=false
    end)
end
LUA
for index in 1 2 3 4; do
    mkdir -p "$output_dir/data-$index"
    ROAG_CLASS_INDEX="$index" ROAG_CLASS_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/data-$index" \
        timeout 25s love "$output_dir" >"$output_dir/class-$index.log" 2>&1
    test -s "$output_dir/class-$index-1.png"
    test -s "$output_dir/class-$index-2.png"
done
printf 'Class GUI PASS: four title/New Run/ability sessions in %s\n' "$output_dir"
