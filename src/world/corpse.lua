-- Corpses preserve the defeated actor's actual Body table. They are inert
-- world entities, not regenerated loot definitions.
local Body = require("src.body.body")

local Corpse = {}
Corpse.__index = Corpse

function Corpse.from_actor(corpse_id, actor)
  assert(actor.body, "Only body-bearing actors can create physical corpses")
  local corpse = setmetatable({
    id = corpse_id,
    kind = "corpse",
    source_kind = actor.kind,
    source_actor_id = actor.content_id,
    x = actor.x,
    y = actor.y,
    body = actor.body,
    fallen_archive_id = actor.fallen_archive_id,
    fallen_source_run_id = actor.fallen_source_run_id,
  }, Corpse)
  actor.body = nil
  return corpse
end

function Corpse:list_components()
  return self.body:list_installed_slots()
end

function Corpse:to_data()
  return {
    id = self.id,
    kind = self.kind,
    source_kind = self.source_kind,
    source_actor_id = self.source_actor_id,
    x = self.x,
    y = self.y,
    body = self.body:to_data(),
    fallen_archive_id = self.fallen_archive_id,
    fallen_source_run_id = self.fallen_source_run_id,
  }
end

function Corpse.from_data(registry, data)
  assert(type(data) == "table" and type(data.id) == "string", "Corpse data must include a stable ID")
  return setmetatable({
    id = data.id,
    kind = "corpse",
    source_kind = data.source_kind,
    source_actor_id = data.source_actor_id,
    x = data.x,
    y = data.y,
    body = Body.from_data(registry, data.body),
    fallen_archive_id = data.fallen_archive_id,
    fallen_source_run_id = data.fallen_source_run_id,
  }, Corpse)
end

return Corpse
