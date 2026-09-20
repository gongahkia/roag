local W=require('src.world')
local P=require('src.particles')
local S=require('src.structures')
local A=require('src.colonists')
local Cmd=require('src.commands')
local Sim={}
function Sim.begin(w)
 w.tick=w.tick+1
 return w.tick
end
function Sim.body(w,clock,timings,context)
 timings=timings or {};local start=clock and clock()
 require('src.labor').refresh(w)
 require('src.blasts').step(w)
 P.step(w)
 if clock then timings.materials=clock()-start;start=clock() end
 -- Resource piles are discrete objects, not duplicate terrain particles.
 for _,p in ipairs(w.items) do if p.n>0 and not W.solid(w,p.x,p.y+1) and p.y<w.height-2 then p.y=p.y+1 end end
 S.step(w)
 if clock then timings.infrastructure=clock()-start;start=clock() end
 if w.content then require('src.ecology').step(w,context) end
 if clock then timings.ecology=clock()-start;start=clock() end
 A.step(w,context)
 if clock then timings.colonists=clock()-start end
 if w.tick%200==0 then
  local items={} for _,p in ipairs(w.items) do if p.n>0 or p.reserved then items[#items+1]=p end end w.items=items
  -- Bound completed-job storage while monotonic stats retain totals.
  if #w.jobs>512 then local jobs={} for _,j in ipairs(w.jobs) do if j.state=='open' then jobs[#jobs+1]=j end end w.jobs=jobs end
 end
 return timings
end
function Sim.step(w,commands,clock)
 local timings={};local start=clock and clock()
 Sim.begin(w)
 for _,c in ipairs(commands or {}) do Cmd.apply(w,c) end
 if clock then timings.commands=clock()-start end
 return Sim.body(w,clock,timings)
end
return Sim
