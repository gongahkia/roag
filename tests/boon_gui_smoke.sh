#!/bin/sh
# Callback-driven LÖVE smoke for the developer loadout and authoritative boon playback.
set -eu

project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-boon-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"

cat > "$output_dir/main.lua" <<'LUA'
io.stdout:setvbuf('line')
function love.errorhandler(why)
 io.stderr:write(debug.traceback(tostring(why),2)..'\n');return function()return 1 end
end
local Game = require("src.game")
local Playback = require("src.playback")
local Scheduler = require("src.scheduler")
local original_new, original_submit = Game.new, Game.submit
local tracked, events, playback
function Game.new(seed)
    tracked = original_new(seed,"core:map/scrolling_room",
        {demo_enemies=true,demo_chests=true})
    local first = Game.spawn_enemy(tracked, "core:enemy/ruin_skitter", 4, 3)
    local second = Game.spawn_enemy(tracked, "core:enemy/thornspitter", 5, 3)
    Scheduler.remove_for_actor(tracked, first.id)
    Scheduler.remove_for_actor(tracked, second.id)
    return tracked
end
function Game.submit(...)
    local ok, reason, output = original_submit(...)
    events = output
    return ok, reason, output
end
local original_add = Playback.add
function Playback.add(queue, output, player_id)
    playback = queue
    return original_add(queue, output, player_id)
end

assert(love.filesystem.load("roag_main.lua"))()
local original_load, original_update, original_draw = love.load, love.update, love.draw
local output_dir = assert(os.getenv("ROAG_BOON_GUI_OUTPUT"))
local phase,pending,captured,failed=1,false,nil,false
local displayed_item
local function snapshot()return require('src.persistence').encode(Game.snapshot(tracked))end
local function showing_boon()
 local event=Playback.current(playback)
 for _,e in ipairs(Playback.current_group(playback)or (event and {event}or {}))do
  if e.kind=='BoonActivated'then return true end
 end
end
local function fail(why)
 failed=true;io.stderr:write('BOON GUI phase '..phase..': '..tostring(why)..'\n');love.event.quit(1)
end
function love.load(...)
 local args={...};local ok,why=xpcall(function()
  love.mouse.getPosition=function()return 0,0 end;love.keyboard.isDown=function()return false end
  original_load(unpack(args))
  assert(tracked.clock==0 and next(Game.player(tracked).boons)==nil)
 end,debug.traceback);if not ok then fail(why)end
end
function love.update()
 if failed then return end
 local ok,why=xpcall(function()
  original_update(0)
  if not captured then return end
  local image=captured;captured=nil
  local file=assert(io.open(output_dir..'/phase'..phase..'.png','wb'))
  assert(file:write(image:encode('png'):getString()));file:close()
  if phase==1 then
   love.keypressed('b');love.keypressed('b');assert(tracked.clock==0)
  elseif phase==2 then
   love.keypressed('f');love.keypressed('d');love.keypressed('return')
   assert(tracked.clock==100 and events)
   local activated,applied=false,false
   for _,e in ipairs(events)do
    if e.kind=='BoonActivated'then activated=true end
    if e.kind=='EffectApplied'then applied=true end
   end
   assert(activated and applied)
   for _=1,400 do if showing_boon()then break end;original_update(.01)end
   assert(showing_boon(),'no actual boon event reached the displayed beat')
   displayed_item=playback.items[1]
  else
   local before=snapshot();love.keypressed('space');original_update(.1)
   assert(tracked.clock==100 and snapshot()==before,'playback accepted a hidden action')
   print('PASS boon loadout, real proc/effect group and unchanged gated snapshot')
   love.event.quit()
  end
  phase=phase+1
 end,debug.traceback);if not ok then fail(why)end
end
function love.draw(...)
 if failed then return end
 local args={...};local ok,why=xpcall(function()
  original_draw(unpack(args))
  if phase>3 or pending or captured then return end
  if phase==2 then assert(tracked.clock==0 and Game.player(tracked).boons['core:boon/storm_conductor'])
  elseif phase==3 then assert(tracked.clock==100 and showing_boon()and playback.items[1]==displayed_item)end
  pending=true
  love.graphics.captureScreenshot(function(image)captured=image;pending=false end)
 end,debug.traceback);if not ok then fail(why)end
end

LUA

luajit -b "$output_dir/main.lua" /dev/null
ROAG_BOON_GUI_OUTPUT="$output_dir" ROAG_GUI_AUTOSTART=1 XDG_DATA_HOME="$output_dir/data" \
 timeout -k 3s 20s love "$output_dir" > "$output_dir/love.log" 2>&1 || {
  cat "$output_dir/love.log";printf 'Retained failed boon GUI: %s\n' "$output_dir";exit 1;
 }
for phase in 1 2 3; do test -s "$output_dir/phase$phase.png"; done
printf 'Boon GUI smoke passed: three screenshots in %s\n' "$output_dir"
