local R = require("src.random")
local N = {}
local function smooth(t) return t * t * t * (t * (t * 6 - 15) + 10) end
local function lerp(a, b, t) return a + (b - a) * t end

-- Interpolated VALUE noise, not Perlin gradient noise or simplex noise.
function N.value(seed, x, y)
    local ix, iy = math.floor(x), math.floor(y)
    local tx, ty = smooth(x - ix), smooth(y - iy)
    local a = lerp(R.hash(seed, ix, iy), R.hash(seed, ix + 1, iy), tx)
    local b = lerp(R.hash(seed, ix, iy + 1), R.hash(seed, ix + 1, iy + 1), tx)
    return lerp(a, b, ty)
end

function N.fbm(seed, x, y, octaves, ridged)
    local sum, weight, amplitude = 0, 0, 1
    for octave = 1, (octaves or 5) do
        local v = N.value(seed + octave * 1013, x, y)
        if ridged then v = (1 - math.abs(2 * v - 1))^2 end
        sum, weight = sum + v * amplitude, weight + amplitude
        amplitude = amplitude * 0.5
        x, y = x * 2, y * 2
    end
    return sum / weight
end

function N.warp(seed, x, y)
    local dx = N.fbm(seed + 730, x, y, 3) - 0.5
    local dy = N.fbm(seed + 910, x, y, 3) - 0.5
    return N.fbm(seed, x + 2 * dx, y + 2 * dy, 5)
end

return N
