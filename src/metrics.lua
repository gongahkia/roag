local W=require('src.world')
local M=require('src.materials')
local S=require('src.structures')
local Metrics={}
local function mineralValue(kind,n)
 return (kind=='component' and 2 or 1)*(n or 0)
end
function Metrics.measure(w)
 local r={water=0,mineral=0,food=W.totalResource(w,'food'),alive=W.alive(w),jobs=0,
  blocked=0,farms=0,readyCrops=0,structures=0,waterCells=0,lavaCells=0,materialCounts={}}
 for m=0,9 do r.materialCounts[m]=0 end
 for i=1,w.n do
  local m=w.mat[i];r.materialCounts[m]=r.materialCounts[m]+1
  if m==M.WATER or m==M.STEAM or m==M.ICE then r.water=r.water+1 end
  if m==M.ROCK or m==M.SOIL or m==M.SAND or m==M.ORE or m==M.LAVA then r.mineral=r.mineral+1 end
 end
 r.waterCells=r.materialCounts[M.WATER];r.lavaCells=r.materialCounts[M.LAVA]
 for _,kind in ipairs({'stone','soil','metal','component'}) do r.mineral=r.mineral+mineralValue(kind,W.totalResource(w,kind)) end
 r.water=r.water+W.totalResource(w,'water')
 for _,j in ipairs(w.jobs) do
  if type(j.delivered)=='table' then for kind,amount in pairs(j.delivered) do r.mineral=r.mineral+mineralValue(kind,amount) end else r.mineral=r.mineral+(j.delivered or 0) end
  if j.state=='open' then r.jobs=r.jobs+1;if not j.assigned then r.blocked=r.blocked+1 end end
 end
 for slot=1,w.cols*w.rows do local s=w.structures[slot]
  if s then
   r.structures=r.structures+1
   for kind,amount in pairs(S.materials(s.kind)) do r.mineral=r.mineral+mineralValue(kind,amount) end
   r.water=r.water+(s.tank or 0)
   if s.kind=='farm' then r.farms=r.farms+1;if s.growth>=w.rules.cropTicks then r.readyCrops=r.readyCrops+1 end end
   if w.industry then
    for _,list in ipairs({s.input,s.output,s.inprocess,s.cargo,s.trade and s.trade.cargo}) do
     for _,record in ipairs(list or {}) do
      if not record.equipmentId and (record.kind=='stone' or record.kind=='soil' or record.kind=='metal' or record.kind=='component') then r.mineral=r.mineral+mineralValue(record.kind,record.n) end
      if not record.equipmentId and record.kind=='water' then r.water=r.water+(record.n or 0) end
      if not record.equipmentId and record.kind=='food' then r.food=r.food+(record.n or 0);if r.foodBudget then r.foodBudget=r.foodBudget+(record.n or 0) end end
     end
    end
   end
  end
 end
 if w.content then
  r.foodBudget=r.food;r.creatures=0;r.growths=0;r.ruins=#w.content.ruins;r.surveyed=0
  for _ in pairs(w.content.discoveries) do r.surveyed=r.surveyed+1 end
  for _,p in ipairs(w.content.flora) do if p.alive then r.growths=r.growths+1;r.foodBudget=r.foodBudget+p.food;r.water=r.water+p.water end end
  for _,c in ipairs(w.content.fauna) do if c.alive then r.creatures=r.creatures+1;r.foodBudget=r.foodBudget+c.food end end
  for _,site in ipairs(w.content.sites) do
   r.foodBudget=r.foodBudget+(site.stock.food or 0);r.water=r.water+(site.stock.water or 0)
   r.mineral=r.mineral+(site.stock.stone or 0)+(site.stock.soil or 0)+(site.stock.metal or 0)
  end
 end
 r.waterResidual=w.baseline and r.water+w.ledger.waterUsed-w.ledger.waterMade-w.baseline.water or 0
 r.mineralResidual=w.baseline and r.mineral+w.ledger.demolitionWaste-(w.ledger.mineralMade or 0)-w.baseline.mineral or 0
 r.foodResidual=w.baseline and (r.foodBudget or r.food)+w.ledger.foodEaten-w.ledger.foodGrown-(w.baseline.foodBudget or w.baseline.food) or 0
 r.changedWater=w.baseline and r.waterCells-w.baseline.waterCells or 0
 return r
end
return Metrics
