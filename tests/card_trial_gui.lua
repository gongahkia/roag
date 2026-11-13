-- Real renderer and public LÖVE callbacks. No live model edits to win fights.
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
function love.errorhandler(message)
 io.stderr:write(tostring(message)..'\n'..debug.traceback()..'\n')
 return function()return 1 end
end
local load,update,draw=love.load,love.update,love.draw
local phase,frames,delay,steps,case='title',0,4,0,1
local pending_capture,pending
local flags,receipts,results={}, {}, {}
local presets={'movement','burning','bare'}
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'GUI and model states differ in '..phase)end
local function state()return assert(love.roagCardTrialState(),'trial inactive')end
local function key(k)love.keypressed(k)end
local function capture(n)pending_capture=n;delay=3 end
local function ui()return require('src.card_trial_ui')end
local function cell(x,y)return 64+(x-1)*48+24,16+(y-1)*48+24 end
local function hover_card(slot,replace)love.mousemoved(700+(slot-1)*108,replace and 310 or 220)end
local function slot_of(s,id)for i,v in ipairs(s.card_trial.cards.hand)do if v==id then return i end end;error('missing card')end
local function perform(i,mouse)
 local before=state();local expected=G.snapshot(before);local ok,why,e=original_submit(expected,i);assert(ok,why)
 key('escape') -- clear stale hover/aim; may open menu if already empty
 if ui().menu then key('escape')end
 key('backspace');if ui().menu then key('escape')end
 if i.kind=='trial_basic'then
  if mouse then
   local p=G.player(before).position;local x,y=cell(p.x+i.dx,p.y+i.dy);love.mousemoved(x,y)
  elseif i.action=='basic_attack'then key('f');local d=({['0,-1']='w',['1,-1']='e',['1,0']='d',['1,1']='c',['0,1']='s',['-1,1']='z',['-1,0']='a',['-1,-1']='q'})[i.dx..','..i.dy];key(d)end
 else
  local slot=slot_of(before,i.card_id);hover_card(slot,i.kind=='replace_card')
  if i.kind=='technique' and not Cards.card(before.card_trial.cards,i.card_id).heal then
   if mouse then love.mousepressed(700+(slot-1)*108,220,1)else key(tostring(slot))end
   love.mousemoved(cell(i.x,i.y))
  end
 end
 same(before,state());pending={i=i,mouse=mouse,before=before,expected=expected,events=e}
 capture('before-'..case..'-'..steps)
