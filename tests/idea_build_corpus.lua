-- Read-only strategy/offer corpus. Every world change is Game.submit; no
-- showcase loadout, teleport, invulnerability or actor/stat edits.
local Driver=require('tests.expansion_legal_playthrough')
local Play=require('tests.legal_playthrough');local G=require('src.game')
local P=require('src.persistence');local A=require('src.achievements')
local sum=require('src.studio_atlas').checksum
local root=assert(arg[1]);assert(root:match('^/var/tmp/roag%-build%-corpus%-[%w%-]+$'))
for _,seed in ipairs({9,21,50})do for _,policy in ipairs({'guided','first'})do
    local run,why,failed=Driver.attempt(seed,{offer_policy=policy=='first' and 'first' or nil})
    local r=run or assert(failed);local state=r.state
    local replay={state=G.new_stage(seed,{multifloor=true,
        class_id='core:actor/adventurer',available_boons=A.pool({})}),trace={},events={}}
    local travel,attacks,waits,chests,dead=0,0,0,0,0
    for index,record in ipairs(r.trace)do
        local ok,reason,events=Play.action(replay,record.intent)
        assert(ok==record.ok and reason==record.reason and replay.state.clock==record.clock
            and #events==#record.event_kinds,'corpus replay action '..index)
        for n,e in ipairs(events)do assert(e.kind==record.event_kinds[n],'corpus event order')end
        local kind=record.intent.kind
        if kind=='move'then travel=travel+1
        elseif kind=='basic_attack' or kind=='sweeping_slash'then attacks=attacks+1
        elseif kind=='wait'then waits=waits+1 elseif kind=='claim_chest'then chests=chests+1 end
    end
    assert(P.encode(G.snapshot(replay.state))==P.encode(G.snapshot(state)),'corpus end snapshot differs')
    for _,e in ipairs(r.events)do if e.kind=='Died' and e.target_id==state.player_id then dead=dead+1 end end
    local boons={};for _,id in ipairs(require('src.boon_content').order)do
        if G.player(state).boons[id]then boons[#boons+1]={id=id,stacks=G.player(state).boons[id]}end
    end
    local record={seed=seed,policy=policy,victory=state.victory~=nil,reason=why,
        actions=#r.trace,hp=G.player(state).hp,currency=state.currency,paid_purchases=r.purchases,
        chests=chests,deaths=dead,travel_intents=travel,attack_intents=attacks,waits=waits,
        world_time=state.victory and state.victory.world_time,stages=r.stages,boons=boons,
        snapshot_checksum=sum(P.encode(G.snapshot(state))),trace=r.trace}
    local f=assert(io.open(root..'/'..seed..'-'..policy..'.data','wb'))
    assert(f:write(P.encode(record)));f:close()
    print(('seed=%d policy=%s victory=%s actions=%d hp=%d currency=%d paid=%d chests=%d deaths=%d travel=%d attacks=%d waits=%d world_time=%s boons=%d snapshot=%s reason=%s'):
        format(seed,policy,tostring(record.victory),#r.trace,record.hp,state.currency,
            #r.purchases,chests,dead,travel,attacks,waits,tostring(record.world_time),#boons,
            record.snapshot_checksum,tostring(why)))
    for _,b in ipairs(boons)do print('  '..b.id..' '..P.encode(b.stacks))end
end end
