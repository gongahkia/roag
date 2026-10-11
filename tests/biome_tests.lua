return function(test,equal)
    local Biomes=require("src.biomes")
    local Rooms=require("src.rooms")
    local Generation=require("src.generation")
    local Game=require("src.game")
    local AI=require("src.ai")
    local Content=require("src.content")
    local Scheduler=require("src.scheduler")
    local Visibility=require("src.visibility")
    local Persistence=require("src.persistence")
    local Play=require("tests.legal_playthrough")

    local function count(events,kind)
        local n=0
        for _,event in ipairs(events) do if event.kind==kind then n=n+1 end end
        return n
    end
    local function adjacent(a,b)
        return math.abs(a.x-b.x)+math.abs(a.y-b.y)==1
    end
    local function stand_near(state,entity)
        local p=Game.player(state)
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
            local x,y=entity.position.x+d[1],entity.position.y+d[2]
            local occupied=false
            for _,e in ipairs(state.entities) do
                if e.id~=p.id and e.blocks_movement and
                    e.position.x==x and e.position.y==y then occupied=true end
            end
            if Content.walkable(state.generated_map,x,y) and not occupied then
                p.position.x,p.position.y=x,y
                Visibility.recompute(state)
                return -d[1],-d[2]
            end
        end
        error("no object approach")
    end
    local function assisted_exit(state,choice)
        -- Unit fixture for branch coverage. The separate guided route below
        -- uses ordinary actions only and never assigns combat state.
        for _,e in ipairs(state.entities) do
            if e.controller=="ai" then Scheduler.remove_for_actor(state,e.id) end
        end
        local elite=assert(Game.entity(state,state.stage.elite_id))
        local dx,dy=stand_near(state,elite)
        Game.player(state).damage=elite.hp+elite.defense+10
        local ok,why=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(ok,why)
        local exit=assert(Game.entity(state,state.stage.exit_id))
        stand_near(state,exit)
        local routes=assert(Game.route_options(state,exit.id))
        local target=routes.routes[choice]
        assert(target)
        local before=state.clock
        assert(Game.route_options(state,exit.id).routes[choice].id==target.id)
        assert(state.clock==before)
        ok,why=Game.submit(state,{kind="choose_route",exit_id=exit.id,
            route_index=choice})
        assert(ok,why)
        assert(state.clock==0)
        return target
    end

    test("four biome registries select transformable authored rooms",function()
        assert(Biomes.validate() and #Biomes.order==4)
        local seen={}
        for _,id in ipairs(Biomes.order) do
            local b=assert(Biomes.get(id))
            local editable_count=0
            local roles={"start","combat","treasure","landmark","elite"}
            for _,role in ipairs(roles) do
                local pool=Rooms.for_role(role,id)
                assert(#pool>0)
                for _,def in ipairs(pool) do
                    editable_count=editable_count+#def.editable
                    assert(not seen[def.id] or seen[def.id]==id)
                    seen[def.id]=id
                    local turned=Rooms.transform(def,1,true)
                    assert(turned.width==#def.rows and turned.height==#def.rows[1]
                        and #turned.sockets==4 and #turned.markers.secret_anchor>0)
                end
            end
            assert(editable_count>0,"biome lost its editable variation masks")
        end
        assert(#Rooms.for_role("combat",Biomes.order[2])>0)
    end)

    test("Aqueduct water is static blocked geometry but transparent sight",function()
        local map=Generation.generate(3,{biome_id=Biomes.order[3]})
        local found=false
        for y,row in ipairs(map.rows) do
            local x=row:find("~",1,true)
            if x then
                assert(not Content.walkable(map,x,y)
                    and not Content.blocks_vision(map,x,y)
                    and Content.projectile_passable(map,x,y)
                    and map.visual_rows[y]:sub(x,x)=="q")
                found=true;break
            end
        end
        assert(found,"Aqueduct lacks visible channel water")
    end)

    test("four 100-seed floor corpora retain distinct playable geometry",function()
        for _,id in ipairs(Biomes.order) do
            local biome=Biomes.get(id)
            local started=os.clock()
            local retry,repair,fallback,parallel,widened,bends=0,0,0,0,0,0
            for seed=1,100 do
                local map=Generation.generate(seed,{biome_id=id})
                local replay=Generation.generate(seed,{biome_id=id})
                equal(map,replay)
                local valid,errors=Generation.validate(map)
                assert(valid,errors[1] and errors[1].kind)
                assert(map.biome_id==id and map.id==biome.map_id
                    and #map.rooms>=8 and #map.rooms<=12
                    and #map.placements.chests==5 and #map.placements.secrets==2
                    and #map.placements.doors==1 and map.elite)
                local has_signature=false
                for _,enemy in ipairs(map.placements.enemies) do
                    if enemy.definition_id==biome.enemy_ids[1] then has_signature=true end
                end
                assert(has_signature,"biome-specific enemy absent")
                for _,room in ipairs(map.rooms) do
                    assert(room.template_id:sub(1,#biome.room_prefix)==biome.room_prefix)
                end
                if biome.strategy=="courtyards" then
                    local hub=false
                    for _,room_id in ipairs(map.graph.critical) do
                        if room_id==6 then hub=true end
                    end
                    assert(hub,"courtyard hub missing from main route")
                    for _,room in ipairs(map.rooms) do
                        if room.id==6 then
                            assert(room.template_id==biome.hub_template_id)
                        end
                    end
                end
                for _,corridor in ipairs(map.corridors) do
                    if corridor.parallel then parallel=parallel+1 end
                    if corridor.widened then widened=widened+1 end
                    local turns=0
                    for i=3,#corridor.cells do
                        local a,b,c=corridor.cells[i-2],corridor.cells[i-1],corridor.cells[i]
                        if b.x-a.x~=c.x-b.x or b.y-a.y~=c.y-b.y then turns=turns+1 end
                    end
                    bends=bends+turns
                end
                retry=retry+(map.attempt>0 and 1 or 0)
                repair=repair+#map.repairs
                fallback=fallback+(map.diagnostics.fallback and 1 or 0)
            end
            if biome.strategy=="canals" then assert(parallel>=100) end
            if biome.strategy=="courtyards" then assert(widened>=100) end
            if biome.strategy=="warrens" then assert(bends>=200) end
            print(("BIOME CORPUS %s: 100 valid, %d retries, %d repairs, %d fallback, %d bends, %d parallel, %d widened, %.3fs CPU")
                :format(id,retry,repair,fallback,bends,parallel,widened,
                    os.clock()-started))
        end
    end)

    test("biome faults repair locally or enter validated protected fallback",function()
        for _,id in ipairs(Biomes.order) do
            local repaired=Generation.generate(11,{biome_id=id,fault="editable"})
            assert(Generation.validate(repaired) and #repaired.repairs>=1
                and repaired.repairs[1].kind=="restore_corridor")
            local protected=Generation.generate(11,{biome_id=id,fault="protected",
                retry_limit=1})
            assert(protected.diagnostics.fallback and Generation.validate(protected)
                and #protected.diagnostics.attempts==2)
        end
    end)

    test("100 runtime stages per biome place one Warden, free chest and exit",function()
        for index=1,#Biomes.order do
            local id=Biomes.order[index]
            local stage_index=index==1 and 1 or 2
            local history=stage_index==1 and {} or
                {(id:gsub("^core:biome/","core:route/"))}
            for seed=1,100 do
                local state=Game.new_stage(seed,{stage_options={index=stage_index,
                    biome_id=id,route_history=history}})
                local elite=assert(Game.entity(state,state.stage.elite_id))
                assert(elite.position.x==state.generated_map.elite.x
                    and elite.position.y==state.generated_map.elite.y)
                for _,e in ipairs(state.entities) do
                    if e.controller=="ai" then Scheduler.remove_for_actor(state,e.id) end
                end
                local dx,dy=stand_near(state,elite)
                Game.player(state).damage=elite.hp+elite.defense+1
                local ok,why=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
                assert(ok,why)
                local chest=assert(Game.entity(state,state.stage.elite_reward_id))
                local exit=assert(Game.entity(state,state.stage.exit_id))
                assert(chest.price==0 and #chest.offers==3 and
                    exit.controller=="exit" and exit.id~=chest.id and
                    state.stage.phase=="extraction")
                local approach=false
                for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
                    if Content.walkable(state.generated_map,
                        exit.position.x+d[1],exit.position.y+d[2]) then
                        approach=true
                    end
                end
                assert(approach,"elite exit has no cardinal approach")
            end
        end
    end)

    test("new biomes retain extraction deadlines and one reachable pursuer",function()
        for index=2,4 do
            local id=Biomes.order[index]
            local state=Game.new_stage(7,{stage_options={index=2,
                biome_id=id,route_history={(id:gsub("^core:biome/","core:route/"))}}})
            for _,e in ipairs(state.entities) do
                if e.controller=="ai" then Scheduler.remove_for_actor(state,e.id) end
            end
            local elite=assert(Game.entity(state,state.stage.elite_id))
            local dx,dy=stand_near(state,elite)
            local player=Game.player(state)
            player.hp,player.max_hp,player.defense=1000,1000,100
            player.damage=elite.hp+elite.defense+1
            assert(Game.submit(state,{kind="basic_attack",dx=dx,dy=dy}))
            local before_pressure=state.stage.pressure_count
            local before_deadline=state.stage.deadline
            while state.clock<before_deadline do
                assert(Game.submit(state,{kind="wait"}))
            end
            local pursuer=assert(Game.entity(state,state.stage.pursuer_id))
            assert(state.stage.phase=="pursuit" and pursuer.invulnerable
                and state.stage.pressure_count==before_pressure
                and Game.entity(state,state.stage.exit_id))
            local restored=Game.restore(Game.snapshot(state))
            assert(restored.stage.deadline==before_deadline
                and restored.stage.pursuer_id==state.stage.pursuer_id)
        end
    end)

    test("new enemies choose three distinct locked shapes and use shared damage",function()
        local cases={
            {id="core:enemy/spore_herald",x=10,y=9,ability="core:ability/spore_burst",n=3},
            {id="core:enemy/canal_lancer",x=9,y=9,ability="core:ability/canal_thrust",n=2},
            {id="core:enemy/cinder_bellkeeper",x=10,y=9,ability="core:ability/bell_cross",n=5},
        }
        for _,case in ipairs(cases) do
            local state=Game.new(8)
            local player=Game.player(state)
            player.position.x,player.position.y=7,9
            local enemy=Game.spawn_enemy(state,case.id,case.x,case.y)
            local intent=AI.choose(state,enemy)
            assert(intent.kind=="windup" and intent.ability_id==case.ability
                and #intent.cells==case.n)
            local ok,why,out=Game.submit(state,{kind="wait"})
            assert(ok,why)
            assert(count(out,"WindupStarted")>=1)
            local windup=enemy.pending_attack
            if windup then
                local locked=windup.target_cells[1]
                player.position.x,player.position.y=12,9
                Visibility.recompute(state)
                assert(windup.target_cells[1].x==locked.x and
                    windup.target_cells[1].y==locked.y)
            end
            equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
        end
    end)

    test("biome area attacks retain locked cells, friendly fire and wall blocking",function()
        local state=Game.new(8)
        local player=Game.player(state)
        player.position.x,player.position.y=7,9
        local herald=Game.spawn_enemy(state,"core:enemy/spore_herald",10,9)
        local ally=Game.spawn_enemy(state,"core:enemy/mossbound_guard",7,8)
        Scheduler.remove_for_actor(state,ally.id)
        herald.speed=35
        local intent=AI.choose(state,herald)
        assert(intent.kind=="windup" and #intent.cells==3)
        local locked={}
        for i,p in ipairs(intent.cells) do locked[i]={x=p.x,y=p.y} end
        local damaged=false
        for _=1,4 do
            local ok,why,out=Game.submit(state,{kind="wait"})
            assert(ok,why)
            for _,event in ipairs(out) do
                if event.kind=="DamageTaken" and event.target_id==ally.id
                    and event.source_id==herald.id then damaged=true end
            end
            if damaged then break end
        end
        assert(damaged and ally.hp<ally.max_hp,"spore ally was not hit")
        for i,p in ipairs(locked) do
            assert(p.x==intent.cells[i].x and p.y==intent.cells[i].y)
        end
        for _,id in ipairs({"core:enemy/canal_lancer",
            "core:enemy/cinder_bellkeeper","core:enemy/spore_herald"}) do
            local blocked=Game.new(8)
            local p=Game.player(blocked)
            p.position.x,p.position.y=5,7
            local enemy=Game.spawn_enemy(blocked,id,7,7)
            assert(AI.choose(blocked,enemy).kind~="windup")
        end
        local corner=Game.new(8)
        local p=Game.player(corner)
        p.position.x,p.position.y=3,3
        local herald=Game.spawn_enemy(corner,"core:enemy/spore_herald",4,4)
        assert(AI.choose(corner,herald).kind~="windup",
            "spore ray cut through a blocked diagonal corner")
    end)

    test("new biome pressure draws from its own hidden encounter pool",function()
        for index=2,4 do
            local id=Biomes.order[index]
            local state=Game.new_stage(17,{stage_options={index=2,
                biome_id=id,route_history={(id:gsub("^core:biome/","core:route/"))}}})
            for _,e in ipairs(state.entities) do
                if e.controller=="ai" then Scheduler.remove_for_actor(state,e.id) end
            end
            local player=Game.player(state)
            player.hp,player.max_hp,player.defense=1000,1000,100
            local spawned
            for _=1,15 do
                local ok,why,out=Game.submit(state,{kind="wait"})
                assert(ok,why)
                for _,event in ipairs(out) do
                    if event.kind=="PressureSpawned" then spawned=event end
                end
                if spawned then break end
            end
            assert(spawned,"pressure never produced biome encounter")
            local pool=Biomes.get(id).enemy_ids
            assert(spawned.definition_id==pool[1] or
                spawned.definition_id==pool[2] or spawned.definition_id==pool[3])
            assert(not Visibility.is_visible(state,state.active_floor_id,
                spawned.x,spawned.y))
        end
    end)

    test("new-biome concrete floor, pending AI and boon state survive inert bytes",function()
        for index=2,3 do
            local id=Biomes.order[index]
            local state=Game.new_stage(23,{stage_options={index=2,
                biome_id=id,route_history={(id:gsub("^core:biome/","core:route/"))}}})
            Game.set_boon_stacks(state,state.player_id,"core:boon/emberbrand","rare",2)
            for _,e in ipairs(state.entities) do
                if e.controller=="ai" then Scheduler.remove_for_actor(state,e.id) end
            end
            local player=Game.player(state)
            player.hp,player.max_hp=100,100
            local enemy=Game.spawn_enemy(state,Biomes.get(id).enemy_ids[1],
                player.position.x+2,player.position.y)
            enemy.speed=35
            assert(Game.submit(state,{kind="wait"}))
            assert(enemy.pending_attack and #enemy.pending_attack.target_cells>=2)
            local bytes=Persistence.encode(Game.snapshot(state))
            local restored=Game.restore(Persistence.decode(bytes))
            assert(restored.generated_map.biome_id==id
                and restored.generated_map.rows[1]==state.generated_map.rows[1]
                and restored.entities[1].boons["core:boon/emberbrand"].rare==2
                and Game.entity(restored,enemy.id).pending_attack.schedule_id==
                    enemy.pending_attack.schedule_id)
            local a_ok,a_why,a_events=Game.submit(state,{kind="wait"})
            local b_ok,b_why,b_events=Game.submit(restored,{kind="wait"})
            assert(a_ok==b_ok and a_why==b_why)
            equal(a_events,b_events)
            equal(Game.snapshot(state),Game.snapshot(restored))
        end
    end)

    test("biome generation and chest offers never consume combat RNG",function()
        local baseline
        for index=1,#Biomes.order do
            local id=Biomes.order[index]
            local stage_index=index==1 and 1 or 2
            local history=stage_index==1 and {} or
                {(id:gsub("^core:biome/","core:route/"))}
            local state=Game.new_stage(29,{stage_options={index=stage_index,
                biome_id=id,route_history=history}})
            local rng_state=state.rng.state
            if baseline then assert(rng_state==baseline)
            else baseline=rng_state end
            assert(#state.generated_map.placements.chests==5)
        end
    end)

    test("new enemy deaths award ordinary XP and collectible currency",function()
        for _,id in ipairs({"core:enemy/spore_herald",
            "core:enemy/canal_lancer","core:enemy/cinder_bellkeeper"}) do
            local state=Game.new(24)
            local enemy=Game.spawn_enemy(state,id,3,2)
            Scheduler.remove_for_actor(state,enemy.id)
            enemy.hp=1
            local ok,why,out=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
            assert(ok,why)
            assert(count(out,"EntityKilled")==1
                and count(out,"ExperienceGained")==1
                and state.progression.level>=1 and #state.drops==1)
            ok,why=Game.submit(state,{kind="move",dx=1,dy=0})
            assert(ok,why)
            assert(state.currency==Content.get_enemy(id).currency_drop
                and state.drops[1].collected)
        end
        local state=Game.new(31)
        Game.set_boon_stacks(state,state.player_id,"core:boon/emberbrand",
            "legendary",3)
        local enemy=Game.spawn_enemy(state,"core:enemy/canal_lancer",3,2)
        Scheduler.remove_for_actor(state,enemy.id)
        local ok,why,out=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(ok,why)
        assert(count(out,"StatusApplied")>=1 and count(out,"StatusTick")>=1
            and count(out,"EntityKilled")==1 and state.progression.level==2)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("route graph selects unvisited biome content and preserves build",function()
        for first=1,2 do
            for second=1,2 do
                local state=Game.new_stage(19)
                local route1=assisted_exit(state,first)
                assert(state.stage.index==2 and state.stage.biome_id==route1.biome_id)
                local route2=assisted_exit(state,second)
                local other=first==1 and Biomes.order[3] or Biomes.order[2]
                assert(route2.biome_id==(second==1 and other or Biomes.order[4]))
                assert(state.stage.index==3 and state.stage.biome_id==route2.biome_id
                    and route2.biome_id~=route1.biome_id)
                assert(state.generated_map.biome_id==route2.biome_id
                    and #state.retired_stages==2)
                equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
                assisted_exit(state,1)
                assert(state.finale and state.finale.phase=="active"
                    and state.map_id=="core:map/final_sanctum")
            end
        end
    end)

    test("same route reproduces a concrete biome; another route changes it",function()
        local a,b,c=Game.new_stage(31),Game.new_stage(31),Game.new_stage(31)
        assisted_exit(a,1)
        assisted_exit(b,1)
        assisted_exit(c,2)
        equal(a.generated_map,b.generated_map)
        assert(a.stage.biome_id==b.stage.biome_id
            and a.run_seed==b.run_seed and a.run_seed~=c.run_seed
            and a.generated_map.id~=c.generated_map.id
            and a.active_floor_id~=c.active_floor_id)
    end)

    test("ordinary actions enter Fungal then Ashen and defeat signature foes",function()
        local run=Play.biome_route(3)
        local state=run.state
        assert(state.stage.biome_id==Biomes.order[4]
            and #state.stage.route_history==2
            and Play.has_event(run,"BoonGranted") and not state.game_over)
        local replay=Play.replay(run)
        equal(Game.snapshot(state),Game.snapshot(replay.state))
        local restored=Game.restore(Game.snapshot(state))
        local a_ok,a_why,a_events=Game.submit(state,{kind="wait"})
        local b_ok,b_why,b_events=Game.submit(restored,{kind="wait"})
        assert(a_ok==b_ok and a_why==b_why)
        equal(a_events,b_events)
        equal(Game.snapshot(state),Game.snapshot(restored))
        print(("BIOME LEGAL seed=3 actions=%d stage=3 biome=%s hp=%d")
            :format(#run.trace,state.stage.biome_id,Game.player(state).hp))
    end)

    test("ordinary route two reaches Aqueduct and defeats a Lancer",function()
        local run=Play.stage_one(3,2)
        assert(run.state.stage.biome_id==Biomes.order[3])
        local lancer=assert(Play.nearest(run.state,function(e)
            return e.definition_id=="core:enemy/canal_lancer" end))
        assert(Play.fight(run,lancer.id,160))
        assert(not run.state.game_over)
    end)
end
