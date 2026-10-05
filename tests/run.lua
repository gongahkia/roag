-- Dependency-free Lua test runner. It deliberately exercises pure modules so
-- tests work with LuaJIT even when LÖVE is not installed on a CI host.
package.path = "./?.lua;./?/init.lua;" .. package.path

local suites = {
  "tests.test_rng",
  "tests.test_generation",
  "tests.test_terrain_landmarks",
  "tests.test_content",
  "tests.test_materials",
  "tests.test_force_cover",
  "tests.test_hazards_impact",
  "tests.test_fire",
  "tests.test_liquids",
  "tests.test_electricity",
  "tests.test_gas",
  "tests.test_interaction_power",
  "tests.test_body",
  "tests.test_body_damage",
  "tests.test_body_weapons",
  "tests.test_enemy_tactics",
  "tests.test_locomotion",
  "tests.test_inventory",
  "tests.test_corpse_salvage",
  "tests.test_spatial_salvage",
  "tests.test_tools",
  "tests.test_expedition",
  "tests.test_modifiers",
  "tests.test_synergies",
  "tests.test_reconstruction",
  "tests.test_app_inventory",
  "tests.test_session",
  "tests.test_campaign",
  "tests.test_campaign_controls",
  "tests.test_build_stance",
  "tests.test_campaign_loadout",
  "tests.test_campaign_succession",
  "tests.test_world_content",
  "tests.test_construction",
  "tests.test_surface_world",
  "tests.test_vertical_world",
  "tests.test_save_resume",
  "tests.test_generation_inspector",
  "tests.test_rooms",
  "tests.test_routes",
  "tests.test_economy",
  "tests.test_balance",
  "tests.test_meta_progression",
  "tests.test_discoveries",
  "tests.test_factions_reinforcements",
  "tests.test_fallen_archive",
  "tests.test_bestiary",
  "tests.test_reactor",
  "tests.test_bosses",
  "tests.test_art_packs",
  "tests.test_presentation",
  "tests.test_play01_presentation",
  "tests.test_tooling_ui",
  "tests.test_ux_models",
}

local passed, failed = 0, 0
for _, module_name in ipairs(suites) do
  local cases = require(module_name)
  for _, case in ipairs(cases) do
    local ok, err = xpcall(case.run, debug.traceback)
    if ok then
      passed = passed + 1
      io.write("PASS ", case.name, "\n")
    else
      failed = failed + 1
      io.write("FAIL ", case.name, "\n", err, "\n")
    end
  end
end

io.write(string.format("\n%d passed, %d failed\n", passed, failed))
if failed > 0 then
  os.exit(1)
end
