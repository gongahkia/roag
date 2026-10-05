local Assets = require("src.rendering.assets")
local App = require("src.app.app")
local SaveStore = require("src.persistence.save_store")
local Renderer = require("src.rendering.renderer")
local Enemies = require("content.enemies.legacy")
local WorldObjects = require("content.world_objects.legacy")

return {
  {
    name = "VIS-01 Loveable Rogue metadata is bounded, complete, and source-owned",
    run = function()
      local metadata = assert(Assets.load_metadata())
      assert(metadata.schema_version == 1 and metadata.atlas.width == 256 and metadata.atlas.height == 256)
      assert(Assets.validate_metadata(metadata))
      local broken = { schema_version = 2, atlas = { image = "x", width = 1, height = 1 }, sprites = {}, font = {}, bindings = {} }
      local valid, errors = Assets.validate_metadata(broken)
      assert(not valid and #errors > 0)
    end,
  },
  {
    name = "VIS-01 every production object role resolves to a bounded Loveable Rogue sprite",
    run = function()
      local assets = Assets.new()
      for _, definition in ipairs(WorldObjects) do
        local sprite = assets:sprite_for_object(definition, nil)
        assert(assets.metadata.sprites[sprite], "missing object sprite for " .. definition.id)
      end
      assert(assets:sprite_for_object({ interaction_role = "door" }, { interaction_role = "door", door_state = "open" }) == "object.door_open")
      assert(assets:sprite_for_object({ interaction_role = "door" }, { interaction_role = "door", door_state = "closed" }) == "object.door_closed")
    end,
  },
  {
    name = "VIS-01 every active Expedition class and production enemy kind has a Loveable Rogue binding",
    run = function()
      local assets = Assets.new()
      local state = { expedition = {}, player = {} }
      for _, character_id in ipairs({ "expedition.gunner", "expedition.bruiser", "expedition.conductor", "expedition.demolitionist" }) do
        state.expedition.character_id = character_id
        assert(assets:sprite_for_actor(state.player, state):match("^player%."))
      end
      for _, enemy in ipairs(Enemies) do
        assert(assets:sprite_for_actor(enemy, state), "missing binding for " .. enemy.id)
      end
      assert(assets:sprite_for_actor({ boss = true }, state) == "boss.default")
      assert(assets:sprite_for_actor({ kind = "bullet" }, state) == "projectile.normal")
      assert(assets:sprite_for_actor({ kind = "bomb" }, state) == "projectile.explosive")
    end,
  },
  {
    name = "VIS-01 runtime has one Loveable Rogue atlas boundary and no art-pack selection state",
    run = function()
      local app = App.new({ seed = 450001, save_store = SaveStore.memory(), meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
      assert(app.assets.metadata.id == "presentation.loveable_rogue")
      assert(app.assets.available_art_packs == nil and app.assets.select_art_pack == nil)
      local renderer = Renderer.new(app.assets)
      assert(renderer.assets == app.assets)
    end,
  },
  {
    name = "VIS-01 bitmap font has deterministic source glyph fallback and integer metrics",
    run = function()
      local assets = Assets.new()
      assert(assets:measure_text("ABC", 1) == 24)
      assert(assets:measure_text("ABC", 1.7) == 48)
      local metadata = assets.metadata
      assert(metadata.font.rows.white_upper.characters:find("A", 1, true))
      assert(metadata.font.rows.white_symbols.characters:find("?", 1, true))
    end,
  },
  {
    name = "VIS-01 runtime atlas loads nearest-neighbour quads without a shape fallback",
    run = function()
      local previous_love, filters, draws = love, {}, 0
      love = { graphics = {
        setDefaultFilter = function(minimum, maximum) filters[#filters + 1] = { minimum, maximum } end,
        newImage = function(path)
          local source = assert(io.open(path, "rb"), "missing runtime atlas " .. path); source:close()
          return { setFilter = function(_, minimum, maximum) filters[#filters + 1] = { minimum, maximum } end,
            getDimensions = function() return 256, 256 end }
        end,
        newQuad = function(...) return { ... } end,
        setColor = function() end,
        draw = function() draws = draws + 1 end,
      } }
      local ok, reason = xpcall(function()
        local assets = Assets.new(); assert(assets:load())
        assets:draw_sprite("terrain.floor", 0, 0, 16)
        assets:draw_text("ROAG?", 0, 0, 1, { 1, 1, 1 })
      end, debug.traceback)
      love = previous_love
      assert(ok, reason)
      assert(filters[1][1] == "nearest" and filters[1][2] == "nearest")
      assert(draws >= 6)
    end,
  },
}
