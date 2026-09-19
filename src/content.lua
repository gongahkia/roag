-- Versioned, bounded encounter templates. No scripts or arbitrary functions.
local U=require('src.util')
local W=require('src.world')
local Cat=require('src.catalog')
local Content={version=1}
local function obj(t,allowed,label)
 assert(type(t)=='table',label..' must be an object')
 for k in pairs(t) do assert(type(k)=='string' and allowed[k],label..': unknown key '..tostring(k)) end
end
local function list(t,limit,label)
 assert(type(t)=='table',label..' must be a list');local n=0
 for k in pairs(t) do U.integer(k,label..' index',1,limit);n=n+1 end
 assert(n==#t and n<=limit,label..' list limit or hole')
end
local function position(p,w,h)
 U.integer(p.x,'feature x',3,w-2);U.integer(p.y,'feature y',4,h-3)
end
function Content.validateTemplate(t,width,height)
 obj(t,{version=true,crew=true,flora=true,fauna=true,sites=true,ruins=true},'Features')
 assert(t.version==1,'Unknown feature schema');assert(t.crew==3 or t.crew==6 or t.crew==9,'Crew must be 3, 6 or 9')
 list(t.flora,128,'flora');list(t.fauna,64,'fauna');list(t.sites,48,'sites');list(t.ruins,32,'ruins')
 for _,p in ipairs(t.flora) do
  obj(p,{kind=true,x=true,y=true,food=true,water=true,phase=true},'Flora');assert(Cat.flora[p.kind],'Unknown growth')
  position(p,width,height);U.integer(p.food,'growth biomass',0,12);U.integer(p.water,'root water',0,6);U.integer(p.phase,'growth phase',0,3599)
 end
 for _,p in ipairs(t.fauna) do
  obj(p,{kind=true,x=true,y=true,food=true,phase=true},'Fauna');assert(Cat.fauna[p.kind],'Unknown creature')
  position(p,width,height);local d=Cat.fauna[p.kind];assert(p.x+d.width-1<width-1,'Creature footprint outside bounds')
  U.integer(p.food,'creature energy',0,24);U.integer(p.phase,'creature phase',0,3599)
 end
 for _,p in ipairs(t.sites) do
  obj(p,{kind=true,x=true,y=true,stock=true,phase=true},'Site');assert(Cat.sites[p.kind],'Unknown site')
  position(p,width,height);U.integer(p.phase,'site phase',0,3599)
  obj(p.stock,{stone=true,soil=true,metal=true,food=true,water=true},'Site stock')
  for _,key in ipairs({'stone','soil','metal','food','water'}) do U.integer(p.stock[key] or 0,'site stock '..key,0,1000) end
 end
 for _,p in ipairs(t.ruins) do
  obj(p,{kind=true,x1=true,y1=true,x2=true,y2=true,name=true},'Ruin');assert(Cat.ruins[p.kind],'Unknown ruin')
  U.integer(p.x1,'ruin x1',3,width-3);U.integer(p.x2,'ruin x2',p.x1+1,width-2)
  U.integer(p.y1,'ruin y1',3,height-3);U.integer(p.y2,'ruin y2',p.y1+1,height-2)
  assert(type(p.name)=='string' and #p.name>0 and #p.name<=80 and not p.name:find('[^\032-\126]'),'Invalid ruin name')
 end
 return true
end
function Content.install(w,t)
 Content.validateTemplate(t,w.width,w.height)
 w.content={version=1,flora={},fauna={},sites={},ruins=U.deep(t.ruins),signals={},discoveries={},observed={},ticks=0}
 local e=w.content
 for _,p in ipairs(t.flora) do local v=U.deep(p);v.id=W.id(w);v.alive=true;e.flora[#e.flora+1]=v end
 for _,p in ipairs(t.fauna) do
  local v=U.deep(p);v.id=W.id(w);v.alive=true;v.hp=Cat.fauna[v.kind].hp;v.homeX=v.x;v.homeY=v.y
  v.awake=v.kind~='sentinel';v.status=v.awake and 'Wandering' or 'Dormant';e.fauna[#e.fauna+1]=v
 end
 for _,p in ipairs(t.sites) do local v=U.deep(p);v.id=W.id(w);v.alive=true;v.active=false;e.sites[#e.sites+1]=v end
 w.labor=require('src.labor').default(w)
end
function Content.template(w)
 if not w.content then return nil end
 local e=w.content;local t={version=1,crew=#w.workers,flora={},fauna={},sites={},ruins=U.deep(e.ruins)}
 for _,p in ipairs(e.flora) do if p.alive then t.flora[#t.flora+1]={kind=p.kind,x=p.x,y=p.y,food=p.food,water=p.water,phase=p.phase} end end
 for _,p in ipairs(e.fauna) do if p.alive then t.fauna[#t.fauna+1]={kind=p.kind,x=p.x,y=p.y,food=p.food,phase=p.phase} end end
 for _,p in ipairs(e.sites) do if p.alive then t.sites[#t.sites+1]={kind=p.kind,x=p.x,y=p.y,stock=U.deep(p.stock),phase=p.phase} end end
 Content.validateTemplate(t,w.width,w.height);return t
end
function Content.find(w,id)
 if not w.content then return end
 for _,kind in ipairs({'flora','fauna','sites'}) do for _,p in ipairs(w.content[kind]) do if p.id==id and p.alive then return p,kind end end end
end
function Content.at(w,x,y)
 if not w.content then return end
 local best,kind,dist
 for _,k in ipairs({'sites','flora','fauna'}) do for _,p in ipairs(w.content[k]) do if p.alive then
  local d=math.abs(p.x-x)+math.abs(p.y-y)
  if d<=3 and (not dist or d<dist) then best,kind,dist=p,k,d end
 end end end
 return best,kind
end
function Content.discover(w,p,category)
 local key=category..':'..p.kind
 if w.content.discoveries[key] then return end
 local defs=category=='sites' and Cat.sites or category=='flora' and Cat.flora or Cat.fauna
 w.content.discoveries[key]={tick=w.tick,category=category,kind=p.kind}
 W.event(w,'discovery','Survey recorded: '..defs[p.kind].name..'. See F4 field notes.',p.id)
end
function Content.validate(w)
 if not w.content then return true end
 local e=w.content
 assert(e.version==1,'Unsupported living-world rules')
 list(e.flora,128,'live flora');list(e.fauna,64,'live fauna');list(e.sites,48,'live sites');list(e.ruins,32,'live ruins');list(e.signals,48,'signals')
 assert(type(e.discoveries)=='table' and type(e.observed)=='table','Missing field records')
 local ids={}
 for _,category in ipairs({'flora','fauna','sites'}) do for _,p in ipairs(e[category]) do
  U.integer(p.id,'feature ID',1,w.nextId-1);assert(not ids[p.id],'Duplicate feature ID');ids[p.id]=true
  assert(type(p.alive)=='boolean','Invalid feature state');position(p,w.width,w.height)
  U.integer(p.phase,'feature phase',0,3599)
  if category=='fauna' then
   assert(Cat.fauna[p.kind],'Bad species');U.integer(p.food,'energy',0,24)
   assert(U.finite(p.hp) and p.hp>=0 and p.hp<=Cat.fauna[p.kind].hp,'Bad creature health')
  elseif category=='flora' then assert(Cat.flora[p.kind],'Bad growth');U.integer(p.food,'biomass',0,12);U.integer(p.water,'root water',0,6)
  else
   assert(Cat.sites[p.kind] and type(p.stock)=='table' and type(p.active)=='boolean','Bad site')
   obj(p.stock,{stone=true,soil=true,metal=true,food=true,water=true},'Site stock')
   for _,key in ipairs({'stone','soil','metal','food','water'}) do U.integer(p.stock[key] or 0,'site stock '..key,0,1000) end
  end
 end end
 for _,signal in ipairs(e.signals) do
  assert(type(signal.kind)=='string' and #signal.kind<=32,'Bad signal')
  U.integer(signal.x,'signal x',1,w.width);U.integer(signal.y,'signal y',1,w.height)
  U.integer(signal.tick,'signal tick',0,w.tick);U.integer(signal.strength,'signal strength',1,100)
 end
 for _,key in ipairs({'observed','discoveries'}) do
  for name,p in pairs(e[key]) do
   assert(type(p)=='table' and type(p.tick)=='number' and p.tick>=0 and p.tick<=w.tick,'Bad field record')
   local registry=p.category=='sites' and Cat.sites or p.category=='flora' and Cat.flora or p.category=='fauna' and Cat.fauna
   assert(registry and registry[p.kind] and name==p.category..':'..p.kind,'Unknown field record')
  end
 end
 return true
end
return Content
