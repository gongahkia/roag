-- Actual LÖVE renderer / public callbacks. No live model edits in this journey.
local G=require('src.game')
local B=require('src.boggle')
local T=require('src.card_trial')
local Codec=require('src.persistence')
local P=require('tests.boggle_policy')
local out=assert(os.getenv('ROAG_BOGGLE_GUI_OUTPUT'))
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,delay,step='opening',5,0
local pending,capture,route,expected
local receipts={}
local function state()return assert(love.roagCardTrialState())end
local function U()return require('src.boggle_ui')end
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'callback/model mismatch in '..phase)end
local function key(k)love.keypressed(k)end
local function shot(name)capture=name;delay=3 end
local function mouse(cell)local x,y=U().center(cell);love.mousemoved(x,y);return x,y end
local function press(cell)local x,y=mouse(cell);love.mousepressed(x,y,1)end
local function release(cell)local x,y=mouse(cell);love.mousereleased(x,y,1)end
local function predict(intent)
 local s=state();local clone=G.snapshot(s);intent.revision=B.stamp(s);local ok,why=G.submit(clone,intent);assert(ok,why);return clone
end
local function summarize(metrics)
 local lines={'boundary: UI callback entry -> return from actual love.graphics.present containing matching ghost; includes present wait, not physical display latency'}
 for _,name in ipairs({'path','forecast','ghost'})do
  local values={};local cached=0;local stamps={};local full=0
  for _,v in ipairs(metrics[name])do if v.tiles>0 then values[#values+1]=v.ms;if v.cached then cached=cached+1 end;if v.stamp then stamps[v.stamp]=true end;if v.tiles==4 then full=full+1 end end end
  table.sort(values);local n=#values;local states=0;for _ in pairs(stamps)do states=states+1 end
  assert(n>20,'insufficient timing samples')
  lines[#lines+1]=('%s n=%d p50=%.3f p95=%.3f worst=%.3f ms; cached=%d full_routes=%d live_states=%d'):format(name,n,values[math.ceil(n*.5)],values[math.ceil(n*.95)],values[n],cached,full,states)
 end
 return table.concat(lines,'\n')
end
function love.errorhandler(m)io.stderr:write(tostring(m)..'\n'..debug.traceback()..'\n');return function()return 1 end end
function love.load(args)
 load(args);assert(love.graphics.getWidth()==1024 and love.graphics.getHeight()==640)
 local name,version,vendor,device=love.graphics.getRendererInfo()
 local f=assert(io.open(out..'/environment.txt','wb'));f:write(table.concat({love.system.getOS(),name,version,vendor,device},'\n')..'\n');f:close()
end
local function advance()
 if phase=='opening'then key('return');same(T.new(G,202,'bare','boggle'),state());shot('opening');phase='fast'
 elseif phase=='fast'then
  local before=state();press(9);mouse(12);release(12);assert(#U().path==4 and U().path[2]==state().boggle.board[10].id);same(before,state())
  shot('fast-harmful-crossing');phase='backtrack'
 elseif phase=='backtrack'then
  local before=state();U().drag=true;local x,y=U().center(12);U().last_point={x,y};mouse(10);release(10);assert(#U().path==2);same(before,state());shot('backtracked');phase='cached-extend'
 elseif phase=='cached-extend'then U().drag=true;local x,y=U().center(10);U().last_point={x,y};mouse(12);release(12);assert(#U().path==4);delay=2;phase='cached-backtrack'
 elseif phase=='cached-backtrack'then U().drag=true;local x,y=U().center(12);U().last_point={x,y};mouse(10);release(10);assert(#U().path==2);delay=2;phase='diagonal'
 elseif phase=='diagonal'then
  press(1);mouse(8);release(8);assert(#U().path==1 and not U().drag);key('c');phase='technical-plan';step=1
 elseif phase=='technical-plan'then
  local cells={10,9,15,21}
  if step==1 then press(cells[1])else mouse(cells[step])end
  step=step+1
  if step==5 then release(cells[4]);phase='technical-preview';shot('costly-route-preview')end
 elseif phase=='technical-preview'then
  local s=state();assert(U().forecast and U().forecast.valid and U().forecast.complete)
  expected=predict({kind='boggle_route',ids=B.copy(U().path)})
  key('return');same(expected,state());key('return');key('w');love.mousepressed(740,492,1);same(expected,state())
  shot('costly-route-result');phase='shuffle'
 elseif phase=='shuffle'then
  expected=predict({kind='boggle_shuffle'});key('y');same(expected,state());shot('paid-shuffle');phase='reset'
 elseif phase=='reset'then
  expected=predict({kind='boggle_reset'});key('escape');key('e');same(expected,state());shot('emergency-reset');phase='death'
 elseif phase=='death'then
  if state().card_trial.phase=='death'then shot('death');phase='replay'
  else key('escape');key('e');delay=2 end
 elseif phase=='replay'then key('return');same(T.new(G,202,'bare','boggle'),state());phase='journey';step=0
 elseif phase=='journey'then
  local s=state();assert(s.card_trial.phase~='death','policy died')
  if s.card_trial.phase=='victory'then shot('victory');phase='old-modes';step=0;return end
  if s.card_trial.phase=='edit'then phase='edit';shot('intermission');return end
  route=P.choose(s);assert(route,'no legal test policy route');step=step+1;assert(step<=40,'policy stalled')
  pending={};for _,id in ipairs(route)do pending[#pending+1]=assert(B.cell(s,id))end
  U().clear();press(pending[1]);phase='extend';U().test_step=2
 elseif phase=='extend'then
  local n=U().test_step;mouse(pending[n]);U().test_step=n+1
  if n==4 then release(pending[4]);phase='inspect-route';shot(state().card_trial.fight==1 and 'boon-route-preview' or 'fight-two-preview')end
 elseif phase=='inspect-route'then
  assert(U().forecast and U().forecast.valid and U().forecast.complete)
  local before=state();key('2');same(before,state());expected=predict({kind='boggle_route',ids=B.copy(U().path)})
  love.mousepressed(742,492,1);same(expected,state())
  receipts[#receipts+1]={route=route,clock=state().clock,hp=G.player(state()).hp,fight=state().card_trial.fight}
  key('w');key('return');love.keypressed('return',nil,true);same(expected,state());phase='journey';delay=2
 elseif phase=='edit'then
  local s=state();local id
  for _,t in ipairs(s.boggle.board)do if t.type=='move1'then id=t.id;break end end;assert(id)
  key('r');local at=B.cell(s,id);press(at);release(at);local before=state();same(before,state());shot('rotate-preview')
  expected=predict({kind='boggle_edit',edit='rotate',first=id});phase='edit-commit'
 elseif phase=='edit-commit'then key('return');same(expected,state());shot('fight-two-opening');phase='journey'
 elseif phase=='old-modes'then
  key('t');assert(love.roagUIState().page=='home');key('t');key('2');key('return');assert(state().card_trial.mode=='poker')
  key('return');key('a');phase='fixed'
 elseif phase=='fixed'then
  key('escape');key('t');require('src.card_trial_ui').open(202,'fixed');key('1');key('return');key('a');assert(state().card_trial.mode=='fixed' and state().clock==100)
  phase='classic'
 elseif phase=='classic'then
  key('escape');key('t');key('c');key('return');delay=3;phase='done'
 elseif phase=='done'then
  assert(love.roagUIState().page=='game');key('d');phase='write'
 elseif phase=='write'then
  local f=assert(io.open(out..'/gui-results.txt','wb'));f:write('PASS callbacks: routes, fast harmful crossing, backtracking, release inspection, effects, Shuffle/Reset, death/replay, both fights, Rotate and all existing modes\n');f:write('Victory seed 202: '..#receipts..' routes; HP '..G.player(expected).hp..'\n');f:close()
  f=assert(io.open(out..'/latency.txt','wb'));f:write(summarize(U().metrics)..'\n');f:close()
  f=assert(io.open(out..'/metrics.roag','wb'));f:write(Codec.encode(U().metrics));f:close()
  f=assert(io.open(out..'/actions.roag','wb'));f:write(Codec.encode(receipts));f:close();print('PASS Boggle rendered callbacks');love.event.quit()
 end
end
function love.update(dt)
 update(dt)
 if delay>0 then delay=delay-1;return end
 if love.roagPlaybackPending()then return end
 advance()
end
function love.draw()
 draw()
 -- Capture after the matching ghost's timing sample, so PNG readback/encoding
 -- is not misreported as interaction cost.
 if capture and not U().pending then local name=capture;capture=nil;love.graphics.captureScreenshot(function(img)local data=img:encode('png');local f=assert(io.open(out..'/'..name..'.png','wb'));f:write(data:getString());f:close()end)end
end
