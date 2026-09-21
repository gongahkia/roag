-- Site-bound G02 commands.  Keeping these at the campaign boundary prevents a
-- local map or an old save from synthesising globally owned equipment.
local U=require('src.util')
local W=require('src.world')
local J=require('src.jobs')
local E=require('src.equipment')
local C={}

function C.valid(c,site,payload)
 local ok,why=pcall(function()
  assert(E.enabled(c) and E.safe(c),'Equipment is unavailable in this older campaign')
  local w=site.world
  if payload.type=='fabricate' then
   U.integer(payload.slot,'Tool bench slot',1,w.cols*w.rows);assert(payload.kind=='pickaxe' or payload.kind=='rope_coil' or payload.kind=='component','Unknown tool recipe')
   assert(payload.kind~='component' or c.features.industry==1,'Machine components require an industry frontier campaign')
   local s=w.structures[payload.slot];assert(s and s.kind=='tool_bench','Select a completed tool bench')
   assert(not s.fabrication,'Tool bench is busy')
   if payload.priority~=nil then U.integer(payload.priority,'Fabrication priority',1,3) end
  elseif payload.type=='place_rope' then
   U.integer(payload.gx,'Rope block X',2,w.cols-1);U.integer(payload.gy,'Rope block Y',2,w.rows-1)
   if payload.priority~=nil then U.integer(payload.priority,'Rope priority',1,3) end
   assert(payload.direction==nil or payload.direction=='down' or payload.direction=='up','Rope direction must be down or up')
  elseif payload.type=='remove_rope' then
   U.integer(payload.ropeId,'Rope ID',1,w.nextRopeId-1);if payload.priority~=nil then U.integer(payload.priority,'Rope priority',1,3) end
   local found=false;for _,rope in ipairs(w.ropes) do if rope.id==payload.ropeId then found=true end end;assert(found,'Rope no longer exists')
  elseif payload.type=='drop_tool' then
   U.integer(payload.equipmentId,'Equipment ID',1,c.equipment.nextId-1);U.integer(payload.worker,'Worker ID',1,w.nextId-1)
   local a=W.find(w.workers,payload.worker);assert(a and a.alive,'Select a living local worker')
   local item=E.find(c,payload.equipmentId);assert(item and item.state=='equipped' and item.personId==a.personId,'Tool is not equipped by this worker')
  elseif payload.type=='load_tool' then
   U.integer(payload.equipmentId,'Equipment ID',1,c.equipment.nextId-1);U.integer(payload.craftId,'Craft ID',1,c.logistics.nextCraftId-1)
   local item=E.find(c,payload.equipmentId);assert(item and item.state=='loose' and item.siteId==site.id,'Tool is not loose at this site')
   local craft=require('src.logistics').craft(c,payload.craftId);assert(craft and craft.dockedSiteId==site.id,'Craft is not docked here');assert(E.craftCount(c,craft.id)<craft.capacity,'Craft tool hold is full')
   if payload.priority~=nil then U.integer(payload.priority,'Tool cargo priority',1,3) end
  elseif payload.type=='unload_tool' then
   U.integer(payload.equipmentId,'Equipment ID',1,c.equipment.nextId-1);U.integer(payload.craftId,'Craft ID',1,c.logistics.nextCraftId-1)
   local item=E.find(c,payload.equipmentId);assert(item and item.state=='craft' and item.craftId==payload.craftId,'Tool is not in this craft')
   local craft=require('src.logistics').craft(c,payload.craftId);assert(craft and craft.dockedSiteId==site.id,'Craft is not docked here')
  else assert(false,'Unknown equipment command') end
 end)
 return ok,ok and nil or tostring(why)
end
function C.apply(c,site,payload)
 local w=site.world
 if payload.type=='fabricate' then
  local s=w.structures[payload.slot];local j,why=J.add(w,'fabricate',s.gx,s.gy,nil,payload.priority)
  if not j then W.event(w,'rejected',why);return false end
  j.slot=payload.slot;j.recipe=payload.kind;s.fabrication={jobId=j.id,kind=payload.kind,progress=0,work=E.recipe(payload.kind).work};return true
 elseif payload.type=='place_rope' then
  local j,why=J.add(w,'rope',payload.gx,payload.gy,nil,payload.priority)
  if not j then W.event(w,'rejected',why);return false end
  j.ropeDirection=payload.direction or 'down';return true
 elseif payload.type=='remove_rope' then
  local rope;for _,r in ipairs(w.ropes) do if r.id==payload.ropeId then rope=r end end
  local gx,gy=W.tile(w,rope.laneLeftX,rope.anchorY);local j,why=J.add(w,'remove_rope',gx,gy,nil,payload.priority)
  if not j then W.event(w,'rejected',why);return false end;j.ropeId=rope.id;return true
 elseif payload.type=='drop_tool' then
  local a=W.find(w.workers,payload.worker);local item=E.find(c,payload.equipmentId);E.drop(c,item,site.id,a.x,a.y);W.event(w,'tool',a.name..' dropped '..item.kind..'.',item.id);return true
 else
  local item=E.find(c,payload.equipmentId);local craft=require('src.logistics').craft(c,payload.craftId);local gx,gy=W.tile(w,craft.anchor.x,craft.anchor.y)
  local j,why=J.add(w,'tool_cargo',gx,gy,nil,payload.priority)
  if not j then W.event(w,'rejected',why);return false end
  j.equipmentId=item.id;j.craftId=craft.id;j.cargoMode=payload.type=='load_tool' and 'load' or 'unload';return true
 end
end
return C
