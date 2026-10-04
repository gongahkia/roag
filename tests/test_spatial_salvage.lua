local App = require("src.app.app")
local Campaign = require("src.campaign.campaign")
local CorpseLootGrid = require("src.inventory.corpse_loot_grid")
local Inventory = require("src.inventory.inventory")
local PhysicalItem = require("src.inventory.physical_item")
local SaveStore = require("src.persistence.save_store")
local Session = require("src.simulation.session")
local Grid = require("src.world.grid")

local function campaign(seed)
  local value = Campaign.new({ seed = seed, campaign_id = "campaign:" .. seed })
  value.session.state.enemies = {}
  return value
end

local function clear_pair(session)
  local world = session.state.world
  for x = 2, Grid.width - 3 do
    for y = 2, Grid.height - 3 do
      if world:is_passable(x, y) and world:is_passable(x + 1, y)
        and not world:object_at(x, y) and not world:object_at(x + 1, y)
        and not world:is_hazardous(x, y) and not world:is_hazardous(x + 1, y) then
        return { x = x, y = y }
      end
    end
  end
  error("No clear pair")
end

local function legacy_corpse(seed)
  local session = Session.new({ seed = seed })
  session:start_run()
  local player = session.state.player
  local enemy = session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
  local corpse = assert(session:_create_corpse(enemy))
  return session, corpse
end

