-- Headless OW-05 construction contract sampler. It deliberately uses the
-- same Campaign, build transaction, storage, death, and persistence seams as
-- play; no renderer or user save path is involved.
package.path = "./?.lua;./?/init.lua;" .. package.path

local Campaign = require("src.campaign.campaign")
local CampaignPersistence = require("src.persistence.campaign")
local SaveStore = require("src.persistence.save_store")
local Building = require("src.construction.building")
local Grid = require("src.world.grid")

local count, seed = 300, 970000
for index = 1, #arg do
  if arg[index] == "--count" then count = assert(tonumber(arg[index + 1])) end
  if arg[index] == "--seed" then seed = assert(tonumber(arg[index + 1])) end
end

local function add(session, resource_id, amount)
  local maximum = session.registry:get_resource(resource_id).max_stack
  while amount > 0 do
    local quantity = math.min(amount, maximum)
    assert(session.state.inventory:auto_place(session:create_resource_stack(resource_id, quantity, "campaign")))
    amount = amount - quantity
  end
end

local function build(session, recipe_id)
  -- A valid construction contract must be tested across the generated zone,
  -- not merely against the four cells around an occasionally crowded spawn.
  -- Preserve a deterministic candidate order and only relocate the test actor
  -- through its primary reachable region.
  for _, location in ipairs(session:_reachable_floor_cells()) do
    session.state.player.x, session.state.player.y = location.x, location.y
    for _, point in ipairs(Grid.neighbours(location)) do
      local result = Building.place(session, recipe_id, point.x, point.y)
      if result.applied then return assert(session.state.world:get_object(result.object_id)) end
    end
  end
  error("No legal construction cell")
end

local failures = 0
for index = 1, count do
  local campaign = Campaign.new({ seed = seed + index, campaign_id = string.format("campaign:%06d", seed + index) })
  local directory = SaveStore.memory_directory()
  campaign:set_persistence_directory(directory)
  assert(CampaignPersistence.save(campaign, directory))
  local session = campaign.session
  add(session, "resource.material.timber", 12)
  add(session, "resource.material.masonry", 8)
  add(session, "resource.material.metal", 16)
  local ok, reason = xpcall(function()
    local wall = build(session, "construction.timber_wall")
    local storage = build(session, "construction.storage_crate")
    local generator = build(session, "construction.generator")
    local breaker = build(session, "construction.breaker")
    assert(session.state.world:is_circuit_powered(generator.circuit_id) and generator.circuit_id == breaker.circuit_id)
    local entry = session.state.inventory.entries[1]
    assert(session:storage_transfer(storage.id, entry.physical_id, "to_storage").applied)
    assert(CampaignPersistence.save(campaign, directory))
    local restored = assert(CampaignPersistence.load(directory))
    restored:validate()
    assert(restored.session.state.world:get_object(wall.id))
  end, debug.traceback)
  if not ok then
    failures = failures + 1
    io.write("FAIL ", index, " ", tostring(reason), "\n")
  end
end

io.write(string.format("construction scenarios=%d failures=%d\n", count, failures))
if failures > 0 then os.exit(1) end
