-- COS-G04's deliberately small physical industrial layer.  Networks and
-- overlays are derived from structures; buffers, charge, wear and cargo are
-- authoritative local state.
local W=require('src.world')
local M=require('src.materials')
local S=require('src.structures')
local U=require('src.util')
local I={version=1}

I.recipes={
 component={id='component/v1',input={metal=2},output={component=1},ticks=40},
 pickaxe={id='pickaxe/v1',input={metal=2},output={pickaxe=1},ticks=50},
 rope_coil={id='rope-coil/v1',input={metal=1},output={rope_coil=1},ticks=30},
 frontier_carbine={id='frontier-carbine/v1',input={metal=2,component=1},output={frontier_carbine=1},ticks=180},
 shock_baton={id='shock-baton/v1',input={metal=1,component=1},output={shock_baton=1},ticks=100},
 protective_vest={id='protective-vest/v1',input={metal=2,component=1},output={protective_vest=1},ticks=150},
 frontier_suit={id='frontier-suit/v1',input={metal=2,component=1},output={frontier_suit=1},ticks=150},
 ammunition={id='ammunition/v1',input={metal=1},output={ammunition=6},ticks=60},
}
local industrial={solar_array=true,power_pole=true,battery=true,fabricator=true,mining_rig=true,industrial_bin=true,conveyor=true,electric_lamp=true,signal_relay=true,environmental_regulator=true,relic_analyzer=true}
local consumer={fabricator=2,mining_rig=3,electric_lamp=1,signal_relay=1,environmental_regulator=2,relic_analyzer=2}
local unique={pickaxe=true,rope_coil=true,frontier_carbine=true,shock_baton=true,protective_vest=true,frontier_suit=true}
local resources={metal=true,component=true,stone=true,soil=true,food=true,water=true,ammunition=true}

function I.enabled(c) return c and c.features and c.features.industry==1 end
function I.attach(w)
 w.industry={version=I.version,topologyRevision=1,rules={version=1,poleRange=6,coverage=3,batteryCap=200,batteryRate=6,conveyorEvery=2}}
end
function I.install(w,s)
 if s.kind=='solar_array' then s.solarRemainder=0
 elseif s.kind=='battery' then s.charge=0
 elseif s.kind=='fabricator' then s.input={};s.output={};s.recipe=nil;s.progress=0;s.inprocess=nil;s.wear=0;s.wearRemainder=0;s.powerPriority=2
 elseif s.kind=='mining_rig' then s.output={};s.wear=0;s.wearRemainder=0;s.powerPriority=2;s.rigProgress={}
 elseif s.kind=='industrial_bin' then s.cargo={};s.mode='receive';s.filter=nil;s.direction='east'
 elseif s.kind=='conveyor' then s.cargo={};s.direction='east'
 elseif s.kind=='electric_lamp' or s.kind=='signal_relay' or s.kind=='environmental_regulator' or s.kind=='relic_analyzer' then s.powerPriority=1 end
end
function I.recipe(kind) return I.recipes[kind] end
function I.find(w,id)
 for _,s in pairs(w.structures) do if s.id==id then return s end end
