local W=require('src.world')
local M=require('src.materials')
local C=require('config')
local S={}
S.def={
 wall={label='Wall',cost=16,resource='stone',work=44},
 platform={label='Floor',cost=4,resource='stone',work=20},
 ladder={label='Ladder',cost=4,resource='stone',work=20},
 bed={label='Bed',cost=6,resource='stone',work=30},
 store={label='Stockpile',cost=4,resource='stone',work=20},
 farm={label='Fungus bed',cost=8,resource='soil',work=35},
 pump={label='Hand pump',cost=8,resource='metal',work=50},
 charge={label='Demolition charge',cost=4,resource='metal',work=35},
 ward={label='Resonance ward',cost=10,resource='metal',work=45},
 field_school={label='Field school',cost=6,resource='stone',materials={stone=4,metal=2},work=60},
}
function S.materials(kind)
 local def=assert(S.def[kind],'Unknown structure')
 return def.materials or {[def.resource]=def.cost}
end
function S.complete(kind,delivered)
 local materials=S.materials(kind)
 if type(delivered)=='number' then return delivered>=assert(S.def[kind]).cost end
 for resource,amount in pairs(materials) do if (delivered[resource] or 0)<amount then return false end end
 return true
end
function S.missing(kind,delivered)
 local materials=S.materials(kind);local out={}
 if type(delivered)=='number' then
  local def=assert(S.def[kind]);if delivered<def.cost then out[#out+1]={resource=def.resource,amount=def.cost-delivered} end
 else
  for _,resource in ipairs({'stone','soil','metal','food','water'}) do
   local amount=materials[resource];if amount and (delivered[resource] or 0)<amount then out[#out+1]={resource=resource,amount=amount-(delivered[resource] or 0)} end
  end
 end
 return out
end
function S.siteClear(w,gx,gy,kind)
 if w.structures[W.slot(w,gx,gy)] then return false,'Structure already present' end
 local x1,y1,x2,y2=W.rect(gx,gy)
 for y=y1,y2 do for x=x1,x2 do
  if W.get(w,x,y)~=M.AIR then return false,'Site occupied: dig or drain it first' end
 end end
 if kind=='wall' or kind=='platform' then
  for _,a in ipairs(w.workers) do if a.alive and a.x<=x2 and a.x+1>=x1 and a.y>=y1 and a.y-2<=y2 then
   return false,'Worker occupies the site'
  end end
  for _,p in ipairs(w.items) do if p.n>0 and p.x>=x1 and p.x<=x2 and p.y>=y1 and p.y<=y2 then
   return false,'Haul loose items off the site'
  end end
 end
 if kind~='ladder' and kind~='wall' and kind~='platform' then
  for x=x1,x2 do if not W.solid(w,x,y2+1) then return false,'Requires solid support' end end
 end
 return true
end
function S.install(w,gx,gy,kind)
 local s={id=W.id(w),gx=gx,gy=gy,kind=kind,enabled=true,growth=0,tank=0,status='Ready'}
 if kind=='pump' then
  s.intake={x=gx*4-6,y=gy*4+2}
  s.outlet={x=gx*4+6,y=gy*4-5}
 end
 w.structures[W.slot(w,gx,gy)]=s
 if kind=='field_school' then require('src.education').install(w,s) end
 w.navRevision=w.navRevision+1
 return s
end
function S.wet(w,s)
 local x1,y1,x2,y2=W.rect(s.gx,s.gy); local n=0
 for y=y1,y2 do for x=x1,x2 do if W.get(w,x,y)==M.WATER then n=n+1 end end end
 return n
end
function S.pumpReady(w,s)
 if not s.enabled then return false,'Disabled' end
 if not W.supportedStructure(w,s) then return false,'No support' end
 if W.get(w,s.intake.x,s.intake.y)~=M.WATER then return false,'Intake dry' end
 if W.get(w,s.outlet.x,s.outlet.y)~=M.AIR or W.blocked(w,s.outlet.x,s.outlet.y) then return false,'Outlet blocked' end
 return true
end
function S.pump(w,s)
 if not S.pumpReady(w,s) then return false end
 W.put(w,s.intake.x,s.intake.y,M.AIR); W.put(w,s.outlet.x,s.outlet.y,M.WATER)
 return true
end
function S.step(w)
 if w.tick%10~=0 then return end
 for slot=1,w.cols*w.rows do
  local s=w.structures[slot]
  if s then
   if not W.supportedStructure(w,s) then s.status='Unsupported'
   elseif s.kind=='farm' then
    if S.wet(w,s)>=4 then s.growth=math.max(0,s.growth-25); s.status='Flooded: crop rotting'
    elseif s.growth>=w.rules.cropTicks then s.status='Ready to harvest'
    elseif s.tank<=0 then s.status='Needs irrigation'
    else
     s.growth=s.growth+10; s.status='Growing'
     if s.growth%100==0 then s.tank=s.tank-1; w.ledger.waterUsed=w.ledger.waterUsed+1 end
    end
   elseif s.kind=='ward' then
    if not s.enabled then s.status='Disabled'
    elseif s.tank<=0 then s.status='Needs irrigation'
    elseif S.wet(w,s)>=4 then s.status='Flooded: ward inactive'
    else
     s.status='Dampening hostile resonance'
     if w.tick%120==0 then s.tank=s.tank-1;w.ledger.waterUsed=w.ledger.waterUsed+1 end
    end
   elseif s.kind=='charge' then s.status=s.fuseAt and ('ARMED / '..math.max(0,s.fuseAt-w.tick)..' ticks') or 'Inert: T orders worker arming'
   elseif s.kind=='pump' then
    local ok,reason=S.pumpReady(w,s); s.status=ok and 'Needs an operator' or reason
   else s.status='Ready' end
  end
 end
end
return S
