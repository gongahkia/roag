return function(test,equal)
    local G=require('src.game')
    local C=require('src.persistence')
    local O=require('src.idea_oaths')
    local function trial(bx,by)
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local a=G.spawn_enemy(s,O.a,8,6,100000)
        local b=G.spawn_enemy(s,O.b,bx or 9,by or 6,100000)
        a.oath_partner_id=b.id;b.oath_partner_id=a.id
        return s,p,a,b
    end
    local function count(events,kind)
        local n=0;for _,e in ipairs(events)do if e.kind==kind then n=n+1 end end;return n
    end
    test('an intact oath splits mitigated loss in one batch with one real hit and no RNG draw',function()
        local s,p,a,b=trial();local before=G.snapshot(s)
        assert(O.partner(s,a)==b and O.partner(s,b)==a);equal(before,G.snapshot(s))
        local _,_,ev=G.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(a.hp==6 and b.hp==6 and s.rng.state==before.rng.state and s.clock==100)
        assert(count(ev,'DamageBatchResolved')==1 and count(ev,'HitConfirmed')==1)
        local seen=false
        for _,e in ipairs(ev)do if e.kind=='DamageTaken' and e.target_id==b.id then
            assert(e.oath_shared_only and not e.direct_attack and e.proc_coefficient==0
                and e.actual_loss==2 and e.damage_type=='oath');seen=true
        end end
        assert(seen)
    end)
    test('an area hit calculates both original losses before exchanging either share',function()
        local s,p,a,b=trial(8,5)
        local _,_,ev=G.submit(s,{kind='sweeping_slash',dx=1,dy=0})
        assert(a.hp==5 and b.hp==5 and count(ev,'HitConfirmed')==2)
        for _,e in ipairs(ev)do if e.kind=='DamageBatchResolved'then
            assert(#e.outcomes==2)
            for _,hit in ipairs(e.outcomes)do
                assert(hit.hp_before==8 and hit.actual_loss==3 and hit.oath_given==1
                    and hit.oath_received==1 and not hit.oath_shared_only)
            end
        end end
    end)
    test('flat defense applies before sharing and is never charged a second time at the recipient',function()
        local s,p,a,b=trial();a.defense=2;b.defense=99
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(a.hp==7 and b.hp==7)
    end)
    test('shared HP loss cannot manufacture a second hit or a recursive Resonant Wounds proc',function()
        local s,p,a,b=trial()
        G.set_boon_stacks(s,p.id,'core:boon/resonant_wounds','common',1)
        local _,_,ev=G.submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(a.hp==6 and b.hp==6 and count(ev,'BoonActivated')==0 and count(ev,'HitConfirmed')==1)
    end)
    test('real simultaneous twin deaths pay each original reward once and retain finite on-kill boons',function()
        local s,p,a,b=trial(8,5);a.hp=2;b.hp=2;p.hp=10
        G.set_boon_stacks(s,p.id,'core:boon/grave_dividend','common',1)
        local _,_,ev=G.submit(s,{kind='sweeping_slash',dx=1,dy=0})
        assert(not G.entity(s,a.id) and not G.entity(s,b.id) and s.progression.xp_total==8)
        assert(count(ev,'EntityKilled')==2 and count(ev,'BoonActivated')==2 and #s.drops==2)
        assert(G.restore(G.snapshot(s)))
    end)
    test('distance displacement breaks the oath permanently and returning does not reconnect it',function()
        local s,p,a,b=trial()
        assert(G.displace(s,b.id,13,6));assert(G.submit(s,{kind='wait'}))
        assert(a.oath_broken and b.oath_broken and not O.partner(s,a))
        assert(G.displace(s,b.id,9,6));assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(a.hp==4 and b.hp==8 and not O.partner(s,b) and G.restore(G.snapshot(s)))
    end)
    test('a blocked sight line breaks the oath instead of sharing through walls or restoring after reopening',function()
        local s,p,a,b=trial(10,6)
        s.generated_map=C.decode(C.encode(require('src.content').map_for(s)))
        local row=s.generated_map.rows[6]
        s.generated_map.rows[6]=row:sub(1,8)..'#'..row:sub(10)
        O.prune(s);assert(a.oath_broken and b.oath_broken)
        s.generated_map.rows[6]=row
        assert(not O.partner(s,a))
    end)
    test('saved pair ownership and broken state resume exactly and forged partners reject',function()
        local s,p,a,b=trial();assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        local restored=G.restore(C.decode(C.encode(G.snapshot(s))))
        for _,intent in ipairs({{kind='basic_attack',dx=1,dy=0},{kind='sweeping_slash',dx=1,dy=0}})do
            local _,_,first=G.submit(s,intent);local _,_,second=G.submit(restored,intent)
            equal(first,second);equal(G.snapshot(s),G.snapshot(restored))
        end
        local snap=G.snapshot(s)
        for _,e in ipairs(snap.entities)do if e.id==a.id then e.oath_partner_id=s.player_id end end
        assert(not pcall(G.restore,snap))
    end)
    test('natural groups contain both reviewed twins and never spawn an ownerless secondary twin',function()
        local found=0
        for seed=1,100 do
            local s=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(s.entities)do if O.is_twin(e)then
                assert(O.partner(s,e) and G.entity(s,e.oath_partner_id))
                if e.definition_id==O.a then found=found+1 end
            end end
            equal(G.snapshot(s),G.snapshot(G.restore(C.decode(C.encode(G.snapshot(s))))))
        end
        assert(found>0)
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'kenney','onebit'})do
            assert(#report.packs[pack].missing==0)
            local seen=0
            for _,h in ipairs(report.packs[pack].bindings)do
                if h.id==O.a then assert(h.index==(pack=='kenney' and 99 or 124));seen=seen+1 end
                if h.id==O.b then assert(h.index==(pack=='kenney' and 100 or 127));seen=seen+1 end
                if h.id=='core:ui/idea_oath_effect'then assert(h.index==574);seen=seen+1 end
            end
            assert(seen==3)
        end
    end)
end
