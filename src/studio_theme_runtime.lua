-- Optional E04 renderer. All counters, images and timers are presentation only.
local Theme=require('src.studio_theme')
local Store=require('src.studio_theme_store')
local R={}
function R.preview(theme,lookup,image_data)
    return setmetatable({theme=theme,visuals={get_id=function(_,id) return lookup(id) end,
        image_data=image_data or {}},
        quads={},panel_quads={},counts={panel=0,icon=0,glyph=0,background=0,effect=0}},
        {__index=R})
end
function R.inspect(read,visuals)
    local theme,status=Store.inspect_published(read)
    if not theme then return nil,status end
    if not visuals or visuals.status~='loaded' or
        visuals.project_id~=theme.project_id then
        return nil,'theme assets unavailable or from a different project'
    end
    local assets={}
    for _,asset in pairs(visuals.by_id) do assets[#assets+1]=asset end
    local valid,why=pcall(Theme.validate,theme,assets)
    if not valid then return nil,'theme asset mismatch: '..tostring(why) end
    for _,slot in ipairs(Theme.slots) do
        local id=theme.slots[slot]
        if id~='' and not (visuals.resolve_id and visuals:resolve_id(id) or
            visuals:get_id(id)) then
            return nil,'theme image unavailable: '..slot..' ('..id..')'
        end
    end
    return setmetatable({theme=theme,visuals=visuals,quads={},panel_quads={},
        counts={panel=0,icon=0,glyph=0,background=0,effect=0}},
        {__index=R}),status
end
function R:image(slot)
    local id=self.theme.slots[slot]
    return id~='' and self.visuals:get_id(id) or nil
end
function R:handle(slot)
    local id=self.theme.slots[slot]
    if id=='' then return nil end
    if self.visuals.resolve_id then return self.visuals:resolve_id(id) end
    local image=self.visuals:get_id(id)
    return image and {texture=image,width=image:getWidth(),height=image:getHeight(),
        offset_x=0,offset_y=0} or nil
end
local function draw_handle(handle,quad,...)
    if quad then love.graphics.draw(handle.texture,quad,...) 
    elseif handle.quad then love.graphics.draw(handle.texture,handle.quad,...)
    else love.graphics.draw(handle.texture,...) end
end
function R:draw_panel(x,y,w,h)
    local handle=self:handle('hud_panel')
    if not handle then return false end
    local ok,pieces=pcall(Theme.nine_slice,handle.width,handle.height,
        self.theme.panel,w,h)
    if not ok then return false end
    love.graphics.setColor(1,1,1,1)
    for _,piece in ipairs(pieces) do
        local key=table.concat({self.theme.slots.hud_panel,piece.sx,piece.sy,
            piece.sw,piece.sh},':')
        local quad=self.panel_quads[key]
        if not quad then
            quad=love.graphics.newQuad(handle.offset_x+piece.sx,
                handle.offset_y+piece.sy,piece.sw,piece.sh,
                handle.texture:getWidth(),handle.texture:getHeight())
            self.panel_quads[key]=quad
        end
        draw_handle(handle,quad,x+piece.x,y+piece.y,0,
            piece.w/piece.sw,piece.h/piece.sh)
    end
    self.counts.panel=self.counts.panel+1
    return true
end
function R:draw_icon(slot,x,y,size)
    local handle=self:handle(slot)
    if not handle then return false end
    love.graphics.setColor(1,1,1,1)
    draw_handle(handle,nil,x,y,0,size/handle.width,size/handle.height)
    self.counts.icon=self.counts.icon+1
    return true
end
function R:draw_background(slot,x,y,w,h)
    local handle=self:handle(slot)
    if not handle then return false end
    local ox,oy,dw,dh=Theme.background_rect(self.theme.background.mode,
        handle.width,handle.height,w,h)
    love.graphics.setScissor(x,y,w,h)
    love.graphics.setColor(1,1,1,1)
    draw_handle(handle,nil,x+ox,y+oy,0,dw/handle.width,dh/handle.height)
    love.graphics.setScissor()
    self.counts.background=self.counts.background+1
    return true
end
function R:draw_text(value,x,y,scale)
    local handle=self:handle('bitmap_font')
    if not handle then return false end
    local font=self.theme.font
    local id=self.theme.slots.bitmap_font
    local glyphs=self.quads[id]
    if not glyphs then
        local positions=Theme.glyphs(font,handle.width,handle.height)
        glyphs={}
        local pixels=self.visuals.image_data and
            (self.visuals.image_data['id:'..id] or self.visuals.image_data[id])
        for c,rect in pairs(positions) do
            local drawn=not pixels
            if pixels then
                local occupied=0
                for py=rect.y,rect.y+rect.h-1 do
                    for px=rect.x,rect.x+rect.w-1 do
                        local _,_,_,alpha=pixels:getPixel(px,py)
                        if alpha>0 then occupied=occupied+1 end
                    end
                end
                -- Sparse accidental marks are not useful replacements for an
                -- essential HUD character; use the built-in font for those.
                drawn=occupied>=math.max(4,math.floor(rect.w*rect.h*.1))
            end
            if drawn then glyphs[c]=love.graphics.newQuad(handle.offset_x+rect.x,
                handle.offset_y+rect.y,rect.w,rect.h,
                handle.texture:getWidth(),handle.texture:getHeight()) end
        end
        self.quads[id]=glyphs
    end
    scale=scale or 2
    local start=x
    local fallback=love.graphics.getFont()
    for i=1,#value do
        local c=value:sub(i,i)
        if c=='\n' then x=start;y=y+font.line_height*scale
        elseif glyphs[c] then
            love.graphics.setColor(1,1,1,1)
            draw_handle(handle,glyphs[c],x,y,0,scale,scale)
            x=x+(font.advance+font.spacing)*scale
            self.counts.glyph=self.counts.glyph+1
        else
            love.graphics.setColor(0.93,0.95,0.88,1)
            love.graphics.print(c,x,y)
            x=x+math.max(fallback:getWidth(c),(font.advance+font.spacing)*scale)
        end
    end
    return true
end
function R:draw_effect(event,playback,screen_position,visible)
    local preset,id=Theme.effect_for(self.theme,event,
        event and visible(event.x,event.y))
    if not preset then return false end
    local handle=self.visuals.resolve_id and self.visuals:resolve_id(id) or nil
    if not handle then
        local image=self.visuals:get_id(id)
        handle=image and {texture=image,width=image:getWidth(),height=image:getHeight()}
    end
    if not handle then return false end
    local item=playback.items[1]
    if not item then return false end
    if item.event~=event then
        local member=false
        for _,part in ipairs(item.events or {}) do
            if part==event then member=true;break end
        end
        if not member then return false end
    end
    local elapsed=item.duration-playback.remaining
    local life=math.min(item.duration,preset.duration_ms/1000)
    if elapsed<0 or elapsed>life then return false end
    local x,y=screen_position(event.x,event.y)
    local alpha=preset.alpha*(1-elapsed/life)
    local size=32*preset.scale
    love.graphics.setColor(1,1,1,alpha)
    draw_handle(handle,nil,x+16,y+16,0,size/handle.width,
        size/handle.height,handle.width/2,handle.height/2)
    love.graphics.setColor(1,1,1,1)
    self.counts.effect=self.counts.effect+1
    return true
end
return R
