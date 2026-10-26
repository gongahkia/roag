-- Lossless RGBA8 PNG for offline art builds and screenshot evidence on LÖVE 11.5.
-- No dependency on ImageData:encode('png').
local Atlas=require('src.studio_atlas')
local P={}
local function u32(n)
    return string.char(math.floor(n/16777216)%256,math.floor(n/65536)%256,
        math.floor(n/256)%256,n%256)
end
local function chunk(kind,bytes)
    return u32(#bytes)..kind..bytes..
        u32(tonumber(Atlas.checksum(kind..bytes):sub(9),16))
end
function P.encode(image)
    assert(image:getFormat()=='rgba8','PNG writer requires RGBA8')
    local w,h=image:getDimensions()
    local bytes=image:getString()
    local rows={}
    for y=0,h-1 do rows[#rows+1]='\0'..bytes:sub(y*w*4+1,(y+1)*w*4) end
    return '\137PNG\r\n\26\n'..
        chunk('IHDR',u32(w)..u32(h)..string.char(8,6,0,0,0))..
        chunk('IDAT',love.data.compress('string','zlib',table.concat(rows),9))..
        chunk('IEND','')
end
function P.write(path,image)
    local file=assert(io.open(path,'wb'))
    assert(file:write(P.encode(image)));assert(file:close())
end
return P
