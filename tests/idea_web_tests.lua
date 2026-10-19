return function(test,equal)
    local G=require('src.game')
    local C=require('src.persistence')
    local AI=require('src.ai')
    local W=require('src.idea_webs')
    local function trial(class_id)
        local s=G.new(37,nil,{class_id=class_id});local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,'core:enemy/web_weaver',10,6,50)
        return s,p,e
    end
    local function created(class_id)
        local s,p,e=trial(class_id)
        while s.clock<250 do assert(G.submit(s,{kind='wait'}))end
        assert(#s.idea_webs==1);return s,p,e,s.idea_webs[1]
    end
    local function count(events,kind)
        local n=0;for _,e in ipairs(events)do if e.kind==kind then n=n+1 end end;return n
    end
    test('Weaver commits a pure five-cell line and spends a finite cast before its anchors appear',function()
        local s,p,e=trial();local before=G.snapshot(s);local intent=AI.choose(s,e)
        equal(before,G.snapshot(s));assert(intent.support.kind=='web' and #intent.cells==5)
        assert(G.submit(s,{kind='wait'}));assert(e.pending_attack.due==206 and e.ai_memory.web_casts==1)
        assert(not s.idea_webs);assert(G.submit(s,{kind='wait'}));assert(not s.idea_webs)
        assert(G.submit(s,{kind='wait'}));local w=s.idea_webs[1]
        assert(w.ax==5 and w.bx==9 and w.ay==6 and w.by==6 and w.expires==606)
        e.ai_memory.web_casts=2
        assert(not AI.choose(s,e).support,'two attempts must exhaust the casting budget')
    end)
    test('walking or Dashing through several web cells adds one recovery delay without damage or RNG draws',function()
        for _,case in ipairs({{'move',450},{'dash',480}})do
            local s,p,e,w=created();local rng=s.rng.state
            local _,_,events=G.submit(s,{kind=case[1],dx=1,dy=0})
            assert(s.clock==case[2] and p.hp==24 and #s.idea_webs==0 and s.rng.state==rng)
            assert(count(events,'WebCrossed')==1 and G.restore(G.snapshot(s)))
        end
    end)
    test('an actual Rammer shove through a web delays the already queued player decision exactly once',function()
        local s,p,e,w=created()
        G.spawn_enemy(s,'core:enemy/heavy_rammer',6,6,300)
        assert(G.submit(s,{kind='wait'}))
        local _,_,events=G.submit(s,{kind='wait'})
        assert(p.position.x==8 and p.position.y==6 and p.hp==22 and s.clock==550)
        assert(count(events,'WebCrossed')==1 and #s.idea_webs==0 and not p.snare_delay)
        assert(G.submit(s,{kind='move',dx=0,dy=-1}));assert(s.clock==650,'forced web delay charged twice')
        assert(G.restore(G.snapshot(s)))
    end)
    test('a real bow shot cuts an anchor but cannot cut through an enemy killed by that same shot',function()
        local s,p,e,w=created('core:actor/ranger')
        local _,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(count(events,'WebAnchorCut')==1 and #s.idea_webs==0 and e.hp==4)
        s,p,e,w=created('core:actor/ranger')
        local blocker=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000);blocker.hp=2
        local _,_,blocked=G.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(not G.entity(s,blocker.id) and count(blocked,'WebAnchorCut')==0)
        assert(#s.idea_webs==1 and s.idea_webs[1].id==w.id and e.hp==7)
    end)
    test('occupying a locked anchor blocks placement without moving the endpoints or refunding the attempt',function()
        local s,p,e=trial();assert(G.submit(s,{kind='wait'}))
        assert(G.submit(s,{kind='move',dx=1,dy=0}))
        local _,_,events=G.submit(s,{kind='move',dx=1,dy=0})
        assert(p.position.x==9 and #(s.idea_webs or {})==0 and count(events,'WebBlocked')==1)
        assert(e.ai_memory.web_casts==1)
    end)
    test('stun cancels a pending weave and killing a caster clears placed lines before a valid save',function()
        local s,p,e=trial();assert(G.submit(s,{kind='wait'}));assert(G.stun(s,e.id,500))
        for _=1,3 do assert(G.submit(s,{kind='wait'}))end
        assert(e.ai_memory.web_casts==1 and #(s.idea_webs or {})==0)
        s,p,e=created('core:actor/ranger');e.hp=3
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(not G.entity(s,e.id) and #s.idea_webs==0 and G.restore(G.snapshot(s)))
    end)
    test('web expiry and restored continuation use world time while idle preview cannot expire or alter it',function()
        local s,p,e,w=created();local before=G.snapshot(s)
        for _=1,30 do AI.choose(s,e);G.preview(s,'dash',1,0)end
        equal(before,G.snapshot(s))
        local b=G.restore(C.decode(C.encode(before)))
        while s.clock<700 do
            local _,_,aev=G.submit(s,{kind='wait'});local _,_,bev=G.submit(b,{kind='wait'})
            equal(aev,bev);equal(G.snapshot(s),G.snapshot(b))
        end
        assert(#s.idea_webs==0 and G.restore(G.snapshot(s)))
        for _,edit in ipairs({function(v)v.ax=v.ax+1 end,
            function(v)v.expires=300 end,function(v)v.source_id=999999 end})do
            local snap=C.decode(C.encode(before));edit(snap.idea_webs[1])
            assert(not pcall(G.restore,snap))
        end
    end)
    test('frozen-floor web lifetime rebases by time away and ignores records belonging to another floor',function()
        local s={clock=800,entities={},schedule={},floor_pause={a={suspended_at=300,schedule={}}},
            idea_webs={{floor_id='a',expires=606},{floor_id='b',expires=900}}}
        require('src.floors').resume(s,'a')
        assert(s.idea_webs[1].expires==1106 and s.idea_webs[1].expires-s.clock==306)
        assert(s.idea_webs[2].expires==900)
    end)
    test('natural Weavers use approved native crawler 366, never the rejected mesh or bodies',function()
        local found=false
        for seed=1,100 do
            local s=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(s.entities)do if e.definition_id=='core:enemy/web_weaver'then found=true end end
        end
        assert(found)
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'onebit'})do
            assert(#report.packs[pack].missing==0)
            local seen=false
            for _,h in ipairs(report.packs[pack].bindings)do if h.id:match('^core:enemy/web_weaver')then
                assert(h.index==366)
                assert(h.index~=414 and h.index~=463 and h.index~=60);seen=true
            end end
            assert(seen)
        end
    end)
end
