-- One approved encounter's finite, breakable web lines. No graphics or RNG.
local W={duration=400,delay=50,cap=8}
local caster_id='core:enemy/web_weaver'
local function actor(state,id)
    for _,e in ipairs(state.entities)do if e.id==id then return e end end
end
local function inside(w,x,y)
    return w.ax==w.bx and x==w.ax and y>math.min(w.ay,w.by) and y<math.max(w.ay,w.by)
        or w.ay==w.by and y==w.ay and x>math.min(w.ax,w.bx) and x<math.max(w.ax,w.bx)
end
function W.cells(w,floor)
    assert(type(w.ax)=='number' and w.ax%1==0 and type(w.ay)=='number' and w.ay%1==0
        and type(w.bx)=='number' and w.bx%1==0 and type(w.by)=='number' and w.by%1==0
        and (w.ax==w.bx or w.ay==w.by)
        and math.abs(w.ax-w.bx)+math.abs(w.ay-w.by)==4,'invalid web anchors')
    local dx,dy=(w.bx-w.ax)/4,(w.by-w.ay)/4
    local cells={}
    for i=0,4 do cells[#cells+1]={floor_id=floor,x=w.ax+dx*i,y=w.ay+dy*i}end
    return cells
end
local function protected(state,map,x,y)
    for _,p in ipairs(map.placements and map.placements.stairs or {})do
        if p.x==x and p.y==y then return true end
    end
    for _,e in ipairs(state.entities)do
        if e.position.floor_id==map.floor_id and e.position.x==x and e.position.y==y
            and e.controller~='ai' and e.controller~='player'
            and (e.blocks_movement or e.controller=='exit')then return true end
    end
    return false
end
function W.fits(state,enemy,w,h)
    local cells=W.cells(w,enemy.position.floor_id)
    for index,c in ipairs(cells)do
        if not h.map.rows[c.y] or h.map.rows[c.y]:sub(c.x,c.x)~='.'
            or protected(state,h.map,c.x,c.y) or not h.visible(c.x,c.y)then return false end
        if (index==1 or index==5) and
            (not h.free(state,enemy.id,c.x,c.y)
                or c.x==enemy.position.x and c.y==enemy.position.y)then return false end
    end
    return true,cells
end
function W.choose(state,enemy,player,h)
    local a,b=enemy.position,player.position
    if (enemy.ai_memory.web_casts or 0)<2 and #(state.idea_webs or {})<W.cap
        and math.abs(a.x-b.x)+math.abs(a.y-b.y)<=5 and h.clear_line(state,a,b)then
        for _,w in ipairs({{ax=b.x-2,ay=b.y,bx=b.x+2,by=b.y},
            {ax=b.x,ay=b.y-2,bx=b.x,by=b.y+2}})do
            local fits,cells=W.fits(state,enemy,w,h)
            if fits then
                for _,other in ipairs(state.idea_webs or {})do
                    if other.floor_id==a.floor_id and other.expires>state.clock then
                        for _,c in ipairs(cells)do
                            if inside(other,c.x,c.y) or c.x==other.ax and c.y==other.ay
                                or c.x==other.bx and c.y==other.by then fits=false end
                        end
                    end
                end
            end
            if fits then
                w.kind='web'
                return {kind='windup',ability_id='core:ability/weaver_web',cells=cells,support=w}
            end
        end
    end
    if math.abs(a.x-b.x)+math.abs(a.y-b.y)==1 then
        return {kind='windup',ability_id='core:ability/weaver_bite',cells={{x=b.x,y=b.y}}}
    end
    return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
end
function W.create(state,enemy,pending,h)
    local fits=W.fits(state,enemy,pending.support,h)
    if not fits or #(state.idea_webs or {})>=W.cap then return false end
    local p=pending.support
    for _,old in ipairs(state.idea_webs or {})do
        if old.floor_id==enemy.position.floor_id and old.expires>state.clock then
            for _,c in ipairs(W.cells(p,old.floor_id))do
                if inside(old,c.x,c.y) or c.x==old.ax and c.y==old.ay
                    or c.x==old.bx and c.y==old.by then return false end
            end
        end
    end
    state.idea_webs=state.idea_webs or {}
    local w={id=pending.action_id,source_id=enemy.id,floor_id=enemy.position.floor_id,
        ax=p.ax,ay=p.ay,bx=p.bx,by=p.by,expires=state.clock+W.duration}
    state.idea_webs[#state.idea_webs+1]=w
    return true,w
end
function W.prune(state,map)
    if not state.idea_webs then return end
    for i=#state.idea_webs,1,-1 do
        local w=state.idea_webs[i];local source=actor(state,w.source_id)
        local remove=not source or source.hp<=0
        if w.floor_id==state.active_floor_id then
            remove=remove or w.expires<=state.clock
            for _,c in ipairs(W.cells(w,w.floor_id))do
                if not map.rows[c.y] or map.rows[c.y]:sub(c.x,c.x)~='.'
                    or protected(state,map,c.x,c.y)then remove=true end
            end
        end
        if remove then table.remove(state.idea_webs,i)end
    end
end
function W.on_event(state,event,emit,delay)
    if not state.idea_webs then return end
    for i=#state.idea_webs,1,-1 do
        local w=state.idea_webs[i]
        if w.floor_id==event.floor_id and w.expires>event.time then
            if event.kind=='TileEntered' and event.source_id~=w.source_id and inside(w,event.x,event.y)then
                local e=actor(state,event.source_id)
                if e and e.hp and e.hp>0 then
                    table.remove(state.idea_webs,i);delay(e,W.delay)
                    emit('WebCrossed',{web_id=w.id,target_id=e.id,delay=W.delay,
                        floor_id=w.floor_id,x=event.x,y=event.y},w.source_id)
                end
            elseif event.kind=='AttackPerformed' and event.raw_damage>0 then
                for _,c in ipairs(event.web_cut_cells or {})do
                    if c.x==w.ax and c.y==w.ay or c.x==w.bx and c.y==w.by then
                        table.remove(state.idea_webs,i)
                        emit('WebAnchorCut',{web_id=w.id,floor_id=w.floor_id,x=c.x,y=c.y},event.source_id)
                        break
                    end
                end
            end
        end
    end
end
function W.validate_pending(enemy,p)
    if p.support and p.support.kind=='web' or p.ability_id=='core:ability/weaver_web' then
        assert(enemy.definition_id==caster_id and p.support and p.support.kind=='web'
            and p.ability_id=='core:ability/weaver_web' and enemy.ai_memory.web_casts>=1,
            'invalid committed web')
        local cells=W.cells(p.support,p.floor_id)
        assert(#p.target_cells==#cells,'invalid committed web cells')
        for i,c in ipairs(cells)do
            local t=p.target_cells[i]
            assert(t.floor_id==c.floor_id and t.x==c.x and t.y==c.y,'invalid committed web footprint')
        end
    end
end
function W.validate(state,maps)
    assert(state.idea_webs==nil or type(state.idea_webs)=='table' and #state.idea_webs<=W.cap,'invalid web store')
    local seen,counts={},{}
    for _,e in ipairs(state.entities)do if e.definition_id==caster_id then
        local n=e.ai_memory.web_casts or 0
        assert(type(n)=='number' and n%1==0 and n>=0 and n<=2,'invalid web budget')
    end end
    for _,w in ipairs(state.idea_webs or {})do
        local source=actor(state,w.source_id);local map=maps[w.floor_id]
        local paused=state.floor_pause and state.floor_pause[w.floor_id]
        local clock=paused and paused.suspended_at or state.clock
        assert(type(w.id)=='number' and w.id%1==0 and w.id>=1 and w.id<state.next_action_id
            and not seen[w.id] and source and source.definition_id==caster_id
            and source.position.floor_id==w.floor_id and map
            and type(w.expires)=='number' and w.expires%1==0 and w.expires>clock
            and w.expires<=clock+W.duration,'invalid active web')
        seen[w.id]=true;counts[w.source_id]=(counts[w.source_id] or 0)+1
        assert(counts[w.source_id]<=source.ai_memory.web_casts and counts[w.source_id]<=2,'web budget exceeded')
        for _,c in ipairs(W.cells(w,w.floor_id))do
            assert(c.x>=1 and c.x<=map.width and c.y>=1 and c.y<=map.height
                and map.rows[c.y]:sub(c.x,c.x)=='.','invalid saved web cell')
        end
    end
end
return W
