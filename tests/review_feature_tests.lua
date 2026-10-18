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
end
