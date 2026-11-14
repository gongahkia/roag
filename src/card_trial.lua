-- Trial orchestration and validation; all combat still executes in Game.submit.
-- Game is injected only when constructing/spawning to avoid a second engine.
local C=require('src.card_trial_content')
local Cards=require('src.cards')
local Content=require('src.content')
local Boons=require('src.boons')
local Rewards=require('src.rewards')
local Visibility=require('src.visibility')
local Poker=require('src.poker')
local RNG=require('src.rng')
local Classes=require('src.classes')
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
    local fights=s.card_trial.mode=='boggle' and require('src.boggle_content').fights or C.fights
    for _,row in ipairs(fights[s.card_trial.fight].enemies)do
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
function Trial.new(Game,seed,preset,mode)
    seed=seed or C.seed;preset=preset or 'bare'
    mode=mode or 'poker';assert(mode=='poker' or mode=='fixed' or mode=='boggle' or mode=='dice','unknown trial policy')
    if mode=='boggle' or mode=='dice'then preset='bare'end
    local def=assert(C.presets[preset],'unknown trial preset')
    local s=Game.new(seed)
    s.final_map=copy(C.arena);s.final_map.seed=seed
    s.active_floor_id=s.final_map.floor_id
    local p=player(s);p.position={floor_id=s.active_floor_id,x=C.arena.start_x,y=C.arena.start_y}
    s.schedule[1].floor_id=s.active_floor_id
    s.card_trial={version=C.version,seed=seed,preset=preset,fight=1,phase='combat',
        mode=mode,cards=mode=='poker' and Cards.new(seed) or nil,
        stamina=mode=='fixed' and C.fixed_stamina or 0,resource_phase=mode=='fixed' and 'spend' or 'prepare',
        retained={},exchange_used=false,cycle=1,healing_charges=C.healing_charges,commits=0}
    if mode=='poker'then
        local rng=RNG.new(RNG.derive(seed,'card-trial:deck-reward/v3'))
        local a=RNG.next_int(rng,52);local b=RNG.next_int(rng,51);if b>=a then b=b+1 end
        s.card_trial.deck_offers={a,b}
    end
    for _,b in ipairs(def.boons)do Boons.set_stacks(p,'core:boon/'..b[1],b[2],1)end
    Trial.spawn_fight(s,Game.spawn_enemy)
    if mode=='boggle'then require('src.boggle').initialize(s)end
    if mode=='dice'then require('src.dice_trial').initialize(s)end
    return s
end
local function invalid(reason)return {valid=false,reason=reason,cells={}}end
function Trial.action(t,id)
    return C.actions[id=='dash' and t.knight_boots and 'knight_hop' or id]
end
function Trial.cash_preview(s,ids)
    local t=s.card_trial
    if not t or t.mode~='poker' or t.phase~='combat' or t.resource_phase~='prepare' or s.game_over then return nil,'Cash is available only while preparing a hand'end
    local pool,why=Cards.selection(t.cards,ids or t.cards.hand,1,C.hand_size)
    if not pool then return nil,why end
    return Poker.best(t.cards,pool)
end
function Trial.begin_second(s,spawn)
    local t=s.card_trial
    t.fight=2;t.phase='combat';t.resource_phase=t.mode=='fixed' and 'spend' or 'prepare'
    t.stamina=t.mode=='fixed' and C.fixed_stamina or 0;t.retained={};t.exchange_used=false;t.last_cash=nil;t.cycle=t.cycle+1
    if t.cards then Cards.opening_reset(t.cards)end
    Trial.spawn_fight(s,spawn)
end
-- Free bookkeeping is distinct from physical actions and emits no combat events.
function Trial.bookkeeping(s,intent,spawn)
    local k=intent.kind
    if k~='cash_hand' and k~='exchange_cards' and k~='retain_cards' and k~='trial_deck_reward'then return false end
    local t=s.card_trial
    if k=='trial_deck_reward'then
        if t.mode~='poker' or t.phase~='deck_reward' or t.deck_edit_applied then return true,false,'Deck reward unavailable'end
        local choice=intent.choice
        if choice~=0 and choice~=1 and choice~=2 then return true,false,'Choose an offer or Skip'end
        if choice>0 then Cards.add(t.cards,t.deck_offers[choice],C.deck_add_count)end
        t.deck_edit_applied=true;t.deck_edit_choice=choice
        Trial.begin_second(s,spawn);return true,true
    end
    if t.mode~='poker' or t.phase~='combat' or t.resource_phase=='recovering' or t.resource_phase=='ended' then return true,false,'Hand controls unavailable'end
    if k=='retain_cards'then
        local ids,seen=Cards.selection(t.cards,intent.ids,0,C.retain_limit)
        if not ids then return true,false,seen end
        t.retained=seen;return true,true
    end
    if t.resource_phase~='prepare'then return true,false,'This hand was already cashed. Recover for another hand'end
    if k=='exchange_cards'then
        if t.exchange_used then return true,false,'Exchange already used for this hand'end
        local ids,why=Cards.selection(t.cards,intent.ids,1,C.exchange_limit)
        if not ids then return true,false,why end
        Cards.exchange(t.cards,ids);for _,id in ipairs(ids)do t.retained[id]=nil end
        t.exchange_used=true;return true,true
    end
    local result,why=Trial.cash_preview(s,intent.ids)
    if not result then return true,false,why or 'No combination selected'end
    Cards.discard(t.cards,result.cards)
    for _,id in ipairs(result.cards)do t.retained[id]=nil end
    t.stamina=result.stamina;t.resource_phase='spend';t.last_cash=result
    return true,true
