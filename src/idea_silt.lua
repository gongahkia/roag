-- Finite bog overlay left by an announced Worm charge. Underlying row bytes
-- are never rewritten, so expiry cannot erase authored art or terrain.
local Terrain=require('src.terrain')
local T={id='core:enemy/silt_worm',duration=300,cap=12}
local function actor(state,id)
    for _,e in ipairs(state.entities)do if e.id==id then return e end end
end
function T.protected(state,map,x,y)
    for _,s in ipairs(map.placements and map.placements.stairs or {})do
        if s.x==x and s.y==y then return true end
    end
    for _,e in ipairs(state.entities)do
        local p=e.position
        if p.floor_id==map.floor_id and p.x==x and p.y==y
            and e.controller~='player' and e.controller~='ai'
            and (e.blocks_movement or e.controller=='exit')then return true end
    end
    return false
end
function T.choose(state,enemy,player,h)
    local a,b=enemy.position,player.position
    local dx,dy=b.x-a.x,b.y-a.y;local distance=math.abs(dx)+math.abs(dy)
    if (enemy.ai_memory.silt_charges or 0)<2 and distance>=2 and distance<=3
        and (dx==0 or dy==0) and h.visible(a.x,a.y) and h.clear_line(state,a,b)then
        local sx,sy=dx==0 and 0 or dx/math.abs(dx),dy==0 and 0 or dy/math.abs(dy)
        local cells,valid={},true
        for i=1,distance do
            local c={x=a.x+sx*i,y=a.y+sy*i}
            local tile=Terrain.at(h.map,c.x,c.y)
            if not tile or not tile.walkable or T.protected(state,h.map,c.x,c.y)
                or not h.visible(c.x,c.y)then valid=false end
            cells[#cells+1]=c
        end
        if valid then return {kind='windup',ability_id='core:ability/worm_charge',cells=cells,
            ranged=true,advance={dx=sx,dy=sy,max_steps=2}}end
    end
    if distance==1 then
        return {kind='windup',ability_id='core:ability/worm_touch',cells={{x=b.x,y=b.y}}}
    end
    return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
end
function T.at(state,x,y)
    for _,r in ipairs(state.idea_silt or {})do
        if r.floor_id==state.active_floor_id and r.expires>state.clock and r.x==x and r.y==y then return r end
    end
end
function T.move_extra(state,kind,cells)
    local extra=0
    for _,c in ipairs(cells or {})do
        if c.status=='open' and T.at(state,c.x,c.y)then extra=extra+(kind=='dash' and 20 or 40)end
    end
    return extra
end
function T.prune(state,map)
    for i=#(state.idea_silt or {}),1,-1 do
        local r=state.idea_silt[i]
        if r.floor_id==state.active_floor_id and (r.expires<=state.clock
            or not map.rows[r.y] or map.rows[r.y]:sub(r.x,r.x)~='.'
            or T.protected(state,map,r.x,r.y))then table.remove(state.idea_silt,i)end
    end
end
function T.leave(state,enemy,pending,from_x,from_y,path,map,emit)
    if pending.ability_id~='core:ability/worm_charge' then return end
    T.prune(state,map)
    local x,y=from_x,from_y
    for i=1,math.min(2,#path)do
        if #(state.idea_silt or {})<T.cap and map.rows[y]:sub(x,x)=='.'
            and not T.protected(state,map,x,y) and not T.at(state,x,y)then
            state.idea_silt=state.idea_silt or {}
            local r={action_id=pending.action_id,source_id=enemy.id,floor_id=map.floor_id,
                x=x,y=y,expires=state.clock+T.duration}
            state.idea_silt[#state.idea_silt+1]=r
            emit('SiltTrailCreated',{floor_id=r.floor_id,x=x,y=y,expires=r.expires})
        end
        x,y=path[i].x,path[i].y
    end
end
function T.validate(state,maps)
    assert(state.idea_silt==nil or type(state.idea_silt)=='table' and #state.idea_silt<=T.cap,
        'invalid silt trail store')
    local positions,actions,owners={},{},{}
    for _,e in ipairs(state.entities)do if e.definition_id==T.id then
        local n=e.ai_memory.silt_charges or 0
        assert(type(n)=='number' and n%1==0 and n>=0 and n<=2,'invalid silt charge budget')
        local p=e.pending_attack
        if p and p.ability_id=='core:ability/worm_charge'then
            local a=p.advance
            assert(n>=1 and a and a.max_steps==2 and p.ranged==true
                and #p.target_cells>=2 and #p.target_cells<=3,'invalid locked silt charge')
            for i,c in ipairs(p.target_cells)do
                assert(c.x==p.source_x+a.dx*i and c.y==p.source_y+a.dy*i,'unlocked silt lane')
            end
        end
    end end
    for _,r in ipairs(state.idea_silt or {})do
        local map=maps[r.floor_id];local owner=actor(state,r.source_id)
        local paused=state.floor_pause and state.floor_pause[r.floor_id]
        local clock=paused and paused.suspended_at or state.clock
        assert(map and type(r.x)=='number' and r.x%1==0 and r.x>=1 and r.x<=map.width
            and type(r.y)=='number' and r.y%1==0 and r.y>=1 and r.y<=map.height
            and map.rows[r.y]:sub(r.x,r.x)=='.' and not T.protected(state,map,r.x,r.y)
            and type(r.source_id)=='number' and r.source_id%1==0
            and r.source_id>=1 and r.source_id<state.next_entity_id
            and (not owner or owner.definition_id==T.id and owner.position.floor_id==r.floor_id)
            and type(r.action_id)=='number' and r.action_id%1==0
            and r.action_id>=1 and r.action_id<state.next_action_id
            and type(r.expires)=='number' and r.expires%1==0 and r.expires>clock
            and r.expires<=clock+T.duration,'invalid saved silt trail')
        local key=r.floor_id..':'..r.x..','..r.y
        assert(not positions[key],'overlapping silt trail');positions[key]=true
        actions[r.action_id]=(actions[r.action_id] or 0)+1
        owners[r.source_id]=(owners[r.source_id] or 0)+1
        assert(actions[r.action_id]<=2 and owners[r.source_id]<=4
            and (not owner or owners[r.source_id]<=2*(owner.ai_memory.silt_charges or 0)),
            'silt trail budget exceeded')
    end
end
return T
