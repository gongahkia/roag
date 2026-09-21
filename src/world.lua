local U=require('src.util')
local M=require('src.materials')
local C=require('config')
local W={}
function W.new(width,height,seed,preset,mode)
 U.integer(width,'width',48,512); U.integer(height,'height',32,256)
 assert(width%4==0 and height%4==0,'Dimensions must be multiples of 4')
 U.integer(seed,'seed',0,2147483646)
 assert(mode=='challenge' or mode=='practice','Invalid mode')
 return {version=C.stateVersion, width=width,height=height,n=width*height,block=4,
  cols=width/4,rows=height/4,seed=seed,preset=preset,mode=mode,tick=0,
  mat=U.array(width*height),stamp=U.array(width*height,-1),
  structures={},jobs={},workers={},items={},events={},nextId=1,eventId=0,
  ledger={mined=0,built=0,foodEaten=0,foodGrown=0,waterUsed=0,waterMade=0,
          rockCooled=0,demolitionWaste=0,mineralMade=0},
  extinct=false,navRevision=0,stats={moves=0,reactions=0,plans=0,jobsDone=0},
  rules={hungerRate=C.hungerRate,fatigueRate=C.fatigueRate,cropTicks=C.cropTicks,
         cropYield=C.cropYield,moveEvery=C.workerMoveEvery,planEvery=C.workerPlanEvery,
         irrigationCapacity=C.irrigationCapacity},
 }
end
function W.id(w) local id=w.nextId w.nextId=id+1 return id end
function W.inside(w,x,y) return x>=1 and x<=w.width and y>=1 and y<=w.height end
function W.index(w,x,y) return (y-1)*w.width+x end
function W.xy(w,i) return (i-1)%w.width+1,math.floor((i-1)/w.width)+1 end
function W.get(w,x,y) if not W.inside(w,x,y) then return M.BEDROCK end return w.mat[W.index(w,x,y)] end
function W.tile(w,x,y) return math.floor((x-1)/4)+1,math.floor((y-1)/4)+1 end
function W.slot(w,gx,gy) return (gy-1)*w.cols+gx end
function W.rect(gx,gy) return (gx-1)*4+1,(gy-1)*4+1,gx*4,gy*4 end
function W.structureAt(w,x,y)
 if not W.inside(w,x,y) then return nil end
 local gx,gy=W.tile(w,x,y) return w.structures[W.slot(w,gx,gy)]
end
function W.blocked(w,x,y)
 if not W.inside(w,x,y) then return true end
 local s=W.structureAt(w,x,y)
 return s and (s.kind=='wall' or (s.kind=='platform' and y==s.gy*4)) or false
end
function W.solid(w,x,y) return W.blocked(w,x,y) or M.def[W.get(w,x,y)].solid or false end
function W.put(w,x,y,m)
 if not W.inside(w,x,y) then return false end
 local i=W.index(w,x,y)
 if w.mat[i]~=m then w.mat[i]=m w.navRevision=w.navRevision+1 end
 return true
