return function(test,equal)
    local G=require('src.game');local C=require('src.persistence');local AI=require('src.ai')
    local T=require('src.idea_silt');local Content=require('src.content')
    local function trial()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,T.id,10,6,50)
        return s,p,e
    end
    local function wait(s,n)
        local events={}
        for _=1,n do local ok,_,list=G.submit(s,{kind='wait'});assert(ok)
            for _,v in ipairs(list)do events[#events+1]=v end
        end
        return events
    end
    local function created()
        local s,p,e=trial();wait(s,3)
        assert(e.position.x==8 and #s.idea_silt==2 and s.idea_silt[1].expires==515)
        return s,p,e
    end
    test('Worm decisions are pure and a short charge keeps its committed lane and direction',function()
        local s,p,e=trial();local before=G.snapshot(s)
        for _=1,30 do local aim=AI.choose(s,e)
            assert(aim.ability_id=='core:ability/worm_charge' and #aim.cells==3
                and aim.advance.dx==-1 and aim.ranged==true)
        end
        equal(before,G.snapshot(s));wait(s,1)
        assert(e.pending_attack.due==215 and e.ai_memory.silt_charges==1)
        assert(G.submit(s,{kind='move',dx=0,dy=1}));wait(s,1)
        assert(p.hp==24 and p.position.y==7 and e.position.x==8 and e.position.y==6)
        assert(#s.idea_silt==2 and s.idea_silt[1].x==10 and s.idea_silt[2].x==9)
    end)
    test('the trail covers only cells actually vacated and expires without rewriting underlying row bytes',function()
        local s,p,e=trial();local rows=C.encode(Content.map_for(s).rows)
        wait(s,3);assert(C.encode(Content.map_for(s).rows)==rows)
        for _,r in ipairs(s.idea_silt)do assert(r.y==6 and r.x~=e.position.x)end
        wait(s,3);assert(#s.idea_silt==0 and C.encode(Content.map_for(s).rows)==rows)
        assert(G.restore(G.snapshot(s)))
    end)
    test('a living blocker stops charge damage and movement before any untraversed ground becomes bog',function()
        local s,p,e=trial();local blocker=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
        wait(s,3)
        assert(blocker.hp==9 and p.hp==24 and e.position.x==10 and #(s.idea_silt or {})==0)
    end)
    test('walking and Dash pay the previewed temporary bog costs and never damage the entrant',function()
        local s,p,e=created();p.position.x,p.position.y=9,7
        local hp=p.hp;local aim=G.preview(s,'move',0,-1)
        assert(aim.valid and aim.cost==140)
        assert(G.submit(s,{kind='move',dx=0,dy=-1}));assert(s.clock==440 and p.hp==hp)
        s,p,e=created();p.position.x,p.position.y=11,6;hp=p.hp
        aim=G.preview(s,'dash',-1,0)
        assert(aim.valid and aim.distance==2 and aim.cost==170)
        assert(G.submit(s,{kind='dash',dx=-1,dy=0}));assert(s.clock==470 and p.hp==hp and p.position.x==9)
    end)
    test('ordinary enemy movement pays the same bog recovery once at its actual move time',function()
        local s,p,e=created();p.position.x,p.position.y=11,6
        local guard=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,7,301)
        wait(s,1);assert(guard.position.x==9 and guard.position.y==6)
        local found=false
        for _,item in ipairs(s.schedule)do if item.actor_id==guard.id and item.kind=='actor_ready'then
            assert(item.due==301+G.effective_cost(140,guard.speed));found=true
        end end
        assert(found)
    end)
    test('stun spends and cancels a pending charge while stairs and water cannot become a charge lane',function()
        local s,p,e=trial();wait(s,1);assert(G.stun(s,e.id,500));wait(s,3)
        assert(e.ai_memory.silt_charges==1 and #(s.idea_silt or {})==0)
        for _,kind in ipairs({'stair','water'})do
            s,p,e=trial();s.generated_map=C.decode(C.encode(Content.map_for(s)))
            if kind=='stair'then s.generated_map.placements={stairs={{x=9,y=6}}}
            else local row=s.generated_map.rows[6];s.generated_map.rows[6]=row:sub(1,8)..'~'..row:sub(10)end
            assert(AI.choose(s,e).ability_id~='core:ability/worm_charge')
        end
    end)
    test('spent charge budgets do not reset and old trails remain finite after their owner dies',function()
        local s,p,e=created();e.ai_memory.silt_charges=2
        p.position.x,p.position.y=11,6
        assert(AI.choose(s,e).ability_id~='core:ability/worm_charge')
        s,p,e=created();e.hp=4
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(not G.entity(s,e.id) and #s.idea_silt==2)
        assert(G.restore(G.snapshot(s)));wait(s,2);assert(#s.idea_silt==0)
    end)
    test('pending charge and live bog save continuations preserve exact rows RNG schedule and expiry',function()
        for _,steps in ipairs({1,3})do
            local s,p,e=trial();wait(s,steps)
            local before=G.snapshot(s);local restored=G.restore(C.decode(C.encode(before)))
            for _=1,5 do equal(wait(s,1),wait(restored,1));equal(G.snapshot(s),G.snapshot(restored))end
        end
        local frozen={clock=900,entities={},schedule={},floor_pause={a={suspended_at=300,schedule={}}},
            idea_silt={{floor_id='a',expires=515},{floor_id='b',expires=1000}}}
        require('src.floors').resume(frozen,'a')
        assert(frozen.idea_silt[1].expires==1115 and frozen.idea_silt[2].expires==1000)
    end)
    test('corrupt expiry ownership overlap charge budgets and unlocked pending lanes are rejected',function()
        local s,p,e=created();local before=G.snapshot(s)
        for _,change in ipairs({function(v)v.idea_silt[1].expires=100 end,
            function(v)v.idea_silt[1].source_id=v.player_id end,
            function(v)v.idea_silt[2].x=v.idea_silt[1].x end,
            function(v)for _,a in ipairs(v.entities)do if a.id==e.id then a.ai_memory.silt_charges=3 end end end})do
            local snap=C.decode(C.encode(before));change(snap);assert(not pcall(G.restore,snap))
        end
        s,p,e=trial();wait(s,1);local snap=G.snapshot(s)
        for _,a in ipairs(snap.entities)do if a.id==e.id then a.pending_attack.target_cells[1].y=7 end end
        assert(not pcall(G.restore,snap))
    end)
    test('the global trail cap refuses further cells without changing ground RNG or existing lifetimes',function()
        local s,p,e=trial();e.ai_memory.silt_charges=2
        local owners={e,G.spawn_enemy(s,T.id,11,6,100000),G.spawn_enemy(s,T.id,12,6,100000)}
        for _,o in ipairs(owners)do o.ai_memory.silt_charges=2 end
        s.next_action_id=7;s.idea_silt={}
        for i=1,12 do s.idea_silt[i]={action_id=math.ceil(i/2),source_id=owners[math.ceil(i/4)].id,
            floor_id=s.active_floor_id,x=i+1,y=2,expires=300}end
        local map=Content.map_for(s);T.validate(s,{[map.floor_id]=map})
        local before=G.snapshot(s)
        T.leave(s,e,{ability_id='core:ability/worm_charge',action_id=6},10,6,
            {{x=9,y=6},{x=8,y=6}},map,function()error('cap should emit nothing')end)
        equal(before,G.snapshot(s));assert(#s.idea_silt==12)
    end)
    test('real generated Worms resolve their approved body and distinct mixed silt cue without missing bindings',function()
        local s=require('tests.idea_seeds').find(T.id);assert(G.restore(G.snapshot(s)))
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'kenney','onebit'})do
            local body,trail=false,false;assert(#report.packs[pack].missing==0)
            for _,h in ipairs(report.packs[pack].bindings)do
                if h.id==T.id then assert(h.sheet=='onebit' and h.index==371);body=true end
                if h.id=='core:object/idea_silt_trail'then
                    assert(h.sheet==(pack=='kenney' and 'caves' or 'onebit')
                        and h.index==(pack=='kenney' and 64 or 268));trail=true
                end
            end
            assert(body and trail)
        end
    end)
end
