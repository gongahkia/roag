local W=require('src.world')
local M=require('src.materials')
local C=require('config')
local B=require('src.body')
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
 torch={label='Torch',cost=1,resource='metal',work=12},
 tool_bench={label='Tool bench',cost=6,resource='stone',materials={stone=4,metal=2},work=60},
 solar_array={label='Small solar array',cost=7,resource='metal',materials={stone=2,metal=4,component=1},work=80,width=2,solid=true,industry=true},
 power_pole={label='Power pole',cost=1,resource='metal',work=20,industry=true},
 battery={label='Battery',cost=10,resource='metal',materials={stone=2,metal=4,component=2},work=70,solid=true,industry=true},
 fabricator={label='Fabricator',cost=12,resource='metal',materials={stone=4,metal=4,component=2},work=100,width=2,solid=true,industry=true},
 mining_rig={label='Mining rig',cost=12,resource='metal',materials={stone=4,metal=4,component=2},work=100,width=2,solid=true,industry=true},
 industrial_bin={label='Industrial bin',cost=5,resource='metal',materials={stone=2,metal=2,component=1},work=65,solid=true,industry=true},
 conveyor={label='Conveyor',cost=1,resource='metal',work=20,industry=true},
 electric_lamp={label='Electric lamp',cost=2,resource='metal',materials={metal=1,component=1},work=35,industry=true},
 environmental_regulator={label='Environmental regulator',cost=5,resource='metal',materials={metal=2,component=1},work=60,industry=true,environments=true},
 relic_analyzer={label='Relic Analyzer',cost=6,resource='metal',materials={metal=2,component=2},work=80,industry=true,relics=true},
 -- Ancient caches are generated G08 entities. They have no build recipe and
 -- therefore cannot be conjured through ordinary construction commands.
 ancient_cache={label='Ancient Cache',cost=0,resource='stone',work=1,ancient=true},
 signal_relay={label='Signal relay',cost=5,resource='metal',materials={stone=2,metal=2,component=1},work=60,industry=true,factions=true},
 trade_depot={label='Trade depot',cost=10,resource='metal',materials={stone=4,metal=4,component=2},work=90,width=2,solid=true,factions=true},
 training_target={label='Training target',cost=2,resource='stone',materials={stone=2,metal=1},work=35,security=true},
 -- A barricade stays walkable; security's fine-cell ray test gives its lower
 -- half physical cover without introducing a percentage cover roll.
 barricade={label='Barricade',cost=2,resource='stone',work=24,security=true},
}
function S.width(kind) return assert(S.def[kind],'Unknown structure').width or 1 end
function S.footprint(s)
 local x1,y1,_,y2=W.rect(s.gx,s.gy)
 return x1,y1,(s.gx+(s.width or S.width(s.kind))-1)*4,y2
