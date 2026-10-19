-- A Moth's two finite world-time fire pulses. No render clocks or RNG.
local Scheduler=require('src.scheduler')
local Protected=require('src.idea_silt')
local function actor(state,id)
    for _,e in ipairs(state.entities)do if e.id==id then return e end end
end
local E={id='core:enemy/ember_moth',interval=100,ticks=2,cap=8}
local function schedule(state,r)
    local item=Scheduler.enqueue(state,{kind='effect',due=r.next_due,
        actor_id=0,attack_id=r.action_id,floor_id=r.floor_id})
    item.effect_id=item.id;item.idea_ember=true;r.schedule_id=item.id
end
function E.choose(state,enemy,player,h)
    local a,b=enemy.position,player.position
    local distance=math.abs(a.x-b.x)+math.abs(a.y-b.y)
    if (enemy.ai_memory.ember_dives or 0)<2 and distance>=2 and distance<=3
        and h.visible(a.x,a.y) and h.visible(b.x,b.y) and h.clear_line(state,a,b)
        and h.map.rows[b.y]:sub(b.x,b.x)=='.' and not Protected.protected(state,h.map,b.x,b.y)then
        return {kind='windup',ability_id='core:ability/moth_dive',ranged='area',
            cells={{x=b.x,y=b.y}},ember_landing={floor_id=a.floor_id,x=b.x,y=b.y}}
    end
    if distance==1 then
        return {kind='windup',ability_id='core:ability/moth_touch',cells={{x=b.x,y=b.y}}}
    end
    return h.next_step(state,enemy,h.adjacent_goals(state,enemy,player,false)) or {kind='wait'}
end
function E.fits(state,map,c)
    return c.floor_id==state.active_floor_id and map.rows[c.y]
        and map.rows[c.y]:sub(c.x,c.x)=='.' and not Protected.protected(state,map,c.x,c.y)
