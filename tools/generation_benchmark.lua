local path=arg and arg[1] or 'generation-results.csv'
local function absent(p) local f=io.open(p,'rb');if f then f:close();error('Refusing to overwrite '..p) end end
absent(path);absent(path..'.manifest.txt')
local co=require('src.generation.benchmark').create(os.clock)
while true do
 local ok,a,b=coroutine.resume(co);assert(ok,a)
 if coroutine.status(co)=='dead' then
  local f=assert(io.open(path,'wb'));assert(f:write(a));f:close()
  f=assert(io.open(path..'.manifest.txt','wb'));assert(f:write(b..'Clock: os.clock CPU seconds.\n'));f:close()
  print('Wrote '..path);break
 end
 print(a)
end
