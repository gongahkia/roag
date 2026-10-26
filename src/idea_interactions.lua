-- Finite, voluntary services. Inspection is pure; only Game.submit calls apply.
local Content=require('src.content')
local Catalog=require('src.boon_content')
local Boons=require('src.boons')
local Rewards=require('src.rewards')
local RNG=require('src.rng')
local Scheduler=require('src.scheduler')
local Visibility=require('src.visibility')
local Def=require('src.idea_interaction_content')
local I={definitions=Def}
local dirs={{0,-1},{1,0},{0,1},{-1,0}}
local function copy(v)
    if type(v)~='table'then return v end
    local t={};for k,x in pairs(v)do t[k]=copy(x)end;return t
end
local function actor(state,id)
    for _,e in ipairs(state.entities)do if e.id==id then return e end end
end
local function player(state)return assert(actor(state,state.player_id))end
local function store(state)
    state.idea_rooms=state.idea_rooms or {version=Def.version,nodes={},groups={}}
    return state.idea_rooms
end
local function key(x,y)return x..','..y end
local function dist(a,b)return math.abs(a.x-b.x)+math.abs(a.y-b.y)end
function I.room_at(map,x,y)
    for _,r in ipairs(map.rooms or {})do
        if x>=r.x and x<r.x+r.width and y>=r.y and y<r.y+r.height then return r end
    end
end
local function inside(r,x,y)
    return r and x>=r.x and x<r.x+r.width and y>=r.y and y<r.y+r.height
end
local function blocked(state,floor,x,y)
    for _,e in ipairs(state.entities)do
        if e.blocks_movement and e.id~=state.player_id and e.position.floor_id==floor
            and e.position.x==x and e.position.y==y then return true end
    end
    return false
end
function I.node(state,id)
    for _,n in ipairs(state.idea_rooms and state.idea_rooms.nodes or {})do if n.id==id then return n end end
