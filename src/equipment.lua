-- G02 owns the deliberately small set of unique physical tools.  It is campaign
-- state because a pick or coil can cross sites, while ropes themselves remain
-- local infrastructure in the world that contains them.
local U=require('src.util')
local W=require('src.world')
local N=require('src.nav')

local E={version=1,maxSiteItems=128,maxTransitItems=64,maxRopes=128}
local value={pickaxe=2,rope_coil=1,frontier_carbine=4,shock_baton=3,protective_vest=4}
local recipes={pickaxe={metal=2,work=120},rope_coil={metal=1,work=60},component={metal=2,work=120}}

local function dense(t,label,limit)
 assert(type(t)=='table',label..' must be an array')
 local n,max=0,0
 for k in pairs(t) do U.integer(k,label..' index',1,limit);n=n+1;if k>max then max=k end end
 assert(n==max and n<=limit,label..' has a hole or exceeds its limit')
 return max
end
local function site(c,id)
 for _,s in ipairs(c.sites or {}) do if s.id==id then return s end end
end
local function person(c,id)
 for _,s in ipairs(c.sites or {}) do for _,a in ipairs(s.world.workers) do if a.personId==id then return s,a end end end
 if c.logistics then for _,craft in ipairs(c.logistics.crafts) do for _,a in ipairs(craft.passengers or {}) do if a.personId==id then return nil,a end end end end
end

function E.enabled(c) return c and c.features and c.features.equipment==1 end
function E.safe(c) return c and c.features and c.features.safe_excavation==1 end
function E.value(kind) return assert(value[kind],'Unknown equipment kind') end
function E.recipe(kind) return assert(recipes[kind],'Unknown tool recipe') end
function E.new() return {version=E.version,nextId=1,items={}} end
function E.find(c,id)
 for _,item in ipairs(c.equipment.items) do if item.id==id then return item end end
