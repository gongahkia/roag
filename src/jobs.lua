local W=require('src.world')
local U=require('src.util')
local M=require('src.materials')
local N=require('src.nav')
local S=require('src.structures')
local C=require('config')
local Labor=require('src.labor')
local Field=require('src.fieldwork')
local J={}
local function releaseClaim(w,a)
 if a.task then
  local t=a.task
  local p=t.item and W.find(w.items,t.item)
  if p and p.reserved==a.id then p.reserved=nil end
  local j=t.job and W.find(w.jobs,t.job)
  if j and j.assigned==a.id then j.assigned=nil end
  local s=t.slot and w.structures[t.slot]
  if s and s.user==a.id then s.user=nil end
 end
 if w.workClaims then
  for k,v in pairs(w.workClaims) do if v==a.id then w.workClaims[k]=nil end end
 end
 a.task=nil
end
function J.release(w,a,drop)
 releaseClaim(w,a)
 if drop and a.carry then W.stack(w,a.carry.kind,a.carry.n,a.x,a.y) a.carry=nil end
end
function J.cancel(w,j)
 if j.state=='done' or j.state=='cancelled' then return end
 for _,a in ipairs(w.workers) do if a.task and a.task.job==j.id then J.release(w,a,true) end end
 if (j.delivered or 0)>0 then
  W.stack(w,S.def[j.build].resource,j.delivered,j.gx*4-2,j.gy*4); j.delivered=0
 end
 j.state='cancelled'; j.assigned=nil
 W.event(w,'order','Cancelled '..j.kind..' order.',j.id)
