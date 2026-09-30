-- Deterministic rectangular inventory. It owns placement records and direct
-- references to physical items, but never creates or copies those objects.
local Inventory = {}
Inventory.__index = Inventory

Inventory.DEFAULT_WIDTH = 5
Inventory.DEFAULT_HEIGHT = 4
Inventory.DEFAULT_THRESHOLDS = {
  burdened = 5,
  heavy = 9,
  overloaded = 13,
}

local function copy_thresholds(source)
  local result = {}
  for key, value in pairs(source) do
    result[key] = value
  end
  return result
end

local function cell_key(x, y)
  return x .. ":" .. y
end

local function dimensions(item, rotated)
  local footprint = assert(item.footprint, "Inventory item must have a footprint")
  if rotated then
    return footprint.height, footprint.width
  end
  return footprint.width, footprint.height
end

function Inventory.new(options)
  options = options or {}
  local width = options.width or Inventory.DEFAULT_WIDTH
  local height = options.height or Inventory.DEFAULT_HEIGHT
  assert(type(width) == "number" and width > 0 and width % 1 == 0, "Inventory width must be a positive integer")
  assert(type(height) == "number" and height > 0 and height % 1 == 0, "Inventory height must be a positive integer")
  return setmetatable({
    width = width,
    height = height,
    thresholds = copy_thresholds(options.thresholds or Inventory.DEFAULT_THRESHOLDS),
    entries = {},
    by_id = {},
    cells = {},
  }, Inventory)
end

function Inventory:footprint(item, rotated)
  if rotated and not item.footprint.rotatable then
    return nil, "Item '" .. item.physical_id .. "' cannot rotate"
  end
  return dimensions(item, rotated)
end

function Inventory:get(physical_id)
  return self.by_id[physical_id]
end

function Inventory:item_at(x, y)
  local entry = self.cells[cell_key(x, y)]
  return entry and self.by_id[entry.physical_id] or nil
end

function Inventory:can_place(item, x, y, rotated, ignored_id)
  if type(item) ~= "table" or type(item.physical_id) ~= "string" or item.physical_id == "" then
    return false, "Inventory item must have a stable physical ID"
  end
  if self.by_id[item.physical_id] and self.by_id[item.physical_id].item ~= item and item.physical_id ~= ignored_id then
    return false, "Physical item '" .. item.physical_id .. "' is already in this inventory"
  end
  local width, height = self:footprint(item, rotated)
  if not width then
    return false, height
  end
  if x < 1 or y < 1 or x + width - 1 > self.width or y + height - 1 > self.height then
    return false, "Item does not fit within inventory bounds"
  end
  for cell_x = x, x + width - 1 do
    for cell_y = y, y + height - 1 do
      local occupant = self.cells[cell_key(cell_x, cell_y)]
      if occupant and occupant.physical_id ~= ignored_id then
        return false, "Item overlaps '" .. occupant.physical_id .. "'"
      end
    end
  end
  return true
end

function Inventory:_occupy(entry)
  local width, height = dimensions(entry.item, entry.rotated)
  for x = entry.x, entry.x + width - 1 do
    for y = entry.y, entry.y + height - 1 do
      self.cells[cell_key(x, y)] = entry
    end
  end
end

function Inventory:_clear(entry)
  local width, height = dimensions(entry.item, entry.rotated)
  for x = entry.x, entry.x + width - 1 do
    for y = entry.y, entry.y + height - 1 do
      self.cells[cell_key(x, y)] = nil
    end
  end
end

