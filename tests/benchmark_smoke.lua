local B=require('src.benchmark')
local co=B.create(os.clock,{presets={'cistern'},seeds={7},scenarios={'arrival','construction'},ticks=30})
local yields=0
while true do
 local ok,result,manifest=coroutine.resume(co);assert(ok,result)
 if coroutine.status(co)=='dead' then
  local count=0 for _ in result:gmatch('\n') do count=count+1 end
  assert(count==3,'Header plus two cases expected')
  assert(manifest:match('Lua heap only'));assert(yields==6)
  print('PASS benchmark adapter: two cases, six cooperative yields, CSV and methodology metadata.');break
 end
 yields=yields+1
end
