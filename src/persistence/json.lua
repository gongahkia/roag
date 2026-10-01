-- Compact, dependency-free JSON for active-run data.  It intentionally
-- supports data only (numbers, strings, booleans, arrays, objects, null) and
-- emits object keys in lexical order for reproducible save output.
local Json = {}

local function encode_string(value)
  return '"' .. value:gsub('[%z\1-\31\\"]', function(character)
    local escapes = { ['\\'] = '\\\\', ['"'] = '\\"', ['\b'] = '\\b', ['\f'] = '\\f', ['\n'] = '\\n', ['\r'] = '\\r', ['\t'] = '\\t' }
    return escapes[character] or string.format("\\u%04x", character:byte())
  end) .. '"'
end

local function array_length(value)
  local length = #value
  for key in pairs(value) do
    if type(key) ~= "number" or key < 1 or key % 1 ~= 0 or key > length then
      return nil
    end
  end
  return length
end

local function encode(value, seen)
  local kind = type(value)
  if value == nil then return "null" end
  if kind == "boolean" then return value and "true" or "false" end
  if kind == "number" then
    assert(value == value and value ~= math.huge and value ~= -math.huge, "JSON cannot encode non-finite numbers")
    return string.format("%.17g", value)
  end
  if kind == "string" then return encode_string(value) end
  assert(kind == "table", "JSON can encode data tables only")
  assert(not seen[value], "JSON cannot encode cyclic data")
  seen[value] = true
  local length = array_length(value)
  local parts = {}
  if length then
    for index = 1, length do parts[#parts + 1] = encode(value[index], seen) end
    seen[value] = nil
    return "[" .. table.concat(parts, ",") .. "]"
  end
  local keys = {}
  for key in pairs(value) do
    assert(type(key) == "string", "JSON object keys must be strings")
    keys[#keys + 1] = key
  end
  table.sort(keys)
  for _, key in ipairs(keys) do
    parts[#parts + 1] = encode_string(key) .. ":" .. encode(value[key], seen)
  end
  seen[value] = nil
  return "{" .. table.concat(parts, ",") .. "}"
end

function Json.encode(value)
  local ok, result = pcall(encode, value, {})
  if not ok then return nil, result end
  return result
end

local function decode_error(position, message)
  error("JSON decode error at byte " .. tostring(position) .. ": " .. message, 0)
end

function Json.decode(text)
  if type(text) ~= "string" then return nil, "JSON input must be a string" end
  local position, length = 1, #text
  local function skip()
    while position <= length and text:sub(position, position):match("%s") do position = position + 1 end
  end
  local parse_value
  local function parse_string()
    position = position + 1
    local parts = {}
    while position <= length do
      local character = text:sub(position, position)
      if character == '"' then position = position + 1; return table.concat(parts) end
      if character == "\\" then
        position = position + 1
        local escape = text:sub(position, position)
        local map = { ['"'] = '"', ['\\'] = '\\', ['/'] = '/', b = '\b', f = '\f', n = '\n', r = '\r', t = '\t' }
        if map[escape] then parts[#parts + 1] = map[escape]; position = position + 1
        elseif escape == "u" then
          local hex = text:sub(position + 1, position + 4)
          if not hex:match("^%x%x%x%x$") then decode_error(position, "invalid unicode escape") end
          local code = tonumber(hex, 16)
          -- Save identifiers/content are ASCII today; retain valid UTF-8 for
          -- ordinary BMP text without pulling a framework into the runtime.
          if code < 128 then parts[#parts + 1] = string.char(code)
          elseif code < 2048 then parts[#parts + 1] = string.char(192 + math.floor(code / 64), 128 + code % 64)
          else parts[#parts + 1] = string.char(224 + math.floor(code / 4096), 128 + math.floor(code / 64) % 64, 128 + code % 64) end
          position = position + 5
        else decode_error(position, "invalid escape") end
      else
        if character:byte() < 32 then decode_error(position, "control character in string") end
        parts[#parts + 1] = character; position = position + 1
      end
    end
    decode_error(position, "unterminated string")
  end
  local function parse_array()
    position = position + 1; skip()
    local result = {}
    if text:sub(position, position) == "]" then position = position + 1; return result end
    while true do
      result[#result + 1] = parse_value(); skip()
      local separator = text:sub(position, position)
      if separator == "]" then position = position + 1; return result end
      if separator ~= "," then decode_error(position, "expected ',' or ']'") end
      position = position + 1; skip()
    end
  end
  local function parse_object()
    position = position + 1; skip()
    local result = {}
    if text:sub(position, position) == "}" then position = position + 1; return result end
    while true do
      if text:sub(position, position) ~= '"' then decode_error(position, "expected object key") end
      local key = parse_string(); skip()
      if text:sub(position, position) ~= ":" then decode_error(position, "expected ':'") end
      position = position + 1; skip(); result[key] = parse_value(); skip()
      local separator = text:sub(position, position)
      if separator == "}" then position = position + 1; return result end
      if separator ~= "," then decode_error(position, "expected ',' or '}'") end
      position = position + 1; skip()
    end
  end
  parse_value = function()
    skip(); local character = text:sub(position, position)
    if character == '"' then return parse_string() end
    if character == "{" then return parse_object() end
    if character == "[" then return parse_array() end
    if text:sub(position, position + 3) == "true" then position = position + 4; return true end
    if text:sub(position, position + 4) == "false" then position = position + 5; return false end
    if text:sub(position, position + 3) == "null" then position = position + 4; return nil end
    local number = text:sub(position):match("^-?%d+%.?%d*[eE]?[+-]?%d*")
    if number and number ~= "" then
      local value = tonumber(number)
      if not value then decode_error(position, "invalid number") end
      position = position + #number; return value
    end
    decode_error(position, "unexpected token")
  end
  local ok, value = xpcall(function()
    local result = parse_value(); skip()
    if position <= length then decode_error(position, "trailing data") end
    return result
  end, function(message) return message end)
  if not ok then return nil, value end
  return value
end

return Json
