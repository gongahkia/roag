local W=require('src.world')
local U=require('src.util')
local M=require('src.materials')
local N=require('src.noise')
local R=require('src.random')
local B=require('src.biomes')
local L=require('src.generation.layouts')
local E=require('src.expedition')
local F={version='frontier-v2'}
function F.options(o)
 o=o or {};local v={layout=o.layout or 'hybrid',climate=o.climate or 'balanced',
  openness=o.openness or 0.48,biomeScale=o.biomeScale or 1,features=o.features or 'living',density=o.density or 1,crew=o.crew or 3}
 assert(v.features=='living' or v.features=='ruins' or v.features=='none','Features must be living, ruins or none')
 assert(v.crew==3 or v.crew==6 or v.crew==9,'Crew must be 3, 6 or 9')
 assert(U.finite(v.density) and v.density>=0.5 and v.density<=1.5,'Density must be 0.5..1.5')
 assert(L.descriptions[v.layout],'Unknown layout technique')
 local known=false;for _,c in ipairs(B.climates) do if c==v.climate then known=true end end
 assert(known,'Unknown biome climate profile')
 assert(U.finite(v.openness) and v.openness>=0.25 and v.openness<=0.70,'Openness must be 0.25..0.70')
 assert(U.finite(v.biomeScale) and v.biomeScale>=0.65 and v.biomeScale<=1.5,'Biome scale must be 0.65..1.5')
 for k in pairs(o) do assert(v[k]~=nil,'Unknown generation option: '..tostring(k)) end
 return v
end
local function geology(w,open)
 local seed=w.seed
 for y=3,w.height-2 do for x=3,w.width-2 do
  local i=W.index(w,x,y);local b=w.biomes[i];local m=M.ROCK
  local layer=N.value(seed+5549,x/20,y/8)
  local vein=math.abs(N.fbm(seed+3847,x/28,y/11,2)-0.5)
  if open[i] then m=M.AIR
  elseif b==1 and layer>0.50 then m=M.ICE
  elseif b==2 and layer>0.40 then m=M.SAND
  elseif (b==4 or b==9) and layer>0.40 then m=M.SOIL
  elseif b==10 and layer>0.67 then m=M.ICE
  elseif vein<((b==6 or b==10) and 0.09 or 0.015) then m=M.ORE
  elseif b==11 and (y%9)<2 then m=M.ORE
  elseif b==5 and layer>0.78 then m=M.SOIL end
  W.put(w,x,y,m)
 end end
end
local function pockets(w)
 local rnd=R.new((w.seed+66203)%2147483647)
 for _=1,math.max(8,math.floor(w.n/1600)) do
  local cx=8+math.floor(rnd()*(w.width-16));local cy=8+math.floor(rnd()*(w.height-16))
  local b=w.biomes[W.index(w,cx,cy)]
  local rx,ry=4+math.floor(rnd()*7),3+math.floor(rnd()*6)
  local fill=M.AIR
  if b==1 then fill=M.ICE elseif b==2 then fill=M.SAND elseif b==4 then fill=M.WATER
  elseif b==5 then fill=M.WATER;rx=rx+4;ry=ry+2 elseif b==7 or b==12 then fill=M.LAVA elseif b==9 then fill=M.WATER elseif b==10 then fill=M.ICE end
  local full=(b==5 and rnd()<0.50)
  for y=math.max(3,cy-ry-2),math.min(w.height-2,cy+ry+2) do
   for x=math.max(3,cx-rx-2),math.min(w.width-2,cx+rx+2) do
    local d=((x-cx)/rx)^2+((y-cy)/ry)^2
    if d<1 then W.put(w,x,y,(full or y>cy+ry*0.15) and fill or M.AIR)
    elseif d<1.4 and fill~=M.AIR then W.put(w,x,y,M.ROCK) end
   end
  end
 end
 -- Roof icicles and loose floor lenses add small-scale silhouette variation.
 for y=4,w.height-5 do for x=4,w.width-4 do
  if W.get(w,x,y)==M.AIR and W.get(w,x,y-1)==M.ICE and R.hash(w.seed+221,x,y)<0.11 then W.put(w,x,y,M.ICE) end
 end end
end
local function make(seed,mode,width,height,options,populated)
 local o=F.options(options)
 local w=W.new(width,height,seed,'frontier',mode)
 assert(width>=128 and height>=80,'Frontier generation needs at least 128 x 80 cells')
 w.biomes=B.generateLiving(seed,width,height,o.climate,o.biomeScale)
 local open=L.make(o.layout,seed,width,height,o.openness,w.biomes)
 geology(w,open);pockets(w)
 for y=1,height do for x=1,width do
  if x<=2 or x>=width-1 or y<=2 or y>=height-1 then W.put(w,x,y,M.BEDROCK) end
 end end
 local features=require('src.generation.wonders').place(w,o)
 if populated then E.stamp(w,o.crew) else E.unpopulated(w) end
 require('src.content').install(w,features)
 w.generation={version=F.version,layout=o.layout,climate=o.climate,openness=o.openness,biomeScale=o.biomeScale,features=o.features,density=o.density,crew=o.crew}
 w.mapTitle='Frontier / '..o.layout..' / '..o.climate..' / '..seed
 return w
end
function F.make(seed,mode,width,height,options)
 return make(seed,mode,width,height,options,true)
end
function F.makeUnpopulated(seed,mode,width,height,options)
 return make(seed,mode,width,height,options,false)
end
return F
