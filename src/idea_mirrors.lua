-- One finite, directional reflector. Paths are transformed at commitment,
-- never at impact, and retain their original shooter and remaining range.
local M={id='core:enemy/mirror_hexer',duration=400,cap=8}
local function actor(state,id)
    for _,e in ipairs(state.entities)do if e.id==id then return e end end
end
local function occupant(state,source,x,y)
    for _,e in ipairs(state.entities)do
        local p=e.position
        if e.id~=source.id and e.hp and e.hp>0 and p.floor_id==source.position.floor_id
            and p.x==x and p.y==y then return e end
    end
end
local function protected(state,map,x,y)
    for _,s in ipairs(map.placements and map.placements.stairs or {})do
        if s.x==x and s.y==y then return true end
    end
    for _,e in ipairs(state.entities)do
        if e.controller=='exit' and e.position.floor_id==map.floor_id
            and e.position.x==x and e.position.y==y then return true end
    end
    return false
end
function M.fits(state,enemy,p,h)
    return h.map.rows[p.y] and h.map.rows[p.y]:sub(p.x,p.x)=='.'
        and math.abs(enemy.position.x-p.x)+math.abs(enemy.position.y-p.y)==1
        and h.free(state,enemy.id,p.x,p.y) and h.visible(p.x,p.y)
        and h.visible(enemy.position.x,enemy.position.y)
        and not protected(state,h.map,p.x,p.y)
