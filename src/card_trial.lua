-- Trial orchestration and validation; all combat still executes in Game.submit.
-- Game is injected only when constructing/spawning to avoid a second engine.
local C=require('src.card_trial_content')
local Cards=require('src.cards')
local Content=require('src.content')
local Boons=require('src.boons')
local Rewards=require('src.rewards')
local Visibility=require('src.visibility')
local Trial={content=C}
local function copy(v)
    if type(v)~='table'then return v end
    local t={};for k,x in pairs(v)do t[k]=copy(x)end;return t
end
local function player(s)
    for _,e in ipairs(s.entities)do if e.id==s.player_id then return e end end
end
local function occupied(s,x,y)
    for _,e in ipairs(s.entities)do
        if e.blocks_movement and e.position.floor_id==s.active_floor_id
            and e.position.x==x and e.position.y==y then return true end
    end
end
function Trial.spawn_fight(s,spawn)
    local map=Content.map_for(s)
    for _,row in ipairs(C.fights[s.card_trial.fight].enemies)do
        local cells={}
        for y=2,map.height-1 do for x=2,map.width-1 do
            if Content.walkable(map,x,y) and not occupied(s,x,y)then
                cells[#cells+1]={x=x,y=y,d=math.abs(x-row[2])+math.abs(y-row[3])}
            end
        end end
        table.sort(cells,function(a,b)
            if a.d~=b.d then return a.d<b.d end
            if a.y~=b.y then return a.y<b.y end
            return a.x<b.x
        end)
        local at=assert(cells[1],'trial arena has no free spawn')
        spawn(s,row[1],at.x,at.y,s.clock)
    end
    Visibility.recompute(s)
end
function Trial.new(Game,seed,preset)
    seed=seed or C.seed;preset=preset or 'bare'
    local def=assert(C.presets[preset],'unknown trial preset')
    local s=Game.new(seed)
    s.final_map=copy(C.arena);s.final_map.seed=seed
    s.active_floor_id=s.final_map.floor_id
    local p=player(s);p.position={floor_id=s.active_floor_id,x=C.arena.start_x,y=C.arena.start_y}
    s.schedule[1].floor_id=s.active_floor_id
    s.card_trial={version=C.version,seed=seed,preset=preset,fight=1,phase='combat',
        cards=Cards.new(seed),healing_charges=C.healing_charges,commits=0}
    for _,b in ipairs(def.boons)do Boons.set_stacks(p,'core:boon/'..b[1],b[2],1)end
    Trial.spawn_fight(s,Game.spawn_enemy)
    return s
end
local function invalid(reason)return {valid=false,reason=reason,cells={}}end
function Trial.preview(s,intent,preview,effective_cost)
    local t=s.card_trial
    if not t or t.phase~='combat' or s.game_over then return invalid('The fight is resolved; choose a reward or replay')end
    if type(intent)~='table' then return invalid('Choose a basic action or technique')end
    if intent.vow~=nil or intent.steps~=nil or intent.cards~=nil then return invalid('Unsupported trial action option')end
    local p=player(s);local kind,card
    if intent.kind=='trial_basic' then
        if intent.card_id~=nil then return invalid('Basic actions do not use a card')end
        if intent.action~='move' and intent.action~='basic_attack' then return invalid('Choose Step or Strike')end
        kind=intent.action
    elseif intent.kind=='technique' or intent.kind=='replace_card' then
        if not Cards.owned(t.cards,intent.card_id)then return invalid('That technique is not in your hand')end
        card=Cards.card(t.cards,intent.card_id)
        kind=intent.kind=='replace_card' and 'wait' or card.action
        if intent.action~=nil then return invalid('The card determines its technique')end
    else return invalid('Use a basic action or a named technique')end
    local result,dx,dy
    local speed=p.speed+Boons.speed_bonus(s,p)
    if kind=='trial_heal' then
        if t.healing_charges<=0 then return invalid('Healing exhausted')end
        if p.hp>=p.max_hp then return invalid('Full health')end
        result={valid=true,cells={},heal=math.min(card.heal,p.max_hp-p.hp),requested_heal=card.heal,
            cost=effective_cost(100,speed)}
    elseif intent.kind=='trial_basic' or kind=='wait' then
        dx,dy=intent.dx,intent.dy;result=preview(s,kind,dx,dy)
    else
        if type(intent.x)~='number' or intent.x%1~=0 or type(intent.y)~='number' or intent.y%1~=0 then
            return invalid('Choose a highlighted target')
        end
        dx,dy=intent.x-p.position.x,intent.y-p.position.y
        if kind=='knight_hop' then
            if not (math.abs(dx)==2 and math.abs(dy)==1 or math.abs(dx)==1 and math.abs(dy)==2)then
                return invalid('Jump two tiles on one axis and one on the other')end
            local map=Content.map_for(s);local x,y=intent.x,intent.y
            local valid=x>1 and x<map.width and y>1 and y<map.height and Content.walkable(map,x,y) and not occupied(s,x,y)
            result={valid=valid,reason=not valid and 'Landing must be a free walkable tile' or nil,
                cost=effective_cost(require('src.classes').for_player(p).costs.dash,speed),
                cells={{x=x,y=y,status=valid and 'open' or 'occupied'}},landing={x=x,y=y},jump=true}
        else
            if kind=='sweeping_slash' and math.max(math.abs(dx),math.abs(dy))~=1 then
                return invalid('Aim at one of the eight adjacent directions')end
            dx=dx==0 and 0 or dx>0 and 1 or -1;dy=dy==0 and 0 or dy>0 and 1 or -1
            result=preview(s,kind,dx,dy)
            if kind=='dash' and result.valid then
                local c=result.cells[result.distance]
                if not c or c.x~=intent.x or c.y~=intent.y then return invalid('Choose the actual Dash landing')end
                result.landing={x=c.x,y=c.y}
            end
        end
    end
    if result.door_id or result.vault_id or result.stair then return invalid('Use a clear arena tile')end
    if not result.valid then return result end
    result.card_id=intent.card_id;result.replace=intent.kind=='replace_card'
    result.intent={kind=kind,dx=dx,dy=dy}
    return result
end
-- Legal target markers are obtained from exactly the same preview as commitment.
function Trial.targets(s,intent,validate)
    local out={};local p=player(s).position;local map=Content.map_for(s)
    if intent.kind~='technique' then return out end
    local def=Cards.card(s.card_trial.cards,intent.card_id)
    if not def then return out end
    if def.heal then
        local r=validate(s,intent)
        return r.valid and {{x=p.x,y=p.y,preview=r}} or out
    end
    for y=2,map.height-1 do for x=2,map.width-1 do
        local probe=copy(intent);probe.x,probe.y=x,y
        local result=validate(s,probe)
        if result.valid then out[#out+1]={x=x,y=y,preview=result}end
    end end
    return out
end
function Trial.after_action(s,emit)
    local t=s.card_trial
    if player(s).hp<=0 then t.phase='death';s.game_over=true;return end
    for _,e in ipairs(s.entities)do
        if e.faction=='enemy' and e.hp and e.hp>0 then return end
    end
    if t.fight==#C.fights then
        t.phase='victory';s.game_over=true
        emit('CardTrialWon',{floor_id=s.active_floor_id},s.player_id)
        return
    end
    t.phase='reward'
    local p=player(s).position;local at
    for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
        local x,y=p.x+d[1],p.y+d[2]
        if Content.walkable(Content.map_for(s),x,y) and not occupied(s,x,y)then at={x=x,y=y};break end
    end
    assert(at,'no adjacent reward tile')
    local chest={id=s.next_entity_id,definition_id='core:chest/free_elite',controller='chest',
        faction='neutral',blocks_movement=true,claimed=false,price=0,offers=copy(C.rewards),
        position={floor_id=s.active_floor_id,x=at.x,y=at.y},trial_reward=true}
    s.next_entity_id=chest.id+1;s.entities[#s.entities+1]=chest;t.reward_id=chest.id
end
function Trial.reward(s,index)
    local t=s.card_trial
    if not t or t.phase~='reward' or s.game_over then return nil,'Reward unavailable'end
    local chest
    for _,e in ipairs(s.entities)do if e.id==t.reward_id then chest=e;break end end
    if not chest then return nil,'Reward missing'end
    if type(index)~='number' or index%1~=0 or index<1 or index>#chest.offers then return nil,'Choose a reward'end
    if chest.offers[index].upgrade_id==C.upgrade.id then
        if chest.claimed then return nil,'Reward already claimed'end
        return chest
    end
    local ok,why=Rewards.can_claim(s,chest,index)
    if not ok then return nil,why end
    return chest
end
return Trial
