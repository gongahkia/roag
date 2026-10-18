local Game=require("src.game")
local Content=require("src.content")
local Generation=require("src.generation")
local Terrain=require("src.terrain")
local StudioWorld=require("src.studio_world")
local AI=require("src.ai")
local Biomes=require("src.biomes")

local function copy(value)
    if type(value)~="table" then return value end
    local out={};for k,v in pairs(value) do out[k]=copy(v) end;return out
end
local function fixture(symbol,x,y)
    local state=Game.new(11)
    state.meta.eligible=false -- deliberately prepared geometry
    local map=copy(Content.get_map("core:map/test_room"))
    state.generated_map=map
    if symbol then
        map.rows[y]=map.rows[y]:sub(1,x-1)..symbol..map.rows[y]:sub(x+1)
    end
    return state,map
end
local function event_count(events,kind)
    local n=0;for _,event in ipairs(events) do
        if event.kind==kind then n=n+1 end
    end
    return n
end

return function(test,equal)
    test("six mechanical terrain symbols have separate collision and sight semantics",function()
        assert(Terrain.validate() and #Terrain.order==6)
        local expected={b={true,false,true},g={true,false,true},
            i={true,false,true},B={false,true,false},
            m={true,true,true},c={true,false,true}}
        for symbol,flags in pairs(expected) do
            local state,map=fixture(symbol,3,2)
            assert(Content.walkable(map,3,2)==flags[1])
            assert(Content.blocks_vision(map,3,2)==flags[2])
            assert(Content.projectile_passable(map,3,2,state)==flags[3])
        end
    end)

    test("paired generated floors place all six terrain mechanics without affecting combat RNG",function()
        for _,biome in ipairs(Biomes.order) do
            for seed=1,100 do
                local maps=Generation.generate_pair(seed,{biome_id=biome})
                assert(Generation.validate_pair(maps))
                for floor=1,2 do
                    assert(#maps[floor].terrain_placements==3)
                    for _,p in ipairs(maps[floor].terrain_placements) do
                        assert(Terrain.symbol(maps[floor],p.x,p.y)==p.symbol)
                    end
                end
            end
        end
        local a=Game.new_stage(3,{multifloor=true})
        local b=Game.new_stage(3,{multifloor=true})
        equal(a.rng,b.rng)
    end)

    test("bramble entry deals one canonical damage once per action and idle is safe",function()
        local state=fixture("b",3,2)
        local player=Game.player(state)
        local ok,_,events=Game.submit(state,{kind="move",dx=1,dy=0})
        assert(ok and player.hp==23 and event_count(events,"TerrainTriggered")==1)
        assert(event_count(events,"DamageTaken")==1)
        local before=player.hp
        assert(Game.submit(state,{kind="wait"}))
        assert(player.hp==before)
    end)

    test("bog increases player recovery while ice extends only legal Dash traversal",function()
        local bog=fixture("g",3,2)
        local preview=Game.preview(bog,"move",1,0)
        assert(preview.valid and preview.cost==140)
        assert(Game.submit(bog,{kind="move",dx=1,dy=0}))
        assert(bog.clock==140)
        local ice=fixture("i",3,2)
        local dash=Game.preview(ice,"dash",1,0)
        assert(dash.valid and dash.distance==3 and dash.cost==130)
        assert(Game.submit(ice,{kind="dash",dx=1,dy=0}))
        assert(Game.player(ice).position.x==5 and ice.clock==130)
        local normal=fixture("i",3,2)
        assert(Game.preview(normal,"move",1,0).distance==1)
        local partial,map=fixture("i",3,2)
        map.rows[2]=map.rows[2]:sub(1,3).."#"..map.rows[2]:sub(5)
        assert(Game.preview(partial,"dash",1,0).distance==1)
    end)

    test("enemy bramble damage resolves at its move time and bog delays its next readiness",function()
        local hazard=fixture("b",3,2)
        local guard=Game.spawn_enemy(hazard,"core:enemy/mossbound_guard",4,2,0)
        guard.defense=0
        local ok,_,events=Game.submit(hazard,{kind="wait"})
        assert(ok and guard.position.x==3 and guard.hp==9)
        local hit_time
        for _,event in ipairs(events) do
            if event.kind=="DamageTaken" and event.damage_type=="bramble"
                and event.target_id==guard.id then hit_time=event.time end
        end
        assert(hit_time==0 and hazard.clock==100)
        local bog=fixture("g",3,2)
        local slowed=Game.spawn_enemy(bog,"core:enemy/mossbound_guard",4,2,0)
        assert(Game.submit(bog,{kind="wait"}))
        assert(slowed.position.x==3)
        local due
        for _,item in ipairs(bog.schedule) do
            if item.kind=="actor_ready" and item.actor_id==slowed.id then due=item.due end
        end
        assert(due==Game.effective_cost(140,slowed.speed))
    end)

    test("attacking a barricade clears authoritative collision and emits one break",function()
        local state,map=fixture("B",3,2)
        assert(not Game.preview(state,"move",1,0).valid)
        local ok,_,events=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(ok and Terrain.symbol(map,3,2)==".")
        assert(event_count(events,"TerrainBroken")==1)
        assert(Content.walkable(map,3,2) and not Content.blocks_vision(map,3,2))
        assert(Game.preview(state,"move",1,0).valid)
    end)

    test("generated barricade break survives a concrete two-floor snapshot",function()
        local state=Game.new_stage(3,{multifloor=true})
        local stair=state.generated_map.placements.stairs[1]
        local player=Game.player(state)
        local dx,dy
        for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
            local x,y=stair.x-d[1],stair.y-d[2]
            local free=Content.walkable(state.generated_map,x,y)
            for _,e in ipairs(state.entities) do
                if e.id~=player.id and e.blocks_movement and
                    e.position.floor_id==state.active_floor_id and
                    e.position.x==x and e.position.y==y then free=false end
            end
            if free then player.position.x,player.position.y=x,y;dx,dy=d[1],d[2];break end
        end
        assert(dx)
        state.meta.eligible=false
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        assert(state.active_floor_index==2)
        local barrier
        for _,p in ipairs(state.generated_map.terrain_placements) do
            if p.symbol=="B" then barrier=p end
        end
        assert(barrier)
        local ax,ay
        for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
            local x,y=barrier.x-d[1],barrier.y-d[2]
            if Content.walkable(state.generated_map,x,y) then
                ax,ay=x,y;dx,dy=d[1],d[2];break
            end
        end
        assert(ax)
        player.position.x,player.position.y=ax,ay
        local ok,_,events=Game.submit(state,{kind="basic_attack",dx=dx,dy=dy})
        assert(ok and event_count(events,"TerrainBroken")==1)
        assert(Terrain.symbol(state.generated_map,barrier.x,barrier.y)==".")
        local restored=Game.restore(Game.snapshot(state))
        equal(Game.snapshot(state),Game.snapshot(restored))
        assert(Generation.validate(restored.generated_map,restored.secrets))
    end)

    test("pre-terrain connected-floor snapshots remain loadable as stored maps",function()
        -- Build this compatibility fixture from the earlier six room families.
        -- Current expanded rooms author mechanical tiles in protected cells,
        -- so stripping those tiles from a current map would corrupt it.
        local Rooms=require('src.rooms')
        local order=Rooms.order
        Rooms.order={}
        for index=1,6 do Rooms.order[index]=order[index] end
        local ok,state=pcall(Game.new_stage,29,{multifloor=true})
        Rooms.order=order
        assert(ok,state)
        local snapshot=Game.snapshot(state)
        snapshot.room_content_version='roag-rooms/2'
        local maps={snapshot.generated_map}
        for _,map in pairs(snapshot.inactive_floor_maps) do maps[#maps+1]=map end
        for _,map in ipairs(maps) do
            map.terrain_placements=nil
            map.breakable_origins=nil
            map.broken_barricades=nil
            for y,row in ipairs(map.rows) do
                map.rows[y]=row:gsub("[bgiBmc]",".")
            end
        end
        local restored=Game.restore(snapshot)
        equal(snapshot,Game.snapshot(restored))
    end)

    test("mist stops ranged AI sight while remaining projectile-passable",function()
        local state,map=fixture("m",2,4)
        local enemy=Game.spawn_enemy(state,"core:enemy/thornspitter",2,5,100000)
        local intent=AI.choose(state,enemy)
        assert(intent.kind~="windup")
        assert(Content.projectile_passable(map,2,4,state))
    end)

    test("resonant crystal arcs credited lightning through canonical damage once",function()
        local state,map=fixture("c",4,3)
        local first=Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2,100000)
        local second=Game.spawn_enemy(state,"core:enemy/ruin_skitter",4,2,100000)
        local third=Game.spawn_enemy(state,"core:enemy/ruin_skitter",4,4,100000)
        Game.set_boon_stacks(state,state.player_id,"core:boon/storm_conductor",
            "legendary",2)
        local ok,_,events=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(ok and first.hp<first.max_hp)
        assert(Game.entity(state,second.id)==nil)
        assert(third.hp==third.max_hp-1)
        local arcs=0
        for _,event in ipairs(events) do
            if event.kind=="DamageTaken" and event.damage_type=="crystal_arc" then
                arcs=arcs+1
                assert(event.source_id==state.player_id and event.target_id==third.id)
            end
        end
        assert(arcs==1 and event_count(events,"TerrainTriggered")==1)
    end)

    test("crystal secondary lethal awards one ordinary kill and drop",function()
        local state=fixture("c",4,3)
        Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2,100000)
        Game.spawn_enemy(state,"core:enemy/ruin_skitter",4,2,100000)
        local victim=Game.spawn_enemy(state,"core:enemy/ruin_skitter",4,4,100000)
        victim.hp=1
        Game.set_boon_stacks(state,state.player_id,"core:boon/storm_conductor",
            "legendary",2)
        local ok,_,events=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(ok and Game.entity(state,victim.id)==nil)
        local kills,xp,drops=0,0,0
        for _,event in ipairs(events) do
            if event.target_id==victim.id and event.kind=="EntityKilled" then
                kills=kills+1
                assert(event.source_id==state.player_id and
                    event.damage_type=="crystal_arc")
            elseif event.target_id==victim.id and
                event.kind=="ExperienceGained" then xp=xp+1
            elseif event.target_id==victim.id and
                event.kind=="CurrencyDropped" then drops=drops+1 end
        end
        assert(kills==1 and xp==1 and drops==1)
    end)

    test("Studio opts into six terrain tiles without altering older three-tile projects",function()
        local pack=StudioWorld.new("terrain-test")
        assert(#pack.tiles==3)
        StudioWorld.add_terrain_tiles(pack)
        StudioWorld.add_terrain_tiles(pack)
        assert(#pack.tiles==9)
        local room=StudioWorld.new_room("studio:room/terrain","Terrain",9,7)
        pack.rooms[1]=room
        StudioWorld.paint_tile(pack,room,3,3,"studio:tile/bramble_bed")
        assert(room.rows[3]:sub(3,3)=="b")
        assert(StudioWorld.validate(pack))
    end)
end
