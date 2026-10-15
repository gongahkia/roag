-- Reproducible ordinary-action completion trace; output is inert codec data.
local Play=require('tests.legal_playthrough')
local Persistence=require('src.persistence')
local seed=tonumber(arg[1]) or 3
local run=Play.complete_run(seed)
local path=arg[2] or ('/tmp/roag-finale-seed-'..seed..'.trace')
local file=assert(io.open(path,'wb'))
file:write(Persistence.encode({seed=seed,trace=run.trace,
    checkpoints=run.checkpoints,victory=run.state.victory}))
file:close()
print(('LEGAL VICTORY seed=%d actions=%d hp=%d time=%d trace=%s'):format(
    seed,#run.trace,run.state.victory.hp,run.state.victory.world_time,path))
