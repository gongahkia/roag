-- No math.random, love.math.random, bit library, or external dependency.
-- A small Lehmer PRNG plus a nonlinear coordinate mixer; NOT cryptographic.
local U = require("src.util")
local R = {}
local MOD = 2147483647

function R.new(seed)
    U.integer(seed, "seed", 0, MOD - 1)
    local state = seed % (MOD - 1) + 1
    return function()
        state = (state * 48271) % MOD
        return (state - 1) / (MOD - 1)
    end
end

function R.hash(seed, x, y)
    -- Integer products stay below 2^53 for the supported coordinate range.
    local n = ((seed + x * 374761 + y * 668265) % MOD * 48271) % MOD
    local lo, hi = n % 65536, math.floor(n / 65536)
    n = (lo * lo * 31 + hi * hi * 17 + lo * hi * 7) % MOD
    return ((n * 48271) % MOD) / MOD
end

return R
