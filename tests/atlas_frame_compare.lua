-- Compare callback-captured uncompressed TGA frames. Presentation delta can
-- change a fading effect's alpha by one 8-bit level between LÖVE processes.
local function read(path)
    local f=assert(io.open(path,'rb'));local bytes=f:read('*a');f:close()
    return bytes
end
local a,b=read(assert(arg[1])),read(assert(arg[2]))
local mode=arg[3] or 'effect'
assert(#a==#b and a:sub(1,18)==b:sub(1,18),'TGA geometry differs')
assert(a:byte(3)==2 and a:byte(17)==32,'expected RGBA32 uncompressed TGA')
local count,max_delta=0,0
local ignored=0
local width=a:byte(13)+a:byte(14)*256
for i=19,#a do
    local delta=math.abs(a:byte(i)-b:byte(i))
    if delta>0 then
        local pixel=math.floor((i-19)/4)
        local x,y=pixel%width,math.floor(pixel/width)
        if mode=='ignore-notice' and x>=740 and x<=1003 and
            y>=248 and y<=288 then ignored=ignored+1
        elseif mode=='expect-world-change' and (x>=720 or
            (x>=70 and x<=120 and y>=70 and y<=120)) then
            ignored=ignored+1
        else count=count+1;max_delta=math.max(max_delta,delta) end
    end
end
assert((mode=='ignore-notice' and count==0) or
    (mode=='effect' and count<=1024 and max_delta<=1) or
    (mode=='expect-world-change' and count>100),
    ('effect frame differs materially: %d channel bytes, max delta %d'):
        format(count,max_delta))
print(('%s frame: %d material channel differences; %d notice differences'):
    format(mode,count,ignored))
