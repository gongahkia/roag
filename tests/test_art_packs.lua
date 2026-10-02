local ArtPacks = require("src.rendering.art_packs")
local Assets = require("src.rendering.assets")
local ArtPackConfig = require("src.presentation.art_pack_config")
local ArtPackCatalog = require("src.presentation.art_pack_catalog")
local App = require("src.app.app")
local SaveStore = require("src.persistence.save_store")
local Renderer = require("src.rendering.renderer")
local WorkbenchExport = require("src.presentation.art_workbench_export")
local WorldObjects = require("content.world_objects.legacy")
local Enemies = require("content.enemies.legacy")
local Bosses = require("content.bosses.legacy")

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
    name = "every production world fixture resolves to a required art-pack sprite role",
    run = function()
      local roles = {}
      for _, role in ipairs(ArtPacks.roles()) do roles[role] = true end
      for _, object in ipairs(WorldObjects) do
        assert(roles[object.render_style], "missing required sprite role for " .. object.id)
      end
      for _, pack in ipairs(ArtPacks.list()) do
        for _, object in ipairs(WorldObjects) do
          assert(pack.sprites[object.render_style], "unassigned " .. object.render_style .. " in " .. pack.id)
        end
      end
    end,
  },
  {
    name = "every runtime actor, projectile, boss, and world fixture role is assigned in every art pack",
    run = function()
      local roles = {}
      for _, role in ipairs(ArtPacks.roles()) do roles[role] = true end
      local runtime_roles = {
        "player", "target", "ammo", "torch", "door", "bullet", "bomb", "flare",
      }
      for _, enemy in ipairs(Enemies) do runtime_roles[#runtime_roles + 1] = enemy.kind end
      for _, boss in ipairs(Bosses) do runtime_roles[#runtime_roles + 1] = boss.presentation.sprite_kind end
      for _, object in ipairs(WorldObjects) do runtime_roles[#runtime_roles + 1] = object.render_style end
      for _, role in ipairs(runtime_roles) do
        assert(roles[role], "renderer role is absent from the required sprite map: " .. role)
      end
      for _, pack in ipairs(ArtPacks.list()) do
        for _, role in ipairs(runtime_roles) do
          assert(pack.sprites[role], "renderer role is unassigned in " .. pack.id .. ": " .. role)
        end
      end
    end,
  },
  {
    name = "complete one-bit wall roles are required mappings and terrain selection follows passable neighbours",
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
      open = { ["10:11"] = true, ["9:10"] = true }
      assert(renderer:wall_terrain_kind(world, 10, 10) == "wall_top_left")
      open = { ["10:11"] = true, ["11:10"] = true }
      assert(renderer:wall_terrain_kind(world, 10, 10) == "wall_top_right")
      open = { ["10:9"] = true, ["9:10"] = true }
      assert(renderer:wall_terrain_kind(world, 10, 10) == "wall_bottom_left")
      open = { ["10:9"] = true, ["11:10"] = true }
      assert(renderer:wall_terrain_kind(world, 10, 10) == "wall_bottom_right")
      open = {}
      assert(renderer:wall_terrain_kind(world, 10, 10) == "wall_center")

      local assets = Assets.new()
      assert(assets.sprites.wall_up, "wall faces are required mapped terrain roles")
      assert(assets.art_pack.terrain.wall_up.role == "wall_up")
      assert(assets.sprites.wall_center, "closed walls are editor-mapped terrain roles")
      assert(assets.art_pack.terrain.wall_top_left.role == "wall_top_left")
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
      local loaded_paths, draws = {}, {}
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
        draw = function(_, _, x, y, rotation, scale_x, scale_y)
          draws[#draws + 1] = { x = x, y = y, rotation = rotation, scale_x = scale_x, scale_y = scale_y }
        end,
      } }
      local ok, reason = xpcall(function()
        for _, pack in ipairs(ArtPacks.list()) do
          local assets = Assets.new({ art_pack_id = pack.id })
          assert(assets:load())
          for _, role in ipairs(ArtPacks.roles()) do
            assert(assets:draw_sprite(role, 0, 0, 16), "required sprite role must draw directly: " .. role)
          end
          assert(assets:draw_sprite("player", 0, 0, 16, nil, {
            offset_x = 1, offset_y = -1, scale_x = 1.01, scale_y = 0.98,
          }))
          local transformed = draws[#draws]
          assert(transformed.x ~= 0 or transformed.y ~= 0, "sprite transform did not affect draw origin")
          assert(transformed.scale_x > 0 and transformed.scale_y > 0, "sprite transform produced invalid scale")
          for kind, mapping in pairs(pack.terrain or {}) do
            local drawn = assets:draw_terrain(kind, 0, 0, 16)
            assert(drawn, "every declared terrain role must resolve to source art")
          end
        end
      end, debug.traceback)
      love = prior_love
      assert(ok, reason)
      assert(next(loaded_paths), "no art-pack sheets were loaded")
    end,
  },
}
