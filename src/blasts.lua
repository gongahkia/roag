-- Fictional game-world demolition, not a real explosive model or recipe.
-- A crew-built charge is inert until a worker completes an arming order.
local W=require('src.world')
local M=require('src.materials')
local S=require('src.structures')
local Signals=require('src.signals')
local B={fuse=80,radius=10}
function B.arm(w,s,worker)
 if not s or s.kind~='charge' or s.fuseAt then return false end
 s.fuseAt=w.tick+B.fuse;s.status='ARMED';s.enabled=true
 for _,a in ipairs(w.workers) do if a.alive and math.abs(a.x-(s.gx*4-2))+math.abs(a.y-(s.gy*4-2))<=B.radius+3 then
  a.evacuate={x=s.gx*4-2,y=s.gy*4-2,untilTick=s.fuseAt+2}
 end end
 W.event(w,'warning',(worker and worker.name or 'A chain reaction')..' armed a charge. '..B.fuse..' ticks remain; no disarm.',s.id)
 return true
end
local function destroyStructure(w,slot,s,charge)
 local J=require('src.jobs')
 for _,a in ipairs(w.workers) do if a.task and (a.task.slot==slot or a.task.store==slot) then J.release(w,a,true) end end
 for _,j in ipairs(w.jobs) do if j.state=='open' and W.slot(w,j.gx,j.gy)==slot then J.cancel(w,j) end end
 local def=S.def[s.kind];local salvage=charge and 0 or math.floor(def.cost/4)
 if salvage>0 then
  if s.kind=='field_school' then W.stack(w,'stone',1,s.gx*4-2,s.gy*4) else W.stack(w,def.resource,salvage,s.gx*4-2,s.gy*4) end
 end
 w.ledger.demolitionWaste=w.ledger.demolitionWaste+def.cost-salvage
 if (s.tank or 0)>0 then W.stack(w,'water',s.tank,s.gx*4-2,s.gy*4) end
 require('src.education').destroy(w,s);w.structures[slot]=nil;w.navRevision=w.navRevision+1
end
function B.detonate(w,slot)
 local source=w.structures[slot];if not source or source.kind~='charge' then return end
 local cx,cy=source.gx*4-2,source.gy*4-2
 -- Compute a deterministic attenuated wave BEFORE changing its obstacles.
 local wave,q,head={[W.index(w,cx,cy)]=20},{W.index(w,cx,cy)},1
 while head<=#q do
  local at=q[head];head=head+1;local x,y=W.xy(w,at);local power=wave[at]
  for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
   local nx,ny=x+d[1],y+d[2]
   if nx>2 and nx<w.width-1 and ny>2 and ny<w.height-1 and (nx-cx)^2+(ny-cy)^2<=B.radius^2 then
    local m=W.get(w,nx,ny)
    if m~=M.BEDROCK then
     local cost=1+(M.def[m].solid and 1 or 0)+(W.blocked(w,nx,ny) and 3 or 0)
     local nextPower=power-cost;local ni=W.index(w,nx,ny)
     if nextPower>0 and nextPower>(wave[ni] or 0) then wave[ni]=nextPower;q[#q+1]=ni end
    end
   end
  end
 end
 local sorted={};for i in pairs(wave) do sorted[#sorted+1]=i end;table.sort(sorted)
 local salvage={stone=0,soil=0,metal=0};local hitStructures={}
 for _,i in ipairs(sorted) do
  local x,y=W.xy(w,i);local m=w.mat[i]
  local s=W.structureAt(w,x,y)
  if s and wave[i]>=5 then hitStructures[W.slot(w,s.gx,s.gy)]=true end
  if m==M.WATER then W.put(w,x,y,M.STEAM)
  elseif m==M.ICE then W.put(w,x,y,M.WATER)
  elseif m==M.ROCK or m==M.SOIL or m==M.SAND or m==M.ORE then
   W.put(w,x,y,M.AIR)
   local resource=M.def[m].resource
   -- One recoverable unit in three; all destroyed remainder is on the ledger.
   if i%3==0 then salvage[resource]=salvage[resource]+1
   else w.ledger.demolitionWaste=w.ledger.demolitionWaste+1 end
  end
 end
 for _,kind in ipairs({'stone','soil','metal'}) do if salvage[kind]>0 then W.stack(w,kind,salvage[kind],cx,cy) end end
 for key=1,w.cols*w.rows do if hitStructures[key] then
  local s=w.structures[key]
  if s then
   if s.kind=='charge' and key~=slot then
    s.fuseAt=math.min(s.fuseAt or (w.tick+1),w.tick+1);s.status='CHAIN REACTION'
   else destroyStructure(w,key,s,s.kind=='charge') end
  end
 end end
 if w.structures[slot] then destroyStructure(w,slot,source,true) end
 for _,a in ipairs(w.workers) do if a.alive then
  local hit=0
  for y=a.y-2,a.y do for x=a.x,a.x+1 do hit=math.max(hit,wave[W.index(w,x,y)] or 0) end end
  if hit>0 then a.hp=math.max(0,a.hp-hit*9);a.injuryCause='blast trauma' end
 end end
 if w.content then
  local E=require('src.ecology')
  for _,c in ipairs(w.content.fauna) do if c.alive and wave[W.index(w,c.x,c.y)] then E.hurt(w,c,wave[W.index(w,c.x,c.y)]*12,'Blast') end end
  for _,p in ipairs(w.content.flora) do if p.alive and wave[W.index(w,p.x,p.y)] then E.floraDie(w,p,'Blast') end end
  for _,p in ipairs(w.content.sites) do if p.alive and (wave[W.index(w,p.x,p.y)] or 0)>5 then
   -- Ruin contents become physical piles; a blast is not a free duplication path.
   for _,kind in ipairs({'stone','soil','metal','food','water'}) do if (p.stock[kind] or 0)>0 then W.stack(w,kind,p.stock[kind],p.x,p.y);p.stock[kind]=0 end end
   p.alive=false;p.deathTick=w.tick
  end end
 end
 Signals.emit(w,'blast',cx,cy,30)
 w.blastEffects=w.blastEffects or {};w.blastEffects[#w.blastEffects+1]={x=cx,y=cy,tick=w.tick,radius=B.radius}
 if #w.blastEffects>12 then table.remove(w.blastEffects,1) end
 W.event(w,'blast','A demolition charge ruptured the surrounding terrain. Supplies and workers may be lost.',source.id)
end
function B.step(w)
 local due={}
 for slot=1,w.cols*w.rows do local s=w.structures[slot]
  if s and s.kind=='charge' and s.fuseAt and w.tick>=s.fuseAt then due[#due+1]=slot end
 end
 for _,slot in ipairs(due) do B.detonate(w,slot) end
 if w.blastEffects then
  local keep={};for _,e in ipairs(w.blastEffects) do if w.tick-e.tick<=12 then keep[#keep+1]=e end end
  w.blastEffects=keep
 end
end
return B
