local Definitions = require("src.expedition.content_definitions")
local Chambers = require("src.expedition.chambers")
local Run = require("src.expedition.run")
local Model = require("src.expedition.workbench_model")
local Store = require("src.expedition.content_store")

local function copy_file(from, to)
  local input = assert(io.open(from, "rb")); local text = input:read("*a"); input:close()
  local output = assert(io.open(to, "wb")); assert(output:write(text)); output:close()
end
local function temporary_store()
  local root = "/tmp/roag_tool01_workbench"
  os.execute("rm -rf " .. root)
  assert(os.execute("mkdir -p " .. root .. "/chambers " .. root .. "/encounters") == 0)
  for _, kind in ipairs({ "chambers", "encounters" }) do
    local source = Store.new(); local files = assert(source:list(kind)); copy_file("content/expedition/" .. kind .. "/manifest.json", root .. "/" .. kind .. "/manifest.json")
    for _, filename in ipairs(files) do copy_file("content/expedition/" .. kind .. "/" .. filename, root .. "/" .. kind .. "/" .. filename) end
  end
  return Store.new({ root = root }), root
end

return {
  {
    name = "TOOL-01 Expedition JSON content validates, composes, and loads deterministically",
    run = function()
      local first, second = assert(Definitions.load()), assert(Definitions.load())
      assert(#first.chambers == 10 and #first.encounters == 12)
      for index, chamber in ipairs(first.chambers) do assert(chamber.id == second.chambers[index].id) end
      local plan = Run.plan(424242)
      for _, entry in ipairs(plan) do
        assert(first.chamber_by_id[entry.chamber_id].id == entry.chamber.id)
        assert(Definitions.compatibility(entry.chamber, entry.template))
        local board = Chambers.instantiate(entry.chamber)
        assert(board.definition.id == entry.chamber_id and board.player_spawn and #board.markers.enemy_spawns > 0)
      end
    end,
  },
  {
    name = "TOOL-01 chamber validation rejects bad grids and incompatible pairs",
    run = function()
      local definitions = assert(Definitions.load())
      local malformed = { schema_version = 1, id = "chamber.bad", name = "Bad", width = 8, height = 7, topology_tags = { "open" }, tile_legend = { ["."] = "floor" }, tiles = { "........" }, markers = {} }
      local valid, failure = Definitions.validate_chamber(malformed, "bad.json")
      assert(not valid and failure.message:find("exactly height", 1, true))
      local compatible, reason = Definitions.compatibility(assert(definitions.chamber_by_id["chamber.open_basic"]), assert(definitions.encounter_by_id["expedition.encounter.reinforcement_pressure"]))
      assert(not compatible and reason:find("topology", 1, true))
    end,
  },
  {
    name = "TOOL-01 Workbench model paints, undoes, saves atomically, protects delete, and previews isolated runtime",
    run = function()
      local store, root = temporary_store()
      local model = assert(Model.new({ store = store }))
      assert(model:open("chambers", "chamber.open_basic"))
      local original = model:current_definition().tiles[1]
      assert(model:paint(1, 1, "#")); assert(model.dirty); assert(model:undo()); assert(model:current_definition().tiles[1] == original); assert(model:redo())
      assert(model:open("chambers", "chamber.open_basic", true))
      assert(model:new_definition("chambers", "chamber.test_board")); assert(model:save())
      assert(Model.new({ store = store }):open("chambers", "chamber.test_board"))
      assert(model:open("chambers", "chamber.open_basic", true)); local pending = assert(model:request_delete()); assert(#pending.references > 0); assert(model:confirm_delete(false) == false)
      local preview = assert(model:preview({ chamber_id = "chamber.conductive_basic", encounter_id = "expedition.encounter.hazard", seed = 77 }))
      assert(preview.run.session.state.expedition.chamber.id == "chamber.conductive_basic")
      os.execute("rm -rf " .. root)
    end,
  },
}
