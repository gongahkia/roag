-- Authoritative trial boundaries. Fixtures are in memory; never player storage.
package.path='./?.lua;./?/init.lua;'..package.path
local G=require('src.game')
local T=require('src.card_trial')
local Cards=require('src.cards')
local C=require('src.card_trial_content')
local RNG=require('src.rng')
local S=require('src.scheduler')
local B=require('src.boons')
local Codec=require('src.persistence')
local passed,failed=0,0
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'states differ')end
local function test(name,fn)local ok,why=pcall(fn);if ok then passed=passed+1;print('PASS '..name)else failed=failed+1;print('FAIL '..name..': '..tostring(why))end end
local function fresh(preset)return T.new(G,202,preset or 'bare')end
local function quiet(s)for _,e in ipairs(s.entities)do if e.controller=='ai'then S.remove_for_actor(s,e.id)end end end
local function action(s,kind,id,x,y)return {kind=kind or 'technique',card_id=id or s.card_trial.cards.hand[1],x=x,y=y}end
local function basic(dx,dy,attack)return {kind='trial_basic',action=attack and 'basic_attack' or 'move',dx=dx,dy=dy}end
local function submit(s,i)local ok,why,e=G.submit(s,i);assert(ok,why);return e end
local function unchanged(s,i)local before=G.snapshot(s);local ok=G.submit(s,i);assert(not ok);same(before,s)end
local function count(events,k,id)local n=0;for _,e in ipairs(events)do if e.kind==k and (not id or e.boon_id=='core:boon/'..id)then n=n+1 end end;return n end
local function hand(s,ids)
 local d=s.card_trial.cards;d.hand=ids;d.draw={};d.discard={};d.retired={};local seen={}
 for _,id in ipairs(ids)do seen[id]=true end
 for id=1,#d.instances do if not seen[id]then d.draw[#d.draw+1]=id end end
 assert(Cards.validate(d))
end
local function set_tile(s,x,y,v)local m=s.final_map;m.rows[y]=m.rows[y]:sub(1,x-1)..v..m.rows[y]:sub(x+1)end
local function near(s,coords)
 quiet(s);local n=0
 for _,e in ipairs(s.entities)do if e.controller=='ai'then n=n+1;e.position.x,e.position.y=unpack(coords[n]or {9,n+1})end end
end
local function clear_fight(s)
 quiet(s)
 for _=1,12 do
  if s.card_trial.phase~='combat'then return end
  local target
  for _,e in ipairs(s.entities)do if e.controller=='ai' and e.hp>0 then target=e;break end end
  assert(target);target.hp=1;target.position.x,target.position.y=G.player(s).position.x+1,G.player(s).position.y
  submit(s,basic(1,0,true))
 end
 error('fight did not resolve')
end
local function resolve_first(s)clear_fight(s);assert(s.card_trial.phase=='reward')end

