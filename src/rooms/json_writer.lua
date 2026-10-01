-- Stable, human-reviewable JSON writer for authored room data. The runtime
-- decoder remains the shared data-only persistence decoder.
local Writer = {}

local function quote(value)
  return '"' .. value:gsub('[%z\1-\31\\"]', function(character)
    local escapes = { ['\\'] = '\\\\', ['"'] = '\\"', ['\b'] = '\\b', ['\f'] = '\\f', ['\n'] = '\\n', ['\r'] = '\\r', ['\t'] = '\\t' }
    return escapes[character] or string.format("\\u%04x", character:byte())
  end) .. '"'
end

local function array_length(value)
  local length = #value
  for key in pairs(value) do
    if type(key) ~= "number" or key < 1 or key % 1 ~= 0 or key > length then return nil end
  end
  return length
end

local function encode(value, level, seen)
  local kind = type(value)
  if value == nil then return "null" end
  if kind == "boolean" then return value and "true" or "false" end
  if kind == "number" then return string.format("%.17g", value) end
  if kind == "string" then return quote(value) end
  assert(kind == "table", "Room JSON can encode data tables only")
  assert(not seen[value], "Room JSON cannot encode cyclic data")
  seen[value] = true
  local indent, next_indent = string.rep("  ", level), string.rep("  ", level + 1)
  local length = array_length(value)
  local pieces = {}
  if length then
    for index = 1, length do pieces[#pieces + 1] = next_indent .. encode(value[index], level + 1, seen) end
    seen[value] = nil
    return #pieces == 0 and "[]" or "[\n" .. table.concat(pieces, ",\n") .. "\n" .. indent .. "]"
  end
  local keys = {}
  for key in pairs(value) do assert(type(key) == "string", "Room JSON keys must be strings"); keys[#keys + 1] = key end
  table.sort(keys)
  for _, key in ipairs(keys) do pieces[#pieces + 1] = next_indent .. quote(key) .. ": " .. encode(value[key], level + 1, seen) end
  seen[value] = nil
  return #pieces == 0 and "{}" or "{\n" .. table.concat(pieces, ",\n") .. "\n" .. indent .. "}"
end

function Writer.encode(value)
  local ok, result = pcall(encode, value, 0, {})
  if not ok then return nil, result end
  return result .. "\n"
end

return Writer
