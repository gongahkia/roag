-- Opt-in CPU measurements from a genuine paired-floor legal Victory.
-- The driver only changes the world through Game.submit; instrumentation is
-- read-only and does not consume gameplay RNG or wall-clock simulation time.
local Game=require('src.game')
local Driver=require('tests.expansion_legal_playthrough')
local Classes=require('src.classes')
local seed=tonumber(arg[1]) or 9
local class_id=arg[2] or Classes.order[1]
if not class_id:find(':',1,true) then class_id='core:actor/'..class_id end
assert(Classes.get(class_id),'unknown class')
local buckets={}
local event_total,max_events,queue_peak,heap_peak=0,0,0,0
local original=Game.submit
Game.submit=function(state,intent)
    local label=state.finale and 'finale' or 'stage_'..state.stage.index
    local before=os.clock()
    local ok,reason,events,preview=original(state,intent)
    if ok then
        buckets[label]=buckets[label] or {}
        local bucket=buckets[label]
        bucket[#bucket+1]=(os.clock()-before)*1000
        event_total=event_total+#events
        max_events=math.max(max_events,#events)
        queue_peak=math.max(queue_peak,#state.schedule)
        heap_peak=math.max(heap_peak,collectgarbage('count'))
    end
    return ok,reason,events,preview
end
collectgarbage('collect')
local heap_start=collectgarbage('count')
local wall=os.clock()
local run,reason,failed=Driver.attempt(seed,{class_id=class_id})
local elapsed=os.clock()-wall
Game.submit=original
assert(run,('profile run failed after %d actions: %s'):format(
    failed and #failed.trace or 0,tostring(reason)))
local all={}
for _,samples in pairs(buckets) do
    for _,value in ipairs(samples) do all[#all+1]=value end
end
local function report(label,samples)
    table.sort(samples)
    local function pct(p)
        return samples[math.max(1,math.ceil(#samples*p))] or 0
    end
    print(('%s class=%s seed=%d actions=%d p50_ms=%.3f p95_ms=%.3f max_ms=%.3f'):
        format(label,class_id,seed,#samples,pct(.5),pct(.95),pct(1)))
end
for _,label in ipairs({'stage_1','stage_2','stage_3','finale'}) do
    report(label,buckets[label] or {})
end
report('all',all)
print(('legal_victory=%s cpu_s=%.3f events=%d max_events_per_action=%d schedule_peak=%d heap_start_kib=%.0f heap_peak_kib=%.0f world_time=%d hp=%d'):
    format(tostring(run.state.victory~=nil),elapsed,event_total,max_events,
        queue_peak,heap_start,heap_peak,run.state.clock,run.state.victory.hp))