end
local function finish()
 local a=pending;pending=nil;local i=a.i
 if i.kind=='trial_basic'then
  if a.mouse then local p=G.player(a.before).position;local x,y=cell(p.x+i.dx,p.y+i.dy);love.mousepressed(x,y,1)
  elseif i.action=='basic_attack'then key('return')
  else local d=({['0,-1']='w',['1,0']='d',['0,1']='s',['-1,0']='a'})[i.dx..','..i.dy];key(d)end
 elseif i.kind=='replace_card'then
  local slot=slot_of(a.before,i.card_id);love.mousepressed(700+(slot-1)*108,310,1);flags.replace=true
 else
  local def=Cards.card(a.before.card_trial.cards,i.card_id)
  if def.heal then love.mousepressed(700+(slot_of(a.before,i.card_id)-1)*108,220,1)
  elseif a.mouse then local x,y=cell(i.x,i.y);love.mousepressed(x,y,1)
  else key('return')end
  flags[def.name]=true
 end
 same(a.expected,state())
 if love.roagPlaybackPending()then
  key('b');key('w');key('return');love.keypressed('return',nil,true)
  love.mousepressed(700,310,1);love.gamepadpressed(nil,'a');same(a.expected,state());flags.input_lock=true
 end
 assert(Cards.validate(state().card_trial.cards))
 receipts[#receipts+1]={intent=i,clock=state().clock,hand=state().card_trial.cards.hand}
 steps=steps+1;delay=3
 if phase=='prefix'then capture('after-'..steps)end
end
function love.load(args)load(args);assert(love.graphics.getWidth()==1024 and love.graphics.getHeight()==640)end
local function advance()
 if pending then finish();return end
 if phase=='title'then
  assert(love.roagUIState().page=='home');capture('title-entry');phase='enter';return
 elseif phase=='enter'then
  love.mousepressed(512,414,1);key('right');key('2');key('return')
  same(T.new(G,202,'movement'),state());phase='prefix';steps=0;capture('opening');return
 elseif phase=='prefix'then
  local s=state();local p=G.player(s).position
  if steps==0 then perform({kind='trial_basic',action='move',dx=-1,dy=0},false);flags.opening_step=true;return
  elseif steps==1 then
   local before=state();hover_card(1);assert(#ui().targets>0);same(before,state());capture('dash-hover');phase='hover-sweep';return
  end
 elseif phase=='hover-sweep'then
  local before=state();hover_card(2);assert(#ui().targets==8);same(before,state());capture('sweep-hover');phase='hover-mend';return
 elseif phase=='hover-mend'then
  local before=state();hover_card(3);assert(ui().preview.reason=='Full health');love.mousepressed(916,220,1);same(before,state())
  capture('mend-full-health');phase='dash';return
 elseif phase=='dash'then
  local s=state();local p=G.player(s).position
  perform({kind='technique',card_id=1,x=p.x-2,y=p.y},true);phase='replace';return
 elseif phase=='replace'then
  local s=state();local before=G.snapshot(s);hover_card(3,true);same(before,state());capture('replace-hover')
  phase='replace-commit';return
 elseif phase=='replace-commit'then perform({kind='replace_card',card_id=state().card_trial.cards.hand[3]},true);phase='fight';return
 elseif phase=='fight'then
  local s=state()
  if s.card_trial.phase=='death'then error('policy died in '..presets[case])end
  if s.card_trial.phase=='reward'then capture(presets[case]..'-reward');phase='reward';return end
  if s.card_trial.phase=='victory'then
   results[#results+1]=presets[case]..' seed 202 VICTORY: '..steps..' actions, HP '..G.player(s).hp
   capture(presets[case]..'-victory');phase='next';return
  end
  if steps>140 then error('policy stalled')end
  if s.card_trial.knight_boots and not flags['Knight Hop']then
   for _,id in ipairs(s.card_trial.cards.hand)do if Cards.card(s.card_trial.cards,id).action=='knight_hop'then
    local at=G.card_targets(s,{kind='technique',card_id=id})[1]
    if at then perform({kind='technique',card_id=id,x=at.x,y=at.y},true);return end
   end end
  end
  perform(Policy.choose(s),steps%2==0);return
 elseif phase=='reward'then
  local before=state();key('escape');key('return');same(before,state());key('1')
  capture('knight-boots-choice');phase='claim';return
 elseif phase=='claim'then
  local before=state();key('return');local s=state();assert(s.card_trial.knight_boots and s.card_trial.fight==2)
  for _,pile in ipairs({'hand','draw','discard','retired'})do same(before.card_trial.cards[pile],s.card_trial.cards[pile])end
  same(before.card_trial.cards.rng,s.card_trial.cards.rng);flags.reward=true;capture('knight-boots-applied');phase='hop-inspect';return
 elseif phase=='hop-inspect'then
  local s=state();local id
  for _,v in ipairs(s.card_trial.cards.hand)do if Cards.card(s.card_trial.cards,v).action=='knight_hop'then id=v;break end end
  if id then
   local before=state();local slot=slot_of(s,id);hover_card(slot);key(tostring(slot))
   local at=G.card_targets(s,{kind='technique',card_id=id})[1];assert(at)
   love.mousemoved(cell(at.x,at.y));same(before,state());capture('knight-hop-preview');phase='hop-invalid';return
  end
  phase='fight';return
 elseif phase=='hop-invalid'then
  local before=state();love.mousemoved(cell(6,5));love.mousepressed(64+5*48+24,16+4*48+24,1)
  same(before,state());flags.invalid_landing=true;capture('invalid-landing');key('escape');phase='fight';return
 elseif phase=='next'then
  if case<3 then
   case=case+1;steps=0;key('p');key(tostring(({movement=2,burning=3,bare=1})[presets[case]]));key('return');phase='fight';return
  end
  key('return');same(T.new(G,202,'bare'),state());flags.replay=true
  phase='exhaust';steps=0;return
 elseif phase=='exhaust'then
  local s=state();local p=G.player(s)
  if s.card_trial.phase=='death'then capture('death');key('return');same(T.new(G,202,'bare'),state());flags.death=true
   key('escape');if not ui().menu then key('escape')end;key('t');phase='classic';return end
  local mend
  for _,id in ipairs(s.card_trial.cards.hand)do if Cards.card(s.card_trial.cards,id).heal then mend=id end end
  if s.card_trial.healing_charges==0 then assert(#s.card_trial.cards.retired==1);flags.mend_retired=true end
  if mend and p.hp<24 then perform({kind='technique',card_id=mend},true);return end
  perform({kind='replace_card',card_id=s.card_trial.cards.hand[1]},true);return
 elseif phase=='classic'then
  assert(love.roagUIState().page=='home');key('c');key('return');phase='classic-actions';return
 elseif phase=='classic-actions'then
  assert(classic and not classic.card_trial);key('d');phase='classic-wait';return
 elseif phase=='classic-wait'then key('space');phase='classic-strike';return
 elseif phase=='classic-strike'then key('f');key('d');key('return');phase='done';return
 elseif phase=='done'then
  assert(classic.clock==300);capture('classic-controls');flags.classic=true
  assert(flags.opening_step and flags.replace and flags.Dash and flags['Knight Hop'] and flags.reward and flags.invalid_landing and flags.mend_retired and flags.death and flags.replay and flags.input_lock)
  local f=assert(io.open(out..'/gui-results.txt','wb'));f:write(table.concat(results,'\n')..'\nPASS hover/target-click, Replace, upgrade/jump, invalid landing, healing retirement, replay and classic controls\n');f:close()
  f=assert(io.open(out..'/gui-actions.roag','wb'));f:write(Codec.encode(receipts));f:close();phase='quit';delay=4;return
 elseif phase=='quit'then print('PASS real LÖVE named-technique GUI');love.event.quit()end
end
function love.update(dt)
 update(dt);frames=frames+1
 if frames>24000 then error('GUI timed out')end
 if delay>0 then delay=delay-1;return end
 if love.roagPlaybackPending()then return end
 advance()
end
function love.draw()
 draw()
 if pending_capture then
  local name=pending_capture;pending_capture=nil
  love.graphics.captureScreenshot(function(image)
   local data=image:encode('png');local f=assert(io.open(out..'/'..name..'.png','wb'));f:write(data:getString());f:close()
  end)
 end
end
