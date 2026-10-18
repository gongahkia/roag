-- Small ROAG presentation helpers inspired by Microcraft's focused lists and
-- framed dialogs. Original LÖVE implementation; no simulation or storage.
local U={colors={background={.025,.025,.04},panel={.12,.13,.32},
    edge={.46,.48,.63},light={.75,.77,.86},text={.88,.89,.94},
    muted={.52,.54,.64},selected={1,.91,.48}}}
local default_colors=U.colors
local monochrome={background={.03,.03,.03},panel={.075,.075,.075},
    edge={.42,.42,.42},light={.74,.74,.74},text={.9,.9,.9},
    muted={.5,.5,.5},selected={1,1,1}}
function U.set_style(style)U.colors=style=='onebit' and monochrome or default_colors end
function U.contains(r,x,y)
    return x>=r.x and y>=r.y and x<r.x+r.w and y<r.y+r.h
end
function U.row(x,y,w,h,index,step)
    return {x=x,y=y+(index-1)*step,w=w,h=h}
end
function U.row_at(x,y,w,h,count,step,px,py)
    for i=1,count do if U.contains(U.row(x,y,w,h,i,step),px,py)then return i end end
end
function U.center(text,y,font,color,x,w)
    local g=love.graphics
    if font then g.setFont(font)end
    g.setColor(color or U.colors.text)
    g.printf(text,x or 0,y,w or g.getWidth(),'center')
end
function U.dim(alpha)
    local g=love.graphics
    g.setColor(0,0,0,alpha or .6);g.rectangle('fill',0,0,g.getDimensions())
end
function U.frame(title,x,y,w,h,font)
    local g=love.graphics
    g.setColor(0,0,0,.85);g.rectangle('fill',x+4,y+4,w,h)
    g.setColor(U.colors.edge);g.rectangle('fill',x,y,w,h)
    g.setColor(U.colors.light);g.rectangle('fill',x+2,y+2,w-4,h-4)
    g.setColor(.035,.035,.08);g.rectangle('fill',x+4,y+4,w-8,h-8)
    g.setColor(U.colors.panel);g.rectangle('fill',x+6,y+6,w-12,h-12)
    if title and title~=''then
        if font then g.setFont(font)end
        local width=g.getFont():getWidth(title)+24
        g.setColor(U.colors.panel);g.rectangle('fill',x+(w-width)/2,y-6,width,24)
        U.center(title,y-6,font,U.colors.selected,x,w)
    end
end
function U.option(label,x,y,w,selected,font,disabled)
    U.center((selected and '> ' or '  ')..label..(selected and ' <' or '  '),
        y,font,disabled and U.colors.muted or selected and U.colors.selected or U.colors.text,x,w)
end
function U.settings_row(x,y,w,index)
    return U.row(x+36,y+166,w-72,40,index,48)
end
function U.settings_at(x,y,w,count,px,py)
    return U.row_at(x+36,y+166,w-72,40,count,48,px,py)
end
function U.home_at(x,y)return U.row_at(280,232,464,34,5,42,x,y)end
function U.class_at(x,y)return U.row_at(190,150,290,32,9,36,x,y)end
function U.pause_at(x,y)return U.row_at(306,252,412,38,3,44,x,y)end
function U.chest_at(x,y)return U.row_at(102,178,420,62,3,76,x,y)end
function U.route_at(count,x,y)return U.row_at(180,216,664,54,count,64,x,y)end
return U
