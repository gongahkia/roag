-- Guided acceptance driver: reads the world, but changes it only through Game.submit.
local Game=require('src.game')
local Content=require('src.content')
local Squads=require('src.enemy_squads')

local Play={}
local dirs={{0,-1},{1,0},{0,1},{-1,0}}

local function same(a,b) return a.x==b.x and a.y==b.y end
local function adjacent(a,b)
    return math.abs(a.x-b.x)<=1 and math.abs(a.y-b.y)<=1 and not same(a,b)
end
local function cardinal(a,b)
    return math.abs(a.x-b.x)+math.abs(a.y-b.y)==1
end

function Play.new(seed,options)
    return {state=Game.new_stage(seed,options),seed=seed,trace={},events={},checkpoints={}}
end

function Play.action(run,intent)
    local state=run.state
    local player=Game.player(state)
    local from={x=player.position.x,y=player.position.y,hp=player.hp,
        clock=state.clock,stage=state.stage and state.stage.index or
            state.finale and state.finale.index}
    local ok,reason,events=Game.submit(state,intent)
    local record={intent=intent,ok=ok,reason=reason,from=from,
        clock=state.clock,hp=player.hp,currency=state.currency,
        event_kinds={}}
    for _,event in ipairs(events) do
        record.event_kinds[#record.event_kinds+1]=event.kind
        run.events[#run.events+1]=event
    end
    run.trace[#run.trace+1]=record
    return ok,reason,events
end

local function passable(state,x,y,avoid_guarded)
    local map=Content.map_for(state)
    if not Content.walkable(map,x,y) then return false end
    for _,entity in ipairs(state.entities) do
        if avoid_guarded and entity.controller=='ai' and entity.position.floor_id==state.active_floor_id and
            entity.position.x==x and entity.position.y==y and
            Squads.block(state,entity,Game.player(state),true) then return false end
        if entity.blocks_movement and entity.position.floor_id==state.active_floor_id
            and entity.position.x==x and entity.position.y==y
            and entity.id~=state.player_id and entity.controller~='ai'
            and entity.controller~='door' then return false end
    end
    return true
end

function Play.path(state,goal,avoid_guarded)
    local p=Game.player(state).position
    if goal(p) then return {} end
    local map=Content.map_for(state)
    local function key(x,y) return (y-1)*map.width+x end
    local queue={{x=p.x,y=p.y,first=nil}}
    local seen={[key(p.x,p.y)]=true}
    local head=1
    while head<=#queue do
        local node=queue[head];head=head+1
        for _,d in ipairs(dirs) do
            local x,y=node.x+d[1],node.y+d[2]
            local k=key(x,y)
            if not seen[k] and passable(state,x,y,avoid_guarded) then
                seen[k]=true
                local first=node.first or d
                if goal({x=x,y=y}) then return first end
                queue[#queue+1]={x=x,y=y,first=first}
            end
        end
    end
end

local function threatened(state,x,y,ignore_armoured)
    for _,strike in ipairs(Game.pending_strikes(state)) do
        -- Support channels are not damaging strikes. In particular, occupying
        -- a phase/revival destination counters it; dodging helps the caster.
        if not strike.support and (not ignore_armoured or strike.shove or
            (strike.damage or math.huge)>(Game.player(state).defense or 0)) then
            for _,cell in ipairs(strike.target_cells) do
                if cell.floor_id==state.active_floor_id and cell.x==x and cell.y==y then
                    return true
                end
            end
        end
    end
    return false
end

function Play.escape(run,goal,ignore_armoured)
    local state=run.state
    local p=Game.player(state).position
    if not threatened(state,p.x,p.y,ignore_armoured) then return false end
    local best,score
    for _,d in ipairs(dirs) do
        local x,y=p.x+d[1],p.y+d[2]
        local preview=Game.preview(state,'move',d[1],d[2])
        if preview.valid and preview.distance==1 and not threatened(state,x,y) then
            local value=goal and math.abs(x-goal.x)+math.abs(y-goal.y) or 0
            if not best or value<score then best,score=d,value end
        end
    end
    if best then
        local ok=Play.action(run,{kind='move',dx=best[1],dy=best[2]})
        return ok
    end
    return false
end

-- The Alchemist's existing area flask is useful against the approved squads.
-- This driver still submits ordinary public actions; no HP/position assistance.
function Play.attack_intent(run,dx,dy)
    local state=run.state
    if Game.player(state).definition_id=='core:actor/alchemist' then
        local area=Game.preview(state,'sweeping_slash',dx,dy)
        local targets=0
        if area.valid then
            for _,e in ipairs(state.entities) do
                if e.controller=='ai' and not e.invulnerable and e.hp>0 and
                    e.position.floor_id==state.active_floor_id then
                    for _,cell in ipairs(area.cells) do
                        if cell.x==e.position.x and cell.y==e.position.y then targets=targets+1;break end
                    end
                end
            end
        end
        if targets>=2 then return {kind='sweeping_slash',dx=dx,dy=dy} end
    end
    return {kind='basic_attack',dx=dx,dy=dy}
end

function Play.attack_adjacent(run,excluded_id)
    local state=run.state
    local p=Game.player(state).position
    local selected
    local function reachable(target)
        local dx,dy=target.position.x-p.x,target.position.y-p.y
        local intent=Play.attack_intent(run,dx,dy)
        local preview=Game.preview(state,intent.kind,dx,dy)
        for _,cell in ipairs(preview.valid and preview.cells or {})do
            if cell.x==target.position.x and cell.y==target.position.y then return true end
        end
        return false
    end
    for _,entity in ipairs(state.entities) do
        if entity.controller=='ai' and entity.id~=excluded_id and not entity.invulnerable
            and entity.position.floor_id==state.active_floor_id
            and adjacent(p,entity.position) and reachable(entity) and
            (not selected or entity.hp<selected.hp or
                (entity.hp==selected.hp and entity.id<selected.id)) then
            selected=entity
        end
    end
    if not selected then return false end
    if Squads.block(state,selected,Game.player(state),true) then
        local ward=Squads.ward_source(state,selected)
        local step=Play.path(state,function(at)
            if ward then return adjacent(at,ward.position) end
            return adjacent(at,selected.position) and not Squads.block(state,selected,
                {position={floor_id=state.active_floor_id,x=at.x,y=at.y}},true)
        end,true)
        if step and step[1] then return Play.action(run,{kind='move',dx=step[1],dy=step[2]}) end
        if ward then
            -- A moved bodyguard can seal a currently reachable flank even
            -- though the original formation had counterplay. Lure it beyond
            -- its caster's four-cell field instead of bumping it forever.
            local retreat,score
            local current=math.abs(p.x-ward.position.x)+math.abs(p.y-ward.position.y)
            for _,d in ipairs(dirs)do
                local preview=Game.preview(state,'move',d[1],d[2])
                local x,y=p.x+d[1],p.y+d[2]
                local distance=math.abs(x-ward.position.x)+math.abs(y-ward.position.y)
                if preview.valid and preview.distance==1 and distance>current
                    and not threatened(state,x,y) and (not score or distance>score)then
                    retreat,score=d,distance
                end
            end
            if retreat then return Play.action(run,{kind='move',dx=retreat[1],dy=retreat[2]})end
        end
        if not ward then return Play.action(run,{kind='wait'}) end
        return false
    end
    local ok=Play.action(run,Play.attack_intent(run,selected.position.x-p.x,selected.position.y-p.y))
    return ok
end

-- Only used after a demonstrated travel cycle. A ranged class must be able
-- to shoot a reachable blocker instead of dodging forever outside bump range.
function Play.attack_in_reach(run)
    local state=run.state;local player=Game.player(state)
    local best
    for _,kind in ipairs({'basic_attack','sweeping_slash'})do
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0},{1,-1},{1,1},{-1,1},{-1,-1}})do
            local preview=Game.preview(state,kind,d[1],d[2])
            if preview.valid then
                local cells,damage,_,ranged=Game.effective_ability(state,kind,d[1],d[2],true)
                for _,cell in ipairs(cells)do
                    local occupied=false
                    for _,e in ipairs(state.entities)do
                        if e.hp and e.hp>0 and e.id~=player.id and e.position.floor_id==state.active_floor_id
                            and e.position.x==cell.x and e.position.y==cell.y then
                            occupied=true
                            if e.controller=='ai' and not e.invulnerable and damage>(e.defense or 0)
                                and not Squads.block(state,e,player,true)
                                and (not best or e.hp<best.hp or e.hp==best.hp and e.id<best.id)then
                                best={hp=e.hp,id=e.id,dx=d[1],dy=d[2],kind=kind}
                            end
                        end
                    end
                    if occupied and ranged==true then break end
                end
            end
        end
    end
    if best then return Play.action(run,{kind=best.kind,dx=best.dx,dy=best.dy})end
    return false
end

function Play.navigate(run,goal,max_actions)
    local first_trace=#run.trace
    for _=1,max_actions do
        local state=run.state
        local p=Game.player(state).position
        if goal(p) then return true end
        if state.game_over then return false,'dead' end
        local n=#run.trace
        local cycle=n-first_trace>=6
        local a,b=run.trace[n],run.trace[n-1]
        if cycle then
            local stationary=p.x==a.from.x and p.y==a.from.y
                and a.from.x==b.from.x and a.from.y==b.from.y
            cycle=stationary or p.x==b.from.x and p.y==b.from.y
            for offset=0,5 do
                local record=run.trace[n-offset]
                local expected=stationary and a.from or offset%2==0 and a.from or b.from
                if not record.ok or record.from.x~=expected.x or record.from.y~=expected.y
                    or record.from.hp~=record.hp
                    or not stationary and record.intent.kind~='move' then cycle=false;break end
            end
        end
        local near=false
        if cycle then
            for _,e in ipairs(state.entities)do
                if e.controller=='ai' and e.hp>0 and e.position.floor_id==state.active_floor_id
                    and math.abs(e.position.x-p.x)+math.abs(e.position.y-p.y)<=3 then near=true;break end
            end
        end
        local step
        if cycle and threatened(state,p.x,p.y) then step=Play.path(state,goal)end
        if cycle and (Play.attack_in_reach(run) or
            Play.attack_adjacent(run,state.stage and state.stage.elite_id)) then
            -- Break a proven cycle with an ordinary immediate attack.
        elseif step and step[1] then
            -- A one-cell alcove between opposed ranged lanes can make greedy
            -- safe-cell dodging repeat forever. Commit legal forward travel
            -- after detecting that cycle, accepting ordinary damage risk.
            -- No actor stats, positions, schedules or HP are assisted.
            local dash=Game.preview(state,'dash',step[1],step[2])
            local kind=not near and dash.valid and dash.distance>1 and 'dash' or 'move'
            local ok,reason=Play.action(run,{kind=kind,dx=step[1],dy=step[2]})
            if not ok then return false,reason end
        elseif Play.escape(run) then
            -- A committed dodge consumes its normal recovery; replan afterward.
        elseif Play.attack_adjacent(run,state.stage and state.stage.elite_id) then
            -- Do not let a nearby normal enemy attack freely during travel.
        else
        local step=Play.path(state,goal)
        if not step then return false,'no path' end
        local ok,reason=Play.action(run,{kind='move',dx=step[1],dy=step[2]})
        if not ok then return false,reason end
        end
    end
    return false,'navigation action limit'
end

function Play.fight(run,id,max_actions,stop_when)
    for _=1,max_actions do
        local state=run.state
        if stop_when and stop_when() then return true end
        local enemy=Game.entity(state,id)
        if not enemy then return true end
        enemy=Squads.ward_source(state,enemy) or enemy
        if state.game_over then return false,'dead' end
        local p=Game.player(state).position
        if Play.escape(run,enemy.position) then
            -- Locked strikes are evaluated against current occupancy after this dodge.
        elseif adjacent(p,enemy.position) and not Squads.block(state,enemy,Game.player(state),true) then
            local dx,dy=enemy.position.x-p.x,enemy.position.y-p.y
            local ok,reason=Play.action(run,{kind='basic_attack',dx=dx,dy=dy})
            if not ok then return false,reason end
        else
            local step=Play.path(state,function(at)
                return adjacent(at,enemy.position) and not Squads.block(state,enemy,
                    {position={floor_id=state.active_floor_id,x=at.x,y=at.y}},true)
            end,true)
            step=step or Play.path(state,function(at) return adjacent(at,enemy.position) end)
            if not step then return false,'no approach' end
            local intent=step[1] and {kind='move',dx=step[1],dy=step[2]} or {kind='wait'}
            local ok,reason=Play.action(run,intent)
            if not ok then return false,reason end
        end
    end
    return false,'combat action limit'
end

function Play.nearest(state,predicate)
    local p=Game.player(state).position
    local best,score
    for _,entity in ipairs(state.entities) do
        if entity.position and entity.position.floor_id==state.active_floor_id
            and predicate(entity) then
            local d=math.abs(p.x-entity.position.x)+math.abs(p.y-entity.position.y)
            if not best or d<score or (d==score and entity.id<best.id) then
                best,score=entity,d
            end
        end
    end
    return best
end

function Play.has_event(run,kind)
    for _,event in ipairs(run.events) do if event.kind==kind then return true end end
    return false
end

local function checkpoint(run,name)
    local state=run.state
    run.checkpoints[#run.checkpoints+1]={name=name,actions=#run.trace,
        clock=state.clock,hp=Game.player(state).hp,currency=state.currency,
        stage=state.stage and state.stage.index or state.finale and state.finale.index,
        floor_id=state.active_floor_id}
end

-- Seed 3 is a guided route, not a human playtest. No player/entity/stat/scheduler
-- field is assigned by the driver; every world change comes from Game.submit.
function Play.stage_one(seed,route_index,options)
    local run=Play.new(seed,options)
    local state=run.state
    checkpoint(run,'new_run')
    local kills=0
    repeat
        local enemy=assert(Play.nearest(state,function(entity)
            return entity.controller=='ai' and entity.id~=state.stage.elite_id
                and not entity.invulnerable
        end),'no ordinary enemy')
        local x,y,id=enemy.position.x,enemy.position.y,enemy.id
        local ok,reason=Play.fight(run,id,120)
        assert(ok,reason)
        kills=kills+1
        ok,reason=Play.navigate(run,function(p) return p.x==x and p.y==y end,150)
        assert(ok,reason)
    until state.currency>=5 and kills>=1 or kills>=6
    assert(state.currency>=5 and not state.game_over,'could not earn chest price')
    checkpoint(run,'earned_currency')

    local chest=assert(Play.nearest(state,function(entity)
        if entity.controller~='chest' or entity.claimed or entity.price<=0 then
            return false
        end
        for _,offer in ipairs(entity.offers) do
            if offer.boon_id=='core:boon/emberbrand' then return true end
        end
        return false
    end),'no paid chest with Emberbrand')
    local ok,reason=Play.navigate(run,function(p)
        return cardinal(p,chest.position)
    end,200)
    assert(ok,reason)
    local before_clock,before_currency=state.clock,state.currency
    local options=assert(Game.chest_options(state,chest.id))
    local reopened=assert(Game.chest_options(state,chest.id))
    assert(#options.offers==3 and #reopened.offers==3 and
        state.clock==before_clock and state.currency==before_currency)
    for i=1,3 do
        assert(options.offers[i].boon_id==reopened.offers[i].boon_id and
            options.offers[i].rarity==reopened.offers[i].rarity)
    end
    local emberbrand_index
    for index,offer in ipairs(options.offers) do
        if offer.boon_id=='core:boon/emberbrand' then emberbrand_index=index end
    end
    assert(emberbrand_index,'selected chest lost Emberbrand')
    ok,reason=Play.action(run,{kind='claim_chest',chest_id=chest.id,
        offer_index=emberbrand_index})
    assert(ok,reason)
    assert(chest.claimed and state.currency==before_currency-chest.price)
    checkpoint(run,'paid_chest_claimed')

    local elite=assert(Game.entity(state,state.stage.elite_id),'missing elite')
    ok,reason=Play.navigate(run,function(p)
        return adjacent(p,elite.position)
    end,400)
    assert(ok,reason)
    checkpoint(run,'elite_reached')
    ok,reason=Play.fight(run,elite.id,150)
    assert(ok,reason)
    assert(Play.has_event(run,'StatusApplied'),'earned Emberbrand never ignited a target')
    assert(state.stage.phase=='extraction' and not state.game_over)
    checkpoint(run,'elite_defeated')

    local free=assert(Game.entity(state,state.stage.elite_reward_id))
    ok,reason=Play.navigate(run,function(p)
        return cardinal(p,free.position)
    end,30)
    assert(ok,reason)
    ok,reason=Play.action(run,{kind='claim_chest',chest_id=free.id,offer_index=1})
    assert(ok,reason)
    checkpoint(run,'free_chest_claimed')
    local exit=assert(Game.entity(state,state.stage.exit_id))
    ok,reason=Play.navigate(run,function(p)
        return cardinal(p,exit.position)
    end,30)
    assert(ok,reason)
    local first_floor=state.active_floor_id
    local route=assert(Game.route_options(state,exit.id))
    assert(#route.routes==3)
    ok,reason=Play.action(run,{kind='choose_route',exit_id=exit.id,
        route_index=route_index or 1})
    assert(ok,reason)
    assert(state.stage.index==2 and state.active_floor_id~=first_floor)
    checkpoint(run,'stage_two_entered')
    local next_enemy=assert(Play.nearest(state,function(entity)
        return entity.controller=='ai' and entity.id~=state.stage.elite_id
            and not entity.invulnerable
    end),'new stage has no ordinary enemy')
    ok,reason=Play.fight(run,next_enemy.id,120)
    assert(ok,reason)
    checkpoint(run,'stage_two_enemy_defeated')
    return run
end

-- Full Tranche 12 guided route, with no direct state edits. This remains a
-- deterministic strategy test rather than a claim of human survival.
function Play.biome_route(seed,options)
    local run=Play.stage_one(seed,1,options)
    local state=run.state
    assert(state.stage.biome_id=='core:biome/fungal_hollows')
    local spore=assert(Play.nearest(state,function(e)
        return e.definition_id=='core:enemy/spore_herald' end))
    assert(Play.fight(run,spore.id,160))
    checkpoint(run,'spore_herald_defeated')
    local before=state.currency
    local x,y=spore.position.x,spore.position.y
    assert(Play.navigate(run,function(p) return p.x==x and p.y==y end,150))
    assert(state.currency>=before+Content.get_enemy(spore.definition_id).currency_drop)
    checkpoint(run,'fungal_currency_collected')
    local elite=assert(Game.entity(state,state.stage.elite_id))
    assert(Play.navigate(run,function(p)
        return math.max(math.abs(p.x-elite.position.x),
            math.abs(p.y-elite.position.y))==1
    end,500))
    assert(Play.fight(run,elite.id,180))
    checkpoint(run,'fungal_elite_defeated')
    local exit=assert(Game.entity(state,state.stage.exit_id))
    assert(Play.navigate(run,function(p) return cardinal(p,exit.position) end,100))
    local routes=assert(Game.route_options(state,exit.id))
    assert(routes.routes[2].biome_id=='core:biome/ashen_bastion')
    assert(Play.action(run,{kind='choose_route',exit_id=exit.id,route_index=2}))
    assert(state.stage.biome_id=='core:biome/ashen_bastion')
    checkpoint(run,'ashen_stage_entered')
    local bell=assert(Play.nearest(state,function(e)
        return e.definition_id=='core:enemy/cinder_bellkeeper' end))
    assert(Play.fight(run,bell.id,170))
    checkpoint(run,'bellkeeper_defeated')
    return run
end

-- Every mutation in this seeded completion uses Game.submit. The driver reads
-- positions and locked cells to choose legal actions; it never edits combat
-- stats, schedules, entity positions or stage flags.
function Play.complete_run(seed,options)
    local run=Play.biome_route(seed,options)
    local state=run.state
    local elite=assert(Game.entity(state,state.stage.elite_id))
    local ok,reason=Play.navigate(run,function(p)
        return math.max(math.abs(p.x-elite.position.x),
            math.abs(p.y-elite.position.y))==1
    end,600)
    assert(ok,reason)
    ok,reason=Play.fight(run,elite.id,250)
    assert(ok,reason)
    checkpoint(run,'third_elite_defeated')
    local reward=Game.entity(state,state.stage.elite_reward_id)
    if reward and not reward.claimed then
        ok,reason=Play.navigate(run,function(p)return cardinal(p,reward.position)end,30)
        assert(ok,reason)
        ok,reason=Play.action(run,{kind='claim_chest',chest_id=reward.id,offer_index=1})
        assert(ok,reason)
    end
    -- The elite already dropped normal recovery supplies. Collect nearby health
    -- by legal movement before leaving; do not carry five HP into the finale
    -- while abandoning an available ten-HP pickup beside the defeated elite.
    local player=Game.player(state)
    for _,drop in ipairs(state.drops)do
        local at=drop.position
        if player.hp<player.max_hp and not drop.collected and not drop.carried_by
            and (drop.kind=='health' or drop.kind=='food') and at.floor_id==state.active_floor_id
            and math.abs(at.x-player.position.x)+math.abs(at.y-player.position.y)<=2 then
            ok,reason=Play.navigate(run,function(p)return p.x==at.x and p.y==at.y end,30)
            assert(ok,reason)
        end
    end
    checkpoint(run,'third_elite_recovery_collected')
    local exit=assert(Game.entity(state,state.stage.exit_id))
    ok,reason=Play.navigate(run,function(p)
        return cardinal(p,exit.position)
    end,100)
    assert(ok,reason)
    local offered=assert(Game.route_options(state,exit.id))
    assert(offered.routes[1].id=='core:route/gateway')
    ok,reason=Play.action(run,{kind='choose_route',exit_id=exit.id,route_index=1})
    assert(ok,reason)
    checkpoint(run,'final_sanctum_entered')
    assert(state.finale and not state.victory)
    ok,reason=Play.fight(run,state.finale.boss_id,350)
    assert(ok,reason)
    assert(state.victory and state.game_over and Play.has_event(run,'Victory'))
    checkpoint(run,'victory')
    return run
end

function Play.replay(run)
    local replay=Play.new(run.seed)
    for index,record in ipairs(run.trace) do
        local ok,reason,events=Play.action(replay,record.intent)
        assert(ok==record.ok and reason==record.reason,
            'replay action differs at '..index)
        assert(replay.state.clock==record.clock and
            #events==#record.event_kinds,'replay time/events differ at '..index)
        for j,event in ipairs(events) do
            assert(event.kind==record.event_kinds[j],
                'replay event differs at '..index..':'..j)
        end
    end
    return replay
end

function Play.cardinal(a,b) return cardinal(a,b) end
return Play
