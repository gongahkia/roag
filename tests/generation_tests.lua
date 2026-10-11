-- Focused Tranche 05 checks, registered into the existing headless runner.
return function(test,equal)
    local AI=require("src.ai")
    local Content=require("src.content")
    local Game=require("src.game")
    local Generation=require("src.generation")
    local RNG=require("src.rng")
    local Rooms=require("src.rooms")
    local Scheduler=require("src.scheduler")

    local function copy(value)
        if type(value)~="table" then return value end
        local result={}
        for k,v in pairs(value) do result[k]=copy(v) end
        return result
    end
    local function events_of(events,kind)
        local result={}
        for _,event in ipairs(events) do
            if event.kind==kind then result[#result+1]=event end
        end
        return result
    end
    local directions={{0,-1},{1,0},{0,1},{-1,0}}
    local function key(x,y) return x..","..y end
    local function quiet_enemies(state)
        for _,entity in ipairs(state.entities) do
            if entity.controller=="ai" then Scheduler.remove_for_actor(state,entity.id) end
        end
    end
    local function path_to(state,predicate)
        local map=Content.map_for(state)
        local player=Game.player(state)
        local blocked={}
        for _,entity in ipairs(state.entities) do
            if entity.id~=player.id and entity.blocks_movement then
                blocked[key(entity.position.x,entity.position.y)]=true
            end
        end
        local origin=player.position
        local seen={[key(origin.x,origin.y)]=true}
        local queue={{x=origin.x,y=origin.y,path={}}}
        local head=1
        while head<=#queue do
            local current=queue[head]; head=head+1
            if predicate(current.x,current.y) then return current.path end
            for _,d in ipairs(directions) do
                local x,y=current.x+d[1],current.y+d[2]
                local k=key(x,y)
                if not seen[k] and Content.walkable(map,x,y) and not blocked[k] then
                    seen[k]=true
                    local path={}
                    for i,step in ipairs(current.path) do path[i]=step end
                    path[#path+1]={dx=d[1],dy=d[2]}
                    queue[#queue+1]={x=x,y=y,path=path}
                end
            end
        end
        return nil
    end
    local function walk_to(state,predicate)
        local path=assert(path_to(state,predicate),"generated route not reachable")
        for _,step in ipairs(path) do
            local ok,reason=Game.submit(state,{kind="move",dx=step.dx,dy=step.dy})
            assert(ok,reason)
        end
        return #path
    end

    test("room schema catches malformed sockets, masks and transforms",function()
        assert(Rooms.validate())
        local defs=copy(Rooms.definitions)
        defs["core:room/entry"].sockets[1].facing="E"
        assert(not pcall(Rooms.validate,defs,Rooms.order))
        defs=copy(Rooms.definitions)
        defs["core:room/entry"].editable[1]={x=5,y=4}
        assert(not pcall(Rooms.validate,defs,Rooms.order))
        defs=copy(Rooms.definitions)
        defs["core:room/entry"].rows[2]="#..x....#"
        assert(not pcall(Rooms.validate,defs,Rooms.order))
        local order=copy(Rooms.order); order[#order+1]=order[1]
        assert(not pcall(Rooms.validate,Rooms.definitions,order))
        local previous=Rooms.biome.enemy_ids[1]
        Rooms.biome.enemy_ids[1]="missing:enemy"
        local invalid=not pcall(Rooms.validate)
        Rooms.biome.enemy_ids[1]=previous
        assert(invalid)
        local entry=Rooms.definitions["core:room/entry"]
        assert(not pcall(Rooms.transform,entry,4,false))
        local rotated=Rooms.transform(Rooms.definitions["core:room/gallery"],1,true)
        assert(rotated.width==11 and rotated.height==15)
        local east
        for _,socket in ipairs(rotated.sockets) do
            if socket.facing=="E" then east=socket end
        end
        assert(east and east.x==11 and east.y==8)
        local source=Rooms.definitions["core:room/gallery"].markers.chest[1]
        local transformed=rotated.markers.chest[1]
        assert(source.x==12 and source.y==3 and transformed.x==9 and transformed.y==4)
        assert(#rotated.editable==#Rooms.definitions["core:room/gallery"].editable)
        assert(entry.rows[3]=="#..#....#") -- transforms never mutate authored data
    end)

    test("same seed reproduces concrete floor and another seed changes it",function()
        local a=Generation.generate(201)
        local b=Generation.generate(201)
        local c=Generation.generate(202)
        equal(a,b)
        assert(a.derived_seed==b.derived_seed)
        assert(a.derived_seed~=c.derived_seed)
        local same=true
        for y=1,a.height do
            if a.rows[y]~=c.rows[y] then same=false; break end
        end
        assert(not same)
        assert(#a.graph.critical>=6 and a.graph.critical[1]==1
            and a.graph.critical[#a.graph.critical]==12)
    end)

    test("100 generated seeds validate rooms, loops, routes, chests and variety",function()
        local started=os.clock()
        local fallback,retried=0,0
        local forms,lengths,templates,roles,depths={},{},{},{},{}
        local min_length,max_length=math.huge,0
        for seed=1,100 do
            local map=Generation.generate(seed)
            local valid,errors=Generation.validate(map)
            assert(valid,(errors[1] and errors[1].kind or "unknown").." seed "..seed)
            if map.diagnostics.fallback then fallback=fallback+1 end
            if #map.diagnostics.attempts>1 then retried=retried+1 end
            assert(#map.rooms>=8 and #map.rooms<=12)
            depths[#map.graph.critical]=true
            assert(#map.rooms-#map.graph.critical>=2)
            assert(#map.graph.edges>=#map.rooms) -- connected graph has a cycle
            assert(#map.placements.chests==5 and #map.placements.enemies>=6)
            assert(Content.walkable(map,map.start_x,map.start_y))
            assert(Content.walkable(map,map.elite.x,map.elite.y))
            local ids,positions={},{}
            for _,kind in ipairs({"chests","enemies"}) do
                for _,p in ipairs(map.placements[kind]) do
                    assert(not ids[p.id] and not positions[key(p.x,p.y)])
                    ids[p.id]=true; positions[key(p.x,p.y)]=true
                end
            end
            for _,room in ipairs(map.rooms) do
                templates[room.template_id]=true
                roles[room.role]=true
                assert(#room.markers.secret_anchor>=1)
            end
            for _,corridor in ipairs(map.corridors) do
                forms[corridor.form]=true
                lengths[corridor.length]=true
                min_length=math.min(min_length,corridor.length)
                max_length=math.max(max_length,corridor.length)
            end
        end
        local length_count=0
        for _ in pairs(lengths) do length_count=length_count+1 end
        for _,role in ipairs({"start","combat","treasure","landmark","elite"}) do
            assert(roles[role],"missing role "..role)
        end
        assert(forms.straight and forms.dogleg and length_count>=10)
        assert(depths[6] and depths[8])
        assert(min_length<=7 and max_length>=25)
        assert(templates["core:room/arena"] and templates["core:room/entry"]
            and templates["core:room/gallery"])
        print(("CORPUS 100 seeds: %d fallback, %d retried, lengths %d–%d, %.3fs CPU")
            :format(fallback,retried,min_length,max_length,os.clock()-started))
    end)

    test("editable corridor fault repairs exactly; protected fault retries then falls back",function()
        local repaired=Generation.generate(301,{fault="editable",retry_limit=1})
        local repeated=Generation.generate(301,{fault="editable",retry_limit=1})
        equal(repaired,repeated)
        assert(not repaired.diagnostics.fallback and #repaired.repairs==1)
        assert(repaired.repairs[1].kind=="restore_corridor"
            and repaired.repairs[1].x==23 and repaired.repairs[1].y==24
            and repaired.repairs[1].valid_after)
        assert(Generation.validate(repaired))
        local protected=Generation.generate(301,{fault="protected",retry_limit=1})
        local protected_again=Generation.generate(301,{fault="protected",retry_limit=1})
        equal(protected,protected_again)
        assert(protected.diagnostics.fallback and #protected.diagnostics.attempts==2)
        assert(#protected.diagnostics.attempts[1].repairs==0)
        for attempt=0,1 do
            assert(protected.diagnostics.attempts[attempt+1].derived_seed==
                RNG.derive(301,Generation.floor_id..":"..attempt))
        end
        assert(Generation.validate(protected))
        local damaged=Generation.generate(302)
        local socket=damaged.corridors[1].from_socket
        Generation.set_tile(damaged,socket.x,socket.y,"#","w")
        local valid,errors,repairs=Generation.repair(damaged)
        assert(not valid and #errors>0 and #repairs==0)
        assert(damaged.rows[socket.y]:sub(socket.x,socket.x)=="#")
    end)

    test("generated runtime has distinct IDs, safe spawn and exact stored map restore",function()
        local state=Game.new_generated(401)
        local map=state.generated_map
        assert(state.map_id==Generation.map_id and state.active_floor_id==Generation.floor_id)
        assert(#state.entities==1+#map.placements.enemies+#map.placements.chests
            +#map.placements.doors)
        local seen={}
        for _,e in ipairs(state.entities) do
            assert(not seen[e.id]); seen[e.id]=true
            assert(e.position.floor_id==map.floor_id)
        end
        local baseline=Game.snapshot(state)
        local restored=Game.restore(baseline)
        equal(baseline,Game.snapshot(restored))
        assert(restored.generated_map.rows==nil or
            restored.generated_map.rows[1]==map.rows[1])
        local cosmetic=Game.snapshot(state)
        local visual=cosmetic.generated_map.visual_rows[map.start_y]
        local changed=visual:sub(map.start_x,map.start_x)=="m" and "s" or "m"
        cosmetic.generated_map.visual_rows[map.start_y]=
            visual:sub(1,map.start_x-1)..changed..visual:sub(map.start_x+1)
        local loaded=Game.restore(cosmetic)
        assert(loaded.generated_map.visual_rows[map.start_y]:sub(map.start_x,map.start_x)==changed)
        assert(loaded.generated_map.visual_rows[map.start_y]~=
            Generation.generate(401).visual_rows[map.start_y])
        local altered=Game.snapshot(state)
        local p=altered.generated_map.corridors[1].from_socket
        Generation.set_tile(altered.generated_map,p.x,p.y,"#","w")
        assert(not pcall(Game.restore,altered))
    end)

    test("world and per-chest loot streams leave combat RNG untouched",function()
        local seed=402
        local authored=Game.new(seed)
        local generated=Game.new_generated(seed)
        local cosmetic=Game.new_generated(seed)
        assert(authored.rng.state==generated.rng.state)
        local before=generated.rng.state
        local map=generated.generated_map
        quiet_enemies(generated); quiet_enemies(cosmetic)
        local p=Game.player(generated).position
        local a=Game.spawn_enemy(generated,"core:enemy/mossbound_guard",p.x+1,p.y,1000)
        local b=Game.spawn_enemy(cosmetic,"core:enemy/mossbound_guard",p.x+1,p.y,1000)
        a.hp,b.hp=1,1
        Game.player(generated).crit_chance_ppm=500000
        Game.player(cosmetic).crit_chance_ppm=500000
        local extra
        for y=2,map.height-1 do
            for x=2,map.width-1 do
                if not extra and Content.walkable(map,x,y) then
                    local free=true
                    for _,e in ipairs(generated.entities) do
                        if e.blocks_movement and e.position.x==x and e.position.y==y then
                            free=false; break
                        end
                    end
                    if free and math.abs(x-p.x)+math.abs(y-p.y)>20 then
                        extra=Game.spawn_chest(generated,"core:chest/free_elite",x,y,
                            "test:extra-chest")
                    end
                end
            end
        end
        assert(extra and generated.rng.state==before)
        local row=cosmetic.generated_map.visual_rows[2]
        cosmetic.generated_map.visual_rows[2]=row:sub(1,2).."m"..row:sub(4)
        assert(cosmetic.rng.state==before)
        local _,_,combat_a=Game.submit(generated,{kind="basic_attack",dx=1,dy=0})
        local _,_,combat_b=Game.submit(cosmetic,{kind="basic_attack",dx=1,dy=0})
        equal(combat_a,combat_b)
        assert(generated.progression.level==cosmetic.progression.level
            and generated.rng.state==cosmetic.rng.state)
        local repeated=Game.new_generated(seed)
        for _,chest in ipairs(generated.entities) do
            if chest.controller=="chest" and chest.stable_marker_id~="test:extra-chest" then
                local match
                for _,other in ipairs(repeated.entities) do
                    if other.stable_marker_id==chest.stable_marker_id then match=other;break end
                end
                assert(match)
                equal(chest.offers,match.offers)
            end
        end
        assert(repeated.rng.state==before)
    end)

    test("guard pursues across room boundary; generated walls block firing",function()
        local state=Game.new_generated(1)
        quiet_enemies(state)
        local guard
        for _,e in ipairs(state.entities) do
            if e.controller=="ai" and e.definition_id=="core:enemy/mossbound_guard"
                and e.position.x>40 and e.position.y<30 then guard=e; break end
        end
        assert(guard)
        Scheduler.enqueue(state,{kind="actor_ready",due=0,actor_id=guard.id,is_player=false})
        local player=Game.player(state)
        player.hp,player.max_hp=1000,1000
        local crossed=false
        for _=1,70 do
            local _,_,events=Game.submit(state,{kind="wait"})
            for _,event in ipairs(events) do
                if event.kind=="Moved" and event.source_id==guard.id
                    and event.x<40 then crossed=true end
            end
            if crossed then break end
        end
        assert(crossed,"guard never crossed the room corridor")
        local map=state.generated_map
        local wall
        for y=3,map.height-2 do
            for x=3,map.width-2 do
                if not wall and map.rows[y]:sub(x,x)=="#"
                    and Content.walkable(map,x-1,y) and Content.walkable(map,x+1,y) then
                    wall={x=x,y=y}
                end
            end
        end
        assert(wall)
        local spitter={id=999,definition_id="core:enemy/thornspitter",
            position={floor_id=map.floor_id,x=wall.x+1,y=wall.y},blocks_movement=true}
        player.position.x,player.position.y=wall.x-1,wall.y
        assert(not Content.projectile_passable(map,wall.x,wall.y))
        assert(AI.choose(state,spitter).kind~="windup")
    end)

    test("real generated fight, pickup, paid chest and boon effect continue",function()
        local state=Game.new_generated(1)
        quiet_enemies(state) -- isolate this long traversal from changing AI decisions
        local guard,spitter,chest,offer_index
        for _,e in ipairs(state.entities) do
            if e.controller=="chest" and not chest then
                for index,offer in ipairs(e.offers) do
                    if offer.boon_id=="core:boon/temporal_echo" then
                        chest,offer_index=e,index;break
                    end
                end
            end
        end
        assert(chest)
        local start=Game.player(state).position
        local guard_distance,spitter_distance=math.huge,math.huge
        for _,e in ipairs(state.entities) do
            if e.controller=="ai" then
                local d_start=math.abs(e.position.x-start.x)+math.abs(e.position.y-start.y)
                local d_chest=math.abs(e.position.x-chest.position.x)
                    +math.abs(e.position.y-chest.position.y)
                if e.definition_id=="core:enemy/mossbound_guard" and d_start<guard_distance then
                    guard,guard_distance=e,d_start
                elseif e.definition_id=="core:enemy/thornspitter"
                    and d_chest<spitter_distance then
                    spitter,spitter_distance=e,d_chest
                end
            end
        end
        assert(guard and spitter and chest)
        local function defeat_and_collect(enemy)
            local x,y=enemy.position.x,enemy.position.y
            walk_to(state,function(px,py) return math.abs(px-x)+math.abs(py-y)==1 end)
            local attacks=0
            while Game.entity(state,enemy.id) do
                local p=Game.player(state).position
                local ok=Game.submit(state,{kind="basic_attack",dx=x-p.x,dy=y-p.y})
                assert(ok); attacks=attacks+1; assert(attacks<=10)
            end
            walk_to(state,function(px,py) return px==x and py==y end)
        end
        defeat_and_collect(guard)
        assert(state.currency==4 and state.progression.level>=2)
        defeat_and_collect(spitter)
        assert(state.currency==7)
        walk_to(state,function(x,y)
            return math.abs(x-chest.position.x)+math.abs(y-chest.position.y)==1
        end)
        local before=Game.snapshot(state)
        local player=Game.player(state).position
        local dx,dy=chest.position.x-player.x,chest.position.y-player.y
        local committed,reason=Game.submit(state,{kind="move",dx=dx,dy=dy})
        assert(not committed and reason=="chest")
        equal(before,Game.snapshot(state))
        local options=Game.chest_options(state,chest.id)
        assert(#options.offers==3
            and options.offers[offer_index].boon_id=="core:boon/temporal_echo")
        local _,_,claim=Game.submit(state,{kind="claim_chest",chest_id=chest.id,
            offer_index=offer_index})
        assert(state.currency==2 and chest.claimed and #events_of(claim,"BoonGranted")==1)
        assert(Game.submit(state,{kind="move",dx=dx,dy=dy}))
        assert(Game.player(state).position.x==chest.position.x
            and Game.player(state).position.y==chest.position.y)
        local _,_,attack=Game.submit(state,{kind="basic_attack",dx=0,dy=-1})
        assert(#events_of(attack,"DelayedEffectScheduled")>=1)
        assert(Game.player(state).boons["core:boon/temporal_echo"]
            [options.offers[offer_index].rarity]==1)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("generated snapshot preserves pending windup, echo, IDs and continuation",function()
        local state=Game.new_generated(403)
        quiet_enemies(state)
        local p=Game.player(state).position
        local guard=Game.spawn_enemy(state,"core:enemy/mossbound_guard",p.x+1,p.y)
        Game.set_boon_stacks(state,state.player_id,"core:boon/temporal_echo","common",1)
        Game.submit(state,{kind="basic_attack",dx=0,dy=-1})
        assert(state.clock==100 and guard.pending_attack)
        assert(#Game.pending_boon_effects(state)>=1)
        local restored=Game.restore(Game.snapshot(state))
        equal(Game.snapshot(state),Game.snapshot(restored))
        local _,_,a=Game.submit(state,{kind="wait"})
        local _,_,b=Game.submit(restored,{kind="wait"})
        equal(a,b)
        equal(Game.snapshot(state),Game.snapshot(restored))
    end)

    test("Crescent Reach and targeting consult generated collision tiles",function()
        local state=Game.new_generated(404)
        local p=Game.player(state).position
        Game.set_boon_stacks(state,state.player_id,"core:boon/crescent_reach",
            "common",1)
        local cells=Game.effective_ability(state,"sweeping_slash",0,-1)
        assert(#cells==4 and cells[4].x==p.x and cells[4].y==p.y-2)
        local map=state.generated_map
        assert(Content.walkable(map,p.x,p.y-1)
            and Content.walkable(map,p.x,p.y-2))
    end)
end
