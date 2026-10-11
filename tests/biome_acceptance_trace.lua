-- Reproducible guided legal-action trace through two distinct new biomes.
-- Usage: luajit tests/biome_acceptance_trace.lua /tmp/roeg-biome-trace.tsv
package.path='./?.lua;./?/init.lua;'..package.path
local Play=require('tests.legal_playthrough')
local Game=require('src.game')
local path=arg[1] or '/tmp/roeg-biome-trace.tsv'
local run=Play.biome_route(3)
local file=assert(io.open(path,'w'))
file:write('action\tstage\tfrom_x\tfrom_y\tintent\tdx\tdy\tworld_time\thp\tcurrency\tevents\n')
for index,record in ipairs(run.trace) do
    local intent=record.intent
    file:write(index,'\t',record.from.stage,'\t',record.from.x,'\t',
        record.from.y,'\t',intent.kind,'\t',intent.dx or '',
        '\t',intent.dy or '','\t',record.clock,'\t',record.hp,
        '\t',record.currency,'\t',table.concat(record.event_kinds,','),'\n')
end
file:close()
for _,point in ipairs(run.checkpoints) do
    print(('%s action=%d stage=%d hp=%d currency=%d')
        :format(point.name,point.actions,point.stage,point.hp,point.currency))
end
print(('TRACE %s actions=%d events=%d stage=%d biome=%s hp=%d')
    :format(path,#run.trace,#run.events,run.state.stage.index,
        run.state.stage.biome_id,Game.player(run.state).hp))
