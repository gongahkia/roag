-- Derived voluntary-locomotion rules.  No mutable injury flag lives here:
-- every query is recomputed from the actor's currently functional body parts.
local Locomotion = {
  ABILITY_ID = "ability.locomotion.move",
  NORMAL = "NORMAL",
  IMPAIRED = "IMPAIRED",
  CRAWLING = "CRAWLING",
}

function Locomotion.derive(body)
  -- Actors which have not yet migrated to physical bodies retain their legacy
  -- movement.  All body-bearing actors use the provider count below.
  if not body then
    return {
      state = Locomotion.NORMAL,
      provider_count = 0,
      providers = {},
      body_derived = false,
    }
  end

  local providers = body:capability_providers(Locomotion.ABILITY_ID)
  local count = #providers
  local state = count >= 2 and Locomotion.NORMAL
    or count == 1 and Locomotion.IMPAIRED
    or Locomotion.CRAWLING
  return {
    state = state,
    provider_count = count,
    providers = providers,
    body_derived = true,
  }
end

function Locomotion.validate_move(locomotion, dx, dy)
  if type(dx) ~= "number" or type(dy) ~= "number" or (dx == 0 and dy == 0) then
    return { applied = false, code = "invalid_direction", reason = "Movement requires a direction", locomotion = locomotion }
  end
  if locomotion.state == Locomotion.CRAWLING and dx ~= 0 and dy ~= 0 then
    return {
      applied = false,
      code = "crawl_cannot_move_diagonally",
      reason = "Crawling cannot move diagonally",
      locomotion = locomotion,
    }
  end
  return { applied = true, locomotion = locomotion }
end

function Locomotion.validate_dash(locomotion)
  if locomotion.state == Locomotion.NORMAL then
    return { applied = true, locomotion = locomotion }
  end
  local code = locomotion.state == Locomotion.IMPAIRED
    and "no_dash_while_impaired"
    or "no_dash_while_crawling"
  return {
    applied = false,
    code = code,
    reason = locomotion.state == Locomotion.IMPAIRED
      and "Cannot dash while locomotion is impaired"
      or "Cannot dash while crawling",
    locomotion = locomotion,
  }
end

return Locomotion
