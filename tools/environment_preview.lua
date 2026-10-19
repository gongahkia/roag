-- Paired with environment_preview.py; fixed repository assets only. This does
-- not start LÖVE. FFI is built into the required LuaJIT, not a native extension.
local ffi=require('ffi');local json=require('src.sprite_audit').json
local E=require('src.idea_environment');local G=require('src.game');local P=require('src.persistence')
local root=assert(arg[1]);assert(root:match('^/var/tmp/roag%-environment%-pixels%-[%w_%-]+$'),'owned preview directory required')
local function read(path)local f=assert(io.open(path,'rb'));local b=f:read('*a');f:close();return b end
local function write(path,b)local f=assert(io.open(path,'wb'));assert(f:write(b));f:close()end
local Image={};Image.__index=Image
function Image.new(w,h,b)
 local t=setmetatable({w=w,h=h,bytes=ffi.new('uint8_t[?]',w*h*4)},Image)
 if b then assert(#b==w*h*4);ffi.copy(t.bytes,b,#b)end;return t
end
function Image:getWidth()return self.w end
function Image:getHeight()return self.h end
function Image:clone()return Image.new(self.w,self.h,ffi.string(self.bytes,self.w*self.h*4))end
function Image:getPixel(x,y)
 assert(x>=0 and y>=0 and x<self.w and y<self.h and x%1==0 and y%1==0,'pixel bounds')
 local i=(y*self.w+x)*4;return self.bytes[i]/255,self.bytes[i+1]/255,self.bytes[i+2]/255,self.bytes[i+3]/255
end
function Image:setPixel(x,y,r,g,b,a)
 assert(x>=0 and y>=0 and x<self.w and y<self.h,'pixel write bounds')
 local i=(y*self.w+x)*4
 for n,v in ipairs({r,g,b,a})do self.bytes[i+n-1]=math.floor(math.max(0,math.min(1,v))*255+.5)end
end
function Image:mapPixel(fn)for y=0,self.h-1 do for x=0,self.w-1 do self:setPixel(x,y,fn(x,y,self:getPixel(x,y)))end end end
local originals={}
for i,v in ipairs({{'assets/kenney/caves-dungeons/terrain.png',492,305},
 {'assets/kenney/1-bit-pack/symbols.png',784,352},{'assets/kenney/approved/tiny-dungeon.png',203,186},
 {'assets/kenney/approved/roguelike-rpg.png',968,526}})do originals[v[1]]=Image.new(v[2],v[3],read(root..'/'..i..'.rgba'))end
local function texture(_,path,mode)
 local source=assert(originals[path]):clone()
 if type(mode)=='table'then return E.surface_sheet(source,mode.sheet,mode.style)
 elseif mode=='onebit-surfaces'then return require('src.onebit_visuals').surface_sheet(source)

 elseif mode=='approved-monochrome'then source:mapPixel(function(x,y,r,g,b,a)return require('src.visual_monochrome').pixel(r,g,b,a)end)
 elseif mode then source:mapPixel(function(x,y,r,g,b,a)if math.floor(r*255+.5)==34 and math.floor(g*255+.5)==35 and math.floor(b*255+.5)==35 then return 0,0,0,0 end;return r,g,b,a end)end
 return source
end
local function quad(x,y,w,h)return {x=x,y=y,w=w,h=h}end
local canvas,color,clip;local graphics={}
function graphics.setColor(r,g,b,a)color=type(r)=='table' and r or {r,g,b,a or 1}end
function graphics.setScissor(x,y,w,h)clip=x and {x,y,w,h}or nil end
local function blend(x,y,r,g,b,a)
 if x<0 or y<0 or x>=canvas.w or y>=canvas.h or a<=0 then return end
 if clip and (x<clip[1]or y<clip[2]or x>=clip[1]+clip[3]or y>=clip[2]+clip[4])then return end
 local rr,gg,bb=canvas:getPixel(x,y);canvas:setPixel(x,y,r*a+rr*(1-a),g*a+gg*(1-a),b*a+bb*(1-a),1)
end
local function dot(x,y)blend(math.floor(x),math.floor(y),color[1],color[2],color[3],color[4]or 1)end
function graphics.line(...)
 local a={...};if type(a[1])=='table'then a=a[1]end
 for i=1,#a-2,2 do local x,y,dx,dy=a[i],a[i+1],a[i+2]-a[i],a[i+3]-a[i+1];local n=math.max(1,math.ceil(math.max(math.abs(dx),math.abs(dy))))
  for j=0,n do dot(x+dx*j/n,y+dy*j/n)end
 end
end
function graphics.rectangle(mode,x,y,w,h)
 if mode=='line'then graphics.line(x,y,x+w,y,x+w,y+h,x,y+h,x,y);return end
 for yy=math.max(0,math.floor(y)),math.min(canvas.h-1,math.ceil(y+h)-1)do
  for xx=math.max(0,math.floor(x)),math.min(canvas.w-1,math.ceil(x+w)-1)do dot(xx,yy)end
 end
end
function graphics.ellipse(mode,x,y,rx,ry)
 for yy=math.floor(y-ry),math.ceil(y+ry)do for xx=math.floor(x-rx),math.ceil(x+rx)do
  local d=((xx-x)/rx)^2+((yy-y)/ry)^2
  if d<=1 and (mode=='fill'or d>.7)then dot(xx,yy)end
 end end
end
function graphics.circle(mode,x,y,r)graphics.ellipse(mode,x,y,r,r)end
function graphics.polygon(mode,...)graphics.line(...)end
function graphics.draw(image,q,x,y,angle,sx,sy)
 assert(type(q)=='table' and q.w and (angle or 0)==0,'CPU preview supports native upright Quads')
 sx,sy=sx or 1,sy or sx or 1
 local left,right=math.min(x,x+q.w*sx),math.max(x,x+q.w*sx)
 local top,bottom=math.min(y,y+q.h*sy),math.max(y,y+q.h*sy)
 for yy=math.max(0,math.ceil(top)),math.min(canvas.h-1,math.ceil(bottom)-1)do
  for xx=math.max(0,math.ceil(left)),math.min(canvas.w-1,math.ceil(right)-1)do
   local px=math.max(0,math.min(q.w-1,math.floor((xx+.5-x)/sx)))
   local py=math.max(0,math.min(q.h-1,math.floor((yy+.5-y)/sy)))
   local r,g,b,a=image:getPixel(q.x+px,q.y+py)
   blend(xx,yy,r*color[1],g*color[2],b*color[3],a*(color[4]or 1))
  end
 end
end
local font={getWidth=function(_,t)return #t*7 end,getHeight=function()return 14 end}
function graphics.getFont()return font end
function graphics.getDimensions()return 1024,640 end
function graphics.getWidth()return 1024 end
for _,n in ipairs({'print','printf','setFont','setLineWidth','push','pop','translate','scale','setLineStyle'})do graphics[n]=function()end end
local fake={graphics=graphics};local env=setmetatable({love=fake},{__index=_G})
local chunk=assert(loadfile('main.lua'));setfenv(chunk,env);chunk()
local function visit(callback)
 local seen={};local function walk(fn)
  if seen[fn]then return end;seen[fn]=true;local children={}
  for i=1,200 do local name,v=debug.getupvalue(fn,i);if not name then break end
   callback(fn,i,name,v);if type(v)=='function'then children[#children+1]=v end
  end
  for _,child in ipairs(children)do walk(child)end
 end
 walk(fake.draw);walk(fake.keypressed)
end
local world
visit(function(_,_,n,v)if n=='draw_world'then world=v end end);assert(world)
local entries={};local function save(name,label,detail)
 write(root..'/'..name..'.rgba',ffi.string(canvas.bytes,canvas.w*canvas.h*4))
 entries[#entries+1]={name=name,label=label,detail=detail,width=canvas.w,height=canvas.h}
end
local cases={};local seen={}
for _,id in ipairs(require('src.biomes').order)do
 local first=id==require('src.biomes').order[1]
 local s=G.new_stage(1,{multifloor=true,stage_options={biome_id=id,index=first and 1 or 2,route_history=first and {}or{id:gsub('^core:biome/','core:route/')}}})
 local plan=E.plan(s,s.generated_map)
 local preferred=plan.rooms[1]
 for _,r in ipairs(plan.rooms)do if r.patch then preferred=r;break end end
 cases[#cases+1]={state=s,room=preferred,label=require('src.biomes').get(id).name}
 seen[preferred.family]=true
end
for seed=1,5 do
 local s=G.new_stage(seed,{multifloor=true});for _,r in ipairs(E.plan(s,s.generated_map).rooms)do
  if r.family=='crypt_sanctuary' or r.family=='root_garden'then if not seen[r.family]then
   cases[#cases+1]={state=s,room=r,label=r.family};seen[r.family]=true
  end end
 end
 local s=G.new_stage(seed,{multifloor=true,stage_options={biome_id='core:biome/fractured_archive',index=2,route_history={'core:route/fractured_archive'}}})
 for _,r in ipairs(E.plan(s,s.generated_map).rooms)do if r.family=='crypt_sanctuary' and not seen[r.family]then
  cases[#cases+1]={state=s,room=r,label='Crypt / Pale Sanctuary'};seen[r.family]=true
 end end
end
for _,style in ipairs({'onebit'})do
 local native=assert(require('src.onebit_visuals').new(read,texture,quad))
 assert(not next(native.environment.failures),'environment resource failure')
 local assets={generated=native,has_terrain=true,resolve=function(_,id)return native:resolve(id)end}
 for i,case in ipairs(cases)do
  local s,r=case.state,case.room.room;local snap=P.encode(G.snapshot(s))
  canvas=Image.new(1024,640);color={1,1,1,1};clip=nil
  local camera=require('src.camera').new(s.generated_map,{x=16,y=16,width=992,height=520},40,r.x+r.width/2,r.y+r.height/2)
  local inject={state=s,visual_assets=assets,camera=camera,spectator_enabled=true,minimap_visible=false,
   ui_body_font=font,ui_display_font=font,ui_menu_font=font}
  visit(function(fn,n,key)if inject[key]~=nil then debug.setupvalue(fn,n,inject[key])end end)
  -- Diagnostic closed-upvalue injection needs new traces, unlike normal input.
  jit.flush();world()
  assert(P.encode(G.snapshot(s))==snap,'render changed authoritative state')
  save(style..'-'..i,case.label..' / '..style,'Generated seed '..s.generated_map.seed..'; '..case.room.family..'; actual world draw pass, spectator crop; text omitted.')
 end
 canvas=Image.new(800,128);color={1,1,1,1};clip=nil
 local names={};for name in pairs(E.content.symbols)do names[#names+1]=name end;table.sort(names)
 for i,name in ipairs(names)do local h=assert(native:resolve(E.content.symbol_id(name)))
  graphics.draw(h.texture,h.quad,((i-1)%16)*48+8,math.floor((i-1)/16)*56+8,0,2,2)
 end
 save(style..'-symbols','Action, status, state and combat cues / '..style,'Sorted semantic IDs; static 2× source pixels. Names and gameplay meanings are documented in the environment guide.')
end
write(root..'/index.json',json(entries));print('CPU preview complete; authoritative state/RNG unchanged in every view')
