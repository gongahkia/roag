-- Approved idea encounters. Effects use the existing Game scheduler/HP batch.
local RNG=require('src.rng')
local Squads=require('src.enemy_squads')
local I={version='roag-idea-enemies/1'}
I.enemies={
    -- id, name, HP, damage, defense, speed, windup, recovery, range
    {'heavy_rammer','Heavy Rammer',12,2,1,70,110,140,1},
    {'hollow_wraith','Hollow Wraith',7,2,0,90,100,120,1},
    {'grave_cantor','Grave Cantor',7,1,0,80,150,160,4},
}
I.roles={['core:enemy/heavy_rammer']='rammer',['core:enemy/hollow_wraith']='wraith',
    ['core:enemy/grave_cantor']='reviver'}
I.tiny={['core:enemy/heavy_rammer']=89,['core:enemy/grave_cantor']=88}
-- The reviewed redirect permits only 416, in BOTH settings choices.
I.shared_onebit={['core:enemy/hollow_wraith']=416}
I.onebit={['core:enemy/heavy_rammer']=129,['core:enemy/hollow_wraith']=416,
    ['core:enemy/grave_cantor']=318}
I.symbols={['core:ui/idea_corpse_effect']=539}
I.abilities={rammer_shove='rammer shove',wraith_phase='wraith phase',wraith_touch='wraith touch',
    cantor_revive='cantor revival',cantor_touch='cantor touch'}
local directions={{0,-1},{1,0},{0,1},{-1,0}}
local corpse_ai={guard=true,spitter=true,skitter=true,spore_herald=true,
    canal_lancer=true,cinder_bellkeeper=true,expansion_regular=true}

function I.revivable(e)
    return corpse_ai[e.ai_id] or e.definition_id=='core:enemy/heavy_rammer'
        or e.definition_id=='core:enemy/hollow_wraith'
end
function I.prune_corpses(state)
    for i=#(state.idea_corpses or {}),1,-1 do
        local c=state.idea_corpses[i]
        local keep=false
        for _,e in ipairs(state.entities)do
            local pending=e.pending_attack
            if e.definition_id=='core:enemy/grave_cantor' and e.hp>0
                and e.position.floor_id==c.floor_id
                and (not e.ai_memory.revival_spent or pending and pending.support
                    and pending.support.kind=='revive' and pending.support.corpse_id==c.id) then
                keep=true;break
            end
        end
        if not keep then table.remove(state.idea_corpses,i)end
    end
end
function I.remember_corpse(state,target)
    if target.controller~='ai' or target.reward_exempt or not I.revivable(target) then return end
    local eligible=false
    for _,e in ipairs(state.entities)do
        if e.definition_id=='core:enemy/grave_cantor' and e.hp>0
            and not e.ai_memory.revival_spent and e.position.floor_id==target.position.floor_id then
            eligible=true;break
        end
    end
    if not eligible then return end
    state.idea_corpses=state.idea_corpses or {}
    if #state.idea_corpses>=32 then return end
    local count=0
    for _,c in ipairs(state.idea_corpses)do if c.floor_id==target.position.floor_id then count=count+1 end end
    if count>=16 then return end
    state.idea_corpses[#state.idea_corpses+1]={id=target.id,definition_id=target.definition_id,
        floor_id=target.position.floor_id,x=target.position.x,y=target.position.y,max_hp=target.max_hp}
end
function I.corpse(state,id)
    for _,c in ipairs(state.idea_corpses or {})do if c.id==id then return c end end
end
function I.commit(state,enemy,intent)
    if intent.support and intent.support.kind=='revive' then
        local c=assert(I.corpse(state,intent.support.corpse_id),'missing committed corpse')
        assert(not c.claimed_by and not enemy.ai_memory.revival_spent,'spent revival')
        c.claimed_by=enemy.id;enemy.ai_memory.revival_spent=true
    end
end

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
    if I.roles[enemy.definition_id]=='reviver' then
        local best,score
        if not enemy.ai_memory.revival_spent then
            for _,c in ipairs(state.idea_corpses or {})do
                local distance=math.abs(c.x-a.x)+math.abs(c.y-a.y)
                if c.floor_id==a.floor_id and not c.claimed_by and distance<=4
                    and h.free(state,enemy.id,c.x,c.y) and h.visible(c.x,c.y)
                    and h.clear_line(state,a,c) and (not score or distance<score) then
                    best,score=c,distance
                end
            end
        end
        if best then return {kind='windup',ability_id='core:ability/cantor_revive',
            cells={{x=best.x,y=best.y}},support={kind='revive',corpse_id=best.id}}end
        if math.abs(dx)+math.abs(dy)==1 then
            return {kind='windup',ability_id='core:ability/cantor_touch',cells={{x=b.x,y=b.y}}}
        end
        return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
    end
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

