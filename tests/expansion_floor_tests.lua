local Game=require("src.game")
local Generation=require("src.generation")
local Biomes=require("src.biomes")
local Content=require("src.content")
local Visibility=require("src.visibility")
local StudioWorld=require("src.studio_world")
local Play=require("tests.legal_playthrough")
local Scheduler=require("src.scheduler")

local function approaching_stair(state)
    local stair=state.generated_map.placements.stairs[1]
    local player=Game.player(state)
    for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
        local x,y=stair.x-d[1],stair.y-d[2]
        local occupied=false
        for _,entity in ipairs(state.entities) do
            if entity.blocks_movement and entity.position.floor_id==state.active_floor_id
                and entity.position.x==x and entity.position.y==y then
                occupied=true;break
            end
        end
        if Content.walkable(state.generated_map,x,y) and not occupied then
            -- Assisted setup, never used as ordinary legal-action evidence.
            state.meta.eligible=false
            player.position.x,player.position.y=x,y
            Visibility.recompute(state)
            return d[1],d[2]
        end
    end
    error("stair has no free approach")
end

local function stand_near(state,entity)
    local player=Game.player(state)
    for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
        local x,y=entity.position.x-d[1],entity.position.y-d[2]
        local free=Content.walkable(state.generated_map,x,y)
        for _,other in ipairs(state.entities) do
            if other.id~=player.id and other.blocks_movement
                and other.position.floor_id==state.active_floor_id
                and other.position.x==x and other.position.y==y then free=false end
        end
        if free then
            state.meta.eligible=false
            player.position.x,player.position.y=x,y
            Visibility.recompute(state)
            return d[1],d[2]
        end
    end
    error("no free entity approach")
end

