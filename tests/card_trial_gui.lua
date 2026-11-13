-- Production renderer/public LÖVE callbacks. No live-state edits in these journeys.
local G=require('src.game')
local T=require('src.card_trial')
local Cards=require('src.cards')
local Codec=require('src.persistence')
local Policy=require('tests.card_trial_policy')
local original_submit=G.submit
local out=assert(os.getenv('ROAG_CARD_GUI_OUTPUT'))
local ordinary_restore=G.restore;local classic
function G.restore(...)local s=ordinary_restore(...);classic=s;return s end
assert(love.filesystem.load('roag_main.lua'))()
function love.errorhandler(m)io.stderr:write(tostring(m)..'\n'..debug.traceback()..'\n');return function()return 1 end end
local load,update,draw=love.load,love.update,love.draw
local phase,delay,frames,step,case='title',4,0,0,1
local pending,capture
local flags,results,receipts={},{},{}
local cases={{'poker','movement'},{'poker','burning'},{'poker','bare'},{'fixed','movement'},{'fixed','burning'},{'fixed','bare'}}
local function U()return require('src.card_trial_ui')end
local function state()return assert(love.roagCardTrialState())end
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'GUI/model differ in '..phase)end
local function key(k)love.keypressed(k)end
local function cell(x,y)return 64+(x-1)*48+24,16+(y-1)*48+24 end
local function shot(name)capture=name;delay=3 end
local function physical(a,dx,dy)return {kind='trial_action',action=a,dx=dx,dy=dy}end
local function clear()key('backspace')end
local dirkeys={['0,-1']='w',['1,-1']='e',['1,0']='d',['1,1']='c',['0,1']='s',['-1,1']='z',['-1,0']='a',['-1,-1']='q'}
local function selection(s,ids)
 for _,id in ipairs(ids or {})do for n,v in ipairs(s.card_trial.cards.hand)do if id==v then key(tostring(n))end end end
end
local function prepare_action(i,mouse,name)
 local before=state();local expected=G.snapshot(before);local ok,why,e=original_submit(expected,i);assert(ok,why)
 clear()
 if i.kind=='cash_hand' or i.kind=='exchange_cards'then selection(before,i.ids)
 elseif i.kind=='retain_cards'then
  -- This helper is used only to mark an initially empty set.
 elseif i.kind=='trial_action'then
  if i.action=='move' or i.action=='strike'then
   if mouse then local p=G.player(before).position;love.mousemoved(cell(p.x+i.dx,p.y+i.dy))
   elseif i.action=='strike'then key('f');key(dirkeys[i.dx..','..i.dy])end
  elseif i.action=='dash' or i.action=='sweep'then
   key(i.action=='dash' and 'x' or 'r');love.mousemoved(cell(i.x,i.y))
  else love.mousemoved(i.action=='mend' and 748 or 910,446)end
 end
 same(before,state())
 pending={i=i,mouse=mouse,before=before,expected=expected,events=e}
 if name then shot(name)else delay=1 end
