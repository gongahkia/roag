-- lua tools/headless.lua [seed] [frontier|cistern|dunes|frost] [ticks]
local G=require('src.generate')
local H=require('src.history')
local W=require('src.world')
local Codec=require('src.codec')
local Metrics=require('src.metrics')
local U=require('src.util')
local seed=tonumber(arg and arg[1]) or 12345
local preset=arg and arg[2] or require('config').preset
local ticks=tonumber(arg and arg[3]) or 2000
U.integer(ticks,'ticks',1,100000)
local h=H.new(G.make(seed,preset,'challenge'))
local gy=(h.live.home.floor-1)/4
local startBlock=((h.live.home.left or 9)-1)/4
h:queue({type='order',kind='build',build='farm',gx=startBlock+7,gy=gy})
h:queue({type='order',kind='build',build='bed',gx=startBlock+5,gy=gy})
local start=os.clock()
for _=1,ticks do h:advance() end
print('CPU seconds: '..(os.clock()-start))
print('State checksum (diagnostic, not cryptographic): '..Codec.hash(h.live))
local m=Metrics.measure(h.live)
for _,k in ipairs(U.keys(m)) do if type(m[k])=='number' then print(k,m[k]) end end
for _,a in ipairs(h.live.workers) do print(a.name,a.alive and 'alive' or 'dead',a.status,a.reason) end
assert(W.validate(h.live))
assert(m.waterResidual==0 and m.mineralResidual==0 and m.foodResidual==0,'Accounting invariant failed')
