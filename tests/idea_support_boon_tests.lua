return function(test,equal)
    local G=require('src.game');local B=require('src.boons');local C=require('src.boon_content')
    local P=require('src.persistence');local V=require('src.visibility');local Q=require('src.scheduler')
    local F=require('tests.idea_support_fixture')
    local function act(s,intent)
        local ok,why,events=G.submit(s,intent or {kind='wait'});assert(ok,why);return events
    end
    local function count(events,slug)
        local n=0;for _,e in ipairs(events)do if e.kind=='BoonActivated' and e.boon_id=='core:boon/'..slug then n=n+1 end end;return n
    end
    local function give(s,slug,tier,n)B.set_stacks(G.player(s),'core:boon/'..slug,tier or 'rare',n or 1)end
    test('fifty new boons retain the fifty-three originals and distinct suitable native icons',function()
        local I=require('src.idea_boon_content');assert(#I.order==50 and #C.order==103 and C.validate())
        local seen={};for _,id in ipairs(I.order)do local art=C.get(id).art
            assert(not seen[art.index],id..' duplicates '..tostring(seen[art.index]));seen[art.index]=id
            assert(require('src.kenney_symbols').boons[id][1]==art.index)
        end
        local d=P.decode(P.encode(C.get('core:boon/mist_lullaby')));d.effects[1].cooldown=0
        assert(not pcall(C.validate,{[d.id]=d},{d.id}))
    end)
    for _,slug in ipairs(F.order)do
        test(slug..' activates through a real paid action, pure preview and exact saved continuation',function()
            local f=F.prepare(slug);local s=f.state;local before=G.snapshot(s)
            for _=1,5 do G.preview(s,f.intent.kind,f.intent.dx,f.intent.dy)end
            equal(before,G.snapshot(s))
            F.check(f,act(s,f.intent))
            local b=G.restore(P.decode(P.encode(G.snapshot(s))))
            for _=1,4 do equal(act(s),act(b));equal(G.snapshot(s),G.snapshot(b))end
        end)
    end
    test('Doorward cannot be renewed by walking through or retreating through an open door',function()
        local f=F.prepare('doorward_lamp');F.check(f,act(f.state,f.intent))
        for _,dx in ipairs({1,-1,1})do assert(count(act(f.state,{kind='move',dx=dx,dy=0}),f.slug)==0)end
    end)
    test('Secret Apple is finite and full-health recovery cannot create overheal',function()
        local f=F.prepare('secret_apple');f.player.hp=f.player.max_hp
        F.install(f);local events=act(f.state,f.intent);assert(count(events,f.slug)==1 and f.player.hp==f.player.max_hp)
        assert(count(act(f.state,f.intent),f.slug)==0)
    end)
    test('Spoils Aegis cannot bypass chest affordability or activate on free elite rewards',function()
        local f=F.prepare('spoils_aegis');f.state.currency=f.chest.price-1
        local before=G.snapshot(f.state);assert(not G.submit(f.state,f.intent));equal(before,G.snapshot(f.state))
        f.chest.definition_id='core:chest/free_elite';f.chest.price=0
        assert(count(act(f.state,f.intent),f.slug)==0)
    end)
    test('Mist Lullaby has an exact 400-time cooldown and full health cannot start it',function()
        local f=F.prepare('mist_lullaby');local s,p=f.state,f.player
        assert(count(act(s),f.slug)==1)
        for _=1,3 do assert(count(act(s),f.slug)==0)end
        assert(s.clock==400 and count(act(s),f.slug)==1)
        f=F.prepare('mist_lullaby');f.player.hp=f.player.max_hp
        assert(count(act(f.state),f.slug)==0 and not f.player.boon_memory)
    end)
    test('ordinary ground and waiting on ice cannot masquerade as a Rime departure',function()
        local f=F.prepare('rime_disk');assert(count(act(f.state),f.slug)==0)
        local s=G.new(37);give(s,'rime_disk')
        assert(count(act(s,{kind='move',dx=1,dy=0}),'rime_disk')==0)
    end)
    test('Vent Mantle preserves the strict locked impact and cannot renew while standing still',function()
        local f=F.prepare('vent_mantle');local s=f.state
        local due=f.vent.due;F.check(f,act(s,f.intent));assert(s.clock==due and f.vent.phase=='cooldown')
        assert(count(act(s),f.slug)==0)
        local s2=G.new(37);give(s2,'vent_mantle')
        assert(count(act(s2,{kind='move',dx=1,dy=0}),'vent_mantle')==0)
    end)
    test('Patient Rebuke spends a single charge and cannot be charged by Vow cost or an empty Wait',function()
        local f=F.prepare('patient_rebuke');F.check(f,act(f.state))
        assert(count(act(f.state),f.slug)==0)
        local s=G.new(37);give(s,'patient_rebuke');give(s,'pilgrims_vow')
        G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,100000)
        act(s,{kind='basic_attack',dx=1,dy=0,vow=true})
        assert(count(act(s),'patient_rebuke')==0)
    end)
    test('Beacon Afterglow previews landing rays without showing the ordinary Dash wake',function()
        local f=F.prepare('beacon_afterglow');local s=f.state;give(s,'backtrail_mantle')
        local before=G.snapshot(s);local preview=G.preview(s,'dash',1,0)
        assert(preview.beacon_return and preview.boon_hint=='beacon landing rays' and #preview.boon_cells>0)
        for _,c in ipairs(preview.boon_cells)do assert(c.boon_id=='core:boon/beacon_afterglow' and (c.x==6 or c.y==6))end
        equal(before,G.snapshot(s));F.check(f,act(s,f.intent));assert(#G.pending_boon_effects(s)==0)
        assert(count(act(s,{kind='dash',dx=1,dy=0}),f.slug)==0)
    end)
    test('Sentry deployment speed affects subsequent costs while autonomous bolts remain isolated',function()
        local f=F.prepare('sentry_quartermaster');local preview=G.preview(f.state,'sweeping_slash',1,0)
        F.check(f,act(f.state,f.intent));assert(f.state.last_action.cost==preview.cost)
        assert(G.preview(f.state,'wait').cost==G.effective_cost(100,f.player.speed+20))
        give(f.state,'storm_conductor','legendary',100)
        local foe=G.spawn_enemy(f.state,'core:enemy/mossbound_guard',10,6,100000)
        for _=1,4 do
            local events=act(f.state)
            assert(count(events,'storm_conductor')==0 and count(events,f.slug)==0)
        end
        assert(foe.hp<10)
    end)
    test('Spirit Shelter is positioning protection and cannot be renewed by a commanded strike',function()
        local f=F.prepare('spirit_shelter');F.check(f,act(f.state,f.intent))
        local e=G.spawn_enemy(f.state,'core:enemy/mossbound_guard',10,6,100000)
        local events=act(f.state,{kind='sweeping_slash',dx=1,dy=0})
        assert(e.hp<10 and count(events,f.slug)==0)
    end)
    test('Grounding Rod cannot create charge from an empty Dash and retains both distinct shields',function()
        local f=F.prepare('grounding_rod');F.check(f,act(f.state,f.intent))
        assert(f.player.storm_charge==0 and f.player.storm_ward and B.ward_total(f.state,f.player)==3)
        assert(count(act(f.state,{kind='dash',dx=1,dy=0}),f.slug)==0)
    end)
    test('Mirror Dividend is one heal on a successful reflected player attack and none on blocked routing',function()
        local f=F.prepare('mirror_dividend');local blocker=G.spawn_enemy(f.state,'core:enemy/mossbound_guard',8,6,100000)
        assert(count(act(f.state,f.intent),f.slug)==0 and f.player.hp==f.hp_before)
        assert(blocker.hp<10 and #f.state.idea_mirrors==1)
    end)
    test('uncapped mixed rarity copies aggregate once while healing and wards retain their limits',function()
        local f=F.prepare('mist_lullaby');give(f.state,f.slug,'common',100);give(f.state,f.slug,'legendary',100)
        assert(count(act(f.state),f.slug)==1 and f.player.hp==f.player.max_hp)
        local f=F.prepare('spirit_shelter');give(f.state,f.slug,'common',100)
        assert(count(act(f.state,f.intent),f.slug)==1)
        assert(B.ward_total(f.state,f.player)==204)
    end)
    test('character status shows active boon wards and expires them without spending or renewing resources',function()
        local f=F.prepare('spirit_shelter');act(f.state,f.intent)
        local before=G.snapshot(f.state);local UI=require('src.idea_presentation')
        local statuses=UI.statuses(f.player,f.state.clock)
        assert(statuses[1].label=='Ward 4')
        assert(#UI.statuses(f.player,151)==0)
        equal(before,G.snapshot(f.state))
    end)
    test('last dice roll has separate visible history even when active status slots are full',function()
        local UI=require('src.idea_presentation')
        local p={definition_id='core:actor/stormcaller',storm_charge=3,pickup_ward_actions=3,
            supply_focus_actions=3,statuses={burning={remaining=3}},
            boons={['core:boon/fateful_dice']={common=1}},
            boon_memory={['core:boon/fateful_dice']={face=7,roll_action_id=1}}}
        local before=P.encode(p);assert(#UI.statuses(p,100)==4)
        local last=UI.last_roll(p);assert(last.id=='core:ui/fate_7_icon' and last.label=='Last roll: SKULL')
        assert(P.encode(p)==before)
    end)
end
