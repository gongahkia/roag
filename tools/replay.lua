-- Inspect a .dw replay bundle or run.dat without executing any code from it.
-- lua tools/replay.lua /path/to/run.dat [tick]
assert(arg and arg[1],'Usage: lua tools/replay.lua bundle.dw [tick]')
local H=require('src.history')
local Codec=require('src.codec')
local Metrics=require('src.metrics')
local U=require('src.util')
local f=assert(io.open(arg[1],'rb'));local data=f:read('*a');f:close()
local h=H.fromText(data)
local target=tonumber(arg[2]) or h.frontier
h:seek(target);while h.seekTarget do h:updateSeek(100) end
print('Seed / preset / mode:',h.view.seed,h.view.preset,h.view.mode)
print('Viewed tick / frontier:',h.view.tick,h.frontier)
print('State checksum:',Codec.hash(h.view))
for _,a in ipairs(h.view.workers) do print(a.name,a.hp,a.status,a.reason) end
local m=Metrics.measure(h.view)
for _,k in ipairs(U.keys(m)) do if type(m[k])=='number' then print(k,m[k]) end end
