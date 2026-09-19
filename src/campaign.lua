-- A campaign owns one or more local worlds without changing local-world identity.
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

local function dense(t,label,limit)
 assert(type(t)=='table',label..' must be an array')
 local count,max=0,0
 for key in pairs(t) do
  U.integer(key,label..' index',1,limit);count=count+1
  if key>max then max=key end
 end
 assert(count==max and count<=limit,label..' has a hole or exceeds its limit')
 return max
end

local function sortedWorkers(w)
 local out={}
 for i=1,dense(w.workers,'workers',32) do out[i]=w.workers[i] end
 table.sort(out,function(a,b) return a.id<b.id end)
 return out
end

function Campaign.sites(campaign)
 local out={}
 for i=1,dense(campaign.sites,'campaign sites',1) do out[i]=campaign.sites[i] end
 table.sort(out,function(a,b) return a.id<b.id end)
 return out
end

local function orderedSites(campaign)
 local sites=campaign._orderedSites
 if not sites then sites=Campaign.sites(campaign);campaign._orderedSites=sites end
 return sites
end

function Campaign.site(campaign,siteId)
 U.integer(siteId,'campaign site ID',1,100000000)
 for _,site in ipairs(Campaign.sites(campaign)) do if site.id==siteId then return site end end
end

function Campaign.extinct(campaign)
 local living=0
 for _,site in ipairs(Campaign.sites(campaign)) do living=living+W.alive(site.world) end
 return living==0
end

function Campaign.validate(campaign)
 exact(campaign,{format=true,version=true,ruleset=true,features=true,tick=true,mode=true,seed=true,
  society=true,nextPersonId=true,sites=true},'campaign')
 assert(campaign.format==Campaign.format,'Incompatible campaign state format')
 assert(campaign.version==Campaign.version,'Unsupported campaign state version')
 assert(campaign.ruleset==Campaign.ruleset,'Unsupported campaign ruleset')
 exact(campaign.features,{core=true},'campaign features')
 assert(campaign.features.core==1,'Unsupported campaign core feature version')
 U.integer(campaign.tick,'campaign tick',0,10000000)
 assert(campaign.mode=='challenge' or campaign.mode=='practice','Invalid campaign mode')
 U.integer(campaign.seed,'campaign seed',0,2147483646)
 exact(campaign.society,{id=true,origin=true,independent=true},'campaign society')
 U.integer(campaign.society.id,'society ID',1,100000000)
 assert(campaign.society.id==1 and campaign.society.origin=='abandoned_convicts'
  and campaign.society.independent==true,'Unsupported campaign society')
 U.integer(campaign.nextPersonId,'next person ID',1,100000000)
 local sites=Campaign.sites(campaign)
 assert(#sites==1,'Campaign core requires exactly one site')
 local seenSites,seenPeople,maxPerson={}, {},0
 for _,site in ipairs(sites) do
  exact(site,{id=true,ownerSocietyId=true,world=true},'campaign site')
  U.integer(site.id,'site ID',1,100000000)
  assert(site.id==1 and not seenSites[site.id],'Duplicate or unsupported site ID')
  seenSites[site.id]=true
  assert(site.ownerSocietyId==campaign.society.id,'Invalid site ownership')
  assert(type(site.world)=='table','Missing site world')
  W.validate(site.world)
  local world=site.world
  assert(world.frontier and world.frontier.version==1 and world.frontier.siteId==site.id,'Campaign site marker mismatch')
  assert(world.tick==campaign.tick,'Campaign and site ticks differ')
  assert(world.mode==campaign.mode and world.seed==campaign.seed,'Campaign and site identity differ')
  dense(world.jobs,'jobs',1024);dense(world.items,'items',10000);dense(world.events,'events',240)
  local localIds,maxLocal={},0
  local function localId(record,label)
   assert(type(record)=='table','Malformed '..label)
   U.integer(record.id,label..' ID',1,world.nextId-1)
   assert(not localIds[record.id],'Duplicate local entity ID')
   localIds[record.id]=true
   if record.id>maxLocal then maxLocal=record.id end
  end
  for _,worker in ipairs(sortedWorkers(world)) do
   localId(worker,'worker')
   U.integer(worker.personId,'campaign person ID',1,100000000)
   assert(not seenPeople[worker.personId],'Duplicate campaign person ID')
   seenPeople[worker.personId]=true
   if worker.personId>maxPerson then maxPerson=worker.personId end
  end
  for _,job in ipairs(world.jobs) do localId(job,'job') end
  for _,item in ipairs(world.items) do localId(item,'item') end
  for _,slot in ipairs(U.keys(world.structures)) do localId(world.structures[slot],'structure') end
  if world.content then
   for _,kind in ipairs({'flora','fauna','sites'}) do
    for _,record in ipairs(world.content[kind]) do localId(record,kind) end
   end
  end
  assert(world.nextId>maxLocal,'Next local ID was already allocated')
 end
 assert(campaign.nextPersonId>maxPerson,'Next person ID was already allocated')
 return true
end

function Campaign.new(source)
 assert(type(source)=='table','Campaign needs a local world')
 W.validate(source)
 assert(source.frontier==nil,'Local world is already attached to a campaign')
 local world=U.deep(source)
 if not world.baseline then world.baseline=Metrics.measure(world) end
 world.frontier={version=1,siteId=1}
 local people=sortedWorkers(world)
 for i,worker in ipairs(people) do worker.personId=i end
 local campaign={format=Campaign.format,version=Campaign.version,ruleset=Campaign.ruleset,features={core=1},
  tick=world.tick,mode=world.mode,seed=world.seed,
  society={id=1,origin='abandoned_convicts',independent=true},nextPersonId=#people+1,
  sites={{id=1,ownerSocietyId=1,world=world}}}
 Campaign.validate(campaign)
 return campaign
end

function Campaign.clone(campaign)
 local copy=U.deep(campaign)
 Campaign.validate(copy)
 return copy
end

function Campaign.step(campaign,commands,clock)
 local start=clock and clock();campaign.tick=campaign.tick+1
 local sites=orderedSites(campaign)
 for _,site in ipairs(sites) do
  Sim.begin(site.world)
  assert(site.world.tick==campaign.tick,'Campaign site tick diverged')
 end
 if commands and #commands>0 then
  local Command=require('src.campaign_commands')
  for _,command in ipairs(commands) do Command.apply(campaign,command) end
 end
 if not clock and #sites==1 then return Sim.body(sites[1].world) end
 local timings={}
 if clock then timings.commands=clock()-start end
 for _,site in ipairs(sites) do timings[site.id]=Sim.body(site.world,clock,{}) end
 return timings
end

return Campaign
