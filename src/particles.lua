local W=require('src.world')
local M=require('src.materials')
local R=require('src.random')
local P={}
local function swap(w,i,j)
 w.mat[i],w.mat[j]=w.mat[j],w.mat[i]
 w.stamp[i],w.stamp[j]=w.tick,w.tick
 w.navRevision=w.navRevision+1; w.stats.moves=w.stats.moves+1
end
local function try(w,i,x,y,m)
 if not W.inside(w,x,y) or W.blocked(w,x,y) then return false end
 local j=W.index(w,x,y)
 if w.stamp[j]==w.tick then return false end
 local b=w.mat[j]
 local free=b==M.AIR or b==M.STEAM
 if M.def[m].powder and b==M.WATER then free=true end
 if m==M.STEAM then free=b==M.AIR end
 if free then swap(w,i,j) return true end
 return false
end
function P.step(w)
 w.stats.moves,w.stats.reactions=0,0
 local flip=w.tick%2==0
 -- Bottom-up solids/liquids; stamps prevent one particle moving repeatedly.
 -- Scan direction alternates to reduce (not claim eliminate) directional bias.
 for y=w.height-2,3,-1 do
  for col=3,w.width-2 do
   local x=flip and col or w.width+1-col
   local i=W.index(w,x,y); local m=w.mat[i]
   if m~=M.AIR and w.stamp[i]~=w.tick and not W.blocked(w,x,y) then
    if m==M.LAVA and w.tick%4==0 then
     local d={{-1,0},{1,0},{0,-1},{0,1}}
     for _,a in ipairs(d) do
      local nx,ny=x+a[1],y+a[2]
      if W.get(w,nx,ny)==M.WATER then
       W.put(w,x,y,M.ROCK); W.put(w,nx,ny,M.STEAM)
       w.stamp[i]=w.tick; w.stamp[W.index(w,nx,ny)]=w.tick
       w.stats.reactions=w.stats.reactions+1; w.ledger.rockCooled=w.ledger.rockCooled+1
       m=M.ROCK; break
      end
     end
    elseif m==M.ICE and (W.get(w,x-1,y)==M.LAVA or W.get(w,x+1,y)==M.LAVA
       or W.get(w,x,y+1)==M.LAVA or W.get(w,x,y-1)==M.LAVA) then
     W.put(w,x,y,M.WATER); w.stamp[i]=w.tick; m=M.WATER
     w.stats.reactions=w.stats.reactions+1
    end
    local def=M.def[m]
    if (def.powder or def.fluid) and w.stamp[i]~=w.tick and (m~=M.LAVA or w.tick%3==0) then
     local dir=(x+y+w.tick)%2==0 and 1 or -1
     if not try(w,i,x,y+1,m) and not try(w,i,x+dir,y+1,m) then
      if not try(w,i,x-dir,y+1,m) and def.fluid then
       if not try(w,i,x+dir,y,m) then try(w,i,x-dir,y,m) end
      end
     end
    end
   end
  end
 end
 -- Gas rises in a separate top-down pass, using the SAME update stamps.
 for y=3,w.height-2 do for x=3,w.width-2 do
  local i=W.index(w,x,y)
  if w.mat[i]==M.STEAM and w.stamp[i]~=w.tick then
   if w.tick%60==0 and R.hash(w.seed+w.tick,x,y)<0.06 then
    W.put(w,x,y,M.WATER); w.stamp[i]=w.tick
   else
    local dir=(x+w.tick)%2==0 and 1 or -1
    if not try(w,i,x,y-1,M.STEAM) and not try(w,i,x+dir,y-1,M.STEAM) then
     if not try(w,i,x-dir,y-1,M.STEAM) then try(w,i,x+dir,y,M.STEAM) end
    end
   end
  end
 end end
end
return P
