-- Deterministic rectangular inventory. It owns placement records and direct
-- references to physical items, but never creates or copies those objects.
local Inventory = {}
Inventory.__index = Inventory

Inventory.DEFAULT_WIDTH = 7
Inventory.DEFAULT_HEIGHT = 7
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

-- A footprint can optionally be a small bitmap.  `1` marks an occupied
-- inventory cell and `0` leaves a hole, making placement play like actual
-- packing rather than a collection of rectangles.  The bitmap stays in the
-- item definition; entries only retain their anchor and orientation.
local function footprint_cells(item, rotated)
  local footprint = assert(item.footprint, "Inventory item must have a footprint")
  local cells = {}
  local shape = footprint.shape
  if shape then
    for y, row in ipairs(shape) do
      for x = 1, #row do
        if row:sub(x, x) == "1" then
          local cell_x, cell_y = x - 1, y - 1
          if rotated then
            -- Clockwise rotation inside the footprint's bounding box.
            cell_x, cell_y = footprint.height - 1 - cell_y, cell_x
          end
          cells[#cells + 1] = { x = cell_x, y = cell_y }
        end
      end
    end
  else
    local width, height = dimensions(item, rotated)
    for y = 0, height - 1 do
      for x = 0, width - 1 do
        cells[#cells + 1] = { x = x, y = y }
      end
    end
  end
  assert(#cells > 0, "Inventory item footprint must occupy at least one cell")
  return cells
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

function Inventory:footprint_cells(item, rotated)
  if rotated and not item.footprint.rotatable then
    return nil, "Item '" .. item.physical_id .. "' cannot rotate"
  end
  return footprint_cells(item, rotated)
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
  for _, cell in ipairs(footprint_cells(item, rotated)) do
    local occupant = self.cells[cell_key(x + cell.x, y + cell.y)]
    if occupant and occupant.physical_id ~= ignored_id then
      return false, "Item overlaps '" .. occupant.physical_id .. "'"
    end
  end
  return true
end

function Inventory:_occupy(entry)
  for _, cell in ipairs(footprint_cells(entry.item, entry.rotated)) do
    self.cells[cell_key(entry.x + cell.x, entry.y + cell.y)] = entry
  end
end

function Inventory:_clear(entry)
  for _, cell in ipairs(footprint_cells(entry.item, entry.rotated)) do
    self.cells[cell_key(entry.x + cell.x, entry.y + cell.y)] = nil
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

function Inventory:resource_quantity(resource_id)
  local total = 0
  for _, entry in ipairs(self.entries) do
    local item = entry.item
    if item.item_type == "resource_stack" and item.resource_id == resource_id then
      total = total + item.quantity
    end
  end
  return total
end

function Inventory:resource_counts()
  local counts = {}
  for _, entry in ipairs(self.entries) do
    local item = entry.item
    if item.item_type == "resource_stack" then
      counts[item.resource_id] = (counts[item.resource_id] or 0) + item.quantity
    end
  end
  return counts
end

function Inventory:can_afford(costs)
  for resource_id, amount in pairs(costs or {}) do
    if self:resource_quantity(resource_id) < amount then return false, resource_id end
  end
  return true
end

-- Resource consumption is reversible until its caller commits. This keeps
-- build placement atomic even if an authoritative World placement rejects an
-- otherwise preview-valid request.
function Inventory:consume_resources(costs)
  local affordable, missing = self:can_afford(costs)
  if not affordable then return nil, "Insufficient " .. tostring(missing) end
  local resource_ids = {}
  for resource_id in pairs(costs or {}) do resource_ids[#resource_ids + 1] = resource_id end
  table.sort(resource_ids)
  local changes = {}
  for _, resource_id in ipairs(resource_ids) do
    local remaining = costs[resource_id]
    local candidates = {}
    for _, entry in ipairs(self.entries) do
      if entry.item.item_type == "resource_stack" and entry.item.resource_id == resource_id then
        candidates[#candidates + 1] = entry
      end
    end
    table.sort(candidates, function(a, b)
      if a.y ~= b.y then return a.y < b.y end
      if a.x ~= b.x then return a.x < b.x end
      return a.physical_id < b.physical_id
    end)
    for _, entry in ipairs(candidates) do
      if remaining <= 0 then break end
      local amount = math.min(remaining, entry.item.quantity)
      local snapshot = { item = entry.item, x = entry.x, y = entry.y, rotated = entry.rotated,
        physical_id = entry.physical_id, quantity = entry.item.quantity, consumed = amount, removed = false }
      changes[#changes + 1] = snapshot
      if amount == entry.item.quantity then
        self:remove(entry.physical_id)
        snapshot.removed = true
      else
        entry.item:set_quantity(entry.item.quantity - amount)
      end
      remaining = remaining - amount
    end
    assert(remaining == 0, "Resource affordability changed during consumption")
  end
  return changes
end

function Inventory:restore_consumed_resources(changes)
  for index = #changes, 1, -1 do
    local change = changes[index]
    if change.removed then
      change.item:set_quantity(change.quantity)
      local placed, reason = self:place(change.item, change.x, change.y, change.rotated)
      assert(placed, reason)
    else
      local entry = assert(self:get(change.physical_id), "Consumed resource stack disappeared")
      entry.item:set_quantity(change.quantity)
    end
  end
  return true
end

function Inventory:merge_resource_stacks(primary_id, secondary_id)
  local primary, secondary = self:get(primary_id), self:get(secondary_id)
  if not primary or not secondary then return nil, "Resource stack is unavailable" end
  local first, second = primary.item, secondary.item
  if first.item_type ~= "resource_stack" or second.item_type ~= "resource_stack" or first.resource_id ~= second.resource_id then
    return nil, "Only matching resource stacks can merge"
  end
  local moved = math.min(first.max_stack - first.quantity, second.quantity)
  if moved <= 0 then return nil, "Resource stack is already full" end
  first:set_quantity(first.quantity + moved)
  if second.quantity == moved then self:remove(secondary_id) else second:set_quantity(second.quantity - moved) end
  return { applied = true, moved = moved, primary_id = primary_id, secondary_id = secondary_id }
end

function Inventory:split_resource_stack(physical_id, quantity, new_item, x, y, rotated)
  local entry = self:get(physical_id)
  if not entry or entry.item.item_type ~= "resource_stack" then return nil, "Resource stack is unavailable" end
  if not new_item or new_item.item_type ~= "resource_stack" or new_item.resource_id ~= entry.item.resource_id then
    return nil, "Split stack does not match source resource"
  end
  if type(quantity) ~= "number" or quantity < 1 or quantity >= entry.item.quantity or quantity % 1 ~= 0
    or new_item.quantity ~= quantity then return nil, "Resource split quantity is invalid" end
  local allowed, reason = self:can_place(new_item, x, y, rotated)
  if not allowed then return nil, reason end
  entry.item:set_quantity(entry.item.quantity - quantity)
  local placed, placement_reason = self:place(new_item, x, y, rotated)
  if not placed then
    entry.item:set_quantity(entry.item.quantity + quantity)
    return nil, placement_reason
  end
  return { applied = true, source_id = physical_id, split_id = new_item.physical_id, quantity = quantity }
end

function Inventory:transfer_to(destination, physical_id, placement)
  assert(destination and destination.auto_place, "Inventory transfer requires a destination inventory")
  local entry = self:get(physical_id)
  if not entry then return nil, "Physical item is not in this inventory" end
  local destination_placement = placement or destination:find_first_fit(entry.item)
  if not destination_placement then return nil, "No space in destination inventory" end
  local allowed, reason = destination:can_place(entry.item, destination_placement.x, destination_placement.y, destination_placement.rotated)
  if not allowed then return nil, reason end
  local item, original = self:remove(physical_id)
  local placed, place_reason = destination:place(item, destination_placement.x, destination_placement.y, destination_placement.rotated)
  if not placed then
    local restored, restore_reason = self:place(item, original.x, original.y, original.rotated)
    assert(restored, restore_reason)
    return nil, place_reason
  end
  return { applied = true, physical_id = physical_id, item = item }
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
    for _, cell in ipairs(footprint_cells(entry.item, entry.rotated)) do
      local key = cell_key(entry.x + cell.x, entry.y + cell.y)
      assert(not expected_cells[key], "Inventory entries overlap at " .. key)
      expected_cells[key] = entry
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
