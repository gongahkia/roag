-- Adapter for explicit survey / salvage / cull / arming jobs. It uses the existing
-- body-aware pathfinder and the same worker/job/work-position reservations.
local W=require('src.world')
local N=require('src.nav')
local Content=require('src.content')
local E=require('src.ecology')
local F={kinds={survey=true,salvage=true,cull=true,arm=true}}
function F.destination(w,j)
 if j.kind=='arm' then
  local s=w.structures[W.slot(w,j.gx,j.gy)]
  if not s or s.kind~='charge' then return nil,'No completed charge' end
  if s.fuseAt then return nil,'Already armed' end
  return {x=j.gx*4-2,y=j.gy*4-1},'charge'
 end
 local p,category=Content.find(w,j.target)
 if not p then return nil,'Target no longer exists' end
 if j.kind=='cull' and category~='fauna' then return nil,'Cull requires a creature' end
 if j.kind=='salvage' and category=='fauna' then return nil,'Use cull for a creature' end
 return p,category
end
function F.offer(w,a,f,closest,offer)
 for _,j in ipairs(w.jobs) do if j.state=='open' and not j.assigned and F.kinds[j.kind] and (not j.owner or j.owner==a.id) then
  local p,why=F.destination(w,j)
  if not p then j.state='done';j.reason=why
  else
   local path,node,dist=closest(w,a,f,function(x,y) return N.reach(w,x,y,p.x,p.y,4) end)
   if path then offer({kind='field',job=j.id,path=path,node=node,label=j.kind..' / fieldwork'},j.priority*100,dist)
   else j.reason='No reachable fieldwork position' end
  end
 end end
end
function F.act(w,a,t,finish,blocked,reRoute)
 local j=W.find(w.jobs,t.job)
 local p,category=F.destination(w,j)
 if not p then j.state='done';finish(w,a,'Target gone');return end
 if not N.reach(w,a.x,a.y,p.x,p.y,4) then
  if not reRoute(w,a,function(x,y) return N.reach(w,x,y,p.x,p.y,4) end) then blocked(w,a,'Field target no longer reachable') end
  return
 end
 t.progress=t.progress+1
 if j.kind=='cull' then
  if t.progress%4==0 then E.hurt(w,p,8,'Culled by '..a.name) end
  if p.alive then return end
 elseif t.progress<30 then return
 elseif j.kind=='arm' then
  require('src.blasts').arm(w,w.structures[W.slot(w,j.gx,j.gy)],a)
 elseif j.kind=='survey' then Content.discover(w,p,category)
 elseif j.kind=='salvage' then
  Content.discover(w,p,category);E.salvage(w,p,category)
  W.event(w,'salvage',a.name..' salvaged an encountered growth or artifact. Its contents must still be hauled.',p.id)
 end
 j.state='done';j.reason='Completed';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,'Fieldwork completed')
end
return F
