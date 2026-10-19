-- Small ROAG presentation helpers inspired by Microcraft's focused lists and
-- framed dialogs. Original LÖVE implementation; no simulation or storage.
local U={colors={background={.025,.025,.04},panel={.12,.13,.32},
    edge={.46,.48,.63},light={.75,.77,.86},text={.88,.89,.94},
    muted={.52,.54,.64},selected={1,.91,.48}}}
local default_colors=U.colors
local monochrome={background={.03,.03,.03},panel={.075,.075,.075},
    edge={.42,.42,.42},light={.74,.74,.74},text={.9,.9,.9},
    muted={.5,.5,.5},selected={1,1,1}}
local high_contrast={background={0,0,0},panel={.04,.06,.12},
    edge={.85,.87,.97},light={1,1,1},text={1,1,1},
    muted={.78,.82,.88},selected={1,.92,.35}}
local monochrome_contrast={background={0,0,0},panel={.04,.04,.04},
    edge={.88,.88,.88},light={1,1,1},text={1,1,1},
    muted={.78,.78,.78},selected={1,1,1}}
-- Position keys are bounded to the visible view; changing views discards them.
-- Hitboxes never move. These clocks do not gate selection or gameplay input.
local feedback={enabled=false,rows={},age=0,frame=0}
function U.update(dt,enabled)
    feedback.enabled=enabled==true
    if not feedback.enabled then feedback.rows={};feedback.age=1;return end
    dt=math.max(0,dt)
    feedback.age=math.min(1,feedback.age+dt)
    for _,row in pairs(feedback.rows)do
        row.age=math.min(1,row.age+dt)
        row.press=math.min(1,row.press+dt)
    end
end
function U.begin_frame(view,enabled)
    if feedback.view~=view then
        feedback.rows={};feedback.age=0;feedback.view=view
    end
    if enabled~=nil then U.update(0,enabled)end
    feedback.frame=feedback.frame+1
    for key,row in pairs(feedback.rows)do
        if row.frame<feedback.frame-1 then feedback.rows[key]=nil end
    end
end
function U.press()
    if not feedback.enabled then return end
    for _,row in pairs(feedback.rows)do
        if row.selected and not row.disabled and row.frame==feedback.frame then row.press=0 end
    end
end
function U.option_feedback(label,x,y,w,selected,disabled)
    if not feedback.enabled then return 0,0,0 end
    local key=x..':'..y..':'..w
    local row=feedback.rows[key]
    if not row then row={age=1,press=1};feedback.rows[key]=row end
    if selected and (not row.selected or row.label~=label)then row.age=0;row.press=1 end
    row.selected,row.disabled,row.label,row.frame=selected,disabled,label,feedback.frame
    if not selected or disabled then return 0,0,0 end
    local settle=math.max(0,1-row.age/.14)
    return row.press<.09 and 1 or row.age<.07 and -1 or 0,
        math.floor(4*settle*settle+.5),settle
end
function U.offer_pose(index)
    if not feedback.enabled then return 0,1 end
    local t=math.min(1,math.max(0,(feedback.age-(index-1)*.025)/.16))
    return math.floor(6*(1-t)^2+.5),1+.12*math.sin(math.pi*t)
end
function U.set_style(style,contrast)
    U.colors=style=='onebit' and (contrast and monochrome_contrast or monochrome)
        or (contrast and high_contrast or default_colors)
end
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
    local g=love.graphics
    if font then g.setFont(font)end
    local face=g.getFont()
    local dy,spread,pulse=U.option_feedback(label,x,y,w,selected,disabled)
    y=y+dy
    g.setColor(disabled and U.colors.muted or
        selected and U.colors.selected or U.colors.text)
    g.printf(label,x,y,w,'center')
    if selected then
        local label_width=face:getWidth(label)
        local left=x+(w-label_width)/2
        local gap=math.max(4,math.floor(face:getHeight()/4))+spread
        g.print('>',left-gap-face:getWidth('>'),y)
        g.print('<',left+label_width+gap,y)
        if pulse>0 then
            g.setColor(U.colors.selected[1],U.colors.selected[2],U.colors.selected[3],pulse*.7)
            g.rectangle('fill',left,y+face:getHeight()+1,label_width,1)
        end
    end
end
function U.settings_row(x,y,w,index)
    return U.row(x+36,y+166,w-72,40,index,48)
end
function U.settings_at(x,y,w,count,px,py)
    return U.row_at(x+36,y+166,w-72,40,count,48,px,py)
end
function U.home_at(x,y)return U.row_at(280,232,464,34,5,42,x,y)end
function U.class_page(order,selected)
    local index=1
    for i,id in ipairs(order)do if id==selected then index=i;break end end
    local first=math.floor((index-1)/9)*9+1
    return first,math.min(#order,first+8)
end
function U.class_nav_at(x,y)
    if y<500 or y>=538 then return end
    if x>=190 and x<310 then return -1 end
    if x>=350 and x<480 then return 1 end
end
function U.class_page_target(order,selected,delta)
    local first=U.class_page(order,selected)
    local target=first+delta*9
    if target<1 or target>#order then return selected end
    return order[target]
end
function U.class_at(x,y,first,last)
    first,last=first or 1,last or 9
    local row=U.row_at(190,150,290,32,last-first+1,36,x,y)
    return row and row+first-1
end
function U.pause_at(x,y)return U.row_at(306,252,412,38,3,44,x,y)end
function U.chest_at(x,y)return U.row_at(102,178,420,62,3,76,x,y)end
function U.route_at(count,x,y)return U.row_at(180,216,664,54,count,64,x,y)end
return U
