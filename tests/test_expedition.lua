local Content = require("src.expedition.content")
local Run = require("src.expedition.run")
local BuildEffects = require("src.simulation.build_effects")
local MetaProfile = require("src.persistence.meta_profile")
local Registry = require("src.content.registry")
local App = require("src.app.app")
local Input = require("src.app.input")
local SaveStore = require("src.persistence.save_store")

local function unlocked_profile()
  local profile = MetaProfile.new()
  for _, id in ipairs({
    "expedition.unlock.character.conductor", "expedition.unlock.character.demolitionist",
    "expedition.unlock.item.arc_relay", "expedition.unlock.item.rupture_core",
  }) do MetaProfile.unlock_expedition(profile, id) end
  return profile
end

return {
  {
    name = "MOD-02 reward cadence provides fourteen acquisitions including elite bonuses",
    run = function()
      local total, elites = 0, 0
      for _, plan in ipairs(Run.plan(80177)) do total = total + 1; if plan.elite then total, elites = total + 1, elites + 1 end end
      assert(total >= 14 and elites >= 2)
      assert(Run.REWARD_ENCOUNTERS[1] == "choice" and Run.REWARD_ENCOUNTERS[2] == "random")
      assert(Run.OWNED_PICK_WEIGHT > 1)
    end,
  },
  {
    name = "Expedition content defines four classes twenty stackable passives and six encounter grammars",
    run = function()
      local registry = Registry.load()
      assert(Content.validate(registry))
      assert(#Content.CHARACTERS == 4 and #Content.PASSIVES >= 20 and #Content.ENCOUNTERS >= 6)
      local changing = 0
      for _, passive in ipairs(Content.PASSIVES) do if passive.behavior_changing then changing = changing + 1 end end
      assert(changing >= 8)
    end,
  },
  {
    name = "Expedition character and item unlocks persist without granting numerical meta power",
    run = function()
      local registry, profile = Registry.load(), MetaProfile.new()
      assert(Run.character_unlocked(profile, Content.character("expedition.gunner")))
      assert(not Run.character_unlocked(profile, Content.character("expedition.conductor")))
      assert(MetaProfile.unlock_expedition(profile, "expedition.unlock.character.conductor").applied)
      assert(MetaProfile.unlock_expedition(profile, "expedition.unlock.item.arc_relay").applied)
      local payload = assert(MetaProfile.encode(profile, registry))
      local restored = assert(MetaProfile.decode(payload, registry))
      assert(Run.character_unlocked(restored, Content.character("expedition.conductor")))
      assert(MetaProfile.has_expedition_unlock(restored, "expedition.unlock.item.arc_relay"))
      assert(not restored.expedition_modifiers)
    end,
  },
  {
    name = "Expedition plans are seed deterministic contain all grammar families and avoid repetition streaks",
    run = function()
      local left, right = Run.plan(70401), Run.plan(70401)
      assert(#left == Run.REGULAR_ENCOUNTERS and #right == #left)
      local seen, streak, prior = {}, 0, nil
      for index, plan in ipairs(left) do
        assert(plan.id == right[index].id and plan.budget == right[index].budget and plan.profile_id == right[index].profile_id)
        assert(plan.actual_budget <= plan.budget and #plan.enemies > 0)
        for role, required in pairs(plan.template.minimum_roles) do
          local found = 0
          for _, enemy in ipairs(plan.enemies) do if enemy.role == role then found = found + 1 end end
          assert(found >= required)
        end
        seen[plan.id] = true
        streak = plan.id == prior and streak + 1 or 1
        assert(streak <= 2)
        prior = plan.id
      end
      local count = 0
      for _ in pairs(seen) do count = count + 1 end
      assert(count == 6)
      assert(left[1].reward_kind == "choice")
    end,
  },
  {
    name = "Expedition classes use isolated class weapons reserves and no spatial inventory progression",
    run = function()
      local profile = unlocked_profile()
      for _, character_id in ipairs({ "expedition.gunner", "expedition.bruiser", "expedition.conductor", "expedition.demolitionist" }) do
        local run = Run.new({ seed = 80100, character_id = character_id, meta_profile = profile })
        local expedition, player = run.session.state.expedition, run.session.state.player
        assert(run.session.expedition and not run.session.campaign)
        assert(player.base_max_health == Content.character(character_id).base_hp)
        assert(expedition.weapon_provider_id and expedition.weapon_ability_id and expedition.active_ability_id)
        assert(run.session.state.inventory:total_mass() == 0)
        local weapon = run.session:expedition_active_weapon()
        assert(weapon and weapon.ability.id == Content.character(character_id).weapon_ability)
        if character_id == "expedition.demolitionist" then
          assert(expedition.passive_stacks["expedition.passive.demolition_kit"] == 1)
        end
      end
    end,
  },
  {
    name = "Expedition passive stacks scale the existing deterministic build-effect pipeline",
    run = function()
      local run = Run.new({ seed = 80106, character_id = "expedition.conductor", meta_profile = unlocked_profile() })
      run:add_passive("expedition.passive.arc_relay")
      run:add_passive("expedition.passive.arc_relay")
      run:add_passive("expedition.passive.arc_relay")
      local effects = BuildEffects.resolve(run.session, run.session.state.player, { type = "on_pierce", source_actor = run.session.state.player,
        attack_tags = { projectile = true } })
      assert(#effects == 1 and effects[1].effect.effect.max_cells == 7)
    end,
  },
  {
    name = "Expedition duplicate passives modify the shared combat modifier layer and run build summary",
    run = function()
      local run = Run.new({ seed = 80101, character_id = "expedition.gunner", meta_profile = unlocked_profile() })
      run:add_passive("expedition.passive.ballistic_lens")
      run:add_passive("expedition.passive.ballistic_lens")
      run:add_passive("expedition.passive.scatter_matrix")
      assert(run.session:modifier_value("projectile_damage") >= 3)
      assert(run.session:modifier_value("projectile_count") == 1)
      local summary = run:build_summary()
      local lens
      for _, entry in ipairs(summary.passives) do if entry.id == "expedition.passive.ballistic_lens" then lens = entry end end
      assert(lens and lens.count == 2)
    end,
  },
  {
    name = "Expedition reward choice chest economy and clear cadence are run local",
    run = function()
      local run = Run.new({ seed = 80102, character_id = "expedition.gunner", meta_profile = unlocked_profile() })
      assert(run:_complete_encounter() == "expedition_reward")
      assert(#run.pending_reward == 3)
      assert(run:choose_reward(1).applied)
      -- Encounter five is the first paid cache in the fixed prototype cadence.
      run.session.state.expedition.encounter_index = 5
      run.session.state.expedition.currency = 99
      run.pending_chest = { cost = run:_chest_cost(), options = { Content.PASSIVES[1] } }
      local before = run.session.state.expedition.currency
      assert(run:open_chest().applied)
      assert(run.session.state.expedition.currency < before)

      local bruiser = Run.new({ seed = 80105, character_id = "expedition.bruiser", meta_profile = unlocked_profile() })
      for _, passive in ipairs(bruiser:reward_options(12)) do
        assert(not passive.modifiers or not passive.modifiers.projectile_damage)
      end
    end,
  },
  {
    name = "Expedition death ends the disposable run without Sandbox succession or active-run persistence",
    run = function()
      local run = Run.new({ seed = 80103, character_id = "expedition.bruiser", meta_profile = unlocked_profile() })
      run.session:_mark_player_dead({ cause = "test" })
      assert(run:turn("w") == "expedition_dead")
      assert(run.summary_data and not run.summary_data.victory)
      assert(not run.session.state.death_pending_archive)
    end,
  },
  {
    name = "Expedition title and I key use character selection and paused build summary instead of Sandbox inventory",
    run = function()
      local slots = { SaveStore.memory_directory(), SaveStore.memory_directory(), SaveStore.memory_directory() }
      local app = App.new({ seed = 80104, save_store = SaveStore.memory(), meta_store = SaveStore.memory(), archive_store = SaveStore.memory(), campaign_slot_stores = slots })
      assert(app:title_options()[1].id == "expedition")
      app:activate_title_choice()
      assert(app.screen == "expedition_character_select")
      assert(app:select_expedition_character())
      assert(app:is_expedition_mode())
      Input.keypressed(app, "i", nil, false)
      assert(app.screen == "expedition_build")
      Input.keypressed(app, "i", nil, false)
      assert(app.screen == "game")
      assert(not app:open_salvage())
      assert(app.session:_interact_player().code == "expedition_no_field_interaction")
      assert(not app.save_store:exists())
    end,
  },
}