end
local function eligible(state)
    local result={};local class=player(state).definition_id
    for _,id in ipairs(state.available_boons)do
        local def=Catalog.get(id);local okay=not def.eligible_classes
        for _,c in ipairs(def.eligible_classes or {})do if c==class then okay=true end end
        if okay then result[#result+1]=id end
    end
    table.sort(result);return result
end
function I.place(state,kind,map,x,y,extra)
    assert(Def.names[kind] and Content.walkable(map,x,y),'invalid interaction placement')
    local s=store(state)
    local n={id=map.floor_id..':interaction:'..kind..':'..key(x,y),kind=kind,
        floor_id=map.floor_id,x=x,y=y,spent=false}
    assert(not I.node(state,n.id),'duplicate interaction')
    for k,v in pairs(extra or {})do n[k]=copy(v)end
    local seed=RNG.derive(state.root_seed or state.run_seed,n.id)
    if kind=='offering_font'then
        n.offers=Rewards.make_offers(state,Content.get_chest('core:chest/standard'),RNG.new(seed))
    elseif kind=='travelling_trader'then
        n.stock=seed%2==0 and 'cleanse_flask' or 'escape_scroll'
    elseif kind=='attunement_forge'then
        n.trades={};local pool=eligible(state)
        for _,id in ipairs(pool)do
            n.trades[id]={}
            for _,rarity in ipairs(Catalog.rarities)do
                local alternatives={};for _,v in ipairs(pool)do if v~=id then alternatives[#alternatives+1]=v end end
                local rng=RNG.new(RNG.derive(seed,id..':'..rarity));local offers={}
                for _=1,math.min(3,#alternatives)do offers[#offers+1]=table.remove(alternatives,RNG.next_int(rng,#alternatives))end
                n.trades[id][rarity]=offers
            end
        end
    end
    s.nodes[#s.nodes+1]=n;return n
end

local services={'offering_font','ration_cache','cleanse_flask','escape_scroll','return_token',
    'charm_well','attunement_forge','travelling_trader','memorial','camp','archive_contract'}
local function ordinary_targets(state,map,room)
    local result={}
    for _,e in ipairs(state.entities)do
        local d=e.controller=='ai' and Content.get_enemy(e.definition_id)
        if d and e.hp>0 and e.position.floor_id==map.floor_id and inside(room,e.position.x,e.position.y)
            and d.ai~='warden' and d.ai~='biome_elite' and d.ai~='pursuer' and d.ai~='hollow_regent'
            and e.definition_id~='core:enemy/rotating_sentinel' and not e.reward_exempt then result[#result+1]=e.id end
    end
    table.sort(result);return result
end
function I.populate(state,map)
    if not map.rooms or #map.rooms==0 then return end
    local taken,candidates={},{}
    local function reserve(x,y) taken[key(x,y)]=true end
    for _,list in ipairs({map.placements.stairs or {},map.placements.chests or {},
        map.placements.doors or {},map.placements.secrets or {},state.review_features or {}})do
        for _,p in ipairs(list)do if not p.floor_id or p.floor_id==map.floor_id then reserve(p.x,p.y)end end
    end
    for _,e in ipairs(state.entities)do if e.position.floor_id==map.floor_id then reserve(e.position.x,e.position.y)end end
    local seed=RNG.derive(state.run_seed,map.floor_id..':interactions')
    for _,r in ipairs(map.rooms)do
        if r.role~='start' and r.role~='entrance' and r.role~='elite' and r.role~='exit' then
            for y=r.y+1,r.y+r.height-2 do for x=r.x+1,r.x+r.width-2 do
                if not taken[key(x,y)] and map.rows[y]:sub(x,x)=='.'
                    and dist({x=x,y=y},{x=map.start_x,y=map.start_y})>=4
                    and (not map.elite or dist({x=x,y=y},map.elite)>=4)then
                    candidates[#candidates+1]={x=x,y=y,room=r,score=RNG.derive(seed,key(x,y))}
                end
            end end
        end
    end
    table.sort(candidates,function(a,b)if a.score~=b.score then return a.score<b.score end
        if a.y~=b.y then return a.y<b.y end;return a.x<b.x end)
    local used_rooms={}
    local function choose(condition)
        for _,p in ipairs(candidates)do
            if not taken[key(p.x,p.y)] and not used_rooms[p.room.id]
                and (not condition or condition(p))then
                reserve(p.x,p.y);used_rooms[p.room.id]=true;return p
            end
        end
    end
    local first=seed%#services+1
    for slot=1,Def.service_cap do
        local kind=services[(first+slot-2)%#services+1]
        local p=choose(kind=='archive_contract' and function(q)return #ordinary_targets(state,map,q.room)>0 end or nil)
        if p then
            local extra=kind=='archive_contract' and {room_id=p.room.id,room=copy(p.room),
                targets=ordinary_targets(state,map,p.room),status='offered',defeated={}} or nil
            I.place(state,kind,map,p.x,p.y,extra)
        end
    end
    if seed%2==0 then
        local p=choose(function(q)
            for _,d in ipairs(dirs)do if taken[key(q.x+d[1],q.y+d[2])] or
                not inside(q.room,q.x+d[1],q.y+d[2]) or not Content.walkable(map,q.x+d[1],q.y+d[2])then return false end end
            return true
        end)
        if p then
            local gid=map.floor_id..':bells:'..key(p.x,p.y)
            local sequence={1,2,3};local rng=RNG.new(RNG.derive(seed,'bells'))
            for i=3,2,-1 do local j=RNG.next_int(rng,i);sequence[i],sequence[j]=sequence[j],sequence[i]end
            store(state).groups[gid]={kind='bells',floor_id=map.floor_id,sequence=sequence,progress=0,completed=false}
            I.place(state,'bell_clue',map,p.x,p.y,{group=gid})
            for i=1,3 do local d=dirs[i];reserve(p.x+d[1],p.y+d[2]);I.place(state,'bell',map,p.x+d[1],p.y+d[2],{group=gid,bell=i})end
        end
    else
        local p=choose(function(q)
            for _,d in ipairs({{0,0},{0,-1},{1,0},{0,1},{-1,0}})do
                local x,y=q.x+1+d[1],q.y+d[2]
                if not inside(q.room,x,y) or not Content.walkable(map,x,y)then return false end
            end
            return true
        end)
        if p then
            local cells={};local cx,cy=p.x+1,p.y
            for _,d in ipairs({{0,0},{0,-1},{1,0},{0,1},{-1,0}})do
                cells[#cells+1]={floor_id=map.floor_id,x=cx+d[1],y=cy+d[2]}
            end
            I.place(state,'falling_fixture',map,p.x,p.y,{cells=cells,center_x=cx,center_y=cy})
        end
    end
    -- One truthful coarse hint to an existing neighbouring room; no revealed cells.
    local p=choose()
    if p then
        local target
        for _,r in ipairs(map.rooms)do if r.id~=p.room.id and (not target or dist(p,{x=r.x,y=r.y})<dist(p,{x=target.x,y=target.y}))then target=r end end
        if target then
            local queue,seen={{x=p.x,y=p.y,dx=0,dy=0}},{[key(p.x,p.y)]=true};local head=1;local found
            while head<=#queue do
                local q=queue[head];head=head+1
                if inside(target,q.x,q.y)then found=q;break end
                for _,d in ipairs(dirs)do local x,y=q.x+d[1],q.y+d[2]
                    if not seen[key(x,y)] and Content.walkable(map,x,y)then
                        local first=q.dx~=0 or q.dy~=0
                        seen[key(x,y)]=true;queue[#queue+1]={x=x,y=y,dx=first and q.dx or d[1],dy=first and q.dy or d[2]}
                    end
                end
            end
            if found then I.place(state,'route_sign',map,p.x,p.y,{dx=found.dx,dy=found.dy,
                hint=(target.role=='treasure' and 'Treasure' or target.role=='supply' and 'Supplies' or 'Rooms / danger')})end
        end
    end
end

local function valid_access(state,n)
    if not n then return false,'unknown interaction' end
    local p=player(state).position
    if p.floor_id~=n.floor_id or dist(p,n)>1 or not Visibility.can_see(state,n.floor_id,n.x,n.y)then
        return false,'stand beside a visible interaction'
    end
    return true
end
local function grant_probe(p,offer,remove)
    local probe={boons=copy(p.boons)}
    if remove then Boons.set_stacks(probe,remove.id,remove.rarity,probe.boons[remove.id][remove.rarity]-1)end
    if offer then
        local n=probe.boons[offer.boon_id] and probe.boons[offer.boon_id][offer.rarity] or 0
        return pcall(Boons.set_stacks,probe,offer.boon_id,offer.rarity,n+1)
    end
    return true
end
local function utility_entry(p,kind,price)
    return {id='take',label=Def.names[kind]..(price>0 and (' · '..price..' gold') or ''),
        utility=kind,price=price,replace_required=p.utility~=nil,
        detail='One charge. '..(p.utility and ('Replaces '..Def.names[p.utility.kind]..'; confirm that exchange explicitly.') or 'Uses the one carried utility slot.')}
end
function I.options(state,id,selection)
    if selection~=nil and (type(selection)~='table' or type(selection.id)~='string' or type(selection.rarity)~='string')then
        return nil,'invalid stack selection'
    end
    local n=I.node(state,id);local okay,reason=valid_access(state,n)
    if not okay then return nil,reason end
    local p=player(state);local result={node_id=id,title=Def.names[n.kind],entries={},
        detail='Confirm spends one normal action. Cancel is free.',selection=copy(selection)}
    local entries=result.entries
    local function add(e)
        e.enabled=e.enabled~=false;e.price=e.price or 0
        if e.price>state.currency then e.enabled=false;e.reason='Need '..(e.price-state.currency)..' more gold' end
        entries[#entries+1]=e
    end
    if n.kind=='route_sign'then
        result.detail=n.hint..' '..(n.dx<0 and 'west' or n.dx>0 and 'east' or n.dy<0 and 'north' or 'south')..'. A coarse hint only; reading costs no time.'
    elseif n.kind=='bell_clue'then
        local g=state.idea_rooms.groups[n.group]
        result.detail='Ring bells '..table.concat(g.sequence,' → ')..'. Labels work without colour or audio. Reading costs no time.'
    elseif n.spent then result.detail='Already used. No refill on returning or Continue.'
    elseif n.kind=='offering_font'then
        local cost=math.max(1,math.ceil(p.max_hp*.2))
        result.detail=('Pay %d HP (remain %d/%d). Choose one saved offer; no rerolls.'):format(cost,math.max(0,p.hp-cost),p.max_hp)
        for index,offer in ipairs(n.offers)do add({id=tostring(index),offer=copy(offer),hp_cost=cost,
            label=Catalog.get(offer.boon_id).name..' · '..offer.rarity,
            enabled=p.hp>cost and grant_probe(p,offer),reason=p.hp<=cost and 'Payment must leave at least 1 HP' or 'Incompatible boon'})end
    elseif n.kind=='ration_cache' or n.kind=='camp'then
        local heal=n.kind=='camp' and math.ceil(p.max_hp*.25) or 4
        add({id='food',label='Recover '..math.min(heal,p.max_hp-p.hp)..' HP',heal=heal,enabled=p.hp<p.max_hp,reason='Already at full HP'})
        add({id='ward',label=n.kind=='camp' and 'Cleanse Burning + one-hit ward' or 'One-hit ward',
            cleanse=n.kind=='camp',ward=true,detail='Blocks one positive hit within the next three committed player actions; replaces a supply ward.'})
    elseif n.kind=='charm_well'then
        add({id='focus',label='Focus · +2 to next direct attack',focus=true,detail='One primary attack batch, expires after three committed actions; replaces a supply condition.'})
        add({id='ward',label='Brace · one-hit ward',ward=true,detail='One positive hit, expires after three committed actions; replaces a supply condition.'})
    elseif n.kind=='cleanse_flask' or n.kind=='escape_scroll' or n.kind=='return_token'then
        add(utility_entry(p,n.kind,0))
    elseif n.kind=='travelling_trader'then
        add({id='food',label='Food · +4 HP · 2 gold',price=2,heal=4,enabled=p.hp<p.max_hp,reason='Already at full HP'})
        local e=utility_entry(p,n.stock,n.stock=='cleanse_flask' and 2 or 3);e.id='utility';add(e)
    elseif n.kind=='attunement_forge' or n.kind=='memorial'then
        result.detail=n.kind=='memorial' and 'Retire exactly one rarity stack; pending committed effects remain. Healing is capped at missing HP.' or 'Trade exactly one rarity stack + 3 gold for a fixed same-rarity alternative. Inspect a stack first.'
        local ids={};for id in pairs(p.boons)do ids[#ids+1]=id end;table.sort(ids)
        for _,bid in ipairs(ids)do for _,rarity in ipairs(Catalog.rarities)do
            local count=p.boons[bid][rarity] or 0
            if count>0 and (not selection or selection.id==bid and selection.rarity==rarity)then
                local remove={id=bid,rarity=rarity}
                if n.kind=='memorial'then
                    local heal=math.ceil(p.max_hp*.35)
                    add({id=bid..'|'..rarity,remove=remove,heal=heal,label=Catalog.get(bid).name..' · '..rarity..' x'..count,
                        detail=('Retire one; %d stacks remain in this rarity. Recover %d HP.'):format(count-1,math.min(heal,p.max_hp-p.hp)),
                        enabled=p.hp<p.max_hp,reason='Already at full HP'})
                elseif not selection then
                    add({id=bid..'|'..rarity,select=remove,label=Catalog.get(bid).name..' · '..rarity..' x'..count})
                else
                    for index,id in ipairs(n.trades[bid] and n.trades[bid][rarity] or {})do
                        local offer={boon_id=id,rarity=rarity}
                        add({id=tostring(index),remove=remove,offer=offer,price=3,label=Catalog.get(id).name..' · '..rarity..' · 3 gold',
                            detail='Retire one '..Catalog.get(bid).name..' ('..rarity..'); other stacks remain. New ability geometry is recalculated.',
                            enabled=grant_probe(p,offer,remove),reason='Incompatible replacement'})
                    end
                end
            end
        end end
        if #entries==0 then result.detail='No eligible owned stack / replacement. Nothing is spent.'end
    elseif n.kind=='bell'then
        local g=state.idea_rooms.groups[n.group]
        result.title='Bell '..n.bell;result.detail='Clue '..table.concat(g.sequence,' → ')..' · progress '..g.progress..'/3. Wrong bells reset progress; one 5-gold reward.'
        if not g.completed then add({id='ring',label='Ring bell '..n.bell})else result.detail='Sequence completed; reward already collected.'end
    elseif n.kind=='falling_fixture'then
        result.detail='One use. Locked five-tile cross, base 4 physical damage to everyone, impact in +100 world time. No guaranteed reaction turn.'
        local visible=true;for _,c in ipairs(n.cells)do if not Visibility.can_see(state,n.floor_id,c.x,c.y)then visible=false end end
        result.cells=copy(n.cells);add({id='trigger',label='Trigger falling fixture',enabled=visible,reason='See the complete impact cross before triggering',detail='Confirm only if its marked cross is safe for you.'})
    elseif n.kind=='archive_contract'then
        result.detail='Clear marked ordinary enemies without leaving this room or using a movement ability. Walking is allowed; abilities are never disabled. Reward: 8 gold once.'
        if n.status=='offered'then
            local available=#n.targets>0
            for _,id in ipairs(n.targets)do local e=actor(state,id)
                if not e or not inside(n.room,e.position.x,e.position.y)then available=false end
            end
            add({id='accept',label='Accept room contract',enabled=available,reason='Original targets have already left or died'})
        elseif n.status=='active'then add({id='abandon',label='Abandon contract',detail='Normal controls and kill rewards remain.'})
        else result.detail='Contract '..n.status..': '..(n.failure or 'reward collected')end
    end
    return result
end
function I.preview(state,intent)
    local options,reason=I.options(state,intent.node_id,intent.selection)
    if not options then return {valid=false,reason=reason}end
    for _,e in ipairs(options.entries)do if e.id==intent.choice_id and not e.select then
        if not e.enabled then return {valid=false,reason=e.reason or 'unavailable'}end
        if e.replace_required and intent.replace~=true then return {valid=false,reason='confirm replacing the carried utility'}end
        return {valid=true,entry=e,node_id=intent.node_id,cells=options.cells or {}}
    end end
    return {valid=false,reason='choose a valid interaction option'}
end
local function cleanse(state,p,emit)
    local burn=p.statuses and p.statuses.burning
    if burn then
        for index=#state.schedule,1,-1 do if state.schedule[index].id==burn.schedule_id then table.remove(state.schedule,index)end end
        p.statuses.burning=nil
        emit('StatusCleansed',{status='burning',target_id=p.id,floor_id=p.position.floor_id,x=p.position.x,y=p.position.y},p.id)
    end
end
local function heal(p,amount,emit,kind)
    local old=p.hp;p.hp=math.min(p.max_hp,p.hp+amount)
    local pickup=kind=='ration_cache' or kind=='travelling_trader'
    emit(pickup and 'SupplyCollected' or 'Healed',{supply_kind=pickup and 'food' or nil,
        target_id=p.id,requested=amount,amount=amount,actual_gain=p.hp-old,hp_before=old,hp_after=p.hp,
        floor_id=p.position.floor_id,x=p.position.x,y=p.position.y},p.id)
end
function I.apply(state,preview,action_id,emit)
    local p=player(state);local n=assert(I.node(state,preview.node_id));local e=preview.entry
    if e.price>0 then
        local old=state.currency;state.currency=old-e.price
        emit('CurrencySpent',{amount=e.price,old_currency=old,new_currency=state.currency},p.id)
    end
    if e.hp_cost then p.hp=p.hp-e.hp_cost;emit('FontOfferingPaid',{amount=e.hp_cost,hp_after=p.hp,floor_id=n.floor_id,x=n.x,y=n.y},p.id)end
    if e.remove then
        local r=e.remove;local count=p.boons[r.id][r.rarity]
        Boons.set_stacks(p,r.id,r.rarity,count-1)
        if not p.boons[r.id] and p.boon_memory then p.boon_memory[r.id]=nil end
        emit('BoonRetired',{boon_id=r.id,rarity=r.rarity,old_count=count,new_count=count-1},p.id)
    end
    if e.offer then
        local o=e.offer;local count=p.boons[o.boon_id] and p.boons[o.boon_id][o.rarity] or 0
        Boons.set_stacks(p,o.boon_id,o.rarity,count+1)
        emit('BoonGranted',{boon_id=o.boon_id,rarity=o.rarity,old_count=count,new_count=count+1},p.id)
    end
    if e.heal then heal(p,e.heal,emit,n.kind)end
    if e.cleanse then cleanse(state,p,emit)end
    if e.ward or e.focus then
        p.pickup_ward_actions=e.ward and 4 or 0;p.supply_focus_actions=e.focus and 4 or 0
        emit('SupplyPrepared',{condition=e.ward and 'ward' or 'focus',actions=3,floor_id=n.floor_id,x=n.x,y=n.y},p.id)
    end
    if e.utility then p.utility={kind=e.utility};emit('UtilityCollected',{utility_kind=e.utility,floor_id=n.floor_id,x=n.x,y=n.y},p.id)end
    if n.kind=='bell'then
        local g=state.idea_rooms.groups[n.group]
        local correct=n.bell==g.sequence[g.progress+1]
        g.progress=correct and g.progress+1 or 0
        emit('BellRung',{bell=n.bell,correct=correct,progress=g.progress,floor_id=n.floor_id,x=n.x,y=n.y},p.id)
        if g.progress==3 then
            g.completed=true;local old=state.currency;state.currency=old+5
            emit('CurrencyCollected',{amount=5,old_currency=old,new_currency=state.currency,floor_id=n.floor_id,x=n.x,y=n.y},p.id)
        end
    elseif n.kind=='falling_fixture'then
        local w=Scheduler.enqueue(state,{kind='effect',actor_id=0,due=state.clock+Def.fixture_delay,attack_id=action_id,floor_id=n.floor_id})
        w.effect_id=w.id;w.idea_fixture=n.id
        n.pending={schedule_id=w.id,due=w.due,action_id=action_id,cells=copy(n.cells),owner_id=p.id}
        emit('FixtureLocked',{target_cells=n.cells,due=w.due,damage=Def.fixture_damage,floor_id=n.floor_id,x=n.center_x,y=n.center_y},p.id)
        n.spent=true
    elseif n.kind=='archive_contract'then
        n.status=e.id=='accept' and 'active' or 'failed';n.failure=e.id=='abandon' and 'abandoned' or nil
        emit('ContractChanged',{status=n.status,floor_id=n.floor_id,x=n.x,y=n.y},p.id)
    else n.spent=true end
    emit('InteractionUsed',{interaction=n.kind,node_id=n.id,choice_id=e.id,floor_id=n.floor_id,x=n.x,y=n.y},p.id)
end

function I.nearby(state)
    local result={}
    for _,n in ipairs(state.idea_rooms and state.idea_rooms.nodes or {})do
        if valid_access(state,n)then result[#result+1]={node_id=n.id,title=Def.names[n.kind],kind=n.kind,x=n.x,y=n.y,spent=n.spent}end
    end
    table.sort(result,function(a,b)if a.spent~=b.spent then return not a.spent end
        local da,db=dist(player(state).position,a),dist(player(state).position,b)
        if da~=db then return da<db end;return a.node_id<b.node_id end)
    return result
end

function I.utility_options(state)
    local p=player(state);local u=p.utility
    if not u then return {title='UTILITY SLOT',detail='Empty. Find a supply node or trader.',entries={}}end
    local detail=u.kind=='cleanse_flask' and 'Remove Burning. One paid action; not a heal.' or u.kind=='escape_scroll' and 'Choose a visible, clear cardinal path of 1–3 tiles. One paid action.'
        or u.planted and 'One paid return to the visible empty marker. Leaving the room forfeits it.' or 'Plant at your room cell, then return once. Two paid actions; leaving that room forfeits the marker.'
    return {title=Def.names[u.kind],detail=detail,entries={{id='use',label=u.kind=='return_token' and (u.planted and 'Return to token' or 'Plant return token') or 'Use '..Def.names[u.kind],enabled=true}}}
end
function I.utility_preview(state,intent)
    local p=player(state);local u=p.utility;local map=Content.map_for(state)
    if not u then return {valid=false,reason='utility slot is empty',cells={}}end
    local result={valid=true,cells={},utility_kind=u.kind}
    if u.kind=='cleanse_flask'then
        if not (p.statuses and p.statuses.burning)then result.valid=false;result.reason='not Burning; charge retained'end
    elseif u.kind=='escape_scroll'then
        local dx,dy,steps=intent.dx,intent.dy,intent.steps or 3
        if type(dx)~='number' or type(dy)~='number' or math.abs(dx)+math.abs(dy)~=1 or dx%1~=0 or dy%1~=0
            or type(steps)~='number' or steps%1~=0 or steps<1 or steps>3 then return {valid=false,reason='choose cardinal direction and 1–3 tiles',cells={}}end
        for i=1,steps do
            local x,y=p.position.x+dx*i,p.position.y+dy*i
            local legal=Content.walkable(map,x,y) and Visibility.can_see(state,map.floor_id,x,y)
                and not blocked(state,map.floor_id,x,y) and not require('src.floors').portal(state,x,y)
            result.cells[#result.cells+1]={x=x,y=y,status=legal and 'open' or 'blocked'}
            if not legal then result.valid=false;result.reason='path needs visible clear cells, without stairs' end
        end
        result.distance=steps;result.utility_move=true
    elseif not u.planted then
        local room=I.room_at(map,p.position.x,p.position.y)
        if not room then result.valid=false;result.reason='plant inside a room'else result.plant_room=copy(room)end
    else
        local b=u.planted
        if b.floor_id~=map.floor_id or not inside(b.room,p.position.x,p.position.y) or
            not Visibility.can_see(state,map.floor_id,b.x,b.y) or blocked(state,map.floor_id,b.x,b.y)then
            return {valid=false,reason='marker must be visible, empty and in this room',cells={}}
        end
        local queue,seen={{x=p.position.x,y=p.position.y,d=0}},{[key(p.position.x,p.position.y)]=true};local head=1;local found
        while head<=#queue do local q=queue[head];head=head+1
            if q.x==b.x and q.y==b.y then found=q;break end
            if q.d<Def.return_range then for _,d in ipairs(dirs)do local x,y=q.x+d[1],q.y+d[2]
                if not seen[key(x,y)] and inside(b.room,x,y) and Content.walkable(map,x,y)
                    and Visibility.was_explored(state,map.floor_id,x,y) and not blocked(state,map.floor_id,x,y)
                    and not require('src.floors').portal(state,x,y)then
                    seen[key(x,y)]=true;queue[#queue+1]={x=x,y=y,d=q.d+1}
                end
            end end
        end
        if not found or found.d==0 then return {valid=false,reason='marker needs a known clear path of 1–4 steps',cells={}}end
        result.cells={{x=b.x,y=b.y,status='open'}};result.distance=1;result.return_distance=found.d;result.utility_move=true;result.utility_return=true
    end
    return result
end
function I.use_utility(state,preview,emit)
    local p=player(state);local u=assert(p.utility)
    if u.kind=='return_token' and not u.planted then
        u.planted={floor_id=p.position.floor_id,x=p.position.x,y=p.position.y,room=copy(preview.plant_room)}
        emit('ReturnTokenPlanted',{floor_id=p.position.floor_id,x=p.position.x,y=p.position.y},p.id)
    else
        if u.kind=='cleanse_flask'then cleanse(state,p,emit)end
        p.utility=nil;emit('UtilityUsed',{utility_kind=u.kind,floor_id=p.position.floor_id,x=p.position.x,y=p.position.y},p.id)
    end
end
local function fail(n,reason,emit,pid)
    n.status='failed';n.failure=reason
    emit('ContractChanged',{status='failed',reason=reason,floor_id=n.floor_id,x=n.x,y=n.y},pid)
end
function I.before_action(state,kind,preview,emit)
    if kind=='dash' and (preview.distance or 0)>0 or kind=='use_utility' and preview.utility_move then
        for _,n in ipairs(state.idea_rooms and state.idea_rooms.nodes or {})do
            if n.kind=='archive_contract' and n.status=='active'then fail(n,'movement ability used',emit,state.player_id)end
        end
    end
end
function I.on_event(state,event,emit)
    local s=state.idea_rooms;if not s then return end
    local p=player(state)
    local moved=(event.kind=='TileEntered' or event.kind=='FloorChanged') and event.source_id==p.id
        or event.kind=='Displaced' and event.target_id==p.id
    if moved then
        local b=p.utility and p.utility.planted
        if b and (event.floor_id~=b.floor_id or not inside(b.room,event.x,event.y))then
            p.utility=nil;emit('ReturnTokenExpired',{floor_id=event.floor_id,x=event.x,y=event.y},p.id)
        end
    end
    for _,n in ipairs(s.nodes)do if n.kind=='archive_contract' and n.status=='active'then
        if moved
            and (event.floor_id~=n.floor_id or not inside(n.room,event.x,event.y))then fail(n,'left the room',emit,p.id)
        elseif event.kind=='EntityKilled'then
            local victim=event.target_id
            for _,id in ipairs(n.targets)do
                if victim==id or event.revival_of==id then
                    if event.source_faction~='player'then fail(n,'target defeated by another faction',emit,p.id)
                    else n.defeated[tostring(id)]=true end
                end
            end
        end
    end end
end
function I.after_action(state,emit)
    local p=player(state)
    if (p.supply_focus_actions or 0)>0 then p.supply_focus_actions=p.supply_focus_actions-1 end
    if p.hp<=0 then return end
    for _,n in ipairs(state.idea_rooms and state.idea_rooms.nodes or {})do
        if n.kind=='archive_contract' and n.status=='active'then
            local complete=true
            for _,id in ipairs(n.targets)do
                if not n.defeated[tostring(id)] or actor(state,id)then complete=false end
                for _,c in ipairs(state.idea_corpses or {})do if c.id==id then complete=false end end
                for _,e in ipairs(state.entities)do if e.revival_of==id then complete=false end end
            end
            if complete then
                n.status='completed';n.spent=true;local old=state.currency;state.currency=old+8
                emit('ContractChanged',{status='completed',reward=8,floor_id=n.floor_id,x=n.x,y=n.y},p.id)
                emit('CurrencyCollected',{amount=8,old_currency=old,new_currency=state.currency,floor_id=n.floor_id,x=n.x,y=n.y},p.id)
            end
        end
    end
end
function I.resolve(state,item,fire)
    local n=assert(I.node(state,item.idea_fixture),'orphan fixture')
    local pending=assert(n.pending,'missing fixture commitment')
    assert(pending.schedule_id==item.id and pending.due==state.clock and n.floor_id==state.active_floor_id,'mistimed fixture')
    n.pending=nil;fire(n,pending)
end
function I.focus(p)
    if (p.supply_focus_actions or 0)>0 then p.supply_focus_actions=0;return 2 end
    return 0
end
function I.focus_bonus(p)return (p.supply_focus_actions or 0)>0 and 2 or 0 end
function I.status(state)
    local p=player(state);local parts={}
    if p.utility then parts[#parts+1]=Def.names[p.utility.kind]..(p.utility.planted and ' (planted)' or '')end
    if (p.supply_focus_actions or 0)>0 then parts[#parts+1]='Focus '..p.supply_focus_actions..' actions'end
    if (p.pickup_ward_actions or 0)>0 then parts[#parts+1]='Ward '..p.pickup_ward_actions..' actions'end
    return table.concat(parts,' · ')
end
function I.marked(state,entity)
    for _,n in ipairs(state.idea_rooms and state.idea_rooms.nodes or {})do
        if n.kind=='archive_contract' and n.status=='active' and n.floor_id==entity.position.floor_id then
            for _,id in ipairs(n.targets)do if entity.id==id or entity.revival_of==id then return true end end
        end
    end
    return false
end
local function integer(v,lo,hi)return type(v)=='number' and v%1==0 and v>=lo and v<=hi end
function I.validate(state,maps)
    local p=player(state);local u=p.utility
    assert(p.supply_focus_actions==nil or integer(p.supply_focus_actions,0,3),'invalid supply focus')
    assert(p.pickup_ward_actions==nil or integer(p.pickup_ward_actions,0,3),'invalid supply ward')
    if u then
        assert(type(u)=='table' and (u.kind=='cleanse_flask' or u.kind=='escape_scroll' or u.kind=='return_token'),'invalid utility')
        if u.planted then
            local b=u.planted;local map=maps[b.floor_id]
            assert(u.kind=='return_token' and map and b.floor_id==p.position.floor_id
                and integer(b.x,1,map.width) and integer(b.y,1,map.height) and Content.walkable(map,b.x,b.y)
                and type(b.room)=='table' and inside(b.room,b.x,b.y) and inside(b.room,p.position.x,p.position.y),'invalid token marker')
            local r=I.room_at(map,b.x,b.y)
            assert(r and r.id==b.room.id and r.x==b.room.x and r.y==b.room.y
                and r.width==b.room.width and r.height==b.room.height,'invalid token room')
        end
    end
    local s=state.idea_rooms
    if not s then assert(not u,'utility without room state');return true end
    assert(type(s)=='table' and s.version==Def.version and type(s.nodes)=='table'
        and #s.nodes<=32 and type(s.groups)=='table','invalid interaction store')
    local pool={};for _,id in ipairs(eligible(state))do pool[id]=true end
    local ids,locations,pending,groups={},{},{},{}
    local function check_offer(o)
        assert(type(o)=='table' and pool[o.boon_id],'offer outside frozen class pool')
        local good=false;for _,r in ipairs(Catalog.rarities)do if r==o.rarity then good=true end end
        assert(good,'invalid interaction rarity')
    end
    for _,n in ipairs(s.nodes)do
        local map=maps[n.floor_id]
        assert(map and Def.names[n.kind] and integer(n.x,1,map.width) and integer(n.y,1,map.height)
            and Content.walkable(map,n.x,n.y) and type(n.spent)=='boolean'
            and n.id==n.floor_id..':interaction:'..n.kind..':'..key(n.x,n.y) and not ids[n.id],'invalid interaction identity')
        ids[n.id]=true
        local k=n.floor_id..':'..key(n.x,n.y);assert(not locations[k],'overlapping interactions');locations[k]=true
        if n.kind=='offering_font'then
            assert(type(n.offers)=='table' and #n.offers==3,'invalid font offers');local seen={}
            for _,o in ipairs(n.offers)do check_offer(o);assert(not seen[o.boon_id],'duplicate font offer');seen[o.boon_id]=true end
        elseif n.kind=='attunement_forge'then
            assert(type(n.trades)=='table','missing forge alternatives')
            for id in pairs(pool)do
                assert(type(n.trades[id])=='table','missing forge pool')
                for _,rarity in ipairs(Catalog.rarities)do
                    local list=n.trades[id][rarity];assert(type(list)=='table' and #list==3,'invalid forge alternatives')
                    local seen={}
                    for _,bid in ipairs(list)do assert(pool[bid] and bid~=id and not seen[bid],'invalid forge offer');seen[bid]=true end
                end
            end
            for id in pairs(n.trades)do assert(pool[id],'forge offer outside frozen class pool')end
        elseif n.kind=='travelling_trader'then
            assert(n.stock=='cleanse_flask' or n.stock=='escape_scroll','invalid trader stock')
        elseif n.kind=='bell' or n.kind=='bell_clue'then
            local g=s.groups[n.group];assert(g and g.floor_id==n.floor_id,'missing bell group')
            groups[n.group]=groups[n.group] or {bells={},clues=0}
            if n.kind=='bell'then
                assert(integer(n.bell,1,3) and not groups[n.group].bells[n.bell],'duplicate bell')
                groups[n.group].bells[n.bell]=true
            else groups[n.group].clues=groups[n.group].clues+1 end
        elseif n.kind=='falling_fixture'then
            assert(type(n.cells)=='table' and #n.cells==5 and integer(n.center_x,1,map.width)
                and integer(n.center_y,1,map.height),'invalid fixture cross')
            local seen={}
            for _,c in ipairs(n.cells)do
                assert(c.floor_id==n.floor_id and integer(c.x,1,map.width) and integer(c.y,1,map.height)
                    and Content.walkable(map,c.x,c.y) and dist(c,{x=n.center_x,y=n.center_y})<=1
                    and not seen[key(c.x,c.y)],'invalid fixture cell');seen[key(c.x,c.y)]=true
            end
            if n.pending then
                local r=n.pending;local clock=state.floor_pause and state.floor_pause[n.floor_id]
                    and state.floor_pause[n.floor_id].suspended_at or state.clock
                assert(n.spent and r.owner_id==p.id and integer(r.schedule_id,1,state.next_schedule_id-1)
                    and integer(r.action_id,1,state.next_action_id-1) and integer(r.due,clock,clock+Def.fixture_delay)
                    and type(r.cells)=='table' and #r.cells==5,'invalid fixture commitment')
                for i,c in ipairs(r.cells)do local a=n.cells[i];assert(c.x==a.x and c.y==a.y and c.floor_id==a.floor_id,'changed locked fixture cells')end
                pending[r.schedule_id]={node=n,record=r}
            end
        elseif n.kind=='route_sign'then
            assert(integer(n.dx,-1,1) and integer(n.dy,-1,1) and math.abs(n.dx)+math.abs(n.dy)==1
                and (n.hint=='Treasure' or n.hint=='Supplies' or n.hint=='Rooms / danger'),'invalid route hint')
        elseif n.kind=='archive_contract'then
            local r=I.room_at(map,n.x,n.y)
            assert(r and type(n.room)=='table' and r.id==n.room_id and r.id==n.room.id
                and r.x==n.room.x and r.y==n.room.y and r.width==n.room.width and r.height==n.room.height
                and type(n.targets)=='table' and #n.targets>=1 and #n.targets<=64
                and type(n.defeated)=='table' and (n.status=='offered' or n.status=='active' or n.status=='failed' or n.status=='completed'), 'invalid contract')
            assert(n.spent==(n.status=='completed'),'invalid contract reward flag')
            local seen={};for _,id in ipairs(n.targets)do assert(integer(id,1,state.next_entity_id-1) and not seen[tostring(id)],'invalid contract target');seen[tostring(id)]=true end
            for id,v in pairs(n.defeated)do assert(seen[id] and v==true,'invalid defeated contract target')end
            assert(n.failure==nil or n.failure=='left the room' or n.failure=='movement ability used'
                or n.failure=='target defeated by another faction' or n.failure=='abandoned','invalid contract failure')
        end
    end
    for id,g in pairs(s.groups)do
        local count=groups[id];assert(g.kind=='bells' and count and count.clues==1
            and count.bells[1] and count.bells[2] and count.bells[3]
            and type(g.sequence)=='table' and #g.sequence==3 and integer(g.progress,0,3)
            and type(g.completed)=='boolean' and g.completed==(g.progress==3),'invalid bell progress')
        local seen={};for _,v in ipairs(g.sequence)do assert(integer(v,1,3) and not seen[v],'invalid bell sequence');seen[v]=true end
    end
    local scheduled={};for _,w in ipairs(state.schedule)do scheduled[#scheduled+1]=w end
    for _,pause in pairs(state.floor_pause or {})do for _,w in ipairs(pause.schedule)do scheduled[#scheduled+1]=w end end
    for _,w in ipairs(scheduled)do
        if w.idea_fixture then
            local r=pending[w.id];assert(r and w.idea_fixture==r.node.id and w.kind=='effect'
                and w.actor_id==0 and w.effect_id==w.id and w.attack_id==r.record.action_id
                and w.due==r.record.due and w.floor_id==r.node.floor_id,'orphan fixture schedule')
            pending[w.id]=nil
        end
    end
    assert(not next(pending),'missing fixture schedule')
    return true
end
return I
