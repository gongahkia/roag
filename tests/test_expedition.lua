local Content = require("src.expedition.content")
local Run = require("src.expedition.run")
local BuildEffects = require("src.simulation.build_effects")
local MetaProfile = require("src.persistence.meta_profile")
local Registry = require("src.content.registry")
local App = require("src.app.app")
local Input = require("src.app.input")
local SaveStore = require("src.persistence.save_store")
local Grid = require("src.world.grid")
local Chambers = require("src.expedition.chambers")

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
    name = "FEEL-01 reward cadence moves primary choices to XP levels while keeping fast drops and paid caches",
    run = function()
      local randoms, caches, elites = 0, 0, 0
      for _, plan in ipairs(Run.plan(80177)) do
        if plan.reward_kind == "random" then randoms = randoms + 1 end
        if plan.reward_kind == "cache" then caches = caches + 1 end
        if plan.elite then elites = elites + 1 end
      end
      assert(randoms >= 3 and caches >= 2 and elites >= 1)
      assert(Run.XP_THRESHOLDS[1] > 0 and Run.OWNED_PICK_WEIGHT >= 2.5)
    end,
  },
  {
    name = "Expedition content defines four classes, stackable passives, and twelve chamber encounter grammars",
    run = function()
      local registry = Registry.load()
      assert(Content.validate(registry))
      assert(#Content.CHARACTERS == 4 and #Content.PASSIVES >= 20 and #Content.ENCOUNTERS >= 12)
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
    name = "Expedition chamber plans are seed deterministic, staged, and use every pressure model once",
    run = function()
      local left, right = Run.plan(70401), Run.plan(70401)
      assert(#left == Run.REGULAR_ENCOUNTERS and #right == #left)
      local seen, streak, prior = {}, 0, nil
      for index, plan in ipairs(left) do
        assert(plan.id == right[index].id and plan.budget == right[index].budget and plan.profile_id == right[index].profile_id and plan.topology == right[index].topology)
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
      assert(count == 12)
      assert(left[1].stage == 1 and left[5].stage == 2 and left[9].stage == 3)
      assert(left[1].reward_kind == "none" and left[2].reward_kind == "random")
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
    name = "FEEL-01 chambers are compact connected boards with no passable traversal outside their bounds",
    run = function()
      local run = Run.new({ seed = 80109, character_id = "expedition.gunner", meta_profile = unlocked_profile() })
      for index, plan in ipairs(run.plan) do
        run:_begin_encounter(index)
        local chamber, world = run.session.state.expedition.chamber, run.session.state.world
        assert(chamber.bounds.width >= 8 and chamber.bounds.width <= 16)
        assert(chamber.bounds.height >= 7 and chamber.bounds.height <= 12)
        assert(world:is_passable(chamber.player_spawn.x, chamber.player_spawn.y))
        for x = 0, Grid.width - 1 do
          for y = 0, Grid.height - 1 do
            if world:is_passable(x, y) then assert(Chambers.contains(chamber.bounds, x, y)) end
          end
        end
        assert(plan.topology == chamber.topology)
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
    name = "FEEL-01 direct and derived player kills award XP and cash exactly once and queue levels",
    run = function()
      local run = Run.new({ seed = 80110, character_id = "expedition.gunner", meta_profile = unlocked_profile() })
      local session, exp, player = run.session, run.session.state.expedition, run.session.state.player
      local first = session.state.enemies[1]
      first.health = 1
      exp.xp = exp.xp_thresholds[1] - 2
      session:_apply_world_actor_damage(first, 1, nil, { source_actor = player, cause = "electrical", source = "fixture" })
      local kills, cash, pending = exp.kills, exp.currency, exp.pending_level_ups
      assert(kills == 1 and cash > 0 and pending == 1 and exp.level == 2)
      -- A second hit against the already removed/dead actor cannot duplicate
      -- the kill reward even if a derived environmental effect reports it.
      session:_apply_world_actor_damage(first, 1, nil, { source_actor = player, cause = "explosive", source = "fixture" })
      assert(exp.kills == kills and exp.currency == cash)
      assert(run:_open_level_reward() == "expedition_reward" and #run.pending_reward == 3)
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
    name = "Expedition level choices and paid cache economy remain run local",
    run = function()
      local run = Run.new({ seed = 80102, character_id = "expedition.gunner", meta_profile = unlocked_profile() })
      run.session.state.expedition.pending_level_ups = 1
      assert(run:_open_level_reward() == "expedition_reward")
      assert(#run.pending_reward == 3)
      assert(run:choose_reward(1).applied)
      -- Chamber four is the first paid cache in the FEEL-01 cadence.
      run.session.state.expedition.encounter_index = 4
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
  {
    name = "FEEL-01 analyzer validates compact boards, no offscreen spawn pressure, and XP/cash progression",
    run = function()
      local report = require("tools.analyze_feel01").analyze(24)
      assert(report.structural_failures == 0 and report.offscreen_threat_failures == 0)
      assert(report.levels / report.seeds >= 7 and report.levels / report.seeds <= 10)
      assert(report.pickups / report.seeds >= 12 and report.pickups / report.seeds <= 16)
    end,
  },
}
