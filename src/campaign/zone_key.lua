-- Canonical identity for a 2D simulation zone.  Z is deliberately part of
-- the zone address rather than a third coordinate inside World/Grid.
local ZoneKey = {}

local function coordinate(value, label)
  assert(type(value) == "number" and value % 1 == 0, "ZoneKey " .. label .. " must be a signed integer")
  return value
end

local key_mt = {
  __newindex = function()
    error("ZoneKey is immutable")
  end,
  __tostring = function(value)
    return ZoneKey.encode(value)
  end,
}

function ZoneKey.new(world_x, world_y, z)
  local values = {
    world_x = coordinate(world_x, "world_x"),
    world_y = coordinate(world_y, "world_y"),
    z = coordinate(z, "z"),
  }
  -- Keep coordinates in a private backing table so normal assignment cannot
  -- accidentally mutate an identity after it has been used as an index key.
  local backing = values
  return setmetatable({}, {
    __index = backing,
    __newindex = key_mt.__newindex,
    __tostring = key_mt.__tostring,
    __pairs = function()
      return pairs(backing)
    end,
    __metatable = "ZoneKey",
  })
end

function ZoneKey.validate(value)
  assert(type(value) == "table", "ZoneKey must be a table")
  coordinate(value.world_x, "world_x")
  coordinate(value.world_y, "world_y")
  coordinate(value.z, "z")
  return true
end

function ZoneKey.from_data(data)
  ZoneKey.validate(data)
  return ZoneKey.new(data.world_x, data.world_y, data.z)
end

function ZoneKey.to_data(value)
  ZoneKey.validate(value)
  return { world_x = value.world_x, world_y = value.world_y, z = value.z }
end

function ZoneKey.encode(value)
  ZoneKey.validate(value)
  return string.format("zone:%d:%d:%d", value.world_x, value.world_y, value.z)
end

function ZoneKey.decode(text)
  assert(type(text) == "string", "ZoneKey encoding must be text")
  local x, y, z = text:match("^zone:([%-]?%d+):([%-]?%d+):([%-]?%d+)$")
  assert(x and y and z, "Malformed ZoneKey '" .. tostring(text) .. "'")
  return ZoneKey.new(tonumber(x), tonumber(y), tonumber(z))
end

-- A safe, deterministic shard name. Signed decimal coordinates contain no
-- path separators and retain their human-readable correspondence to ZoneKey.
function ZoneKey.filename(value)
  ZoneKey.validate(value)
  return string.format("zone_%d_%d_%d.json", value.world_x, value.world_y, value.z)
end

function ZoneKey.equal(left, right)
  return left and right and left.world_x == right.world_x and left.world_y == right.world_y and left.z == right.z
end

return ZoneKey
