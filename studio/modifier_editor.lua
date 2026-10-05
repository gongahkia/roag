-- Compact surface over the tested ModifierEditor model. It intentionally
-- edits ordinary metadata and exposes registry-backed hook/effect inspection;
-- complex hook construction stays data-schema driven in the model for now.
local Model = require("src.expedition.modifier_editor_model")
local Registry = require("src.content.registry")

local Editor = {}
Editor.__index = Editor
local COLORS = { bg = { .025, .035, .055 }, panel = { .055, .075, .11 }, panel2 = { .075, .105, .15 }, border = { .18, .25, .34 }, text = { .84, .89, .96 }, muted = { .52, .61, .72 }, cyan = { .42, .84, 1 }, gold = { .95, .82, .24 }, mint = { .45, .9, .68 }, coral = { 1, .44, .34 } }
local function color(v) love.graphics.setColor(v[1], v[2], v[3], v[4] or 1) end
local function inside(x, y, r) return x >= r.x and y >= r.y and x <= r.x + r.width and y <= r.y + r.height end
local function box(r, fill, outline) if fill then color(fill); love.graphics.rectangle("fill", r.x, r.y, r.width, r.height, 5, 5) end; color(outline or COLORS.border); love.graphics.rectangle("line", r.x + .5, r.y + .5, r.width - 1, r.height - 1, 5, 5) end

function Editor.new()
  local self = setmetatable({ model = Model.new({ registry = Registry.load() }), fonts = {}, active = nil, draft = "", controls = {} }, Editor)
  self.fonts = { normal = love.graphics.newFont(14), small = love.graphics.newFont(12), title = love.graphics.newFont(27) }
  self.model:select(self.model.selected_id)
  return self
end
function Editor:text(text, x, y, scale, tint, limit)
  love.graphics.setFont(scale >= 1.5 and self.fonts.title or scale < .8 and self.fonts.small or self.fonts.normal); color(tint or COLORS.text)
  if limit then love.graphics.printf(text, x, y, limit) else love.graphics.print(text, x, y) end
end
function Editor:field(key, label, x, y, width)
  local definition = self.model:current(); local r = { x = x, y = y, width = width, height = 48 }; box(r, self.active == key and { .1, .24, .34 } or COLORS.panel2, self.active == key and COLORS.cyan or COLORS.border)
  self:text(label, x + 7, y + 5, .62, COLORS.muted); self:text(self.active == key and self.draft or tostring(definition[key] or ""), x + 7, y + 22, .78, self.active == key and COLORS.gold or COLORS.text, width - 14); return r
