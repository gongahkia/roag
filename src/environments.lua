-- COS-G07: bounded local-system environments.  Profiles are campaign data;
-- local exposure is derived only for instantiated, feature-enabled worlds.
local U=require('src.util')
local R=require('src.campaign_random')
local W=require('src.world')
local E={version=1,siteCount=7}

local catalog={
 airless_crag={archetype='airless_crag',gravityPct=40,atmosphere='vacuum',thermalKind='cold',thermalSeverity=1,radiationSeverity=2,solarPct=120,machineWearPct=115},
 dust_basin={archetype='dust_basin',gravityPct=80,atmosphere='thin',thermalKind='hot',thermalSeverity=1,radiationSeverity=1,solarPct=95,machineWearPct=130},
 cold_hollow={archetype='cold_hollow',gravityPct=60,atmosphere='thin',thermalKind='cold',thermalSeverity=2,radiationSeverity=1,solarPct=75,machineWearPct=115},
 heavy_garden={archetype='heavy_garden',gravityPct=125,atmosphere='breathable',thermalKind='mild',thermalSeverity=0,radiationSeverity=0,solarPct=85,machineWearPct=100},
 ash_world={archetype='ash_world',gravityPct=95,atmosphere='toxic',thermalKind='hot',thermalSeverity=2,radiationSeverity=1,solarPct=105,machineWearPct=120},
 twilight_moon={archetype='twilight_moon',gravityPct=50,atmosphere='vacuum',thermalKind='cold',thermalSeverity=2,radiationSeverity=2,solarPct=60,machineWearPct=110},
}
local order={'airless_crag','dust_basin','cold_hollow','heavy_garden','ash_world','twilight_moon'}
local first={'Astra','Brin','Calyx','Dara','Eris','Faro','Galen','Hedra'}
local last={'reach','hollow','crag','basin','veil','garden','march','rise'}
local atmosphere={breathable=true,thin=true,toxic=true,vacuum=true}
local thermal={mild=true,cold=true,hot=true}

local function copy(t) return U.deep(t) end
function E.enabled(c) return c and c.features and c.features.environments==1 end
function E.catalog() return copy(catalog) end
function E.profile(c,siteId)
 for _,body in ipairs(c.region and c.region.bodies or {}) do if body.siteId==siteId then return body.environment end end
end
function E.validateProfile(p)
 assert(type(p)=='table','Missing environment profile')
 for key in pairs(p) do assert(({version=true,archetype=true,gravityPct=true,atmosphere=true,thermalKind=true,thermalSeverity=true,radiationSeverity=true,solarPct=true,machineWearPct=true})[key],'Unknown environment profile key '..tostring(key)) end
 if p.version~=nil then assert(p.version==E.version,'Unsupported environment profile version') end
 assert(type(p.archetype)=='string' and (catalog[p.archetype] or p.archetype=='home_planet' or p.archetype=='moon_i' or p.archetype=='moon_ii'),'Unknown environment archetype')
 U.integer(p.gravityPct,'environment gravity',20,160);assert(atmosphere[p.atmosphere],'Invalid atmosphere');assert(thermal[p.thermalKind],'Invalid thermal kind')
 U.integer(p.thermalSeverity,'thermal severity',0,2);U.integer(p.radiationSeverity,'radiation severity',0,2);U.integer(p.solarPct,'solar percentage',40,160);U.integer(p.machineWearPct,'machine wear percentage',80,160)
 return true
end
function E.established(id)
 if id==1 then return {archetype='home_planet',gravityPct=100,atmosphere='breathable',thermalKind='mild',thermalSeverity=0,radiationSeverity=0,solarPct=100,machineWearPct=100} end
 if id==2 then return {archetype='moon_i',gravityPct=55,atmosphere='thin',thermalKind='cold',thermalSeverity=1,radiationSeverity=1,solarPct=105,machineWearPct=105} end
 return {archetype='moon_ii',gravityPct=70,atmosphere='thin',thermalKind='cold',thermalSeverity=2,radiationSeverity=1,solarPct=85,machineWearPct=110}
