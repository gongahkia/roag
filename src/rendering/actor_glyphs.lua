-- Shape-first actor presentation.  This module intentionally maps only
-- presentation-facing character IDs and AI roles to glyphs: combat state never
-- needs to know whether an actor is drawn as a circle, diamond, or texture.
local ActorGlyphs = {}

local DIRECTIONS = {
  w = { 0, -1, -math.pi * 0.5 },
  a = { -1, 0, math.pi },
  s = { 0, 1, math.pi * 0.5 },
  d = { 1, 0, 0 },
}

local CHARACTER_GLYPHS = {
  ["expedition.gunner"] = { shape = "circle", size = 0.34, cue = "barrel", color = { 0.38, 0.76, 1 } },
  ["expedition.bruiser"] = { shape = "square", size = 0.35, cue = "wedge", color = { 1, 0.55, 0.3 } },
  ["expedition.conductor"] = { shape = "diamond", size = 0.39, cue = "electric_core", color = { 0.52, 0.76, 1 } },
  ["expedition.demolitionist"] = { shape = "hexagon", size = 0.39, cue = "explosive_core", color = { 1, 0.68, 0.24 } },
}

local ROLE_GLYPHS = {
  rusher = { shape = "triangle", size = 0.39, cue = "front" },
  ranged = { shape = "diamond", size = 0.38, cue = "barrel" },
  skirmisher = { shape = "diamond", size = 0.38, cue = "barrel" },
  flanker = { shape = "angled", size = 0.4, cue = "front" },
  controller = { shape = "hexagon", size = 0.4, cue = "ring" },
  heavy = { shape = "square", size = 0.44, cue = "front" },
}

local DEFAULT_GLYPH = { shape = "circle", size = 0.31, cue = "none", color = { 0.8, 0.82, 0.88 } }

local function copy_glyph(source)
  local result = {}
  for key, value in pairs(source or DEFAULT_GLYPH) do result[key] = value end
  return result
end

local function direction_for(actor)
  return DIRECTIONS[actor and actor.direction] or DIRECTIONS.s
end

local function is_player(actor, state)
  return actor and state and actor == state.player
end

function ActorGlyphs.definition(actor, state)
  local glyph
  if is_player(actor, state) then
    local expedition = state.expedition or {}
    glyph = CHARACTER_GLYPHS[expedition.character_id]
      or { shape = "circle", size = 0.35, cue = "barrel", color = { 0.55, 0.82, 1 } }
  elseif actor and (actor.boss or (state and actor == state.boss)) then
    glyph = { shape = "boss", size = 0.53, cue = "modules", color = { 1, 0.4, 0.3 } }
  elseif actor and actor.kind == "bullet" then
    glyph = { shape = "projectile", size = 0.17, cue = "none", color = { 1, 0.84, 0.28 } }
  elseif actor and actor.kind == "bomb" then
    glyph = { shape = "hexagon", size = 0.26, cue = "explosive_core", color = { 1, 0.5, 0.2 } }
  elseif actor and actor.kind == "flare" then
    glyph = { shape = "diamond", size = 0.19, cue = "electric_core", color = { 1, 0.72, 0.22 } }
  elseif actor and actor.kind == "torch" then
    glyph = { shape = "circle", size = 0.17, cue = "electric_core", color = { 1, 0.58, 0.2 } }
  elseif actor and actor.kind == "target" then
    glyph = { shape = "ring", size = 0.3, cue = "none", color = { 0.9, 0.9, 0.78 } }
  elseif actor and actor.kind == "fallen_echo" then
    glyph = { shape = "diamond", size = 0.34, cue = "ring", color = { 0.76, 0.34, 0.88 } }
  else
    glyph = ROLE_GLYPHS[actor and actor.ai_role]
      or (actor and actor.body and ROLE_GLYPHS.heavy)
      or DEFAULT_GLYPH
  end
  glyph = copy_glyph(glyph)
  glyph.direction = direction_for(actor)
  glyph.elite = actor and actor.elite == true or false
  glyph.boss = glyph.shape == "boss"
  return glyph
end

