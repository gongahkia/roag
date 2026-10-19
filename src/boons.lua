local Catalog = require("src.boon_content")
local Content = require("src.content")
local RNG = require("src.rng")
local Terrain=require("src.terrain")
local Classes=require("src.classes")
local Visibility=require("src.visibility")
local Floors=require("src.floors")

local Boons = {}
local PPM = 1000000
local subscriptions = {}
for _, id in ipairs(Catalog.order) do
    for _, kind in ipairs(Catalog.get(id).triggers) do
        local list = subscriptions[kind] or {}
        list[#list + 1] = id
        subscriptions[kind] = list
    end
end

local function entity(state, id)
    for _, actor in ipairs(state.entities) do if actor.id == id then return actor end end
end

local function distance(a, b)
    return math.abs(a.x - b.x) + math.abs(a.y - b.y)
end

local function event_position(event)
    if event.x and event.y then return {floor_id=event.floor_id, x=event.x, y=event.y} end
    if event.from_x and event.from_y then
        return {floor_id=event.floor_id, x=event.from_x, y=event.from_y}
    end
    return event.source_position
end

local function observes(owner, scope, event)
    if scope.kind == "owner" then
        return event.source_id == owner.id or event.target_id == owner.id
    end
    local position = event_position(event)
    if not position or position.floor_id ~= owner.position.floor_id then return false end
    if scope.kind == "floor" then return true end
    return distance(owner.position, position) <= scope.radius
end

local function hostile(owner, faction)
    return faction ~= nil and faction ~= owner.faction
end

local function pair_key(a, b)
    if a > b then a, b = b, a end
    return a .. ":" .. b
end

local function pair_candidates(state, owner, boon_id, event, history, neighborhood)
    local matches = {}
    if not event.target_id or not event.x or not event.y then return matches end
    local seen = history.pairs
    for _, prior in ipairs(history.damages) do
        local dx, dy = math.abs(prior.x - event.x), math.abs(prior.y - event.y)
        local adjacent = neighborhood == "cardinal" and dx + dy == 1 or math.max(dx, dy) == 1
        if prior.target_id ~= event.target_id and prior.floor_id == event.floor_id
            and prior.target_faction and hostile(owner, prior.target_faction)
            and adjacent then
            local key = owner.id .. ":" .. boon_id .. ":" .. pair_key(prior.target_id, event.target_id)
            if not seen[key] then
                matches[#matches + 1] = {key=key, prior=prior}
            end
        end
    end
    return matches
end

local function predicate(node, state, owner, boon_id, event, history)
    if node.all then
        for _, child in ipairs(node.all) do
            if not predicate(child, state, owner, boon_id, event, history) then return false end
        end
        return true
    end
    if node.any then
        for _, child in ipairs(node.any) do
            if predicate(child, state, owner, boon_id, event, history) then return true end
        end
        return false
    end
    if node.not_ then return not predicate(node.not_, state, owner, boon_id, event, history) end
    local op = node.op
    if op=='roll_face' then return event.fate_face==node.value end
    if op=='unique_action' then
        if not event.root_action_id then return false end
        history.actions=history.actions or {}
        local key=owner.id..':'..boon_id..':'..event.root_action_id
        if history.actions[key] then return false end
        history.actions[key]=true;return true
    end
    if op=='supply_kind' then return event.supply_kind==node.value end
    if op=='supply_excess' then
        return (event.amount or 0)>(event.actual_gain or 0)
    end
    if op=='event_amount' then
        return type(event[node.field])=='number' and event[node.field]>=node.min
    end
    if op=='rewarded_drop' then
        local def=Content.get_enemy(event.target_definition_id)
        return not event.reward_exempt and def and def.currency_drop>0 or false
    end
    if op=="event_kind" then return event.kind==node.value end
    if op=="visible_source" then
        local p=event.source_position
        return p and Visibility.is_visible(state,p.floor_id,p.x,p.y) or false
    end
    if op=="visible_event" then
        local p=event_position(event)
        return p and Visibility.is_visible(state,p.floor_id,p.x,p.y) or false
    end
    if op=="terrain_id" then return event.terrain_id==node.value end
    if op=="damage_type" then return event.damage_type==node.value end
    if op=="from_terrain" then
        return Terrain.symbol(Floors.map(state,event.floor_id),event.from_x or -1,
            event.from_y or -1)==node.value
    end
    if op=="path_terrain" then
        local map=Floors.map(state,event.floor_id)
        for _,cell in ipairs(event.path or {}) do
            if cell.floor_id==owner.position.floor_id
                and Terrain.symbol(map,cell.x,cell.y)==node.value then return true end
        end
        return false
    end
    if op=="source_on_terrain" then
        local p=event.source_position
        return p and p.floor_id==owner.position.floor_id and
            Terrain.symbol(Floors.map(state,p.floor_id),p.x,p.y)==node.value
    end
    if op=="target_near_terrain" then
        if not event.x or not event.y or event.floor_id~=owner.position.floor_id then
            return false
        end
        local map=Floors.map(state,event.floor_id)
        for y=event.y-1,event.y+1 do for x=event.x-1,event.x+1 do
            if Terrain.symbol(map,x,y)==node.value then return true end
        end end
        return false
    end
    if op=="primed" then
        local memory=owner.boon_memory and owner.boon_memory[boon_id]
        return memory and (memory.charges or 0)>0
            and (memory.expires_at or -1)>=state.clock or false
    end
    if op=="special_attack" then
        return event.ability_id==Classes.for_player(owner).special_id
    end
    if op=="target_hp_percent_before" then
        return event.hp_before and event.target_max_hp
            and event.hp_before*100<=event.target_max_hp*node.max or false
    end
    if op=="owner_in_locked_cells" then
        for _,cell in ipairs(event.target_cells or {}) do
            if cell.floor_id==owner.position.floor_id
                and cell.x==owner.position.x and cell.y==owner.position.y then
                return true
            end
        end
        return false
    end
    if op=="overlapping_windups" then
        for _,actor in ipairs(state.entities) do
            if actor.id~=event.source_id and actor.pending_attack
                and actor.faction~=owner.faction and actor.hp and actor.hp>0 then
                for _,a in ipairs(actor.pending_attack.target_cells or {}) do
                    for _,b in ipairs(event.target_cells or {}) do
                        if a.floor_id==b.floor_id and a.x==b.x and a.y==b.y then
                            return true
                        end
                    end
                end
            end
        end
        return false
    end
    if op=="approaching_owner" then
        return event.floor_id==owner.position.floor_id and event.from_x and event.x
            and math.abs(event.x-owner.position.x)+math.abs(event.y-owner.position.y)
                <math.abs(event.from_x-owner.position.x)+
                    math.abs(event.from_y-owner.position.y)
    end
    if op=="aligned_hostiles" then
        if not event.x or not event.y then return false end
        local row,column=0,0
        for _,actor in ipairs(state.entities) do
            local p=actor.position
            if actor.hp and actor.hp>0 and actor.faction~=owner.faction
                and p.floor_id==event.floor_id then
                if p.y==event.y then row=row+1 end
                if p.x==event.x then column=column+1 end
            end
        end
        return math.max(row,column)>=node.min
    end
    if op=="event_distance" then
        local p=event_position(event)
        if not p or p.floor_id~=owner.position.floor_id then return false end
        local d=distance(owner.position,p)
        return d>=node.min and d<=node.max
    end
    if op == "source_owner" then return event.source_id == owner.id end
    if op == "target_owner" then return event.target_id == owner.id end
    if op == "source_not_owner" then return event.source_id ~= owner.id end
    if op == "target_hostile" then return hostile(owner, event.target_faction) end
    if op == "source_hostile" then return hostile(owner, event.source_faction) end
    if op == "source_in_radius" then
        local p=event.source_position
        return p and p.floor_id==owner.position.floor_id
            and distance(owner.position,p)<=node.radius
    end
    if op == "direct_attack" then return event.direct_attack == true end
    if op == "other_boon" then return event.boon_id ~= nil and event.boon_id ~= boon_id end
    if op == "target_was_full" then
        return event.hp_before ~= nil and event.hp_before==event.target_max_hp
    end
    if op == "owner_hp_percent" then
        local hp=event.source_hp_at_attack or owner.hp
        local max_hp=event.source_max_hp_at_attack or owner.max_hp
        return hp and max_hp and hp*100<=max_hp*node.max
    end
    if op == "adjacent_hostiles" then
        if event.adjacent_hostile_count then return event.adjacent_hostile_count>=node.min end
        if not event.x or not event.y then return false end
        local count=0
        for _,other in ipairs(state.entities) do
            if other.id~=owner.id and other.hp and other.hp>0
                and other.faction~=owner.faction
                and other.position.floor_id==event.floor_id
                and math.max(math.abs(other.position.x-event.x),
                    math.abs(other.position.y-event.y))==1 then count=count+1 end
        end
        return count>=node.min
    end
    if op == "burning_pair" then
        if event.target_burning~=nil then return event.target_burning
            and (event.adjacent_burning_count or 0)>0 end
        return false
    end
    if op == "distinct_chain_group" then
        if not event.target_id then return false end
        history.groups=history.groups or {}
        local key=owner.id..":"..boon_id
        local group=history.groups[key] or {seen={},count=0}
        history.groups[key]=group
        if group.seen[event.target_id] then return false end
        group.seen[event.target_id]=true
        group.count=group.count+1
        if group.count<node.size then return false end
        group.count=0
        return true
    end
    if op == "unique_windup" then
        if not event.windup_id then return false end
        history.windups=history.windups or {}
        local key=owner.id..":"..boon_id..":"..event.windup_id
        if history.windups[key] then return false end
        history.windups[key]=true
        return true
    end
    if op == "ability_id" then return event.ability_id==node.value end
    if op == "positive" then return (event.actual_loss or 0) > 0 end
    if op == "source_target_same_faction" then
        return event.source_id ~= event.target_id and event.source_faction ~= nil
            and event.source_faction == event.target_faction
    end
    if op == "pair_adjacent_damage" then
        return #pair_candidates(state, owner, boon_id, event, history, node.neighborhood) > 0
    end
    if op == "source_id" then return event.source_id == node.value end
    if op == "target_id" then return event.target_id == node.value end
    if op == "source_faction" then return event.source_faction == node.value end
    if op == "target_faction" then return event.target_faction == node.value end
    if op == "source_tag" or op == "target_tag" then
        local tags = op == "source_tag" and event.source_tags or event.target_tags
        for _, tag in ipairs(tags or {}) do if tag == node.value then return true end end
        return false
    end
    error("unknown predicate operation")
end

local function magnitude(def, stacks, field)
    local result = 0
    for _, rarity in ipairs(Catalog.rarities) do
        result = result + (stacks[rarity] or 0) * def.parameters[rarity][field]
    end
    return result
end

function Boons.aggregate(id, stacks, field)
    local def = assert(Catalog.get(id), "unknown boon")
    assert(field == "chance" or field == "power", "unknown boon parameter")
    return magnitude(def, stacks, field)
end

function Boons.proc_count(state, chance_ppm, coefficient)
    assert(type(coefficient) == "number" and coefficient >= 0 and coefficient <= 10,
        "invalid proc coefficient")
    local scaled = math.floor(chance_ppm * coefficient + 0.5)
    local guaranteed, fraction = math.floor(scaled / PPM), scaled % PPM
    if fraction > 0 and RNG.next_int(state.rng, PPM) <= fraction then
        return guaranteed + 1
    end
    return guaranteed
end

local function owns_any(owner)
    for _, id in ipairs(Catalog.order) do
        local stacks = owner.boons and owner.boons[id]
        if stacks then
            for _, rarity in ipairs(Catalog.rarities) do
                if (stacks[rarity] or 0) > 0 then return true end
            end
        end
    end
    return false
end

function Boons.active(state)
    for _, owner in ipairs(state.entities) do if owns_any(owner) then return true end end
    return false
end

function Boons.set_stacks(owner, id, rarity, count)
    assert(Catalog.get(id), "unknown boon")
    assert(type(count) == "number" and count % 1 == 0 and count >= 0,
        "invalid boon stack count")
    local valid = false
    for _, name in ipairs(Catalog.rarities) do if rarity == name then valid = true end end
    assert(valid, "invalid boon rarity")
    owner.boons = owner.boons or {}
    local def = Catalog.get(id)
    if count > 0 then
        for _, other in ipairs(Catalog.order) do
            local owned = owner.boons[other]
            if owned and other ~= id then
                local other_def = Catalog.get(other)
                local conflict = false
                for _, conflict_id in ipairs(def.incompatible or {}) do
                    if conflict_id == other then conflict = true end
                end
                for _, conflict_id in ipairs(other_def.incompatible or {}) do
                    if conflict_id == id then conflict = true end
                end
                if conflict then
                    for _, name in ipairs(Catalog.rarities) do
                        assert((owned[name] or 0) == 0, "incompatible boons")
                    end
                end
            end
        end
    end
    local stacks = owner.boons[id] or {}
    if count == 0 then stacks[rarity] = nil else stacks[rarity] = count end
    if next(stacks) then owner.boons[id] = stacks else owner.boons[id] = nil end
end

function Boons.validate_inventory(owner,state)
    for id, stacks in pairs(owner.boons or {}) do
        assert(Catalog.get(id), "unknown owned boon")
        assert(type(stacks) == "table", "invalid boon stacks")
        for rarity, count in pairs(stacks) do
            local valid = false
            for _, name in ipairs(Catalog.rarities) do if rarity == name then valid = true end end
            assert(valid and type(count) == "number" and count % 1 == 0 and count > 0,
                "invalid boon stacks")
        end
    end
    for _, id in ipairs(Catalog.order) do
        local stacks = owner.boons and owner.boons[id]
        if stacks then
            for _, other in ipairs(Catalog.get(id).incompatible or {}) do
                assert(not (owner.boons and owner.boons[other]), "incompatible boons")
            end
        end
    end
    for id,memory in pairs(owner.boon_memory or {}) do
        assert(Catalog.get(id) and type(memory)=="table","invalid boon memory")
        for _,field in ipairs({"charges","expires_at","speed","ward","mark_count",
            "mark_target_id"}) do
            local value=memory[field]
            assert(value==nil or (type(value)=="number" and value%1==0
                and value>=0),"invalid boon memory value")
        end
        if memory.charges or memory.speed or memory.ward or memory.mark_count then
            assert(memory.expires_at~=nil,"boon memory missing expiry")
        end
        if memory.mark_count then
            assert(memory.mark_target_id~=nil,"boon mark missing target")
        end
        if memory.face~=nil then
            assert(id=='core:boon/fateful_dice' and type(memory.face)=='number'
                and memory.face%1==0 and memory.face>=1 and memory.face<=8
                and type(memory.roll_action_id)=='number' and memory.roll_action_id%1==0
                and memory.roll_action_id>=1,'invalid saved dice roll')
            if state then
                assert(memory.roll_action_id<state.next_action_id,'future saved dice roll')
            end
        end
    end
end

function Boons.attack_vow(owner)
    local id='core:boon/pilgrims_vow'
    local stacks=owner.boons and owner.boons[id]
    if not stacks then return end
    local def=Catalog.get(id)
    return {boon_id=id,hp_cost=def.attack_payment.hp,
        bonus=magnitude(def,stacks,'power')}
end

function Boons.dice_power(owner)
    local id='core:boon/fateful_dice'
    local stacks=owner.boons and owner.boons[id]
    if stacks then return magnitude(Catalog.get(id),stacks,'power') end
end
function Boons.roll_dice(state,owner,action_id)
    local power=Boons.dice_power(owner)
    if not power then return end
    local face=RNG.next_int(state.rng,8)
    owner.boon_memory=owner.boon_memory or {}
    local memory=owner.boon_memory['core:boon/fateful_dice'] or {}
    memory.face=face;memory.roll_action_id=action_id
    owner.boon_memory['core:boon/fateful_dice']=memory
    return {face=face,power=power}
end
function Boons.rebase_stage_memory(owner,old_clock,new_clock)
    for _,id in ipairs(Catalog.order)do
        local memory=owner.boon_memory and owner.boon_memory[id]
        if memory and memory.expires_at then
            local remaining=memory.expires_at-old_clock
            memory.expires_at=new_clock+math.max(0,remaining)
            if remaining<0 then
                -- Clock zero is a valid inclusive expiry, so merely clamping
                -- an expired timer to zero would revive it for one action.
                for _,field in ipairs({'charges','speed','ward','mark_count'})do
                    if memory[field]~=nil then memory[field]=0 end
                end
            end
        end
    end
end
local regular_ai={guard=true,spitter=true,skitter=true,spore_herald=true,
    canal_lancer=true,cinder_bellkeeper=true,expansion_regular=true,squad=true,idea_regular=true}
function Boons.skull_target(target)
    local def=Content.get_enemy(target.definition_id)
    return target.controller=='ai' and target.faction=='enemy'
        and not target.reward_exempt and not target.invulnerable
        and def and not def.special_encounter and regular_ai[def.ai] or false
end

function Boons.speed_bonus(state,owner)
    if not owner.boon_memory then return 0 end
    local bonus=0
    for _,id in ipairs(Catalog.order)do
        local memory=owner.boon_memory[id]
        if memory and (memory.expires_at or -1)>=state.clock then
            bonus=bonus+(memory.speed or 0)
        end
    end
    return bonus
end

function Boons.consume_ward(state,owner,damage)
    if damage<=0 then return 0 end
    if not owner.boon_memory then return 0 end
    local absorbed=0
    -- Consume in catalogue order, leaving later wards intact when an earlier
    -- ward already covered the hit. No pairs-order-dependent protection.
    for _,id in ipairs(Catalog.order)do
        local memory=owner.boon_memory[id]
        if absorbed<damage and memory and (memory.expires_at or -1)>=state.clock
            and (memory.ward or 0)>0 then
            absorbed=absorbed+math.min(damage-absorbed,memory.ward)
            memory.ward=0
        end
    end
    return absorbed
end

function Boons.ability(state, owner, ability_id, base_cells, base_damage, dx, dy,
    explicit, ranged)
    local cells, damage = {}, base_damage
    local map=Content.map_for(state)
    local function living_at(cell)
        for _,actor in ipairs(state.entities) do
            local p=actor.position
            if actor.id~=owner.id and actor.hp and actor.hp>0 and p
                and p.floor_id==owner.position.floor_id
                and p.x==cell.x and p.y==cell.y then return actor end
        end
    end
    local function append_unique(x,y)
        if x==owner.position.x and y==owner.position.y then return end
        if not Content.projectile_passable(map,x,y,state) then return end
        for _,cell in ipairs(cells) do
            if cell.x==x and cell.y==y then return end
        end
        cells[#cells+1]={floor_id=owner.position.floor_id,x=x,y=y}
    end
    for index, cell in ipairs(base_cells) do
        cells[index] = {floor_id=cell.floor_id, x=cell.x, y=cell.y}
    end
    for _, id in ipairs(Catalog.order) do
        local stacks = owner.boons and owner.boons[id]
        if stacks then
            local def = Catalog.get(id)
            local power = magnitude(def, stacks, "power")
            for _, modifier in ipairs(def.modifiers or {}) do
                if modifier.ability_id == ability_id then
                    if modifier.kind == "forward_cell" then
                        local x = owner.position.x + dx * 2
                        local y = owner.position.y + dy * 2
                        local path_open = Content.walkable(map,
                            owner.position.x + dx, owner.position.y + dy)
                            and Content.walkable(map, x, y)
                        local found = false
                        for _, cell in ipairs(cells) do
                            if cell.x == x and cell.y == y then found = true end
                        end
                        if path_open and not found then
                            cells[#cells + 1] = {floor_id=owner.position.floor_id, x=x, y=y}
                        end
                    elseif modifier.kind == "line_second_cell" and explicit~=false then
                        local mid_x,mid_y=owner.position.x+dx,owner.position.y+dy
                        local x,y=mid_x+dx,mid_y+dy
                        if Content.projectile_passable(map,mid_x,mid_y,state)
                            and Content.projectile_passable(map,x,y,state) then
                            cells[#cells+1]={floor_id=owner.position.floor_id,x=x,y=y}
                        end
                    elseif modifier.kind=="branch_near" or
                        modifier.kind=="status_chain" then
                        local primary,index
                        for j,cell in ipairs(cells) do
                            local found=living_at(cell)
                            if found then primary,index=found,j;break end
                        end
                        if primary and (modifier.kind=="branch_near" or
                            primary.statuses and primary.statuses.burning) then
                            while #cells>index do table.remove(cells) end
                            local candidates={}
                            for _,actor in ipairs(state.entities) do
                                local p=actor.position
                                if actor.id~=primary.id and actor.id~=owner.id
                                    and actor.hp and actor.hp>0
                                    and actor.faction~=owner.faction and p
                                    and p.floor_id==owner.position.floor_id
                                    and math.max(math.abs(p.x-primary.position.x),
                                        math.abs(p.y-primary.position.y))==1
                                    and (modifier.kind=="branch_near" or
                                        actor.statuses and actor.statuses.burning) then
                                    candidates[#candidates+1]=actor
                                end
                            end
                            table.sort(candidates,function(a,b)return a.id<b.id end)
                            local added=0
                            for _,candidate in ipairs(candidates) do
                                local p=candidate.position
                                local sx=p.x-primary.position.x
                                local sy=p.y-primary.position.y
                                if sx==0 or sy==0 or
                                    (Content.projectile_passable(map,
                                        primary.position.x+sx,primary.position.y,state)
                                    and Content.projectile_passable(map,
                                        primary.position.x,primary.position.y+sy,state)) then
                                    append_unique(p.x,p.y)
                                    added=added+1
                                    if modifier.kind=="branch_near" and added>=2 then
                                        break
                                    end
                                end
                            end
                            ranged="piercing_line"
                        end
                    elseif modifier.kind=="line_extend" then
                        local last=cells[#cells]
                        local end_x=owner.position.x+5*dx
                        local end_y=owner.position.y+5*dy
                        if last and last.x==end_x and last.y==end_y
                            and Content.projectile_passable(map,end_x,end_y,state)
                            and (dx==0 or dy==0 or
                                (Content.projectile_passable(map,end_x+dx,end_y,state)
                                and Content.projectile_passable(map,end_x,end_y+dy,state))) then
                            append_unique(end_x+dx,end_y+dy)
                        end
                    elseif modifier.kind=="area_expand" then
                        local cx=owner.position.x+2*dx
                        local cy=owner.position.y+2*dy
                        local reached=false
                        for _,cell in ipairs(cells) do
                            if cell.x==cx and cell.y==cy then reached=true;break end
                        end
                        if reached and Content.projectile_passable(map,cx,cy,state) then
                            for oy=-2,2 do for ox=-2,2 do
                                if math.max(math.abs(ox),math.abs(oy))==2 then
                                    append_unique(cx+ox,cy+oy)
                                end
                            end end
                        end
                    elseif modifier.kind=="shockwave" then
                        local hit=cells[1]
                        if hit then
                            for _,step in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
                                append_unique(hit.x+step[1],hit.y+step[2])
                            end
                        end
                    elseif modifier.kind == "add_damage" then damage = damage + power end
                end
            end
        end
    end
    return cells, damage, ranged
end

local function chain_record(state, chain_id)
    local record = state.chain_history[chain_id]
    if not record then
        record = {damages={}, pairs={},groups={},windups={}}
        state.chain_history[chain_id] = record
    end
    return record
end

local function contains(ancestry, activation_key)
    for _, key in ipairs(ancestry or {}) do if key == activation_key then return true end end
    return false
end

local function copied_ancestry(event, key)
    local result = {}
    for _, value in ipairs(event.ancestry or {}) do result[#result + 1] = value end
    result[#result + 1] = key
    return result
end

local function pair_neighborhood(node)
    if node.op == "pair_adjacent_damage" then return node.neighborhood end
    for _, child in ipairs(node.all or node.any or {}) do
        local result = pair_neighborhood(child)
        if result then return result end
    end
    return node.not_ and pair_neighborhood(node.not_) or nil
end

-- Subscriptions are indexed by event kind; only owners of a subscribed boon are visited.
function Boons.on_event(state, event, enqueue, emit_activation)
    -- A rolled cross is a paid miss, not an invisible Temporal Echo or other
    -- on-attack proc. Ordinary empty non-dice attacks retain their old rules.
    if event.fate_miss and (event.kind=='AttackPerformed' or event.kind=='AbilityUsed') then return end
    -- Shared HP loss is not a second weapon hit or a recursive damage proc.
    -- A real credited death still earns the existing finite on-kill effects.
    if event.oath_shared_only and event.kind~='EntityKilled'then return end
    local list = subscriptions[event.kind]
    if not list then return end
    local history = chain_record(state, event.chain_id)
    for _, owner in ipairs(state.entities) do
        for _, id in ipairs(list) do
            local stacks = owner.boons and owner.boons[id]
            if stacks then
                local def = Catalog.get(id)
                local key = owner.id .. ":" .. id
                if not contains(event.ancestry, key) and observes(owner, def.scope, event)
                    and predicate(def.predicate, state, owner, id, event, history) then
                    local pairs = nil
                    local neighborhood = pair_neighborhood(def.predicate)
                    if neighborhood then
                        pairs = pair_candidates(state, owner, id, event, history, neighborhood)
                    end
                    local count = def.stack_policy == "chance_overflow" and
                        Boons.proc_count(state, magnitude(def, stacks, "chance"),
                            event.proc_coefficient or 1) or 1
                    local pair_count = pairs and #pairs or 1
                    for pair_index = 1, pair_count do
                        local pair = pairs and pairs[pair_index] or nil
                        if pair then history.pairs[pair.key] = true end
                        for _ = 1, count do
                            local activation_id = state.next_activation_id
                            state.next_activation_id = activation_id + 1
                            local activation = {
                                id=activation_id, boon_id=id, owner_id=owner.id,
                                source_faction=owner.faction,
                                source_position={floor_id=owner.position.floor_id,
                                    x=owner.position.x, y=owner.position.y},
                                chain_id=event.chain_id, root_action_id=event.root_action_id,
                                parent_event_id=event.id, ancestry=copied_ancestry(event, key),
                                power=magnitude(def, stacks, "power"),
                                event=event, pair_target_id=pair and pair.prior.target_id,
                            }
                            emit_activation(activation)
                            for _, effect in ipairs(def.effects) do
                                if not effect.when or predicate(effect.when,state,
                                    owner,id,event,history) then
                                    enqueue(activation, effect)
                                end
                            end
                        end
                    end
                end
            end
        end
    end
    if event.kind == "DamageTaken" and event.actual_loss > 0 then
        history.damages[#history.damages + 1] = {
            target_id=event.target_id, target_faction=event.target_faction,
            floor_id=event.floor_id, x=event.x, y=event.y,
        }
    end
end

function Boons.prune_histories(state)
    local keep = {}
    for _, payload in pairs(state.delayed_effects) do keep[payload.activation.chain_id] = true end
    for _,actor in ipairs(state.entities) do
        for _,status in pairs(actor.statuses or {}) do
            if status.activation then keep[status.activation.chain_id]=true end
        end
    end
    for id in pairs(state.chain_history) do
        if not keep[id] then state.chain_history[id] = nil end
    end
end

return Boons
