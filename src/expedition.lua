-- The same starting colony is placed on generated and imported terrain.
-- A map never supplies workers, needs, inventory, jobs or executable rules.
local W=require('src.world')
local M=require('src.materials')
local C=require('config')
local U=require('src.util')
local E={}
function E.validateStart(w,home,crew)
 assert(type(home)=='table','Map needs an arrival marker')
 local left=home.left or 9
 U.integer(left,'arrival left',5,w.width-64)
 U.integer(home.right,'arrival right',left+39,w.width-4)
 U.integer(home.floor,'arrival floor',25,w.height-6)
 assert((left-1)%4==0 and home.right%4==0 and (home.floor-1)%4==0,'Arrival must align to the 4-cell building grid')
 local starts={};for n=1,(crew or 3) do starts[#starts+1]=left+(crew==9 and 9+(n-1)*4 or 11+(n-1)*5) end
 for _,x in ipairs(starts) do
  for yy=home.floor-3,home.floor-1 do for xx=x,x+1 do assert(W.get(w,xx,yy)==M.AIR,'Arrival worker footprint is not empty') end end
  assert(M.def[W.get(w,x,home.floor)].solid and M.def[W.get(w,x+1,home.floor)].solid,'Arrival workers need initial footing')
 end
 for x=left+4,left+8 do
  for y=home.floor-4,home.floor-1 do assert(W.get(w,x,y)==M.AIR,'Starting stockpile footprint is not empty') end
  assert(M.def[W.get(w,x,home.floor)].solid,'Starting supplies need initial footing')
 end
 return true
end
function E.populate(w,home,crew)
 crew=crew or 3
 E.validateStart(w,home,crew)
 assert(#w.workers==0 and #w.items==0 and next(w.structures)==nil,'Arrival can only populate a fresh world')
 local left=home.left or 9;local floorY=home.floor
 w.home={left=left,x=left+7,y=floorY-1,right=home.right,floor=floorY}
 local gx,gy=W.tile(w,left+5,floorY-1)
 local s={id=W.id(w),kind='store',gx=gx,gy=gy,enabled=true}
 w.structures[W.slot(w,gx,gy)]=s
 local x,y=left+5,floorY-1
 W.stack(w,'stone',80,x,y);W.stack(w,'soil',32,x+1,y)
 W.stack(w,'metal',16,x+2,y);W.stack(w,'food',C.startFood+(crew-3)*4,x+3,y)
 for i=1,crew do
  local name=({'Mara','Oren','Ivo','Sen','Kest','Asha','Tovin','Neris','Vale'})[i]
  w.workers[i]={id=W.id(w),name=name,x=left+(crew==9 and 9+(i-1)*4 or 11+(i-1)*5),y=y,alive=true,
   hp=100,hunger=18+i*3,fatigue=12+i*4,breath=100,mine=({1.4,1,0.9})[(i-1)%3+1],build=({0.9,1.4,1})[(i-1)%3+1],
   status='Awaiting work',reason='',fall=0,worked=false,job=nil,carry=nil,thinkAt=0,progress=0}
 end
 w.navRevision=0
 W.event(w,'arrival',(crew==3 and 'Three settlers' or tostring(crew)..' settlers')..' enter a finite underground frontier. There are no reinforcements.')
 W.event(w,'warning','Survey all directions. Biomes contain different materials; no safe route or recovery is promised.')
end
function E.stamp(w,crew)
 local floorY=math.floor(w.height*0.46/4)*4+1
 local left=math.floor((w.width/2-32)/4)*4+1;local right=left+63
 local function fill(x1,y1,x2,y2,m)
  for y=math.max(3,y1),math.min(w.height-2,y2) do for x=math.max(3,x1),math.min(w.width-2,x2) do W.put(w,x,y,m) end end
 end
 fill(left-4,floorY-24,right+4,floorY+5,M.ROCK)
 fill(left,floorY-20,right,floorY-1,M.AIR)
 local well=right-15
 fill(well-1,floorY,well+12,floorY+2,M.ROCK)
 fill(well,floorY,well+11,floorY+1,M.WATER)
 -- A small starter well, never an infinite source. The surrounding geology is not repaired.
 local home={left=left,right=right,floor=floorY}
 E.populate(w,home,crew)
end
return E
