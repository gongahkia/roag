-- Adapter for explicit survey / salvage / cull / arming jobs. It uses the existing
-- body-aware pathfinder and the same worker/job/work-position reservations.
local W=require('src.world')
local N=require('src.nav')
local Content=require('src.content')
local E=require('src.ecology')
local F={kinds={survey=true,salvage=true,cull=true,arm=true,study=true}}
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
 if j.kind=='study' and category~='flora' then return nil,'Study needs a living growth' end
 return p,category
end
function F.offer(w,a,f,closest,offer,context)
 for _,j in ipairs(w.jobs) do if j.state=='open' and not j.assigned and F.kinds[j.kind] and (not j.owner or j.owner==a.id) then
  local p,category=F.destination(w,j)
  if not p then j.state='done';j.reason=category
  else
   local eligible=true
   if j.kind=='study' then
    local source=require('src.knowledge').source(context and context.siteId or 1,category or 'flora',p)
    local ok,reason=require('src.knowledge').canStudy(context,a,source)
    eligible=ok;if not eligible then j.reason=reason end
   end
   if eligible then
    local path,node,dist=closest(w,a,f,function(x,y) return N.reach(w,x,y,p.x,p.y,4) end)
    if path then offer({kind='field',job=j.id,path=path,node=node,label=j.kind..' / fieldwork'},j.priority*100,dist)
    else j.reason='No reachable fieldwork position' end
   end
  end
 end end
end
function F.act(w,a,t,finish,blocked,reRoute,context)
 local j=W.find(w.jobs,t.job)
 local p,category=F.destination(w,j)
 if not p then j.state='done';finish(w,a,'Target gone');return end
 if not N.reach(w,a.x,a.y,p.x,p.y,4) then
  if not reRoute(w,a,function(x,y) return N.reach(w,x,y,p.x,p.y,4) end) then blocked(w,a,'Field target no longer reachable') end
  return
 end
 t.progress=t.progress+1
 if j.kind=='study' then
  local source=require('src.knowledge').source(context and context.siteId or 1,category,p)
  local progressed,result=require('src.knowledge').studyAction(context,a,source)
  if not progressed then
   if result~='Already performed field study this tick' then blocked(w,a,result) end
   return
  end
  j.reason=result.complete and 'Completed personal field study' or ('Personal field study '..result.progress..'/120')
  if not result.complete then a.status='Studying field evidence';return end
  j.state='done';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,'Field study completed');return
 end
 if j.kind=='cull' then
  if t.progress%4==0 then E.hurt(w,p,8,'Culled by '..a.name) end
  if p.alive then return end
 elseif t.progress<30 then return
 elseif j.kind=='arm' then
  require('src.blasts').arm(w,w.structures[W.slot(w,j.gx,j.gy)],a)
 elseif j.kind=='survey' then
  if require('src.knowledge').enabled(context) then
   local learned,why=require('src.knowledge').survey(context,a,require('src.knowledge').source(context.siteId,category,p))
   if not learned then blocked(w,a,why);return end
  else Content.discover(w,p,category) end
 elseif j.kind=='salvage' then
  if not require('src.knowledge').enabled(context) then Content.discover(w,p,category) end
  E.salvage(w,p,category)
  W.event(w,'salvage',a.name..' salvaged an encountered growth or artifact. Its contents must still be hauled.',p.id)
 end
 j.state='done';j.reason='Completed';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,'Fieldwork completed')
end
return F
