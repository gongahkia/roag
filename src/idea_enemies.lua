-- Approved idea encounters. Effects use the existing Game scheduler/HP batch.
local RNG=require('src.rng')
local I={version='roag-idea-enemies/1'}
I.enemies={
    -- id, name, HP, damage, defense, speed, windup, recovery, range
    {'heavy_rammer','Heavy Rammer',12,2,1,70,110,140,1},
}
I.roles={['core:enemy/heavy_rammer']='rammer'}
I.tiny={['core:enemy/heavy_rammer']=89}
I.onebit={['core:enemy/heavy_rammer']=129}
I.abilities={rammer_shove='rammer shove'}

function I.choose(state,enemy,player,h)
    local a,b=enemy.position,player.position
    local dx,dy=b.x-a.x,b.y-a.y
    if math.abs(dx)+math.abs(dy)==1 then
        return {kind='windup',ability_id='core:ability/rammer_shove',
            cells={{x=b.x,y=b.y}},
            shove={floor_id=a.floor_id,x=b.x,y=b.y,dx=dx,dy=dy}}
    end
    return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
end

-- At most one ordinary replacement, with a stable independent substream.
-- Existing saved floors are not repopulated. No extra crowd or combat-RNG draw.
function I.populate(state,map,replace)
    local seed=RNG.derive(state.root_seed or state.run_seed,'idea-enemies:'..map.floor_id)
    if seed%4~=0 then return end
    local choices={}
    for _,e in ipairs(state.entities) do
        local p=e.position
        if e.controller=='ai' and p.floor_id==map.floor_id and not e.squad_id
            and (e.ai_id=='guard' or e.ai_id=='skitter' or e.ai_id=='expansion_regular')
            and math.abs(p.x-map.start_x)+math.abs(p.y-map.start_y)>=8 then
            choices[#choices+1]=e
        end
    end
    table.sort(choices,function(a,b)return a.id<b.id end)
    if #choices==0 then return end
    local target=choices[math.floor(seed/4)%#choices+1]
    replace(target,'core:enemy/heavy_rammer')
    for _,p in ipairs(map.placements.enemies) do
        if p.x==target.position.x and p.y==target.position.y then
            p.definition_id=target.definition_id
            break
        end
    end
end

function I.validate_pending(enemy,pending)
    local s=pending.shove
    if not s and pending.ability_id~='core:ability/rammer_shove' then return end
    assert(enemy.definition_id=='core:enemy/heavy_rammer'
        and pending.ability_id=='core:ability/rammer_shove'
        and type(s)=='table' and s.floor_id==pending.floor_id
        and type(s.x)=='number' and s.x%1==0
        and type(s.y)=='number' and s.y%1==0
        and type(s.dx)=='number' and s.dx%1==0
        and type(s.dy)=='number' and s.dy%1==0
        and math.abs(s.dx)+math.abs(s.dy)==1
        and s.x==pending.source_x+s.dx and s.y==pending.source_y+s.dy
        and #pending.target_cells==1
        and pending.target_cells[1].x==s.x and pending.target_cells[1].y==s.y,
        'invalid committed shove')
end

return I
