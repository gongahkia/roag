-- Authoritative poker/stamina cycle and simulation boundaries, in-memory only.
package.path='./?.lua;./?/init.lua;'..package.path
local G=require('src.game')
local T=require('src.card_trial')
local C=require('src.card_trial_content')
local Cards=require('src.cards')
local Poker=require('src.poker')
local RNG=require('src.rng')
local S=require('src.scheduler')
local B=require('src.boons')
local Codec=require('src.persistence')
local passed,failed=0,0
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'states differ')end
local function test(name,fn)local ok,why=xpcall(fn,debug.traceback);if ok then passed=passed+1;print('PASS '..name)else failed=failed+1;print('FAIL '..name..': '..tostring(why))end end
local function fresh(preset,mode)return T.new(G,202,preset or 'bare',mode)end
local function quiet(s)for _,e in ipairs(s.entities)do if e.controller=='ai'then S.remove_for_actor(s,e.id)end end end
local function i(a,dx,dy)return {kind='trial_action',action=a,dx=dx,dy=dy}end
local function target(a,x,y)return {kind='trial_action',action=a,x=x,y=y}end
local function submit(s,intent)local ok,why,e=G.submit(s,intent);assert(ok,why);return e end
local function unchanged(s,intent)local before=G.snapshot(s);local ok=G.submit(s,intent);assert(not ok);same(before,s)end
local function count(e,k,id)local n=0;for _,v in ipairs(e)do if v.kind==k and (not id or v.boon_id=='core:boon/'..id)then n=n+1 end end;return n end
local function fund(s,n)
 if s.card_trial.resource_phase=='prepare'then submit(s,{kind='cash_hand'})end
 if n then s.card_trial.stamina=n end -- explicit model fixture only
end
local function near(s,points)
 quiet(s);local n=0
 for _,e in ipairs(s.entities)do if e.controller=='ai'then n=n+1;e.position.x,e.position.y=unpack(points[n]or {9,n+1})end end