end
local function finish()
 local a=pending;pending=nil;local i=a.i
 if i.kind=='cash_hand'then key('return');flags.cash=true
 elseif i.kind=='exchange_cards'then key('y');flags.exchange=true
 elseif i.kind=='retain_cards'then
  for _,id in ipairs(i.ids)do for n,v in ipairs(a.before.card_trial.cards.hand)do if id==v then
   local x=688+((n-1)%4)*82;local y=136+math.floor((n-1)/4)*74+50;love.mousepressed(x,y,1)
  end end end;flags.retain=true
 else
  if i.action=='move' or i.action=='strike'then
   if a.mouse then local p=G.player(a.before).position;local x,y=cell(p.x+i.dx,p.y+i.dy);love.mousepressed(x,y,1)
   elseif i.action=='strike'then key('return')else key(dirkeys[i.dx..','..i.dy])end
  elseif i.action=='dash' or i.action=='sweep'then
   if a.mouse then local x,y=cell(i.x,i.y);love.mousepressed(x,y,1)else key('return')end
  elseif i.action=='mend'then love.mousepressed(748,446,1)
  else key('g')end
  flags[i.action]=true
 end
 same(a.expected,state())
 if love.roagPlaybackPending()then
  key('w');key('b');key('return');love.keypressed('return',nil,true);love.mousepressed(810,295,1);love.gamepadpressed(nil,'a')
  same(a.expected,state());flags.input_lock=true
 end
 if i.kind=='cash_hand'then key('return');same(a.expected,state())end
 if i.kind=='exchange_cards'then key('y');same(a.expected,state())end
 if state().card_trial.cards then assert(Cards.validate(state().card_trial.cards))end
 receipts[#receipts+1]={phase=phase,case=case,intent=i,clock=state().clock,stamina=state().card_trial.stamina}
 step=step+1;delay=2
end
local function start_case()
 local c=cases[case];U().open(202,c[1]);key(tostring(({bare=1,movement=2,burning=3})[c[2]]));key('return')
 same(T.new(G,202,c[2],c[1]),state());step=0;phase='fight';shot(c[1]..'-'..c[2]..'-opening')
end
function love.load(args)load(args);assert(love.graphics.getWidth()==1024 and love.graphics.getHeight()==640)end
local function advance()
 if pending then finish();return end
 if phase=='title'then assert(love.roagUIState().page=='home');shot('title-entry');phase='enter'
 elseif phase=='enter'then
  love.mousepressed(512,414,1);key('right');key('2');key('return');same(T.new(G,202,'movement'),state())
  local before=state();key('a');key('b');key('u');same(before,state());flags.unfunded=true
  shot('eight-card-opening');phase='budget';step=0
 elseif phase=='budget'then
  local s=state()
  if step==0 then prepare_action({kind='cash_hand'},false,'cash-preview')
  elseif step==1 then prepare_action(physical('move',-1,0),false,'funded-step-preview')
  elseif step==2 then prepare_action(physical('move',0,-1),true)
  elseif step==3 then prepare_action(physical('strike',0,-1),true,'strike-preview')
  elseif step==4 then prepare_action(physical('move',0,1),false)
  elseif step==5 then
   assert(s.card_trial.stamina==0 and #s.card_trial.cards.hand==4 and s.clock==400)
   flags.multi_action_budget=true;shot('zero-stamina');prepare_action({kind='retain_cards',ids={s.card_trial.cards.hand[1],s.card_trial.cards.hand[2]}},true,'retention-preview')
  elseif step==6 then prepare_action(physical('recover'),false,'zero-recover-preview')
  elseif step==7 then
   assert(s.card_trial.resource_phase=='prepare' and #s.card_trial.cards.hand==8 and not next(s.card_trial.retained))
   prepare_action({kind='exchange_cards',ids={s.card_trial.cards.hand[3],s.card_trial.cards.hand[5],s.card_trial.cards.hand[8]}},false,'exchange-preview')
  elseif step==8 then prepare_action({kind='cash_hand',ids={s.card_trial.cards.hand[1]}},true,'weaker-subset-preview');flags.override=true
  elseif step==9 then assert(s.card_trial.stamina==3);prepare_action(physical('recover'),false,'early-recover-preview');flags.early_recover=true
  else phase='healing';step=0 end
 elseif phase=='healing'then
  local s=state();local t=s.card_trial
  if t.phase=='death'then shot('death');phase='technical-replay';return end
  if t.healing_charges==0 and not flags.healing_exhausted then
   local before=state();key('h');same(before,state());flags.healing_exhausted=true;shot('healing-exhausted');return
  end
  if t.resource_phase=='prepare' and t.healing_charges>0 and G.player(s).hp<24 then prepare_action({kind='cash_hand'},false)
  elseif t.resource_phase=='spend' and t.stamina>=2 and t.healing_charges>0 and G.player(s).hp<24 then prepare_action(physical('mend'),true,'mend-preview')
  else prepare_action(physical('recover'),false)end
 elseif phase=='technical-replay'then
  key('return');same(T.new(G,202,'movement'),state());flags.death=true;flags.replay=true;start_case()
 elseif phase=='fight'then
  local s=state();local t=s.card_trial
  if t.phase=='death'then error('policy died in '..t.mode..' '..t.preset)end
  if t.phase=='reward'then shot(t.mode..'-'..t.preset..'-boon-reward');phase='reward';return end
  if t.phase=='deck_reward'then shot('deck-offers');phase='deck';return end
  if t.phase=='victory'then
   results[#results+1]=t.mode..' '..t.preset..' seed 202 VICTORY: '..step..' commands, HP '..G.player(s).hp..', time '..s.clock
   shot(t.mode..'-'..t.preset..'-victory');phase='next';return
  end
  if step>160 then error('policy stalled')end
  local proposed=Policy.choose(s);prepare_action(proposed,step%2==0)
 elseif phase=='reward'then
  local before=state();key('escape');key('return');same(before,state());key('2');key('return');flags.boon_reward=true;phase='fight'
 elseif phase=='deck'then
  local before=state();key('1');same(before,state());shot('deck-edit-details');phase='deck-claim'
 elseif phase=='deck-claim'then
  local before=state();key('return');local s=state();assert(s.card_trial.fight==2 and #s.card_trial.cards.instances==55 and #s.card_trial.cards.hand==8)
  assert(s.clock==before.clock and G.player(s).hp==G.player(before).hp and s.card_trial.healing_charges==before.card_trial.healing_charges)
  local after=state();local ok=original_submit(G.snapshot(after),{kind='trial_deck_reward',choice=1});assert(not ok)
  flags.deck_edit=true;shot('fight-two-fresh-hand');phase='fight'
 elseif phase=='next'then
  if case<#cases then case=case+1;start_case()else
   key('return');same(T.new(G,202,'bare','fixed'),state());key('escape');key('t');phase='classic';delay=3
  end
 elseif phase=='classic'then assert(love.roagUIState().page=='home');key('c');key('return');phase='classic-step'
 elseif phase=='classic-step'then assert(classic and not classic.card_trial);key('d');phase='classic-wait'
 elseif phase=='classic-wait'then key('space');phase='classic-strike'
 elseif phase=='classic-strike'then key('f');key('d');key('return');phase='done'
 elseif phase=='done'then
  assert(classic.clock==300);flags.classic=true;shot('classic-controls')
  for _,k in ipairs({'cash','exchange','retain','multi_action_budget','early_recover','healing_exhausted','death','replay','input_lock','deck_edit','boon_reward','classic','dash','sweep','mend'})do assert(flags[k],'missing '..k)end
  local f=assert(io.open(out..'/gui-results.txt','wb'));f:write(table.concat(results,'\n')..'\nPASS actual callbacks: exchange/retain/cash/recover, both modes, rewards, exhaustion/death/replay and classic\n');f:close()
  f=assert(io.open(out..'/gui-actions.roag','wb'));f:write(Codec.encode(receipts));f:close();phase='quit';delay=4
 elseif phase=='quit'then print('PASS poker/fixed stamina GUI journeys');love.event.quit()end
end
function love.update(dt)
 update(dt);frames=frames+1;if frames>40000 then error('GUI timed out')end
 if delay>0 then delay=delay-1;return end
 if love.roagPlaybackPending()then return end
 advance()
end
function love.draw()
 draw()
 if capture then local name=capture;capture=nil;love.graphics.captureScreenshot(function(image)
  local data=image:encode('png');local f=assert(io.open(out..'/'..name..'.png','wb'));f:write(data:getString());f:close()
 end)end
end
