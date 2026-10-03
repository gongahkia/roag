local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local App = require("src.app.app")
local Component = require("src.body.component")
local Inventory = require("src.inventory.inventory")
local Input = require("src.app.input")
local PhysicalItem = require("src.inventory.physical_item")
local SaveStore = require("src.persistence.save_store")
local Session = require("src.simulation.session")

local function new_campaign(seed)
  local campaign = Campaign.new({ seed = seed, campaign_id = "campaign:" .. seed })
  local directory = SaveStore.memory_directory()
  campaign:set_persistence_directory(directory)
  assert(CampaignPersistence.save(campaign, directory))
  campaign.session.state.enemies = {}
  return campaign, directory
end

local function install_left_arm(session, definition_id)
  local body = session.state.player.body
  local old = assert(body:detach("left_arm"))
  assert(session.state.inventory:auto_place(PhysicalItem.from_component(old, session.registry)))
  local component = session.component_factory:create(definition_id)
  assert(body:install("left_arm", component))
  return component
end

local function active_weapon(session)
  local loadout = assert(session:campaign_loadout())
  local binding = assert(loadout.weapon_slots[loadout.active_weapon])
  local installed = assert(session.state.player.body:find_component(binding.physical_id))
  return loadout, binding, installed.component, session.registry:get_ability(binding.ability_id)
end

