-- Click-first standalone workbench for ROAG's 1-bit sprite role map.
local Model = require("model")
local CursorManager = require("cursor_manager")

local TILE, JSON_FILE = 16, "mappings.json"
local model, sheet, quads, cursors, fonts, json_path
local selected, category, search, search_focus, scroll = "player", "All", "", false, 0
local zoom, pan_x, pan_y, panning, hover, status = 1, 0, 0, false, nil, "Choose a role, then click a tile."

local C = { bg = { .025, .035, .055 }, panel = { .055, .075, .11 }, panel2 = { .075, .105, .15 }, border = { .18, .25, .34 }, text = { .84, .89, .96 }, muted = { .52, .61, .72 }, cyan = { .42, .84, 1 }, gold = { .95, .82, .24 }, mint = { .45, .9, .68 }, red = { 1, .44, .34 } }
local function color(v) love.graphics.setColor(v[1], v[2], v[3], v[4] or 1) end
local function clamp(v, a, b) return math.max(a, math.min(b, v)) end
local function inside(x, y, r) return x >= r.x and y >= r.y and x <= r.x + r.width and y <= r.y + r.height end
local function font(scale) return scale >= 1.7 and fonts.title or scale >= 1.1 and fonts.large or scale < .8 and fonts.small or fonts.normal end
local function text(s, x, y, scale, tint, limit)
  love.graphics.setFont(font(scale or 1)); color(tint or C.text)
  if limit then love.graphics.printf(tostring(s), x, y, limit) else love.graphics.print(tostring(s), x, y) end
end
local function box(r, fill, outline)
  if fill then color(fill); love.graphics.rectangle("fill", r.x, r.y, r.width, r.height, 5, 5) end
  color(outline or C.border); love.graphics.rectangle("line", r.x + .5, r.y + .5, r.width - 1, r.height - 1, 5, 5)
end

local function role() return model:role(selected) end
local function tile() return model:tile(selected) end
local function roles() return model:filtered(category, search) end
local function view()
  local w, h = love.graphics.getDimensions(); local left, right, gap, top, bottom = 300, 286, 14, 118, 88
  local cx, cw, ch = left + gap, math.max(220, w - left - right - gap * 2), math.max(160, h - top - bottom)
  local base = clamp(math.min(cw / (Model.COLUMNS * TILE), ch / (Model.ROWS * TILE)), .45, 1)
  local size = TILE * base * zoom
  local bx, by = cx + (cw - Model.COLUMNS * TILE * base) / 2, top + (ch - Model.ROWS * TILE * base) / 2
  return { w = w, h = h, left = { x = 12, y = top, width = left - 20, height = ch }, canvas = { x = cx, y = top, width = cw, height = ch }, inspector = { x = cx + cw + gap, y = top, width = right - 12, height = ch }, search = { x = 12, y = 84, width = left - 20, height = 27 }, base_x = bx, base_y = by, sheet_x = bx + pan_x, sheet_y = by + pan_y, size = size, footer_y = h - 56 }
end
local function tile_at(x, y, v)
  if not inside(x, y, v.canvas) then return nil end
  local c, r = math.floor((x - v.sheet_x) / v.size) + 1, math.floor((y - v.sheet_y) / v.size) + 1
  if model:valid_tile(c, r) then return c, r end
end
local function role_rect(index, v) return { x = v.left.x + 6, y = v.left.y + 39 + (index - 1 - scroll) * 33, width = v.left.width - 12, height = 29 } end

