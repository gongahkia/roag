-- Shared authored-room contract for the Reactor Complex.  It deliberately
-- retains the v1 chunk grammar so rotation, editor behaviour, assembly, and
-- diagnostic provenance remain identical to the Dungeon corpus.
return {
  CORPUS_ID = "room_corpus.reactor",
  FORMAT = "roag.room_template",
  VERSION = 1,
  BIOME = "reactor",
  DIRECTORY = "content/rooms/reactor",
  WIDTH = 11,
  HEIGHT = 11,
  GRID_WIDTH = 6,
  GRID_HEIGHT = 3,
  ROOM_COUNT = 11,
  ORIGIN_X = 2,
  ORIGIN_Y = 9,
  TAGS = {
    entrance = true,
    standard = true,
    corridor = true,
    junction = true,
    maintenance = true,
    machinery = true,
    dead_end = true,
  },
  SIDES = { north = true, east = true, south = true, west = true },
  SIDE_ORDER = { "north", "east", "south", "west" },
  REQUIRED_PATTERNS = {
    { "north" }, { "east" }, { "south" }, { "west" },
    { "north", "east" }, { "north", "south" }, { "north", "west" },
    { "east", "south" }, { "east", "west" }, { "south", "west" },
    { "north", "east", "south" }, { "north", "east", "west" },
    { "north", "south", "west" }, { "east", "south", "west" },
    { "north", "east", "south", "west" },
  },
  PALETTE = {
    ["#"] = "material.structure.industrial_bulkhead",
    ["="] = "material.floor.conductive_metal",
    ["."] = "material.terrain.air",
  },
}
