-- Discrete authoritative displacement. It intentionally does not query body
-- locomotion: external force can move any physical target cell-by-cell.
local Force = {}

local function sign(value)
  if value > 0 then
    return 1
  elseif value < 0 then
    return -1
  end
  return 0
end

function Force.apply(world, target, spec)
  if not world or not target or type(spec) ~= "table" then
    return { applied = false, code = "invalid_force", reason = "Force requires world, target, and specification" }
  end
  if type(spec.dx) ~= "number" or type(spec.dy) ~= "number" then
    return { applied = false, code = "invalid_direction", reason = "Force direction must be numeric" }
  end
  local dx, dy = sign(spec.dx), sign(spec.dy)
  if dx == 0 and dy == 0 then
    return { applied = false, code = "no_direction", reason = "Force has no direction", path = {} }
  end
  if type(spec.distance) ~= "number" or spec.distance <= 0 or spec.distance % 1 ~= 0 then
    return { applied = false, code = "invalid_distance", reason = "Force distance must be a positive integer" }
  end
  if type(target.x) ~= "number" or type(target.y) ~= "number" then
    return { applied = false, code = "invalid_target", reason = "Force target has no grid position" }
  end

  local is_blocked = spec.is_blocked or function(x, y)
    return not world:is_passable(x, y), "blocked_world"
  end
  local move = spec.move or function(value, x, y)
    value.x, value.y = x, y
    return { applied = true }
  end
  local path, stopped_code, blocker_code = {}, nil, nil
  local start_x, start_y = target.x, target.y
  for _ = 1, spec.distance do
    local x, y = target.x + dx, target.y + dy
    local blocked, code = is_blocked(x, y, target)
    if blocked then
      stopped_code = code or "blocked"
      blocker_code = stopped_code
      break
    end
    local moved = move(target, x, y)
    if not moved or not moved.applied then
      stopped_code = moved and moved.code or "move_rejected"
      break
    end
    path[#path + 1] = { x = x, y = y }
    if spec.on_step then
      local continuation = spec.on_step(target, x, y, {
        index = #path,
        requested_distance = spec.distance,
        path = path,
      })
      if continuation == false or (type(continuation) == "table" and continuation.stop) then
        stopped_code = type(continuation) == "table" and continuation.code or "stopped"
        break
      end
    end
  end
  return {
    applied = #path > 0,
    code = stopped_code or "applied",
    cause = spec.cause,
    source_actor_id = spec.source_actor_id,
    source_component_id = spec.source_component_id,
    ability_id = spec.ability_id,
    direction = { dx = dx, dy = dy },
    requested_distance = spec.distance,
    moved_distance = #path,
    remaining_distance = spec.distance - #path,
    blocked = blocker_code ~= nil,
    blocker_code = blocker_code,
    final_x = target.x,
    final_y = target.y,
    start_x = start_x,
    start_y = start_y,
    path = path,
  }
end

return Force
