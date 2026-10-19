-- Approved idea encounters. Effects use the existing Game scheduler/HP batch.
local RNG=require('src.rng')
local I={version='roag-idea-enemies/1'}
I.enemies={
    -- id, name, HP, damage, defense, speed, windup, recovery, range
    {'heavy_rammer','Heavy Rammer',12,2,1,70,110,140,1},
    {'hollow_wraith','Hollow Wraith',7,2,0,90,100,120,1},
}
I.roles={['core:enemy/heavy_rammer']='rammer',['core:enemy/hollow_wraith']='wraith'}
I.tiny={['core:enemy/heavy_rammer']=89}
-- The reviewed redirect permits only 416, in BOTH settings choices.
I.shared_onebit={['core:enemy/hollow_wraith']=416}
I.onebit={['core:enemy/heavy_rammer']=129,['core:enemy/hollow_wraith']=416}
I.abilities={rammer_shove='rammer shove',wraith_phase='wraith phase',wraith_touch='wraith touch'}
local directions={{0,-1},{1,0},{0,1},{-1,0}}

function I.phase_geometry(from,to)
    local dx,dy=to.x-from.x,to.y-from.y
    return (dx==0 or dy==0) and math.abs(dx)+math.abs(dy)==2
end

function I.phase_landing(state,enemy,cell,map,free,visible)
    if cell.floor_id~=enemy.position.floor_id or cell.floor_id~=state.active_floor_id
        or not I.phase_geometry(enemy.position,cell) then return false end
    if not free(state,enemy.id,cell.x,cell.y) or not visible(cell.x,cell.y) then return false end
    for _,stair in ipairs(map.placements and map.placements.stairs or {}) do
        if stair.x==cell.x and stair.y==cell.y then return false end
    end
    for _,other in ipairs(state.entities) do
        local p=other.position
        if other.controller=='exit' and p.floor_id==cell.floor_id
            and p.x==cell.x and p.y==cell.y then return false end
    end
    return true
end

function I.choose(state,enemy,player,h)
    local a,b=enemy.position,player.position
    local dx,dy=b.x-a.x,b.y-a.y
    if I.roles[enemy.definition_id]=='wraith' then
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/wraith_touch',cells={{x=b.x,y=b.y}}}
        end
        local best,score
        for _,d in ipairs(directions) do
            local x,y=a.x+d[1]*2,a.y+d[2]*2
            local middle=h.map.rows[a.y+d[2]]
            local cell={floor_id=a.floor_id,x=x,y=y}
            local distance=math.abs(x-b.x)+math.abs(y-b.y)
            if middle and middle:sub(a.x+d[1],a.x+d[1])=='#'
                and distance<math.abs(dx)+math.abs(dy)
                and I.phase_landing(state,enemy,cell,h.map,h.free,h.visible)
                and (not score or distance<score) then best,score=cell,distance end
        end
        if best then
            return {kind='windup',ability_id='core:ability/wraith_phase',cells={best},
                support={kind='phase'}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
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
    local definition=I.enemies[math.floor(seed/16)%#I.enemies+1]
    replace(target,'core:enemy/'..definition[1])
    for _,p in ipairs(map.placements.enemies) do
        if p.x==target.position.x and p.y==target.position.y then
            p.definition_id=target.definition_id
            break
        end
    end
end

function I.validate_pending(enemy,pending)
    local phase=pending.support and pending.support.kind=='phase'
    if phase or pending.ability_id=='core:ability/wraith_phase' then
        assert(phase and enemy.definition_id=='core:enemy/hollow_wraith'
            and pending.ability_id=='core:ability/wraith_phase'
            and #pending.target_cells==1
            and pending.target_cells[1].floor_id==pending.floor_id
            and I.phase_geometry({x=pending.source_x,y=pending.source_y},pending.target_cells[1]),
            'invalid committed phase')
    end
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
