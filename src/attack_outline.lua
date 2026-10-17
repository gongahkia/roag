-- Pixel-aligned dashed warning borders. The tile interior stays untouched.
local Outline={}
function Outline.rectangles(tile)
    local inset,thickness=3,2
    local span=tile-inset*2
    local dash=math.max(4,math.floor(tile/8))
    local rects={}
    for offset=0,span-1,dash+3 do
        local length=math.min(dash,span-offset)
        rects[#rects+1]={inset+offset,inset,length,thickness}
        rects[#rects+1]={inset+offset,tile-inset-thickness,length,thickness}
        rects[#rects+1]={inset,inset+offset,thickness,length}
        rects[#rects+1]={tile-inset-thickness,inset+offset,thickness,length}
    end
    return rects
end
return Outline
