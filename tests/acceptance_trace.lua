-- Usage: luajit tests/acceptance_trace.lua [output.tsv]
package.path='./?.lua;./?/init.lua;'..package.path
local Play=require('tests.legal_playthrough')
local Game=require('src.game')
local run=Play.stage_one(3)
local output=arg[1] and assert(io.open(arg[1],'w')) or io.stdout
output:write('action\tstage_before\tfrom_x\tfrom_y\tintent\tclock_after\thp_after\tcurrency_after\tevents\n')
for index,record in ipairs(run.trace) do
    local intent=record.intent
    local description=intent.kind
    if intent.dx then description=description..'('..intent.dx..','..intent.dy..')' end
    if intent.chest_id then description=description..'#'..intent.chest_id..':'..intent.offer_index end
    if intent.exit_id then description=description..'#'..intent.exit_id..':'..intent.route_index end
    output:write(table.concat({index,record.from.stage,record.from.x,
        record.from.y,description,record.clock,record.hp,record.currency,
        table.concat(record.event_kinds,',')},'\t'),'\n')
end
if output~=io.stdout then output:close() end
for _,point in ipairs(run.checkpoints) do
    print(('%s: action %d, stage %d, time %d, HP %d, currency %d'):
        format(point.name,point.actions,point.stage,point.clock,
            point.hp,point.currency))
end
print(('Final: %d actions, %d events, stage %d, HP %d'):
    format(#run.trace,#run.events,run.state.stage.index,Game.player(run.state).hp))
