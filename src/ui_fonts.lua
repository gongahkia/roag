-- Shared temporary UI face. Authored theme glyphs can still replace HUD text.
local Fonts={}
local path='assets/fonts/BigBlue_TerminalPlus.ttf'

function Fonts.load()
    local graphics=love.graphics
    local fallback=graphics.getFont()
    local body=graphics.newFont(path,12)
    local display=graphics.newFont(path,24)
    local menu=graphics.newFont(path,16)
    body:setFilter('nearest','nearest')
    display:setFilter('nearest','nearest')
    body:setFallbacks(fallback)
    display:setFallbacks(fallback)
    menu:setFilter('nearest','nearest')
    menu:setFallbacks(fallback)
    return body,display,menu
end

return Fonts
