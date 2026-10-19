-- Prepared mechanics; continuation is exclusively through real Game.submit.
local G=require('src.game');local T=require('src.idea_terrain');local C=require('src.content')
local P=require('src.persistence');local V=require('src.visibility');local Q=require('src.scheduler')
local mode,dir,kind=arg[1],arg[2],arg[3]
assert((mode=='write' or mode=='continue') and dir:match('^/var/tmp/roag%-terrain%-disk%-[%w%-]+$'))
local names={['active-a.roag']=true,['active-b.roag']=true,['plan.data']=true,['expected.data']=true,['events.data']=true}
local storage={}
function storage:read(name)assert(names[name]);local f=io.open(dir..'/'..name,'rb');if f then local b=f:read('*a');f:close();return b end end
function storage:write(name,bytes)assert(names[name]);local f=assert(io.open(dir..'/'..name,'wb'));assert(f:write(bytes));assert(f:close());return true end
local store=P.new(storage)
local function advance(s,plan)
 local result={}
 for _,intent in ipairs(plan)do local okay,why,events=G.submit(s,intent);assert(okay,why);result[#result+1]=events end
 return P.encode(result)
end
if mode=='write'then
 local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6;V.recompute(s)
 local plan
 if kind=='heat'then
  local n=T.place(s,C.map_for(s),'heat_vent',8,6);advance(s,{{kind='wait'}});assert(n.phase=='warning')
  plan={{kind='move',dx=1,dy=0},{kind='wait'}}
 elseif kind=='push'then
  T.place(s,C.map_for(s),'push_floor',8,6,1,0)
  plan={{kind='move',dx=1,dy=0},{kind='move',dx=1,dy=0},{kind='wait'}}
 elseif kind=='fragile'then
  s=G.new_generated(37);s.meta.eligible=false;p=G.player(s);local map=C.map_for(s)
  for index=#s.entities,1,-1 do local e=s.entities[index];if e.controller=='ai'then Q.remove_for_actor(s,e.id);table.remove(s.entities,index)end end
  s.idea_terrain={version=1,nodes={}};local protected={}
  for _,c in ipairs(map.protected_cells)do protected[c.x..','..c.y]=true end
  local node
  for _,c in ipairs(map.editable_cells)do
   if T.optional_cell(map,c.x,c.y)
    and C.walkable(map,c.x-1,c.y) and C.walkable(map,c.x+1,c.y) and T.alternate_route(s,map,c.x,c.y)then
    local okay=true
    for _,e in ipairs(s.entities)do if e.id~=p.id and e.blocks_movement and e.position.y==c.y and math.abs(e.position.x-c.x)<=1 then okay=false end end
    if okay then
     p.position.x,p.position.y=c.x-1,c.y;V.recompute(s);node=T.place(s,map,'fragile_crossing',c.x,c.y);break
    end
   end
  end
  assert(node);advance(s,{{kind='move',dx=1,dy=0}});assert(node.phase=='armed')
  plan={{kind='move',dx=1,dy=0},{kind='wait'}}
 else error('unknown terrain fixture')end
 assert(store:save(s));storage:write('plan.data',P.encode(plan))
 local next=G.restore(G.snapshot(s));storage:write('events.data',advance(next,plan));storage:write('expected.data',P.encode(G.snapshot(next)))
 print('PASS '..kind..': checked generation 1 contains exact prepared terrain state')
else
 local s=assert(store:load());local plan=P.decode(assert(storage:read('plan.data')))
 assert(advance(s,plan)==storage:read('events.data'),'terrain events changed after process restart')
 assert(P.encode(G.snapshot(s))==storage:read('expected.data'),'terrain state/RNG/scheduler changed after process restart')
 assert(store:save(s));assert(store:inspect().record.seq==2)
 print('PASS '..kind..': fresh-process exact continuation and checked generation 2')
end
