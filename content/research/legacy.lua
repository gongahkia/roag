-- Permanent account research.  Each node is declarative: runtime systems
-- interpret only the small modifier/unlock vocabulary validated by Registry.
return {
  { id = "research.body.hardened_frame_i", display_name = "Hardened Frame I", category = "body", cost = 1,
    description = "+1 maximum health at the start of future runs.", prerequisites = {}, modifiers = { max_health = 1 } },
  { id = "research.body.hardened_frame_ii", display_name = "Hardened Frame II", category = "body", cost = 2,
    description = "+1 maximum health at the start of future runs.", prerequisites = { "research.body.hardened_frame_i" }, modifiers = { max_health = 1 } },
  { id = "research.body.hardened_frame_iii", display_name = "Hardened Frame III", category = "body", cost = 3,
    description = "+1 maximum health at the start of future runs.", prerequisites = { "research.body.hardened_frame_ii" }, modifiers = { max_health = 1 } },
  { id = "research.mobility.efficient_actuators_i", display_name = "Efficient Actuators I", category = "mobility", cost = 1,
    description = "Dash cooldown -1 in future runs, when your body can dash.", prerequisites = {}, modifiers = { dash_cooldown = -1 } },
  { id = "research.mobility.efficient_actuators_ii", display_name = "Efficient Actuators II", category = "mobility", cost = 2,
    description = "Dash cooldown -1 in future runs, when your body can dash.", prerequisites = { "research.mobility.efficient_actuators_i" }, modifiers = { dash_cooldown = -1 } },
  { id = "research.loadout.expanded_charm_lattice", display_name = "Expanded Charm Lattice", category = "loadout", cost = 2,
    description = "+1 charm slot in future runs.", prerequisites = {}, modifiers = { charm_slots = 1 } },
  { id = "research.loadout.cargo_frame_i", display_name = "Cargo Frame I", category = "loadout", cost = 1,
    description = "+1 inventory row in future runs.", prerequisites = {}, modifiers = { inventory_rows = 1 } },
  { id = "research.loadout.cargo_frame_ii", display_name = "Cargo Frame II", category = "loadout", cost = 3,
    description = "+1 inventory row in future runs.", prerequisites = { "research.loadout.cargo_frame_i" }, modifiers = { inventory_rows = 1 } },
  { id = "research.traversal.reinforced_breach", display_name = "Reinforced Breach", category = "traversal", cost = 3,
    description = "Future bodies can breach reinforced traversal barriers.", prerequisites = {}, unlocks = { "unlock.traversal.reinforced_breach" } },
  { id = "research.preparation.salvage_reserve", display_name = "Salvage Reserve", category = "preparation", cost = 1,
    description = "Start each future run with 3 SCRAP.", prerequisites = {}, modifiers = { starting_scrap = 3 } },
}
