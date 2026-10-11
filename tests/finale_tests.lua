return function(test,equal)
    local Game=require('src.game')
    local Content=require('src.content')
    local Finale=require('src.finale')
    local AI=require('src.ai')
    local RNG=require('src.rng')
    local Scheduler=require('src.scheduler')
    local Visibility=require('src.visibility')
    local Persistence=require('src.persistence')
    local Play=require('tests.legal_playthrough')

    local directions={{0,-1},{1,0},{0,1},{-1,0}}
    local function count(events,kind)
        local n=0
        for _,event in ipairs(events) do if event.kind==kind then n=n+1 end end
        return n
    end
    local function approach(state,target)
        local player=Game.player(state)
        local map=Content.map_for(state)
        for _,d in ipairs(directions) do
            local x,y=target.position.x+d[1],target.position.y+d[2]
            local free=Content.walkable(map,x,y)
            for _,other in ipairs(state.entities) do
                if other.id~=player.id and other.blocks_movement
                    and other.position.x==x and other.position.y==y then free=false end
            end
            if free then
                player.position.x,player.position.y=x,y
                Visibility.recompute(state)
                return -d[1],-d[2]
            end
        end
        error('no approach to final encounter object')
    end
    local function prepared(seed,first,second)
        local state=Game.new_stage(seed or 5)
        for index=1,3 do
            for _,actor in ipairs(state.entities) do
                if actor.controller=='ai' then Scheduler.remove_for_actor(state,actor.id) end
            end
            local boss=assert(Game.entity(state,state.stage.elite_id))
            local player=Game.player(state)
            player.max_hp,player.hp,player.damage=300,300,100
            local dx,dy=approach(state,boss)
            local ok,reason=Game.submit(state,{kind='basic_attack',dx=dx,dy=dy})
            assert(ok,reason)
            local exit=assert(Game.entity(state,state.stage.exit_id))
            approach(state,exit)
            local route_index=index==1 and (first or 1)
                or index==2 and (second or 2) or 1
            ok,reason=Game.submit(state,{kind='choose_route',
                exit_id=exit.id,route_index=route_index})
            assert(ok,reason)
        end
        assert(state.finale and #state.retired_stages==3)
        return state
    end

    test('final arena is reachable, distinct and stable across 100 run seeds',function()
        assert(Content.validate_map(Finale.map))
        local map=Finale.map
        local seen={[map.start_y*map.width+map.start_x]=true}
        local queue={{map.start_x,map.start_y}}
        local head=1
        while head<=#queue do
            local p=queue[head];head=head+1
            for _,d in ipairs(directions) do
                local x,y=p[1]+d[1],p[2]+d[2]
                local key=y*map.width+x
                if Content.walkable(map,x,y) and not seen[key] then
                    seen[key]=true;queue[#queue+1]={x,y}
                end
            end
        end
        assert(seen[map.boss_y*map.width+map.boss_x] and #queue>200)
        for seed=1,100 do
            local run_seed=RNG.derive(seed,'finale:core:route/gateway')
            local state=Game.new_finale(run_seed,{root_seed=seed,
                route_history={'core:route/fungal_hollows',
                    'core:route/ashen_bastion','core:route/gateway'}})
            local boss=assert(Game.entity(state,state.finale.boss_id))
            assert(state.run_seed==run_seed and state.map_id==map.id
                and boss.position.x==map.boss_x and boss.position.y==map.boss_y
                and Game.player(state).position.x==map.start_x
                and Game.player(state).position.y==map.start_y
                and not state.stage and not state.generated_map)
        end
    end)

    test('third exit enters final arena once without winning or leaking schedules',function()
        for first=1,2 do
            for second=1,2 do
                local state=prepared(30+first*2+second,first,second)
                assert(state.finale.phase=='active' and not state.game_over
                    and not state.victory and #state.retired_stages==3)
                assert(state.finale.route_history[3]=='core:route/gateway')
                local boss=Game.entity(state,state.finale.boss_id)
                assert(boss.definition_id=='core:enemy/hollow_regent')
                for _,item in ipairs(state.schedule) do
                    assert(item.kind~='stage_pressure' and item.kind~='stage_deadline'
                        and item.actor_id~=state.retired_stages[3].stage.pursuer_id)
                end
                assert(not Game.submit(state,{kind='choose_route',exit_id=1,route_index=1}))
                equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
            end
        end
    end)

    test('Regent commits three different locked shapes and phase changes future speed',function()
        local state=prepared(41)
        local boss=Game.entity(state,state.finale.boss_id)
        local player=Game.player(state)
        player.damage=3
        local dx,dy=approach(state,boss)
        local ids={}
        for index=0,2 do
            boss.ai_memory.attacks=index
            local intent=AI.choose(state,boss)
            assert(intent.kind=='windup' and #intent.cells>0)
            ids[#ids+1]=intent.ability_id
            if index==0 then assert(intent.ranged=='piercing_line') end
            if index>0 then assert(intent.ranged=='area') end
        end
        assert(ids[1]=='core:ability/regent_line'
            and ids[2]=='core:ability/regent_crown'
            and ids[3]=='core:ability/regent_sigil')
        assert(boss.speed==Content.get_enemy(boss.definition_id).speed)
        boss.hp=math.floor(boss.max_hp/2)+1
        local _,_,events=Game.submit(state,{kind='basic_attack',dx=dx,dy=dy})
        assert(boss.phase_two and boss.speed==100
            and count(events,'RegentPhaseChanged')==1)
    end)

    test('Regent pending attack is locked, restores and ordinary damage does not cancel',function()
        local state=prepared(42)
        local boss=Game.entity(state,state.finale.boss_id)
        local player=Game.player(state)
        player.damage=2
        local dx,dy=approach(state,boss)
        local _,_,events=Game.submit(state,{kind='wait'})
        assert(count(events,'WindupStarted')>=1 and boss.pending_attack)
        local locked=Game.snapshot(boss.pending_attack.target_cells)
        local restored=Game.restore(Game.snapshot(state))
        equal(Game.snapshot(state),Game.snapshot(restored))
        local _,_,hit=Game.submit(state,{kind='basic_attack',dx=dx,dy=dy})
        local _,_,replayed=Game.submit(restored,{kind='basic_attack',dx=dx,dy=dy})
        equal(hit,replayed);equal(Game.snapshot(state),Game.snapshot(restored))
        assert(count(hit,'WindupCancelled')==0)
        if boss.pending_attack then equal(locked,boss.pending_attack.target_cells) end
    end)

    test('Regent stun, displacement and death cancel committed windups',function()
        for _,mode in ipairs({'stun','displace','death'}) do
            local state=prepared(50)
            local boss=Game.entity(state,state.finale.boss_id)
            approach(state,boss)
            assert(Game.submit(state,{kind='wait'}))
            assert(boss.pending_attack)
            local events
            if mode=='stun' then
                local ok,reason;ok,reason,events=Game.stun(state,boss.id,150,
                    state.player_id,999)
                assert(ok,reason)
            elseif mode=='displace' then
                local ok,reason;ok,reason,events=Game.displace(state,boss.id,
                    boss.position.x+1,boss.position.y,state.player_id,999)
                assert(ok,reason)
            else
                local player=Game.player(state)
                player.damage=100
                local dx=boss.position.x-player.position.x
                local dy=boss.position.y-player.position.y
                local ok,reason;ok,reason,events=Game.submit(state,
                    {kind='basic_attack',dx=dx,dy=dy})
                assert(ok,reason)
            end
            assert(count(events,'WindupCancelled')==1)
        end
    end)

    test('Burning boss death uses canonical pipeline and exactly one Victory',function()
        local state=prepared(55)
        local boss=Game.entity(state,state.finale.boss_id)
        local player=Game.player(state)
        local dx,dy=approach(state,boss)
        player.damage=2;boss.hp=6
        Game.set_boon_stacks(state,player.id,'core:boon/emberbrand','legendary',1)
        local _,_,events=Game.submit(state,{kind='basic_attack',dx=dx,dy=dy})
        assert(count(events,'StatusApplied')==1 and count(events,'StatusTick')==1
            and count(events,'EntityKilled')==1 and count(events,'Victory')==1
            and state.victory and state.finale.phase=='victory')
        assert(not Game.submit(state,{kind='wait'}))
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test('finale disk-equivalent save restores pending attack and delayed boon effect',function()
        local state=prepared(58)
        local boss=Game.entity(state,state.finale.boss_id)
        local player=Game.player(state)
        player.damage=2
        Game.set_boon_stacks(state,player.id,'core:boon/temporal_echo','legendary',1)
        local dx,dy=approach(state,boss)
        assert(Game.submit(state,{kind='basic_attack',dx=dx,dy=dy}))
        assert(boss.pending_attack and #Game.pending_boon_effects(state)>0)
        local bytes=Persistence.encode(Game.snapshot(state))
        local restored=Game.restore(Persistence.decode(bytes))
        equal(Game.snapshot(state),Game.snapshot(restored))
        local _,_,a=Game.submit(state,{kind='wait'})
        local _,_,b=Game.submit(restored,{kind='wait'})
        equal(a,b);equal(Game.snapshot(state),Game.snapshot(restored))
    end)

    test('terminal Victory and Defeat tombstone both save generations',function()
        local function memory()
            local storage={}
            function storage:read(name)return self[name] end
            function storage:write(name,bytes)self[name]=bytes;return true end
            return storage
        end
        for _,ending in ipairs({'victory','defeat'}) do
            local state=prepared(62)
            local storage=memory()
            local service=Persistence.new(storage)
            assert(service:save(state));assert(service:save(state))
            local boss=Game.entity(state,state.finale.boss_id)
            local player=Game.player(state)
            local dx,dy=approach(state,boss)
            if ending=='victory' then player.damage=100
            else player.hp=1;boss.damage=100;Scheduler.remove_for_actor(state,boss.id)
                Scheduler.enqueue(state,{kind='actor_ready',due=state.clock,
                    actor_id=boss.id,is_player=false}) end
            assert(Game.submit(state,ending=='victory'
                and {kind='basic_attack',dx=dx,dy=dy} or {kind='wait'}))
            if ending=='defeat' and not state.game_over then
                assert(Game.submit(state,{kind='wait'}))
            end
            assert(state.game_over and (state.victory~=nil)==(ending=='victory'))
            assert(service:save(state))
            local fresh=Persistence.new(storage)
            local loaded,meta=fresh:load()
            assert(not loaded and meta.status=='terminal')
            storage[Persistence.slots[2]]=storage[Persistence.slots[2]]:sub(1,12)
            loaded,meta=fresh:load()
            assert(not loaded and meta.status=='terminal')
        end
    end)

    test('seed 3 ordinary actions clear three stages and Hollow Regent',function()
        local run=Play.complete_run(3)
        local state=run.state
        assert(state.victory and state.victory.seed==3
            and #state.retired_stages==3 and #run.trace>=900
            and Play.has_event(run,'RegentPhaseChanged')
            and Play.has_event(run,'Victory'))
        local attacks={}
        for _,event in ipairs(run.events) do
            if event.kind=='WindupStarted' and event.source_id==state.finale.boss_id then
                attacks[event.ability_id]=true
            end
        end
        assert(attacks['core:ability/regent_line']
            and attacks['core:ability/regent_crown']
            and attacks['core:ability/regent_sigil'])
        local replay=Play.replay(run)
        equal(Game.snapshot(state),Game.snapshot(replay.state))
    end)
end
