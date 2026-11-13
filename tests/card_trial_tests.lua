-- Focused authoritative boundaries; all fixtures are in memory, no player files.
package.path='./?.lua;./?/init.lua;'..package.path
local Game=require('src.game')
local Trial=require('src.card_trial')
local Cards=require('src.cards')
local RNG=require('src.rng')
local Scheduler=require('src.scheduler')
local Codec=require('src.persistence')
local Boons=require('src.boons')
local passed,failed=0,0
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'states differ')end
local function test(name,fn)
    local ok,why=pcall(fn)
    if ok then passed=passed+1;print('PASS '..name)
    else failed=failed+1;io.stderr:write('FAIL '..name..': '..tostring(why)..'\n')end
end
local function fresh(preset)return Trial.new(Game,101,preset or 'bare')end
local function hand(s,ids)
    local deck=s.card_trial.cards;deck.hand={};deck.draw={};deck.discard={}
    local seen={};for i,id in ipairs(ids)do deck.hand[i]=id;seen[id]=true end
    for id=1,52 do if not seen[id]then deck.draw[#deck.draw+1]=id end end
    assert(Cards.validate(deck))
end
local function quiet(s)
    for _,e in ipairs(s.entities)do if e.controller=='ai'then Scheduler.remove_for_actor(s,e.id)end end
end
local function action(s,a,ids,dx,dy)
    return {kind='card_action',action=a,cards=ids or {s.card_trial.cards.hand[1]},dx=dx,dy=dy}
end
local function submit(s,intent)
    local ok,why,events=Game.submit(s,intent);assert(ok,why);return events
end
local function count(events,kind,boon)
    local n=0;for _,e in ipairs(events)do if e.kind==kind and (not boon or e.boon_id=='core:boon/'..boon)then n=n+1 end end;return n
end
local function unchanged(s,intent)
    local before=Game.snapshot(s);local ok=Game.submit(s,intent);assert(not ok);same(before,s)
end
local function near(s,positions)
    local n=0
    for _,e in ipairs(s.entities)do if e.controller=='ai'then
        n=n+1
        if positions[n]then e.position.x,e.position.y=unpack(positions[n])
        else e.hp=0;e.blocks_movement=false end
        Scheduler.remove_for_actor(s,e.id)
    end end
    for i=#s.entities,1,-1 do
        if s.entities[i].controller=='ai' and s.entities[i].hp<=0 then table.remove(s.entities,i)end
    end
end

test('standard deck has 52 stable physical identities and five opening cards',function()
    local s=fresh();assert(Cards.validate(s.card_trial.cards));assert(#s.card_trial.cards.draw==47)
    local ranks,suits={},{}
    for id=1,52 do local c=Cards.card(id);ranks[c.rank]=(ranks[c.rank]or 0)+1;suits[c.suit]=(suits[c.suit]or 0)+1 end
    for i=1,13 do assert(ranks[i]==4)end;for i=1,4 do assert(suits[i]==13)end
end)
test('single spending refills only its slot and retains all other physical cards',function()
    local s=fresh();quiet(s);local d=s.card_trial.cards;local old=Game.snapshot(d)
    local ev=submit(s,action(s,'move',{old.hand[2]},-1,0))
    for i=1,5 do if i~=2 then assert(d.hand[i]==old.hand[i])end end
    assert(d.hand[2]==old.draw[#old.draw] and d.discard[1]==old.hand[2])
    assert(#d.draw==46 and #d.discard==1 and s.clock==100 and count(ev,'Moved')==1)
    assert(count(ev,'CardTechniqueCommitted')==1 and s.card_trial.commits==1 and Cards.validate(d))
end)
test('pair dash and sweep each pay two cards and one existing action cost',function()
    for _,a in ipairs({'move','attack'})do
        local s=fresh();quiet(s);hand(s,{1,14,3,4,5})
        local ev=submit(s,action(s,a,{1,14},-1,0))
        assert(s.clock==(a=='move' and 130 or 150))
        assert(s.last_action.kind==(a=='move' and 'dash' or 'sweeping_slash'))
        assert(#s.card_trial.cards.discard==2 and s.card_trial.cards.hand[3]==3)
        assert(count(ev,'CardTechniqueCommitted')==1 and Cards.validate(s.card_trial.cards))
        if a=='move'then assert(Game.player(s).position.x==4)end
    end
end)
test('discard exhaustion conserves every card and retained cards never enter reshuffle',function()
    local d=Cards.new(202);local retained=d.hand[5];local reshuffles=0
    for _=1,180 do
        local selection=assert(Cards.selection(d,{d.hand[1],d.hand[2],d.hand[3],d.hand[4]}))
        local before=#d.draw;Cards.spend(d,selection)
        if before<4 then reshuffles=reshuffles+1 end
        assert(d.hand[5]==retained and Cards.validate(d))
        for _,id in ipairs(d.draw)do assert(id~=retained)end
        for _,id in ipairs(d.discard)do assert(id~=retained)end
    end
    assert(reshuffles>10)
end)
test('card RNG is deterministic and independent of unrelated combat RNG draws',function()
    local a,b=fresh(),fresh();quiet(a);quiet(b)
    for _=1,100 do
        for _=1,7 do RNG.next_raw(b.rng)end
        submit(a,action(a,'redraw'));submit(b,action(b,'redraw'))
        same(a.card_trial.cards,b.card_trial.cards)
    end
end)
test('preview, invalid recipes, forged ownership and raw actions have no side effects',function()
    local s=fresh();hand(s,{1,14,3,4,5})
    local cases={
        action(s,'attack',{}),action(s,'attack',{1,3},1,0),action(s,'attack',{1,14,3},1,0),
        action(s,'move',{1,1},1,0),action(s,'move',{52},1,0),action(s,'move',{1},1,1),
        action(s,'attack',{1},2,0),action(s,'nothing',{1}),action(s,'redraw',{}),
        {kind='move',dx=1,dy=0},{kind='basic_attack',dx=1,dy=0},{kind='dash',dx=1,dy=0},
        {kind='sweeping_slash',dx=1,dy=0},{kind='wait'},{kind='use_utility'},
        {kind='interact'},{kind='claim_chest'},{kind='choose_route'},
    }
    for _,intent in ipairs(cases)do unchanged(s,intent)end
    local before=Game.snapshot(s)
    for _=1,50 do Game.card_preview(s,action(s,'move',{1,14},-1,0))end
    same(before,s)
end)
test('blocked movement and Move bump cannot spend cards or turn boots into a weapon',function()
    local s=fresh();Game.player(s).position.x=2;unchanged(s,action(s,'move',nil,-1,0))
    Game.player(s).position.x,Game.player(s).position.y=6,7
    unchanged(s,action(s,'move',nil,0,-1)) -- central wall
    Game.player(s).position.x,Game.player(s).position.y=4,6
    unchanged(s,action(s,'move',nil,0,-1)) -- live guard
end)
test('healing uses 4/8 HP, one charge and Wait-equivalent time but no Wait or pickup events',function()
    local s=fresh();quiet(s);hand(s,{1,14,3,4,5});local p=Game.player(s);p.hp=8
    Boons.set_stacks(p,'core:boon/crosswind','rare',1)
    Boons.set_stacks(p,'core:boon/prospectors_heart','rare',1)
    local ev=submit(s,action(s,'heal',{3}));assert(p.hp==12 and s.clock==100)
    local ev2=submit(s,action(s,'heal',{1,14}));assert(p.hp==20 and s.clock==200)
    assert(s.card_trial.healing_charges==0)
    for _,events in ipairs({ev,ev2})do
        assert(count(events,'Healed')==1 and count(events,'Waited')==0)
        assert(count(events,'SupplyCollected')==0 and count(events,'CurrencyCollected')==0)
        assert(count(events,'BoonActivated')==0 and count(events,'CardTechniqueCommitted')==1)
        for _,e in ipairs(events)do if e.kind=='Healed'then assert(e.healing_source=='trial:equipment/healing_flask')end end
    end
    unchanged(s,action(s,'heal'))
end)
test('full-health healing is invalid and near-full healing caps without a free charge',function()
    local s=fresh();quiet(s);unchanged(s,action(s,'heal'))
    Game.player(s).hp=23;submit(s,action(s,'heal'));assert(Game.player(s).hp==24 and s.card_trial.healing_charges==1)
end)
test('redraw pays exactly one real Wait and retains unselected slots',function()
    local s=fresh();quiet(s);hand(s,{1,14,3,4,5})
    Boons.set_stacks(Game.player(s),'core:boon/crosswind','rare',1)
    local ev=submit(s,action(s,'redraw',{1,3,5}))
    assert(s.clock==100 and count(ev,'Waited')==1 and count(ev,'BoonActivated','crosswind')==1)
    assert(s.card_trial.cards.hand[2]==14 and s.card_trial.cards.hand[4]==4)
    assert(#s.card_trial.cards.discard==3 and s.card_trial.healing_charges==2)
end)
test('speed scaling and enemy multi-phase scheduler still operate once per technique',function()
    local s=fresh();Game.player(s).speed=200
    local ev=submit(s,action(s,'move',nil,-1,0))
    assert(s.clock==50 and s.last_action.cost==50 and count(ev,'CardTechniqueCommitted')==1)
    local enemy_moved=0;for _,e in ipairs(ev)do if e.kind=='Moved' and e.source_id~=s.player_id then enemy_moved=enemy_moved+1 end end
    assert(enemy_moved>0)
end)
test('Movement preset primes Rhythm from card movement and spends it on a card weapon hit',function()
    local s=fresh('movement');quiet(s)
    submit(s,action(s,'move',nil,-1,0))
    near(s,{{5,6},{8,3},{9,3}})
    submit(s,action(s,'move',nil,0,-1))
    local ev=submit(s,action(s,'attack',nil,0,-1))
    assert(count(ev,'BoonActivated','rhythm_of_steel')==1)
    local extra=false;for _,e in ipairs(ev)do if e.kind=='DamageTaken' and not e.direct_attack and e.actual_loss>0 then extra=true end end
    assert(extra)
end)
test('Ring of Reversal marks an escaped real locked attack and adds to the next primary hit',function()
    local s=fresh('movement');near(s,{{7,8}})
    local foe=s.entities[2]
    -- Use a real AI windup during the first paid action, then escape its cells.
    Scheduler.enqueue(s,{kind='actor_ready',due=0,actor_id=foe.id,is_player=false})
    submit(s,action(s,'redraw'))
    assert(foe.pending_attack)
    local ev=submit(s,action(s,'move',nil,0,1))
    assert(count(ev,'RingReversalPrimed')==1)
    local ev2=submit(s,action(s,'attack',nil,1,-1))
    assert(count(ev2,'RingReversalSpent')==1)
end)
test('Burning preset preserves direct identity, scheduled ticks and Shared Agony between victims',function()
    local s=fresh('burning');hand(s,{1,14,3,4,5});near(s,{{5,7},{6,7}})
    local ev=submit(s,action(s,'attack',{1,14},0,-1))
    assert(count(ev,'AttackPerformed')==1 and count(ev,'StatusApplied')==2)
    assert(count(ev,'StatusTick')>=2 and count(ev,'BoonActivated','shared_agony')>=1)
    assert(s.progression.level==1 and s.progression.xp_total==0 and s.currency==0 and #s.drops==0)
end)
local function clear_first(s)
    near(s,{{7,8}});local foe=s.entities[2];foe.hp=1
    return submit(s,action(s,'attack',nil,1,0))
end
test('reward is fixed, paid once through real chest acquisition, and all trial resources carry',function()
    local s=fresh('movement');local p=Game.player(s);p.hp=15
    s.card_trial.healing_charges=1
    p.boon_memory={['core:boon/rhythm_of_steel']={charges=1,expires_at=400}}
    Boons.set_stacks(p,'core:boon/temporal_echo','rare',1)
    clear_first(s);assert(s.card_trial.phase=='reward')
    assert(next(s.delayed_effects))
    unchanged(s,action(s,'move',nil,0,1));unchanged(s,action(s,'heal'));unchanged(s,action(s,'redraw'))
    local before=Game.snapshot(s);local chest=assert(Trial.reward(s,1))
    same(chest.offers,Trial.content.rewards);unchanged(s,{kind='trial_reward',offer_index=4})
    local ev=submit(s,{kind='trial_reward',offer_index=2})
    assert(count(ev,'BoonGranted')==1 and count(ev,'ChestClaimed')==1)
    assert(p.boons['core:boon/detonation_bloom'].rare==1 and s.card_trial.fight==2)
    assert(s.card_trial.healing_charges==1 and p.hp==15 and s.clock==before.clock)
    same(before.card_trial.cards,s.card_trial.cards);same(before.delayed_effects,s.delayed_effects)
    assert(p.boon_memory['core:boon/rhythm_of_steel'])
    unchanged(s,{kind='trial_reward',offer_index=1})
end)
test('simultaneous lethal scheduled work wins over first-fight reward',function()
    local s=fresh();near(s,{{7,8}});s.entities[2].hp=1;Game.player(s).hp=1
    local work=Scheduler.enqueue(s,{kind='effect',due=100,actor_id=0,attack_id=99})
    work.effect_id=work.id
    s.delayed_effects[work.id]={activation={id=99,owner_id=999,source_faction='enemy',power=1,
        source_position={floor_id=s.active_floor_id,x=8,y=8},chain_id=99,root_action_id=99,ancestry={},
        event={target_cells={{floor_id=s.active_floor_id,x=6,y=8}}}},
        effect={kind='echo_impact',damage_type='physical'}}
    submit(s,action(s,'attack',nil,1,0))
    assert(s.game_over and s.card_trial.phase=='death' and not s.card_trial.reward_id)
end)
test('victory locks empty arena and replay resets the complete trial deterministically',function()
    local s=fresh('burning');clear_first(s);submit(s,{kind='trial_reward',offer_index=3})
    near(s,{{7,8}})
    local foe
    for _,e in ipairs(s.entities)do if e.controller=='ai' and e.hp>0 then foe=e;break end end
    foe.hp=1;submit(s,action(s,'attack',nil,1,0))
    assert(s.game_over and s.card_trial.phase=='victory')
    unchanged(s,action(s,'redraw'));same(fresh('burning'),fresh('burning'))
end)
test('normal run and profile stores receive no writes, including terminal trial saves',function()
    local writes=0;local store={read=function()return nil end,write=function()writes=writes+1;return true end}
    local service=Codec.new(store);local s=fresh()
    assert(not service:save(s) and not service:replace(s))
    s.game_over=true;assert(not service:save(s) and not service:pending_terminal(s))
    assert(writes==0 and not s.meta.eligible and not next(s.meta.pending))
    assert(not pcall(Game.restore,s))
end)
test('classic raw movement, attacks and Wait still work without a card trial',function()
    local s=Game.new(101)
    for _,i in ipairs({{kind='move',dx=1,dy=0},{kind='basic_attack',dx=1,dy=0},{kind='wait'}})do submit(s,i)end
    assert(s.clock==300 and not s.card_trial)
end)
print(('Card Trial: %d passed, %d failed'):format(passed,failed))
os.exit(failed==0 and 0 or 1)
