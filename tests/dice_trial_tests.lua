package.path='./?.lua;'..package.path
local G=require('src.game')
local T=require('src.card_trial')
local D=require('src.dice_trial')
local R=require('src.rng')
local S=require('src.scheduler')
local Codec=require('src.persistence')
local U=require('src.dice_ui')
local passed,failed=0,0
local function test(name,fn)
 local ok,why=xpcall(fn,debug.traceback)
 if ok then passed=passed+1;print('PASS '..name)else failed=failed+1;print('FAIL '..name..'\n'..why)end
end
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'state differs')end
local function fresh(quiet)
 local s=T.new(G,202,'bare','dice')
 if quiet then for _,e in ipairs(s.entities)do if e.faction=='enemy'then S.remove_for_actor(s,e.id)end end end
 return s
end
-- Explicit fixtures only: playable dice never consult these helpers.
local function face(s,n,f)s.dice.results[n]={index=1,face=f}end
local function command(s,n,dx,dy)return {kind='dice_action',die=n,revision=s.dice.revision,dx=dx,dy=dy}end
local function run(s,n,dx,dy)local ok,why,e=G.submit(s,command(s,n,dx,dy));assert(ok,why);return e end
local function unchanged(s,i)local before=G.snapshot(s);local ok=G.submit(s,i);assert(not ok);same(before,s)end
local function event(events,kind,filter)
 for _,e in ipairs(events)do if e.kind==kind and (not filter or filter(e))then return e end end
end
local function compare(s,i)
 local before=G.snapshot(s);local forecast=D.forecast(s,i,G);assert(forecast.valid);same(s,before)
 assert(not forecast.state.dice.results and not forecast.state.dice.rng,'forecast leaks dice')
 local ok,why,events=G.submit(s,i);assert(ok,why)
 local after=G.snapshot(s);after.dice.results=nil;after.dice.rng=nil;same(after,forecast.state);same(events,forecast.events)
 return events
end
local function position(s,x,y)G.player(s).position.x=x;G.player(s).position.y=y end
local function enemies(s)return s.entities[2],s.entities[3],s.entities[4]end
local function impact(s,due,power)
 local e=s.entities[2];local p=G.player(s).position
 local work=S.enqueue(s,{kind='impact',due=due,actor_id=e.id,attack_id=900})
 e.pending_attack={schedule_id=work.id,sequence=work.seq,action_id=900,source_id=e.id,floor_id=s.active_floor_id,
  source_x=e.position.x,source_y=e.position.y,ability_id='core:ability/basic_attack',target_cells={{floor_id=s.active_floor_id,x=p.x,y=p.y}},
  damage=power,due=due,committed_time=s.clock}
end

