-- Opt-in deterministic action CPU/playback measurements, not an acceptance test
-- or a benchmark of native graphics hardware. Prepared crowd/builds are assisted.
local G=require('src.game');local PB=require('src.playback');local P=require('src.persistence')
local B=require('src.boons');local V=require('src.visibility');local C=require('src.content')
local function pct(values,p)
    table.sort(values);return values[math.max(1,math.ceil(#values*p))]or 0
end
local function measure(name,s,plan)
    collectgarbage('collect')
    local heap=collectgarbage('count');local samples,maxevents,peakqueue,latency={},0,0,0
    for _,intent in ipairs(plan)do
        local start=os.clock();local ok,why,events=G.submit(s,intent);assert(ok,why)
        samples[#samples+1]=(os.clock()-start)*1000;maxevents=math.max(maxevents,#events)
        local snapshot=P.encode(G.snapshot(s));local pb=PB.new();PB.add(pb,events,s.player_id)
        peakqueue=math.max(peakqueue,#pb.items)
        local seconds=0;for _,item in ipairs(pb.items)do seconds=seconds+item.duration end
        latency=math.max(latency,seconds);PB.update(pb,seconds+1)
        assert(P.encode(G.snapshot(s))==snapshot,'playback changed simulation')
        heap=math.max(heap,collectgarbage('count'))
    end
    print(('%s actions=%d p50_ms=%.3f p95_ms=%.3f max_ms=%.3f max_events=%d playback_beats=%d max_playback_s=%.3f heap_peak_kib=%.0f'):
        format(name,#samples,pct(samples,.5),pct(samples,.95),pct(samples,1),maxevents,peakqueue,latency,heap))
end
local waits={};for _=1,12 do waits[#waits+1]={kind='wait'}end
measure('natural_generated_seed21',G.new_stage(21,{multifloor=true}),waits)
for _,n in ipairs({30,100})do
    local s=G.new(37,'core:map/scrolling_room');local p=G.player(s)
    p.position.x,p.position.y=15,16;local placed=0
    for y=2,30 do for x=2,35 do
        if placed<n and C.walkable(C.map_for(s),x,y)
            and math.abs(x-15)+math.abs(y-16)>=4 then
            G.spawn_enemy(s,'core:enemy/mossbound_guard',x,y,0);placed=placed+1
        end
    end end
    assert(placed==n);V.recompute(s)
    measure('assisted_crowd_'..n,s,waits)
end
local s=G.new(37,'core:map/scrolling_room',{class_id='core:actor/berserker'})
local p=G.player(s);p.position.x,p.position.y=15,16;p.hp=10
for _,slug in ipairs({'backswing','threefold_remedy','fourfold_ledger','long_wick',
    'passing_ember','rooks_wake','fan_of_blades','emberbrand','temporal_echo',
    'cluster_rupture','grave_dividend','storm_conductor'})do
    B.set_stacks(p,'core:boon/'..slug,'rare',2)
end
for y=-2,2 do for x=-2,2 do if x~=0 or y~=0 then
    local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',15+x,16+y,100000)
    e.hp=1000;e.max_hp=1000
end end end
V.recompute(s)
measure('assisted_new_boon_chain',s,{{kind='sweeping_slash',dx=1,dy=0},
    {kind='wait'},{kind='wait'},{kind='sweeping_slash',dx=1,dy=0},{kind='wait'}})
