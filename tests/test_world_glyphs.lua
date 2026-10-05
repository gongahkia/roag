local Assets = require("src.rendering.assets")
local WorldGlyphs = require("src.rendering.world_glyphs")

return {
  {
    name = "shape-first world glyphs classify physical and interactive object roles without simulation metadata",
    run = function()
      assert(WorldGlyphs.object_kind({ render_style = "old_growth_tree" }) == "tree")
      assert(WorldGlyphs.object_kind({ render_style = "granite_boulder" }) == "rock")
      assert(WorldGlyphs.object_kind({ render_style = "cable_trunk" }) == "cable")
      assert(WorldGlyphs.object_kind({ interaction_role = "door" }) == "door")
      assert(WorldGlyphs.object_kind({ interaction_role = "reinforcement" }) == "reinforcement")
      assert(WorldGlyphs.object_kind({ render_style = "crate" }) == "crate")
    end,
  },
  {
    name = "shape-first runtime asset load does not open a legacy texture sheet",
    run = function()
      local previous_love, opened_image = love, false
      love = { graphics = {
        setDefaultFilter = function() end,
        newFont = function() return {} end,
        setFont = function() end,
        newImage = function() opened_image = true; error("shape-first load must not open a texture sheet") end,
      } }
      local ok, reason = xpcall(function() assert(Assets.new():load()) end, debug.traceback)
      love = previous_love
      assert(ok, reason)
      assert(not opened_image)
    end,
  },
  {
    name = "shape-first world glyph renderer covers terrain effects and interactive objects without texture calls",
    run = function()
      local previous_love, calls = love, 0
      local graphics = {}
      for _, name in ipairs({ "setColor", "rectangle", "line", "ellipse", "setLineWidth", "arc", "circle", "polygon" }) do
        graphics[name] = function() calls = calls + 1 end
      end
      love = { graphics = graphics }
      local ok, reason = xpcall(function()
        WorldGlyphs.draw_floor(0, 0, 24, { 0.1, 0.2, 0.3 }, 2, 3)
        WorldGlyphs.draw_wall("wall_top_left", 0, 0, 24, { 0.2, 0.2, 0.25 })
        WorldGlyphs.draw_liquid(0, 0, 24, 0.7, { 0.3, 0.8, 1 })
        WorldGlyphs.draw_gas(0, 0, 24, 0.6, 0.04)
        WorldGlyphs.draw_spikes(0, 0, 24, { 1, 0.3, 0.2 })
        WorldGlyphs.draw_fire(0, 0, 24, 0.05)
        WorldGlyphs.draw_object({ interaction_role = "door" }, {}, 0, 0, 24, { 1, 0.8, 0.3 })
        WorldGlyphs.draw_electric_arc(0, 0, 24, 0.8)
      end, debug.traceback)
      love = previous_love
      assert(ok, reason)
      assert(calls > 20)
    end,
  },
}
