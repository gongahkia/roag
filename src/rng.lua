local RNG = {}

-- Park-Miller minimal standard. Products stay below 2^53 in Lua number arithmetic.
local MODULUS = 2147483647
local MULTIPLIER = 48271

function RNG.new(seed)
    assert(type(seed) == "number" and seed % 1 == 0 and seed >= 1 and seed < MODULUS,
        "seed must be an integer from 1 to 2147483646")
    return { state = seed }
end

function RNG.next_raw(rng)
    assert(type(rng.state) == "number" and rng.state % 1 == 0 and rng.state >= 1 and rng.state < MODULUS,
        "invalid RNG state")
    rng.state = (rng.state * MULTIPLIER) % MODULUS
    return rng.state
end

function RNG.next_int(rng, maximum)
    assert(type(maximum) == "number" and maximum % 1 == 0 and maximum >= 1 and maximum < MODULUS,
        "invalid RNG range")
    -- Rejection avoids modulo bias for bounded integer draws.
    local span = MODULUS - 1
    local limit = span - (span % maximum)
    local value
    repeat value = RNG.next_raw(rng) - 1 until value < limit
    return (value % maximum) + 1
end

-- Stable independent stream seed; does not consume the parent run RNG.
function RNG.derive(seed, label)
    RNG.new(seed)
    assert(type(label) == "string" and #label > 0, "RNG stream needs a label")
    local value = seed
    for index = 1, #label do
        value = (value * 131 + label:byte(index)) % (MODULUS - 1)
    end
    return value + 1
end

return RNG
