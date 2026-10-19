-- Locate a deterministic candidate cheaply, then prove the real stage consumes
-- it. No fixtures, RNG mutation, actor replacement or simulation actions.
local G=require('src.game')
local I=require('src.idea_enemies')
local S={}
function S.find(definition_id,limit)
    local sample=G.new_stage(1,{multifloor=true})
    for seed=1,limit or 512 do
        local selected=false
        for _,floor_id in ipairs(sample.floor_order)do
            local row=I.selection(seed,floor_id)
            if row and 'core:enemy/'..row[1]==definition_id then selected=true;break end
        end
        if selected then
            local state=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(state.entities)do
                if e.definition_id==definition_id then return state,seed end
            end
        end
    end
    error('no real generated '..definition_id..' in bounded seed search')
end
return S
