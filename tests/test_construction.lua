local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local Building = require("src.construction.building")
local PhysicalItem = require("src.inventory.physical_item")
local Inventory = require("src.inventory.inventory")
local ZoneKey = require("src.campaign.zone_key")
local Grid = require("src.world.grid")

local function campaign(seed)
  local value = Campaign.new({ seed = seed, campaign_id = "campaign:" .. seed })
  local directory = SaveStore.memory_directory()
  value:set_persistence_directory(directory)
  assert(CampaignPersistence.save(value, directory))
  return value, directory
end

local function add_resource(session, resource_id, amount)
  local maximum = session.registry:get_resource(resource_id).max_stack
  while amount > 0 do
    local quantity = math.min(amount, maximum)
    local item = session:create_resource_stack(resource_id, quantity, "campaign")
    assert(session.state.inventory:auto_place(item))
    amount = amount - quantity
  end
end

local function build_cell(session, recipe_id)
  local player = session.state.player
  for _, point in ipairs(Grid.neighbours({ x = player.x, y = player.y })) do
    local valid = Building.validate(session, recipe_id, point.x, point.y)
    if valid.applied then return point end
  end
  error("No legal adjacent construction cell")
end

local function free_adjacent(session)
  local player, world = session.state.player, session.state.world
  for _, point in ipairs(Grid.neighbours({ x = player.x, y = player.y })) do
    if world:is_passable(point.x, point.y) and not world:object_at(point.x, point.y)
      and not session:_actor_at(point.x, point.y) then return point end
  end
  error("No free adjacent cell")
end

local function build(session, recipe_id)
  local point
  local ok, candidate = pcall(build_cell, session, recipe_id)
  if ok then point = candidate else
    for _, location in ipairs(session:_reachable_floor_cells()) do
      session.state.player.x, session.state.player.y = location.x, location.y
      local found, alternative = pcall(build_cell, session, recipe_id)
      if found then point = alternative; break end
    end
  end
  assert(point, "No legal construction cell")
  local result = Building.place(session, recipe_id, point.x, point.y)
  assert(result.applied, result.reason)
  return assert(session.state.world:get_object(result.object_id)), point
end

local function descend(campaign_value)
  local connection = assert(campaign_value.active_zone.connections.down)
  local object = assert(campaign_value.session.state.world:object_at(connection.cell.x, connection.cell.y))
  campaign_value.session.state.player.x, campaign_value.session.state.player.y = object.x + 1, object.y
  assert(campaign_value.session:turn("interact") == "zone_transition")
end

