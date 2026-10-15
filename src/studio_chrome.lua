-- Shared, presentation-only Studio chrome. Coordinates belong to each editor.
local UI={}
UI.color={
    background={0.075,0.083,0.101}, header={0.125,0.140,0.168},
    panel={0.150,0.168,0.194}, inset={0.092,0.105,0.126},
    raised={0.205,0.227,0.260}, border={0.310,0.340,0.383},
    text={0.925,0.936,0.950}, muted={0.690,0.730,0.780},
    accent={0.380,0.690,0.940}, accent_dark={0.205,0.365,0.515},
    warning={1.000,0.725,0.435}, checker_light={0.430,0.455,0.485},
    checker_dark={0.305,0.330,0.360},
}
local c=UI.color
local function fill(color,x,y,w,h)
    local g=love.graphics
    g.setColor(color);g.rectangle('fill',x,y,w,h)
end
local function line(color,x,y,w,h)
    local g=love.graphics
    g.setColor(color);g.rectangle('line',x+.5,y+.5,w-1,h-1)
end
UI.fill=fill
UI.line=line
function UI.begin(title,subtitle)
    local g=love.graphics
    local w,h=g.getDimensions()
    fill(c.background,0,0,w,h)
    fill(c.header,0,0,w,65)
    fill(c.accent,0,0,3,65)
    fill(c.border,0,64,w,1)
    g.setColor(c.text);g.print(title,18,13)
    g.setColor(c.muted);g.print(subtitle or '',18,39)
end
function UI.panel(x,y,w,h)
    fill(c.panel,x,y,w,h)
    line(c.border,x,y,w,h)
end
function UI.inset(x,y,w,h)
    fill(c.inset,x,y,w,h)
    line(c.border,x,y,w,h)
end
function UI.selected(x,y,w,h)
    fill(c.accent_dark,x,y,w,h)
    fill(c.accent,x,y,3,h)
end
function UI.button(x,y,w,h,label,active,enabled)
    local g=love.graphics
    local bg=not enabled and c.inset or active and c.accent_dark or c.raised
    fill(bg,x,y,w,h)
    line(active and c.accent or c.border,x,y,w,h)
    g.setColor(enabled and c.text or c.muted)
    g.print(label,x+math.max(5,math.floor((w-g.getFont():getWidth(label))/2)),
        y+math.floor((h-g.getFont():getHeight())/2))
end
function UI.rule(x,y,w)
    fill(c.border,x,y,w,1)
end
function UI.statusbar(y,h)
    local w=love.graphics.getWidth()
    fill(c.header,0,y,w,h)
    fill(c.border,0,y,w,1)
end
function UI.modal(x,y,w,h)
    UI.panel(x,y,w,h)
    fill(c.accent,x,y,w,2)
end
return UI
