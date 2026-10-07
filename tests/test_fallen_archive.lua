local Registry = require("src.content.registry")
local SaveStore = require("src.persistence.save_store")
local ActiveRun = require("src.persistence.active_run")
local FallenArchive = require("src.persistence.fallen_archive")
local Recurrence = require("src.simulation.fallen_recurrence")
local Session = require("src.simulation.session")
local App = require("src.app.app")
local Json = require("src.persistence.json")
local Grid = require("src.world.grid")
local RecurrenceAnalysis = require("src.simulation.fallen_recurrence_analysis")
local InspectionFloor = require("src.generation.inspection_floor")

local function source_session(seed, run_id)
  local session = Session.new({ seed = seed or 98400, registry = Registry.load(), run_id = run_id or "run:000901" })
  session:start_run()
  return session
end

local function record_from(session, id)
  return {
    id = id or "fallen:000001",
    source_run_id = session.state.run_id,
    body = session.state.player.body:to_data(),
    metadata = { route_path = {}, charm_ids = {}, research_ids = {}, biome_id = "biome.legacy.forest", route_depth = 1 },
  }
end

local function complete_to_route(session)
  assert(session:_complete_stage() == "reconstruction")
  assert(session:complete_reconstruction().next == "curse")
  assert(session:choose_curse(session.state.curse_options[1]).next == "route")
end

local function start_depth_two(session)
  complete_to_route(session)
  assert(session:select_route_node(session:available_route_nodes()[1].id).applied)
end

local function recurrence_session(mode, include_shock_coil)
  local source = source_session(98401, "run:000901")
  if include_shock_coil then
    assert(source.state.player.body:install("internal_2", source.component_factory:create("component.internal.legacy_shock_coil")))
  end
  local record = record_from(source)
  local spec = assert(Recurrence.assign("run:000902", 98402, { record }))
  spec.mode, spec.target_depth = mode or "corpse", 2
  local session = Session.new({ seed = 98402, registry = source.registry, run_id = "run:000902", fallen_recurrence = spec })
  session:start_run()
  start_depth_two(session)
  return session, record
end