return function(test,equal)
    test("connected biome pairs use twelve rooms, five chests and reciprocal stairs across 100 seeds",function()
        for _,biome_id in ipairs(Biomes.order) do
            for seed=1,100 do
                local maps=Generation.generate_pair(seed,{biome_id=biome_id})
                assert(Generation.validate_pair(maps))
                assert(#maps[1].rooms==6 and #maps[2].rooms==6)
                assert(#maps[1].placements.chests+#maps[2].placements.chests==5)
                assert(#maps[1].placements.secrets+#maps[2].placements.secrets==2)
                assert(maps[1].floor_id~=maps[2].floor_id)
                local down,up=maps[1].placements.stairs[1],maps[2].placements.stairs[1]
                assert(down.to_floor_id==maps[2].floor_id and up.to_floor_id==maps[1].floor_id)
                assert(Content.walkable(maps[1],down.x,down.y)
                    and Content.walkable(maps[2],up.x,up.y))
            end
        end
    end)

    test("connected generated stage has one active floor and a frozen lower-floor Warden",function()
        local state=Game.new_stage(3,{multifloor=true})
        assert(state.active_floor_index==1 and state.floor_order[1]==state.active_floor_id)
        assert(state.stage.elite_floor_id==state.floor_order[2])
        assert(Game.entity(state,state.stage.elite_id).position.floor_id==state.floor_order[2])
        assert(#state.floor_pause[state.floor_order[2]].schedule>0)
        assert(#state.secrets==2)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("ordinary stair move costs one recovery and keeps old-floor work frozen",function()
        local state=Game.new_stage(3,{multifloor=true})
        local dx,dy=approaching_stair(state)
        local old_id=state.active_floor_id
        local old_enemy
        for _,entity in ipairs(state.entities) do
            if entity.controller=="ai" and entity.position.floor_id==old_id then
                old_enemy=entity;break
            end
        end
        local original={x=old_enemy.position.x,y=old_enemy.position.y,hp=old_enemy.hp}
        local committed,reason,events=Game.submit(state,{kind="move",dx=dx,dy=dy})
        assert(committed and not reason and state.active_floor_index==2)
        assert(state.clock==100 and state.last_action.cost==100)
        local changes=0
        for _,event in ipairs(events) do
            if event.kind=="FloorChanged" then changes=changes+1 end
        end
        assert(changes==1 and state.floor_pause[old_id])
        local parked=Game.snapshot(state).floor_pause[old_id]
        for _=1,3 do assert(Game.submit(state,{kind="wait"})) end
        assert(state.clock>=400 and old_enemy.position.x==original.x
            and old_enemy.position.y==original.y and old_enemy.hp==original.hp)
        equal(parked,state.floor_pause[old_id])
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("connected-floor generation and stage creation preserve seed determinism",function()
        local a=Game.new_stage(29,{multifloor=true})
        local b=Game.new_stage(29,{multifloor=true})
        equal(Game.snapshot(a),Game.snapshot(b))
        local pair=Generation.generate_pair(29,{biome_id=Biomes.order[1]})
        equal(pair,Generation.generate_pair(29,{biome_id=Biomes.order[1]}))
    end)

    test("E03 authored stair marker transforms into a real reciprocal generated link",function()
        local pack=StudioWorld.new("stair_fixture")
        local room=StudioWorld.new_room("studio:room/stair_hall","Stair hall",9,7)
        room.roles={"elite"};room.weight=20;room.markers.enemy={}
        room.markers.elite={{x=5,y=4}}
        room.markers.stair=nil -- old E03 documents omitted this optional kind
        assert(StudioWorld.add_marker(room,"stair",3,3))
        pack.rooms[1]=room
        assert(StudioWorld.validate(pack))
        local restored=StudioWorld.decode(StudioWorld.encode(pack))
        assert(#restored.rooms[1].markers.stair==1)
        local found=false
        for seed=1,60 do
            local maps=Generation.generate_pair(seed,{studio_pack=restored})
            local upper=maps[1]
            if upper.rooms[6].template_id==room.id then
                found=true
                local marker=upper.rooms[6].markers.stair[1]
                local down=upper.placements.stairs[1]
                assert(marker.x==down.x and marker.y==down.y)
                assert(maps[2].placements.stairs[1].to_x==marker.x)
                assert(Generation.validate_pair(maps))
                break
            end
        end
        assert(found,"authored stair template never entered generated pool")
    end)

    test("normal legal movement reaches generated second floor without fixture mutation",function()
        local run=Play.new(3,{multifloor=true})
        local state=run.state
        local stair=state.generated_map.placements.stairs[1]
        for _=1,150 do
            if state.active_floor_index==2 then break end
            assert(not state.game_over,"player died before legal stair entry")
            local step=Play.path(state,function(p)
                return p.x==stair.x and p.y==stair.y
            end)
            assert(step,"no legal route to stair")
            local ok,reason=Play.action(run,{kind="move",dx=step[1],dy=step[2]})
            assert(ok,reason)
        end
        assert(state.active_floor_index==2 and #run.trace<150)
        assert(state.clock>0 and Game.player(state).hp>0)
        assert(Generation.validate_pair({state.inactive_floor_maps[state.floor_order[1]],
            state.generated_map},state.secrets))
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("returning upstairs rebases only floor-local work while stage pressure stays global",function()
        local state=Game.new_stage(3,{multifloor=true})
        local upper=state.active_floor_id
        local pending=Scheduler.enqueue(state,{kind="effect",due=1200,
            effect_id="test:floor_local"})
        local pressure
        for _,item in ipairs(state.schedule) do
            if item.kind=="stage_pressure" then pressure=item;break end
        end
        assert(pressure and pressure.floor_id==nil)
        local dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        assert(state.floor_pause[upper])
        for _=1,3 do assert(Game.submit(state,{kind="wait"})) end
        assert(state.clock==400 and state.floor_pause[upper].suspended_at==0)
        assert(pressure.due==state.stage.next_pressure)
        dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        assert(state.active_floor_id==upper and state.clock==500)
        local found
        for _,item in ipairs(state.schedule) do
            if item.id==pending.id then found=item;break end
        end
        assert(found and found.floor_id==upper and found.due==1600)
        assert(pressure.due==state.stage.next_pressure)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("lower-floor Warden extraction hands off to a fresh paired biome without leaking old work",function()
        local state=Game.new_stage(3,{multifloor=true})
        local dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        local retired_ids={state.floor_order[1],state.floor_order[2]}
        local elite=assert(Game.entity(state,state.stage.elite_id))
        dx,dy=stand_near(state,elite)
        Game.player(state).damage=elite.hp+elite.defense+20 -- assisted fixture
        assert(Game.submit(state,{kind="basic_attack",dx=dx,dy=dy}))
        assert(state.stage.phase=="extraction")
        local exit=assert(Game.entity(state,state.stage.exit_id))
        stand_near(state,exit)
        local options=assert(Game.route_options(state,exit.id))
        local chosen=options.routes[1]
        assert(Game.submit(state,{kind="choose_route",exit_id=exit.id,route_index=1}))
        assert(state.clock==0 and state.stage.index==2
            and state.stage.biome_id==chosen.biome_id
            and state.floor_order and #state.retired_stages==1)
        assert(state.floor_order[1]~=retired_ids[1]
            and state.floor_order[2]~=retired_ids[2])
        for _,item in ipairs(state.schedule) do
            assert(not item.floor_id or item.floor_id==state.active_floor_id)
        end
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("a real Temporal Echo stays pending on its source floor through stairs and save",function()
        local state=Game.new_stage(3,{multifloor=true})
        Game.set_boon_stacks(state,state.player_id,
            "core:boon/temporal_echo","common",1)
        local dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="basic_attack",dx=-dx,dy=-dy}))
        local echo=Game.pending_boon_effects(state)[1]
        assert(echo and echo.boon_id=="core:boon/temporal_echo")
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        local old_id=state.floor_order[1]
        assert(state.active_floor_index==2 and state.delayed_effects[echo.id])
        local parked
        for _,item in ipairs(state.floor_pause[old_id].schedule) do
            if item.id==echo.id then parked=item;break end
        end
        assert(parked and parked.floor_id==old_id and parked.due==echo.due)
        local saved=Game.restore(Game.snapshot(state))
        local _,_,a=Game.submit(state,{kind="wait"})
        local _,_,b=Game.submit(saved,{kind="wait"})
        equal(a,b)
        assert(state.delayed_effects[echo.id] and saved.delayed_effects[echo.id])
        equal(Game.snapshot(state),Game.snapshot(saved))
    end)

    test("locked upper-floor windup keeps its cells and remaining wait after a return",function()
        local state=Game.new_stage(3,{multifloor=true})
        local dx,dy=approaching_stair(state)
        local guard
        for _,entity in ipairs(state.entities) do
            if entity.controller=="ai" and
                entity.position.floor_id==state.active_floor_id then
                guard=entity;break
            end
        end
        assert(guard)
        Scheduler.remove_for_actor(state,guard.id)
        local locked={floor_id=state.active_floor_id,
            x=Game.player(state).position.x,y=Game.player(state).position.y}
        local impact=Scheduler.enqueue(state,{kind="impact",due=1000,
            actor_id=guard.id,attack_id=999})
        guard.pending_attack={schedule_id=impact.id,sequence=impact.seq,
            action_id=999,source_id=guard.id,floor_id=state.active_floor_id,
            source_x=guard.position.x,source_y=guard.position.y,
            ability_id="core:ability/guard_strike",target_cells={locked},
            ranged=false,damage=guard.damage,due=1000,committed_time=0}
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        for _=1,2 do assert(Game.submit(state,{kind="wait"})) end
        assert(state.clock==300 and guard.pending_attack.due==1000)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
        dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        assert(state.active_floor_index==1 and state.clock==400)
        assert(guard.pending_attack.due==1300
            and guard.pending_attack.target_cells[1].floor_id==locked.floor_id
            and guard.pending_attack.target_cells[1].x==locked.x
            and guard.pending_attack.target_cells[1].y==locked.y)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("extraction pursuer and deadline survive stair ping-pong without duplication",function()
        local state=Game.new_stage(3,{multifloor=true})
        local dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        local elite=assert(Game.entity(state,state.stage.elite_id))
        dx,dy=stand_near(state,elite)
        local player=Game.player(state)
        player.max_hp,player.hp=999,999 -- assisted timing fixture
        player.damage=elite.hp+elite.defense+20
        assert(Game.submit(state,{kind="basic_attack",dx=dx,dy=dy}))
        local deadline=assert(state.stage.deadline)
        for _=1,40 do
            if state.stage.pursuer_id then break end
            assert(Game.submit(state,{kind="wait"}))
        end
        local pursuer_id=assert(state.stage.pursuer_id)
        local pursuer=assert(Game.entity(state,pursuer_id))
        assert(state.clock>=deadline and pursuer.position.floor_id==state.active_floor_id)
        dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        assert(state.active_floor_index==1 and state.stage.deadline==deadline
            and state.stage.pursuer_id==pursuer_id
            and pursuer.position.floor_id==state.active_floor_id)
        local count=0
        for _,entity in ipairs(state.entities) do
            if entity.invulnerable then count=count+1 end
        end
        assert(count==1)
        dx,dy=approaching_stair(state)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        assert(state.active_floor_index==2 and state.stage.deadline==deadline
            and state.stage.pursuer_id==pursuer_id
            and pursuer.position.floor_id==state.active_floor_id)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("occupied stair landing chooses a reachable neighbor or rejects at zero cost",function()
        local state=Game.new_stage(3,{multifloor=true})
        local dx,dy=approaching_stair(state)
        local portal=state.generated_map.placements.stairs[1]
        local lower=state.inactive_floor_maps[portal.to_floor_id]
        local function blocker(x,y)
            local id=state.next_entity_id
            state.next_entity_id=id+1
            state.entities[#state.entities+1]={id=id,controller="fixture",
                faction="neutral",blocks_movement=true,
                position={floor_id=lower.floor_id,x=x,y=y}}
        end
        blocker(portal.to_x,portal.to_y)
        local preview=Game.preview(state,"move",dx,dy)
        assert(preview.valid and preview.landing
            and (preview.landing.x~=portal.to_x or preview.landing.y~=portal.to_y))
        for oy=-3,3 do for ox=-3,3 do
            if math.abs(ox)+math.abs(oy)<=3 and (ox~=0 or oy~=0)
                and Content.walkable(lower,portal.to_x+ox,portal.to_y+oy) then
                blocker(portal.to_x+ox,portal.to_y+oy)
            end
        end end
        local before=Game.snapshot(state)
        local ok,reason=Game.submit(state,{kind="move",dx=dx,dy=dy})
        assert(not ok and reason=="stair landing blocked" and state.clock==0)
        equal(before,Game.snapshot(state))
    end)
end
