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
local Knowledge=require('src.knowledge')
local Crew=require('src.ui.crew')
local Notes=require('src.ui.fieldnotes')
local ContentView=require('src.ui.world_content')
local Campaign=require('src.campaign')
local Logistics=require('src.logistics')
local Travel=require('src.travel')
local Education=require('src.education')
local ActionHud=require('src.ui.action_hud')
local Body=require('src.body')
local Visibility=require('src.visibility')
local R={};R.__index=R
-- Presentation-only ordered stipple for remembered fog. Simulation memory
-- remains the authoritative last-seen snapshot in src.visibility.
local fogBayer={0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5}
local function rememberedFogFactor(x,y)
 local rank=fogBayer[((y-1)%4)*4+((x-1)%4)+1]
 return rank<8 and 0.035 or 0.16
end
local colors={bg={0.039,0.053,0.067},panel={0.070,0.085,0.103},edge={0.19,0.23,0.26},
 text={0.86,0.88,0.86},muted={0.49,0.56,0.59},amber={0.89,0.66,0.35},
 cyan={0.35,0.72,0.74},red={0.88,0.35,0.29},green={0.48,0.71,0.42}}
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
 self.sw,self.sh=sw,sh;self.panelX=sw-326;app.buttons={}
 self.viewport={x=18,y=72,w=self.panelX-36,h=sh-210}
 self.timeline={x=24,y=sh-65,w=self.panelX-48,h=15}
 app.regionButton=nil;app.siteButtons={};app.schoolButton=nil
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

