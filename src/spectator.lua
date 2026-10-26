-- Disposable visual-inspection runs. Nothing here writes a save or profile.
local Achievements=require('src.achievements')
local Biomes=require('src.biomes')
local Classes=require('src.classes')
local Content=require('src.content')
local Floors=require('src.floors')
local Game=require('src.game')
local Visibility=require('src.visibility')

local S={}

function S.new(seed,biome_id,class_id)
    assert(Biomes.get(biome_id),'unknown spectator biome')
    assert(Classes.get(class_id),'unknown spectator class')
    local state=Game.new_stage(seed,{multifloor=true,
        stage_options={index=1,biome_id=biome_id},class_id=class_id,
        meta=Achievements.new_run(false)})
    Game.player(state).invulnerable=true
    return state
end

function S.next_index(order,current)
    assert(type(order)=='table' and #order>0,'empty spectator selection')
    return current%#order+1
end

function S.camera_step(state,focus,dx,dy,fast)
    local map=assert(Content.map_for(state),'missing spectator map')
    local stride=fast and 8 or 1
    return {x=math.max(1,math.min(map.width,focus.x+dx*stride)),
        y=math.max(1,math.min(map.height,focus.y+dy*stride))}
end

function S.switch_floor(state)
    local map=assert(Content.map_for(state),'missing spectator map')
    for _,stair in ipairs(map.placements and map.placements.stairs or {}) do
        local landing=Floors.landing(state,stair)
        if landing then
            local before=state.clock
            Floors.change(state,stair,landing)
            Visibility.recompute(state)
            assert(state.clock==before,'spectator floor switch advanced time')
            return true
        end
    end
    return false,'no usable stair on this floor'
end

return S