return {
  {
    name = "resource stacks are physical spatial inventory items with deterministic merge split and data round trip",
    run = function()
      local registry = require("src.content.registry").load()
      local inventory = Inventory.new()
      local one = PhysicalItem.from_resource("resource.material.timber", 5, "item:1", registry)
      local two = PhysicalItem.from_resource("resource.material.timber", 4, "item:2", registry)
      assert(inventory:place(one, 1, 1, false))
      assert(inventory:place(two, 2, 1, false))
      assert(inventory:merge_resource_stacks("item:1", "item:2").moved == 4)
      local split = PhysicalItem.from_resource("resource.material.timber", 3, "item:3", registry)
      assert(inventory:split_resource_stack("item:1", 3, split, 2, 1, false).applied)
      local restored = Inventory.from_data(inventory:to_data(), function(data) return PhysicalItem.from_data(data, registry) end)
      assert(restored:resource_quantity("resource.material.timber") == 9 and restored:get("item:3"))
    end,
  },
  {
    name = "explicit harvest sources create persistent ground resource stacks while nonharvestable destruction yields nothing",
    run = function()
      local value, directory = campaign(905001)
      local session, world = value.session, value.session.state.world
      local point = free_adjacent(session)
      local crate = assert(world:place_object("world_object.cover.timber_crate", point.x, point.y))
      local destroyed = session:damage_world_object(crate, { amount = crate.current_integrity, cause = "kinetic" })
      assert(destroyed.destroyed and #destroyed.harvest_drops == 1)
      local ground = destroyed.harvest_drops[1]
      assert(ground.item.resource_id == "resource.material.timber" and session:pickup_ground_item(ground.id).applied)
      local quiet_point = free_adjacent(session)
      assert(world:register_circuit("circuit:test", { enabled = true }).applied)
      local generator = assert(world:place_object("world_object.power.generator_legacy", quiet_point.x, quiet_point.y, { circuit_id = "circuit:test" }))
      local no_yield = session:damage_world_object(generator, { amount = generator.current_integrity, cause = "kinetic" })
      assert(no_yield.destroyed and #no_yield.harvest_drops == 0)
      assert(CampaignPersistence.save(value, directory))
      local restored = assert(CampaignPersistence.load(directory))
      assert(restored.session.state.inventory:resource_quantity("resource.material.timber") >= 2)
    end,
  },
  {
    name = "constructed walls use ordinary movement vision projectile gas and fire material rules and protected throats reject placement",
    run = function()
      local value = campaign(905002)
      local session, world = value.session, value.session.state.world
      add_resource(session, "resource.material.timber", 4)
      local wall, point = build(session, "construction.timber_wall")
      assert(not world:is_passable(point.x, point.y) and world:blocks_vision(point.x, point.y)
        and world:blocks_projectile(point.x, point.y) and not world:allows_gas_at(point.x, point.y))
      assert(session:ignite_world_object(wall, { source = "test" }).applied)
      local reserved
      for cell in pairs(session.state.surface_connector_cells) do reserved = cell; break end
      local x, y = reserved:match("(%d+):(%d+)")
      x, y = tonumber(x), tonumber(y)
      for _, neighbour in ipairs(Grid.neighbours({ x = x, y = y })) do
        if world:is_passable(neighbour.x, neighbour.y) then
          session.state.player.x, session.state.player.y = neighbour.x, neighbour.y
          break
        end
      end
      local rejected = Building.validate(session, "construction.timber_wall", x, y)
      assert(not rejected.applied and rejected.code == "blocks_travel_connection")
    end,
  },
  {
    name = "manual constructed door uses ordinary door state without requiring a circuit",
    run = function()
      local value = campaign(905003)
      local session, world = value.session, value.session.state.world
      add_resource(session, "resource.material.metal", 2); add_resource(session, "resource.material.timber", 1)
      local door = build(session, "construction.door")
      assert(door.interaction_role == "door" and door.circuit_id == nil)
      assert(world:set_door_state(door, "open").applied and world:is_passable(door.x, door.y))
      assert(world:set_door_state(door, "closed").applied and not world:is_passable(door.x, door.y))
    end,
  },
  {
    name = "storage owns components and resources exclusively through persistence and drops contents on destruction",
    run = function()
      local value, directory = campaign(905004)
      local session, world = value.session, value.session.state.world
      add_resource(session, "resource.material.timber", 4); add_resource(session, "resource.material.metal", 3)
      local storage = build(session, "construction.storage_crate")
      local resource_entry
      for _, entry in ipairs(session.state.inventory.entries) do
        if entry.item.item_type == "resource_stack" then resource_entry = entry; break end
      end
      assert(session:storage_transfer(storage.id, resource_entry.physical_id, "to_storage").applied)
      assert(storage.storage_inventory:get(resource_entry.physical_id) and not session.state.inventory:get(resource_entry.physical_id))
      assert(CampaignPersistence.save(value, directory))
      value = assert(CampaignPersistence.load(directory))
      storage = assert(value.session.state.world:get_object(storage.id))
      assert(storage.storage_inventory:get(resource_entry.physical_id))
      local destroyed = value.session:damage_world_object(storage, { amount = storage.current_integrity, cause = "kinetic" })
      assert(destroyed.destroyed and #destroyed.storage_drops == 1)
      assert(value.session.state.world:get_ground_item(resource_entry.physical_id))
    end,
  },
  {
    name = "built generator and breaker join a persistent local construction circuit",
    run = function()
      local value = campaign(905005)
      local session, world = value.session, value.session.state.world
      add_resource(session, "resource.material.metal", 6)
      local generator = build(session, "construction.generator")
      local breaker = build(session, "construction.breaker")
      assert(generator.circuit_id == breaker.circuit_id and world:is_circuit_powered(generator.circuit_id))
      assert(world:set_circuit_enabled(generator.circuit_id, false).applied and not world:is_circuit_powered(generator.circuit_id))
    end,
  },
  {
    name = "carried resources transfer to player corpses while stored resources survive death and player-built anchors control succession",
    run = function()
      local value, directory = campaign(905006)
      descend(value)
      local session = value.session
      add_resource(session, "resource.material.metal", 10); add_resource(session, "resource.material.masonry", 4); add_resource(session, "resource.material.timber", 3)
      local storage = build(session, "construction.storage_crate")
      local held
      for _, entry in ipairs(session.state.inventory.entries) do
        if entry.item.resource_id == "resource.material.timber" then held = entry; break end
      end
      assert(session:storage_transfer(storage.id, held.physical_id, "to_storage").applied)
      local station = build(session, "construction.reconstruction_station")
      session.state.player.x, session.state.player.y = station.x + 1, station.y
      assert(session:set_reconstruction_anchor(station).applied)
      local carried = session.state.inventory.entries[1]
      assert(carried and carried.item.item_type == "resource_stack")
      local death, failure = value:handle_player_death({ cause = "construction_test" })
      assert(death, failure and failure.reason)
      assert(ZoneKey.equal(value.state.current_zone, ZoneKey.new(0, 0, -1)))
      local corpse = value.session.state.corpses[#value.session.state.corpses]
      assert(corpse.carried_inventory:get(carried.physical_id) and storage.storage_inventory:get(held.physical_id))
      value.session.state.player.x, value.session.state.player.y = corpse.x + 1, corpse.y
      assert(value.session:salvage_corpse_carried_item(corpse.id, carried.physical_id).applied)
      assert(CampaignPersistence.save(value, directory))
      assert(CampaignPersistence.load(directory):validate())
    end,
  },
}