function R:drawActionHud(app)
 local hud=app.hud;if not hud then return end
 local model=ActionHud.model(app,app.currentWorld())
 if not model or #model.actions==0 then app.hud=nil;return end
 local columns=#model.actions>7 and 2 or 1
 local bw=columns==2 and 184 or 278;local rows=math.ceil(#model.actions/columns)
 local pw,ph=columns*bw+36,rows*30+70
 local v=self.viewport;local x=U.clamp(hud.x or v.x,v.x,v.x+v.w-pw);local y=U.clamp(hud.y or v.y,v.y,v.y+v.h-ph)
 hud.buttons={};hud.model=model
 box(x,y,pw,ph,colors.bg,0.94);box(x+2,y+2,pw-4,ph-4,colors.panel)
 text('DELEGATE / '..model.title,x+14,y+12,colors.amber,self.normal)
 local detail=model.area and string.format('%d blocks — batch actions queue ordinary work%s',model.count,model.count>model.limit and ('; first '..model.limit..' only') or '') or string.format('block %d,%d — actions queue ordinary work',model.gx,model.gy)
 text(detail,x+14,y+35,colors.muted,self.small)
 for index,action in ipairs(model.actions) do
  local column=(index-1)%columns;local row=math.floor((index-1)/columns)
  local bx,by=x+14+column*bw,y+56+row*30;local record={action=action.id,x=bx,y=by,w=bw-10,h=25,hint=action.hint}
  hud.buttons[#hud.buttons+1]=record
  box(bx,by,record.w,record.h,colors.edge);text(action.label,bx+8,by+6,colors.cyan,self.small)
 end
end
function R:mapRect(w)
 local v=self.viewport
 local fit=math.min(v.w/w.width,v.h/w.height)
 local scale=(fit>=1 and math.floor(fit) or fit)*self.zoom
 local maxX=math.max(0,(w.width*scale-v.w)/2);local maxY=math.max(0,(w.height*scale-v.h)/2)
 self.panX=U.clamp(self.panX,-maxX,maxX);self.panY=U.clamp(self.panY,-maxY,maxY)
 local x=v.x+(v.w-w.width*scale)/2+self.panX
 local y=v.y+(v.h-w.height*scale)/2+self.panY
 self.rect={x=x,y=y,scale=scale,w=w.width*scale,h=w.height*scale}
 return self.rect
end
function R:pan(w,dx,dy)
 local v=self.viewport;local fit=math.min(v.w/w.width,v.h/w.height)
 local scale=(fit>=1 and math.floor(fit) or fit)*self.zoom
 local maxX=math.max(0,(w.width*scale-v.w)/2);local maxY=math.max(0,(w.height*scale-v.h)/2)
 self.panX=U.clamp(self.panX+dx,-maxX,maxX);self.panY=U.clamp(self.panY+dy,-maxY,maxY)
end
function R:cell(mx,my)
 local r,v=self.rect,self.viewport
 if not r or mx<v.x or mx>v.x+v.w or my<v.y or my>v.y+v.h then return end
 local x,y=math.floor((mx-r.x)/r.scale)+1,math.floor((my-r.y)/r.scale)+1
 if x<1 or x>r.w/r.scale or y<1 or y>r.h/r.scale then return end
 return x,y
end
function R:refresh(w,view,context)
 if self.lastWorld==w and self.lastTick==w.tick and self.lastView==view and self.lastVisibility==w.visibility then return end
 if not self.data or self.width~=w.width or self.height~=w.height then
  if self.image then self.image:release();self.data:release() end
  self.data=love.image.newImageData(w.width,w.height,'rgba8')
  self.image=love.graphics.newImage(self.data);self.image:setFilter('nearest','nearest')
  self.width,self.height=w.width,w.height
 end
 local sight=Visibility.enabled(w) and Visibility.derive(w,context) or nil
 for y=1,w.height do for x=1,w.width do
  local i=W.index(w,x,y);local current=not sight or sight.current[i];local remembered=nil
  local m=current and w.mat[i] or Visibility.memoryAt(w,x,y)
  if not m then self.data:setPixel(x-1,y-1,0.006,0.009,0.012,1)
  else
  local c=M.def[m].color
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
  if current and view==4 then
   local tint=(region or B.def[0]).color
   local light=m==M.AIR and 0.38 or m==M.BEDROCK and 0.20 or 0.92
   r,g,b=tint[1]*light,tint[2]*light,tint[3]*light
  elseif current and view==2 then
   if m==M.WATER or m==M.ICE or m==M.STEAM then r,g,b=c[1],c[2],c[3]
   elseif m==M.LAVA then r,g,b=0.9,0.18,0.05
   else r,g,b=r*0.26,g*0.26,b*0.26 end
  end
  if sight then
   local light=sight.light[i] or 0
   local factor=current and (light>0 and U.clamp(0.22+light/(Visibility.shuttleRadius+1)*0.78,0.22,1) or 0.20) or rememberedFogFactor(x,y)
   r,g,b=r*factor,g*factor,b*factor
  end
  self.data:setPixel(x-1,y-1,U.clamp(r),U.clamp(g),U.clamp(b),1)
  end
 end end
 self.image:replacePixels(self.data)
 self.lastWorld,self.lastTick,self.lastView,self.lastVisibility=w,w.tick,view,w.visibility
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
 local visibilityContext=app.campaign and {campaign=app.history.view,siteId=app.siteId} or nil
 self:refresh(w,app.view,visibilityContext)
 color({1,1,1});love.graphics.draw(self.image,rect.x,rect.y,0,sc,sc)
 if app.grid and sc>=3 then
  color(colors.edge,0.25)
  for x=1,w.width,4 do local px=point(x,1);love.graphics.line(px,rect.y,px,rect.y+rect.h) end
  for y=1,w.height,4 do local _,py=point(1,y);love.graphics.line(rect.x,py,rect.x+rect.w,py) end
 end
 for slot=1,w.cols*w.rows do local s=w.structures[slot]
  if s then
   local visible=not Visibility.enabled(w) or Visibility.currentlyVisible(w,s.gx*4-2,s.gy*4-2,visibilityContext)
   local remembered=Visibility.enabled(w) and not visible and w.visibility.structures[slot]
   if visible or remembered then
   local shown=visible and s or remembered
   local px,py=point(shown.gx*4-3,shown.gy*4-3);local size=4*sc
   if not visible then box(px,py,size,size,colors.muted,0.15)
   elseif s.kind=='wall' then
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
   elseif s.kind=='field_school' then
    box(px,py+size-sc,size,sc,{0.43,0.31,0.18})
    box(px+sc,py+sc,2*sc,2*sc,colors.cyan)
    color(colors.amber);love.graphics.rectangle('line',px+sc,py+sc,2*sc,2*sc)
    if sc>=4 then text('F',px+sc*1.2,py+sc*1.2,colors.bg,self.small) end
   elseif s.kind=='torch' then
    local mount=S.torchMount(w,s.gx,s.gy)
    if mount=='left' then
     box(px+sc*.45,py+sc*1.2,sc*.6,sc*1.8,colors.amber);box(px+sc*.7,py+sc*.7,sc*1.35,sc,colors.amber)
    elseif mount=='right' then
     box(px+size-sc*1.05,py+sc*1.2,sc*.6,sc*1.8,colors.amber);box(px+size-sc*2.05,py+sc*.7,sc*1.35,sc,colors.amber)
    else
     box(px+sc*1.7,py+sc,sc*.6,sc*2.5,colors.amber);box(px+sc*1.3,py+sc*.5,sc*1.4,sc,colors.amber)
    end
   end
   end
  end
 end
 if app.campaign and app.history.view.features.logistics==1 then
  for _,craft in ipairs(Logistics.craftsAt(app.history.view,app.siteId)) do
   if not Visibility.enabled(w) or Visibility.currentlyVisible(w,craft.anchor.x,craft.anchor.y,visibilityContext) then
   local px,py=point(craft.anchor.x,craft.anchor.y)
   box(px-sc*1.5,py-sc*2.8,sc*5,sc*1.7,colors.edge)
   box(px-sc,py-sc*2.4,sc*4,sc*1.2,colors.cyan)
   box(px+sc*0.3,py-sc*3.2,sc*1.2,sc*0.8,colors.amber)
   if sc>=3 then text('S',px+sc*0.5,py-sc*2.5,colors.bg,self.small) end
   end
  end
 end
 for _,j in ipairs(w.jobs) do if j.state=='open' then
  if not Visibility.enabled(w) or Visibility.currentlyVisible(w,j.gx*4-2,j.gy*4-2,visibilityContext) then
  local x,y=point(j.gx*4-3,j.gy*4-3);local sz=4*sc
  local c=j.assigned and colors.cyan or colors.amber
  box(x,y,sz,sz,c,0.12);dashed(x,y,sz,sz,c)
  if sc>=4 then text(j.kind=='dig' and '/' or j.kind=='remove' and 'x' or '+',x+1,y,c,self.small) end
  end
 end end
 for _,cmd in ipairs(app.history.commands[app.history.live.tick+1] or {}) do
  local payload=app.campaign and cmd.siteId==app.siteId and cmd.payload or cmd
  if app.history:atPresent() and payload and payload.type=='order' then local x,y=point(payload.gx*4-3,payload.gy*4-3);dashed(x,y,4*sc,4*sc,colors.cyan) end
 end
 for _,p in ipairs(w.items) do if p.n>0 and (not Visibility.enabled(w) or Visibility.currentlyVisible(w,p.x,p.y,visibilityContext)) then
  local x,y=point(p.x,p.y)
  local c=p.kind=='food' and colors.green or p.kind=='water' and colors.cyan or colors.amber
  box(x,y+sc*0.4,sc*1.2,sc*0.6,c)
 end end
 ContentView.draw(w,point,sc,app,self,visibilityContext)
 for index,a in ipairs(w.workers) do
  local x1,y1,_,y2=Body.rect(w,a.x,a.y)
  local x,y=point(x1,y1);local bodyHeight=(y2-y1+1)*sc
  if a.alive and (not Visibility.enabled(w) or Visibility.currentlyVisible(w,a.x,a.y,visibilityContext)) then
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
   box(x,y+bodyHeight-sc*0.7,sc*0.7,sc*0.7,c);box(x+sc*1.3,y+bodyHeight-sc*0.7,sc*0.7,sc*0.7,c)
   if a.carry then box(x+sc*1.7,y+sc,sc,sc,colors.amber) end
   box(x,y-sc,2*sc,2,colors.red);box(x,y-sc,2*sc*a.hp/100,2,colors.green)
   if app.selectedWorker==a.id then color(colors.text);love.graphics.rectangle('line',x-2,y-2,2*sc+4,bodyHeight+4) end
  else
   color(colors.red);love.graphics.line(x,y+bodyHeight-sc,x+2*sc,y+bodyHeight);love.graphics.line(x,y+bodyHeight,x+2*sc,y+bodyHeight-sc)
  end
 end
 if app.hover then
  local gx,gy=W.tile(w,app.hover.x,app.hover.y);local x,y=point(gx*4-3,gy*4-3)
  color(colors.text,0.55);love.graphics.rectangle('line',x,y,4*sc,4*sc)
 end
 local function drawBlockArea(gx1,gy1,gx2,gy2,c,alpha)
  local x,y=point(math.min(gx1,gx2)*4-3,math.min(gy1,gy2)*4-3)
  local width,height=(math.abs(gx2-gx1)+1)*4*sc,(math.abs(gy2-gy1)+1)*4*sc
  box(x,y,width,height,c,alpha);dashed(x,y,width,height,c)
 end
 if app.selection then
  local s=app.selection;drawBlockArea(s.gx1,s.gy1,s.gx2,s.gy2,colors.green,0.16)
 end
 if app.selectionDrag then
  local d=app.selectionDrag;local ex,ey=d.gx,d.gy
  if app.hover then ex,ey=W.tile(w,app.hover.x,app.hover.y) end
  drawBlockArea(d.gx,d.gy,ex,ey,colors.cyan,0.13)
 end
 if app.drag then
  local gx,gy=app.drag.gx,app.drag.gy;local ex,ey=gx,gy
  if app.hover then ex,ey=W.tile(w,app.hover.x,app.hover.y) end
  drawBlockArea(gx,gy,ex,ey,colors.cyan,0.17)
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
  wrap('Right-click a target to delegate actions.\nY: reserve new orders / M: rally / J: release\nH: duties and workforce percentages',x+16,y,pw-32,colors.cyan,self.small);y=y+52
 end
 if app.selection and not a then
  local s=app.selection
  text(string.format('%d BLOCKS SELECTED',s.count or (s.gx2-s.gx1+1)*(s.gy2-s.gy1+1)),x+16,y,colors.green,self.normal);y=y+23
  wrap('Right-click inside the selection to queue one build or cancel action for each block. Left-click a selected block or Escape clears it.',x+16,y,pw-32,colors.cyan,self.small);y=y+53
 end
 local cell=not a and (app.selectedCell or app.hover)
 if cell then
  local context=app.campaign and {campaign=app.history.view,siteId=app.siteId} or nil
  local current=not Visibility.enabled(w) or Visibility.currentlyVisible(w,cell.x,cell.y,context)
  local remembered=Visibility.memoryAt(w,cell.x,cell.y)
  local gx,gy=W.tile(w,cell.x,cell.y)
  text(string.format('Cell %d,%d / block %d,%d',cell.x,cell.y,gx,gy),x+16,y,colors.cyan,self.small);y=y+20
  if not current then
   text(remembered and ('Last known: '..M.def[remembered].name) or 'Unexplored',x+16,y,remembered and colors.muted or colors.amber,self.normal);y=y+24
   text('Current terrain and structures are not visible.',x+16,y,colors.muted,self.small);y=y+21
  else
  local s=W.structureAt(w,cell.x,cell.y)
  text(M.def[W.get(w,cell.x,cell.y)].name..(s and ' + '..S.def[s.kind].label or ''),x+16,y,colors.text,self.normal);y=y+24
  local region=B.get(w,cell.x,cell.y)
  text(region.name,x+16,y,region.color,self.small);y=y+21
  local encounter,category=Content.at(w,cell.x,cell.y)
  if encounter then
   local personal=w.frontier and w.frontier.knowledge==1
   local known=personal and Knowledge.identified(a,category,encounter.kind) or w.content.discoveries[category..':'..encounter.kind]
   local registry=category=='flora' and Catalog.flora or category=='fauna' and Catalog.fauna or Catalog.sites
   wrap(known and registry[encounter.kind].name or ('Unidentified '..(category=='flora' and 'growth' or category=='fauna' and 'creature' or 'site')),x+16,y,pw-32,colors.amber,self.small);y=y+22
   wrap(category=='fauna' and 'Right-click: survey or cull / F4 field notes' or 'Right-click: survey, study, or salvage / F4 field notes',x+16,y,pw-32,colors.cyan,self.small);y=y+26
  elseif w.biomes then wrap(region.note,x+16,y,pw-32,colors.muted,self.small);y=y+36 end
  if s then
   wrap(s.status or 'Ready',x+16,y,pw-32,colors.muted,self.small);y=y+24
   if s.kind=='farm' then text(string.format('Growth %d%% / water %d',math.floor(100*s.growth/w.rules.cropTicks),s.tank),x+16,y,colors.green,self.small);y=y+22 end
   if s.kind=='pump' then wrap('I intake / O outlet / T toggle\nHose range: 20 cells.',x+16,y,pw-32,colors.cyan,self.small);y=y+36 end
   if s.kind=='charge' then wrap(s.fuseAt and ('ARMED: '..math.max(0,s.fuseAt-w.tick)..' ticks. No disarm.') or 'T: order a field worker to arm. An 80-tick fuse follows.',x+16,y,pw-32,colors.red,self.small);y=y+36 end
   if s.kind=='ward' then text('Stored water '..s.tank..' / T toggle',x+16,y,colors.cyan,self.small);y=y+24 end
   if s.kind=='field_school' and s.education then
    local data=s.education
    text((data.enabled and 'Enabled' or 'Disabled')..' / '..data.mode..' / priority '..data.priority,x+16,y,colors.cyan,self.small);y=y+20
    local topic=data.topicId and Knowledge.spec(data.topicId)
    wrap(topic and topic.label or 'No school topic selected.',x+16,y,pw-32,colors.muted,self.small);y=y+24
    app.schoolButton={slot=W.slot(w,s.gx,s.gy),schoolId=data.id,x=x+16,y=y,w=146,h=25}
    box(x+16,y,146,25,colors.edge);text('School policy',x+25,y+6,colors.cyan,self.small);y=y+34
   end
  end
  for _,j in ipairs(w.jobs) do if j.gx==gx and j.gy==gy and j.state=='open' then
   local owner=j.owner and W.find(w.workers,j.owner)
   wrap('Order: '..j.kind..(owner and (' / '..owner.name) or '')..'\n'..j.reason,x+16,y,pw-32,colors.amber,self.small);y=y+42;break
  end end
 for _,rope in ipairs(w.ropes or {}) do
  local visible=not Visibility.enabled(w) or Visibility.currentlyVisible(w,rope.laneLeftX,rope.anchorY,visibilityContext)
  if visible then
   local px,py=point(rope.laneLeftX,rope.anchorY);color(colors.amber);love.graphics.setLineWidth(math.max(1,sc/3));love.graphics.line(px+sc,py,px+sc,py+rope.length*sc)
  end
 end
  end
 end
 if not cell and not a and not app.selection then wrap('H: assign duties and quotas. Select a block or worker, then right-click to delegate actions. F4 opens field notes.',x+16,y,pw-32,colors.muted,self.normal);y=y+76 end
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
  '4. L unfurls rope downward; Shift+L unfurls upward. F Floors and W Walls stop liquids.',
  '5. H assigns individual duties and whole-worker percentage quotas.',
  '6. Left-drag selects blocks. Right-click inside them for batch actions; click a selected block or Escape clears it.',
  '',
  'A: build charge, then select it and T to order arming. No disarm.',
  'Shift+drag pans the camera. Wheel zooms; R fits the map. Left/Right: select or step    Shift+arrows: 20 ticks',
  'Encounter HUD: survey / study / salvage / cull. F4 field notes / F9 ward.',
  'N: generation lab (C selects local/campaign action) / F2 export / F3 maps / F7 biomes',
  'Frontier campaigns: Shift+F7 or Region opens settlements; transport is pending.',
  'F5: save   Ctrl+Q: quit   F6: diagnostics   F8: benchmark   F10: tests   F12: screenshot',
  '',
  'CHALLENGE: no resurrection, no historical edits, no replacement settlers.',
  'Archive rewind is inspection only. A fresh PRACTICE run permits branching.',
  'Practice only: V cycles a material brush; click places its cells.',
  'Space play/pause / 1-3 speed / camera pan and block selection / README.md',
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
 local travel=campaign.features.travel==1
 wrap(travel and 'One campaign clock advances every generated landing region. Docked shuttles can be prepared and launched; travelling crews remain under this same clock.' or campaign.features.logistics==1 and 'One campaign clock advances every generated landing region. A docked shuttle can be prepared, but departure and founding are pending.' or 'One campaign clock advances every generated landing region. Transport and founding are pending; switching a site changes only this view.',x+24,y+58,pw-152,colors.muted,self.small)
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
 if travel then
  local yy=y+470
  for _,craft in ipairs(campaign.logistics.crafts) do
   local journey=craft.journey
   if journey then
    local fromId,toId=journey.leg=='outbound' and journey.originSiteId or journey.destinationSiteId,journey.leg=='outbound' and journey.destinationSiteId or journey.originSiteId
    local from,to=Campaign.body(campaign,fromId),Campaign.body(campaign,toId)
    local label=journey.status=='holding' and ('Holding at '..(to and to.name or ('site '..toId))) or ('Travelling '..(from and from.name or ('site '..fromId))..' -> '..(to and to.name or ('site '..toId))..' / '..journey.remainingTicks..' ticks')
    text('Craft '..craft.id..': '..label,x+24,yy,journey.status=='holding' and colors.amber or colors.cyan,self.small)
    app.regionButtons[#app.regionButtons+1]={action='expedition',siteId=journey.originSiteId,craftId=craft.id,x=x+pw-156,y=yy-5,w=108,h=24}
    box(x+pw-156,yy-5,108,24,colors.edge);text('View craft',x+pw-144,yy+1,colors.cyan,self.small)
    yy=yy+26
   end
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
 local journey=vehicle.journey
 if journey then
  local fromId,toId=journey.leg=='outbound' and journey.originSiteId or journey.destinationSiteId,journey.leg=='outbound' and journey.destinationSiteId or journey.originSiteId
  local from,to=Campaign.body(campaign,fromId),Campaign.body(campaign,toId)
  heading(journey.status=='holding' and 'CRAFT HOLDING' or 'CRAFT IN TRANSIT',x+24,y+58,journey.status=='holding' and colors.amber or colors.cyan,self.sub)
  text('Craft '..vehicle.id..': '..(from and from.name or ('Site '..fromId))..' -> '..(to and to.name or ('Site '..toId)),x+24,y+102,colors.text,self.normal)
  text(journey.status=='holding' and ('Landing blocked: '..journey.holdingReason) or ('Remaining: '..journey.remainingTicks..' campaign ticks'),x+24,y+130,journey.status=='holding' and colors.amber or colors.muted,self.normal)
  local living,total=0,#vehicle.passengers;for _,passenger in ipairs(vehicle.passengers) do if passenger.alive then living=living+1 end end
  text('Passengers: '..living..' living / '..total..' total',x+24,y+164,colors.text,self.normal)
  local cargo={};for _,kind in ipairs(Logistics.resources()) do if (vehicle.cargo[kind] or 0)>0 then cargo[#cargo+1]=kind..' '..vehicle.cargo[kind] end end
  wrap('Transit cargo: '..(#cargo>0 and table.concat(cargo,', ') or 'empty')..'. Docked cargo remains unavailable to ordinary settlement work until physically unloaded.',x+24,y+194,pw-48,colors.muted,self.small)
  local canEdit=app.history:atPresent() or campaign.mode=='practice'
  if journey.status=='holding' and journey.leg=='outbound' then
   local ok,reason=Travel.valid(campaign,{scope='campaign',type='return_to_origin',craftId=vehicle.id,journeyId=journey.id,expectedLeg='outbound'})
   text(ok and 'Return to origin costs one metal aboard.' or ('Return unavailable: '..reason),x+24,y+258,ok and colors.amber or colors.muted,self.normal)
   if canEdit and ok then button('return','Return to origin',x+24,y+292,146,{journeyId=journey.id}) end
  elseif journey.status=='holding' then
   text('This craft has already used its one allowed reversal.',x+24,y+258,colors.amber,self.normal)
  end
  if not canEdit then text('Challenge archive: inspection only.',x+24,y+ph-42,colors.amber,self.normal) end
  return
 end
 local destination=Campaign.site(campaign,d.destinationSiteId);local sourceBody=Campaign.body(campaign,source.bodyId);local destinationBody=destination and Campaign.body(campaign,destination.bodyId)
 text('Craft '..vehicle.id..' at '..(sourceBody and sourceBody.name or ('Site '..source.id)),x+24,y+64,colors.text,self.normal)
 text('Destination: '..(destinationBody and destinationBody.name or 'Choose destination'),x+24,y+91,colors.text,self.normal)
 button('destination','Previous',x+360,y+84,86,{delta=-1});button('destination','Next',x+452,y+84,68,{delta=1})
 local Equipment=require('src.equipment')
 local cargoCount=0;for _,kind in ipairs(Logistics.resources()) do cargoCount=cargoCount+(vehicle.cargo[kind] or 0) end
 cargoCount=cargoCount+Equipment.craftCount(campaign,vehicle.id)
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
 if campaign.features.equipment==1 then
  local loose=Equipment.forSite(campaign,source.id,nil,'loose')
  local aboard={};for _,item in ipairs(campaign.equipment.items) do if item.state=='craft' and item.craftId==vehicle.id then aboard[#aboard+1]=item end end
  table.sort(aboard,function(a,b)return a.id<b.id end)
  local function itemLabel(item) return (item.kind=='pickaxe' and 'Pickaxe' or 'Rope coil')..' #'..item.id end
  local held={};for _,item in ipairs(aboard) do held[#held+1]=itemLabel(item) end
  text('TOOLS / ACTUAL CUSTODY',x+630,y+154,colors.muted,self.small)
  wrap(#held>0 and ('Aboard: '..table.concat(held,', ')) or 'Aboard: none.',x+630,y+178,pw-650,colors.text,self.small)
  local toolY=y+220
  for i,item in ipairs(loose) do
   if i>3 then break end
   text(itemLabel(item),x+630,toolY+5,colors.text,self.small)
   if app.siteId==source.id then button('loadTool','Load',x+770,toolY,58,{equipmentId=item.id}) end
   toolY=toolY+29
  end
  if #loose==0 then text('No loose local tools.',x+630,toolY+5,colors.muted,self.small) end
 end
 local canEdit=app.history:atPresent() or campaign.mode=='practice'
 local statusY=y+390
 if manifest then
  local ready,reason=Logistics.readiness(campaign,manifest)
  text(ready and 'PREPARATION READY' or ('BLOCKED: '..reason),x+24,statusY,ready and colors.green or colors.amber,self.normal)
  text('Manifest '..manifest.id..' revision '..manifest.revision..' / source '..manifest.sourceSiteId..' -> destination '..manifest.destinationSiteId,x+24,statusY+25,colors.muted,self.small)
  if canEdit then
   button('prepare','Apply changes',x+24,statusY+58,112);button('assemble','Assemble crew',x+142,statusY+58,112,{manifestId=manifest.id});button('cancel','Cancel preparation',x+260,statusY+58,132,{manifestId=manifest.id})
   if campaign.features.travel==1 then button('launch','Launch',x+398,statusY+58,86,{manifestId=manifest.id,revision=manifest.revision}) end
  end
 else
  text('Set passengers and cargo, then prepare physical loading work.',x+24,statusY,colors.muted,self.normal)
  if canEdit then button('prepare','Prepare expedition',x+24,statusY+42,142) end
 end
 for _,op in ipairs(campaign.logistics.operations) do if op.craftId==vehicle.id then
  text('Unloading '..op.resource..': '..(op.amount-op.remaining)..' / '..op.amount,x+24,statusY+100,colors.muted,self.small)
  if canEdit then button('cancelUnload','Cancel unloading',x+220,statusY+94,120,{operationId=op.id}) end
 end end
 if not canEdit then text('Challenge archive: inspection only.',x+24,y+ph-42,colors.amber,self.normal) end
 text(campaign.features.travel==1 and 'Launch checks the applied revision, assembled crew, exact cargo, and one metal aboard. Cargo aboard is not a settlement stockpile.' or 'Departure becomes available in the next implementation tranche. Cargo aboard is not a settlement stockpile.',x+24,y+ph-24,colors.muted,self.small)
end
function R:drawSchool(app)
 local d=app.school;if not d then return end
 local campaign=app.history.view;local site=Campaign.site(campaign,d.siteId)
 local structure=site and site.world.structures[d.slot]
 if not structure or structure.kind~='field_school' or not structure.education or structure.education.id~=d.schoolId then
  return
 end
 local data=structure.education;local sw,sh=love.graphics.getDimensions();local pw,ph=math.min(850,sw-44),math.min(640,sh-44);local x,y=(sw-pw)/2,(sh-ph)/2
 box(0,0,sw,sh,colors.bg,0.94);box(x,y,pw,ph,colors.panel);heading('FIELD SCHOOL',x+24,y+20,colors.amber,self.title)
 d.buttons={}
 local function button(action,label,bx,by,bw,extra)
  local record={action=action,x=bx,y=by,w=bw,h=26};if extra then for key,value in pairs(extra) do record[key]=value end end
  d.buttons[#d.buttons+1]=record;box(bx,by,bw,26,colors.edge);text(label,bx+8,by+6,colors.cyan,self.small)
 end
 button('close','ESC close',x+pw-112,y+18,88)
 local draft=d.draft or {enabled=data.enabled,mode=data.mode,topicId=data.topicId,topicVersion=data.topicVersion,priority=data.priority,expectedPolicyRevision=data.policyRevision}
 d.draft=draft
 local topics=Education.sourceTopics(site.world,structure)
 local selected
 for _,topic in ipairs(topics) do if topic.id==draft.topicId and topic.version==draft.topicVersion then selected=topic end end
 if not selected and #topics>0 and not draft.topicId then selected=topics[1];draft.topicId,draft.topicVersion=selected.id,selected.version end
 local spec=selected and Knowledge.spec(selected.id) or draft.topicId and Knowledge.spec(draft.topicId)
 text('This installed surface is local: its records do not travel with an expert.',x+24,y+66,colors.muted,self.normal)
 text((draft.enabled and 'Policy enabled' or 'Policy disabled')..' / '..draft.mode..' / ordinary priority '..draft.priority,x+24,y+101,draft.enabled and colors.cyan or colors.amber,self.normal)
 button('enabled',draft.enabled and 'Disable' or 'Enable',x+24,y+128,96)
 button('mode','Mode: '..draft.mode,x+128,y+128,138)
 button('topic',spec and spec.label or (#topics>0 and 'Choose local source' or 'No local source'),x+274,y+128,252)
 button('priority','Priority '..draft.priority,x+534,y+128,104)
 local infoY=y+174
 local modeText=draft.mode=='record' and 'Record: one qualified local worker copies a known fact. A new topic or mode abandons unfinished copy work.' or draft.mode=='teach' and 'Teach: one qualified teacher and one distinct learner must both attend. Progress is checked every ten campaign ticks.' or 'Study record: one learner works from a completed record physically installed in this school.'
 wrap(modeText,x+24,infoY,pw-48,colors.text,self.normal);infoY=infoY+52
 if data.draft then text('Unreadable recording draft: '..data.draft.progress..'/120',x+24,infoY,colors.amber,self.normal);infoY=infoY+27 end
 if data.session then text('Active '..data.session.mode..' session '..data.session.id..' / evaluation '..data.session.lastEvaluationTick,x+24,infoY,colors.cyan,self.normal);infoY=infoY+27 end
 text('Completed records '..#data.records..' / '..Education.maxRecords,x+24,infoY,colors.text,self.normal);infoY=infoY+25
 for _,record in ipairs(data.records) do
  local rs=Knowledge.spec(record.topicId);text((rs and rs.label or record.topicId)..' / record '..record.id..' / tick '..record.tick,x+36,infoY,colors.muted,self.small);infoY=infoY+20
 end
 if #data.records==0 then text('No completed records at this site.',x+36,infoY,colors.muted,self.small);infoY=infoY+20 end
 local experts={}
 for _,worker in ipairs(site.world.workers) do
  if worker.alive and spec and Knowledge.fact(worker,spec.id) then experts[#experts+1]=worker.name..' / field '..worker.frontier.education.fieldworkXP..' / teach '..worker.frontier.education.teachingXP end
 end
 wrap(#experts>0 and ('Living local source: '..table.concat(experts,', ')) or 'No living local expert for the selected topic. Historical findings and remote experts are not sources.',x+24,infoY+12,pw-48,#experts>0 and colors.green or colors.muted,self.small)
 local canEdit=app.history:atPresent() or campaign.mode=='practice'
 if canEdit then button('apply','Apply policy',x+24,y+ph-58,126) else text('Challenge archive: inspection only.',x+24,y+ph-48,colors.amber,self.normal) end
 text('F4 distinguishes personal notes from school records. Study, teaching and recording need actual worker actions.',x+172,y+ph-50,colors.muted,self.small)
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
 local hint='Shift+drag pans. Left-drag selects blocks; right-click a selection to delegate actions.'
 if app.playtest then hint='ISOLATED PLAYTEST — saves: '..app.playtest.saveDir end
 text(hint,18,51,app.playtest and colors.amber or colors.muted,self.small)
 if app.regionButton then
  box(app.regionButton.x,app.regionButton.y,app.regionButton.w,app.regionButton.h,app.region and colors.cyan or colors.edge)
  text('Region',app.regionButton.x+12,app.regionButton.y+6,colors.bg,self.small)
  for _,b in ipairs(app.siteButtons) do
   box(b.x,b.y,b.w,b.h,b.siteId==app.siteId and colors.cyan or colors.edge)
   text(b.label,b.x+10,b.y+6,colors.bg,self.small)
  end
 end
 self:drawMap(app);self:sidebar(app);self:drawActionHud(app)
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
 self:drawSchool(app)
 Crew.draw(app,self)
 Notes.draw(app,self)
 self:help(app)
end
return R
