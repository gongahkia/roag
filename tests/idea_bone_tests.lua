return function(test,equal)
    local G=require('src.game')
    local C=require('src.persistence')
    local I=require('src.idea_enemies')
    local function trial(class_id)
        local s=G.new(37,nil,{class_id=class_id});local p=G.player(s);p.position.x,p.position.y=7,6
        local bone=G.spawn_enemy(s,'core:enemy/bone_splitter',8,6,100000);bone.hp=3
        return s,p,bone
    end
    local function fragments(s)
        local out={};for _,e in ipairs(s.entities)do if e.fragment_of then out[#out+1]=e end end;return out
    end
    test('Bone Splitter fragments appear only after the complete primary HP batch and do not inherit its damage',function()
        local s,p,bone=trial()
        local guard=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,5,100000);guard.hp=2
        local _,_,events=G.submit(s,{kind='sweeping_slash',dx=1,dy=0})
        local batch,split,dead=0,0,{}
        for index,event in ipairs(events)do
            if event.kind=='DamageBatchResolved' and batch==0 then
                batch=index;assert(#event.outcomes==2)
                for _,v in ipairs(event.outcomes)do assert(v.hp_after==0);dead[v.target_id]=true end
            elseif event.kind=='BoneFragmented'then
                split=split+1;assert(index>batch and event.original_id==bone.id)
            end
        end
        assert(dead[bone.id] and dead[guard.id] and not G.entity(s,bone.id) and not G.entity(s,guard.id))
        assert(split==2 and #fragments(s)==2 and s.progression.xp_total==9)
        for _,e in ipairs(fragments(s))do assert(e.max_hp==2 and e.hp==2 and e.reward_exempt)end
    end)
    test('two fragments die once with no new split, XP, currency or supply reward',function()
        local s,p,bone=trial('core:actor/sentinel');assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(#fragments(s)==2 and s.progression.xp_total==4)
        local drops=#s.drops
        local _,_,events=G.submit(s,{kind='sweeping_slash',dx=1,dy=0})
        assert(#fragments(s)==0 and s.progression.xp_total==4 and #s.drops==drops)
        local deaths=0
        for _,e in ipairs(events)do
            assert(e.kind~='BoneFragmented')
            if e.kind=='EntityKilled'then assert(e.reward_exempt);deaths=deaths+1 end
        end
        assert(deaths==2 and G.restore(G.snapshot(s)))
    end)
    test('a cramped split uses only free legal cells and never occupies stairs or carves walls',function()
        local s,p,bone=trial()
        s.generated_map=C.decode(C.encode(require('src.content').map_for(s)))
        local map=s.generated_map
        local before=C.encode(map.rows)
        map.placements={stairs={{x=8,y=5}}}
        G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
        G.spawn_enemy(s,'core:enemy/mossbound_guard',8,7,100000)
        -- South/east are occupied, west contains the player, north is stairs.
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        local list=fragments(s);assert(#list==1 and list[1].position.x==8 and list[1].position.y==6)
        assert(C.encode(map.rows)==before)
    end)
    test('fragment continuation preserves pending attacks, exact rewards, events and RNG across codec saves',function()
        local s=trial();assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        local b=G.restore(C.decode(C.encode(G.snapshot(s))))
        for _,intent in ipairs({{kind='wait'},{kind='sweeping_slash',dx=1,dy=0},{kind='wait'}})do
            local _,_,aev=G.submit(s,intent);local _,_,bev=G.submit(b,intent)
            equal(aev,bev);equal(G.snapshot(s),G.snapshot(b))
        end
    end)
    test('forged fragment ownership, duplicate bodies and renewed rewards fail save validation',function()
        local s=trial();assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        local id=fragments(s)[1].id
        for _,edit in ipairs({function(e)e.reward_exempt=false end,
            function(e)e.fragment_of=e.id end,function(e)e.revival_of=1 end})do
            local snap=G.snapshot(s)
            for _,e in ipairs(snap.entities)do if e.id==id then edit(e)end end
            assert(not pcall(G.restore,snap))
        end
        local snap=G.snapshot(s);local third=C.decode(C.encode(fragments(s)[1]))
        third.id=snap.next_entity_id;snap.next_entity_id=third.id+1
        snap.entities[#snap.entities+1]=third
        assert(not pcall(G.restore,snap),'one parent cannot own three fragments')
        assert(G.restore(G.snapshot(s)))
    end)
    test('natural floors contain splitters but never pre-spawn fragments and both reviewed bodies resolve',function()
        local found=false
        for seed=1,50 do
            local s=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(s.entities)do
                assert(e.definition_id~='core:enemy/bone_fragment')
                if e.definition_id=='core:enemy/bone_splitter'then found=true end
            end
        end
        if not found then
            local s=require('tests.idea_seeds').find('core:enemy/bone_splitter')
            for _,e in ipairs(s.entities)do assert(e.definition_id~='core:enemy/bone_fragment')end
            found=true
        end
        assert(found)
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'onebit'})do
            assert(#report.packs[pack].missing==0)
            local seen={}
            for _,h in ipairs(report.packs[pack].bindings)do
                if h.id=='core:enemy/bone_splitter'then assert(h.index==323);seen[323]=true end
                if h.id=='core:enemy/bone_fragment'then assert(h.index==323);seen.fragment=true end
            end
            assert(seen[323] and seen.fragment)
            local size=require('src.visual_slots').enemy_size
            assert(size('core:enemy/bone_fragment',48)==32)
            assert(size('core:enemy/bone_fragment',32)==16)
            assert(size('core:enemy/bone_fragment',16)==16)
            assert(size('core:enemy/bone_splitter',48)==48)
        end
        assert(not I.revivable({ai_id='idea_regular',definition_id='core:enemy/bone_splitter'}))
    end)
end
