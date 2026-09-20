local W=require('src.world')
local U=require('src.util')
local L=require('src.labor')
local J=require('src.jobs')
local Content=require('src.content')
local C={types={labor=true,rally=true,releaserally=true,field=true,arm=true,target_order=true,school_policy=true}}
function C.valid(w,c)
 local ok,why=pcall(function()
  if c.type=='school_policy' then assert(false,'Field school policies require a campaign education context')
  elseif c.type=='labor' then L.validate(w,c.plan)
  elseif c.type=='field' then
   assert(c.kind=='survey' or c.kind=='salvage' or c.kind=='cull' or c.kind=='study','Unknown field order')
   U.integer(c.target,'target',1,w.nextId-1);local p,category=Content.find(w,c.target);assert(p,'Select an existing encounter')
   assert(c.kind~='cull' or category=='fauna','Cull needs a creature')
   assert(c.kind~='salvage' or category~='fauna','Use cull for a creature')
   assert(c.kind~='study' or category=='flora','Study needs a living growth')
   if c.worker and c.worker~=0 then local a=W.find(w.workers,c.worker);assert(a and a.alive,'Select a living worker') end
  elseif c.type=='arm' then
   U.integer(c.slot,'charge slot',1,w.cols*w.rows);local s=w.structures[c.slot]
   assert(s and s.kind=='charge','Select a completed charge');assert(not s.fuseAt,'Already armed; no disarm')
   if c.worker and c.worker~=0 then U.integer(c.worker,'worker ID',1,w.nextId-1);local a=W.find(w.workers,c.worker);assert(a and a.alive,'Select a living worker') end
  elseif c.type=='target_order' then
   U.integer(c.gx,'order x',2,w.cols-1);U.integer(c.gy,'order y',2,w.rows-1)
   U.integer(c.worker,'worker ID',0,w.nextId-1)
   if c.worker>0 then local a=W.find(w.workers,c.worker);assert(a and a.alive,'Select a living worker') end
  else
   U.integer(c.worker,'worker ID',0,w.nextId-1)
   if c.worker>0 then local a=W.find(w.workers,c.worker);assert(a and a.alive,'Select a living worker') end
   if c.type=='rally' then U.integer(c.x,'rally x',3,w.width-3);U.integer(c.y,'rally y',5,w.height-3) end
  end
  if c.priority~=nil then U.integer(c.priority,'priority',1,3) end
 end)
 return ok,not ok and tostring(why) or nil
end
function C.apply(w,c)
 if c.type=='labor' then L.apply(w,c.plan)
 elseif c.type=='rally' or c.type=='releaserally' then
  for _,a in ipairs(w.workers) do if a.alive and (c.worker==0 or c.worker==a.id) then
   J.release(w,a,true);a.thinkAt=w.tick
   a.directive=c.type=='rally' and {x=c.x,y=c.y} or nil
   a.status=a.directive and 'Rally ordered' or 'Released to work'
  end end
  W.event(w,'orders',c.type=='rally' and 'Rally order issued. Workers still eat, rest and react to hazards.' or 'Rally hold released.')
 elseif c.type=='target_order' then
  for _,j in ipairs(w.jobs) do if j.gx==c.gx and j.gy==c.gy and j.state=='open' then
   for _,a in ipairs(w.workers) do if a.task and a.task.job==j.id then J.release(w,a,true);a.thinkAt=w.tick end end
   j.owner=c.worker>0 and c.worker or nil;j.reason='Worker assignment updated'
  end end
 elseif c.type=='arm' then
  local s=w.structures[c.slot];local j,why=J.add(w,'arm',s.gx,s.gy,nil,c.priority)
  if j then j.owner=c.worker and c.worker~=0 and c.worker or nil else W.event(w,'rejected',why) end
 elseif c.type=='field' then
  local p=Content.find(w,c.target);local gx,gy=W.tile(w,p.x,p.y)
  local j,why=J.add(w,c.kind,gx,gy,nil,c.priority)
  if j then j.target=p.id;j.owner=c.worker and c.worker~=0 and c.worker or nil else W.event(w,'rejected',why) end
 end
 return true
end
return C
