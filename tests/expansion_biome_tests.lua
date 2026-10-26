local Biomes=require("src.biomes")
local Rooms=require("src.rooms")
local Generation=require("src.generation")
local Game=require("src.game")
local Content=require("src.content")
local Play=require("tests.legal_playthrough")

local function route_id(id)
    local value=id:gsub("^core:biome/","core:route/")
    return value
end
local function frozen_stage(seed,index,biome_id,history)
    return Game.new_stage(seed,{multifloor=true,root_seed=seed,
        stage_options={index=index,biome_id=biome_id,route_history=history}})
end

return function(test,equal)
    test("seven biome identities have separate strategies and authored template families",function()
        assert(Biomes.validate() and #Biomes.order==7)
        local strategies,prefixes,ids={},{},{}
        for _,id in ipairs(Biomes.order) do
            local b=assert(Biomes.get(id))
            assert(not strategies[b.strategy] and not prefixes[b.room_prefix]
                and not ids[b.map_id])
            strategies[b.strategy],prefixes[b.room_prefix],ids[b.map_id]=true,true,true
            for _,role in ipairs({"start","combat","treasure","landmark","elite"}) do
                local pool=Rooms.for_role(role,id)
                assert(#pool>0)
                for _,room in ipairs(pool) do
                    assert(room.id:sub(1,#b.room_prefix)==b.room_prefix)
                end
            end
        end
    end)

    test("new biome 100-seed connected-floor corpora retain full exploration budgets",function()
        local metrics={}
        for index=5,7 do
            local id=Biomes.order[index]
            local retries,repairs,fallbacks,loops,widened,parallel=0,0,0,0,0,0
            for seed=1,100 do
                local maps=Generation.generate_pair(seed,{biome_id=id})
                assert(Generation.validate_pair(maps))
                assert(#maps[1].rooms+#maps[2].rooms==12)
                assert(#maps[1].placements.chests+#maps[2].placements.chests==5)
                assert(#maps[1].placements.secrets+#maps[2].placements.secrets==2)
                for _,map in ipairs(maps) do
                    assert(map.biome_id==id and map.strategy==Biomes.get(id).strategy)
                    assert(Content.walkable(map,map.start_x,map.start_y))
                    retries=retries+#map.diagnostics.attempts-1
                    repairs=repairs+#map.repairs
                    if map.diagnostics.fallback then fallbacks=fallbacks+1 end
                    for _,edge in ipairs(map.graph.edges) do
                        if edge.kind=="loop" then loops=loops+1 end
                    end
                    for _,corridor in ipairs(map.corridors) do
                        if corridor.widened then widened=widened+#corridor.widened end
                        if corridor.parallel then parallel=parallel+1 end
                    end
                end
            end
            assert(fallbacks==0 and repairs==0)
            metrics[id]={loops=loops,widened=widened,parallel=parallel}
            print(("EXPANSION BIOME %s: 100 pairs, %d retries, %d repairs, %d fallbacks, %d loops, %d widened, %d parallel"):format(
                id,retries,repairs,fallbacks,loops,widened,parallel))
        end
        assert(metrics[Biomes.order[5]].widened>0
            and metrics[Biomes.order[5]].parallel==0)
        assert(metrics[Biomes.order[6]].parallel>0
            and metrics[Biomes.order[6]].widened==0)
        assert(metrics[Biomes.order[7]].widened>0
            and metrics[Biomes.order[7]].parallel==0)
    end)

    test("route choices expose every new biome and never revisit a visited identity",function()
        local first=Biomes.routes_for(1,{})
        assert(#first==3 and first[1].biome_id==Biomes.order[2]
            and first[2].biome_id==Biomes.order[3]
            and first[3].biome_id==Biomes.order[5])
        local reachable={[Biomes.order[1]]=true}
        for _,route in ipairs(first) do
            reachable[route.biome_id]=true
            local next_routes=Biomes.routes_for(2,{route.id})
            assert(#next_routes==3)
            local seen={[Biomes.order[1]]=true,[route.biome_id]=true}
            for _,second in ipairs(next_routes) do
                assert(not seen[second.biome_id])
                seen[second.biome_id]=true
                reachable[second.biome_id]=true
                assert(#Biomes.routes_for(3,{route.id,second.id})==1)
            end
            equal(next_routes,Biomes.routes_for(2,{route.id}))
        end
        for _,id in ipairs(Biomes.order) do assert(reachable[id],id.." unreachable") end
    end)

    test("new route destinations create the selected concrete biome and restore exactly",function()
        local scenarios={
            {2,Biomes.order[5],{route_id(Biomes.order[5])}},
            {3,Biomes.order[6],{route_id(Biomes.order[2]),route_id(Biomes.order[6])}},
            {3,Biomes.order[7],{route_id(Biomes.order[3]),route_id(Biomes.order[7])}},
        }
        for _,case in ipairs(scenarios) do
            local index,id,history=case[1],case[2],case[3]
            local state=frozen_stage(37,index,id,history)
            assert(state.stage.biome_id==id and state.generated_map.biome_id==id
                and state.floor_order[1]:find(id:match("[^/]+$"),1,true))
            local pool={};for _,enemy_id in ipairs(Biomes.get(id).enemy_ids) do
                pool[enemy_id]=true
            end
            local found=0
            for _,entity in ipairs(state.entities) do
                if entity.controller=="ai" and entity.id~=state.stage.elite_id then
                    if entity.squad_kind then
                        local squads=require('src.enemy_squads')
                        local allowed=false
                        for _,kind in ipairs(squads.biomes[id:match('[^/]+$')]) do
                            if kind==entity.squad_kind then allowed=true end
                        end
                        assert(allowed and squads.roles[entity.definition_id],
                            'squad outside its approved biome pool')
                    elseif require('src.idea_enemies').roles[entity.definition_id] then
                        assert(entity.ai_id=='idea_regular','invalid approved encounter controller')
                    else assert(pool[entity.definition_id]) end
                    found=found+1
                end
            end
            assert(found>0)
            equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
        end
    end)

    test("assisted route confirmations reach two distinct new biomes with zero-time handoffs",function()
        local state=Game.new_stage(3,{multifloor=true})
        state.meta.eligible=false -- prepared elite approaches; not a legal full run
        local player=Game.player(state)
        local function stand_near(x,y)
            for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
                local px,py=x-d[1],y-d[2]
                local free=Content.walkable(state.generated_map,px,py)
                for _,entity in ipairs(state.entities) do
                    local p=entity.position
                    if entity.id~=player.id and entity.blocks_movement
                        and p.floor_id==state.active_floor_id
                        and p.x==px and p.y==py then free=false end
                end
                if free then
                    player.position.x,player.position.y=px,py
                    return d[1],d[2]
                end
            end
            error("no prepared approach")
        end
        for stage=1,2 do
            local stair=state.generated_map.placements.stairs[1]
            local dx,dy=stand_near(stair.x,stair.y)
            assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
            assert(state.active_floor_index==2)
            local elite=assert(Game.entity(state,state.stage.elite_id))
            dx,dy=stand_near(elite.position.x,elite.position.y)
            player.damage=elite.hp+elite.defense+20
            assert(Game.submit(state,{kind="basic_attack",dx=dx,dy=dy}))
            local exit=assert(Game.entity(state,state.stage.exit_id))
            stand_near(exit.position.x,exit.position.y)
            local routes=assert(Game.route_options(state,exit.id))
            local selected=routes.routes[3]
            assert(selected and Game.submit(state,{kind="choose_route",
                exit_id=exit.id,route_index=3}))
            player=Game.player(state)
            assert(state.clock==0 and state.stage.index==stage+1
                and state.stage.biome_id==selected.biome_id)
        end
        assert(state.stage.biome_id==Biomes.order[7]
            and state.stage.route_history[1]==route_id(Biomes.order[5])
            and state.stage.route_history[2]==route_id(Biomes.order[7]))
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("ordinary legacy-stage actions select Observatory and defeat one encounter",function()
        -- The existing guide still uses the historical single-floor stage
        -- mode. Every mutation here is an ordinary Game.submit intent; the
        -- default connected-floor legal full-run driver remains M8 work.
        local run=Play.stage_one(3,3)
        local state=run.state
        assert(state.stage.index==2 and state.stage.biome_id==Biomes.order[5])
        local enemy=assert(Play.nearest(state,function(entity)
            return entity.controller=="ai" and entity.id~=state.stage.elite_id
                and entity.position.floor_id==state.active_floor_id
        end))
        local enemy_id=enemy.id
        local ok,reason=Play.fight(run,enemy_id,120)
        assert(ok,reason)
        assert(not Game.entity(state,enemy_id) and not state.game_over)
        print(("EXPANSION LEGAL seed=3 route=Observatory actions=%d hp=%d defeated=%s"):format(
            #run.trace,Game.player(state).hp,enemy.definition_id))
    end)

    test("an existing two-offer extraction save retains its cached choices",function()
        local state=Game.new_stage(3)
        local player=Game.player(state)
        local elite=assert(Game.entity(state,state.stage.elite_id))
        state.meta.eligible=false -- prepared boss approach
        local dx,dy
        for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
            local x,y=elite.position.x-d[1],elite.position.y-d[2]
            if Content.walkable(state.generated_map,x,y) then
                player.position.x,player.position.y=x,y;dx,dy=d[1],d[2];break
            end
        end
        assert(dx)
        player.damage=100
        assert(Game.submit(state,{kind="basic_attack",dx=dx,dy=dy}))
        local exit=assert(Game.entity(state,state.stage.exit_id))
        assert(#exit.routes==3)
        exit.routes[3]=nil -- represent a saved pre-M3 cached menu
        local restored=Game.restore(Game.snapshot(state))
        local old_exit=assert(Game.entity(restored,restored.stage.exit_id))
        assert(#old_exit.routes==2)
        local restored_player=Game.player(restored)
        for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
            local x,y=old_exit.position.x-d[1],old_exit.position.y-d[2]
            if Content.walkable(restored.generated_map,x,y) then
                restored_player.position.x,restored_player.position.y=x,y
                break
            end
        end
        local menu=assert(Game.route_options(restored,old_exit.id))
        assert(#menu.routes==2 and menu.routes[1].id==exit.routes[1].id
            and menu.routes[2].id==exit.routes[2].id)
    end)
end
