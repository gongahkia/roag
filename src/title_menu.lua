-- Read-only title summaries. The one active run remains in its existing service;
-- this module never reads storage, creates a run, or advances simulation.
local Biomes=require('src.biomes')
local Classes=require('src.classes')
local Game=require('src.game')

local T={}

function T.resumable(found)
    return found and (found.status=='active' or found.status=='recovered') or false
end

function T.default_selection(found)
    return T.resumable(found) and 1 or 2
end

function T.save_summary(found)
    if not found or found.status=='absent' then
        return 'NO SAVED RUN','Choose New Game to begin'
    end
    if T.resumable(found) and found.state then
        local state=found.state
        local player=Game.player(state)
        local class=Classes.get(player.definition_id)
        local biome=state.stage and Biomes.get(state.stage.biome_id)
        local where=state.finale and 'Final Sanctum' or
            (biome and biome.name or 'Unknown biome')
        local place=state.finale and 'Final encounter' or
            ('Stage %d · Floor %d'):format(state.stage and state.stage.index or 1,
                state.active_floor_index or 1)
        return (found.status=='recovered' and 'RECOVERED · ' or '')..
            (class and class.name or 'Unknown class')..' · Lv '..
            tostring(player.progression and player.progression.level or 1)..
            ' · Seed '..tostring(state.root_seed or state.run_seed or '?'),
            ('%s · %s · HP %d/%d'):format(where,place,player.hp,player.max_hp)
    end
    if found.status=='pending_terminal' then
        return 'UNLOCK RECOVERY NEEDED','Press R before replacing this save'
    elseif found.status=='terminal' then
        return 'RUN COMPLETE','Available for a new game'
    elseif found.status=='incompatible' then
        return 'INCOMPATIBLE SAVE','Preserved; new game requires confirmation'
    elseif found.status=='corrupt' then
        return 'DAMAGED SAVE','Preserved; new game requires confirmation'
    end
    return 'UNAVAILABLE','Cannot continue this save'
end

return T
