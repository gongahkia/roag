-- Disposable native-input observer. Never submits a live combat command.
local G=require('src.game')
local D=require('src.dice_trial')
local T=require('src.card_trial')
local Codec=require('src.persistence')
local Policy=require('tests.dice_trial_policy')
local out=assert(os.getenv('ROAG_DICE_GUI_OUTPUT'))
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local previous,expected,expected_events,advice,stamp,capture
local receipts={}
local serial=0
local inspected=false
local title_seen=false
local edit_view,preview_view
local function write(name,data)local f=assert(io.open(out..'/'..name,'w'));f:write(data);f:close()end
local function same(a,b)if Codec.encode(a)~=Codec.encode(b)then write('actual.dat',Codec.encode(a));write('expected.dat',Codec.encode(b));error('native/model state mismatch')end end
function love.errorhandler(m)write('error.txt',m..'\n'..debug.traceback());return function()return 1 end end
function love.load(args)
 load(args);assert(love.graphics.getWidth()==1024 and love.graphics.getHeight()==640)
end
function love.update(dt)
 update(dt)
 local s=love.roagCardTrialState();if not s or not s.dice then
  if serial>0 and not title_seen then write('title.json','{\"title\":true}');capture='title';title_seen=true end
  return
 end
 local next_stamp=s.clock..':'..s.dice.revision..':'..s.card_trial.phase
 if next_stamp~=stamp then
  if expected and previous and s.clock~=0 then same(s,expected)end
  stamp=next_stamp;serial=serial+1
  receipts[#receipts+1]={time=s.clock,hp=G.player(s).hp,phase=s.card_trial.phase,fight=s.card_trial.fight,rolls=s.dice.rolls,results=s.dice.results}
  write('receipts.dat',Codec.encode(receipts))
  previous=s;expected=nil;advice=nil
  if s.card_trial.phase=='combat'then
   advice=Policy.choose(s);expected=G.snapshot(s);local ok,why,events=G.submit(expected,advice);assert(ok,why);expected_events=events
  elseif s.card_trial.phase=='face_edit'then
   expected=G.snapshot(s);assert(G.submit(expected,{kind='dice_edit',skip=false,die=1,index=1,replacement='quick_step'}))
   capture='intermission'
  elseif s.card_trial.phase=='victory'then capture='victory'
  end
  if serial==1 then capture='opening'end
  local details={};for _,e in ipairs(expected_events or {})do
   if e.kind=='DamageTaken'and not e.direct_attack and e.actual_loss>0 then
    for _,ancestor in ipairs(e.ancestry or {})do
     if ancestor:find('rhythm_of_steel',1,true)or ancestor:find('detonation_bloom',1,true)then details[#details+1]=ancestor..' damage='..e.actual_loss end
    end
   end
  end
  if #details>0 then write('boons-'..serial..'.txt',table.concat(details,'\n'))end
 end
 local U=require('src.dice_ui')
 if s.card_trial.phase=='face_edit'then
  local view=table.concat({U.edit_die,U.edit_index,U.edit_type},':')
  if view~=edit_view then capture='intermission';edit_view=view end
 end
 if U.inspect and not inspected then capture='inspection'end
 inspected=U.inspect
 if U.forecast and U.forecast.valid then
  local view=Codec.encode(U.aim)
  if view~=preview_view then capture='preview';preview_view=view end
 end
 local dx,dy=advice and advice.dx or 0,advice and advice.dy or 0
 local fields={serial=serial,busy=love.roagPlaybackPending()and true or false,phase=s.card_trial.phase,
  time=s.clock,hp=G.player(s).hp,x=G.player(s).position.x,y=G.player(s).position.y,face=advice and s.dice.results[advice.die].face or '',die=advice and advice.die or 0,dx=dx,dy=dy,inspect=U.inspect or false}
 local parts={};for k,v in pairs(fields)do parts[#parts+1]='"'..k..'":'..(type(v)=='string'and '"'..v..'"'or tostring(v))end
 write('status.json','{'..table.concat(parts,',')..'}')
end
function love.draw()
 draw()
 if capture and not love.roagPlaybackPending()then
  local name=capture;capture=nil
  love.graphics.captureScreenshot(function(data)local f=assert(io.open(out..'/'..name..'.png','wb'));f:write(data:encode('png'):getString());f:close();if name=='title'then love.event.quit()end end)
 end
end
