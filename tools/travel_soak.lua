-- Controlled COS-P04 soak: it uses real preparation commands/jobs and a real
-- production route, then shortens no production rules.  It keeps generated
-- worlds quiet only to isolate travel from unrelated ecology mortality.
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Logistics=require('src.logistics')
local Codec=require('src.campaign_codec')
local seed=tonumber(arg[1]) or 73421
local ticks=tonumber(arg[2]) or 10000
assert(seed%1==0 and seed>=1 and seed<=2147483646,'Seed must be a campaign integer')
assert(ticks%1==0 and ticks>=10000 and ticks<=100000,'Ticks must be 10,000..100,000')
local options={preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true}
local campaign=Campaign.newRegion(seed,options)
for _,site in ipairs(campaign.sites) do local c=site.world.content;c.flora={};c.fauna={};c.sites={};c.ruins={};c.signals={};c.discoveries={};c.observed={} end
local h=History.new(campaign)
local function command(kind,fields) local out={scope='campaign',type=kind};for key,value in pairs(fields) do out[key]=value end;return out end
local function advance(n) for _=1,n do assert(h:advance()) end end
local function first(siteId) return h.live.sites[siteId].world.workers[1] end
local function prepare(source,destination,cargo)
 local person=first(source).personId
 assert(h:queue(command('prepare_expedition',{sourceSiteId=source,craftId=1,destinationSiteId=destination,passengers={person},cargo=cargo})));advance(480)
 local m=assert(Logistics.manifest(h.live,Logistics.craft(h.live,1).activeManifestId));assert(h:queue(command('assemble_expedition',{sourceSiteId=source,craftId=1,manifestId=m.id})));advance(180);m=assert(Logistics.manifest(h.live,Logistics.craft(h.live,1).activeManifestId))
 local ready,why=Logistics.readiness(h.live,m);assert(ready,why);assert(h:queue(command('launch_expedition',{sourceSiteId=source,craftId=1,manifestId=m.id,expectedManifestRevision=m.revision})));advance(1)
end
prepare(1,2,{food=4,metal=3});advance(399)
local craft=Logistics.craft(h.live,1);assert(craft.dockedSiteId==2 and h.live.sites[2].ownerSocietyId==1,'Outbound founding did not complete')
assert(h:queue(command('unload_cargo',{sourceSiteId=2,craftId=1,resource='food',amount=2})));advance(220)
prepare(2,1,{food=2,metal=1});advance(399);assert(craft.dockedSiteId==1,'Return did not complete')
prepare(1,2,{food=1,metal=1});advance(399);assert(craft.dockedSiteId==2,'Resupply did not complete')
local saved=h:saveText();local restored=History.fromText(saved);assert(Codec.encode(restored.live)==Codec.encode(h.live),'Soak save/load changed campaign')
while h.frontier<ticks do advance(1) end
local verified,why=History.fromText(h:saveText()):verifyReplay(20000);assert(verified,why)
local metrics=Campaign.metrics(h.live);print(string.format('PASS COS-P04 travel soak: seed=%d ticks=%d site2Owned=%s craftSite=%d receipts=%d encoded=%d bytes checkpoints=%d',seed,h.live.tick,tostring(h.live.sites[2].ownerSocietyId==1),craft.dockedSiteId,#h.live.travel.receipts,#h:saveText(),#h.checkpoints))
print(string.format('Transit cargo food=%d metal=%d; campaign measured food=%d mineral=%d.',metrics.transit.cargo.food,metrics.transit.cargo.metal,metrics.totals.food,metrics.totals.mineral))