end
function E.create(state,enemy,pending,map,emit)
    local c=pending.ember_landing
    if not E.fits(state,map,c) or #(state.idea_embers or {})>=E.cap then return false end
    for _,old in ipairs(state.idea_embers or {})do
        if old.floor_id==c.floor_id and old.x==c.x and old.y==c.y then return false end
    end
    state.idea_embers=state.idea_embers or {}
    local r={action_id=pending.action_id,source_id=enemy.id,floor_id=c.floor_id,x=c.x,y=c.y,
        remaining=E.ticks,next_due=state.clock+E.interval,expires=state.clock+E.interval*E.ticks}
    schedule(state,r);state.idea_embers[#state.idea_embers+1]=r
    emit('EmberCreated',{floor_id=c.floor_id,x=c.x,y=c.y,next_due=r.next_due,expires=r.expires})
    return true,r
end
local function erase(state,index)
    local r=state.idea_embers[index]
    for i=#state.schedule,1,-1 do if state.schedule[i].id==r.schedule_id then table.remove(state.schedule,i)end end
    table.remove(state.idea_embers,index)
end
function E.prune(state,map)
    for i=#(state.idea_embers or {}),1,-1 do
        local r=state.idea_embers[i]
        if r.floor_id==state.active_floor_id and (r.expires<state.clock or not E.fits(state,map,r))then erase(state,i)end
    end
end
function E.resolve(state,item,entity_at,emit,damage)
    if not item.idea_ember then return false end
    local index,r
    for i,v in ipairs(state.idea_embers or {})do if v.schedule_id==item.id then index,r=i,v;break end end
    assert(r and r.floor_id==state.active_floor_id and r.next_due==state.clock,'missing scheduled ember')
    local target=entity_at(r.x,r.y)
    emit('EmberPulsed',{floor_id=r.floor_id,x=r.x,y=r.y,
        target_id=target and target.id,remaining=r.remaining-1},r.source_id,r.action_id)
    if target and target.id~=r.source_id then damage(target,r.source_id,r.action_id,1)end
    r.remaining=r.remaining-1
    if r.remaining>0 then r.next_due=state.clock+E.interval;schedule(state,r)
    else table.remove(state.idea_embers,index)end
    return true
end
function E.validate(state,maps)
    assert(state.idea_embers==nil or type(state.idea_embers)=='table' and #state.idea_embers<=E.cap,
        'invalid ember store')
    local schedule_ids,positions,counts={},{},{}
    local scheduled={}
    for _,item in ipairs(state.schedule)do scheduled[#scheduled+1]=item end
    for _,pause in pairs(state.floor_pause or {})do for _,item in ipairs(pause.schedule)do scheduled[#scheduled+1]=item end end
    for _,e in ipairs(state.entities)do if e.definition_id==E.id then
        local n=e.ai_memory.ember_dives or 0
        assert(type(n)=='number' and n%1==0 and n>=0 and n<=2,'invalid moth dive budget')
        local p=e.pending_attack
        if p and (p.ember_landing or p.ability_id=='core:ability/moth_dive')then
            local c=p.ember_landing
            assert(n>=1 and c and p.ability_id=='core:ability/moth_dive' and p.ranged=='area'
                and c.floor_id==p.floor_id and #p.target_cells==1
                and p.target_cells[1].x==c.x and p.target_cells[1].y==c.y
                and math.abs(p.source_x-c.x)+math.abs(p.source_y-c.y)>=2
                and math.abs(p.source_x-c.x)+math.abs(p.source_y-c.y)<=3,'invalid locked moth landing')
        end
    else
        assert(not e.pending_attack or not e.pending_attack.ember_landing,'non-Moth landing')
    end end
    for _,r in ipairs(state.idea_embers or {})do
        local map=maps[r.floor_id];local owner=actor(state,r.source_id)
        local pause=state.floor_pause and state.floor_pause[r.floor_id]
        local clock=pause and pause.suspended_at or state.clock
        assert(map and type(r.x)=='number' and r.x%1==0 and r.x>=1 and r.x<=map.width
            and type(r.y)=='number' and r.y%1==0 and r.y>=1 and r.y<=map.height
            and map.rows[r.y]:sub(r.x,r.x)=='.' and not Protected.protected(state,map,r.x,r.y)
            and type(r.source_id)=='number' and r.source_id%1==0 and r.source_id>=1
            and r.source_id<state.next_entity_id and type(r.action_id)=='number'
            and r.action_id%1==0 and r.action_id>=1 and r.action_id<state.next_action_id
            and type(r.remaining)=='number' and r.remaining%1==0 and r.remaining>=1 and r.remaining<=E.ticks
            and type(r.next_due)=='number' and r.next_due%1==0 and r.next_due>=clock
            and r.next_due<=clock+E.interval and r.expires==r.next_due+(r.remaining-1)*E.interval
            and not schedule_ids[r.schedule_id],'invalid active ember')
        local key=r.floor_id..':'..r.x..','..r.y
        assert(not positions[key],'overlapping ember');positions[key]=true
        counts[r.source_id]=(counts[r.source_id] or 0)+1
        assert(counts[r.source_id]<=2 and (not owner or owner.definition_id==E.id
            and owner.position.floor_id==r.floor_id and counts[r.source_id]<=(owner.ai_memory.ember_dives or 0)),
            'ember dive budget exceeded')
        local found=false
        for _,item in ipairs(scheduled)do
            if item.id==r.schedule_id and item.kind=='effect' and item.idea_ember==true
                and item.effect_id==item.id and item.actor_id==0 and item.attack_id==r.action_id
                and item.floor_id==r.floor_id and item.due==r.next_due then found=true end
        end
        assert(found,'missing ember pulse');schedule_ids[r.schedule_id]=true
    end
    for _,item in ipairs(scheduled)do if item.idea_ember then
        assert(item.idea_ember==true and schedule_ids[item.id],'orphaned ember pulse')
    end end
end
return E
