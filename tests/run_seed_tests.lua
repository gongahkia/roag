local Game=require('src.game')
local Profile=require('src.profile')
local RNG=require('src.rng')
local RunSeed=require('src.run_seed')

return function(test)
    test('successive new runs get distinct valid maps across profile reloads',function()
        local disk={}
        function disk:read(name)return self[name]end
        function disk:write(name,bytes)self[name]=bytes;return true end
        local first_profile=Profile.new(disk)
        local _,first=assert(first_profile:allocate_run())
        local first_seed=RunSeed.for_serial(first.next_run_serial-1)
        local second_profile=Profile.new(disk)
        local _,second=assert(second_profile:allocate_run())
        local second_seed=RunSeed.for_serial(second.next_run_serial-1,first_seed)
        assert(first_seed~=second_seed)
        RNG.new(first_seed);RNG.new(second_seed)
        local first_map=Game.new_stage(first_seed).generated_map
        local second_map=Game.new_stage(second_seed).generated_map
        assert(table.concat(first_map.rows)~=table.concat(second_map.rows))
    end)
    test('new run seed skips the current seed even after an older manual choice',function()
        local existing=RunSeed.for_serial(3)
        local fresh=RunSeed.for_serial(3,existing)
        assert(fresh~=existing)
        RNG.new(fresh)
    end)
end
