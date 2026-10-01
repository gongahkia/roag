-- Declarative corpus lookup shared by normal generation, the inspector, and
-- the standalone editor.  Adding another interior biome means adding a
-- config/content pair here, never a second room assembly implementation.
local dungeon = require("src.rooms.config")
local reactor = require("src.rooms.reactor_config")

local Corpora = { all = { dungeon, reactor } }

local by_id, by_biome = {}, {}
for _, config in ipairs(Corpora.all) do
  by_id[config.CORPUS_ID] = config
  by_biome[config.BIOME] = config
end

function Corpora.get(corpus_id)
  return by_id[corpus_id]
end

function Corpora.for_biome(biome)
  return by_biome[biome]
end

function Corpora.list()
  local result = {}
  for _, config in ipairs(Corpora.all) do result[#result + 1] = config end
  return result
end

return Corpora
