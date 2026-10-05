local DebugCockpit = require("src.tools.debug_cockpit")

return {
  {
    name = "debug cockpit validates production content without opening persistent save domains",
    run = function()
      local report = DebugCockpit.doctor()
      assert(report.ok and report.content.abilities > 0 and report.modifiers.definitions >= 1)
      assert(report.contracts.active_save_opened == false and report.contracts.meta_profile_opened == false)
    end,
  },
  {
    name = "debug cockpit modifier probe uses the production serialized resolver and trace",
    run = function()
      local report = DebugCockpit.modifier({ id = "expedition.passive.arc_relay", stacks = 4, trigger = "on_pierce",
        tags = "projectile,piercing", capabilities = "ability.electrical.discharge" })
      assert(report.definition.id == "expedition.passive.arc_relay")
      assert(#report.result.trace.nodes >= 4 and report.result.effects[1].effect.kind == "chain_electricity")
    end,
  },
  {
    name = "debug cockpit reproduces a self-owned bomb presentation path without mutating save state",
    run = function()
      local report = DebugCockpit.bomb_self({ seed = 44002 })
      assert(report.health_after == report.health_before - 1)
      assert(report.presentation.damage_numbers == 1 and report.presentation.hit_stop_requested)
      assert(report.world_valid)
    end,
  },
  {
    name = "debug cockpit reports deterministic scenario and expedition diagnostics",
    run = function()
      local report = DebugCockpit.determinism({ seed = 44003, character = "expedition.gunner" })
      assert(report.bomb_self and report.expedition_plan)
    end,
  },
}
