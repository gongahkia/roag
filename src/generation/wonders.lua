-- Authored rule sets + seeded room assembly. No guaranteed connected route to loot.
local W=require('src.world')
local M=require('src.materials')
local R=require('src.random')
local Cat=require('src.catalog')
local G={}
function G.place(w,o)
 local t={version=1,crew=o.crew,flora={},fauna={},sites={},ruins={}}
 if o.features=='none' then return t end
 local rnd=R.new((w.seed+663817)%2147483647)
 local function pick(list)return list[1+math.floor(rnd()*#list)] end
 local function int(a,b)return a+math.floor(rnd()*(b-a+1)) end
 local floor=math.floor(w.height*0.46/4)*4+1
 local left=math.floor((w.width/2-32)/4)*4+1
 local protect={x1=left-10,x2=left+73,y1=floor-30,y2=floor+11}
 local function overlaps(a,b)return a.x1<=b.x2 and a.x2>=b.x1 and a.y1<=b.y2 and a.y2>=b.y1 end
 local function validRect(r)
  if overlaps(r,protect) then return false end
  for _,other in ipairs(t.ruins) do
   if overlaps(r,{x1=other.x1-4,y1=other.y1-4,x2=other.x2+4,y2=other.y2+4}) then return false end
  end
  return true
 end
 local function fill(x1,y1,x2,y2,m)
  for y=y1,y2 do for x=x1,x2 do W.put(w,x,y,m) end end
 end
 local function site(kind,x,y,stock)
  t.sites[#t.sites+1]={kind=kind,x=x,y=y,stock=stock or {},phase=int(0,3599)}
 end
 local function flora(kind,x,y)
  t.flora[#t.flora+1]={kind=kind,x=x,y=y,food=int(2,5),water=int(0,2),phase=int(0,3599)}
 end
 local function creature(kind,x,y)
  local d=Cat.fauna[kind]
  for yy=y-d.height+1,y do for xx=x,x+d.width-1 do
   local m=W.get(w,xx,yy)
   if (kind=='leech' and m~=M.WATER) or (kind~='leech' and m~=M.AIR) then return false end
  end end
  t.fauna[#t.fauna+1]={kind=kind,x=x,y=y,food=kind=='sentinel' and 0 or int(6,12),phase=int(0,3599)};return true
 end
 local target=math.min(12,math.max(2,math.floor(w.n/5500*o.density)))
 for _=1,target*35 do
  if #t.ruins>=target then break end
  local rw,rh=int(25,39),int(14,22)
  local x,y=int(5,w.width-rw-5),int(5,w.height-rh-5)
  local kind=pick({'cistern','ossuary','archive','forge','nursery'})
  local r={kind=kind,x1=x,y1=y,x2=x+rw,y2=y+rh,
   name=pick({'Silent','Sunken','Hollow','Seventh','Blind','Fractured'})..' '..pick({'Annex','Reliquary','Engine','Sanctum','Repository','Gate'})}
  if validRect(r) then
   t.ruins[#t.ruins+1]=r
   fill(x,y,r.x2,r.y2,M.ROCK);fill(x+2,y+2,r.x2-2,r.y2-2,M.AIR)
   local mid=math.floor((x+r.x2)/2);local fy=r.y2-1
   -- Split rooms, connect via a low sealed aperture. All walls can be mined.
   fill(mid,y+2,mid+1,fy-4,M.ROCK)
   if kind=='cistern' then
    fill(mid+3,fy-4,r.x2-2,fy-1,M.WATER)
    fill(mid+2,fy-5,mid+2,fy,M.ROCK)
    site('cache',x+5,fy-1,{metal=int(5,11),food=int(2,4)})
   elseif kind=='ossuary' then
    for px=x+5,r.x2-4,7 do fill(px,y+3,px+1,fy-5,M.ORE) end
    site('resonator',mid+5,fy-1);site('cache',x+4,fy-1,{stone=int(12,24),metal=int(4,10)})
   elseif kind=='archive' then
    fill(x+3,y+7,mid-2,y+8,M.ROCK)
    site('cache',mid+5,fy-1,{metal=int(8,18),food=int(4,7)})
   elseif kind=='forge' then
    fill(mid+4,fy-3,r.x2-3,fy-1,M.LAVA)
    fill(mid+3,fy-4,mid+3,fy,M.ROCK)
    site('vent',x+6,fy-1);site('cache',x+3,fy-1,{metal=int(12,22)})
   else
    fill(x+2,fy,r.x2-2,fy,M.SOIL)
    site('nursery',mid+5,fy-1,{water=int(3,6),food=8})
    site('cache',x+5,fy-1,{soil=int(12,20),food=int(3,5)})
   end
   if o.features=='living' then
    if kind=='nursery' then flora('veil',x+8,fy-1);creature('grazer',x+11,fy-1)
    else creature('sentinel',mid+3,fy-1) end
   end
  end
 end
 if o.features~='living' then return t end
 local function away(x,y)return not overlaps({x1=x-3,x2=x+3,y1=y-3,y2=y+3},protect) end
 local count=math.min(90,math.max(8,math.floor(w.n/1100*o.density)))
 local used={}
 for _=1,w.n/3 do
  if #t.flora>=count then break end
  local x,y=int(4,w.width-5),int(5,w.height-5);local i=W.index(w,x,y)
  if away(x,y) and not used[i] and W.get(w,x,y)==M.AIR and W.solid(w,x,y+1) then
   local biome=w.biomes and w.biomes[i] or 0
   local kind=(biome==6 or biome==7 or biome==11) and 'thorn' or (biome==1 or biome==10 or biome==12) and 'filter' or 'veil'
   flora(kind,x,y);used[i]=true
  end
 end
 local animalTarget=math.min(40,math.max(5,math.floor(w.n/2900*o.density)))
 for _=1,w.n/3 do
  if #t.fauna>=animalTarget then break end
  local x,y=int(4,w.width-6),int(5,w.height-5)
  if away(x,y) then
   if W.get(w,x,y)==M.WATER and rnd()<0.6 then creature('leech',x,y)
   elseif W.get(w,x,y)==M.AIR and W.solid(w,x,y+1) then creature(rnd()<0.7 and 'grazer' or 'stalker',x,y) end
  end
 end
 return t
end
return G
