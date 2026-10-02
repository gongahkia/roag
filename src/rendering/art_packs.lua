-- Declarative presentation-only art-pack catalog.  Gameplay never reads this
-- module: every entry maps the stable renderer roles to source art while the
-- simulation continues to own terrain, actors, objects, and saves.
local BaseSprites = require("sprite_map")

local ArtPacks = {
  DEFAULT_ID = "art_pack.roag_kenney_1bit",
}

local function sorted_roles()
  local roles = {}
  for role in pairs(BaseSprites) do roles[#roles + 1] = role end
  table.sort(roles)
  return roles
end

local ROLES = sorted_roles()

local function clone_tile(tile)
  return { tile[1], tile[2], sheet = tile.sheet }
end

local function clone_sprites(source)
  local result = {}
  for role, tile in pairs(source or {}) do result[role] = clone_tile(tile) end
  return result
end

local function base_sprites()
  return clone_sprites(BaseSprites)
end

-- Starter role maps deliberately use a compact occupied region from each
-- source sheet.  Some supplied packs are environment-only packs; the map is
-- still complete so a pack can be used immediately, while the standalone
-- sprite editor remains the place to tune the original ROAG 1-bit mapping.
local function grid_sprites(columns, first_column, first_row, sheet)
  local result = {}
  for index, role in ipairs(ROLES) do
    local zero = index - 1
    result[role] = {
      (first_column or 1) + (zero % columns),
      (first_row or 1) + math.floor(zero / columns),
      sheet = sheet or "main",
    }
  end
  return result
end

local function dawnlike_sprites()
  local result = grid_sprites(8, 1, 1, "humanoids")
  local function use(role, sheet, column, row)
    result[role] = { column, row, sheet = sheet }
  end
  use("player", "player", 1, 1)
  use("target", "traps", 1, 1)
  use("ammo", "ammo", 1, 1)
  use("torch", "lights", 1, 1)
  use("door", "doors", 1, 1)
  use("bullet", "effects", 1, 1)
  use("bomb", "rocks", 1, 1)
  use("flare", "lights", 2, 1)
  use("wolf", "quadrupeds", 1, 1)
  use("bomber", "pests", 1, 1)
  use("necromancer", "undead", 1, 1)
  use("cultist", "humanoids", 2, 1)
  use("ripper", "reptiles", 1, 1)
  use("skirmisher", "humanoids", 3, 1)
  use("conductor", "elementals", 1, 1)
  use("bulwark", "humanoids", 4, 1)
  use("reclaimer", "humanoids", 5, 1)
  use("gunner_elite", "humanoids", 6, 1)
  use("shock_bruiser", "elementals", 2, 1)
  use("volatile_heavy", "pests", 2, 1)
  use("arc_cutter", "humanoids", 7, 1)
  use("maintenance_heavy", "humanoids", 8, 1)
  use("reactor_suppressor", "humanoids", 1, 2)
  use("arc_warden", "undead", 2, 1)
  use("boss", "undead", 3, 1)
  return result
end

local function pack(id, display_name, source_url, license, credit, sheets, sprites, description, terrain)
  return {
    id = id,
    display_name = display_name,
    source_url = source_url,
    license = license,
    credit = credit,
    sheets = sheets,
    sprites = sprites,
    description = description,
    terrain = terrain,
  }
end

local CATALOG = {
  pack(
    "art_pack.roag_kenney_1bit", "ROAG 1-BIT (DEFAULT)", "assets/kenney/License.txt", "CC0", "Kenney",
    { main = { path = "assets/kenney/Tilesheet/colored-transparent_packed.png", tile_width = 16, tile_height = 16, columns = 49, rows = 22 } },
    base_sprites(), "Original ROAG mapping; editable with the standalone Sprite Editor.",
    {
      floor = { role = "ground" },
      wall_left = { role = "wall_left" }, wall_right = { role = "wall_right" },
      wall_up = { role = "wall_up" }, wall_down = { role = "wall_down" },
      wall_top_left = { role = "wall_top_left" }, wall_top_right = { role = "wall_top_right" },
      wall_bottom_left = { role = "wall_bottom_left" }, wall_bottom_right = { role = "wall_bottom_right" },
      wall_center = { role = "wall_center" },
    }
  ),
  pack(
    "art_pack.loveable_rogue", "LOVEABLE ROGUE", "https://opengameart.org/content/loveable-rogue", "CC0", "surt / OpenGameArt",
    { main = { path = "assets/art_packs/loveable_rogue.png", tile_width = 16, tile_height = 16, columns = 64, rows = 64 } },
    grid_sprites(8, 1, 10), "Classic compact roguelike sheet.",
    { floor = { 1, 8 }, wall = { 1, 7 } }
  ),
  pack(
    "art_pack.dawnlike", "DAWNLIKE 16X16", "https://opengameart.org/content/dawnlike-16x16-universal-rogue-like-tileset-v181", "CC-BY 4.0", "DragonDePlatino and DawnBringer",
    {
      player = { path = "assets/art_packs/dawnlike/Characters/Player0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 15 },
      humanoids = { path = "assets/art_packs/dawnlike/Characters/Humanoid0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 27 },
      undead = { path = "assets/art_packs/dawnlike/Characters/Undead0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 10 },
      quadrupeds = { path = "assets/art_packs/dawnlike/Characters/Quadraped0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 12 },
      reptiles = { path = "assets/art_packs/dawnlike/Characters/Reptile0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 15 },
      pests = { path = "assets/art_packs/dawnlike/Characters/Pest0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 11 },
      elementals = { path = "assets/art_packs/dawnlike/Characters/Elemental0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 11 },
      traps = { path = "assets/art_packs/dawnlike/Objects/Trap0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 5 },
      doors = { path = "assets/art_packs/dawnlike/Objects/Door0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 6 },
      effects = { path = "assets/art_packs/dawnlike/Objects/Effect0.png", tile_width = 16, tile_height = 16, columns = 8, rows = 26 },
      ammo = { path = "assets/art_packs/dawnlike/Items/Ammo.png", tile_width = 16, tile_height = 16, columns = 8, rows = 6 },
      lights = { path = "assets/art_packs/dawnlike/Items/Light.png", tile_width = 16, tile_height = 16, columns = 8, rows = 1 },
      rocks = { path = "assets/art_packs/dawnlike/Items/Rock.png", tile_width = 16, tile_height = 16, columns = 8, rows = 2 },
      floor = { path = "assets/art_packs/dawnlike/Objects/Floor.png", tile_width = 16, tile_height = 16, columns = 21, rows = 39 },
      walls = { path = "assets/art_packs/dawnlike/Objects/Wall.png", tile_width = 16, tile_height = 16, columns = 20, rows = 51 },
    },
    dawnlike_sprites(), "Universal color roguelike tiles. Attribution is required; see assets/art_packs/ATTRIBUTION.md.",
    { floor = { 1, 4, sheet = "floor" }, wall = { 1, 3, sheet = "walls" } }
  ),
  pack(
    "art_pack.kenney_micro_roguelike", "KENNEY MICRO ROGUELIKE", "https://kenney-assets.itch.io/micro-roguelike", "CC0", "Kenney",
    { main = { path = "assets/art_packs/kenney_micro_roguelike/Tilemap/colored_tilemap_packed.png", tile_width = 8, tile_height = 8, columns = 16, rows = 10 } },
    grid_sprites(8, 1, 3), "Small 8x8 roguelike pack, scaled cleanly at runtime.",
    { floor = { 1, 3 }, wall = { 1, 1 } }
  ),
  pack(
    "art_pack.kenney_roguelike_indoors", "KENNEY ROGUELIKE INDOORS", "https://kenney.nl/assets/roguelike-indoors", "CC0", "Kenney",
    { main = { path = "assets/art_packs/kenney_roguelike_indoors/Tilesheets/roguelikeIndoor_transparent.png", tile_width = 16, tile_height = 16, spacing = 1, columns = 27, rows = 18 } },
    grid_sprites(9, 1, 4), "Indoor architecture and fixtures.",
    { floor = { 1, 12 }, wall = { 1, 1 } }
  ),
  pack(
    "art_pack.kenney_roguelike_modern_city", "KENNEY ROGUELIKE MODERN CITY", "https://kenney.nl/assets/roguelike-modern-city", "CC0", "Kenney",
    { main = { path = "assets/art_packs/kenney_roguelike_modern_city/Tilemap/tilemap_packed.png", tile_width = 16, tile_height = 16, columns = 37, rows = 28 } },
    grid_sprites(9, 1, 10), "Dense urban and industrial tile language.",
    { floor = { 1, 1 }, wall = { 2, 1 } }
  ),
  pack(
    "art_pack.kenney_roguelike_caves_dungeons", "KENNEY CAVES & DUNGEONS", "https://kenney.nl/assets/roguelike-caves-dungeons", "CC0", "Kenney",
    { main = { path = "assets/art_packs/kenney_roguelike_caves_dungeons/Spritesheet/roguelikeDungeon_transparent.png", tile_width = 16, tile_height = 16, spacing = 1, columns = 29, rows = 18 } },
    grid_sprites(9, 1, 7), "Cavern, water, stone, and dungeon architecture.",
    { floor = { 8, 8 }, wall = { 9, 2 } }
  ),
  pack(
    "art_pack.kenney_roguelike_rpg_pack", "KENNEY ROGUELIKE RPG PACK", "https://kenney.nl/assets/roguelike-rpg-pack", "CC0", "Kenney and Lynn Evers",
    { main = { path = "assets/art_packs/kenney_roguelike_rpg_pack/Spritesheet/roguelikeSheet_transparent.png", tile_width = 16, tile_height = 16, spacing = 1, columns = 57, rows = 31 } },
    grid_sprites(9, 31, 12), "Broad fantasy terrain, prop, and character sheet.",
    { floor = { 7, 19 }, wall = { 1, 24 } }
  ),
  pack(
    "art_pack.kenney_roguelike_characters", "KENNEY ROGUELIKE CHARACTERS", "https://kenney.nl/assets/roguelike-characters", "CC0", "Kenney",
    { main = { path = "assets/art_packs/kenney_roguelike_characters/Spritesheet/roguelikeChar_transparent.png", tile_width = 16, tile_height = 16, spacing = 1, columns = 54, rows = 12 } },
    grid_sprites(9, 1, 4), "Character-focused sheet with color-coded creature families."
  ),
}

local BY_ID = {}
for _, definition in ipairs(CATALOG) do BY_ID[definition.id] = definition end

function ArtPacks.roles()
  local result = {}
  for index, role in ipairs(ROLES) do result[index] = role end
  return result
end

function ArtPacks.list()
  local result = {}
  for index, definition in ipairs(CATALOG) do result[index] = definition end
  return result
end

function ArtPacks.get(id)
  return BY_ID[id]
end

function ArtPacks.has(id)
  return BY_ID[id] ~= nil
end

function ArtPacks.clone_sprites(sprites)
  return clone_sprites(sprites)
end

return ArtPacks
