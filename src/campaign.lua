-- Campaign state coordinates bounded local worlds without changing their
-- local identifiers or simulation ownership.
local U=require('src.util')
local W=require('src.world')
local Metrics=require('src.metrics')
local Sim=require('src.sim')
local C=require('config')
local Campaign={format='cosmonauts-campaign-state',version=1,ruleset='frontier-campaign-v1'}

local function exact(t,allowed,label)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do
  if type(key)~='string' or key:sub(1,1)~='_' then assert(allowed[key],'Unknown '..label..' key '..tostring(key)) end
 end
 for key in pairs(allowed) do assert(t[key]~=nil,'Missing '..label..' key '..key) end
end
local function allowed(t,keys,label)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do
  if type(key)~='string' or key:sub(1,1)~='_' then assert(keys[key],'Unknown '..label..' key '..tostring(key)) end
 end
end
local function dense(t,label,limit)
 assert(type(t)=='table',label..' must be an array')
 local count,max=0,0
 for key in pairs(t) do U.integer(key,label..' index',1,limit);count=count+1;if key>max then max=key end end
 assert(count==max and count<=limit,label..' has a hole or exceeds its limit')
 return max
end
local function sortedWorkers(w)
 local out={};for i=1,dense(w.workers,'workers',32) do out[i]=w.workers[i] end
 table.sort(out,function(a,b) return a.id<b.id end);return out
end
local function isRegion(c) return c.features and c.features.region==1 end
local function isTravel(c) return c.features and c.features.travel==1 end

function Campaign.sites(c)
 local out={};local limit=isRegion(c) and 3 or 1
 for i=1,dense(c.sites,'campaign sites',limit) do out[i]=c.sites[i] end
 table.sort(out,function(a,b) return a.id<b.id end);return out
end
local function orderedSites(c)
 local sites=c._orderedSites
 if not sites then sites=Campaign.sites(c);c._orderedSites=sites end
 return sites
end
function Campaign.site(c,siteId)
 U.integer(siteId,'campaign site ID',1,100000000)
 for _,site in ipairs(Campaign.sites(c)) do if site.id==siteId then return site end end
end
function Campaign.body(c,bodyId)
 if not isRegion(c) then return end
 U.integer(bodyId,'campaign body ID',1,3)
 for _,body in ipairs(Campaign.bodies(c)) do if body.id==bodyId then return body end end
end
function Campaign.bodies(c)
 if not isRegion(c) then return {} end
 local out={};for i=1,dense(c.region.bodies,'campaign bodies',3) do out[i]=c.region.bodies[i] end
 table.sort(out,function(a,b) return a.id<b.id end);return out
end
function Campaign.extinct(c)
 local living=0;for _,site in ipairs(Campaign.sites(c)) do living=living+W.alive(site.world) end
 if isTravel(c) then living=living+require('src.travel').living(c) end
 return living==0
end

local function validateRecipe(recipe,label)
 exact(recipe,{version=true,preset=true,layout=true,climate=true,openness=true,biomeScale=true,features=true,density=true,
  crew=true,width=true,height=true,populated=true},label)
 assert(recipe.version==1,'Unsupported '..label..' version')
 assert(type(recipe.preset)=='string' and type(recipe.layout)=='string' and type(recipe.climate)=='string' and type(recipe.features)=='string' and type(recipe.populated)=='boolean','Malformed '..label)
 assert(recipe.preset=='frontier' or recipe.preset=='cistern' or recipe.preset=='dunes' or recipe.preset=='frost','Unknown recipe preset')
 assert(recipe.features=='living' or recipe.features=='ruins' or recipe.features=='none','Unknown recipe features')
 assert(U.finite(recipe.openness) and recipe.openness>=.25 and recipe.openness<=.70,'Invalid recipe openness')
 assert(U.finite(recipe.biomeScale) and recipe.biomeScale>=.65 and recipe.biomeScale<=1.5,'Invalid recipe biome scale')
 assert(U.finite(recipe.density) and recipe.density>=.5 and recipe.density<=1.5,'Invalid recipe density')
 assert(recipe.crew==3 or recipe.crew==6 or recipe.crew==9,'Invalid recipe crew')
 U.integer(recipe.width,'recipe width',64,512);U.integer(recipe.height,'recipe height',64,256)
 assert(recipe.width%4==0 and recipe.height%4==0,'Recipe dimensions must align to the local grid')
