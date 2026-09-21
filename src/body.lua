-- Settler geometry is versioned simulation state.  A worker anchor is its
-- left foot on the lowest occupied cell; historical worlds omit `body` and
-- retain the original 2x3 profile.
local B={}

local legacy={id='settler-2x3-v0',width=2,height=3,handOffset=1}
local current={id='settler-2x4-v1',width=2,height=4,handOffset=2}

function B.profile(world)
 return world and (world.body==1 or world.frontier and world.frontier.body==1) and current or legacy
end
function B.width(world) return B.profile(world).width end
function B.height(world) return B.profile(world).height end
function B.rect(world,x,y)
 local p=B.profile(world)
 return x,y-p.height+1,x+p.width-1,y
end
function B.occupied(world,x,y,visit)
 local x1,y1,x2,y2=B.rect(world,x,y)
 for yy=y1,y2 do for xx=x1,x2 do visit(xx,yy) end end
end
local function anchor(value,y)
 if type(value)=='table' then return value.x,value.y end
 return value,y
end
function B.eye(world,value,y)
 local p=B.profile(world)
 local x,foot=anchor(value,y)
 return x,foot-p.height+1
end
function B.eyes(world,value,y)
 local x,foot=anchor(value,y);local p=B.profile(world);local top=foot-p.height+1
 return {{x=x,y=top},{x=x+p.width-1,y=top}}
end
function B.hand(world,value,y)
 local p=B.profile(world)
 local x,foot=anchor(value,y)
 return x+(p.width-1)/2,foot-p.handOffset
end
function B.contains(world,value,y,targetX,targetY)
 local x,foot,tx,ty
 if type(value)=='table' then x,foot=value.x,value.y;tx,ty=y,targetX else x,foot,tx,ty=value,y,targetX,targetY end
 local x1,y1,x2,y2=B.rect(world,x,foot)
 return tx>=x1 and tx<=x2 and ty>=y1 and ty<=y2
end

return B
