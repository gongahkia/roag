-- Shared temporary UI face. Authored theme glyphs can still replace HUD text.
local Fonts={}
local path='assets/fonts/BigBlue_TerminalPlus.ttf'

function Fonts.load(style,size)
    local graphics=love.graphics
    local fallback=graphics.getFont()
    local pixel=style~='sans'
    local large=size=='large'
    local source=pixel and path or nil
    local function font(points)
        if source then return graphics.newFont(source,points) end
        return graphics.newFont(points)
    end
    local body=font(large and 14 or 12)
    local display=font(large and 26 or 24)
    local menu=font(large and 18 or 16)
    if pixel then
        body:setFilter('nearest','nearest')
        display:setFilter('nearest','nearest')
        menu:setFilter('nearest','nearest')
    end
    body:setFallbacks(fallback)
    display:setFallbacks(fallback)
    menu:setFallbacks(fallback)
    return body,display,menu
end

return Fonts
