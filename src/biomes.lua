-- Geological regions are immutable location metadata, not mobile material.
-- They control GENERATION and presentation, not hidden temperature/plant rules.
local N=require('src.noise')
local R=require('src.random')
local U=require('src.util')
local B={keys={'rime','dunes','shale','loam','aquifer','iron','basalt','vault','mycelium','glass','ossuary','vent'}}
B.def={
 [0]={key='unclassified',name='Unclassified / legacy',color={0.34,0.34,0.35},note='Original expedition; no biome field.'},
 [1]={key='rime',name='Rime galleries',color={0.51,0.68,0.76},note='Ice lenses; little accessible liquid water.'},
 [2]={key='dunes',name='Buried dunes',color={0.64,0.43,0.23},note='Loose sand deposits; shifting excavation faces.'},
 [3]={key='shale',name='Shale shelves',color={0.40,0.40,0.49},note='Layered rock and shafts; sparse supplies.'},
 [4]={key='loam',name='Loam hollows',color={0.36,0.43,0.28},note='Loose soil and shallow water pockets.'},
 [5]={key='aquifer',name='Drowned limestone',color={0.28,0.53,0.57},note='Large finite reservoirs; flood risk when breached.'},
 [6]={key='iron',name='Ferric seams',color={0.58,0.32,0.25},note='Ore-rich veins with narrow excavation routes.'},
 [7]={key='basalt',name='Basalt deeps',color={0.43,0.30,0.40},note='Lava pockets; contact reactions and lethal exposure.'},
 [8]={key='vault',name='Pale vaults',color={0.58,0.55,0.44},note='Tall chambers and pillars; dangerous vertical drops.'},
}
B.def[9]={key='mycelium',name='Mycelial folds',color={0.39,0.53,0.38},note='Root passages, water-holding loam and wild growth.'}
B.def[10]={key='glass',name='Glass fractures',color={0.43,0.66,0.73},note='Angular mineral seams, ice lenses and glass reeds.'}
B.def[11]={key='ossuary',name='Buried ossuary',color={0.65,0.57,0.48},note='Maze-like deposits around sealed ancient rooms.'}
B.def[12]={key='vent',name='Breathing faults',color={0.69,0.39,0.38},note='Faults, lava and finite water. Steam is a contact hazard.'}
B.count=12
B.byKey={} ;for id,d in pairs(B.def) do B.byKey[d.key]=id end
B.climates={'balanced','arid','frozen','volcanic','overgrown','ruined','abyssal'}
function B.get(w,x,y)
 if not w.biomes or x<1 or y<1 or x>w.width or y>w.height then return B.def[0],0 end
 local id=w.biomes[(y-1)*w.width+x] or 0
 return B.def[id],id
end
function B.generate(seed,width,height,climate,scale)
 climate=climate or 'balanced';scale=scale or 1
 local known=false;for _,c in ipairs(B.climates) do if c==climate then known=true end end
 assert(known,'Unknown climate profile')
 local nx=U.clamp(math.floor(4/scale+0.5),3,6);local ny=3
 local rows={{1,2,3,8},{4,5,6,3},{7,8,6,5}}
 if climate=='arid' then rows={{2,2,3,1},{2,6,4,8},{7,3,6,5}}
 elseif climate=='frozen' then rows={{1,1,8,3},{1,5,4,6},{8,7,6,1}}
 elseif climate=='volcanic' then rows={{2,3,6,1},{7,5,6,8},{7,7,6,8}} end
 local sites={};local rnd=R.new((seed+44017)%2147483647)
 for by=1,ny do
  local shift=math.floor(rnd()*4)
  for bx=1,nx do
   sites[#sites+1]={x=(bx-0.5+(rnd()-0.5)*0.5)*width/nx,
    y=(by-0.5+(rnd()-0.5)*0.40)*height/ny,id=rows[by][(bx-1+shift)%4+1]}
  end
 end
 local field={}
 for y=1,height do for x=1,width do
  local wx=x+(N.value(seed+1387,x/35,y/29)-0.5)*width/nx*0.38
  local wy=y+(N.value(seed+9167,x/29,y/37)-0.5)*height/ny*0.38
  local best,id=math.huge,0
  for _,site in ipairs(sites) do
   local d=((wx-site.x)/(width/nx))^2+((wy-site.y)/(height/ny))^2
   if d<best then best,id=d,site.id end
  end
  field[(y-1)*width+x]=id
 end end
 return field
end
function B.generateLiving(seed,width,height,climate,scale)
 local baseProfile=({overgrown='balanced',ruined='arid',abyssal='volcanic'})[climate] or climate
 local a=B.generate(seed,width,height,baseProfile,scale)
 for y=1,height do for x=1,width do
  local i=(y-1)*width+x;local v=N.value(seed+87713,x/(31*scale),y/(25*scale))
  if climate=='overgrown' and v>0.43 then a[i]=9
  elseif climate=='ruined' and v>0.44 then a[i]=11
  elseif climate=='abyssal' and v>0.48 then a[i]=y>height*0.50 and 12 or 10
  elseif v>0.64 then a[i]=({9,10,11,12})[1+math.floor(R.hash(seed+171,math.floor(x/35),math.floor(y/29))*4)] end
 end end
 return a
end
return B
