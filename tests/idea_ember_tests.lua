return function(test,equal)
    local G=require('src.game');local C=require('src.persistence');local AI=require('src.ai')
    local E=require('src.idea_embers');local Content=require('src.content')
    local function trial()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,E.id,10,6,50)
        return s,p,e
    end
    local function wait(s,n)
        local all={}
        for _=1,n do local ok,_,events=G.submit(s,{kind='wait'});assert(ok)
            for _,v in ipairs(events)do all[#all+1]=v end
        end
        return all
    end
    local function created()
        local s,p,e=trial();wait(s,3)
        assert(p.hp==22 and #s.idea_embers==1 and s.idea_embers[1].next_due==330)
        return s,p,e
    end
    test('Moth preview is pure and a dive retains its locked landing after the player moves',function()
        local s,p,e=trial();local before=G.snapshot(s)
        for _=1,30 do local aim=AI.choose(s,e)
            assert(aim.ember_landing.x==7 and aim.ember_landing.y==6 and aim.ranged=='area')
        end
        equal(before,G.snapshot(s));wait(s,1)
        assert(e.pending_attack.due==230 and e.ai_memory.ember_dives==1 and G.restore(G.snapshot(s)))
        assert(G.submit(s,{kind='move',dx=0,dy=1}));wait(s,1)
        assert(p.hp==24 and e.position.x==7 and e.position.y==6 and s.idea_embers[1].x==7)
    end)
    test('an occupied landing keeps bodies separate and the finite ember pulses at exact world times',function()
        local s,p,e=created();assert(e.position.x==10 and p.position.x==7)
        local times={}
        for _,v in ipairs(wait(s,2))do if v.kind=='EmberPulsed'then times[#times+1]=v.time end end
        assert(#times==2 and times[1]==330 and times[2]==430 and p.hp==20 and #s.idea_embers==0)
        for _,item in ipairs(s.schedule)do assert(not item.idea_ember)end
    end)
    test('fire warnings expose the actual next pulse while read-only targeting cannot advance or consume it',function()
        local s,p,e=created();local before=G.snapshot(s)
        for _=1,30 do
            local found=false
            for _,v in ipairs(G.pending_strikes(s))do if v.ember_pulse then
                assert(v.due==330 and v.damage==1 and v.target_cells[1].x==7);found=true
                v.target_cells[1].x=99
            end end
            assert(found);G.preview(s,'dash',0,1)
        end
        equal(before,G.snapshot(s))
    end)
    test('leaving a burning cell avoids later pulses without changing the underlying floor',function()
        local s,p,e=created();local rows=C.encode(Content.map_for(s).rows)
        assert(G.submit(s,{kind='move',dx=0,dy=1}));wait(s,1)
        assert(p.hp==22 and #s.idea_embers==0 and rows==C.encode(Content.map_for(s).rows))
    end)
    test('an ember survives its caster death for only its remaining scheduled pulse',function()
        local s,p,e=created();p.position.x,p.position.y=9,6;e.hp=4
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(not G.entity(s,e.id) and #s.idea_embers==1 and s.idea_embers[1].remaining==1)
        assert(G.restore(G.snapshot(s)));p.position.x,p.position.y=7,6
        wait(s,1);assert(p.hp==21 and #s.idea_embers==0)
    end)
    test('stun and displacement cancel the committed dive without refunding its finite attempt',function()
        for _,kind in ipairs({'stun','displace'})do
            local s,p,e=trial();wait(s,1);local id=e.pending_attack.action_id
            if kind=='stun'then assert(G.stun(s,e.id,500))else assert(G.displace(s,e.id,9,6))end
            wait(s,2);assert(p.hp==24 and #(s.idea_embers or {})==0 and e.ai_memory.ember_dives>=1)
            assert(not e.pending_attack or e.pending_attack.action_id~=id)
        end
    end)
    test('a different creature on the locked tile takes normal friendly fire and prevents body overlap',function()
        local s,p,e=trial();wait(s,1);assert(G.submit(s,{kind='move',dx=0,dy=1}))
        local other=G.spawn_enemy(s,'core:enemy/mossbound_guard',7,6,100000)
        wait(s,1);assert(other.hp==9 and p.hp==24 and e.position.x==10)
        assert(s.idea_embers[1].x==7 and G.restore(G.snapshot(s)))
    end)
    test('only the source Moth is immune to its own ember and another Moth can burn',function()
        local s,p,e=trial();wait(s,1);assert(G.submit(s,{kind='move',dx=0,dy=1}));wait(s,3)
        assert(e.hp==6 and #s.idea_embers==0 and p.hp==24)
        s,p,e=trial();wait(s,1);assert(G.submit(s,{kind='move',dx=0,dy=1}));wait(s,1)
        assert(G.displace(s,e.id,8,6))
        local other=G.spawn_enemy(s,E.id,7,6,100000);wait(s,2)
        assert(other.hp==4 and e.hp==6 and #s.idea_embers==0)
    end)
    test('pending dive and active ember restore exactly alongside real Burning and Temporal Echo work',function()
        local s,p,e=created();local target=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
        G.set_boon_stacks(s,p.id,'core:boon/emberbrand','common',1)
        G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(target.statuses.burning and #s.idea_embers==1 and next(s.delayed_effects))
        local restored=G.restore(C.decode(C.encode(G.snapshot(s))))
        for _=1,3 do equal(wait(s,1),wait(restored,1));equal(G.snapshot(s),G.snapshot(restored))end
        s,p,e=trial();wait(s,1);restored=G.restore(C.decode(C.encode(G.snapshot(s))))
        equal(wait(s,4),wait(restored,4));equal(G.snapshot(s),G.snapshot(restored))
    end)
    test('frozen-floor fire clocks and queued pulses rebase together without touching another floor',function()
        local frozen={clock=900,entities={},schedule={},floor_pause={a={suspended_at=300,
                schedule={{kind='effect',id=1,seq=1,due=330,actor_id=0,floor_id='a',idea_ember=true}}}},
            idea_embers={{floor_id='a',next_due=330,expires=430},{floor_id='b',next_due=1000,expires=1100}}}
        require('src.floors').resume(frozen,'a')
        assert(frozen.idea_embers[1].next_due==930 and frozen.idea_embers[1].expires==1030
            and frozen.schedule[1].due==930 and frozen.idea_embers[2].next_due==1000)
    end)
    test('invalid dive ownership pulse counts lost timers and orphaned fire work reject safely',function()
        local s,p,e=created();local before=G.snapshot(s)
        for _,change in ipairs({function(v)v.idea_embers[1].remaining=3 end,
            function(v)v.idea_embers[1].source_id=v.player_id end,
            function(v)v.idea_embers[1].next_due=299 end,
            function(v)v.idea_embers[1].schedule_id=999999 end,
            function(v)v.idea_embers={}end})do
            local snap=C.decode(C.encode(before));change(snap);assert(not pcall(G.restore,snap))
        end
        s,p,e=trial();wait(s,1);local snap=G.snapshot(s)
        for _,v in ipairs(snap.entities)do if v.id==e.id then v.pending_attack.ember_landing.x=8 end end
        assert(not pcall(G.restore,snap))
    end)
    test('a naturally selected Moth uses approved unused winged body 411 in the fixed 1-Bit look',function()
        local s=require('tests.idea_seeds').find(E.id);assert(G.restore(G.snapshot(s)))
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'onebit'})do
            local found=false;assert(#report.packs[pack].missing==0)
            for _,h in ipairs(report.packs[pack].bindings)do if h.id==E.id then
                assert(h.sheet=='onebit' and h.index==411);found=true
            end end
            assert(found)
        end
    end)
end