return {
  {
    name = "corpse loot grids deterministically project body parts and carried cargo without changing owners",
    run = function()
      local session, corpse = legacy_corpse(940001)
      local cargo = session:create_resource_stack("resource.material.timber", 3, "test")
      corpse.carried_inventory = Inventory.new()
      assert(corpse.carried_inventory:place(cargo, 1, 1))
      local first, second = CorpseLootGrid.project(corpse, session.registry), CorpseLootGrid.project(corpse, session.registry)
      assert(first.inventory.width == 14 and first.inventory.height == 14)
      assert(#first.inventory.entries == #corpse:list_components() + 1)
      for _, entry in ipairs(first.inventory.entries) do
        local repeat_entry = assert(second.inventory:get(entry.physical_id))
        assert(entry.x == repeat_entry.x and entry.y == repeat_entry.y and entry.rotated == repeat_entry.rotated)
      end
      assert(corpse.carried_inventory:get(cargo.physical_id) and session:validate_physical_ownership())
    end,
  },
  {
    name = "spatial corpse salvage validates before detaching and preserves exact component identity",
    run = function()
      local session, corpse = legacy_corpse(940002)
      local grid = assert(session:corpse_loot_grid(corpse.id))
      local entry = assert(grid.inventory.entries[1])
      local component = corpse.body:get_component(grid:source(entry.physical_id).slot_id)
      session.state.inventory = Inventory.new({ width = 2, height = 2 })
      session.state.run.inventory = session.state.inventory
      local invalid = session:salvage_corpse_to_inventory(corpse.id, entry.physical_id, { x = 3, y = 1, rotated = false })
      assert(not invalid.applied and corpse.body:get_component(grid:source(entry.physical_id).slot_id) == component)
      local transferred = assert(session:salvage_corpse_to_inventory(corpse.id, entry.physical_id, { x = 1, y = 1, rotated = false }))
      assert(transferred.physical_id == component.id and not corpse.body:get_component(grid:source(entry.physical_id).slot_id))
      assert(session.state.inventory:get(component.id).item.object == component and session:validate_physical_ownership())
    end,
  },
  {
    name = "Campaign corpse mouse transfer uses the dual grid and remains a paused inventory action",
    run = function()
      local slots = { SaveStore.memory_directory(), SaveStore.memory_directory(), SaveStore.memory_directory() }
      local app = App.new({ seed = 940003, campaign_slot_stores = slots, meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
      assert(app:request_new_campaign())
      local session, player = app.session, app.session.state.player
      session.state.enemies = {}
      local enemy = session:_make_enemy("bomber", { x = player.x + 1, y = player.y })
      local corpse = assert(session:_create_corpse(enemy))
      assert(app:open_salvage(corpse.id))
      local projection, source = app:salvage_grid(), nil
      for _, entry in ipairs(projection.inventory.entries) do
        if entry.item.footprint.width == 1 and entry.item.footprint.height == 1 then source = entry; break end
      end
      source = source or projection.inventory.entries[1]
      local target = assert(session.state.inventory:find_first_fit(source.item))
      local layout = assert(app:salvage_layout(1280, 820))
      local from_x = layout.corpse.grid_x + (source.x - 0.5) * layout.cell
      local from_y = layout.corpse.grid_y + (source.y - 0.5) * layout.cell
      local to_x = layout.player.grid_x + (target.x - 0.5) * layout.cell
      local to_y = layout.player.grid_y + (target.y - 0.5) * layout.cell
      local dash = player.dash
      assert(app:mousepressed(from_x, from_y, 1, 1280, 820))
      assert(app:mousemoved(to_x, to_y, 0, 0, 1280, 820).valid)
      assert(app:mousereleased(to_x, to_y, 1, 1280, 820).applied)
      assert(session.state.inventory:get(source.physical_id) and player.dash == dash and app.screen == "salvage")
    end,
  },
  {
    name = "player corpse projections include installed parts and every carried physical stack exactly once",
    run = function()
      local value = campaign(940008)
      local session, player = value.session, value.session.state.player
      local cargo = session:create_resource_stack("resource.ammo.bullets", 6, "test")
      assert(session.state.inventory:auto_place(cargo))
      local carried = session.state.inventory
      local corpse = assert(session:_create_corpse(player, carried, { death_cause = "test" }))
      local replacement = Inventory.new()
      session.state.inventory, session.state.run.inventory = replacement, replacement
      local grid = assert(session:corpse_loot_grid(corpse.id))
      assert(grid.inventory:get(cargo.physical_id) and grid:source(cargo.physical_id).source_kind == "cargo")
      for _, installed in ipairs(corpse:list_components()) do
        assert(grid.inventory:get(installed.component.id) and grid:source(installed.component.id).source_kind == "body")
      end
      local placement = assert(replacement:find_first_fit(cargo))
      assert(session:salvage_corpse_to_inventory(corpse.id, cargo.physical_id, placement).applied)
      assert(replacement:get(cargo.physical_id) and not corpse.carried_inventory:get(cargo.physical_id))
    end,
  },
  {
    name = "large irregular boss-grade parts use the ordinary corpse grid without a special anatomy path",
    run = function()
      local session = Session.new({ seed = 940009 })
      session:start_run()
      local player = session.state.player
      local enemy = session:_make_enemy("enemy.legacy.cultist", { x = player.x, y = player.y + 1 })
      local corpse = assert(session:_create_corpse(enemy))
      local original = assert(corpse.body:detach("right_arm"))
      corpse.carried_inventory = Inventory.new()
      assert(corpse.carried_inventory:auto_place(PhysicalItem.from_component(original, session.registry)))
      local large = session.component_factory:create("component.arm.reactor_arc_blade", "test")
      local installed, reason = corpse.body:install("right_arm", large)
      assert(installed, reason)
      local grid = CorpseLootGrid.project(corpse, session.registry)
      local entry = assert(grid.inventory:get(large.id))
      assert(entry.item.footprint.width > 1 or entry.item.footprint.height > 1)
      assert(grid:source(large.id).source_kind == "body")
    end,
  },
  {
    name = "rotated corpse components transfer only when the chosen player-grid orientation fits",
    run = function()
      local session = Session.new({ seed = 940010 })
      session:start_run()
      local player = session.state.player
      local enemy = session:_make_enemy("enemy.legacy.cultist", { x = player.x, y = player.y + 1 })
      local corpse = assert(session:_create_corpse(enemy))
      local grid = CorpseLootGrid.project(corpse, session.registry)
      local entry
      for _, candidate in ipairs(grid.inventory.entries) do
        if candidate.item.footprint.rotatable and candidate.item.footprint.height > candidate.item.footprint.width then entry = candidate; break end
      end
      assert(entry)
      local inventory = Inventory.new({ width = entry.item.footprint.height, height = entry.item.footprint.width })
      session.state.inventory, session.state.run.inventory = inventory, inventory
      assert(not session:salvage_corpse_to_inventory(corpse.id, entry.physical_id, { x = 1, y = 1, rotated = false }).applied)
      assert(session:salvage_corpse_to_inventory(corpse.id, entry.physical_id, { x = 1, y = 1, rotated = true }).applied)
      assert(inventory:get(entry.physical_id).rotated)
    end,
  },
  {
    name = "Campaign walk-over supplies transfer atomically without another player turn",
    run = function()
      local value = campaign(940004)
      local session, point = value.session, clear_pair(value.session)
      local player, world = session.state.player, session.state.world
      player.x, player.y, player.direction = point.x, point.y, "d"
      local stack = session:create_resource_stack("resource.material.timber", 4, "test")
      assert(world:place_ground_item(stack, point.x + 1, point.y))
      assert(session:turn("d") == nil and player.x == point.x + 1)
      assert(not world:get_ground_item(stack.physical_id))
      assert(session.state.inventory:resource_quantity("resource.material.timber") >= 4)
    end,
  },
  {
    name = "walk-over pickup leaves a whole stack on the ground when full cargo cannot fit",
    run = function()
      local value = campaign(940005)
      local session, point = value.session, clear_pair(value.session)
      local player, world = session.state.player, session.state.world
      local inventory = Inventory.new({ width = 1, height = 1 })
      local blocker = session:create_resource_stack("resource.material.timber", 16, "test")
      assert(inventory:place(blocker, 1, 1))
      session.state.inventory, session.state.run.inventory = inventory, inventory
      player.x, player.y, player.direction = point.x, point.y, "d"
      local stack = session:create_resource_stack("resource.material.metal", 10, "test")
      assert(world:place_ground_item(stack, point.x + 1, point.y))
      assert(session:turn("d") == nil and player.x == point.x + 1)
      local ground = assert(world:get_ground_item(stack.physical_id))
      assert(ground.item.quantity == 10 and inventory:resource_quantity("resource.material.metal") == 0)
    end,
  },
  {
    name = "inventory drops preserve physical identity and wait for a new movement arrival before recollection",
    run = function()
      local value = campaign(940006)
      local session, point = value.session, clear_pair(value.session)
      local player, world = session.state.player, session.state.world
      player.x, player.y, player.direction = point.x, point.y, "d"
      local stack = session:create_resource_stack("resource.ammo.shells", 4, "test")
      assert(session.state.inventory:auto_place(stack))
      assert(session:drop_inventory_item(stack.physical_id).applied)
      assert(world:get_ground_item(stack.physical_id) and not session.state.inventory:get(stack.physical_id))
      -- No update-time pickup loop: the cargo remains under the player until
      -- they leave and make a later successful arrival.
      assert(world:get_ground_item(stack.physical_id))
      assert(session:turn("d") == nil)
      player.direction = "a"
      assert(session:turn("a") == nil)
      assert(session.state.inventory:get(stack.physical_id) and not world:get_ground_item(stack.physical_id))
    end,
  },
  {
    name = "loose components remain deliberate faced pickups rather than walk-over supplies",
    run = function()
      local value = campaign(940007)
      local session, point = value.session, clear_pair(value.session)
      local player, world = session.state.player, session.state.world
      player.x, player.y, player.direction = point.x, point.y, "d"
      local component = session.component_factory:create("component.internal.legacy_support", "test")
      local item = PhysicalItem.from_component(component, session.registry)
      assert(world:place_ground_item(item, point.x + 1, point.y))
      assert(session:turn("d") == nil and world:get_ground_item(item.physical_id) and not session.state.inventory:get(item.physical_id))
      assert(session:turn("a") == nil)
      player.direction = "d"
      assert(session:turn("interact") == nil and session.state.inventory:get(item.physical_id))
    end,
  },
}
