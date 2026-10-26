#!/bin/sh
# Actual native world renderer; independent generated rooms, no committed turns.
set -eu
cd "$(dirname "$0")/.."
if [ "$#" -eq 0 ];then
 set -- crypt_sanctuary observatory root_garden drowned_sanctum forge_masonry \
  archive_library broken_cavern ruin_masonry ruin_carved
fi
for family do
 output=$(mktemp -d /var/tmp/roag-environment-gui-XXXXXX)
 mkdir -p "$output/app/tools" "$output/data"
 cp main.lua "$output/app/roag_main.lua"
 cp conf.lua "$output/app/conf.lua"
 cp -R src assets "$output/app/"
 cp tools/art_png.lua "$output/app/tools/"
 cat > "$output/app/main.lua" <<'LUA'
io.stdout:setvbuf('line')
function love.errorhandler(why)
 io.stderr:write(debug.traceback(tostring(why),2)..'\n');return function()return 1 end
end
local G=require('src.game');local E=require('src.idea_environment');local B=require('src.biomes')
local C=require('src.content');local V=require('src.visibility');local P=require('src.persistence')
local O=require('src.onebit_visuals');local PNG=require('tools.art_png')
local wanted=assert(os.getenv('ROAG_ENV_FAMILY'));local output=assert(os.getenv('ROAG_ENV_OUTPUT'))
local tracked,native,entry
local new=G.new_stage
function G.new_stage()
 for seed=1,8 do for _,biome in ipairs(B.order)do
  local s=new(seed,{multifloor=true,stage_options={biome_id=biome}})
  for _,r in ipairs(E.plan(s,s.generated_map).rooms)do if r.family==wanted
   and (wanted~='root_garden' and wanted~='archive_library' or r.patch)then
   local p=G.player(s);local room=r.room
   local candidates={}
   for y=room.y+1,room.y+room.height-2 do for x=room.x+1,room.x+room.width-2 do
    if C.walkable(s.generated_map,x,y)then
     local free=true
     for _,a in ipairs(s.entities)do if a.id~=p.id and a.blocks_movement and a.position.x==x and a.position.y==y then free=false end end
     if free then candidates[#candidates+1]={x=x,y=y,
      d=math.abs(x-(room.x+room.width/2))+math.abs(y-(room.y+room.height/2))}end
    end
   end end
   table.sort(candidates,function(a,b)
    if a.d~=b.d then return a.d<b.d elseif a.y~=b.y then return a.y<b.y else return a.x<b.x end
   end)
   if candidates[1]then
    p.position.x,p.position.y=candidates[1].x,candidates[1].y
    V.recompute(s,12);tracked=s;entry=r;return s
   end
  end end
 end end
 error('generated family unavailable: '..wanted)
end
local make=O.new
function O.new(...)local n,why=make(...);native=n;return n,why end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,pending,captured,failed=1,false,nil,false
local before,baseline,quads,draws
local rawdraw=love.graphics.draw
love.graphics.draw=function(texture,quad,...)
 if quads and quads[quad]then draws=draws+1 end
 return rawdraw(texture,quad,...)
end
local function snapshot()return P.encode(G.snapshot(tracked))end
local function fail(why)failed=true;io.stderr:write(tostring(why)..'\n');love.event.quit(1)end
local function collect_quads()
 assert(native and native.environment and not next(native.environment.failures))
 quads={}
 for _,list in pairs(native.environment.materials)do for _,h in pairs(list)do quads[h.quad]=true end end
 for _,h in pairs(native.environment.handles)do quads[h.quad]=true end
end
function love.load(...)
 local args={...};local ok,why=xpcall(function()
  love.mouse.getPosition=function()return 0,0 end;love.keyboard.isDown=function()return false end
  load(unpack(args));update(10)
  if os.getenv('ROAG_GUI_WIDE')=='1'then love.window.setMode(1280,800,{resizable=false})end
  assert(entry.family==wanted);collect_quads();before=snapshot()
  print('Renderer:',love.graphics.getRendererInfo())
 end,debug.traceback);if not ok then fail(why)end
end
function love.update()
 if failed or not captured then return end
 local ok,why=xpcall(function()
  local im=captured;captured=nil
  assert(snapshot()==before and draws>0,'native environment was not drawn or changed simulation')
  PNG.write(output..'/'..wanted..'-'..phase..'.png',im)
  if phase==1 then
   baseline=im;local initial,peak,ms
   ms=0
   for i=1,20 do
    local start=love.timer.getTime();love.keypressed('f5');update(0);ms=ms+(love.timer.getTime()-start)*1000
    assert(snapshot()==before,'refresh changed generated room/RNG')
    collectgarbage('collect');collectgarbage('collect')
    local stats=love.graphics.getStats();if i==2 then initial=stats end
    if initial then assert(stats.images<=initial.images+2 and stats.texturememory<=initial.texturememory+1024,'refresh resource accumulation')end
    peak=stats
   end
   print('Refresh20 mean_ms='..(ms/20)..' images='..peak.images..' texture_bytes='..peak.texturememory)
   collect_quads();phase=2
  else
   for y=16,535 do for x=16,1007 do
    local a={baseline:getPixel(x,y)};local b={im:getPixel(x,y)}
    for c=1,4 do assert(math.floor(a[c]*255+.5)==math.floor(b[c]*255+.5),'F5 changed native world pixels')end
   end end
   print('PASS '..wanted..': generated room, actual native draws, pure frames, 20 bounded refreshes, identical world pixels')
   love.event.quit()
  end
 end,debug.traceback);if not ok then fail(why)end
end
function love.draw()
 if failed then return end
 local ok,why=xpcall(function()
  draws=0;draw()
  if not pending and not captured then pending=true
   love.graphics.captureScreenshot(function(im)captured=im;pending=false end)
  end
 end,debug.traceback);if not ok then fail(why)end
end
LUA
 luajit -b "$output/app/main.lua" /dev/null
 ROAG_GUI_AUTOSTART=1 ROAG_ENV_FAMILY="$family" ROAG_ENV_OUTPUT="$output" \
  XDG_DATA_HOME="$output/data" timeout -k 3s 45s love "$output/app" > "$output/love.log" 2>&1 || {
   cat "$output/love.log";printf 'Retained failed environment: %s\n' "$output";exit 1;
  }
 cat "$output/love.log"
 printf 'Retained environment GUI: %s\n' "$output"
done
