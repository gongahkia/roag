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
    if type(intent)~='table' or intent.kind~='card_action' then return invalid('Card Trial requires a card action')end
    if intent.vow~=nil or intent.steps~=nil then return invalid('Unsupported trial action option')end
    local selection,why=Cards.selection(t.cards,intent.cards)
    if not selection then return invalid(why)end
    local equipment=C.equipment[intent.action]
    if not equipment and intent.action~='redraw'then return invalid('Choose Move, Attack, Heal or Wait & Redraw')end
    if intent.action~='redraw' and not selection.recipe then return invalid('Use one card, or exactly two cards of equal rank')end
    local kind=intent.action=='redraw' and 'wait' or equipment[selection.recipe]
    local p=player(s)
    local result
    if intent.action=='heal'then
        if t.healing_charges<=0 then return invalid('No healing charges remain')end
        if p.hp>=p.max_hp then return invalid('Already at full health')end
        result={valid=true,cells={},heal=math.min(kind,p.max_hp-p.hp),requested_heal=kind,
            cost=effective_cost(100,p.speed+Boons.speed_bonus(s,p))}
        kind='trial_heal'
    else
        result=preview(s,kind,intent.dx,intent.dy)
        if result.bump then return invalid('Boots cannot attack; choose Attack')end
        if result.door_id or result.vault_id or result.stair then return invalid('Use a clear arena tile')end
        if not result.valid then return result end
    end
    result.selection=selection;result.recipe=selection.recipe
    result.intent={kind=kind,dx=intent.dx,dy=intent.dy}
    return result
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
        position={floor_id=s.active_floor_id,x=at.x,y=at.y}}
    s.next_entity_id=chest.id+1;s.entities[#s.entities+1]=chest;t.reward_id=chest.id
end
function Trial.reward(s,index)
    local t=s.card_trial
    if not t or t.phase~='reward' or s.game_over then return nil,'Reward unavailable'end
    local chest
    for _,e in ipairs(s.entities)do if e.id==t.reward_id then chest=e;break end end
    if not chest then return nil,'Reward missing'end
    local ok,why=Rewards.can_claim(s,chest,index)
    if not ok then return nil,why end
    return chest
end
return Trial