end
local function hand(s,ids)
 local d=s.card_trial.cards;d.hand={};d.draw={};d.discard={};local seen={}
 for _,id in ipairs(ids)do d.hand[#d.hand+1]=id;seen[id]=true end
 for id=1,#d.instances do if not seen[id]then d.draw[#d.draw+1]=id end end
 assert(Cards.validate(d))
end
local function deck_faces(faces)
 local d={instances={},hand={},draw={},discard={},rng=RNG.new(3)}
 for id,face in ipairs(faces)do d.instances[id]={id=id,face=face};d.hand[id]=id end;return d
end
local function best(faces)local d=deck_faces(faces);return Poker.best(d,d.hand)end
local function clear_fight(s)
 quiet(s);fund(s,100)
 for _=1,14 do
  if s.card_trial.phase~='combat'then return end
  local e;for _,v in ipairs(s.entities)do if v.controller=='ai' and v.hp>0 then e=v;break end end
  e.hp=1;e.position.x,e.position.y=G.player(s).position.x+1,G.player(s).position.y
  submit(s,i('strike',1,0))
 end;error('fight did not resolve')
end

test('payout table: all nine categories use only contributing instances',function()
 local cases={{{1},'single',1},{{2,15},'pair',2},{{2,15,3,16},'two_pair',4},{{2,15,28},'three_kind',3},
  {{1,15,29,43,5},'straight',5},{{1,3,6,8,11},'flush',5},{{2,15,28,3,16},'full_house',5},
  {{2,15,28,41},'four_kind',4},{{2,3,4,5,6},'straight_flush',5}}
 for _,v in ipairs(cases)do local r=best(v[1]);assert(r.key==v[2] and #r.cards==v[3] and r.stamina==C.payouts[v[2]])end
end)
test('Ace is low or high in exactly five distinct ranks; no wrapping or duplicate ranks',function()
 assert(best({1,2,3,4,5}).key=='straight_flush');assert(best({10,11,12,13,1}).key=='straight_flush')
 assert(best({13,1,15,29,43}).key~='straight')
 assert(best({1,14,3,4,5}).key~='straight');assert(best({10,24,38,52,14}).key=='straight')
end)
test('best subset ignores kickers, honours an override pool and deterministically breaks ties',function()
 local d=deck_faces({2,15,28,41,3,16,29,52});local r=Poker.best(d,d.hand)
 assert(r.key=='four_kind');same(r.cards,{1,2,3,4})
 local pair=Poker.best(d,{1,2});assert(pair.key=='pair');same(pair.cards,{1,2})
 local full=Poker.best(d,d.hand,'full_house');assert(full.key=='full_house' and #full.cards==5)
 local reversed={8,7,6,5,4,3,2,1};same(r,Poker.best(d,reversed))
end)
test('identical faces remain distinct; five matching ranks give a four-card subset, not five-kind',function()
 local d=deck_faces({1,1,1,1,1,8,20,35});local r=Poker.best(d,d.hand)
 assert(r.key=='four_kind' and #r.cards==4);same(r.cards,{1,2,3,4})
 assert(Poker.classify(d,{1,2,3,4,5})=='flush')
 assert(best({1,1,1,1,1}).stamina==10)
end)
test('opening deals eight physical instances from a seeded independent 52-card deck at zero time',function()
 local a,b=fresh(),fresh();same(a,b);local t=a.card_trial
 assert(t.resource_phase=='prepare' and t.stamina==0 and #t.cards.hand==8 and #t.cards.draw==44 and a.clock==0)
 assert(#t.cards.instances==52 and Cards.validate(t.cards));assert(not t.exchange_used)
end)
test('cash consumes exactly the displayed contributors, no refills/time/events, and only once',function()
 local s=fresh();local before=G.snapshot(s);local r=T.cash_preview(s);local e=submit(s,{kind='cash_hand'})
 assert(#e==0 and s.clock==0 and s.card_trial.stamina==r.stamina and s.card_trial.resource_phase=='spend')
 assert(#s.card_trial.cards.hand==8-#r.cards and #s.card_trial.cards.draw==44)
 same(before.schedule,s.schedule);same(before.rng,s.rng);same(before.card_trial.cards.rng,s.card_trial.cards.rng)
 for _,id in ipairs(r.cards)do assert(not Cards.owned(s.card_trial.cards,id))end
 unchanged(s,{kind='cash_hand'});assert(Cards.validate(s.card_trial.cards))
end)
test('a larger selected pool shows a best subset and preserves irrelevant kickers',function()
 local s=fresh();hand(s,{2,15,28,4,6,8,10,12});local r=T.cash_preview(s,{2,15,28,4})
 assert(r.key=='three_kind' and #r.cards==3);submit(s,{kind='cash_hand',ids={2,15,28,4}})
 assert(Cards.owned(s.card_trial.cards,4) and s.card_trial.stamina==6)
end)
test('exchange is one free one-to-three operation, retains other instances and cannot repeat',function()
 local s=fresh();local d=s.card_trial.cards;local old=G.snapshot(d);local ids={d.hand[1],d.hand[3],d.hand[7]}
 local e=submit(s,{kind='exchange_cards',ids=ids});assert(#e==0 and s.clock==0 and s.card_trial.stamina==0)
 for j,id in ipairs(old.hand)do if j~=1 and j~=3 and j~=7 then assert(d.hand[j]==id)end end
 for _,id in ipairs(ids)do assert(not Cards.owned(d,id))end
 assert(s.card_trial.exchange_used and Cards.validate(d));unchanged(s,{kind='exchange_cards',ids={d.hand[1]}})
end)
test('invalid exchanges and cash ownership leave all state and RNG untouched',function()
 local s=fresh();local h=s.card_trial.cards.hand
 for _,ids in ipairs({{}, {h[1],h[1]}, {h[1],h[2],h[3],h[4]}, {999}, {[2]=h[1]}})do unchanged(s,{kind='exchange_cards',ids=ids})end
 unchanged(s,{kind='cash_hand',ids={999}});unchanged(s,{kind='cash_hand',ids={h[1],h[1]}})
 submit(s,{kind='cash_hand'});unchanged(s,{kind='exchange_cards',ids={s.card_trial.cards.hand[1]}})
end)
test('exchange quarantine excludes just-exchanged IDs even when replacement reshuffles',function()
 local s=fresh();local d=s.card_trial.cards
 for _,id in ipairs(d.draw)do d.discard[#d.discard+1]=id end;d.draw={}
 local ids={d.hand[1],d.hand[2],d.hand[3]};submit(s,{kind='exchange_cards',ids=ids})
 for _,id in ipairs(ids)do assert(not Cards.owned(d,id));for _,v in ipairs(d.draw)do assert(v~=id)end end
 assert(#d.discard==3 and Cards.validate(d))
end)
test('retention is optional, limited to two unused identities, and cash removes ineligible marks',function()
 local s=fresh();local h=s.card_trial.cards.hand
 unchanged(s,{kind='retain_cards',ids={h[1],h[2],h[3]}});unchanged(s,{kind='retain_cards',ids={999}})
 submit(s,{kind='retain_cards',ids={h[1],h[2]}});assert(s.card_trial.retained[h[1]])
 local id=h[1];submit(s,{kind='cash_hand',ids={id}});assert(not s.card_trial.retained[id])
 unchanged(s,{kind='retain_cards',ids={id}})
end)
test('Recover pays one real Wait, retains marked cards, loses budget, and then fills to eight',function()
 local s=fresh();quiet(s);submit(s,{kind='cash_hand'});local t=s.card_trial
 local keep=t.cards.hand[1];submit(s,{kind='retain_cards',ids={keep}})
 B.set_stacks(G.player(s),'core:boon/crosswind','rare',1)
 local cycle=t.cycle;local e=submit(s,i('recover'))
 assert(s.clock==100 and t.stamina==0 and t.resource_phase=='prepare' and t.cycle==cycle+1 and #t.cards.hand==8)
 assert(Cards.owned(t.cards,keep) and next(t.retained)==nil and not t.exchange_used)
 assert(count(e,'Waited')==1 and count(e,'BoonActivated','crosswind')==1 and Cards.validate(t.cards))
end)
test('early Recover before cashing is paid and cannot become a free repeat deal',function()
 local s=fresh();quiet(s);local old=s.card_trial.cycle
 submit(s,i('recover'));assert(s.clock==100 and s.card_trial.cycle==old+1)
 submit(s,i('recover'));assert(s.clock==200 and s.card_trial.cycle==old+2)
end)
test('card conservation holds through cash, retention, exchanges, exhaustion and reshuffling',function()
 local s=fresh();quiet(s)
 for _=1,80 do
  local d=s.card_trial.cards;submit(s,{kind='exchange_cards',ids={d.hand[1],d.hand[2],d.hand[3]}})
  submit(s,{kind='cash_hand'});local ids={};for n=1,math.min(2,#d.hand)do ids[n]=d.hand[n]end
  submit(s,{kind='retain_cards',ids=ids});submit(s,i('recover'));assert(Cards.validate(d))
 end
 assert(#s.card_trial.cards.instances==52 and s.clock==8000)
end)
test('render-like evaluation and preview are pure; unrelated combat RNG does not change cards/offers',function()
 local a,b=fresh(),fresh();quiet(a);quiet(b);local before=G.snapshot(a)
 for _=1,20 do T.cash_preview(a);G.card_targets(a,{kind='trial_action',action='dash'});G.card_preview(a,i('recover'))end
 same(before,a)
 for _=1,12 do
  for _=1,9 do RNG.next_raw(b.rng)end
  submit(a,{kind='cash_hand'});submit(b,{kind='cash_hand'});submit(a,i('recover'));submit(b,i('recover'))
  same(a.card_trial.cards,b.card_trial.cards);same(a.card_trial.deck_offers,b.card_trial.deck_offers)
 end
end)
test('unfunded controls and every obsolete raw route are authoritative no-ops',function()
 local s=fresh()
 for _,v in ipairs({i('move',-1,0),i('strike',-1,0),target('dash',8,8),target('sweep',5,7),
  {kind='move',dx=1,dy=0},{kind='basic_attack',dx=1,dy=0},{kind='dash',dx=1,dy=0},{kind='wait'},
  {kind='technique',card_id=1,x=8,y=8},{kind='replace_card',card_id=1},{kind='trial_basic',action='move',dx=1,dy=0},
  {kind='use_utility'},{kind='interact'},{kind='claim_chest'},{kind='cancel'}})do unchanged(s,v)end
end)
test('ordinary Step costs one, bump/aimed Strike cost two, and cards never cycle',function()
 local s=fresh();near(s,{{5,7},{9,3},{9,4}});fund(s,5);local d=G.snapshot(s.card_trial.cards)
 submit(s,i('move',-1,0));assert(s.card_trial.stamina==4 and s.clock==100)
 submit(s,i('move',0,-1));assert(s.last_action.bump and s.card_trial.stamina==2 and s.clock==200)
 submit(s,i('strike',0,-1));assert(s.card_trial.stamina==0 and s.clock==300);same(d,s.card_trial.cards)
 unchanged(s,i('move',0,1));unchanged(s,i('strike',0,-1))
end)
test('Dash pays two stamina once despite two TileEntered events; preview matches actual landing',function()
 local s=fresh('movement');quiet(s);fund(s,5);local d=G.snapshot(s.card_trial.cards)
 local p=G.card_preview(s,target('dash',8,8));local e=submit(s,target('dash',8,8))
 assert(p.distance==2 and p.stamina_after==3 and s.card_trial.stamina==3 and s.clock==130)
 assert(count(e,'TileEntered')==2 and count(e,'BoonActivated','rhythm_of_steel')==1)
 assert(G.player(s).position.x==p.landing.x);same(d,s.card_trial.cards)
end)
test('Sweep pays three once, keeps primary special identity and forecast agrees with its footprint',function()
 local s=fresh('burning');near(s,{{5,7},{6,7},{9,3}});fund(s,6)
 local v=target('sweep',6,7);local p=G.card_preview(s,v);local before=G.snapshot(s);local hits=G.card_forecast(s,v);same(before,s)
 local e=submit(s,v);assert(s.clock==150 and s.card_trial.stamina==3 and #hits==2)
 assert(count(e,'BoonActivated','emberbrand')>=2 and count(e,'BoonActivated','shared_agony')>0)
 assert(s.last_action.kind=='sweeping_slash' and p.stamina_after==3)
 for _,ev in ipairs(e)do if ev.kind=='AttackPerformed' and ev.source_id==s.player_id then for j,c in ipairs(ev.target_cells)do assert(c.x==p.cells[j].x and c.y==p.cells[j].y)end end end
end)
test('illegal movement/targets, insufficient bump budget and cancellation spend nothing',function()
 local s=fresh();near(s,{{7,8},{9,3},{9,4}});fund(s,1)
 unchanged(s,i('move',1,0));unchanged(s,target('dash',8,8));unchanged(s,target('sweep',9,8));unchanged(s,{kind='cancel'})
 G.player(s).position.x,G.player(s).position.y=6,7;unchanged(s,i('move',0,-1))
end)
test('Mend pays two and one charge for four capped HP; no Wait/pickup/card retirement',function()
 local s=fresh();quiet(s);fund(s,8);local p=G.player(s);local d=G.snapshot(s.card_trial.cards)
 unchanged(s,i('mend'));p.hp=10
 B.set_stacks(p,'core:boon/crosswind','rare',1)
 local e=submit(s,i('mend'));assert(p.hp==14 and s.card_trial.stamina==6 and s.card_trial.healing_charges==1 and s.clock==100)
 local e2=submit(s,i('mend'));assert(p.hp==18 and s.card_trial.healing_charges==0 and s.card_trial.stamina==4)
 for _,ev in ipairs({e,e2})do assert(count(ev,'Healed')==1 and count(ev,'Waited')==0 and count(ev,'SupplyCollected')==0 and count(ev,'BoonActivated','crosswind')==0)end
 unchanged(s,i('mend'));same(d,s.card_trial.cards)
end)
test('Recover ending in death never draws, restores stamina or reopens preparation',function()
 local s=fresh();quiet(s);fund(s,5);G.player(s).hp=1
 local work=S.enqueue(s,{kind='effect',due=100,actor_id=0,attack_id=99});work.effect_id=work.id
 s.delayed_effects[work.id]={activation={id=99,owner_id=999,source_faction='enemy',power=1,
  source_position={floor_id=s.active_floor_id,x=8,y=8},chain_id=99,root_action_id=99,ancestry={},
  event={target_cells={{floor_id=s.active_floor_id,x=6,y=8}}}},effect={kind='echo_impact',damage_type='physical'}}
 local cycle=s.card_trial.cycle;submit(s,i('recover'))
 assert(s.game_over and s.card_trial.phase=='death' and s.card_trial.resource_phase=='ended')
 assert(#s.card_trial.cards.hand==0 and s.card_trial.stamina==0 and s.card_trial.cycle==cycle)
 assert(Cards.validate(s.card_trial.cards));unchanged(s,i('recover'))
end)
test('Recover can finish a fight via scheduled effects but does not grant another active hand',function()
 local s=fresh();quiet(s);fund(s,5)
 for n=#s.entities,3,-1 do table.remove(s.entities,n)end
 local enemy=s.entities[2];enemy.hp=1;enemy.position.x,enemy.position.y=7,8
 local work=S.enqueue(s,{kind='effect',due=100,actor_id=0,attack_id=99});work.effect_id=work.id
 s.delayed_effects[work.id]={activation={id=99,owner_id=s.player_id,source_faction='player',power=2,
  source_position={floor_id=s.active_floor_id,x=6,y=8},chain_id=99,root_action_id=99,ancestry={},
  event={target_cells={{floor_id=s.active_floor_id,x=7,y=8}}}},effect={kind='echo_impact',damage_type='physical'}}
 local cycle=s.card_trial.cycle;submit(s,i('recover'))
 assert(s.card_trial.phase=='reward' and s.card_trial.resource_phase=='ended' and s.card_trial.cycle==cycle)
 assert(#s.card_trial.cards.hand==0 and s.card_trial.stamina==0 and Cards.validate(s.card_trial.cards))
 unchanged(s,i('recover'));unchanged(s,{kind='cash_hand'})
end)
test('Fixed Stamina is the same action path: four to start, paid Recover restores four, no cards',function()
 local s=fresh('movement','fixed');quiet(s);local t=s.card_trial
 assert(t.stamina==4 and t.resource_phase=='spend' and not t.cards)
 submit(s,i('move',-1,0));assert(s.clock==100 and t.stamina==3)
 submit(s,target('dash',3,8));assert(s.clock==230 and t.stamina==1)
 unchanged(s,i('strike',-1,0));local e=submit(s,i('recover'));assert(s.clock==330 and t.stamina==4 and count(e,'Waited')==1)
 unchanged(s,{kind='cash_hand'});unchanged(s,{kind='exchange_cards',ids={1}});unchanged(s,{kind='retain_cards',ids={}})
end)
test('speed scaling, multi-phase enemies and status work remain scheduled once per command',function()
 local s=fresh();fund(s,4);G.player(s).speed=200
 local e=submit(s,i('move',-1,0));assert(s.clock==50 and s.card_trial.stamina==3 and s.last_action.cost==50)
 local moved=0;for _,v in ipairs(e)do if v.kind=='Moved' and v.source_id~=s.player_id then moved=moved+1 end end;assert(moved>0)
 submit(s,i('recover'));assert(s.clock==100)
end)
test('Ring of Reversal and general movement boons still react to funded escape and primary hit',function()
 local s=fresh('movement');near(s,{{7,8},{9,3},{9,4}});fund(s,8)
 local foe=s.entities[2];S.enqueue(s,{kind='actor_ready',due=0,actor_id=foe.id,is_player=false})
 submit(s,i('recover'));submit(s,{kind='cash_hand'})
 local e=submit(s,i('move',0,1));assert(count(e,'RingReversalPrimed')==1)
 local hit=submit(s,i('strike',1,-1));assert(count(hit,'RingReversalSpent')==1 and count(hit,'BoonActivated','rhythm_of_steel')==1)
end)
test('boon reward then deck edit each apply once; transition preserves simulation and resets only hand resources',function()
 local s=fresh('movement');quiet(s);fund(s,10);G.player(s).hp=15;s.card_trial.healing_charges=1
 B.set_stacks(G.player(s),'core:boon/temporal_echo','rare',1)
 clear_fight(s);assert(s.card_trial.phase=='reward');local before=G.snapshot(s)
 local e=submit(s,{kind='trial_reward',offer_index=2});assert(count(e,'BoonGranted','pulse_step')==1)
 assert(s.card_trial.phase=='deck_reward' and s.card_trial.fight==1)
 same(before.card_trial.cards,s.card_trial.cards);unchanged(s,{kind='trial_reward',offer_index=2});unchanged(s,i('recover'))
 local d=s.card_trial.cards;local face=s.card_trial.deck_offers[1];local copies=Cards.face_count(d,face);local clock=s.clock;local pending=G.snapshot(s.delayed_effects)
 local e2=submit(s,{kind='trial_deck_reward',choice=1});assert(#e2==0)
 assert(#d.instances==55 and Cards.face_count(d,face)==copies+3 and Cards.validate(d))
 assert(s.card_trial.phase=='combat' and s.card_trial.fight==2 and s.card_trial.resource_phase=='prepare')
 assert(s.card_trial.stamina==0 and #d.hand==8 and not s.card_trial.exchange_used and not next(s.card_trial.retained))
 assert(s.clock==clock and G.player(s).hp==G.player(before).hp and s.card_trial.healing_charges==1)
 same(pending,s.delayed_effects);unchanged(s,{kind='trial_deck_reward',choice=1});unchanged(s,{kind='trial_deck_reward',choice=0})
end)
test('deck offers are distinct, seeded independently of performance; added copies really enter draws',function()
 local a,b=fresh(),fresh();local offers=G.snapshot(a.card_trial.deck_offers)
 assert(offers[1]~=offers[2]);for _=1,50 do RNG.next_raw(b.rng)end
 clear_fight(a);clear_fight(b);submit(a,{kind='trial_reward',offer_index=3});submit(b,{kind='trial_reward',offer_index=3})
 same(offers,a.card_trial.deck_offers);same(offers,b.card_trial.deck_offers)
 submit(a,{kind='trial_deck_reward',choice=2});local d=a.card_trial.cards;local seen={}
 quiet(a)
 for _=1,35 do for _,id in ipairs(d.hand)do if id>52 then seen[id]=true end end
  submit(a,i('recover'));assert(Cards.validate(d))
 end
 assert(seen[53] and seen[54] and seen[55])
end)
test('Skip does not edit deck; Fixed Stamina omits deck reward and starts fight two with four',function()
 local s=fresh();clear_fight(s);submit(s,{kind='trial_reward',offer_index=1});submit(s,{kind='trial_deck_reward',choice=0})
 assert(#s.card_trial.cards.instances==52 and s.card_trial.knight_boots)
 local f=fresh('bare','fixed');clear_fight(f);local clock=f.clock;submit(f,{kind='trial_reward',offer_index=3})
 assert(f.card_trial.fight==2 and f.card_trial.stamina==4 and f.clock==clock and not f.card_trial.cards)
 unchanged(f,{kind='trial_deck_reward',choice=1})
end)
test('Knight Hop still ignores intermediate space and charges the movement action once',function()
 local s=fresh('movement');near(s,{{6,4},{8,5},{2,2}});fund(s,5);s.card_trial.knight_boots=true
 local p=G.player(s);p.position.x,p.position.y=5,6
 B.set_stacks(p,'core:boon/pulse_step','rare',1);B.set_stacks(p,'core:boon/phantom_lunge','rare',1)
 local map=s.final_map;map.rows[5]=map.rows[5]:sub(1,6)..'b'..map.rows[5]:sub(8)
 local before=G.snapshot(s);local v=target('dash',7,5);local pr=G.card_preview(s,v);same(before,s)
 assert(pr.jump and #pr.cells==1 and pr.stamina_after==3)
 local e=submit(s,v);assert(s.clock==130 and s.card_trial.stamina==3 and p.hp==23)
 assert(count(e,'TileEntered')==1 and count(e,'BoonActivated','pulse_step')==1 and count(e,'BoonActivated','phantom_lunge')==0)
 for _,ev in ipairs(e)do if ev.kind=='Moved' and ev.source_id==s.player_id then assert(ev.movement_kind=='jump' and not ev.path)end end
 unchanged(s,target('dash',6,4))
end)
test('resolved encounters gate all economics/actions; victory/replay restore original deck',function()
 local s=fresh();clear_fight(s)
 for _,v in ipairs({i('move',1,0),i('recover'),{kind='exchange_cards',ids={1}},{kind='cash_hand'},{kind='retain_cards',ids={}}})do unchanged(s,v)end
 submit(s,{kind='trial_reward',offer_index=1});submit(s,{kind='trial_deck_reward',choice=1});clear_fight(s)
 assert(s.card_trial.phase=='victory' and s.game_over);unchanged(s,i('recover'))
 local replay=fresh();assert(#replay.card_trial.cards.instances==52 and replay.card_trial.stamina==0 and replay.card_trial.healing_charges==2)
 same(replay,fresh())
end)
test('trial storage guards never write or load; classic raw controls and restoration still work',function()
 local writes=0;local disk={read=function()end,write=function()writes=writes+1;return true end};local p=Codec.new(disk)
 for _,mode in ipairs({'poker','fixed'})do local s=fresh('bare',mode);assert(not p:save(s) and not p:replace(s));s.game_over=true;assert(not p:pending_terminal(s) and not p:save(s));assert(not pcall(G.restore,G.snapshot(s)))end
 assert(writes==0)
 local classic=G.restore(G.snapshot(G.new(99)));submit(classic,{kind='move',dx=1,dy=0});submit(classic,{kind='basic_attack',dx=1,dy=0});submit(classic,{kind='wait'});assert(classic.clock==300)
 unchanged(classic,{kind='cash_hand'});unchanged(classic,i('recover'))
end)
local function input_fixture(s)
 local U=require('src.card_trial_ui')
 U.attach({state=function()return s end,busy=function()return false end,inspection=function()return false end,target=function()end,setup=function()end,start=function()end,hide_reward=function()end,
  submit=function(v)return G.submit(s,v)end,cell=function(x,y)return x,y end})
 U.open(202,s.card_trial.mode);U.start();return U
end
test('real trial UI routes opening cash, direct/bump movement, retention and recovery through authority',function()
 local s=fresh();near(s,{{5,7},{9,3},{9,4}});local U=input_fixture(s)
 local before=G.snapshot(s);U.keypressed('a');same(before,s);U.keypressed('return');assert(s.card_trial.resource_phase=='spend')
 local expected=G.snapshot(s);submit(expected,i('move',-1,0));U.keypressed('a');same(expected,s)
 expected=G.snapshot(s);submit(expected,i('move',0,-1));U.keypressed('w');same(expected,s)
 local d=s.card_trial.cards;U.focus=1;U.keypressed('t');assert(s.card_trial.retained[d.hand[1]])
 U.keypressed('g');assert(s.card_trial.resource_phase=='prepare')
end)
test('eight-card hit regions separate Keep, candidate selection, exchange, cash and actions',function()
 local s=fresh();quiet(s);local U=input_fixture(s);local before=G.snapshot(s)
 U.mousepressed(690,150,1);same(before,s);assert(#U.ids()==1)
 U.mousepressed(690,188,1);assert(s.card_trial.retained[s.card_trial.cards.hand[1]])
 U.mousepressed(710,330,1);assert(s.card_trial.exchange_used and s.clock==0 and s.card_trial.stamina==0)
 U.mousepressed(810,295,1);assert(s.card_trial.resource_phase=='spend')
 local old=G.snapshot(s);U.mousepressed(1000,330,1);same(old,s)
end)
test('an aim selected immediately after startup survives the first presentation update',function()
 local s=fresh();quiet(s);fund(s,4);local U=input_fixture(s);local before=G.snapshot(s)
 U.keypressed('x');U.keypressed('right');assert(U.armed=='dash' and U.preview.valid)
 U.update();assert(U.armed=='dash' and U.preview.valid);same(before,s)
end)
test('early Recover clears UI candidate selection even when a selected card is retained',function()
 local s=fresh();quiet(s);local U=input_fixture(s)
 U.keypressed('1');U.keypressed('t');assert(#U.ids()==1)
 U.keypressed('g');assert(#U.ids()==0 and not next(s.card_trial.retained) and s.card_trial.resource_phase=='prepare')
end)
print(('Card Trial: %d passed, %d failed'):format(passed,failed))
if failed>0 then os.exit(1)end
