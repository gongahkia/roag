local ArtPacks = require("src.rendering.art_packs")
local Assets = require("src.rendering.assets")
local ArtPackSettings = require("src.persistence.art_pack_settings")
local SaveStore = require("src.persistence.save_store")
local App = require("src.app.app")

local function title_option(app, name)
  for index, option in ipairs(app:title_options()) do if option.name == name then return index, option end end
end

return {
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
      end
      assert(seen["art_pack.loveable_rogue"] and seen["art_pack.dawnlike"])
      assert(seen["art_pack.kenney_micro_roguelike"] and seen["art_pack.kenney_roguelike_characters"])
    end,
  },
  {
    name = "art-pack preferences round trip separately and reject unknown packs",
    run = function()
      local store = SaveStore.memory()
      local settings = ArtPackSettings.new()
      settings.art_pack_id = "art_pack.dawnlike"
      assert(ArtPackSettings.save(settings, store))
      local loaded = assert(ArtPackSettings.load(store))
      assert(loaded.art_pack_id == "art_pack.dawnlike")
      local invalid, error_data = ArtPackSettings.decode('{"format":"roag.art_pack_settings","version":1,"settings":{"art_pack_id":"art_pack.missing"}}')
      assert(not invalid and error_data.code == "invalid_state")
    end,
  },
  {
    name = "art-pack selection persists as presentation state without altering active-run storage",
    run = function()
      local active, meta, archive, art = SaveStore.memory(), SaveStore.memory(), SaveStore.memory(), SaveStore.memory()
      local app = App.new({ seed = 450001, save_store = active, meta_store = meta, archive_store = archive, art_pack_store = art })
      local index = assert(title_option(app, "ART PACKS"))
      app.menu = index
      assert(app:activate_title_choice() and app.screen == "art_packs")
      assert(app:select_art_pack("art_pack.kenney_micro_roguelike"))
      assert(app.assets.art_pack_id == "art_pack.kenney_micro_roguelike")
      assert(not active:exists(), "visual preferences must not create an active run")
      local restored = App.new({ seed = 450002, save_store = active, meta_store = meta, archive_store = archive, art_pack_store = art })
      assert(restored.assets.art_pack_id == "art_pack.kenney_micro_roguelike")
      local prior = assert(art:read())
      local changed, error_data = restored:select_art_pack("art_pack.missing")
      assert(not changed and error_data.code == "unknown_art_pack")
      assert(assert(art:read()) == prior, "failed selection must not mutate preferences")
    end,
  },
  {
    name = "art assets retain default sprite-editor isolation and headless swapping",
    run = function()
      local assets = Assets.new()
      assert(assets:select_art_pack("art_pack.kenney_roguelike_rpg_pack"))
      assert(assets.art_pack_id == "art_pack.kenney_roguelike_rpg_pack")
      assert(not assets:refresh_sprite_mappings(), "the 1-bit sidecar cannot override a third-party pack")
      local reset, error_data = assets:reset_sprite("player")
      assert(not reset and error_data.code == "not_editable")
      assert(assets:select_art_pack(ArtPacks.DEFAULT_ID))
      assert(assets:reset_sprite("player"))
    end,
  },
}
