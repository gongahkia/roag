-- Exploratory guided legal-action attempt. Failure is reported, never masked.
package.path='./?.lua;./?/init.lua;'..package.path
local Play=require('tests.legal_playthrough')
local Game=require('src.game')
local run=Play.stage_one(3)
local function adjacent(a,b)
    return math.abs(a.x-b.x)<=1 and math.abs(a.y-b.y)<=1
        and (a.x~=b.x or a.y~=b.y)
end
local function require_step(ok,reason,label)
    if not ok then error(label..': '..tostring(reason),0) end
end
local result,why=pcall(function()
    for stage_index=2,3 do
        local state=run.state
        local elite=assert(Game.entity(state,state.stage.elite_id))
        local ok,reason=Play.navigate(run,function(p)
            return adjacent(p,elite.position)
        end,550)
        require_step(ok,reason,'stage '..stage_index..' elite approach')
        ok,reason=Play.fight(run,elite.id,200)
        require_step(ok,reason,'stage '..stage_index..' elite fight')
        print(('stage %d elite defeated at action %d, HP %d, time %d'):
            format(stage_index,#run.trace,Game.player(state).hp,state.clock))
        local free=assert(Game.entity(state,state.stage.elite_reward_id))
        ok,reason=Play.navigate(run,function(p)
            return Play.cardinal(p,free.position)
        end,100)
        require_step(ok,reason,'stage '..stage_index..' free chest approach')
        ok,reason=Play.action(run,{kind='claim_chest',chest_id=free.id,offer_index=1})
        require_step(ok,reason,'stage '..stage_index..' free chest claim')
        local exit=assert(Game.entity(state,state.stage.exit_id))
        ok,reason=Play.navigate(run,function(p)
            return Play.cardinal(p,exit.position)
        end,100)
        require_step(ok,reason,'stage '..stage_index..' exit approach')
        ok,reason=Play.action(run,{kind='choose_route',exit_id=exit.id,route_index=1})
        require_step(ok,reason,'stage '..stage_index..' route')
        print(('after stage %d route: phase %s, index %d, HP %d'):
            format(stage_index,state.stage.phase,state.stage.index,Game.player(state).hp))
    end
end)
print(('result=%s reason=%s actions=%d stage=%d phase=%s HP=%d time=%d'):
    format(tostring(result),tostring(why),#run.trace,run.state.stage.index,
        run.state.stage.phase,Game.player(run.state).hp,run.state.clock))
if arg[1] then
    local output=assert(io.open(arg[1],'w'))
    output:write('action\tstage\tintent\ttime\thp\tevents\n')
    for i,record in ipairs(run.trace) do
        output:write(table.concat({i,record.from.stage,record.intent.kind,
            record.clock,record.hp,table.concat(record.event_kinds,',')},'\t'),'\n')
    end
    output:close()
end
