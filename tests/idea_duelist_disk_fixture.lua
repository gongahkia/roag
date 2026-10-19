-- Prepared timing fixture; separate LuaJIT processes and real checked store.
-- No LÖVE, personal save directories or claimed human/legal-run assistance.
local G=require('src.game');local P=require('src.persistence')
local mode,dir=arg[1],arg[2]
assert((mode=='write' or mode=='continue') and type(dir)=='string'
    and dir:match('^/var/tmp/roag%-duelist%-disk%-[%w%-]+$'),'isolated fixture directory required')
local allowed={['active-a.roag']=true,['active-b.roag']=true,
    ['expected.roag-data']=true,['expected-events.roag-data']=true}
local storage={}
function storage:read(name)
    assert(allowed[name]);local f=io.open(dir..'/'..name,'rb');if not f then return end
    local b=f:read('*a');f:close();return b
end
function storage:write(name,bytes)
    assert(allowed[name]);local f=assert(io.open(dir..'/'..name,'wb'))
    assert(f:write(bytes));assert(f:close());return true
end
local store=P.new(storage)
if mode=='write'then
    local s=G.new(37,nil,{class_id='core:actor/duelist'})
    local p=G.player(s);p.position.x,p.position.y=7,6
    G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,0)
    for _=1,2 do assert(G.submit(s,{kind='sweeping_slash',dx=1,dy=0}))end
    assert(s.clock==174 and p.hp==20 and p.duelist_riposte and not s.class_special.duelist_guard)
    assert(store:save(s));assert(store:inspect().record.seq==1)
    local expected=G.restore(G.snapshot(s))
    local ok,_,events=G.submit(expected,{kind='basic_attack',dx=1,dy=0});assert(ok)
    storage:write('expected.roag-data',P.encode(G.snapshot(expected)))
    storage:write('expected-events.roag-data',P.encode(events))
    print('PASS saved the real paid-parry result and one riposte charge in checked generation 1')
else
    local s=assert(store:load());local p=G.player(s)
    assert(s.clock==174 and p.definition_id=='core:actor/duelist' and p.duelist_riposte)
    local ok,_,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(ok)
    assert(s.clock==261 and p.hp==20 and not p.duelist_riposte)
    local hit=false
    for _,v in ipairs(events)do if v.kind=='DamageTaken'then
        assert(v.source_id==p.id and v.attempted_damage==6 and v.actual_loss==5);hit=true
    end end
    assert(hit and P.encode(events)==storage:read('expected-events.roag-data'))
    assert(P.encode(G.snapshot(s))==storage:read('expected.roag-data'))
    assert(store:save(s));assert(store:inspect().record.seq==2)
    assert(storage:read('active-a.roag') and storage:read('active-b.roag'))
    print('PASS fresh-process Continue: paid riposte, exact HP/events/RNG/scheduler and checked generation 2')
end