return {
  {
    name = "Campaign quick loadout defaults are stable physical bindings with starter ammunition",
    run = function()
      local campaign = new_campaign(930001)
      local session, player = campaign.session, campaign.session.state.player
      local loadout = assert(session:campaign_loadout())
      assert(loadout.body_actor_id == player.actor_id and loadout.active_weapon == 1 and loadout.active_ability == 1)
      assert(loadout.weapon_slots[1].source_kind == "component" and loadout.weapon_slots[1].physical_id)
      assert(loadout.ability_slots[1].source_kind == "actor" and loadout.ability_slots[1].ability_id == "ability.mobility.dash")
      assert(player.ammo == 0 and session:ammo_reserve("resource.ammo.bullets") > 0)
      local _, _, component, ability = active_weapon(session)
      assert(session:weapon_magazine(component, ability).loaded == ability.ammo.magazine_capacity)
    end,
  },
  {
    name = "Campaign weapon slots bind exact providers swap for one turn and never auto-fallback",
    run = function()
      local campaign = new_campaign(930002)
      local session, player = campaign.session, campaign.session.state.player
      local blade = install_left_arm(session, "component.arm.impact_blade")
      assert(session:assign_campaign_loadout("weapon", 2, {
        source_kind = "component", physical_id = blade.id, ability_id = "ability.weapon.melee.basic",
      }).applied)
      local before = player.dash
      assert(session:turn("swap_weapon") == nil)
      local loadout = session:campaign_loadout()
      assert(loadout.active_weapon == 2 and session.last_action_result.applied and player.dash == math.max(0, before - 1))
      blade.current_integrity = 0
      assert(session:turn("attack") == nil)
      assert(session.last_action_result.code == "provider_broken" and session:campaign_loadout().active_weapon == 2)
      blade.current_integrity = 1
      assert(session:turn("attack") == nil)
      assert(session.last_action_result.code ~= "provider_broken")
      -- Removing the exact provider does not let a same-definition or other
      -- emitter silently take over the assigned slot.
      assert(player.body:detach("left_arm") == blade)
      assert(session:turn("attack") == nil and session.last_action_result.code == "slot_empty")
    end,
  },
  {
    name = "Campaign invalid quick swaps are readable no-turn failures while valid ability swaps spend one turn",
    run = function()
      local campaign = new_campaign(930003)
      local session, player = campaign.session, campaign.session.state.player
      assert(session:turn("swap_weapon") == "no_action")
      assert(session.last_action_result.code == "slot_empty")
      local arc_blade = install_left_arm(session, "component.arm.reactor_arc_blade")
      assert(session:assign_campaign_loadout("ability", 2, {
        source_kind = "component", physical_id = arc_blade.id, ability_id = "ability.electrical.discharge",
      }).applied)
      local before = player.dash
      assert(session:turn("swap_ability") == nil)
      assert(session:campaign_loadout().active_ability == 2 and player.dash == math.max(0, before - 1))
      arc_blade.current_integrity = 0
      assert(session:turn("swap_ability") == nil)
      assert(session:campaign_loadout().active_ability == 1)
      assert(session:turn("swap_ability") == "no_action" and session.last_action_result.code == "provider_broken")
    end,
  },
  {
    name = "Campaign magazines fire loaded rounds then reload physical compatible ammunition without firing",
    run = function()
      local campaign = new_campaign(930004)
      local session = campaign.session
      local _, _, component, ability = active_weapon(session)
      local magazine = assert(session:weapon_magazine(component, ability))
      magazine.loaded = 1
      local reserve = session:ammo_reserve(ability.ammo.family)
      session.state.bullets = {}
      assert(session:turn("attack") == nil)
      assert(magazine.loaded == 0 and #session.state.bullets == 1 and session:ammo_reserve(ability.ammo.family) == reserve)
      session.state.bullets = {}
      assert(session:turn("attack") == nil)
      assert(session.last_action_result.code == "reloaded" and #session.state.bullets == 0)
      assert(magazine.loaded == reserve and session:ammo_reserve(ability.ammo.family) == 0)
      magazine.loaded = 0
      assert(session:turn("attack") == nil)
      assert(session.last_action_result.code == "no_compatible_ammo" and #session.state.bullets == 0)
    end,
  },
  {
    name = "Campaign physical ammunition consumption is positional and family-specific",
    run = function()
      local session = Session.new({ seed = 930005 })
      session:start_run()
      local inventory = Inventory.new()
      local first = PhysicalItem.from_resource("resource.ammo.bullets", 2, "item:first", session.registry)
      local second = PhysicalItem.from_resource("resource.ammo.bullets", 3, "item:second", session.registry)
      local shells = PhysicalItem.from_resource("resource.ammo.shells", 4, "item:shells", session.registry)
      assert(inventory:place(second, 2, 1))
      assert(inventory:place(first, 1, 1))
      assert(inventory:place(shells, 3, 1))
      local changes = assert(inventory:consume_resources({ ["resource.ammo.bullets"] = 4 }))
      assert(changes[1].physical_id == "item:first" and changes[2].physical_id == "item:second")
      assert(inventory:resource_quantity("resource.ammo.bullets") == 1 and inventory:resource_quantity("resource.ammo.shells") == 4)
      assert(first.mass == 0.05 * 2 and shells.footprint.width == 1 and shells.footprint.height == 1)
    end,
  },
  {
    name = "Campaign loadout magazine and physical reserve survive manifest and zone round trip",
    run = function()
      local campaign = new_campaign(930006)
      local session = campaign.session
      local loadout, _, component, ability = active_weapon(session)
      loadout.active_weapon = 1
      session:weapon_magazine(component, ability).loaded = 2
      local manifest, shard = campaign:to_manifest_data(), campaign:to_zone_data()
      local restored = Campaign.from_data(manifest, shard)
      local restored_session = restored.session
      local _, _, restored_component, restored_ability = active_weapon(restored_session)
      assert(restored_session:weapon_magazine(restored_component, restored_ability).loaded == 2)
      assert(restored_session:ammo_reserve(restored_ability.ammo.family) == session:ammo_reserve(ability.ammo.family))
    end,
  },
  {
    name = "Campaign old scalar ammunition migration is idempotent and successor loadouts never retain corpse bindings",
    run = function()
      local campaign, directory = new_campaign(930007)
      local session, player = campaign.session, campaign.session.state.player
      local old_binding = session:campaign_loadout().weapon_slots[1].physical_id
      campaign.state.loadout = nil
      player.ammo = 3
      local before = session:ammo_reserve("resource.ammo.bullets")
      local migrated = session:ensure_campaign_loadout()
      assert(player.ammo == 0 and session:ammo_reserve("resource.ammo.bullets") == before + 3)
      session:ensure_campaign_loadout()
      assert(session:ammo_reserve("resource.ammo.bullets") == before + 3 and migrated.ammo_migration_actor_id == player.actor_id)
      local old_player = player.actor_id
      local result = assert(campaign:handle_player_death({ cause = "loadout_test" }))
      assert(result.code == "campaign_succession" and campaign.session.state.player.actor_id ~= old_player)
      local successor_loadout = campaign.session:campaign_loadout()
      assert(successor_loadout.body_actor_id == campaign.session.state.player.actor_id)
      assert(successor_loadout.weapon_slots[1].physical_id ~= old_binding)
      assert(CampaignPersistence.save(campaign, directory))
    end,
  },
  {
    name = "weapon state serializes with the exact component physical identity",
    run = function()
      local campaign = new_campaign(930008)
      local session = campaign.session
      local _, _, component, ability = active_weapon(session)
      session:weapon_magazine(component, ability).loaded = 3
      local data = Component.to_data(component)
      local restored = Component.from_data(session.registry:get_component(component.definition_id), data)
      assert(restored.id == component.id and restored.weapon_state[ability.ammo.magazine_key].loaded == 3)
    end,
  },
  {
    name = "Campaign inventory loadout assignment is paused while field swap remains tactical",
    run = function()
      local slots = { SaveStore.memory_directory(), SaveStore.memory_directory(), SaveStore.memory_directory() }
      local app = App.new({ seed = 930009, campaign_slot_stores = slots, meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
      assert(app:request_new_campaign())
      local player = app.session.state.player
      local dash, x, y = player.dash, player.x, player.y
      Input.keypressed(app, "i", nil, false)
      Input.keypressed(app, "tab", nil, false)
      app.loadout_focus, app.loadout_selection = 2, 1
      assert(app:assign_selected_loadout().applied)
      assert(app.screen == "inventory" and player.dash == dash and player.x == x and player.y == y)
      Input.keypressed(app, "i", nil, false)
      assert(app.screen == "game")
      assert(app.session:campaign_loadout().weapon_slots[2])
    end,
  },
}
