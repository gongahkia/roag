-- Give each reserved profile run a distinct, reproducible map seed.
local RNG=require('src.rng')
local RunSeed={}
local maximum=2147483646

function RunSeed.for_serial(serial,previous)
    assert(type(serial)=='number' and serial%1==0 and serial>=1,
        'invalid run serial')
    local position=(serial-1)%maximum+1
    local seed=RNG.next_raw(RNG.new(position))
    -- Older saves may have chosen this seed manually; never repeat the active run.
    if seed==previous then seed=seed%maximum+1 end
    return seed
end

return RunSeed
