local ScreenManager = require("src.ui.screen_manager")
local SpriteModel = require("sprite_editor.model")
local PresentationFlow = require("src.presentation.presentation_flow")

return {
  {
    name = "presentation title flow is validated data with safe action targets and conditional continuation",
    run = function()
      local source = assert(io.open("content/presentation/flow.json", "rb"))
      local payload = source:read("*a")
      source:close()
      local flow = assert(PresentationFlow.load({ payload = payload }))
      -- The title is now Expedition-first: Sandbox is optional and the
      -- preserved Research/Fallen compatibility surfaces are no longer modes.
      assert(#flow:available({ continue_available = false }) == 3)
      assert(#flow:available({ continue_available = true }) == 4)
      for _, action in ipairs(flow:available({ continue_available = true })) do
        assert(action.id ~= "research" and action.id ~= "fallen")
      end
      local invalid, failure = PresentationFlow.decode('{"format":"roag.presentation_flow","version":1,"home":"title","title_actions":[{"id":"new_run","label":"NEW","description":"x","target":"game"}]}')
      assert(not invalid and failure.code == "invalid_presentation_transition")
      local restored = assert(PresentationFlow.decode(assert(PresentationFlow.encode(flow:to_data()))))
      assert(restored.title_actions[1].id == "expedition")
    end,
  },
  {
    name = "screen definitions are validated serializable data with safe fallback semantics",
    run = function()
      local source = assert(io.open("content/screens/legacy.json", "rb"))
      local payload = source:read("*a")
      source:close()
      local manager = assert(ScreenManager.load({ payload = payload }))
      assert(manager:screen("title").title == "ROAG")
      assert(manager:screen("route").layout == "route")
      local encoded = assert(ScreenManager.encode(manager:to_data()))
      local restored = assert(ScreenManager.load({ payload = encoded }))
      assert(restored:text("research", "footer"):find("PURCHASE", 1, true))
      local invalid, failure = ScreenManager.load({ payload = '{"format":"roag.screen_definitions","version":1,"screens":[{"id":"title","layout":"unknown","title":"x","subtitle":"x","footer":"x","accent":"cyan"}]}' })
      assert(not invalid and failure.code == "invalid_screen_layout")
      assert(ScreenManager.fallback():screen("title"))
    end,
  },
  {
    name = "sprite workbench model filters roles and preserves undoable physical mapping edits",
    run = function()
      local model = SpriteModel.new()
      assert(#model:filtered("Reactor", "") == 4)
      assert(#model:filtered("All", "wall") == 9)
      assert(#model:filtered("Terrain", "water") == 2)
      local original = assert(model:tile("player"))
      local changed = assert(model:assign("player", 1, 1))
      assert(changed.applied and model:tile("player")[1] == 1)
      assert(model:undo().applied and model:tile("player")[1] == original[1])
      assert(model:redo().applied and model:tile("player")[1] == 1)
      local forbidden, failure = model:clear("player")
      assert(not forbidden and failure.code == "required_role")
      assert(model:assign("wall_left", 2, 2).applied)
      assert(model:assign("wall_top_left", 3, 3).applied)
      assert(model:assign("spikes", 4, 4).applied)
      local wall_clear, wall_failure = model:clear("wall_left")
      assert(not wall_clear and wall_failure.code == "required_role")
      assert(model:adjust("player", "column", -99).mapping[1] == 1)
      assert(#model:roles_at(1, 1) >= 1)
    end,
  },
  {
    name = "sprite mapping JSON round trips known roles while rejecting invalid-only data",
    run = function()
      local model = SpriteModel.new()
      assert(model:assign("wall_up", 4, 5).applied)
      assert(model:assign("wall_center", 5, 6).applied)
      local restored = assert(SpriteModel.deserialize(model:serialize()))
      assert(restored.wall_up[1] == 4 and restored.wall_up[2] == 5)
      assert(restored.wall_center[1] == 5 and restored.wall_center[2] == 6)
      local missing, failure = SpriteModel.deserialize('{"sprites":{"player":{"column":99,"row":99}}}')
      assert(not missing and failure.code == "no_valid_mappings")
    end,
  },
}
