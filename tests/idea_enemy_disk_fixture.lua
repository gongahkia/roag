-- Two distinct LuaJIT processes, real rotating run envelopes, no LÖVE/window.
local Game=require('src.game')
local P=require('src.persistence')
local mode,dir=arg[1],arg[2]
local scenario=arg[3] or 'rammer'
assert(scenario=='rammer' or scenario=='wraith' or scenario=='cantor','unknown fixture scenario')
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
    local s=Game.new(37);local p=Game.player(s)
    local enemy
    if scenario=='cantor' then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/grave_cantor',10,6,200)
        local victim=Game.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
        victim.hp=3;victim.max_hp=6
        assert(Game.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(Game.submit(s,{kind='wait'}))
    elseif scenario=='wraith' then
        p.position.x,p.position.y=8,7
        enemy=Game.spawn_enemy(s,'core:enemy/hollow_wraith',5,7,0)
    else
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/heavy_rammer',6,6,0)
    end
    assert(Game.submit(s,{kind='wait'}))
    assert(s.clock==(scenario=='cantor' and 300 or 100)
        and enemy.pending_attack.due==(scenario=='cantor' and 388 or scenario=='wraith' and 111 or 157))
    assert(store:save(s));assert(store:inspect().record.seq==1)
    local expected=Game.restore(Game.snapshot(s))
    local ok,_,events=Game.submit(expected,{kind='wait'});assert(ok)
    storage:write('expected.roag-data',P.encode(Game.snapshot(expected)))
    storage:write('expected-events.roag-data',P.encode(events))
    print('PASS disk write: pending '..scenario..' saved in generation 1')
else
    local s=assert(store:load());assert(s.clock==(scenario=='cantor' and 300 or 100))
    local ok,_,events=Game.submit(s,{kind='wait'});assert(ok)
    assert(P.encode(events)==storage:read('expected-events.roag-data'))
    assert(P.encode(Game.snapshot(s))==storage:read('expected.roag-data'))
    assert(Game.player(s).position.x==(scenario=='cantor' and 7 or 8)
        and s.clock==(scenario=='cantor' and 400 or 200))
    if scenario=='cantor' then
        local found=false
        for _,e in ipairs(s.entities)do if e.revival_of then
            assert(e.reward_exempt and e.hp==3);found=true
        end end
        assert(found and #s.idea_corpses==0 and s.progression.xp_total==5)
    end
    if scenario=='wraith' then
        local found=false
        for _,e in ipairs(events)do if e.kind=='Displaced' and e.reason=='phase' then found=true end end
        assert(found and Game.player(s).hp==24)
    end
    assert(store:save(s));assert(store:inspect().record.seq==2)
    assert(storage:read('active-a.roag') and storage:read('active-b.roag'))
    print('PASS fresh-process Continue: exact events, RNG/state and generation 2')
end
