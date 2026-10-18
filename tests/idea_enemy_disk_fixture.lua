-- Two distinct LuaJIT processes, real rotating run envelopes, no LÖVE/window.
local Game=require('src.game')
local P=require('src.persistence')
local mode,dir=arg[1],arg[2]
assert((mode=='write' or mode=='continue') and type(dir)=='string'
    and dir:match('^/var/tmp/roag%-idea%-disk%-[%w%-]+$'),'isolated fixture directory required')
local allowed={['active-a.roag']=true,['active-b.roag']=true,
    ['expected.roag-data']=true,['expected-events.roag-data']=true}
local storage={}
function storage:read(name)
    assert(allowed[name]);local f=io.open(dir..'/'..name,'rb');if not f then return end
    local bytes=f:read('*a');f:close();return bytes
end
function storage:write(name,bytes)
    assert(allowed[name]);local f=assert(io.open(dir..'/'..name,'wb'))
    assert(f:write(bytes));assert(f:close());return true
end
local store=P.new(storage)
if mode=='write' then
    local s=Game.new(37);local p=Game.player(s);p.position.x,p.position.y=7,6
    local rammer=Game.spawn_enemy(s,'core:enemy/heavy_rammer',6,6,0)
    assert(Game.submit(s,{kind='wait'}))
    assert(s.clock==100 and rammer.pending_attack.due==157)
    assert(store:save(s));assert(store:inspect().record.seq==1)
    local expected=Game.restore(Game.snapshot(s))
    local ok,_,events=Game.submit(expected,{kind='wait'});assert(ok)
    storage:write('expected.roag-data',P.encode(Game.snapshot(expected)))
    storage:write('expected-events.roag-data',P.encode(events))
    print('PASS disk write: pending locked shove saved in generation 1')
else
    local s=assert(store:load());assert(s.clock==100)
    local ok,_,events=Game.submit(s,{kind='wait'});assert(ok)
    assert(P.encode(events)==storage:read('expected-events.roag-data'))
    assert(P.encode(Game.snapshot(s))==storage:read('expected.roag-data'))
    assert(Game.player(s).position.x==8 and s.clock==200)
    assert(store:save(s));assert(store:inspect().record.seq==2)
    assert(storage:read('active-a.roag') and storage:read('active-b.roag'))
    print('PASS fresh-process Continue: exact events, RNG/state and generation 2')
end
