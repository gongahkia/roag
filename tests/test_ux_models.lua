local App = require("src.app.app")
local GameplayUI = require("src.presentation.gameplay_ui")
local Input = require("src.app.input")
local Renderer = require("src.rendering.renderer")
local SaveStore = require("src.persistence.save_store")
local ZoneKey = require("src.campaign.zone_key")

local function new_app(seed)
  return App.new({ seed = seed, save_store = SaveStore.memory(), meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
end

local function new_campaign_app(seed)
  return App.new({
    seed = seed,
    campaign_slot_stores = { SaveStore.memory_directory(), SaveStore.memory_directory(), SaveStore.memory_directory() },
    meta_store = SaveStore.memory(), archive_store = SaveStore.memory(),
  })
end

return {
  {
    name = "UX failure text maps structured simulation codes without leaking enums",
    run = function()
      assert(GameplayUI.failure_text({ code = "inventory_full" }) == "INVENTORY FULL")
      assert(GameplayUI.failure_text({ code = "requires_power" }) == "NO POWER")
      assert(GameplayUI.failure_text({ code = "charm_slots_full" }) == "NO FREE CHARM SLOT")
      assert(GameplayUI.failure_text("component_broken") == "COMPONENT BROKEN")
    end,
  },
  {
    name = "UX HUD and body models show effective values, charm capacity, and readable component condition",
    run = function()
      local app = new_app(880101)
      assert(app:request_new_run())
      local session = app.session
      local hud = GameplayUI.hud(session)
      assert(hud.health == session.state.player.health and hud.max_health == session.state.player.max_health)
      assert(hud.charm_slots == session:modifier_value("charm_slots"))
      local body = GameplayUI.body(session, session.state.player)
      assert(#body == #session.state.player.body.slot_order)
      assert(body[1].slot and not body[1].slot:find("_", 1, true))
    end,
  },
  {
    name = "UX ability and salvage models expose physical provider, cost, condition, and inventory fit",
    run = function()
      local app = new_app(880102)
      assert(app:request_new_run())
      local player = app.session.state.player
      local enemy = app.session:_make_enemy("bomber", { x = player.x, y = player.y + 1 })
      local ability_id = app.session:available_actor_abilities(enemy, "body")[1]
      local ability = GameplayUI.ability(app.session, enemy, ability_id)
      assert(ability.provider and ability.resource and ability.name)
      app.session.state.enemies[#app.session.state.enemies + 1] = enemy
      app.session:_destroy_enemy(#app.session.state.enemies)
      local installed = app.session.state.corpses[#app.session.state.corpses]:list_components()[1]
      local salvage = GameplayUI.salvage(app.session, installed)
      assert(salvage.component.name and salvage.component.ability_text and salvage.fit_text)
    end,
  },
  {
    name = "UX context action consumes the existing corpse query without changing interaction authority",
    run = function()
      local app = new_app(880103)
      assert(app:request_new_run())
      local player = app.session.state.player
      local enemy = app.session:_make_enemy("bomber", { x = player.x + 1, y = player.y })
      app.session.state.enemies[#app.session.state.enemies + 1] = enemy
      app.session:_destroy_enemy(#app.session.state.enemies)
      local context = GameplayUI.context_action(app.session)
      assert(context.key == "G" and context.label == "SALVAGE REMAINS" and context.available)
    end,
  },
  {
    name = "UX context prioritizes the existing exit and translates unavailable interaction reasons",
    run = function()
      local unavailable = {
        state = { player = { x = 4, y = 4 } },
        available_interactions = function()
          return {
            {
              display_name = "POWERED DOOR",
              actions = { { label = "OPEN POWERED DOOR", available = false, code = "requires_power" } },
            },
          }
        end,
        nearby_corpse = function() return nil end,
      }
      local door = GameplayUI.context_action(unavailable)
      assert(door.key == "U" and door.label == "OPEN POWERED DOOR" and not door.available and door.reason == "NO POWER")
      unavailable.state.exit = { x = 5, y = 4 }
      local exit = GameplayUI.context_action(unavailable)
      assert(exit.key == "MOVE" and exit.priority == 1)
    end,
  },
  {
    name = "first run presents compact onboarding while later and direct starts retain existing flow",
    run = function()
      local app = new_app(880104)
      assert(app:begin_new_run() and app.screen == "onboarding")
      Input.keypressed(app, "return", nil, false)
      assert(app.screen == "game")
      assert(#app:onboarding_sections() == 4 and #app:help_sections() >= 7)
      app:return_to_title()
      assert(not app:begin_new_run() and app.screen == "replace_save")
    end,
  },
  {
    name = "Campaign HUD and succession acknowledgement explain the active reconstruction anchor and corpse recovery",
    run = function()
      local app = new_campaign_app(880105)
      assert(app:request_new_campaign())
      local campaign, source = app.campaign, app.session
      local source_zone = ZoneKey.to_data(campaign.active_zone.key)
      local anchor = GameplayUI.campaign_anchor(source)
      assert(anchor and anchor.current_zone and anchor.location:find("SURFACE 0, 0", 1, true))

      -- Use the real durable death transaction behind the App boundary, then
      -- make perform_turn observe its usual campaign_succession result.
      source.turn = function()
        local result, failure = campaign:handle_player_death({ cause = "ux_test" })
        assert(result, failure and failure.reason)
        return result.code
      end
      assert(app:perform_turn("ux_test") == "campaign_succession")
      assert(app.screen == "campaign_succession" and app.campaign_succession_notice)
      assert(app.campaign_succession_notice.death_location == GameplayUI.campaign_zone_label(campaign, source_zone))
      assert(app.campaign_succession_notice.anchor_location == GameplayUI.campaign_zone_label(campaign, campaign.state.reconstruction_anchor.zone_key))
      assert(app.session == campaign.session and app.session.state.player)
      Input.keypressed(app, "escape", nil, false)
      assert(app.screen == "game" and not app.campaign_succession_notice)
    end,
  },
  {
    name = "Campaign successor at the active anchor clears the one-turn death flag before field input resumes",
    run = function()
      local app = new_campaign_app(880106)
      assert(app:request_new_campaign())
      local campaign, source, original_turn = app.campaign, app.session, app.session.turn
      source.turn = function()
        local result, failure = campaign:handle_player_death({ cause = "ux_anchor_death" })
        assert(result, failure and failure.reason)
        -- Mirror Session:_mark_player_dead after its durable campaign
        -- transaction: source and successor are the same anchor Session.
        source.state.ended = "campaign_succession"
        return result.code
      end
      assert(app:perform_turn("ux_anchor_death") == "campaign_succession")
      assert(app.session == source and app.session.state.player and app.session.state.ended == nil)
      Input.keypressed(app, "escape", nil, false)
      local moved = false
      app.session.turn = function(self, input)
        assert(self.state.ended == nil, "successor must not retain a terminal death state")
        moved = input == "w"
        return "no_action"
      end
      Input.keypressed(app, "w", nil, false)
      Input.keyreleased(app, "w")
      assert(app.screen == "game" and moved)
      app.session.turn = original_turn
    end,
  },
  {
    name = "generic menus retain every supported label field and layout bounds stay explicit",
    run = function()
      local renderer = Renderer.new({})
      assert(renderer:menu_item_label({ name = "NAME" }) == "NAME")
      assert(renderer:menu_item_label({ display_name = "DISPLAY" }) == "DISPLAY")
      assert(renderer:menu_item_label({ label = "LABEL" }) == "LABEL")
      assert(renderer:menu_item_label({ id = "fallback.id" }) == "fallback.id")
      assert(GameplayUI.layout_bounds(1280, 720, 20, 20, 1240, 640))
      assert(not GameplayUI.layout_bounds(1280, 720, 20, 20, 1261, 640))
    end,
  },
}
