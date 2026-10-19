-- Prepared mechanics fixtures in separate LuaJIT processes, isolated checked store.
local G=require('src.game');local P=require('src.persistence')
local mode,dir,class=arg[1],arg[2],arg[3]
assert((mode=='write' or mode=='continue') and dir:match('^/var/tmp/roag%-class%-disk%-[%w%-]+$'))
local allowed={['active-a.roag']=true,['active-b.roag']=true,['expected.roag-data']=true,['events.roag-data']=true}
local storage={}
function storage:read(n)assert(allowed[n]);local f=io.open(dir..'/'..n,'rb');if not f then return end;local b=f:read('*a');f:close();return b end
function storage:write(n,b)assert(allowed[n]);local f=assert(io.open(dir..'/'..n,'wb'));assert(f:write(b));assert(f:close());return true end
local store=P.new(storage)
local function advance(s)
 local events={}
 for i=1,4 do local intent=class=='spiritbinder' and i==1 and {kind='sweeping_slash',dx=1,dy=0} or {kind='wait'};local ok,why,e=G.submit(s,intent);assert(ok,why);events[#events+1]=e end
 return P.encode(events)
end
if mode=='write'then
 assert(class=='geomancer' or class=='spiritbinder')
 local s=G.new(37,nil,{class_id='core:actor/'..class});local p=G.player(s);p.position.x,p.position.y=7,6
 if class=='spiritbinder'then G.spawn_enemy(s,'core:enemy/mossbound_guard',10,6,100000)end
 assert(G.submit(s,{kind='sweeping_slash',dx=class=='spiritbinder' and 1 or 0,dy=class=='spiritbinder' and 0 or 1}))
 assert(store:save(s));local b=G.restore(G.snapshot(s))
 storage:write('events.roag-data',advance(b));storage:write('expected.roag-data',P.encode(G.snapshot(b)))
 print('PASS '..class..' prepared special saved in checked generation 1')
else
 local s=assert(store:load());assert(G.player(s).definition_id=='core:actor/'..class)
 assert(advance(s)==storage:read('events.roag-data'))
 assert(P.encode(G.snapshot(s))==storage:read('expected.roag-data'))
 assert(store:save(s));assert(store:inspect().record.seq==2)
 print('PASS '..class..' fresh-process Continue exact events/state/RNG/schedule, generation 2')
end
