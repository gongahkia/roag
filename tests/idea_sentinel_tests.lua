return function(test,equal)
    local G=require('src.game');local C=require('src.persistence')
    local S=require('src.idea_sentinel');local AI=require('src.ai')
    local function trial()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,S.id,8,6,50)
        return s,p,e
    end
    local function wait(s,n)
        local all={}
        for _=1,n do local ok,_,events=G.submit(s,{kind='wait'});assert(ok)
            for _,v in ipairs(events)do all[#all+1]=v end
        end
        return all
    end
    test('Sentinel preview is pure and its first quarter remains locked after the player leaves',function()
        local s,p,e=trial();local before=G.snapshot(s)
        for _=1,30 do local aim=AI.choose(s,e)
            assert(aim.sentinel_facing==4 and #aim.cells==3)
        end
        equal(before,G.snapshot(s));wait(s,1)
        local cells=C.encode(e.pending_attack.target_cells)
        assert(e.pending_attack.due==250 and e.ai_memory.sentinel_strikes==1)
        assert(G.submit(s,{kind='move',dx=-1,dy=0}))
        assert(C.encode(e.pending_attack.target_cells)==cells)
        wait(s,1);assert(p.hp==24 and e.position.x==8 and s.clock==300)
    end)
    test('four finite quarters advance clockwise with independent strict windups then ordinary melee',function()
        local s,p,e=trial();local rng=s.rng.state
        local events=wait(s,14);local starts,impacts={},{}
        for _,v in ipairs(events)do
            if v.kind=='WindupStarted' and v.ability_id=='core:ability/sentinel_quarter'then
                starts[#starts+1]=v;assert(v.time==50+(#starts-1)*300)
            elseif v.kind=='AttackPerformed' and v.ability_id=='core:ability/sentinel_quarter'then
                impacts[#impacts+1]=v;assert(v.time==250+(#impacts-1)*300)
            end
        end
        assert(#starts==4 and #impacts==4 and e.ai_memory.sentinel_finished
            and e.ai_memory.sentinel_strikes==4 and s.rng.state==rng)
        for i,v in ipairs(starts)do assert(v.sentinel_facing==({4,1,2,3})[i])end
        assert(e.pending_attack and e.pending_attack.ability_id=='core:ability/sentinel_touch')
    end)
    test('the locked arc keeps friendly fire and commits all three HP outcomes in the same batch',function()
        local s,p,e=trial()
        local a=G.spawn_enemy(s,'core:enemy/mossbound_guard',7,5,100000)
        local b=G.spawn_enemy(s,'core:enemy/mossbound_guard',7,7,100000)
        local events=wait(s,3);local batch
        for _,v in ipairs(events)do if v.kind=='DamageBatchResolved' and v.source_id==e.id then batch=v end end
        assert(batch and #batch.outcomes==3 and a.hp==9 and b.hp==9 and p.hp==22)
        for _,hit in ipairs(batch.outcomes)do assert(hit.actual_loss==(hit.target_id==p.id and 2 or 1))end
    end)
    test('ordinary damage does not cancel or rotate an already committed quarter',function()
        local s,p,e=trial();wait(s,1)
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(e.hp==11 and e.pending_attack.due==250 and e.pending_attack.sentinel_facing==4)
        wait(s,1);assert(p.hp==22 and not e.pending_attack)
    end)
    test('stun interrupts both a pending quarter and the recovery gap without restarting the sequence',function()
        for _,steps in ipairs({1,3})do
            local s,p,e=trial();wait(s,steps);assert(G.stun(s,e.id,500))
            assert(e.ai_memory.sentinel_finished and not e.pending_attack)
            for _,v in ipairs(wait(s,7))do
                assert(v.kind~='WindupStarted' or v.ability_id~='core:ability/sentinel_quarter')
            end
            assert(G.restore(G.snapshot(s)))
        end
    end)
    test('displacement during recovery ends the sequence and death removes every queued impact',function()
        local s,p,e=trial();wait(s,3);assert(not e.pending_attack)
        assert(G.displace(s,e.id,10,6));assert(e.ai_memory.sentinel_finished)
        for _,v in ipairs(wait(s,5))do
            assert(v.kind~='WindupStarted' or v.ability_id~='core:ability/sentinel_quarter')
        end
        s,p,e=trial();wait(s,1);e.hp=3
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(not G.entity(s,e.id))
        wait(s,4);assert(p.hp==24)
        for _,item in ipairs(s.schedule)do assert(item.actor_id~=e.id)end
    end)
    test('saved facing and locked footprint resume exactly while forged sequence data is rejected',function()
        local s,p,e=trial();wait(s,1);local before=G.snapshot(s)
        local b=G.restore(C.decode(C.encode(before)))
        for _=1,10 do equal(wait(s,1),wait(b,1));equal(G.snapshot(s),G.snapshot(b))end
        for _,change in ipairs({function(v)v.ai_memory.sentinel_facing=8 end,
            function(v)v.ai_memory.sentinel_strikes=5 end,
            function(v)v.pending_attack.target_cells[1].x=6 end,
            function(v)v.pending_attack.sentinel_facing=2 end})do
            local snap=C.decode(C.encode(before))
            for _,v in ipairs(snap.entities)do if v.id==e.id then change(v)end end
            assert(not pcall(G.restore,snap))
        end
    end)
    test('natural champions occupy optional room interiors without replacing any biome elite or changing geometry',function()
        local found=0
        for seed=1,100 do
            local s=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(s.entities)do if e.definition_id==S.id then
                local map=require('src.floors').map(s,e.position.floor_id)
                assert(S.optional_room(map,e.position));found=found+1
                assert(require('src.content').get_enemy(S.id).special_encounter)
                assert(map.elite and map.elite.definition_id~=S.id)
            end end
            assert(G.restore(G.snapshot(s)))
        end
        assert(found>0,'optional champion never generated')
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'onebit'})do
            local found=false;assert(#report.packs[pack].missing==0)
            for _,h in ipairs(report.packs[pack].bindings)do if h.id==S.id then
                assert(h.sheet=='onebit' and h.index==224);found=true
            end end
            assert(found)
        end
    end)
end
