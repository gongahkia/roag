local W=require('src.world')
local U=require('src.util')
local M=require('src.materials')
local N=require('src.nav')
local S=require('src.structures')
local C=require('config')
local Labor=require('src.labor')
local Field=require('src.fieldwork')
local Body=require('src.body')
local Equipment=require('src.equipment')
local J={}
local function contribution(w,a,context,t,amount)
 if not (context and context.campaign and context.campaign.features.psychology==1) then return amount end
 local role=Labor.roleForTask(w,t) or (t.kind=='harvest' and 'farm') or (t.kind=='pump' and 'pump') or 'build'
 local value,reason=require('src.psychology').work(context.campaign,a,role,amount)
 a.reason=reason
 return value
end
local function deliveredTotal(value)
 if type(value)=='number' then return value end
 local total=0;for _,amount in pairs(value or {}) do total=total+amount end;return total
end
local function returnDelivered(w,j)
 if type(j.delivered)=='number' then
  if j.delivered>0 then W.stack(w,S.def[j.build].resource,j.delivered,j.gx*4-2,j.gy*4) end
 else
  for _,resource in ipairs({'stone','soil','metal','component','food','water'}) do if (j.delivered[resource] or 0)>0 then W.stack(w,resource,j.delivered[resource],j.gx*4-2,j.gy*4) end end
 end
 j.delivered=type(j.delivered)=='number' and 0 or {}
end
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
 if a.task and a.task.kind=='school' then require('src.education').releaseTask(w,a.task) end
 releaseClaim(w,a)
 if drop and a.carry then W.stack(w,a.carry.kind,a.carry.n,a.x,a.y) a.carry=nil end
end
function J.cancel(w,j)
 if j.state=='done' or j.state=='cancelled' then return end
 for _,a in ipairs(w.workers) do if a.task and a.task.job==j.id then J.release(w,a,true) end end
 if (j.kind=='build' or j.kind=='fabricate') and deliveredTotal(j.delivered)>0 then
  if j.kind=='fabricate' then W.stack(w,'metal',j.delivered,j.gx*4-2,j.gy*4);j.delivered=0 else returnDelivered(w,j) end
 end
 j.state='cancelled'; j.assigned=nil
 W.event(w,'order','Cancelled '..j.kind..' order.',j.id)
