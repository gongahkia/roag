-- Validation and coordinate transforms for declarative room-template JSON.
-- No template may carry executable behavior; runtime only consumes the
-- resulting material/passability geometry and diagnostic provenance.
local Config = require("src.rooms.config")

local Template = {}

local function copy(value)
  local result = {}
  for key, field in pairs(value or {}) do
    if type(field) == "table" then result[key] = copy(field) else result[key] = field end
  end
  return result
end

local function error_at(errors, code, message, extra)
  local item = { code = code, message = message }
  for key, value in pairs(extra or {}) do item[key] = value end
  errors[#errors + 1] = item
end

local function id_valid(id)
  return type(id) == "string" and id:match("^room%.dungeon%.[a-z0-9_%.]+$") ~= nil
end

function Template.glyph_at(template, x, y)
  if x < 0 or x >= template.width or y < 0 or y >= template.height then return nil end
  local row = template.layout[template.height - y]
  return row and row:sub(x + 1, x + 1) or nil
end

function Template.connector_position(template, connector)
  if connector.side == "north" then return connector.offset, template.height - 1 end
  if connector.side == "south" then return connector.offset, 0 end
  if connector.side == "east" then return template.width - 1, connector.offset end
  if connector.side == "west" then return 0, connector.offset end
  return nil
end

function Template.connector_from_position(template, x, y)
  if y == template.height - 1 then return { side = "north", offset = x } end
  if x == template.width - 1 then return { side = "east", offset = y } end
  if y == 0 then return { side = "south", offset = x } end
  if x == 0 then return { side = "west", offset = y } end
  return nil
end

function Template.connector_set(template)
  local result = {}
  for _, connector in ipairs(template.connectors or {}) do result[connector.side] = true end
  return result
end

function Template.pattern_key(sides)
  local lookup, ordered = {}, {}
  for _, side in ipairs(sides or {}) do lookup[side] = true end
  for _, side in ipairs(Config.SIDE_ORDER) do if lookup[side] then ordered[#ordered + 1] = side end end
  return table.concat(ordered, "+")
end

local function material_for(template, glyph, registry)
  local material_id = (template.legend or {})[glyph] or Config.PALETTE[glyph]
  if not material_id then return nil end
  local material = registry.materials[material_id]
  return material_id, material
end

function Template.validate(template, registry, options)
  options = options or {}
  local errors, warnings = {}, {}
  if type(template) ~= "table" then
    return { valid = false, errors = { { code = "invalid_template", message = "Template must be an object" } }, warnings = warnings }
  end
  if template.format ~= Config.FORMAT then error_at(errors, "unsupported_format", "Template format must be " .. Config.FORMAT) end
  if template.version ~= Config.VERSION then error_at(errors, "unsupported_version", "Template version must be " .. Config.VERSION) end
  if not id_valid(template.id) then error_at(errors, "invalid_id", "Template has an invalid room semantic ID") end
  if template.biome ~= Config.BIOME then error_at(errors, "invalid_biome", "Template biome must be dungeon") end
  if template.width ~= Config.WIDTH or template.height ~= Config.HEIGHT then
    error_at(errors, "wrong_dimensions", string.format("Dungeon rooms must be %dx%d", Config.WIDTH, Config.HEIGHT))
  end
  if type(template.weight) ~= "number" or template.weight <= 0 then error_at(errors, "invalid_weight", "Template weight must be positive") end
  if type(template.allow_rotation) ~= "boolean" then error_at(errors, "invalid_rotation_flag", "allow_rotation must be boolean") end
  if type(template.tags) ~= "table" then
    error_at(errors, "invalid_tags", "tags must be an array")
  else
    local seen_tags = {}
    for _, tag in ipairs(template.tags) do
      if type(tag) ~= "string" or not Config.TAGS[tag] then error_at(errors, "invalid_tag", "Unknown room tag " .. tostring(tag))
      elseif seen_tags[tag] then error_at(errors, "duplicate_tag", "Duplicate room tag " .. tag)
      else seen_tags[tag] = true end
    end
  end
  if type(template.legend) ~= "table" then
    error_at(errors, "invalid_legend", "legend must map ASCII glyphs to material semantic IDs")
  end
  if type(template.layout) ~= "table" or #template.layout ~= Config.HEIGHT then
    error_at(errors, "wrong_row_count", "layout must have exactly " .. Config.HEIGHT .. " rows")
  else
    for row_index, row in ipairs(template.layout) do
      if type(row) ~= "string" or #row ~= Config.WIDTH then
        error_at(errors, "wrong_row_width", "layout row " .. row_index .. " must have width " .. Config.WIDTH, { row = row_index })
      else
        for x = 0, Config.WIDTH - 1 do
          local glyph = row:sub(x + 1, x + 1)
          local material_id, material = material_for(template, glyph, registry)
          if not material_id then error_at(errors, "unknown_glyph", "Unknown glyph '" .. glyph .. "'", { glyph = glyph, row = row_index, x = x })
          elseif not material then error_at(errors, "unknown_material", "Glyph '" .. glyph .. "' references unknown material " .. material_id, { glyph = glyph, material_id = material_id }) end
        end
      end
    end
  end

  local connector_points, connector_sides = {}, {}
  if type(template.connectors) ~= "table" then
    error_at(errors, "invalid_connectors", "connectors must be an array")
  else
    for index, connector in ipairs(template.connectors) do
      if type(connector) ~= "table" or not Config.SIDES[connector.side] then
        error_at(errors, "invalid_connector_side", "Connector " .. index .. " has an unsupported side", { connector = index })
      elseif type(connector.offset) ~= "number" or connector.offset % 1 ~= 0
        or connector.offset < 0 or connector.offset >= Config.WIDTH then
        error_at(errors, "invalid_connector_offset", "Connector " .. index .. " has an invalid offset", { connector = index })
      else
        local x, y = Template.connector_position({ width = Config.WIDTH, height = Config.HEIGHT }, connector)
        local point_key = x .. ":" .. y
        if connector_points[point_key] or connector_sides[connector.side] then
          error_at(errors, "duplicate_connector", "Duplicate connector " .. connector.side .. "@" .. connector.offset, { connector = index })
        else
          connector_points[point_key], connector_sides[connector.side] = true, true
          local glyph = type(template.layout) == "table" and Template.glyph_at({ width = Config.WIDTH, height = Config.HEIGHT, layout = template.layout }, x, y)
          local _, material = glyph and material_for(template, glyph, registry) or nil
          if not material or material.blocks_movement then
            error_at(errors, "solid_connector", "Connector " .. connector.side .. "@" .. connector.offset .. " must open onto passable material", { connector = index })
          end
        end
      end
    end
  end

  -- V1 has a deliberately strict perimeter: every boundary opening is a
  -- declared one-cell connector, preventing accidental leaks between chunks.
  local passable, first = {}, nil
  if type(template.layout) == "table" then
    for x = 0, Config.WIDTH - 1 do
      for y = 0, Config.HEIGHT - 1 do
        local glyph = Template.glyph_at({ width = Config.WIDTH, height = Config.HEIGHT, layout = template.layout }, x, y)
        local _, material = glyph and material_for(template, glyph, registry) or nil
        if material and not material.blocks_movement then
          local point_key = x .. ":" .. y
          passable[point_key] = true
          first = first or { x = x, y = y }
          if x == 0 or y == 0 or x == Config.WIDTH - 1 or y == Config.HEIGHT - 1 then
            if not connector_points[point_key] then
              error_at(errors, "open_boundary", "Passable boundary cell must be declared as a connector", { x = x, y = y })
            end
          end
        end
      end
    end
  end
  if not first then
    error_at(errors, "no_passable_cells", "Template has no passable cells")
  else
    local visited, queue, cursor = { [first.x .. ":" .. first.y] = true }, { first }, 1
    while queue[cursor] do
      local point = queue[cursor]
      cursor = cursor + 1
      for _, delta in ipairs({ { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } }) do
        local nx, ny = point.x + delta[1], point.y + delta[2]
        local point_key = nx .. ":" .. ny
        if passable[point_key] and not visited[point_key] then
          visited[point_key] = true
          queue[#queue + 1] = { x = nx, y = ny }
        end
      end
    end
    for point_key in pairs(passable) do
      if not visited[point_key] then
        error_at(errors, "disconnected_passable_area", "All v1 passable room cells must be connected", { point = point_key })
        break
      end
    end
    for point_key in pairs(connector_points) do
      if not visited[point_key] then error_at(errors, "disconnected_connector", "Connector does not reach the primary passable region", { point = point_key }) end
    end
  end
  if options.existing_ids and template.id and options.existing_ids[template.id] and options.existing_ids[template.id] ~= options.current_id then
    error_at(errors, "duplicate_id", "Duplicate room semantic ID " .. template.id)
  end
  return { valid = #errors == 0, errors = errors, warnings = warnings }
end

local function rotated_position(width, height, x, y)
  return height - 1 - y, x
end

function Template.rotate(template, turns)
  turns = (turns or 0) % 4
  local result = copy(template)
  result.rotation = turns * 90
  result.base_id = template.base_id or template.id
  for _ = 1, turns do
    local rows = {}
    for row = 1, Config.HEIGHT do rows[row] = {} end
    for x = 0, Config.WIDTH - 1 do
      for y = 0, Config.HEIGHT - 1 do
        local nx, ny = rotated_position(Config.WIDTH, Config.HEIGHT, x, y)
        rows[Config.HEIGHT - ny][nx + 1] = Template.glyph_at(result, x, y)
      end
    end
    result.layout = {}
    for row = 1, Config.HEIGHT do result.layout[row] = table.concat(rows[row]) end
    local connectors = {}
    for _, connector in ipairs(result.connectors) do
      local x, y = Template.connector_position(result, connector)
      local nx, ny = rotated_position(Config.WIDTH, Config.HEIGHT, x, y)
      connectors[#connectors + 1] = Template.connector_from_position(result, nx, ny)
    end
    result.connectors = connectors
  end
  table.sort(result.connectors, function(a, b)
    local ai, bi = 0, 0
    for index, side in ipairs(Config.SIDE_ORDER) do if side == a.side then ai = index end if side == b.side then bi = index end end
    return ai == bi and a.offset < b.offset or ai < bi
  end)
  return result
end

function Template.default(id)
  local layout = {}
  for row = 1, Config.HEIGHT do
    layout[row] = (row == 1 or row == Config.HEIGHT) and string.rep("#", Config.WIDTH)
      or ("#" .. string.rep(".", Config.WIDTH - 2) .. "#")
  end
  return {
    format = Config.FORMAT, version = Config.VERSION, id = id or "room.dungeon.standard.new_room",
    biome = Config.BIOME, tags = { "standard" }, weight = 1, allow_rotation = true,
    width = Config.WIDTH, height = Config.HEIGHT,
    connectors = {}, legend = copy(Config.PALETTE), layout = layout,
  }
end

return Template
