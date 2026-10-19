-- A prepared fixture, then ordinary actions through a real checked save store.
-- Run legacy/write from the pre-addition tree to prove old saves and events,
-- rather than merely asserting a new->new round trip.
local G=require('src.game')
local P=require('src.persistence')
local R=require('src.rewards')
local Catalog=require('src.boon_content')
local mode,dir,scenario=arg[1],arg[2],arg[3]
assert((mode=='write' or mode=='continue') and
    dir:match('^/var/tmp/roag%-boons%-disk%-[%w%-]+$'))
assert(scenario=='supply' or scenario=='legacy')
local names={['active-a.roag']=true,['active-b.roag']=true,
    ['expected.data']=true,['events.data']=true,['plan.data']=true}
local storage={}
function storage:read(name)
    assert(names[name]);local f=io.open(dir..'/'..name,'rb')
    if not f then return end
    local bytes=f:read('*a');f:close();return bytes
end
function storage:write(name,bytes)
    assert(names[name]);local f=assert(io.open(dir..'/'..name,'wb'))
    assert(f:write(bytes));assert(f:close());return true
end
local store=P.new(storage)
local function submit(s,intent)
    local ok,why,events=G.submit(s,intent);assert(ok,why);return events
end
local function advance(s,plan)
    local events={}
    for _,intent in ipairs(plan)do events[#events+1]=submit(s,intent)end
    return P.encode(events)
end
if mode=='write' then
    local pool={}
    for n,id in ipairs(Catalog.normal_order())do
        if scenario~='legacy' or n<=49 then pool[#pool+1]=id end
    end
    local s=G.new(37,nil,{available_boons=pool});local p=G.player(s)
    local function grant(slug)G.set_boon_stacks(s,p.id,'core:boon/'..slug,'rare',1)end
    grant('temporal_echo');grant('emberbrand')
    local foe=G.spawn_enemy(s,'core:enemy/mossbound_guard',4,2,100000)
    foe.hp=100;foe.max_hp=100
    p.hp=20
    if scenario=='legacy' then
        grant('bloodrush');grant('betrayal_ward')
        p.boon_memory={['core:boon/bloodrush']={speed=20,expires_at=600},
            ['core:boon/betrayal_ward']={ward=3,expires_at=600}}
        R.spawn_supply(s,'food',s.active_floor_id,3,2,function()end,p.id)
        submit(s,{kind='move',dx=1,dy=0})
    else
        for _,id in ipairs(require('src.idea_boon_content').order)do
            G.set_boon_stacks(s,p.id,id,'rare',1)
        end
        R.spawn_supply(s,'health',s.active_floor_id,3,2,function()end,p.id)
        submit(s,{kind='move',dx=1,dy=0})
        p.hp=20
        R.spawn_supply(s,'food',s.active_floor_id,2,2,function()end,p.id)
        submit(s,{kind='move',dx=-1,dy=0})
        submit(s,{kind='move',dx=1,dy=0})
        assert(p.boon_memory['core:boon/ration_reserve'].charges==1)
        assert(p.boon_memory['core:boon/road_flask'].speed==20)
    end
    submit(s,{kind='basic_attack',dx=1,dy=0})
    assert(next(s.delayed_effects) and foe.statuses.burning)
    local plan={{kind='wait'},{kind='basic_attack',dx=1,dy=0},
        {kind='move',dx=-1,dy=0},{kind='wait'},{kind='move',dx=1,dy=0}}
    assert(store:save(s));storage:write('plan.data',P.encode(plan))
    local restored=G.restore(G.snapshot(s))
    storage:write('events.data',advance(restored,plan))
    storage:write('expected.data',P.encode(G.snapshot(restored)))
    print('PASS '..scenario..': saved pending Echo/Burning and finite boon state, generation 1')
else
    local s=assert(store:load())
    local plan=P.decode(assert(storage:read('plan.data')))
    assert(advance(s,plan)==storage:read('events.data'),'events or timing changed after restart')
    assert(P.encode(G.snapshot(s))==storage:read('expected.data'),'authoritative continuation changed')
    assert(store:save(s));assert(store:inspect().record.seq==2)
    print('PASS '..scenario..': fresh process exact continuation, generation 2')
end
