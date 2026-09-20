local W=require('src.world')
local M=require('src.materials')
local N=require('src.nav')
local S=require('src.structures')
local Metrics=require('src.metrics')
local U=require('src.util')
local C=require('config')
local B=require('src.biomes')
local Layouts=require('src.generation.layouts')
local Labor=require('src.labor')
local Content=require('src.content')
local Catalog=require('src.catalog')
local Crew=require('src.ui.crew')
local Notes=require('src.ui.fieldnotes')
local ContentView=require('src.ui.world_content')
local Campaign=require('src.campaign')
local Logistics=require('src.logistics')
local R={};R.__index=R
local colors={bg={0.039,0.053,0.067},panel={0.070,0.085,0.103},edge={0.19,0.23,0.26},
 text={0.86,0.88,0.86},muted={0.49,0.56,0.59},amber={0.89,0.66,0.35},
 cyan={0.35,0.72,0.74},red={0.88,0.35,0.29},green={0.48,0.71,0.42}}
local tools={{'inspect','Q Inspect'},{'dig','D Dig'},{'ladder','L Ladder'},{'platform','F Floor'},
 {'wall','W Wall'},{'bed','B Bed'},{'store','S Store'},{'farm','C Farm'},
 {'pump','P Pump'},{'remove','X Remove'},{'cancel','E Cancel'},
 {'charge','A Charge'},{'ward','F9 Ward'},{'survey','U Survey'},{'salvage','Z Salvage'},{'cull','K Cull'},{'rally','M Rally'}}
local function color(c,a) love.graphics.setColor(c[1],c[2],c[3],a or 1) end
local function box(x,y,w,h,c,a) color(c,a);love.graphics.rectangle('fill',x,y,w,h) end
local function text(s,x,y,c,font) if font then love.graphics.setFont(font) end color(c or colors.text);love.graphics.print(tostring(s),x,y) end
local function wrap(s,x,y,width,c,font) if font then love.graphics.setFont(font) end color(c or colors.text);love.graphics.printf(tostring(s),x,y,width,'left') end
local function heading(s,x,y,c,font)
 love.graphics.setFont(font);color(c or colors.text);love.graphics.push();love.graphics.translate(x,y);love.graphics.scale(2,2);love.graphics.print(tostring(s),0,0);love.graphics.pop()
end
local function font()
 -- Cozette is a fixed 13-pixel bitmap strike. LÖVE rejects every other size.
 local ok,value=pcall(love.graphics.newFont,'assets/fonts/cozette.otb',13)
 if ok then return value end
 -- The bundled bitmap file is the normal runtime path.  The fallback keeps a
 -- missing/corrupt install readable instead of making an error screen unreadable.
 return love.graphics.newFont(13)
end
function R.new()
 local native=font()
 return setmetatable({small=native,normal=native,title=native,sub=native,zoom=1,panX=0,panY=0},R)
