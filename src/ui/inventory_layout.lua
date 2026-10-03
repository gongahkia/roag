-- Shared inventory geometry keeps rendering and pointer hit-testing in exact
-- agreement.  It intentionally knows nothing about LÖVE or game state.
local InventoryLayout = {}

function InventoryLayout.for_viewport(inventory, viewport_width, viewport_height)
  assert(inventory and inventory.width and inventory.height, "Inventory layout requires an inventory")
  local usable_width = math.max(1, viewport_width - 48)
  local usable_height = math.max(1, viewport_height - 150)
  local cell = math.max(1, math.min(72,
    math.floor(usable_width / inventory.width),
    math.floor(usable_height / inventory.height)))
  local width, height = inventory.width * cell, inventory.height * cell
  return {
    cell = cell,
    width = width,
    height = height,
    grid_x = math.floor((viewport_width - width) / 2),
    grid_y = math.floor((viewport_height - height) / 2),
    columns = inventory.width,
    rows = inventory.height,
  }
end

-- `outside` is useful while dragging: it maps a pointer just beyond the
-- board to a tentative anchor so an invalid drop is visible immediately.
function InventoryLayout.cell_at(layout, x, y, outside)
  local cell_x = math.floor((x - layout.grid_x) / layout.cell) + 1
  local cell_y = math.floor((y - layout.grid_y) / layout.cell) + 1
  if not outside and (cell_x < 1 or cell_y < 1 or cell_x > layout.columns or cell_y > layout.rows) then
    return nil
  end
  return cell_x, cell_y
end

return InventoryLayout