end
local function sorted(w)
 local out={};for _,s in pairs(w.structures) do if industrial[s.kind] then out[#out+1]=s end end
 table.sort(out,function(a,b)return a.id<b.id end);return out
end
local function units(record) return record and (record.equipmentId and 1 or record.n or 0) or 0 end
local function capacity(list) local n=0;for _,r in ipairs(list or {}) do n=n+units(r) end;return n end
local function compatible(a,b) return a.kind==b.kind and not a.equipmentId and not b.equipmentId end
local function add(list,record,limit)
 local n=units(record);if n<=0 or capacity(list)+n>limit then return false end
 for _,old in ipairs(list) do if compatible(old,record) then old.n=old.n+record.n;return true end end
 list[#list+1]={kind=record.kind,n=record.n,equipmentId=record.equipmentId};return true
end
local function take(list,kind,n)
 for i,r in ipairs(list or {}) do if r.kind==kind and not r.equipmentId and r.n>=n then
  local out={kind=kind,n=n};r.n=r.n-n;if r.n==0 then table.remove(list,i) end;return out
 end end
end
local function count(list,kind)
 local n=0;for _,r in ipairs(list or {}) do if r.kind==kind then n=n+units(r) end end;return n
end
I.capacity=capacity;I.addCargo=add;I.takeCargo=take;I.countCargo=count
function I.inputNeed(s)
 local recipe=s and s.recipe and I.recipes[s.recipe]
 if not recipe or s.inprocess then return nil end
 for kind,n in pairs(recipe.input) do
  local have=count(s.input,kind)
  if have<n then return kind,n-have end
 end
end
function I.deposit(s,record)
 assert(s and record,'Industrial destination is missing')
 if s.kind=='fabricator' then
  local recipe=s.recipe and I.recipes[s.recipe]
  if not recipe or not recipe.input[record.kind] then return false end
  return add(s.input,record,16)
 elseif s.kind=='industrial_bin' then
  if s.mode~='receive' or (s.filter and s.filter~=record.kind) then return false end
  return add(s.cargo,record,32)
 end
 return false
end
function I.withdrawOne(s)
 local list=s and (s.output or s.cargo)
 local record=list and list[1]
 if not record then return nil end
 if record.equipmentId then table.remove(list,1);return {kind=record.kind,equipmentId=record.equipmentId} end
 record.n=record.n-1;local out={kind=record.kind,n=1}
 if record.n==0 then table.remove(list,1) end
 return out
end
function I.needsMaintenance(s) return s and (s.kind=='fabricator' or s.kind=='mining_rig') and s.wear>=600 end
function I.completeMaintenance(w,s)
 assert(I.needsMaintenance(s),'Machine does not require maintenance')
 s.wear=0;s.status='Maintained';w.ledger.demolitionWaste=w.ledger.demolitionWaste+2
end
local function center(s,block)
 return s.gx+(block or 0),s.gy
end
local function nearPole(s,pole)
 for bx=s.gx,s.gx+(s.width or 1)-1 do
  local dx,dy=bx-pole.gx,s.gy-pole.gy
  if dx*dx+dy*dy<=9 then return true end
 end
 return false
end
function I.topology(w)
 if not w.industry then return {networks={},byStructure={}} end
 local cached=w._industryTopology
 if cached and cached.revision==w.industry.topologyRevision then return cached end
 local all=sorted(w);local poles={};for _,s in ipairs(all) do if s.kind=='power_pole' then poles[#poles+1]=s end end
 local parent={};for i=1,#poles do parent[i]=i end
 local function root(a) while parent[a]~=a do parent[a]=parent[parent[a]];a=parent[a] end;return a end
 local function join(a,b) a,b=root(a),root(b);if a~=b then parent[b]=a end end
 for i=1,#poles do for j=i+1,#poles do local dx,dy=poles[i].gx-poles[j].gx,poles[i].gy-poles[j].gy;if dx*dx+dy*dy<=36 then join(i,j) end end end
 local nets,byPole,byStructure={},{},{}
 for i,p in ipairs(poles) do local r=root(i);local net=nets[r] or {key=r,poles={},members={}};nets[r]=net;net.poles[#net.poles+1]=p;byPole[p.id]=net end
 for _,s in ipairs(all) do if s.kind~='power_pole' then
  local best
  for _,p in ipairs(poles) do if nearPole(s,p) then local n=byPole[p.id];if not best or n.key<best.key then best=n end end end
  if best then best.members[#best.members+1]=s;byStructure[s.id]=best end
 end end
 local list={};for _,n in pairs(nets) do
  table.sort(n.poles,function(a,b)return a.id<b.id end);table.sort(n.members,function(a,b)return a.id<b.id end);list[#list+1]=n
 end;table.sort(list,function(a,b)return a.poles[1].id<b.poles[1].id end)
 cached={revision=w.industry.topologyRevision,networks=list,byStructure=byStructure};w._industryTopology=cached;return cached
end
local function sky(w,s,half)
 local bx=s.gx+half;local x=(bx-1)*4+2;local _,y1= W.rect(bx,s.gy)
 for y=y1-1,3,-1 do if W.solid(w,x,y) then return false end end
 return true
end
local function solar(w,s)
 local n=0;for half=0,(s.width or 1)-1 do if sky(w,s,half) then n=n+3 end end
 local pct=w.environment and w.environment.solarPct or 100
 local raw=n*pct+(s.solarRemainder or 0);s.solarRemainder=raw%100;return math.floor(raw/100)
end
local function recipeReady(s)
 local r=s.recipe and I.recipes[s.recipe];if not r or not s.enabled or s.wear>=600 or (s.sabotagedUntil and s.sabotagedUntil>=(s._tick or 0)) then return false end
 if s.progress>=r.ticks-1 and capacity(s.output)>=16 then return false end
 if not s.inprocess then for kind,n in pairs(r.input) do if count(s.input,kind)<n then return false end end end
 return true
end
local function lineClear(w,x,y,tx,ty,ignore)
 local steps=math.max(math.abs(tx-x),math.abs(ty-y))*2
 for k=1,math.max(0,steps-1) do local xx=math.floor(x+(tx-x)*k/steps+.5);local yy=math.floor(y+(ty-y)*k/steps+.5)
  local insideIgnore=ignore and xx>=ignore.x1 and xx<=ignore.x2 and yy>=ignore.y1 and yy<=ignore.y2
  -- The designated solid endpoint is the thing being drilled, not an
  -- intervening blocker.  Rounding a short vertical segment can otherwise
  -- visit that endpoint on its penultimate sample.
  if k>0 and not (xx==tx and yy==ty) and not insideIgnore and W.solid(w,xx,yy) then return false end
 end;return true
end
local function rigTarget(w,s)
 -- The drill mouth is just beyond the rig's right face.  Starting its segment
 -- at the visual centre made the rig's own floor support block every downward
 -- line before it could reach a designated side face.
 local x1,y1,x2,y2=S.footprint(s)
 local ox,oy=x2+1,math.floor((y1+y2)/2);local best
 local own={x1=x1,y1=y1,x2=x2,y2=y2}
 for _,j in ipairs(w.jobs) do if j.state=='open' and j.kind=='dig' then
  local x1,y1,x2,y2=W.rect(j.gx,j.gy)
  for y=y1,y2 do for x=x1,x2 do local m=W.get(w,x,y);local d=math.abs(x-ox)+math.abs(y-oy)
   if M.def[m].work and not W.blocked(w,x,y) and d<=12 and lineClear(w,ox,oy,x,y,own) then
    local item={job=j,x=x,y=y,m=m,d=d};if not best or j.priority<best.job.priority or (j.priority==best.job.priority and (d<best.d or (d==best.d and (y<best.y or (y==best.y and x<best.x))))) then best=item end
   end
  end end
 end end;return best
end
local function rigReady(w,s)
 return s.enabled and s.wear<600 and (not s.sabotagedUntil or s.sabotagedUntil<w.tick) and capacity(s.output)<16 and rigTarget(w,s)~=nil
end
local function consumerReady(w,s)
 if s.sabotagedUntil and s.sabotagedUntil>=w.tick then return false end
 if s.kind=='fabricator' then return recipeReady(s) end
 if s.kind=='mining_rig' then return rigReady(w,s) end
 if s.kind=='electric_lamp' then return s.enabled end
 if s.kind=='signal_relay' then return s.enabled end
 if s.kind=='environmental_regulator' then return s.enabled end
 if s.kind=='relic_analyzer' then return s.enabled end
 return false
end
local function allocation(w)
 local top=I.topology(w)
 for _,s in ipairs(sorted(w)) do s._powerGranted=false end
 for _,net in ipairs(top.networks) do
  local solarPower=0;local batteries={};local users={}
  for _,s in ipairs(net.members) do
   if s.kind=='solar_array' and s.enabled and (not s.sabotagedUntil or s.sabotagedUntil<w.tick) then solarPower=solarPower+solar(w,s)
   elseif s.kind=='battery' and s.enabled and (not s.sabotagedUntil or s.sabotagedUntil<w.tick) then batteries[#batteries+1]=s
   elseif consumer[s.kind] and consumerReady(w,s) then users[#users+1]=s end
  end
  table.sort(batteries,function(a,b)return a.id<b.id end);table.sort(users,function(a,b) if (a.powerPriority or 2)~=(b.powerPriority or 2) then return (a.powerPriority or 2)<(b.powerPriority or 2) end;return a.id<b.id end)
  local available=solarPower
  for _,s in ipairs(users) do local demand=consumer[s.kind]
   if available>=demand then available=available-demand;s._powerGranted=true
   else
    local missing=demand-available;local possible=0;for _,b in ipairs(batteries) do possible=possible+math.min(6,b.charge or 0) end
    if possible>=missing then
     local need=missing;available=0;for _,b in ipairs(batteries) do local n=math.min(need,6,b.charge);b.charge=b.charge-n;need=need-n;if need==0 then break end end;s._powerGranted=true
    end
   end
  end
  for _,b in ipairs(batteries) do local n=math.min(available,6,200-(b.charge or 0));b.charge=(b.charge or 0)+n;available=available-n end
  net.generation=solarPower;net.leftover=available
 end
 return top
end
local function consumeInputs(s,r)
 s.inprocess={};for kind,n in pairs(r.input) do s.inprocess[#s.inprocess+1]=assert(take(s.input,kind,n),'Fabricator input changed') end
end
local function outputFits(s,r)
 local n=0;for _,amount in pairs(r.output) do n=n+amount end;return capacity(s.output)+n<=16 end
local function addWear(w,s)
 local raw=(s.wearRemainder or 0)+(w.environment and w.environment.machineWearPct or 100)
 s.wear=s.wear+math.floor(raw/100);s.wearRemainder=raw%100
end
local function fabricate(w,s,context)
 local r=I.recipes[s.recipe];if not r or not s._powerGranted or not recipeReady(s) then return end
 if not s.inprocess then consumeInputs(s,r) end
 if s.progress>=r.ticks-1 and not outputFits(s,r) then s.status='Output full';return end
 s.progress=s.progress+1;addWear(w,s)
 if s.progress>=r.ticks then
  local c=context and context.campaign
  for kind,n in pairs(r.output) do
   if unique[kind] then
    local E=require('src.equipment');local item=E.create(c,context.siteId,kind,s.gx*4-2,s.gy*4-3);E.toIndustry(item,context.siteId,s.id,'output');add(s.output,{kind=kind,equipmentId=item.id},16)
   else assert(add(s.output,{kind=kind,n=n},16),'Fabricator output changed') end
  end
  s.inprocess=nil;s.progress=0;s.status='Completed '..r.id
 else s.status='Fabricating '..r.id..' '..s.progress..'/'..r.ticks end
end
local function mine(w,s)
 if not s._powerGranted or not rigReady(w,s) then return end
 local t=rigTarget(w,s);if not t then return end
 -- A rig cannot mine the exact floor cells that carry its own footprint, but
 -- it may work a designated face beside that footing.  Comparing only y here
 -- would wrongly reject every low side-wall target and make a properly
 -- installed rig unable to excavate at all.
 local x1,_,x2,y2=S.footprint(s)
 if t.y==y2+1 and t.x>=x1 and t.x<=x2 then s.status='Support would fail';return end
 local key=W.index(w,t.x,t.y);local p=(s.rigProgress[key] or 0)+4;s.rigProgress[key]=p
 if p>=M.def[t.m].work then
  if capacity(s.output)>=16 then s.status='Output full';return end
  s.rigProgress[key]=nil
  if t.m==M.ICE then W.put(w,t.x,t.y,M.WATER) else
   W.put(w,t.x,t.y,M.AIR);assert(add(s.output,{kind=M.def[t.m].resource,n=1},16),'Rig output changed');w.ledger.mined=w.ledger.mined+1
  end
 end
 addWear(w,s);s.status='Mining designated face'
end
local dirs={north={0,-1},south={0,1},east={1,0},west={-1,0}}
local function neighbor(w,s,direction)
 local d=dirs[direction or 'east'];if not d then return nil end
 local gx=s.gx+d[1]*(s.width or 1);local gy=s.gy+d[2]
 if gx<1 or gx>w.cols or gy<1 or gy>w.rows then return nil end
 return w.structures[W.slot(w,gx,gy)]
end
local function accepts(dest,r)
 if not dest then return false end
 if dest.kind=='conveyor' then return (not dest.cargo[1] or (dest.cargo[1].kind==r.kind and not dest.cargo[1].equipmentId)) and capacity(dest.cargo)<8 end
 if dest.kind=='industrial_bin' then return dest.mode=='receive' and (not dest.filter or dest.filter==r.kind) and capacity(dest.cargo)<32 end
 if dest.kind=='fabricator' then
  local recipe=dest.recipe and I.recipes[dest.recipe];return recipe and recipe.input[r.kind] and capacity(dest.input)<16
 end
 return false
end
local function ownerFor(s)
 if s.kind=='fabricator' then return 'input' end
 if s.kind=='conveyor' then return 'belt' end
 if s.kind=='industrial_bin' then return 'bin' end
 return 'output'
end
local function moveOne(from,to,context)
 local r=from[1];if not r then return false end
 local piece={kind=r.kind,n=r.equipmentId and nil or 1,equipmentId=r.equipmentId}
 if not accepts(to,piece) then return false end
 if r.equipmentId then table.remove(from,1) else r.n=r.n-1;if r.n==0 then table.remove(from,1) end end
 local ok=add(to.kind=='fabricator' and to.input or to.cargo,piece,to.kind=='fabricator' and 16 or to.kind=='industrial_bin' and 32 or 8)
 if ok and piece.equipmentId and context then
  local item=require('src.equipment').find(context.campaign,piece.equipmentId)
  if item then require('src.equipment').toIndustry(item,context.siteId,to.id,ownerFor(to)) end
 end
 return ok
end
local function transfers(w,context)
 if w.tick%2~=0 then return end
 local all=sorted(w);local beltStarts={}
 for _,s in ipairs(all) do if s.kind=='conveyor' and s.enabled then beltStarts[s.id]=s.cargo[1] and {kind=s.cargo[1].kind,n=s.cargo[1].n,equipmentId=s.cargo[1].equipmentId} or nil end end
 -- Existing belts move first from a snapshot; a newly injected item cannot
 -- travel again in this tick.
 for _,s in ipairs(all) do if s.kind=='conveyor' and beltStarts[s.id] then moveOne(s.cargo,neighbor(w,s,s.direction),context) end end
 for _,s in ipairs(all) do
  if (s.kind=='fabricator' or s.kind=='mining_rig') and s.output[1] then
   local best;for _,b in ipairs(all) do if b.kind=='conveyor' and b.enabled then
    local outward=(b.gy==s.gy and b.gx==s.gx-1 and b.direction=='west') or (b.gy==s.gy and b.gx==s.gx+(s.width or 1) and b.direction=='east') or (b.gx>=s.gx and b.gx<s.gx+(s.width or 1) and b.gy==s.gy-1 and b.direction=='north') or (b.gx>=s.gx and b.gx<s.gx+(s.width or 1) and b.gy==s.gy+1 and b.direction=='south')
    if outward and (not best or b.id<best.id) then best=b end
   end end;if best then moveOne(s.output,best,context) end
  elseif s.kind=='industrial_bin' and s.mode=='supply' and s.cargo[1] then moveOne(s.cargo,neighbor(w,s,s.direction),context) end
 end
end
function I.step(w,context)
 if not (context and I.enabled(context.campaign) and w.industry) then return end
 for _,s in ipairs(sorted(w)) do s._tick=w.tick end
 allocation(w)
 for _,s in ipairs(sorted(w)) do
  if s.sabotagedUntil and s.sabotagedUntil>=w.tick then s.status='Sabotaged / '..math.max(0,s.sabotagedUntil-w.tick)..' ticks'
  elseif s.kind=='fabricator' then fabricate(w,s,context)
  elseif s.kind=='mining_rig' then mine(w,s)
  elseif s.kind=='electric_lamp' then s.status=s._powerGranted and 'Lighting work area' or 'No power' end
  if s.kind=='environmental_regulator' then s.status=s._powerGranted and 'Protecting thermal/radiation radius 24' or 'No power' end
  if (s.kind=='fabricator' or s.kind=='mining_rig') and s.wear>=600 then s.status='Maintenance required' end
 end
 transfers(w,context)
end
function I.lightSources(w)
 local out={};for _,s in pairs(w.structures) do if s.kind=='electric_lamp' and s.enabled and s._powerGranted then out[#out+1]={x=s.gx*4-2,y=s.gy*4-2,radius=18,id=s.id} end end;table.sort(out,function(a,b)return a.id<b.id end);return out
end
function I.validConfig(s,payload)
 assert(s and industrial[s.kind],'Industrial structure is unavailable')
 assert(type(payload)=='table','Malformed industrial configuration')
 if payload.recipe then assert(I.recipes[payload.recipe],'Unknown industrial recipe');assert(s.kind=='fabricator','Only fabricators use recipes') end
 if payload.priority then assert(type(payload.priority)=='number' and payload.priority%1==0 and payload.priority>=1 and payload.priority<=3,'Invalid power priority');assert(consumer[s.kind],'Structure has no power priority') end
 if payload.direction then assert(dirs[payload.direction],'Invalid industrial direction');assert(s.kind=='conveyor' or s.kind=='industrial_bin','Structure has no direction') end
 if payload.mode then assert(payload.mode=='receive' or payload.mode=='supply','Invalid bin mode');assert(s.kind=='industrial_bin','Structure has no bin mode') end
 if payload.filter~=nil then assert(resources[payload.filter] or payload.filter=='pickaxe' or payload.filter=='rope_coil','Invalid bin filter');assert(s.kind=='industrial_bin','Structure has no bin filter') end
 if payload.enabled~=nil then assert(type(payload.enabled)=='boolean','Enabled must be boolean') end
 return true
end
function I.configure(w,s,payload)
 I.validConfig(s,payload)
 if payload.recipe=='frontier_suit' then assert(w.frontier and w.frontier.environments==1,'Frontier suits require environments') end
 if payload.recipe then s.recipe=payload.recipe;s.progress=0;s.inprocess=nil end
 if payload.priority then s.powerPriority=payload.priority end
 if payload.direction then s.direction=payload.direction end
 if payload.mode then s.mode=payload.mode end
 if payload.filter~=nil then s.filter=payload.filter end
 if payload.enabled~=nil then s.enabled=payload.enabled end
 return true
end
function I.destroy(w,s,context,x,y,blast)
 if not industrial[s.kind] then return end
 for _,list in ipairs({s.input,s.output,s.cargo}) do for _,r in ipairs(list or {}) do
  if r.equipmentId and context then local item=require('src.equipment').find(context.campaign,r.equipmentId);if item then require('src.equipment').drop(context.campaign,item,context.siteId,x,y) end
  elseif r.n then W.stack(w,r.kind,r.n,x,y) end
 end end
 if s.inprocess then
  if blast then
   for _,r in ipairs(s.inprocess) do if r.equipmentId and context then
    local item=require('src.equipment').find(context.campaign,r.equipmentId)
    if item then for i,v in ipairs(context.campaign.equipment.items) do if v==item then table.remove(context.campaign.equipment.items,i);break end end end
   elseif r.n then w.ledger.demolitionWaste=w.ledger.demolitionWaste+(r.kind=='component' and r.n*2 or r.n) end end
  else for _,r in ipairs(s.inprocess) do
   if r.equipmentId and context then local item=require('src.equipment').find(context.campaign,r.equipmentId);if item then require('src.equipment').drop(context.campaign,item,context.siteId,x,y) end
   elseif r.n then W.stack(w,r.kind,r.n,x,y) end
  end end
 end
 s.input={};s.output={};s.inprocess={};s.cargo={};if w.industry then w.industry.topologyRevision=w.industry.topologyRevision+1;w._industryTopology=nil end
end
function I.validateWorld(w)
 if not (w.frontier and w.frontier.industry==1) then assert(w.industry==nil,'Industry state requires industry feature');return true end
 assert(type(w.industry)=='table' and w.industry.version==I.version and type(w.industry.rules)=='table','Invalid industry state')
 U.integer(w.industry.topologyRevision,'Industry topology revision',1,100000000)
 local seenEquipment={}
 local function validateCargo(list,limit,label)
  assert(type(list)=='table',label..' cargo is missing');assert(capacity(list)<=limit,label..' capacity exceeded')
  for _,record in ipairs(list) do
   assert(type(record)=='table' and (record.kind=='metal' or record.kind=='component' or record.kind=='stone' or record.kind=='soil' or record.kind=='food' or record.kind=='water' or unique[record.kind]),'Unknown '..label..' cargo')
   if record.equipmentId then U.integer(record.equipmentId,label..' equipment ID',1,100000000);assert(record.n==nil,'Unique '..label..' cargo has a stack count');assert(not seenEquipment[record.equipmentId],'Duplicated industrial equipment cargo');seenEquipment[record.equipmentId]=true
   else U.integer(record.n,label..' quantity',1,limit) end
  end
 end
 for _,s in pairs(w.structures) do if industrial[s.kind] then
  assert((s.width or 1)==S.width(s.kind),'Industrial footprint mismatch')
  if s.kind=='battery' then U.integer(s.charge,'Battery charge',0,200)
  elseif s.kind=='fabricator' then assert(type(s.input)=='table' and type(s.output)=='table' and type(s.wear)=='number' and (s.recipe==nil or I.recipes[s.recipe]),'Invalid fabricator');U.integer(s.wear,'Fabricator wear',0,600);if s.wearRemainder~=nil then U.integer(s.wearRemainder,'Fabricator wear remainder',0,99) end;U.integer(s.progress,'Fabricator progress',0,1000000);U.integer(s.powerPriority,'Fabricator priority',1,3);validateCargo(s.input,16,'Fabricator input');validateCargo(s.output,16,'Fabricator output');if s.inprocess then validateCargo(s.inprocess,16,'Fabricator in-process') end
  elseif s.kind=='mining_rig' then U.integer(s.wear,'Rig wear',0,600);if s.wearRemainder~=nil then U.integer(s.wearRemainder,'Rig wear remainder',0,99) end;U.integer(s.powerPriority,'Rig priority',1,3);assert(type(s.output)=='table' and type(s.rigProgress)=='table','Invalid mining rig');validateCargo(s.output,16,'Mining rig output')
  elseif s.kind=='industrial_bin' then assert((s.mode=='receive' or s.mode=='supply') and dirs[s.direction],'Invalid industrial bin');validateCargo(s.cargo,32,'Industrial bin')
  elseif s.kind=='conveyor' then assert(dirs[s.direction],'Invalid conveyor');validateCargo(s.cargo,8,'Conveyor')
  elseif s.kind=='electric_lamp' then U.integer(s.powerPriority,'Lamp priority',1,3)
  elseif s.kind=='signal_relay' then U.integer(s.powerPriority,'Relay priority',1,3)
  elseif s.kind=='environmental_regulator' then U.integer(s.powerPriority,'Regulator priority',1,3)
  elseif s.kind=='solar_array' and s.solarRemainder~=nil then U.integer(s.solarRemainder,'Solar remainder',0,99) end
 end end;return true
end
return I
