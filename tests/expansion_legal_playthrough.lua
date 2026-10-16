-- A two-floor, three-stage legal-action driver. Reads complete authoritative
-- state to plan, but changes gameplay only through documented Game.submit.
-- This is automated strategy evidence, never a claim of human play.
local Play=require('tests.legal_playthrough')
local Game=require('src.game')
local Content=require('src.content')
local Achievements=require('src.achievements')
local Classes=require('src.classes')
local P={}
local dirs={{0,-1},{1,0},{0,1},{-1,0}}
local function adjacent(a,b)
    return math.max(math.abs(a.x-b.x),math.abs(a.y-b.y))==1
end
local function cardinal(a,b)
    return math.abs(a.x-b.x)+math.abs(a.y-b.y)==1
end
local function ranged_direction(state,at,target)
    local class=Game.player(state).definition_id
    local range=(class==Classes.order[3] or class==Classes.order[6]) and 3 or nil
    if not range then return nil end
    local ddx,ddy=target.x-at.x,target.y-at.y
    local dx=ddx==0 and 0 or (ddx>0 and 1 or -1)
    local dy=ddy==0 and 0 or (ddy>0 and 1 or -1)
    local steps=math.max(math.abs(ddx),math.abs(ddy))
    if steps<1 or steps>range or
        (ddx~=0 and ddy~=0 and math.abs(ddx)~=math.abs(ddy)) then
        return nil
    end
    local map=Content.map_for(state)
    for step=1,steps do
        local x,y=at.x+dx*step,at.y+dy*step
        if dx~=0 and dy~=0 then
            if not Content.projectile_passable(map,x-dx,y,state) or
                not Content.projectile_passable(map,x,y-dy,state) then
                return nil
            end
        end
        if not Content.projectile_passable(map,x,y,state) then return nil end
    end
    return dx,dy
end
local function fight_target(run,id,max_actions)
    local state=run.state
    local class=Game.player(state).definition_id
    if class~=Classes.order[3] and class~=Classes.order[6] then
        return Play.fight(run,id,max_actions)
    end
    for _=1,max_actions do
        local enemy=Game.entity(state,id)
        if not enemy then return true end
        if state.game_over then return false,'dead' end
        local p=Game.player(state).position
        if Play.escape(run,enemy.position) then
            -- Locked impact cells have priority over a shot.
        else
            local dx,dy=ranged_direction(state,p,enemy.position)
            if dx then
                local ok,why=Play.action(run,{kind='basic_attack',dx=dx,dy=dy})
                if not ok then return false,why end
            else
                local step=Play.path(state,function(at)
                    return ranged_direction(state,at,enemy.position)~=nil
                end)
                if not step then return false,'no ranged firing position' end
                local ok,why=Play.action(run,{kind='move',dx=step[1],dy=step[2]})
                if not ok then return false,why end
            end
        end
    end
    return false,'ranged combat action limit'
end
local function nearest_enemy(state)
    return Play.nearest(state,function(e)
        return e.controller=='ai' and not e.invulnerable and
            e.id~=state.stage.elite_id
    end)
end
local function navigate_stair(run,max_actions)
    local state=run.state
    local prior=state.active_floor_id
    local stair=assert(state.generated_map.placements.stairs[1],
        'paired floor has no stair')
    for _=1,max_actions do
        if state.active_floor_id~=prior then return true end
        if state.game_over then return false,'dead on way to stair' end
        local player=Game.player(state)
        if Play.escape(run,stair) or Play.attack_adjacent(run,state.stage.elite_id) then
            -- Replan around committed attacks and any ordinary opponent.
        else
            local step=Play.path(state,function(at)
                return at.x==stair.x and at.y==stair.y
            end)
            if not step then return false,'no legal path to stair' end
            local ok,why=Play.action(run,{kind='move',dx=step[1],dy=step[2]})
            if not ok then return false,why end
        end
    end
    return false,'stair action limit'
end
local function chest_score(offer)
    local priority={grave_dividend=60,thorn_mirror=50,emberbrand=45,
        storm_conductor=40,perfect_guard=35,temporal_echo=30,
        detonation_bloom=26,opening_cut=25,rhythm_of_steel=24}
    local slug=offer.boon_id:match('([^/]+)$')
    local rarity={common=1,uncommon=3,rare=6,legendary=10}
    return (priority[slug] or 0)+(rarity[offer.rarity] or 0)
