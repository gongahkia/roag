-- The final chamber is authored content. Its mutable encounter state lives in
-- state.finale; retired biome floors never simulate after the handoff.
local Finale={}

Finale.map={
    id="core:map/final_sanctum",floor_id="core:floor/final_sanctum",
    width=23,height=17,start_x=12,start_y=14,boss_x=12,boss_y=5,
    rows={
        "#######################",
        "#.....................#",
        "#.....................#",
        "#.....##.....##.......#",
        "#.....................#",
        "#.....................#",
        "#.....................#",
        "#....##.........##....#",
        "#.....................#",
        "#.....................#",
        "#....##.........##....#",
        "#.....................#",
        "#.....................#",
        "#.....................#",
        "#.....##.....##.......#",
        "#.....................#",
        "#######################",
    },
}

local function entity(state,id)
    for _,actor in ipairs(state.entities) do
        if actor.id==id then return actor end
    end
end

function Finale.init(state,spawn_enemy,history)
    local map=Finale.map
    local boss=spawn_enemy(state,"core:enemy/hollow_regent",map.boss_x,map.boss_y,0)
    boss.ai_memory.attacks=0
    state.finale={phase="active",boss_id=boss.id,route_history=history,
        index=4,defeat_time=nil}
end

function Finale.on_event(state,event,emit)
    local finale=state.finale
    if not finale or finale.phase~="active" or event.target_id~=finale.boss_id then return end
    if event.kind=="DamageTaken" then
        local boss=entity(state,finale.boss_id)
        if boss and boss.hp>0 and not boss.phase_two and boss.hp*2<=boss.max_hp then
            boss.phase_two=true
            boss.speed=boss.speed+25
            emit("RegentPhaseChanged",{boss_id=boss.id,phase=2,
                floor_id=state.active_floor_id,x=boss.position.x,y=boss.position.y},boss.id)
        end
    elseif event.kind=="EntityKilled" then
        finale.phase="boss_defeated"
        finale.defeat_time=event.time
        emit("RegentDefeated",{boss_id=finale.boss_id,
            floor_id=state.active_floor_id,x=event.x,y=event.y},event.source_id)
    end
end

function Finale.finish(state,emit)
    local finale=state.finale
    if not finale or finale.phase~="boss_defeated" then return end
    local player=entity(state,state.player_id)
    if not player or player.hp<=0 then return end -- competing lethal effects: death wins
    finale.phase="victory"
    state.game_over=true
    local elapsed=state.clock
    for _,floor in ipairs(state.retired_stages) do elapsed=elapsed+floor.clock end
    state.victory={boss_id=finale.boss_id,boss_name="Hollow Regent",
        seed=state.root_seed,route_history=finale.route_history,
        level=state.progression.level,xp_total=state.progression.xp_total,
        hp=player.hp,max_hp=player.max_hp,currency=state.currency,
        boons=player.boons,world_time=elapsed,
        defeat_time=finale.defeat_time}
    emit("Victory",{boss_id=finale.boss_id,
        floor_id=state.active_floor_id,world_time=elapsed},state.player_id)
end

function Finale.validate(state)
    if not state.finale then return true end
    local finale=state.finale
    assert(state.stage==nil and state.map_id==Finale.map.id
        and state.active_floor_id==Finale.map.floor_id
        and finale.index==4 and type(finale.route_history)=="table"
        and #finale.route_history==3 and #state.retired_stages==3,
        "invalid finale identity")
    assert(finale.route_history[3]=="core:route/gateway",
        "invalid final route")
    assert(finale.phase=="active" or finale.phase=="boss_defeated"
        or finale.phase=="victory","invalid finale phase")
    local boss=entity(state,finale.boss_id)
    if finale.phase=="active" then
        assert(boss and boss.definition_id=="core:enemy/hollow_regent"
            and boss.hp>0 and not state.game_over and not state.victory,
            "missing active Regent")
    else
        assert(not boss and type(finale.defeat_time)=="number",
            "defeated Regent still active")
    end
    if finale.phase=="victory" then
        assert(state.game_over and state.victory and
            state.victory.boss_id==finale.boss_id,
            "missing terminal victory")
    end
    return true
end

return Finale
