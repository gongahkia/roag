-- Each generator produces a boolean open-space mask. No colony state is mutated.
local N=require('src.noise')
local R=require('src.random')
local U=require('src.util')
local L={names={'hybrid','noise','cellular','worms','faults','vaults','karst','labyrinth','roots','chasms','crystal'}}
L.descriptions={hybrid='Regional mixture of all five methods',noise='Warped multi-octave density cavities',
 cellular='Buffered cellular-automaton caves',worms='Correlated walkers carving branching tunnels',
 faults='Layered galleries cut by deep fissures',vaults='Elliptical halls, pillars and connecting cuts'}
L.descriptions.karst='Stacked sink basins and dissolution-like shafts'
L.descriptions.labyrinth='Depth-first maze with rooms and narrow loops'
L.descriptions.roots='Recursive root-shaped passages with bulb chambers'
L.descriptions.chasms='Wide vertical rifts, ledges and hanging islands'
L.descriptions.crystal='Angular lattice fractures and faceted pockets'
local function wallMask(w,h) local a={} for i=1,w*h do a[i]=false end return a end
local function ellipse(a,w,h,cx,cy,rx,ry,open)
 for y=math.max(3,math.floor(cy-ry)),math.min(h-2,math.ceil(cy+ry)) do
  for x=math.max(3,math.floor(cx-rx)),math.min(w-2,math.ceil(cx+rx)) do
   if ((x-cx)/rx)^2+((y-cy)/ry)^2<=1 then a[(y-1)*w+x]=open end
  end
 end
end
function L.noise(seed,w,h,o)
 local a={}
 for y=1,h do for x=1,w do
  local dx=(N.value(seed+83,x/47,y/41)-0.5)*15
  local dy=(N.value(seed+719,x/39,y/53)-0.5)*12
  a[(y-1)*w+x]=N.fbm(seed+3001,(x+dx)/23,(y+dy)/16,3)>0.76-o*0.54
 end end
 return a
end
function L.cellular(seed,w,h,o)
 local cw,ch=math.ceil(w/2),math.ceil(h/2);local a={}
 for y=1,ch do for x=1,cw do a[(y-1)*cw+x]=(x>1 and x<cw and y>1 and y<ch and R.hash(seed+9001,x,y)<0.36+o*0.31) end end
 for _=1,4 do
  local b={}
  for y=1,ch do for x=1,cw do
   local walls=0
   for dy=-1,1 do for dx=-1,1 do
    local xx,yy=x+dx,y+dy
    if xx<1 or xx>cw or yy<1 or yy>ch or not a[(yy-1)*cw+xx] then walls=walls+1 end
   end end
   b[(y-1)*cw+x]=(walls<5 and x>1 and x<cw and y>1 and y<ch)
  end end
  a=b
 end
 local full={}
 for y=1,h do for x=1,w do full[(y-1)*w+x]=a[(math.ceil(y/2)-1)*cw+math.ceil(x/2)] end end
 return full
end
function L.worms(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+62011)%2147483647)
 local count=math.floor(w*h/1200*(0.65+o))
 for _=1,count do
  local x,y=5+rnd()*(w-10),5+rnd()*(h-10)
  local angle=rnd()*math.pi*2;local steps=20+math.floor(rnd()*60)
  for step=1,steps do
   local r=(1.4+rnd()*2.8)*(0.65+o)
   ellipse(a,w,h,x,y,r*1.45,r,true)
   if step%17==0 then ellipse(a,w,h,x,y,r*2.8,r*2.1,true) end
   angle=angle+(rnd()-0.5)*1.0
   x=x+math.cos(angle)*2.2;y=y+math.sin(angle)*1.7
   if x<5 or x>w-5 then angle=math.pi-angle;x=U.clamp(x,5,w-5) end
   if y<5 or y>h-5 then angle=-angle;y=U.clamp(y,5,h-5) end
  end
 end
 return a
