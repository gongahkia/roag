return function(test,equal)
    local G=require('src.game')
    local AI=require('src.ai')
    local C=require('src.persistence')
    local function trial(class_id)
        local s=G.new(37,nil,{class_id=class_id})
        local p=G.player(s);p.position.x,p.position.y=8,6
        local e=G.spawn_enemy(s,'core:enemy/haunted_cache',10,6,50)
        return s,p,e
    end
    test('a suspicious cache stays still until approached or injured and its AI is pure',function()
        local s,p,e=trial();local before=G.snapshot(s)
        assert(AI.choose(s,e).kind=='wait');equal(before,G.snapshot(s))
        for _=1,4 do assert(G.submit(s,{kind='wait'}))end
        assert(not e.ai_memory.alerted and not e.pending_attack and e.position.x==10 and p.hp==24)
    end)
    test('approaching a cache announces a locked bite instead of dealing an instant ambush hit',function()
        local s,p,e=trial();assert(G.submit(s,{kind='wait'}))
        local _,_,events=G.submit(s,{kind='move',dx=1,dy=0})
        assert(p.hp==24 and e.ai_memory.alerted and e.pending_attack.due==370)
        local started=0
        for _,v in ipairs(events)do if v.kind=='WindupStarted'then started=started+1 end end
        assert(started==1 and e.pending_attack.target_cells[1].x==9)
        assert(G.submit(s,{kind='move',dx=0,dy=1}))
        assert(G.submit(s,{kind='wait'}));assert(p.hp==24,'bite must not follow the moving player')
    end)
    test('a real ranged hit wakes the cache but does not grant it a free strike',function()
        local s,p,e=trial('core:actor/ranger')
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(e.hp==5 and e.ai_memory.alerted and e.position.x==9 and p.hp==20)
        assert(not e.pending_attack,'the wake movement is an ordinary scheduled action')
    end)
    test('killing a cache gives one normal enemy reward and never opens a boon-chest menu',function()
        local s,p,e=trial();p.position.x=9
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(p.hp==24 and e.pending_attack)
        local _,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(not G.entity(s,e.id) and s.progression.xp_total==4 and #s.drops==1)
        for _,v in ipairs(events)do assert(v.kind~='ChestOpened' and v.kind~='BoonGranted')end
        assert(G.submit(s,{kind='move',dx=1,dy=0}));assert(s.currency==3)
    end)
    test('dormant and awakened cache saves preserve exact subsequent events and reject malformed wake state',function()
        local s,p,e=trial()
        for _,intent in ipairs({{kind='wait'},{kind='move',dx=1,dy=0},{kind='wait'},{kind='wait'}})do
            local b=G.restore(C.decode(C.encode(G.snapshot(s))))
            local _,_,a=G.submit(s,intent);local _,_,bev=G.submit(b,intent)
            equal(a,bev);equal(G.snapshot(s),G.snapshot(b))
        end
        assert(p.hp==22,'ordinary committed bite should resolve')
        local snap=G.snapshot(s)
        for _,v in ipairs(snap.entities)do if v.id==e.id then v.ai_memory.alerted='bad'end end
        assert(not pcall(G.restore,snap))
    end)
    test('generated caches use reviewed dormant and revealed art and restore without repopulation',function()
        local found=false
        for seed=1,40 do
            local s=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(s.entities)do if e.definition_id=='core:enemy/haunted_cache'then
                assert(not e.ai_memory.alerted);found=true
            end end
            equal(G.snapshot(s),G.snapshot(G.restore(C.decode(C.encode(G.snapshot(s))))))
        end
        assert(found)
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'kenney','onebit'})do
            assert(#report.packs[pack].missing==0)
            local count=0
            for _,h in ipairs(report.packs[pack].bindings)do
                if h.id=='core:enemy/haunted_cache'then
                    assert(h.index==(pack=='kenney' and 92 or 417));count=count+1
                elseif h.id=='core:object/idea_cache_dormant'then
                    assert(h.index==(pack=='kenney' and 90 or 303));count=count+1
                end
            end
            assert(count==2)
        end
    end)
end
