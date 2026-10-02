-- Production tuning is deliberately plain data.  Runtime modules consume
-- these values through their existing generation/economy paths; this is not
-- a second progression or simulation layer.
return {
  id = "balance.legacy.production",
  discoveries = {
    placement_percent = 55,
    max_sites_per_floor = 1,
  },
  reinforcements = {
    placement_percent = 28,
    max_sources_per_floor = 1,
    warning_delay_turns = 2,
  },
  economy = {
    -- Selling is a relief valve for unwanted salvage, not a full-value
    -- conversion that funds every kiosk after a complete clear.
    resale_base_fraction = 0.20,
    resale_condition_fraction = 0.45,
  },
  scenarios = {
    -- These are report assumptions, not claims about a tactical playthrough.
    moderate_clear_remaining_fraction = 0.50,
  },
}
