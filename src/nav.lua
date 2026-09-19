local W=require('src.world')
local M=require('src.materials')
local N={}
-- A worker occupies two cells wide and three cells tall; x is the left foot.
function N.occupy(w,x,y,safe)
 if x<3 or x>w.width-3 or y<5 or y>w.height-2 then return false end
 for yy=y-2,y do for xx=x,x+1 do
  if W.solid(w,xx,yy) then return false end
  local m=W.get(w,xx,yy)
  if safe and (m==M.LAVA or m==M.STEAM or (m==M.WATER and yy<=y-1)) then return false end
 end end
 return true
end
function N.ladder(w,x,y)
 local s=W.structureAt(w,x,y)
 return s and s.kind=='ladder' or false
end
function N.support(w,x,y)
 return W.solid(w,x,y+1) or W.solid(w,x+1,y+1) or N.ladder(w,x,y)
end
function N.stand(w,x,y,safe) return N.occupy(w,x,y,safe) and N.support(w,x,y) end
function N.neighbours(w,x,y)
 local out={}
 local function add(nx,ny) out[#out+1]=W.index(w,nx,ny) end
 for _,dx in ipairs({-1,1}) do
  if N.stand(w,x+dx,y,true) then add(x+dx,y)
  elseif N.occupy(w,x,y-1,true) and N.stand(w,x+dx,y-1,true) then add(x+dx,y-1)
  elseif N.occupy(w,x+dx,y,true) then
   -- Controlled drops of up to four cells; cannot path through intervening solid/water.
   for drop=1,4 do
    if not N.occupy(w,x+dx,y+drop,true) then break end
    if N.support(w,x+dx,y+drop) then add(x+dx,y+drop) break end
   end
  end
 end
 for _,dy in ipairs({-1,1}) do
  if (N.ladder(w,x,y) or N.ladder(w,x,y+dy)) and N.stand(w,x,y+dy,true) then add(x,y+dy) end
 end
 return out
end
function N.edge(w,x,y,nx,ny)
 for _,i in ipairs(N.neighbours(w,x,y)) do if i==W.index(w,nx,ny) then return true end end
 return false
end
-- One deterministic BFS per planning pass, reused for all candidate jobs.
function N.flood(w,x,y)
 local start=W.index(w,x,y)
 local q,head,parent,distance={start},1,{[start]=0},{[start]=0}
 while head<=#q do
  local i=q[head]; head=head+1
  local px,py=W.xy(w,i)
  for _,j in ipairs(N.neighbours(w,px,py)) do if parent[j]==nil then
   parent[j]=i; distance[j]=distance[i]+1; q[#q+1]=j
  end end
 end
 w.stats.plans=w.stats.plans+1
 return {start=start,queue=q,parent=parent,distance=distance}
end
-- Directed walking graphs can contain one-way drops. Supply trips must be able
-- to return to their starting region before committing to a pickup. This cache
-- belongs to one planning pass only; changing terrain is still checked per edge.
function N.returnable(w,f)
 if f.returnable then return f.returnable end
 local reverse={}
 for _,i in ipairs(f.queue) do
  local x,y=W.xy(w,i)
  for _,j in ipairs(N.neighbours(w,x,y)) do
   if f.parent[j]~=nil then
    reverse[j]=reverse[j] or {};reverse[j][#reverse[j]+1]=i
   end
  end
 end
 local seen,q,head={[f.start]=true},{f.start},1
 while head<=#q do
  local i=q[head];head=head+1
  for _,j in ipairs(reverse[i] or {}) do
   if not seen[j] then seen[j]=true;q[#q+1]=j end
  end
 end
 f.returnable=seen;return seen
end
function N.path(f,i)
 local p={}
 while i~=f.start do p[#p+1]=i i=f.parent[i] if not i then return nil end end
 local out={} for k=#p,1,-1 do out[#out+1]=p[k] end return out
end
function N.closest(w,f,predicate)
 for _,i in ipairs(f.queue) do local x,y=W.xy(w,i)
  if predicate(x,y) then return N.path(f,i),i,f.distance[i] end
 end
end
-- A hand can reach a nearby item/cell only if the straight segment is unobstructed.
-- The endpoint itself may be solid (mining); intervening cells may not.
function N.reach(w,x,y,tx,ty,range)
 local sx,sy=x+0.5,y-1
 if math.abs(tx-sx)+math.abs(ty-sy)>(range or 4) then return false end
 local steps=math.max(1,math.ceil(math.max(math.abs(tx-sx),math.abs(ty-sy))*2))
 for k=1,steps-1 do
  local xx=math.floor(sx+(tx-sx)*k/steps+0.5)
  local yy=math.floor(sy+(ty-sy)*k/steps+0.5)
  if not (xx==tx and yy==ty) and W.solid(w,xx,yy) then return false end
 end
 return true
end
function N.reachRect(w,x,y,gx,gy)
 local x1,y1,x2,y2=W.rect(gx,gy)
 local tx=math.max(x1,math.min(x2,x)); local ty=math.max(y1,math.min(y2,y-1))
 return N.reach(w,x,y,tx,ty,4)
end
return N