end
function J.add(w,kind,gx,gy,build,priority)
 if #w.jobs>=1024 then return nil,'Job limit reached' end
 for _,j in ipairs(w.jobs) do if j.gx==gx and j.gy==gy and j.state=='open' then return nil,'Order already present' end end
 local j={id=W.id(w),kind=kind,gx=gx,gy=gy,build=build,priority=priority or 2,
  state='open',delivered=kind=='build' and S.def[build] and S.def[build].materials and {} or 0,progress=0,reason='Waiting for a worker'}
 w.jobs[#w.jobs+1]=j return j
end
local function feature(context) return context and context.campaign and Equipment.safe(context.campaign) end
local function currentSite(context) return context and context.siteId end
local function safeAlternate(w,a,c)
 local f=N.flood(w,a.x,a.y)
 for _,i in ipairs(f.queue) do
  local x,y=W.xy(w,i);local holds=false
  for xx=x,x+Body.width(w)-1 do if xx==c.x and y+1==c.y then holds=true end end
  if not holds and N.reach(w,x,y,c.x,c.y,4) then return N.path(f,i),i end
 end
end
-- This post-action check is intentionally local. It establishes whether the
-- miner can keep a supported or rope-controlled body after this one mutation;
-- it does not inspect undiscovered terrain or predict a whole dig plan.
local function digSafety(w,a,c,context,t)
 if not feature(context) then return true end
 -- A coil fetched for the final supporting cell is attached atomically with
 -- that cell's actual removal. Until then it is carried at the reachable
 -- anchor, so the miner never spends an intervening physics tick unsupported.
 if t.stage=='rope_ready' and t.ropePendingX==c.x and t.ropePendingY==c.y then return true end
 local holds=false;for xx=a.x,a.x+Body.width(w)-1 do if xx==c.x and a.y+1==c.y then holds=true end end
 if not holds or N.rope(w,a.x,a.y) then return true end
 local path,node=safeAlternate(w,a,c)
 if path then t.path,t.node,t.next=path,node,1;a.status='Repositioning for safe excavation';return false,'reposition' end
 local campaign=context.campaign;local f=N.flood(w,a.x,a.y);local coil,fetchPath,fetchNode=Equipment.nearestLoose(campaign,currentSite(context),'rope_coil',w,a,f,N.returnable(w,f))
 -- The rope's body-wide lane is the miner's current lane. The selected last
 -- supporting cell is the only temporary obstruction allowed: it is removed
 -- and the carried line becomes support in the same successful mutation.
 local lane={x=a.x,startY=a.y,length=0}
 for y=a.y,math.min(w.height,a.y+23) do
  local target=y==c.y and (c.x==a.x or c.x==a.x+1)
  if (W.solid(w,a.x,y) or W.solid(w,a.x+1,y)) and not target then break end
  lane.length=lane.length+1
 end
 if lane.length<4 or not N.reach(w,a.x,a.y,a.x,a.y,4) then lane=nil end
 if coil and lane and Equipment.reserve(coil,a) then
  t.stage='rope_fetch';t.equipmentId=coil.id;t.anchorX=a.x;t.anchorY=a.y;t.ropeLaneX=lane.x;t.ropeLength=lane.length;t.ropePendingX=c.x;t.ropePendingY=c.y;t.path=fetchPath;t.node=fetchNode;t.next=1
  a.status='Fetching rope coil for safe descent';return false,'rope'
 end
 return false,'Unsafe descent — rope required'
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
  local ax1,ay1,ax2,ay2=Body.rect(w,x,y)
  if ax1<=x2 and ax2>=x1 and ay2>=y1 and ay1<=y2 then return false end
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
 if feature(context) and a.panic then
  for _,j in ipairs(w.jobs) do if j.state=='open' and j.kind=='arm' and (not j.owner or j.owner==a.id) then j.reason='Panicked workers cannot arm demolition charges' end end
  local Visibility=require('src.visibility')
  local p,n=closest(w,a,f,function(x,y)
   if not N.stand(w,x,y,true) then return false end
   return not w.frontier.visibility or Visibility.currentlyVisible(w,x,y,context)
  end)
  if not p then p,n=closest(w,a,f,function(x,y) return N.stand(w,x,y,true) end) end
  if p then assign(w,a,{kind='panic_escape',path=p,node=n,label='Panicked — seeking safety'})
  else a.status='Panicked / trapped';a.reason='No safe escape route' end
  return
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
   if assembly then return x==d.x and y==d.y end
   return math.abs(x-d.x)+math.abs(y-d.y)<=3
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
  elseif j.kind=='fabricate' then
   local s=w.structures[j.slot];local path,node,dist
   if not s or s.kind~='tool_bench' then j.reason='Tool bench no longer exists'
   elseif s.fabrication and s.fabrication.jobId~=j.id then j.reason='Tool bench is busy'
   else
    path,node,dist=closest(w,a,f,function(x,y) return N.reachRect(w,x,y,s.gx,s.gy) end)
    if not path then j.reason='Tool bench is unreachable'
    elseif j.delivered<Equipment.recipe(j.recipe).metal then
     local p,pp,nn,dd=itemChoice(w,a,f,'metal',true)
     if p then offer({kind='work',job=j.id,stage='fetch',item=p.id,path=pp,node=nn,label='Fetching metal for '..j.recipe},j.priority*100,dist+dd) else j.reason='Needs accessible metal' end
    else offer({kind='work',job=j.id,stage='work',path=path,node=node,label='Fabricating '..j.recipe},j.priority*100,dist) end
   end
  elseif j.kind=='tool_cargo' then
   local craft=context and require('src.logistics').craft(context.campaign,j.craftId);local item=feature(context) and Equipment.find(context.campaign,j.equipmentId) or nil
   if not craft or craft.dockedSiteId~=currentSite(context) then j.reason='Craft is no longer docked here'
   elseif not item then j.reason='Tool no longer exists'
   elseif j.cargoMode=='load' and item.state~='loose' then j.reason='Tool is no longer loose'
   elseif j.cargoMode=='unload' and item.state~='craft' then j.reason='Tool is no longer in craft'
   else
    local path,node,dist
    if j.cargoMode=='load' then path,node,dist=closest(w,a,f,function(x,y) return N.reach(w,x,y,item.x,item.y,4) end)
    else path,node,dist=closest(w,a,f,function(x,y) return N.reach(w,x,y,craft.anchor.x,craft.anchor.y,4) end) end
    if not path then j.reason='Tool or craft is unreachable' else offer({kind='work',job=j.id,stage=j.cargoMode=='load' and 'tool_fetch' or 'tool_unload',path=path,node=node,label=(j.cargoMode=='load' and 'Loading ' or 'Unloading ')..item.kind},200,dist) end
   end
  elseif j.kind=='rope' then
   local direction=j.ropeDirection or 'down'
   local anchorX= j.gx*4-3
   -- Downward ropes start at the top edge of the selected block.  Upward
   -- ropes are thrown/unfurled from the selected lower edge, then stored in
   -- the same canonical top-to-bottom rope representation.
   local anchorY=direction=='up' and j.gy*4 or j.gy*4-3
   local path,node,dist=closest(w,a,f,function(x,y) return N.reach(w,x,y,anchorX,anchorY,4) end)
   if not path then j.reason='Rope anchor is unreachable'
   elseif not feature(context) then j.reason='Ropes require a safe-excavation frontier'
   else
    local coil,pp,nn,dd=Equipment.nearestLoose(context.campaign,currentSite(context),'rope_coil',w,a,f,N.returnable(w,f))
    if coil then
     local px,py=W.xy(w,node)
     offer({kind='work',job=j.id,stage='rope_fetch',equipmentId=coil.id,path=pp,node=nn,anchorX=px,anchorY=py,ropeLaneX=anchorX,ropeStartY=anchorY,ropeDirection=direction,label=direction=='up' and 'Fetching rope coil to unfurl upward' or 'Fetching rope coil to unfurl downward'},j.priority*100,dist+dd)
    else j.reason='Needs accessible rope coil' end
   end
  elseif j.kind=='remove_rope' then
   local rope;for _,r in ipairs(w.ropes or {}) do if r.id==j.ropeId then rope=r end end
   local path,node,dist=rope and closest(w,a,f,function(x,y) return N.reach(w,x,y,rope.laneLeftX,rope.anchorY,4) end) or nil
   if not rope then j.reason='Rope no longer exists' elseif not path then j.reason='Rope is unreachable' else offer({kind='work',job=j.id,stage='work',path=path,node=node,label='Recovering rope'},j.priority*100,dist) end
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
   elseif j.kind=='build' and not S.complete(j.build,j.delivered) then
    local missing=S.missing(j.build,j.delivered)[1]
    local p,pp,nn,dd=itemChoice(w,a,f,missing.resource,true)
    if p then offer({kind='work',job=j.id,stage='fetch',item=p.id,path=pp,node=nn,label='Fetching '..p.kind},j.priority*100,dist+dd)
    else j.reason='Needs accessible '..missing.resource end
   else
    if j.kind=='dig' and feature(context) and not Equipment.pickFor(context.campaign,a) then
     local pick,pp,nn,dd=Equipment.nearestLoose(context.campaign,currentSite(context),'pickaxe',w,a,f,N.returnable(w,f))
     if pick then offer({kind='equipment',equipmentId=pick.id,path=pp,node=nn,label='Fetching pickaxe'},j.priority*100+15,dist+dd) end
    end
    offer({kind='work',job=j.id,stage='work',path=path,node=node,label=j.kind=='build' and 'Building '..j.build or j.kind},j.priority*100,dist)
   end
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
 if context and context.campaign and context.campaign.features.industry==1 then
  local Industry=require('src.industry')
  for slot=1,w.cols*w.rows do local s=w.structures[slot]
   if s and not s.user and W.supportedStructure(w,s) then
    local path,node,dist=closest(w,a,f,function(x,y) return N.reachRect(w,x,y,s.gx,s.gy) end)
    if path and Industry.needsMaintenance(s) then
     local p,pp,nn,dd=itemChoice(w,a,f,'component',true)
     if p then offer({kind='industry',stage='fetch',purpose='maintenance',structureId=s.id,slot=slot,item=p.id,amount=1,path=pp,node=nn,label='Fetching component for maintenance'},250,dist+dd) end
    elseif path and (s.kind=='fabricator' or s.kind=='industrial_bin') then
     local kind,need=Industry.inputNeed(s)
     if s.kind=='industrial_bin' and s.mode=='receive' then
      for _,p in ipairs(w.items) do if p.n>0 and not p.reserved and (not s.filter or p.kind==s.filter) then kind,need=p.kind,math.min(p.n,12);break end end
     end
     if kind and need and need>0 then
      local p,pp,nn,dd=itemChoice(w,a,f,kind,true)
     if p then offer({kind='industry',stage='fetch',purpose='feed',structureId=s.id,slot=slot,item=p.id,amount=math.min(need,12),path=pp,node=nn,label='Feeding '..S.def[s.kind].label},120,dist+dd) end
     end
    end
    if path and ((s.kind=='fabricator' or s.kind=='mining_rig') and s.output and s.output[1] or s.kind=='industrial_bin' and s.cargo and s.cargo[1] and s.mode=='supply') then
     offer({kind='industry',stage='withdraw',purpose='withdraw',structureId=s.id,slot=slot,path=path,node=node,label='Unloading '..S.def[s.kind].label},90,dist)
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
 Field.offer(w,a,f,closest,offer,context)
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
 elseif t.kind=='school' then
  return reRoute(w,a,function(x,y) return x==t.x and y==t.y end)
 elseif t.kind=='cargo' then
  local j=W.find(w.jobs,t.job)
  return j and context and require('src.logistics').routeToCraft(context.campaign,context.siteId,w,a,j,reRoute)
 elseif t.kind=='industry' then
  local s=require('src.industry').find(w,t.structureId)
  return s and reRoute(w,a,function(x,y) return N.reachRect(w,x,y,s.gx,s.gy) end)
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
 elseif t.kind=='equipment' then
  if not feature(context) then blocked(w,a,'Equipment is unavailable') return end
  local item=Equipment.find(context.campaign,t.equipmentId)
  if not item or item.state~='loose' or not N.reach(w,a.x,a.y,item.x,item.y,4) then blocked(w,a,'Pickaxe moved or became inaccessible') return end
  if not Equipment.reserve(item,a) then blocked(w,a,'Pickaxe is reserved') return end
  Equipment.equip(context.campaign,item,a);finish(w,a,'Equipped pickaxe')
 elseif t.kind=='field' then Field.act(w,a,t,finish,blocked,reRoute,context)
 elseif t.kind=='school' then
  local ok,why=require('src.education').act(w,a,t,context)
  if not ok then blocked(w,a,why) end
 elseif t.kind=='rally' then a.worked=false;a.status='Rally / holding';a.reason='J releases this worker to normal duties'
 elseif t.kind=='escape' or t.kind=='panic_escape' then finish(w,a,t.kind=='panic_escape' and 'Panicked but safe' or 'Reached safety')
 elseif t.kind=='eat' then
  local p=W.find(w.items,t.item)
  if not p or p.n<1 or not N.reach(w,a.x,a.y,p.x,p.y,4) then blocked(w,a,'Food unavailable') return end
  p.n=p.n-1;a.hunger=math.max(0,a.hunger-48);w.ledger.foodEaten=w.ledger.foodEaten+1
  finish(w,a,'Ate a ration')
 elseif t.kind=='rest' then
  a.worked=false;a.status=t.slot and 'Sleeping in bed' or 'Sleeping on ground'
  if a.fatigue<=25 then finish(w,a,'Rested') end
 elseif t.kind=='industry' then
  local Industry=require('src.industry');local s=Industry.find(w,t.structureId)
  if not s or not N.reachRect(w,a.x,a.y,s.gx,s.gy) then blocked(w,a,'Industrial structure changed or is unreachable') return end
  if t.stage=='fetch' then
   local p=W.find(w.items,t.item)
   if not p or p.n<=0 or p.reserved~=a.id or not N.reach(w,a.x,a.y,p.x,p.y,4) then blocked(w,a,'Industrial supply moved or became inaccessible') return end
   local n=math.min(p.n,t.amount or 1);a.carry={kind=p.kind,n=n};p.n=p.n-n;p.reserved=nil;t.item=nil;t.stage='deliver'
   if not routeDestination(w,a,context) then blocked(w,a,'Industrial destination is unreachable') end
  elseif t.stage=='withdraw' then
   local record=Industry.withdrawOne(s)
   if not record then finish(w,a,'Industrial output already moved') return end
   if record.equipmentId then
    local item=Equipment.find(context.campaign,record.equipmentId)
    if not item then blocked(w,a,'Industrial tool custody changed') return end
    Equipment.drop(context.campaign,item,currentSite(context),a.x,a.y)
    finish(w,a,'Unloaded '..record.kind)
   else
    a.carry={kind=record.kind,n=record.n};finish(w,a,'Unloaded '..record.kind)
   end
  elseif t.stage=='deliver' then
   if not a.carry then blocked(w,a,'Industrial carried supply is missing') return end
   if t.purpose=='maintenance' then
    if a.carry.kind~='component' or a.carry.n<1 or not Industry.needsMaintenance(s) then blocked(w,a,'Maintenance requirement changed') return end
    t.stage='work';t.progress=0;a.status='Maintaining '..S.def[s.kind].label
   elseif t.purpose=='feed' then
    if not Industry.deposit(s,a.carry) then blocked(w,a,'Machine or bin cannot accept this supply') return end
    a.carry=nil;finish(w,a,'Fed '..S.def[s.kind].label)
   else blocked(w,a,'Unknown industrial task') end
  elseif t.stage=='work' then
   if not Industry.needsMaintenance(s) or not a.carry or a.carry.kind~='component' then blocked(w,a,'Maintenance state changed') return end
   t.progress=t.progress+contribution(w,a,context,t,1)
   if t.progress>=60 then
    a.carry.n=a.carry.n-1;if a.carry.n<=0 then a.carry=nil end
    Industry.completeMaintenance(w,s);finish(w,a,'Maintained '..S.def[s.kind].label)
   end
  else blocked(w,a,'Industrial task state changed') end
 elseif t.stage=='fetch' then
  if t.item then
   local p=W.find(w.items,t.item)
   if not p or p.n<=0 or p.reserved~=a.id or not N.reach(w,a.x,a.y,p.x,p.y,4) then blocked(w,a,'Supply moved or became inaccessible') return end
   local capacity=t.kind=='irrigate' and 6 or 12
   if t.kind=='work' then
    local j=W.find(w.jobs,t.job)
    if j.kind=='fabricate' then capacity=math.min(capacity,Equipment.recipe(j.recipe).metal-j.delivered)
    else local missing=S.missing(j.build,j.delivered)[1];capacity=math.min(capacity,missing and missing.amount or 0) end
   end
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
   if type(j.delivered)=='number' then j.delivered=j.delivered+a.carry.n else j.delivered[a.carry.kind]=(j.delivered[a.carry.kind] or 0)+a.carry.n end;a.carry=nil
   if j.kind=='fabricate' then
    if j.delivered<Equipment.recipe(j.recipe).metal then finish(w,a,'Delivered partial fabrication metal') return end
   elseif not S.complete(j.build,j.delivered) then finish(w,a,'Delivered partial materials') return end
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
  if t.stage=='tool_fetch' then
   local item=feature(context) and Equipment.find(context.campaign,j.equipmentId) or nil
   if not item or item.state~='loose' or not N.reach(w,a.x,a.y,item.x,item.y,4) or not Equipment.reserve(item,a) then blocked(w,a,'Tool moved or became inaccessible') return end
   Equipment.carry(context.campaign,item,a);t.stage='tool_deliver'
   local craft=require('src.logistics').craft(context.campaign,j.craftId)
   if not reRoute(w,a,function(x,y) return N.reach(w,x,y,craft.anchor.x,craft.anchor.y,4) end) then blocked(w,a,'Craft is unreachable') end
   return
  elseif t.stage=='tool_deliver' then
   local item=feature(context) and Equipment.find(context.campaign,j.equipmentId) or nil;local craft=context and require('src.logistics').craft(context.campaign,j.craftId)
   local total=0;for _,n in pairs(craft and craft.cargo or {}) do total=total+n end
   if not item or item.state~='carried' or item.personId~=a.personId or not craft or craft.dockedSiteId~=currentSite(context) or total+Equipment.craftCount(context.campaign,craft.id)>=craft.capacity then blocked(w,a,'Craft cargo changed') return end
   Equipment.loadCraft(context.campaign,item,craft.id);j.state='done';j.reason='Loaded';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,'Loaded tool into craft');return
  elseif t.stage=='tool_unload' then
   local item=feature(context) and Equipment.find(context.campaign,j.equipmentId) or nil;local craft=context and require('src.logistics').craft(context.campaign,j.craftId)
   if not item or item.state~='craft' or not craft or craft.dockedSiteId~=currentSite(context) or not N.reach(w,a.x,a.y,craft.anchor.x,craft.anchor.y,4) then blocked(w,a,'Craft tool cargo changed') return end
   Equipment.unloadCraft(context.campaign,item,currentSite(context),a.x,a.y);j.state='done';j.reason='Unloaded';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,'Unloaded tool from craft');return
  elseif t.stage=='rope_fetch' then
   if not feature(context) then blocked(w,a,'Ropes are unavailable') return end
   local item=Equipment.find(context.campaign,t.equipmentId)
   if not item or item.state~='loose' or not N.reach(w,a.x,a.y,item.x,item.y,4) or not Equipment.reserve(item,a) then blocked(w,a,'Rope coil moved or became inaccessible') return end
   Equipment.carry(context.campaign,item,a);t.stage='rope_return'
   if not reRoute(w,a,function(x,y) return x==t.anchorX and y==t.anchorY end) then blocked(w,a,'Rope anchor is unreachable') end
   return
  elseif t.stage=='rope_return' then
   t.stage='rope_deploy';t.path={};t.next=1;return
  elseif t.stage=='rope_deploy' then
   local item=feature(context) and Equipment.find(context.campaign,t.equipmentId) or nil
   if t.ropePendingX then
    -- A support cell still occupies part of the lane.  Keep the real coil in
    -- the worker's custody until the existing dig progress reaches its normal
    -- terrain-mutation boundary.
    t.stage='rope_ready';t.path={};t.next=1;a.status='Rope ready for controlled descent';return
   end
   local length=0
   local startY=t.ropeStartY or t.anchorY;local lane=t.ropeLaneX or t.anchorX
   local direction=t.ropeDirection or j.ropeDirection or 'down'
   local topY=startY
   if direction=='up' then
    for y=startY,math.max(1,startY-23),-1 do
     if W.solid(w,lane,y) or W.solid(w,lane+1,y) then break end
     length=length+1;topY=y
    end
   else
    for y=startY,math.min(w.height,startY+23) do
     if W.solid(w,lane,y) or W.solid(w,lane+1,y) then break end
     length=length+1
    end
   end
   local ok,rope=pcall(Equipment.installRope,context.campaign,w,currentSite(context),item,lane,topY,length)
   if not ok then blocked(w,a,'Rope path is obstructed') return end
   w.navRevision=w.navRevision+1
   if j.kind=='rope' then j.state='done';j.reason='Installed';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,direction=='up' and 'Unfurled rope upward' or 'Unfurled rope downward')
   else t.stage='work';t.path={};t.next=1;a.status='Rope installed' end
   return
  end
  if j.kind~='dig' and not workPose(w,j,a.x,a.y) then blocked(w,a,'Work position invalid') return end
  if j.kind=='dig' then
   local c=J.digCell(w,j,a.x,a.y)
   if not c then
    if not J.digRemaining(w,j) then j.state='done';j.reason='Completed';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a)
    elseif not routeDestination(w,a,context) then blocked(w,a,'No reachable excavation face') end
    return
   end
   local safe,why=digSafety(w,a,c,context,t)
   if not safe then
    -- `digSafety` converts a controllable descent into a physical rope-fetch
    -- substage.  It remains the same player designation; releasing it here
    -- would make the automatic safety repair impossible.
    if why=='reposition' or why=='rope' then return end
    blocked(w,a,why);return
   end
   t.progress=t.progress+contribution(w,a,context,t,a.mine*Equipment.digWork(context and context.campaign,a,c.m))
   if t.progress>=M.def[c.m].work then
    t.progress=0
   if c.m==M.ICE then W.put(w,c.x,c.y,M.WATER)
   else
    W.put(w,c.x,c.y,M.AIR);W.stack(w,M.def[c.m].resource,1,a.x,a.y)
    w.ledger.mined=w.ledger.mined+1
    require('src.signals').emit(w,'mining',c.x,c.y,4)
   end
   if t.stage=='rope_ready' then
    local item=Equipment.find(context.campaign,t.equipmentId);local length=0
    for y=t.anchorY,math.min(w.height,t.anchorY+23) do
     if W.solid(w,t.ropeLaneX,y) or W.solid(w,t.ropeLaneX+1,y) then break end
     length=length+1
    end
    local ok=pcall(Equipment.installRope,context.campaign,w,currentSite(context),item,t.ropeLaneX,t.anchorY,length)
    if not ok then blocked(w,a,'Rope attachment changed before descent') return end
    w.navRevision=w.navRevision+1;t.stage='work';t.ropePendingX=nil;t.ropePendingY=nil
    a.status='Rope controls descent'
   end
   end
  elseif j.kind=='build' then
   local clear,why=S.siteClear(w,j.gx,j.gy,j.build)
   if not clear then blocked(w,a,why) return end
   if not workPose(w,j,a.x,a.y) then blocked(w,a,'Work position invalid') return end
   j.progress=j.progress+contribution(w,a,context,t,a.build)
   if j.progress>=S.def[j.build].work then
    S.install(w,j.gx,j.gy,j.build);w.ledger.built=w.ledger.built+deliveredTotal(j.delivered);j.delivered=type(j.delivered)=='number' and 0 or {}
    j.state='done';j.reason='Completed';w.stats.jobsDone=w.stats.jobsDone+1
    W.event(w,'build',a.name..' completed '..S.def[j.build].label..'.',j.id);finish(w,a)
   end
  elseif j.kind=='fabricate' then
   local s=w.structures[j.slot]
   if not s or s.kind~='tool_bench' or j.delivered<Equipment.recipe(j.recipe).metal then blocked(w,a,'Tool bench state changed') return end
   s.fabrication={jobId=j.id,kind=j.recipe,progress=j.progress,work=Equipment.recipe(j.recipe).work}
   j.progress=j.progress+contribution(w,a,context,t,1);s.fabrication.progress=j.progress
   if j.progress>=Equipment.recipe(j.recipe).work then
    local outputId
    if j.recipe=='component' then
     local pile=W.stack(w,'component',1,s.gx*4-2,s.gy*4-3);outputId=pile.id
    else
     local ok,item=pcall(Equipment.create,context.campaign,currentSite(context),j.recipe,s.gx*4-2,s.gy*4-3)
     if not ok then blocked(w,a,'No valid tool-bench output space') return end
     outputId=item.id
    end
    s.fabrication=nil;j.state='done';j.reason='Completed';w.stats.jobsDone=w.stats.jobsDone+1;W.event(w,'fabricate',a.name..' fabricated '..j.recipe..'.',outputId);finish(w,a)
   end
  elseif j.kind=='arm' then
   if feature(context) and a.panic then blocked(w,a,'Panicked workers cannot arm demolition charges') return end
   local s=w.structures[W.slot(w,j.gx,j.gy)]
   if not s or s.kind~='charge' or s.fuseAt then blocked(w,a,'Charge state changed') return end
   local f=N.flood(w,a.x,a.y);local escape=false
   for _,i in ipairs(f.queue) do local x,y=W.xy(w,i);if math.abs(x-(s.gx*4-2))+math.abs(y-(s.gy*4-2))>require('src.blasts').radius+3 then escape=true;break end end
   if not escape then blocked(w,a,'No safe evacuation route for charge arming') return end
   j.progress=j.progress+contribution(w,a,context,t,1)
   if j.progress>=20 then require('src.blasts').arm(w,s,a);j.state='done';j.reason='Armed';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,'Armed demolition charge') end
  elseif j.kind=='remove_rope' then
   local rope;for _,r in ipairs(w.ropes or {}) do if r.id==j.ropeId then rope=r end end
   if not rope or not feature(context) then blocked(w,a,'Rope no longer exists') return end
   j.progress=j.progress+contribution(w,a,context,t,1)
   if j.progress>=20 then Equipment.removeRope(context.campaign,w,currentSite(context),rope,a.x,a.y);j.state='done';j.reason='Recovered';w.stats.jobsDone=w.stats.jobsDone+1;finish(w,a,'Recovered rope coil') end
  else
   local slot=W.slot(w,j.gx,j.gy);local s=w.structures[slot]
   if not s then j.state='done';finish(w,a) return end
   if s.kind=='charge' and s.fuseAt then blocked(w,a,'Armed charge cannot be dismantled') return end
   j.progress=j.progress+contribution(w,a,context,t,1)
   if j.progress>=20 then
    local def=S.def[s.kind];local recovered=math.floor(def.cost/2)
    if s.kind=='field_school' then
     W.stack(w,'stone',2,a.x,a.y);W.stack(w,'metal',1,a.x,a.y)
    else W.stack(w,def.resource,recovered,a.x,a.y) end
    w.ledger.demolitionWaste=w.ledger.demolitionWaste+def.cost-recovered
    if s.tank and s.tank>0 then W.stack(w,'water',s.tank,a.x,a.y) end
    require('src.industry').destroy(w,s,context,a.x,a.y,false)
    require('src.education').destroy(w,s);w.structures[slot]=nil;w.navRevision=w.navRevision+1;j.state='done';w.stats.jobsDone=w.stats.jobsDone+1
    W.event(w,'remove',a.name..' dismantled '..def.label..'; half the construction material was lost.',j.id)
    finish(w,a)
   end
  end
 elseif t.kind=='harvest' then
  local s=w.structures[t.slot]
  if not N.reachRect(w,a.x,a.y,s.gx,s.gy) or not W.supportedStructure(w,s) or s.growth<w.rules.cropTicks or S.wet(w,s)>=4 then blocked(w,a,'Crop no longer harvestable') return end
  t.progress=t.progress+contribution(w,a,context,t,1)
  if t.progress>=20 then
   W.stack(w,'food',w.rules.cropYield,s.gx*4-2,s.gy*4)
   s.growth=0;w.ledger.foodGrown=w.ledger.foodGrown+w.rules.cropYield
   W.event(w,'harvest',a.name..' harvested '..w.rules.cropYield..' rations.',s.id);finish(w,a)
  end
 elseif t.kind=='pump' then
  local s=w.structures[t.slot]
  if not N.reachRect(w,a.x,a.y,s.gx,s.gy) or not S.pump(w,s) then finish(w,a,'Pump stopped')
  else s.status='Pumping: '..a.name;t.progress=t.progress+contribution(w,a,context,t,1)
   if t.progress>=120 then finish(w,a,'Operator rotation') end
  end
 end
end
return J
