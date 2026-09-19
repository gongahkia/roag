-- Optional, slower boundary check. Run from the repository root. No local saves touched.
local G=require('src.generate')
local Map=require('src.mapfile')
local Codec=require('src.codec')
local W=require('src.world')
local H=require('src.history')
local Metrics=require('src.metrics')
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
print('Maximum-dimension boundary smoke only, not an interactive performance or long-term balance claim.')
