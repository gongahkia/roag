-- Descriptors, not a quality score or proof of colonist reachability.
local M=require('src.materials')
local B=require('src.biomes')
local U=require('src.util')
local Rep={}
function Rep.measure(w)
 local r={width=w.width,height=w.height,seed=w.seed,biomeCount=0,airComponents=0,
  largestAir=0,air=0,water=0,lava=0,ice=0,ore=0,loose=0,horizontalTransitions=0,verticalTransitions=0,biomes={}}
 local material={};for id=0,9 do material[id]=0 end
 for i=1,w.n do
  material[w.mat[i]]=material[w.mat[i]]+1
  local b=w.biomes and w.biomes[i] or 0;r.biomes[b]=(r.biomes[b] or 0)+1
  if i%w.width~=0 and w.biomes and b~=w.biomes[i+1] then r.horizontalTransitions=r.horizontalTransitions+1 end
  if i<=w.n-w.width and w.biomes and b~=w.biomes[i+w.width] then r.verticalTransitions=r.verticalTransitions+1 end
 end
 for b in pairs(r.biomes) do if b~=0 then r.biomeCount=r.biomeCount+1 end end
 r.air,r.water,r.lava,r.ice,r.ore=material[M.AIR],material[M.WATER],material[M.LAVA],material[M.ICE],material[M.ORE]
 r.loose=material[M.SAND]+material[M.SOIL];r.materialCounts=material
 local seen={};local queue={}
 for i=1,w.n do if w.mat[i]==M.AIR and not seen[i] then
  r.airComponents=r.airComponents+1;queue[1]=i;local head,tail=1,1;seen[i]=true
  while head<=tail do
   local at=queue[head];head=head+1;local x=(at-1)%w.width+1
   local function visit(k) if k>=1 and k<=w.n and not seen[k] and w.mat[k]==M.AIR then seen[k]=true;tail=tail+1;queue[tail]=k end end
   if x>1 then visit(at-1) end;if x<w.width then visit(at+1) end;visit(at-w.width);visit(at+w.width)
  end
  r.largestAir=math.max(r.largestAir,tail)
 end end
 r.airFraction=r.air/w.n;r.largestAirFraction=r.air>0 and r.largestAir/r.air or 0
 return r
end
function Rep.csv(r)
 local lines={U.csv({'metric','value'})}
 for _,k in ipairs(U.keys(r)) do if type(r[k])~='table' then lines[#lines+1]=U.csv({k,r[k]}) end end
 for id=0,9 do lines[#lines+1]=U.csv({'material_'..id,r.materialCounts[id]}) end
 for id=0,require('src.biomes').count do lines[#lines+1]=U.csv({'biome_'..B.def[id].key,r.biomes[id] or 0}) end
 return table.concat(lines,'\n')..'\n'
end
function Rep.compare(a,b)
 assert(a.width==b.width and a.height==b.height,'Map dimensions must match for cell comparison')
 local material,biome=0,0
 for i=1,a.n do if a.mat[i]~=b.mat[i] then material=material+1 end
  if (a.biomes and a.biomes[i] or 0)~=(b.biomes and b.biomes[i] or 0) then biome=biome+1 end
 end
 return {changedMaterial=material,changedBiome=biome,totalCells=a.n,materialFraction=material/a.n,biomeFraction=biome/a.n}
end
return Rep
