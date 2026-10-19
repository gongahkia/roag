-- Optional room champion. Four separately committed quarter-turn strikes,
-- then ordinary melee. Read-only aiming never progresses the sequence.
local S={id='core:enemy/rotating_sentinel'}
S.directions={{0,-1},{1,0},{0,1},{-1,0}}
function S.cells(map,p,facing)
    local d=assert(S.directions[facing],'invalid sentinel facing')
    local cells={}
    for side=-1,1 do
        local x,y=p.x+d[1]-d[2]*side,p.y+d[2]+d[1]*side
        if x>=1 and x<=map.width and y>=1 and y<=map.height
            and map.rows[y]:sub(x,x)~='#' then
            cells[#cells+1]={floor_id=p.floor_id,x=x,y=y}
        end
    end
    return cells
end
function S.choose(state,enemy,player,h)
    local m,a,b=enemy.ai_memory,enemy.position,player.position
    local dx,dy=b.x-a.x,b.y-a.y
    local count=m.sentinel_strikes or 0
    if not m.sentinel_finished and (count>0 or
        math.abs(dx)+math.abs(dy)<=4 and h.visible(a.x,a.y) and h.clear_line(state,a,b))then
        local facing
        if count>0 then facing=m.sentinel_facing%4+1
        elseif math.abs(dx)>=math.abs(dy)then facing=dx>=0 and 2 or 4
        else facing=dy>=0 and 3 or 1 end
        return {kind='windup',ability_id='core:ability/sentinel_quarter',
            cells=S.cells(h.map,a,facing),sentinel_facing=facing}
    end
    if math.abs(dx)+math.abs(dy)==1 then
        return {kind='windup',ability_id='core:ability/sentinel_touch',cells={{x=b.x,y=b.y}}}
    end
    return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
end
function S.commit(enemy,intent)
    if not intent.sentinel_facing then return end
    local m=enemy.ai_memory
    assert(enemy.definition_id==S.id and not m.sentinel_finished
        and (m.sentinel_strikes or 0)<4,'spent sentinel sequence')
    m.sentinel_facing=intent.sentinel_facing
    m.sentinel_strikes=(m.sentinel_strikes or 0)+1
    if m.sentinel_strikes==4 then m.sentinel_finished=true end
end
function S.interrupt(enemy)
    if enemy.definition_id==S.id and (enemy.ai_memory.sentinel_strikes or 0)>0 then
        enemy.ai_memory.sentinel_finished=true
    end
end
function S.optional_room(map,p)
    local critical={}
    for _,id in ipairs(map.graph and map.graph.critical or {})do critical[id]=true end
    for _,r in ipairs(map.rooms or {})do
        if not critical[r.id] and p.x>r.x and p.x<r.x+r.width-1
            and p.y>r.y and p.y<r.y+r.height-1 then return r.id end
    end
end
function S.validate(state,maps)
    for _,e in ipairs(state.entities)do if e.definition_id==S.id then
        local m=e.ai_memory;local n=m.sentinel_strikes or 0
        assert(type(n)=='number' and n%1==0 and n>=0 and n<=4
            and (m.sentinel_finished==nil or m.sentinel_finished==true)
            and (n<4 or m.sentinel_finished),'invalid sentinel sequence')
        if n>0 then
            assert(type(m.sentinel_facing)=='number' and m.sentinel_facing%1==0
                and m.sentinel_facing>=1 and m.sentinel_facing<=4,'invalid sentinel facing')
        else assert(m.sentinel_facing==nil and not m.sentinel_finished,'unstarted sentinel sequence')end
        local p=e.pending_attack
        if p and (p.ability_id=='core:ability/sentinel_quarter' or p.sentinel_facing)then
            assert(n>0 and p.sentinel_facing==m.sentinel_facing
                and p.ability_id=='core:ability/sentinel_quarter' and not p.support,
                'invalid sentinel commitment')
            local cells=S.cells(assert(maps[p.floor_id]),
                {floor_id=p.floor_id,x=p.source_x,y=p.source_y},p.sentinel_facing)
            assert(#cells==#p.target_cells,'invalid sentinel footprint')
            for i,c in ipairs(cells)do local actual=p.target_cells[i]
                assert(c.floor_id==actual.floor_id and c.x==actual.x and c.y==actual.y,
                    'unlocked sentinel footprint')
            end
        end
    end end
end
return S
