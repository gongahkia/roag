-- Controlled renderer/landing fixture, separate from the unchanged trial arena.
-- Tests public hover/click callbacks and uses the real authoritative resolver.
local G=require('src.game')
local T=require('src.card_trial')
local Cards=require('src.cards')
local S=require('src.scheduler')
local B=require('src.boons')
local Codec=require('src.persistence')
local out=assert(os.getenv('ROAG_CARD_GUI_OUTPUT'))
local original_new=T.new
local fixture,last_events,mode,commits=nil,{},1,0
local function tile(s,x,y,symbol)
 local m=s.final_map;m.rows[y]=m.rows[y]:sub(1,x-1)..symbol..m.rows[y]:sub(x+1)
end
function T.new(...)
 local s=original_new(...);fixture=s
 for _,e in ipairs(s.entities)do if e.controller=='ai'then S.remove_for_actor(s,e.id)end end
 local p=G.player(s);p.position.x,p.position.y=5,7
 local points={{4,7},{5,6},{9,3}}
 if mode==2 then
  p.position.x,p.position.y=5,6;points={{6,4},{8,5},{2,2}}
  Cards.knight_boots(s.card_trial.cards);s.card_trial.knight_boots=true
  B.set_stacks(p,'core:boon/pulse_step','rare',1)
  B.set_stacks(p,'core:boon/phantom_lunge','rare',1)
  tile(s,7,5,'b')
  local R=require('src.rewards');local emit=function()end
  R.spawn_currency(s,2,s.active_floor_id,6,6,emit,p.id)
  R.spawn_currency(s,3,s.active_floor_id,7,5,emit,p.id)
 end
 for i,at in ipairs(points)do s.entities[i+1].position.x,s.entities[i+1].position.y=at[1],at[2]end
 require('src.visibility').recompute(s);return s
end
local submit=G.submit
function G.submit(s,i)
 local ok,why,e,p=submit(s,i)
 if s==fixture and ok then last_events=e;commits=commits+1 end
 return ok,why,e,p
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
function love.errorhandler(message)io.stderr:write(tostring(message)..'\n'..debug.traceback()..'\n');return function()return 1 end end
local stage,delay=0,4
local capture
local function key(k)love.keypressed(k)end
local function cell(x,y)return 64+(x-1)*48+24,16+(y-1)*48+24 end
local function snap()return love.roagCardTrialState()end
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'preview mutated live state')end
local function shot(name)capture=name;delay=4 end
function love.load(args)load(args);key('1');key('return')end
function love.update(dt)
 update(dt)
 if delay>0 then delay=delay-1;return end
 if love.roagPlaybackPending()then return end
 local U=require('src.card_trial_ui')
 if stage==0 then
  local before=snap();love.mousemoved(820,220);key('2');love.mousemoved(cell(4,6));same(before,snap())
  assert(U.preview.valid and #U.preview.cells==3 and #U.forecast==2 and U.forecast[1].loss==2)
  shot('sweep-primary-preview');stage=1
 elseif stage==1 then
  local x,y=cell(4,6);love.mousepressed(x,y,1);assert(commits==1 and snap().clock==150)
  shot('sweep-after');stage=2
 elseif stage==2 then
  mode=2;U.open(202);key('2');key('return');assert(snap().card_trial.knight_boots)
  local before=snap();love.mousemoved(710,220);key('1');love.mousemoved(cell(7,5));same(before,snap())
  assert(U.preview.jump and #U.preview.cells==1 and U.preview.landing.x==7)
  shot('pillar-hop-preview');stage=3
 elseif stage==3 then
  local before=snap();local x,y=cell(6,4);love.mousemoved(x,y);love.mousepressed(x,y,1);same(before,snap())
  assert(U.preview.reason=='Landing must be a free walkable tile')
  shot('pillar-hop-invalid');stage=4
 elseif stage==4 then
  local x,y=cell(7,5);love.mousemoved(x,y);love.mousepressed(x,y,1)
  local s=snap();assert(commits==2 and s.clock==130 and G.player(s).hp==23 and s.currency==3)
  assert(G.player(s).position.x==7 and G.player(s).position.y==5)
  local entered,moved,pulse,rhythm,path=0,0,0,0,0
  for _,e in ipairs(last_events)do
   if e.kind=='TileEntered' and e.source_id==s.player_id then entered=entered+1;assert(e.x==7 and e.y==5)end
   if e.kind=='Moved' and e.source_id==s.player_id then moved=moved+1;assert(e.movement_kind=='jump' and not e.path)end
   if e.kind=='BoonActivated'then
    if e.boon_id=='core:boon/pulse_step'then pulse=pulse+1 end
    if e.boon_id=='core:boon/rhythm_of_steel'then rhythm=rhythm+1 end
    if e.boon_id=='core:boon/phantom_lunge'then path=path+1 end
   end
  end
  assert(entered==1 and moved==1 and pulse==1 and rhythm==1 and path==0)
  assert(not s.drops[1].collected and s.drops[2].collected and Cards.validate(s.card_trial.cards))
  stage=5;delay=4
 elseif stage==5 then shot('pillar-hop-after');stage=6
 elseif stage==6 then
  local f=assert(io.open(out..'/preview-results.txt','wb'));f:write('PASS public callbacks: exact Sweep primary labels; pillar jump, invalid landing, landing bramble/pickup/Pulse/Rhythm, no intermediate pickup or path boon\n');f:close()
  print('PASS controlled LÖVE preview and landing fixture');love.event.quit()
 end
end
function love.draw()
 draw()
 if capture then local name=capture;capture=nil
  love.graphics.captureScreenshot(function(image)local data=image:encode('png');local f=assert(io.open(out..'/'..name..'.png','wb'));f:write(data:getString());f:close()end)
 end
end