end
function L.faults(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+13337)%2147483647)
 local spacing=14+rnd()*7;local phase=rnd()*spacing
 local faults={}
 for _=1,math.max(2,math.floor(w/65)) do faults[#faults+1]={x=8+rnd()*(w-16),phase=rnd()*7} end
 for y=3,h-2 do for x=3,w-2 do
  local warp=(N.value(seed+8209,x/36,y/28)-0.5)*12
  local shelf=(y+warp+phase)%spacing
  local open=shelf<(3+o*8)
  for _,f in ipairs(faults) do if math.abs(x-f.x-math.sin(y/22+f.phase)*8)<1.5+o*3 then open=true end end
  a[(y-1)*w+x]=open
 end end
 return a
end
function L.vaults(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+81281)%2147483647)
 local rooms={};local count=math.max(5,math.floor(w*h/2100*(0.7+o)))
 for k=1,count do
  local cx,cy=10+rnd()*(w-20),10+rnd()*(h-20)
  local rx,ry=(6+rnd()*15)*(0.6+o),6+rnd()*17
  ellipse(a,w,h,cx,cy,rx,ry,true)
  rooms[k]={x=cx,y=cy,rx=rx,ry=ry}
  if k>1 then
   local p=rooms[k-1];local steps=math.ceil(math.max(math.abs(p.x-cx),math.abs(p.y-cy)))
   for t=0,steps do ellipse(a,w,h,cx+(p.x-cx)*t/steps,cy+(p.y-cy)*t/steps,2,2,true) end
  end
 end
 -- Rock columns remain in some halls; these are not structural-load constraints.
 for k,r in ipairs(rooms) do if k%2==0 then
  for y=math.max(3,math.floor(r.y-r.ry+2)),math.min(h-2,math.floor(r.y+r.ry)) do
   local x=math.floor(r.x+r.rx*0.25)
   for dx=0,1 do if x+dx<w-1 then a[(y-1)*w+x+dx]=false end end
  end
 end end
 return a
end

function L.karst(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+192193)%2147483647)
 for x=8,w-8,18 do
  local cx=x+(rnd()-0.5)*8;local cy=10+rnd()*12
  while cy<h-8 do
   local rx=5+rnd()*(9+o*8);local ry=3+rnd()*6
   ellipse(a,w,h,cx,cy,rx,ry,true)
   local ny=cy+10+rnd()*16;local nx=U.clamp(cx+(rnd()-0.5)*14,5,w-5)
   local steps=math.ceil(ny-cy)
   for k=0,steps do ellipse(a,w,h,cx+(nx-cx)*k/steps,cy+k,1.6+o,2,true) end
   cx,cy=nx,ny
  end
 end
 return a
