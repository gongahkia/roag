-- Visual authoring surface for `content/screens/legacy.json`. It writes only
-- declarative presentation data and never mounts an active run or profile.
local ScreenManager = require("src.ui.screen_manager")

local Editor = {}
Editor.__index = Editor

local ACCENTS = { "cyan", "amber", "mint", "coral", "violet" }
local LAYOUTS = { "title_menu", "catalog", "list_detail", "route", "menu", "notice" }
local COLORS = { bg = { .025, .035, .055 }, panel = { .055, .075, .11 }, panel2 = { .075, .105, .15 }, border = { .18, .25, .34 }, text = { .84, .89, .96 }, muted = { .52, .61, .72 }, cyan = { .42, .84, 1 }, gold = { .95, .82, .24 }, mint = { .45, .9, .68 }, coral = { 1, .44, .34 }, violet = { .84, .58, .95 } }

local function color(value) love.graphics.setColor(value[1], value[2], value[3], value[4] or 1) end
local function inside(x, y, rect) return x >= rect.x and y >= rect.y and x <= rect.x + rect.width and y <= rect.y + rect.height end
local function copy(data)
  local encoded = assert(require("src.persistence.json").encode(data))
  return assert(require("src.persistence.json").decode(encoded))
end
local function cycle(values, current)
  for index, value in ipairs(values) do if value == current then return values[index % #values + 1] end end
  return values[1]
end

function Editor.new(options)
  options = options or {}
  local source = love.filesystem.getSource()
  local root = source:match("^(.*)/studio$") or "."
  local self = setmetatable({ path = options.path or (root .. "/content/screens/legacy.json"), root = root, selected = 1, active_field = nil, draft = nil, dirty = false, message = "Click a copy field to edit it. Changes are saved only to JSON.", fonts = {} }, Editor)
  self.fonts = { small = love.graphics.newFont(12), normal = love.graphics.newFont(14), large = love.graphics.newFont(19), title = love.graphics.newFont(30) }
  assert(self:reload())
  return self
end

function Editor:_read(path)
  local file, reason = io.open(path, "rb")
  if not file then return nil, reason end
  local contents = file:read("*a"); file:close(); return contents
end

function Editor:reload()
  local manager, failure = ScreenManager.load({ path = self.path, read = function(path) return self:_read(path) end })
  if not manager then self.message = failure.reason; return nil, failure end
  self.data, self.dirty, self.active_field, self.draft = manager:to_data(), false, nil, nil
  self.selected = math.max(1, math.min(self.selected, #self.data.screens))
  self.message = "Loaded “content/screens/legacy.json”."
  return true
end

function Editor:current()
  return self.data.screens[self.selected]
end

function Editor:save()
  if self.active_field then self:commit_field() end
  local payload, failure = ScreenManager.encode(self.data)
  if not payload then self.message = failure.reason; return nil, failure end
  local file, reason = io.open(self.path, "wb")
  if not file then self.message = tostring(reason); return nil, { code = "screen_write_failed", reason = tostring(reason) } end
  local ok, write_reason = file:write(payload); file:close()
  if not ok then self.message = tostring(write_reason); return nil, { code = "screen_write_failed", reason = tostring(write_reason) } end
  self.dirty, self.message = false, "Saved validated JSON. ROAG uses it at next launch."
  return true
end

function Editor:begin_field(field)
  self.active_field, self.draft = field, tostring(self:current()[field] or "")
end

function Editor:commit_field()
  if not self.active_field then return end
  self:current()[self.active_field] = self.draft
  self.active_field, self.draft, self.dirty = nil, nil, true
end

function Editor:cancel_field()
  self.active_field, self.draft = nil, nil
end

function Editor:layout(width, height)
  return { browser = { x = 14, y = 92, width = 270, height = height - 164 }, edit = { x = 302, y = 92, width = 350, height = height - 164 }, preview = { x = 670, y = 92, width = width - 684, height = height - 164 }, footer_y = height - 56 }
end

function Editor:_text(value, x, y, scale, tint, limit)
  love.graphics.setFont(scale >= 1.7 and self.fonts.title or scale >= 1.1 and self.fonts.large or scale < .8 and self.fonts.small or self.fonts.normal)
  color(tint or COLORS.text)
  if limit then love.graphics.printf(value, x, y, limit) else love.graphics.print(value, x, y) end
end

function Editor:_box(rect, fill, outline)
  if fill then color(fill); love.graphics.rectangle("fill", rect.x, rect.y, rect.width, rect.height, 5, 5) end
  color(outline or COLORS.border); love.graphics.rectangle("line", rect.x + .5, rect.y + .5, rect.width - 1, rect.height - 1, 5, 5)
end

function Editor:field_rect(index, layout)
  return { x = layout.edit.x + 10, y = layout.edit.y + 86 + (index - 1) * 72, width = layout.edit.width - 20, height = 62 }
end

function Editor:draw()
  local width, height = love.graphics.getDimensions(); local view = self:layout(width, height); local current = self:current()
  love.graphics.clear(COLORS.bg)
  self:_text("ROAG STUDIO / SCREEN COMPOSER", 14, 16, 1.7, COLORS.cyan)
  self:_text("Validated JSON screens — visual copy and hierarchy only; no simulation behavior is editable here.", 14, 52, .76, COLORS.muted)
  self:_text(self.dirty and "UNSAVED" or "SAVED", width - 94, 24, .74, self.dirty and COLORS.gold or COLORS.mint)
  self:_box(view.browser, COLORS.panel); self:_text("SCREENS", view.browser.x + 10, view.browser.y + 11, .76, COLORS.gold)
  for index, screen in ipairs(self.data.screens) do
    local rect = { x = view.browser.x + 7, y = view.browser.y + 38 + (index - 1) * 35, width = view.browser.width - 14, height = 30 }
    self:_box(rect, index == self.selected and { .1, .24, .34 } or nil, index == self.selected and COLORS.cyan or { .1, .14, .2 })
    self:_text(screen.title, rect.x + 8, rect.y + 6, .77, index == self.selected and COLORS.gold or COLORS.text, rect.width - 16)
    self:_text(screen.id, rect.x + 8, rect.y + 18, .59, COLORS.muted, rect.width - 16)
  end
  self:_box(view.edit, COLORS.panel); self:_text("SCREEN DATA", view.edit.x + 10, view.edit.y + 11, .76, COLORS.gold)
  self:_text("ID  " .. current.id .. "  •  " .. current.layout, view.edit.x + 10, view.edit.y + 37, .7, COLORS.muted)
  local fields = { { key = "title", label = "TITLE" }, { key = "subtitle", label = "SUBTITLE" }, { key = "footer", label = "FOOTER" } }
  for index, field in ipairs(fields) do
    local rect = self:field_rect(index, view); self:_box(rect, self.active_field == field.key and { .1, .24, .34 } or COLORS.panel2, self.active_field == field.key and COLORS.cyan or COLORS.border)
    self:_text(field.label, rect.x + 8, rect.y + 7, .62, COLORS.muted)
    local value = self.active_field == field.key and self.draft or current[field.key]
    self:_text(value, rect.x + 8, rect.y + 25, .78, self.active_field == field.key and COLORS.gold or COLORS.text, rect.width - 16)
    if self.active_field == field.key then self:_text("|", rect.x + 9 + #value * 7, rect.y + 25, .78, COLORS.cyan) end
  end
  local layout_rect = { x = view.edit.x + 10, y = view.edit.y + 310, width = view.edit.width - 20, height = 32 }
  local accent_rect = { x = view.edit.x + 10, y = view.edit.y + 350, width = view.edit.width - 20, height = 32 }
  self:_box(layout_rect, COLORS.panel2); self:_text("LAYOUT  " .. current.layout .. "  (CLICK TO CYCLE)", layout_rect.x + 8, layout_rect.y + 8, .7, COLORS.text)
  self:_box(accent_rect, COLORS.panel2); self:_text("ACCENT  " .. current.accent:upper() .. "  (CLICK TO CYCLE)", accent_rect.x + 8, accent_rect.y + 8, .7, COLORS[current.accent] or COLORS.text)
  self:_text("Enter commits text • Esc cancels text", view.edit.x + 10, view.edit.y + 404, .66, COLORS.muted)

  self:_box(view.preview, COLORS.panel); self:_text("LIVE PREVIEW", view.preview.x + 10, view.preview.y + 11, .76, COLORS.gold)
  local accent = COLORS[current.accent] or COLORS.cyan; local center = view.preview.x + view.preview.width / 2
  self:_text(current.title, center - #current.title * 8, view.preview.y + 86, 1.7, accent)
  self:_text(current.subtitle, center - #current.subtitle * 3.5, view.preview.y + 128, .75, COLORS.muted)
  if current.layout == "title_menu" or current.layout == "menu" or current.layout == "notice" then
    for index, label in ipairs({ "PRIMARY ACTION", "SECONDARY ACTION", "TERTIARY ACTION" }) do
      local r = { x = view.preview.x + view.preview.width * .16, y = view.preview.y + 180 + (index - 1) * 58, width = view.preview.width * .68, height = 46 }; self:_box(r, index == 1 and { .1, .24, .34 } or COLORS.panel2, index == 1 and accent or COLORS.border); self:_text((index == 1 and "> " or "  ") .. label, r.x + 12, r.y + 14, .8, index == 1 and COLORS.gold or COLORS.text)
    end
  else
    for index = 1, 4 do local r = { x = view.preview.x + 18, y = view.preview.y + 174 + (index - 1) * 54, width = view.preview.width - 36, height = 42 }; self:_box(r, index == 1 and { .1, .24, .34 } or COLORS.panel2, index == 1 and accent or COLORS.border); self:_text("CONTENT ROW " .. index, r.x + 10, r.y + 12, .75, index == 1 and COLORS.gold or COLORS.text) end
  end
  self:_text(current.footer, center - #current.footer * 3.3, view.preview.y + view.preview.height - 42, .68, COLORS.muted)
  local controls = { { label = "SAVE  ⌘S", action = "save", x = 14 }, { label = "RELOAD", action = "reload", x = 126 }, { label = "BACK", action = "back", x = 238 } }
  for _, control in ipairs(controls) do local r = { x = control.x, y = view.footer_y, width = 104, height = 34 }; self:_box(r, COLORS.panel2); self:_text(control.label, r.x + 8, r.y + 9, .72, COLORS.text); control.rect = r end
  self.controls = controls
  self:_text(self.message, 362, height - 18, .7, COLORS.muted, width - 376)
end

function Editor:mousepressed(x, y, button)
  if button ~= 1 then return end
  local view = self:layout(love.graphics.getDimensions())
  if self.controls then for _, control in ipairs(self.controls) do if inside(x, y, control.rect) then if control.action == "save" then self:save() elseif control.action == "reload" then self:reload() else return "back" end; return end end end
  for index in ipairs(self.data.screens) do if inside(x, y, { x = view.browser.x + 7, y = view.browser.y + 38 + (index - 1) * 35, width = view.browser.width - 14, height = 30 }) then if self.active_field then self:commit_field() end; self.selected = index; return end end
  for index, field in ipairs({ "title", "subtitle", "footer" }) do if inside(x, y, self:field_rect(index, view)) then if self.active_field and self.active_field ~= field then self:commit_field() end; self:begin_field(field); return end end
  if inside(x, y, { x = view.edit.x + 10, y = view.edit.y + 310, width = view.edit.width - 20, height = 32 }) then self:commit_field(); self:current().layout = cycle(LAYOUTS, self:current().layout); self.dirty = true; return end
  if inside(x, y, { x = view.edit.x + 10, y = view.edit.y + 350, width = view.edit.width - 20, height = 32 }) then self:commit_field(); self:current().accent = cycle(ACCENTS, self:current().accent); self.dirty = true end
end

function Editor:textinput(value) if self.active_field then self.draft = self.draft .. value end end
function Editor:keypressed(key)
  local ctrl = love.keyboard.isDown("lctrl") or love.keyboard.isDown("rctrl") or love.keyboard.isDown("lgui") or love.keyboard.isDown("rgui")
  if ctrl and key == "s" then self:save(); return end
  if self.active_field then if key == "return" then self:commit_field() elseif key == "escape" then self:cancel_field() elseif key == "backspace" then self.draft = self.draft:sub(1, -2) end; return end
  if key == "escape" then return "back" elseif key == "up" then self.selected = math.max(1, self.selected - 1) elseif key == "down" then self.selected = math.min(#self.data.screens, self.selected + 1) end
end

return Editor