end
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
  for _,resource in ipairs({'stone','soil','metal','component','food','water'}) do
   local amount=materials[resource];if amount and (delivered[resource] or 0)<amount then out[#out+1]={resource=resource,amount=amount-(delivered[resource] or 0)} end
  end
 end
 return out
end
-- Torches can stand on a floor or mount to a solid face behind their build
-- block.  This is deliberately a placement/support query rather than saved
-- orientation state: if the backing wall is demolished, the installed torch
-- loses support through the same structure lifecycle as every other build.
function S.torchMount(w,gx,gy)
 local x1,y1,x2,y2=W.rect(gx,gy)
 local floor=true
 for x=x1,x2 do if not W.solid(w,x,y2+1) then floor=false;break end end
 if floor then return 'floor' end
 local function face(x,slot)
  local wall=slot and w.structures[slot]
  if wall and wall.kind=='wall' then return true end
  if x<1 or x>w.width then return false end
  for y=y1,y2 do if not W.solid(w,x,y) then return false end end
  return true
 end
 if face(x1-1,gx>1 and W.slot(w,gx-1,gy) or nil) then return 'left' end
 if face(x2+1,gx<w.cols and W.slot(w,gx+1,gy) or nil) then return 'right' end
 return nil,'Requires a solid floor or wall face'
end
function S.supported(w,s)
 if s.kind=='torch' then return S.torchMount(w,s.gx,s.gy)~=nil end
 return W.supportedStructure(w,s)
end
function S.siteClear(w,gx,gy,kind)
 local width=S.width(kind)
 if gx+width-1>w.cols then return false,'Structure footprint exceeds map bounds' end
 local x1,y1,_,y2=W.rect(gx,gy);local x2=(gx+width-1)*4
 for blockX=gx,gx+width-1 do if w.structures[W.slot(w,blockX,gy)] then return false,'Structure already present' end end
 for blockX=gx,gx+width-1 do
  local occupied=W.structureAt(w,(blockX-1)*4+1,y1)
  if occupied then return false,'Structure footprint overlaps '..S.def[occupied.kind].label end
 end
 for y=y1,y2 do for x=x1,x2 do
  if W.get(w,x,y)~=M.AIR then return false,'Site occupied: dig or drain it first' end
 end end
 if kind=='wall' or kind=='platform' then
  for _,a in ipairs(w.workers) do local ax1,ay1,ax2,ay2=B.rect(w,a.x,a.y);if a.alive and ax1<=x2 and ax2>=x1 and ay2>=y1 and ay1<=y2 then
   return false,'Worker occupies the site'
  end end
  for _,p in ipairs(w.items) do if p.n>0 and p.x>=x1 and p.x<=x2 and p.y>=y1 and p.y<=y2 then
   return false,'Haul loose items off the site'
  end end
 end
 if kind=='torch' then
  local mount,reason=S.torchMount(w,gx,gy)
  if not mount then return false,reason end
 elseif kind~='ladder' and kind~='wall' and kind~='platform' and kind~='power_pole' and kind~='electric_lamp' and kind~='signal_relay' then
  for x=x1,x2 do if not W.solid(w,x,y2+1) then return false,'Requires solid support' end end
 end
 return true
end
function S.install(w,gx,gy,kind)
 local def=assert(S.def[kind],'Unknown structure')
 local s={id=W.id(w),gx=gx,gy=gy,kind=kind,enabled=true,growth=0,tank=0,status='Ready'}
 if def.industry then s.width=def.width or 1;s.solid=def.solid or false end
 if kind=='pump' then
  s.intake={x=gx*4-6,y=gy*4+2}
  s.outlet={x=gx*4+6,y=gy*4-5}
 end
 w.structures[W.slot(w,gx,gy)]=s
 if kind=='field_school' then require('src.education').install(w,s) end
 if def.industry then require('src.industry').install(w,s) end
 if def.factions then require('src.factions').install(w,s) end
 if def.relics then s.relic={slots={0,0},task=nil} end
 w.navRevision=w.navRevision+1
 if w.industry then w.industry.topologyRevision=w.industry.topologyRevision+1 end
 return s
end
function S.torchCount(w)
 local count=0;for _,s in pairs(w.structures) do if s.kind=='torch' then count=count+1 end end;return count
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
   if not S.supported(w,s) then s.status='Unsupported'
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
   elseif s.kind=='tool_bench' then
    local f=s.fabrication
    local label=f and (f.kind=='pickaxe' and 'pickaxe' or f.kind=='rope_coil' and 'rope coil' or 'machine component')
    s.status=f and ('Fabricating '..label..' '..f.progress..'/'..f.work) or 'Ready for tool fabrication'
   elseif S.def[s.kind].ancient then s.status=s.enabled and 'Ancient cache — requires excavation' or 'Excavated ancient cache'
   elseif S.def[s.kind].industry then
    -- The industrial phase gives a more precise live status after power and
    -- logistics have been resolved. Keep this fallback for an unpowered tick.
    s.status=s.status or 'Industrial structure ready'
   elseif s.kind=='pump' then
    local ok,reason=S.pumpReady(w,s); s.status=ok and 'Needs an operator' or reason
   else s.status='Ready' end
  end
 end
end
return S