end
local function shuffled(seed)
 local ids={};for i,key in ipairs(order) do ids[i]=key end
 local random=R.new(R.derive(seed,'environment/frontier/archetypes/v1'))
 for i=#ids,2,-1 do local j=R.uniform(random,i);ids[i],ids[j]=ids[j],ids[i] end
 return ids
end
function E.frontierProfiles(seed)
 -- The fixed anchors satisfy low gravity, high gravity and non-breathable
 -- requirements; the two seeded selections supply deterministic variety.
 local picked={'airless_crag','heavy_garden'};local used={airless_crag=true,heavy_garden=true}
 for _,key in ipairs(shuffled(seed)) do if not used[key] then picked[#picked+1]=key;used[key]=true;if #picked==4 then break end end end
 local out={};for i,key in ipairs(picked) do out[i]=copy(catalog[key]) end
 return out
end
function E.names(seed,count)
 local random=R.new(R.derive(seed,'environment/frontier/names/v1'));local out,used={},{}
 for i=1,count do
  local name
  repeat name=first[R.uniform(random,#first)]..' '..last[R.uniform(random,#last)] until not used[name]
  used[name]=true;out[i]=name
 end
 return out
end
function E.attachWorld(w,profile)
 E.validateProfile(profile);w.environment={version=E.version,archetype=profile.archetype,gravityPct=profile.gravityPct,atmosphere=profile.atmosphere,thermalKind=profile.thermalKind,thermalSeverity=profile.thermalSeverity,radiationSeverity=profile.radiationSeverity,solarPct=profile.solarPct,machineWearPct=profile.machineWearPct}
 w.frontier.environments=1
 for _,a in ipairs(w.workers) do E.attachPerson(a) end
end
function E.attachPerson(a)
 a.environment=a.environment or {atmosphere=0,thermal=0,radiation=0,danger=false,warnedAtmosphere=false,warnedThermal=false,warnedRadiation=false}
end
function E.validatePerson(x)
 assert(type(x)=='table','Missing environmental exposure')
 for key in pairs(x) do assert(({atmosphere=true,thermal=true,radiation=true,danger=true,warnedAtmosphere=true,warnedThermal=true,warnedRadiation=true})[key],'Unknown environmental exposure key '..tostring(key)) end
 for _,key in ipairs({'atmosphere','thermal','radiation'}) do U.integer(x[key],'environmental '..key..' exposure',0,100) end
 for _,key in ipairs({'danger','warnedAtmosphere','warnedThermal','warnedRadiation'}) do assert(type(x[key])=='boolean','Invalid environmental flag') end
end
function E.jumpMax(w)
 local gravity=w and w.environment and w.environment.gravityPct or 100
 return gravity<=60 and 3 or gravity>110 and 1 or 2
end
function E.fallDamage(base,w)
 local gravity=w and w.environment and w.environment.gravityPct or 100
 return math.floor((base*gravity+99)/100)
end
local function suited(c,a)
 return c.features.equipment==1 and require('src.equipment').equipped(c,a.personId,'environment')~=nil
end
local function regulated(w,a)
 for _,s in pairs(w.structures) do
  if s.kind=='environmental_regulator' and s.enabled and s._powerGranted and (not s.sabotagedUntil or s.sabotagedUntil<w.tick) then
   local x,y=s.gx*4-2,s.gy*4-2
   if math.abs(a.x-x)+math.abs(a.y-y)<=24 then return true end
  end
 end
 return false
end
local function channel(a,key,rate,warning)
 local old=a.environment[key];local next=rate==0 and math.max(0,old-2) or math.min(100,old+rate);a.environment[key]=next
 if old<50 and next>=50 and not a.environment[warning] then a.environment[warning]=true end
 if next<50 then a.environment[warning]=false end
end
function E.step(c,site)
 if not E.enabled(c) or not site.world or c.tick%10~=0 then return end
 local w,p=site.world,site.world.environment;assert(p,'Environment state is missing')
 for _,a in ipairs(w.workers) do if a.alive then
  E.attachPerson(a);local suit=suited(c,a);local cover=regulated(w,a)
  local atmosphereRate=suit and 0 or (p.atmosphere=='breathable' and 0 or p.atmosphere=='thin' and 1 or p.atmosphere=='toxic' and 3 or 5)
  local thermalRate=cover and 0 or math.max(0,p.thermalSeverity-(suit and 1 or 0))
  local radiationRate=cover and 0 or math.max(0,p.radiationSeverity-(suit and 1 or 0))
  channel(a,'atmosphere',atmosphereRate,'warnedAtmosphere');channel(a,'thermal',thermalRate,'warnedThermal');channel(a,'radiation',radiationRate,'warnedRadiation')
  local maximum=math.max(a.environment.atmosphere,a.environment.thermal,a.environment.radiation);a.environment.danger=maximum>=70
 if maximum>=90 then a.hp=a.hp-1;if a.hp<=0 then require('src.colonists').kill(w,a,'environmental exposure',{campaign=c,siteId=site.id}) end end
 end end
 -- External people use the same bounded channels.  Their suit is a real
 -- raid-owned item record and therefore cannot be silently treated as an
 -- innate vacuum immunity.
 if c.features.security==1 then for _,raid in ipairs(c.security.raids) do
  if raid.siteId==site.id and (raid.status=='active' or raid.status=='withdrawing') then for _,a in ipairs(raid.actors) do if a.alive then
   E.attachPerson(a);local suit=a.suit~=nil;local cover=regulated(w,a)
   local atmosphereRate=suit and 0 or (p.atmosphere=='breathable' and 0 or p.atmosphere=='thin' and 1 or p.atmosphere=='toxic' and 3 or 5)
   local thermalRate=cover and 0 or math.max(0,p.thermalSeverity-(suit and 1 or 0))
   local radiationRate=cover and 0 or math.max(0,p.radiationSeverity-(suit and 1 or 0))
   channel(a,'atmosphere',atmosphereRate,'warnedAtmosphere');channel(a,'thermal',thermalRate,'warnedThermal');channel(a,'radiation',radiationRate,'warnedRadiation')
   local maximum=math.max(a.environment.atmosphere,a.environment.thermal,a.environment.radiation);a.environment.danger=maximum>=70
   if maximum>=90 then a.hp=a.hp-1;if a.hp<=0 then a.hp=0;a.alive=false;a.dead=true end end
  end end end
 end end
end
function E.transitRecover(a,tick)
 if a.environment and tick%10==0 then
  for _,key in ipairs({'atmosphere','thermal','radiation'}) do a.environment[key]=math.max(0,a.environment[key]-2) end
  a.environment.danger=math.max(a.environment.atmosphere,a.environment.thermal,a.environment.radiation)>=70
 end
end
-- Environmental safety is deliberately an ordinary bounded navigation choice:
-- first acquire a reachable loose suit, otherwise seek a powered regulator.
-- It never teleports a body or aliases an item into a personal inventory.
function E.seekSafety(c,w,a)
 if not a.alive or regulated(w,a) then return true end
 local N=require('src.nav');local Equipment=require('src.equipment');local siteId=w.frontier.siteId
 local flood=N.flood(w,a.x,a.y,true)
 local item,path=Equipment.nearestLoose(c,siteId,'frontier_suit',w,a,flood,nil,false)
 if item then
  if N.reach(w,a.x,a.y,item.x,item.y,4) then
   Equipment.equip(c,item,a,'environment');a.status='Equipped Frontier Suit';return true
  end
  local nextIndex=path and path[1]
  if nextIndex then local x,y=W.xy(w,nextIndex);if N.edge(w,a.x,a.y,x,y) then a.x,a.y=x,y;a.status='Seeking Frontier Suit';return true end end
 end
 local path0=select(1,N.closest(w,flood,function(x,y)
  for _,s in pairs(w.structures) do if s.kind=='environmental_regulator' and s.enabled and s._powerGranted and N.reachRect(w,x,y,s.gx,s.gy) then return true end end
  return false
 end))
 local nextIndex=path0 and path0[1]
 if nextIndex then local x,y=W.xy(w,nextIndex);if N.edge(w,a.x,a.y,x,y) then a.x,a.y=x,y;a.status='Seeking Environmental Regulator';return true end end
 a.status='Environmental danger';return false
end
function E.torchWorks(w) return not (w.environment and w.environment.atmosphere=='vacuum') end
return E