end
function L.labyrinth(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+142921)%2147483647)
 local cw,ch=math.floor((w-8)/10),math.floor((h-8)/8)
 local seen,stack={[1]=true},{1}
 local function xy(i)return (i-1)%cw+1,math.floor((i-1)/cw)+1 end
 while #stack>0 do
  local at=stack[#stack];local x,y=xy(at);local choices={}
  for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}}) do
   local nx,ny=x+d[1],y+d[2];local ni=(ny-1)*cw+nx
   if nx>=1 and nx<=cw and ny>=1 and ny<=ch and not seen[ni] then choices[#choices+1]=ni end
  end
  if #choices==0 then table.remove(stack) else
   local ni=choices[1+math.floor(rnd()*#choices)];seen[ni]=true;stack[#stack+1]=ni
   local nx,ny=xy(ni);local sx,sy=4+x*10-5,4+y*8-4;local tx,ty=4+nx*10-5,4+ny*8-4
   local steps=math.max(math.abs(tx-sx),math.abs(ty-sy))
   for k=0,steps do ellipse(a,w,h,sx+(tx-sx)*k/steps,sy+(ty-sy)*k/steps,1.8+o*2,1.6+o*2,true) end
   if rnd()<o*0.5 then ellipse(a,w,h,tx,ty,5,3.5,true) end
  end
 end
 return a
end
function L.roots(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+92821)%2147483647)
 local function branch(x,y,angle,length,radius,depth)
  local steps=math.ceil(length)
  for k=1,steps do
   angle=angle+(rnd()-0.5)*0.16;x=x+math.cos(angle);y=y+math.sin(angle)
   ellipse(a,w,h,x,y,radius*(1-k/steps*0.25),radius,true)
   if x<5 or x>w-5 or y<5 or y>h-5 then break end
  end
  ellipse(a,w,h,x,y,radius*2.0,radius*1.5,true)
  if depth>0 then
   branch(x,y,angle-0.65,length*0.69,radius*0.8,depth-1)
   branch(x,y,angle+0.65,length*0.69,radius*0.8,depth-1)
  end
 end
 for x=12,w-12,math.max(28,math.floor(w/5)) do branch(x,6,math.pi/2,h*0.33,2+o*3,4) end
 return a
end

function L.chasms(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+99371)%2147483647)
 for _=1,math.max(2,math.floor(w/65)) do
  local cx=12+rnd()*(w-24);local phase=rnd()*6
  for y=4,h-4 do
   local x=cx+math.sin(y/22+phase)*8+(N.value(seed+11,y/12,cx/20)-0.5)*5
   ellipse(a,w,h,x,y,5+o*9,2,true)
   if y%23==0 then ellipse(a,w,h,x,y,18+o*18,4,true) end
  end
 end
 for _=1,math.floor(w*h/2400) do
  local x,y=10+rnd()*(w-20),12+rnd()*(h-24)
  ellipse(a,w,h,x,y,5,2,false)
 end
 return a
end
function L.crystal(seed,w,h,o)
 local a=wallMask(w,h);local rnd=R.new((seed+71119)%2147483647)
 local offset=rnd()*20
 for y=3,h-2 do for x=3,w-2 do
  local warp=(N.value(seed+747,x/24,y/24)-0.5)*5
  local d1=math.abs(((x+y*0.70+offset+warp)%28)-14)
  local d2=math.abs(((x-y*0.85+offset-warp)%33)-16.5)
  a[(y-1)*w+x]=math.min(d1,d2)<1.4+o*3
 end end
 for _=1,math.floor(w*h/1300) do
  local cx,cy=8+rnd()*(w-16),8+rnd()*(h-16);local r=5+rnd()*10
  for y=math.max(3,math.floor(cy-r)),math.min(h-2,math.ceil(cy+r)) do
   for x=math.max(3,math.floor(cx-r)),math.min(w-2,math.ceil(cx+r)) do
    if math.abs(x-cx)+math.abs(y-cy)<r then a[(y-1)*w+x]=true end
   end
  end
 end
 return a
end

function L.make(name,seed,w,h,o,biomes)
 assert(L.descriptions[name],'Unknown layout technique')
 if name~='hybrid' then return L[name](seed,w,h,o) end
 local masks={noise=L.noise(seed,w,h,o),cellular=L.cellular(seed,w,h,o),worms=L.worms(seed,w,h,o),
  faults=L.faults(seed,w,h,o),vaults=L.vaults(seed,w,h,o),roots=L.roots(seed,w,h,o),crystal=L.crystal(seed,w,h,o),karst=L.karst(seed,w,h,o),labyrinth=L.labyrinth(seed,w,h,o)}
 local pick={[1]='noise',[2]='faults',[3]='faults',[4]='cellular',[5]='cellular',[6]='worms',[7]='noise',[8]='vaults',[9]='roots',[10]='crystal',[11]='labyrinth',[12]='karst'}
 local a={};for i=1,w*h do a[i]=masks[pick[biomes[i]] or 'noise'][i] end
 return a
end
return L
