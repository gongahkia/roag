-- Approved sprite-review room objects. All mutable state is plain snapshot data.
local Content=require('src.content')
local RNG=require('src.rng')
local F={}

local function key(x,y)return x..','..y end
local function distance(a,b)return math.abs(a.x-b.x)+math.abs(a.y-b.y) end
local directions={{0,-1},{1,0},{0,1},{-1,0}}
local function occupied(state,floor_id,x,y,exclude_id)
    for _,e in ipairs(state.entities) do
        local p=e.position
        if e.id~=exclude_id and e.blocks_movement and p and p.floor_id==floor_id
            and p.x==x and p.y==y then return true end
    end
    return false
end
local function room_at(map,x,y)
    for _,room in ipairs(map.rooms or {}) do
        if x>=room.x and x<room.x+room.width
            and y>=room.y and y<room.y+room.height then return room end
    end
end
local function safe(state,map,x,y,taken)
    if taken[key(x,y)] or not Content.walkable(map,x,y)
        or map.rows[y]:sub(x,x)~='.'
        or occupied(state,map.floor_id,x,y)
        or distance({x=x,y=y},{x=map.start_x,y=map.start_y})<4
        or distance({x=x,y=y},map.elite)<3 then return false end
    for _,place in ipairs(map.placements.chests or {}) do
        if distance({x=x,y=y},place)<2 then return false end
    end
    for _,place in ipairs(map.placements.doors or {}) do
        if distance({x=x,y=y},place)<2 then return false end
    end
    for _,place in ipairs(map.placements.secrets or {}) do
        if distance({x=x,y=y},place)<2 then return false end
    end
    return true
end
local function ordered(map,seed,kind)
    local result={}
    for _,p in ipairs(map.editable_cells or {}) do
        if p.kind=='room' or p.kind=='corridor' or p.kind=='alcove'
            or p.kind=='parallel_bank' or p.kind=='courtyard_node'
            or p.kind=='star_node' or p.kind=='forge_bay' then
            result[#result+1]={x=p.x,y=p.y,kind=p.kind,
                score=RNG.derive(seed,map.floor_id..':'..kind..':'..key(p.x,p.y))}
        end
    end
    table.sort(result,function(a,b)
        if a.score~=b.score then return a.score<b.score end
        if a.y~=b.y then return a.y<b.y end
        return a.x<b.x
    end)
    return result
end
local function open_neighbors(map,x,y)
    local count=0
    for _,d in ipairs(directions) do
        if Content.walkable(map,x+d[1],y+d[2]) then count=count+1 end
    end
    return count
