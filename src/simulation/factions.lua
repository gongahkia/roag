-- Shared faction query boundary.  AI, projectiles, and rewards ask this
-- module rather than comparing content IDs, keeping affiliations declarative.
local Factions = {}

Factions.PLAYER_ID = "faction.player"
Factions.ECHO_ID = "faction.echo"

function Factions.actor_id(registry, actor, player)
  if not actor or actor == player then return Factions.PLAYER_ID end
  if type(actor.faction_id) == "string" and registry.factions[actor.faction_id] then
    return actor.faction_id
  end
  if actor.kind == "fallen_echo" then return Factions.ECHO_ID end
  local definition = actor.content_id and registry.enemies[actor.content_id] or nil
  return definition and definition.faction_id or Factions.ECHO_ID
end

function Factions.are_hostile(registry, first, second, player)
  if not first or not second or first == second then return false end
  local first_id = Factions.actor_id(registry, first, player)
  local second_id = Factions.actor_id(registry, second, player)
  if first_id == second_id then return false end
  local faction = registry.factions[first_id]
  if not faction then return false end
  for _, hostile_id in ipairs(faction.hostile_faction_ids or {}) do
    if hostile_id == second_id then return true end
  end
  return false
end

return Factions
