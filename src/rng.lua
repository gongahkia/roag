-- Deterministic pseudo-random number generator for authoritative run state.
--
-- Park-Miller's generator fits safely in Lua number arithmetic and does not
-- rely on LÖVE's process-global random sequence.
local Rng = {}
Rng.__index = Rng

local MODULUS = 2147483647
local MULTIPLIER = 48271

local function normalize_seed(seed)
  if type(seed) == "string" then
    local value = 0
    for index = 1, #seed do
      value = (value * 31 + seed:byte(index)) % MODULUS
    end
    seed = value
  end

  seed = math.floor(tonumber(seed) or 1) % (MODULUS - 1)
  if seed <= 0 then
    seed = 1
  end
  return seed
end

local function hash_seed(seed, label)
  local value = normalize_seed(seed)
  label = tostring(label or "")
  for index = 1, #label do
    value = (value * 31 + label:byte(index)) % MODULUS
  end
  return normalize_seed(value)
end

function Rng.new(seed)
  local normalized = normalize_seed(seed)
  return setmetatable({
    seed = normalized,
    state = normalized,
  }, Rng)
end

function Rng:next()
  self.state = (self.state * MULTIPLIER) % MODULUS
  return self.state
end

function Rng:float()
  return self:next() / MODULUS
end

function Rng:int(minimum, maximum)
  if maximum == nil then
    maximum = minimum
    minimum = 1
  end
  assert(minimum <= maximum, "RNG range is invalid")
  return minimum + math.floor(self:float() * (maximum - minimum + 1))
end

function Rng:choice(values)
  assert(#values > 0, "Cannot choose from an empty list")
  return values[self:int(#values)]
end

function Rng:shuffle(values)
  local shuffled = {}
  for index, value in ipairs(values) do
    shuffled[index] = value
  end
  for index = #shuffled, 2, -1 do
    local other = self:int(index)
    shuffled[index], shuffled[other] = shuffled[other], shuffled[index]
  end
  return shuffled
end

function Rng:derive(label)
  return Rng.new(hash_seed(self.seed, label))
end

function Rng:clone()
  local clone = Rng.new(self.seed)
  clone.state = self.state
  return clone
end

-- Authoritative save state.  A root seed alone is not enough once a stream
-- has been consumed, so active-run persistence records the current Park-
-- Miller state as well.  This stays deliberately small and data-only.
function Rng:to_data()
  return { seed = self.seed, state = self.state }
end

function Rng.from_data(data)
  assert(type(data) == "table", "RNG data must be a table")
  assert(type(data.seed) == "number" and data.seed % 1 == 0 and data.seed > 0 and data.seed < MODULUS,
    "RNG data has an invalid seed")
  assert(type(data.state) == "number" and data.state % 1 == 0 and data.state > 0 and data.state < MODULUS,
    "RNG data has an invalid state")
  local rng = Rng.new(data.seed)
  rng.state = data.state
  return rng
end

return Rng
