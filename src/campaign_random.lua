-- Versioned, campaign-only deterministic random stream.  This deliberately
-- does not share the legacy map generator's random state.
local U=require('src.util')
local R={version=1,modulus=2147483647,multiplier=48271}

local function validState(value,label)
 U.integer(value,label or 'campaign random state',1,R.modulus-1)
 return value
end

function R.new(seed)
 return {version=R.version,state=validState(seed,'campaign random seed')}
end

function R.validate(stream)
 assert(type(stream)=='table','Campaign random stream must be a table')
 for key in pairs(stream) do assert(key=='version' or key=='state','Unknown campaign random stream key '..tostring(key)) end
 assert(stream.version==R.version,'Unsupported campaign random stream version')
 validState(stream.state)
 return true
end

function R.next(stream)
 R.validate(stream)
 stream.state=(stream.state*R.multiplier)%R.modulus
 return stream.state
end

function R.derive(master,namespace)
 validState(master,'campaign master seed')
 assert(type(namespace)=='string','Campaign random namespace must be ASCII')
 for i=1,#namespace do assert(namespace:byte(i)<=127,'Campaign random namespace must be ASCII') end
 local h=master
 for i=1,#namespace do h=(h*131+namespace:byte(i))%R.modulus end
 return h==0 and 1 or h
end

function R.uniform(stream,n)
 R.validate(stream);U.integer(n,'campaign random bound',1,R.modulus-1)
 local range=R.modulus-1;local limit=range-(range%n)
 local u
 repeat u=R.next(stream)-1 until u<limit
 return (u%n)+1
end

return R