-- Read-only generation plan: a revival encounter needs a reachable ordinary
-- ally. Reuse a nearby one, or place one original ordinary body beside the
-- caster. Never repaint geometry or occupy reserved markers to force a fit.
function I.revival_company(state,map,caster)
    local a=caster.position
    for _,e in ipairs(state.entities)do
        local b=e.position
        if e~=caster and e.controller=='ai' and e.hp>0 and I.revivable(e)
            and not e.reward_exempt and b.floor_id==a.floor_id
            and math.abs(a.x-b.x)+math.abs(a.y-b.y)<=4
            and Squads.clear(state,a,b)then return true end
    end
    local blocked={}
    local function reserve(p)if p and p.x and p.y then blocked[p.x..','..p.y]=true end end
    for _,e in ipairs(state.entities)do if e.position.floor_id==map.floor_id then reserve(e.position)end end
    for _,list in pairs(map.placements or {})do
        for _,p in ipairs(list)do reserve(p)end
    end
    for _,q in ipairs(map.corridor_quests or {})do for _,p in ipairs(q.cells)do reserve(p)end end
    for _,d in ipairs(directions)do
        local x,y=a.x+d[1],a.y+d[2]
        if map.rows[y] and map.rows[y]:sub(x,x)=='.' and not blocked[x..','..y]
            and math.abs(x-map.start_x)+math.abs(y-map.start_y)>=8
            and (not map.elite or math.abs(x-map.elite.x)+math.abs(y-map.elite.y)>2)then
            return true,{x=x,y=y}
        end
    end
    return false
end

-- At most one idea replacement and, only for an isolated Cantor, one ordinary
-- companion. Existing saved floors are not repopulated; no combat RNG draw.
function I.populate(state,map,replace,spawn)
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
    local definition=I.enemies[math.floor(seed/16)%#I.enemies+1]
    local offset=math.floor(seed/4)%#choices
    local target,companion
    for index=1,#choices do
        local candidate=choices[(offset+index-1)%#choices+1]
        local fits,point=true,nil
        if definition[1]=='grave_cantor' then fits,point=I.revival_company(state,map,candidate)end
        if fits and (not point or spawn)then target,companion=candidate,point;break end
    end
    if not target then return end
    local original=target.definition_id
    replace(target,'core:enemy/'..definition[1])
    for _,p in ipairs(map.placements.enemies) do
        if p.x==target.position.x and p.y==target.position.y then
            p.definition_id=target.definition_id
            break
        end
    end
    if companion then spawn(state,original,companion.x,companion.y)end
end

function I.validate_pending(enemy,pending)
    local revive=pending.support and pending.support.kind=='revive'
    if revive or pending.ability_id=='core:ability/cantor_revive' then
        local id=revive and pending.support.corpse_id
        assert(revive and enemy.definition_id=='core:enemy/grave_cantor'
            and pending.ability_id=='core:ability/cantor_revive'
            and enemy.ai_memory.revival_spent==true
            and type(id)=='number' and id%1==0 and id>=1
            and #pending.target_cells==1 and pending.target_cells[1].floor_id==pending.floor_id,
            'invalid committed revival')
    end
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

function I.validate(state,maps,content)
    local corpses=state.idea_corpses
    assert(corpses==nil or type(corpses)=='table' and #corpses<=32,'invalid corpse store')
    local seen,counts={},{}
    local function actor(id)
        for _,e in ipairs(state.entities)do if e.id==id then return e end end
    end
    for _,e in ipairs(state.entities)do
        if e.definition_id=='core:enemy/grave_cantor' then
            assert(e.ai_memory.revival_spent==nil or e.ai_memory.revival_spent==true,'invalid revival budget')
        end
        if e.revival_of or e.revived_by or e.reward_exempt then
            assert(e.controller=='ai' and I.revivable(e) and e.reward_exempt==true
                and type(e.revival_of)=='number' and e.revival_of%1==0
                and e.revival_of>=1 and e.revival_of<e.id
                and type(e.revived_by)=='number' and e.revived_by%1==0
                and e.revived_by>=1 and e.revived_by<e.id,'invalid revived actor')
        end
    end
    for _,c in ipairs(corpses or {})do
        local map=maps[c.floor_id]
        local def=content.get_enemy(c.definition_id)
        assert(type(c.id)=='number' and c.id%1==0 and c.id>=1 and c.id<state.next_entity_id
            and not seen[c.id] and not actor(c.id) and map and def
            and I.revivable({ai_id=def.ai,definition_id=c.definition_id})
            and type(c.x)=='number' and c.x%1==0 and c.x>=1 and c.x<=map.width
            and type(c.y)=='number' and c.y%1==0 and c.y>=1 and c.y<=map.height
            and type(c.max_hp)=='number' and c.max_hp%1==0 and c.max_hp>=1 and c.max_hp<=1000000,
            'invalid corpse record')
        seen[c.id]=true;counts[c.floor_id]=(counts[c.floor_id] or 0)+1
        assert(counts[c.floor_id]<=16,'corpse floor cap exceeded')
        if c.claimed_by then
            local caster=actor(c.claimed_by)
            local p=caster and caster.pending_attack
            assert(caster and caster.definition_id=='core:enemy/grave_cantor'
                and caster.position.floor_id==c.floor_id and p and p.support
                and p.support.kind=='revive' and p.support.corpse_id==c.id
                and p.target_cells[1].x==c.x and p.target_cells[1].y==c.y,'invalid corpse claim')
        end
    end
    for _,e in ipairs(state.entities)do
        local p=e.pending_attack
        if p and p.support and p.support.kind=='revive' then
            local c=I.corpse(state,p.support.corpse_id)
            assert(c and c.claimed_by==e.id,'missing pending corpse claim')
        end
    end
end

return I
