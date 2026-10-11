return function(test,equal)
    local Content=require("src.content")
    local Game=require("src.game")
    local Generation=require("src.generation")
    local RNG=require("src.rng")
    local Rooms=require("src.rooms")
    local Scheduler=require("src.scheduler")
    local Visibility=require("src.visibility")
    local World=require("src.world")

    local function events(events,kind)
        local found={}
        for _,event in ipairs(events) do
            if event.kind==kind then found[#found+1]=event end
        end
        return found
    end
    local function quiet(state)
        for _,entity in ipairs(state.entities) do
            if entity.controller=="ai" then Scheduler.remove_for_actor(state,entity.id) end
        end
    end
    local dirs={{0,-1},{1,0},{0,1},{-1,0}}
    local function key(x,y) return x..","..y end
    local function walk_to(state,x_goal,y_goal)
        local map=Content.map_for(state)
        local player=Game.player(state)
        local blocked={}
        for _,entity in ipairs(state.entities) do
            if entity.id~=player.id and entity.blocks_movement
                and entity.controller~="door" then
                blocked[key(entity.position.x,entity.position.y)]=true
            end
        end
        local start=player.position
        local queue={{x=start.x,y=start.y,path={}}}
        local seen={[key(start.x,start.y)]=true}
        local head=1
        local path
        while head<=#queue do
            local current=queue[head]; head=head+1
            if current.x==x_goal and current.y==y_goal then path=current.path; break end
            for _,direction in ipairs(dirs) do
                local x,y=current.x+direction[1],current.y+direction[2]
                local k=key(x,y)
                if not seen[k] and not blocked[k] and Content.walkable(map,x,y) then
                    seen[k]=true
                    local next_path={}
                    for index,step in ipairs(current.path) do next_path[index]=step end
                    next_path[#next_path+1]=direction
                    queue[#queue+1]={x=x,y=y,path=next_path}
                end
            end
        end
        assert(path,"required generated exploration route is unavailable")
        for _,direction in ipairs(path) do
            local before_x,before_y=player.position.x,player.position.y
            local ok,reason=Game.submit(state,{kind="move",dx=direction[1],dy=direction[2]})
            assert(ok,reason)
            if player.position.x==before_x and player.position.y==before_y then
                assert(state.last_action.kind=="open_door")
                assert(Game.submit(state,{kind="move",dx=direction[1],dy=direction[2]}))
            end
        end
        assert(player.position.x==x_goal and player.position.y==y_goal)
        return #path
    end

    test("initial FOV sees an opaque wall but not beyond it or sealed diagonal corners",function()
        local state=Game.new(1)
        local player=Game.player(state)
        player.position.x,player.position.y=4,2
        Visibility.recompute(state)
        local floor=state.active_floor_id
        assert(Game.is_visible(state,floor,4,3))
        assert(not Game.is_visible(state,floor,4,4))
        local before=Game.snapshot(state)
        for _=1,100 do
            Game.is_visible(state,floor,4,3)
            Game.was_explored(state,floor,4,3)
        end
        equal(before,Game.snapshot(state))
        player.position.x,player.position.y=4,4
        Visibility.recompute(state,1)
        assert(Game.is_visible(state,floor,4,3))
        assert(not Game.is_visible(state,floor,4,2))
        local corner=Game.new(1)
        corner.generated_map={id="test:corner",floor_id=corner.active_floor_id,
            width=5,height=5,start_x=2,start_y=2,
            rows={"#####","#.#.#","##..#","#...#","#####"}}
        Visibility.recompute(corner,3)
        assert(not Game.is_visible(corner,floor,3,3))
        assert(Game.is_visible(corner,floor,3,2)
            and Game.is_visible(corner,floor,2,3))
    end)

    test("exploration memory survives movement and restore without world-time draws",function()
        local state=Game.new_generated(1)
        quiet(state)
        local floor=state.active_floor_id
        local player=Game.player(state)
        local origin_x,origin_y=player.position.x,player.position.y
        assert(Game.is_visible(state,floor,origin_x,origin_y))
        assert(not Game.was_explored(state,floor,state.generated_map.elite.x,
            state.generated_map.elite.y))
        local old_rng,old_clock=state.rng.state,state.clock
        Visibility.recompute(state)
        assert(state.rng.state==old_rng and state.clock==old_clock)
        local far=state.generated_map.elite
        player.position.x,player.position.y=far.x,far.y
        Visibility.recompute(state)
        assert(Game.was_explored(state,floor,origin_x,origin_y)
            and not Game.is_visible(state,floor,origin_x,origin_y))
        assert(Game.is_visible(state,floor,far.x,far.y))
        local snap=Game.snapshot(state)
        equal(snap,Game.snapshot(Game.restore(snap)))
    end)

    test("100 generated floors have two optional reachable transformed secrets and door",function()
        local retries,fallbacks=0,0
        local locations={}
        for seed=1,100 do
            local map=Generation.generate(seed)
            local valid,issues=Generation.validate(map)
            assert(valid,(issues[1] and issues[1].kind or "unknown").." seed "..seed)
            if #map.diagnostics.attempts>1 then retries=retries+1 end
            if map.diagnostics.fallback then fallbacks=fallbacks+1 end
            assert(#map.placements.secrets==2 and #map.placements.doors==1)
            local critical={}
            for _,id in ipairs(map.graph.critical) do critical[id]=true end
            for _,secret in ipairs(map.placements.secrets) do
                assert(not critical[secret.room_id])
                assert(Content.walkable(map,secret.x,secret.y))
                locations[key(secret.x,secret.y)]=true
                if secret.wall_x then
                    assert(map.rows[secret.wall_y]:sub(secret.wall_x,secret.wall_x)=="#")
                    assert(map.visual_rows[secret.wall_y]:sub(secret.wall_x,secret.wall_x)=="k")
                else
                    assert(map.visual_rows[secret.y]:sub(secret.x,secret.x)=="p")
                end
            end
        end
        local varied=0
        for _ in pairs(locations) do varied=varied+1 end
        assert(varied>=12)
        print(("SECRETS 100 seeds: %d retries, %d fallbacks, %d clue positions")
            :format(retries,fallbacks,varied))
    end)

    test("secret clue repair is local; protected fault still retries and falls back",function()
        local repaired=Generation.generate(301,{fault="secret_clue",retry_limit=1})
        local again=Generation.generate(301,{fault="secret_clue",retry_limit=1})
        equal(repaired,again)
        assert(#repaired.repairs==1 and repaired.repairs[1].kind=="restore_secret_clue"
            and repaired.repairs[1].valid_after and Generation.validate(repaired))
        local fallback=Generation.generate(301,{fault="protected",retry_limit=1})
        assert(fallback.diagnostics.fallback and Generation.validate(fallback)
            and #fallback.placements.secrets==2)
        local source=Rooms.definitions["core:room/gallery"]
        local transformed=Rooms.transform(source,1,true)
        local a,b=transformed.markers.secret_anchor[1],transformed.markers.secret_wall[1]
        assert(math.abs(a.x-b.x)+math.abs(a.y-b.y)==1)
        assert(transformed.rows[b.y]:sub(b.x,b.x)=="#")
    end)

    test("wall secret needs explicit empty Sword and opens one cached reward alcove",function()
        local state=Game.new_generated(1)
        quiet(state)
        local secret=state.secrets[1]
        local wall_x,wall_y=secret.wall_x,secret.wall_y
        local before_rng=state.rng.state
        walk_to(state,secret.x,secret.y)
        local clock=state.clock
        local before_entities=#state.entities
        assert(not Game.is_visible(state,state.active_floor_id,
            secret.cache_x,secret.cache_y))
        local ok,reason,output=Game.submit(state,{kind="basic_attack",
            dx=wall_x-secret.x,dy=wall_y-secret.y})
        assert(ok,reason)
        assert(state.last_action.cost==100 and state.clock==clock+100)
        assert(#events(output,"AttackPerformed")==1 and #events(output,"HitConfirmed")==0)
        assert(#events(output,"SecretDiscovered")==1
            and #events(output,"SecretRewardSpawned")==1)
        assert(secret.revealed and #state.entities==before_entities+1)
        assert(Content.walkable(state.generated_map,wall_x,wall_y)
            and Content.walkable(state.generated_map,secret.cache_x,secret.cache_y))
        assert(Game.is_visible(state,state.active_floor_id,
            secret.cache_x,secret.cache_y))
        assert(state.rng.state==before_rng)
        local reward=Game.entity(state,secret.reward_id)
        local cached=Game.snapshot(reward.offers)
        local _,_,repeat_output=Game.submit(state,{kind="basic_attack",
            dx=wall_x-secret.x,dy=wall_y-secret.y})
        assert(#events(repeat_output,"SecretDiscovered")==0
            and #state.entities==before_entities+1)
        equal(cached,reward.offers)
        local saved=Game.snapshot(state)
        equal(saved,Game.snapshot(Game.restore(saved)))
        assert(Game.submit(state,{kind="move",dx=wall_x-secret.x,dy=wall_y-secret.y}))
        local dx,dy=secret.cache_x-wall_x,secret.cache_y-wall_y
        local committed,why=Game.submit(state,{kind="move",dx=dx,dy=dy})
        assert(not committed and why=="chest")
        local _,_,claim=Game.submit(state,{kind="claim_chest",
            chest_id=reward.id,offer_index=1})
        assert(#events(claim,"BoonGranted")==1 and reward.claimed)
        assert(state.currency==0)
    end)

    test("movement sigil rewards exactly once on TileEntered and persists",function()
        local state=Game.new_generated(1)
        quiet(state)
        local secret=state.secrets[2]
        local idle=Game.snapshot(state)
        assert(not Game.submit(state,{kind="cancel"}))
        equal(idle,Game.snapshot(state))
        local steps=walk_to(state,secret.x,secret.y)
        assert(steps>0 and secret.revealed and state.currency==4)
        local drop=state.drops[#state.drops]
        assert(drop.secret_id==secret.id and drop.collected)
        assert(state.generated_map.visual_rows[secret.y]:sub(secret.x,secret.x)=="m")
        local snapshot=Game.snapshot(state)
        equal(snapshot,Game.snapshot(Game.restore(snapshot)))
        Game.submit(state,{kind="wait"})
        assert(state.currency==4)
        local direction
        for _,d in ipairs(dirs) do
            if Content.walkable(state.generated_map,secret.x+d[1],secret.y+d[2]) then
                local occupied=false
                for _,entity in ipairs(state.entities) do
                    if entity.blocks_movement and entity.position.x==secret.x+d[1]
                        and entity.position.y==secret.y+d[2] then occupied=true end
                end
                if not occupied then direction=d; break end
            end
        end
        assert(direction)
        assert(Game.submit(state,{kind="move",dx=direction[1],dy=direction[2]}))
        local _,_,again=Game.submit(state,{kind="move",dx=-direction[1],dy=-direction[2]})
        assert(#events(again,"SecretDiscovered")==0 and state.currency==4)
        local standing=Game.new_generated(2)
        quiet(standing)
        local s=standing.secrets[2]
        Game.player(standing).position.x,Game.player(standing).position.y=s.x,s.y
        Visibility.recompute(standing)
        Game.submit(standing,{kind="wait"})
        assert(not s.revealed and standing.currency==0)
    end)

    test("closed optional door blocks sight and projectiles; bump opens once",function()
        local state=Game.new_generated(1)
        quiet(state)
        local door
        for _,entity in ipairs(state.entities) do
            if entity.controller=="door" then door=entity end
        end
        assert(door)
        local room
        for _,candidate in ipairs(state.generated_map.rooms) do
            if candidate.id==state.generated_map.placements.doors[1].room_id then
                room=candidate end
        end
        local dx=door.position.x==room.x and -1 or
            (door.position.x==room.x+room.width-1 and 1 or 0)
        local dy=door.position.y==room.y and -1 or
            (door.position.y==room.y+room.height-1 and 1 or 0)
        local approach_x,approach_y=door.position.x+dx,door.position.y+dy
        assert(Content.walkable(state.generated_map,approach_x,approach_y))
        Game.player(state).position.x,Game.player(state).position.y=
            approach_x,approach_y
        Visibility.recompute(state)
        local map=state.generated_map
        local inner_x,inner_y=door.position.x-dx,door.position.y-dy
        assert(Content.walkable(map,inner_x,inner_y))
        assert(not Game.is_visible(state,state.active_floor_id,inner_x,inner_y))
        assert(Content.blocks_vision(map,door.position.x,door.position.y,state))
        assert(not Content.projectile_passable(map,door.position.x,door.position.y,state))
        assert(not Game.preview(state,"dash",-dx,-dy).valid)
        local clock=state.clock
        local ok,reason,output=Game.submit(state,{kind="move",dx=-dx,dy=-dy})
        assert(ok,reason)
        assert(state.last_action.kind=="open_door" and state.clock==clock+100)
        assert(Game.player(state).position.x==approach_x and
            Game.player(state).position.y==approach_y)
        assert(#events(output,"DoorOpened")==1 and door.open)
        assert(not Content.blocks_vision(map,door.position.x,door.position.y,state))
        assert(Content.projectile_passable(map,door.position.x,door.position.y,state))
        assert(Game.is_visible(state,state.active_floor_id,inner_x,inner_y))
        local restored=Game.restore(Game.snapshot(state))
        equal(Game.snapshot(state),Game.snapshot(restored))
        local _,_,one=Game.submit(state,{kind="move",dx=-dx,dy=-dy})
        local _,_,two=Game.submit(restored,{kind="move",dx=-dx,dy=-dy})
        equal(one,two)
        equal(Game.snapshot(state),Game.snapshot(restored))
        assert(Game.player(state).position.x==door.position.x)
    end)

    test("off-FOV enemy still follows the strict authoritative AI schedule",function()
        local state=Game.new_generated(1)
        local player=Game.player(state)
        local distant
        for _,entity in ipairs(state.entities) do
            if entity.controller=="ai" and
                not Game.is_visible(state,state.active_floor_id,
                    entity.position.x,entity.position.y) then
                distant=entity; break
            end
        end
        assert(distant)
        player.hp,player.max_hp=1000,1000
        local ok,reason,output=Game.submit(state,{kind="wait"})
        assert(ok,reason)
        local acted=false
        for _,event in ipairs(output) do
            if event.source_id==distant.id and
                (event.kind=="Moved" or event.kind=="WindupStarted"
                    or event.kind=="Waited") then acted=true end
        end
        assert(acted and state.clock==100)
    end)

    test("secret rewards use isolated streams and continuation replays identically",function()
        local a=Game.new_generated(1)
        local b=Game.new_generated(1)
        quiet(a); quiet(b)
        local t=a.secrets[1]
        local ta=b.secrets[1]
        for _,state in ipairs({a,b}) do
            local p=Game.player(state)
            p.position.x,p.position.y=t.x,t.y
            Visibility.recompute(state)
        end
        local before=a.rng.state
        local intent={kind="basic_attack",dx=t.wall_x-t.x,dy=t.wall_y-t.y}
        local _,_,events_a=Game.submit(a,intent)
        local _,_,events_b=Game.submit(b,intent)
        equal(events_a,events_b)
        assert(a.rng.state==before and b.rng.state==before)
        assert(t.revealed and ta.revealed)
        local restored=Game.restore(Game.snapshot(a))
        local future={{kind="wait"},{kind="wait"},{kind="basic_attack",dx=1,dy=0}}
        for _,next_intent in ipairs(future) do
            local _,_,one=Game.submit(a,next_intent)
            local _,_,two=Game.submit(restored,next_intent)
            equal(one,two)
        end
        equal(Game.snapshot(a),Game.snapshot(restored))
    end)
end
