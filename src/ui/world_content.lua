local Cat=require('src.catalog')
local UI={}
local function box(x,y,w,h,c)love.graphics.setColor(c);love.graphics.rectangle('fill',x,y,w,h)end
local function ring(x,y,r,c)
 love.graphics.setColor(c)
 for k=0,15 do local a,b=k*math.pi/8,(k+1)*math.pi/8;love.graphics.line(x+math.cos(a)*r,y+math.sin(a)*r,x+math.cos(b)*r,y+math.sin(b)*r) end
end
function UI.draw(w,point,sc,app,r)
 if w.content then
  for _,p in ipairs(w.content.flora) do if p.alive then
   local x,y=point(p.x,p.y);local color=Cat.flora[p.kind].color
   box(x+sc*0.35,y-sc,sc*0.3,sc*2,color)
   if p.kind=='thorn' then
    love.graphics.setColor(color);love.graphics.line(x-sc,y,x+sc*0.5,y-sc,x+sc*1.5,y)
   else
    local cap=sc*(0.7+p.food/12);box(x+sc*0.5-cap,y-sc*0.9,cap*2,sc*0.6,color)
    if p.kind=='filter' then box(x+sc*0.8,y-sc*1.7,sc*0.25,sc*2.5,color) end
   end
   if p.pulseUntil and p.pulseUntil>=w.tick then ring(x+sc/2,y,sc*4,{color[1],color[2],color[3],0.3}) end
  end end
  for _,p in ipairs(w.content.sites) do if p.alive then
   local x,y=point(p.x,p.y);local color=Cat.sites[p.kind].color
   box(x-sc,y-sc,sc*3,sc*2,color)
   box(x-sc*0.5,y-sc*0.5,sc*2,sc,{0.06,0.08,0.10})
   if sc>=3 then
    love.graphics.setFont(r.small);love.graphics.setColor(color)
    love.graphics.print(w.content.discoveries['sites:'..p.kind] and p.kind:sub(1,1):upper() or '?',x-sc/2,y-sc)
   end
   if p.pulseUntil and p.pulseUntil>=w.tick then ring(x,y,sc*5,{color[1],color[2],color[3],0.5}) end
  end end
  for _,c in ipairs(w.content.fauna) do if c.alive then
   local d=Cat.fauna[c.kind];local x,y=point(c.x,c.y-d.height+1)
   box(x,y,d.width*sc,d.height*sc,d.color)
   local dir=(math.floor(w.tick/20)+c.id)%2==0 and 0.2 or 0.6
   box(x+dir*sc,y+sc*0.15,sc*0.3,sc*0.3,{0.94,0.96,0.79})
   if c.kind=='grazer' then box(x+sc,y-sc*0.5,sc*0.4,sc*0.5,{0.78,0.91,0.52}) end
   if c.kind=='sentinel' and not c.awake then box(x,y+sc*0.4,2*sc,sc*0.3,{0.2,0.2,0.25}) end
  end end
  if app.view==4 then
   for _,d in ipairs(w.content.ruins) do
    local x,y=point(d.x1,d.y1);love.graphics.setColor(0.87,0.68,0.4,0.55)
    love.graphics.rectangle('line',x,y,(d.x2-d.x1+1)*sc,(d.y2-d.y1+1)*sc)
    if sc>=2 then love.graphics.setFont(r.small);love.graphics.print(d.name,x+2,y+2) end
   end
  end
 end
 for slot=1,w.cols*w.rows do local s=w.structures[slot]
  if s and (s.kind=='charge' or s.kind=='ward') then
   local x,y=point(s.gx*4-3,s.gy*4-3)
   if s.kind=='charge' then
    box(x+sc,y+2*sc,2*sc,2*sc,{0.78,0.36,0.25});box(x+1.5*sc,y+sc,sc*0.3,sc,{0.96,0.79,0.40})
    if s.fuseAt then
     ring(x+2*sc,y+2*sc,require('src.blasts').radius*sc,{0.92,0.38,0.25,0.55})
     love.graphics.setFont(r.small);love.graphics.setColor(1,0.8,0.4);love.graphics.print(tostring(math.max(0,s.fuseAt-w.tick)),x,y-sc*2)
    end
   else
    box(x+sc,y+sc,2*sc,3*sc,{0.31,0.63,0.69});box(x+sc*1.5,y,sc,sc*3,{0.75,0.81,0.87})
    if s.enabled and s.tank>0 then ring(x+sc*2,y+sc*2,sc*4,{0.3,0.7,0.8,0.4}) end
   end
  end
 end
 for _,fx in ipairs(w.blastEffects or {}) do
  local x,y=point(fx.x,fx.y);ring(x,y,sc*(fx.radius)*(w.tick-fx.tick+1)/13,{1,0.63,0.29,math.max(0,1-(w.tick-fx.tick)/13)})
 end
end
return UI
