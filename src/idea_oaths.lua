-- One finite enemy pair, sharing mitigated HP loss inside the primary batch.
local Sight=require('src.enemy_squads')
local O={radius=4,a='core:enemy/oath_twin_a',b='core:enemy/oath_twin_b'}
function O.is_twin(e)return e and (e.definition_id==O.a or e.definition_id==O.b)end
local function actor(state,id)
    for _,e in ipairs(state.entities)do if e.id==id then return e end end
end
function O.partner(state,e)
    if not O.is_twin(e) or e.oath_broken or not e.hp or e.hp<=0 then return end
    local other=actor(state,e.oath_partner_id)
    if O.is_twin(other) and not other.oath_broken and other.hp>0
        and other.oath_partner_id==e.id and other.position.floor_id==e.position.floor_id
        and Sight.distance(e.position,other.position)<=O.radius
        and Sight.clear(state,e.position,other.position)
        and Sight.clear(state,other.position,e.position)then return other end
end
function O.prune(state)
    for _,e in ipairs(state.entities)do
        if O.is_twin(e) and not e.oath_broken and not O.partner(state,e)then
            e.oath_broken=true
            local other=actor(state,e.oath_partner_id)
            if O.is_twin(other)then other.oath_broken=true end
        end
    end
end
local function copy(v)
    if type(v)~='table'then return v end
    local out={};for k,x in pairs(v)do out[k]=copy(x)end;return out
end
function O.redistribute(state,outcomes)
    local active=false
    for _,hit in ipairs(outcomes)do
        if hit.target_definition_id==O.a or hit.target_definition_id==O.b then active=true;break end
    end
    if not active then return end
    local indexed,received={},{}
    local count=#outcomes
    for i=1,count do indexed[outcomes[i].target_id]=outcomes[i]end
    -- Plan every transfer using pre-batch HP and intact links, never the
    -- progressively committed damage of another primary target.
    for i=1,count do
        local hit=outcomes[i]
        local e=actor(state,hit.target_id)
        local partner=e and O.partner(state,e)
        if partner and not hit.oath_shared_only and not hit.fate_skull then
            local transfer=math.floor(hit.actual_loss/2)
            if transfer>0 then
                hit.actual_loss=hit.actual_loss-transfer
                hit.hp_after=hit.hp_before-hit.actual_loss;hit.oath_given=transfer
                local r=received[partner.id] or {amount=0,template=hit}
                r.amount=r.amount+transfer;received[partner.id]=r
            end
        end
    end
    local ids={};for id in pairs(received)do ids[#ids+1]=id end;table.sort(ids)
    for _,id in ipairs(ids)do
        local data=received[id];local e=assert(actor(state,id))
        local hit=indexed[id]
        if not hit then
            hit=copy(data.template)
            hit.target_id=e.id;hit.target_definition_id=e.definition_id
            hit.target_faction=e.faction;hit.target_tags={e.definition_id,e.faction}
            hit.floor_id=e.position.floor_id;hit.x,hit.y=e.position.x,e.position.y
            hit.hp_before=e.hp;hit.target_max_hp=e.max_hp;hit.actual_loss=0
            hit.attempted_damage=data.amount;hit.after_defense=data.amount
            hit.ward_absorbed=0;hit.pickup_ward=nil;hit.reversal_bonus=nil
            hit.block_reason=nil;hit.protecting_id=nil;hit.oath_given=nil
            hit.direct_attack=false;hit.critical=false;hit.damage_type='oath'
            hit.ability_id='core:ability/oath_share';hit.oath_shared_only=true
            hit.proc_coefficient=0;hit.reward_exempt=e.reward_exempt or nil
            hit.target_burning=e.statuses and e.statuses.burning~=nil or false
            hit.adjacent_hostile_count=nil;hit.adjacent_burning_count=nil
            outcomes[#outcomes+1]=hit
        end
        hit.actual_loss=math.min(hit.hp_before,hit.actual_loss+data.amount)
        hit.hp_after=hit.hp_before-hit.actual_loss;hit.oath_received=data.amount
    end
end
function O.validate(state)
    for _,e in ipairs(state.entities)do
        if O.is_twin(e) or e.oath_partner_id or e.oath_broken then
            local id=e.oath_partner_id;local other=actor(state,id)
            assert(O.is_twin(e) and e.controller=='ai' and type(id)=='number'
                and id%1==0 and id>=1 and id<state.next_entity_id and id~=e.id
                and (e.oath_broken==nil or e.oath_broken==true),'invalid oath owner')
            if other then
                assert(O.is_twin(other) and other.definition_id~=e.definition_id
                    and other.oath_partner_id==e.id,'invalid oath pair')
            end
            assert(e.oath_broken or O.partner(state,e),'invalid intact oath')
        end
    end
end
return O