local function polygon(cx, cy, radius_x, radius_y, sides, angle)
  local points = {}
  for index = 0, sides - 1 do
    local theta = (angle or -math.pi * 0.5) + index * math.pi * 2 / sides
    points[#points + 1] = cx + math.cos(theta) * radius_x
    points[#points + 1] = cy + math.sin(theta) * radius_y
  end
  return points
end

local function draw_shape(shape, cx, cy, radius_x, radius_y, direction)
  if shape == "circle" then
    love.graphics.ellipse("fill", cx, cy, radius_x, radius_y)
  elseif shape == "square" then
    love.graphics.rectangle("fill", cx - radius_x, cy - radius_y, radius_x * 2, radius_y * 2)
  elseif shape == "diamond" then
    love.graphics.polygon("fill", polygon(cx, cy, radius_x, radius_y, 4, -math.pi * 0.5))
  elseif shape == "triangle" then
    love.graphics.polygon("fill", polygon(cx, cy, radius_x, radius_y, 3, direction[3]))
  elseif shape == "angled" then
    local dx, dy = direction[1], direction[2]
    local px, py = -dy, dx
    love.graphics.polygon("fill",
      cx + dx * radius_x * 1.08, cy + dy * radius_y * 1.08,
      cx + px * radius_x * 0.72 - dx * radius_x * 0.66, cy + py * radius_y * 0.72 - dy * radius_y * 0.66,
      cx - dx * radius_x * 0.9, cy - dy * radius_y * 0.9,
      cx - px * radius_x * 0.72 - dx * radius_x * 0.66, cy - py * radius_y * 0.72 - dy * radius_y * 0.66)
  elseif shape == "hexagon" then
    love.graphics.polygon("fill", polygon(cx, cy, radius_x, radius_y, 6, math.pi / 6))
  elseif shape == "projectile" then
    love.graphics.polygon("fill", polygon(cx, cy, radius_x, radius_y, 4, direction[3]))
  elseif shape == "ring" then
    love.graphics.setLineWidth(math.max(1, math.floor(math.min(radius_x, radius_y) * 0.42)))
    love.graphics.ellipse("line", cx, cy, radius_x, radius_y)
    love.graphics.setLineWidth(1)
  elseif shape == "boss" then
    love.graphics.polygon("fill", polygon(cx, cy, radius_x, radius_y, 6, math.pi / 6))
  else
    love.graphics.ellipse("fill", cx, cy, radius_x, radius_y)
  end
end

local function draw_cue(cue, cx, cy, radius_x, radius_y, direction, outline)
  local dx, dy = direction[1], direction[2]
  if cue == "barrel" then
    love.graphics.setLineWidth(math.max(1, math.floor(math.min(radius_x, radius_y) * 0.45)))
    love.graphics.line(cx + dx * radius_x * 0.25, cy + dy * radius_y * 0.25, cx + dx * radius_x * 1.52, cy + dy * radius_y * 1.52)
    love.graphics.setLineWidth(1)
  elseif cue == "wedge" or cue == "front" then
    local px, py = -dy, dx
    love.graphics.polygon("fill",
      cx + dx * radius_x * 1.18, cy + dy * radius_y * 1.18,
      cx + px * radius_x * 0.34 + dx * radius_x * 0.42, cy + py * radius_y * 0.34 + dy * radius_y * 0.42,
      cx - px * radius_x * 0.34 + dx * radius_x * 0.42, cy - py * radius_y * 0.34 + dy * radius_y * 0.42)
  elseif cue == "electric_core" then
    love.graphics.ellipse(outline and "fill" or "line", cx, cy, radius_x * 0.35, radius_y * 0.35)
  elseif cue == "explosive_core" then
    love.graphics.polygon(outline and "fill" or "line", polygon(cx, cy, radius_x * 0.38, radius_y * 0.38, 4, math.pi / 4))
  elseif cue == "ring" then
    love.graphics.setLineWidth(math.max(1, math.floor(math.min(radius_x, radius_y) * 0.24)))
    love.graphics.ellipse("line", cx, cy, radius_x * 0.52, radius_y * 0.52)
    love.graphics.setLineWidth(1)
  elseif cue == "modules" then
    for _, offset in ipairs({ { -0.92, 0 }, { 0.92, 0 }, { 0, -0.92 }, { 0, 0.92 } }) do
      love.graphics.rectangle("fill", cx + offset[1] * radius_x - radius_x * 0.21, cy + offset[2] * radius_y - radius_y * 0.21, radius_x * 0.42, radius_y * 0.42)
    end
  end
end

-- Draws a complete actor glyph, including the silhouette required by the
-- renderer's existing four-offset outline pass.  It contains no sprite or
-- animation assumptions and safely remains a no-op in headless tests.
function ActorGlyphs.draw(actor, state, x, y, size, tint, transform)
  if not love or not love.graphics then return false end
  local glyph = ActorGlyphs.definition(actor, state)
  transform = transform or {}
  local scale_x, scale_y = transform.scale_x or 1, transform.scale_y or 1
  local cx = x + size * 0.5 + (transform.offset_x or 0)
  local cy = y + size * 0.5 + (transform.offset_y or 0)
  local radius_x = size * glyph.size * scale_x
  local radius_y = size * glyph.size * scale_y
  local color = tint or glyph.color or DEFAULT_GLYPH.color
  love.graphics.setColor(color[1], color[2], color[3], color[4] or 1)
  draw_shape(glyph.shape, cx, cy, radius_x, radius_y, glyph.direction)

  if glyph.cue ~= "none" then
    if transform.outline then
      love.graphics.setColor(color[1], color[2], color[3], color[4] or 1)
    else
      love.graphics.setColor(0.04, 0.07, 0.11, color[4] or 1)
    end
    draw_cue(glyph.cue, cx, cy, radius_x, radius_y, glyph.direction, transform.outline)
  end

  if glyph.elite then
    love.graphics.setColor(1, 0.84, 0.24, color[4] or 1)
    love.graphics.setLineWidth(math.max(1, math.floor(size * 0.07)))
    if glyph.shape == "circle" then
      love.graphics.ellipse("line", cx, cy, radius_x * 1.22, radius_y * 1.22)
    else
      love.graphics.polygon("line", polygon(cx, cy, radius_x * 1.22, radius_y * 1.22, glyph.shape == "hexagon" and 6 or 4, math.pi / 4))
    end
    love.graphics.setLineWidth(1)
  end
  love.graphics.setColor(1, 1, 1)
  return true
end

return ActorGlyphs
