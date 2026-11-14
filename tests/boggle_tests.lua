package.path='./?.lua;'..package.path
local G=require('src.game')
local T=require('src.card_trial')
local B=require('src.boggle')
local C=B.content
local S=require('src.scheduler')
local Boons=require('src.boons')
local Codec=require('src.persistence')
local good,bad=0,0
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'canonical state mismatch')end
local function test(name,fn)local ok,why=xpcall(fn,debug.traceback);if ok then good=good+1;print('PASS '..name)else bad=bad+1;print('FAIL '..name..'\n'..why)end end
local function fresh(quiet)
 local s=T.new(G,202,'bare','boggle')
 if quiet then for _,e in ipairs(s.entities)do if e.faction=='enemy'then S.remove_for_actor(s,e.id)end end end
 return s
end
local function ids(s,cells)local out={};for _,i in ipairs(cells or {1,2,3,4})do out[#out+1]=s.boggle.board[i].id end;return out end
local function submit(s,kind,fields)
 local intent=fields or {};intent.kind=kind;intent.revision=B.stamp(s)
 local ok,why,e=G.submit(s,intent);assert(ok,why);return e
end
local function unchanged(s,intent)local before=G.snapshot(s);intent.revision=intent.revision or B.stamp(s);local ok=G.submit(s,intent);assert(not ok);same(before,s)end
local function line(s,types)
 -- Explicit test fixture: arrange existing finite instances; ordinary samples
 -- may repeat by design. Never used to generate a production board.
 for n,key in ipairs(types)do
  local at
  if C.types[key].finite then for j,t in ipairs(s.boggle.board)do if j>=n and t.type==key then at=j;break end end
  else for j,t in ipairs(s.boggle.board)do if j>=n and not C.types[t.type].finite then at=j;break end end end
  assert(at,'missing fixture tile '..key)
  s.boggle.board[n],s.boggle.board[at]=s.boggle.board[at],s.boggle.board[n]
  if not C.types[key].finite then s.boggle.board[n].type=key end
 end
 s.boggle.revision=s.boggle.revision+1;return ids(s)
end
local function resolve(s,route)
 local before=G.snapshot(s);local f=B.forecast(s,route,G);same(before,s);assert(f.valid,f.reason)
 local e=submit(s,'boggle_route',{ids=route});same(f.state,s);B.validate(s);return f,e
end
local function count(events,kind,boon)local n=0;for _,e in ipairs(events)do if e.kind==kind and (not boon or e.boon_id=='core:boon/'..boon)then n=n+1 end end;return n end
local function enemy(s,n)return s.entities[n or 2]end
local function pending(s,due,cells,power,action)
 local e=enemy(s);local work=S.enqueue(s,{kind='impact',due=due,actor_id=e.id,attack_id=action or 900})
 e.pending_attack={schedule_id=work.id,sequence=work.seq,action_id=action or 900,source_id=e.id,
  floor_id=s.active_floor_id,source_x=e.position.x,source_y=e.position.y,ability_id='core:ability/basic_attack',
  target_cells=cells,damage=power or 3,due=due,committed_time=s.clock}
 return e
end
local function cell(s,x,y)return {floor_id=s.active_floor_id,x=x,y=y}end
local function echo(s,due,x,y,power,faction)
 local work=S.enqueue(s,{kind='effect',due=due,actor_id=0,attack_id=901});work.effect_id=work.id
 s.delayed_effects[work.id]={activation={id=901,owner_id=faction=='player' and s.player_id or 999,
  source_faction=faction or 'enemy',power=power or 1,source_position=cell(s,6,8),chain_id=901,root_action_id=901,ancestry={},
  event={target_cells={cell(s,x,y)}}},effect={kind='echo_impact',damage_type='physical'}}
end

test('initial pool has 31 weight units, five stable finite instances, no inherited boons, independent board RNG',function()
 local s=fresh();local b=s.boggle;assert(#b.board==36 and B.validate(s));local ordinary,finite=0,0;local counts={}
 for _,t in ipairs(b.board)do counts[t.type]=(counts[t.type]or 0)+1;if C.types[t.type].finite then finite=finite+1 else ordinary=ordinary+1 end end
 assert(ordinary==31 and finite==5 and counts.mend==2 and counts.guard==2)
 for i=1,4 do assert(counts['move'..i]==3 and counts['strike'..i]==3)end
 assert(not next(G.player(s).boons));same(s,fresh());assert(s.clock==0)
end)
test('route geometry accepts any start, exactly four distinct cardinal neighbours and rejects stale IDs',function()
 local s=fresh(true)
 for i=1,36 do assert(B.geometry(s,ids(s,{i}),false))end
 for _,cells in ipairs({{1,2,3},{1,2,3,4,5},{1,2,1,7},{1,8,9,10},{6,7,8,9}})do unchanged(s,{kind='boggle_route',ids=ids(s,cells)})end
 unchanged(s,{kind='boggle_route',ids={999,2,3,4}})
 local r=line(s,{'guard','guard','guard','guard'});local stamp=B.stamp(s);resolve(s,r)
 unchanged(s,{kind='boggle_route',ids=r,revision=stamp})
 unchanged(s,{kind='boggle_route',ids=ids(s),revision='stale'})
end)
test('all classic and earlier trial movement/attack/utility paths are authoritative no-ops in Boggle',function()
 local s=fresh()
 assert(not G.card_preview(s,{kind='trial_action',action='move',dx=1,dy=0}).valid)
 for _,k in ipairs({'move','basic_attack','dash','sweeping_slash','wait','trial_action','card_action','technique','cash_hand','use_utility','trial_reward','boggle_command'})do
  unchanged(s,{kind=k,dx=1,dy=0,action='move',offer_index=1})
 end
end)
test('an unexpected runtime invalidation stops before that command and reports a defect without spending the suffix',function()
 local s=fresh(true);local r=line(s,{'guard','mend','sweep','guard'});local proxy={}
 for k,v in pairs(G)do proxy[k]=v end
 proxy.submit=function(state,intent)
  if state==s and state.clock>=100 then return false,'injected runtime invalidation',{}end
  return G.submit(state,intent)
 end
 local ok,reason=B.submit(s,{kind='boggle_route',ids=r,revision=B.stamp(s)},proxy)
 assert(ok and reason:find('Tile 2')and s.clock==100 and s.boggle.defect:find('injected'))
 assert(s.boggle.board[2].id==r[2]and s.boggle.board[3].id==r[3]and not s.boggle.retired[r[2]]and not s.boggle.retired[r[3]])
 assert(B.validate(s))
end)
test('a world change invalidates old forecasts, including public spawn/stat grants and a changed HP value',function()
 local s=fresh(true);local r=line(s,{'guard','guard','guard','guard'});local stamp=B.stamp(s)
 G.spawn_enemy(s,'core:enemy/thornspitter',9,9,1000);unchanged(s,{kind='boggle_route',ids=r,revision=stamp})
 stamp=B.stamp(s);G.set_boon_stacks(s,s.player_id,'core:boon/rhythm_of_steel','common',1)
 unchanged(s,{kind='boggle_route',ids=r,revision=stamp})
 stamp=B.stamp(s);G.player(s).hp=23;unchanged(s,{kind='boggle_route',ids=r,revision=stamp})
end)
test('blocked Move never bumps, and a known later invalid tile rejects the whole four-command route',function()
 local s=fresh(true);G.player(s).position.x,G.player(s).position.y=5,7;enemy(s).position.x,enemy(s).position.y=5,6
 local r=line(s,{'guard','focus','move1','guard'});local f=B.forecast(s,r,G);assert(not f.valid and f.reason:find('Tile 3'))
 unchanged(s,{kind='boggle_route',ids=r});assert(s.clock==0 and s.boggle.focus==0)
 local cache={};B.forecast(s,{r[1],r[2]},G,cache);f=B.forecast(s,r,G,cache);assert(f.reason:find('Tile 3'))
end)
test('empty Strike and finite Sweep are legal misses with normal scaled timings; Move and Dash use real geometry',function()
 local s=fresh(true);local r=line(s,{'strike2','sweep','guard','guard'});s.boggle.sweep_direction=2
 local f,e=resolve(s,r);assert(s.clock==450 and count(e,'AttackPerformed')==2 and s.boggle.retired[r[2]])
 s=fresh(true);G.player(s).speed=200;r=line(s,{'move4','dash4','strike1','guard'});f=resolve(s,r)
 assert(s.clock==215 and G.player(s).position.x==3);assert(f.steps[2].plan.distance==2)
 s=fresh(true);G.player(s).position.x,G.player(s).position.y=5,6;r=line(s,{'dash2','guard','guard','guard'});unchanged(s,{kind='boggle_route',ids=r})
end)
test('Wound bypasses armour, Guard and wards; uses a self-cost event rather than hostile hit/Wait',function()
 local s=fresh(true);local p=G.player(s);p.defense=50;s.boggle.guard=2
 p.boon_memory={['core:boon/betrayal_ward']={ward=5,expires_at=1000}}
 local r=line(s,{'wound','guard','guard','guard'});local f,e=resolve(s,r)
 assert(p.hp==22 and Boons.ward_total(s,p)==5 and s.boggle.guard==2 and s.clock==400)
 assert(count(e,'HealthCostPaid')==1 and count(e,'DamageTaken')==0 and count(e,'Waited')==0)
end)
test('Wound can kill at tile one; only the executed prefix retires/consumes and no refill occurs',function()
 local s=fresh(true);G.player(s).hp=2;local r=line(s,{'wound','mend','guard','guard'});local rng=s.boggle.rng.state
 local f,e=resolve(s,r);assert(s.game_over and s.card_trial.phase=='death' and f.executed==1)
 assert(s.boggle.board[1]==false and s.boggle.board[2].id==r[2] and not s.boggle.retired[r[2]] and s.boggle.rng.state==rng)
 assert(s.clock==0 and count(e,'EntityKilled')==0)
end)
test('self-cost feedback uses existing player hurt/death audio cues without fabricating a hostile hit',function()
 local s=fresh();local audio=require('src.studio_audio_runtime')
 same(audio.cues_for_event({kind='HealthCostPaid',target_id=s.player_id},s),{'core:cue/player/damage'})
 same(audio.cues_for_event({kind='Died',self_cost=true,target_id=s.player_id},s),{'core:cue/player/death'})
end)
test('Guard refresh caps at two after ordinary defence/wards and survives route boundaries',function()
 local s=fresh(true);G.player(s).boon_memory={['core:boon/betrayal_ward']={ward=1,expires_at=1000}}
 pending(s,50,{cell(s,6,8)},4)
 local r=line(s,{'guard','guard','guard','guard'});local f=resolve(s,r)
 assert(f.steps[1].state.boggle.guard==0 and G.player(f.steps[1].state).hp==23)
 assert(G.player(s).hp==23 and s.boggle.guard==2 and Boons.ward_total(s,G.player(s))==0)
 r=line(s,{'strike1','strike1','strike1','strike1'});resolve(s,r);assert(s.boggle.guard==2)
end)
test('Mend at full HP forecasts zero, costs time and retires; the two instances are the whole supply',function()
 local s=fresh(true);local r=line(s,{'mend','mend','guard','guard'});local f,e=resolve(s,r)
 assert(count(e,'Healed')==2 and e[2].kind=='Healed' and e[2].actual_gain==0 and s.clock==400)
 assert(s.boggle.retired[r[1]] and s.boggle.retired[r[2]] and not s.boggle.healing_charges)
 for _,t in ipairs(s.boggle.board)do assert(t.type~='mend')end
end)
test('Focus/Dull stack, persist through nonattacks and consume once on a miss; damage cannot heal',function()
 local s=fresh(true);local r=line(s,{'focus','focus','dull','guard'});resolve(s,r);assert(s.boggle.focus==2 and s.boggle.dull==1)
 r=line(s,{'strike2','guard','guard','guard'});local f,e=resolve(s,r);assert(s.boggle.focus==0 and s.boggle.dull==0)
 for _,v in ipairs(e)do if v.kind=='AttackPerformed' and v.source_id==s.player_id then assert(v.raw_damage==5)end end
 s.boggle.dull=100;enemy(s).position.x,enemy(s).position.y=7,8;local hp=enemy(s).hp
 r=line(s,{'strike2','guard','guard','guard'});resolve(s,r);assert(enemy(s).hp==hp)
 s=fresh(true);G.player(s).crit_chance_ppm=1000000;G.player(s).crit_damage_percent=200
 enemy(s).position.x,enemy(s).position.y=7,8
 r=line(s,{'focus','strike2','guard','guard'});resolve(s,r)
 assert(enemy(s).hp==1,'Focus must apply before critical multiplication and flat defence')
end)
test('captured primary modifier covers every Sweep target before defence; secondary boon packets receive none',function()
 local s=fresh(true);local p=G.player(s);p.position.x,p.position.y=5,7
 enemy(s,2).position.x,enemy(s,2).position.y=4,6;enemy(s,3).position.x,enemy(s,3).position.y=5,6
 s.boggle.sweep_direction=1
 local r=line(s,{'focus','sweep','guard','guard'});local f,e=resolve(s,r)
 assert(enemy(s,2).hp==7 and enemy(s,3).hp==7 and s.boggle.focus==0)
 s=fresh(true);s.boggle.focus=2;echo(s,50,4,5,2,'player')
 r=line(s,{'guard','guard','guard','guard'});resolve(s,r);assert(s.boggle.focus==2 and enemy(s).hp==9)
end)
test('Fury stacks during an existing windup, applies at resolution to the whole batch and consumes on a miss',function()
 local s=fresh(true);local e=enemy(s);e.position.x,e.position.y=4,7
 enemy(s,3).position.x,enemy(s,3).position.y=5,7
 pending(s,150,{cell(s,6,8),cell(s,5,7)},3)
 local r=line(s,{'fury','fury','focus','focus'});resolve(s,r)
 assert(G.player(s).hp==19 and enemy(s,3).hp==6 and not s.boggle.fury[e.id])
 assert(B.enemy_attack(s,e,900)==2 and B.enemy_attack(s,e,900)==2)
 s=fresh(true);e=pending(s,50,{cell(s,2,2)},3);r=line(s,{'fury','guard','guard','guard'});resolve(s,r)
 assert(not s.boggle.fury[e.id] and s.boggle.fury_batches[e.id..':900']==1 and G.player(s).hp==24)
end)
test('cancelled windups and secondary damage do not spend Fury; new enemies do not inherit it',function()
 local s=fresh(true);local e=pending(s,50,{cell(s,6,8)},3);s.boggle.fury[e.id]=2
 assert(G.stun(s,e.id,1000,s.player_id,800));local r=line(s,{'guard','guard','guard','guard'});resolve(s,r)
 assert(s.boggle.fury[e.id]==2)
 local added=G.spawn_enemy(s,'core:enemy/thornspitter',9,9,999);assert(not s.boggle.fury[added.id])
 echo(s,450,6,8,1);r=line(s,{'focus','focus','focus','focus'});resolve(s,r);assert(s.boggle.fury[e.id]==2)
end)
test('two pickup slots equip common boons before later route events; duplicate/full pickup is invalid',function()
 local s=fresh(true);local p=G.player(s);p.position.x,p.position.y=5,7
 enemy(s).position.x,enemy(s).position.y=5,8;enemy(s).hp=3
 enemy(s,3).position.x,enemy(s,3).position.y=9,4
 local r=line(s,{'rhythm','move4','move2','strike3'});local f,e=resolve(s,r)
 assert(p.boons['core:boon/rhythm_of_steel'].common==1 and s.boggle.retired[r[1]])
 assert(count(e,'BoonActivated','rhythm_of_steel')>=1 and count(e,'EntityKilled')==1)
 r=line(s,{'bloom','guard','guard','guard'});resolve(s,r);assert(p.boons['core:boon/detonation_bloom'].common==1)
 s=fresh(true);Boons.set_stacks(G.player(s),'core:boon/rhythm_of_steel','common',1)
 r=line(s,{'rhythm','guard','guard','guard'});unchanged(s,{kind='boggle_route',ids=r})
 s=fresh(true);Boons.set_stacks(G.player(s),'core:boon/pulse_step','common',1);Boons.set_stacks(G.player(s),'core:boon/emberbrand','common',1)
 r=line(s,{'bloom','guard','guard','guard'});unchanged(s,{kind='boggle_route',ids=r})
end)
test('acquiring Rhythm changes a later same-route kill threshold without changing enemy stats',function()
 local s=fresh(true);local p=G.player(s);p.position.x,p.position.y=5,7
 for _,e in ipairs(s.entities)do if e.faction=='enemy'then e.position.x,e.position.y=9,e.id+1 end end
 local target=G.spawn_enemy(s,'core:enemy/thornspitter',4,6,1000)
 local r=line(s,{'rhythm','focus','move4','strike1'});local f,e=resolve(s,r)
 assert(not G.entity(s,target.id)and count(e,'EntityKilled')==1 and count(e,'BoonActivated','rhythm_of_steel')==2)
 local primary,secondary
 for _,v in ipairs(e)do if v.kind=='DamageTaken' and v.target_id==target.id then
  if v.direct_attack then primary=v.actual_loss else secondary=v.actual_loss end
 end end
 assert(primary==5 and secondary==1 and G.player(s).hp==24)
end)
test('seed 202 opening offers an ordinary safer route and a harmful bridge with greater enemy loss',function()
 local s=fresh();local safer=B.forecast(s,ids(s,{26,20,14,13}),G);local harmful=B.forecast(s,ids(s,{10,9,15,14}),G)
 assert(safer.valid and harmful.valid and G.player(safer.state).hp==24 and G.player(harmful.state).hp==19)
 local function loss(f)local total=0;for _,e in ipairs(s.entities)do if e.faction=='enemy'then local a=G.entity(f.state,e.id);total=total+e.hp-(a and a.hp or 0)end end;return total end
 assert(loss(safer)==4 and loss(harmful)==6);assert(harmful.state.boggle.spent[s.boggle.board[10].id])
end)
test('intermission carries protection, timed boon memory, delayed work, penalties, finite supply and Shuffle use',function()
 local s=fresh(true);s.boggle.guard=2;s.boggle.focus=3;s.boggle.dull=1;s.boggle.shuffle_left=0
 Boons.set_stacks(G.player(s),'core:boon/rhythm_of_steel','common',1)
 G.player(s).boon_memory={['core:boon/rhythm_of_steel']={expires_at=900,primed=true}}
 echo(s,800,9,3,1,'player');local scheduled=G.snapshot(s.schedule);local delayed=G.snapshot(s.delayed_effects);local finite=G.snapshot(s.boggle.finite)
 s.card_trial.phase='intermission';submit(s,'boggle_intermission');submit(s,'boggle_edit',{edit='skip'})
 assert(s.boggle.guard==2 and s.boggle.focus==3 and s.boggle.dull==1 and s.boggle.shuffle_left==0 and s.clock==0)
 same(delayed,s.delayed_effects);same(finite,s.boggle.finite)
 assert(G.player(s).boon_memory['core:boon/rhythm_of_steel'].expires_at==900)
 for _,work in ipairs(scheduled)do local found=false;for _,next in ipairs(s.schedule)do if next.id==work.id then same(next,work);found=true end end;assert(found)end
end)
test('early victory consumes only executed prefix, waits for explicit intermission and does not refill early',function()
 local s=fresh(true);for n=#s.entities,3,-1 do table.remove(s.entities,n)end
 local p=G.player(s);p.position.x=2;enemy(s).position.x,enemy(s).position.y=3,8;enemy(s).hp=3
 local r=line(s,{'strike2','move4','mend','guard'});local rng=s.boggle.rng.state
 local f=resolve(s,r);assert(f.executed==1 and s.card_trial.phase=='intermission' and s.boggle.rng.state==rng)
 assert(s.boggle.board[1]==false and s.boggle.board[2].id==r[2]);unchanged(s,{kind='boggle_shuffle'})
 local hp,time=G.player(s).hp,s.clock;submit(s,'boggle_intermission');assert(s.card_trial.phase=='edit' and s.boggle.board[1] and B.validate(s))
 unchanged(s,{kind='boggle_intermission'});submit(s,'boggle_edit',{edit='skip'})
 assert(s.card_trial.fight==2 and s.card_trial.phase=='combat' and G.player(s).hp==hp and s.clock==time)
 unchanged(s,{kind='boggle_edit',edit='skip'})
end)
test('Shuffle spends one actual Wait, permutes identities only and retains finite supply, penalties and protection',function()
 local s=fresh(true);s.boggle.focus=2;s.boggle.dull=1;s.boggle.guard=2
 local before=G.snapshot(s.boggle);local e=submit(s,'boggle_shuffle');assert(s.clock==100 and count(e,'Waited')==1 and s.boggle.shuffle_left==0)
 assert(s.boggle.focus==2 and s.boggle.dull==1 and s.boggle.guard==2)
 local seen={};for _,v in ipairs(s.boggle.board)do seen[v.id]=v.type end
 for _,v in ipairs(before.board)do assert(seen[v.id]==v.type)end
 same(before.pool,s.boggle.pool);same(before.finite,s.boggle.finite);B.validate(s);unchanged(s,{kind='boggle_shuffle'})
end)
test('Emergency Reset pays two Waits, preserves finite positions and RNG, and guarantees a four-Guard route',function()
 local s=fresh(true);local before=G.snapshot(s.boggle);s.boggle.dull=2
 local e=submit(s,'boggle_reset');assert(count(e,'Waited')==2 and s.clock==200 and s.boggle.dull==2)
 assert(s.boggle.rng.state==before.rng.state and s.boggle.shuffle_left==1)
 for i,t in ipairs(before.board)do if C.types[t.type].finite then same(t,s.boggle.board[i])end end
 local found
 for row=0,5 do local all=true;for x=1,4 do if s.boggle.board[row*6+x].type~='guard'then all=false end end;if all then found=row;break end end
 assert(found);resolve(s,ids(s,{found*6+1,found*6+2,found*6+3,found*6+4}));B.validate(s)
end)
test('utilities stop after death/clear during either Wait, without a permutation, rebuild, refund or extra Wait',function()
 for _,kind in ipairs({'boggle_shuffle','boggle_reset'})do for _,due in ipairs({50,150})do
  if kind=='boggle_reset' or due==50 then
   local s=fresh(true);G.player(s).hp=1;echo(s,due,6,8,1)
   local before=G.snapshot(s.boggle);local e=submit(s,kind);assert(s.game_over and s.clock==due)
   assert(count(e,'Waited')==(due==50 and 1 or 2));same(before.board,s.boggle.board);assert(before.rng.state==s.boggle.rng.state)
   assert(s.boggle.shuffle_left==(kind=='boggle_shuffle' and 0 or 1))
  end
 end end
 for _,due in ipairs({50,150})do
  local s=fresh(true);for n=#s.entities,3,-1 do table.remove(s.entities,n)end
  local e=enemy(s);e.hp=1;e.position.x,e.position.y=7,8;echo(s,due,7,8,2,'player')
  local before=G.snapshot(s.boggle);local events=submit(s,'boggle_reset')
  assert(s.card_trial.phase=='intermission' and count(events,'Waited')==(due==50 and 1 or 2));same(before.board,s.boggle.board)
 end
end)
test('Rotate transfers one pool weight and changes only the visible instance; Swap preserves effects and weights',function()
 local s=fresh(true);s.card_trial.phase='intermission';submit(s,'boggle_intermission');local b=s.boggle
 local at;for i,t in ipairs(b.board)do local d=C.types[t.type];if d.dir and not d.finite then at=i;break end end
 local old=G.snapshot(b);local tile=b.board[at];local d=C.types[tile.type];local target=d.kind..(d.dir%4+1)
 submit(s,'boggle_edit',{edit='rotate',first=tile.id})
 assert(tile.type==target and b.pool[d.key]==old.pool[d.key]-1 and b.pool[target]==old.pool[target]+1)
 for i,t in ipairs(old.board)do if i~=at then same(t,b.board[i])end end;B.validate(s)
 local original=G.snapshot(s);original.boggle.pool=old.pool;local changed=false
 for _=1,80 do
  for _,state in ipairs({s,original})do
   state.boggle.spent[state.boggle.board[1].id]=true;state.boggle.board[1]=false;B.refill(state)
  end
  if s.boggle.board[1].type~=original.boggle.board[1].type then changed=true end
 end
 assert(changed,'Rotate weights never affected later refills')
 s=fresh(true);s.card_trial.phase='intermission';submit(s,'boggle_intermission');b=s.boggle
 local a,c=b.board[4],b.board[8];local pool=G.snapshot(b.pool);submit(s,'boggle_edit',{edit='swap',first=a.id,second=c.id})
 assert(b.board[4].id==c.id and b.board[8].id==a.id);same(pool,b.pool);B.validate(s)
end)
test('forecast and live canonical states match uncached/cached routes, harmful bridges, enemy responses and RNG',function()
 local cache={};local s=fresh();local original=G.snapshot(s)
 for _,cells in ipairs({{1,2,3,4},{4,3,9,15},{9,10,16,15},{31,32,33,34},{36,35,29,28},{13,14,15,16}})do
  local r=ids(s,cells)
  for n=1,4 do local part={};for i=1,n do part[i]=r[i]end;B.forecast(s,part,G,cache);same(original,s)end
  local f=B.forecast(s,r,G,cache);same(f,B.forecast(s,r,G,cache));same(original,s)
  if f.valid then local clone=G.snapshot(s);submit(clone,'boggle_route',{ids=r});same(f.state,clone)end
 end
 local r=ids(s,{1,2,3,4});resolve(s,r);local f=B.forecast(s,ids(s),G,cache);assert(f.stamp==B.stamp(s) and f.stamp~=B.stamp(original))
end)
test('finite retirement survives refills, Shuffle, Reset and intermission; replay restores original five',function()
 local s=fresh(true);local r=line(s,{'mend','mend','sweep','rhythm'});resolve(s,r);local retired=G.snapshot(s.boggle.retired)
 submit(s,'boggle_shuffle');submit(s,'boggle_reset');s.card_trial.phase='intermission';submit(s,'boggle_intermission');submit(s,'boggle_edit',{edit='skip'})
 same(retired,s.boggle.retired);B.validate(s);for _,t in ipairs(s.boggle.board)do assert(not retired[t.id])end
 local replay=fresh();assert(not next(replay.boggle.retired)and B.validate(replay));same(replay,fresh())
end)
test('ordinary refill changes only executed cells, allocates fresh nonfinite IDs and ignores combat RNG',function()
 local s=fresh(true);local r=line(s,{'guard','guard','guard','guard'});local before=G.snapshot(s.boggle.board)
 resolve(s,r)
 for i=1,36 do
  if i<=4 then assert(s.boggle.board[i].id>36 and not C.types[s.boggle.board[i].type].finite)
  else same(s.boggle.board[i],before[i])end
 end
 assert(s.boggle.next_id==41 and s.clock==400)
 local other=G.snapshot(s);other.rng.state=42;local world=s.rng.state
 for _,state in ipairs({s,other})do state.boggle.spent[state.boggle.board[1].id]=true;state.boggle.board[1]=false;B.refill(state)end
 same(s.boggle.board,other.boggle.board);same(s.boggle.rng,other.boggle.rng)
 assert(s.rng.state==world and other.rng.state==42)
end)
test('trial storage refuses saves/terminal records/restoration; Classic, Poker and Fixed still accept their own commands',function()
 local writes=0;local store={read=function()end,write=function()writes=writes+1;return true end};local p=Codec.new(store);local s=fresh()
 assert(not p:save(s) and not p:replace(s));s.game_over=true;assert(not p:pending_terminal(s) and not pcall(G.restore,s));assert(writes==0)
 local classic=G.new(99);assert(G.submit(classic,{kind='move',dx=1,dy=0}));assert(G.submit(classic,{kind='wait'}));assert(classic.clock==200)
 for _,mode in ipairs({'poker','fixed'})do local a=T.new(G,202,'movement',mode);if mode=='poker'then assert(G.submit(a,{kind='cash_hand'}))end
  assert(G.submit(a,{kind='trial_action',action='move',dx=-1,dy=0}));assert(a.clock==100)
 end
end)
test('ordered pointer trajectory includes harmful crossings, rejects diagonal gaps, truncates and never executes on release',function()
 local s=fresh(true);local U=require('src.boggle_ui');local submits=0
 U.attach({state=function()return s end,busy=function()return false end,setup=function()end,start=function()end,target=function()end,
  submit=function(i)submits=submits+1;return G.submit(s,i)end})
 U.open(202);U.start();local before=G.snapshot(s)
 local x,y=U.center(9);local ex,ey=U.center(12)
 U.mousepressed(x,y,1);U.mousemoved(ex,ey);U.mousereleased(ex,ey,1);assert(#U.path==4 and U.path[2]==s.boggle.board[10].id)
 assert(C.types[s.boggle.board[10].type].harmful);same(before,s);assert(submits==0)
 U.drag=true;U.last_point={ex,ey};local bx,by=U.center(10);U.mousemoved(bx,by);assert(#U.path==2);same(before,s)
 U.clear();x,y=U.center(1);ex,ey=U.center(8);U.mousepressed(x,y,1);U.mousemoved(ex,ey);assert(#U.path==1 and not U.drag)
 U.clear();for _,k in ipairs({'w','f','x','r','h','g','b','u'})do U.keypressed(k)end;same(before,s)
end)
test('keyboard route construction, undo/cancellation and one Enter commitment use the same authority',function()
 local s=fresh(true);line(s,{'guard','guard','guard','guard'});local U=require('src.boggle_ui')
 U.attach({state=function()return s end,busy=function()return false end,setup=function()end,start=function()end,target=function()end,submit=function(i)return G.submit(s,i)end})
 U.open(202);U.start();local before=G.snapshot(s)
 for _,k in ipairs({'space','d','space','d','space','d','space'})do U.keypressed(k)end
 U.refresh_forecast();assert(#U.path==4 and U.forecast.valid and U.forecast.complete);same(before,s)
 U.keypressed('backspace');U.refresh_forecast();assert(#U.path==3);same(before,s)
 U.keypressed('space');U.refresh_forecast();local expected=G.snapshot(U.forecast.state)
 U.keypressed('return');same(expected,s);assert(s.clock==400)
 local after=G.snapshot(s);U.keypressed('return');same(after,s)
 U.keypressed('space');U.keypressed('escape');assert(#U.path==0);same(after,s)
end)
print(('Boggle: %d passed, %d failed'):format(good,bad));if bad>0 then os.exit(1)end
