-- Prepared mechanics, then real Game.submit continuation in a separate process.
local G=require('src.game');local I=require('src.idea_interactions');local C=require('src.content')
local P=require('src.persistence');local Q=require('src.scheduler');local V=require('src.visibility')
local mode,dir,kind=arg[1],arg[2],arg[3]
assert((mode=='write' or mode=='continue') and dir:match('^/var/tmp/roag%-items%-disk%-[%w%-]+$'))
local allowed={['active-a.roag']=true,['active-b.roag']=true,['expected.data']=true,['events.data']=true,['plan.data']=true}
local storage={}
function storage:read(name)assert(allowed[name]);local f=io.open(dir..'/'..name,'rb');if not f then return end;local b=f:read('*a');f:close();return b end
function storage:write(name,bytes)assert(allowed[name]);local f=assert(io.open(dir..'/'..name,'wb'));assert(f:write(bytes));assert(f:close());return true end
local store=P.new(storage)
local function submit(s,intent)local ok,why,events=G.submit(s,intent);assert(ok,why);return events end
local function use(s,n,id,selection)return submit(s,{kind='interact',node_id=n.id,choice_id=id,selection=selection})end
local function advance(s,plan)
 local events={};for _,intent in ipairs(plan)do events[#events+1]=submit(s,intent)end;return P.encode(events)
end
if mode=='write'then
 local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6;V.recompute(s)
 local map=C.map_for(s);local n,plan
 if kind=='token' or kind=='contract'then
  s=G.new_generated(37);s.meta.eligible=false;p=G.player(s);map=C.map_for(s)
  for i=#s.entities,1,-1 do local e=s.entities[i];if e.controller=='ai'then Q.remove_for_actor(s,e.id);table.remove(s.entities,i)end end
  s.idea_rooms={version=1,nodes={},groups={}}
  local room
  for _,r in ipairs(map.rooms)do
   for y=r.y+1,r.y+r.height-3 do for x=r.x+1,r.x+r.width-4 do
    local good=true
    for yy=y,y+2 do for xx=x,x+3 do
     if not C.walkable(map,xx,yy)then good=false end
     for _,e in ipairs(s.entities)do if e.id~=p.id and e.blocks_movement and e.position.x==xx and e.position.y==yy then good=false end end
    end end
    if good and not room then room=r;p.position.x,p.position.y=x,y end
   end end
  end
  assert(room);V.recompute(s)
  if kind=='token'then
   n=I.place(s,'return_token',map,p.position.x,p.position.y+1);use(s,n,'take')
   submit(s,{kind='use_utility'});submit(s,{kind='move',dx=1,dy=0})
   assert(p.utility.planted);plan={{kind='use_utility'},{kind='wait'}}
  else
   local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',p.position.x+1,p.position.y,100000);enemy.hp=1
   n=I.place(s,'archive_contract',map,p.position.x,p.position.y+1,{room=room,room_id=room.id,targets={enemy.id},status='offered',defeated={}})
   use(s,n,'accept');plan={{kind='basic_attack',dx=1,dy=0},{kind='wait'}}
  end
 elseif kind=='forge'then
  n=I.place(s,'attunement_forge',map,7,5);s.currency=10
  G.set_boon_stacks(s,p.id,'core:boon/crescent_reach','common',2)
  G.set_boon_stacks(s,p.id,'core:boon/crescent_reach','rare',1)
  local selection={id='core:boon/crescent_reach',rarity='common'};local entry
  for _,e in ipairs(G.interaction_options(s,n.id,selection).entries)do if e.enabled then entry=e;break end end
  assert(entry);plan={{kind='interact',node_id=n.id,choice_id=entry.id,selection=selection},{kind='wait'}}
 elseif kind=='bells'then
  n=I.place(s,'bell_clue',map,7,5,{group='disk-bells'})
  s.idea_rooms.groups['disk-bells']={kind='bells',floor_id=map.floor_id,sequence={2,1,3},progress=0,completed=false}
  local a=I.place(s,'bell',map,8,6,{group='disk-bells',bell=1})
  local b=I.place(s,'bell',map,6,6,{group='disk-bells',bell=2})
  local c=I.place(s,'bell',map,7,7,{group='disk-bells',bell=3})
  use(s,b,'ring');plan={{kind='interact',node_id=a.id,choice_id='ring'},{kind='interact',node_id=c.id,choice_id='ring'},{kind='wait'}}
 elseif kind=='fixture'then
  local cells={};for _,d in ipairs({{0,0},{0,-1},{1,0},{0,1},{-1,0}})do cells[#cells+1]={floor_id=map.floor_id,x=8+d[1],y=6+d[2]}end
  n=I.place(s,'falling_fixture',map,7,5,{cells=cells,center_x=8,center_y=6});p.speed=300
  use(s,n,'trigger');assert(n.pending and s.clock==33)
  plan={{kind='wait'},{kind='wait'},{kind='wait'}}
 elseif kind=='cleanse_echo'then
  n=I.place(s,'cleanse_flask',map,7,5);use(s,n,'take')
  local enemy=G.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,s.clock);enemy.hp=40;enemy.max_hp=40
  G.set_boon_stacks(s,enemy.id,'core:boon/emberbrand','common',1)
  G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
  submit(s,{kind='wait'});submit(s,{kind='wait'});assert(p.statuses.burning)
  p.speed=300;submit(s,{kind='basic_attack',dx=1,dy=0});assert(next(s.delayed_effects) and p.statuses.burning)
  plan={{kind='use_utility'},{kind='wait'},{kind='wait'},{kind='wait'},{kind='wait'}}
 elseif kind=='utility'then
  n=I.place(s,'escape_scroll',map,7,5);use(s,n,'take')
  plan={{kind='use_utility',dx=1,dy=0,steps=2},{kind='wait'}}
 else error('unknown disk scenario')end
 assert(store:save(s));storage:write('plan.data',P.encode(plan))
 local b=G.restore(G.snapshot(s));storage:write('events.data',advance(b,plan));storage:write('expected.data',P.encode(G.snapshot(b)))
 print('PASS '..kind..': prepared finite interaction saved in checked generation 1')
else
 local s=assert(store:load());local plan=P.decode(assert(storage:read('plan.data')))
 assert(advance(s,plan)==storage:read('events.data'),'events differ after process restart')
 assert(P.encode(G.snapshot(s))==storage:read('expected.data'),'authoritative state differs after process restart')
 assert(store:save(s));assert(store:inspect().record.seq==2)
 print('PASS '..kind..': fresh-process exact events/state/RNG/schedule; checked generation 2')
end
