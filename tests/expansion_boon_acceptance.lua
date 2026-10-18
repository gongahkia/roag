-- Genuine normal-action M6 chest trace on a generated paired-floor New Run.
local Play=require("tests.legal_playthrough")
local Game=require("src.game")
local Achievements=require("src.achievements")
local M={}
function M.run()
    local prior_pool={}
    for _,id in ipairs(Achievements.pool({})) do
        if id~='core:boon/ring_of_reversal' then
            prior_pool[#prior_pool+1]=id
        end
    end
    local run=Play.new(50,{meta=Achievements.new_run(true,"m6-acceptance/50"),
        available_boons=prior_pool,multifloor=true})
    local state=run.state
    local function enemy(actor)
        return actor.controller=="ai" and actor.position.floor_id==state.active_floor_id
            and actor.id~=state.stage.elite_id and not actor.invulnerable
    end
    for _=1,7 do
        if state.currency>=5 then break end
        local target=assert(Play.nearest(state,enemy),"no ordinary foe")
        local x,y=target.position.x,target.position.y
        local ok,reason=Play.fight(run,target.id,140)
        assert(ok,reason)
        ok,reason=Play.navigate(run,function(p)return p.x==x and p.y==y end,170)
        assert(ok,reason)
    end
    assert(state.currency>=5 and not state.game_over,"failed to earn chest cost")
    local chest=assert(Game.entity(state,7),"seed-50 paid chest missing")
    assert(chest.controller=="chest" and chest.price>0
        and chest.position.floor_id==state.active_floor_id)
    local ok,reason=Play.navigate(run,function(p)
        return math.abs(p.x-chest.position.x)+math.abs(p.y-chest.position.y)==1
    end,250)
    assert(ok,reason)
    local offers=assert(Game.chest_options(state,chest.id))
    assert(offers.offers[2].boon_id=="core:boon/rhythm_of_steel")
    local before=state.currency
    ok,reason=Play.action(run,{kind="claim_chest",chest_id=chest.id,offer_index=2})
    assert(ok,reason)
    assert(chest.claimed and state.currency==before-chest.price
        and Game.player(state).boons["core:boon/rhythm_of_steel"].common==1)
    local target=assert(Play.nearest(state,enemy))
    ok,reason=Play.fight(run,target.id,120)
    assert(ok,reason)
    local movement,hit=0,0
    for _,event in ipairs(run.events) do
        if event.kind=="BoonActivated" and
            event.boon_id=="core:boon/rhythm_of_steel" then
            if event.trigger_event_id then
                for _,prior in ipairs(run.events) do
                    if prior.id==event.trigger_event_id then
                        if prior.kind=="Moved" then movement=movement+1
                        elseif prior.kind=="DamageTaken" then hit=hit+1 end
                        break
                    end
                end
            end
        end
    end
    assert(movement>=1 and hit>=1,"purchased boon did not prime and hit")
    return run,{seed=50,chest_id=chest.id,offer_index=2,
        boon_id=offers.offers[2].boon_id,rarity=offers.offers[2].rarity,
        actions=#run.trace,hp=Game.player(state).hp,clock=state.clock,
        movement_activations=movement,hit_activations=hit}
end
return M
