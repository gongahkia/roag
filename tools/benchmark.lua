-- Run from the repository root: lua tools/benchmark.lua [output-prefix]
local B=require('src.benchmark')
local prefix=(arg and arg[1]) or ('benchmark-'..os.date('%Y%m%d-%H%M%S'))
local co=B.create(os.clock)
while true do
 local ok,result,manifest=coroutine.resume(co)
 assert(ok,result)
 if coroutine.status(co)=='dead' then
  local f=assert(io.open(prefix..'.csv','wb'));f:write(result);f:close()
  f=assert(io.open(prefix..'.txt','wb'));f:write(manifest,'Clock: os.clock (CPU seconds)\n');f:close()
  print('Wrote '..prefix..'.csv and '..prefix..'.txt');break
 end
end
