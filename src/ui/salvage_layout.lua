-- Geometry shared by the dual-grid salvage renderer and its pointer input.
-- Keeping it small and pure prevents display scaling from changing where an
-- authoritative transfer is attempted.
local SalvageLayout = {}

function SalvageLayout.for_viewport(corpse_inventory, player_inventory, viewport_width, viewport_height)
  assert(corpse_inventory and player_inventory, "Salvage layout requires both inventories")
  local gap, horizontal_padding, vertical_padding = 48, 48, 172
  local columns = corpse_inventory.width + player_inventory.width
  local cell = math.max(14, math.min(42,
    math.floor((viewport_width - horizontal_padding - gap) / columns),
    math.floor((viewport_height - vertical_padding) / math.max(corpse_inventory.height, player_inventory.height))))
  local corpse_width, player_width = corpse_inventory.width * cell, player_inventory.width * cell
  local total = corpse_width + gap + player_width
  local left_x = math.floor((viewport_width - total) / 2)
  local top_y = math.max(94, math.floor((viewport_height - math.max(corpse_inventory.height, player_inventory.height) * cell) / 2) + 18)
  return {
    cell = cell,
    corpse = { grid_x = left_x, grid_y = top_y, width = corpse_width, height = corpse_inventory.height * cell,
      columns = corpse_inventory.width, rows = corpse_inventory.height },
    player = { grid_x = left_x + corpse_width + gap, grid_y = top_y, width = player_width, height = player_inventory.height * cell,
      columns = player_inventory.width, rows = player_inventory.height },
  }
end

function SalvageLayout.cell_at(grid, cell, x, y, outside)
  local cell_x = math.floor((x - grid.grid_x) / cell) + 1
  local cell_y = math.floor((y - grid.grid_y) / cell) + 1
  if not outside and (cell_x < 1 or cell_y < 1 or cell_x > grid.columns or cell_y > grid.rows) then return nil end
  return cell_x, cell_y
end

return SalvageLayout
