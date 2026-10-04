local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local Economy = require("src.simulation.economy")
local Grid = require("src.world.grid")
local PhysicalItem = require("src.inventory.physical_item")
local Renderer = require("src.rendering.renderer")
local Tool = require("src.inventory.tool")
local SaveStore = require("src.persistence.save_store")

local function new_campaign(seed, events)
  local value = Campaign.new({
    seed = seed,
    campaign_id = "campaign:" .. seed,
    emit = function(event) events[#events + 1] = event end,
  })
  value.session.state.enemies = {}
  return value
end

local function clear_pair(session)
  local world = session.state.world
  for x = 1, Grid.width - 3 do
    for y = 1, Grid.height - 2 do
      if world:is_passable(x, y) and world:is_passable(x + 1, y)
        and not world:object_at(x, y) and not world:object_at(x + 1, y)
        and not world:is_hazardous(x, y) and not world:is_hazardous(x + 1, y)
        and not (session.state.surface_connector_cells or {})[Grid.key(x, y)]
        and not (session.state.surface_connector_cells or {})[Grid.key(x + 1, y)] then
        return x, y
      end
    end
  end
  error("No clear adjacent fixture cells")
end

local function carried_tool(session, definition_id)
  for _, entry in ipairs(session.state.inventory.entries) do
    if entry.item.item_type == "tool" and entry.item.tool_definition_id == definition_id then return entry.item end
  end
  local item = session:create_tool(definition_id, "test")
  assert(session.state.inventory:auto_place(item))
  return item
end

local function equip_tool(session, item)
  assert(session:assign_campaign_loadout("weapon", 1, {
    source_kind = "tool", physical_id = item.physical_id, ability_id = "tool.attack",
    attack_id = "tool.attack", tool_definition_id = item.tool_definition_id,
  }).applied)
  session:campaign_loadout().active_weapon = 1
end

local function face_fixture(session)
  local x, y = clear_pair(session)
  session.state.player.x, session.state.player.y, session.state.player.direction = x, y, "d"
  return x + 1, y
end

return {
  {
    name = "physical tools keep exact identity, durability, drop state, and loadout resolution",
    run = function()
      local events = {}
      local campaign = new_campaign(940001, events)
      local session = campaign.session
      local axe = carried_tool(session, "tool.axe")
      assert(axe.object.current_durability == axe.object.maximum_durability)
      equip_tool(session, axe)
      assert(session:attack_active_weapon().committed)
      assert(session:drop_inventory_item(axe.physical_id).applied)
      local resolved, failure = require("src.simulation.loadout").resolve(session, session.state.player,
        session:quick_slot("weapon", 1), "weapon")
      assert(not resolved and failure.code == "provider_missing")
      assert(session:pickup_ground_item(axe.physical_id).applied)
      assert(require("src.simulation.loadout").resolve(session, session.state.player, session:quick_slot("weapon", 1), "weapon"))
      local storage_x, storage_y = face_fixture(session)
      local storage = assert(session.state.world:place_object("world_object.build.storage_crate", storage_x, storage_y))
      assert(session:storage_transfer(storage.id, axe.physical_id, "to_storage").applied)
      assert(storage.storage_inventory:get(axe.physical_id) and not session.state.inventory:get(axe.physical_id))
      assert(session:storage_transfer(storage.id, axe.physical_id, "to_player").applied)
      local restored = PhysicalItem.from_data(axe:to_data(), session.registry)
      assert(restored.physical_id == axe.physical_id and restored.object.current_durability == axe.object.current_durability)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "production tools have valid physical profiles and persistent supply acquisition paths",
    run = function()
      local events = {}
      local campaign = new_campaign(9400011, events)
      local session = campaign.session
      for _, id in ipairs({ "tool.axe", "tool.pickaxe", "tool.cutter", "tool.drill" }) do
        local definition = session.registry:get_tool(id)
        assert(definition.family and definition.max_durability > 0 and definition.inventory.width >= 1)
        local item = carried_tool(session, id)
        assert(item.mass == definition.mass and item.object.maximum_durability == definition.max_durability)
        -- Conventional tools remain emergency weapons: none may dominate the
        -- dedicated Impact Maul in both direct damage and forced displacement.
        local maul = session.registry.abilities["ability.weapon.melee.impact_maul"]
        assert(definition.combat.damage < maul.damage or definition.combat.force < maul.force)
      end
      local features = require("content.terrain_features.legacy")
      local function contains(profile, definition_id)
        for _, entry in ipairs(profile) do if entry.definition_id == definition_id then return true end end
        return false
      end
      assert(contains(features.forest, "world_object.feature.old_growth_tree"))
      assert(contains(features.cave, "world_object.feature.stalagmite"))
      assert(contains(features.dungeon, "world_object.feature.rubble_pile"))
      assert(contains(features.reactor, "world_object.feature.machine_bank"))
      assert(contains(features.reactor, "world_object.feature.reinforced_access_panel"))
      local stock = Economy.create_stock(session, "service.supply.legacy", session.rng:derive("tool.stock"))
      local seen = {}
      for _, offer in ipairs(stock.offers) do if offer.tool_definition_id then seen[offer.tool_definition_id] = true end end
      assert(seen["tool.pickaxe"] and seen["tool.cutter"] and seen["tool.drill"])
    end,
  },
  {
    name = "tool weapon routing damages actors before terrain and consumes durability",
    run = function()
      local events = {}
      local campaign = new_campaign(940002, events)
      local session, player = campaign.session, campaign.session.state.player
      local axe = carried_tool(session, "tool.axe")
      equip_tool(session, axe)
      local x, y = face_fixture(session)
      local enemy = session.state.enemies[1] or session:_make_enemy("enemy.legacy.cultist", { x = x, y = y }, {})
      enemy.x, enemy.y = x, y
      session.state.enemies = { enemy }
      local health, durability = enemy.health, axe.object.current_durability
      assert(session:turn("attack") == nil)
      assert(enemy.health < health and axe.object.current_durability == durability - 1)
      assert(session.state.world:object_at(x, y) == nil)
    end,
  },
  {
    name = "Axe harvests wood through persistent integrity and emits one physical yield event",
    run = function()
      local events = {}
      local campaign = new_campaign(940003, events)
      local session = campaign.session
      local axe = carried_tool(session, "tool.axe")
      equip_tool(session, axe)
      local x, y = face_fixture(session)
      local tree = assert(session.state.world:place_object("world_object.feature.old_growth_tree", x, y))
      local maximum = tree.current_integrity
      assert(session:turn("attack") == nil and tree.current_integrity < maximum and not tree.destroyed)
      assert(session:turn("attack") == nil and not tree.destroyed)
      assert(session:turn("attack") == nil and tree.destroyed)
      assert(#session.state.world:ground_items_at(x, y) == 1)
      local impacts = 0
      for _, event in ipairs(events) do if event.type == "tool_impact" then impacts = impacts + 1 end end
      assert(impacts == 3 and axe.object.current_durability == axe.object.maximum_durability - 3)
      local manifest, shard = campaign:to_manifest_data(), campaign:to_zone_data()
      local restored = Campaign.from_data(manifest, shard)
      assert(restored.session.state.world:get_object(tree.id).destroyed)
    end,
  },
  {
    name = "tool family policies distinguish masonry metal reinforced and protected infrastructure",
    run = function()
      local events = {}
      local campaign = new_campaign(940004, events)
      local session = campaign.session
      local x, y = face_fixture(session)
      local world = session.state.world
      local axe = carried_tool(session, "tool.axe")
      equip_tool(session, axe)
      local stone = world.cells[Grid.key(x, y)]
      stone.material_id, stone.current_integrity, stone.destroyed = "material.terrain.stone", 4, false
      assert(session:turn("attack") == nil and stone.current_integrity == 4)
      local pickaxe = carried_tool(session, "tool.pickaxe")
      equip_tool(session, pickaxe)
      assert(session:turn("attack") == nil and stone.current_integrity == 3)
      local metal_x, metal_y = face_fixture(session)
      local machine = assert(world:place_object("world_object.feature.machine_bank", metal_x, metal_y))
      local cutter = carried_tool(session, "tool.cutter")
      equip_tool(session, cutter)
      assert(session:turn("attack") == nil and machine.current_integrity < session.registry:get_material(machine.material_id).max_integrity)
      local reinforced_x, reinforced_y = face_fixture(session)
      local panel = assert(world:place_object("world_object.feature.reinforced_access_panel", reinforced_x, reinforced_y))
      local drill = carried_tool(session, "tool.drill")
      equip_tool(session, drill)
      assert(session:turn("attack") == nil and panel.current_integrity < session.registry:get_material(panel.material_id).max_integrity)
      stone.material_id, stone.current_integrity, stone.destroyed = "material.structure.reinforced", nil, false
      session.state.player.x, session.state.player.y, session.state.player.direction = x - 1, y, "d"
      assert(session:turn("attack") == nil and stone.material_id == "material.structure.reinforced")
      -- Traversal objects deliberately retain research-gate protection even
      -- when their material is otherwise drill-compatible.
      stone.material_id, stone.current_integrity, stone.destroyed = "material.terrain.air", nil, false
      local barrier = assert(world:place_object("world_object.traversal.reinforced_barrier", x, y))
      assert(session:turn("attack") == nil and barrier.current_integrity
        == session.registry:get_material(barrier.material_id).max_integrity)
    end,
  },
  {
    name = "terrain harvest drops auto-pick up only after the normal movement arrival",
    run = function()
      local events = {}
      local campaign = new_campaign(9400041, events)
      local session = campaign.session
      local pickaxe = carried_tool(session, "tool.pickaxe")
      equip_tool(session, pickaxe)
      local x, y = face_fixture(session)
      local stone = session.state.world.cells[Grid.key(x, y)]
      stone.material_id, stone.current_integrity, stone.destroyed = "material.terrain.stone", 1, false
      local before = session.state.inventory:resource_quantity("resource.material.masonry")
      assert(session:turn("attack") == nil and stone.destroyed)
      assert(#session.state.world:ground_items_at(x, y) == 1)
      assert(session:turn("d") == nil)
      assert(session.state.inventory:resource_quantity("resource.material.masonry") == before + 1)
      assert(#session.state.world:ground_items_at(x, y) == 0)
    end,
  },
  {
    name = "generic one-sprite crack severity is derived only from authoritative integrity",
    run = function()
      local renderer = Renderer.new(nil)
      assert(renderer:_integrity_severity(6, 6) == 0)
      assert(renderer:_integrity_severity(4, 6) == 1)
      assert(renderer:_integrity_severity(2, 6) == 2)
      assert(renderer:_integrity_severity(0, 6) == 0)
    end,
  },
  {
    name = "tools break persist as cargo and repair through exact paid repair transactions",
    run = function()
      local events = {}
      local campaign = new_campaign(940005, events)
      local session = campaign.session
      local axe = carried_tool(session, "tool.axe")
      equip_tool(session, axe)
      local x, y = face_fixture(session)
      local tree = assert(session.state.world:place_object("world_object.feature.fallen_log", x, y))
      axe.object.current_durability = 1
      assert(session:turn("attack") == nil and axe.object.current_durability == 0)
      assert(session:attack_active_weapon().code == "tool_broken")
      session.state.scrap = 10
      local stock = Economy.create_stock(session, "service.repair.legacy", session.rng:derive("tool.repair"))
      local repaired = Economy.repair_tool(session, stock, axe.physical_id)
      assert(repaired.applied and repaired.tool_id == axe.physical_id and axe.object.current_durability == 1)
      assert(session:attack_active_weapon())
      assert(tree.id and Tool.condition(axe.object) == "broken")
    end,
  },
  {
    name = "tool death corpse salvage and save round trip retain exact durability",
    run = function()
      local events = {}
      local campaign = new_campaign(940006, events)
      local session = campaign.session
      local axe = carried_tool(session, "tool.axe")
      axe.object.current_durability = 3
      local directory = SaveStore.memory_directory()
      campaign:set_persistence_directory(directory)
      assert(CampaignPersistence.save(campaign, directory))
      local dead_id = axe.physical_id
      assert(campaign:handle_player_death({ cause = "tool_test" }).applied)
      local corpse
      for _, value in ipairs(campaign.session.state.corpses) do if value.source_kind == "player" then corpse = value; break end end
      assert(corpse)
      local found
      for _, entry in ipairs(corpse:list_carried_items()) do if entry.physical_id == dead_id then found = entry.item; break end end
      assert(found and found.object.current_durability == 3)
      -- Spatial corpse salvage transfers the same physical tool rather than a
      -- recreated replacement, with its remaining durability intact.
      campaign.session.state.player.x, campaign.session.state.player.y = corpse.x - 1, corpse.y
      campaign.session.state.player.direction = "d"
      local projection = assert(campaign.session:corpse_loot_grid(corpse.id))
      local carried = assert(projection.inventory:get(dead_id))
      local placement = assert(campaign.session.state.inventory:find_first_fit(carried.item))
      assert(campaign.session:salvage_corpse_to_inventory(corpse.id, dead_id, placement).applied)
      local recovered = assert(campaign.session.state.inventory:get(dead_id))
      assert(recovered.item.object.current_durability == 3)
      assert(campaign.session:validate_physical_ownership())
    end,
  },
}
