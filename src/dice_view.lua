-- Pixel-sized projected cube faces and the matching physical cross net.
local P=require('src.dice_presentation')
local D=require('src.dice_trial')
local Pixel=require('src.pixel_ui')
local V={net_x=728,net_y=152,cell=54}
local pi=math.pi
local base={{0,0},{0,-pi/2},{pi/2,0},{0,pi/2},{-pi/2,0},{0,pi}}
local axes={{{1,0,0},{0,-1,0}},{{0,0,-1},{0,-1,0}},{{1,0,0},{0,0,1}},
 {{0,0,1},{0,-1,0}},{{1,0,0},{0,0,-1}},{{-1,0,0},{0,-1,0}}}
local function rotate(v,ax,ay,az)
 local x,y,z=v[1],v[2],v[3]
 y,z=y*math.cos(ax)-z*math.sin(ax),y*math.sin(ax)+z*math.cos(ax)
 x,z=x*math.cos(ay)+z*math.sin(ay),-x*math.sin(ay)+z*math.cos(ay)
 return x*math.cos(az)-y*math.sin(az),x*math.sin(az)+y*math.cos(az),z
end
function V.pose(p,n)
 local r=p.results[n];local b=base[r.index];local t=math.min(1,math.max(0,(p.age-(n-1)*.012)/.16))
 local roll=p.rolling and not p.reduced and 1-t or 0
 return b[1]+roll*pi*2,b[2]+roll*pi*2,roll*.5,t
end
function V.normal(p,n,j)
 local ax,ay,az=V.pose(p,n)
 local x,y,z=rotate(P.normals[j],ax,ay,az)
 return rotate({x,y,z},-.22,.3,0)
end
function V.front(p,n)
 local index,best
 for j=1,6 do local _,_,z=V.normal(p,n,j);if not best or z>best then index,best=j,z end end
 return index
end
function V.icon(face)
 local f=D.faces[face]
 return f.kind=='dice_guard'and 'core:ui/ability_shield_icon'or require('src.idea_presentation').weapon('core:actor/adventurer',f.kind=='move'and 'dash'or f.kind)
end
local function text(t,x,y,font,color,w)
 local g=love.graphics;g.setFont(font);g.setColor(color or Pixel.colors.text)
 if w then g.printf(t,x,y,w,'center')else g.print(t,x,y)end