function Inventory:place(item, x, y, rotated)
  rotated = rotated or false
  if self.by_id[item.physical_id] then
    return nil, "Physical item '" .. item.physical_id .. "' is already in this inventory"
  end
  local allowed, reason = self:can_place(item, x, y, rotated)
  if not allowed then
    return nil, reason
  end
  local entry = { item = item, physical_id = item.physical_id, x = x, y = y, rotated = rotated }
  self.entries[#self.entries + 1] = entry
  self.by_id[entry.physical_id] = entry
  self:_occupy(entry)
  return entry
end

function Inventory:remove(physical_id)
  local entry = self.by_id[physical_id]
  if not entry then
    return nil, "Physical item '" .. tostring(physical_id) .. "' is not in this inventory"
  end
  self:_clear(entry)
  self.by_id[physical_id] = nil
  for index, candidate in ipairs(self.entries) do
    if candidate == entry then
      table.remove(self.entries, index)
      break
    end
  end
  return entry.item, entry
end

function Inventory:move(physical_id, x, y, rotated)
  local entry = self.by_id[physical_id]
  if not entry then
    return nil, "Physical item '" .. tostring(physical_id) .. "' is not in this inventory"
  end
  if rotated == nil then
    rotated = entry.rotated
  end
  local allowed, reason = self:can_place(entry.item, x, y, rotated, physical_id)
  if not allowed then
    return nil, reason
  end
  self:_clear(entry)
  entry.x, entry.y, entry.rotated = x, y, rotated
  self:_occupy(entry)
  return entry
end

function Inventory:rotate(physical_id)
  local entry = self.by_id[physical_id]
  if not entry then
    return nil, "Physical item '" .. tostring(physical_id) .. "' is not in this inventory"
  end
  return self:move(physical_id, entry.x, entry.y, not entry.rotated)
end

function Inventory:find_first_fit(item)
  local orientations = { false }
  if item.footprint.rotatable then
    orientations[#orientations + 1] = true
  end
  for _, rotated in ipairs(orientations) do
    for y = 1, self.height do
      for x = 1, self.width do
        if self:can_place(item, x, y, rotated) then
          return { x = x, y = y, rotated = rotated }
        end
      end
    end
  end
  return nil, "No space for '" .. item.physical_id .. "'"
end

function Inventory:auto_place(item)
  local placement, reason = self:find_first_fit(item)
  if not placement then
    return nil, reason
  end
  return self:place(item, placement.x, placement.y, placement.rotated)
end

function Inventory:total_mass()
  local mass = 0
  for _, entry in ipairs(self.entries) do
    mass = mass + entry.item.mass
  end
  return mass
end

function Inventory:encumbrance()
  local mass, thresholds = self:total_mass(), self.thresholds
  if mass >= thresholds.overloaded then
    return "OVERLOADED"
  elseif mass >= thresholds.heavy then
    return "HEAVY"
  elseif mass >= thresholds.burdened then
    return "BURDENED"
  end
  return "LIGHT"
end

function Inventory:validate()
  local expected_cells, seen = {}, {}
  for _, entry in ipairs(self.entries) do
    assert(type(entry.physical_id) == "string" and entry.physical_id ~= "", "Inventory entry must have a stable physical ID")
    assert(entry.item and entry.item.physical_id == entry.physical_id, "Inventory entry physical ID does not match item")
    assert(self.by_id[entry.physical_id] == entry, "Inventory entry index is invalid")
    assert(not seen[entry.physical_id], "Inventory contains physical item '" .. entry.physical_id .. "' more than once")
    seen[entry.physical_id] = true
    local allowed, reason = self:can_place(entry.item, entry.x, entry.y, entry.rotated, entry.physical_id)
    assert(allowed, reason)
    local width, height = dimensions(entry.item, entry.rotated)
    for x = entry.x, entry.x + width - 1 do
      for y = entry.y, entry.y + height - 1 do
        local key = cell_key(x, y)
        assert(not expected_cells[key], "Inventory entries overlap at " .. key)
        expected_cells[key] = entry
      end
    end
  end
  for physical_id, entry in pairs(self.by_id) do
    assert(seen[physical_id] and seen[physical_id] == true and entry.physical_id == physical_id, "Inventory index contains an unknown entry")
  end
  for key, entry in pairs(expected_cells) do
    assert(self.cells[key] == entry, "Inventory cell occupancy is invalid at " .. key)
  end
  for key, entry in pairs(self.cells) do
    assert(expected_cells[key] == entry, "Inventory has a stale occupied cell at " .. key)
  end
  return true
end

function Inventory:to_data()
  local entries = {}
  for _, entry in ipairs(self.entries) do
    assert(type(entry.item.to_data) == "function", "Inventory item must provide to_data")
    entries[#entries + 1] = {
      physical_id = entry.physical_id,
      x = entry.x,
      y = entry.y,
      rotated = entry.rotated,
      item = entry.item:to_data(),
    }
  end
  table.sort(entries, function(first, second)
    return first.physical_id < second.physical_id
  end)
  return {
    width = self.width,
    height = self.height,
    thresholds = copy_thresholds(self.thresholds),
    entries = entries,
  }
end

function Inventory.from_data(data, decode_item)
  assert(type(data) == "table" and type(data.entries) == "table", "Inventory data must include entries")
  assert(type(decode_item) == "function", "Inventory data requires an item decoder")
  local inventory = Inventory.new({ width = data.width, height = data.height, thresholds = data.thresholds })
  for _, entry_data in ipairs(data.entries) do
    local item = decode_item(entry_data.item)
    assert(item.physical_id == entry_data.physical_id, "Inventory entry physical ID does not match item")
    local entry, reason = inventory:place(item, entry_data.x, entry_data.y, entry_data.rotated)
    assert(entry, reason)
  end
  return inventory
end

return Inventory
