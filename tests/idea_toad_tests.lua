return function(test,equal)
    local G=require('src.game')
    local AI=require('src.ai')
    local Rewards=require('src.rewards')
    local Codec=require('src.persistence')
    local function trial(kind)
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=8,6
        local toad=G.spawn_enemy(s,'core:enemy/gulp_toad',10,6,50);toad.hp=3
        local drop
        if kind=='currency' then drop=Rewards.spawn_currency(s,7,s.active_floor_id,9,6,function()end)
        else drop=Rewards.spawn_supply(s,kind,s.active_floor_id,9,6,function()end)end
        return s,p,toad,drop
    end
    test('Toad chooses a locked visible drop without consuming RNG or mutating the world',function()
        local s,p,t,d=trial('currency');local before=G.snapshot(s)
        local intent=AI.choose(s,t);equal(before,G.snapshot(s))
        assert(intent.support.kind=='gulp' and intent.support.drop_id==d.id and intent.cells[1].x==9)
        assert(G.submit(s,{kind='wait'}));assert(t.pending_attack.due==175 and not d.carried_by)
        assert(G.submit(s,{kind='wait'}))
        assert(t.carried_drop_id==d.id and d.carried_by==t.id and not d.collected and s.currency==0)
        assert(not AI.choose(s,t).support,'one toad cannot swallow a second drop')
    end)
    test('intercepting a committed coin pickup cancels swallowing without creating replacement loot',function()
        local s,p,t,d=trial('currency');assert(G.submit(s,{kind='wait'}))
        local _,_,events=G.submit(s,{kind='move',dx=1,dy=0})
        assert(d.collected and s.currency==7 and not t.carried_drop_id and #s.drops==1)
        local blocked=false;for _,e in ipairs(events)do if e.kind=='GulpBlocked'then blocked=true end end
        assert(blocked)
    end)
    test('standing over an unneeded ration protects it without consuming the food',function()
        local s,p,t,d=trial('food');assert(p.hp==p.max_hp)
        assert(G.submit(s,{kind='wait'}))
        assert(G.submit(s,{kind='move',dx=1,dy=0}))
        assert(p.position.x==9 and not d.collected and not d.carried_by and not t.carried_drop_id)
        assert(AI.choose(s,t).support==nil,'guarded food is not loose reachable loot')
    end)
    test('swallowed coins and supplies retain their original ID and amount until a real kill releases them',function()
        for _,kind in ipairs({'currency','food','health'})do
            local s,p,t,d=trial(kind);p.hp=1
            assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}))
            assert(G.submit(s,{kind='move',dx=1,dy=0}))
            assert(not d.collected and s.currency==0 and p.hp==1)
            local id,amount=d.id,d.amount
            local _,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0})
            assert(not G.entity(s,t.id) and d.id==id and d.amount==amount
                and not d.carried_by and not d.collected and d.position.x==10)
            local released=0;for _,e in ipairs(events)do if e.kind=='DropRegurgitated'then released=released+1 end end
            assert(released==1)
            assert(G.submit(s,{kind='move',dx=1,dy=0}));assert(d.collected)
            if kind=='currency'then assert(s.currency==10)
            else assert(p.hp==1+amount and s.currency==3)end
            local hp,gold=p.hp,s.currency
            assert(G.submit(s,{kind='wait'}));assert(p.hp==hp and s.currency==gold)
        end
    end)
    test('a saved gulp and saved cargo each resume with exact drop, reward and RNG state',function()
        local s,p,t,d=trial('currency');assert(G.submit(s,{kind='wait'}))
        local b=G.restore(Codec.decode(Codec.encode(G.snapshot(s))))
        local _,_,aev=G.submit(s,{kind='wait'});local _,_,bev=G.submit(b,{kind='wait'})
        equal(aev,bev);equal(G.snapshot(s),G.snapshot(b))
        b=G.restore(Codec.decode(Codec.encode(G.snapshot(s))))
        for _,intent in ipairs({{kind='move',dx=1,dy=0},{kind='basic_attack',dx=1,dy=0},{kind='move',dx=1,dy=0}})do
            local _,_,a=G.submit(s,intent);local _,_,c=G.submit(b,intent);equal(a,c)
        end
        equal(G.snapshot(s),G.snapshot(b))
    end)
    test('missing, duplicated or falsely collected cargo is rejected without changing source snapshots',function()
        local s=trial('currency');assert(G.submit(s,{kind='wait'}));assert(G.submit(s,{kind='wait'}))
        local snapshot=G.snapshot(s)
        snapshot.drops[1].collected=true;assert(not pcall(G.restore,snapshot))
        snapshot=G.snapshot(s);snapshot.drops={};assert(not pcall(G.restore,snapshot))
        snapshot=G.snapshot(s);snapshot.drops[1].carried_by=s.player_id;assert(not pcall(G.restore,snapshot))
        assert(G.restore(G.snapshot(s)))
    end)
    test('two committed gulps cannot duplicate one drop and natural generation uses the approved native body',function()
        local s,p,first,drop=trial('currency')
        local second=G.spawn_enemy(s,'core:enemy/gulp_toad',9,7,50)
        assert(G.submit(s,{kind='wait'}))
        local _,_,events=G.submit(s,{kind='wait'})
        assert(drop.carried_by==first.id and first.carried_drop_id==drop.id and not second.carried_drop_id)
        local taken,blocked=0,0
        for _,e in ipairs(events)do
            if e.kind=='DropSwallowed'then taken=taken+1 elseif e.kind=='GulpBlocked'then blocked=blocked+1 end
        end
        assert(taken==1 and blocked==1 and G.restore(G.snapshot(s)))
        local found=false
        for seed=1,30 do
            local stage=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(stage.entities)do if e.definition_id=='core:enemy/gulp_toad'then found=true end end
            equal(G.snapshot(stage),G.snapshot(G.restore(G.snapshot(stage))))
        end
        assert(found)
        local audit=require('src.sprite_audit').collect()
        for _,pack in ipairs({'onebit'})do
            assert(#audit.packs[pack].missing==0)
            local bound=false
            for _,h in ipairs(audit.packs[pack].bindings)do if h.id=='core:enemy/gulp_toad'then
                assert(h.index==422);bound=true
            end end
            assert(bound)
        end
    end)
end