end
function W.event(w,kind,text,subject)
 w.eventId=w.eventId+1
 w.events[#w.events+1]={id=w.eventId,tick=w.tick,kind=kind,text=text,subject=subject}
 if #w.events>240 then table.remove(w.events,1) end
end
function W.find(t,id) for _,v in ipairs(t) do if v.id==id then return v end end end
function W.stack(w,kind,n,x,y)
 if n<=0 then return end
 -- Reservations are per stack; do not merge into an in-use stack.
 for _,p in ipairs(w.items) do
  if p.kind==kind and p.x==x and p.y==y and not p.reserved and p.n>0 then p.n=p.n+n return p end
 end
 local p={id=W.id(w),kind=kind,n=n,x=x,y=y} w.items[#w.items+1]=p return p
end
function W.totalResource(w,kind)
 local n=0
 for _,p in ipairs(w.items) do if p.kind==kind then n=n+p.n end end
 for _,a in ipairs(w.workers) do if a.carry and a.carry.kind==kind then n=n+a.carry.n end end
 return n
end
function W.alive(w) local n=0 for _,a in ipairs(w.workers) do if a.alive then n=n+1 end end return n end
function W.supportedStructure(w,s)
 if s.kind=='ladder' or s.kind=='wall' or s.kind=='platform' then return true end
 local x1,_,x2,y2=W.rect(s.gx,s.gy)
 for x=x1,x2 do if not W.solid(w,x,y2+1) then return false end end
 return true
end
function W.validate(w)
 assert(type(w)=='table' and w.version==C.stateVersion,'Incompatible world version')
 U.integer(w.width,'width',48,512); U.integer(w.height,'height',32,256)
 assert(w.width%4==0 and w.height%4==0 and w.n==w.width*w.height,'Bad dimensions')
 assert(w.cols==w.width/4 and w.rows==w.height/4 and w.block==4,'Bad building grid')
 U.integer(w.tick,'tick',0,10000000); U.integer(w.seed,'seed',0,2147483646)
 assert(w.mode=='practice' or w.mode=='challenge','Bad mode')
 assert(#w.mat==w.n and #w.stamp==w.n,'Bad cell arrays')
 for i=1,w.n do assert(M.def[w.mat[i]],'Unknown material') end
 assert(type(w.workers)=='table' and type(w.jobs)=='table' and type(w.items)=='table'
  and type(w.structures)=='table' and type(w.events)=='table' and type(w.ledger)=='table','Missing state tables')
 assert(#w.workers<=32 and #w.jobs<=1024 and #w.items<=10000,'Entity limit')
 assert(type(w.rules)=='table','Missing simulation rules')
 if w.body~=nil then assert(w.body==1,'Unsupported settler body profile') end
 for _,key in ipairs({'hungerRate','fatigueRate'}) do assert(U.finite(w.rules[key]) and w.rules[key]>=0 and w.rules[key]<=1,'Invalid need rate') end
 for _,key in ipairs({'cropTicks','cropYield','moveEvery','planEvery','irrigationCapacity'}) do U.integer(w.rules[key],key,1,100000) end
 U.integer(w.nextId,'next id',1,100000000)
 for slot,s in pairs(w.structures) do
  U.integer(slot,'structure slot',1,w.cols*w.rows)
  assert(type(s)=='table' and require('src.structures').def[s.kind],'Unknown structure')
  U.integer(s.gx,'structure x',1,w.cols);U.integer(s.gy,'structure y',1,w.rows)
  assert(slot==W.slot(w,s.gx,s.gy),'Misplaced structure')
 end
 for _,p in ipairs(w.items) do U.integer(p.n,'stack',0,1000000) end
 if w.frontier then
  assert(type(w.frontier)=='table' and w.frontier.version==1,'Unsupported campaign world marker')
  for key in pairs(w.frontier) do assert(key=='version' or key=='siteId' or key=='knowledge' or key=='education' or key=='body' or key=='visibility' or key=='equipment' or key=='safe_excavation','Unknown campaign world marker key') end
  U.integer(w.frontier.siteId,'campaign site ID',1,100000000)
  if w.frontier.knowledge~=nil then assert(w.frontier.knowledge==1,'Unsupported campaign world knowledge marker') end
  if w.frontier.education~=nil then assert(w.frontier.education==1 and w.frontier.knowledge==1,'Education requires campaign knowledge') end
  if w.frontier.body~=nil then assert(w.frontier.body==1 and w.body==1,'Campaign body marker mismatch') end
  if w.frontier.visibility~=nil then assert(w.frontier.visibility==1 and w.frontier.body==1,'Visibility requires campaign body') end
  if w.frontier.equipment~=nil then assert(w.frontier.equipment==1 and w.frontier.visibility==1 and w.frontier.body==1,'Equipment requires current body and visibility') end
  if w.frontier.safe_excavation~=nil then assert(w.frontier.safe_excavation==1 and w.frontier.equipment==1,'Safe excavation requires equipment') end
 end
 for _,job in ipairs(w.jobs) do if job.logistics then assert(w.frontier,'Cargo jobs require a campaign world marker') end end
 for _,a in ipairs(w.workers) do
  U.integer(a.x,'worker x',1,w.width); U.integer(a.y,'worker y',1,w.height)
  for _,key in ipairs({'hp','hunger','fatigue','breath'}) do assert(U.finite(a[key]) and a[key]>=0 and a[key]<=100,'Invalid worker '..key) end
  assert(type(a.alive)=='boolean' and type(a.name)=='string','Invalid worker identity')
  if w.frontier then U.integer(a.personId,'campaign person ID',1,100000000)
  else assert(a.personId==nil,'Campaign person ID requires campaign world marker') end
  if w.frontier and w.frontier.knowledge==1 then
   assert(a.frontier,'Knowledge-enabled worker lacks personal frontier state')
   require('src.knowledge').validatePersonal(a.frontier,w.tick,w.frontier.education==1)
  else assert(a.frontier==nil,'Personal frontier state requires campaign knowledge') end
  if a.directive and a.directive.kind=='assembly' then assert(w.frontier,'Assembly directives require a campaign world marker') end
  if w.frontier and w.frontier.safe_excavation==1 then
   U.integer(a.stress or 0,'Worker stress',0,100);assert(type(a.panic or false)=='boolean','Invalid worker panic state')
   if a.lastStressTick~=nil then U.integer(a.lastStressTick,'Worker stress tick',0,w.tick) end
  else assert(a.stress==nil and a.panic==nil and a.lastStressTick==nil,'Legacy worker gained stress state') end
  if a.task then
   assert(type(a.task.path)=='table','Missing task path')
   for _,index in ipairs(a.task.path) do U.integer(index,'path cell',1,w.n) end
  end
 end
 if w.frontier and w.frontier.safe_excavation==1 then
  assert(type(w.ropes)=='table','Safe-excavation world lacks rope state');U.integer(w.nextRopeId,'Next rope ID',1,100000000)
 else assert(w.ropes==nil and w.nextRopeId==nil,'Legacy world gained rope state') end
 if w.frontier and w.frontier.education==1 then require('src.education').validateWorld(w,w.tick) else assert(w.education==nil,'Education state requires campaign education') end
 require('src.visibility').validate(w)
 if w.labor then require('src.labor').validate(w,w.labor) end
 require('src.content').validate(w)
 if w.biomes then
  assert(type(w.biomes)=='table' and #w.biomes==w.n,'Bad biome grid')
  local B=require('src.biomes')
  for i=1,w.n do assert(B.def[w.biomes[i]],'Unknown biome') end
 end
 return true
end
return W
