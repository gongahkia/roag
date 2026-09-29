local Rng = require("src.rng")

return {
  {
    name = "RNG repeats an integer sequence for the same seed",
    run = function()
      local first, second = Rng.new(424242), Rng.new(424242)
      for _ = 1, 32 do
        assert(first:int(1, 1000000) == second:int(1, 1000000))
      end
    end,
  },
  {
    name = "RNG keeps different seeds distinct",
    run = function()
      local first, second = Rng.new(100), Rng.new(101)
      local differs = false
      for _ = 1, 12 do
        if first:next() ~= second:next() then
          differs = true
          break
        end
      end
      assert(differs)
    end,
  },
  {
    name = "RNG shuffle is deterministic and leaves the input untouched",
    run = function()
      local values = { "a", "b", "c", "d", "e" }
      local first = Rng.new("shuffle"):shuffle(values)
      local second = Rng.new("shuffle"):shuffle(values)
      assert(table.concat(first) == table.concat(second))
      assert(table.concat(values) == "abcde")
    end,
  },
}
