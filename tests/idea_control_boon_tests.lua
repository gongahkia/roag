return function(test,equal)
    local G=require('src.game')
    local B=require('src.boons')
    local C=require('src.boon_content')
    local R=require('src.rewards')
    local P=require('src.persistence')
    local V=require('src.visibility')
    local Q=require('src.scheduler')
    local function grant(s,slug,rarity,n)
        G.set_boon_stacks(s,s.player_id,'core:boon/'..slug,rarity or 'rare',n or 1)
    end
    local function fixture(slug,class)
        local s=G.new(37,nil,{class_id=class});local p=G.player(s)
        if slug then grant(s,slug)end
        return s,p
    end
    local function foe(s,x,y,definition,due)
        local e=G.spawn_enemy(s,definition or 'core:enemy/mossbound_guard',x or 3,y or 2,due or 100000)
        e.hp=100;e.max_hp=100;return e
    end
    local function action(s,kind,dx,dy)
        local ok,why,events=G.submit(s,{kind=kind or 'basic_attack',dx=dx or 1,dy=dy or 0})
        assert(ok,why);return events
    end
    local function count(events,kind,slug)
        local n=0
        for _,e in ipairs(events)do
            if e.kind==kind and (not slug or e.boon_id=='core:boon/'..slug)then n=n+1 end
        end
        return n
    end
    local function ward(s,power,expires,slug)
        local p=G.player(s);slug=slug or 'beggars_cap';grant(s,slug)
        p.boon_memory=p.boon_memory or {}
        p.boon_memory['core:boon/'..slug]={ward=power,expires_at=expires or 500}
    end
    local function memory(s,slug)return G.player(s).boon_memory['core:boon/'..slug]end
    local function money(s,x,y,amount)
        return R.spawn_currency(s,amount or 1,s.active_floor_id,x,y,function()end,s.player_id)
    end
    test('six control/resource boons declare real finite effects and native icons',function()
        local seen={}
        for _,slug in ipairs({'grasping_hand','anchor_maul','hourglass_clasp',
            'ward_ledger','cinder_stopper','scavengers_hook'})do
            local d=assert(C.get('core:boon/'..slug))
            assert(not seen[d.art.index]);seen[d.art.index]=true
            assert(require('src.kenney_symbols').boons[d.id][1]==d.art.index)
        end
        assert(C.validate())
        for _,kind in ipairs({'displace_target','stun_target'})do
            local id='core:boon/'..(kind=='displace_target' and 'anchor_maul' or 'hourglass_clasp')
            local d=P.decode(P.encode(C.get(id)))
            if kind=='displace_target'then d.effects[1].mode='teleport'
            else d.effects[1].cooldown=0 end
            assert(not pcall(C.validate,{[id]=d},{id}))
        end
    end)
    test('Grasping Hand special hits add ordinary defended damage then pull a surviving regular foe one cell',function()
        local s=fixture('grasping_hand','core:actor/ranger');local e=foe(s,5,2)
        local events=action(s,'sweeping_slash')
        assert(e.position.x==4 and count(events,'Displaced')==1)
        local primary,bonus=0,0
        for _,event in ipairs(events)do if event.kind=='DamageTaken' and event.target_id==e.id then
            if event.direct_attack then primary=event.actual_loss else bonus=event.actual_loss end
        end end
        assert(bonus==2 and e.hp==100-primary-bonus)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('Grasping Hand cannot pull through an occupied cell and basic hits do not trigger it',function()
        local s=fixture('grasping_hand','core:actor/ranger');local e=foe(s,5,2)
        G.spawn_chest(s,'core:chest/standard',4,2)
        local events=action(s,'sweeping_slash')
        assert(e.position.x==5 and count(events,'ShoveBlocked')==1)
        s=fixture('grasping_hand');e=foe(s)
        events=action(s);assert(e.hp==97 and count(events,'BoonActivated','grasping_hand')==0)
    end)
    test('Grasping Hand uses the real owner row/column even when a Spirit also owns strike-origin pattern boons',function()
        for _,pattern in ipairs({false,true})do
            local s,p=fixture('grasping_hand','core:actor/spiritbinder')
            p.position.x,p.position.y=7,6
            require('src.idea_companions').place(s,p,{x=8,y=8},function()end)
            local e=foe(s,9,8);V.recompute(s)
            if pattern then grant(s,'linebreaker_pawn')end
            local preview=G.preview(s,'sweeping_slash',1,1)
            assert(preview.valid and preview.companion_mode=='strike')
            local events=action(s,'sweeping_slash',1,1)
            assert(count(events,'BoonActivated','grasping_hand')==0 and e.hp==98)
        end
    end)
    test('Anchor Maul pushes a cardinal basic victim without adding collision damage in open space',function()
        local s=fixture('anchor_maul');local e=foe(s)
        local events=action(s)
        assert(e.position.x==4 and e.hp==97 and count(events,'Displaced')==1)
        assert(count(events,'DamageTaken')==1 and count(events,'ShoveBlocked')==0)
    end)
    test('a wall-blocked Anchor Maul collision deals defended damage without crossing masonry',function()
        local s,p=fixture('anchor_maul');p.position.x,p.position.y=2,3;V.recompute(s)
        local e=foe(s,3,3)
        local events=action(s)
        assert(e.position.x==3 and e.hp==95 and count(events,'ShoveBlocked')==1)
        assert(count(events,'DamageTaken')==2 and count(events,'Displaced')==0)
    end)
    test('successful boon displacement cancels the actual locked windup and preserves its committed cells',function()
        local s=fixture('anchor_maul');local e=foe(s,3,2,nil,0);e.speed=20
        action(s,'wait');assert(e.pending_attack)
        local old=P.decode(P.encode(e.pending_attack))
        local events=action(s)
        assert(count(events,'WindupCancelled')==1 and not e.pending_attack)
        for _,event in ipairs(events)do if event.kind=='WindupCancelled'then
            assert(event.windup_id==old.schedule_id);equal(event.target_cells,old.target_cells)
        end end
        for _,item in ipairs(s.schedule)do assert(item.id~=old.schedule_id)end
    end)
    test('all miniboss/finale/special and reward-exempt targets resist the three regular-only control boons',function()
        local ids={'core:enemy/hollow_regent','core:enemy/rotating_sentinel','core:enemy/extraction_pursuer'}
        for _,biome in ipairs(require('src.biomes').order)do
            ids[#ids+1]=require('src.biomes').get(biome).elite_id
        end
        for _,slug in ipairs({'grasping_hand','anchor_maul','hourglass_clasp'})do
            for _,definition in ipairs(ids)do
                local s=fixture(slug);local e=foe(s,3,2,definition)
                local events=action(s,slug=='grasping_hand' and 'sweeping_slash' or 'basic_attack')
                assert(count(events,'BoonActivated',slug)==0 and e.position.x==3,slug..definition)
            end
            local s=fixture(slug);local e=foe(s);e.reward_exempt=true
            assert(count(action(s,'sweeping_slash'),'BoonActivated',slug)==0)
        end
    end)
    test('Hourglass stuns only the first surviving primary victim per attack and never accelerates later recovery',function()
        local s,p=fixture('hourglass_clasp');p.position.x,p.position.y=7,6;V.recompute(s)
        local targets={foe(s,8,5),foe(s,8,6),foe(s,8,7)}
        local events=action(s,'sweeping_slash')
        assert(count(events,'Stunned')==1 and memory(s,'hourglass_clasp').expires_at==400)
        local stunned=0
        for _,e in ipairs(targets)do
            if e.stunned_until>0 then
                stunned=stunned+1;assert(e.stunned_until==30)
                for _,item in ipairs(s.schedule)do if item.actor_id==e.id then assert(item.due==100000)end end
            end
        end
        assert(stunned==1 and s.clock==150)
    end)
    test('Hourglass cooldown has an exact 400-time boundary and uncapped stacks cannot exceed its 100-time stun cap',function()
        local s=fixture('hourglass_clasp');foe(s)
        grant(s,'hourglass_clasp','legendary',20)
        local events=action(s);assert(count(events,'Stunned')==1)
        for _,event in ipairs(events)do if event.kind=='Stunned'then assert(event.until_time==100)end end
        for _=1,3 do assert(count(action(s),'Stunned')==0)end
        assert(s.clock==400 and count(action(s),'Stunned')==1)
    end)
    test('Hourglass cancels a real windup without requiring an extra player reaction turn',function()
        local s=fixture('hourglass_clasp');local e=foe(s,3,2,nil,0);e.speed=20
        action(s,'wait');local old=assert(e.pending_attack).schedule_id
        local events=action(s)
        assert(count(events,'Stunned')==1 and count(events,'WindupCancelled')==1)
        for _,event in ipairs(events)do assert(not(event.kind=='DamageBatchResolved' and event.windup_id==old))end
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('Ward Ledger spends active wards in catalogue order, caps healing and leaves no reusable capacity',function()
        local s,p=fixture('ward_ledger');p.hp=10
        ward(s,2,500,'beggars_cap');ward(s,6,500,'overflow_chalice')
        assert(B.ward_total(s,p)==8)
        local events=action(s,'wait')
        assert(p.hp==13 and B.ward_total(s,p)==0 and count(events,'Healed')==1)
        assert(memory(s,'beggars_cap').ward==0 and memory(s,'overflow_chalice').ward==0)
        action(s,'wait');assert(p.hp==13)
    end)
    test('Ward Ledger cannot spend expired wards or full-health wards, and cannot overheal',function()
        local s,p=fixture('ward_ledger');ward(s,6)
        assert(count(action(s,'wait'),'BoonActivated','ward_ledger')==0 and memory(s,'beggars_cap').ward==6)
        p.hp=23
        local events=action(s,'wait');assert(p.hp==24 and B.ward_total(s,p)==0)
        assert(count(events,'Healed')==1)
        s,p=fixture('ward_ledger');p.hp=10;ward(s,6,0)
        action(s,'move',0,1);assert(s.clock==100)
        events=action(s,'wait');assert(p.hp==10 and count(events,'BoonActivated','ward_ledger')==0)
    end)
    test('Cinder Stopper consumes real remaining Burning and removes its exact scheduled tick',function()
        local s=fixture('cinder_stopper');local e=foe(s)
        grant(s,'emberbrand','common');action(s)
        local status=assert(e.statuses.burning);local old=status.schedule_id
        assert(status.power==2 and status.remaining==2)
        grant(s,'emberbrand','common',0)
        local hp=e.hp;local events=action(s)
        assert(not e.statuses.burning and e.hp==hp-3-3)
        assert(count(events,'StatusCleansed')==1 and count(events,'StatusTick')==0)
        for _,item in ipairs(s.schedule)do assert(item.id~=old)end
        local after=e.hp;action(s,'wait');action(s,'wait');assert(e.hp==after)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('Cinder Stopper ignores newly ignited and blocked hits, and later Emberbrand may ignite normally again',function()
        local s=fixture('cinder_stopper');local e=foe(s);grant(s,'emberbrand','common')
        local events=action(s)
        assert(count(events,'BoonActivated','cinder_stopper')==0 and e.statuses.burning)
        events=action(s)
        assert(count(events,'StatusCleansed')==1 and e.statuses.burning)
        e.defense=100;local old=e.statuses.burning.schedule_id
        events=action(s)
        assert(count(events,'BoonActivated','cinder_stopper')==0 and e.statuses.burning)
        assert(e.statuses.burning.schedule_id~=old or e.statuses.burning.next_due>s.clock)
    end)
    test('Scavenger Hook gathers a rarity-limited number of tiles in stable north/east/south/west order',function()
        local s,p=fixture('scavengers_hook');p.position.x,p.position.y=7,6;V.recompute(s)
        grant(s,'scavengers_hook','rare',0);grant(s,'scavengers_hook','common')
        local drops={money(s,7,5,1),money(s,8,6,2),money(s,7,7,3),money(s,6,6,4)}
        local rng=s.rng.state;local events=action(s,'wait')
        assert(s.currency==1 and drops[1].collected and not drops[2].collected)
        assert(count(events,'CurrencyCollected')==1 and s.clock==100 and s.rng.state==rng)
        grant(s,'scavengers_hook','common',0);grant(s,'scavengers_hook','rare')
        action(s,'wait');assert(s.currency==10)
        for _,drop in ipairs(drops)do assert(drop.collected)end
        action(s,'wait');assert(s.currency==10)
    end)
    test('Scavenger Hook cannot gather through walls or closed doors, and opening a door exposes the pickup safely',function()
        local s=fixture('scavengers_hook')
        local wall_drop=money(s,2,1);local east=money(s,3,2);local south=money(s,2,3)
        local door=G.spawn_door(s,3,2)
        action(s,'wait');assert(south.collected and not east.collected and not wall_drop.collected)
        door.open=true;door.blocks_movement=false;door.blocks_vision=false;door.blocks_projectiles=false
        action(s,'wait');assert(east.collected and not wall_drop.collected)
    end)
    test('full-health food and carried drops do not spend the Hook tile budget',function()
        local s,p=fixture('scavengers_hook');p.position.x,p.position.y=7,6;V.recompute(s)
        grant(s,'scavengers_hook','rare',0);grant(s,'scavengers_hook','common')
        local food=R.spawn_supply(s,'food',s.active_floor_id,7,5,function()end,p.id)
        local held=money(s,7,5);held.carried_by=999 -- read-only skip prerequisite, not a saved fixture
        local east=money(s,8,6)
        action(s,'wait');assert(not food.collected and not held.collected and east.collected)
    end)
    test('gathered supplies and gold retain normal healing, cleanse/speed and finite pickup-boon triggers',function()
        local s,p=fixture('scavengers_hook');p.position.x,p.position.y=7,6;p.hp=6;V.recompute(s)
        grant(s,'road_flask');grant(s,'field_dressing');grant(s,'prospectors_heart','common')
        R.spawn_supply(s,'food',s.active_floor_id,7,5,function()end,p.id)
        money(s,8,6,2)
        R.spawn_supply(s,'health',s.active_floor_id,7,7,function()end,p.id)
        local events=action(s,'wait')
        assert(count(events,'SupplyCollected')==2 and count(events,'CurrencyCollected')==1)
        assert(p.hp==24 and s.currency==2 and memory(s,'road_flask').speed==20 and s.clock==100)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('saved cooldowns, wards and pending Burning continue identically through real later control and Wait actions',function()
        local s,p=fixture('hourglass_clasp');foe(s)
        for _,slug in ipairs({'anchor_maul','ward_ledger','scavengers_hook','cinder_stopper','emberbrand'})do grant(s,slug)end
        ward(s,4);p.hp=10;action(s)
        local restored=G.restore(P.decode(P.encode(G.snapshot(s))))
        for _,kind in ipairs({'wait','move','basic_attack','wait','basic_attack'})do
            equal(action(s,kind,0,kind=='move' and 1 or -1),action(restored,kind,0,kind=='move' and 1 or -1))
            equal(G.snapshot(s),G.snapshot(restored))
        end
    end)
    test('actual stage handoff preserves only the remaining Hourglass cooldown and keeps expired cooldowns ready',function()
        local F=require('tests.idea_boon_handoff_fixture')
        for _,remaining in ipairs({250,-10})do
            local s,p,exit=F.ready();grant(s,'hourglass_clasp')
            p.boon_memory={['core:boon/hourglass_clasp']={expires_at=s.clock+remaining}}
            F.route(s,exit)
            assert(s.clock==0 and memory(s,'hourglass_clasp').expires_at==math.max(0,remaining))
            equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
        end
    end)
    test('new control previews are read-only and actual effects do not invalidate ordinary run eligibility',function()
        local s=G.new_stage(37,{meta=require('src.achievements').new_run(true,'control-boon-meta-fixture')})
        local p=G.player(s);local e,dx,dy
        local Content=require('src.content');local map=Content.map_for(s)
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
            local x,y=p.position.x+d[1],p.position.y+d[2]
            local free=Content.walkable(map,x,y)
            for _,actor in ipairs(s.entities)do
                if actor.blocks_movement and actor.position.floor_id==s.active_floor_id
                    and actor.position.x==x and actor.position.y==y then free=false end
            end
            if free then e=foe(s,x,y);dx,dy=d[1],d[2];break end
        end
        assert(e)
        B.set_stacks(p,'core:boon/anchor_maul','rare',1)
        assert(s.meta.eligible)
        local before=G.snapshot(s)
        for _=1,30 do G.preview(s,'basic_attack',dx,dy);G.preview(s,'sweeping_slash',dx,dy)end
        equal(before,G.snapshot(s))
        assert(count(action(s,'basic_attack',dx,dy),'BoonActivated','anchor_maul')==1)
        assert(s.meta.eligible)
    end)
end
