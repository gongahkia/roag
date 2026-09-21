-- Versioned, bounded frontier visibility.  Light and current sight are derived
-- caches; only last-seen terrain and structures are campaign state.
local W=require('src.world')
local M=require('src.materials')
local Body=require('src.body')
local U=require('src.util')
local S=require('src.structures')
local V={version=1,sightRadius=20,darkRadius=3,torchRadius=16,shuttleRadius=20,maxTorches=128}

function V.enabled(w) return w and w.frontier and w.frontier.visibility==1 end
function V.new(w)
 local rows={};for y=1,w.height do rows[y]=string.rep('\0',w.width) end
 return {version=V.version,width=w.width,height=w.height,rows=rows,structures={}}
end
local function exact(t,keys,label)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do assert(keys[key],'Unknown '..label..' key '..tostring(key)) end
 for key in pairs(keys) do assert(t[key]~=nil,'Missing '..label..' key '..key) end
end
function V.validate(w)
 if not V.enabled(w) then assert(w.visibility==nil,'Visibility state requires visibility feature');return true end
 local torches=0;for _,s in pairs(w.structures) do if s.kind=='torch' then torches=torches+1 end end
 assert(torches<=V.maxTorches,'Torch cap exceeded')
 local memory=w.visibility;exact(memory,{version=true,width=true,height=true,rows=true,structures=true},'Visibility memory')
 assert(memory.version==V.version and memory.width==w.width and memory.height==w.height,'Invalid visibility memory dimensions')
 assert(type(memory.rows)=='table' and #memory.rows==w.height,'Invalid visibility memory rows')
 for y,row in ipairs(memory.rows) do
  assert(type(row)=='string' and #row==w.width,'Invalid visibility memory row '..y)
  for x=1,#row do local code=row:byte(x);assert(code and code<=M.ICE+1,'Invalid remembered material code') end
 end
 assert(type(memory.structures)=='table','Invalid remembered structures')
 local count=0
 for slot,record in pairs(memory.structures) do
  count=count+1;U.integer(slot,'Remembered structure slot',1,w.cols*w.rows)
  exact(record,{id=true,kind=true,gx=true,gy=true,enabled=true,tick=true},'Remembered structure')
  U.integer(record.id,'Remembered structure ID',1,100000000);assert(require('src.structures').def[record.kind],'Unknown remembered structure')
  U.integer(record.gx,'Remembered structure x',1,w.cols);U.integer(record.gy,'Remembered structure y',1,w.rows)
  assert(slot==W.slot(w,record.gx,record.gy),'Misplaced remembered structure');assert(type(record.enabled)=='boolean','Invalid remembered structure enabled');U.integer(record.tick,'Remembered structure tick',0,w.tick)
 end
 assert(count<=w.cols*w.rows,'Too many remembered structures')
 return true
end
function V.memoryAt(w,x,y)
 if not V.enabled(w) then return W.get(w,x,y),'current' end
 local code=w.visibility.rows[y]:byte(x)
 if code==0 then return nil,'unseen' end
 return code-1,'remembered'
end
local function setMemory(w,x,y,m,changed)
 local row=w.visibility.rows[y];local code=m+1
 if row:byte(x)==code then return end
 local pending=changed[y];if not pending then pending={};changed[y]=pending end;pending[x]=code
end
local function commitMemory(w,changed)
 for y,pending in pairs(changed) do
  local row=w.visibility.rows[y];local bytes={}
  for x=1,w.width do bytes[x]=string.char(pending[x] or row:byte(x)) end
  w.visibility.rows[y]=table.concat(bytes)
 end
end
function V.opaque(w,x,y) return W.solid(w,x,y) end
-- Recursive octant shadowcasting. It visits visibility regions, not a ray for
-- every target. The returned table is keyed by the world cell index.
function V.fov(w,ox,oy,radius,opaque)
 local visible={};if not W.inside(w,ox,oy) then return visible end
 opaque=opaque or V.opaque;visible[W.index(w,ox,oy)]=true
 local transforms={{1,0,0,1},{0,1,1,0},{0,-1,1,0},{-1,0,0,1},{-1,0,0,-1},{0,-1,-1,0},{0,1,-1,0},{1,0,0,-1}}
 local radius2=radius*radius
 local function cast(row,startSlope,endSlope,xx,xy,yx,yy)
  if startSlope<endSlope then return end
  local nextStart=startSlope
  for distance=row,radius do
   local blocked=false;local dx=-distance-1;local dy=-distance
   while dx<=0 do
    dx=dx+1
    local x=ox+dx*xx+dy*xy;local y=oy+dx*yx+dy*yy
    local left=(dx-0.5)/(dy+0.5);local right=(dx+0.5)/(dy-0.5)
    if startSlope<right then
     -- cell is above the current visible region
    elseif endSlope>left then
     break
    else
     if dx*dx+dy*dy<=radius2 and W.inside(w,x,y) then visible[W.index(w,x,y)]=true end
     local blockedHere=not W.inside(w,x,y) or opaque(w,x,y)
     if blocked then
      if blockedHere then nextStart=right else blocked=false;startSlope=nextStart end
     elseif blockedHere and distance<radius then
      blocked=true;cast(distance+1,startSlope,left,xx,xy,yx,yy);nextStart=right
     end
    end
   end
   if blocked then break end
  end
 end
 for _,t in ipairs(transforms) do cast(1,1,0,t[1],t[2],t[3],t[4]) end
 return visible
end
local function floorDistance(dx,dy) return math.floor(math.sqrt(dx*dx+dy*dy)) end
local function mergeLight(w,light,mask,x,y,radius)
 for index in pairs(mask) do
  local tx,ty=W.xy(w,index);local distance=floorDistance(tx-x,ty-y)
  if distance<=radius then
   local value=math.max(1,radius+1-distance)
   if value>(light[index] or 0) then light[index]=value end
  end
 end
end
local function sourceMasks(w,context,light)
 for _,s in pairs(w.structures) do if s.kind=='torch' and s.enabled and S.supported(w,s) then
  local x,y=s.gx*4-2,s.gy*4-2;mergeLight(w,light,V.fov(w,x,y,V.torchRadius),x,y,V.torchRadius)
 end end
 if w.frontier and w.frontier.industry==1 then
  for _,source in ipairs(require('src.industry').lightSources(w)) do
   mergeLight(w,light,V.fov(w,source.x,source.y,source.radius),source.x,source.y,source.radius)
  end
 end
 if context and context.campaign and context.campaign.features.logistics==1 then
  for _,craft in ipairs(require('src.logistics').craftsAt(context.campaign,context.siteId)) do
   local x,y=craft.anchor.x,craft.anchor.y;mergeLight(w,light,V.fov(w,x,y,V.shuttleRadius),x,y,V.shuttleRadius)
  end
 end
end
function V.derive(w,context)
 if not V.enabled(w) then return nil end
 local signature={tostring(w.tick),tostring(w.navRevision)}
 for _,worker in ipairs(w.workers) do signature[#signature+1]=worker.id..':'..worker.x..':'..worker.y..':'..(worker.alive and '1' or '0') end
 for slot=1,w.cols*w.rows do local s=w.structures[slot];if s and (s.kind=='torch' or s.kind=='electric_lamp') then signature[#signature+1]=(s.kind=='torch' and 'T' or 'L')..slot..':'..(s.enabled and '1' or '0')..':'..(s._powerGranted and '1' or '0') end end
 if context and context.campaign and context.campaign.features.logistics==1 then
  for _,craft in ipairs(require('src.logistics').craftsAt(context.campaign,context.siteId)) do signature[#signature+1]='C'..craft.id..':'..craft.anchor.x..':'..craft.anchor.y end
 end
 signature=table.concat(signature,'|')
 local cache=w._visibility
 if cache and cache.signature==signature then return cache end
 local light={};sourceMasks(w,context,light)
 local current,people={},{}
 for _,worker in ipairs(w.workers) do if worker.alive then
  local personal={};for _,eye in ipairs(Body.eyes(w,worker.x,worker.y)) do
   local mask=V.fov(w,eye.x,eye.y,V.sightRadius)
   for index in pairs(mask) do
    local x,y=W.xy(w,index)
    if (light[index] or 0)>0 or floorDistance(x-eye.x,y-eye.y)<=V.darkRadius then personal[index]=true;current[index]=true end
   end
  end
  people[worker.id]=personal
 end end
 cache={signature=signature,light=light,current=current,people=people}
 w._visibility=cache;return cache
end
function V.visible(w,worker,x,y,context)
 if not V.enabled(w) then return true end
 if not worker or not worker.alive or not W.inside(w,x,y) then return false end
 return V.derive(w,context).people[worker.id][W.index(w,x,y)]==true
end
function V.currentlyVisible(w,x,y,context)
 if not V.enabled(w) then return true end
 return W.inside(w,x,y) and V.derive(w,context).current[W.index(w,x,y)]==true or false
end
function V.lightAt(w,x,y,context)
 if not V.enabled(w) then return nil end
 return V.derive(w,context).light[W.index(w,x,y)] or 0
end
function V.update(w,context)
 if not V.enabled(w) then return end
 local cache=V.derive(w,context);local changed={}
 for index in pairs(cache.current) do local x,y=W.xy(w,index);setMemory(w,x,y,W.get(w,x,y),changed) end
 commitMemory(w,changed)
 -- A block is remembered only after its service/centre cell was actually seen.
 local records=w.visibility.structures
 for slot=1,w.cols*w.rows do
  local s=w.structures[slot];local x,y
  if s then x,y=s.gx*4-2,s.gy*4-2
  elseif records[slot] then local record=records[slot];x,y=record.gx*4-2,record.gy*4-2 end
  if x and V.currentlyVisible(w,x,y,context) then
   if s then records[slot]={id=s.id,kind=s.kind,gx=s.gx,gy=s.gy,enabled=s.enabled,tick=w.tick} else records[slot]=nil end
  end
 end
 return cache
end
return V
