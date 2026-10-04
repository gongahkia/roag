-- A deterministic, presentation-facing spatial projection of a corpse.  A
-- corpse continues to own its Body and optional carried Inventory; this grid
-- never becomes another owner of those physical items.  It simply lets the
-- salvage UI use the same placement geometry as ordinary cargo.
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")

local CorpseLootGrid = {}
CorpseLootGrid.__index = CorpseLootGrid

-- A Fallen body can contain a full 7x7 carried pack plus its installed,
-- irregular parts.  Fourteen cells square keeps every current body and boss
-- readable on one board without an anatomy-shaped secondary UI or paging.
CorpseLootGrid.WIDTH = 14
CorpseLootGrid.HEIGHT = 14

local function stable_sources(corpse, registry)
  local sources = {}
  for order, installed in ipairs(corpse:list_components()) do
    local component = installed.component
    sources[#sources + 1] = {
      source_kind = "body",
      slot_id = installed.slot_id,
      physical_id = component.id,
      item = PhysicalItem.from_component(component, registry),
      order = order,
    }
  end
  local carried = {}
  for _, entry in ipairs(corpse:list_carried_items()) do carried[#carried + 1] = entry end
  table.sort(carried, function(left, right)
    if left.y ~= right.y then return left.y < right.y end
    if left.x ~= right.x then return left.x < right.x end
    return left.physical_id < right.physical_id
  end)
  for order, entry in ipairs(carried) do
    sources[#sources + 1] = {
      source_kind = "cargo",
      physical_id = entry.physical_id,
      item = entry.item,
      order = order,
    }
  end
  -- Installed parts use the Body's stable slot order before carried cargo.
  -- This preserves a familiar projection across repeated visits while making
  -- no anatomical claim about the visual grid itself.
  table.sort(sources, function(left, right)
    if left.source_kind ~= right.source_kind then return left.source_kind == "body" end
    if left.order ~= right.order then return left.order < right.order end
    return left.physical_id < right.physical_id
  end)
  return sources
end

function CorpseLootGrid.project(corpse, registry)
  assert(corpse and corpse.id, "Corpse loot projection requires a corpse")
  local inventory = Inventory.new({ width = CorpseLootGrid.WIDTH, height = CorpseLootGrid.HEIGHT })
  local sources_by_id = {}
  for _, source in ipairs(stable_sources(corpse, registry)) do
    local placement, reason = inventory:find_first_fit(source.item)
    assert(placement, "Corpse loot grid overflow for " .. source.physical_id .. ": " .. tostring(reason))
    local entry, place_reason = inventory:place(source.item, placement.x, placement.y, placement.rotated)
    assert(entry, place_reason)
    sources_by_id[source.physical_id] = source
  end
  return setmetatable({
    corpse_id = corpse.id,
    inventory = inventory,
    sources_by_id = sources_by_id,
  }, CorpseLootGrid)
end

function CorpseLootGrid:source(physical_id)
  return self.sources_by_id[physical_id]
end

return CorpseLootGrid