end
local function validateRegion(c)
 exact(c.region,{version=true,bodies=true,notices=true},'campaign region')
 assert(c.region.version==1,'Unsupported campaign region version');assert(#c.sites==3,'Campaign region requires exactly three sites')
 assert(dense(c.region.bodies,'campaign bodies',3)==3,'Campaign region requires exactly three bodies')
 local sitesById={};for _,site in ipairs(Campaign.sites(c)) do sitesById[site.id]=site end
 local seenBodies={}
 for _,body in ipairs(c.region.bodies) do
  allowed(body,{id=true,kind=true,parentBodyId=true,name=true,siteId=true,terrainSeed=true,recipe=true},'campaign body')
  U.integer(body.id,'body ID',1,3);assert(not seenBodies[body.id],'Duplicate body ID');seenBodies[body.id]=true;U.integer(body.siteId,'body site ID',1,3);U.integer(body.terrainSeed,'body terrain seed',1,2147483646)
  assert(type(body.name)=='string' and #body.name>0 and #body.name<=64,'Invalid body display name');validateRecipe(body.recipe,'body recipe')
  assert(body.id==body.siteId and sitesById[body.siteId],'Missing body site')
  if body.id==1 then assert(body.kind=='planet' and body.parentBodyId==nil and body.name=='Home Planet','Invalid home body')
  else
   assert(body.kind=='moon' and body.parentBodyId==1 and body.name==('Moon '..(body.id==2 and 'I' or 'II')),'Invalid moon body')
   assert(body.recipe.preset=='frontier' and body.recipe.width==192 and body.recipe.height==112 and not body.recipe.populated,'Invalid moon recipe')
  end
 end
 local lastTick,lastSite,lastOrdinal=-1,-1,-1
 dense(c.region.notices,'campaign notices',128)
 for _,notice in ipairs(c.region.notices) do
  exact(notice,{tick=true,siteId=true,ordinal=true,kind=true,text=true,count=true},'campaign notice')
  U.integer(notice.tick,'notice tick',0,c.tick);U.integer(notice.siteId,'notice site ID',1,3);U.integer(notice.ordinal,'notice ordinal',0,100000000);U.integer(notice.count,'notice count',1,100000000)
  assert(type(notice.kind)=='string' and type(notice.text)=='string','Malformed campaign notice')
  assert(notice.tick>lastTick or (notice.tick==lastTick and (notice.siteId>lastSite or (notice.siteId==lastSite and notice.ordinal>lastOrdinal))),'Campaign notices are not stably ordered')
  lastTick,lastSite,lastOrdinal=notice.tick,notice.siteId,notice.ordinal
 end
end

function Campaign.validate(c)
 allowed(c,{format=true,version=true,ruleset=true,features=true,tick=true,mode=true,seed=true,society=true,nextPersonId=true,sites=true,region=true,logistics=true,travel=true},'campaign')
 for _,key in ipairs({'format','version','ruleset','features','tick','mode','seed','society','nextPersonId','sites'}) do assert(c[key]~=nil,'Missing campaign key '..key) end
 assert(c.format==Campaign.format,'Incompatible campaign state format');assert(c.version==Campaign.version,'Unsupported campaign state version');assert(c.ruleset==Campaign.ruleset,'Unsupported campaign ruleset')
 allowed(c.features,{core=true,region=true,logistics=true,travel=true},'campaign features');assert(c.features.core==1,'Unsupported campaign core feature version')
 if c.features.region~=nil then assert(c.features.region==1,'Unsupported campaign region feature version') end
 if c.features.logistics~=nil then assert(c.features.logistics==1,'Unsupported campaign logistics feature version') end
 if c.features.travel~=nil then assert(c.features.travel==1,'Unsupported campaign travel feature version') end
 assert((c.features.region==1)==(c.region~=nil),'Campaign region feature/state mismatch')
 assert((c.features.logistics==1)==(c.logistics~=nil),'Campaign logistics feature/state mismatch')
 assert((c.features.travel==1)==(c.travel~=nil),'Campaign travel feature/state mismatch')
 assert(c.features.logistics==nil or c.features.region==1,'Campaign logistics requires region')
 assert(c.features.travel==nil or (c.features.region==1 and c.features.logistics==1),'Campaign travel requires region logistics')
 U.integer(c.tick,'campaign tick',0,10000000);assert(c.mode=='challenge' or c.mode=='practice','Invalid campaign mode');U.integer(c.seed,'campaign seed',0,2147483646)
 exact(c.society,{id=true,origin=true,independent=true},'campaign society');U.integer(c.society.id,'society ID',1,100000000)
 assert(c.society.id==1 and c.society.origin=='abandoned_convicts' and c.society.independent==true,'Unsupported campaign society');U.integer(c.nextPersonId,'next person ID',1,100000000)
 if isRegion(c) then validateRegion(c) else assert(c.region==nil and #Campaign.sites(c)==1,'Campaign core requires exactly one site') end
 local bodiesBySite={};if isRegion(c) then for _,body in ipairs(c.region.bodies) do bodiesBySite[body.siteId]=body end end
 local seenSites,seenPeople,maxPerson={}, {},0
 for _,site in ipairs(Campaign.sites(c)) do
  allowed(site,isRegion(c) and {id=true,ownerSocietyId=true,bodyId=true,world=true} or {id=true,ownerSocietyId=true,world=true},'campaign site')
  assert(site.id~=nil and site.world~=nil,'Incomplete campaign site');U.integer(site.id,'site ID',1,100000000);assert(not seenSites[site.id],'Duplicate site ID');seenSites[site.id]=true
  if isRegion(c) then U.integer(site.bodyId,'site body ID',1,3);assert(site.id==site.bodyId and bodiesBySite[site.id],'Site/body mismatch');assert(site.ownerSocietyId==nil or site.ownerSocietyId==c.society.id,'Invalid site ownership');if site.id==1 then assert(site.ownerSocietyId==c.society.id,'Home site must be owned') end
  else assert(site.id==1 and site.ownerSocietyId==c.society.id,'Invalid core site ownership') end
  W.validate(site.world);local world=site.world
  assert(world.frontier and world.frontier.version==1 and world.frontier.siteId==site.id,'Campaign site marker mismatch');assert(world.tick==c.tick,'Campaign and site ticks differ');assert(world.mode==c.mode,'Campaign and site modes differ')
  if isRegion(c) then assert(world.seed==bodiesBySite[site.id].terrainSeed,'Campaign terrain seed differs') else assert(world.seed==c.seed,'Campaign and site identity differ') end
  dense(world.jobs,'jobs',1024);dense(world.items,'items',10000);dense(world.events,'events',240)
  local localIds,maxLocal={},0
  local function localId(record,label)
   assert(type(record)=='table','Malformed '..label);U.integer(record.id,label..' ID',1,world.nextId-1);assert(not localIds[record.id],'Duplicate local entity ID');localIds[record.id]=true;if record.id>maxLocal then maxLocal=record.id end
  end
  for _,worker in ipairs(sortedWorkers(world)) do localId(worker,'worker');U.integer(worker.personId,'campaign person ID',1,100000000);assert(not seenPeople[worker.personId],'Duplicate campaign person ID');seenPeople[worker.personId]=true;if worker.personId>maxPerson then maxPerson=worker.personId end end
  for _,job in ipairs(world.jobs) do localId(job,'job') end;for _,item in ipairs(world.items) do localId(item,'item') end;for _,slot in ipairs(U.keys(world.structures)) do localId(world.structures[slot],'structure') end
  if world.content then for _,kind in ipairs({'flora','fauna','sites'}) do for _,record in ipairs(world.content[kind]) do localId(record,kind) end end end
  assert(world.nextId>maxLocal,'Next local ID was already allocated')
 end
 assert(c.nextPersonId>maxPerson,'Next person ID was already allocated')
 if c.features.logistics==1 then require('src.logistics').validate(c) end
 if isTravel(c) then require('src.travel').validate(c) end
 return true
end

local function attach(world,siteId,nextPerson)
 if not world.baseline then world.baseline=Metrics.measure(world) end
 world.frontier={version=1,siteId=siteId};for _,worker in ipairs(sortedWorkers(world)) do worker.personId=nextPerson;nextPerson=nextPerson+1 end
 return nextPerson
end
function Campaign.new(source)
 assert(type(source)=='table','Campaign needs a local world');W.validate(source);assert(source.frontier==nil,'Local world is already attached to a campaign')
 local world=U.deep(source);local nextPerson=attach(world,1,1)
 local c={format=Campaign.format,version=Campaign.version,ruleset=Campaign.ruleset,features={core=1},tick=world.tick,mode=world.mode,seed=world.seed,society={id=1,origin='abandoned_convicts',independent=true},nextPersonId=nextPerson,sites={{id=1,ownerSocietyId=1,world=world}}}
 Campaign.validate(c);return c
end

local function sortedValues(values)
 local out={};for i,value in ipairs(values) do assert(type(value)=='string','Generation key must be a string');out[i]=value end;table.sort(out);return out
end
local function recipeFrom(o,populated)
 return {version=1,preset=o.preset,layout=o.layout,climate=o.climate,openness=o.openness,biomeScale=o.biomeScale,features=o.features,density=o.density,crew=o.crew,width=o.width,height=o.height,populated=populated}
end
local function regionOptions(options)
 local o=options or {};local base={preset=o.preset or 'frontier',layout=o.layout or C.layout,climate=o.climate or C.climate,openness=o.openness or C.openness,biomeScale=o.biomeScale or C.biomeScale,features=o.features or C.features,density=o.density or C.density,crew=o.crew or C.crew,width=o.width or C.width,height=o.height or C.height,mode=o.mode or C.mode,logistics=o.logistics==true,travel=o.travel==true}
 for key in pairs(o) do assert(base[key]~=nil or key=='bodyOrder','Unknown campaign region option '..tostring(key)) end;assert(base.mode=='challenge' or base.mode=='practice','Invalid campaign region mode');return base
end
local function bodyOrder(order)
 if order==nil then return {1,2,3} end;assert(dense(order,'campaign body construction order',3)==3,'Invalid campaign body construction order')
 local seen={};for _,id in ipairs(order) do U.integer(id,'campaign body construction ID',1,3);assert(not seen[id],'Duplicate campaign body construction ID');seen[id]=true end;return order
end
function Campaign.newRegion(master,options)
 local Random=require('src.campaign_random');local G=require('src.generate');local Layouts=require('src.generation.layouts');local Biomes=require('src.biomes')
 U.integer(master,'campaign master seed',1,2147483646);local home=regionOptions(options);local layouts,climates=sortedValues(Layouts.names),sortedValues(Biomes.climates);local bodies,sitesById={},{}
 for _,id in ipairs(bodyOrder(options and options.bodyOrder)) do
  local terrainSeed=Random.derive(master,'region/body/'..id..'/terrain/v1');local recipe,world
  if id==1 then
   recipe=recipeFrom(home,true);world=G.make(terrainSeed,home.preset,home.mode,home.width,home.height,{layout=home.layout,climate=home.climate,openness=home.openness,biomeScale=home.biomeScale,features=home.features,density=home.density,crew=home.crew})
  else
   local stream=Random.new(Random.derive(master,'region/body/'..id..'/recipe/v1'))
   -- Fixed selection order: layout first, then climate.
   local moon={preset='frontier',layout=layouts[Random.uniform(stream,#layouts)],climate=climates[Random.uniform(stream,#climates)],openness=0.48,biomeScale=1,features='living',density=1,crew=3,width=192,height=112}
   recipe=recipeFrom(moon,false);world=G.makeUnpopulated(terrainSeed,home.mode,moon.width,moon.height,{layout=moon.layout,climate=moon.climate,openness=moon.openness,biomeScale=moon.biomeScale,features=moon.features,density=moon.density,crew=moon.crew})
  end
  bodies[id]={id=id,kind=id==1 and 'planet' or 'moon',name=id==1 and 'Home Planet' or (id==2 and 'Moon I' or 'Moon II'),siteId=id,terrainSeed=terrainSeed,recipe=recipe}
  if id>1 then bodies[id].parentBodyId=1 end
  sitesById[id]={id=id,bodyId=id,ownerSocietyId=id==1 and 1 or nil,world=world}
 end
 local sites={sitesById[1],sitesById[2],sitesById[3]};local nextPerson=1;for _,site in ipairs(sites) do nextPerson=attach(site.world,site.id,nextPerson) end
 assert(not home.travel or home.logistics,'Travel campaigns require logistics')
 local c={format=Campaign.format,version=Campaign.version,ruleset=Campaign.ruleset,features={core=1,region=1},tick=0,mode=home.mode,seed=master,society={id=1,origin='abandoned_convicts',independent=true},nextPersonId=nextPerson,sites=sites,region={version=1,bodies={bodies[1],bodies[2],bodies[3]},notices={}}}
 if home.logistics then c.features.logistics=1;c.logistics=require('src.logistics').new(c.sites[1],home.travel) end
 if home.travel then c.features.travel=1;c.travel=require('src.travel').new() end
 Campaign.validate(c);return c
end
function Campaign.metrics(c)
 Campaign.validate(c);local totals,sites={},{}
 local Travel=isTravel(c) and require('src.travel') or nil
 for _,site in ipairs(Campaign.sites(c)) do
  local measured=Metrics.measure(site.world)
  if c.features.logistics==1 then
   local reconciliation=require('src.logistics').reconciliation(c,site.id);measured.cargo=reconciliation.cargo;measured.reconciliation=reconciliation
   measured.food=measured.food+reconciliation.cargo.food;measured.water=measured.water+reconciliation.cargo.water
   measured.mineral=measured.mineral+reconciliation.cargo.stone+reconciliation.cargo.soil+reconciliation.cargo.metal
   if measured.foodBudget then measured.foodBudget=measured.foodBudget+reconciliation.cargo.food end
   if site.world.baseline then
    measured.waterResidual=measured.water+site.world.ledger.waterUsed-site.world.ledger.waterMade-site.world.baseline.water
    measured.mineralResidual=measured.mineral+site.world.ledger.demolitionWaste-(site.world.ledger.mineralMade or 0)-site.world.baseline.mineral
    measured.foodResidual=(measured.foodBudget or measured.food)+site.world.ledger.foodEaten-site.world.ledger.foodGrown-(site.world.baseline.foodBudget or site.world.baseline.food)
   end
  end
  if Travel then
   local account=Travel.account(c,site.id);measured.travel={imports=account.imports,exports=account.exports,consumed=account.consumed}
   local mineralExport=account.exports.stone+account.exports.soil+account.exports.metal
   local mineralImport=account.imports.stone+account.imports.soil+account.imports.metal
   local mineralConsumed=account.consumed.stone+account.consumed.soil+account.consumed.metal
   measured.foodResidual=measured.foodResidual+account.exports.food-account.imports.food+account.consumed.food
   measured.waterResidual=measured.waterResidual+account.exports.water-account.imports.water+account.consumed.water
   measured.mineralResidual=measured.mineralResidual+mineralExport-mineralImport+mineralConsumed
  end
  sites[#sites+1]={siteId=site.id,metrics=measured};for _,key in ipairs(U.keys(measured)) do if type(measured[key])=='number' then totals[key]=(totals[key] or 0)+measured[key] end end
 end
 if Travel then
  local transit=Travel.reconciliation(c).transit;totals.food=(totals.food or 0)+transit.cargo.food;totals.water=(totals.water or 0)+transit.cargo.water
  totals.mineral=(totals.mineral or 0)+transit.cargo.stone+transit.cargo.soil+transit.cargo.metal
 end
 return {sites=sites,totals=totals,transit=Travel and Travel.reconciliation(c).transit or nil}
end
local function appendNotice(c,site,event,ordinal)
 if not isRegion(c) then return end;local notices=c.region.notices;local prior=notices[#notices]
 if prior and prior.siteId==site.id and prior.kind==event.kind and prior.text==event.text then prior.count=prior.count+1;return end
 notices[#notices+1]={tick=c.tick,siteId=site.id,ordinal=ordinal,kind=event.kind,text=event.text,count=1};while #notices>128 do table.remove(notices,1) end
end
function Campaign.addNotice(c,siteId,kind,text,ordinal)
 local record=Campaign.site(c,siteId);assert(record,'Unknown notice site')
 appendNotice(c,record,{kind=kind,text=text},ordinal or 0)
end
local function noticesForSite(c,site)
 if not isRegion(c) then return end;local start=1
 for i=#site.world.events,1,-1 do if site.world.events[i].tick<c.tick then start=i+1;break end end
 for i=start,#site.world.events do local event=site.world.events[i];if event.tick==c.tick then appendNotice(c,site,event,event.id) end end
 if W.alive(site.world)==0 then appendNotice(c,site,{kind='depopulation',text='Settlement has no living crew.'},100000000) end
end
function Campaign.clone(c) local copy=U.deep(c);Campaign.validate(copy);return copy end
function Campaign.step(c,commands,clock)
 local start=clock and clock();c.tick=c.tick+1;local sites=orderedSites(c)
 for _,site in ipairs(sites) do Sim.begin(site.world);assert(site.world.tick==c.tick,'Campaign site tick diverged') end
 if commands and #commands>0 then local Command=require('src.campaign_commands');for _,command in ipairs(commands) do Command.apply(c,command) end end
 if c.features.logistics==1 then require('src.logistics').reconcile(c) end
 local timings={};if clock then timings.commands=clock()-start end
 for _,site in ipairs(sites) do
  local context=c.features.logistics==1 and {campaign=c,siteId=site.id} or nil
  timings[site.id]=Sim.body(site.world,clock,{},context)
 end
 if isTravel(c) then require('src.travel').step(c) end
 for _,site in ipairs(sites) do noticesForSite(c,site) end
 if not clock and #sites==1 then return timings[1] end;return timings
end
return Campaign
