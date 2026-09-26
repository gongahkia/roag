-- COS-G08 keeps the ancient layer deliberately small: nine immutable physical
-- objects, five caches, and two remote systems.  The records below are the
-- authoritative custody ledger; no UI or research helper gets a second copy.
local U=require('src.util')
local W=require('src.world')
local N=require('src.nav')
local Random=require('src.campaign_random')
local R={version=1,maxRelics=9,maxCaches=5,maxTransfers=9}

local descriptors={'black lattice','glass spindle','folded ring','pale polyhedron','humming cage','mottled prism','threaded lens','silent spindle','weathered crown'}
local nameA={'Aster','Cinder','Halcyon','Nacre','Peregrine','Sable','Vesper','Orison'}
local nameB={'Reach','Crown','Vale','Drift','Hollow','March','Gate','Basin'}
local remoteProfiles={'heavy_garden','cold_hollow','dust_basin','ash_world'}

local function exact(t,keys,label)
 assert(type(t)=='table',label..' must be a table')
 for k in pairs(t) do assert(keys[k],'Unknown '..label..' key '..tostring(k)) end
 for k in pairs(keys) do assert(t[k]~=nil,'Missing '..label..' key '..k) end
end
local function allowed(t,keys,label)
 assert(type(t)=='table',label..' must be a table')
 for k in pairs(t) do assert(keys[k],'Unknown '..label..' key '..tostring(k)) end
end
local function dense(t,label,limit)
 assert(type(t)=='table',label..' must be an array')
 local n,m=0,0
 for k in pairs(t) do U.integer(k,label..' index',1,limit);n=n+1;m=math.max(m,k) end
 assert(n==m and n<=limit,label..' has a hole or exceeds its limit')
 return n
end
local function enabled(c) return c and c.features and c.features.relics==1 end
R.enabled=enabled
local function site(c,id) return require('src.campaign').site(c,id) end
local function craft(c,id) return require('src.logistics').craft(c,id) end
local function personExists(c,personId)
 for _,record in ipairs(c.sites or {}) do if record.world then for _,worker in ipairs(record.world.workers) do if worker.personId==personId then return true end end end end
 for _,vehicle in ipairs(c.logistics and c.logistics.crafts or {}) do for _,passenger in ipairs(vehicle.passengers or {}) do if passenger.personId==personId then return true end end end
 return false
