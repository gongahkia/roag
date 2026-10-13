-- Reproducible default paired-floor legal-action Victory. No fixture grants,
-- direct state edits, teleports, invulnerability or forced boss weakening.
local Driver=require('tests.expansion_legal_playthrough')
local Play=require('tests.legal_playthrough')
local Game=require('src.game')
local Achievements=require('src.achievements')
local Classes=require('src.classes')
local Persistence=require('src.persistence')
local seed=tonumber(arg[1]) or 9
local path=arg[2] or '/var/tmp/roeg-expansion-legal-victory.trace'
local class_id=arg[3] or Classes.order[1]
if not class_id:find(':',1,true) then class_id='core:actor/'..class_id end
assert(Classes.get(class_id),'unknown class '..class_id)
local run,reason,failed=Driver.attempt(seed,{class_id=class_id})
assert(run,('seed %d failed after %d actions: %s'):format(seed,
    failed and #failed.trace or 0,tostring(reason)))
local replay={state=Game.new_stage(seed,{multifloor=true,
    class_id=class_id,available_boons=Achievements.pool({})}),
    trace={},events={}}
for index,record in ipairs(run.trace) do
    local ok,why,events=Play.action(replay,record.intent)
    assert(ok==record.ok and why==record.reason and
        replay.state.clock==record.clock and #events==#record.event_kinds,
        'replay diverged at action '..index)
    for i,event in ipairs(events) do
        assert(event.kind==record.event_kinds[i],
            'ordered event mismatch at '..index..':'..i)
    end
end
assert(Persistence.encode(Game.snapshot(run.state))==
    Persistence.encode(Game.snapshot(replay.state)),
    'legal Victory and replay authoritative snapshots differ')
local file=assert(io.open(path,'wb'))
local route_indices={}
for _,record in ipairs(run.trace) do
    if record.intent.kind=='choose_route' then
        route_indices[#route_indices+1]=record.intent.route_index
    end
end
file:write(Persistence.encode({seed=seed,class_id=class_id,
    action_count=#run.trace,route_indices=route_indices,
    stages=run.stages,purchases=run.purchases,trace=run.trace,
    victory=run.state.victory}))
assert(file:close())
print(('PAIRED LEGAL VICTORY class=%s seed=%d actions=%d hp=%d world_time=%d purchases=%d trace=%s'):
    format(class_id,seed,#run.trace,run.state.victory.hp,
        run.state.victory.world_time,#run.purchases,path))
