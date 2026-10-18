return function(test,equal)
    local Game=require('src.game')
    local Content=require('src.content')
    local function feature(state,kind)
        for _,f in ipairs(state.review_features) do
            if f.kind==kind and f.floor_id==state.active_floor_id then return f end
        end
    end
    local function entity(state,kind)
        for _,e in ipairs(state.entities) do
            if e.controller==kind and e.position.floor_id==state.active_floor_id then return e end
        end
    end
    local function near(state,x,y,target_kind)
        local player=Game.player(state)
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
            local px,py=x-d[1],y-d[2]
            if Content.walkable(Content.map_for(state),px,py) then
                player.position.x,player.position.y=px,py
                local preview=Game.preview(state,'move',d[1],d[2])
                if preview.valid and (not target_kind or preview.vault_id) then
                    return d
                end
            end
        end
        error('no reachable neighbor for review object')
    end
    local function has(events,kind)
        for _,e in ipairs(events) do if e.kind==kind then return e end end
    end

    test('optional vault key, four outcomes and exact saved state',function()
        for _,case in ipairs({{1,'clue'},{2,'cache'},{3,'ambush'},{9,'boon'}}) do
            local state=Game.new_stage(case[1])
            local key=assert(feature(state,'key'))
            local vault=assert(entity(state,'review_vault'))
            local d=near(state,key.x,key.y)
            local ok,reason,events=Game.submit(state,{kind='move',dx=d[1],dy=d[2]})
            assert(ok,reason)
            assert(has(events,'VaultKeyCollected') and state.review_keys[state.active_floor_id])
            equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
            d=near(state,vault.position.x,vault.position.y,true)
            ok,reason,events=Game.submit(state,{kind='move',dx=d[1],dy=d[2]})
            assert(ok,reason)
            local opened=assert(has(events,'VaultOpened'))
            assert(opened.outcome==case[2] and vault.opened
                and not state.review_keys[state.active_floor_id])
            if case[2]=='boon' then
                local reward=false
                for _,e in ipairs(state.entities) do
                    if e.controller=='chest' and e.price==0
                        and e.position.x==vault.position.x and
                        e.position.y==vault.position.y then reward=true end
                end
                assert(reward,'boon vault did not provide a free chest')
            elseif case[2]=='ambush' then
                local ambush=false
                for _,e in ipairs(state.entities) do
                    if e.controller=='ai' and math.abs(e.position.x-vault.position.x)
                        +math.abs(e.position.y-vault.position.y)==1 then ambush=true end
                end
                assert(ambush,'ambush vault had no enemy')
            else
                local currency=false
                for _,drop in ipairs(state.drops) do
                    if drop.kind=='currency' and drop.position.x==vault.position.x
                        and drop.position.y==vault.position.y then currency=true end
                end
                assert(currency,'reward vault had no currency')
            end
            equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
        end
    end)

    test('closed review objects leave the required elite route reachable',function()
        for seed=1,20 do
            local state=Game.new_stage(seed)
            local map=state.generated_map
            local blocked={}
            for _,e in ipairs(state.entities) do
                if e.blocks_movement and e.controller:match('^review_') then
                    blocked[e.position.x..','..e.position.y]=true
                end
            end
            local queue={{x=map.start_x,y=map.start_y}}
            local seen={[map.start_x..','..map.start_y]=true}
            local head=1
            while head<=#queue do
                local p=queue[head];head=head+1
                for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
                    local x,y=p.x+d[1],p.y+d[2]
                    local k=x..','..y
                    if not seen[k] and not blocked[k] and
                        Content.walkable(map,x,y) then
                        seen[k]=true;queue[#queue+1]={x=x,y=y}
                    end
                end
            end
            assert(seen[map.elite.x..','..map.elite.y],
                'review objects blocked the main route for seed '..seed)
            local baseline={{x=map.start_x,y=map.start_y}}
            local naturally_seen={[map.start_x..','..map.start_y]=true}
            local base_head=1
            while base_head<=#baseline do
                local p=baseline[base_head];base_head=base_head+1
                for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
                    local x,y=p.x+d[1],p.y+d[2]
                    local k=x..','..y
                    if not naturally_seen[k] and Content.walkable(map,x,y) then
                        naturally_seen[k]=true
                        baseline[#baseline+1]={x=x,y=y}
                    end
                end
            end
            for cell in pairs(naturally_seen) do
                assert(blocked[cell] or seen[cell],
                    'review objects isolated a reachable tile for seed '..seed)
            end
            local plate=feature(state,'plate')
            if plate then
                assert(seen[plate.x..','..plate.y],
                    'closed gate hides its own switch for seed '..seed)
            end
        end
    end)

    test('plate opens a timed gate and expiration survives Continue',function()
        local state=Game.new_stage(3)
        local plate=assert(feature(state,'plate'))
        local gate=assert(Game.entity(state,plate.gate_id))
        assert(gate.controller=='review_gate' and not gate.open)
        local d=near(state,plate.x,plate.y)
        local ok,reason,events=Game.submit(state,{kind='move',dx=d[1],dy=d[2]})
        assert(ok,reason)
        assert(has(events,'TimedGateOpened') and gate.open
            and gate.closes_at>state.clock)
        state=Game.restore(Game.snapshot(state));gate=Game.entity(state,plate.gate_id)
        for _=1,4 do Game.submit(state,{kind='wait'}) end
        assert(not gate.open and gate.blocks_movement and gate.closes_at==nil)
    end)

    test('ore, ward and scenery placement retain their independent states',function()
        local state=Game.new_stage(3)
        local ore=assert(feature(state,'ore'))
        local player=Game.player(state)
        local d=near(state,ore.x,ore.y)
        local ok,reason,events=Game.submit(state,{kind='basic_attack',dx=d[1],dy=d[2]})
        assert(ok,reason)
        assert(ore.broken and has(events,'OreBroken'))
        local ward=assert(feature(state,'ward'))
        d=near(state,ward.x,ward.y)
        ok,reason,events=Game.submit(state,{kind='move',dx=d[1],dy=d[2]})
        assert(ok,reason)
        assert(ward.collected and has(events,'WardFlaskCollected')
            and player.pickup_ward_actions==3)
        for _=1,3 do Game.submit(state,{kind='wait'}) end
        assert(player.pickup_ward_actions==0)
        equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
    end)

    test('new class actions use the committed attack and movement scheduler',function()
        local classes={'shade','hookrunner','trapper'}
        for _,name in ipairs(classes) do
            local state=Game.new(7,'core:map/test_room',
                {class_id='core:actor/'..name})
            local preview=Game.preview(state,'sweeping_slash',1,0)
            assert(preview.valid)
            local ok,reason,events=Game.submit(state,
                {kind='sweeping_slash',dx=1,dy=0})
            assert(ok,reason)
            if name=='shade' then
                assert(has(events,'DecoyPlaced') and state.class_special.decoy)
                assert(Game.preview(state,'dash',1,0).swap)
                assert(Game.submit(state,{kind='dash',dx=1,dy=0}))
                assert(not state.class_special.decoy)
            elseif name=='trapper' then
                assert(has(events,'SnarePlaced') and state.class_special.snare)
            else
                assert(has(events,'AttackPerformed'))
            end
            equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
        end
    end)

    test('hook pulls an ordinary foe while a snare deals damage and skips one move',function()
        local Scheduler=require('src.scheduler')
        local hook=Game.new(7,'core:map/test_room',
            {class_id='core:actor/hookrunner'})
        local pulled=Game.spawn_enemy(hook,'core:enemy/mossbound_guard',5,2)
        Scheduler.remove_for_actor(hook,pulled.id)
        local ok,reason,events=Game.submit(hook,
            {kind='sweeping_slash',dx=1,dy=0})
        assert(ok,reason)
        assert(pulled.position.x==3 and pulled.position.y==2
            and has(events,'Displaced') and pulled.hp<pulled.max_hp)

        local trap=Game.new(7,'core:map/test_room',
            {class_id='core:actor/trapper'})
        local victim=Game.spawn_enemy(trap,'core:enemy/mossbound_guard',5,2)
        assert(Game.submit(trap,{kind='sweeping_slash',dx=1,dy=0}))
        ok,reason,events=Game.submit(trap,{kind='wait'})
        assert(ok,reason)
        assert(has(events,'SnareTriggered') and victim.position.x==3
            and victim.hp<victim.max_hp and victim.skip_next_turn)
        ok,reason,events=Game.submit(trap,{kind='wait'})
        assert(ok,reason)
        assert(has(events,'SnareHeld') and victim.position.x==3
            and not victim.skip_next_turn)
    end)

    test('ward flask consumes one entire hostile hit before ordinary defense',function()
        local state=Game.new(7)
        local player=Game.player(state)
        player.position.x,player.position.y=8,5
        player.pickup_ward_actions=4
        Game.spawn_enemy(state,'core:enemy/thornspitter',8,2)
        local hp=player.hp
        local ok,reason,events=Game.submit(state,{kind='wait'})
        assert(ok,reason)
        assert(has(events,'WardFlaskBlocked') and player.hp==hp
            and player.pickup_ward_actions==0)
        equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
    end)

    test('Ring of Reversal primes on escaped windup and spends on that attacker',function()
        local state=Game.new(37)
        local player=Game.player(state)
        player.position.x,player.position.y=8,5
        player.speed=300
        Game.set_boon_stacks(state,player.id,'core:boon/ring_of_reversal','rare',1)
        local attacker=Game.spawn_enemy(state,'core:enemy/thornspitter',8,2)
        attacker.hp,attacker.max_hp=100,100
        assert(Game.submit(state,{kind='wait'}))
        assert(attacker.pending_attack)
        local ok,reason,events=Game.submit(state,{kind='move',dx=-1,dy=0})
        assert(ok,reason)
        assert(has(events,'RingReversalPrimed') and
            player.reversal_target_id==attacker.id)
        player.position.x,player.position.y=8,3
        ok,reason,events=Game.submit(state,{kind='basic_attack',dx=0,dy=-1})
        assert(ok,reason)
        assert(has(events,'RingReversalSpent') and attacker.hp==92
            and not player.reversal_target_id)
        equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
    end)

    test('lit brazier clears nearby mist sight only until its clock deadline',function()
        local map=Content.get_map('core:map/test_room')
        local rows={}
        for i,row in ipairs(map.rows) do rows[i]=row end
        map=setmetatable({rows=rows},{__index=map})
        map.rows[5]=map.rows[5]:sub(1,7)..'m'..map.rows[5]:sub(9)
        local state={clock=100,review_features={{kind='brazier',
            floor_id=map.floor_id,x=7,y=5,lit_until=300}},entities={}}
        assert(not Content.blocks_vision(map,8,5,state))
        state.clock=300
        assert(Content.blocks_vision(map,8,5,state))
    end)
end