end
function V.cube(p,n,x,y,size,host,focus)
 local g=love.graphics;local ax,ay,az,t=V.pose(p,n)
 local lift=p.reduced and 0 or p.rolling and math.sin(t*pi)*9 or p.focus[n]*3
 if p.rolling and not p.reduced and p.age>.16 then lift=math.sin(math.min(1,(p.age-.16)/.04)*pi)*2 end
 local kick=p.reduced and 0 or n==p.use_die and p.use_age<.09 and math.sin(p.use_age/.09*pi)*5 or 0
 local press=p.reduced and 0 or (1-p.press[n])*3
 y=y-lift+press+kick
 g.setColor(.02,.02,.02,.7);g.rectangle('fill',x-size*.5-3,y+size*.58+lift,size+10,4)
 local faces={}
 for j,normal in ipairs(P.normals)do
  local _,_,nz=V.normal(p,n,j)
  if nz>0 then
   local points={};local cx,cy=0,0
   for _,corner in ipairs({{-1,-1},{1,-1},{1,1},{-1,1}})do
    local u,v=axes[j][1],axes[j][2];local point={}
    for k=1,3 do point[k]=normal[k]+u[k]*corner[1]+v[k]*corner[2]end
    local px,py,pz=rotate(point,ax,ay,az);px,py=rotate({px,py,pz},-.22,.3,0)
    local sx,sy=math.floor(x+px*size/2+.5),math.floor(y-py*size/2+.5)
    points[#points+1]=sx;points[#points+1]=sy;cx=cx+sx;cy=cy+sy
   end
   faces[#faces+1]={j=j,points=points,z=nz,cx=cx/4,cy=cy/4}
  end
 end
 table.sort(faces,function(a,b)return a.z<b.z end)
 for _,f in ipairs(faces)do
  local shade=.12+.2*f.z;g.setColor(shade,shade,shade);g.polygon('fill',f.points)
  g.setColor(focus and Pixel.colors.selected or Pixel.colors.edge);g.polygon('line',f.points)
  local icon=math.max(7,math.floor(size*.57*f.z));host.visual(V.icon(p.dice[n][f.j]),math.floor(f.cx-icon/2),math.floor(f.cy-icon/2),icon,icon)
 end
 if p.rolling and not p.reduced then
  g.setColor(Pixel.colors.muted);g.line(x-size/2-7,y+5,x-size/2-4,y-3);g.line(x+size/2+4,y+12,x+size/2+6,y+4)
 end
end
function V.net_cell(p,j)
 local grid=P.net[j];local t=p.reduced and 1 or p.net
 local ease=1-(1-t)^3
 local ox,oy=689,129+((p.net_die or 1)-1)*80
 local x=ox+(V.net_x+grid[1]*V.cell-ox)*ease
 local y=oy+(V.net_y+grid[2]*V.cell-oy)*ease
 return math.floor(x+.5),math.floor(y+.5)
end
function V.hit(p,x,y)
 if p.net<.99 then return nil end
 for j=1,6 do local cx,cy=V.net_cell(p,j);if x>=cx and x<cx+50 and y>=cy and y<cy+50 then return j end end
end
local short={step='STEP',guarded_step='G.STEP',quick_step='Q.STEP',strike='STRIKE',sweep='SWEEP',pierce='PIERCE',guard='GUARD',dash='DASH',heavy_strike='HEAVY'}
function V.net(p,n,focus,proposal,host)
 local g=love.graphics;local body,menu=host.fonts()
 -- Draw outward panels first, then centre; hit testing waits for settled layout.
 for _,j in ipairs({6,5,4,3,2,1})do
  local x,y=V.net_cell(p,j);local face=P.net_face(p,n,j)
  local chosen=j==focus;local current=p.results[n].index==j
  g.setColor(chosen and Pixel.colors.selected or Pixel.colors.edge);g.rectangle('fill',x,y,50,50)
  g.setColor(Pixel.colors.background);g.rectangle('fill',x+1,y+1,48,48)
  if p.edit and p.edit.index==j and p.edit.die==n and not p.reduced then
   local pulse=math.sin(math.min(1,p.edit.age/.24)*pi)
   g.setColor(1,1,1,pulse*.45);g.rectangle('fill',x+2,y+2,46,46)
  end
  if current then g.setColor(Pixel.colors.light);g.rectangle('line',x+3,y+3,44,44)end
  text('f'..j..(current and '*'or ''),x+3,y+2,body,chosen and Pixel.colors.selected or Pixel.colors.muted)
  host.visual(V.icon(face),x+15,y+12,22,22)
  text(short[face],x,y+36,body,nil,50)
  if proposal and chosen and proposal~=face then
   -- A corner tag marks a proposal; the committed face remains visible.
   g.setColor(Pixel.colors.selected);g.rectangle('line',x+32,y+1,16,16)
   host.visual(V.icon(proposal),x+33,y+2,14,14)
  end
 end
 local face=p.dice[n][focus]
 text('Face '..focus..' · '..D.faces[face].name,672,378,body,Pixel.colors.text,320)
 if proposal then
  text('Proposed: '..D.faces[proposal].name,672,398,body,Pixel.colors.selected,320)
 else
  text(D.faces[face].description..' Base '..D.faces[face].cost..'.',672,400,body,nil,320)
 end
end
function V.accents(p,host,hero)
 local g=love.graphics;local body=host.fonts()
 if p.use_age<.15 then
  local x,y=host.screen(hero.x,hero.y);local fade=1-p.use_age/.15
  g.setColor(1,1,1,fade*.7)
  g.rectangle('line',x+7,y+7,34,34)
  host.visual(V.icon(p.use_face),x+32,y-8,18,18)
 end
 for n,a in ipairs(p.accents)do
  local x,y=host.screen(a.x or hero.x,a.y or hero.y)
  local alpha=math.min(1,(.55-a.age)/.14);local rise=p.reduced and 0 or math.floor(a.age*14)
  text(a.label,x-18,y-12-rise-(n%2)*10,body,{1,.91,.48,alpha})
  if a.kind=='impact'then
   g.setColor(1,1,1,alpha*.65);local span=a.face=='heavy_strike'and 20 or 16
   g.rectangle('line',x+24-span,y+24-span,span*2,span*2)
   if a.face=='pierce'then g.line(x+16,y+36,x+32,y+12)end
  elseif a.kind=='guard'then g.setColor(.7,.8,.9,alpha);g.rectangle('line',x+5,y+5,38,38)
  elseif a.kind=='bloom'then
   g.setColor(1,.85,.55,alpha*.65);local r=p.reduced and 20 or 10+a.age*60
   g.rectangle('line',x+24-r,y+24-r,r*2,r*2)
  end
 end
end
return V
