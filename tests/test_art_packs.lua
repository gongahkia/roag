local ArtPacks = require("src.rendering.art_packs")
local Assets = require("src.rendering.assets")
local ArtPackConfig = require("src.presentation.art_pack_config")
local ArtPackCatalog = require("src.presentation.art_pack_catalog")
local App = require("src.app.app")
local SaveStore = require("src.persistence.save_store")
local Renderer = require("src.rendering.renderer")
local WorkbenchExport = require("src.presentation.art_workbench_export")

return {
  {
    name = "ROAG reads externalized presentation data while title no longer exposes art authoring",
    run = function()
      local configured = assert(ArtPackConfig.load())
      local app = App.new({ seed = 450001, save_store = SaveStore.memory(), meta_store = SaveStore.memory(), archive_store = SaveStore.memory() })
      assert(app.assets.art_pack_id == configured.art_pack_id)
      for _, option in ipairs(app:title_options()) do
        assert(option.id ~= "art_packs" and option.name ~= "ART PACKS")
      end
      assert(app:title_options()[1].id == "new_run")
    end,
  },
  {
    name = "source-controlled art-pack catalog exactly mirrors renderer pack identifiers",
    run = function()
      local catalog = assert(ArtPackCatalog.load())
      assert(#catalog.art_packs == #ArtPacks.list())
      local partial, failure = ArtPackCatalog.load({ payload = '{"format":"roag.presentation_art_pack_catalog","version":1,"art_packs":[]}' })
      assert(not partial and failure.code == "incomplete_presentation_art_pack_catalog")
    end,
  },
  {
    name = "art workbench exports a presentation-only sibling-project contract",
    run = function()
      assert(WorkbenchExport.validate())
      local manifest = WorkbenchExport.manifest()
      assert(manifest.project_name == "unpolished-bees" and manifest.integration.selection_format == "roag.presentation_art_pack")
      assert(manifest.integration.settings_path == "content/presentation/art_pack.json")
      assert(#manifest.art_packs == #ArtPacks.list())
      for _, entry in ipairs(WorkbenchExport.entries) do
        assert(not entry.source:match("^src/simulation/") and not entry.source:match("^src/persistence/"))
      end
    end,
  },
  {
    name = "art-pack catalog contains every bundled source with complete bounded role maps",
    run = function()
      local packs, roles = ArtPacks.list(), ArtPacks.roles()
      assert(#packs == 9, "expected the original ROAG pack plus eight bundled alternatives")
      local seen = {}
      for _, pack in ipairs(packs) do
        assert(not seen[pack.id], "duplicate art pack " .. pack.id)
        seen[pack.id] = true
        assert(type(pack.display_name) == "string" and pack.display_name ~= "")
        assert(type(pack.license) == "string" and pack.license ~= "")
        for _, sheet in pairs(pack.sheets) do
          local file = assert(io.open(sheet.path, "rb"), "missing bundled sheet " .. sheet.path)
          file:close()
        end
        for _, role in ipairs(roles) do
          local sprite = assert(pack.sprites[role], "missing " .. role .. " mapping for " .. pack.id)
          local sheet = assert(pack.sheets[sprite.sheet or "main"], "unknown mapping sheet for " .. role)
          assert(sprite[1] >= 1 and sprite[1] <= sheet.columns and sprite[2] >= 1 and sprite[2] <= sheet.rows,
            "out-of-range mapping for " .. role .. " in " .. pack.id)
        end
        for terrain_kind, tile in pairs(pack.terrain or {}) do
          if tile.role then
            assert(pack.id == ArtPacks.DEFAULT_ID, "only the original pack may reference an editable terrain role")
            assert(tile.role:match("^wall_"), "unexpected editable terrain role " .. tile.role)
          else
            local sheet = assert(pack.sheets[tile.sheet or "main"], "unknown terrain sheet for " .. terrain_kind)
            assert(tile[1] >= 1 and tile[1] <= sheet.columns and tile[2] >= 1 and tile[2] <= sheet.rows,
              "out-of-range terrain mapping for " .. terrain_kind .. " in " .. pack.id)
          end
        end
      end
      assert(seen["art_pack.loveable_rogue"] and seen["art_pack.dawnlike"])
      assert(seen["art_pack.kenney_micro_roguelike"] and seen["art_pack.kenney_roguelike_characters"])
    end,
  },
  {
    name = "directional one-bit wall roles remain opt-in and terrain selection follows passable neighbours",
    run = function()
      local renderer = Renderer.new({})
      local open = {}
      local world = {
        terrain_is_passable = function(_, x, y) return open[x .. ":" .. y] == true end,
      }
      local function face(key, expected)
        open = { [key] = true }
        assert(renderer:wall_terrain_kind(world, 10, 10) == expected)
      end
      face("10:11", "wall_up")
      face("10:9", "wall_down")
      face("9:10", "wall_left")
      face("11:10", "wall_right")
      open = {}
      assert(renderer:wall_terrain_kind(world, 10, 10) == "wall")

      local assets = Assets.new()
      assert(not assets.sprites.wall_up, "wall faces start unassigned so existing presentation is preserved")
      assets.sprites.wall_up = { 1, 1, sheet = "main" }
      assert(assets.art_pack.terrain.wall_up.role == "wall_up")
    end,
  },
  {
    name = "source-controlled art-pack selection validates outside persistence and rejects unknown packs",
    run = function()
      local selected = assert(ArtPackConfig.load({ payload = '{"format":"roag.presentation_art_pack","version":1,"art_pack_id":"art_pack.dawnlike"}' }))
      assert(selected.art_pack_id == "art_pack.dawnlike")
      local invalid, error_data = ArtPackConfig.decode('{"format":"roag.presentation_art_pack","version":1,"art_pack_id":"art_pack.missing"}')
      assert(not invalid and error_data.code == "unknown_presentation_art_pack")
      local payload = assert(ArtPackConfig.encode(selected))
      assert(assert(ArtPackConfig.decode(payload)).art_pack_id == "art_pack.dawnlike")
    end,
  },
  {
    name = "art assets retain default sprite-editor isolation and headless swapping",
    run = function()
      local assets = Assets.new()
      assert(assets:select_art_pack("art_pack.kenney_roguelike_rpg_pack"))
      assert(assets.art_pack_id == "art_pack.kenney_roguelike_rpg_pack")
      assert(assets:current_art_pack().terrain.floor and assets:current_art_pack().terrain.wall)
      assert(not assets:refresh_sprite_mappings(), "the 1-bit sidecar cannot override a third-party pack")
      local reset, error_data = assets:reset_sprite("player")
      assert(not reset and error_data.code == "not_editable")
      assert(assets:select_art_pack(ArtPacks.DEFAULT_ID))
      assert(assets:reset_sprite("player"))
    end,
  },
  {
    name = "every art pack builds its declared sheets and terrain mappings through the shared renderer boundary",
    run = function()
      local prior_love = love
      local loaded_paths = {}
      love = { graphics = {
        setDefaultFilter = function() end,
        newFont = function() return {} end,
        setFont = function() end,
        newImage = function(path)
          local source = assert(io.open(path, "rb"), "missing renderer source " .. path)
          source:close()
          loaded_paths[path] = true
          return { getDimensions = function() return 4096, 4096 end }
        end,
        newQuad = function() return {} end,
        setColor = function() end,
        draw = function() end,
      } }
      local ok, reason = xpcall(function()
        for _, pack in ipairs(ArtPacks.list()) do
          local assets = Assets.new({ art_pack_id = pack.id })
          assert(assets:load())
          assert(assets:draw_sprite("player", 0, 0, 16))
          for kind, mapping in pairs(pack.terrain or {}) do
            local drawn = assets:draw_terrain(kind, 0, 0, 16)
            if mapping.role then
              assert(not drawn, "unassigned editable terrain role should preserve procedural fallback")
              assets.sprites[mapping.role] = { 1, 1, sheet = "main" }
              assert(assets:draw_terrain(kind, 0, 0, 16))
            else
              assert(drawn)
            end
          end
        end
      end, debug.traceback)
      love = prior_love
      assert(ok, reason)
      assert(next(loaded_paths), "no art-pack sheets were loaded")
    end,
  },
}
