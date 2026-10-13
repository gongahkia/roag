-- Opt-in 100-seed paired-floor generation cost, measured separately from play.
local Generation=require('src.generation')
local Biomes=require('src.biomes')
for _,biome_id in ipairs(Biomes.order) do
    local timings={}
    local retry,repair,fallback=0,0,0
    for seed=1,100 do
        local before=os.clock()
        local pair=Generation.generate_pair(seed,{biome_id=biome_id})
        timings[#timings+1]=(os.clock()-before)*1000
        assert(Generation.validate_pair(pair))
        for _,map in ipairs(pair) do
            retry=retry+#map.diagnostics.attempts-1
            repair=repair+#map.repairs
            if map.diagnostics.fallback then fallback=fallback+1 end
        end
    end
    table.sort(timings)
    local function percentile(p)
        return timings[math.ceil(#timings*p)]
    end
    print(('%s pairs=100 floors=200 generation_p50_ms=%.3f p95_ms=%.3f max_ms=%.3f retries=%d repairs=%d fallbacks=%d'):
        format(biome_id,percentile(.5),percentile(.95),percentile(1),
            retry,repair,fallback))
end
