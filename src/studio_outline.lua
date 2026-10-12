-- Optional one-output-pixel silhouette stroke around a 2x nearest-neighbor image.
-- Editing data stays at its source resolution. The stroke is clipped at its edges.
local M={}
function M.render(raw,w,h,color)
    assert(type(raw)=='string' and #raw==w*h*4,'invalid source RGBA')
    assert(type(color)=='string' and color:match('^%x%x%x%x%x%x%x%x$'),
        'outline color must be RRGGBBAA')
    local c={}
    for i=1,8,2 do c[#c+1]=tonumber(color:sub(i,i+1),16) end
    local stroke=string.char(unpack(c))
    local ow,oh=w*2,h*2
    local function opaque(x,y)
        if x<0 or y<0 or x>=ow or y>=oh then return false end
        local sx,sy=math.floor(x/2),math.floor(y/2)
        return raw:byte((sy*w+sx)*4+4)>0
    end
    local rows={}
    for y=0,oh-1 do
        local row={}
        for x=0,ow-1 do
            local sx,sy=math.floor(x/2),math.floor(y/2)
            local p=(sy*w+sx)*4+1
            local rgba=raw:sub(p,p+3)
            if raw:byte(p+3)==0 then
                local adjacent=false
                for dy=-1,1 do
                    for dx=-1,1 do
                        if (dx~=0 or dy~=0) and opaque(x+dx,y+dy) then
                            adjacent=true;break
                        end
                    end
                    if adjacent then break end
                end
                if adjacent then rgba=stroke end
            end
            row[#row+1]=rgba
        end
        rows[#rows+1]=table.concat(row)
    end
    return ow,oh,table.concat(rows)
end
return M