end
function Editor:draw()
  local w, h = love.graphics.getDimensions(); local model, definition = self.model, self.model:current(); love.graphics.clear(COLORS.bg)
  self:text("ROAG STUDIO / MODIFIER WORKBENCH", 16, 15, 1.55, COLORS.cyan); self:text("Serialized Expedition modifiers. Registry-backed triggers, effects and stack expressions; no active run is edited.", 16, 48, .7, COLORS.muted)
  self:text(model.dirty and "UNSAVED" or "SAVED", w - 105, 22, .7, model.dirty and COLORS.gold or COLORS.mint)
  local list = { x = 16, y = 84, width = 280, height = h - 144 }; box(list, COLORS.panel); self:text("MODIFIERS", 26, 94, .72, COLORS.gold)
  self.rows = {}
  for index, entry in ipairs(model:list()) do
    if index <= math.floor((list.height - 48) / 34) then
      local r = { x = list.x + 7, y = list.y + 30 + (index - 1) * 34, width = list.width - 14, height = 29 }; box(r, entry.id == model.selected_id and { .1, .24, .34 } or nil, entry.id == model.selected_id and COLORS.cyan or { .1, .14, .2 }); self:text(entry.name, r.x + 7, r.y + 5, .71, entry.enabled and COLORS.text or COLORS.muted); self:text(entry.enabled and "POOL" or "DRAFT", r.x + r.width - 45, r.y + 6, .55, entry.enabled and COLORS.mint or COLORS.gold); self.rows[#self.rows + 1] = { rect = r, id = entry.id }
    end
  end
  local edit = { x = 312, y = 84, width = w - 328, height = h - 144 }; box(edit, COLORS.panel); self:text("DEFINITION", edit.x + 12, edit.y + 10, .72, COLORS.gold)
  self.field_rects = { id = self:field("id", "ID", edit.x + 12, edit.y + 40, edit.width - 24), name = self:field("name", "NAME", edit.x + 12, edit.y + 95, edit.width - 24), description = self:field("description", "DESCRIPTION", edit.x + 12, edit.y + 150, edit.width - 24) }
  self:text("CATEGORY  " .. definition.category:upper() .. "   •   POOL " .. (definition.pool.enabled and "ENABLED" or "DISABLED") .. "   •   WEIGHT " .. definition.pool.weight, edit.x + 14, edit.y + 212, .7, COLORS.text)
  self:text("TAGS: " .. table.concat(definition.tags or {}, ", "), edit.x + 14, edit.y + 235, .68, COLORS.muted, edit.width - 28)
  self:text("STATIC EFFECTS", edit.x + 14, edit.y + 270, .68, COLORS.gold)
  local y = edit.y + 292
  for _, effect in ipairs(definition.static_effects or {}) do self:text(effect.stat:upper() .. " — " .. (effect.value.kind or "expression"), edit.x + 20, y, .66, COLORS.text); y = y + 19 end
  self:text("REACTIVE HOOKS", edit.x + edit.width * .52, edit.y + 270, .68, COLORS.gold)
  y = edit.y + 292
  for _, hook in ipairs(definition.hooks or {}) do self:text(hook.trigger:upper() .. " → " .. table.concat((function() local a = {}; for _, e in ipairs(hook.effects) do a[#a+1] = e.kind end; return a end)(), ", "), edit.x + edit.width * .52, y, .66, COLORS.text, edit.width * .43); y = y + 19 end
  local preview = model:description_preview(3) or {}; self:text("STACK PREVIEW ×3: " .. (preview.current or "—"), edit.x + 14, edit.y + edit.height - 91, .67, COLORS.mint, edit.width - 28); self:text("NEXT ×4: " .. (preview.next or "—"), edit.x + 14, edit.y + edit.height - 70, .64, COLORS.muted, edit.width - 28)
  self.controls = { { label = "NEW", action = "new", x = 16 }, { label = "DUP", action = "duplicate", x = 82 }, { label = "SAVE", action = "save", x = 148 }, { label = "VALIDATE", action = "validate", x = 220 }, { label = "SIMULATE", action = "simulate", x = 320 }, { label = "DELETE", action = "delete", x = 422 }, { label = "BACK", action = "back", x = 504 } }
  for _, control in ipairs(self.controls) do control.rect = { x = control.x, y = h - 48, width = control.label == "VALIDATE" and 91 or control.label == "SIMULATE" and 94 or 60, height = 32 }; box(control.rect, COLORS.panel2); self:text(control.label, control.rect.x + 7, control.rect.y + 9, .66, COLORS.text) end
  self:text(model.message or "", 586, h - 25, .66, COLORS.muted, w - 600)
end
function Editor:begin(key) self.active, self.draft = key, tostring(self.model:current()[key] or "") end
function Editor:commit()
  if not self.active then return end
  self.model:current()[self.active] = self.draft; self.model:touch(); self.active, self.draft = nil, ""
end
function Editor:mousepressed(x, y, button)
  if button ~= 1 then return end
  for _, control in ipairs(self.controls or {}) do if inside(x, y, control.rect) then
    self:commit()
    if control.action == "new" then self.model:create("expedition.passive.new_" .. tostring(#self.model:list() + 1))
    elseif control.action == "duplicate" then self.model:duplicate(self.model.selected_id, self.model.selected_id .. "_copy")
    elseif control.action == "save" then local ok, err = self.model:save(); self.model.message = ok and "Saved canonical JSON." or (err.message or err.reason)
    elseif control.action == "validate" then local ok, err = self.model:validate(); self.model.message = ok and "Valid definition." or (err.message or err.reason)
    elseif control.action == "simulate" then local result, err = self.model:simulate({ stack_count = 3, trigger = "on_hit", attack_tags = { projectile = true }, capabilities = { ["ability.electrical.discharge"] = true, electrical_discharge = true }); self.model.message = result and ("Simulation: " .. #result.effects .. " effect(s), trace " .. #result.trace.nodes .. " nodes.") or (err.message or err.reason)
    elseif control.action == "delete" then
      if self.model.pending_delete then self.model:confirm_delete(true); self.model.message = "Deleted definition and updated manifest."
      else local pending = self.model:request_delete(); if pending then self.model.message = "Delete requested — click DELETE again to confirm." end end
    else return "back" end
    return
  end end
  for _, row in ipairs(self.rows or {}) do if inside(x, y, row.rect) then self:commit(); local ok, err = self.model:select(row.id); if not ok then self.model.message = err.reason end; return end end
  for key, rect in pairs(self.field_rects or {}) do if inside(x, y, rect) then self:commit(); self:begin(key); return end end
end
function Editor:textinput(value) if self.active then self.draft = self.draft .. value end end
function Editor:keypressed(key)
  if self.active then if key == "return" then self:commit() elseif key == "escape" then self.active, self.draft = nil, "" elseif key == "backspace" then self.draft = self.draft:sub(1, -2) end; return end
  if key == "escape" then return "back" end
end

return Editor
