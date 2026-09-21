-- Deterministic bounded G06 continuation smoke/soak.  Focused G06 tests own
-- geometry and custody assertions; this script keeps a live security campaign
-- ticking so raid, grievance, and save-shape state are exercised together.
local Campaign=require('src.campaign')
local Codec=require('src.campaign_codec')
local F=require('tests.fixtures')
local seed=assert(tonumber(arg[1]),'usage: g06_soak.lua <seed> <ticks>')
local ticks=assert(tonumber(arg[2]),'usage: g06_soak.lua <seed> <ticks>')
assert(ticks>=1 and ticks<=30000,'ticks must be 1..30000')
local w=F.world('practice');F.worker(w,10,24,'Security one');F.worker(w,18,24,'Security two')
local c=Campaign.new(w,{body=true,visibility=true,equipment=true,safe_excavation=true,knowledge=true,psychology=true,industry=true,factions=true,security=true})
local hostile=c.factions.factions[1];hostile.relations[1].tension=100;hostile.relations[1].grievance=100;hostile.culture.expansion=100;hostile.culture.hierarchy=100;hostile.stocks.food=200;hostile.stocks.metal=200
local initial=#Codec.encode(c)
for _=1,ticks do Campaign.step(c) end
Campaign.validate(c);local encoded=Codec.encode(c);assert(#encoded<32*1024*1024,'save bound exceeded')
local raids,resolved=0,0;for _,r in ipairs(c.security.raids) do raids=raids+1;if r.status=='resolved' then resolved=resolved+1 end end
print(('G06 soak seed=%d ticks=%d save=%d->%d raids=%d resolved=%d securityEvents=%d'):format(seed,ticks,initial,#encoded,raids,resolved,#c.sites[1].world.security.events))
