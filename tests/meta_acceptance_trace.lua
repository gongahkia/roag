-- Two ordinary-command runs: secret unlock, then paid chest acquisition and proc.
local Play=require('tests.legal_playthrough')
local Game=require('src.game')
local Achievements=require('src.achievements')
local AchievementContent=require('src.achievement_content')
local Profile=require('src.profile')
local Content=require('src.content')
local first=Play.new(1,{meta=Achievements.new_run(true,'trace/run-1')})
local secret=first.state.secrets[1]
local ok,reason=Play.navigate(first,function(p)
    return p.x==secret.x and p.y==secret.y end,350)
assert(ok,reason)
local _,_,reveal=Play.action(first,{kind='basic_attack',
    dx=secret.wall_x-secret.x,dy=secret.wall_y-secret.y})
assert(first.state.meta.earned[AchievementContent.order[2]])
local store={}
function store:read(name) return self[name] end
function store:write(name,bytes) self[name]=bytes;return true end
local service=Profile.new(store)
assert(service:commit_awards(first.state.meta.pending))
local pool=Achievements.pool(service:inspect().profile.earned)
assert(#pool==21 and pool[21]=='core:boon/prospectors_heart')

local next_seed=tonumber(arg and arg[1]) or 34
local second=Play.new(next_seed,{meta=Achievements.new_run(true,'trace/run-2'),
    available_boons=pool})
local state=second.state
local chest,offer_index
for _,entity in ipairs(state.entities) do
    if entity.controller=='chest' and entity.price>0 then
        for i,offer in ipairs(entity.offers) do
            if offer.boon_id==pool[21] then chest,offer_index=entity,i;break end
        end
    end
    if chest then break end
end
assert(chest and offer_index,'no unlocked offer in chosen seed')
local kills=0
while state.currency<chest.price and kills<8 and not state.game_over do
    local enemy=assert(Play.nearest(state,function(entity)
        return entity.controller=='ai' and entity.id~=state.stage.elite_id
            and not entity.invulnerable end))
    local x,y,id=enemy.position.x,enemy.position.y,enemy.id
    ok,reason=Play.fight(second,id,120);assert(ok,reason)
    kills=kills+1
    ok,reason=Play.navigate(second,function(p) return p.x==x and p.y==y end,150)
    assert(ok,reason)
end
assert(state.currency>=chest.price and not state.game_over,'could not fund paid chest')
ok,reason=Play.navigate(second,function(p)
    return math.abs(p.x-chest.position.x)+math.abs(p.y-chest.position.y)==1 end,260)
assert(ok,reason)
local cost=chest.price
local before=state.currency
local options=assert(Game.chest_options(state,chest.id))
assert(options.offers[offer_index].boon_id==pool[21])
ok,reason=Play.action(second,{kind='claim_chest',chest_id=chest.id,
    offer_index=offer_index})
assert(ok,reason)
assert(chest.claimed and state.currency==before-cost
    and Game.player(state).boons[pool[21]])

local proc=false
for _=1,6 do
    local enemy=Play.nearest(state,function(entity)
        return entity.controller=='ai' and entity.id~=state.stage.elite_id
            and not entity.invulnerable end)
    if not enemy or state.game_over then break end
    local x,y,id=enemy.position.x,enemy.position.y,enemy.id
    local start=#second.events
    ok,reason=Play.fight(second,id,120);assert(ok,reason)
    ok,reason=Play.navigate(second,function(p) return p.x==x and p.y==y end,150)
    assert(ok,reason)
    for index=start+1,#second.events do
        local event=second.events[index]
        if event.kind=='BoonActivated' and event.boon_id==pool[21] then proc=true end
    end
    if proc then break end
end
assert(proc,'unlocked boon did not activate on legal currency pickup')
print(('meta trace: seed 1 secret in %d actions; seed %d paid chest offer %d, '
    ..'%d kills, %d actions, unlocked boon activated'):format(
    #first.trace,next_seed,offer_index,kills,#second.trace))
