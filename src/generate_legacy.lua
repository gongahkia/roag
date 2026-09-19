local W=require('src.world')
local N=require('src.noise')
local R=require('src.random')
local M=require('src.materials')
local C=require('config')
local G={presets={'cistern','dunes','frost'}}
local function fill(w,x1,y1,x2,y2,m)
 for y=math.max(2,y1),math.min(w.height-1,y2) do
  for x=math.max(2,x1),math.min(w.width-1,x2) do W.put(w,x,y,m) end
 end
end
function G.make(seed,preset,mode,width,height)
 preset=preset or C.preset
 assert(preset=='cistern' or preset=='dunes' or preset=='frost','Unknown terrain preset')
 local w=W.new(width or C.width,height or C.height,seed,preset,mode or C.mode)
 assert(w.width>=128 and w.height>=80,'Generated scenarios require at least 128 x 80 cells')
 local h,ww=w.height,w.width
 for y=1,h do for x=1,ww do
  local m=M.ROCK
  local v=N.fbm(seed,x/25,y/18,3)
  if v>0.53 and y>8 then m=M.AIR end
  if v<0.3 and R.hash(seed,x,y)>0.73 then m=M.ORE end
  if preset=='dunes' and y<18+math.sin(x/14)*4 then m=y<9 and M.AIR or M.SAND end
  if preset=='frost' and y<12 and x>ww*0.5 then m=M.ICE end
  if x<=2 or x>=ww-1 or y<=2 or y>=h-1 then m=M.BEDROCK end
  W.put(w,x,y,m)
 end end
 -- Seeded cavern shapes; their geology is independent of colony jobs.
 local rnd=R.new((seed+117)%2147483647)
 for k=1,12 do
  local cx=math.floor(ww*(0.3+rnd()*0.6))
  local cy=math.floor(h*(0.45+rnd()*0.4))
  local rx,ry=5+math.floor(rnd()*12),3+math.floor(rnd()*7)
  for y=cy-ry,cy+ry do for x=cx-rx,cx+rx do
   if ((x-cx)/rx)^2+((y-cy)/ry)^2<1 and W.inside(w,x,y) and x>2 and x<ww-1 and y<h-1 then
    W.put(w,x,y,y>cy+ry-3 and (k%4==0 and M.LAVA or M.WATER) or M.AIR)
   end
  end end
 end
 -- A survivable arrival chamber, NOT an indestructible refuge or recovery system.
 local floorY=math.floor(h*0.36/4)*4+1
 local right=math.floor(ww*0.42/4)*4
 w.home={x=16,y=floorY-1,right=right,floor=floorY}
 fill(w,5,floorY-24,right+4,floorY+5,M.ROCK)
 fill(w,9,floorY-20,right,floorY-1,M.AIR)
 -- An exposed finite well. It can be drained, blocked or undermined.
 local well=right-15
 fill(w,well-1,floorY,well+12,floorY+2,M.ROCK)
 fill(w,well,floorY,well+11,floorY+1,M.WATER)
 -- A thin-walled reservoir beside the arrival chamber: inspect before excavating.
 local rx=right+7
 fill(w,rx-2,floorY-17,rx+25,floorY+1,M.ROCK)
 fill(w,rx,floorY-15,rx+23,floorY-2,preset=='frost' and M.ICE or M.WATER)
 -- Loose overburden above a separate mine seam.
 fill(w,right+10,floorY+12,right+25,floorY+17,M.SAND)
 fill(w,right+10,floorY+18,right+25,floorY+22,M.AIR)
 local row=(floorY-1)/4
 local s={id=W.id(w),kind='store',gx=4,gy=row,enabled=true}
 w.structures[W.slot(w,4,row)]=s
 local x,y=14,floorY-1
 W.stack(w,'stone',80,x,y); W.stack(w,'soil',32,x+1,y)
 W.stack(w,'metal',16,x+2,y); W.stack(w,'food',C.startFood,x+3,y)
 local names={'Mara','Oren','Ivo'}
 for i=1,3 do
  w.workers[i]={id=W.id(w),name=names[i],x=20+(i-1)*5,y=y,
   alive=true,hp=100,hunger=18+i*3,fatigue=12+i*4,breath=100,
   mine=({1.4,1.0,0.9})[i],build=({0.9,1.4,1.0})[i],
   status='Awaiting work',reason='',fall=0,worked=false,job=nil,carry=nil,
   thinkAt=0,progress=0}
 end
 w.navRevision=0
 W.event(w,'arrival','Three settlers enter the abandoned cistern. There are no reinforcements.')
 W.event(w,'warning','Finite supplies. Build food production; inspect the reservoir before digging.')
 return w
end
return G