end
local function route_clear(map,bx,by,blocking)
    local queue,seen={{x=map.start_x,y=map.start_y}},
        {[key(map.start_x,map.start_y)]=true}
    local head=1
    while head<=#queue do
        local p=queue[head];head=head+1
        for _,d in ipairs(directions) do
            local x,y=p.x+d[1],p.y+d[2]
            local k=key(x,y)
            if not seen[k] and not (x==bx and y==by)
                and not (blocking and blocking[k])
                and Content.walkable(map,x,y) then
                seen[k]=true;queue[#queue+1]={x=x,y=y}
            end
        end
    end
    if not seen[key(map.elite.x,map.elite.y)] then return false end
    for _,room in ipairs(map.rooms) do
        local found=false
        for y=room.y,room.y+room.height-1 do
            for x=room.x,room.x+room.width-1 do
                if seen[key(x,y)] then found=true;break end
            end
            if found then break end
        end
        if not found then return false end
    end
    return true
end
local function add_entity(state,map,kind,p,extra)
    local e={id=state.next_entity_id,definition_id='core:object/review_'..kind,
        controller='review_'..kind,faction='neutral',
        position={floor_id=map.floor_id,x=p.x,y=p.y},
        blocks_movement=true,blocks_vision=kind=='pillar' or kind=='gate',
        blocks_projectiles=kind=='pillar' or kind=='gate'}
    for k,v in pairs(extra or {}) do e[k]=v end
    state.next_entity_id=e.id+1;state.entities[#state.entities+1]=e
    return e
end
local function add_feature(state,map,kind,p,extra)
    local f={id=map.floor_id..':'..kind..':'..key(p.x,p.y),kind=kind,
        floor_id=map.floor_id,x=p.x,y=p.y}
    for k,v in pairs(extra or {}) do f[k]=v end
    state.review_features[#state.review_features+1]=f
    return f
end

function F.populate(state,map)
    if not map or not map.rooms or #map.rooms==0 then return end
    local taken,blocking={},{}
    local function choose(kind,condition)
        for _,p in ipairs(ordered(map,state.run_seed,kind)) do
            if safe(state,map,p.x,p.y,taken)
                and (not condition or condition(p)) then
                taken[key(p.x,p.y)]=true
                return p
            end
        end
    end
    local vault=choose('vault',function(p)
        local room=room_at(map,p.x,p.y)
        return room and room.role=='treasure' and open_neighbors(map,p.x,p.y)>=3
            and route_clear(map,p.x,p.y,blocking)
    end)
    if not vault then
        vault=choose('vault_landmark',function(p)
            local room=room_at(map,p.x,p.y)
            return room and room.role=='landmark'
                and open_neighbors(map,p.x,p.y)>=3
                and route_clear(map,p.x,p.y,blocking)
        end)
    end
    if vault then
        add_entity(state,map,'vault',vault,{opened=false})
        local key_pos=choose('key',function(p)
            return distance(p,vault)>=8 and open_neighbors(map,p.x,p.y)>=2
        end)
        if key_pos then
            add_feature(state,map,'key',key_pos,{collected=false})
            blocking[key(vault.x,vault.y)]=true
        else
            local e=state.entities[#state.entities]
            if e and e.controller=='review_vault' then
                table.remove(state.entities);state.next_entity_id=e.id
            end
            taken[key(vault.x,vault.y)]=nil
        end
    end
    local gate=choose('gate',function(p)
        return p.kind=='corridor' and open_neighbors(map,p.x,p.y)>=2
            and route_clear(map,p.x,p.y,blocking)
    end)
    if gate then
        local plate
        for _,d in ipairs(directions) do
            local x,y=gate.x+d[1],gate.y+d[2]
            if safe(state,map,x,y,taken) then plate={x=x,y=y};break end
        end
        if plate then
            local e=add_entity(state,map,'gate',gate,{open=false,closes_at=nil,
                art_variant=(map.biome_id=='core:biome/sunken_aqueduct') and 'rpg' or 'tiny'})
            blocking[key(gate.x,gate.y)]=true
            taken[key(plate.x,plate.y)]=true
            add_feature(state,map,'plate',plate,{gate_id=e.id})
        else taken[key(gate.x,gate.y)]=nil end
    end
    local pillar=choose('pillar',function(p)
        return p.kind=='room' and open_neighbors(map,p.x,p.y)==4
            and route_clear(map,p.x,p.y,blocking)
    end)
    if pillar then add_entity(state,map,'pillar',pillar) end
    local ore=choose('ore',function(p)return p.kind=='room' end)
    if ore then add_feature(state,map,'ore',ore,{broken=false}) end
    local brazier=choose('brazier',function(p)return p.kind=='room' end)
    if brazier then add_feature(state,map,'brazier',brazier,{lit_until=0}) end
    local ward=choose('ward',function(p)return p.kind=='room' end)
    if ward then add_feature(state,map,'ward',ward,{collected=false}) end
    local biome=map.biome_id
    if biome=='core:biome/overgrown_ruins' then
        for i=1,2 do
            local p=choose('overgrowth'..i,function(q)return q.kind=='room' end)
            if p then add_feature(state,map,'overgrowth',p,{variant=i}) end
        end
    elseif biome=='core:biome/fungal_hollows' then
        for i=1,2 do
            local p=choose('mushroom'..i,function(q)return q.kind=='room' end)
            if p then add_feature(state,map,'mushroom',p,{variant=i}) end
        end
    elseif biome=='core:biome/ashen_bastion' or
        biome=='core:biome/sundered_forge' then
        for i=1,2 do
            local p=choose('mine_rail'..i,function(q)return q.kind=='corridor' end)
            if p then add_feature(state,map,'mine_rail',p,{variant=i}) end
        end
    end
end

function F.on_event(state,event,emit,spawn_currency)
    local floor=event.floor_id
    if not floor then return end
    for _,f in ipairs(state.review_features or {}) do
        if f.floor_id==floor then
            if f.kind=='key' and not f.collected and event.kind=='TileEntered'
                and event.source_id==state.player_id
                and f.x==event.x and f.y==event.y then
                f.collected=true
                state.review_keys[floor]=true
                emit('VaultKeyCollected',{floor_id=floor,x=f.x,y=f.y},state.player_id)
            elseif f.kind=='ward' and not f.collected
                and event.kind=='TileEntered' and event.source_id==state.player_id
                and f.x==event.x and f.y==event.y then
                f.collected=true
                for _,player in ipairs(state.entities) do
                    if player.id==state.player_id then
                        player.pickup_ward_actions=4;break
                    end
                end
                emit('WardFlaskCollected',{floor_id=floor,x=f.x,y=f.y},state.player_id)
            elseif f.kind=='brazier' and event.kind=='TileEntered'
                and event.source_id==state.player_id
                and f.x==event.x and f.y==event.y then
                f.lit_until=state.clock+300
                emit('BrazierLit',{floor_id=floor,x=f.x,y=f.y,
                    due=f.lit_until},state.player_id)
            elseif f.kind=='ore' and not f.broken
                and event.kind=='AttackPerformed' then
                for _,cell in ipairs(event.target_cells or {}) do
                    if cell.x==f.x and cell.y==f.y then
                        f.broken=true
                        emit('OreBroken',{floor_id=floor,x=f.x,y=f.y},event.source_id)
                        spawn_currency(3,floor,f.x,f.y,event.source_id)
                        break
                    end
                end
            elseif f.kind=='plate' and event.kind=='TileEntered'
                and f.x==event.x and f.y==event.y then
                for _,e in ipairs(state.entities) do
                    if e.id==f.gate_id and e.controller=='review_gate' then
                        e.open=true;e.blocks_movement=false
                        e.blocks_vision=false;e.blocks_projectiles=false
                        e.closes_at=state.clock+300
                        emit('TimedGateOpened',{floor_id=floor,x=e.position.x,
                            y=e.position.y,due=e.closes_at},event.source_id)
                        break
                    end
                end
            end
        end
    end
end

function F.after_time(state,emit)
    for _,e in ipairs(state.entities) do
        if e.controller=='review_gate' and e.open and e.closes_at
            and state.clock>=e.closes_at
            and not occupied(state,e.position.floor_id,e.position.x,e.position.y,e.id) then
            e.open=false;e.blocks_movement=true;e.blocks_vision=true
            e.blocks_projectiles=true;e.closes_at=nil
            emit('TimedGateClosed',{floor_id=e.position.floor_id,
                x=e.position.x,y=e.position.y},e.id)
        end
    end
end

function F.mist_cleared(state,map,x,y)
    for _,f in ipairs(state.review_features or {}) do
        if f.kind=='brazier' and f.floor_id==map.floor_id
            and (f.lit_until or 0)>state.clock
            and math.abs(f.x-x)+math.abs(f.y-y)<=2 then return true end
    end
    return false
end

function F.validate(state,maps)
    assert(type(state.review_features)=='table' and
        type(state.review_keys)=='table' and type(state.review_clues)=='table' and
        type(state.class_special)=='table','invalid review state')
    local feature_kinds={key=true,ward=true,ore=true,brazier=true,plate=true,
        mushroom=true,overgrowth=true,mine_rail=true}
    local seen,keys,plates={}, {}, {}
    local function position(floor_id,x,y)
        local map=maps[floor_id]
        assert(map and type(x)=='number' and x%1==0 and
            type(y)=='number' and y%1==0 and Content.walkable(map,x,y),
            'review object outside floor')
    end
    for _,f in ipairs(state.review_features) do
        assert(type(f)=='table' and feature_kinds[f.kind]
            and type(f.id)=='string' and not seen[f.id],'invalid review feature')
        seen[f.id]=true
        position(f.floor_id,f.x,f.y)
        if f.kind=='key' then
            assert(type(f.collected)=='boolean','invalid vault key state')
            keys[f.floor_id]=(keys[f.floor_id] or 0)+1
        elseif f.kind=='ward' then
            assert(type(f.collected)=='boolean','invalid ward flask state')
        elseif f.kind=='ore' then
            assert(type(f.broken)=='boolean','invalid ore state')
        elseif f.kind=='brazier' then
            assert(type(f.lit_until)=='number' and f.lit_until>=0,
                'invalid brazier state')
        elseif f.kind=='plate' then
            local gate
            for _,e in ipairs(state.entities) do
                if e.id==f.gate_id then gate=e;break end
            end
            assert(gate and gate.controller=='review_gate'
                and gate.position.floor_id==f.floor_id,'plate without gate')
            plates[f.gate_id]=(plates[f.gate_id] or 0)+1
        else
            assert(f.variant==1 or f.variant==2,'invalid scenery variant')
        end
    end
    for floor_id,count in pairs(keys) do
        assert(count==1,'duplicate vault key')
        local has_vault=false
        for _,e in ipairs(state.entities) do
            if e.controller=='review_vault' and e.position.floor_id==floor_id then
                has_vault=true;break
            end
        end
        assert(has_vault,'vault key without vault')
    end
    for _,e in ipairs(state.entities) do
        if e.controller=='review_gate' then
            assert(plates[e.id]==1,'gate without one plate')
        elseif e.controller=='review_vault' then
            assert(keys[e.position.floor_id]==1,'vault without key')
        end
    end
    for floor_id,has_key in pairs(state.review_keys) do
        assert(maps[floor_id] and has_key==true and keys[floor_id]==1,
            'invalid held vault key')
    end
    for floor_id,direction in pairs(state.review_clues) do
        assert(maps[floor_id] and
            (direction=='north' or direction=='east' or
                direction=='south' or direction=='west'),'invalid vault clue')
    end
    for _,kind in ipairs({'decoy','snare'}) do
        local marker=state.class_special[kind]
        if marker then position(marker.floor_id,marker.x,marker.y) end
    end
end

return F