return {
  {
    name = "fallen archive is versioned, persistent, deduplicated by source run, and tolerates incompatible history",
    run = function()
      local registry, store = Registry.load(), SaveStore.memory()
      local source = source_session(98410, "run:000910")
      local archive = FallenArchive.new()
      local first = assert(FallenArchive.append(archive, record_from(source)))
      assert(first.record.id == "fallen:000001" and archive.next_archive_sequence == 2)
      local duplicate = assert(FallenArchive.append(archive, record_from(source)))
      assert(not duplicate.applied and #archive.characters == 1)
      assert(FallenArchive.save(archive, store))
      local restored = assert(FallenArchive.load(store))
      assert(restored.characters[1].source_run_id == "run:000910")
      local incompatible = FallenArchive.copy(restored)
      incompatible.characters[1].body.slots[1].component.definition_id = "component.removed.history"
      assert(FallenArchive.validate(incompatible))
      local compatible, reason = FallenArchive.compatibility(incompatible.characters[1], registry)
      assert(not compatible and type(reason) == "string")
      local text = assert(FallenArchive.encode(restored))
      assert(text:find('"format":"roag.fallen_archive"', 1, true))
      local _, invalid = FallenArchive.decode('{"format":"roag.fallen_archive","version":2,"characters":[]}')
      assert(invalid.code == "unsupported_version")
    end,
  },
  {
    name = "empty archives assign no recurrence while victory and live-run replacement create no fallen history",
    run = function()
      local active, meta, archive = SaveStore.memory(), SaveStore.memory(), SaveStore.memory()
      local app = App.new({ seed = 98415, save_store = active, meta_store = meta, archive_store = archive })
      assert(app:request_new_run() and not app.session.state.fallen_recurrence)
      app.session.state.ended = "victory"
      assert(app:autosave("victory") and #app.fallen_archive.characters == 0)
      assert(app:request_new_run())
      assert(app:confirm_replace_save() == nil and #app.fallen_archive.characters == 0)
    end,
  },
  {
    name = "fatal run archives one immutable body before active save retirement and retries safely after a write failure",
    run = function()
      local active, meta, archive = SaveStore.memory(), SaveStore.memory(), SaveStore.memory()
      local app = App.new({ seed = 98420, save_store = active, meta_store = meta, archive_store = archive })
      assert(app:request_new_run())
      local player = app.session.state.player
      player.body:get_component("left_leg").current_integrity = 0
      assert(player.body:uninstall("right_arm"))
      player.health = 1
      app.session:_hurt("fixture fatal attack")
      assert(app:autosave("death"))
      assert(not active:exists() and #app.fallen_archive.characters == 1)
      local body = app.fallen_archive.characters[1].body
      assert(body.slots[5].component.current_integrity == 0 and not body.slots[4].component)
      -- The source-run key prevents duplicate append after any repeated
      -- death handling/reconciliation attempt.
      assert(not FallenArchive.append(app.fallen_archive, app.fallen_archive.characters[1]).applied)

      local failing = SaveStore.memory()
      local failure_once = true
      function failing:write(_)
        if failure_once then failure_once = false; return nil, { code = "write_failed", reason = "fixture failure" } end
        self.value = _
        return true
      end
      local pending_active, pending_meta = SaveStore.memory(), SaveStore.memory()
      local retry = App.new({ seed = 98421, save_store = pending_active, meta_store = pending_meta, archive_store = failing })
      assert(retry:request_new_run())
      retry.session.state.player.health = 1
      retry.session:_hurt("fixture fatal attack")
      local saved, failure = retry:autosave("death")
      assert(not saved and failure.code == "write_failed" and pending_active:exists())
      local reconciled = App.new({ seed = 98422, save_store = pending_active, meta_store = pending_meta, archive_store = failing })
      assert(not pending_active:exists() and #reconciled.fallen_archive.characters == 1)
    end,
  },
  {
    name = "recurrence selection is deterministic, snapshotted, and materializes fresh current-run bodies with provenance",
    run = function()
      local source = source_session(98430, "run:000930")
      source.state.player.body:get_component("left_leg").current_integrity = 0
      assert(source.state.player.body:uninstall("right_arm"))
      local record = record_from(source)
      local first = assert(Recurrence.assign("run:000931", 98431, { record }))
      local second = assert(Recurrence.assign("run:000931", 98431, { record }))
      assert(Json.encode(first) == Json.encode(second))
      first.mode, first.target_depth = "corpse", 2
      local session = Session.new({ seed = 98431, registry = source.registry, run_id = "run:000931", fallen_recurrence = first })
      session:start_run(); start_depth_two(session)
      local corpse = session.state.corpses[1]
      assert(corpse and corpse.fallen_archive_id == record.id)
      assert(not corpse.body:get_component("right_arm"))
      local component = corpse.body:get_component("left_leg")
      assert(component.current_integrity == 0 and component.id ~= record.body.slots[5].component.id)
      assert(component.origin.archive_id == record.id and component.origin.source_run_id == record.source_run_id)
      assert(session:validate_physical_ownership())
      local saved = SaveStore.memory(); assert(ActiveRun.save(session, saved))
      local restored = assert(ActiveRun.load(saved, { registry = source.registry }))
      assert(restored.state.fallen_recurrence.spawned and #restored.state.corpses == 1)
      assert(restored.state.corpses[1].body:get_component("left_leg").origin.source_component_id == record.body.slots[5].component.id)

      -- The target is a route depth, never a particular branch node: either
      -- legal depth-two choice receives the same snapshotted encounter.
      for choice = 1, 2 do
        local branch_spec = Recurrence.copy_spec(first)
        branch_spec.target_depth, branch_spec.mode = 2, "corpse"
        local branch = Session.new({ seed = 98432, registry = source.registry, run_id = "run:000932", fallen_recurrence = branch_spec })
        branch:start_run(); complete_to_route(branch)
        assert(branch:select_route_node(branch:available_route_nodes()[choice].id).applied)
        assert(branch.state.fallen_recurrence.spawned and #branch.state.corpses == 1)
      end
    end,
  },
  {
    name = "fallen corpse salvage preserves fresh identity through inventory reconstruction and a later archive lineage",
    run = function()
      local session, record = recurrence_session("corpse")
      local corpse = assert(session.state.corpses[1])
      local component_id = corpse.body:get_component("left_arm").id
      session.state.player.x, session.state.player.y = corpse.x, corpse.y
      assert(session:salvage_corpse_component(corpse.id, "left_arm").applied)
      assert(session.state.inventory:get(component_id).item.object.origin.archive_id == record.id)
      session.state.phase = "reconstruction"
      assert(session:uninstall_body_component("left_arm").applied)
      assert(session:install_inventory_component(component_id, "left_arm").applied)
      assert(session.state.player.body:get_component("left_arm").id == component_id)
      session:_mark_player_dead({ cause = "lineage" })
      local archive = FallenArchive.new()
      assert(FallenArchive.append(archive, session.state.death_pending_archive).applied)
      assert(archive.characters[1].body.slots[3].component.id == component_id)
      assert(archive.characters[1].body.slots[3].component.definition_id == record.body.slots[3].component.definition_id)
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "corrupted fallen shell uses body capabilities, preserves broken providers, and dies into the same salvageable body",
    run = function()
      local session = recurrence_session("hostile", true)
      local echo
      for _, enemy in ipairs(session.state.enemies) do if enemy.kind == "fallen_echo" then echo = enemy end end
      assert(echo and session:actor_has_capability(echo, "ability.weapon.projectile.basic"))
      local projectile = session:activate_actor_ability(echo, "ability.weapon.projectile.basic", { direction = "w" })
      assert(projectile.applied and #session.state.bullets >= 1)
      assert(session.state.world:set_liquid(echo.x, echo.y, "liquid.water.legacy", 1).applied)
      local shock = session:activate_actor_ability(echo, "ability.electrical.discharge", { direction = "w" })
      assert(shock.applied and shock.implementation == "electrical_discharge")
      local emitted_id = echo.body:get_component("right_arm").id
      local index = session:_enemy_index(echo)
      session:_destroy_enemy(index)
      local corpse = session.state.corpses[#session.state.corpses]
      assert(corpse.fallen_archive_id and corpse.body:get_component("right_arm").id == emitted_id)
      corpse.body:get_component("right_arm").current_integrity = 0
      assert(not corpse.body:has_capability("ability.weapon.projectile.basic"))
      assert(session:validate_physical_ownership())
    end,
  },
  {
    name = "recurrence placement is deterministic, reachable, non-overlapping, and persists partial salvage without rematerialization",
    run = function()
      local first, second = recurrence_session("corpse"), recurrence_session("corpse")
      local a, b = first.state.fallen_recurrence.placement, second.state.fallen_recurrence.placement
      assert(a.x == b.x and a.y == b.y and Grid.distance(first.state.player, a) >= 6)
      for _, target in ipairs(first.state.targets) do assert(target.x ~= a.x or target.y ~= a.y) end
      assert(not first.state.world:object_at(a.x, a.y) and not first.state.world:is_hazardous(a.x, a.y))
      first.state.player.x, first.state.player.y = a.x, a.y
      local corpse = first.state.corpses[1]
      local removed = corpse.body:get_component("left_arm").id
      assert(first:salvage_corpse_component(corpse.id, "left_arm").applied)
      local store = SaveStore.memory(); assert(ActiveRun.save(first, store))
      local restored = assert(ActiveRun.load(store, { registry = first.registry }))
      assert(#restored.state.corpses == 1 and not restored.state.corpses[1].body:get_component("left_arm"))
      assert(restored.state.inventory:get(removed) and restored.state.fallen_recurrence.spawned)
    end,
  },
  {
    name = "new run snapshots a compatible archive recurrence while live archive changes and tooling remain isolated",
    run = function()
      local active, meta, archive_store = SaveStore.memory(), SaveStore.memory(), SaveStore.memory()
      local source = source_session(98470, "run:000970")
      local archive = FallenArchive.new(); assert(FallenArchive.append(archive, record_from(source)).applied)
      assert(FallenArchive.save(archive, archive_store))
      local app = App.new({ seed = 98471, save_store = active, meta_store = meta, archive_store = archive_store })
      assert(app:request_new_run())
      local frozen = Json.encode(app.session.state.fallen_recurrence)
      local later = source_session(98472, "run:000971")
      assert(FallenArchive.append(app.fallen_archive, record_from(later, "fallen:000002")).applied)
      assert(Json.encode(app.session.state.fallen_recurrence) == frozen)
      assert(#app:fallen_archive_entries() == 2)
      -- Fallen archives remain a compatibility/persistence system, but are no
      -- longer promoted as a primary Expedition title action.
      for _, option in ipairs(app:title_options()) do assert(option.id ~= "fallen") end
      assert(app:open_fallen_archive() and app.screen == "fallen_archive")
      assert(active:exists())
      local archive_before = assert(archive_store:read())
      local preview_spec = Recurrence.copy_spec(app.session.state.fallen_recurrence)
      preview_spec.mode = "corpse"
      local preview = assert(InspectionFloor.generate({ biome = "biome.legacy.cave", tier = "tier.legacy.2", seed = 98473,
        fallen_recurrence = preview_spec, recurrence_depth = preview_spec.target_depth }))
      assert(preview.provenance.fallen_recurrence and preview.provenance.fallen_recurrence.archive_id == preview_spec.archive_id)
      assert(archive_store:read() == archive_before)
    end,
  },
  {
    name = "synthetic recurrence batch covers both modes and later route depths without persistent archive access",
    run = function()
      local report = assert(RecurrenceAnalysis.batch({ seed = 98480, count = 8 }))
      assert(report.summary.generated == 8 and report.summary.corpse == 4 and report.summary.hostile == 4)
      assert(report.summary.placement_failures == 0 and report.summary.critical_failures == 0)
    end,
  },
}