local function category_controls()
  local output, x = {}, 12
  for _, value in ipairs(Model.categories()) do
    local w = math.max(40, #value * 7 + 17); output[#output + 1] = { x = x, y = 53, width = w, height = 24, value = value }; x = x + w + 4
  end
  return output
end
local function bottom_controls(v)
  local labels, output, x = { { "SAVE", "save" }, { "RELOAD", "load" }, { "UNDO", "undo" }, { "REDO", "redo" }, { "FIT", "fit" }, { "CLOSE", "close" } }, {}, 12
  for _, value in ipairs(labels) do
    output[#output + 1] = { x = x, y = v.footer_y, width = 96, height = 34, label = value[1], action = value[2], enabled = value[2] ~= "undo" or model.history_index > 0 }
    if value[2] == "redo" then output[#output].enabled = model.history_index < #model.history end
    x = x + 104
  end
  return output
end
local function inspector_controls(v)
  local x, y = v.inspector.x + 10, v.inspector.y + 154
  return { { x = x, y = y, width = 30, height = 28, axis = "column", delta = -1, label = "−" }, { x = x + 166, y = y, width = 30, height = 28, axis = "column", delta = 1, label = "+" }, { x = x, y = y + 39, width = 30, height = 28, axis = "row", delta = -1, label = "−" }, { x = x + 166, y = y + 39, width = 30, height = 28, axis = "row", delta = 1, label = "+" }, { x = x, y = y + 94, width = 196, height = 32, action = "reset", label = "RESET ROLE" }, { x = x, y = y + 132, width = 196, height = 32, action = "clear", label = "CLEAR OPTIONAL" } }
end

local function read_file()
  local f, err = io.open(json_path, "rb"); if not f then return nil, err end
  local value = f:read("*a"); f:close(); return value
end
local function save()
  local f, err = io.open(json_path, "wb")
  if not f then status = "Could not save: " .. tostring(err); return end
  local ok, write_err = f:write(model:serialize()); f:close()
  if not ok then status = "Could not save: " .. tostring(write_err); return end
  model.dirty, status = false, "Saved mappings.json — ROAG refreshes it when its window regains focus."
end
local function load()
  local contents, err = read_file(); if not contents then status = "No mapping file yet: choose a tile, then save."; return end
  local mappings, failure = Model.deserialize(contents)
  if not mappings then status = "Could not load: " .. failure.reason; return end
  model:replace(mappings, false); status = "Loaded mappings.json."
end
local function action(kind)
  if kind == "save" then save() elseif kind == "load" then load() elseif kind == "undo" then local r = model:undo(); status = r.applied and "Undid mapping change." or "Nothing to undo." elseif kind == "redo" then local r = model:redo(); status = r.applied and "Redid mapping change." or "Nothing to redo." elseif kind == "fit" then zoom, pan_x, pan_y, status = 1, 0, 0, "Sheet fitted." elseif kind == "close" then love.event.quit() end
end
local function edit(control)
  if control.axis then
    local result = model:adjust(selected, control.axis, control.delta); local current = result and result.mapping
    status = current and (role().label .. " → [" .. current[1] .. ", " .. current[2] .. "]") or "Could not adjust mapping."
  elseif control.action == "reset" then model:reset(selected); status = "Role reset to default." 
  elseif control.action == "clear" then local ok, failure = model:clear(selected); status = ok and "Optional role cleared; fallback art will render." or (failure and failure.reason or "Cannot clear this role.") end
end

function love.load()
  love.graphics.setDefaultFilter("nearest", "nearest")
  fonts = { small = love.graphics.newFont(12), normal = love.graphics.newFont(14), large = love.graphics.newFont(19), title = love.graphics.newFont(30) }
  json_path = love.filesystem.getSource() .. "/" .. JSON_FILE; model = Model.new(); sheet = love.graphics.newImage("colored-transparent_packed.png"); quads = {}
  for c = 1, Model.COLUMNS do for r = 1, Model.ROWS do quads[c .. ":" .. r] = love.graphics.newQuad((c - 1) * TILE, (r - 1) * TILE, TILE, TILE, sheet) end end
  cursors = CursorManager.new(); cursors:load(); cursors:set("default")
  if read_file() then load() end
end

function love.update()
  local x, y = love.mouse.getPosition(); local v = view()
  if panning then cursors:set("pan") elseif inside(x, y, v.canvas) then cursors:set("picker") elseif inside(x, y, v.left) or inside(x, y, v.inspector) or inside(x, y, v.search) then cursors:set("action") else cursors:set("default") end
end

function love.draw()
  local v = view(); love.graphics.clear(C.bg)
  text("ROAG SPRITE WORKBENCH", 12, 14, 1.8, C.cyan); text("Click roles and tiles • precise coordinate controls • undoable JSON", 12, 49, .76, C.muted)
  text(model.dirty and "UNSAVED" or "SAVED", v.w - 90, 24, .75, model.dirty and C.gold or C.mint)
  for _, item in ipairs(category_controls()) do box(item, item.value == category and { .12, .28, .39 } or C.panel2, item.value == category and C.cyan or C.border); text(item.value, item.x + 7, item.y + 6, .72, item.value == category and C.gold or C.text) end
  box(v.search, C.panel, search_focus and C.cyan or C.border); text(search == "" and "Filter roles…  (F)" or search, v.search.x + 8, v.search.y + 6, .76, search == "" and C.muted or C.text)
  if search_focus then text("|", v.search.x + 9 + #search * 7, v.search.y + 6, .76, C.cyan) end

  box(v.left, C.panel); local list = roles(); text("ROLES  " .. #list, v.left.x + 10, v.left.y + 11, .76, C.gold)
  love.graphics.setScissor(v.left.x, v.left.y + 35, v.left.width, v.left.height - 35)
  for i, item in ipairs(list) do
    local r = role_rect(i, v); if r.y + r.height >= v.left.y + 35 and r.y <= v.left.y + v.left.height then
      local selected_row, mapped = item.key == selected, model:tile(item.key); box(r, selected_row and { .1, .24, .34 } or nil, selected_row and C.cyan or { .1, .14, .2 })
      if mapped then love.graphics.draw(sheet, quads[mapped[1] .. ":" .. mapped[2]], r.x + 6, r.y + 7) end
      text(item.label, r.x + 30, r.y + 7, .78, selected_row and C.gold or C.text, r.width - 84); text(mapped and (mapped[1] .. "," .. mapped[2]) or "—", r.x + r.width - 46, r.y + 8, .65, mapped and C.muted or C.red)
    end
  end
  love.graphics.setScissor()

  box(v.canvas, C.panel); love.graphics.setScissor(v.canvas.x, v.canvas.y, v.canvas.width, v.canvas.height); love.graphics.draw(sheet, v.sheet_x, v.sheet_y, 0, v.size / TILE, v.size / TILE)
  local function outline(c, r, tint, width) color(tint); love.graphics.setLineWidth(width); love.graphics.rectangle("line", v.sheet_x + (c - 1) * v.size, v.sheet_y + (r - 1) * v.size, v.size, v.size) end
  local current = tile(); if current then outline(current[1], current[2], C.cyan, 2) end
  local mx, my = love.mouse.getPosition(); local c, r = tile_at(mx, my, v); hover = c and { c, r } or nil; if hover then outline(c, r, C.gold, 3) end
  love.graphics.setLineWidth(1); love.graphics.setScissor(); text(hover and ("HOVER [" .. hover[1] .. ", " .. hover[2] .. "] • CLICK TO ASSIGN") or "WHEEL TO ZOOM • RIGHT/MIDDLE DRAG TO PAN", v.canvas.x + 4, v.canvas.y + v.canvas.height + 8, .67, hover and C.gold or C.muted)

  box(v.inspector, C.panel); local selected_role, current_tile = role(), tile(); text("ASSIGNMENT", v.inspector.x + 10, v.inspector.y + 11, .76, C.gold); text(selected_role.label, v.inspector.x + 10, v.inspector.y + 35, 1.06, C.text, v.inspector.width - 20); text(selected_role.category:upper() .. (selected_role.optional and " • OPTIONAL" or " • REQUIRED"), v.inspector.x + 10, v.inspector.y + 60, .66, selected_role.optional and C.muted or C.mint)
  if current_tile then love.graphics.draw(sheet, quads[current_tile[1] .. ":" .. current_tile[2]], v.inspector.x + 10, v.inspector.y + 84, 0, 3, 3); text("COLUMN", v.inspector.x + 58, v.inspector.y + 88, .62, C.muted); text(current_tile[1], v.inspector.x + 58, v.inspector.y + 102, 1.05, C.text); text("ROW", v.inspector.x + 122, v.inspector.y + 88, .62, C.muted); text(current_tile[2], v.inspector.x + 122, v.inspector.y + 102, 1.05, C.text) else text("UNASSIGNED", v.inspector.x + 10, v.inspector.y + 97, .86, C.red) end
  for _, control in ipairs(inspector_controls(v)) do box(control, (control.action == "clear" and not selected_role.optional) and { .045, .055, .07 } or C.panel2); text(control.label, control.x + 8, control.y + 7, .72, (control.action == "clear" and not selected_role.optional) and C.muted or C.text) end
  text("Arrow keys adjust coordinates", v.inspector.x + 10, v.inspector.y + 344, .67, C.muted); text("R resets • Delete clears optional", v.inspector.x + 10, v.inspector.y + 364, .67, C.muted)
  if hover then local used = model:roles_at(hover[1], hover[2]); local names = {}; for _, item in ipairs(used) do names[#names + 1] = item.label end; text("HOVERED TILE [" .. hover[1] .. ", " .. hover[2] .. "]", v.inspector.x + 10, v.inspector.y + 405, .72, C.gold); text(#names > 0 and ("ALSO USED BY: " .. table.concat(names, ", ")) or "No current role uses this tile.", v.inspector.x + 10, v.inspector.y + 429, .65, C.muted, v.inspector.width - 20) end
  for _, control in ipairs(bottom_controls(v)) do box(control, control.enabled and C.panel2 or { .045, .055, .07 }); text(control.label, control.x + 9, control.y + 9, .72, control.enabled and C.text or C.muted) end
  text(status, 12, v.h - 18, .7, C.muted, v.w - 24)
end

function love.mousepressed(x, y, button)
  local v = view(); if (button == 2 or button == 3) and inside(x, y, v.canvas) then panning = true; return end; if button ~= 1 then return end
  for _, item in ipairs(category_controls()) do if inside(x, y, item) then category, scroll, search_focus = item.value, 0, false; return end end
  if inside(x, y, v.search) then search_focus = true; return end
  for _, control in ipairs(bottom_controls(v)) do if inside(x, y, control) and control.enabled then action(control.action); return end end
  for i, item in ipairs(roles()) do if inside(x, y, role_rect(i, v)) then selected, search_focus, status = item.key, false, item.label .. " selected."; return end end
  for _, control in ipairs(inspector_controls(v)) do if inside(x, y, control) then if control.action == "clear" and not role().optional then status = "Core mappings cannot be cleared." else edit(control) end; return end end
  local c, r = tile_at(x, y, v); if c then model:assign(selected, c, r); status = role().label .. " assigned to [" .. c .. ", " .. r .. "]."; search_focus = false end
end
function love.mousereleased(_, _, button) if button == 2 or button == 3 then panning = false end end
function love.mousemoved(_, _, dx, dy) if panning then pan_x, pan_y = pan_x + dx, pan_y + dy end end
function love.wheelmoved(_, dy)
  local x, y = love.mouse.getPosition(); local v = view(); if inside(x, y, v.left) then scroll = clamp(scroll - dy, 0, math.max(0, #roles() - math.floor((v.left.height - 37) / 33))); return end; if not inside(x, y, v.canvas) or dy == 0 then return end
  local sx, sy = (x - v.sheet_x) / v.size, (y - v.sheet_y) / v.size; zoom = clamp(zoom * (1.14 ^ dy), .45, 6); local after = view(); pan_x, pan_y = x - after.base_x - sx * after.size, y - after.base_y - sy * after.size
end
function love.textinput(value) if search_focus then search, scroll = search .. value, 0 end end
function love.keypressed(key)
  local ctrl = love.keyboard.isDown("lctrl") or love.keyboard.isDown("rctrl") or love.keyboard.isDown("lgui") or love.keyboard.isDown("rgui")
  if ctrl and key == "s" then save(); return elseif ctrl and key == "z" then action("undo"); return elseif ctrl and (key == "y" or key == "r") then action("redo"); return end
  if key == "f" and not ctrl then search_focus = true; return end
  if search_focus then if key == "escape" or key == "return" then search_focus = false elseif key == "backspace" then search, scroll = search:sub(1, -2), 0 end; return end
  if key == "escape" then love.event.quit() elseif key == "left" then edit({ axis = "column", delta = -1 }) elseif key == "right" then edit({ axis = "column", delta = 1 }) elseif key == "up" then edit({ axis = "row", delta = 1 }) elseif key == "down" then edit({ axis = "row", delta = -1 }) elseif key == "r" then edit({ action = "reset" }) elseif key == "delete" or key == "backspace" then edit({ action = "clear" }) elseif key == "home" then action("fit") elseif key == "tab" then local list = roles(); local i = 1; for n, item in ipairs(list) do if item.key == selected then i = n end end; selected = list[i % #list + 1].key end
end