end
function M.choose(state,enemy,player,h)
    local a,b=enemy.position,player.position
    local dx,dy=b.x-a.x,b.y-a.y;local distance=math.abs(dx)+math.abs(dy)
    local active=false
    for _,r in ipairs(state.idea_mirrors or {})do
        if r.source_id==enemy.id and r.expires>state.clock then active=true end
    end
    if not active and (enemy.ai_memory.mirror_casts or 0)<2
        and #(state.idea_mirrors or {})<M.cap and distance>=2 and distance<=4
        and (dx==0 or dy==0) and h.clear_line(state,a,b)then
        local sx,sy=dx==0 and 0 or dx/math.abs(dx),dy==0 and 0 or dy/math.abs(dy)
        local r={kind='mirror',x=a.x+sx,y=a.y+sy,in_dx=-sx,in_dy=-sy}
        if M.fits(state,enemy,r,h)then
            return {kind='windup',ability_id='core:ability/hexer_mirror',
                cells={{x=r.x,y=r.y}},support=r}
        end
    end
    if distance>=1 and distance<=4 and (dx==0 or dy==0) and h.clear_line(state,a,b)then
        local sx,sy=dx==0 and 0 or dx/math.abs(dx),dy==0 and 0 or dy/math.abs(dy)
        local cells={}
        for i=1,distance do cells[#cells+1]={x=a.x+sx*i,y=a.y+sy*i}end
        return {kind='windup',ability_id='core:ability/hexer_bolt',cells=cells,ranged=true}
    end
    return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
end
function M.create(state,enemy,pending,h)
    local p=pending.support
    if not M.fits(state,enemy,p,h) or #(state.idea_mirrors or {})>=M.cap then return false end
    for _,old in ipairs(state.idea_mirrors or {})do
        if old.source_id==enemy.id or old.floor_id==enemy.position.floor_id
            and old.x==p.x and old.y==p.y then return false end
    end
    local r={id=pending.action_id,source_id=enemy.id,floor_id=enemy.position.floor_id,
        x=p.x,y=p.y,source_x=pending.source_x,source_y=pending.source_y,
        in_dx=p.in_dx,in_dy=p.in_dy,expires=state.clock+M.duration}
    state.idea_mirrors=state.idea_mirrors or {}
    state.idea_mirrors[#state.idea_mirrors+1]=r
    return true,r
end
function M.route(state,source,cells,ranged,known_only)
    if #(state.idea_mirrors or {})==0 or ranged~=true and ranged~='piercing_line'
        or #cells<2 then return cells end
    local p=source.position;local dx,dy=cells[1].x-p.x,cells[1].y-p.y
    if math.abs(dx)+math.abs(dy)~=1 then return cells end
    for i,c in ipairs(cells)do
        if c.x~=p.x+dx*i or c.y~=p.y+dy*i then return cells end
    end
    local Content=require('src.content');local map=Content.map_for(state)
    for i,c in ipairs(cells)do
        local contact=ranged==true and occupant(state,source,c.x,c.y)
        local known_contact=contact and (not known_only or
            require('src.visibility').is_visible(state,contact.position.floor_id,c.x,c.y))
        if not Content.projectile_passable(map,c.x,c.y,state)
            or known_contact then return cells end
        for _,r in ipairs(state.idea_mirrors)do
            local owner=actor(state,r.source_id)
            if i<#cells and r.floor_id==p.floor_id and r.expires>state.clock
                and r.x==c.x and r.y==c.y and r.in_dx==dx and r.in_dy==dy
                and owner and owner.hp>0 then
                local result={}
                for j=1,i do result[#result+1]=cells[j]end
                local ox,oy=-dy,dx
                for j=1,#cells-i do result[#result+1]={floor_id=p.floor_id,x=r.x+ox*j,y=r.y+oy*j}end
                return result,{id=r.id,source_id=r.source_id,x=r.x,y=r.y,
                    in_dx=dx,in_dy=dy,out_dx=ox,out_dy=oy,step=i}
            end
        end
    end
    return cells
end
function M.consume(state,reflection)
    if not reflection then return end
    for i,r in ipairs(state.idea_mirrors or {})do if r.id==reflection.id then
        table.remove(state.idea_mirrors,i);return true
    end end
    error('committed reflector disappeared before activation')
end
function M.prune(state,map)
    for i=#(state.idea_mirrors or {}),1,-1 do
        local r=state.idea_mirrors[i];local owner=actor(state,r.source_id)
        if not owner or owner.hp<=0 or r.floor_id==state.active_floor_id and
            (r.expires<=state.clock or not map.rows[r.y]
                or map.rows[r.y]:sub(r.x,r.x)~='.' or protected(state,map,r.x,r.y))then
            table.remove(state.idea_mirrors,i)
        end
    end
end
local function direction(dx,dy)
    return type(dx)=='number' and dx%1==0 and type(dy)=='number' and dy%1==0
        and math.abs(dx)+math.abs(dy)==1
end
function M.validate(state,maps)
    local seen,owners,positions={},{},{}
    assert(state.idea_mirrors==nil or type(state.idea_mirrors)=='table'
        and #state.idea_mirrors<=M.cap,'invalid mirror store')
    for _,e in ipairs(state.entities)do
        if e.definition_id==M.id then
            local n=e.ai_memory.mirror_casts or 0
            assert(type(n)=='number' and n%1==0 and n>=0 and n<=2,'invalid mirror budget')
        end
        local p=e.pending_attack
        if p and (p.ability_id=='core:ability/hexer_mirror' or p.support and p.support.kind=='mirror')then
            local r=p.support
            assert(e.definition_id==M.id and p.ability_id=='core:ability/hexer_mirror'
                and r and r.kind=='mirror' and direction(r.in_dx,r.in_dy)
                and e.ai_memory.mirror_casts and e.ai_memory.mirror_casts>=1
                and r.x==p.source_x-r.in_dx and r.y==p.source_y-r.in_dy
                and #p.target_cells==1 and p.target_cells[1].x==r.x and p.target_cells[1].y==r.y,
                'invalid committed mirror')
        end
        if p and p.reflection then
            local r=p.reflection;local n=r.step
            assert(type(r.id)=='number' and r.id%1==0 and r.id>=1 and r.id<state.next_action_id
                and type(r.source_id)=='number' and r.source_id%1==0
                and r.source_id>=1 and r.source_id<state.next_entity_id
                and direction(r.in_dx,r.in_dy) and r.out_dx==-r.in_dy and r.out_dy==r.in_dx
                and (p.ranged==true or p.ranged=='piercing_line')
                and type(n)=='number' and n%1==0 and n>=1 and n<#p.target_cells,
                'invalid locked reflection')
            for i,c in ipairs(p.target_cells)do
                local x,y
                if i<=n then x,y=p.source_x+r.in_dx*i,p.source_y+r.in_dy*i
                else x,y=r.x+r.out_dx*(i-n),r.y+r.out_dy*(i-n)end
                assert(c.x==x and c.y==y and (i~=n or c.x==r.x and c.y==r.y),
                    'unlocked reflected path')
            end
        end
    end
    for _,r in ipairs(state.idea_mirrors or {})do
        local owner=actor(state,r.source_id);local map=maps[r.floor_id]
        local paused=state.floor_pause and state.floor_pause[r.floor_id]
        local clock=paused and paused.suspended_at or state.clock
        local key=r.floor_id..':'..r.x..','..r.y
        assert(type(r.id)=='number' and r.id%1==0 and r.id>=1 and r.id<state.next_action_id
            and not seen[r.id] and not positions[key] and not owners[r.source_id]
            and owner and owner.hp>0 and owner.definition_id==M.id
            and owner.position.floor_id==r.floor_id and (owner.ai_memory.mirror_casts or 0)>=1
            and map and type(r.x)=='number' and r.x%1==0 and r.x>=1 and r.x<=map.width
            and type(r.y)=='number' and r.y%1==0 and r.y>=1 and r.y<=map.height
            and map.rows[r.y]:sub(r.x,r.x)=='.' and direction(r.in_dx,r.in_dy)
            and r.source_x==r.x+r.in_dx and r.source_y==r.y+r.in_dy
            and r.source_x>=1 and r.source_x<=map.width and r.source_y>=1 and r.source_y<=map.height
            and type(r.expires)=='number' and r.expires%1==0
            and r.expires>clock and r.expires<=clock+M.duration,'invalid active mirror')
        seen[r.id]=true;owners[r.source_id]=true;positions[key]=true
    end
end
return M
