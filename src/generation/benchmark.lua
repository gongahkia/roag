-- Generation/interchange experiment, separate from the colony-tick benchmark.
local G=require('src.generate')
local L=require('src.generation.layouts')
local Map=require('src.mapfile')
local Rep=require('src.generation.report')
local U=require('src.util')
local C=require('config')
local B={}
function B.create(clock,options)
 options=options or {}
 return coroutine.create(function()
  local lines={U.csv({'layout','climate','seed','width','height','openness','biome_scale',
   'generation_ms','serialize_ms','deserialize_and_start_ms','retained_world_Lua_KiB','json_bytes','fingerprint',
   'ruins','flora','fauna','features','crew','biomes','air_fraction','air_components','largest_air_fraction','water_cells','lava_cells','ice_cells','ore_cells'})}
  local climates=options.climates or {'balanced'}
  for _,climate in ipairs(climates) do for _,size in ipairs(options.sizes or {{128,80},{256,160}}) do
   for _,layout in ipairs(options.layouts or L.names) do
    -- Warm the matching method/size; one pass is exploratory, not validated JIT warm-up.
    G.make(1,'frontier','challenge',size[1],size[2],{layout=layout,climate=climate})
    for _,seed in ipairs(options.seeds or {1,7,12345}) do
     collectgarbage('collect');local before=collectgarbage('count')
     local start=clock();local w=G.make(seed,'frontier','challenge',size[1],size[2],{layout=layout,climate=climate})
     local gen=(clock()-start)*1000;collectgarbage('collect')
     local retained=collectgarbage('count')-before
     start=clock();local d=Map.fromWorld(w);local text=Map.encode(d);local encode=(clock()-start)*1000
     start=clock();local decoded=Map.decode(text);local restored=Map.toWorld(decoded);local decode=(clock()-start)*1000
     for i=1,w.n do assert(w.mat[i]==restored.mat[i] and w.biomes[i]==restored.biomes[i],'Benchmark map roundtrip mismatch') end
     assert(require('src.codec').encode(require('src.content').template(w))==require('src.codec').encode(require('src.content').template(restored)),'Encounter roundtrip mismatch')
     local r=Rep.measure(w)
     lines[#lines+1]=U.csv({layout,climate,seed,size[1],size[2],0.48,1,gen,encode,decode,retained,#text,
      Map.fingerprint(d),w.content and #w.content.ruins or 0,w.content and #w.content.flora or 0,w.content and #w.content.fauna or 0,w.generation.features,#w.workers,r.biomeCount,r.airFraction,r.airComponents,r.largestAirFraction,r.water,r.lava,r.ice,r.ore})
     coroutine.yield(layout..' / '..climate..' / '..size[1]..'x'..size[2]..' / '..seed)
    end
   end
  end end
  return table.concat(lines,'\n')..'\n',table.concat({
   C.title..' '..C.version..' / generation + map interchange experiment',_VERSION..(jit and (' / '..jit.version) or ''),
   'Default: 11 layouts x 2 sizes x 3 seeds = 66 measured worlds; one warm-up per layout/size.',
   'Clock supplied by caller. Generation includes biome/material fields and starting colony.',
   'Serialization includes RLE packing, validation and JSON encoding. Import includes parsing, validation, cell expansion and starter placement.',
   'Report traversal, checksums, explicit collection, drawing and coroutine yields are outside timed regions.',
   'Automatic GC remains on in timed work. Memory is a post-collection retained Lua-heap delta, not peak, RSS or GPU memory.',
   'One warm-up is not proof of JIT steady state. Rows are exploratory measurements, not algorithm rankings.',
   'Air regions use point-cell 4-neighbor connectivity. They do NOT establish body-aware colonist reachability.',
   'Same seed/layout/settings replay within the same source/runtime; cross-runtime floating-point bit identity not promised.',
  },'\n')..'\n'
 end)
end
return B