test('opening has exactly three physical results, one roll and independent deterministic RNG',function()
 local s=fresh();same(s,fresh());assert(s.clock==0 and s.dice.rolls==1 and s.dice.revision==1 and #s.dice.results==3)
 local rng=R.new(R.derive(202,'dice-trial:faces/v1'))
 for n,r in ipairs(s.dice.results)do assert(r.index==R.next_int(rng,6));assert(r.face==s.dice.dice[n][r.index]);assert(#s.dice.dice[n]==6)end
 assert(s.rng.state==G.new(202).rng.state)
 assert(G.player(s).boons['core:boon/rhythm_of_steel'].uncommon==1)
 assert(G.player(s).boons['core:boon/detonation_bloom'].rare==1)
end)
test('all six physical indices enumerate exact frequencies; duplicate outcomes are retained',function()
 local s=fresh();local original=R.next_int
 local ok,why=pcall(function()
  for index=1,6 do
   R.next_int=function(_,maximum)assert(maximum==6);return index end
   local before=s.dice.rolls;D.roll(s);assert(s.dice.rolls==before+1)
   for n,r in ipairs(s.dice.results)do assert(r.index==index and r.face==D.catalogue[n][math.ceil(index/2)])end
  end
  local previous=s.dice.results;D.roll(s);same(previous,s.dice.results)
 end)
 R.next_int=original;assert(ok,why)
end)
test('each face pays its base cost scaled once; Quick Step never alters later actions',function()
 for n,cat in ipairs(D.catalogue)do for _,f in ipairs(cat)do
  local s=fresh(true);local p=G.player(s);p.speed=125;face(s,n,f)
  local before=G.snapshot(s);compare(s,command(s,n,1,0))
  assert(s.clock==G.effective_cost(D.faces[f].cost,125));assert(s.last_action.cost==s.clock)
  assert(p.speed==125 and p.damage==before.entities[1].damage and s.dice.rolls==2)
  face(s,1,'step');local time=s.clock;run(s,1,-1,0);assert(s.clock-time==G.effective_cost(100,125))
 end end
end)
test('invalid, stale, unsupported legacy commands and blocked movement are pure no-ops',function()
 local s=fresh(true);position(s,2,2)
 for _,kind in ipairs({'move','basic_attack','sweeping_slash','dash','wait','trial_action','boggle_route','cash_hand','use_utility','claim_chest','trial_reward'})do unchanged(s,{kind=kind,dx=1,dy=0})end
 for _,i in ipairs({{kind='dice_action',die=1,revision=0,dx=1,dy=0},command(s,9,1,0),command(s,1,-1,0),command(s,1,1,1),command(s,2,2,0)})do unchanged(s,i)end
 local stale=command(s,1,1,0);run(s,1,1,0);unchanged(s,stale)
 face(s,1,'guarded_step');position(s,2,2);unchanged(s,command(s,1,-1,0));assert(D.guard(s)==0)
end)
test('Guard caps, refreshes, expires exactly and protects during scheduled enemy resolution',function()
 local s=fresh(true);face(s,3,'guard');run(s,3);assert(D.guard(s)==2 and s.dice.guard_until==200)
 s.dice.guard=1;face(s,3,'guard');run(s,3);assert(D.guard(s)==2 and s.dice.guard_until==300 and s.clock==200)
 face(s,1,'step');run(s,1,1,0);assert(s.clock==300 and D.guard(s)==0)
 s=fresh(true);face(s,3,'guard');impact(s,50,3);local hp=G.player(s).hp
 local e=compare(s,command(s,3));local hit=event(e,'DamageTaken',function(x)return x.target_id==s.player_id end)
 assert(hit and hit.guard_absorbed==2 and hit.actual_loss==1 and G.player(s).hp==hp-1)
 s=fresh(true);face(s,3,'guard');G.player(s).speed=50;impact(s,200,3)
 e=run(s,3);hit=event(e,'DamageTaken',function(x)return x.target_id==s.player_id end)
 assert(hit and hit.actual_loss==3 and (hit.guard_absorbed or 0)==0)
end)
test('Guarded Step uses real movement and grants protection only after displacement',function()
 local s=fresh(true);face(s,1,'guarded_step');local e=compare(s,command(s,1,1,0))
 assert(G.player(s).position.x==7 and D.guard(s)==2 and s.dice.guard_until==200)
 assert(event(e,'Moved')and event(e,'TileEntered')and event(e,'BoardGuarded'))
end)
test('Sweep is shared three-cell arc at base 3; Heavy Strike is adjacent base 6 before crit/defence',function()
 for _,f in ipairs({'sweep','heavy_strike'})do
  local s=fresh(true);local a,b=enemies(s);position(s,4,7);a.position.x=4;a.position.y=6;b.position.x=3;b.position.y=6
  face(s,f=='sweep'and 2 or 3,f)
  local e=compare(s,command(s,f=='sweep'and 2 or 3,0,-1));local attack=event(e,'AttackPerformed',function(x)return x.source_id==s.player_id end)
  assert(attack.raw_damage==(f=='sweep'and 3 or 6));assert(#attack.target_cells==(f=='sweep'and 3 or 1))
  assert(a.hp==(f=='sweep'and 8 or 5));assert(b.hp==(f=='sweep'and 8 or 10))
 end
 local s=fresh(true);face(s,3,'heavy_strike');G.player(s).crit_chance_ppm=1000000;G.player(s).crit_damage_percent=200
 local a=enemies(s);position(s,4,6);local e=run(s,3,0,-1);local attack=event(e,'AttackPerformed');assert(attack.raw_damage==12 and attack.critical)
end)
test('Pierce passes through two creatures in one primary batch, stops at wall and blocked corner',function()
 local s=fresh(true);local a,b=enemies(s);position(s,4,7);a.position.x=4;a.position.y=6;b.position.x=4;b.position.y=5;face(s,2,'pierce')
 local e=compare(s,command(s,2,0,-1));local attacks=0
 for _,v in ipairs(e)do if v.kind=='AttackPerformed'and v.source_id==s.player_id then attacks=attacks+1;assert(#v.target_cells==2 and v.raw_damage==4)end end
 assert(attacks==1 and a.hp==7 and b.hp==7 and G.player(s).position.y==7)
 position(s,6,7);face(s,2,'pierce');assert(#D.plan(s,command(s,2,0,-1),G).cells==0)
 assert(#D.plan(s,command(s,2,1,-1),G).cells==0)
end)
test('Dash uses existing positive partial displacement and completely blocked Dash is invalid',function()
 local s=fresh(true);position(s,6,8);face(s,3,'dash');compare(s,command(s,3,0,-1));assert(G.player(s).position.y==7)
 face(s,3,'dash');unchanged(s,command(s,3,0,-1))
end)
test('empty aimed attacks are legal misses; repeated forecasts preserve every real state field',function()
 for _,f in ipairs({'strike','sweep','pierce','heavy_strike','guard','dash','guarded_step','quick_step'})do
  local s=fresh(true);local n=D.faces[f].kind=='move'and 1 or (f=='guard'or f=='dash'or f=='heavy_strike')and 3 or 2;face(s,n,f)
  local before=G.snapshot(s);for _=1,8 do D.forecast(s,command(s,n,1,0),G);same(s,before)end
  compare(s,command(s,n,1,0));assert(s.dice.rolls==2)
 end
end)
test('single-index face edit rejects identical/incompatible edits, preserves world, changes actual sampled frequency',function()
 local s=fresh(true);s.card_trial.phase='face_edit';local before=G.snapshot(s)
 unchanged(s,{kind='dice_edit',die=1,index=1,replacement='step'})
 unchanged(s,{kind='dice_edit',die=1,index=1,replacement='strike'})
 assert(G.submit(s,{kind='dice_edit',die=1,index=1,replacement='quick_step'}))
 assert(s.card_trial.fight==2 and s.clock==before.clock and G.player(s).hp==before.entities[1].hp and s.dice.rolls==2)
 for n,die in ipairs(s.dice.dice)do for j,f in ipairs(die)do assert(f==(n==1 and j==1 and 'quick_step'or before.dice.dice[n][j]))end end
 local counts={};for _,f in ipairs(s.dice.dice[1])do counts[f]=(counts[f]or 0)+1 end
 assert(counts.step==1 and counts.guarded_step==2 and counts.quick_step==3)
 unchanged(s,{kind='dice_edit',skip=true})
 local draw=R.next_int
 R.next_int=function(_,maximum)assert(maximum==6);return 1 end
 local ok,why=pcall(D.roll,s);R.next_int=draw;assert(ok,why)
 assert(s.dice.results[1].face=='quick_step','edited physical index was not sampled')
 same(fresh().dice.dice,before.dice.dice)
end)
test('intermission, death and victory never generate an extra roll; Skip opens fight two once',function()
 for _,phase in ipairs({'face_edit','victory','death'})do
  local s=fresh(true);s.entities={G.player(s)};S.remove_for_actor(s,2);S.remove_for_actor(s,3);S.remove_for_actor(s,4)
  if phase=='victory'then s.card_trial.fight=2 elseif phase=='death'then G.player(s).hp=0 end
  local rolls=s.dice.rolls;D.after_action(s,G,function()end);assert(s.card_trial.phase==phase and s.dice.rolls==rolls)
  if phase=='face_edit'then assert(G.submit(s,{kind='dice_edit',skip=true}));assert(s.dice.rolls==rolls+1);unchanged(s,{kind='dice_edit',skip=true})end
 end
end)

test('unaltered seed 202 fixture demonstrates positive Rhythm and Bloom damage and wins two fights reproducibly',function()
 local function journey()
  local s=fresh();local log={};local rhythm,bloom=0,0
  for n=1,80 do
   if s.game_over then break end
   if s.card_trial.phase=='face_edit'then
    local before=G.snapshot(s);assert(G.submit(s,{kind='dice_edit',die=1,index=1,replacement='quick_step'}))
    assert(s.clock==before.clock and G.player(s).hp==before.entities[1].hp)
    same(s.delayed_effects,before.delayed_effects);same(G.player(s).boon_memory,before.entities[1].boon_memory)
   end
   local i=require('tests.dice_trial_policy').choose(s);local events=compare(s,i)
   for _,e in ipairs(events)do if e.kind=='DamageTaken'and e.actual_loss>0 then
    for _,ancestor in ipairs(e.ancestry or {})do
     if ancestor:find('rhythm_of_steel',1,true)then rhythm=rhythm+1;assert(e.attempted_damage==2)end
     if ancestor:find('detonation_bloom',1,true)then bloom=bloom+1;assert(e.attempted_damage==3)end
    end
   end end
   log[#log+1]={intent=i,clock=s.clock,hp=G.player(s).hp}
  end
  assert(s.card_trial.phase=='victory'and G.player(s).hp==11 and #log==25)
  assert(rhythm>0 and bloom>0,'starting engine did not deal positive damage')
  assert(s.dice.rolls==25,'extra intermission or terminal roll')
  return s,log
 end
 local a,al=journey();local b,bl=journey();same(a,b);same(al,bl)
end)

test('ordinary armour and boon wards absorb before the separate dice guard',function()
 local s=fresh(true);local p=G.player(s);p.defense=1
 p.boon_memory=p.boon_memory or {};p.boon_memory['core:boon/betrayal_ward']={ward=2,expires_at=500}
 face(s,3,'guard');impact(s,50,4);local events=compare(s,command(s,3))
 local hit=event(events,'DamageAttempted',function(e)return e.target_id==s.player_id end)
 assert(hit and hit.ward_absorbed==2 and hit.guard_absorbed==1 and hit.actual_loss==0)
 assert(D.guard(s)==1 and p.boon_memory['core:boon/betrayal_ward'].ward==0)
end)
test('trial save, replace, terminal recovery and restore are rejected before any storage writes',function()
 local writes=0;local store={read=function()return nil end,write=function()writes=writes+1;return true end}
 local persistence=Codec.new(store);local s=fresh()
 for _,method in ipairs({'save','replace','pending_terminal'})do assert(not persistence[method](persistence,s))end
 assert(not pcall(G.restore,G.snapshot(s)));assert(writes==0)
end)

test('a real lethal scheduled impact agrees with preview, stops rolls and rejects further commands',function()
 local s=fresh(true);face(s,3,'guard');impact(s,50,100)
 local rolls=s.dice.rolls;compare(s,command(s,3))
 assert(s.game_over and s.card_trial.phase=='death'and G.player(s).hp==0)
 assert(s.dice.rolls==rolls);unchanged(s,command(s,1,1,0))
end)
local state,busy,submissions
local function attach()
 state=fresh(true);busy=false;submissions=0
 U.attach({state=function()return state end,busy=function()return busy end,target=function()end,
  submit=function(i)submissions=submissions+1;return G.submit(state,i)end,
  start=function(seed)state=T.new(G,seed,'bare','dice')end,exit=function()state=nil end,
  cell=function(x,y)return x,y end})
 U.seed=202;U.menu=false;U.inspect=false;U.await_update=false;U.clear();U.seen=state.dice.revision;U.shown=G.snapshot(state).dice.results
end
test('keyboard, neutral click and selected click movement apply each rolled Footwork face',function()
 for _,f in ipairs(D.catalogue[1])do for _,mode in ipairs({'keyboard','neutral','selected'})do
  attach();face(state,1,f)
  if mode=='keyboard'then U.keypressed('d')else if mode=='selected'then U.keypressed('1')end;U.mousepressed(7,8,1)end
  assert(submissions==1 and state.clock==D.faces[f].cost and G.player(state).position.x==7)
  assert(D.guard(state)==(f=='guarded_step'and 2 or 0))
 end end
end)
test('enemy clicks, bumps and F shortcut use current Weapon; Tactics Guard executes immediately',function()
 for _,mode in ipairs({'click','bump','shortcut'})do
  attach();position(state,4,6);face(state,2,'sweep')
  if mode=='click'then U.mousepressed(4,5,1)elseif mode=='bump'then U.keypressed('w')else U.keypressed('f');U.keypressed('w')end
  assert(submissions==1 and state.last_action.kind=='sweeping_slash'and state.clock==150)
 end
 attach();face(state,3,'guard');U.keypressed('3');assert(state.clock==100 and D.guard(state)==2)
end)
test('cancel, inspect and legacy keys are inert; playback, repeat, same-batch and double click input cannot commit twice',function()
 attach();local before=G.snapshot(state)
 for _,key in ipairs({'f','escape','i','escape','r','x','g','space','y','b'})do U.keypressed(key)end;same(before,state)
 U.mousemoved(7,8);U.mousemoved(8,8);same(before,state)
 busy=true;U.keypressed('w');U.mousepressed(7,8,1);same(before,state);busy=false
 U.keypressed('d',true);same(before,state)
 U.keypressed('d');assert(submissions==1);local after=G.snapshot(state)
 U.keypressed('a');U.mousepressed(6,8,1);same(after,state)
 U.update();U.mousepressed(6,8,1,2);same(after,state)
 U.keypressed('a',true);same(after,state)
 U.keypressed('a');assert(submissions==2)
end)
test('physical held key remains latched across playback and a new roll until release',function()
 attach();U.downkeys={};U.keypressed('d',false,true);local after=G.snapshot(state)
 U.update();U.keypressed('d',false,true);same(state,after)
 U.keyreleased('d');U.keypressed('d',false,true);assert(submissions==2)
end)
test('previous result presentation remains frozen during playback and updates only after unlock',function()
 attach();U.update();local shown=U.shown;local revision=state.dice.revision
 U.keypressed('d');assert(state.dice.revision==revision+1)
 busy=true;U.update();same(U.shown,shown);assert(U.shown~=state.dice.results)
 busy=false;U.update();assert(U.shown==state.dice.results and not U.selected and not U.aim)
end)
print(('Dice Trial: %d passed, %d failed'):format(passed,failed))
os.exit(failed==0 and 0 or 1)
