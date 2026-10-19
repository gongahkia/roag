-- Shared temporary UI face. Authored theme glyphs can still replace HUD text.
local Fonts={}
local path='assets/fonts/BigBlue_TerminalPlus.ttf'

function Fonts.load(style,size)
    local graphics=love.graphics
    local fallback=graphics.getFont()
    local pixel=style~='sans'
    local sizes=({small={10,20,14},normal={12,24,16},large={14,26,18}})[size]
        or {12,24,16}
    local source=pixel and path or nil
    local function font(points)
        if source then return graphics.newFont(source,points) end
        return graphics.newFont(points)
    end
    local body=font(sizes[1])
    local display=font(sizes[2])
    local menu=font(sizes[3])
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
