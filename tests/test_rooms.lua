local Batch = require("src.generation.batch_analysis")
local ContentRegistry = require("src.content.registry")
local DungeonRooms = require("src.generation.dungeon_rooms")
local Grid = require("src.world.grid")
local InspectionFloor = require("src.generation.inspection_floor")
local Inspector = require("src.tools.generation_inspector")
local Json = require("src.persistence.json")
local Rng = require("src.rng")
local Config = require("src.rooms.config")
local RoomRegistry = require("src.rooms.registry")
local Store = require("src.rooms.store")
local Template = require("src.rooms.template")
local EditorModel = require("src.tools.room_editor_model")
local SaveStore = require("src.persistence.save_store")
local Analysis = require("src.generation.analysis")

local function encode(value)
  local text, failure = Json.encode(value)
  assert(text, failure)
  return text
end

local function replace(value, index, replacement)
  return value:sub(1, index - 1) .. replacement .. value:sub(index + 1)
end

local function set_glyph(template, x, y, glyph)
  local row = template.height - y
  template.layout[row] = replace(template.layout[row], x + 1, glyph)
end

local function valid_template(id, connectors)
  local template = Template.default(id)
  for _, connector in ipairs(connectors or {}) do
    local x, y = Template.connector_position(template, connector)
    set_glyph(template, x, y, ".")
    template.connectors[#template.connectors + 1] = { side = connector.side, offset = connector.offset }
  end
  return template
end

local function corpus_memory()
  local source = Store.new()
  local files = assert(source:list())
  local values = {}
  values["manifest.json"] = assert(source:read("manifest.json"))
  for _, filename in ipairs(files) do values[filename] = assert(source:read(filename)) end
  return Store.memory(values)
end

local function room_by_slot(metadata, x, y)
  for _, room in ipairs(metadata.rooms) do
    if room.slot.x == x and room.slot.y == y then return room end
  end
end

local function connector(room, side)
  for _, value in ipairs(room.connectors) do if value.side == side then return value end end
end

return {
  {
    name = "versioned external room templates load headlessly with complete connector coverage",
    run = function()
      local rooms = assert(RoomRegistry.load())
      assert(#rooms.order == 10)
      local coverage = rooms:coverage()
      assert(coverage.valid and coverage.patterns["north+east+south+west"] > 0)
    end,
  },
  {
    name = "room loader rejects malformed JSON formats versions and duplicate semantic IDs",
    run = function()
      local invalid = valid_template("room.dungeon.standard.invalid", {})
      invalid.format = "wrong"
      local store = Store.memory({ ["manifest.json"] = encode({ format = "roag.room_manifest", version = 1, files = { "invalid.room.json" } }),
        ["invalid.room.json"] = encode(invalid) })
      local _, failure = RoomRegistry.load({ store = store })
      assert(failure.code == "invalid_template" and failure.errors[1].code == "unsupported_format")

      local malformed = Store.memory({ ["manifest.json"] = encode({ format = "roag.room_manifest", version = 1, files = { "bad.room.json" } }),
        ["bad.room.json"] = "{" })
      local _, malformed_failure = RoomRegistry.load({ store = malformed })
      assert(malformed_failure.code == "invalid_json")

      local first, second = valid_template("room.dungeon.standard.duplicate", {}), valid_template("room.dungeon.standard.duplicate", {})
      local duplicates = Store.memory({ ["manifest.json"] = encode({ format = "roag.room_manifest", version = 1, files = { "a.room.json", "b.room.json" } }),
        ["a.room.json"] = encode(first), ["b.room.json"] = encode(second) })
      local _, duplicate_failure = RoomRegistry.load({ store = duplicates })
      assert(duplicate_failure.code == "invalid_template" and duplicate_failure.errors[1].code == "duplicate_id")
    end,
  },
  {
    name = "room validation rejects invalid dimensions glyphs materials and connector geometry",
    run = function()
      local registry = ContentRegistry.load()
      local room = valid_template("room.dungeon.standard.fixture", { { side = "north", offset = 5 } })
      assert(Template.validate(room, registry).valid)
      room.width = Config.WIDTH - 1
      assert(not Template.validate(room, registry).valid)
      room.width = Config.WIDTH
      table.remove(room.layout)
      assert(Template.validate(room, registry).errors[1].code == "wrong_row_count")
      room = valid_template("room.dungeon.standard.fixture", { { side = "north", offset = 5 } })
      room.layout[2] = room.layout[2]:sub(1, -2)
      assert(Template.validate(room, registry).errors[1].code == "wrong_row_width")
      room = valid_template("room.dungeon.standard.fixture", { { side = "north", offset = 5 } })
      room.layout[1] = "?" .. room.layout[1]:sub(2)
      local glyph_errors = Template.validate(room, registry).errors
      assert(glyph_errors[1].code == "unknown_glyph")
      room = valid_template("room.dungeon.standard.fixture", { { side = "north", offset = 5 } })
      room.legend["."] = "material.missing"
      assert(Template.validate(room, registry).errors[1].code == "unknown_material")
      room = valid_template("room.dungeon.standard.fixture", { { side = "north", offset = 5 } })
      set_glyph(room, 5, 10, "#")
      assert(Template.validate(room, registry).errors[1].code == "solid_connector")
      room = valid_template("room.dungeon.standard.fixture", { { side = "north", offset = 5 }, { side = "north", offset = 5 } })
      assert(Template.validate(room, registry).errors[1].code == "duplicate_connector")
      room = valid_template("room.dungeon.standard.fixture", {})
      room.connectors = { { side = "interior", offset = 5 } }
      assert(Template.validate(room, registry).errors[1].code == "invalid_connector_side")
    end,
  },
  {
    name = "room validation requires one connected passable area",
    run = function()
      local room = valid_template("room.dungeon.standard.disconnected", {})
      for y = 1, Config.HEIGHT - 2 do set_glyph(room, 5, y, "#") end
      local validation = Template.validate(room, ContentRegistry.load())
      assert(not validation.valid)
      local found = false
      for _, error in ipairs(validation.errors) do if error.code == "disconnected_passable_area" then found = true end end
      assert(found)
    end,
  },
  {
    name = "room rotation transforms layout connectors and returns after four turns",
    run = function()
      local original = valid_template("room.dungeon.standard.rotate", { { side = "north", offset = 5 }, { side = "east", offset = 5 } })
      set_glyph(original, 2, 3, "#")
      local once = Template.rotate(original, 1)
      assert(Template.pattern_key(once.connectors) == "north+west")
      local x, y = Template.connector_position(once, once.connectors[1])
      assert(Template.glyph_at(once, x, y) == ".")
      local restored = Template.rotate(original, 4)
      assert(Json.encode(restored.layout) == Json.encode(original.layout))
      assert(Template.pattern_key(restored.connectors) == Template.pattern_key(original.connectors))
    end,
  },
  {
    name = "dungeon room graphs and template selections are deterministic but vary by seed",
    run = function()
      local start, rooms = Grid.cell(40, 25), assert(RoomRegistry.load())
      local _, first = assert(DungeonRooms.generate(start, Rng.new(87101), { room_registry = rooms }))
      local _, second = assert(DungeonRooms.generate(start, Rng.new(87101), { room_registry = rooms }))
      assert(encode(first.rooms) == encode(second.rooms) and encode(first.graph_edges) == encode(second.graph_edges))
      local _, other = assert(DungeonRooms.generate(start, Rng.new(87102), { room_registry = rooms }))
      assert(encode(first.rooms) ~= encode(other.rooms) or encode(first.graph_edges) ~= encode(other.graph_edges))
      assert(#first.rooms == Config.ROOM_COUNT and first.player_spawn.x == 40 and first.player_spawn.y == 25)
      local looped = false
      for seed = 1, 32 do
        local _, metadata = assert(DungeonRooms.generate(start, Rng.new(seed), { room_registry = rooms }))
        if #metadata.graph_edges > Config.ROOM_COUNT - 1 then looped = true end
      end
      assert(looped)
    end,
  },
  {
    name = "assembled room connectors stitch exactly and leave unoccupied chunk space solid",
    run = function()
      local layout, metadata = assert(DungeonRooms.generate(Grid.cell(40, 25), Rng.new(87110)))
      for _, edge in ipairs(metadata.graph_edges) do
        local from = assert(room_by_slot(metadata, edge.from.x, edge.from.y))
        local to = assert(room_by_slot(metadata, edge.to.x, edge.to.y))
        local forward = assert(connector(from, edge.side))
        local opposite = edge.side == "north" and "south" or edge.side == "east" and "west"
          or edge.side == "south" and "north" or "east"
        local reverse = assert(connector(to, opposite))
        assert(layout[Grid.key(forward.x, forward.y)] and layout[Grid.key(reverse.x, reverse.y)])
        assert(math.abs(forward.x - reverse.x) + math.abs(forward.y - reverse.y) == 1)
      end
      assert(not layout[Grid.key(0, 0)])
    end,
  },
  {
    name = "template dungeon keeps spawn objectives and post-terrain placement legal",
    run = function()
      for seed = 1, 16 do
        local floor = assert(InspectionFloor.generate({ stage = "dungeon", seed = seed }))
        local report = Analysis.analyze(floor.world, { seed = seed, stage = floor.stage, terrain = floor.terrain,
          state = floor.state, session = floor.session, provenance = floor.provenance })
        assert(report.valid, report.errors[1] and report.errors[1].code)
        assert(floor.world:is_passable(floor.state.player.x, floor.state.player.y))
      end
    end,
  },
  {
    name = "inspector room overlay data exposes cell template provenance and deterministic batch usage",
    run = function()
      local inspector = Inspector.new({ stage = "dungeon", seed = 87120 })
      local player = inspector.floor.state.player
      local detail = assert(inspector:inspect_at(player.x, player.y))
      assert(detail.provenance.room and detail.provenance.room.template_id and detail.provenance.room.slot)
      local batch = assert(Batch.run({ stage = "dungeon", seed = 87120, count = 4 }))
      assert(batch.summary.statistics.room_count.average == Config.ROOM_COUNT)
      local total = 0
      for _, count in pairs(batch.summary.template_usage) do total = total + count end
      assert(total == Config.ROOM_COUNT * 4)
    end,
  },
  {
    name = "room editor model paints validates safely saves stable JSON and stays isolated from active saves",
    run = function()
      local active_save = SaveStore.memory()
      assert(active_save:write("preserve-active-run"))
      local store = corpus_memory()
      local editor = assert(EditorModel.new({ store = store }))
      local source_id = editor:list()[1].id
      assert(editor:open(source_id))
      assert(editor:paint(1, 1, "#"))
      local _, unsaved = editor:open(editor:list()[2].id)
      assert(unsaved.code == "unsaved_changes")
      assert(editor:confirm_discard())
      local created = assert(editor:new_template("room.dungeon.standard.editor_fixture"))
      assert(created)
      assert(editor:paint(5, Config.HEIGHT - 1, "."))
      assert(editor:toggle_connector("north", 5).applied)
      assert(editor:validation().valid)
      local saved = assert(editor:save())
      assert(saved.filename == "standard_editor_fixture.room.json")
      local reloaded = assert(RoomRegistry.load({ store = store }))
      assert(reloaded:get("room.dungeon.standard.editor_fixture"))
      assert((assert(store:read(saved.filename))):find("\n", 1, true))
      assert(active_save:read() == "preserve-active-run")
      assert(not store:write("../escape.room.json", "{}"))
    end,
  },
  {
    name = "room editor refuses invalid production saves",
    run = function()
      local editor = assert(EditorModel.new({ store = corpus_memory() }))
      assert(editor:new_template("room.dungeon.standard.invalid_save"))
      assert(editor:set_metadata("weight", 0))
      local _, failure = editor:save()
      assert(failure.code == "invalid_template")
    end,
  },
}
