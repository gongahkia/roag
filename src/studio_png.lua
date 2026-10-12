-- LÖVE 11.5-compatible RGBA8 PNG writer. Stored DEFLATE blocks, no external encoder.
local bit=require('bit')
local M={}
local function u32(n) return string.char(math.floor(n/16777216)%256,math.floor(n/65536)%256,math.floor(n/256)%256,n%256) end
local crc_table={}
for i=0,255 do local c=i;for _=1,8 do c=bit.band(c,1)==1 and bit.bxor(bit.rshift(c,1),0xedb88320) or bit.rshift(c,1) end;crc_table[i]=c end
local function crc(bytes)
    local c=0xffffffff
    for i=1,#bytes do c=bit.bxor(crc_table[bit.band(bit.bxor(c,bytes:byte(i)),255)],bit.rshift(c,8)) end
    return bit.bxor(c,0xffffffff)%4294967296
end
local function chunk(kind,bytes)
    local body=kind..bytes
    return u32(#bytes)..body..u32(crc(body))
end
function M.encode(w,h,rgba,derived_max,compress)
    -- Editable documents remain capped at 512. Only the derived atlas builder
    -- may request a larger output; source documents never pass this argument.
    local limit=derived_max or 512
    assert(limit==512 or limit==2048,'invalid derived PNG limit')
    assert(type(w)=='number' and type(h)=='number' and w%1==0 and h%1==0 and w>=1 and h>=1 and w<=limit and h<=limit,'invalid PNG size')
    assert(type(rgba)=='string' and #rgba==w*h*4,'invalid PNG RGBA bytes')
    local scan={};local stride=w*4
    for y=0,h-1 do scan[#scan+1]='\0'..rgba:sub(y*stride+1,(y+1)*stride) end
    local raw=table.concat(scan)
    if compress then
        local compressed=compress(raw)
        assert(type(compressed)=='string' and #compressed>0 and
            #compressed<=32*1024*1024,'invalid derived PNG compression')
        return '\137PNG\r\n\26\n'..chunk('IHDR',u32(w)..u32(h)..
            string.char(8,6,0,0,0))..chunk('IDAT',compressed)..chunk('IEND','')
    end
    local blocks={'\x78\x01'};local pos=1
    while pos<=#raw do
        local n=math.min(65535,#raw-pos+1);local final=pos+n>#raw
        blocks[#blocks+1]=string.char(final and 1 or 0,n%256,math.floor(n/256),255-n%256,255-math.floor(n/256))
        blocks[#blocks+1]=raw:sub(pos,pos+n-1);pos=pos+n
    end
    local a,b=1,0
    for i=1,#raw do a=(a+raw:byte(i))%65521;b=(b+a)%65521 end
    blocks[#blocks+1]=u32(b*65536+a)
    return '\137PNG\r\n\26\n'..chunk('IHDR',u32(w)..u32(h)..string.char(8,6,0,0,0))..
        chunk('IDAT',table.concat(blocks))..chunk('IEND','')
end
return M
