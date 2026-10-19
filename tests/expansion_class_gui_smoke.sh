#!/bin/sh
# Every current class: title -> visible class choice -> New Run -> attack/stats.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-class-gui-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/." "$output_dir/assets/"
mkdir -p "$output_dir/tools"
cp "$project_dir/tools/art_png.lua" "$output_dir/tools/"
cat > "$output_dir/main.lua" <<'LUA'
io.stdout:setvbuf('line')
function love.errorhandler(why)
 io.stderr:write(debug.traceback(tostring(why),2)..'\n');return function()return 1 end
end
local G=require('src.game');local C=require('src.classes');local P=require('src.persistence')
local PNG=require('tools.art_png')
local index=assert(tonumber(os.getenv('ROAG_CLASS_INDEX')));local id=assert(C.order[index])
local s,last
local new,submit=G.new_stage,G.submit
function G.new_stage(...)s=new(...);return s end
function G.submit(...)
 local ok,why,events,preview=submit(...)
 for _,e in ipairs(events or {})do if e.kind=='AttackPerformed' and s and e.source_id==s.player_id then last=e end end
 return ok,why,events,preview
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,pending,captured,failed=1,false,nil,false
local output=assert(os.getenv('ROAG_CLASS_OUTPUT'));local snapshot
local labels={};local print_,printf=love.graphics.print,love.graphics.printf
love.graphics.print=function(v,...)labels[#labels+1]=tostring(v);return print_(v,...)end
love.graphics.printf=function(v,...)labels[#labels+1]=tostring(v);return printf(v,...)end
local function has(text)for _,v in ipairs(labels)do if v:find(text,1,true)then return true end end end
local function fail(why)failed=true;io.stderr:write(tostring(why)..'\n');love.event.quit(1)end
function love.load(...)
 local args={...};local ok,why=xpcall(function()
  love.mouse.getPosition=function()return 0,0 end;love.keyboard.isDown=function()return false end
  load(unpack(args));love.keypressed('return')
  assert(love.roagUIState().page=='class')
  for _=2,index do love.keypressed('right')end
  assert(love.roagUIState().class==id)
 end,debug.traceback);if not ok then fail(why)end
end
function love.update()
 if failed or not captured then return end
 local ok,why=xpcall(function()
  PNG.write(output..'/class-'..index..'-'..phase..'.png',captured);captured=nil
  assert(has(C.get(id).name:upper()),'selected class name not drawn')
  if phase==1 then
   assert(has('CHOOSE YOUR CHARACTER'));love.keypressed('return')
   assert(s and G.player(s).definition_id==id and s.floor_order)
   love.keypressed('f');love.keypressed('d');love.keypressed('return');update(10)
   assert(s.clock>0 and last and last.ability_id==C.get(id).basic_id)
   love.keypressed('i');snapshot=P.encode(G.snapshot(s));phase=2
  else
   assert(P.encode(G.snapshot(s))==snapshot and love.roagUIState().page=='inventory')
   print('PASS class '..id..': visible choice, normal paired New Run, real F and character stats')
   love.event.quit()
  end
 end,debug.traceback);if not ok then fail(why)end
end
function love.draw()
 if failed then return end
 local ok,why=xpcall(function()
  labels={};draw()
  if not pending and not captured then pending=true
   love.graphics.captureScreenshot(function(im)captured=im;pending=false end)
  end
 end,debug.traceback);if not ok then fail(why)end
end

LUA
for index in $(luajit -e 'for i in ipairs(require("src.classes").order)do io.write(i," ")end'); do
    mkdir -p "$output_dir/data-$index"
    ROAG_CLASS_INDEX="$index" ROAG_CLASS_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/data-$index" \
        timeout 25s love "$output_dir" >"$output_dir/class-$index.log" 2>&1
    test -s "$output_dir/class-$index-1.png"
    test -s "$output_dir/class-$index-2.png"
done
printf 'Class GUI PASS: all current title/New Run/ability sessions in %s\n' "$output_dir"