end
function R:layout(app)
 local sw,sh=love.graphics.getDimensions()
 self.sw,self.sh=sw,sh;self.panelX=sw-326
 app.buttons={};local x,y=18,72
 for _,t in ipairs(tools) do
  local bw=self.small:getWidth(t[2])+20
  if x+bw>self.panelX-18 then x=18;y=y+32 end
  app.buttons[#app.buttons+1]={kind=t[1],label=t[2],x=x,y=y,w=bw,h=27};x=x+bw+5
 end
 self.viewport={x=18,y=y+40,w=self.panelX-36,h=sh-y-138}
 self.timeline={x=24,y=sh-65,w=self.panelX-48,h=15}
 app.regionButton=nil;app.siteButtons={}
 if app.campaign and app.history and app.history.view.features.region==1 then
  app.regionButton={x=650,y=14,w=78,h=26}
  local x=738
  for _,site in ipairs(Campaign.sites(app.history.view)) do
   if site.ownerSocietyId==app.history.view.society.id then
    local body=Campaign.body(app.history.view,site.bodyId)
    local label=body and body.name or ('Site '..site.id);local width=math.max(96,self.small:getWidth(label)+22)
    app.siteButtons[#app.siteButtons+1]={siteId=site.id,label=label,x=x,y=14,w=width,h=26};x=x+width+4
   end
  end
 end
end
function R:mapRect(w)
 local v=self.viewport
 local fit=math.min(v.w/w.width,v.h/w.height)
 local scale=(fit>=1 and math.floor(fit) or fit)*self.zoom
 local x=v.x+(v.w-w.width*scale)/2+self.panX
 local y=v.y+(v.h-w.height*scale)/2+self.panY
 self.rect={x=x,y=y,scale=scale,w=w.width*scale,h=w.height*scale}
 return self.rect
end
function R:cell(mx,my)
 local r,v=self.rect,self.viewport
 if not r or mx<v.x or mx>v.x+v.w or my<v.y or my>v.y+v.h then return end
 local x,y=math.floor((mx-r.x)/r.scale)+1,math.floor((my-r.y)/r.scale)+1
 if x<1 or x>r.w/r.scale or y<1 or y>r.h/r.scale then return end
 return x,y
end
function R:refresh(w,view)
 if self.lastWorld==w and self.lastTick==w.tick and self.lastView==view then return end
 if not self.data or self.width~=w.width or self.height~=w.height then
  if self.image then self.image:release();self.data:release() end
  self.data=love.image.newImageData(w.width,w.height,'rgba8')
  self.image=love.graphics.newImage(self.data);self.image:setFilter('nearest','nearest')
  self.width,self.height=w.width,w.height
 end
 for y=1,w.height do for x=1,w.width do
  local i=W.index(w,x,y);local m=w.mat[i];local c=M.def[m].color
  local shade=0.85+((x*13+y*7+x*y)%19)/100
  if m==M.AIR then shade=0.55+y/w.height*0.35
  elseif m==M.ROCK or m==M.ORE or m==M.SOIL then
   if W.get(w,x,y-1)==M.AIR or W.get(w,x-1,y)==M.AIR then shade=shade+0.20 end
  end
  local r,g,b=c[1]*shade,c[2]*shade,c[3]*shade
  local region=w.biomes and B.def[w.biomes[i]]
  if region and region.key~='unclassified' then
   local tint=region.color
   if m==M.ROCK then r,g,b=tint[1]*shade,tint[2]*shade,tint[3]*shade
   elseif m==M.AIR then r,g,b=0.018+tint[1]*0.09,0.022+tint[2]*0.09,0.028+tint[3]*0.09 end
  end
  if view==4 then
   local tint=(region or B.def[0]).color
   local light=m==M.AIR and 0.38 or m==M.BEDROCK and 0.20 or 0.92
   r,g,b=tint[1]*light,tint[2]*light,tint[3]*light
  elseif view==2 then
   if m==M.WATER or m==M.ICE or m==M.STEAM then r,g,b=c[1],c[2],c[3]
   elseif m==M.LAVA then r,g,b=0.9,0.18,0.05
   else r,g,b=r*0.26,g*0.26,b*0.26 end
  end
  self.data:setPixel(x-1,y-1,U.clamp(r),U.clamp(g),U.clamp(b),1)
 end end
 self.image:replacePixels(self.data)
 self.lastWorld,self.lastTick,self.lastView=w,w.tick,view
end
local function dashed(x,y,w,h,c)
 color(c)
 for px=x,x+w,7 do love.graphics.line(px,y,math.min(px+3,x+w),y);love.graphics.line(px,y+h,math.min(px+3,x+w),y+h) end
 for py=y,y+h,7 do love.graphics.line(x,py,x,math.min(py+3,y+h));love.graphics.line(x+w,py,x+w,math.min(py+3,y+h)) end
end
function R:drawMap(app)
 local w=app.currentWorld();local rect=self:mapRect(w);local sc=rect.scale
 local function point(x,y) return rect.x+(x-1)*sc,rect.y+(y-1)*sc end
 local v=self.viewport
 box(v.x,v.y,v.w,v.h,colors.panel)
 love.graphics.setScissor(v.x,v.y,v.w,v.h)
 self:refresh(w,app.view)
 color({1,1,1});love.graphics.draw(self.image,rect.x,rect.y,0,sc,sc)
 if app.grid and sc>=3 then
  color(colors.edge,0.25)
  for x=1,w.width,4 do local px=point(x,1);love.graphics.line(px,rect.y,px,rect.y+rect.h) end
  for y=1,w.height,4 do local _,py=point(1,y);love.graphics.line(rect.x,py,rect.x+rect.w,py) end
 end
 for slot=1,w.cols*w.rows do local s=w.structures[slot]
  if s then
   local px,py=point(s.gx*4-3,s.gy*4-3);local size=4*sc
   if s.kind=='wall' then
    box(px,py,size,size,{0.37,0.37,0.36})
    color({0.20,0.23,0.23});love.graphics.rectangle('line',px,py,size,size)
    love.graphics.line(px,py+size/2,px+size,py+size/2);love.graphics.line(px+size/2,py,px+size/2,py+size/2)
   elseif s.kind=='platform' then box(px,py+size-sc,size,sc,colors.amber)
   elseif s.kind=='ladder' then
    color(colors.amber);love.graphics.setLineWidth(math.max(1,sc/3))
    love.graphics.line(px+sc,py,px+sc,py+size);love.graphics.line(px+3*sc,py,px+3*sc,py+size)
    for k=1,3 do love.graphics.line(px+sc,py+k*sc,px+3*sc,py+k*sc) end love.graphics.setLineWidth(1)
   elseif s.kind=='bed' then
    box(px,py+size-sc,sc,sc,colors.amber);box(px+3*sc,py+size-sc,sc,sc,colors.amber)
    box(px,py+size-2*sc,size,sc,{0.44,0.50,0.52});box(px,py+size-2.5*sc,sc,sc,colors.text)
   elseif s.kind=='store' then
    dashed(px,py,size,size,colors.amber);text('S',px+sc,py+sc,colors.amber,self.small)
   elseif s.kind=='farm' then
    box(px,py+size-sc,size,sc,{0.33,0.24,0.18})
    local height=sc+(s.growth/w.rules.cropTicks)*sc*2
    for k=0,2 do
     box(px+(k+0.5)*sc,py+size-sc-height,sc*0.35,height,colors.green)
     box(px+k*sc,py+size-sc-height,sc*1.3,sc*0.6,s.growth>=w.rules.cropTicks and colors.amber or colors.green)
    end
    if s.tank<=0 then box(px,py,size,2,colors.red) end
   elseif s.kind=='pump' then
    local ix,iy=point(s.intake.x,s.intake.y);local ox,oy=point(s.outlet.x,s.outlet.y)
    color(colors.cyan,0.5);love.graphics.line(ix+sc/2,iy+sc/2,px+size/2,py+size/2,ox+sc/2,oy+sc/2)
    box(ix,iy,sc,sc,colors.cyan);color(colors.amber);love.graphics.rectangle('line',ox,oy,sc,sc)
    box(px+sc,py+sc,2*sc,3*sc,colors.amber);color(colors.text);love.graphics.line(px+sc,py+sc,px+size,py)
   end
  end
 end
 if app.campaign and app.history.view.features.logistics==1 then
  for _,craft in ipairs(Logistics.craftsAt(app.history.view,app.siteId)) do
   local px,py=point(craft.anchor.x,craft.anchor.y)
   box(px-sc*1.5,py-sc*2.8,sc*5,sc*1.7,colors.edge)
   box(px-sc,py-sc*2.4,sc*4,sc*1.2,colors.cyan)
   box(px+sc*0.3,py-sc*3.2,sc*1.2,sc*0.8,colors.amber)
   if sc>=3 then text('S',px+sc*0.5,py-sc*2.5,colors.bg,self.small) end
  end
 end
 for _,j in ipairs(w.jobs) do if j.state=='open' then
  local x,y=point(j.gx*4-3,j.gy*4-3);local sz=4*sc
  local c=j.assigned and colors.cyan or colors.amber
  box(x,y,sz,sz,c,0.12);dashed(x,y,sz,sz,c)
  if sc>=4 then text(j.kind=='dig' and '/' or j.kind=='remove' and 'x' or '+',x+1,y,c,self.small) end
 end end
 for _,cmd in ipairs(app.history.commands[app.history.live.tick+1] or {}) do
  local payload=app.campaign and cmd.siteId==app.siteId and cmd.payload or cmd
  if app.history:atPresent() and payload and payload.type=='order' then local x,y=point(payload.gx*4-3,payload.gy*4-3);dashed(x,y,4*sc,4*sc,colors.cyan) end
 end
 for _,p in ipairs(w.items) do if p.n>0 then
  local x,y=point(p.x,p.y)
  local c=p.kind=='food' and colors.green or p.kind=='water' and colors.cyan or colors.amber
  box(x,y+sc*0.4,sc*1.2,sc*0.6,c)
 end end
 ContentView.draw(w,point,sc,app,self)
 for index,a in ipairs(w.workers) do
  local x,y=point(a.x,a.y-2)
  if a.alive then
   local c=({colors.amber,colors.cyan,colors.green})[(index-1)%3+1]
   if app.view==3 and a.task and a.task.path then
    color(c,0.55);local lastX,lastY=x+sc,y+2*sc
    for k=a.task.next,#a.task.path do
     local xx,yy=W.xy(w,a.task.path[k]);xx,yy=point(xx,yy)
     love.graphics.line(lastX,lastY,xx+sc,yy);lastX,lastY=xx+sc,yy
    end
   end
   box(x+sc*0.25,y,sc*1.5,sc,colors.text)
   box(x,y+sc,sc*2,sc*1.4,c)
   box(x,y+sc*2.3,sc*0.7,sc*0.7,c);box(x+sc*1.3,y+sc*2.3,sc*0.7,sc*0.7,c)
   if a.carry then box(x+sc*1.7,y+sc,sc,sc,colors.amber) end
   box(x,y-sc,2*sc,2,colors.red);box(x,y-sc,2*sc*a.hp/100,2,colors.green)
   if app.selectedWorker==a.id then color(colors.text);love.graphics.rectangle('line',x-2,y-2,2*sc+4,3*sc+4) end
  else
   color(colors.red);love.graphics.line(x,y+sc*2,x+2*sc,y+sc*3);love.graphics.line(x,y+sc*3,x+2*sc,y+sc*2)
  end
 end
 if app.hover then
  local gx,gy=W.tile(w,app.hover.x,app.hover.y);local x,y=point(gx*4-3,gy*4-3)
  color(colors.text,0.55);love.graphics.rectangle('line',x,y,4*sc,4*sc)
 end
 if app.drag then
  local gx,gy=app.drag.gx,app.drag.gy;local ex,ey=gx,gy
  if app.hover then ex,ey=W.tile(w,app.hover.x,app.hover.y) end
  local x,y=point(math.min(gx,ex)*4-3,math.min(gy,ey)*4-3)
  box(x,y,(math.abs(ex-gx)+1)*4*sc,(math.abs(ey-gy)+1)*4*sc,colors.cyan,0.17)
 end
 love.graphics.setScissor()
 color(colors.edge);love.graphics.rectangle('line',v.x,v.y,v.w,v.h)
 local viewNames={'MATERIALS','WATER / HEAT CONTACT','WORKER ROUTES','BIOME SURVEY'}
 text(viewNames[app.view]..'  |  '..w.width..' x '..w.height..' cells',v.x+10,v.y+8,colors.muted,self.small)
 if app.view==4 then
  if w.biomes then
   box(v.x+8,v.y+v.h-116,570,108,colors.bg,0.91)
   for id=1,B.count do local d=B.def[id];local xx=v.x+20+math.floor((id-1)/4)*185;local yy=v.y+v.h-106+((id-1)%4)*24
    box(xx,yy+3,10,10,d.color);text(d.name,xx+17,yy,colors.text,self.small)
   end
  else text('Legacy map: regions were not generated. N opens the new map lab.',v.x+12,v.y+34,colors.amber,self.small) end
 end
 if w.extinct then
  box(v.x+v.w/2-215,v.y+v.h/2-44,430,88,colors.bg,0.96)
  text('THE SETTLEMENT HAS FALLEN',v.x+v.w/2-193,v.y+v.h/2-28,colors.red,self.sub)
  text('Losses stand. Inspect the archive, or press N to begin again.',v.x+v.w/2-193,v.y+v.h/2+8,colors.muted,self.small)
 end
end
local function bar(x,y,width,value,c)
 box(x,y,width,3,colors.edge);box(x,y,width*U.clamp(value,0,1),3,c)
end
function R:sidebar(app)
 local w=app.currentWorld();local x=self.panelX;local pw=308
 box(x,72,pw,self.sh-90,colors.panel)
 text('SETTLERS / H assigns duties',x+16,86,colors.muted,self.small)
 app.crewButtons={}
 local rh=#w.workers>6 and 31 or #w.workers>3 and 40 or 66
 for i,a in ipairs(w.workers) do
  local y=110+(i-1)*rh
  app.crewButtons[#app.crewButtons+1]={id=a.id,x=x+10,y=y-4,w=pw-20,h=rh-3}
  if a.id==app.selectedWorker then box(x+8,y-3,pw-16,rh-4,colors.edge,0.5) end
  text(a.name..(app.orderWorker==a.id and ' [orders]' or ''),x+16,y,a.alive and colors.text or colors.red,self.normal)
  if rh==66 then
   text(a.alive and string.format('HP %d  HUNGER %d  FATIGUE %d',math.floor(a.hp),math.floor(a.hunger),math.floor(a.fatigue)) or 'DEAD / '..(a.reason or ''),x+16,y+21,colors.muted,self.small)
   bar(x+16,y+40,84,a.hp/100,colors.green);bar(x+108,y+40,84,a.hunger/100,colors.amber);bar(x+200,y+40,84,a.fatigue/100,colors.cyan)
  else
   local allocation=w.laborAssignments and w.laborAssignments[a.id]
   text(a.alive and string.format('%d HP / %s',math.floor(a.hp),allocation or 'general') or 'DEAD',x+155,y,colors.muted,self.small)
   bar(x+16,y+21,84,a.hp/100,colors.green);bar(x+108,y+21,84,a.hunger/100,colors.amber);bar(x+200,y+21,84,a.fatigue/100,colors.cyan)
  end
 end
 local y=110+#w.workers*rh+12
 text('INSPECT',x+16,y,colors.muted,self.small);y=y+22
 local a=app.selectedWorker and W.find(w.workers,app.selectedWorker)
 if a then
  text(a.name,x+16,y,colors.text,self.normal);y=y+21
  wrap(a.status..' / '..(a.reason~='' and a.reason or (a.task and a.task.kind or 'No active task')),x+16,y,pw-32,colors.muted,self.small);y=y+38
  wrap('Y: reserve new orders for this worker\nM: rally here / J: release hold\nH: duties and workforce percentages',x+16,y,pw-32,colors.cyan,self.small);y=y+52
 end
 local cell=not a and (app.selectedCell or app.hover)
 if cell then
  local gx,gy=W.tile(w,cell.x,cell.y);local s=W.structureAt(w,cell.x,cell.y)
  text(string.format('Cell %d,%d / block %d,%d',cell.x,cell.y,gx,gy),x+16,y,colors.cyan,self.small);y=y+20
  text(M.def[W.get(w,cell.x,cell.y)].name..(s and ' + '..S.def[s.kind].label or ''),x+16,y,colors.text,self.normal);y=y+24
  local region=B.get(w,cell.x,cell.y)
  text(region.name,x+16,y,region.color,self.small);y=y+21
  local encounter,category=Content.at(w,cell.x,cell.y)
  if encounter then
   local known=w.content.discoveries[category..':'..encounter.kind]
   local registry=category=='flora' and Catalog.flora or category=='fauna' and Catalog.fauna or Catalog.sites
   wrap(known and registry[encounter.kind].name or 'Unclassified '..category,x+16,y,pw-32,colors.amber,self.small);y=y+22
   wrap(category=='fauna' and 'U survey / K cull / F4 field notes' or 'U survey / Z salvage / F4 field notes',x+16,y,pw-32,colors.cyan,self.small);y=y+26
  elseif w.biomes then wrap(region.note,x+16,y,pw-32,colors.muted,self.small);y=y+36 end
  if s then
   wrap(s.status or 'Ready',x+16,y,pw-32,colors.muted,self.small);y=y+24
   if s.kind=='farm' then text(string.format('Growth %d%% / water %d',math.floor(100*s.growth/w.rules.cropTicks),s.tank),x+16,y,colors.green,self.small);y=y+22 end
   if s.kind=='pump' then wrap('I intake / O outlet / T toggle\nHose range: 20 cells.',x+16,y,pw-32,colors.cyan,self.small);y=y+36 end
   if s.kind=='charge' then wrap(s.fuseAt and ('ARMED: '..math.max(0,s.fuseAt-w.tick)..' ticks. No disarm.') or 'T: order a field worker to arm. An 80-tick fuse follows.',x+16,y,pw-32,colors.red,self.small);y=y+36 end
   if s.kind=='ward' then text('Stored water '..s.tank..' / T toggle',x+16,y,colors.cyan,self.small);y=y+24 end
  end
  for _,j in ipairs(w.jobs) do if j.gx==gx and j.gy==gy and j.state=='open' then
   local owner=j.owner and W.find(w.workers,j.owner)
   wrap('Order: '..j.kind..(owner and (' / '..owner.name) or '')..'\n'..j.reason,x+16,y,pw-32,colors.amber,self.small);y=y+42;break
  end end
 end
 if not cell and not a then wrap('H: assign duties and quotas. Click a worker, then Y to reserve new orders. F4 opens field notes.',x+16,y,pw-32,colors.muted,self.normal);y=y+76 end
 y=math.max(y+12,500)
 local available=math.max(0,math.floor((self.sh-y-75)/42))
 if available>0 then
  color(colors.edge);love.graphics.line(x+16,y,x+pw-16,y);y=y+14
  text('CHRONICLE',x+16,y,colors.muted,self.small);y=y+24
  for i=math.max(1,#w.events-available+1),#w.events do local e=w.events[i]
   text(string.format('%05d',e.tick),x+16,y,colors.muted,self.small)
   wrap(e.text,x+62,y,pw-78,e.kind=='death' and colors.red or colors.text,self.small);y=y+42
  end
 end
end
function R:help(app)
 if not app.help then return end
 box(0,0,self.sw,self.sh,colors.bg,0.85)
 local x,y=self.sw/2-370,self.sh/2-285
 box(x,y,740,570,colors.panel);heading(C.title..' / FIELD MANUAL',x+28,y+24,colors.amber,self.title)
 local lines={
  'A living frontier. Colonists eat, sleep, work, explore, and can die.',
  '1. Drag C Farm over empty, supported blocks in the arrival chamber.',
  '2. Build B Beds. Settlers fetch materials, irrigate, harvest and eat.',
  '3. D Dig exposes material cell by cell. Inspect reservoirs before mining.',
  '4. L Ladders traverse shafts. F Floors and W Walls stop liquids.',
  '5. H assigns individual duties and whole-worker percentage quotas.',
  '6. Select a worker: Y reserves orders, M rallies, J releases the hold.',
  '',
  'A: build charge, then select it and T to order arming. No disarm.',
  'Left/Right: step or inspect    Shift+arrows: 20 ticks    Home/End: past/live',
  'U survey / Z salvage / K cull / F4 field notes / F9 ward' ,
  'N: generation lab (C selects local/campaign action) / F2 export / F3 maps / F7 biomes',
  'Frontier campaigns: Shift+F7 or Region opens settlements; transport is pending.',
  'F5: save   F6: diagnostics   F8: benchmark   F10: tests   F12: screenshot',
  '',
  'CHALLENGE: no resurrection, no historical edits, no replacement settlers.',
  'Archive rewind is inspection only. A fresh PRACTICE run permits branching.',
  'Practice only: V cycles a material brush; click places its cells.',
  'Space play/pause / 1-3 speed / wheel zoom / middle pan / README.md',
 }
 for i,line in ipairs(lines) do text(line,x+30,y+80+(i-1)*24,i>=15 and colors.amber or colors.text,self.normal) end
 text('F1 or Escape closes this page.',x+30,y+535,colors.muted,self.small)
end
function R:drawLab(app)
 local n=app.newRun;if not n then return end
 box(0,0,self.sw,self.sh,colors.bg,0.93)
 local pw,ph=math.min(1140,self.sw-40),math.min(738,self.sh-40)
 local x,y=(self.sw-pw)/2,(self.sh-ph)/2
 box(x,y,pw,ph,colors.panel)
 heading(n.imported and 'IMPORT TERRAIN TEMPLATE' or 'GENERATION LAB',x+24,y+20,colors.amber,self.title)
 local action=n.action or 'local_run'
 local actionLabel=action=='campaign_new' and 'NEW FRONTIER CAMPAIGN' or action=='campaign_continue' and 'CONTINUE FRONTIER CAMPAIGN' or 'NEW LOCAL EXPEDITION'
 text('Action: '..actionLabel..'  [C cycles actions]',x+24,y+58,colors.muted,self.small)
 local lx=x+24;local yy=y+94
 text('Seed: '..n.seed..(n.imported and '' or '_'),lx,yy,colors.text,self.sub);yy=yy+29
 text('Scenario: '..n.preset,lx,yy,colors.text,self.normal);yy=yy+28
 if not n.imported then
  text('Layout: '..n.layout,lx,yy,colors.cyan,self.normal);yy=yy+28
  text('Biomes: '..n.climate,lx,yy,colors.text,self.normal);yy=yy+28
  text(string.format('Size: %d x %d',n.width,n.height),lx,yy,colors.text,self.normal);yy=yy+28
  text(string.format('Openness: %.2f',n.openness),lx,yy,colors.text,self.normal);yy=yy+28
  text(string.format('Regions: %.2f / density %.1f',n.biomeScale,n.density or 1),lx,yy,colors.text,self.normal);yy=yy+25
  text('Contents: '..(n.features or 'living')..' / crew '..(n.crew or 3),lx,yy,colors.cyan,self.normal);yy=yy+25
  text('Mode: '..n.mode:upper(),lx,yy,colors.amber,self.normal);yy=yy+32
  local controls={'Digits / Backspace: seed','Left/Right scenario; Up/Down layout','B profile / S size / K crew','F contents / X encounter density','O openness / G region scale','Tab: challenge / practice','Space: local preview / V biomes','C action / F2 export / F3 maps / Enter confirms'}
  for _,line in ipairs(controls) do text(line,lx,yy,colors.muted,self.small);yy=yy+19 end
  if n.preset~='frontier' then wrap('Legacy scenario: layout, content, crew and geology settings are ignored.',lx,yy+6,280,colors.amber,self.small) end
 else
  text(string.format('%d x %d cells',n.width,n.height),lx,yy,colors.text,self.normal);yy=yy+30
  text('Mode: '..n.mode:upper()..' [Tab]',lx,yy,colors.amber,self.normal);yy=yy+35
  wrap('Exact terrain and region cells are loaded. The seed is provenance; import does not call a generator.',lx,yy,278,colors.text,self.normal);yy=yy+92
  wrap('Fresh settlers and supplies are created. Buildings, jobs, injuries and colony history are NOT included.',lx,yy,278,colors.amber,self.normal);yy=yy+104
  text('V: materials / biome preview',lx,yy,colors.muted,self.small);yy=yy+26
  text('Enter: start    Escape: cancel',lx,yy,colors.muted,self.small)
 end
 local px,py,pvw,pvh=x+330,y+94,pw-354,ph-236
 box(px,py,pvw,pvh,colors.bg)
 if n.preview then
  local p=n.preview
  if not self.labRenderer then self.labRenderer=setmetatable({},R) end
  self.labRenderer:refresh(p.world,n.view or 1)
  local scale=math.min(pvw/p.world.width,pvh/p.world.height)
  color({1,1,1});love.graphics.draw(self.labRenderer.image,px+(pvw-p.world.width*scale)/2,py+(pvh-p.world.height*scale)/2,0,scale,scale)
  local r=p.report
  local e=p.world.content
  local summary=string.format('%d biomes | %.1f%% air | %d ruins | %d creatures',r.biomeCount,r.airFraction*100,e and #e.ruins or 0,e and #e.fauna or 0)
  text(summary,px,py+pvh+12,colors.cyan,self.small)
  text(string.format('Water %d   Ice %d   Lava %d   Ore %d',r.water,r.ice,r.lava,r.ore),px,py+pvh+34,colors.muted,self.small)
  local timing=p.generationMs and string.format('Generation only: %.2f ms',p.generationMs) or 'Loaded exact cells; no generation'
  text(timing,px,py+pvh+56,colors.muted,self.small)
  text(action=='local_run' and 'Enter starts THIS local preview. Escape keeps your current colony.' or 'This is a LOCAL preview only; campaign terrain uses derived seeds.',px,py+pvh+82,colors.amber,self.small)
 else
  wrap(action=='campaign_continue' and 'Enter loads only campaign.run.dat. A missing, corrupt, or unsupported campaign save leaves this session unchanged.' or 'Press Space to generate a local preview.\n\nNothing here replaces your live settlement until you confirm the selected action.',px+24,py+42,pvw-48,colors.muted,self.normal)
 end
 if n.error then wrap(n.error,x+24,y+ph-40,pw-48,colors.amber,self.small) end
end
function R:drawMapBrowser(app)
 local b=app.mapBrowser;if not b then return end
 box(0,0,self.sw,self.sh,colors.bg,0.95)
 local pw,ph=math.min(850,self.sw-48),math.min(600,self.sh-48)
 local x,y=(self.sw-pw)/2,(self.sh-ph)/2
 box(x,y,pw,ph,colors.panel)
 heading('MAP LIBRARY',x+24,y+22,colors.amber,self.title)
 wrap('Up / Down selects. Enter validates and previews; Escape cancels. You may also drag a .dwmap.json file onto the window.',x+24,y+68,pw-48,colors.muted,self.normal)
 local visible=math.floor((ph-180)/27);local start=math.max(1,b.index-visible+1)
 for i=start,math.min(#b.entries,start+visible-1) do
  local yy=y+124+(i-start)*27
  if i==b.index then box(x+16,yy-3,pw-32,25,colors.edge) end
  text(b.entries[i],x+26,yy,i==b.index and colors.cyan or colors.text,self.small)
 end
 if #b.entries==0 then wrap('No maps found. F2 exports to maps/ in the save directory. Example maps ship in maps/examples/.',x+24,y+140,pw-48,colors.amber,self.normal) end
 if b.error then wrap(b.error,x+24,y+ph-48,pw-48,colors.red,self.small) end
end
function R:drawRegion(app)
 if not app.region then return end
 local campaign=app.history.view;local region=campaign.region
 if not region then return end
 box(0,0,self.sw,self.sh,colors.bg,0.95)
 local pw,ph=math.min(820,self.sw-48),math.min(590,self.sh-48);local x,y=(self.sw-pw)/2,(self.sh-ph)/2
 box(x,y,pw,ph,colors.panel);heading('REGION / SETTLEMENTS',x+24,y+20,colors.amber,self.title)
 wrap(app.history.view.features.logistics==1 and 'One campaign clock advances every generated landing region. A docked shuttle can be prepared, but departure and founding are pending.' or 'One campaign clock advances every generated landing region. Transport and founding are pending; switching a site changes only this view.',x+24,y+58,pw-152,colors.muted,self.small)
 app.regionButtons={{action='close',x=x+pw-118,y=y+18,w=92,h=28}}
 box(x+pw-118,y+18,92,28,colors.edge);text('ESC close',x+pw-108,y+25,colors.cyan,self.small)
 local sites={};for _,site in ipairs(Campaign.sites(campaign)) do sites[site.id]=site end
 for _,body in ipairs(Campaign.bodies(campaign)) do
  local site=sites[body.siteId];local yy=y+120+(body.id-1)*115;local owned=site.ownerSocietyId==campaign.society.id
  box(x+24,yy,pw-48,94,owned and colors.edge or colors.bg)
  text(body.kind=='planet' and 'PLANET' or 'MOON',x+40,yy+14,colors.muted,self.small)
  text(body.name,x+40,yy+34,owned and colors.cyan or colors.text,self.sub)
  if body.parentBodyId then text('orbits '..Campaign.body(campaign,body.parentBodyId).name,x+250,yy+16,colors.muted,self.small) end
  if owned then
   text(string.format('Owned / %d living crew',W.alive(site.world)),x+250,yy+42,colors.text,self.normal)
   local alerts={};for _,notice in ipairs(region.notices) do if notice.siteId==site.id then alerts[#alerts+1]=notice.kind..(notice.count>1 and (' x'..notice.count) or '') end end
   text(#alerts>0 and ('Alerts @ '..body.name..': '..table.concat(alerts,', ')) or 'No current campaign notices.',x+250,yy+67,#alerts>0 and colors.amber or colors.muted,self.small)
   app.regionButtons[#app.regionButtons+1]={action='site',siteId=site.id,x=x+pw-156,y=yy+31,w=108,h=32}
   box(x+pw-156,yy+31,108,32,site.id==app.siteId and colors.cyan or colors.edge);text(site.id==app.siteId and 'Viewing' or 'View site',x+pw-145,yy+40,colors.bg,self.small)
   if campaign.features.logistics==1 and #Logistics.craftsAt(campaign,site.id)>0 then
    app.regionButtons[#app.regionButtons+1]={action='expedition',siteId=site.id,x=x+pw-156,y=yy+63,w=108,h=24}
    box(x+pw-156,yy+63,108,24,colors.edge);text('Prepare craft',x+pw-145,yy+69,colors.cyan,self.small)
   end
  else
   text('Unvisited — Transport pending',x+250,yy+42,colors.amber,self.normal)
   text('Orbital identity only; underground details remain unavailable.',x+250,yy+67,colors.muted,self.small)
   app.regionButtons[#app.regionButtons+1]={action='unvisited',siteId=site.id,x=x+pw-172,y=yy+31,w=124,h=32}
   box(x+pw-172,yy+31,124,32,colors.edge);text('Summary only',x+pw-160,yy+40,colors.cyan,self.small)
  end
 end
 text('Shift+F7 toggles this overlay. Space controls the global campaign clock.',x+24,y+ph-30,colors.muted,self.small)
end
function R:drawExpedition(app)
 local d=app.expedition;if not d then return end
 local campaign=app.history.view;local source=Campaign.site(campaign,d.sourceSiteId);local vehicle=campaign.logistics and Logistics.craft(campaign,d.craftId)
 if not source or not vehicle then return end
 box(0,0,self.sw,self.sh,colors.bg,0.94)
 local pw,ph=math.min(900,self.sw-44),math.min(680,self.sh-44);local x,y=(self.sw-pw)/2,(self.sh-ph)/2
 box(x,y,pw,ph,colors.panel);heading('PREPARE EXPEDITION',x+24,y+20,colors.amber,self.title)
 d.buttons={}
 local function button(action,label,bx,by,bw,extra)
  local record={action=action,x=bx,y=by,w=bw,h=26};if extra then for key,value in pairs(extra) do record[key]=value end end
  d.buttons[#d.buttons+1]=record;box(bx,by,bw,26,colors.edge);text(label,bx+8,by+6,colors.cyan,self.small)
 end
 button('close','ESC close',x+pw-112,y+18,88)
 local destination=Campaign.site(campaign,d.destinationSiteId);local sourceBody=Campaign.body(campaign,source.bodyId);local destinationBody=destination and Campaign.body(campaign,destination.bodyId)
 text('Craft '..vehicle.id..' at '..(sourceBody and sourceBody.name or ('Site '..source.id)),x+24,y+64,colors.text,self.normal)
 text('Destination: '..(destinationBody and destinationBody.name or 'Choose destination'),x+24,y+91,colors.text,self.normal)
 button('destination','Previous',x+360,y+84,86,{delta=-1});button('destination','Next',x+452,y+84,68,{delta=1})
 local cargoCount=0;for _,kind in ipairs(Logistics.resources()) do cargoCount=cargoCount+(vehicle.cargo[kind] or 0) end
 text(string.format('Cargo %d / %d slots',cargoCount,vehicle.capacity),x+24,y+122,colors.amber,self.normal)
 local manifest=vehicle.activeManifestId and Logistics.manifest(campaign,vehicle.activeManifestId) or nil
 local yy=y+154;text('PASSENGERS (selection does not assemble them)',x+24,yy,colors.muted,self.small);yy=yy+24
 local selected={};for _,id in ipairs(d.passengers) do selected[id]=true end
 for _,worker in ipairs(source.world.workers) do
  local label=(selected[worker.personId] and '[x] ' or '[ ] ')..worker.name..' / person '..worker.personId..(worker.alive and '' or ' / DEAD')
  button('passenger',label,x+24,yy,250,{personId=worker.personId});yy=yy+30
 end
 local cargoY=y+154;text('CARGO TARGET / ACTUAL ABOARD',x+350,cargoY,colors.muted,self.small);cargoY=cargoY+25
 for _,resource in ipairs(Logistics.resources()) do
  local target=manifest and (manifest.cargo[resource] or 0) or (d.cargo[resource] or 0);local actual=vehicle.cargo[resource] or 0
  text(resource..': '..actual..' / '..target,x+350,cargoY+5,colors.text,self.normal)
  button('cargo','-',x+490,cargoY,26,{resource=resource,delta=-1});button('cargo','+',x+522,cargoY,26,{resource=resource,delta=1})
  if actual>0 and (not manifest or actual>(manifest.cargo[resource] or 0)) then button('unload','Unload 1',x+555,cargoY,76,{resource=resource}) end
  cargoY=cargoY+34
 end
 local canEdit=app.history:atPresent() or campaign.mode=='practice'
 local statusY=y+390
 if manifest then
  local ready,reason=Logistics.readiness(campaign,manifest)
  text(ready and 'PREPARATION READY' or ('BLOCKED: '..reason),x+24,statusY,ready and colors.green or colors.amber,self.normal)
  text('Manifest '..manifest.id..' revision '..manifest.revision..' / source '..manifest.sourceSiteId..' -> destination '..manifest.destinationSiteId,x+24,statusY+25,colors.muted,self.small)
  if canEdit then button('prepare','Apply changes',x+24,statusY+58,112);button('assemble','Assemble crew',x+142,statusY+58,112,{manifestId=manifest.id});button('cancel','Cancel preparation',x+260,statusY+58,132,{manifestId=manifest.id}) end
 else
  text('Set passengers and cargo, then prepare physical loading work.',x+24,statusY,colors.muted,self.normal)
  if canEdit then button('prepare','Prepare expedition',x+24,statusY+42,142) end
 end
 for _,op in ipairs(campaign.logistics.operations) do if op.craftId==vehicle.id then
  text('Unloading '..op.resource..': '..(op.amount-op.remaining)..' / '..op.amount,x+24,statusY+100,colors.muted,self.small)
  if canEdit then button('cancelUnload','Cancel unloading',x+220,statusY+94,120,{operationId=op.id}) end
 end end
 if not canEdit then text('Challenge archive: inspection only.',x+24,y+ph-42,colors.amber,self.normal) end
 text('Departure becomes available in the next implementation tranche. Cargo aboard is not a settlement stockpile.',x+24,y+ph-24,colors.muted,self.small)
end
function R:draw(app)
 self:layout(app)
 love.graphics.clear(colors.bg)
 local w=app.currentWorld()
 heading(C.title,18,16,colors.text,self.title)
 text('A SETTLEMENT UNDER PRESSURE',196,24,colors.muted,self.small)
 if self.metricsWorld~=w or not self.metricsTick or math.abs(w.tick-self.metricsTick)>=20 then
  self.metrics=Metrics.measure(w);self.metricsTick=w.tick;self.metricsWorld=w
 end
 local met=self.metrics
 local state=not app.history:atPresent() and 'ARCHIVE / READ ONLY' or app.paused and 'PAUSED' or 'RUNNING'
 text(state,470,16,app.history:atPresent() and colors.amber or colors.cyan,self.normal)
 text(string.format('%s  |  %s  |  seed %d',w.mode:upper(),w.preset,w.seed),470,39,colors.muted,self.small)
 text(string.format('SURVIVORS %d/%d   FOOD %d   FARMS %d',met.alive,#w.workers,met.food,met.farms),self.panelX-10,18,colors.text,self.normal)
 text(string.format('tick %d    day %.2f    %dx speed',w.tick,w.tick/C.dayTicks,app.speed),self.panelX-10,41,colors.muted,self.small)
 for _,b in ipairs(app.buttons) do
  box(b.x,b.y,b.w,b.h,app.tool==b.kind and colors.edge or colors.panel)
  text(b.label,b.x+10,b.y+6,app.tool==b.kind and colors.amber or colors.text,self.small)
 end
 if app.regionButton then
  box(app.regionButton.x,app.regionButton.y,app.regionButton.w,app.regionButton.h,app.region and colors.cyan or colors.edge)
  text('Region',app.regionButton.x+12,app.regionButton.y+6,colors.bg,self.small)
  for _,b in ipairs(app.siteButtons) do
   box(b.x,b.y,b.w,b.h,b.siteId==app.siteId and colors.cyan or colors.edge)
   text(b.label,b.x+10,b.y+6,colors.bg,self.small)
  end
 end
 self:drawMap(app);self:sidebar(app)
 local t=self.timeline
 box(t.x,t.y,t.w,t.h,colors.edge)
 local ratio=app.history.frontier>0 and w.tick/app.history.frontier or 0
 box(t.x,t.y,t.w*ratio,t.h,colors.cyan,0.55)
 box(t.x+t.w*ratio-2,t.y-3,4,t.h+6,colors.text)
 text('0',t.x,t.y+21,colors.muted,self.small)
 text(string.format('ARCHIVE  %d / %d',w.tick,app.history.frontier),t.x+40,t.y+21,colors.muted,self.small)
 text('H crew / F4 notes / N world / F1 help',t.x+t.w-278,t.y+21,colors.muted,self.small)
 local status=app.toast or string.format('Priority %d  |  tick %.2f ms  |  Lua heap %.1f MiB  |  grid %s',app.priority,(app.tickMs or 0),collectgarbage('count')/1024,app.grid and 'on' or 'off')
 if app.orderWorker and not app.toast then local a=W.find(w.workers,app.orderWorker);status='New orders: '..(a and a.name or '?')..' (Y clears) | '..status end
 if app.history.seekTarget then status='Replaying archive: '..w.tick..' -> '..app.history.seekTarget end
 if app.port then status='Click '..app.port..' position. The hose must stay within 20 cells of the pump.' end
 if app.benchmark then status='Benchmark: isolated from this settlement / '..(app.benchmarkStatus or 'starting') end
 text(status,24,self.sh-91,colors.amber,self.small)
 self:drawLab(app)
 self:drawMapBrowser(app)
 self:drawRegion(app)
 self:drawExpedition(app)
 Crew.draw(app,self)
 Notes.draw(app,self)
 self:help(app)
end
return R
