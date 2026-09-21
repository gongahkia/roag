local W=require('src.world')
local M=require('src.materials')
local B=require('src.body')
local N={}
-- A worker uses the world's versioned body profile; x is the left foot.
function N.occupy(w,x,y,safe)
 local x1,y1,x2,y2=B.rect(w,x,y)
 if x1<1 or x2>w.width or y1<1 or y2>w.height then return false end
 for yy=y1,y2 do for xx=x1,x2 do
  if W.solid(w,xx,yy) then return false end
  local m=W.get(w,xx,yy)
  if safe and (m==M.LAVA or m==M.STEAM or (m==M.WATER and yy<=y-1)) then return false end
 end end
 return true
end
function N.ladder(w,x,y)
 local s=W.structureAt(w,x,y)
 if s and s.kind=='ladder' then return true end
 local ropes=w.ropes
 if ropes then
  for _,rope in ipairs(ropes) do
   if x==rope.laneLeftX and y>=rope.anchorY and y<rope.anchorY+rope.length then return true end
  end
 end
 return false
end
function N.support(w,x,y)
 local width=B.width(w)
 for xx=x,x+width-1 do if W.solid(w,xx,y+1) then return true end end
 return N.ladder(w,x,y)
end
function N.rope(w,x,y)
 local ropes=w.ropes or {}
 for _,rope in ipairs(ropes) do if x==rope.laneLeftX and y>=rope.anchorY and y<rope.anchorY+rope.length then return rope end end
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
function N.flood(w,x,y,quiet)
 local start=W.index(w,x,y)
 local q,head,parent,distance={start},1,{[start]=0},{[start]=0}
 while head<=#q do
  local i=q[head]; head=head+1
  local px,py=W.xy(w,i)
  for _,j in ipairs(N.neighbours(w,px,py)) do if parent[j]==nil then
   parent[j]=i; distance[j]=distance[i]+1; q[#q+1]=j
  end end
 end
 if not quiet then w.stats.plans=w.stats.plans+1 end
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
 local sx,sy=B.hand(w,{x=x,y=y})
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
 local sx,sy=B.hand(w,{x=x,y=y})
 local tx=math.max(x1,math.min(x2,math.floor(sx+.5))); local ty=math.max(y1,math.min(y2,math.floor(sy+.5)))
 return N.reach(w,x,y,tx,ty,4)
end
return N
