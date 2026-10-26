-- Prepared actors/supplies; every proc below is reached through a paid action.
return function(test,equal)
    local G=require('src.game')
    local B=require('src.boons')
    local C=require('src.boon_content')
    local Content=require('src.content')
    local P=require('src.persistence')
    local R=require('src.rewards')
    local V=require('src.visibility')
    local Q=require('src.scheduler')
    local order={'threefold_remedy','twin_seals','fourfold_ledger','stone_backstop',
        'borrowed_guard','long_wick','passing_ember','purging_seal'}
    local function grant(s,slug,tier,n)
        B.set_stacks(G.player(s),'core:boon/'..slug,tier or 'rare',n or 1)
    end
    local function fixture(slug,class)
        local s=G.new(37,'core:map/scrolling_room',{class_id=class})
        local p=G.player(s);p.position.x,p.position.y=15,16
        if slug then grant(s,slug)end
        V.recompute(s);return s,p
    end
    local function foe(s,dx,dy,hp,definition)
        local p=G.player(s).position
        local e=G.spawn_enemy(s,definition or 'core:enemy/mossbound_guard',p.x+dx,p.y+dy,100000)
        e.hp=hp or 100;e.max_hp=e.hp;return e
    end
    local function action(s,kind,dx,dy)
        local ok,why,events=G.submit(s,{kind=kind or 'basic_attack',dx=dx or 1,dy=dy or 0})
        assert(ok,why);return events
    end
    local function count(events,kind,slug)
        local n=0;for _,e in ipairs(events)do
            if e.kind==kind and (not slug or e.boon_id=='core:boon/'..slug)then n=n+1 end
        end;return n
    end
    local function tile(s,x,y,symbol)
        s.generated_map=s.generated_map or P.decode(P.encode(Content.map_for(s)))
        local row=s.generated_map.rows[y]
        s.generated_map.rows[y]=row:sub(1,x-1)..symbol..row:sub(x+1)
    end
    local function memory(p,slug)return p.boon_memory and p.boon_memory['core:boon/'..slug]end
    local function ignite(s,e)
        grant(s,'emberbrand','common');action(s)
        grant(s,'emberbrand','common',0);assert(e.statuses.burning)
    end
    local function burning_player(slug)
        local s=G.new(37);local p=G.player(s)
        p.position.x,p.position.y=7,6;V.recompute(s)
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,0)
        G.set_boon_stacks(s,e.id,'core:boon/emberbrand','common',1)
        action(s,'wait');action(s,'wait');assert(p.statuses.burning)
        Q.remove_for_actor(s,e.id)
        for n,a in ipairs(s.entities)do if a.id==e.id then table.remove(s.entities,n);break end end
        grant(s,slug);return s,p
    end
    test('eight cadence/status boons validate distinct native icons and bounded extensions',function()
        local icons={}
        for _,slug in ipairs(order)do
            local d=assert(C.get('core:boon/'..slug))
            assert(not icons[d.art.index]);icons[d.art.index]=true
            assert(require('src.kenney_symbols').boons[d.id][1]==d.art.index)
        end
        assert(C.validate())
        local d=P.decode(P.encode(C.get('core:boon/long_wick')));d.effects[1].cap=1000
        assert(not pcall(C.validate,{[d.id]=d},{d.id}))
        d=P.decode(P.encode(C.get('core:boon/fourfold_ledger')))
        d.predicate.all[3].field='arbitrary_field'
        assert(not pcall(C.validate,{[d.id]=d},{d.id}))
    end)
    test('Threefold Remedy requires three different primary victims in the same committed attack',function()
        for _,n in ipairs({2,3})do
            local s,p=fixture('threefold_remedy','core:actor/berserker');p.hp=10
            for _,d in ipairs(n==3 and {{1,-1},{1,0},{1,1}} or {{1,-1},{1,0}})do foe(s,d[1],d[2])end
            local events=action(s,'sweeping_slash')
            assert(p.hp==10+(n==3 and 3 or 0))
            assert(count(events,'BoonActivated','threefold_remedy')==(n==3 and 1 or 0))
        end
    end)
    test('Threefold groups reset per causal chain and secondary Echo damage never fills them',function()
        local s,p=fixture('threefold_remedy','core:actor/berserker');p.hp=10
        foe(s,1,0);foe(s,1,1);grant(s,'temporal_echo')
        for _=1,3 do
            assert(count(action(s,'sweeping_slash'),'BoonActivated','threefold_remedy')==0)
        end
        assert(p.hp==10)
    end)
    test('six distinct Backswing victims form two remedies after the primary HP batch',function()
        local s,p=fixture('threefold_remedy','core:actor/berserker');p.hp=p.max_hp-1
        grant(s,'backswing')
        for _,d in ipairs({{1,-1},{1,0},{1,1},{-1,-1},{-1,0},{-1,1}})do foe(s,d[1],d[2])end
        local events=action(s,'sweeping_slash');assert(count(events,'BoonActivated','threefold_remedy')==2)
        assert(p.hp==p.max_hp)
        local batch,heal
        for n,e in ipairs(events)do
            if e.kind=='DamageBatchResolved' and not batch then batch=n end
            if e.kind=='Healed' and not heal then heal=n end
        end
        assert(batch and heal and batch<heal)
    end)
    test('Twin Seals needs two rewarded kills in one chain and grants exactly one finite ward',function()
        local s,p=fixture('twin_seals','core:actor/berserker')
        foe(s,1,0,1);foe(s,1,1,1)
        local events=action(s,'sweeping_slash')
        assert(count(events,'BoonActivated','twin_seals')==1)
        assert(memory(p,'twin_seals').ward==4 and memory(p,'twin_seals').expires_at==300)
        assert(B.consume_ward(s,p,3)==3 and B.consume_ward(s,p,3)==0)
    end)
    test('separate kills and revived reward-exempt victims cannot assemble Twin Seals',function()
        local s,p=fixture('twin_seals');foe(s,1,0,1);action(s)
        foe(s,1,0,1);assert(count(action(s),'BoonActivated','twin_seals')==0)
        s,p=fixture('twin_seals','core:actor/berserker')
        foe(s,1,0,1);local e=foe(s,1,1,1);e.reward_exempt=true
        assert(count(action(s,'sweeping_slash'),'BoonActivated','twin_seals')==0)
    end)
    test('Fourfold Ledger counts four different real pickup procs without counting itself',function()
        local s,p=fixture('fourfold_ledger');p.hp=1;p.max_hp=100
        for _,slug in ipairs({'beggars_cap','road_flask','ration_reserve','field_dressing'})do grant(s,slug)end
        R.spawn_currency(s,1,s.active_floor_id,16,16,function()end,p.id)
        R.spawn_supply(s,'food',s.active_floor_id,16,16,function()end,p.id)
        R.spawn_supply(s,'health',s.active_floor_id,16,16,function()end,p.id)
        local events=action(s,'move')
        assert(count(events,'BoonActivated','fourfold_ledger')==1)
        assert(p.hp==22 and count(events,'BoonActivated')==5)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('repeated activations of one boon are one ledger entry and cannot recurse',function()
        local s,p=fixture('fourfold_ledger','core:actor/berserker');p.hp=10
        grant(s,'rooks_wake');grant(s,'emberbrand');grant(s,'linebreaker_pawn')
        foe(s,1,-1);foe(s,1,0);foe(s,1,1)
        local events=action(s,'sweeping_slash')
        assert(count(events,'BoonActivated')>4 and count(events,'BoonActivated','fourfold_ledger')==0)
        assert(p.hp==10)
    end)
    test('Stone Backstop requires actual owner cardinal alignment and a blocking tile behind the victim',function()
        for _,mode in ipairs({'open','wall','door','diagonal'})do
            local s,p=fixture('stone_backstop');local e=foe(s,1,mode=='diagonal' and 1 or 0)
            if mode=='wall'then tile(s,17,16,'#')
            elseif mode=='door'then G.spawn_door(s,17,16)
            elseif mode=='diagonal'then tile(s,17,18,'#')end
            local events=action(s,'basic_attack',1,mode=='diagonal' and 1 or 0)
            assert(count(events,'BoonActivated','stone_backstop')==((mode=='wall' or mode=='door')and 1 or 0))
            assert(e.hp==((mode=='wall' or mode=='door')and 94 or 97))
        end
    end)
    test('a piercing Needle captures Backstop before the same attack destroys its barricade',function()
        local s,p=fixture('stone_backstop','core:actor/ranger');grant(s,'needle_flight')
        local e=foe(s,1,0);tile(s,17,16,'B')
        local events=action(s)
        assert(count(events,'TerrainBroken')==1 and count(events,'BoonActivated','stone_backstop')==1)
        assert(e.hp==92 and s.generated_map.rows[16]:sub(17,17)=='.')
    end)
    test('unowned Backstop leaves original damage outcome metadata absent',function()
        local s=fixture();foe(s,1,0);tile(s,17,16,'#')
        for _,e in ipairs(action(s))do assert(e.wall_backed==nil)end
    end)
    test('Borrowed Guard triggers once on a fully blocked primary attack and has a real cooldown',function()
        local s,p=fixture('borrowed_guard');local e=foe(s,1,0);e.defense=100
        local first=action(s);assert(count(first,'BoonActivated','borrowed_guard')==1)
        assert(memory(p,'borrowed_guard').ward==3 and memory(p,'borrowed_guard').expires_at==300)
        B.consume_ward(s,p,1)
        assert(count(action(s),'BoonActivated','borrowed_guard')==0)
        assert(count(action(s),'BoonActivated','borrowed_guard')==0)
        assert(count(action(s),'BoonActivated','borrowed_guard')==1)
    end)
    test('Borrowed Guard excludes bosses, exempt foes and ordinary positive hits',function()
        for _,mode in ipairs({'boss','exempt','positive'})do
            local s,p=fixture('borrowed_guard')
            local e=foe(s,1,0,nil,mode=='boss' and 'core:enemy/thorn_warden' or nil)
            e.defense=mode=='positive' and 0 or 100
            if mode=='exempt'then e.reward_exempt=true end
            assert(count(action(s),'BoonActivated','borrowed_guard')==0)
        end
    end)
    test('a blocked multi-target sweep creates one Borrowed Guard rather than one per victim',function()
        local s,p=fixture('borrowed_guard','core:actor/berserker')
        for _,d in ipairs({{1,-1},{1,0},{1,1}})do foe(s,d[1],d[2]).defense=100 end
        assert(count(action(s,'sweeping_slash'),'BoonActivated','borrowed_guard')==1)
    end)
    test('Long Wick extends real Burning without changing its power or first queued tick',function()
        local s=fixture('long_wick');local e=foe(s,1,0);ignite(s,e)
        local status=e.statuses.burning
        assert(status.remaining==5 and status.power==2 and status.interval==100 and status.next_due==200)
        local scheduled;for _,v in ipairs(s.schedule)do if v.id==status.schedule_id then scheduled=v end end
        assert(scheduled and scheduled.due==status.next_due)
        equal(G.snapshot(s),G.snapshot(G.restore(G.snapshot(s))))
    end)
    test('uncapped Wick stacks cap remaining ticks at eight and cannot amplify tick power',function()
        local s=fixture();grant(s,'long_wick','legendary',100)
        local e=foe(s,1,0);ignite(s,e)
        assert(e.statuses.burning.remaining==7 and e.statuses.burning.power==2)
        local before=e.hp;for _=1,7 do action(s,'wait')end
        assert(not e.statuses.burning and e.hp==before-7)
    end)
    test('Passing Ember ignites the nearest clear foe after a real Burning victim dies',function()
        local s=fixture('passing_ember');local killed=foe(s,1,0);ignite(s,killed)
        killed.hp=1;local near=foe(s,1,1);local far=foe(s,3,0)
        local events=action(s)
        assert(count(events,'BoonActivated','passing_ember')==1)
        assert(near.statuses.burning and not (far.statuses and far.statuses.burning))
        assert(near.statuses.burning.power==3 and near.statuses.burning.remaining==1)
    end)
    test('Passing Ember breaks equal-distance ties by ID and cannot cross a closed door',function()
        local s=fixture('passing_ember');local dead=foe(s,1,0);ignite(s,dead);dead.hp=1
        local first=foe(s,2,0);local second=foe(s,1,1)
        action(s);assert(first.statuses.burning and not (second.statuses and second.statuses.burning))
        s=fixture('passing_ember');dead=foe(s,1,0);ignite(s,dead);dead.hp=1
        local blocked=foe(s,3,0);G.spawn_door(s,17,16)
        action(s);assert(not (blocked.statuses and blocked.statuses.burning))
    end)
    test('unburned or exempt deaths never pass an ember',function()
        for _,exempt in ipairs({false,true})do
            local s=fixture('passing_ember');local e=foe(s,1,0)
            if exempt then ignite(s,e);e.reward_exempt=true end
            e.hp=1;local next_foe=foe(s,1,1)
            assert(count(action(s),'BoonActivated','passing_ember')==0
                and not (next_foe.statuses and next_foe.statuses.burning))
        end
    end)
    test('an inherited passed-fire kill cannot make an endless automatic ember chain',function()
        local s=fixture('passing_ember');local e=foe(s,1,0);ignite(s,e);e.hp=1
        local next_foe=foe(s,1,1,2);local last=foe(s,2,1)
        local events=action(s)
        assert(next_foe.hp==0 and not (last.statuses and last.statuses.burning))
        assert(count(events,'BoonActivated','passing_ember')==1)
    end)
    test('Purging Seal releases one real cleanse burst without self damage or an empty-try proc',function()
        local s,p=burning_player('purging_seal');grant(s,'field_dressing')
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',6,5,100000);e.hp=100;e.max_hp=100
        R.spawn_supply(s,'health',s.active_floor_id,6,6,function()end,p.id)
        local events=action(s,'move',-1,0)
        assert(not p.statuses.burning and count(events,'BoonActivated','purging_seal')==1 and e.hp==98)
        R.spawn_supply(s,'health',s.active_floor_id,7,6,function()end,p.id);p.hp=10
        assert(count(action(s,'move',1,0),'BoonActivated','purging_seal')==0)
    end)
    test('consuming enemy Burning with Cinder Stopper cannot trigger the owner Purging Seal',function()
        local s=fixture('purging_seal');local e=foe(s,1,0);ignite(s,e);grant(s,'cinder_stopper')
        local events=action(s)
        assert(count(events,'StatusCleansed')==1 and count(events,'BoonActivated','purging_seal')==0)
    end)
    test('cadence inventory and extended queued status retain exact saved action continuation',function()
        local s,p=fixture();for _,slug in ipairs(order)do grant(s,slug)end
        grant(s,'emberbrand');grant(s,'temporal_echo');foe(s,1,0);foe(s,1,1)
        p.hp=10;action(s);assert(next(s.delayed_effects))
        local restored=G.restore(P.decode(P.encode(G.snapshot(s))))
        for _,k in ipairs({'wait','basic_attack','wait','move','wait'})do
            equal(action(s,k,0,-1),action(restored,k,0,-1))
            equal(G.snapshot(s),G.snapshot(restored))
        end
    end)
    test('zero-gain healing stays authoritative but adds no empty playback beat',function()
        local PB=require('src.playback');local pb=PB.new()
        local events={{kind='Healed',actual_gain=0,requested=3,target_id=1}}
        local before=P.encode(events);PB.add(pb,events,1)
        assert(#pb.items==0 and before==P.encode(events))
        PB.add(pb,{{kind='Healed',actual_gain=1,requested=3,target_id=1}},1)
        assert(#pb.items==1)
    end)
end
