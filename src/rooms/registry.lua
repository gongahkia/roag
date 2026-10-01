-- Headless registry for the external JSON dungeon-room corpus.
local Config = require("src.rooms.config")
local Json = require("src.persistence.json")
local Store = require("src.rooms.store")
local Template = require("src.rooms.template")
local Registry = require("src.content.registry")

local RoomRegistry = {}
RoomRegistry.__index = RoomRegistry

local function has_tag(template, tag)
  for _, current in ipairs(template.tags or {}) do if current == tag then return true end end
  return false
end

local function same_pattern(template, required)
  return Template.pattern_key(template.connectors) == Template.pattern_key(required)
end

function RoomRegistry.load(options)
  options = options or {}
  local store, content = options.store or Store.new(), options.registry or Registry.load()
  local files, failure = store:list()
  if not files then return nil, failure end
  local self = setmetatable({ store = store, registry = content, templates = {}, order = {}, filenames = {}, validation = {} }, RoomRegistry)
  for _, filename in ipairs(files) do
    local text, read_failure = store:read(filename)
    if not text then return nil, read_failure end
    local template, decode_error = Json.decode(text)
    if not template then return nil, { code = "invalid_json", reason = "Room file " .. filename .. ": " .. decode_error, filename = filename } end
    local validation = Template.validate(template, content, { existing_ids = self.templates })
    if not validation.valid then return nil, { code = "invalid_template", reason = "Room file " .. filename .. " is invalid", filename = filename, errors = validation.errors } end
    self.templates[template.id] = template
    self.order[#self.order + 1] = template.id
    self.filenames[template.id] = filename
    self.validation[template.id] = validation
  end
  table.sort(self.order)
  local coverage = self:coverage()
  if not coverage.valid then return nil, { code = "missing_connector_coverage", reason = "Dungeon room corpus lacks required connector patterns", coverage = coverage } end
  return self
end

function RoomRegistry:list()
  local result = {}
  for _, id in ipairs(self.order) do result[#result + 1] = self.templates[id] end
  return result
end

function RoomRegistry:get(id)
  return self.templates[id]
end

function RoomRegistry:coverage()
  local patterns, missing = {}, {}
  for _, required in ipairs(Config.REQUIRED_PATTERNS) do
    local key, count = Template.pattern_key(required), 0
    for _, template in ipairs(self:list()) do
      local rotations = template.allow_rotation and 4 or 1
      for turn = 0, rotations - 1 do
        if same_pattern(Template.rotate(template, turn), required) then count = count + 1 end
      end
    end
    patterns[key] = count
    if count == 0 then missing[#missing + 1] = key end
  end
  return { valid = #missing == 0, patterns = patterns, missing = missing }
end

function RoomRegistry:candidates(required_sides, tags, exclude_tags)
  local result = {}
  for _, template in ipairs(self:list()) do
    local allowed = true
    for _, tag in ipairs(tags or {}) do if not has_tag(template, tag) then allowed = false break end end
    for _, tag in ipairs(exclude_tags or {}) do if has_tag(template, tag) then allowed = false break end end
    if allowed then
      local rotations = template.allow_rotation and 4 or 1
      for turn = 0, rotations - 1 do
        local transformed = Template.rotate(template, turn)
        if same_pattern(transformed, required_sides) then
          transformed.rotation = turn * 90
          transformed.base_id = template.id
          result[#result + 1] = transformed
        end
      end
    end
  end
  table.sort(result, function(a, b)
    return a.base_id == b.base_id and a.rotation < b.rotation or a.base_id < b.base_id
  end)
  return result
end

return RoomRegistry
