-- Procedural arena presentation.  These glyphs are deliberately small,
-- high-contrast shapes rather than texture stand-ins: terrain and objects stay
-- legible beneath footprints, tracers, particles, and dense late-run combat.
local WorldGlyphs = {}

local function set_color(color, multiplier, alpha)
  multiplier = multiplier or 1
  love.graphics.setColor(
    math.min(1, color[1] * multiplier),
    math.min(1, color[2] * multiplier),
    math.min(1, color[3] * multiplier),
    alpha or color[4] or 1)
end

local function polygon(cx, cy, radius, sides, angle)
  local points = {}
  for index = 0, sides - 1 do
    local theta = (angle or -math.pi * 0.5) + index * math.pi * 2 / sides
    points[#points + 1] = cx + math.cos(theta) * radius
    points[#points + 1] = cy + math.sin(theta) * radius
  end
  return points
end

function WorldGlyphs.draw_floor(x, y, size, color, world_x, world_y)
  if not love or not love.graphics then return false end
  -- A sparse deterministic inset mark keeps grid position readable without
  -- turning every tile into visual noise.
  local phase = (math.abs((world_x or 0) * 17 + (world_y or 0) * 31) % 5)
  set_color(color, 1.45, 0.33)
  if phase == 0 then
    love.graphics.rectangle("fill", x + size * 0.24, y + size * 0.24, math.max(1, size * 0.09), math.max(1, size * 0.09))
  elseif phase == 1 then
    love.graphics.line(x + size * 0.67, y + size * 0.3, x + size * 0.76, y + size * 0.39)
  end
  love.graphics.setColor(1, 1, 1)
  return true
end

function WorldGlyphs.draw_wall(kind, x, y, size, color)
  if not love or not love.graphics then return false end
  local inset = math.max(1, size * 0.08)
  set_color(color, 0.78)
  love.graphics.rectangle("fill", x + inset, y + inset, size - inset * 2, size - inset * 2)
  set_color(color, 1.7, 0.86)
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.07)))
  local left, right, top, bottom = x + inset, x + size - inset, y + inset, y + size - inset
  if kind == "wall_up" or kind == "wall_top_left" or kind == "wall_top_right" then love.graphics.line(left, top, right, top) end
  if kind == "wall_down" or kind == "wall_bottom_left" or kind == "wall_bottom_right" then love.graphics.line(left, bottom, right, bottom) end
  if kind == "wall_left" or kind == "wall_top_left" or kind == "wall_bottom_left" then love.graphics.line(left, top, left, bottom) end
  if kind == "wall_right" or kind == "wall_top_right" or kind == "wall_bottom_right" then love.graphics.line(right, top, right, bottom) end
  if kind == "wall_center" then
    love.graphics.rectangle("line", left, top, right - left, bottom - top)
  end
  love.graphics.setLineWidth(1)
  love.graphics.setColor(1, 1, 1)
  return true
end

function WorldGlyphs.draw_liquid(x, y, size, depth, color)
  if not love or not love.graphics then return false end
  local cx, cy = x + size * 0.5, y + size * 0.5
  set_color(color, 1, 0.32 + depth * 0.35)
  love.graphics.ellipse("fill", cx, cy, size * 0.42, size * (0.18 + depth * 0.1))
  set_color(color, 1.5, 0.7)
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.055)))
  love.graphics.arc("line", "open", cx, cy, size * 0.28, 0, math.pi)
  love.graphics.setLineWidth(1)
  love.graphics.setColor(1, 1, 1)
  return true
end

function WorldGlyphs.draw_gas(x, y, size, density, drift)
  if not love or not love.graphics then return false end
  local alpha = 0.14 + density * 0.34
  love.graphics.setColor(0.56, 1, 0.4, alpha)
  love.graphics.circle("fill", x + size * (0.39 + drift), y + size * 0.5, size * 0.2)
  love.graphics.circle("fill", x + size * (0.61 - drift), y + size * 0.4, size * 0.14)
  love.graphics.setColor(1, 1, 1)
  return true
end

function WorldGlyphs.draw_spikes(x, y, size, color)
  if not love or not love.graphics then return false end
  set_color(color)
  for index = 0, 2 do
    local left = x + size * (0.13 + index * 0.25)
    love.graphics.polygon("fill", left, y + size * 0.77, left + size * 0.11, y + size * 0.24, left + size * 0.22, y + size * 0.77)
  end
  love.graphics.setColor(1, 1, 1)
  return true
end

function WorldGlyphs.draw_fire(x, y, size, flicker)
  if not love or not love.graphics then return false end
  local cx, cy = x + size * 0.5, y + size * (0.58 - flicker)
  love.graphics.setColor(1, 0.32 + flicker, 0.1, 0.92)
  love.graphics.polygon("fill", cx, cy - size * 0.34, cx + size * 0.22, cy + size * 0.24, cx - size * 0.22, cy + size * 0.24)
  love.graphics.setColor(1, 0.82, 0.24, 0.95)
  love.graphics.polygon("fill", cx, cy - size * 0.16, cx + size * 0.1, cy + size * 0.16, cx - size * 0.1, cy + size * 0.16)
  love.graphics.setColor(1, 1, 1)
  return true
