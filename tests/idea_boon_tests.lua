-- Prepared fixtures exercise real committed actions, not synthetic proc calls.
return function(test,equal)
    local G=require('src.game')
    local B=require('src.boons')
    local C=require('src.boon_content')
    local I=require('src.idea_boon_content')
    local R=require('src.rewards')
    local Q=require('src.scheduler')
    local V=require('src.visibility')
    local Content=require('src.content')
    local P=require('src.persistence')
    local function grant(s,slug,tier,n)
        G.set_boon_stacks(s,s.player_id,'core:boon/'..slug,tier or 'rare',n or 1)
    end
    local function fixture(slug,hp)
        local s=G.new(37);local p=G.player(s)
        if slug then grant(s,slug)end
        if hp then p.hp=hp end
        return s,p
    end
    local function action(s,kind,dx,dy)
        local ok,why,events=G.submit(s,{kind=kind,dx=dx,dy=dy})
        assert(ok,why);return events
    end
    local function count(events,kind,slug)
        local n=0
        for _,e in ipairs(events)do
            if e.kind==kind and (not slug or e.boon_id=='core:boon/'..slug)then n=n+1 end
        end
        return n
    end
    local function supply(s,kind,x,y)
        return R.spawn_supply(s,kind,s.active_floor_id,x or 3,y or 2,function()end,s.player_id)
    end
    local function money(s)
        return R.spawn_currency(s,2,s.active_floor_id,3,2,function()end,s.player_id)
    end
    local function memory(p,slug)return p.boon_memory and p.boon_memory['core:boon/'..slug]end
    local function burning(slug)
        local s,p=fixture(slug)
        p.position.x,p.position.y=7,6;V.recompute(s)
        local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,0)
        G.set_boon_stacks(s,enemy.id,'core:boon/emberbrand','common',1)
        action(s,'wait');action(s,'wait');assert(p.statuses.burning)
        -- Retire the prepared attacker; only the real scheduled burn remains.
        Q.remove_for_actor(s,enemy.id)
        for n,e in ipairs(s.entities)do if e.id==enemy.id then table.remove(s.entities,n);break end end
        return s,p
    end

    test('idea boons append distinct IDs and native artwork while retaining the old frozen pool',function()
        assert(#I.order>=7 and C.validate() and #C.order==53+#I.order)
        local seen={}
        local old_pool={}
        for n,id in ipairs(C.normal_order())do if n<=49 then old_pool[#old_pool+1]=id end end
        local s=G.new(37,nil,{available_boons=old_pool})
        local before=G.snapshot(s)
        equal(before,G.snapshot(G.restore(before)))
        assert(#s.available_boons==49 and #C.normal_order()==49+#I.order)
        for _,id in ipairs(I.order)do
            assert(not seen[id]);seen[id]=true
            assert(C.get(id).id==id)
            assert(require('src.kenney_symbols').boons[id][1]==C.get(id).art.index)
            for _,old in ipairs(old_pool)do assert(old~=id)end
            for _,tier in ipairs(C.rarities)do
                assert(C.offer_text(id,tier):find(C.get(id).description,1,true))
            end
        end
        assert(C.get('core:boon/beggars_cap').art.index==33)
        assert(C.get('core:boon/overflow_chalice').art.index==824)
    end)
    test('all new supply boons occur in normal offers without entering the starter-only fixture pool',function()
        local found={}
        for seed=1,320 do
            local s=G.new(seed,nil,{available_boons=C.normal_order()})
            for _,offer in ipairs(R.make_offers(s,Content.get_chest('core:chest/standard')))do
                found[offer.boon_id]=true
            end
        end
        for _,id in ipairs(I.order)do assert(found[id],'never offered '..id)end
        local s=G.new(37);assert(#s.available_boons==20)
    end)
    test('Beggar cap grants a timed one-hit ward only for a low-health real gold pickup',function()
        for _,hp in ipairs({6,12,13})do
            local s,p=fixture('beggars_cap',hp);money(s);local rng=s.rng.state
            local events=action(s,'move',1,0)
            assert(s.rng.state==rng and p.hp==hp and s.currency==2)
            if hp<=12 then
                assert(memory(p,'beggars_cap').ward==4 and memory(p,'beggars_cap').expires_at==300)
                assert(count(events,'BoonActivated','beggars_cap')==1)
            else assert(not memory(p,'beggars_cap'))end
        end
    end)
    test('stored wards consume in catalogue order and leave an unused later ward intact',function()
        local s,p=fixture('beggars_cap',6);grant(s,'overflow_chalice');grant(s,'betrayal_ward')
        money(s);supply(s,'food');action(s,'move',1,0)
        p.boon_memory['core:boon/betrayal_ward']={ward=2,expires_at=400}
        p.boon_memory['core:boon/overflow_chalice']={ward=3,expires_at=400}
        assert(B.consume_ward(s,p,1)==1)
        assert(memory(p,'betrayal_ward').ward==0 and memory(p,'beggars_cap').ward==4)
        assert(B.consume_ward(s,p,6)==6)
        assert(memory(p,'beggars_cap').ward==0 and memory(p,'overflow_chalice').ward==0)
        assert(B.consume_ward(s,p,10)==0)
    end)
    test('a real committed enemy impact uses the new ward and keeps damage bookkeeping exact',function()
        local s,p=fixture('beggars_cap',6);money(s);action(s,'move',1,0)
        G.spawn_enemy(s,'core:enemy/mossbound_guard',4,2,s.clock)
        local events=action(s,'wait');local before=p.hp
        for _=1,3 do
            if memory(p,'beggars_cap').ward==0 then break end
            events=action(s,'wait')
        end
        assert(memory(p,'beggars_cap').ward==0 and p.hp==before)
        local got=false
        for _,e in ipairs(events)do
            if e.kind=='DamageBlocked' and e.target_id==p.id then
                assert(e.ward_absorbed>0 and e.actual_loss==0);got=true
            end
        end
        assert(got)
    end)
    test('Overflow Chalice captures only actual wasted healing and a fully used supply gives no ward',function()
        for _,hp in ipairs({10,23,24})do
            local s,p=fixture('overflow_chalice',hp);local drop=supply(s,'food')
            action(s,'move',1,0)
            if hp==23 then assert(drop.collected and p.hp==24 and memory(p,'overflow_chalice').ward==3)
            elseif hp==10 then assert(drop.collected and p.hp==14 and not memory(p,'overflow_chalice'))
            else assert(not drop.collected and not memory(p,'overflow_chalice'))end
        end
    end)
    test('captured healing respects both rarity-stack cap and finite supply waste',function()
        for _,copies in ipairs({1,100})do
            local s,p=fixture(nil,23);grant(s,'overflow_chalice','common',copies)
            supply(s,'food');action(s,'move',1,0)
            assert(memory(p,'overflow_chalice').ward==math.min(2*copies,3))
            assert(B.consume_ward(s,p,100)==math.min(2*copies,3))
            assert(B.consume_ward(s,p,100)==0)
        end
    end)
    test('expired capture cannot block and preview never spends an active ward',function()
        local s,p=fixture('overflow_chalice',23);supply(s,'food');action(s,'move',1,0)
        local before=G.snapshot(s)
        for _=1,20 do G.preview(s,'basic_attack',1,0)end
        equal(before,G.snapshot(s))
        for _=1,4 do action(s,'wait')end
        assert(B.consume_ward(s,p,4)==0 and memory(p,'overflow_chalice').ward==3)
    end)
    test('Road Flask changes future action costs, combines with Bloodrush, and expires on world time',function()
        local s,p=fixture('road_flask',10);supply(s,'food');local rng=s.rng.state
        action(s,'move',1,0)
        assert(s.clock==100 and B.speed_bonus(s,p)==20)
        assert(G.preview(s,'wait').cost==G.effective_cost(100,120))
        grant(s,'bloodrush')
        p.boon_memory['core:boon/bloodrush']={speed=15,expires_at=300}
        assert(B.speed_bonus(s,p)==35)
        while s.clock<=300 do action(s,'wait')end
        assert(B.speed_bonus(s,p)==0 and G.preview(s,'wait').cost==100 and s.rng.state==rng)
    end)
    test('an expired large captured ward cannot be renewed by a smaller new supply waste',function()
        local s,p=fixture('overflow_chalice',23);supply(s,'health');action(s,'move',1,0)
        assert(memory(p,'overflow_chalice').ward==4)
        for _=1,4 do action(s,'wait')end
        p.hp=23;supply(s,'food',2,2);action(s,'move',-1,0)
        assert(memory(p,'overflow_chalice').ward==3 and memory(p,'overflow_chalice').expires_at==900)
    end)
    test('more food refreshes speed without stacking repeat activations, and health is not food',function()
        local s,p=fixture('road_flask',10);supply(s,'food');supply(s,'food')
        action(s,'move',1,0);assert(memory(p,'road_flask').speed==20)
        assert(memory(p,'road_flask').expires_at==300)
        local a,b=fixture('road_flask',10);supply(a,'health');action(a,'move',1,0)
        assert(not memory(b,'road_flask'))
    end)
    test('Field Dressing cleanses real Burning, cancels its pending tick, and caps bonus healing',function()
        local s,p=burning('field_dressing');local schedule_id=p.statuses.burning.schedule_id
        supply(s,'health',6,6);local before=p.hp
        local events=action(s,'move',-1,0)
        assert(not p.statuses.burning and p.hp==math.min(p.max_hp,before+13))
        assert(count(events,'StatusCleansed')==1 and count(events,'StatusTick')==0)
        for _,w in ipairs(s.schedule)do assert(w.id~=schedule_id)end
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('food does not masquerade as a health pickup or cancel a pending burn',function()
        local s,p=burning('field_dressing');p.hp=8;supply(s,'food',6,6)
        local events=action(s,'move',-1,0)
        assert(count(events,'StatusCleansed')==0 and count(events,'Healed')==0)
        assert(count(events,'StatusTick')==1 and p.statuses.burning)
        assert(p.hp==12-C.get('core:boon/emberbrand').parameters.common.power)
    end)
    test('Ration Reserve pays out only once per food and spends a full-health charge too',function()
        local s,p=fixture('ration_reserve',10);supply(s,'food');supply(s,'food')
        action(s,'move',1,0);assert(p.hp==18 and memory(p,'ration_reserve').charges==1)
        local events=action(s,'wait');assert(p.hp==21 and memory(p,'ration_reserve').charges==0)
        assert(count(events,'Healed')==1)
        action(s,'wait');assert(p.hp==21)
        local a,b=fixture('ration_reserve',23);supply(a,'food');action(a,'move',1,0)
        assert(b.hp==24);action(a,'wait');assert(memory(b,'ration_reserve').charges==0 and b.hp==24)
    end)
    test('Ration Reserve expires and health pickups cannot replenish its charge',function()
        local s,p=fixture('ration_reserve',10);supply(s,'food');action(s,'move',1,0)
        for n=1,6 do action(s,'basic_attack',0,1)end
        local hp=p.hp;action(s,'wait');assert(p.hp==hp)
        local a,b=fixture('ration_reserve',10);supply(a,'health');action(a,'move',1,0)
        assert(not memory(b,'ration_reserve'))
    end)
    test('Broker seal requires the full chest price and refunds one real purchase without a pickup proc',function()
        local s,p=fixture('brokers_seal');grant(s,'prospectors_heart')
        p.hp=10
        local chest=G.spawn_chest(s,'core:chest/standard',3,2)
        chest.offers={{boon_id='core:boon/opening_cut',rarity='common'},
            {boon_id='core:boon/thorn_mirror',rarity='common'},
            {boon_id='core:boon/longblade_geometry',rarity='common'}}
        s.currency=chest.price-1
        local before=G.snapshot(s)
        assert(not G.submit(s,{kind='claim_chest',chest_id=chest.id,offer_index=1}))
        equal(before,G.snapshot(s))
        s.currency=chest.price
        local ok,why,events=G.submit(s,{kind='claim_chest',chest_id=chest.id,offer_index=1})
        assert(ok,why)
        assert(s.currency==math.min(chest.price,3) and chest.claimed and p.hp==10)
        assert(count(events,'CurrencyRefunded')==1 and count(events,'CurrencyCollected')==0)
        before=G.snapshot(s)
        assert(not G.submit(s,{kind='claim_chest',chest_id=chest.id,offer_index=2}))
        equal(before,G.snapshot(s))
    end)
    test('Broker refund is capped by price even with uncapped mixed rarity copies',function()
        local s=fixture();grant(s,'brokers_seal','common',100)
        local chest=G.spawn_chest(s,'core:chest/standard',3,2);s.currency=chest.price
        local ok,why=G.submit(s,{kind='claim_chest',chest_id=chest.id,offer_index=1});assert(ok,why)
        assert(s.currency==chest.price)
    end)
    test('Funeral Coin enriches exactly the finite killed foe drop and requires a later pickup',function()
        local s=fixture('funeral_coin')
        local foe=G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,100000);foe.hp=1
        local events=action(s,'basic_attack',1,0)
        local drop
        for _,d in ipairs(s.drops)do if d.kind=='currency' and d.killed_entity_id==foe.id then assert(not drop);drop=d end end
        local base=Content.get_enemy(foe.definition_id).currency_drop
        assert(drop and drop.amount==base+3 and s.currency==0)
        assert(count(events,'CurrencyDropEnriched')==1 and count(events,'EntityKilled')==1)
        action(s,'move',1,0);assert(drop.collected and s.currency==base+3)
        action(s,'move',-1,0);action(s,'move',1,0);assert(s.currency==base+3)
    end)
    test('reward-exempt revived or called foes cannot farm Funeral Coin gold',function()
        local s=fixture('funeral_coin')
        local foe=G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,100000)
        foe.hp=1;foe.reward_exempt=true
        local events=action(s,'basic_attack',1,0)
        assert(count(events,'CurrencyDropEnriched')==0 and #s.drops==0)
        assert(count(events,'BoonActivated','funeral_coin')==0)
    end)
    test('new supply state and pending Burning/Echo resume with exact events, RNG and scheduled costs',function()
        local s,p=fixture(nil,20)
        for _,slug in ipairs({'road_flask','ration_reserve','overflow_chalice','temporal_echo','emberbrand'})do grant(s,slug)end
        G.spawn_enemy(s,'core:enemy/mossbound_guard',4,2,100000)
        supply(s,'health');action(s,'move',1,0)
        p.hp=20;supply(s,'food',2,2);action(s,'move',-1,0);action(s,'move',1,0)
        action(s,'basic_attack',1,0)
        assert(next(s.delayed_effects) and memory(p,'road_flask').speed==20)
        local restored=G.restore(P.decode(P.encode(G.snapshot(s))))
        for _,intent in ipairs({{kind='wait'},{kind='basic_attack',dx=1,dy=0},
            {kind='move',dx=-1,dy=0},{kind='wait'},{kind='move',dx=1,dy=0}})do
            local a,aw,ae=G.submit(s,intent);local b,bw,be=G.submit(restored,intent)
            assert(a and b,aw or bw);equal(ae,be);equal(G.snapshot(s),G.snapshot(restored))
        end
    end)
    test('invalid actions and repeated previews neither consume supply memory nor advance RNG',function()
        local s=fixture('ration_reserve',10);grant(s,'overflow_chalice');grant(s,'road_flask')
        supply(s,'food');action(s,'move',1,0)
        local before=G.snapshot(s)
        for _=1,10 do
            G.preview(s,'wait');G.preview(s,'basic_attack',1,0)
            assert(not G.submit(s,{kind='move',dx=1,dy=1}))
            assert(not G.submit(s,{kind='cancel'}))
        end
        equal(before,G.snapshot(s))
    end)
    test('new supply predicates, art and effect scales reject malformed authored data',function()
        local function rejects(change)
            local def=P.decode(P.encode(C.get('core:boon/road_flask')))
            change(def)
            assert(not pcall(C.validate,{[def.id]=def},{def.id}))
        end
        rejects(function(d)d.art.index=1078 end)
        rejects(function(d)d.predicate={op='supply_kind',value='gold'}end)
        rejects(function(d)d.predicate={op='event_amount',field='secret_raw_state',min=1}end)
        rejects(function(d)d.effects[1].scale='supply_excess'end)
        rejects(function(d)d.effects={{kind='cleanse_owner',status='any'}}end)
    end)
end
