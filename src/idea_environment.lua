-- Read-only room presentation, fixed derived surfaces and native image handles.
-- Original maps, source pixels, run/profile data and gameplay RNG are untouched.
local D=require('src.idea_environment_content')
local Neighbors=require('src.kenney_terrain')
local RNG=require('src.rng')
local Mono=require('src.visual_monochrome')
local E={content=D}
local dirs={{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},{1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}}
local quadrants={{1,8,128,0,0,1,2,4,1},{1,2,16,8,0,3,2,6,2},
 {4,2,32,8,8,9,8,6,3},{4,8,64,0,8,7,8,4,4}}
local function has(mask,bit)return math.floor(mask/bit)%2==1 end
local function keys(t)local a={};for k in pairs(t)do a[#a+1]=k end;table.sort(a);return a end
local function inside(r,x,y)return x>=r.x and x<r.x+r.width and y>=r.y and y<r.y+r.height end
local function hash(map,text)return RNG.derive(map.seed or 1,(map.floor_id or '')..':'..tostring(text))end
function E.material(name,style)return D.materials[name][style=='onebit' and 'mono' or 'mixed']end
function E.surfaces(sheet,style)
 local out={}
 for _,name in ipairs(D.material_order)do
  local m=E.material(name,style);local def=D.materials[name]
  if m.sheet==sheet then
   for _,mask in ipairs((m.grid or m.frame or m.border) and Neighbors.masks or {255})do
    out[#out+1]={name=name,kind=def.kind,mask=mask,material=m,slot=#out}
   end
  end
 end
 return out
end
function E.surface_sheet(source,sheet,style)
 local info=assert(D.sources[sheet]);local out=source:clone();local columns=math.floor(info.width/16)
 local function sample(index,x,y)
  return source:getPixel(index%info.columns*info.stride+x,math.floor(index/info.columns)*info.stride+y)
 end
 for _,entry in ipairs(E.surfaces(sheet,style))do
  local m,mask=entry.material,entry.mask;local ox,oy=entry.slot%columns*16,math.floor(entry.slot/columns)*16
  assert(oy+16<=info.height,'environment surface page overflow')
  for y=0,15 do for x=0,15 do
   local index=m.center or m.grid[5];local sx,sy=x,y
   if m.grid then
    for _,q in ipairs(quadrants)do if x>=q[4] and x<q[4]+8 and y>=q[5] and y<q[5]+8 then
     local a,b,diagonal=has(mask,q[1]),has(mask,q[2]),has(mask,q[3]);local part
     if not a and not b then part=q[6]elseif not a then part=q[7]elseif not b then part=q[8]
     elseif not diagonal and m.inner then index=m.inner[q[9]]
     else part=5 end
     if part then index=m.grid[part]end
    end end
   elseif m.frame then
    if not has(mask,1) and y<4 then index=m.frame;sx=8 end
    if not has(mask,4) and y>=12 then index=m.frame;sx=8 end
    if not has(mask,8) and x<4 then index=m.frame;sy=8 end
    if not has(mask,2) and x>=12 then index=m.frame;sy=8 end
   end
   local r,g,b,a=sample(index,sx,sy)
   -- Wall concave corners are explicit derived caps; no missing diagonal
   -- becomes a portal, detached door frame or unrelated floor ornament.
   if entry.kind=='wall'then
    for _,q in ipairs(quadrants)do
     local dx=q[4]==0 and x or 15-x;local dy=q[5]==0 and y or 15-y
     if has(mask,q[1]) and has(mask,q[2]) and not has(mask,q[3]) and dx<3 and dy<3 then
      local f=m.frame or m.grid[q[6]];r,g,b,a=sample(f,q[4]==0 and dx or 15-dx,q[5]==0 and dy or 15-dy)
     end
    end
   end
   if m.accent and x>=4 and x<12 and y>=4 and y<12 then
    local rr,gg,bb,aa=sample(m.accent,x,y)
    local strength=m.sheet=='onebit' and .35 or .4
    r,g,b=r*(1-aa*strength)+rr*aa*strength,g*(1-aa*strength)+gg*aa*strength,b*(1-aa*strength)+bb*aa*strength
   end
   if style=='onebit'then
    if m.water_conversion then
     local water=b>r+.06 and b>g*.9;local v=water and ((r*.2126+g*.7152+b*.0722)>.76 and .8 or .055) or .75
     r,g,b=v,v,v
    else
     local v=(r*.2126+g*.7152+b*.0722)*a
     local light=entry.kind=='floor' and .22 or .48
     r,g,b=.045+v*light,.045+v*light,.045+v*light
    end
   elseif entry.kind=='floor' then r,g,b=r*.72,g*.72,b*.72 end
   if m.border and ((not has(mask,1) and y<1) or (not has(mask,4) and y>=15)
    or (not has(mask,8) and x<1) or (not has(mask,2) and x>=15))then r,g,b=.55,.55,.55 end
   out:setPixel(ox+x,oy+y,r,g,b,1)
  end end
 end
 return out
end
local biome_families={overgrown_ruins='ruin_masonry',fungal_hollows='broken_cavern',sunken_aqueduct='drowned_sanctum',
 ashen_bastion='forge_masonry',glacial_observatory='observatory',fractured_archive='archive_library',sundered_forge='forge_masonry'}
function E.family(map,room)
 local slug=(map.biome_id or ''):match('/([^/]+)$') or 'overgrown_ruins';local seed=hash(map,room.id)
 if room.role~='start' and room.role~='elite' and room.role~='exit'then
  if (slug=='fungal_hollows' or slug=='glacial_observatory' or slug=='fractured_archive') and seed%4==0 then return 'crypt_sanctuary'end
  if slug=='overgrown_ruins'then return seed%3==0 and 'root_garden' or seed%2==0 and 'ruin_carved' or 'ruin_masonry'end
 end
 return biome_families[slug] or 'ruin_masonry'
end
function E.plan(state,map)
 local plan={rooms={},props={}};local reserved={}
 for _,p in ipairs(map.placements and map.placements.secrets or {})do
  for _,c in ipairs({{p.x,p.y},{p.wall_x,p.wall_y}})do if c[1] and c[2]then reserved[c[1]..','..c[2]]=true end end
 end
 for _,room in ipairs(map.rooms or {})do
  local family=E.family(map,room);local decor
  for _,node in ipairs(state and state.idea_rooms and state.idea_rooms.nodes or {})do
   if node.floor_id==map.floor_id and inside(room,node.x,node.y)then
    if node.kind=='memorial' or node.kind=='offering_font'then family='crypt_sanctuary'
    elseif node.kind=='attunement_forge'then family='forge_masonry';decor={'workshop_supplies','workshop_hearth'}
    elseif node.kind=='camp'then decor={'camp_table','camp_seat'}
    elseif node.kind=='travelling_trader'then decor={'workshop_supplies','camp_seat'}end
   end
  end
  local def=D.families[family];local entry={room=room,family=family,floor=def.floor,wall=def.wall,water=def.water}
  if def.patch and room.width>=7 and room.height>=7 then
   entry.patch={x=room.x+math.floor(room.width/2)-1,y=room.y+math.floor(room.height/2)-1,width=3,height=3,material=def.patch}
  end
  plan.rooms[#plan.rooms+1]=entry
  decor=decor or def.decor
  if family=='forge_masonry' and not decor then decor={'forge_panel','prison_bars'}end
  local choices={}
  for y=room.y,room.y+room.height-1 do for x=room.x,room.x+room.width-1 do
   if map.rows[y]:sub(x,x)=='#' and not reserved[x..','..y] and not (map.studio_visuals and map.studio_visuals[x..','..y])then
    local adjacent=false;for _,d in ipairs(dirs)do if d[3]<=8 and inside(room,x+d[1],y+d[2])
     and map.rows[y+d[2]]:sub(x+d[1],x+d[1])=='.'then adjacent=true end end
    if adjacent then choices[#choices+1]={x=x,y=y,score=hash(map,'decor:'..x..','..y)}end
   end
  end end
  table.sort(choices,function(a,b)if a.score~=b.score then return a.score<b.score end;if a.y~=b.y then return a.y<b.y end;return a.x<b.x end)
  for index=1,math.min(2,#choices)do
   local p=choices[index];local name=decor[(hash(map,room.id..':'..index)%#decor)+1]
   plan.props[#plan.props+1]={x=p.x,y=p.y,id=D.prop_id(name),family=family,room_id=room.id}
  end
 end
 return plan
end
local function entry_at(plan,x,y)
 for _,e in ipairs(plan.rooms)do if inside(e.room,x,y)then return e end end
end
local function material_at(plan,map,x,y,kind)
 local e=entry_at(plan,x,y);if not e then return end
 if kind=='floor' and e.patch and inside(e.patch,x,y)then return e.patch.material end
 return e[kind]
end
function E.new(style,read,make_texture,make_quad,existing)
 local self=setmetatable({style=style,handles={},materials={},pages={},failures={},plan={rooms={},props={}}},{__index=E})
 local textures={};local raw={};local signature={D.version,style}
 local function bytes(sheet)
  if raw[sheet]==false then return end
  if raw[sheet]then return raw[sheet]end
  local meta=D.sources[sheet];local okay,data=pcall(read,meta.path)
  if not okay or not data or require('src.studio_atlas').checksum(data)~=meta.checksum then
   self.failures[sheet]='missing/corrupt '..sheet;raw[sheet]=false;return
  end
  raw[sheet]=data;return data
 end
 local function texture(sheet,surfaces)
  local id=sheet..':'..tostring(surfaces);if textures[id]~=nil then return textures[id]or nil end
  local meta=D.sources[sheet];local data=bytes(sheet);if not data then textures[id]=false;return end
  local okay,image=pcall(function()
   local image=not surfaces and existing and existing[sheet]
   if not image then image=make_texture(data,meta.path,surfaces and {environment=true,sheet=sheet,style=style}
    or style=='onebit' and sheet~='onebit' and 'approved-monochrome' or false)end
   assert(image and image:getWidth()==meta.width and image:getHeight()==meta.height,'wrong environment image dimensions')
   return image
  end)
  if not okay then self.failures[id]=tostring(image);textures[id]=false;return end
  textures[id]=image;self.pages[id]={texture=image};return image
 end
 local function native(sheet,index)
  local image=texture(sheet,false);if not image then return end
  local meta=D.sources[sheet];local x,y=index%meta.columns*meta.stride,math.floor(index/meta.columns)*meta.stride
  assert(x+16<=meta.width and y+16<=meta.height,'bad environment source cell')
  return {texture=image,quad=make_quad(x,y,16,16,meta.width,meta.height),width=16,height=16,
   offset_x=x,offset_y=y,source=style,approved_sheet=sheet,tile_index=index,monochrome=style=='onebit'}
 end
 for _,sheet in ipairs({'rpg','caves','onebit'})do
  local entries=E.surfaces(sheet,style)
  if #entries>0 then
   local image=texture(sheet,true);local meta=D.sources[sheet];local columns=math.floor(meta.width/16)
   if image then for _,entry in ipairs(entries)do
    local x,y=entry.slot%columns*16,math.floor(entry.slot/columns)*16;local m=entry.material
    local h={texture=image,quad=make_quad(x,y,16,16,meta.width,meta.height),width=16,height=16,
     offset_x=x,offset_y=y,source=style,approved_sheet=sheet,tile_index=m.center or m.grid[5],
     derived=true,material=entry.name,mask=entry.mask,monochrome=style=='onebit'}
    self.materials[entry.name]=self.materials[entry.name]or {};self.materials[entry.name][entry.mask]=h
    if entry.mask==255 then self.handles[D.material_id(entry.name)]=h end
   end end
  end
 end
 for _,name in ipairs(keys(D.props))do local p=D.props[name];local sheet,index=p[1],p[2]
  if style=='onebit' and p[3]then sheet,index='onebit',p[3]end
  self.handles[D.prop_id(name)]=native(sheet,index) or native('onebit',name:match('column') and 542 or 3)
 end
 for _,name in ipairs(keys(D.symbols))do self.handles[D.symbol_id(name)]=native('onebit',D.symbols[name])end
 for _,sheet in ipairs(keys(raw))do signature[#signature+1]=sheet..':'..tostring(raw[sheet]~=false)end
 for _,id in ipairs(keys(self.failures))do signature[#signature+1]=id end
 self.signature=table.concat(signature,':');return self
end
function E:set_world(state)
 local map=state.generated_map or state.final_map
 if not map then self.plan={rooms={},props={}};return end
 local key=tostring(map)..':'..state.active_floor_id..':'..state.next_event_id
 if key~=self.plan_key then self.plan=E.plan(state,map);self.plan_key=key end
end
function E:resolve(map,x,y,kind,known)
 local plan=self.plan
 if #plan.rooms==0 then plan=E.plan(nil,map)end
 local name=material_at(plan,map,x,y,kind);if not name or not self.materials[name]then return end
 local list=self.materials[name];if list[255] and not list[0]then return list[255]end
 local mark=kind=='wall' and '#' or kind=='water' and '~' or '.';local mask=0
 for _,d in ipairs(dirs)do local a,b=x+d[1],y+d[2];local row=map.rows[b]
  if row and a>=1 and a<=#row and ((known and not known(a,b)) or
   row:sub(a,a)==mark and material_at(plan,map,a,b,kind)==name)then mask=mask+d[3]end
 end
 return list[Neighbors.canonical(mask)] or list[255]
end
function E:prop(name)return self.handles[D.prop_id(name)]end
return E
