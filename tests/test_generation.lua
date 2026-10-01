local Generator = require("src.generation.map")
local Grid = require("src.world.grid")
local Rng = require("src.rng")

local function fingerprint(space)
  local parts = {}
  for x = 0, Grid.width - 1 do
    for y = 0, Grid.height - 1 do
      parts[#parts + 1] = space[Grid.key(x, y)] and "1" or "0"
    end
  end
  return table.concat(parts)
end

return {
  {
    name = "forest generation reproduces for a seed",
    run = function()
      local start = Grid.cell(40, 25)
      local first = Generator.generate("forest", start, Rng.new(17))
      local second = Generator.generate("forest", start, Rng.new(17))
      assert(fingerprint(first) == fingerprint(second))
    end,
  },
  {
    name = "forest generation differs for different seeds",
    run = function()
      local start = Grid.cell(40, 25)
      local first = Generator.generate("forest", start, Rng.new(17))
      local second = Generator.generate("forest", start, Rng.new(18))
      assert(fingerprint(first) ~= fingerprint(second))
    end,
  },
  {
    name = "generated worlds retain a legal player start",
    run = function()
      local start = Grid.cell(40, 25)
      for _, terrain in ipairs({ "forest", "cave", "dungeon", "reactor" }) do
        local world = Generator.generate(terrain, start, Rng.new("start-" .. terrain))
        assert(world[Grid.key(start.x, start.y)])
      end
    end,
  },
}