end
function E.forSite(c,siteId,kind,state)
 local out={}
 for _,item in ipairs(c.equipment.items) do
  if item.siteId==siteId and (not kind or item.kind==kind) and (not state or item.state==state) then out[#out+1]=item end
 end
 table.sort(out,function(a,b)return a.id<b.id end);return out
end
function E.equipped(c,personId,slot)
 for _,item in ipairs(c.equipment.items) do if item.state=='equipped' and item.personId==personId and (not slot or item.slot==slot or (not item.slot and slot=='tool')) then return item end end
end
function E.create(c,siteId,kind,x,y)
 assert(E.enabled(c),'Equipment is unavailable in this older campaign')
 assert(site(c,siteId),'Equipment site is missing');assert(value[kind],'Unknown equipment kind')
 assert(#E.forSite(c,siteId)<E.maxSiteItems,'Equipment site limit reached')
 local id=c.equipment.nextId;c.equipment.nextId=id+1
 local item={id=id,kind=kind,state='loose',siteId=siteId,x=x,y=y}
 c.equipment.items[#c.equipment.items+1]=item;return item
end
function E.drop(c,item,siteId,x,y)
 assert(item and value[item.kind],'Invalid equipment')
 item.state='loose';item.siteId=siteId;item.x=x;item.y=y;item.personId=nil;item.craftId=nil;item.ropeId=nil;item.structureId=nil;item.owner=nil;item.reservedBy=nil
 return item
end
function E.toIndustry(item,siteId,structureId,owner)
 assert(item and value[item.kind],'Invalid equipment')
 item.state='industry';item.siteId=siteId;item.structureId=structureId;item.owner=owner;item.personId=nil;item.craftId=nil;item.ropeId=nil;item.x=nil;item.y=nil;item.reservedBy=nil
 return item
end
function E.equip(c,item,worker,slot)
 assert(item and item.state=='loose','Equipment is no longer loose')
 slot=slot or (item.kind=='pickaxe' and 'tool' or item.kind=='protective_vest' and 'armor' or 'weapon')
 assert((slot=='tool' and item.kind=='pickaxe') or (slot=='weapon' and (item.kind=='frontier_carbine' or item.kind=='shock_baton')) or (slot=='armor' and item.kind=='protective_vest'),'Invalid equipment slot')
 assert(not E.equipped(c,worker.personId,slot),'Worker already has equipped '..slot)
 item.state='equipped';item.slot=slot;item.personId=worker.personId;item.siteId=nil;item.x=nil;item.y=nil;item.structureId=nil;item.owner=nil;item.reservedBy=nil
 return item
end
function E.pickFor(c,worker)
 local item=E.equipped(c,worker.personId,'tool')
 return item and item.kind=='pickaxe' and item or nil
end
function E.digWork(c,worker,material)
 if not E.safe(c) or not E.pickFor(c,worker) then return 1 end
 local M=require('src.materials')
 if material==M.SOIL or material==M.SAND or material==M.ICE then return 2 end
 if material==M.ROCK or material==M.ORE then return 3 end
 return 1
end
function E.nearestLoose(c,siteId,kind,w,a,f,returnable)
 local best,bestPath,bestNode,bestDistance
 for _,item in ipairs(E.forSite(c,siteId,kind,'loose')) do
  if not item.reservedBy or item.reservedBy==a.personId then
   for _,i in ipairs(f.queue) do
    local x,y=W.xy(w,i)
    if (not returnable or returnable[i]) and N.reach(w,x,y,item.x,item.y,4) then
     local d=f.distance[i]
     if not best or d<bestDistance or (d==bestDistance and item.id<best.id) then
      best,bestNode,bestDistance=item,i,d;bestPath=N.path(f,i)
     end
     break
    end
   end
  end
 end
 return best,bestPath,bestNode,bestDistance
end
function E.reserve(item,worker) if item and (not item.reservedBy or item.reservedBy==worker.personId) then item.reservedBy=worker.personId;return true end end
function E.release(item,worker)
 if item and (not worker or item.reservedBy==worker.personId) then item.reservedBy=nil end
end
function E.carry(c,item,worker)
 assert(item and item.state=='loose' and item.reservedBy==worker.personId,'Equipment pickup changed')
 item.state='carried';item.slot=nil;item.personId=worker.personId;item.siteId=nil;item.x=nil;item.y=nil;item.structureId=nil;item.owner=nil;item.reservedBy=nil
end
function E.carried(c,worker,kind)
 for _,item in ipairs(c.equipment.items) do if item.state=='carried' and item.personId==worker.personId and (not kind or item.kind==kind) then return item end end
end
function E.craftCount(c,craftId)
 local n=0;for _,item in ipairs(c.equipment and c.equipment.items or {}) do if item.state=='craft' and item.craftId==craftId then n=n+1 end end;return n
end
function E.siteMineral(c,siteId)
 local n=0
 for _,item in ipairs(c.equipment and c.equipment.items or {}) do
  local localHere=item.siteId==siteId
  if item.state=='equipped' or item.state=='carried' then
   local s=select(1,person(c,item.personId));localHere=s and s.id==siteId
  elseif item.state=='craft' then
   for _,craft in ipairs(c.logistics and c.logistics.crafts or {}) do if craft.id==item.craftId and craft.dockedSiteId==siteId then localHere=true end end
  end
  if localHere then n=n+E.value(item.kind) end
 end
 return n
end
function E.transitMineral(c)
 local n=0
 for _,item in ipairs(c.equipment and c.equipment.items or {}) do
  if item.state=='craft' then
   for _,craft in ipairs(c.logistics and c.logistics.crafts or {}) do if craft.id==item.craftId and craft.journey then n=n+E.value(item.kind) end end
  end
 end
 return n
end
function E.loadCraft(c,item,craftId)
 assert(item and item.state=='carried','A carried item is required for craft loading');item.state='craft';item.craftId=craftId;item.personId=nil;item.siteId=nil;item.x=nil;item.y=nil;item.structureId=nil;item.owner=nil;item.reservedBy=nil
end
function E.unloadCraft(c,item,siteId,x,y)
 assert(item and item.state=='craft','Craft item is missing');E.drop(c,item,siteId,x,y)
end
function E.installRope(c,w,siteId,item,anchorX,anchorY,length)
 assert(item and item.kind=='rope_coil' and item.state=='carried','A carried rope coil is required')
 assert(#(w.ropes or {})<E.maxRopes,'Rope installation limit reached')
 U.integer(length,'Rope length',1,24);U.integer(anchorX,'Rope lane x',1,w.width);U.integer(anchorY,'Rope anchor y',1,w.height)
 assert(anchorX+1<=w.width and anchorY+length-1<=w.height,'Rope exceeds map bounds')
 for y=anchorY,anchorY+length-1 do
  assert(not W.solid(w,anchorX,y) and not W.solid(w,anchorX+1,y),'Rope path is obstructed')
 end
 w.ropes=w.ropes or {};w.nextRopeId=w.nextRopeId or 1
 local id=w.nextRopeId;w.nextRopeId=id+1
 w.ropes[#w.ropes+1]={id=id,anchorX=anchorX,anchorY=anchorY,laneLeftX=anchorX,length=length,itemId=item.id}
 item.state='rope';item.siteId=siteId;item.personId=nil;item.ropeId=id;return w.ropes[#w.ropes]
end
function E.removeRope(c,w,siteId,rope,x,y)
 assert(rope,'Rope is missing');local item=E.find(c,rope.itemId);assert(item and item.state=='rope' and item.ropeId==rope.id,'Rope custody is invalid')
 E.drop(c,item,siteId,x,y)
 for i,r in ipairs(w.ropes) do if r==rope then table.remove(w.ropes,i);break end end
 w.navRevision=w.navRevision+1
end
function E.destroyPending(c,w)
 if not w.destroyedRopes then return end
 local gone={};for _,id in ipairs(w.destroyedRopes) do gone[id]=true end
 local keep={}
 for _,rope in ipairs(w.ropes or {}) do
  if gone[rope.id] then
   for i,item in ipairs(c.equipment.items) do if item.id==rope.itemId then table.remove(c.equipment.items,i);break end end
  else keep[#keep+1]=rope end
 end
 w.ropes=keep;w.destroyedRopes=nil;w.navRevision=w.navRevision+1
end
function E.ropeAt(w,x,y)
 for _,rope in ipairs(w.ropes or {}) do
  if x==rope.laneLeftX and y>=rope.anchorY and y<rope.anchorY+rope.length then return rope end
 end
end
function E.stress(c,a,amount,tick,kind)
 if not E.safe(c) or not a.alive then return end
 if c.features.psychology==1 then return require('src.psychology').physical(c,a,amount,tick,kind) end
 a.stress=math.min(100,(a.stress or 0)+amount);a.lastStressTick=tick
 if a.stress>=80 then a.panic=true end
end
function E.recover(c,a,w)
 if not E.safe(c) or not a.alive then return end
 if c.features.psychology==1 then return require('src.psychology').recover(c,a,w) end
 a.stress=a.stress or 0
 if w.tick%20==0 and not a.evacuate and a.hunger<82 and a.fatigue<95 and a.breath>0 then a.stress=math.max(0,a.stress-1) end
 if a.panic and a.stress<=50 and not a.evacuate then a.panic=false end
end
function E.validate(c)
 if not E.enabled(c) then assert(c.equipment==nil,'Equipment state requires equipment feature');return true end
 local state=c.equipment;assert(type(state)=='table' and state.version==E.version,'Unsupported equipment state')
 U.integer(state.nextId,'Next equipment ID',1,100000000);dense(state.items,'Equipment',E.maxSiteItems*3+E.maxTransitItems)
 local seen,max,siteCount,transit,equipped={},0,{},0,{}
 for _,item in ipairs(state.items) do
  assert(type(item)=='table','Malformed equipment item');for key in pairs(item) do assert(({id=true,kind=true,state=true,siteId=true,x=true,y=true,personId=true,craftId=true,ropeId=true,reservedBy=true,structureId=true,owner=true,slot=true})[key],'Unknown equipment key') end
  U.integer(item.id,'Equipment ID',1,state.nextId-1);assert(not seen[item.id],'Duplicate equipment ID');seen[item.id]=true;max=math.max(max,item.id);assert(value[item.kind],'Unknown equipment kind')
  assert(item.state=='loose' or item.state=='carried' or item.state=='equipped' or item.state=='craft' or item.state=='rope' or item.state=='industry','Unknown equipment state')
  if item.state=='loose' then local s=site(c,item.siteId);assert(s,'Loose equipment site missing');U.integer(item.x,'Equipment x',1,s.world.width);U.integer(item.y,'Equipment y',1,s.world.height);siteCount[item.siteId]=(siteCount[item.siteId] or 0)+1
  elseif item.state=='carried' or item.state=='equipped' then
   local _,a=person(c,item.personId);assert(a,'Equipment person is missing');if item.state=='equipped' then
    local slot=item.slot or 'tool';assert((slot=='tool' and item.kind=='pickaxe') or (slot=='weapon' and (item.kind=='frontier_carbine' or item.kind=='shock_baton')) or (slot=='armor' and item.kind=='protective_vest'),'Invalid equipped item')
    local key=item.personId..':'..slot;assert(not equipped[key],'More than one equipped '..slot);equipped[key]=true
   end
  elseif item.state=='craft' then
   local found=false;for _,craft in ipairs(c.logistics and c.logistics.crafts or {}) do if craft.id==item.craftId then found=true end end;assert(found,'Equipment craft is missing');transit=transit+1
  elseif item.state=='rope' then
   local s=site(c,item.siteId);assert(s,'Rope equipment site missing');local found=false;for _,rope in ipairs(s.world.ropes or {}) do if rope.id==item.ropeId and rope.itemId==item.id then found=true end end;assert(found,'Installed rope is missing')
  else
   local s=site(c,item.siteId);assert(s and c.features.industry==1,'Industrial equipment state is invalid');U.integer(item.structureId,'Industrial equipment structure',1,100000000);assert(item.owner=='input' or item.owner=='output' or item.owner=='belt' or item.owner=='bin','Invalid industrial equipment owner')
   local structure=require('src.industry').find(s.world,item.structureId);assert(structure,'Industrial equipment owner is missing')
   local list=item.owner=='input' and structure.input or item.owner=='output' and structure.output or structure.cargo
   local found=false;for _,record in ipairs(list or {}) do if record.equipmentId==item.id then found=true end end;assert(found,'Industrial equipment is absent from its owner')
  end
 end
 for _,n in pairs(siteCount) do assert(n<=E.maxSiteItems,'Equipment site limit exceeded') end
 assert(transit<=E.maxTransitItems,'Transit equipment limit exceeded');assert(state.nextId>max,'Equipment allocator regressed')
 for _,s in ipairs(c.sites) do
  local w=s.world
  if E.safe(c) then
   assert(type(w.ropes)=='table','Safe-excavation world lacks ropes');dense(w.ropes,'Ropes',E.maxRopes);U.integer(w.nextRopeId,'Next rope ID',1,100000000)
   local ids,maxRope={},0
   for _,rope in ipairs(w.ropes) do
    for key in pairs(rope) do assert(({id=true,anchorX=true,anchorY=true,laneLeftX=true,length=true,itemId=true})[key],'Unknown rope key') end
    U.integer(rope.id,'Rope ID',1,w.nextRopeId-1);assert(not ids[rope.id],'Duplicate rope ID');ids[rope.id]=true;maxRope=math.max(maxRope,rope.id)
    U.integer(rope.anchorX,'Rope anchor X',1,w.width-1);U.integer(rope.anchorY,'Rope anchor Y',1,w.height);assert(rope.laneLeftX==rope.anchorX,'Rope lane differs from anchor');U.integer(rope.length,'Rope length',1,24);assert(rope.anchorY+rope.length-1<=w.height,'Rope out of bounds');U.integer(rope.itemId,'Rope equipment ID',1,state.nextId-1)
   end
   assert(w.nextRopeId>maxRope,'Rope allocator regressed')
  else assert(w.ropes==nil and w.nextRopeId==nil,'Legacy world gained rope state') end
 end
 return true
end

return E
