return function(test,equal)
    local G=require('src.game');local C=require('src.persistence');local D=require('src.idea_duelist')
    local function trial(definition,x,y,ready)
        local s=G.new(37,nil,{class_id=D.id});local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,definition or 'core:enemy/mossbound_guard',x or 8,y or 6,ready or 0)
        return s,p,e
    end
    local function parry(s,dx,dy)
        local ok,why,events=G.submit(s,{kind='sweeping_slash',dx=dx or 1,dy=dy or 0})
        assert(ok,why);return events
    end
    local function ready()
        local s,p,e=trial();parry(s);parry(s)
        assert(p.hp==20 and p.duelist_riposte and s.clock==174)
        return s,p,e
    end
    local function count(events,kind)
        local n=0;for _,v in ipairs(events)do if v.kind==kind then n=n+1 end end;return n
    end
    test('Duelist explicit thrust is a pure two-cell lane while hybrid bump retains one target',function()
        local s,p,e=trial(nil,nil,nil,100000)
        local other=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
        local before=G.snapshot(s)
        for _=1,30 do local aim=G.preview(s,'basic_attack',1,0)
            assert(aim.valid and aim.cost==87 and #aim.cells==2)
        end
        equal(before,G.snapshot(s));assert(G.submit(s,{kind='move',dx=1,dy=0}))
        assert(e.hp==7 and other.hp==10 and p.position.x==7 and s.clock==87)
        s,p,e=trial(nil,nil,nil,100000);other=G.spawn_enemy(s,'core:enemy/mossbound_guard',9,6,100000)
        local _,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(e.hp==7 and other.hp==7 and count(events,'DamageBatchResolved')==1)
    end)
    test('parry pays its real recovery and catches only a melee impact inside that committed window',function()
        local s,p,e=trial();local rng=s.rng.state
        parry(s);assert(s.clock==87 and not p.duelist_riposte and not s.class_special.duelist_guard)
        local events=parry(s)
        assert(s.clock==174 and p.hp==20 and p.duelist_riposte and e.hp==10 and s.rng.state==rng)
        assert(count(events,'DuelistParried')==1 and count(events,'DamageBlocked')==1)
        assert(not s.class_special.duelist_guard and G.restore(G.snapshot(s)))
    end)
    test('a successful parry funds one paid plus-two thrust and never an automatic counterattack',function()
        local s,p,e=ready();local before=G.snapshot(s)
        for _=1,30 do local _,damage=G.effective_ability(s,'basic_attack',1,0,true);assert(damage==6)end
        equal(before,G.snapshot(s));assert(e.hp==10)
        local _,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(s.clock==261 and e.hp==5 and not p.duelist_riposte and count(events,'DuelistRiposteSpent')==1)
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(e.hp==2 and s.clock==348)
    end)
    test('rear strikes ranged shots and later Burning ticks bypass a frontal melee parry',function()
        local s,p,e=trial(nil,6,6);parry(s);parry(s)
        assert(p.hp==17 and not p.duelist_riposte)
        s,p,e=trial('core:enemy/thornspitter',10,6,50);parry(s);parry(s)
        assert(p.hp==18 and not p.duelist_riposte)
        s,p,e=trial(nil,6,6)
        G.set_boon_stacks(s,e.id,'core:boon/emberbrand','common',1)
        parry(s);parry(s);assert(p.statuses.burning)
        parry(s);assert(p.hp==15 and not p.duelist_riposte)
    end)
    test('one stance blocks at most one hit even when two adjacent attackers impact at the same timestamp',function()
        local s,p,e=trial('core:enemy/ruin_skitter',8,5,100)
        G.spawn_enemy(s,'core:enemy/ruin_skitter',8,7,100)
        parry(s);local events=parry(s)
        assert(p.hp==19 and p.duelist_riposte and count(events,'DuelistParried')==1)
        assert(count(events,'DamageTaken')==1 and count(events,'DamageBlocked')==1)
    end)
    test('raising a guard deals no damage and cannot manufacture an attack or Temporal Echo',function()
        local s,p,e=trial(nil,nil,nil,100000)
        G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
        local rng=s.rng.state;local events=parry(s)
        assert(e.hp==10 and p.hp==20 and not p.duelist_riposte and not next(s.delayed_effects))
        assert(count(events,'AttackPerformed')==0 and count(events,'AbilityUsed')==0 and s.rng.state==rng)
    end)
    test('a charged miss spends the riposte once and rejected input cannot spend it',function()
        local s,p,e=ready();local before=G.snapshot(s)
        assert(not G.submit(s,{kind='basic_attack',dx=0,dy=0}));equal(before,G.snapshot(s))
        assert(G.submit(s,{kind='basic_attack',dx=-1,dy=0}))
        assert(not p.duelist_riposte and e.hp==10 and s.clock==261)
    end)
    test('a physical two-cell thrust ignores a reflector instead of becoming a projectile',function()
        local s,p,e=trial('core:enemy/mirror_hexer',10,6,50)
        while s.clock<250 do assert(G.submit(s,{kind='wait'}))end
        assert(#s.idea_mirrors==1);p.position.x=8
        local aim=G.preview(s,'basic_attack',1,0)
        assert(not aim.reflection and aim.cells[2].x==10 and aim.cells[2].y==6)
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}));assert(e.hp==3 and #s.idea_mirrors==1)
    end)
    test('one-cell sidestep and thrust use actual terrain restrictions without diagonal corner penetration',function()
        local s,p,e=trial(nil,nil,nil,100000)
        local aim=G.preview(s,'dash',0,1);assert(aim.valid and aim.distance==1 and aim.cost==74)
        assert(G.submit(s,{kind='dash',dx=0,dy=1}));assert(p.position.y==7 and s.clock==74)
        p.position.x,p.position.y=8,4
        aim=G.preview(s,'basic_attack',1,1)
        assert(#aim.cells==1 and aim.cells[1].x==9 and aim.cells[1].y==5)
        local behind=G.spawn_enemy(s,'core:enemy/mossbound_guard',10,6,100000)
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=1}));assert(behind.hp==10)
    end)
    test('held riposte roundtrips exactly and forged charges or outliving parry windows reject',function()
        local s,p,e=ready();local before=G.snapshot(s)
        local b=G.restore(C.decode(C.encode(before)))
        for _=1,3 do
            local intent={kind='basic_attack',dx=1,dy=0}
            local _,_,first=G.submit(s,intent);local _,_,second=G.submit(b,intent)
            equal(first,second);equal(G.snapshot(s),G.snapshot(b))
        end
        for _,change in ipairs({function(v)for _,a in ipairs(v.entities)do if a.id==v.player_id then a.duelist_riposte=false end end end,
            function(v)v.class_special.duelist_guard={expires=v.clock+100}end,
            function(v)for _,a in ipairs(v.entities)do if a.controller=='ai'then a.duelist_riposte=true end end end})do
            local snap=C.decode(C.encode(before));change(snap);assert(not pcall(G.restore,snap))
        end
    end)
    test('Duelist enters genuine paired generation and can defeat an ordinary first foe by legal actions',function()
        local Play=require('tests.legal_playthrough');local run=Play.new(9,{multifloor=true,class_id=D.id})
        local s=run.state;assert(G.player(s).definition_id==D.id)
        local enemy=assert(Play.nearest(s,function(e)return e.controller=='ai' and not e.invulnerable end))
        assert(Play.fight(run,enemy.id,180));assert(not G.entity(s,enemy.id) and #run.trace>0)
        assert(G.restore(G.snapshot(s)))
    end)
    test('both sprite choices resolve the reviewed Duelist and report its deliberate Twin body reuse',function()
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'kenney','onebit'})do
            local found=false;assert(#report.packs[pack].missing==0)
            for _,h in ipairs(report.packs[pack].bindings)do if h.id==D.id then
                assert(h.sheet==(pack=='kenney' and 'tiny' or 'onebit')
                    and h.index==(pack=='kenney' and 99 or 124));found=true
            end end
            local shared=false
            for _,row in ipairs(report.packs[pack].shared_actors)do
                for _,id in ipairs(row.ids)do if id==D.id then shared=true end end
            end
            assert(found and shared)
        end
    end)
end