end
function Trial.preview(s,intent,preview,effective_cost)
    local t=s.card_trial
    if s.dice then return invalid('Use the current dice results')end
    if t and t.mode=='boggle'then return invalid('Use the Boggle action board')end
    if not t or t.phase~='combat' or s.game_over then return invalid('Encounter ended; choose rewards or replay')end
    if type(intent)~='table' or intent.kind~='trial_action' then return invalid('Use a funded action or Recover')end
    if intent.vow~=nil or intent.steps~=nil or intent.cards~=nil or intent.card_id~=nil then return invalid('Unsupported trial action option')end
    if t.resource_phase~='prepare' and t.resource_phase~='spend' then return invalid('Recover is resolving')end
    local def=Trial.action(t,intent.action)
    if not def then return invalid('Choose Step, Strike, Dash, Sweep, Mend or Recover')end
    local p=player(s);local kind=def.kind;local result,dx,dy
    local speed=p.speed+Boons.speed_bonus(s,p)
    if kind=='trial_heal' then
        if t.healing_charges<=0 then return invalid('Healing exhausted')end
        if p.hp>=p.max_hp then return invalid('Full health')end
        result={valid=true,cells={},heal=math.min(def.heal,p.max_hp-p.hp),requested_heal=def.heal,
            cost=effective_cost(100,speed)}
    elseif kind=='move' or kind=='basic_attack' or kind=='wait'then
        dx,dy=intent.dx,intent.dy;result=preview(s,kind,dx,dy)
    else
        if type(intent.x)~='number' or intent.x%1~=0 or type(intent.y)~='number' or intent.y%1~=0 then return invalid('Choose a highlighted target')end
        dx,dy=intent.x-p.position.x,intent.y-p.position.y
        if kind=='knight_hop'then
            if not (math.abs(dx)==2 and math.abs(dy)==1 or math.abs(dx)==1 and math.abs(dy)==2)then return invalid('Jump two tiles on one axis and one on the other')end
            local map=Content.map_for(s);local x,y=intent.x,intent.y
            local valid=x>1 and x<map.width and y>1 and y<map.height and Content.walkable(map,x,y) and not occupied(s,x,y)
            result={valid=valid,reason=not valid and 'Landing must be a free walkable tile' or nil,
                cost=effective_cost(Classes.for_player(p).costs.dash,speed),cells={{x=x,y=y,status=valid and 'open' or 'occupied'}},landing={x=x,y=y},jump=true}
        else
            if kind=='sweeping_slash' and math.max(math.abs(dx),math.abs(dy))~=1 then return invalid('Aim at an adjacent direction')end
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
    result.intent={kind=kind,dx=dx,dy=dy};result.recover=intent.action=='recover'
    result.stamina_cost=result.bump and C.stamina_costs.strike or C.stamina_costs[intent.action]
    result.stamina_after=result.recover and 0 or t.stamina-result.stamina_cost
    result.target_valid=true
    if not result.recover and t.resource_phase~='spend'then result.valid=false;result.reason='Cash a hand for stamina, or Recover'
    elseif t.stamina<result.stamina_cost then result.valid=false;result.reason='Need '..result.stamina_cost..' stamina (have '..t.stamina..'). G: Recover'end
    return result
end
function Trial.targets(s,intent,validate)
    local out={};local map=Content.map_for(s)
    if type(intent)~='table' or intent.kind~='trial_action' or intent.action~='dash' and intent.action~='sweep'then return out end
    for y=2,map.height-1 do for x=2,map.width-1 do
        local probe=copy(intent);probe.x,probe.y=x,y;local result=validate(s,probe)
        if result.target_valid then out[#out+1]={x=x,y=y,preview=result}end
    end end
    return out
end
function Trial.pay(s,plan)
    local t=s.card_trial;t.commits=t.commits+1
    if plan.recover then
        t.stamina=0;t.resource_phase='recovering'
        if t.cards then Cards.recover_begin(t.cards,t.retained)end
    else t.stamina=t.stamina-plan.stamina_cost end
end
function Trial.finish_recover(s)
    local t=s.card_trial
    if t.phase~='combat' or s.game_over then t.resource_phase='ended';return end
    t.retained={};t.exchange_used=false;t.last_cash=nil;t.cycle=t.cycle+1
    if t.mode=='fixed'then t.stamina=C.fixed_stamina;t.resource_phase='spend'
    else Cards.refill(t.cards);t.resource_phase='prepare'end
end
function Trial.after_action(s,emit)
    local t=s.card_trial
    if player(s).hp<=0 then t.phase='death';t.resource_phase='ended';s.game_over=true;return end
    for _,e in ipairs(s.entities)do
        if e.faction=='enemy' and e.hp and e.hp>0 then return end
    end
    if t.fight==#C.fights then
        t.phase='victory';t.resource_phase='ended';s.game_over=true
        emit('CardTrialWon',{floor_id=s.active_floor_id},s.player_id)
        return
    end
    t.phase='reward';t.resource_phase='ended'
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