test('nine stable instances; exactly Dash Sweep Mend open; only remainder is shuffled',function()
 local a,b=fresh(),fresh();local d=a.card_trial.cards
 same(d,b.card_trial.cards);assert(Cards.validate(d));assert(#d.instances==9 and #d.draw==6)
 local n={};for _,c in ipairs(d.instances)do n[c.kind]=(n[c.kind]or 0)+1 end
 assert(n.dash==4 and n.sweep==4 and n.mend==1)
 for i,k in ipairs(C.opening)do assert(d.instances[d.hand[i]].kind==k)end
end)
test('ordinary Step, bump and aimed Strike cost time without cycling cards or card RNG',function()
 local s=fresh();near(s,{{5,7},{9,3},{9,4}});local d=G.snapshot(s.card_trial.cards)
 submit(s,basic(-1,0));same(d,s.card_trial.cards);assert(s.clock==100)
 local e=submit(s,basic(0,-1));assert(s.last_action.kind=='basic_attack' and s.last_action.bump)
 same(d,s.card_trial.cards);assert(s.clock==200 and count(e,'AttackPerformed')==1)
 submit(s,basic(0,-1,true));same(d,s.card_trial.cards);assert(s.clock==300)
end)
test('technique consumes only its instance, refills its slot and retains other identities',function()
 local s=fresh();quiet(s);local before=G.snapshot(s.card_trial.cards)
 local e=submit(s,action(s,'technique',1,8,8));local d=s.card_trial.cards
 assert(d.discard[1]==1 and #d.draw==5 and d.hand[1]==before.draw[6])
 assert(d.hand[2]==5 and d.hand[3]==9 and Cards.validate(d))
 assert(s.clock==130 and count(e,'CardTechniqueCommitted')==1 and s.last_action.kind=='dash')
end)
test('retention and conservation hold through repeated exhaustion and reshuffles',function()
 local d=Cards.new(101);local keep=d.hand[3]
 for _=1,120 do Cards.consume(d,d.hand[1]);Cards.consume(d,d.hand[2]);assert(d.hand[3]==keep and Cards.validate(d))end
 for _,pile in ipairs({d.draw,d.discard,d.retired})do for _,id in ipairs(pile)do assert(id~=keep)end end
end)
test('card randomness is deterministic and independent of combat randomness',function()
 local a,b=fresh(),fresh();quiet(a);quiet(b)
 for _=1,60 do
  for _=1,7 do RNG.next_raw(b.rng)end
  submit(a,action(a,'replace_card'));submit(b,action(b,'replace_card'))
  same(a.card_trial.cards,b.card_trial.cards)
 end
end)
test('invalid card, legacy poker, raw actions and malformed targets are atomic rejections',function()
 local s=fresh()
 for _,i in ipairs({{kind='technique',card_id=2,x=8,y=8},{kind='technique',card_id=1.5},
  {kind='technique',card_id=1,action='heal',x=8,y=8},{kind='card_action',cards={1}},
  {kind='move',dx=1,dy=0},{kind='dash',dx=1,dy=0},{kind='wait'},{kind='use_utility'},
  {kind='trial_basic',action='wait'},{kind='technique',card_id=1,x=7,y=8},
  {kind='technique',card_id=5,x=8,y=8},{kind='replace_card',card_id=77},
  {kind='trial_basic',action='move',dx=1,dy=1}})do unchanged(s,i)end
end)
test('preview paths, target enumeration, primary forecast and cancellation are state-pure',function()
 local s=fresh('movement');local before=G.snapshot(s)
 for _=1,5 do
  local targets=G.card_targets(s,action(s,'technique',1));assert(#targets==4)
  for _,at in ipairs(targets)do assert(G.card_preview(s,action(s,'technique',1,at.x,at.y)).valid)end
  G.card_targets(s,action(s,'technique',5));G.card_preview(s,action(s,'technique',9));G.card_forecast(s,basic(1,0,true))
 end
 same(before,s);unchanged(s,{kind='cancel'})
end)
test('Dash preview traversal and landing match committed movement and normal time',function()
 local s=fresh();quiet(s);local i=action(s,'technique',1,8,8);local p=G.card_preview(s,i)
 assert(p.valid and p.distance==2 and p.cells[1].x==7 and p.landing.x==8)
 local e=submit(s,i);local entries={}
 for _,v in ipairs(e)do if v.kind=='TileEntered' and v.source_id==s.player_id then entries[#entries+1]=v end end
 assert(#entries==2 and entries[1].x==7 and entries[2].x==8 and G.player(s).position.x==8)
 assert(s.clock==130 and s.last_action.cost==p.cost)
end)
test('Sweep preview matches exact special footprint and guaranteed primary damage',function()
 local s=fresh();near(s,{{5,7},{6,7},{9,3}});local i=action(s,'technique',5,6,7)
 local p=G.card_preview(s,i);local before=G.snapshot(s);local hits=G.card_forecast(s,i);same(before,s)
 assert(#p.cells==3 and #hits==2 and hits[1].loss==2)
 local e=submit(s,i);assert(s.clock==150)
 for _,v in ipairs(e)do if v.kind=='AttackPerformed' and v.source_id==s.player_id then
  assert(v.ability_id=='core:ability/sweeping_slash');for j,c in ipairs(v.target_cells)do assert(c.x==p.cells[j].x and c.y==p.cells[j].y)end
 end end
end)
test('Mend uses 4 capped HP and real healing source, never Wait/pickup triggers',function()
 local s=fresh();quiet(s);G.player(s).hp=22
 B.set_stacks(G.player(s),'core:boon/crosswind','rare',1)
 local i=action(s,'technique',9);local p=G.card_preview(s,i);assert(p.heal==2)
 local e=submit(s,i);assert(G.player(s).hp==24 and s.clock==100 and s.card_trial.healing_charges==1)
 assert(count(e,'Healed')==1 and count(e,'Waited')==0 and count(e,'SupplyCollected')==0 and count(e,'BoonActivated')==0)
 for _,v in ipairs(e)do if v.kind=='Healed'then assert(v.healing_source=='trial:equipment/healing_flask' and v.actual_gain==2)end end
end)
test('Full health and exhausted Mend reject without changing any state',function()
 local s=fresh();unchanged(s,action(s,'technique',9));assert(G.card_preview(s,action(s,'technique',9)).reason=='Full health')
 G.player(s).hp=12;s.card_trial.healing_charges=0;unchanged(s,action(s,'technique',9))
end)
test('second Mend retires its identity, refills once and replay restores charges/deck',function()
 local s=fresh();quiet(s);G.player(s).hp=4;submit(s,action(s,'technique',9))
 while not Cards.owned(s.card_trial.cards,9)do submit(s,action(s,'replace_card'))end
 local before=G.snapshot(s.card_trial.cards);local slot=Cards.owned(before,9);local e=submit(s,action(s,'technique',9))
 local d=s.card_trial.cards;assert(s.card_trial.healing_charges==0 and #d.retired==1 and d.retired[1]==9)
 assert(count(e,'TechniqueRetired')==1 and count(e,'Healed')==1 and count(e,'Waited')==0)
 for i,id in ipairs(before.hand)do if i~=slot then assert(d.hand[i]==id)end end
 for _,pile in ipairs({d.hand,d.draw,d.discard})do for _,id in ipairs(pile)do assert(id~=9)end end
 for _=1,30 do submit(s,action(s,'replace_card'));assert(Cards.validate(d))end
 local replay=fresh();assert(replay.card_trial.healing_charges==2 and replay.card_trial.cards.hand[3]==9)
end)
test('Replace pays one real scaled Wait with applicable effects and retains other cards',function()
 local s=fresh();quiet(s);G.player(s).speed=200;B.set_stacks(G.player(s),'core:boon/crosswind','rare',1)
 local e=submit(s,action(s,'replace_card',5));assert(s.clock==50 and s.last_action.kind=='wait')
 assert(s.card_trial.cards.hand[1]==1 and s.card_trial.cards.hand[3]==9)
 assert(count(e,'Waited')==1 and count(e,'BoonActivated','crosswind')==1 and count(e,'CardReplaced')==1 and count(e,'AttackPerformed')==0)
end)
test('Knight Boots transforms all Dash instances without changing piles, RNG, HP or charges',function()
 local s=fresh();quiet(s);submit(s,action(s,'technique',1,8,8));resolve_first(s)
 local before=G.snapshot(s);local e=submit(s,{kind='trial_reward',offer_index=1});local d=s.card_trial.cards
 for _,pile in ipairs({'hand','draw','discard','retired'})do same(before.card_trial.cards[pile],d[pile])end
 same(before.card_trial.cards.rng,d.rng);assert(G.player(s).hp==G.player(before).hp and s.clock==before.clock)
 assert(s.card_trial.healing_charges==before.card_trial.healing_charges and s.card_trial.fight==2)
 for id=1,4 do assert(d.instances[id].kind=='knight_hop' and d.instances[id].id==id)end
 assert(count(e,'TechniqueUpgradeGranted')==1 and not G.player(s).boons['trial:upgrade/knight_boots'])
 unchanged(s,{kind='trial_reward',offer_index=1});assert(Cards.validate(d))
end)
test('Knight Hop accepts exactly free walkable L landings, not walls, water, bodies or other offsets',function()
 local s=fresh();quiet(s);Cards.knight_boots(s.card_trial.cards)
 local p=G.player(s).position;p.x,p.y=6,8
 set_tile(s,4,7,'~');set_tile(s,8,7,'#');s.entities[2].position.x,s.entities[2].position.y=7,6
 local legal=0
 for y=1,11 do for x=1,11 do
  local dx,dy=x-p.x,y-p.y
  local expected=(math.abs(dx)==1 and math.abs(dy)==2 or math.abs(dx)==2 and math.abs(dy)==1)
   and x>1 and x<11 and y>1 and y<11 and require('src.content').walkable(s.final_map,x,y)
  for _,e in ipairs(s.entities)do if e.blocks_movement and e.position.x==x and e.position.y==y then expected=false end end
  local i=action(s,'technique',1,x,y);assert(G.card_preview(s,i).valid==(expected or false))
  if expected then legal=legal+1 else unchanged(s,i)end
 end end
 assert(#G.card_targets(s,action(s,'technique',1))==legal and legal>0)
end)
test('jump ignores intermediate hazards and pickups; landing enters exactly once and collects only there',function()
 local s=fresh();quiet(s);Cards.knight_boots(s.card_trial.cards)
 set_tile(s,6,7,'b');set_tile(s,7,7,'b');set_tile(s,7,6,'b');G.player(s).hp=20
 local emit=function()end;local R=require('src.rewards')
 local middle=R.spawn_currency(s,2,s.active_floor_id,6,7,emit,s.player_id)
 local landing=R.spawn_currency(s,3,s.active_floor_id,7,6,emit,s.player_id)
 local e=submit(s,action(s,'technique',1,7,6));assert(s.clock==130 and G.player(s).hp==19)
 assert(not middle.collected and landing.collected and s.currency==3 and count(e,'TerrainTriggered')==1)
 assert(count(e,'TileEntered')==1 and count(e,'Moved')>=1)
 for _,v in ipairs(e)do if v.kind=='Moved' and v.source_id==s.player_id then
  assert(v.ability_id=='trial:ability/knight_hop' and v.movement_kind=='jump' and v.path==nil)
 end end
 assert(s.last_action.kind=='knight_hop' and not G.player(s).invulnerable)
end)
test('Knight Hop retains general movement and landing boons, excludes actual-dash/path reactions',function()
 local s=fresh('movement');near(s,{{3,7},{4,6},{9,3}});Cards.knight_boots(s.card_trial.cards)
 B.set_stacks(G.player(s),'core:boon/pulse_step','rare',1)
 B.set_stacks(G.player(s),'core:boon/phantom_lunge','rare',1)
 local e=submit(s,action(s,'technique',1,4,7))
 assert(count(e,'BoonActivated','rhythm_of_steel')==1 and count(e,'BoonActivated','pulse_step')==1)
 assert(count(e,'BoonActivated','phantom_lunge')==0)
 local hit=submit(s,basic(0,-1,true));assert(count(hit,'BoonActivated','rhythm_of_steel')==1)
end)
test('jump away from a real locked attack primes Ring of Reversal',function()
 local s=fresh('movement');near(s,{{7,8},{9,3},{9,4}});Cards.knight_boots(s.card_trial.cards)
 local foe=s.entities[2];S.enqueue(s,{kind='actor_ready',due=0,actor_id=foe.id,is_player=false})
 submit(s,action(s,'replace_card',5));assert(foe.pending_attack)
 local e=submit(s,action(s,'technique',1,4,7));assert(count(e,'RingReversalPrimed')==1)
 foe.position.x,foe.position.y=3,7
 local hit=submit(s,basic(-1,0,true));assert(count(hit,'RingReversalSpent')==1)
end)
test('existing Burning basic and Sweep packets still ignite and link actual enemies',function()
 local s=fresh('burning');near(s,{{5,7},{6,7},{9,3}})
 local e=submit(s,action(s,'technique',5,6,7))
 assert(count(e,'BoonActivated','emberbrand')>=2 and count(e,'BoonActivated','shared_agony')>0)
 assert(count(e,'StatusApplied')>=2 and s.last_action.kind=='sweeping_slash')
end)
test('same board distinguishes straight Dash, Knight Hop and blocked intermediate space',function()
 local s=fresh();quiet(s);local i=action(s,'technique',1,8,8);assert(G.card_preview(s,i).valid)
 Cards.knight_boots(s.card_trial.cards);assert(not G.card_preview(s,i).valid)
 assert(G.card_preview(s,action(s,'technique',1,7,6)).valid)
 s.card_trial.cards.instances[1].kind='dash';assert(not G.card_preview(s,action(s,'technique',1,7,6)).valid)
 G.player(s).position.x,G.player(s).position.y=5,6
 assert(not G.preview(s,'dash',1,0).valid) -- central wall (6,6)
 Cards.knight_boots(s.card_trial.cards);local p=G.card_preview(s,action(s,'technique',1,7,5));assert(p.valid and p.jump and #p.cells==1)
 submit(s,action(s,'technique',1,7,5));assert(G.player(s).position.x==7 and G.player(s).position.y==5)
end)
test('jump playback snaps between endpoints without a fabricated walking trail',function()
 local P=require('src.playback');local s=fresh();quiet(s);Cards.knight_boots(s.card_trial.cards)
 local e=submit(s,action(s,'technique',1,7,6));local p=P.new();P.add(p,e,s.player_id)
 local track=p.items[1].motions[s.player_id];assert(track.snap and #track.path==2)
 local x,y=P.position(p,s.player_id,s.active_floor_id,7,6);assert(x==6 and y==8)
 P.update(p,1);x,y=P.position(p,s.player_id,s.active_floor_id,7,6);assert(x==7 and y==6)
end)
test('ordinary boon rewards apply once and preserve HP, charges, cards and ongoing schedules',function()
 for index,id in ipairs({'pulse_step','detonation_bloom'})do
  local s=fresh();quiet(s);G.player(s).hp=16;s.card_trial.healing_charges=1
  B.set_stacks(G.player(s),'core:boon/temporal_echo','rare',1);resolve_first(s)
  local before=G.snapshot(s);local cards=G.snapshot(s.card_trial.cards)
  local e=submit(s,{kind='trial_reward',offer_index=index+1})
  assert(G.player(s).boons['core:boon/'..id].rare==1 and count(e,'BoonGranted',id)==1)
  same(cards,s.card_trial.cards);assert(s.clock==before.clock and s.card_trial.healing_charges==1)
  assert(G.player(s).hp==G.player(before).hp and s.card_trial.fight==2)
  for _,item in ipairs(before.schedule)do
   if item.kind=='effect'then local found=false;for _,v in ipairs(s.schedule)do if v.id==item.id then same(item,v);found=true end end;assert(found)end
  end
  unchanged(s,{kind='trial_reward',offer_index=index+1})
 end
end)
test('resolved fight gates basic actions, replacement and techniques until the fixed reward',function()
 local s=fresh();resolve_first(s);local offers=G.snapshot(G.chest_options(s,s.card_trial.reward_id))
 unchanged(s,basic(1,0));unchanged(s,action(s,'replace_card'));unchanged(s,action(s,'technique',1,8,8))
 same(offers,G.chest_options(s,s.card_trial.reward_id));assert(offers.offers[1].upgrade_id==C.upgrade.id)
end)
test('simultaneous lethal scheduled resolution takes precedence over opening a reward',function()
 local s=fresh();quiet(s)
 for i=#s.entities,3,-1 do S.remove_for_actor(s,s.entities[i].id);table.remove(s.entities,i)end
 local foe=s.entities[2];foe.hp=1;foe.position.x,foe.position.y=7,8;G.player(s).hp=1
 local work=S.enqueue(s,{kind='effect',due=100,actor_id=0,attack_id=99});work.effect_id=work.id
 s.delayed_effects[work.id]={activation={id=99,owner_id=999,source_faction='enemy',power=1,
  source_position={floor_id=s.active_floor_id,x=8,y=8},chain_id=99,root_action_id=99,ancestry={},
  event={target_cells={{floor_id=s.active_floor_id,x=6,y=8}}}},effect={kind='echo_impact',damage_type='physical'}}
 submit(s,basic(1,0,true));assert(s.card_trial.phase=='death' and s.game_over and not s.card_trial.reward_id)
end)
test('death, victory and replay lock resolved encounters and restore the initial state',function()
 local s=fresh();resolve_first(s);submit(s,{kind='trial_reward',offer_index=1})
 clear_fight(s);assert(s.card_trial.phase=='victory' and s.game_over)
 unchanged(s,basic(1,0));unchanged(s,action(s,'replace_card'))
 same(fresh(),fresh());assert(fresh().card_trial.healing_charges==2)
end)
test('trial persistence rejection performs zero writes; ordinary save/restore/actions remain usable',function()
 local writes=0;local disk={read=function()end,write=function()writes=writes+1;return true end}
 local p=Codec.new(disk);local s=fresh()
 assert(not p:save(s) and not p:replace(s));s.game_over=true;assert(not p:pending_terminal(s) and not p:save(s))
 assert(writes==0 and not pcall(G.restore,G.snapshot(s)))
 local classic=G.new(99);local restored=G.restore(G.snapshot(classic))
 submit(restored,{kind='move',dx=1,dy=0});submit(restored,{kind='basic_attack',dx=1,dy=0});submit(restored,{kind='wait'})
 assert(restored.clock==300);unchanged(restored,{kind='technique',card_id=1,x=3,y=2})
end)
print(('Card Trial: %d passed, %d failed'):format(passed,failed))
if failed>0 then os.exit(1)end
