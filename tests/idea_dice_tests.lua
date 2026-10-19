return function(test,equal)
    local G=require('src.game')
    local C=require('src.boon_content')
    local B=require('src.boons')
    local I=require('src.idea_boon_content')
    local RNG=require('src.rng')
    local P=require('src.persistence')
    local Classes=require('src.classes')
    local V=require('src.visibility')
    local Content=require('src.content')
    local Oaths=require('src.idea_oaths')
    local id='core:boon/fateful_dice'
    local seeds={}
    for seed=1,80 do
        local s=G.new(seed);local copy={state=s.rng.state}
        local face=RNG.next_int(copy,8)
        seeds[face]=seeds[face] or seed
    end
    local function fixture(face,class)
        local s=G.new(assert(seeds[face]),nil,{class_id=class})
        G.set_boon_stacks(s,s.player_id,id,'rare',1)
        return s,G.player(s)
    end
    local function foe(s,x,y,definition)
        local e=G.spawn_enemy(s,definition or 'core:enemy/mossbound_guard',x or 3,y or 2,100000)
        e.hp=100;e.max_hp=100;return e
    end
    local function action(s,intent)
        local ok,why,events=G.submit(s,intent or {kind='basic_attack',dx=1,dy=0})
        assert(ok,why);return events
    end
    local function count(events,kind)
        local n=0;for _,e in ipairs(events)do if e.kind==kind then n=n+1 end end;return n
    end
    local function rolled(events,face)
        assert(count(events,'FateRolled')==1)
        for _,e in ipairs(events)do if e.kind=='FateRolled' then
            assert(e.face==face and e.binding_id=='core:ui/fate_'..face..'_icon');return e
        end end
    end
    test('dice declares six numbered faces, skull and miss with exact approved native bindings',function()
        assert(#I.cues==8 and #I.faces==8 and C.validate())
        local slots=require('src.visual_slots').bindings
        for n,cue in ipairs(I.cues)do
            assert(seeds[n] and slots[cue.id] and cue.id=='core:ui/fate_'..n..'_icon')
            assert(cue.index==(n<=6 and 727+n or n==7 and 727 or 726))
        end
        assert(C.get(id).art.index==728)
        assert(#require('src.sprite_audit').collect().requirements==491+2*#I.order+#I.cues)
    end)
    test('dice preview and canceled or invalid input never roll or consume RNG',function()
        local s=fixture(8);foe(s);local before=G.snapshot(s)
        for _=1,30 do
            local preview=G.preview(s,'basic_attack',1,0)
            assert(preview.valid and preview.fate_power==3)
            assert(Classes.preview_hint(G.player(s),'basic_attack',preview):find('commit',1,true))
            assert(not G.submit(s,{kind='move',dx=1,dy=1}))
            assert(not G.submit(s,{kind='cancel'}))
        end
        equal(before,G.snapshot(s))
    end)
    test('every numbered or special outcome consumes one bounded RNG draw on a committed empty attack',function()
        for face=1,8 do
            local s,p=fixture(face);local expected={state=s.rng.state}
            assert(RNG.next_int(expected,8)==face)
            local events=action(s);rolled(events,face)
            assert(s.rng.state==expected.state and s.clock==100)
            assert(p.boon_memory[id].face==face and p.boon_memory[id].roll_action_id==1)
            equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
        end
    end)
    test('face one heals once after real primary damage and cannot farm empty attacks or supply overflow',function()
        local s,p=fixture(1);p.hp=10;foe(s)
        G.set_boon_stacks(s,p.id,'core:boon/overflow_chalice','rare',1)
        local events=action(s);rolled(events,1)
        assert(p.hp==13 and count(events,'Healed')==1)
        assert(not p.boon_memory['core:boon/overflow_chalice'])
        local a,b=fixture(1);b.hp=10
        events=action(a);assert(b.hp==10 and count(events,'Healed')==0)
    end)
    test('face two boosts the primary batch before defense without changing action cost',function()
        local s=fixture(2);local e=foe(s)
        local before=G.preview(s,'basic_attack',1,0).cost
        local events=action(s);rolled(events,2)
        assert(e.hp==94 and s.clock==before)
        for _,event in ipairs(events)do if event.kind=='AttackPerformed' then assert(event.raw_damage==7)end end
    end)
    test('face three ignites actual hit targets for three real scheduled ticks',function()
        local s=fixture(3);local e=foe(s)
        local events=action(s);rolled(events,3)
        assert(e.statuses.burning and e.statuses.burning.power==3)
        assert(e.statuses.burning.remaining==2 and e.statuses.burning.next_due==200)
        assert(count(events,'StatusApplied')==1 and count(events,'StatusTick')==1 and e.hp==95)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('face four stores a finite one-hit ward without erasing saved face or renewing on other rolls',function()
        local s,p=fixture(4);foe(s)
        local events=action(s);rolled(events,4)
        local memory=p.boon_memory[id]
        assert(memory.ward==3 and memory.expires_at==200 and memory.face==4)
        local expected={state=s.rng.state};local next_face=RNG.next_int(expected,8)
        assert(next_face~=4)
        action(s);assert(memory.expires_at==200 and memory.face==next_face)
        assert(B.consume_ward(s,p,9)==3 and memory.ward==0)
    end)
    test('face five arcs from the actual first victim to one other nearby foe',function()
        local s=fixture(5);local first=foe(s);local other=foe(s,4,2)
        local events=action(s);rolled(events,5)
        assert(first.hp==97 and other.hp==98)
        assert(count(events,'EffectApplied')==1)
        for _,e in ipairs(events)do
            if e.kind=='DamageTaken' and not e.direct_attack then
                assert(e.target_id==other.id and e.damage_type=='lightning')
            end
        end
    end)
    test('face six produces one victim-centered burst after the complete primary sweep batch',function()
        local s,p=fixture(6);p.position.x,p.position.y=7,6;V.recompute(s)
        local targets={}
        for _,cell in ipairs(G.preview(s,'sweeping_slash',1,0).cells)do
            targets[#targets+1]=foe(s,cell.x,cell.y)
        end
        local events=action(s,{kind='sweeping_slash',dx=1,dy=0});rolled(events,6)
        assert(count(events,'EffectApplied')==1 and count(events,'BoonActivated')==1)
        local primary,activation
        for n,e in ipairs(events)do
            if e.kind=='DamageBatchResolved' and not primary then primary=n;assert(#e.outcomes==3)end
            if e.kind=='BoonActivated' and e.boon_id==id then activation=n end
        end
        assert(primary<activation and p.hp==p.max_hp)
        assert(targets[1].hp<100 and targets[2].hp<100 and targets[3].hp<100)
    end)
    test('a landed skull kills an ordinary full-health foe through normal defense and rewards once',function()
        local s=fixture(7);local e=foe(s);local events=action(s);rolled(events,7)
        assert(not G.entity(s,e.id) and count(events,'EntityKilled')==1)
        assert(count(events,'FateSkullResisted')==0)
        local drops=0
        for _,d in ipairs(s.drops)do if d.kind=='currency' and d.killed_entity_id==e.id then drops=drops+1 end end
        assert(drops==1)
    end)
    test('skull excludes every biome miniboss, finale, pursuer and special champion',function()
        local ids={'core:enemy/hollow_regent','core:enemy/extraction_pursuer','core:enemy/rotating_sentinel'}
        for _,biome in ipairs(require('src.biomes').order)do
            ids[#ids+1]=require('src.biomes').get(biome).elite_id
        end
        for _,definition in ipairs(ids)do
            local s=fixture(7);local e=foe(s,3,2,definition)
            assert(not B.skull_target(e),definition)
            local events=action(s);rolled(events,7)
            assert(G.entity(s,e.id) and e.hp>=96,definition)
            assert(count(events,'FateSkullResisted')==1,definition)
        end
    end)
    test('skull cannot bypass reward-exempt resurrection or a living wizard forcefield',function()
        assert(not B.skull_target({controller='ai',faction='player',
            definition_id='core:enemy/mossbound_guard'}))
        local s=fixture(7);local e=foe(s);e.reward_exempt=true
        local events=action(s);assert(e.hp==97 and count(events,'FateSkullResisted')==1)
        s=fixture(7);e=foe(s,3,2,'core:enemy/bodyguard_brute')
        local mage=G.spawn_enemy(s,'core:enemy/ward_weaver',4,2,100000)
        mage.ai_memory.ward_ready=true
        for _,actor in ipairs({e,mage})do
            actor.squad_id='dice-ward';actor.squad_kind='warded_bodyguards'
            actor.squad_leader_id=mage.id
            actor.squad_home={floor_id=s.active_floor_id,x=4,y=2}
        end
        assert(G.restore(G.snapshot(s)))
        events=action(s)
        assert(G.entity(s,e.id) and e.hp==100 and count(events,'FateSkullResisted')==1)
        assert(count(events,'EntityKilled')==0)
    end)
    test('skull executes the selected ordinary Oath twin without converting execution to shared chip damage',function()
        local s=fixture(7)
        local a=foe(s,3,2,Oaths.a);local b=foe(s,4,2,Oaths.b)
        a.oath_partner_id=b.id;b.oath_partner_id=a.id
        assert(Oaths.partner(s,a)==b)
        local events=action(s)
        assert(not G.entity(s,a.id) and b.hp==100 and b.oath_broken)
        assert(count(events,'EntityKilled')==1)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('a skull sweep evaluates the living forcefield for every target before committing wizard death',function()
        local s,p=fixture(7);p.position.x,p.position.y=7,6;V.recompute(s)
        local mage=G.spawn_enemy(s,'core:enemy/ward_weaver',8,5,100000)
        local brute=foe(s,8,6,'core:enemy/bodyguard_brute');mage.ai_memory.ward_ready=true
        for _,actor in ipairs({mage,brute})do
            actor.squad_id='skull-batch';actor.squad_kind='warded_bodyguards'
            actor.squad_leader_id=mage.id
            actor.squad_home={floor_id=s.active_floor_id,x=8,y=5}
        end
        local events=action(s,{kind='sweeping_slash',dx=1,dy=0})
        assert(not G.entity(s,mage.id) and brute.hp==100 and count(events,'EntityKilled')==1)
        assert(count(events,'FateSkullResisted')==1)
    end)
    test('a cross is a genuine miss with no hidden hit, Burning, Storm charge or newly queued Echo',function()
        local s,p=fixture(8,'core:actor/stormcaller');local e=foe(s)
        for _,slug in ipairs({'temporal_echo','emberbrand','storm_conductor'})do
            G.set_boon_stacks(s,p.id,'core:boon/'..slug,'rare',1)
        end
        local events=action(s);rolled(events,8)
        assert(e.hp==100 and not e.statuses and (p.storm_charge or 0)==0)
        assert(count(events,'DamageBatchResolved')==0 and count(events,'HitConfirmed')==0)
        assert(count(events,'BoonActivated')==0 and not next(s.delayed_effects))
        for _,event in ipairs(events)do
            if event.kind=='AttackPerformed' then
                assert(event.fate_miss and #event.target_cells==0 and #event.aim_cells>0 and event.raw_damage==0)
            end
        end
    end)
    test('a missed attack cannot break a barricade and trigger hidden terrain-boon damage',function()
        local s,p=fixture(8)
        s.generated_map=P.decode(P.encode(Content.map_for(s)))
        local map=s.generated_map;map.rows[2]=map.rows[2]:sub(1,2)..'B'..map.rows[2]:sub(4)
        G.set_boon_stacks(s,p.id,'core:boon/fracture_dividend','rare',1)
        local events=action(s)
        assert(map.rows[2]:sub(3,3)=='B' and count(events,'TerrainBroken')==0)
        assert(count(events,'EffectApplied')==0)
    end)
    test('a rolled cross also misses the Hookrunner pull instead of displacing an unhit foe',function()
        local s=fixture(8,'core:actor/hookrunner');local e=foe(s,5,2)
        local events=action(s,{kind='sweeping_slash',dx=1,dy=0});rolled(events,8)
        assert(e.hp==100 and e.position.x==5 and e.position.y==2)
        assert(count(events,'Displaced')==0 and count(events,'DamageTaken')==0)
    end)
    test('a cross does not cancel an earlier committed Echo and does not create another one',function()
        local s=G.new(seeds[8]);local p=G.player(s);local e=foe(s)
        G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','rare',1)
        action(s);assert(next(s.delayed_effects))
        G.set_boon_stacks(s,p.id,id,'rare',1)
        local hp=e.hp;local events=action(s);rolled(events,8)
        assert(count(events,'DelayedEffectScheduled')==0 and count(events,'DelayedEffectResolved')==1)
        assert(e.hp<hp and not next(s.delayed_effects))
    end)
    test('Pilgrim still pays on a rolled miss and heavy dice combine before one critical',function()
        for _,face in ipairs({8,2})do
            local s,p=fixture(face);p.hp=10;local e=foe(s)
            G.set_boon_stacks(s,p.id,'core:boon/pilgrims_vow','rare',1)
            if face==2 then p.crit_chance_ppm=1000000 end
            local events=action(s,{kind='basic_attack',dx=1,dy=0,vow=true})
            assert(p.hp==8 and count(events,'VowPaid')==1);rolled(events,face)
            if face==8 then assert(e.hp==100)
            else assert(e.hp==100-(math.floor((p.damage+6+3)*p.crit_damage_percent/100+.5)-e.defense))end
        end
    end)
    test('uncapped mixed rarity dice copies increase power without extra rolls or extra primary events',function()
        local s,p=fixture(4);foe(s)
        G.set_boon_stacks(s,p.id,id,'common',10)
        G.set_boon_stacks(s,p.id,id,'legendary',20)
        local expected={state=s.rng.state};RNG.next_int(expected,8)
        local events=action(s);rolled(events,4)
        assert(s.rng.state==expected.state and p.boon_memory[id].ward==93)
        assert(count(events,'AttackPerformed')==1 and count(events,'BoonActivated')==1)
    end)
    test('all fifteen basic attacks roll once while utility placement and generated sentry shots never roll',function()
        for _,class in ipairs(Classes.order)do
            local s,p=fixture(8,class);foe(s)
            local events=action(s);rolled(events,8)
            assert(count(events,'HitConfirmed')==0 and p.boon_memory[id].face==8)
        end
        local s,p=fixture(4,'core:actor/tinkerer');foe(s,5,2)
        local rng=s.rng.state
        local events=action(s,{kind='sweeping_slash',dx=1,dy=0})
        assert(count(events,'FateRolled')==0)
        for _=1,6 do
            events=action(s,{kind='wait'});assert(count(events,'FateRolled')==0)
        end
        assert(not p.boon_memory and s.rng.state==rng)
    end)
    test('saved last roll and active ward continue exactly with pending Echo and later real dice outcomes',function()
        local s,p=fixture(4);foe(s)
        G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','rare',1)
        action(s);assert(p.boon_memory[id].ward==3 and next(s.delayed_effects))
        local restored=G.restore(P.decode(P.encode(G.snapshot(s))))
        for _,intent in ipairs({{kind='wait'},{kind='basic_attack',dx=1,dy=0},
            {kind='basic_attack',dx=1,dy=0},{kind='wait'}})do
            equal(action(s,intent),action(restored,intent));equal(G.snapshot(s),G.snapshot(restored))
        end
    end)
    test('a commanded Spirit roll is saved on the real owner rather than its temporary attack proxy',function()
        local s,p=fixture(4,'core:actor/spiritbinder')
        require('src.idea_companions').place(s,p,{x=3,y=2},function()end)
        foe(s,4,2)
        local events=action(s,{kind='sweeping_slash',dx=1,dy=0});rolled(events,4)
        assert(p.boon_memory[id].face==4 and p.boon_memory[id].ward==3)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('saved dice faces, roll IDs and malformed face predicates reject before activation',function()
        local s=fixture(4);foe(s);action(s)
        local before=G.snapshot(s)
        for _,face in ipairs({0,9,1.5})do
            local bad=P.decode(P.encode(before));bad.entities[1].boon_memory[id].face=face
            assert(not pcall(G.restore,bad))
        end
        local bad=P.decode(P.encode(before));bad.entities[1].boon_memory[id].roll_action_id=bad.next_action_id
        assert(not pcall(G.restore,bad))
        local def=P.decode(P.encode(C.get(id)));def.predicate={op='roll_face',value=9}
        assert(not pcall(C.validate,{[id]=def},{id}))
        equal(before,G.snapshot(s))
    end)
end
