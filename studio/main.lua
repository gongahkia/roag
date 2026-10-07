-- ROAG Studio is a standalone authoring workspace. It owns no run state.
local source = love.filesystem.getSource()
local root = source:match("^(.*)/studio$") or "."
package.path = source .. "/?.lua;" .. source .. "/?/init.lua;" .. root .. "/?.lua;" .. root .. "/?/init.lua;" .. package.path

local ScreenEditor = require("screen_editor")
local ModifierEditor = require("modifier_editor")
local ExpeditionWorkbench = require("expedition_workbench")
local CursorManager = require("src.ui.cursor_manager")
local mode, editor, fonts, cursor
local cards = {
  { title = "SCREEN COMPOSER", subtitle = "Edit validated JSON copy, layout and palette tokens.", action = "screens" },
  { title = "MODIFIER WORKBENCH", subtitle = "Author serialized Expedition passives and inspect their resolution traces.", action = "modifiers" },
  { title = "EXPEDITION WORKBENCH", subtitle = "Author validated chambers and encounters, then preview the real Expedition runtime.", action = "expedition" },
  { title = "VISUAL ATLAS", subtitle = "Loveable Rogue mapping is a validated fixed production contract; see its JSON and mapping guide.", command = "content/presentation/loveable_rogue_atlas.json" },
  { title = "SANDBOX ROOM TEMPLATES", subtitle = "Legacy Dungeon and Reactor room templates; this does not author Expedition chambers.", command = "love level_editor --room-editor" },
  { title = "GENERATION INSPECTOR", subtitle = "Inspect deterministic floors and environment overlays.", command = "love level_editor" },
}
local function color(v) love.graphics.setColor(v[1], v[2], v[3], v[4] or 1) end
local function inside(x, y, r) return x >= r.x and y >= r.y and x <= r.x + r.width and y <= r.y + r.height end
local function txt(v, x, y, scale, tint, limit) love.graphics.setFont(scale >= 1.7 and fonts.title or scale >= 1.1 and fonts.large or fonts.normal); color(tint or { .84, .89, .96 }); if limit then love.graphics.printf(v, x, y, limit) else love.graphics.print(v, x, y) end end
function love.load()
  fonts = { normal = love.graphics.newFont(14), large = love.graphics.newFont(19), title = love.graphics.newFont(30) }
  cursor = CursorManager.new(root .. "/assets/cursors/kenney/PNG/Basic/Default/"); cursor:load(); cursor:set("default"); mode = "home"
end
function love.update()
  local x, y = love.mouse.getPosition()
  if mode == "screens" or mode == "modifiers" or mode == "expedition" then
    cursor:set("action")
  else
    local active = false
    for _, card in ipairs(cards) do if card.rect and inside(x, y, card.rect) and card.action then active = true end end
    cursor:set(active and "action" or "default")
  end
end
function love.draw()
  if mode == "screens" or mode == "modifiers" or mode == "expedition" then editor:draw(); return end
  local w, h = love.graphics.getDimensions(); love.graphics.clear(.025, .035, .055)
  txt("ROAG STUDIO", 24, 24, 1.8, { .42, .84, 1 }); txt("A focused 2D authoring workspace — content tools stay isolated from active runs.", 24, 62, .8, { .52, .61, .72 })
  for i, card in ipairs(cards) do
    local r = { x = 28 + ((i - 1) % 2) * (w * .46), y = 122 + math.floor((i - 1) / 2) * 150, width = w * .41, height = 124 }; color({ .055, .075, .11 }); love.graphics.rectangle("fill", r.x, r.y, r.width, r.height, 6, 6); color(i == 1 and { .42, .84, 1 } or { .18, .25, .34 }); love.graphics.rectangle("line", r.x + .5, r.y + .5, r.width - 1, r.height - 1, 6, 6)
    txt(card.title, r.x + 16, r.y + 16, 1.05, i == 1 and { .95, .82, .24 } or { .84, .89, .96 }); txt(card.subtitle, r.x + 16, r.y + 49, .76, { .52, .61, .72 }, r.width - 32); txt(card.action and "CLICK TO OPEN" or card.command, r.x + 16, r.y + 93, .72, card.action and { .45, .9, .68 } or { .42, .84, 1 })
    card.rect = r
  end
  txt("Studio currently focuses on ROAG's own deterministic content format; it is not a replacement for a general-purpose engine.", 28, h - 40, .7, { .52, .61, .72 })
end
function love.mousepressed(x, y, button) if mode == "screens" or mode == "modifiers" or mode == "expedition" then if editor:mousepressed(x, y, button) == "back" then mode = "home" end; return end; if button == 1 then for _, card in ipairs(cards) do if card.action and inside(x, y, card.rect) then editor, mode = card.action == "screens" and ScreenEditor.new() or card.action == "modifiers" and ModifierEditor.new() or ExpeditionWorkbench.new(), card.action; return end end end end
function love.keypressed(key) if mode == "screens" or mode == "modifiers" or mode == "expedition" then if editor:keypressed(key) == "back" then mode = "home" end elseif key == "escape" then love.event.quit() end end
function love.textinput(value) if mode == "screens" or mode == "modifiers" or mode == "expedition" then editor:textinput(value) end end
