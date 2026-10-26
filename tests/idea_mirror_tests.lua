return function(test,equal)
    local G=require('src.game');local C=require('src.persistence')
    local M=require('src.idea_mirrors');local AI=require('src.ai')
    local function trial()
        local s=G.new(37,nil,{class_id='core:actor/ranger'})
        local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,M.id,10,6,50)
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
        assert(s.clock==261 and #s.idea_mirrors==1 and s.idea_mirrors[1].expires==626)
        return s,p,e,s.idea_mirrors[1]
    end
    test('a visible Hexer commits one locked directional marker with a finite spent cast',function()
        local s,p,e=trial();local before=G.snapshot(s)
        for _=1,30 do local aim=AI.choose(s,e)
            assert(aim.support.kind=='mirror' and aim.support.x==9 and aim.support.in_dx==1)
        end
        equal(before,G.snapshot(s));wait(s,1)
        assert(e.pending_attack.due==226 and e.ai_memory.mirror_casts==1)
        assert(e.pending_attack.target_cells[1].x==9 and not s.idea_mirrors)
        assert(G.restore(G.snapshot(s)))
    end)
    test('repeated player preview shows both legs without consuming the mirror RNG or time',function()
        local s,p,e=created();local before=G.snapshot(s)
        for _=1,30 do
            local preview=G.preview(s,'basic_attack',1,0)
            assert(preview.valid and preview.cost==87 and preview.reflection.step==2)
            for i,c in ipairs(preview.cells)do
                assert(c.x==({8,9,9,9})[i] and c.y==({6,6,7,8})[i])
            end
        end
        equal(before,G.snapshot(s))
    end)
    test('a real reflected bow shot hits the turned cell with original shooter damage and one consumption',function()
        local s,p,e=created();local target=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,7,100000)
        local rng=s.rng.state;local ok,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(ok)
        assert(target.hp==8 and e.hp==7 and #s.idea_mirrors==0 and s.clock==348 and s.rng.state==rng)
        local reflected,hits=0,0
        for _,v in ipairs(events)do
            if v.kind=='MirrorReflected'then assert(v.source_id==p.id);reflected=reflected+1 end
            if v.kind=='DamageTaken'then
                assert(v.source_id==p.id and v.target_id==target.id and v.direct_attack);hits=hits+1
            end
        end
        assert(reflected==1 and hits==1 and G.restore(G.snapshot(s)))
    end)
    test('an intervening creature killed by the same shot blocks reflection instead of disappearing early',function()
        local s,p,e=created();local blocker=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
        blocker.hp=2
        assert(not G.preview(s,'basic_attack',1,0).reflection)
        local ok,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(ok)
        assert(not G.entity(s,blocker.id) and #s.idea_mirrors==1)
        for _,v in ipairs(events)do assert(v.kind~='MirrorReflected')end
    end)
    test('melee area diagonal and wrong-direction attacks bypass the limited reflector',function()
        local s,p,e,r=created();local before=G.snapshot(s)
        local cells={{x=8,y=6},{x=9,y=6},{x=10,y=6},{x=11,y=6}}
        for _,ranged in ipairs({false,'area'})do
            local result,reflection=M.route(s,p,cells,ranged)
            equal(result,cells);assert(not reflection)
        end
        local diagonal={{x=8,y=7},{x=9,y=8},{x=10,y=9}}
        local result,reflection=M.route(s,p,diagonal,true)
        equal(result,diagonal);assert(not reflection)
        local source={id=p.id,position={floor_id=s.active_floor_id,x=9,y=8}}
        local wrong={{x=9,y=7},{x=9,y=6},{x=9,y=5}}
        result,reflection=M.route(s,source,wrong,true);equal(result,wrong);assert(not reflection)
        equal(before,G.snapshot(s))
    end)
    test('a turned path preserves its range and cannot bounce again from a second mirror',function()
        local s,p,e,r=created()
        local other=G.spawn_enemy(s,M.id,10,7,100000);other.ai_memory.mirror_casts=1
        local id=s.next_action_id;s.next_action_id=id+1
        s.idea_mirrors[2]={id=id,source_id=other.id,floor_id=r.floor_id,
            x=9,y=7,source_x=9,source_y=8,in_dx=0,in_dy=1,expires=r.expires}
        local before=G.snapshot(s);local cells=G.preview(s,'basic_attack',1,0).cells
        assert(#cells==4 and cells[3].x==9 and cells[3].y==7 and cells[4].x==9 and cells[4].y==8)
        equal(before,G.snapshot(s));assert(G.restore(G.snapshot(s)))
    end)
    test('enemy reflection locks at windup commitment retains friendly fire and survives a fresh restore',function()
        local s,p,e=created();p.position.x,p.position.y=11,6
        local shooter=G.spawn_enemy(s,'core:enemy/thornspitter',7,6,s.clock+1)
        local target=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,7,100000)
        wait(s,1)
        assert(shooter.pending_attack and shooter.pending_attack.reflection and #s.idea_mirrors==0)
        local cells=C.encode(shooter.pending_attack.target_cells)
        local restored=G.restore(C.decode(C.encode(G.snapshot(s))))
        local events=wait(s,1);equal(events,wait(restored,1));equal(G.snapshot(s),G.snapshot(restored))
        local found=false
        for _,v in ipairs(events)do if v.kind=='DamageTaken' and v.source_id==shooter.id then
            assert(v.target_id==target.id);found=true
        end end
        assert(found and target.hp<10 and p.hp==20 and cells:find('S1:yD1:7',1,true))
    end)
    test('occupying a locked marker blocks placement and stun cannot refund its finite attempt',function()
        local s,p,e=trial();wait(s,1)
        assert(G.submit(s,{kind='move',dx=1,dy=0}));assert(G.submit(s,{kind='move',dx=1,dy=0}))
        assert(p.position.x==9 and #(s.idea_mirrors or {})==0 and e.ai_memory.mirror_casts==1)
        s,p,e=trial();wait(s,1);assert(G.stun(s,e.id,500));wait(s,3)
        assert(not e.pending_attack and e.ai_memory.mirror_casts==1 and #(s.idea_mirrors or {})==0)
    end)
    test('expiry and caster death clear the finite marker while saved continuation stays byte exact',function()
        local s,p,e=created();assert(G.stun(s,e.id,1000))
        local before=G.snapshot(s);local restored=G.restore(C.decode(C.encode(before)))
        equal(wait(s,5),wait(restored,5));equal(G.snapshot(s),G.snapshot(restored))
        assert(#s.idea_mirrors==0 and s.clock>=626)
        s,p,e=created();p.position.x,p.position.y=9,6;e.hp=3
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(not G.entity(s,e.id) and #s.idea_mirrors==0 and G.restore(G.snapshot(s)))
    end)
    test('inactive floor lifetime rebases and invalid active markers or locked paths reject',function()
        local frozen={clock=900,entities={},schedule={},floor_pause={a={suspended_at=300,schedule={}}},
            idea_mirrors={{floor_id='a',expires=626},{floor_id='b',expires=1000}}}
        require('src.floors').resume(frozen,'a')
        assert(frozen.idea_mirrors[1].expires==1226 and frozen.idea_mirrors[2].expires==1000)
        local s,p,e=created();local before=G.snapshot(s)
        for _,change in ipairs({function(r)r.in_dx=2 end,function(r)r.expires=200 end,
            function(r)r.source_id=s.player_id end,function(r)r.x=6 end})do
            local snap=C.decode(C.encode(before));change(snap.idea_mirrors[1])
            assert(not pcall(G.restore,snap))
        end
        p.position.x,p.position.y=11,6
        local shooter=G.spawn_enemy(s,'core:enemy/thornspitter',7,6,s.clock+1);wait(s,1)
        local snap=G.snapshot(s)
        for _,v in ipairs(snap.entities)do if v.id==shooter.id then v.pending_attack.reflection.out_dx=1 end end
        assert(not pcall(G.restore,snap))
    end)
    test('movement-linked charges and Hookrunner grapples keep their original straight geometry',function()
        local s,p,e=created();p.position.x,p.position.y=11,6
        local charger=G.spawn_enemy(s,'core:enemy/glass_charger',7,6,s.clock+1)
        wait(s,1)
        assert(charger.pending_attack and charger.pending_attack.advance
            and not charger.pending_attack.reflection and #s.idea_mirrors==1)
        for _,c in ipairs(charger.pending_attack.target_cells)do assert(c.y==6)end
        s=G.new(37,nil,{class_id='core:actor/hookrunner'});p=G.player(s)
        p.position.x,p.position.y=7,6;e=G.spawn_enemy(s,M.id,10,6,50)
        while s.clock<250 do assert(G.submit(s,{kind='wait'}))end
        assert(#s.idea_mirrors==1)
        local aim=G.preview(s,'sweeping_slash',1,0)
        assert(not aim.reflection)
        for _,c in ipairs(aim.cells)do assert(c.y==6)end
        assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}))
        assert(#s.idea_mirrors==1 and e.position.x==8 and e.position.y==6)
    end)
    test('a real generated Hexer has approved caster and reflector art in the fixed 1-Bit renderer',function()
        local s=require('tests.idea_seeds').find(M.id);assert(G.restore(G.snapshot(s)))
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'onebit'})do
            local found={};assert(#report.packs[pack].missing==0)
            for _,h in ipairs(report.packs[pack].bindings)do
                if h.id==M.id then assert(h.sheet=='onebit' and h.index==320);found.body=true end
                if h.id=='core:ui/idea_mirror_effect'then assert(h.index==493);found.mark=true end
                if h.id=='core:ui/idea_reflect_effect'then assert(h.index==494);found.reflect=true end
            end
            assert(found.body and found.mark and found.reflect)
        end
    end)
end
