-- Opt-in CPU profile of a real legal-action completion. No gameplay mutation.
local Game=require('src.game')
local Play=require('tests.legal_playthrough')
local seed=tonumber(arg[1]) or 3
local samples,events,peak_heap={},0,0
local old_submit=Game.submit
Game.submit=function(state,intent)
    local finale=state.finale and state.finale.phase=='active'
    local before=os.clock()
    local ok,reason,output,preview=old_submit(state,intent)
    if finale and ok then
        samples[#samples+1]=(os.clock()-before)*1000
        events=events+#output
        peak_heap=math.max(peak_heap,collectgarbage('count'))
    end
    return ok,reason,output,preview
end
local run=Play.complete_run(seed)
Game.submit=old_submit
table.sort(samples)
local function percentile(p)
    return samples[math.max(1,math.ceil(#samples*p))] or 0
end
print(('FINALE PROFILE seed=%d runtime=%s actions=%d boss_actions=%d ' ..
    'boss_events=%d cpu_ms_p50=%.3f cpu_ms_p95=%.3f cpu_ms_max=%.3f ' ..
    'peak_lua_heap_kb=%.1f victory_hp=%d'):format(seed,jit.version,
    #run.trace,#samples,events,percentile(0.5),percentile(0.95),
    samples[#samples] or 0,peak_heap,run.state.victory.hp))
