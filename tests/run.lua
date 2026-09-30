-- Dependency-free Lua test runner. It deliberately exercises pure modules so
-- tests work with LuaJIT even when LÖVE is not installed on a CI host.
package.path = "./?.lua;./?/init.lua;" .. package.path

local suites = {
  "tests.test_rng",
  "tests.test_generation",
  "tests.test_content",
  "tests.test_materials",
  "tests.test_force_cover",
  "tests.test_hazards_impact",
  "tests.test_fire",
  "tests.test_liquids",
  "tests.test_gas",
  "tests.test_body",
  "tests.test_body_damage",
  "tests.test_body_weapons",
  "tests.test_locomotion",
  "tests.test_inventory",
  "tests.test_corpse_salvage",
  "tests.test_reconstruction",
  "tests.test_app_inventory",
  "tests.test_session",
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
