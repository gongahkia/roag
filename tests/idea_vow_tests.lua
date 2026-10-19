return function(test,equal)
    local G=require('src.game')
    local B=require('src.boons')
    local C=require('src.boon_content')
    local Classes=require('src.classes')
    local Comp=require('src.idea_companions')
    local V=require('src.visibility')
    local Content=require('src.content')
    local P=require('src.persistence')
    local id='core:boon/pilgrims_vow'
    local function fixture(class)
        local s=G.new(37,nil,{class_id=class})
        G.set_boon_stacks(s,s.player_id,id,'rare',1)
        return s,G.player(s)
    end
    local function foe(s,x,y)
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',x or 3,y or 2,100000)
        e.hp=100;e.max_hp=100;return e
    end
    local function submit(s,intent)
        local ok,why,events=G.submit(s,intent);assert(ok,why);return events
    end
    local function count(events,kind)
        local n=0;for _,e in ipairs(events)do if e.kind==kind then n=n+1 end end;return n
    end
    local function attack_event(events)
        for _,e in ipairs(events)do if e.kind=='AttackPerformed' then return e end end
        error('no immediate attack')
    end
    test('the real game callbacks compile within LuaJIT upvalue limits after adding vow controls',function()
        assert(loadfile('main.lua'))
    end)
    test('Pilgrim preview shows exact HP payment and bonus without altering run, RNG or recovery',function()
        local s,p=fixture();foe(s)
        local before=G.snapshot(s);local base=G.preview(s,'basic_attack',1,0)
        for _=1,30 do
            local preview=G.preview(s,'basic_attack',1,0,nil,{vow=true})
            assert(preview.valid and preview.vow.hp_cost==2 and preview.vow.bonus==6)
            assert(preview.damage==p.damage+6 and preview.cost==base.cost)
            assert(Classes.preview_hint(p,'basic_attack',preview):find('2 HP',1,true))
        end
        equal(before,G.snapshot(s))
    end)
    test('Pilgrim rejects unaffordable HP and cannot make an attack commitment lethal by payment',function()
        for _,hp in ipairs({1,2,3})do
            local s,p=fixture();p.hp=hp;foe(s)
            local before=G.snapshot(s)
            local preview=G.preview(s,'basic_attack',1,0,nil,{vow=true})
            if hp<=2 then
                assert(not preview.valid and preview.reason:find('leave 1 HP',1,true))
                assert(not G.submit(s,{kind='basic_attack',dx=1,dy=0,vow=true}))
                equal(before,G.snapshot(s))
            else
                assert(preview.valid)
                submit(s,{kind='basic_attack',dx=1,dy=0,vow=true})
                assert(p.hp==1)
            end
        end
    end)
    test('Pilgrim owns no class slot and every existing class can pay on its immediate basic attack',function()
        assert(#Classes.order==15)
        for _,class in ipairs(Classes.order)do
            local s,p=fixture(class);local e=foe(s);local hp=p.hp
            local _,base=G.effective_ability(s,'basic_attack',1,0,true)
            local preview=G.preview(s,'basic_attack',1,0,nil,{vow=true})
            assert(preview.valid,class)
            local events=submit(s,{kind='basic_attack',dx=1,dy=0,vow=true})
            local hit=attack_event(events)
            local expected=base+6
            if hit.critical then expected=math.floor(expected*p.crit_damage_percent/100+.5)end
            assert(hit.raw_damage==expected and p.hp==hp-2 and s.clock==preview.cost,class)
            assert(e.hp==100-math.max(0,expected-e.defense),class)
            assert(count(events,'VowPaid')==1)
        end
    end)
    test('one Pilgrim payment empowers the whole primary sweep batch before secondary effects',function()
        local s,p=fixture();p.position.x,p.position.y=7,6;V.recompute(s)
        local targets={}
        for _,cell in ipairs(G.preview(s,'sweeping_slash',1,0).cells)do
            assert(Content.walkable(Content.map_for(s),cell.x,cell.y))
            targets[#targets+1]=foe(s,cell.x,cell.y)
        end
        assert(#targets==3)
        local _,base=G.effective_ability(s,'sweeping_slash',1,0,true)
        local hp=p.hp;local events=submit(s,{kind='sweeping_slash',dx=1,dy=0,vow=true})
        assert(count(events,'VowPaid')==1 and count(events,'DamageBatchResolved')==1 and p.hp==hp-2)
        for _,e in ipairs(targets)do assert(e.hp==100-math.max(0,base+6-e.defense))end
        local batch
        for _,e in ipairs(events)do if e.kind=='DamageBatchResolved' then batch=e end end
        assert(#batch.outcomes==3)
    end)
    test('Pilgrim bonus is included before one critical roll and preserves critical RNG consumption',function()
        local a,p=fixture();local b,q=fixture();foe(a);foe(b)
        p.crit_chance_ppm=1000000;q.crit_chance_ppm=1000000
        local _,base=G.effective_ability(a,'basic_attack',1,0,true)
        local ae=submit(a,{kind='basic_attack',dx=1,dy=0,vow=true})
        local be=submit(b,{kind='basic_attack',dx=1,dy=0})
        assert(attack_event(ae).critical and attack_event(be).critical)
        assert(attack_event(ae).raw_damage==math.floor((base+6)*p.crit_damage_percent/100+.5))
        assert(a.rng.state==b.rng.state and a.clock==b.clock)
    end)
    test('Pilgrim HP payment is a cost rather than a wardable hit, retaliation or healing toggle',function()
        local s,p=fixture();foe(s);p.hp=10;p.invulnerable=true
        G.set_boon_stacks(s,p.id,'core:boon/betrayal_ward','rare',1)
        G.set_boon_stacks(s,p.id,'core:boon/pain_dividend','rare',1)
        G.set_boon_stacks(s,p.id,'core:boon/thorn_mirror','rare',1)
        p.boon_memory={['core:boon/betrayal_ward']={ward=3,expires_at=500}}
        local events=submit(s,{kind='basic_attack',dx=1,dy=0,vow=true})
        assert(p.hp==8 and p.boon_memory['core:boon/betrayal_ward'].ward==3)
        assert(not p.boon_memory['core:boon/pain_dividend'])
        assert(count(events,'Healed')==0 and count(events,'BoonActivated')==0)
        for _,e in ipairs(events)do assert(not(e.kind=='DamageTaken' and e.target_id==p.id))end
    end)
    test('Pilgrim requires ownership, rejects forged flags and never pays on movement, waits or chests',function()
        local plain=G.new(37);local before=G.snapshot(plain)
        assert(not G.submit(plain,{kind='basic_attack',dx=1,dy=0,vow=true}))
        equal(before,G.snapshot(plain))
        local s=fixture();local chest=G.spawn_chest(s,'core:chest/standard',3,2);s.currency=100
        for _,intent in ipairs({{kind='basic_attack',dx=1,dy=0,vow=1},
            {kind='move',dx=1,dy=0,vow=true},{kind='dash',dx=0,dy=1,vow=true},
            {kind='wait',vow=true},{kind='interact',vow=true},
            {kind='claim_chest',chest_id=chest.id,offer_index=1,vow=true}})do
            before=G.snapshot(s);assert(not G.submit(s,intent));equal(before,G.snapshot(s))
        end
    end)
    test('bump attacks are ordinary and cannot silently spend Pilgrim HP',function()
        local s,p=fixture();foe(s);local hp=p.hp
        local events=submit(s,{kind='move',dx=1,dy=0})
        assert(p.hp==hp and count(events,'VowPaid')==0)
        assert(attack_event(events).raw_damage==p.damage)
    end)
    test('Pilgrim cancellation and an empty miss do not create a reusable empowered charge',function()
        local s,p=fixture();local before=G.snapshot(s)
        assert(not G.submit(s,{kind='cancel'}));equal(before,G.snapshot(s))
        local events=submit(s,{kind='basic_attack',dx=1,dy=0,vow=true})
        assert(count(events,'VowPaid')==1 and p.hp==22)
        events=submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(count(events,'VowPaid')==0 and attack_event(events).raw_damage==p.damage and p.hp==22)
    end)
    test('non-attack special skills refuse Pilgrim payment even when ordinary placement is valid',function()
        for _,class in ipairs({'core:actor/duelist','core:actor/geomancer','core:actor/tinkerer',
            'core:actor/wayfinder','core:actor/shade','core:actor/trapper','core:actor/spiritbinder'})do
            local s,p=fixture(class)
            assert(not Classes.immediate_attack(p,'sweeping_slash',{companion_mode='place'}))
            local before=G.snapshot(s)
            assert(not G.preview(s,'sweeping_slash',1,0,nil,{vow=true}).valid)
            assert(not G.submit(s,{kind='sweeping_slash',dx=1,dy=0,vow=true}))
            equal(before,G.snapshot(s))
        end
        local s,p=fixture('core:actor/stormcaller');p.storm_charge=3
        assert(not Classes.immediate_attack(p,'sweeping_slash',{}))
        assert(not G.preview(s,'sweeping_slash',1,0,nil,{vow=true}).valid)
    end)
    test('a real commanded Spirit attack pays on its owner and adds the bonus exactly once',function()
        local s,p=fixture('core:actor/spiritbinder')
        Comp.place(s,p,{x=3,y=2},function()end)
        local e=foe(s,4,2)
        local preview=G.preview(s,'sweeping_slash',1,0,nil,{vow=true})
        assert(preview.valid and preview.companion_mode=='strike')
        local hp=p.hp;local events=submit(s,{kind='sweeping_slash',dx=1,dy=0,vow=true})
        assert(p.hp==hp-2 and count(events,'VowPaid')==1 and count(events,'AttackPerformed')==1)
        assert(e.hp==100-math.max(0,p.slash_damage+6-e.defense))
    end)
    test('Pilgrim does not empower delayed Echo for free and paid state resumes exactly',function()
        local s,p=fixture();foe(s)
        G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','rare',1)
        submit(s,{kind='basic_attack',dx=1,dy=0,vow=true})
        assert(next(s.delayed_effects))
        local restored=G.restore(P.decode(P.encode(G.snapshot(s))))
        local ae=submit(s,{kind='wait'});local be=submit(restored,{kind='wait'})
        equal(ae,be);equal(G.snapshot(s),G.snapshot(restored))
        assert(p.hp==22 and count(ae,'VowPaid')==0)
        local got=false
        for _,e in ipairs(ae)do
            if e.kind=='DamageTaken' and not e.direct_attack then
                assert(e.attempted_damage==C.get('core:boon/temporal_echo').parameters.rare.power)
                got=true
            end
        end
        assert(got)
    end)
    test('mixed Pilgrim rarities add power while HP price stays fixed and the declaration validates',function()
        local s,p=fixture()
        G.set_boon_stacks(s,p.id,id,'common',5)
        G.set_boon_stacks(s,p.id,id,'legendary',4)
        local vow=B.attack_vow(p)
        assert(vow.hp_cost==2 and vow.bonus==15+6+32)
        local def=P.decode(P.encode(C.get(id)));def.attack_payment.hp=0
        assert(not pcall(C.validate,{[id]=def},{id}))
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
end