end

local function object_kind(definition, object)
  local interaction = object and object.interaction_role or definition and definition.interaction_role
  if interaction then return interaction end
  local style = definition and definition.render_style or ""
  if style:find("tree", 1, true) then return "tree" end
  if style:find("log", 1, true) then return "log" end
  if style:find("boulder", 1, true) or style:find("rubble", 1, true) or style:find("stalagmite", 1, true) then return "rock" end
  if style:find("cable", 1, true) then return "cable" end
  if style:find("machine", 1, true) or style:find("panel", 1, true) then return "machine" end
  if style:find("statue", 1, true) then return "statue" end
  if style:find("barricade", 1, true) then return "barricade" end
  return "crate"
end

function WorldGlyphs.object_kind(definition, object)
  return object_kind(definition, object)
end

function WorldGlyphs.draw_object(definition, object, x, y, size, color)
  if not love or not love.graphics then return false end
  local kind = object_kind(definition, object)
  local cx, cy = x + size * 0.5, y + size * 0.5
  set_color(color)
  if kind == "tree" then
    love.graphics.circle("fill", cx, cy - size * 0.1, size * 0.29)
    set_color(color, 1.35)
    love.graphics.rectangle("fill", cx - size * 0.06, cy + size * 0.08, size * 0.12, size * 0.26)
  elseif kind == "log" then
    love.graphics.rectangle("fill", x + size * 0.13, cy - size * 0.11, size * 0.74, size * 0.22)
    set_color(color, 1.45)
    love.graphics.circle("line", x + size * 0.18, cy, size * 0.11)
  elseif kind == "rock" then
    love.graphics.polygon("fill", polygon(cx, cy, size * 0.3, 5, math.pi / 5))
  elseif kind == "statue" then
    love.graphics.rectangle("fill", cx - size * 0.11, y + size * 0.18, size * 0.22, size * 0.54)
    love.graphics.circle("fill", cx, y + size * 0.2, size * 0.12)
  elseif kind == "machine" or kind == "generator" or kind == "breaker" or kind == "service" then
    love.graphics.rectangle("fill", x + size * 0.2, y + size * 0.2, size * 0.6, size * 0.6)
    set_color(color, 1.55)
    love.graphics.circle("fill", cx, cy, size * 0.11)
  elseif kind == "cable" then
    love.graphics.setLineWidth(math.max(1, math.floor(size * 0.1)))
    love.graphics.line(x + size * 0.15, cy, x + size * 0.85, cy)
    love.graphics.setLineWidth(1)
  elseif kind == "door" then
    love.graphics.rectangle("fill", x + size * 0.2, y + size * 0.1, size * 0.6, size * 0.8)
    set_color(color, 1.5)
    love.graphics.line(cx, y + size * 0.16, cx, y + size * 0.84)
  elseif kind == "reinforcement" then
    love.graphics.polygon("line", polygon(cx, cy, size * 0.31, 6, math.pi / 6))
    love.graphics.circle("fill", cx, cy, size * 0.1)
  elseif kind == "traversal" or kind == "zone_connection" then
    love.graphics.setLineWidth(math.max(1, math.floor(size * 0.09)))
    love.graphics.line(cx, y + size * 0.16, cx, y + size * 0.84)
    love.graphics.line(cx, y + size * 0.16, cx - size * 0.16, y + size * 0.36)
    love.graphics.line(cx, y + size * 0.16, cx + size * 0.16, y + size * 0.36)
    love.graphics.setLineWidth(1)
  elseif kind == "reconstruction_station" then
    love.graphics.circle("line", cx, cy, size * 0.3)
    love.graphics.circle("fill", cx, cy, size * 0.1)
  elseif kind == "discovery" or kind == "clue" then
    love.graphics.polygon("fill", polygon(cx, cy, size * 0.22, 4, math.pi / 4))
  elseif kind == "barricade" then
    love.graphics.rectangle("fill", x + size * 0.1, y + size * 0.31, size * 0.8, size * 0.38)
    set_color(color, 1.5)
    love.graphics.line(x + size * 0.16, y + size * 0.38, x + size * 0.84, y + size * 0.62)
  else
    love.graphics.rectangle("fill", x + size * 0.2, y + size * 0.2, size * 0.6, size * 0.6)
    set_color(color, 1.45)
    love.graphics.rectangle("line", x + size * 0.28, y + size * 0.28, size * 0.44, size * 0.44)
  end
  love.graphics.setColor(1, 1, 1)
  return true
end

function WorldGlyphs.draw_electric_arc(x, y, size, alpha)
  if not love or not love.graphics then return false end
  love.graphics.setColor(0.35, 0.85, 1, alpha)
  love.graphics.setLineWidth(math.max(1, math.floor(size * 0.075)))
  love.graphics.line(x + size * 0.16, y + size * 0.7, x + size * 0.42, y + size * 0.43, x + size * 0.56, y + size * 0.59, x + size * 0.84, y + size * 0.28)
  love.graphics.setLineWidth(1)
  love.graphics.setColor(1, 1, 1)
  return true
end

return WorldGlyphs