end
local function profile(kind) return U.deep(require('src.environments').catalog()[kind]) end
local function stream(seed,key) return Random.new(Random.derive(seed,key)) end
local function generatedName(seed,index)
 local s=stream(seed,'relic/system/'..index..'/name/v1')
 return nameA[Random.uniform(s,#nameA)]..' '..nameB[Random.uniform(s,#nameB)]
end
local function item(c,id)
 for _,x in ipairs(c.relics.items) do if x.id==id then return x end end
end
R.find=item
local function cache(c,id)
 for _,x in ipairs(c.relics.caches) do if x.id==id then return x end end
end
R.cache=cache
local function system(c,id)
 for _,x in ipairs(c.region.systems or {}) do if x.id==id then return x end end
end
R.system=system
function R.revealed(c,id) local s=system(c,id);return s and s.revealed or false end
function R.hiddenSite(c,id)
 local body=require('src.campaign').body(c,id)
 return body and body.systemId and body.systemId>1 and not R.revealed(c,body.systemId) or false
end
function R.roleFact(id) return 'relic/'..id..'/role/v1' end
function R.signatureFact(id) return 'relic/'..id..'/signature/v1' end
function R.knowsRole(worker,id) return require('src.knowledge').fact(worker,R.roleFact(id))~=nil end
function R.knowsSignature(worker,id) return require('src.knowledge').fact(worker,R.signatureFact(id))~=nil end
function R.describe(worker,relic)
 return {id=relic.id,descriptor=relic.descriptor,role=R.knowsRole(worker,relic.id) and relic.role or nil,family=R.knowsSignature(worker,relic.id) and relic.family or nil}
end

function R.systemRecords(seed)
 local second,third=generatedName(seed,2),generatedName(seed,3)
 if third==second then third=third..' II' end
 return {
  {id=1,seed=Random.derive(seed,'relic/system/1/v1'),name='Starting System',siteIds={1,2,3,4,5,6,7},revealed=true},
  {id=2,seed=Random.derive(seed,'relic/system/2/v1'),name=second,siteIds={8,9},revealed=false},
  {id=3,seed=Random.derive(seed,'relic/system/3/v1'),name=third,siteIds={10,11},revealed=false},
 }
end
function R.remoteBodies(seed)
 local systems=R.systemRecords(seed)
 local out={}
 for index,id in ipairs({8,9,10,11}) do
  local sys=index<=2 and systems[2] or systems[3]
  -- These fixed slots are seed-named and seed-terrained, while their profile
  -- pairing intentionally guarantees a habitable/solar-viable body per system.
  local kind=remoteProfiles[index]
  local primary=id==8 or id==10
  local parent=nil;if not primary then parent=id-1 end
  out[#out+1]={id=id,kind=primary and 'remote_primary' or 'remote_satellite',parentBodyId=parent,
   systemId=primary and (id==8 and 2 or 3) or (id==9 and 2 or 3),name=sys.name..(primary and ' Prime' or ' Moon'),
   siteId=id,terrainSeed=Random.derive(seed,'relic/body/'..id..'/terrain/v1'),environment=profile(kind),visited=false}
 end
 return out
end
function R.new(c)
 local items={}
 -- IDs 1..3 are the deliberately solvable, distributed initial trio.  The
 -- other starting items are deterministic alternatives, not additional keys.
 local roles={'core','lens','anchor','core','lens','anchor','core','lens','anchor'}
 local families={'K-3','K-3','K-3','M-7','M-7','N-2','N-2','M-7','N-2'}
 local origins={1,2,3,1,2,4,4,5,5}
 for id=1,9 do items[id]={id=id,descriptor=descriptors[id],role=roles[id],family=families[id],originCacheId=origins[id],state='cache',cacheId=origins[id]} end
 return {version=R.version,nextTransferId=1,items=items,
  caches={{id=1,siteId=4,relicIds={1,4},placed=false,empty=false,progress=0,workerId=0},
           {id=2,siteId=5,relicIds={2,5},placed=false,empty=false,progress=0,workerId=0},
           {id=3,siteId=6,relicIds={3},placed=false,empty=false,progress=0,workerId=0},
           {id=4,siteId=8,relicIds={6,7},placed=false,empty=false,progress=0,workerId=0},
           {id=5,siteId=10,relicIds={8,9},placed=false,empty=false,progress=0,workerId=0}},
  transfers={},drive={version=1,installed=false,progress=0,siteId=0,craftId=0,cooldownUntil=0,sockets={0,0,0},workerId=0}}
end

local function cleanLocation(x)
 x.cacheId=nil;x.siteId=nil;x.x=nil;x.y=nil;x.personId=nil;x.structureId=nil;x.craftId=nil;x.socket=nil;x.factionId=nil;x.raiderId=nil;x.transferId=nil
end
local function move(x,state,fields)
 cleanLocation(x);x.state=state
 for k,v in pairs(fields or {}) do x[k]=v end
end
function R.drop(c,relic,siteId,x,y) move(relic,'ground',{siteId=siteId,x=x,y=y}) return relic end
function R.carry(c,relic,worker) move(relic,'person',{personId=worker.personId}) return relic end
function R.pickup(c,siteId,relicId,personId)
 local record=site(c,siteId);local relic=item(c,relicId);local worker
 for _,a in ipairs(record and record.world and record.world.workers or {}) do if a.personId==personId then worker=a end end
 if not relic or relic.state~='ground' or relic.siteId~=siteId or not worker or not worker.alive or not N.reach(record.world,worker.x,worker.y,relic.x,relic.y,4) then return false,'Relic is not physically reachable' end
 R.carry(c,relic,worker);return true,'Relic picked up'
end
function R.dropPerson(c,personId,siteId,x,y)
 if not enabled(c) then return end
 for _,relic in ipairs(c.relics.items) do if relic.state=='person' and relic.personId==personId then R.drop(c,relic,siteId,x,y) end end
end
function R.atSite(c,siteId)
 local out={};for _,relic in ipairs(c.relics.items) do if relic.state=='ground' and relic.siteId==siteId then out[#out+1]=relic end end
 table.sort(out,function(a,b)return a.id<b.id end);return out
end
function R.craftCount(c,craftId)
 local n=0;for _,relic in ipairs(c.relics and c.relics.items or {}) do if relic.state=='craft' and relic.craftId==craftId then n=n+1 end end;return n
end
local function hasCraftSpace(c,vehicle)
 local used=0;for _,kind in ipairs(require('src.logistics').resources()) do used=used+(vehicle.cargo[kind] or 0) end
 if c.features.equipment==1 then used=used+require('src.equipment').craftCount(c,vehicle.id) end
 return used+R.craftCount(c,vehicle.id)<vehicle.capacity
end
function R.loadCraft(c,siteId,craftId,relicId,personId)
 local record=site(c,siteId);local vehicle=craft(c,craftId);local relic=item(c,relicId);local worker
 for _,a in ipairs(record and record.world and record.world.workers or {}) do if a.personId==personId then worker=a end end
 if not record or not vehicle or vehicle.dockedSiteId~=siteId or not relic or not worker or not worker.alive or not N.reach(record.world,worker.x,worker.y,vehicle.anchor.x,vehicle.anchor.y,4) then return false,'Docked shuttle or worker is unavailable' end
 if not hasCraftSpace(c,vehicle) then return false,'Shuttle cargo capacity is full' end
 if relic.state=='ground' and relic.siteId==siteId and N.reach(record.world,worker.x,worker.y,relic.x,relic.y,4) then move(relic,'craft',{craftId=craftId})
 elseif relic.state=='person' and relic.personId==personId then move(relic,'craft',{craftId=craftId}) else return false,'Relic is not physically available to this shuttle' end
 return true,'Relic loaded into shuttle cargo'
end
function R.unloadCraft(c,siteId,craftId,relicId,personId)
 local record=site(c,siteId);local vehicle=craft(c,craftId);local relic=item(c,relicId);local worker
 for _,a in ipairs(record and record.world and record.world.workers or {}) do if a.personId==personId then worker=a end end
 if not record or not vehicle or vehicle.dockedSiteId~=siteId or not relic or relic.state~='craft' or relic.craftId~=craftId or not worker or not worker.alive or not N.reach(record.world,worker.x,worker.y,vehicle.anchor.x,vehicle.anchor.y,4) then return false,'Shuttle relic cargo is unavailable' end
 R.carry(c,relic,worker);return true,'Relic removed from shuttle cargo'
end
function R.installWorld(c,siteId,w)
 if not enabled(c) then return end
 for _,entry in ipairs(c.relics.caches) do if entry.siteId==siteId and not entry.placed then
  local offset=(entry.id*17)%math.max(1,w.cols*w.rows);local chosen
  for n=0,w.cols*w.rows-1 do
   local slot=(offset+n)%(w.cols*w.rows)+1;local gx=(slot-1)%w.cols+1;local gy=math.floor((slot-1)/w.cols)+1
   local x,y=gx*4-2,gy*4
   if not w.structures[slot] and N.stand(w,x,y,true) then chosen={gx=gx,gy=gy};break end
  end
  assert(chosen,'Ancient cache needs one valid physical position')
  local s=require('src.structures').install(w,chosen.gx,chosen.gy,'ancient_cache')
  s.cacheId=entry.id;entry.placed=true;entry.gx=chosen.gx;entry.gy=chosen.gy
 end end
end
function R.cacheVisible(c,w,a,entry,context)
 if not entry.placed or entry.empty then return false end
 local s=w.structures[W.slot(w,entry.gx,entry.gy)]
 if not s then return false end
 if w.frontier.visibility==1 then return require('src.visibility').visible(w,a,entry.gx*4-2,entry.gy*4-2,context) end
 return true
end
function R.beginExcavation(c,siteId,cacheId,personId)
 local s=site(c,siteId);local q=cache(c,cacheId);if not s or not s.world or not q or q.siteId~=siteId or q.empty then return false,'Ancient cache is unavailable' end
 local worker;for _,a in ipairs(s.world.workers) do if a.personId==personId then worker=a end end
 if not worker or not worker.alive or not R.cacheVisible(c,s.world,worker,q,{campaign=c,siteId=siteId}) then return false,'Cache is not personally reachable and visible' end
 q.workerId=personId;return true,'Excavation ordered'
end
local function context(c,siteId) return {campaign=c,siteId=siteId} end
local function sourceFor(c,relic,siteId)
 return {siteId=siteId,category='sites',id=relic.originCacheId,kind='ancient_cache',definitionVersion=1}
end
function R.offer(c,w,a,f,closest)
 if not enabled(c) or not a.alive then return end
 local sid=w.frontier.siteId
 for _,q in ipairs(c.relics.caches) do if q.siteId==sid and q.workerId==a.personId and not q.empty and q.placed then
  local path,node=closest(w,a,f,function(x,y)return N.reachRect(w,x,y,q.gx,q.gy) end)
  if path then return {kind='relic',mode='excavate',cacheId=q.id,path=path,node=node,label='Excavating ancient cache'} end
 end end
 for _,s in pairs(w.structures) do if s.kind=='relic_analyzer' and s.relic and s.relic.task and s.relic.task.workerId==a.personId then
  local path,node=closest(w,a,f,function(x,y)return N.reachRect(w,x,y,s.gx,s.gy) end)
  if path then return {kind='relic',mode=s.relic.task.kind,structureId=s.id,path=path,node=node,label=s.relic.task.kind=='scan' and 'Scanning deep space' or 'Analyzing relic'} end
 end end
 local d=c.relics.drive
 if d.workerId==a.personId and not d.installed then
  local craftRecord=craft(c,d.craftId);if craftRecord and craftRecord.dockedSiteId==sid then
   local path,node=closest(w,a,f,function(x,y)return N.reach(w,x,y,craftRecord.anchor.x,craftRecord.anchor.y,4) end)
   if path then return {kind='relic',mode='frame',path=path,node=node,label='Fitting relic drive frame'} end
  end
 end
end
local function consume(w,a,materials)
 local selected={}
 for kind,n in pairs(materials) do
  local need=n;for _,p in ipairs(w.items) do if p.kind==kind and not p.reserved and N.reach(w,a.x,a.y,p.x,p.y,4) then
   local take=math.min(need,p.n);selected[#selected+1]={p=p,n=take};need=need-take;if need==0 then break end
  end end;if need>0 then return false end
 end
 for _,v in ipairs(selected) do v.p.n=v.p.n-v.n end
 return true
end
local function completeCache(c,w,q,a)
 q.empty=true;q.workerId=0;q.progress=180
 local structure=w.structures[W.slot(w,q.gx,q.gy)];if structure then structure.status='Excavated ancient cache';structure.enabled=false end
 for _,id in ipairs(q.relicIds) do R.drop(c,assert(item(c,id)),q.siteId,a.x,a.y) end
 local P=require('src.psychology');P.memory(c,a,'recovered_relic',{siteId=q.siteId,source='relic-cache:'..q.id})
 W.event(w,'ancient_cache','Ancient cache recovered.',q.id)
end
function R.act(c,w,a,t)
 if t.mode=='excavate' then
  local q=cache(c,t.cacheId);if not q or q.empty or q.siteId~=w.frontier.siteId or q.workerId~=a.personId or not N.reachRect(w,a.x,a.y,q.gx,q.gy) then return false,false,'Ancient cache changed or is unreachable' end
  q.progress=q.progress+1;if q.progress>=180 then completeCache(c,w,q,a);return true,true,'Ancient cache excavated' end
  return true,false,'Excavating ancient cache '..q.progress..'/180'
 elseif t.mode=='analysis' then
  local s=require('src.industry').find(w,t.structureId);local task=s and s.relic and s.relic.task
  if not s or not task or task.workerId~=a.personId or not s._powerGranted or not N.reachRect(w,a.x,a.y,s.gx,s.gy) then return false,false,'Relic analysis is unavailable' end
  task.progress=task.progress+1;local relic=item(c,task.relicId);local K=require('src.knowledge')
  if task.progress==120 then K.learn(context(c,w.frontier.siteId),a,R.roleFact(relic.id),'record',sourceFor(c,relic,w.frontier.siteId),{}) end
  if task.progress>=300 then K.learn(context(c,w.frontier.siteId),a,R.signatureFact(relic.id),'record',sourceFor(c,relic,w.frontier.siteId),{});s.relic.task=nil;return true,true,'Relic signature decoded' end
  return true,false,'Analyzing relic '..task.progress..'/300'
 elseif t.mode=='scan' then
  local s=require('src.industry').find(w,t.structureId);local task=s and s.relic and s.relic.task
  local relay=require('src.factions').relay(w)
  if not s or not task or not s._powerGranted or not relay or not relay._powerGranted or task.workerId~=a.personId or not N.reachRect(w,a.x,a.y,s.gx,s.gy) then return false,false,'Deep-space scan is unavailable' end
  task.progress=task.progress+1
  if task.progress>=300 then system(c,2).revealed=true;system(c,3).revealed=true;s.relic.task=nil;require('src.campaign').addNotice(c,w.frontier.siteId,'deep_scan','Two remote systems resolved by relic lens.',s.id);return true,true,'Deep-space systems revealed' end
  return true,false,'Scanning deep space '..task.progress..'/300'
 elseif t.mode=='frame' then
  local d=c.relics.drive;local v=craft(c,d.craftId)
  if not v or v.dockedSiteId~=w.frontier.siteId or not N.reach(w,a.x,a.y,v.anchor.x,v.anchor.y,4) then return false,false,'Shuttle is unavailable for refit' end
  if d.progress==0 and not consume(w,a,{metal=4,component=3}) then return false,false,'Needs reachable metal and Machine Components' end
  d.progress=d.progress+1
  if d.progress>=300 then d.installed=true;d.siteId=w.frontier.siteId;d.workerId=0;return true,true,'Relic Drive Frame installed' end
  return true,false,'Fitting Relic Drive Frame '..d.progress..'/300'
 end
 return false,false,'Unknown relic task'
end
function R.loadAnalyzer(c,siteId,structureId,relicId,personId)
 local record=site(c,siteId);local relic=item(c,relicId);local s=record and record.world and require('src.industry').find(record.world,structureId)
 local worker;for _,a in ipairs(record and record.world and record.world.workers or {}) do if a.personId==personId then worker=a end end
 if not s or s.kind~='relic_analyzer' or not relic or not worker or not worker.alive or not N.reachRect(record.world,worker.x,worker.y,s.gx,s.gy) then return false,'Analyzer or worker is unavailable' end
 s.relic.slots=s.relic.slots or {0,0};local open
 for i=1,2 do if s.relic.slots[i]==0 then open=i;break end end
 if not open then return false,'Analyzer slots are full' end
 if relic.state=='ground' and relic.siteId==siteId and N.reach(record.world,worker.x,worker.y,relic.x,relic.y,4) then move(relic,'analyzer',{siteId=siteId,structureId=s.id})
 elseif relic.state=='person' and relic.personId==personId then move(relic,'analyzer',{siteId=siteId,structureId=s.id}) else return false,'Relic is not in physical local custody' end
 s.relic.slots[open]=relicId;return true,'Relic loaded into Analyzer'
end
function R.beginAnalysis(c,siteId,structureId,relicId,personId)
 local record=site(c,siteId);local s=record and record.world and require('src.industry').find(record.world,structureId);local relic=item(c,relicId)
 if not s or s.kind~='relic_analyzer' or not s._powerGranted or not relic or relic.state~='analyzer' or relic.structureId~=s.id then return false,'Powered Analyzer and loaded relic are required' end
 local worker;for _,a in ipairs(record.world.workers) do if a.personId==personId then worker=a end end
 if not worker or not worker.alive then return false,'Analyst is unavailable' end
 s.relic.task={kind='analysis',relicId=relicId,workerId=personId,progress=0};return true,'Relic analysis ordered'
end
function R.unloadAnalyzer(c,siteId,structureId,relicId,personId)
 local record=site(c,siteId);local s=record and record.world and require('src.industry').find(record.world,structureId);local relic=item(c,relicId)
 local worker;for _,a in ipairs(record and record.world and record.world.workers or {}) do if a.personId==personId then worker=a end end
 if not s or s.kind~='relic_analyzer' or not relic or relic.state~='analyzer' or relic.structureId~=s.id or not worker or not N.reachRect(record.world,worker.x,worker.y,s.gx,s.gy) then return false,'Analyzer relic is unavailable' end
 if s.relic.task and s.relic.task.relicId==relicId then return false,'Active analysis must be cancelled before unloading' end
 for i,id in ipairs(s.relic.slots) do if id==relicId then s.relic.slots[i]=0;R.carry(c,relic,worker);return true,'Relic removed from Analyzer' end end
 return false,'Analyzer slot changed'
end
function R.beginScan(c,siteId,structureId,relicId,personId)
 local record=site(c,siteId);local s=record and record.world and require('src.industry').find(record.world,structureId);local relic=item(c,relicId)
 local worker;for _,a in ipairs(record and record.world and record.world.workers or {}) do if a.personId==personId then worker=a end end
 local relay=record and record.world and require('src.factions').relay(record.world)
 if not s or s.kind~='relic_analyzer' or not s._powerGranted or not relay or not relay._powerGranted or not relic or relic.state~='analyzer' or relic.structureId~=s.id or relic.role~='lens' or not worker or not R.knowsRole(worker,relicId) then return false,'Powered Analyzer, Relay, loaded identified lens and worker are required' end
 s.relic.task={kind='scan',relicId=relicId,workerId=personId,progress=0};return true,'Deep-space scan ordered'
end
function R.beginFrame(c,siteId,craftId,personId)
 local d=c.relics.drive;local v=craft(c,craftId);if d.installed or not v or v.dockedSiteId~=siteId then return false,'Landed shuttle is unavailable for refit' end
 d.craftId=craftId;d.siteId=siteId;d.workerId=personId;d.progress=0;return true,'Relic drive refit ordered'
end
function R.socket(c,siteId,craftId,relicId,personId,slot)
 local d=c.relics.drive;local v=craft(c,craftId);local relic=item(c,relicId);U.integer(slot,'Relic drive socket',1,3)
 if not d.installed or d.craftId~=craftId or not v or v.dockedSiteId~=siteId or not relic or d.sockets[slot]~=0 then return false,'Drive socket is unavailable' end
 local record=site(c,siteId);local worker;for _,a in ipairs(record.world.workers) do if a.personId==personId then worker=a end end
 if not worker or not N.reach(record.world,worker.x,worker.y,v.anchor.x,v.anchor.y,4) then return false,'Worker cannot reach the shuttle' end
 if relic.state=='ground' and relic.siteId==siteId and N.reach(record.world,worker.x,worker.y,relic.x,relic.y,4) then move(relic,'drive',{craftId=craftId,socket=slot}) elseif relic.state=='person' and relic.personId==personId then move(relic,'drive',{craftId=craftId,socket=slot}) else return false,'Relic is not locally available' end
 d.sockets[slot]=relicId;return true,'Relic installed in drive socket'
end
function R.unsocket(c,siteId,craftId,slot,personId)
 local d=c.relics.drive;local v=craft(c,craftId);U.integer(slot,'Relic drive socket',1,3);local id=d.sockets[slot] or 0
 if not d.installed or d.craftId~=craftId or not v or v.dockedSiteId~=siteId or id==0 then return false,'Drive socket is empty' end
 local record=site(c,siteId);local worker;for _,a in ipairs(record.world.workers) do if a.personId==personId then worker=a end end
 if not worker or not N.reach(record.world,worker.x,worker.y,v.anchor.x,v.anchor.y,4) then return false,'Worker cannot reach the shuttle' end
 R.carry(c,item(c,id),worker);d.sockets[slot]=0;return true,'Relic removed from drive socket'
end
function R.deepRoute(c,a,b)
 return (a==1 and (b==8 or b==10)) or (b==1 and (a==8 or a==10)) or ((a==8 and b==10) or (a==10 and b==8))
end
local function trio(c,craftId)
 local d=c.relics.drive;if not d.installed or d.craftId~=craftId then return nil,'incomplete' end
 local list={};for _,id in ipairs(d.sockets) do if id==0 then return nil,'incomplete' end;list[#list+1]=item(c,id) end
 local roles,family={},nil
 for _,x in ipairs(list) do roles[x.role]=(roles[x.role] or 0)+1;family=family or x.family;if x.family~=family then return list,'incompatible' end end
 if roles.core==1 and roles.lens==1 and roles.anchor==1 then return list,'compatible' end
 return list,'incompatible'
end
function R.driveStatus(c,craftId,observer)
 local list,state=trio(c,craftId);if state~='compatible' then return state end
 if not observer then return 'unknown' end
 for _,x in ipairs(list) do if not R.knowsRole(observer,x.id) or not R.knowsSignature(observer,x.id) then return 'unknown' end end
 return 'understood'
end
function R.compatibility(c,craftId,observer)
 local list,state=trio(c,craftId)
 if state=='incomplete' then return 'incomplete' end
 if not observer then return 'unknown' end
 for _,x in ipairs(list) do if not R.knowsSignature(observer,x.id) then return 'unknown' end end
 return state=='compatible' and 'compatible' or 'incompatible'
end
local function discharge(c,source,craftId)
 local P=require('src.psychology');local v=craft(c,craftId)
 for _,a in ipairs(source.world.workers) do if a.alive and math.abs(a.x-v.anchor.x)+math.abs(a.y-v.anchor.y)<=6 then
  a.hp=math.max(0,a.hp-5);P.memory(c,a,'relic_discharge',{siteId=source.id,stress=10,source='relic-discharge:'..c.tick..':'..a.personId})
 end end
 require('src.campaign').addNotice(c,source.id,'relic_discharge','Relic field collapsed during activation.',craftId)
end
function R.activation(c,source,vehicle,workers)
 local d=c.relics.drive;if c.tick<d.cooldownUntil then return false,'Relic drive is cooling down' end
 local list,state=trio(c,vehicle.id);if state~='compatible' then d.cooldownUntil=c.tick+600;discharge(c,source,vehicle.id);return false,'Relic field collapsed during activation' end
 local knowledgeable=false
 for _,a in ipairs(workers) do
  local all=true;for _,x in ipairs(list) do if not R.knowsRole(a,x.id) or not R.knowsSignature(a,x.id) then all=false end end
  if all then knowledgeable=true end
 end
 return true,{unstable=not knowledgeable}
end
function R.transitPulse(c,vehicle,journey)
 if not journey.deep or not journey.unstable or c.tick<journey.nextPulseAt then return end
 local P=require('src.psychology')
 for _,a in ipairs(vehicle.passengers) do if a.alive then a.hp=math.max(0,a.hp-2);P.memory(c,a,'unstable_deep_transit',{siteId=journey.originSiteId,stress=5,source='relic-pulse:'..journey.id..':'..journey.nextPulseAt..':'..a.personId}) end end
 journey.nextPulseAt=journey.nextPulseAt+600
end
function R.arrival(c,journey,world)
 local body=require('src.campaign').body(c,journey.destinationSiteId);if not body or body.systemId<=1 then return end
 local P=require('src.psychology');for _,a in ipairs(world.workers) do if a.alive then P.memory(c,a,'reached_star_system',{siteId=journey.destinationSiteId,source='star-arrival:'..body.systemId..':'..a.personId}) end end
end
function R.storeDepot(c,siteId,depotId,relicId,personId)
 local record=site(c,siteId);local depot=record and record.world and require('src.factions').depot(record.world,depotId);local relic=item(c,relicId)
 local worker;for _,a in ipairs(record and record.world and record.world.workers or {}) do if a.personId==personId then worker=a end end
 if not depot or not relic or not worker or not N.reachRect(record.world,worker.x,worker.y,depot.gx,depot.gy) then return false,'Trade depot or worker is unavailable' end
 if relic.state=='ground' and relic.siteId==siteId and N.reach(record.world,worker.x,worker.y,relic.x,relic.y,4) then move(relic,'depot',{siteId=siteId,structureId=depot.id}) elseif relic.state=='person' and relic.personId==personId then move(relic,'depot',{siteId=siteId,structureId=depot.id}) else return false,'Relic is not in local custody' end
 return true,'Relic stored in Trade Depot'
end
function R.trade(c,siteId,depotId,relicId,factionId)
 local relic=item(c,relicId);local record=site(c,siteId);local depot=record and record.world and require('src.factions').depot(record.world,depotId)
 if not relic or relic.state~='depot' or relic.siteId~=siteId or relic.structureId~=(depot and depot.id) or not require('src.factions').relay(record.world) or not require('src.factions').find(c,factionId) then return false,'Powered trade depot and faction are required' end
 local id=c.relics.nextTransferId;c.relics.nextTransferId=id+1;move(relic,'courier',{transferId=id,factionId=factionId})
 c.relics.transfers[#c.relics.transfers+1]={id=id,relicId=relicId,factionId=factionId,siteId=siteId,arrivalTick=c.tick+400};return true,'Relic courier dispatched'
end
function R.raidSteal(c,w,raider,relic)
 if relic.state~='ground' or relic.siteId~=w.frontier.siteId or not N.reach(w,raider.x,raider.y,relic.x,relic.y,4) then return false end
 if not require('src.security').perceives(c,w,raider,{x=relic.x,y=relic.y,alive=true},nil) then return false end
 move(relic,'raider',{siteId=w.frontier.siteId,raiderId=raider.id,factionId=raider.factionId});return true
end
function R.dropRaider(c,w,raider)
 if not enabled(c) then return end
 for _,relic in ipairs(c.relics.items) do if relic.state=='raider' and relic.raiderId==raider.id then R.drop(c,relic,w.frontier.siteId,raider.x,raider.y) end end
end
function R.retreatRaider(c,raider)
 if not enabled(c) then return end
 for _,relic in ipairs(c.relics.items) do if relic.state=='raider' and relic.raiderId==raider.id then move(relic,'faction',{factionId=raider.factionId}) end end
end
function R.step(c)
 if not enabled(c) then return end
 for i=#c.relics.transfers,1,-1 do local t=c.relics.transfers[i]
  if c.tick>=t.arrivalTick then local x=item(c,t.relicId);if x and x.state=='courier' and x.transferId==t.id then move(x,'faction',{factionId=t.factionId}) end;table.remove(c.relics.transfers,i) end
 end
end
function R.validate(c)
 local state=c.relics;exact(state,{version=true,nextTransferId=true,items=true,caches=true,transfers=true,drive=true},'Campaign relic state')
 assert(state.version==R.version,'Unsupported relic state version');U.integer(state.nextTransferId,'Next relic transfer ID',1,100000000)
 assert(dense(state.items,'Relics',R.maxRelics)==R.maxRelics and dense(state.caches,'Ancient caches',R.maxCaches)==R.maxCaches,'Wrong bounded relic generation')
 local seen={};for _,x in ipairs(state.items) do
  allowed(x,{id=true,descriptor=true,role=true,family=true,originCacheId=true,state=true,cacheId=true,siteId=true,x=true,y=true,personId=true,structureId=true,craftId=true,socket=true,factionId=true,raiderId=true,transferId=true},'Relic')
  U.integer(x.id,'Relic ID',1,R.maxRelics);assert(not seen[x.id],'Duplicate relic ID');seen[x.id]=true;assert(type(x.descriptor)=='string' and (x.role=='core' or x.role=='lens' or x.role=='anchor') and type(x.family)=='string','Malformed relic intrinsic state');U.integer(x.originCacheId,'Relic origin cache',1,R.maxCaches)
  assert(x.state=='cache' or x.state=='ground' or x.state=='person' or x.state=='craft' or x.state=='analyzer' or x.state=='drive' or x.state=='depot' or x.state=='courier' or x.state=='faction' or x.state=='raider','Unknown relic custody')
  if x.state=='cache' then U.integer(x.cacheId,'Relic cache custody',1,R.maxCaches)
  elseif x.state=='ground' then local record=site(c,x.siteId);assert(record and record.world,'Relic ground site is not instantiated');U.integer(x.siteId,'Relic ground site',1,11);U.integer(x.x,'Relic ground x',1,record.world.width);U.integer(x.y,'Relic ground y',1,record.world.height)
  elseif x.state=='person' then U.integer(x.personId,'Relic carrier',1,100000000);assert(personExists(c,x.personId),'Relic carrier is missing')
  elseif x.state=='craft' then U.integer(x.craftId,'Relic shuttle cargo craft',1,100000000);assert(craft(c,x.craftId),'Relic shuttle cargo craft is missing')
  elseif x.state=='analyzer' or x.state=='depot' then U.integer(x.siteId,'Relic local structure site',1,11);U.integer(x.structureId,'Relic local structure',1,100000000)
  elseif x.state=='drive' then U.integer(x.craftId,'Relic drive craft',1,100000000);U.integer(x.socket,'Relic drive socket',1,3)
  elseif x.state=='courier' then U.integer(x.transferId,'Relic courier transfer',1,state.nextTransferId-1);U.integer(x.factionId,'Relic courier faction',2,5)
  elseif x.state=='faction' then U.integer(x.factionId,'Relic faction custody',2,5)
  else U.integer(x.siteId,'Relic raider site',1,11);U.integer(x.raiderId,'Relic raider',1,100000000);U.integer(x.factionId,'Relic raider faction',2,5) end
 end
 local cacheIds={};for _,q in ipairs(state.caches) do allowed(q,{id=true,siteId=true,relicIds=true,placed=true,empty=true,progress=true,workerId=true,gx=true,gy=true},'Ancient cache');for _,k in ipairs({'id','siteId','relicIds','placed','empty','progress','workerId'}) do assert(q[k]~=nil,'Missing Ancient cache key '..k) end;U.integer(q.id,'Ancient cache ID',1,R.maxCaches);assert(not cacheIds[q.id],'Duplicate Ancient cache ID');cacheIds[q.id]=true;U.integer(q.siteId,'Ancient cache site',4,10);dense(q.relicIds,'Ancient cache relic IDs',2);assert(type(q.placed)=='boolean' and type(q.empty)=='boolean','Malformed ancient cache state');U.integer(q.progress,'Ancient cache progress',0,180);U.integer(q.workerId,'Ancient cache worker',0,100000000);if q.placed then U.integer(q.gx,'Ancient cache x',1,512);U.integer(q.gy,'Ancient cache y',1,256) else assert(q.gx==nil and q.gy==nil,'Unplaced cache has coordinates') end
  local members={};for _,id in ipairs(q.relicIds) do assert(not members[id],'Duplicate cache relic');members[id]=true;local x=item(c,id);assert(x and x.originCacheId==q.id,'Cache origin mismatch');if not q.empty then assert(x.state=='cache' and x.cacheId==q.id,'Cache relic custody mismatch') end end
 end
 for _,t in ipairs(state.transfers) do exact(t,{id=true,relicId=true,factionId=true,siteId=true,arrivalTick=true},'Relic courier');U.integer(t.id,'Relic courier ID',1,state.nextTransferId-1);U.integer(t.relicId,'Relic courier relic ID',1,R.maxRelics);U.integer(t.factionId,'Relic courier faction ID',1,4);U.integer(t.siteId,'Relic courier site',1,11);U.integer(t.arrivalTick,'Relic courier arrival',0,10000000) end
 exact(state.drive,{version=true,installed=true,progress=true,siteId=true,craftId=true,cooldownUntil=true,sockets=true,workerId=true},'Relic drive')
 assert(state.drive.version==1 and type(state.drive.installed)=='boolean','Malformed Relic Drive');U.integer(state.drive.progress,'Relic drive progress',0,300);U.integer(state.drive.siteId,'Relic drive site',0,11);U.integer(state.drive.craftId,'Relic drive craft',0,100000000);U.integer(state.drive.cooldownUntil,'Relic drive cooldown',0,10000000);dense(state.drive.sockets,'Relic drive sockets',3)
 if state.drive.installed or state.drive.craftId>0 then
  assert(state.drive.craftId>0 and craft(c,state.drive.craftId),'Relic drive shuttle reference is missing')
  assert(state.drive.siteId>0 and site(c,state.drive.siteId),'Relic drive installation site is missing')
 end
 if not state.drive.installed then
  for _,id in ipairs(state.drive.sockets) do assert(id==0,'Unfitted Relic Drive has an occupied socket') end
 end
 local claimed={};for _,id in ipairs(state.drive.sockets) do U.integer(id,'Drive relic ID',0,R.maxRelics);if id>0 then assert(not claimed[id],'Duplicate drive relic socket');claimed[id]=true;local x=item(c,id);assert(x and x.state=='drive' and x.craftId==state.drive.craftId,'Drive socket custody mismatch') end end
 -- Cross-check every active local structured custody.  This is intentionally
 -- derived from the single relic ledger, so malformed saves cannot represent
 -- the same object in a cache, a depot, and a socket simultaneously.
 for _,x in ipairs(state.items) do if x.state=='analyzer' or x.state=='depot' then
  local record=site(c,x.siteId);local st=record and record.world and require('src.industry').find(record.world,x.structureId)
  assert(st and ((x.state=='analyzer' and st.kind=='relic_analyzer') or (x.state=='depot' and st.kind=='trade_depot')),'Relic local custody structure is missing')
  if x.state=='analyzer' then local count=0;for _,id in ipairs(st.relic.slots or {}) do if id==x.id then count=count+1 end end;assert(count==1,'Analyzer relic slot custody mismatch') end
 end end
 return true
end
return R