end
function J.add(w,kind,gx,gy,build,priority)
 if #w.jobs>=1024 then return nil,'Job limit reached' end
 for _,j in ipairs(w.jobs) do if j.gx==gx and j.gy==gy and j.state=='open' then return nil,'Order already present' end end
 local j={id=W.id(w),kind=kind,gx=gx,gy=gy,build=build,priority=priority or 2,
  state='open',delivered=0,progress=0,reason='Waiting for a worker'}
 w.jobs[#w.jobs+1]=j return j
end
function J.digCell(w,j,x,y)
 local x1,y1,x2,y2=W.rect(j.gx,j.gy); local best,dist
 for yy=y1,y2 do for xx=x1,x2 do
  local m=W.get(w,xx,yy)
  if M.def[m].work and not W.blocked(w,xx,yy) and N.reach(w,x,y,xx,yy,4) then
   local d=U.distance(x,y-1,xx,yy)
   if not dist or d<dist then best={x=xx,y=yy,m=m};dist=d end
  end
 end end
 return best
end
function J.digRemaining(w,j)
 local x1,y1,x2,y2=W.rect(j.gx,j.gy)
 for yy=y1,y2 do for xx=x1,x2 do if M.def[W.get(w,xx,yy)].work then return true end end end
 return false
end
local function workPose(w,j,x,y)
 if not N.reachRect(w,x,y,j.gx,j.gy) then return false end
 if j.kind=='dig' then return J.digCell(w,j,x,y)~=nil end
 if j.kind=='build' and (j.build=='wall' or j.build=='platform') then
  local x1,y1,x2,y2=W.rect(j.gx,j.gy)
  if x<=x2 and x+1>=x1 and y>=y1 and y-2<=y2 then return false end
 end
 return true
end
local function closest(w,a,f,predicate)
 return N.closest(w,f,function(x,y)
  local k=W.index(w,x,y)
  return (not w.workClaims or not w.workClaims[k] or w.workClaims[k]==a.id) and predicate(x,y)
 end)
end
local function itemChoice(w,a,f,kind,mustReturn)
 local back=mustReturn and N.returnable(w,f)
 local best,path,node,dist
 for _,p in ipairs(w.items) do if p.n>0 and p.kind==kind and (not p.reserved or p.reserved==a.id) then
  local pp,nn,dd=closest(w,a,f,function(x,y) return (not back or back[W.index(w,x,y)]) and N.reach(w,x,y,p.x,p.y,4) end)
  if pp and (not dist or dd<dist or (dd==dist and p.id<best.id)) then best,path,node,dist=p,pp,nn,dd end
 end end
 return best,path,node,dist
end
function J.canEat(w,a)
 local f=N.flood(w,a.x,a.y)
 return itemChoice(w,a,f,'food')~=nil
end
local function assign(w,a,t)
 if t.item then local p=W.find(w.items,t.item) if p then p.reserved=a.id end end
 if t.job then local j=W.find(w.jobs,t.job) j.assigned=a.id; j.reason='Assigned to '..a.name end
 if t.slot then local s=w.structures[t.slot] if s then s.user=a.id end end
 if t.node then w.workClaims=w.workClaims or {}; w.workClaims[t.node]=a.id end
 t.next=1; t.progress=0; a.task=t; a.reason=''; a.status=t.label or t.kind
end
function J.plan(w,a,context)
 J.release(w,a,true)
 local f=N.flood(w,a.x,a.y)
 if a.evacuate then
  local e=a.evacuate
  if w.tick>e.untilTick or math.abs(a.x-e.x)+math.abs(a.y-e.y)>require('src.blasts').radius+3 then a.evacuate=nil
  else
   local p,n=closest(w,a,f,function(x,y)return math.abs(x-e.x)+math.abs(y-e.y)>require('src.blasts').radius+3 end)
   if p then assign(w,a,{kind='escape',path=p,node=n,label='Evacuating armed charge'}) return end
  end
 end
 if not N.occupy(w,a.x,a.y,true) then
  local p,n=closest(w,a,f,function(x,y) return N.stand(w,x,y,true) end)
  if p then assign(w,a,{kind='escape',path=p,node=n,label='Escaping danger'}) return end
  a.status='Trapped';a.reason='No safe escape route'; return
 end
 if a.hunger>=60 then
  local p,path,node=itemChoice(w,a,f,'food')
  if p then assign(w,a,{kind='eat',item=p.id,path=path,node=node,label='Going to food'}) return end
 end
 if a.fatigue>=75 then
  local best
  for slot=1,w.cols*w.rows do local s=w.structures[slot]
   if s and s.kind=='bed' and not s.user and W.supportedStructure(w,s) and S.wet(w,s)==0 then
    local p,n,d=closest(w,a,f,function(x,y) return N.reachRect(w,x,y,s.gx,s.gy) end)
    if p and (not best or d<best.distance) then best={kind='rest',slot=slot,path=p,node=n,distance=d,label='Going to bed'} end
   end
  end
  assign(w,a,best or {kind='rest',path={},node=W.index(w,a.x,a.y),label='Resting on ground'}) return
 end
 if a.directive then
  local d=a.directive
  local assembly=d.kind=='assembly'
  local path,node=closest(w,a,f,function(x,y)
   return assembly and x==d.x and y==d.y or math.abs(x-d.x)+math.abs(y-d.y)<=3
  end)
  if path then assign(w,a,{kind='rally',path=path,node=node,label=assembly and 'Assembling expedition' or 'Rally / hold'})
  else a.status=assembly and 'Assembly blocked' or 'Rally blocked';a.reason=assembly and 'No safe reachable assembly pose' or 'No reachable standing position near the destination' end
  return
 end
 local choices={}
 local function offer(t,score,distance)
  local bias=Labor.score(w,a,t);if bias==nil then return end
  t.score=score+bias-(distance or 0)*0.05;choices[#choices+1]=t
 end
 if context then require('src.logistics').offer(w,a,context,f,closest,itemChoice,offer) end
 for _,j in ipairs(w.jobs) do if j.state=='open' and not j.assigned and not Field.kinds[j.kind] and (not j.owner or j.owner==a.id) then
  if j.logistics then
   -- Cargo jobs are offered by the campaign logistics context above.
  elseif j.kind=='dig' and not J.digRemaining(w,j) then j.state='done';j.reason='Already clear'
  else
   local valid,reason=true,nil
   if j.kind=='build' then valid,reason=S.siteClear(w,j.gx,j.gy,j.build) end
   if j.kind=='remove' then
    local st=w.structures[W.slot(w,j.gx,j.gy)]
    if not st then valid=false;reason='No structure'
    elseif st.kind=='charge' and st.fuseAt then valid=false;reason='Armed charge cannot be dismantled' end
   end
   local path,node,dist
   if valid then path,node,dist=closest(w,a,f,function(x,y) return workPose(w,j,x,y) end) end
   if not valid then j.reason=reason
   elseif not path then j.reason='No reachable work position'
   elseif j.kind=='build' and j.delivered<S.def[j.build].cost then
    local p,pp,nn,dd=itemChoice(w,a,f,S.def[j.build].resource,true)
    if p then offer({kind='work',job=j.id,stage='fetch',item=p.id,path=pp,node=nn,label='Fetching '..p.kind},j.priority*100,dist+dd)
    else j.reason='Needs accessible '..S.def[j.build].resource end
   else offer({kind='work',job=j.id,stage='work',path=path,node=node,label=j.kind=='build' and 'Building '..j.build or j.kind},j.priority*100,dist) end
  end
 end end
 for slot=1,w.cols*w.rows do local s=w.structures[slot]
  if s and not s.user and W.supportedStructure(w,s) then
   local path,node,dist=closest(w,a,f,function(x,y) return N.reachRect(w,x,y,s.gx,s.gy) end)
   if path then
    if (s.kind=='farm' or s.kind=='ward') and S.wet(w,s)<4 then
     if s.kind=='farm' and s.growth>=w.rules.cropTicks then
      offer({kind='harvest',slot=slot,path=path,node=node,label='Harvesting fungi'},260,dist)
     elseif s.tank<6 then
      -- Water is picked up physically, either in a dropped canister or from reachable cells.
      local p,pp,nn,dd=itemChoice(w,a,f,'water',true)
      if p then offer({kind='irrigate',stage='fetch',slot=slot,item=p.id,path=pp,node=nn,label='Fetching irrigation water'},240,dist+dd)
      else
       local waterX,waterY
       local back=N.returnable(w,f)
       local wp,wn,wd=closest(w,a,f,function(x,y)
        if not back[W.index(w,x,y)] then return false end
        for yy=y-3,y+3 do for xx=x-3,x+4 do
         if W.get(w,xx,yy)==M.WATER and N.reach(w,x,y,xx,yy,4) then waterX,waterY=xx,yy return true end
        end end
        return false
       end)
       if wp then offer({kind='irrigate',stage='fetch',slot=slot,water={x=waterX,y=waterY},path=wp,node=wn,label='Fetching irrigation water'},240,dist+wd) end
      end
     end
    elseif s.kind=='pump' and S.pumpReady(w,s) then
     offer({kind='pump',slot=slot,path=path,node=node,label='Operating hand pump'},110,dist)
    end
   end
  end
 end
 -- Hauling is real transport. Storage labels don't teleport resources.
 for _,p in ipairs(w.items) do if p.n>0 and not p.reserved then
  local at=W.structureAt(w,p.x,p.y)
  if not at or at.kind~='store' then
   local back=N.returnable(w,f)
   local pp,pn,pd=closest(w,a,f,function(x,y) return back[W.index(w,x,y)] and N.reach(w,x,y,p.x,p.y,4) end)
   if pp then
    for slot=1,w.cols*w.rows do local s=w.structures[slot]
     if s and s.kind=='store' and W.supportedStructure(w,s) then
      local sp,sn,sd=closest(w,a,f,function(x,y) return N.reachRect(w,x,y,s.gx,s.gy) end)
      if sp then offer({kind='haul',stage='fetch',item=p.id,store=slot,path=pp,node=pn,label='Hauling '..p.kind},80,pd+sd) break end
     end
    end
   end
  end
 end end
 Field.offer(w,a,f,closest,offer)
 table.sort(choices,function(x,y)
  if x.score~=y.score then return x.score>y.score end
  return (x.job or 100000+(x.slot or x.item or 0))<(y.job or 100000+(y.slot or y.item or 0))
 end)
 if choices[1] then assign(w,a,choices[1])
 else
  a.status='Idle'; a.reason=a.hunger>=60 and 'No reachable food' or 'No reachable, supplied work'
 end
end
local function reRoute(w,a,predicate)
 local f=N.flood(w,a.x,a.y)
 local path,node=closest(w,a,f,predicate)
 if not path then return false end
 for k,v in pairs(w.workClaims or {}) do if v==a.id then w.workClaims[k]=nil end end
 w.workClaims=w.workClaims or {}; w.workClaims[node]=a.id
 a.task.path=path;a.task.node=node;a.task.next=1
 return true
end
local function finish(w,a,reason)
 J.release(w,a,true);a.thinkAt=w.tick+1;a.status=reason or 'Finished';a.worked=false
end
local function blocked(w,a,reason)
 if a.task and a.task.job then
  local j=W.find(w.jobs,a.task.job)
  if j then
   if j.reason~=reason then W.event(w,'blocked',a.name..': '..reason,j.id) end
   j.reason=reason
  end
 end
 finish(w,a,'Blocked');a.reason=reason;a.thinkAt=w.tick+20
end
local function routeDestination(w,a,context)
 local t=a.task
 if t.kind=='work' then
  local j=W.find(w.jobs,t.job)
  return j and reRoute(w,a,function(x,y) return workPose(w,j,x,y) end)
 elseif t.kind=='cargo' then
  local j=W.find(w.jobs,t.job)
  return j and context and require('src.logistics').routeToCraft(context.campaign,context.siteId,w,a,j,reRoute)
 else
  local s=w.structures[t.slot or t.store]
  return s and reRoute(w,a,function(x,y) return N.reachRect(w,x,y,s.gx,s.gy) end)
 end
end
function J.act(w,a,context)
 local t=a.task
 if not t then return end
 if t.job then local j=W.find(w.jobs,t.job) if not j or j.state~='open' then finish(w,a) return end end
 if t.slot and not w.structures[t.slot] then finish(w,a) return end
 -- Every edge is revalidated against the current material field, not stale terrain.
 local nextIndex=t.path and t.path[t.next]
 if nextIndex then
  local nx,ny=W.xy(w,nextIndex)
  if N.edge(w,a.x,a.y,nx,ny) then a.x,a.y=nx,ny;t.next=t.next+1; a.worked=true
  else blocked(w,a,'Route changed or flooded') end
  return
 end
 a.worked=true
 if t.kind=='cargo' then
  local j=W.find(w.jobs,t.job);local Logistics=require('src.logistics')
  if not context or not j then blocked(w,a,'Cargo context is unavailable') return end
  if t.stage=='fetch' and j.kind=='load' then
   local p=W.find(w.items,t.item)
   if not p or p.n<=0 or p.reserved~=a.id or not N.reach(w,a.x,a.y,p.x,p.y,4) then blocked(w,a,'Supply moved or became inaccessible') return end
   local n,why=Logistics.loadFetch(context.campaign,context.siteId,j,p,a)
   if not n then blocked(w,a,why) return end
   a.carry={kind=p.kind,n=n};p.n=p.n-n;p.reserved=nil;t.item=nil;t.stage='deliver';a.status='Delivering '..a.carry.kind..' to craft'
   if not routeDestination(w,a,context) then blocked(w,a,'Craft is no longer reachable') end
  elseif t.stage=='fetch' and j.kind=='unload' then
   local n,why=Logistics.unloadFetch(context.campaign,context.siteId,j,a)
   if not n then blocked(w,a,why) return end
   a.carry={kind=j.logistics.resource,n=n};t.stage='deliver';t.path={};t.next=1;a.status='Carrying cargo to ground'
  elseif t.stage=='deliver' and j.kind=='load' then
   local delivered,why=Logistics.loadDeliver(context.campaign,context.siteId,j,a)
   if not delivered then blocked(w,a,why) else finish(w,a,'Loaded craft cargo') end
  elseif t.stage=='deliver' and j.kind=='unload' then
   local delivered,why=Logistics.unloadDeliver(context.campaign,context.siteId,j,a)
   if not delivered then blocked(w,a,why) else finish(w,a,'Unloaded craft cargo') end
  else blocked(w,a,'Cargo task state changed') end
 elseif t.kind=='field' then Field.act(w,a,t,finish,blocked,reRoute)
 elseif t.kind=='rally' then a.worked=false;a.status='Rally / holding';a.reason='J releases this worker to normal duties'
 elseif t.kind=='escape' then finish(w,a,'Reached safety')
 elseif t.kind=='eat' then
  local p=W.find(w.items,t.item)
  if not p or p.n<1 or not N.reach(w,a.x,a.y,p.x,p.y,4) then blocked(w,a,'Food unavailable') return end
  p.n=p.n-1;a.hunger=math.max(0,a.hunger-48);w.ledger.foodEaten=w.ledger.foodEaten+1
  finish(w,a,'Ate a ration')
 elseif t.kind=='rest' then
  a.worked=false;a.status=t.slot and 'Sleeping in bed' or 'Sleeping on ground'
  if a.fatigue<=25 then finish(w,a,'Rested') end
 elseif t.stage=='fetch' then
  if t.item then
   local p=W.find(w.items,t.item)
   if not p or p.n<=0 or p.reserved~=a.id or not N.reach(w,a.x,a.y,p.x,p.y,4) then blocked(w,a,'Supply moved or became inaccessible') return end
   local capacity=t.kind=='irrigate' and 6 or 12
   if t.kind=='work' then local j=W.find(w.jobs,t.job);capacity=math.min(capacity,S.def[j.build].cost-j.delivered) end
   local count=math.min(p.n,capacity)
   a.carry={kind=p.kind,n=count}; p.n=p.n-count;p.reserved=nil;t.item=nil
  else
   local n=0
   for yy=a.y-3,a.y+3 do for xx=a.x-3,a.x+4 do
    if n<6 and W.get(w,xx,yy)==M.WATER and N.reach(w,a.x,a.y,xx,yy,4) then W.put(w,xx,yy,M.AIR);n=n+1 end
   end end
   if n==0 then blocked(w,a,'Water source moved') return end
   a.carry={kind='water',n=n}
  end
  t.stage='deliver';a.status='Delivering '..a.carry.kind
  if not routeDestination(w,a,context) then blocked(w,a,'Destination no longer reachable') end
 elseif t.stage=='deliver' then
  if not a.carry then blocked(w,a,'Missing carried supply') return end
  if t.kind=='work' then
   local j=W.find(w.jobs,t.job)
   if not workPose(w,j,a.x,a.y) then blocked(w,a,'Work position invalid') return end
   j.delivered=j.delivered+a.carry.n;a.carry=nil
   if j.delivered<S.def[j.build].cost then finish(w,a,'Delivered partial materials') return end
   t.stage='work'
  elseif t.kind=='irrigate' then
   local s=w.structures[t.slot]
   if not N.reachRect(w,a.x,a.y,s.gx,s.gy) then blocked(w,a,'Farm unreachable') return end
   local n=math.min(a.carry.n,w.rules.irrigationCapacity-s.tank)
   s.tank=s.tank+n;a.carry.n=a.carry.n-n
   if a.carry.n==0 then a.carry=nil end
   finish(w,a,'Irrigated crop')
  elseif t.kind=='haul' then
   local s=w.structures[t.store]
   if not s or not N.reachRect(w,a.x,a.y,s.gx,s.gy) then blocked(w,a,'Stockpile unavailable') return end
   W.stack(w,a.carry.kind,a.carry.n,s.gx*4-2,s.gy*4);a.carry=nil
   finish(w,a,'Delivered to stockpile')
  end
 elseif t.kind=='work' then
  local j=W.find(w.jobs,t.job)
  if j.kind~='dig' and not workPose(w,j,a.x,a.y) then blocked(w,a,'Work position invalid') return end
  if j.kind=='dig' then
   local c=J.digCell(w,j,a.x,a.y)
   if not c then
    if not J.digRemaining(w,j) then j.state='done';j.reason='Completed';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a)
    elseif not routeDestination(w,a,context) then blocked(w,a,'No reachable excavation face') end
    return
   end
   t.progress=t.progress+a.mine
   if t.progress>=M.def[c.m].work then
    t.progress=0
    if c.m==M.ICE then W.put(w,c.x,c.y,M.WATER)
    else
     W.put(w,c.x,c.y,M.AIR);W.stack(w,M.def[c.m].resource,1,a.x,a.y)
     w.ledger.mined=w.ledger.mined+1
     require('src.signals').emit(w,'mining',c.x,c.y,4)
    end
   end
  elseif j.kind=='build' then
   local clear,why=S.siteClear(w,j.gx,j.gy,j.build)
   if not clear then blocked(w,a,why) return end
   if not workPose(w,j,a.x,a.y) then blocked(w,a,'Work position invalid') return end
   j.progress=j.progress+a.build
   if j.progress>=S.def[j.build].work then
    S.install(w,j.gx,j.gy,j.build);w.ledger.built=w.ledger.built+j.delivered;j.delivered=0
    j.state='done';j.reason='Completed';w.stats.jobsDone=w.stats.jobsDone+1
    W.event(w,'build',a.name..' completed '..S.def[j.build].label..'.',j.id);finish(w,a)
   end
  else
   local slot=W.slot(w,j.gx,j.gy);local s=w.structures[slot]
   if not s then j.state='done';finish(w,a) return end
   if s.kind=='charge' and s.fuseAt then blocked(w,a,'Armed charge cannot be dismantled') return end
   j.progress=j.progress+1
   if j.progress>=20 then
    local def=S.def[s.kind];local recovered=math.floor(def.cost/2)
    W.stack(w,def.resource,recovered,a.x,a.y)
    w.ledger.demolitionWaste=w.ledger.demolitionWaste+def.cost-recovered
    if s.tank and s.tank>0 then W.stack(w,'water',s.tank,a.x,a.y) end
    w.structures[slot]=nil;w.navRevision=w.navRevision+1;j.state='done';w.stats.jobsDone=w.stats.jobsDone+1
    W.event(w,'remove',a.name..' dismantled '..def.label..'; half the construction material was lost.',j.id)
    finish(w,a)
   end
  end
 elseif t.kind=='harvest' then
  local s=w.structures[t.slot]
  if not N.reachRect(w,a.x,a.y,s.gx,s.gy) or not W.supportedStructure(w,s) or s.growth<w.rules.cropTicks or S.wet(w,s)>=4 then blocked(w,a,'Crop no longer harvestable') return end
  t.progress=t.progress+1
  if t.progress>=20 then
   W.stack(w,'food',w.rules.cropYield,s.gx*4-2,s.gy*4)
   s.growth=0;w.ledger.foodGrown=w.ledger.foodGrown+w.rules.cropYield
   W.event(w,'harvest',a.name..' harvested '..w.rules.cropYield..' rations.',s.id);finish(w,a)
  end
 elseif t.kind=='pump' then
  local s=w.structures[t.slot]
  if not N.reachRect(w,a.x,a.y,s.gx,s.gy) or not S.pump(w,s) then finish(w,a,'Pump stopped')
  else s.status='Pumping: '..a.name;t.progress=t.progress+1
   if t.progress>=120 then finish(w,a,'Operator rotation') end
  end
 end
end
return J
