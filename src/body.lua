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
function B.eye(world,worker)
 local p=B.profile(world)
 return worker.x,worker.y-p.height+1
end
function B.eyes(world,worker)
 local p=B.profile(world);local y=worker.y-p.height+1
 return {{x=worker.x,y=y},{x=worker.x+p.width-1,y=y}}
end
function B.hand(world,worker)
 local p=B.profile(world)
 return worker.x+(p.width-1)/2,worker.y-p.handOffset
end
function B.contains(world,worker,x,y)
 local x1,y1,x2,y2=B.rect(world,worker.x,worker.y)
 return x>=x1 and x<=x2 and y>=y1 and y<=y2
end

return B
