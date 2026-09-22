-- Optional, slower boundary check. Run from the repository root. No local saves touched.
local G=require('src.generate')
local Map=require('src.mapfile')
local Codec=require('src.codec')
local W=require('src.world')
local H=require('src.history')
local Metrics=require('src.metrics')
local Campaign=require('src.campaign')
local CampaignCodec=require('src.campaign_codec')
local methods={'hybrid','noise','cellular','worms','faults','vaults','karst','labyrinth','roots','chasms','crystal'}
for _,layout in ipairs(methods)do
 local t=os.clock()
 local w=G.make(789,'frontier','challenge',512,256,{layout=layout,climate='abyssal',crew=9,features='living',density=1.5})
 W.validate(w)
 local d=Map.fromWorld(w)
 local text=Map.encode(d)
 local back=Map.toWorld(Map.decode(text),'challenge')
 for i=1,w.n do assert(w.mat[i]==back.mat[i]);assert(w.biomes[i]==back.biomes[i])end
 assert(Codec.encode(d.features)==Codec.encode(Map.fromWorld(back).features))
 local h=H.new(back);for _=1,2 do h:advance()end
 local m=Metrics.measure(back);assert(m.waterResidual==0 and m.foodResidual==0 and m.mineralResidual==0)
 print('PASS '..layout..' / 512x256 / crew 9 / exact map round-trip / two ticks accounting / CPU seconds '..(os.clock()-t))
 collectgarbage('collect')
end
-- G07's bounded local system is a campaign-save envelope rather than another
-- infinite map generator.  Materialize every permitted destination so this is
-- not merely a cheap unvisited-metadata check; legacy map-size checks above
-- remain intentionally independent.
local g07=Campaign.newRegion(79007,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=9,
 logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true})
for siteId=4,7 do Campaign.instantiate(g07,siteId) end
Campaign.validate(g07)
local g07Bytes=#CampaignCodec.encode(g07)
assert(#g07.sites==7 and g07Bytes<32*1024*1024,'G07 seven-destination campaign exceeds 32 MiB whole-save bound')
print('PASS G07 / 7 destinations / 7 instantiated maps / encoded campaign bytes '..g07Bytes..' / under 32 MiB')
-- G08 extends the same bounded campaign envelope: remote metadata is cheap
-- before arrival, but this check deliberately materializes all eleven worlds.
local g08=Campaign.newRegion(80008,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=9,
 logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true,relics=true})
for siteId=4,11 do Campaign.instantiate(g08,siteId) end
Campaign.validate(g08)
local g08Bytes=#CampaignCodec.encode(g08)
assert(#g08.sites==11 and #g08.relics.items==9 and #g08.relics.caches==5 and g08Bytes<32*1024*1024,'G08 eleven-destination campaign exceeds 32 MiB whole-save bound')
print('PASS G08 / 11 destinations / 11 instantiated maps / 5 caches / 9 relics / encoded campaign bytes '..g08Bytes..' / under 32 MiB')
print('Maximum-dimension boundary smoke only, not an interactive performance or long-term balance claim.')
