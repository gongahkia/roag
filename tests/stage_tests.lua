return function(test,equal)
    local Game=require("src.game")
    local Content=require("src.content")
    local Scheduler=require("src.scheduler")
    local AI=require("src.ai")
    local RNG=require("src.rng")
    local Visibility=require("src.visibility")

    local directions={{0,-1},{1,0},{0,1},{-1,0}}
    local function events(output,kind)
        local list={}
        for _,e in ipairs(output) do if e.kind==kind then list[#list+1]=e end end
        return list
    end
    local function quiet(state)
        for _,e in ipairs(state.entities) do
            if e.controller=="ai" then Scheduler.remove_for_actor(state,e.id) end
        end
    end
    local function stand_near(state,entity)
        local player=Game.player(state)
        local map=Content.map_for(state)
        for _,d in ipairs(directions) do
            local x,y=entity.position.x+d[1],entity.position.y+d[2]
            local open=Content.walkable(map,x,y)
            for _,e in ipairs(state.entities) do
                if e.id~=player.id and e.blocks_movement and e.position.x==x
                    and e.position.y==y then open=false end
            end
            if open then
                player.position.x,player.position.y=x,y
                Visibility.recompute(state)
                return entity.position.x-x,entity.position.y-y
            end
        end
        error("no approach to stage object")
    end
    local function defeat(state)
        quiet(state)
        local elite=assert(Game.entity(state,state.stage.elite_id))
        local player=Game.player(state)
        player.damage=math.max(player.damage,elite.hp+elite.defense)
        player.hp=player.max_hp
        local dx,dy=stand_near(state,elite)
        local ok,reason,output=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(ok,reason)
        assert(#events(output,"EliteDefeated")==1)
        return output
    end

    test("stage elite exists once and never overlaps placements across 100 seeds",function()
        for seed=1,100 do
            local state=Game.new_stage(seed)
            local elite=assert(Game.entity(state,state.stage.elite_id))
            assert(elite.definition_id=="core:enemy/thorn_warden" and elite.dormant)
            assert(elite.position.x==state.generated_map.elite.x
                and elite.position.y==state.generated_map.elite.y)
            for _,item in ipairs(state.schedule) do
                assert(item.actor_id~=elite.id)
            end
            local count,seen=0,{}
            for _,entity in ipairs(state.entities) do
                if entity.definition_id==elite.definition_id then count=count+1 end
                if entity.blocks_movement then
                    local key=entity.position.x..","..entity.position.y
                    assert(not seen[key]); seen[key]=true
                end
            end
            assert(count==1 and state.stage.phase=="exploring")
            defeat(state)
            local chest=Game.entity(state,state.stage.elite_reward_id)
            local exit=Game.entity(state,state.stage.exit_id)
            assert(chest and exit and chest.price==0)
            local function has_approach(object)
                for _,d in ipairs(directions) do
                    local x,y=object.position.x+d[1],object.position.y+d[2]
                    local free=Content.walkable(state.generated_map,x,y)
                    for _,other in ipairs(state.entities) do
                        if other.blocks_movement and other.position.x==x
                            and other.position.y==y then free=false end
                    end
                    if free then return true end
                end
            end
            assert(has_approach(chest) and has_approach(exit))
            local hero=Game.player(state)
            hero.hp,hero.max_hp,hero.defense=1000,1000,100
            while state.clock<state.stage.deadline do
                assert(Game.submit(state,{kind="wait"}))
            end
            assert(state.stage.phase=="pursuit" and
                Game.entity(state,state.stage.pursuer_id))
        end
    end)

    test("Warden has different locked sweep and root line decisions",function()
        local state=Game.new_stage(3)
        local elite=Game.entity(state,state.stage.elite_id)
        local dx,dy=stand_near(state,elite)
        elite.ai_memory.attacks=0
        local sweep=AI.choose(state,elite)
        elite.ai_memory.attacks=1
        local roots=AI.choose(state,elite)
        assert(sweep.kind=="windup" and sweep.ability_id=="core:ability/warden_sweep"
            and #sweep.cells==3)
        assert(roots.kind=="windup" and roots.ability_id=="core:ability/warden_roots"
            and #roots.cells>=1 and roots.ranged)
        assert(dx~=0 or dy~=0)
    end)

    test("both Warden patterns commit distinct scheduler telegraphs",function()
        local base=Game.new_stage(12)
        quiet(base)
        local elite=Game.entity(base,base.stage.elite_id)
        stand_near(base,elite)
        base.stage.phase="elite_active"
        elite.dormant=nil
        local function commit(pattern)
            local state=Game.restore(Game.snapshot(base))
            local e=Game.entity(state,state.stage.elite_id)
            e.ai_memory.attacks=pattern
            Scheduler.enqueue(state,{kind="actor_ready",due=state.clock,
                actor_id=e.id,is_player=false})
            local _,_,out=Game.submit(state,{kind="wait"})
            local windup=events(out,"WindupStarted")
            assert(#windup>=1 and windup[1].source_id==e.id)
            return windup[1]
        end
        local sweep,roots=commit(0),commit(1)
        assert(sweep.ability_id=="core:ability/warden_sweep"
            and roots.ability_id=="core:ability/warden_roots")
        assert(#sweep.target_cells==3 and #roots.target_cells==1)
        assert(sweep.due>sweep.time and roots.due>roots.time)
    end)

    test("elite activation and phase change use committed simulation only",function()
        local state=Game.new_stage(2)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        stand_near(state,elite)
        local initial=Game.snapshot(state)
        Game.player(state); Visibility.recompute(state)
        equal(initial,Game.snapshot(state))
        local ok,reason,output=Game.submit(state,{kind="wait"})
        assert(ok,reason)
        assert(state.stage.phase=="elite_active" and #events(output,"EliteEngaged")==1)
        quiet(state)
        elite.hp=math.floor(elite.max_hp/2)+1
        local player=Game.player(state)
        player.damage=2+elite.defense
        local dx,dy=elite.position.x-player.position.x,elite.position.y-player.position.y
        local _,_,hit=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(elite.phase_two and #events(hit,"ElitePhaseChanged")==1)
        local previous=elite.speed
        local _,_,next_hit=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(#events(next_hit,"ElitePhaseChanged")==0 and elite.speed==previous)
    end)

    test("Warden windup survives damage but stun, displacement and death cancel it",function()
        local state=Game.new_stage(11)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        local player=Game.player(state)
        player.speed=200;player.hp=player.max_hp
        stand_near(state,elite)
        assert(Game.submit(state,{kind="wait"}))
        assert(elite.pending_attack and elite.pending_attack.due>state.clock)
        local locked=Game.snapshot(elite.pending_attack.target_cells)
        local base=Game.snapshot(state)
        local dx,dy=elite.position.x-player.position.x,elite.position.y-player.position.y
        local _,_,out=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(#events(out,"WindupCancelled")==0)
        assert(#events(out,"AttackPerformed")>=2) -- player and committed elite impact
        equal(locked,events(out,"AttackPerformed")[2].target_cells)
        local stunned=Game.restore(base)
        local yes,reason,interrupt=Game.stun(stunned,elite.id,200,player.id,10)
        assert(yes,reason)
        assert(#events(interrupt,"WindupCancelled")==1)
        local _,_,after=Game.submit(stunned,{kind="wait"})
        assert(#events(after,"AttackPerformed")==0)
        local displaced=Game.restore(base)
        local e2=Game.entity(displaced,elite.id)
        local moved=false
        for _,d in ipairs(directions) do
            local x,y=e2.position.x+d[1],e2.position.y+d[2]
            local free=Content.walkable(displaced.generated_map,x,y)
            for _,v in ipairs(displaced.entities) do
                if v.blocks_movement and v.position.x==x and v.position.y==y then free=false end
            end
            if free then
                assert(Game.displace(displaced,e2.id,x,y,player.id,11))
                moved=true;break
            end
        end
        assert(moved and not e2.pending_attack)
        local killed=Game.restore(base)
        local k=Game.entity(killed,elite.id)
        k.hp=1
        Game.player(killed).damage=20
        local _,_,final=Game.submit(killed,{kind="basic_attack",dx=dx,dy=dy})
        assert(#events(final,"WindupCancelled")==1 and not Game.entity(killed,elite.id)
            and killed.stage.phase=="extraction")
    end)

    test("elite death creates exactly one free cached chest, exit and deadline",function()
        local state=Game.new_stage(1)
        local output=defeat(state)
        local stage=state.stage
        assert(stage.phase=="extraction" and stage.defeat_time==0
            and stage.deadline==Content.get_stage_rules().grace)
        local chest=Game.entity(state,stage.elite_reward_id)
        local exit=Game.entity(state,stage.exit_id)
        assert(chest.price==0 and #chest.offers==3 and exit.controller=="exit")
        assert(chest.position.x~=exit.position.x or chest.position.y~=exit.position.y)
        local offers=Game.snapshot(chest.offers)
        stand_near(state,chest)
        local menu=Game.chest_options(state,chest.id)
        assert(menu and menu.price==0 and #menu.offers==3)
        equal(offers,Game.chest_options(state,chest.id).offers)
        local ok,reason,claim=Game.submit(state,{kind="claim_chest",chest_id=chest.id,
            offer_index=1})
        assert(ok,reason)
        assert(chest.claimed and #events(claim,"BoonGranted")==1)
        assert(#events(output,"EntityKilled")==1)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("enemy friendly fire can finish Warden without player kill credit",function()
        local state=Game.new_stage(4)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        elite.hp=1
        local x,y
        for _,d in ipairs(directions) do
            local nx,ny=elite.position.x+d[1],elite.position.y+d[2]
            if Content.walkable(state.generated_map,nx,ny) then
                local free=true
                for _,e in ipairs(state.entities) do
                    if e.blocks_movement and e.position.x==nx and e.position.y==ny then
                        free=false
                    end
                end
                if free then x,y=nx,ny;break end
            end
        end
        assert(x)
        local attacker=Game.spawn_enemy(state,"core:enemy/mossbound_guard",x,y)
        Scheduler.remove_for_actor(state,attacker.id)
        attacker.damage=5
        local impact=Scheduler.enqueue(state,{kind="impact",due=50,
            actor_id=attacker.id,attack_id=999})
        attacker.pending_attack={schedule_id=impact.id,sequence=impact.seq,
            action_id=999,source_id=attacker.id,floor_id=state.active_floor_id,
            source_x=x,source_y=y,ability_id="core:ability/guard_strike",
            target_cells={{floor_id=state.active_floor_id,x=elite.position.x,
                y=elite.position.y}},ranged=false,damage=5,due=50,committed_time=0}
        local xp=state.progression.xp_total
        local _,_,out=Game.submit(state,{kind="wait"})
        assert(#events(out,"EliteDefeated")==1 and state.stage.phase=="extraction")
        assert(state.progression.xp_total==xp and #events(out,"ExperienceGained")==0)
        assert(#events(out,"CurrencyDropped")>=1)
    end)

    test("a credited boon explosion can defeat Warden through the event queue",function()
        local state=Game.new_stage(13)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        elite.hp=1
        local player=Game.player(state)
        Game.set_boon_stacks(state,player.id,"core:boon/detonation_bloom","legendary",1)
        local guard,dx,dy
        for _,d in ipairs(directions) do
            local gx,gy=elite.position.x+d[1],elite.position.y+d[2]
            local px,py=gx+d[1],gy+d[2]
            if Content.walkable(state.generated_map,gx,gy)
                and Content.walkable(state.generated_map,px,py) then
                local free=true
                for _,e in ipairs(state.entities) do
                    if e.blocks_movement and ((e.position.x==gx and e.position.y==gy)
                        or (e.position.x==px and e.position.y==py)) then free=false end
                end
                if free then
                    guard=Game.spawn_enemy(state,"core:enemy/mossbound_guard",gx,gy)
                    Scheduler.remove_for_actor(state,guard.id)
                    player.position.x,player.position.y=px,py
                    dx,dy=-d[1],-d[2]
                    break
                end
            end
        end
        assert(guard)
        guard.hp=1
        local xp=state.progression.xp_total
        local ok,reason,out=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(ok,reason)
        assert(#events(out,"BoonActivated")>=1
            and #events(out,"EliteDefeated")==1
            and #events(out,"EntityKilled")==2)
        assert(state.stage.phase=="extraction" and state.progression.xp_total>xp)
    end)

    test("a saved delayed echo can finish Warden at its committed timestamp",function()
        local state=Game.new_stage(14)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        elite.hp=4
        elite.dormant=nil
        state.stage.phase="elite_active"
        local player=Game.player(state)
        player.damage=4
        Game.set_boon_stacks(state,player.id,"core:boon/temporal_echo","legendary",1)
        local dx,dy=stand_near(state,elite)
        local _,_,primary=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(elite.hp==1 and #events(primary,"DelayedEffectScheduled")==1)
        assert(state.stage.phase=="elite_active" and state.clock<200)
        local restored=Game.restore(Game.snapshot(state))
        local _,_,after=Game.submit(state,{kind="wait"})
        local _,_,after2=Game.submit(restored,{kind="wait"})
        equal(after,after2)
        equal(Game.snapshot(state),Game.snapshot(restored))
        assert(#events(after,"DelayedEffectResolved")==1
            and #events(after,"EliteDefeated")==1
            and state.stage.defeat_time==200)
    end)

    test("pressure is time-driven, bounded and uses independent RNG",function()
        local state=Game.new_stage(5)
        quiet(state)
        local before=state.rng.state
        local pressure_before=state.stage.pressure_rng.state
        local snap=Game.snapshot(state)
        for _=1,4 do Game.player(state); Visibility.recompute(state) end
        equal(snap,Game.snapshot(state))
        local spawned={}
        for _=1,Content.get_stage_rules().pressure_first/100 do
            local _,_,out=Game.submit(state,{kind="wait"})
            for _,e in ipairs(events(out,"PressureSpawned")) do spawned[#spawned+1]=e end
        end
        assert(state.clock==Content.get_stage_rules().pressure_first and #spawned==1)
        assert(state.rng.state==before and state.stage.pressure_rng.state~=pressure_before)
        local e=spawned[1]
        assert(not Game.is_visible(state,state.active_floor_id,e.x,e.y))
        assert(math.abs(e.x-Game.player(state).position.x)
            +math.abs(e.y-Game.player(state).position.y)>=Content.get_stage_rules().pressure_safe_radius)
    end)

    test("pressure reports a legal no-spawn outcome without hanging",function()
        local state=Game.new_stage(6)
        quiet(state)
        for _=1,Content.get_stage_rules().pressure_first/100-1 do
            assert(Game.submit(state,{kind="wait"}))
        end
        local map=state.generated_map
        for y=1,map.height do
            for x=1,map.width do
                state.visible.cells[(y-1)*map.width+x]=true
            end
        end
        local _,_,out=Game.submit(state,{kind="wait"})
        assert(#events(out,"PressureSpawned")==0
            and state.stage.pressure_count==0)
        assert(state.stage.diagnostics[#state.stage.diagnostics].kind=="no_legal_anchor")
    end)

    test("stage pressure and enemy turns replay identically from seed and snapshot",function()
        local first,second=Game.new_stage(25),Game.new_stage(25)
        equal(Game.snapshot(first),Game.snapshot(second))
        for _=1,Content.get_stage_rules().pressure_first/100 do
            local ok,reason,out=Game.submit(first,{kind="wait"})
            local ok2,reason2,out2=Game.submit(second,{kind="wait"})
            assert(ok and ok2,reason or reason2)
            equal(out,out2)
        end
        equal(Game.snapshot(first),Game.snapshot(second))
        local restored=Game.restore(Game.snapshot(first))
        for _=1,3 do
            local _,_,out=Game.submit(first,{kind="wait"})
            local _,_,out2=Game.submit(restored,{kind="wait"})
            equal(out,out2)
        end
        equal(Game.snapshot(first),Game.snapshot(restored))
    end)

    test("deadline impact precedes ready player and spawns one immune pursuer",function()
        local state=Game.new_stage(7)
        defeat(state)
        quiet(state)
        local deadline=state.stage.deadline
        local saved
        while state.clock<deadline-100 do
            assert(Game.submit(state,{kind="wait"}))
        end
        saved=Game.snapshot(state)
        local restored=Game.restore(saved)
        local ok,reason,out=Game.submit(state,{kind="wait"})
        local ok2,reason2,out2=Game.submit(restored,{kind="wait"})
        assert(ok and ok2,reason or reason2)
        equal(Game.snapshot(state),Game.snapshot(restored))
        equal(out,out2)
        assert(state.clock==deadline and state.stage.phase=="pursuit")
        assert(#events(out,"PursuerArrived")==1)
        local pursuer=Game.entity(state,state.stage.pursuer_id)
        assert(pursuer and pursuer.invulnerable)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
        local before=state.currency
        local dx,dy=stand_near(state,pursuer)
        local _,_,attack=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        local blocked,harmed,contacts=0,0,0
        for _,event in ipairs(attack) do
            if event.target_id==pursuer.id then
                if event.kind=="DamageBlocked" then blocked=blocked+1 end
                if event.kind=="DamageTaken" then harmed=harmed+1 end
                if event.kind=="HitConfirmed" then contacts=contacts+1 end
            end
        end
        assert(blocked>=1 and harmed==0 and contacts==0)
        local threatening=false
        for _,event in ipairs(attack) do
            if event.source_id==pursuer.id and
                (event.kind=="Moved" or event.kind=="WindupStarted"
                    or event.kind=="AttackPerformed") then threatening=true end
        end
        assert(threatening)
        assert(pursuer.hp==pursuer.max_hp and state.currency==before)
        assert(not Game.stun(state,pursuer.id,100)
            and not Game.displace(state,pursuer.id,pursuer.position.x+1,pursuer.position.y))
    end)

    test("route menu is free, stable and transitions one time preserving build",function()
        local state=Game.new_stage(8)
        defeat(state)
        quiet(state)
        local exit=Game.entity(state,state.stage.exit_id)
        local dx,dy=stand_near(state,exit)
        local before=Game.snapshot(state)
        local ok,reason,_,preview=Game.submit(state,{kind="move",dx=dx,dy=dy})
        assert(not ok and reason=="exit" and preview.exit_id==exit.id)
        local menu=Game.route_options(state,exit.id)
        assert(#menu.routes==2)
        equal(before,Game.snapshot(state))
        Game.set_boon_stacks(state,state.player_id,"core:boon/crescent_reach","rare",2)
        local player=Game.player(state)
        player.hp=17; player.max_hp=31; state.currency=13
        state.progression.level=2; state.progression.xp_total=7
        local next_id=state.next_entity_id
        local root=state.root_seed
        local pending_id=state.next_schedule_id
        local pending=Scheduler.enqueue(state,{kind="effect",due=state.clock+500,
            effect_id=pending_id})
        state.delayed_effects[pending.id]={activation={ancestry={},
            owner_id=state.player_id,root_action_id=300,chain_id=300,
            event={target_cells={}}},effect={}}
        local alternative=Game.restore(Game.snapshot(state))
        local committed,err,out=Game.submit(state,{kind="choose_route",
            exit_id=exit.id,route_index=2})
        assert(committed,err)
        assert(#events(out,"StageEntered")==1 and state.stage.index==2
            and state.clock==0 and #state.retired_stages==1)
        assert(state.run_seed==RNG.derive(root,"stage:2:core:route/sunken_aqueduct")
            and state.stage.biome_id=="core:biome/sunken_aqueduct"
            and state.generated_map.strategy=="canals")
        assert(Game.submit(alternative,{kind="choose_route",exit_id=exit.id,
            route_index=1}))
        assert(alternative.run_seed~=state.run_seed)
        assert(state.player_id>=next_id and
            state.stage.route_history[1]=="core:route/sunken_aqueduct")
        player=Game.player(state)
        assert(player.hp==17 and player.max_hp==31 and state.currency==13
            and state.progression.level==2 and state.progression.xp_total==7
            and player.boons["core:boon/crescent_reach"].rare==2)
        assert(state.retired_stages[1].stage.phase=="transition")
        assert(state.retired_stages[1].delayed_effects[pending.id]
            and not state.delayed_effects[pending.id])
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
        assert(not Game.submit(state,{kind="choose_route",exit_id=exit.id,route_index=2}))
    end)

    test("three biome stages hand off to a living final boss, not victory",function()
        local state=Game.new_stage(9)
        for index=1,3 do
            defeat(state)
            quiet(state)
            local exit=Game.entity(state,state.stage.exit_id)
            stand_near(state,exit)
            local ok,reason=Game.submit(state,{kind="choose_route",exit_id=exit.id,
                route_index=1})
            assert(ok,reason)
            if index<3 then assert(state.stage.index==index+1 and state.stage.phase=="exploring")
            else assert(state.finale and state.finale.phase=="active"
                and not state.game_over and not state.victory
                and Game.entity(state,state.finale.boss_id)
                and #state.retired_stages==3) end
        end
        assert(#state.finale.route_history==3)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
        assert(Game.submit(state,{kind="wait"}))
    end)

    test("continuous controlled elite fight, reward, pursuer, route and next-stage combat",function()
        local state=Game.new_stage(17)
        quiet(state)
        local elite=Game.entity(state,state.stage.elite_id)
        local hero=Game.player(state)
        hero.max_hp,hero.hp,hero.damage=500,500,15
        stand_near(state,elite)
        local _,_,engagement=Game.submit(state,{kind="wait"})
        assert(state.stage.phase=="elite_active"
            and #events(engagement,"WindupStarted")>=1)
        local phase_changes,death_count=0,0
        for _=1,5 do
            if not Game.entity(state,elite.id) then break end
            local dx,dy=stand_near(state,elite)
            local ok,reason,out=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
            assert(ok,reason)
            phase_changes=phase_changes+#events(out,"ElitePhaseChanged")
            death_count=death_count+#events(out,"EliteDefeated")
        end
        assert(phase_changes==1 and death_count==1
            and state.stage.phase=="extraction" and hero.hp<hero.max_hp)
        quiet(state)
        local chest=Game.entity(state,state.stage.elite_reward_id)
        stand_near(state,chest)
        local chosen=Game.chest_options(state,chest.id).offers[1]
        assert(Game.submit(state,{kind="claim_chest",chest_id=chest.id,offer_index=1}))
        assert(chest.claimed)
        local owned=Game.player(state).boons[chosen.boon_id][chosen.rarity]
        assert(owned==1)
        local arrived=0
        while state.clock<state.stage.deadline do
            local ok,reason,out=Game.submit(state,{kind="wait"})
            assert(ok,reason)
            arrived=arrived+#events(out,"PursuerArrived")
        end
        assert(arrived==1 and state.stage.phase=="pursuit")
        local exit=Game.entity(state,state.stage.exit_id)
        stand_near(state,exit)
        local snapshot=Game.snapshot(state)
        local restored=Game.restore(snapshot)
        local ok,reason,out=Game.submit(state,{kind="choose_route",exit_id=exit.id,
            route_index=1})
        local ok2,reason2,out2=Game.submit(restored,{kind="choose_route",
            exit_id=exit.id,route_index=1})
        assert(ok and ok2,reason or reason2)
        equal(out,out2)
        equal(Game.snapshot(state),Game.snapshot(restored))
        assert(Game.player(state).boons[chosen.boon_id][chosen.rarity]==owned)
        quiet(state)
        local target
        for _,entity in ipairs(state.entities) do
            if entity.controller=="ai" and entity.id~=state.stage.elite_id then
                target=entity;break
            end
        end
        assert(target)
        local dx,dy=stand_near(state,target)
        local _,_,attack=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(#events(attack,"AttackPerformed")>=1
            and #events(attack,"DamageTaken")>=1)
    end)
end
