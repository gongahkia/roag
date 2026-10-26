local Content=require("src.achievement_content")
local Boons=require("src.boon_content")
local A={}
local ids=Content.order
function A.pool(earned)
    local result=Boons.normal_order()
    for _,id in ipairs(ids) do
        local reward=Content.get(id).reward
        if reward and earned and earned[id] then result[#result+1]=reward end
    end
    return result
end
function A.validate_pool(pool)
    assert(type(pool)=="table" and #pool>=Boons.starter_count
        and #pool<=#Boons.order,"invalid available boon pool")
    local seen={}
    for i,id in ipairs(pool) do
        assert(type(id)=="string" and Boons.get(id) and not seen[id],
            "invalid available boon ID")
        if i<=Boons.starter_count then
            assert(id==Boons.order[i],"starter boon pool changed")
        end
        seen[id]=true
    end
    return true
end
function A.new_run(eligible,run_id)
    if eligible then
        assert(type(run_id)=="string" and #run_id>0 and #run_id<=100,
            "eligible run needs unique ID")
    end
    return {eligible=eligible==true,run_id=run_id or "fixture",earned={},pending={},
        chains={}}
end
function A.validate(meta)
    assert(type(meta)=="table" and type(meta.eligible)=="boolean"
        and type(meta.run_id)=="string" and #meta.run_id>0 and #meta.run_id<=100
        and type(meta.earned)=="table" and type(meta.pending)=="table"
        and type(meta.chains)=="table","invalid achievement state")
    for id,value in pairs(meta.earned) do
        assert(Content.get(id) and value==true,"invalid earned achievement")
    end
    for id,value in pairs(meta.pending) do
        assert(Content.get(id) and value==true and meta.earned[id],
            "invalid pending achievement")
    end
    local chain_count=0
    for chain_id,boons in pairs(meta.chains) do
        chain_count=chain_count+1
        assert(type(chain_id)=="number" and chain_id%1==0 and chain_id>=1
            and type(boons)=="table","invalid chain observation")
        for id,value in pairs(boons) do
            assert(Boons.get(id) and value==true,"invalid chain boon observation")
        end
    end
    assert(chain_count<=10000,"too many achievement chains")
    return true
end
local function award(state,id,emit)
    local meta=state.meta
    if meta.earned[id] then return end
    meta.earned[id]=true
    meta.pending[id]=true
    emit("AchievementEarned",{achievement_id=id,
        reward_boon_id=Content.get(id).reward,run_id=meta.run_id},state.player_id)
end
function A.observe(state,event,emit)
    local meta=state.meta
    if not meta or not meta.eligible then return end
    if event.kind=="EliteDefeated" then award(state,ids[1],emit)
    elseif event.kind=="SecretDiscovered" then award(state,ids[2],emit)
    elseif event.kind=="BoonActivated" and event.boon_id
        and event.source_id==state.player_id then
        local chain=meta.chains[event.chain_id] or {}
        meta.chains[event.chain_id]=chain
        chain[event.boon_id]=true
        local count=0
        for _ in pairs(chain) do count=count+1 end
        if count>=3 then award(state,ids[3],emit) end
    elseif event.kind=="Victory" and state.victory then
        award(state,ids[4],emit)
    end
    if event.kind=="EntityKilled" and event.source_id==state.player_id
        and event.target_id~=state.player_id then award(state,ids[5],emit)
    elseif event.kind=="FloorChanged" then award(state,ids[6],emit)
    elseif event.kind=="ChestClaimed" then award(state,ids[7],emit)
    elseif event.kind=="CurrencyCollected" and event.source_id==state.player_id
        and (event.new_currency or 0)>=50 then award(state,ids[8],emit)
    elseif event.kind=="LevelUp" and event.target_id==state.player_id then
        award(state,ids[9],emit)
        if (event.new_level or 0)>=5 then award(state,ids[10],emit) end
    elseif event.kind=="BoonGranted" then award(state,ids[11],emit)
    elseif event.kind=="TerrainBroken" and event.source_id==state.player_id then
        award(state,ids[12],emit)
    elseif event.kind=="OreBroken" and event.source_id==state.player_id then
        award(state,ids[13],emit)
    elseif event.kind=="TimedGateOpened" then award(state,ids[14],emit)
    elseif event.kind=="VaultKeyCollected" then award(state,ids[15],emit)
    elseif event.kind=="WardFlaskCollected" then award(state,ids[16],emit)
    elseif event.kind=="BrazierLit" and event.source_id==state.player_id then
        award(state,ids[17],emit)
    elseif event.kind=="SupplyCollected" and event.source_id==state.player_id then
        award(state,ids[18],emit)
    elseif event.kind=="PursuerArrived" then award(state,ids[19],emit)
    elseif event.kind=="StageEntered" and (event.stage_index or 0)>=2 then
        award(state,ids[20],emit)
    elseif event.kind=="FinalSanctumEntered" then award(state,ids[21],emit)
    elseif event.kind=="RegentPhaseChanged" then award(state,ids[22],emit)
    end
end
function A.prune(state)
    local meta=state.meta
    if not meta then return end
    if meta.earned[ids[3]] or not meta.eligible then meta.chains={};return end
    for chain_id in pairs(meta.chains) do
        if not state.chain_history[chain_id] then meta.chains[chain_id]=nil end
    end
end
return A
