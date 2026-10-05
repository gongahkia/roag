local Definitions = require("src.expedition.modifiers")
local Store = require("src.expedition.modifier_store")
local Editor = require("src.expedition.modifier_editor_model")
local Simulator = require("src.expedition.modifier_simulator")
local Registry = require("src.content.registry")

local function production_files()
  local manifest = assert(io.open("content/expedition/modifiers/manifest.json", "rb")):read("*a")
  local decoded = assert(require("src.persistence.json").decode(manifest))
  local files = { ["manifest.json"] = manifest }
  for _, filename in ipairs(decoded.files) do files[filename] = assert(io.open("content/expedition/modifiers/" .. filename, "rb")):read("*a") end
  return files
end

return {
  {
    name = "serialized Expedition modifier corpus loads canonically with stable IDs",
    run = function()
      local registry = assert(Definitions.load({ registry = Registry.load() }))
      assert(#registry.ordered == 25 and registry:get("expedition.passive.arc_relay"))
      for _, definition in ipairs(registry.ordered) do
        local round_tripped = assert(Definitions.round_trip(definition, { registry = Registry.load() }))
        assert(round_tripped.id == definition.id)
      end
    end,
  },
  {
    name = "modifier validation reports file paths and semantic field failures",
    run = function()
      local definition = Definitions.new_definition("expedition.passive.bad")
      definition.static_effects = { { kind = "modify_stat", stat = "not_a_stat", value = { kind = "linear", base = 1, per_stack = 1 } } }
      local ok, failure = Definitions.validate(definition, { file = "fixture.json", registry = Registry.load() })
      assert(not ok and failure.file == "fixture.json" and failure.path:find("static_effects", 1, true))
      definition.static_effects[1].stat = "projectile_damage"
      definition.schema_version = 99
      ok, failure = Definitions.validate(definition, { file = "fixture.json", registry = Registry.load() })
      assert(not ok and failure.code == "unsupported_schema")
    end,
  },
  {
    name = "stack expressions are explicit deterministic and safely validated",
    run = function()
      assert(Definitions.evaluate_expression({ kind = "constant", value = 3 }, 10) == 3)
      assert(Definitions.evaluate_expression({ kind = "linear", base = 2, per_stack = 3 }, 3) == 8)
      assert(Definitions.evaluate_expression({ kind = "geometric", base = 2, multiplier_per_stack = 2 }, 3) == 8)
      assert(Definitions.evaluate_expression({ kind = "every_n", base = 1, per_step = 2, n = 2 }, 5) == 5)
      assert(Definitions.evaluate_expression({ kind = "thresholds", values = { { stacks = 1, value = 2 }, { stacks = 3, value = 5 } } }, 4) == 5)
      local bad = Definitions.new_definition("expedition.passive.bad_expression")
      bad.static_effects = { { kind = "modify_stat", stat = "projectile_damage", value = { kind = "every_n", base = 1, per_step = 1, n = 0 } } }
      local ok, failure = Definitions.validate(bad, { file = "bad.json" })
      assert(not ok and failure.path:find("%.n"))
    end,
  },
  {
    name = "static Expedition modifiers resolve from data without modifier ID branches",
    run = function()
      local registry = assert(Definitions.load({ registry = Registry.load() }))
      local values = Definitions.static_values({ ["expedition.passive.ballistic_lens"] = 3, ["expedition.passive.brutal_edge"] = 2 }, registry)
      assert(values.projectile_damage == 3 and values.melee_damage == 2 and values.melee_force == 2)
    end,
  },
  {
    name = "reactive modifier resolver compiles data hooks in stable order with stack values",
    run = function()
      local registry = assert(Definitions.load({ registry = Registry.load() }))
      local actor = { kind = "player", health = 6, max_health = 6 }
      local session = { state = { player = actor, expedition = { character_id = "expedition.conductor", passive_stacks = { ["expedition.passive.arc_relay"] = 3, ["expedition.passive.conductive_payload"] = 2 } } }, actor_has_capability = function(_, _, id) return id == "ability.electrical.discharge" end }
      local effects = Definitions.resolve_hooks(session, actor, { type = "on_pierce", source_actor = actor, attack_tags = { projectile = true } }, registry)
      assert(#effects == 1 and effects[1].effect.effect.max_cells == 7 and effects[1].key:find("arc_relay", 1, true))
    end,
  },
  {
    name = "modifier simulator uses real compiled data runtime and stays isolated",
    run = function()
      local registry = assert(Definitions.load({ registry = Registry.load() }))
      local definition = registry:get("expedition.passive.arc_relay")
      local result = Simulator.run({ definition = definition, modifier_registry = registry, stack_count = 4, trigger = "on_pierce", attack_tags = { projectile = true }, capabilities = { ["ability.electrical.discharge"] = true } })
      assert(#result.effects == 1 and result.effects[1].effect.max_cells == 9 and #result.trace.nodes >= 4)
      assert(result.truncated == false)
    end,
  },
  {
    name = "modifier editor model creates validates saves duplicates and protects delete confirmation",
    run = function()
      local files = production_files()
      local editor = Editor.new({ store = Store.memory(files, true), registry = Registry.load() })
      assert(#editor:list() == 25)
      local draft = assert(editor:create("expedition.passive.fixture_damage"))
      draft.name, draft.description = "FIXTURE DAMAGE", "Adds projectile damage."
      editor:add_static_stat("projectile_damage")
      assert(editor:save())
      assert(#editor:list() == 26)
      assert(editor:duplicate("expedition.passive.fixture_damage", "expedition.passive.fixture_damage_copy"))
      assert(editor:save())
      local pending = assert(editor:request_delete("expedition.passive.fixture_damage_copy"))
      assert(pending.id == "expedition.passive.fixture_damage_copy")
      assert(editor:confirm_delete(false).code == "cancelled")
      assert(editor.modifiers:get("expedition.passive.fixture_damage_copy"))
    end,
  },
  {
    name = "modifier registries are introspectable and ordinary fixtures need no simulation code",
    run = function()
      assert(Definitions.TRIGGERS.on_pierce.fields and Definitions.CONDITIONS.attack_has_tag.fields)
      assert(Definitions.EFFECTS.chain_electricity.fields and Definitions.STACK_EXPRESSIONS.thresholds.fields)
      local definition = Definitions.new_definition("expedition.passive.fixture_reactive")
      definition.name, definition.description = "FIXTURE", "Tests a registered primitive."
      definition.hooks = { { trigger = "on_hit", conditions = { { kind = "attack_has_tag", tag = "projectile" } }, effects = { { kind = "ignite", radius = { kind = "linear", base = 0, per_stack = 1 } } } } }
      assert(Definitions.validate(definition, { registry = Registry.load() }))
    end,
  },
}
