-- COS-G06: bounded high-level security.  It joins campaign people, equipment,
-- faction capacity, current visibility, and the normal structure registry.
local U=require('src.util')
local R=require('src.campaign_random')
local W=require('src.world')
local B=require('src.body')
local V=require('src.visibility')
local N=require('src.nav')
local E=require('src.equipment')
local F=require('src.factions')
local S={version=1,maxRaids=4,maxRaiders=16,maxEvents=64,maxReceipts=128}
local hostile,targetFor
local function clamp(v,a,b) return math.max(a,math.min(b,v)) end
local function site(c,id) for _,s in ipairs(c.sites) do if s.id==id then return s end end end
local function actorId(a) return a.personId or a.id end
local function events(w,kind,text,subject)
 w.security.events[#w.security.events+1]={tick=w.tick,kind=kind,text=text,subject=subject}
 while #w.security.events>S.maxEvents do table.remove(w.security.events,1) end
 W.event(w,kind,text,subject)
end
function S.enabled(c) return c and c.features and c.features.security==1 end
function S.attach(w) w.security={version=S.version,posture='normal',posts={},refuge=nil,policyRevision=1,events={},protest=nil,cell=nil} end
function S.attachPerson(c,a)
 local r=R.new(R.derive(c.seed,'person/'..a.personId..'/security/combatXP/v1'))
 a.security={guardEnabled=false,combatXP=R.uniform(r,80)-1,grievance=0,causes={},ammo=0,lastCombatActionTick=nil,lastXP=nil,protestUntil=nil,insurgentCellId=nil,allegiance='society',equipmentIntent=nil,reloadIntent=nil}
end
function S.initialise(c)
 c.security=c.security or {version=S.version,nextRaidId=1,nextActorId=1,nextCellId=1,raids={},lootReceipts={}}
 for _,s in ipairs(c.sites) do if not s.world.security then S.attach(s.world) end;for _,a in ipairs(s.world.workers) do if not a.security then S.attachPerson(c,a) end end end
end
function S.configure(w,p,expected)
 assert(w.security,'Security is unavailable');if expected~=nil then assert(expected==w.security.policyRevision,'Stale security policy') end
 assert(p.posture=='normal' or p.posture=='alert' or p.posture=='lockdown','Invalid security posture');w.security.posture=p.posture;w.security.policyRevision=w.security.policyRevision+1;return true
end
function S.setPosts(w,posts,refuge,expected,context)
 assert(w.security and (expected==nil or expected==w.security.policyRevision),'Stale security policy');assert(type(posts)=='table' and #posts<=8,'Too many security posts')
 local out={};for _,p in ipairs(posts) do U.integer(p.x,'Defense post x',1,w.width);U.integer(p.y,'Defense post y',1,w.height);assert(N.stand(w,p.x,p.y,true),'Defense post is blocked or unsafe');if context then assert(V.currentlyVisible(w,p.x,p.y,context),'Defense post is not currently visible') end;out[#out+1]={x=p.x,y=p.y} end
 if refuge then
  for _,k in ipairs({'x1','y1','x2','y2'}) do U.integer(refuge[k],'Refuge '..k,1,k:sub(1,1)=='x' and w.width or w.height) end;assert(refuge.x1<=refuge.x2 and refuge.y1<=refuge.y2,'Invalid refuge')
  if context then for y=refuge.y1,refuge.y2 do for x=refuge.x1,refuge.x2 do assert(V.currentlyVisible(w,x,y,context),'Refuge includes unexplored terrain') end end end
 end
 w.security.posts=out;w.security.refuge=refuge and {x1=refuge.x1,y1=refuge.y1,x2=refuge.x2,y2=refuge.y2} or nil;w.security.policyRevision=w.security.policyRevision+1;return true
end
function S.toggleGuard(c,siteId,personId,on)
 U.integer(siteId,'Security site ID',1,100000000);U.integer(personId,'Security person ID',1,100000000);assert(type(on)=='boolean','Guard policy must be boolean')
 local s=site(c,siteId);for _,a in ipairs(s and s.world.workers or {}) do if a.personId==personId and a.alive then a.security.guardEnabled=on==true;return true end end;return false,'Local living person is unavailable'
end
function S.equip(c,siteId,personId,itemId)
 U.integer(siteId,'Security site ID',1,100000000);U.integer(personId,'Security person ID',1,100000000);U.integer(itemId,'Security equipment ID',1,100000000)
 local s=site(c,siteId);local a;for _,p in ipairs(s and s.world.workers or {}) do if p.personId==personId then a=p end end
 local item=E.find(c,itemId);if not a or not a.alive or not item or item.state~='loose' or item.siteId~=siteId then return false,'Equipment is not locally loose' end
 -- Submission only establishes a local work intent.  Custody changes later,
 -- when this exact body reaches the item in `S.act`; commands cannot remotely
 -- equip an item from another room, site, shuttle, or worker.
 a.security.equipmentIntent=itemId
 return true
end
function S.reload(c,siteId,personId,amount)
 U.integer(siteId,'Security site ID',1,100000000);U.integer(personId,'Security person ID',1,100000000);if amount~=nil then U.integer(amount,'Reload amount',1,12) end
 local s=site(c,siteId);local a;for _,p in ipairs(s and s.world.workers or {}) do if p.personId==personId then a=p end end
 if not a or not E.equipped(c,personId,'weapon') then return false,'A local equipped weapon is required' end
 amount=math.min(amount or 12,12-a.security.ammo);if amount<=0 then return false,'Ammunition pouch is full' end
 a.security.reloadIntent=amount
 return true
end
function S.cooldown(a,kind)
 local xp=a.security and a.security.combatXP or a.combatXP or 0
 if kind=='frontier_carbine' then return math.max(12,20-math.floor(xp/50)) end
 if kind=='shock_baton' then return math.max(10,16-math.floor(xp/100)) end
 return 20
end
function S.weapon(c,a) return a.personId and E.equipped(c,a.personId,'weapon') or a.weapon end
local function vest(c,a) return a.personId and E.equipped(c,a.personId,'armor') or a.armor end
local function point(a) return a.x+1,a.y-2 end
local function bresenham(x0,y0,x1,y1,visit)
 local dx,dy=math.abs(x1-x0),math.abs(y1-y0);local sx=x0<x1 and 1 or -1;local sy=y0<y1 and 1 or -1;local err=dx-dy
 while true do if visit(x0,y0)==false or (x0==x1 and y0==y1) then return end;local e2=2*err;if e2>-dy then err=err-dy;x0=x0+sx end;if e2<dx then err=err+dx;y0=y0+sy end end
end
local function barricadeAt(w,x,y) local st=W.structureAt(w,x,y);if not st or st.kind~='barricade' then return false end;local _,_,_,bottom=W.rect(st.gx,st.gy);return y>=bottom-1 end
local function contains(w,a,x,y) if a.personId then return B.contains(w,a,x,y) end;return x>=a.x and x<=a.x+1 and y>=a.y-3 and y<=a.y end
function S.ray(w,from,to,actors)
 local x,y=point(from);local tx,ty=point(to);local hit
 bresenham(x,y,tx,ty,function(cx,cy)
  if cx==x and cy==y then return true end
  if W.solid(w,cx,cy) or barricadeAt(w,cx,cy) then hit={kind='cover',x=cx,y=cy};return false end
  for _,a in ipairs(actors) do if a.alive and contains(w,a,cx,cy) then hit={kind='body',actor=a,x=cx,y=cy};return false end end
 end);return hit
end
function S.perceives(c,w,a,b,context)
 local x,y=point(b)
 if a.personId then return V.visible(w,a,x,y,context) end
 local ax,ay=point(a);local d=math.abs(ax-x)+math.abs(ay-y)
 -- Raid lamps are only an actor capability.  They light the raider's own
 -- perception out to ten cells; outside that radius darkness permits only
 -- the three-cell close range.  Neither branch consults player fog memory.
 if d>20 or (d>10 and d>3) then return false end
 return V.fov(w,ax,ay,20)[W.index(w,x,y)]==true
end
function S.damage(c,w,target,amount,source,context)
 if vest(c,target) then amount=math.max(1,math.floor(amount*.75)) end;target.hp=target.hp-amount
 if target.personId and target.security then
  require('src.psychology').memory(c,target,'was_injured_in_combat',{source='combat-hit:'..actorId(source)..':'..w.tick,stress=10,siteId=w.frontier.siteId})
  if amount>=20 and target.task and target.task.kind=='work' then S.grieve(c,target,'ordinary_job_injury',8,'combat-hit:'..actorId(source)..':'..w.tick) end
 end
 if target.hp<=0 then target.hp=0;if target.personId then require('src.colonists').kill(w,target,'combat',context) else target.alive=false;target.dead=true end end;return amount
end
local function canAct(w,a,kind) local last=a.security and a.security.lastCombatActionTick or a.lastCombatActionTick or -999999;return w.tick-last>=S.cooldown(a,kind) end
local function markAct(a,tick) if a.security then a.security.lastCombatActionTick=tick else a.lastCombatActionTick=tick end end
function S.attack(c,w,from,to,actors,context)
 if not from.alive or not to.alive then return false,'Actor unavailable' end;local weapon=S.weapon(c,from);local kind=weapon and weapon.kind or 'unarmed';if not canAct(w,from,kind) then return false,'Cooldown' end
 local fx,fy=point(from);local tx,ty=point(to);local distance=math.abs(fx-tx)+math.abs(fy-ty)
 if kind=='frontier_carbine' then
  if distance>28 then return false,'Out of range' end;if (from.security and from.security.ammo or from.ammo or 0)<1 then return false,'No ammunition' end
  local hit=S.ray(w,from,to,actors);if hit and hit.kind=='body' and hit.actor~=to and hit.actor.allegiance==from.allegiance then return false,'Friendly blocks line of fire' end
  if from.security then from.security.ammo=from.security.ammo-1 else from.ammo=from.ammo-1 end;markAct(from,w.tick)
  if hit and hit.kind=='body' then
   S.damage(c,w,hit.actor,24,from,context)
   if from.security and hostile(from,hit.actor) and from.security.lastXP~=w.tick then from.security.combatXP=math.min(400,from.security.combatXP+1);from.security.lastXP=w.tick end
   return true,hit.actor
  end;return true,nil
 end
 if distance>4 then return false,'Not in contact range' end;markAct(from,w.tick);S.damage(c,w,to,kind=='shock_baton' and 12 or 6,from,context)
 if from.security and hostile(from,to) and from.security.lastXP~=w.tick then from.security.combatXP=math.min(400,from.security.combatXP+1);from.security.lastXP=w.tick end
 return true,to
end
function S.train(c,w,a)
 if not a.security or not a.security.guardEnabled or a.panic then return false end
 for _,st in pairs(w.structures) do if st.kind=='training_target' and (not st.sabotagedUntil or st.sabotagedUntil<w.tick) and math.abs(st.gx*4-2-a.x)+math.abs(st.gy*4-2-a.y)<=8 then if a.security.lastXP~=w.tick then a.security.combatXP=math.min(400,a.security.combatXP+1);a.security.lastXP=w.tick end;return true end end;return false
end

local function localSite(c,w)
 for _,s in ipairs(c.sites) do if s.world==w then return s end end
end
local function allRaiders(c,siteId)
 local out={}
 for _,r in ipairs(c.security.raids or {}) do
  if r.siteId==siteId and (r.status=='active' or r.status=='withdrawing') then
   for _,a in ipairs(r.actors or {}) do if a.alive then out[#out+1]=a end end
  end
 end
 table.sort(out,function(a,b) return a.id<b.id end)
 return out
end
function S.playerContacts(c,w)
 local s=localSite(c,w);local out={}
 for _,a in ipairs(allRaiders(c,s.id)) do if V.currentlyVisible(w,a.x,a.y,{campaign=c,siteId=s.id}) then out[#out+1]={id=a.id,kind='external hostile'} end end
 for _,a in ipairs(w.workers) do if a.alive and a.security and a.security.allegiance=='insurgent' and V.currentlyVisible(w,a.x,a.y,{campaign=c,siteId=s.id}) then out[#out+1]={id=a.personId,kind='hostile insurgent'} end end
 table.sort(out,function(a,b)return a.id<b.id end);return out
end
function S.visibleRaiders(c,w)
 local s=localSite(c,w);local out={}
 for _,a in ipairs(allRaiders(c,s.id)) do
  if V.currentlyVisible(w,a.x,a.y,{campaign=c,siteId=s.id}) then out[#out+1]=a end
 end
 return out
end
-- Reconcile posture before the normal local worker update.  Only ordinary
-- work is released; eating, rest, panic/escape, and explicit rally/assembly
-- retain their established higher precedence and will be reconsidered by the
-- same existing planner rather than receiving a second actor update.
function S.preStep(c,record)
 if not S.enabled(c) or not record or not record.world then return end
 local w=record.world;local context={campaign=c,siteId=record.id};local J=require('src.jobs')
 for _,a in ipairs(w.workers) do if a.alive and a.security and a.task then
  local ordinary=a.task.kind~='eat' and a.task.kind~='rest' and a.task.kind~='escape' and a.task.kind~='panic_escape' and a.task.kind~='rally' and a.task.kind~='security'
  if ordinary then
   local threat=targetFor(c,w,a)
   local respond=(a.security.guardEnabled and a.security.allegiance=='society' and (w.security.posture~='normal' or threat))
    or (a.security.allegiance=='society' and not a.security.guardEnabled and (w.security.posture=='lockdown' or (threat and distance(a,threat)<=12)))
   if respond then J.release(w,a,true);a.thinkAt=w.tick end
  end
 end end
end
local function localActors(c,w)
 local s=localSite(c,w);local out={}
 for _,a in ipairs(w.workers) do if a.alive then out[#out+1]=a end end
 for _,a in ipairs(allRaiders(c,s.id)) do out[#out+1]=a end
 table.sort(out,function(a,b)
  local aa,bb=actorId(a),actorId(b)
  return aa<bb
 end)
 return out
end
hostile=function(a,b)
 local aa=a.security and a.security.allegiance or a.allegiance or 'society'
 local bb=b.security and b.security.allegiance or b.allegiance or 'society'
 return aa~=bb and ((aa=='society' and (bb=='raider' or bb=='insurgent')) or (bb=='society' and (aa=='raider' or aa=='insurgent')) or (aa=='raider' and bb=='insurgent') or (aa=='insurgent' and bb=='raider'))
end
local function distance(a,b)
 local ax,ay=point(a);local bx,by=point(b)
 return math.abs(ax-bx)+math.abs(ay-by)
end
targetFor=function(c,w,a)
 local candidates={}
 for _,b in ipairs(localActors(c,w)) do
  if b~=a and hostile(a,b) and S.perceives(c,w,a,b,{campaign=c,siteId=localSite(c,w).id}) then
   local priority=3
   local side=b.security and b.security.allegiance or b.allegiance
   if side=='society' and a.allegiance=='raider' then priority=1 end
   if side=='society' and a.security and a.security.allegiance=='insurgent' and b.security and b.security.guardEnabled then priority=0 end
   if a.security and a.security.guardEnabled and side~='society' then
    for _,civilian in ipairs(w.workers) do
     if civilian.alive and civilian.security.allegiance=='society' and not civilian.security.guardEnabled and distance(b,civilian)<=12 then priority=-1;break end
    end
   end
   candidates[#candidates+1]={actor=b,priority=priority,distance=distance(a,b),id=actorId(b)}
  end
 end
 table.sort(candidates,function(x,y) return x.priority~=y.priority and x.priority<y.priority or x.distance~=y.distance and x.distance<y.distance or x.id<y.id end)
 return candidates[1] and candidates[1].actor
end
local function walkTask(w,a,f,closest,predicate,label,mode,extra)
 local path,node=closest(w,a,f,predicate)
 if not path then return nil end
 local task={kind='security',securityMode=mode,path=path,node=node,label=label}
 for k,v in pairs(extra or {}) do task[k]=v end
 return task
end
local function atRefuge(w,a,refuge)
 return refuge and a.x>=refuge.x1 and a.x<=refuge.x2 and a.y>=refuge.y1 and a.y<=refuge.y2
end
local function meetPoint(w,members)
 -- Existing home/landing space is the stable public gathering anchor.  It is
 -- only selected if it is an ordinary valid standing pose.
 local h=w.home
 if h and N.stand(w,h.x,h.y,true) then return {x=h.x,y=h.y} end
 for _,a in ipairs(members) do if N.stand(w,a.x,a.y,true) then return {x=a.x,y=a.y} end end
end
local function cellFor(w,a)
 local cell=w.security and w.security.cell
 if cell and a.security and a.security.insurgentCellId==cell.id then return cell end
 if cell then for _,id in ipairs(cell.members or {}) do if id==a.personId then return cell end end end
end
-- Called from the normal worker planner after needs, panic, and explicit
-- rally/assembly precedence have had their say.  It returns an ordinary task;
-- no security movement teleports or mutates inventory at planning time.
function S.offer(c,w,a,f,closest)
 if not S.enabled(c) or not a.security or not a.alive then return nil end
 local sid=localSite(c,w).id;local policy=w.security
 local function reachItem(itemId,mode,amount)
  local item=E.find(c,itemId)
  if not item or item.state~='loose' or item.siteId~=sid then return nil end
  return walkTask(w,a,f,closest,function(x,y) return N.reach(w,x,y,item.x,item.y,4) end,
   mode=='equip' and 'Fetching security equipment' or 'Fetching ammunition',''..mode,{equipmentId=itemId,amount=amount})
 end
 if a.security.equipmentIntent then
  local t=reachItem(a.security.equipmentIntent,'equip');if t then return t end
  a.security.equipmentIntent=nil
 end
 if a.security.reloadIntent and a.security.ammo<12 and E.equipped(c,a.personId,'weapon') then
  local choice
  for _,pile in ipairs(w.items) do if pile.kind=='ammunition' and pile.n>0 and not pile.reserved then
   local p,n,d=closest(w,a,f,function(x,y) return N.reach(w,x,y,pile.x,pile.y,4) end)
   if p and (not choice or d<choice.distance or (d==choice.distance and pile.id<choice.pile.id)) then choice={pile=pile,path=p,node=n,distance=d} end
  end end
  if choice then return {kind='security',securityMode='reload',pileId=choice.pile.id,amount=a.security.reloadIntent,path=choice.path,node=choice.node,label='Fetching ammunition'} end
  a.security.reloadIntent=nil
 end
 if policy.protest then
  for _,id in ipairs(policy.protest.members or {}) do if id==a.personId then
   local members={};for _,pid in ipairs(policy.protest.members) do for _,p in ipairs(w.workers) do if p.personId==pid and p.alive then members[#members+1]=p end end end
   local meet=policy.protest.meeting or meetPoint(w,members);policy.protest.meeting=meet
   if meet then return walkTask(w,a,f,closest,function(x,y) return math.abs(x-meet.x)+math.abs(y-meet.y)<=3 end,'Joining protest','protest') end
  end end
 end
 local cell=cellFor(w,a)
 if cell and cell.state=='sabotage' and cell.saboteurId==a.personId then
  local target=require('src.industry').find(w,cell.targetId)
  if target then return walkTask(w,a,f,closest,function(x,y) return N.reachRect(w,x,y,target.gx,target.gy) end,'Sabotaging '..target.kind,'sabotage',{cellId=cell.id,targetId=target.id}) end
 end
 if cell and cell.state=='organizing' and c.tick%100<20 then
  local members={};for _,id in ipairs(cell.members) do for _,p in ipairs(w.workers) do if p.personId==id and p.alive then members[#members+1]=p end end end
  local meet=cell.meeting or meetPoint(w,members);cell.meeting=meet
  if meet then return walkTask(w,a,f,closest,function(x,y) return math.abs(x-meet.x)+math.abs(y-meet.y)<=3 end,'Attending private meeting','meeting',{cellId=cell.id}) end
 end
 local target=targetFor(c,w,a)
 if target then
  local weapon=S.weapon(c,a);local kind=weapon and weapon.kind or 'unarmed'
  if a.security.allegiance=='society' and not a.security.guardEnabled then
   if distance(a,target)<=12 then
    -- Find a reachable pose that increases distance; this is deliberate
    -- self-preservation, never a civilian hunt order.
    local best,bestD
    for _,i in ipairs(f.queue) do local x,y=W.xy(w,i);local d=math.abs(x-target.x)+math.abs(y-target.y)
     if (not best or d>bestD) and N.stand(w,x,y,true) then best,bestD={x=x,y=y},d end
    end
    if policy.posture=='lockdown' and policy.refuge then
     local t=walkTask(w,a,f,closest,function(x,y) return x>=policy.refuge.x1 and x<=policy.refuge.x2 and y>=policy.refuge.y1 and y<=policy.refuge.y2 end,'Moving to civilian refuge','refuge')
     if t then return t end
    end
    if best then return walkTask(w,a,f,closest,function(x,y) return x==best.x and y==best.y end,'Avoiding hostile contact','flee') end
   end
  else
   if kind=='frontier_carbine' and a.security.ammo==0 then a.security.reloadIntent=12
   else
    local desired=kind=='frontier_carbine' and 28 or 4
    if distance(a,target)<=desired then return {kind='security',securityMode='combat',targetId=actorId(target),targetPerson=target.personId and true or false,path={},node=W.index(w,a.x,a.y),label='Engaging hostile'} end
    return walkTask(w,a,f,closest,function(x,y) return math.abs(x-target.x)+math.abs(y-target.y)<=desired end,'Moving to hostile contact','combat_move',{targetId=actorId(target),targetPerson=target.personId and true or false})
   end
  end
 end
 if a.security.allegiance=='society' and a.security.guardEnabled then
  if not S.weapon(c,a) then
   local weapons=E.forSite(c,sid)
   for _,item in ipairs(weapons) do if item.state=='loose' and (item.kind=='frontier_carbine' or item.kind=='shock_baton') then a.security.equipmentIntent=item.id;return S.offer(c,w,a,f,closest) end end
  end
  if policy.posture~='normal' and #policy.posts>0 then
   local guards={};for _,p in ipairs(w.workers) do if p.alive and p.security and p.security.guardEnabled and p.security.allegiance=='society' then guards[#guards+1]=p end end
   table.sort(guards,function(x,y)return x.personId<y.personId end);local index=1;for i,p in ipairs(guards) do if p==a then index=i end end
   local post=policy.posts[(index-1)%#policy.posts+1]
   return walkTask(w,a,f,closest,function(x,y) return x==post.x and y==post.y end,'Taking defense post','post')
  end
 end
 if a.security.allegiance=='society' and not a.security.guardEnabled and policy.posture=='lockdown' and policy.refuge and not atRefuge(w,a,policy.refuge) then
  return walkTask(w,a,f,closest,function(x,y) return x>=policy.refuge.x1 and x<=policy.refuge.x2 and y>=policy.refuge.y1 and y<=policy.refuge.y2 end,'Moving to civilian refuge','refuge')
 end
 return nil
end

local function findActor(c,w,id,person)
 if person then for _,a in ipairs(w.workers) do if a.personId==id then return a end end
 else for _,a in ipairs(allRaiders(c,localSite(c,w).id)) do if a.id==id then return a end end end
end
local function completeSabotage(c,w,cell,target)
 target.sabotagedUntil=c.tick+600;cell.state='hostile';cell.completedTick=c.tick
 for _,id in ipairs(cell.members) do for _,a in ipairs(w.workers) do if a.personId==id and a.alive then
  a.security.allegiance='insurgent';a.security.insurgentCellId=cell.id;a.directive=nil
  require('src.jobs').release(w,a,true)
  require('src.psychology').memory(c,a,'insurgency_began',{source='cell:'..cell.id,siteId=localSite(c,w).id})
 end end end
 events(w,'insurgent_sabotage','Internal sabotage disabled '..target.kind..'.',target.id)
end
function S.act(c,w,a,t,context)
 if t.securityMode=='equip' then
  local item=E.find(c,t.equipmentId)
  if not item or item.state~='loose' or item.siteId~=context.siteId or not N.reach(w,a.x,a.y,item.x,item.y,4) then return false,nil,'Equipment moved or is unreachable' end
  local ok,why=pcall(E.equip,c,item,a);if not ok then return false,nil,tostring(why) end
  a.security.equipmentIntent=nil;return true,true,'Equipped '..item.kind
 elseif t.securityMode=='reload' then
  local pile=W.find(w.items,t.pileId);local want=math.min(t.amount or 12,12-a.security.ammo)
  if not pile or pile.kind~='ammunition' or pile.n<1 or not N.reach(w,a.x,a.y,pile.x,pile.y,4) then return false,nil,'Ammunition moved or is unreachable' end
  local n=math.min(want,pile.n);pile.n=pile.n-n;a.security.ammo=a.security.ammo+n;a.security.reloadIntent=nil
  return true,true,'Reloaded '..n..' rounds'
 elseif t.securityMode=='combat' then
  local target=findActor(c,w,t.targetId,t.targetPerson)
  if not target or not target.alive or not hostile(a,target) then return true,true,'Hostile contact changed' end
  if not S.perceives(c,w,a,target,context) then return true,true,'Hostile is no longer perceived' end
  local ok,hit=S.attack(c,w,a,target,localActors(c,w),context)
  return ok,true,ok and (hit and 'Combat hit' or 'Combat shot') or 'Combat action unavailable'
 elseif t.securityMode=='sabotage' then
  local cell=w.security.cell;local target=cell and cell.id==t.cellId and require('src.industry').find(w,t.targetId)
  if not target or cell.state~='sabotage' or cell.saboteurId~=a.personId or not N.reachRect(w,a.x,a.y,target.gx,target.gy) then return false,nil,'Sabotage target changed or is unreachable' end
  cell.progress=cell.progress+1
  if cell.progress>=120 then completeSabotage(c,w,cell,target);return true,true,'Sabotage completed' end
  return true,false,'Sabotage work '..cell.progress..'/120'
 end
 return true,true,t.securityMode=='post' and 'Holding defense post' or t.securityMode=='refuge' and 'Reached civilian refuge' or 'Security movement complete'
end
function S.grieve(c,a,kind,amount,source)
 if not a.security then return end
 local x=a.security;amount=amount or 0
 for _,cause in ipairs(x.causes) do
  if cause.kind==kind then
   if source and cause.source==source then return false end
   cause.amount=clamp(cause.amount+amount,-100,100);cause.tick=c.tick;cause.source=source or cause.source
   x.grievance=clamp(x.grievance+amount,0,100);return true
  end
 end
 if amount~=0 then
  x.causes[#x.causes+1]={kind=kind,amount=amount,tick=c.tick,source=source}
  while #x.causes>8 do table.remove(x.causes,1) end
  x.grievance=clamp(x.grievance+amount,0,100);return true
 end
 return false
end
local function pressure(f) return 50+math.floor(f.culture.expansion/2)+math.floor(f.culture.hierarchy/4)+math.min(40,f.relations[1].grievance)+((f.strategicGoal=='expand_influence' or f.strategicGoal=='expand_industry') and 20 or 0)-math.min(30,f.recentLossPenalty or 0) end
local function targetSite(c)
 local best;for _,s in ipairs(c.sites) do if s.ownerSocietyId==c.society.id then local score=W.alive(s.world)*10;for _,st in pairs(s.world.structures) do if st.kind=='signal_relay' or st.kind=='trade_depot' or st.kind=='fabricator' or st.kind=='battery' or st.kind=='solar_array' then score=score+20 end end;if not best or score>best.score or (score==best.score and s.id<best.s.id) then best={s=s,score=score} end end end;return best and best.s
end
local function schedule(c)
 if c.tick%1000~=0 then return end
 local activeCount=0;for _,r in ipairs(c.security.raids) do if r.status~='resolved' then activeCount=activeCount+1 end end
 for _,f in ipairs(c.factions.factions) do local active=false;for _,r in ipairs(c.security.raids) do if r.factionId==f.id and r.status~='resolved' then active=true end end;local last=f.lastRaidResolvedTick or -4000
  if not active and c.tick-last>=4000 and activeCount<S.maxRaids and F.attitude(f.relations[1])=='HOSTILE' and (f.stocks.food or 0)>=2 and (f.stocks.metal or 0)>=2 and pressure(f)>=100 then
   local s=targetSite(c);local size=clamp(1+math.floor(f.culture.expansion/34),1,4)
   if s and f.stocks.food>=size and f.stocks.metal>=size then
    f.stocks.food=f.stocks.food-size;f.stocks.metal=f.stocks.metal-size
    local id=c.security.nextRaidId;c.security.nextRaidId=id+1
    c.security.raids[#c.security.raids+1]={id=id,factionId=f.id,siteId=s.id,size=size,initialSize=size,committedTick=c.tick,arrivalTick=c.tick+800,status='approaching',goal=(f.culture.expansion>=f.culture.hierarchy and 'sabotage' or 'assault'),actors={},lastContactTick=nil,withdrawTick=nil}
    activeCount=activeCount+1
   end
  end
 end
end
local function spawn(c,r)
 local s=site(c,r.siteId);if not s then return end;local w=s.world;local anchor=w.home or {x=math.floor(w.width/2),y=math.floor(w.height/2)};local f=F.find(c,r.factionId);local poses={}
 for d=1,12 do for y=anchor.y-d,anchor.y+d do for x=anchor.x-d,anchor.x+d do
  if math.abs(x-anchor.x)+math.abs(y-anchor.y)==d and N.stand(w,x,y,true) then
   local blocked=false;for _,a in ipairs(w.workers) do if a.alive and math.abs(a.x-x)<2 and math.abs(a.y-y)<4 then blocked=true end end
   if not blocked then poses[#poses+1]={x=x,y=y} end
  end
 end end end
 if #poses<r.size then r.status='holding';return end
 r.ingress={x=anchor.x,y=anchor.y}
 for i=1,r.size do local id=c.security.nextActorId;c.security.nextActorId=id+1;local weapon=((f.culture.expansion+i)%2==0) and 'frontier_carbine' or 'shock_baton';r.actors[#r.actors+1]={id=id,alive=true,allegiance='raider',factionId=f.id,x=poses[i].x,y=poses[i].y,hp=100,combatXP=clamp(80+math.floor((f.culture.hierarchy+f.culture.expansion)/2),80,240),weapon={kind=weapon},ammo=weapon=='frontier_carbine' and 6 or 0,armor=(i==1 and f.culture.hierarchy>=60) and {kind='protective_vest'} or nil} end
 r.status='active';r.lastContactTick=c.tick;events(w,'raid_arrived','Hostile expedition arrived.',r.id)
end
local function pathMove(w,a,predicate,blockers)
 local f=N.flood(w,a.x,a.y,true);local path=select(1,N.closest(w,f,predicate));local nextIndex=path and path[1]
 if not nextIndex then return false end
 local x,y=W.xy(w,nextIndex)
 for _,b in ipairs(blockers) do if b~=a and b.alive and math.abs(b.x-x)<2 and math.abs(b.y-y)<4 then return false end end
 if N.edge(w,a.x,a.y,x,y) then a.x,a.y=x,y;return true end
 return false
end
local function raidTarget(w,raider,c)
 local candidates={}
 for _,st in pairs(w.structures) do
  if (st.kind=='signal_relay' or st.kind=='trade_depot' or st.kind=='fabricator' or st.kind=='mining_rig' or st.kind=='battery' or st.kind=='solar_array') and not st.sabotagedUntil and S.perceives(c,w,raider,{x=st.gx*4-3,y=st.gy*4}, {campaign=c,siteId=localSite(c,w).id}) then candidates[#candidates+1]=st end
 end
 table.sort(candidates,function(a,b)
  local da=math.abs(a.gx*4-2-raider.x)+math.abs(a.gy*4-raider.y);local db=math.abs(b.gx*4-2-raider.x)+math.abs(b.gy*4-raider.y)
  return da~=db and da<db or a.id<b.id
 end)
 return candidates[1]
end
local function resolveRaid(c,r,w,reason)
 if r.status=='resolved' then return end
 r.status='resolved';r.resolvedTick=c.tick;r.resolution=reason
 local f=F.find(c,r.factionId);if f then f.lastRaidResolvedTick=c.tick;f.recentLossPenalty=clamp((f.recentLossPenalty or 0)+(reason=='losses' and 10 or 0),0,30) end
 events(w,'raid_resolved',reason=='withdrawal' and 'Hostile expedition withdrew.' or 'Hostile expedition was repelled.',r.id)
end
local function dropRaidLoot(c,r,w,a)
 if a.lootDropped then return end;a.lootDropped=true
 local receipt={raidId=r.id,factionId=r.factionId,tick=c.tick,actorId=a.id,itemIds={},ammo=a.ammo or 0}
 if a.weapon then receipt.itemIds[#receipt.itemIds+1]=E.create(c,localSite(c,w).id,a.weapon.kind,a.x,a.y).id end
 if a.armor then receipt.itemIds[#receipt.itemIds+1]=E.create(c,localSite(c,w).id,a.armor.kind,a.x,a.y).id end
 if a.ammo and a.ammo>0 then W.stack(w,'ammunition',a.ammo,a.x,a.y) end
 c.security.lootReceipts[#c.security.lootReceipts+1]=receipt;while #c.security.lootReceipts>S.maxReceipts do table.remove(c.security.lootReceipts,1) end
end
local function raidStep(c,r)
 local s=site(c,r.siteId);if not s then return end;local w=s.world
 if r.status=='approaching' and c.tick>=r.arrivalTick then spawn(c,r) elseif r.status=='holding' then spawn(c,r) end
 if r.status~='active' and r.status~='withdrawing' then return end
 local actors=localActors(c,w);local alive=0
 for _,a in ipairs(r.actors) do if a.alive then alive=alive+1 else dropRaidLoot(c,r,w,a) end end
 if alive==0 then resolveRaid(c,r,w,'losses');return end
 if r.status=='active' and alive<math.ceil(r.initialSize/2) then r.status='withdrawing';r.withdrawTick=c.tick end
 for _,raider in ipairs(r.actors) do if raider.alive then
  if r.status=='withdrawing' then pathMove(w,raider,function(x,y) return math.abs(x-r.ingress.x)+math.abs(y-r.ingress.y)<=2 end,actors)
  else
   local target=targetFor(c,w,raider)
   if target then
    r.lastContactTick=c.tick
    local kind=raider.weapon and raider.weapon.kind or 'unarmed';local reach=kind=='frontier_carbine' and 28 or 4
    if distance(raider,target)<=reach then S.attack(c,w,raider,target,actors,{campaign=c,siteId=s.id}) else pathMove(w,raider,function(x,y) return math.abs(x-target.x)+math.abs(y-target.y)<=reach end,actors) end
   elseif r.goal=='sabotage' then
    local target0=r.targetId and require('src.industry').find(w,r.targetId) or nil
    if not target0 or target0.sabotagedUntil then target0=raidTarget(w,raider,c);r.targetId=target0 and target0.id or nil;r.sabotageProgress=target0 and 0 or r.sabotageProgress end
    if target0 then
     if N.reachRect(w,raider.x,raider.y,target0.gx,target0.gy) then r.sabotageProgress=(r.sabotageProgress or 0)+1;if r.sabotageProgress>=120 then target0.sabotagedUntil=c.tick+600;r.status='withdrawing';r.withdrawTick=c.tick;events(w,'raid_sabotage','Hostile expedition disabled '..target0.kind..'.',target0.id) end
     else pathMove(w,raider,function(x,y) return N.reachRect(w,x,y,target0.gx,target0.gy) end,actors) end
    end
   end
  end
 end end
 if r.status=='active' and c.tick-(r.lastContactTick or c.tick)>=600 then r.status='withdrawing';r.withdrawTick=c.tick end
 if r.status=='withdrawing' then
  local escaped=true;for _,a in ipairs(r.actors) do if a.alive and math.abs(a.x-r.ingress.x)+math.abs(a.y-r.ingress.y)>2 then escaped=false end end
  if escaped then resolveRaid(c,r,w,'withdrawal') end
 end
end
local function sharedCause(a,b)
 local function strongest(x)
  local best;for _,cause in ipairs(x.security.causes) do if not best or cause.amount>best.amount or (cause.amount==best.amount and cause.kind<best.kind) then best=cause end end
  return best and best.kind
 end
 local ak,bk=strongest(a),strongest(b)
 return ak and ak==bk and ak or nil
end
local function mutualTrust(a,b)
 local P=require('src.psychology');local ab=P.relation(a,b.personId,false);local ba=P.relation(b,a.personId,false)
 return ab and ba and ab.trust>=20 and ba.trust>=20
end
local sabotageKinds={signal_relay=true,trade_depot=true,battery=true,solar_array=true,fabricator=true,mining_rig=true,training_target=true,field_school=true}
local function chooseCellTarget(w,cell)
 local out={}
 for _,st in pairs(w.structures) do if sabotageKinds[st.kind] then out[#out+1]=st end end
 table.sort(out,function(a,b)
  local score={signal_relay=8,trade_depot=7,fabricator=6,mining_rig=5,battery=4,solar_array=3,field_school=2,training_target=1}
  local aa,bb=score[a.kind] or 0,score[b.kind] or 0
  return aa~=bb and aa>bb or a.id<b.id
 end)
 return out[1]
end
local function unrest(c,s)
 local w=s.world;local p=w.security
 if p.protest and c.tick>=p.protest.untilTick then p.protest.endedTick=c.tick;p.lastProtest=p.protest;p.protest=nil end
 if p.cell and p.cell.state=='organizing' then
  local members={};for _,id in ipairs(p.cell.members) do for _,a in ipairs(w.workers) do if a.personId==id and a.alive and a.security.allegiance=='society' then members[#members+1]=a end end end
  if #members<2 then p.cell.state='cancelled';p.cell.resolvedTick=c.tick
  elseif c.tick<p.cell.readyTick then
   for _,a in ipairs(members) do if a.security.grievance<85 then p.cell.state='cancelled';p.cell.resolvedTick=c.tick;break end end
   if p.cell.state=='organizing' and c.tick%100<20 then
    local meeting=p.cell.meeting or meetPoint(w,members);p.cell.meeting=meeting
    if meeting then
     local near={};for _,a in ipairs(members) do if math.abs(a.x-meeting.x)+math.abs(a.y-meeting.y)<=6 then near[#near+1]=a end end
     if #near>=2 and not p.cell.detected then
      for _,guard in ipairs(w.workers) do if guard.alive and guard.security.guardEnabled and guard.security.allegiance=='society' and S.perceives(c,w,guard,near[1],{campaign=c,siteId=s.id}) and S.perceives(c,w,guard,near[2],{campaign=c,siteId=s.id}) then
       p.cell.detected=true;p.cell.detectedTick=c.tick;events(w,'suspicious_gathering','A Guard noticed a suspicious gathering.',guard.personId);break
      end end
     end
    end
   end
  else
   local target=chooseCellTarget(w,p.cell)
   if target then p.cell.state='sabotage';p.cell.targetId=target.id;p.cell.saboteurId=members[1].personId;p.cell.progress=0 else p.cell.state='blocked';p.cell.blockedTick=c.tick end
  end
 elseif p.cell and p.cell.state=='hostile' then
  local alive=false;for _,id in ipairs(p.cell.members) do for _,a in ipairs(w.workers) do if a.personId==id and a.alive then alive=true end end end
  if not alive then p.cell.state='resolved';p.cell.resolvedTick=c.tick;events(w,'insurgency_resolved','The insurgent cell is no longer active.',p.cell.id) end
 end
 if c.tick%1000~=0 then return end
 local candidates={};for _,a in ipairs(w.workers) do if a.alive and a.security.allegiance=='society' and a.security.grievance>=65 then candidates[#candidates+1]=a end end;table.sort(candidates,function(a,b) return a.security.grievance==b.security.grievance and a.personId<b.personId or a.security.grievance>b.security.grievance end)
 if p.lastProtest and not p.cell and #candidates<2 then p.lastProtest=nil end
 if not p.protest and not p.lastProtest and #candidates>=2 and sharedCause(candidates[1],candidates[2]) and mutualTrust(candidates[1],candidates[2]) then
  local cause=sharedCause(candidates[1],candidates[2]);local members={}
  for _,a in ipairs(candidates) do if sharedCause(candidates[1],a)==cause then members[#members+1]=a.personId;require('src.psychology').memory(c,a,'protested_conditions',{source='protest:'..c.tick,siteId=s.id});if #members==4 then break end end end
  p.protest={members=members,untilTick=c.tick+200,cause=cause};events(w,'protest','Colonists are protesting conditions.',members[1])
 end
 if (not p.cell or p.cell.state=='cancelled' or p.cell.state=='resolved') and not p.protest and p.lastProtest and p.lastProtest.endedTick and c.tick>=p.lastProtest.endedTick then
  local members={};local cause
  for _,a in ipairs(candidates) do if a.security.grievance>=85 and (a.stress or 0)>=60 and a.psychology.facets.independence>=60 then
   local compatible=false;for _,id in ipairs(members) do for _,other in ipairs(w.workers) do if other.personId==id and mutualTrust(a,other) and sharedCause(a,other) then compatible=true;cause=sharedCause(a,other) end end end
   if #members==0 then compatible=true;cause=(a.security.causes[1] and a.security.causes[1].kind) end
   if compatible then members[#members+1]=a.personId;if #members==4 then break end end
  end end
  if #members>=2 and cause then local id=c.security.nextCellId;c.security.nextCellId=id+1;p.cell={id=id,members=members,state='organizing',readyTick=c.tick+400,cause=cause,formedTick=c.tick,detected=false};p.lastProtest=nil end
 end
end
function S.step(c)
 if not S.enabled(c) then return end;schedule(c);for _,r in ipairs(c.security.raids) do
  if r.status=='approaching' and not r.warned and c.tick==r.arrivalTick-300 then local s=site(c,r.siteId);if s and F.relay(s.world) and c.factions.scan.known[r.factionId] then r.warned=true;events(s.world,'raid_warning','Inbound hostile expedition detected.',r.id) end end
  raidStep(c,r)
 end
 for _,s in ipairs(c.sites) do local w=s.world;for _,a in ipairs(w.workers) do if a.alive and a.security.guardEnabled and w.security.posture=='normal' and not a.task then S.train(c,w,a) end end;if c.tick%1000==0 then for _,a in ipairs(w.workers) do if a.alive and a.security.grievance>0 and (a.stress or 0)<40 then S.grieve(c,a,'stable_period',-1,'stable:'..math.floor(c.tick/1000)) end end end;unrest(c,s) end
 -- Terminal entries retain their compact public outcome briefly for history
 -- and then make room for later expeditions.  The persistent faction/security
 -- incidents and loot receipts are the bounded durable record.
 local kept={};for _,r in ipairs(c.security.raids) do if r.status~='resolved' or c.tick-(r.resolvedTick or c.tick)<1000 then kept[#kept+1]=r end end;c.security.raids=kept
end
function S.validatePersonal(x,tick)
 assert(type(x)=='table','Missing personal security')
 for key in pairs(x) do assert(({guardEnabled=true,combatXP=true,grievance=true,causes=true,ammo=true,lastCombatActionTick=true,lastXP=true,protestUntil=true,insurgentCellId=true,allegiance=true,equipmentIntent=true,reloadIntent=true})[key],'Unknown personal security key '..tostring(key)) end
 assert(type(x.guardEnabled)=='boolean');U.integer(x.combatXP,'combat XP',0,400);U.integer(x.grievance,'grievance',0,100);U.integer(x.ammo,'ammo',0,12)
 if x.lastCombatActionTick~=nil then U.integer(x.lastCombatActionTick,'combat action tick',0,tick) end
 if x.lastXP~=nil then U.integer(x.lastXP,'security XP tick',0,tick) end
 if x.protestUntil~=nil then U.integer(x.protestUntil,'protest end tick',0,10000000) end
 if x.insurgentCellId~=nil then U.integer(x.insurgentCellId,'insurgent cell ID',1,100000000) end
 if x.equipmentIntent~=nil then U.integer(x.equipmentIntent,'equipment intent',1,100000000) end
 if x.reloadIntent~=nil then U.integer(x.reloadIntent,'reload intent',1,12) end
 assert(x.allegiance=='society' or x.allegiance=='insurgent','Invalid allegiance')
 assert(type(x.causes)=='table' and #x.causes<=8,'Too many grievance causes')
 for _,cause in ipairs(x.causes) do
  for key in pairs(cause) do assert(({kind=true,amount=true,tick=true,source=true})[key],'Unknown grievance cause key') end
  assert(type(cause.kind)=='string' and #cause.kind<=48);U.integer(cause.amount,'grievance cause amount',-100,100);U.integer(cause.tick,'grievance cause tick',0,tick)
  assert(cause.source==nil or (type(cause.source)=='string' and #cause.source<=160),'Invalid grievance event source')
 end
 return true
end
local function ids(records,label,limit)
 assert(type(records)=='table' and #records<=limit,label..' bound exceeded');local seen={}
 for _,id in ipairs(records) do U.integer(id,label,1,100000000);assert(not seen[id],'Duplicate '..label);seen[id]=true end
 return seen
end
function S.validate(c)
 local root=c.security;assert(type(root)=='table' and root.version==S.version,'Invalid security campaign state');U.integer(root.nextRaidId,'Next raid ID',1,100000000);U.integer(root.nextActorId,'Next security actor ID',1,100000000);U.integer(root.nextCellId,'Next cell ID',1,100000000);assert(type(root.raids)=='table' and type(root.lootReceipts)=='table' and #root.raids<=S.maxRaids and #root.lootReceipts<=S.maxReceipts,'Security bounds exceeded')
 local raidIds,actorIds={},{}
 for _,r in ipairs(root.raids) do
  for key in pairs(r) do assert(({id=true,factionId=true,siteId=true,size=true,initialSize=true,committedTick=true,arrivalTick=true,status=true,goal=true,actors=true,lastContactTick=true,withdrawTick=true,warned=true,ingress=true,targetId=true,sabotageProgress=true,resolvedTick=true,resolution=true})[key],'Unknown raid key '..tostring(key)) end
  U.integer(r.id,'raid ID',1,root.nextRaidId-1);assert(not raidIds[r.id],'Duplicate raid ID');raidIds[r.id]=true;U.integer(r.factionId,'raid faction',2,5);assert(F.find(c,r.factionId));assert(site(c,r.siteId));U.integer(r.size,'raid size',1,4);U.integer(r.initialSize or r.size,'raid initial size',1,4);U.integer(r.committedTick,'raid committed tick',0,c.tick);U.integer(r.arrivalTick,'raid arrival tick',r.committedTick+800,10000000);assert(r.status=='approaching' or r.status=='holding' or r.status=='active' or r.status=='withdrawing' or r.status=='resolved');assert(r.goal=='assault' or r.goal=='sabotage');assert(type(r.actors)=='table' and #r.actors<=r.size)
  for _,a in ipairs(r.actors) do
   for key in pairs(a) do assert(({id=true,alive=true,allegiance=true,factionId=true,x=true,y=true,hp=true,combatXP=true,weapon=true,ammo=true,armor=true,dead=true,lootDropped=true,lastCombatActionTick=true})[key],'Unknown raid actor key') end
   U.integer(a.id,'raid actor ID',1,root.nextActorId-1);assert(not actorIds[a.id],'Duplicate raid actor ID');actorIds[a.id]=true;assert(type(a.alive)=='boolean' and a.allegiance=='raider' and a.factionId==r.factionId);local w=site(c,r.siteId).world;U.integer(a.x,'raid actor x',1,w.width);U.integer(a.y,'raid actor y',1,w.height);assert(U.finite(a.hp) and a.hp>=0 and a.hp<=100);U.integer(a.combatXP,'raid combat expertise',80,240);assert(type(a.weapon)=='table' and (a.weapon.kind=='frontier_carbine' or a.weapon.kind=='shock_baton'));U.integer(a.ammo,'raid ammo',0,6);assert(a.armor==nil or (type(a.armor)=='table' and a.armor.kind=='protective_vest'))
  end
 end
 for _,receipt in ipairs(root.lootReceipts) do
  for key in pairs(receipt) do assert(({raidId=true,factionId=true,tick=true,actorId=true,itemIds=true,ammo=true})[key],'Unknown loot receipt key') end
  U.integer(receipt.raidId,'loot raid ID',1,root.nextRaidId-1);U.integer(receipt.factionId,'loot faction ID',2,5);U.integer(receipt.tick,'loot tick',0,c.tick);U.integer(receipt.actorId,'loot actor ID',1,root.nextActorId-1);ids(receipt.itemIds,'loot item ID',2);U.integer(receipt.ammo,'loot ammo',0,6)
 end
 local cellIds={}
 for _,s in ipairs(c.sites) do local w=s.world;assert(w.frontier.security==1 and type(w.security)=='table' and w.security.version==S.version,'Missing security state');local p=w.security
  for key in pairs(p) do assert(({version=true,posture=true,posts=true,refuge=true,policyRevision=true,events=true,protest=true,cell=true,lastProtest=true})[key],'Unknown site security key '..tostring(key)) end
  assert(p.posture=='normal' or p.posture=='alert' or p.posture=='lockdown','Invalid security posture');assert(type(p.posts)=='table' and #p.posts<=8 and type(p.events)=='table' and #p.events<=S.maxEvents,'Security policy exceeds bound');U.integer(p.policyRevision,'Security policy revision',1,100000000)
  for _,post in ipairs(p.posts) do U.integer(post.x,'post x',1,w.width);U.integer(post.y,'post y',1,w.height);assert(N.stand(w,post.x,post.y,true),'Invalid defense post') end
  if p.refuge then for _,k in ipairs({'x1','x2'}) do U.integer(p.refuge[k],'refuge x',1,w.width) end;for _,k in ipairs({'y1','y2'}) do U.integer(p.refuge[k],'refuge y',1,w.height) end;assert(p.refuge.x1<=p.refuge.x2 and p.refuge.y1<=p.refuge.y2) end
  for _,event in ipairs(p.events) do U.integer(event.tick,'security event tick',0,c.tick);assert(type(event.kind)=='string' and type(event.text)=='string');assert(event.subject==nil or type(event.subject)=='number') end
  if p.protest then ids(p.protest.members,'protest member',4);U.integer(p.protest.untilTick,'protest end tick',0,10000000);assert(type(p.protest.cause)=='string') end
  if p.cell then
   local q=p.cell;U.integer(q.id,'cell ID',1,root.nextCellId-1);assert(not cellIds[q.id],'Duplicate cell ID');cellIds[q.id]=true;ids(q.members,'cell member',4);assert(q.state=='organizing' or q.state=='sabotage' or q.state=='hostile' or q.state=='blocked' or q.state=='cancelled' or q.state=='resolved');assert(type(q.cause)=='string');if q.readyTick then U.integer(q.readyTick,'cell ready tick',0,10000000) end;if q.targetId then U.integer(q.targetId,'cell target',1,w.nextId-1) end;if q.saboteurId then U.integer(q.saboteurId,'cell saboteur',1,100000000) end;if q.progress then U.integer(q.progress,'cell sabotage progress',0,120) end
  end
  for _,a in ipairs(w.workers) do S.validatePersonal(a.security,c.tick) end
 end
 return true
end
return S
