-- One temporary, owned block. Collision is an entity overlay; source terrain
-- and published tile images are never rewritten.
local Terrain=require('src.terrain')
local Scheduler=require('src.scheduler')
local G={id='core:actor/geomancer',controller='geomancer_block',
    block_id='core:object/geomancer_bulwark',duration=300,tiny=100,onebit=466}
G.definition={id=G.id,name='Geomancer',
    description='Heavy stone bolt, one temporary bulwark, grounded Dash',
    hp=24,damage=5,slash_damage=0,defense=1,speed=85,
    crit_chance_ppm=0,crit_damage_percent=150,
    costs={move=100,basic_attack=120,sweeping_slash=130,dash=130},
    dash_distance=2,basic_id='core:ability/geomancer_bolt',special_id='core:ability/geomancer_raise',
    basic_name='Stone Bolt',special_name='Raise Bulwark',dash_name='Grounded Dash',
    ui_controls='F Stone Bolt · R Bulwark · X Dash'}
local directions={{0,-1},{1,0},{0,1},{-1,0}}
function G.preview(state,map,x,y)
    local player
    for _,e in ipairs(state.entities)do if e.id==state.player_id then player=e;break end end
    local p=assert(player).position
    if math.abs(x-p.x)+math.abs(y-p.y)~=1 or not map.rows[y] or map.rows[y]:sub(x,x)~='.'then
        return false,'choose adjacent plain floor'
    end
    for _,stair in ipairs(map.placements and map.placements.stairs or {})do
        if stair.x==x and stair.y==y then return false,'stairs must stay clear'end
    end
    local blocked={}
    local function key(a,b)return (b-1)*map.width+a end
    for _,e in ipairs(state.entities)do
        local q=e.position
        if q.floor_id==map.floor_id and e.controller~=G.controller then
            if q.x==x and q.y==y and e.blocks_movement then return false,'cell occupied'end
            -- Doors/gates can open later. Check their eventual access too;
            -- mobile actors cannot make a permanent connectivity decision.
            if e.blocks_movement and e.controller~='ai' and e.controller~='player'
                and e.controller~='door' and e.controller~='review_gate' and e.controller~='exit'then
                blocked[key(q.x,q.y)]=true
            end
        end
    end
    local function reachable(candidate)
        local queue={{x=p.x,y=p.y}};local seen={[key(p.x,p.y)]=true};local head=1
        while head<=#queue do
            local at=queue[head];head=head+1
            for _,d in ipairs(directions)do
                local a,b=at.x+d[1],at.y+d[2];local k=key(a,b)
                local tile=Terrain.at(map,a,b)
                if tile and tile.walkable and not blocked[k] and not seen[k] and k~=candidate then
                    seen[k]=true;queue[#queue+1]={x=a,y=b}
                end
            end
        end
        return #queue
    end
    if reachable(key(x,y))~=reachable(nil)-1 then return false,'cannot seal an accessible route'end
    return true
end
local function remove_timer(list,id)
    for i=#list,1,-1 do if list[i].id==id then table.remove(list,i)end end
end
local function remove_owned(state)
    for i=#state.entities,1,-1 do
        local e=state.entities[i]
        if e.controller==G.controller then
            remove_timer(state.schedule,e.expiry_schedule_id)
            for _,pause in pairs(state.floor_pause or {})do remove_timer(pause.schedule,e.expiry_schedule_id)end
            table.remove(state.entities,i)
        end
    end
end
function G.place(state,owner,x,y,action_id,emit)
    remove_owned(state)
    local id=state.next_entity_id;state.next_entity_id=id+1
    local e={id=id,definition_id=G.block_id,controller=G.controller,owner_id=owner.id,
        position={floor_id=owner.position.floor_id,x=x,y=y},expires=state.clock+G.duration,
        blocks_movement=true,blocks_vision=true,blocks_projectiles=true,boons={}}
    local item=Scheduler.enqueue(state,{kind='effect',due=e.expires,actor_id=0,
        attack_id=action_id,floor_id=e.position.floor_id})
    item.effect_id=item.id;item.idea_geomancer=true;e.expiry_schedule_id=item.id
    state.entities[#state.entities+1]=e
    emit('GeomancerBlockRaised',{target_id=id,floor_id=e.position.floor_id,x=x,y=y,expires=e.expires})
    return e
end
function G.expire(state,item,emit)
    assert(item.idea_geomancer)
    for i,e in ipairs(state.entities)do
        if e.controller==G.controller and e.expiry_schedule_id==item.id then
            assert(e.expires==state.clock and e.position.floor_id==state.active_floor_id,'mistimed bulwark expiry')
            table.remove(state.entities,i)
            emit('GeomancerBlockExpired',{target_id=e.id,floor_id=e.position.floor_id,
                x=e.position.x,y=e.position.y},e.owner_id,item.attack_id)
            return
        end
    end
    error('orphaned bulwark expiry')
end
function G.validate(state,maps)
    local scheduled={};for _,item in ipairs(state.schedule)do scheduled[#scheduled+1]=item end
    for _,pause in pairs(state.floor_pause or {})do for _,item in ipairs(pause.schedule)do scheduled[#scheduled+1]=item end end
    local count,ids=0,{}
    for _,e in ipairs(state.entities)do if e.controller==G.controller then
        count=count+1
        local owner,map,pause
        for _,a in ipairs(state.entities)do if a.id==e.owner_id then owner=a;break end end
        map=maps[e.position.floor_id];pause=state.floor_pause and state.floor_pause[e.position.floor_id]
        local clock=pause and pause.suspended_at or state.clock
        assert(count<=1 and owner and owner.id==state.player_id and owner.definition_id==G.id
            and e.definition_id==G.block_id and e.hp==nil and e.blocks_movement==true
            and e.blocks_vision==true and e.blocks_projectiles==true
            and map and map.rows[e.position.y] and map.rows[e.position.y]:sub(e.position.x,e.position.x)=='.'
            and type(e.expires)=='number' and e.expires%1==0 and e.expires>=clock
            and e.expires<=clock+G.duration,'invalid owned bulwark')
        local found=false
        for _,item in ipairs(scheduled)do
            if item.id==e.expiry_schedule_id and item.kind=='effect' and item.actor_id==0
                and item.idea_geomancer==true and item.effect_id==item.id
                and item.floor_id==e.position.floor_id and item.due==e.expires then found=true end
        end
        assert(found,'missing bulwark expiry');ids[e.expiry_schedule_id]=true
    end end
    for _,item in ipairs(scheduled)do if item.idea_geomancer then
        assert(item.idea_geomancer==true and ids[item.id],'orphaned bulwark work')
    end end
end
return G
