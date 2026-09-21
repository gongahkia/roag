local W=require('src.world')
local U=require('src.util')
local J=require('src.jobs')
local S=require('src.structures')
local M=require('src.materials')
local C={}
function C.valid(w,c)
 if type(c)~='table' or type(c.type)~='string' then return false,'Malformed command' end
 if require('src.colony_commands').types[c.type] then return require('src.colony_commands').valid(w,c) end
 if c.type=='order' or c.type=='cancel' or c.type=='priority' or c.type=='toggle' then
  if not U.finite(c.gx) or not U.finite(c.gy) or c.gx%1~=0 or c.gy%1~=0 or c.gx<2 or c.gx>w.cols-1 or c.gy<2 or c.gy>w.rows-1 then return false,'Outside buildable map' end
 end
 if c.type=='order' then
  if c.worker~=nil and c.worker~=0 then local a=W.find(w.workers,c.worker);if not a or not a.alive then return false,'Select a living worker' end end
  if c.kind~='dig' and c.kind~='build' and c.kind~='remove' then return false,'Unknown job' end
  if c.kind=='build' and not S.def[c.build] then return false,'Unknown structure' end
  if c.kind=='build' and c.build=='field_school' and not (w.frontier and w.frontier.education==1) then return false,'Field schools require a new education frontier campaign' end
  if c.kind=='build' and c.build=='tool_bench' and not (w.frontier and w.frontier.equipment==1) then return false,'Tool benches require an equipment frontier campaign' end
  if c.kind=='build' and c.build=='torch' then
   if not (w.frontier and w.frontier.visibility==1) then return false,'Torches require a visibility-enabled frontier campaign' end
   if S.torchCount(w)>=128 then return false,'This site already has 128 torches' end
  end
  if c.priority~=nil and c.priority~=1 and c.priority~=2 and c.priority~=3 then return false,'Priority must be 1, 2 or 3' end
 elseif c.type=='priority' then
  if c.value~=1 and c.value~=2 and c.value~=3 then return false,'Priority must be 1, 2 or 3' end
 elseif c.type=='port' then
  local s=w.structures[c.slot]
  if not s or s.kind~='pump' or (c.port~='intake' and c.port~='outlet') then return false,'Select a completed pump' end
  if not U.finite(c.x) or not U.finite(c.y) or c.x%1~=0 or c.y%1~=0 or not W.inside(w,c.x,c.y) then return false,'Invalid port' end
  if math.abs(c.x-(s.gx*4-2))+math.abs(c.y-(s.gy*4-2))>20 then return false,'Hose range: 20 material cells' end
 elseif c.type=='paint' then
  if w.mode~='practice' then return false,'Material brush is practice-only' end
  if not U.finite(c.x) or not U.finite(c.y) or c.x%1~=0 or c.y%1~=0 or c.x<=2 or c.x>=w.width-1 or c.y<=2 or c.y>=w.height-1 or not M.def[c.material] or c.material==M.BEDROCK then return false,'Invalid brush cell' end
 elseif c.type~='cancel' and c.type~='toggle' then return false,'Unknown command' end
 return true
end
function C.apply(w,c)
 local ok,why=C.valid(w,c)
 if not ok then W.event(w,'rejected',why);return false end
 if w.extinct and w.mode=='challenge' then return false end
 if require('src.colony_commands').types[c.type] then return require('src.colony_commands').apply(w,c) end
 if c.type=='order' then
  local j=J.add(w,c.kind,c.gx,c.gy,c.build,c.priority)
  if j and c.worker and c.worker~=0 then j.owner=c.worker end
 elseif c.type=='cancel' or c.type=='priority' then
  for _,j in ipairs(w.jobs) do if j.gx==c.gx and j.gy==c.gy and j.state=='open' then
   if c.type=='cancel' then J.cancel(w,j) else j.priority=c.value end
  end end
 elseif c.type=='toggle' then local s=w.structures[W.slot(w,c.gx,c.gy)] if s then s.enabled=not s.enabled end
 elseif c.type=='port' then w.structures[c.slot][c.port]={x=c.x,y=c.y}
 elseif c.type=='paint' then
  local old=W.get(w,c.x,c.y)
  local function wet(m) return m==M.WATER or m==M.ICE or m==M.STEAM end
  w.ledger.waterMade=w.ledger.waterMade+(wet(c.material) and 1 or 0)-(wet(old) and 1 or 0)
  local function mineral(m) return m==M.ROCK or m==M.SOIL or m==M.SAND or m==M.ORE or m==M.LAVA end
  w.ledger.mineralMade=w.ledger.mineralMade+(mineral(c.material) and 1 or 0)-(mineral(old) and 1 or 0)
  W.put(w,c.x,c.y,c.material)
 end
 return true
end
return C
