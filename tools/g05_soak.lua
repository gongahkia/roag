-- Bounded headless G05 living-world trace.  The fixture builds real powered
-- relay infrastructure; the long duration lets background society turns and
-- contact progression run on the campaign clock.
local Campaign=require('src.campaign')
local S=require('src.structures')
local W=require('src.world')
local M=require('src.materials')
local F=require('src.factions')
local X=require('tests.fixtures')
local Codec=require('src.campaign_codec')
local seed=tonumber(arg[1]) or 10505;local ticks=tonumber(arg[2]) or 30000
local o={preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true}
local c=Campaign.newRegion(seed,o);local w=c.sites[1].world
local function clear(gx,gy,n) local x,y,_,y2=W.rect(gx,gy);local x2=(gx+(n or 1)-1)*4;X.fill(w,x,y,x2,y2,M.AIR);X.fill(w,x,y2+1,x2,y2+1,M.ROCK);X.fill(w,x,3,x2,y-1,M.AIR) end
clear(6,6,2);S.install(w,6,6,'solar_array');clear(8,6);S.install(w,8,6,'power_pole');clear(9,6);S.install(w,9,6,'signal_relay')
for i=1,ticks do Campaign.step(c) end
assert(c.factions.scan.known[c.factions.scan.order[1]],'No signal was discovered');assert(#c.factions.incidents>0,'No living-world incident');Campaign.validate(c)
print(('PASS G05 soak: seed=%d tick=%d factions=%d incidents=%d known=%d bytes=%d'):format(seed,c.tick,#c.factions.factions,#c.factions.incidents,(function() local n=0;for _ in pairs(c.factions.scan.known) do n=n+1 end;return n end)(),#Codec.encode(c)))
