-- Shared v1 authored-dungeon room contract.  Keeping these values together
-- makes the loader, editor, assembler, and diagnostics agree on a single
-- fixed-size corpus rather than carrying hidden dimensions in each system.
return {
  FORMAT = "roag.room_template",
  VERSION = 1,
  BIOME = "dungeon",
  DIRECTORY = "content/rooms/dungeon",
  WIDTH = 10,
  HEIGHT = 10,
  GRID_WIDTH = 6,
  GRID_HEIGHT = 4,
  ROOM_COUNT = 11,
  ORIGIN_X = 10,
  ORIGIN_Y = 5,
  TAGS = {
    entrance = true,
    standard = true,
    corridor = true,
    junction = true,
    arena = true,
    dead_end = true,
  },
  SIDES = { north = true, east = true, south = true, west = true },
  SIDE_ORDER = { "north", "east", "south", "west" },
  -- Every non-empty cardinal neighbour pattern can be produced by the v1
  -- graph grammar. The corpus validator checks candidates after rotation.
  REQUIRED_PATTERNS = {
    { "north" }, { "east" }, { "south" }, { "west" },
    { "north", "east" }, { "north", "south" }, { "north", "west" },
    { "east", "south" }, { "east", "west" }, { "south", "west" },
    { "north", "east", "south" }, { "north", "east", "west" },
    { "north", "south", "west" }, { "east", "south", "west" },
    { "north", "east", "south", "west" },
  },
  PALETTE = {
    ["#"] = "material.structure.masonry",
    ["."] = "material.terrain.air",
  },
}
