local BalanceReport = require("src.balance.report")
local Economy = require("src.simulation.economy")
local Session = require("src.simulation.session")

local function session(seed)
  local value = Session.new({ seed = seed })
  value:start_run()
  return value
end

return {
  {
    name = "balance report exposes explicit bounded non-tactical run scenarios",
    run = function()
      local report = BalanceReport.build()
      assert(report.format == "roag.balance_report" and report.run_structure.normal_floors == 3)
      assert(report.run_structure.total_bosses == 4 and #report.tiers == 3)
      assert(report.economy_scenarios.fresh_conservative.expected_scrap_before_sales
        < report.economy_scenarios.fresh_aggressive.expected_scrap_before_sales)
      assert(report.economy_scenarios.fresh_conservative.persistent_data_before_discoveries == 15)
      assert(report.research.node_count == 18 and report.research.total_cost == 51)
    end,
  },
  {
    name = "tier pacing grows normal encounters without requiring rewardless refills by default",
    run = function()
      local report = BalanceReport.build()
      local first, second, third = report.tiers[1], report.tiers[2], report.tiers[3]
      assert(first.settings.enemies < second.settings.enemies and second.settings.enemies < third.settings.enemies)
      for _, tier in ipairs(report.tiers) do
        assert(tier.settings.objective_required <= tier.initial_reward_eligible)
        assert(tier.settings.completion_scrap_reward > 0 and tier.settings.completion_data_reward > 0)
      end
    end,
  },
  {
    name = "authored service stock preserves final-two-boss tradeoffs and repair limits",
    run = function()
      local value = session(99801)
      local supply = Economy.create_stock(value, "service.supply.legacy", value.rng:derive("balance.supply"), false)
      local hub_supply = Economy.create_stock(value, "service.supply.legacy", value.rng:derive("balance.hub.supply"), true)
      local repair = Economy.create_stock(value, "service.repair.legacy", value.rng:derive("balance.repair"), false)
      local hub_repair = Economy.create_stock(value, "service.repair.legacy", value.rng:derive("balance.hub.repair"), true)
      assert(supply.offers[1].remaining == 2 and supply.offers[2].price == 5)
      assert(hub_supply.offers[1].remaining == 6 and hub_supply.offers[2].remaining == 2)
      assert(repair.remaining == 2 and repair.price == 2)
      assert(hub_repair.remaining == 4 and hub_repair.price == 3)
    end,
  },
  {
    name = "boss-grade salvage survives more than a handful of uses while retaining cargo pressure",
    run = function()
      local report = BalanceReport.build()
      local by_id = {}
      for _, component in ipairs(report.components.entries) do by_id[component.id] = component end
      for _, id in ipairs({ "component.arm.impact_maul", "component.arm.barrage_emitter", "component.arm.vector_lance" }) do
        local component = by_id[id]
        assert(component.integrity >= 6)
        assert(component.wear_per_use == 1 and component.footprint.width == 3 and component.footprint.height == 3)
      end
    end,
  },
}