end
local function buy_nearest(run,max_actions)
    local state=run.state
    if state.currency<5 then return false,'unaffordable' end
    local chest=Play.nearest(state,function(e)
        return e.controller=='chest' and not e.claimed and e.price>0
    end)
    if not chest then return false,'no chest' end
    local ok,why=Play.navigate(run,function(p)
        return cardinal(p,chest.position)
    end,max_actions)
    if not ok then return false,why end
    local offers=assert(Game.chest_options(state,chest.id)).offers
    local best,score=1,-math.huge
    for index,offer in ipairs(offers) do
        local value=chest_score(offer)
        if value>score then best,score=index,value end
    end
    ok,why=Play.action(run,{kind='claim_chest',chest_id=chest.id,
        offer_index=best})
    if not ok then return false,why end
    run.purchases[#run.purchases+1]={stage=state.stage.index,
        floor=state.active_floor_index,boon_id=offers[best].boon_id,
        rarity=offers[best].rarity,price=chest.price}
    return true
end
local function earn_for_chest(run,max_kills)
    local state=run.state
    for _=1,max_kills do
        if state.currency>=5 then return true end
        local enemy=nearest_enemy(state)
        if not enemy then return false,'no living ordinary enemy' end
        local x,y,id=enemy.position.x,enemy.position.y,enemy.id
        local ok,why=fight_target(run,id,180)
        if not ok then return false,why end
        ok,why=Play.navigate(run,function(p)return p.x==x and p.y==y end,200)
        if not ok then return false,why end
    end
    return state.currency>=5,state.currency>=5 and nil or 'no currency after kills'
end
local function handle_upper(run)
    local state=run.state
    if state.active_floor_index~=1 then return true end
    if state.stage.index==1 and #run.purchases==0 then
        local earned,why=earn_for_chest(run,4)
        if not earned then return false,'early economy: '..tostring(why) end
        local bought,because=buy_nearest(run,300)
        if not bought then return false,'first purchase: '..tostring(because) end
    elseif state.currency>=5 then
        -- A second choice is optional; travel to the stair remains the priority.
        buy_nearest(run,250)
    end
    return navigate_stair(run,500)
end
local function handle_lower(run)
    local state=run.state
    local elite=assert(Game.entity(state,state.stage.elite_id),
        'missing biome elite')
    local ok,why=Play.navigate(run,function(p)
        return ranged_direction(state,p,elite.position)~=nil or
            adjacent(p,elite.position)
    end,650)
    if not ok then return false,'elite approach: '..tostring(why) end
    ok,why=fight_target(run,elite.id,300)
    if not ok then return false,'elite combat: '..tostring(why) end
    local reward=Game.entity(state,state.stage.elite_reward_id)
    if reward then
        ok,why=Play.navigate(run,function(p)
            return cardinal(p,reward.position)
        end,80)
        if not ok then return false,'elite reward approach: '..tostring(why) end
        ok,why=Play.action(run,{kind='claim_chest',chest_id=reward.id,offer_index=1})
        if not ok then return false,'elite reward: '..tostring(why) end
    end
    local exit=assert(Game.entity(state,state.stage.exit_id),'missing exit')
    ok,why=Play.navigate(run,function(p)return cardinal(p,exit.position)end,130)
    if not ok then return false,'exit approach: '..tostring(why) end
    return true
end
function P.attempt(seed,options)
    options=options or {}
    local class_id=options.class_id or Classes.order[1]
    local run=Play.new(seed,{multifloor=true,class_id=class_id,
        available_boons=Achievements.pool({})})
    run.purchases={}
    local state=run.state
    local stages={}
    run.stages=stages
    for index=1,3 do
        if state.game_over then return nil,'dead before stage '..index,run end
        if state.stage.index~=index or state.active_floor_index~=1 then
            return nil,'wrong stage/floor at entry '..index,run
        end
        local ok,why=handle_upper(run)
        if not ok then return nil,'stage '..index..' upper: '..tostring(why),run end
        if state.active_floor_index~=2 then
            return nil,'stage '..index..' stair did not reach lower floor',run
        end
        ok,why=handle_lower(run)
        if not ok then return nil,'stage '..index..' lower: '..tostring(why),run end
        stages[#stages+1]={biome_id=state.stage.biome_id,
            actions=#run.trace,hp=Game.player(state).hp,clock=state.clock,
            floor=state.active_floor_id}
        if options.stop_after_stage==index then return run end
        local exit=assert(Game.entity(state,state.stage.exit_id))
        local offered=assert(Game.route_options(state,exit.id))
        local route_index=(options.routes or {1,2,1})[index] or 1
        assert(offered.routes[route_index],'missing route '..route_index)
        ok,why=Play.action(run,{kind='choose_route',exit_id=exit.id,
            route_index=route_index})
        if not ok then return nil,'stage '..index..' route: '..tostring(why),run end
    end
    assert(state.finale and not state.victory,
        'final sanctum entered without an ordinary boss fight')
    local ok,why=fight_target(run,state.finale.boss_id,400)
    if not ok then return nil,'final boss: '..tostring(why),run end
    if not state.victory then return nil,'boss fell without Victory',run end
    return run
end
return P
