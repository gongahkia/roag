-- Optional E04 renderer. All counters, images and timers are presentation only.
local Theme=require('src.studio_theme')
local Store=require('src.studio_theme_store')
local Slots=require('src.visual_slots')
local R={}
local function has_glyph_bindings(bindings)
    for byte=32,126 do
        if bindings[Slots.glyph_binding(string.char(byte))] then return true end
    end
    return false
end
function R.preview(theme,lookup,image_data,assets)
    local visuals={get_id=function(_,id) return lookup(id) end,
        image_data=image_data or {},bindings={}}
    for _,asset in ipairs(assets or {}) do
        if asset.binding~='' and (asset.source~='' or asset.derived~='') then
            visuals.bindings[asset.binding]=asset
        end
    end
    function visuals:resolve(binding)
        local asset=self.bindings[binding]
        local image=asset and lookup(asset.id)
        return image and {texture=image,width=image:getWidth(),
            height=image:getHeight()} or nil
    end
    return setmetatable({theme=theme,visuals=visuals,
        has_direct_glyphs=has_glyph_bindings(visuals.bindings),
        quads={},panel_quads={},counts={panel=0,icon=0,glyph=0,background=0,effect=0}},
        {__index=R})
end
function R.inspect(read,visuals)
    local theme,status=Store.inspect_published(read)
    local bindings=visuals and visuals.bindings or {}
    local direct=false
    for _,slot in ipairs(Theme.slots) do
        if bindings['core:ui/'..slot] then direct=true;break end
    end
    local direct_glyphs=has_glyph_bindings(bindings)
    direct=direct or direct_glyphs
    if not theme and not direct then return nil,status end
    if not visuals or visuals.status~='loaded' then
        return nil,'theme assets unavailable'
    end
    if theme and visuals.project_id~=theme.project_id then
        if not direct then return nil,'theme assets unavailable or from a different project' end
        theme=nil
    end
    local assets={}
    for _,asset in pairs(visuals.by_id) do assets[#assets+1]=asset end
    -- Individually published Artwork slots take precedence over the optional
    -- theme's asset choices. A stale theme cannot suppress valid direct art.
    local function prepare(candidate)
        local effective=Theme.decode(Theme.encode(candidate))
        for _,slot in ipairs(Theme.slots) do
            local asset=bindings['core:ui/'..slot]
            if asset then effective.slots[slot]=asset.id end
        end
        local valid,why=pcall(Theme.validate,effective,assets)
        if not valid then return nil,'theme asset mismatch: '..tostring(why) end
        for _,slot in ipairs(Theme.slots) do
            local id=effective.slots[slot]
            if id~='' and not (visuals.resolve_id and visuals:resolve_id(id) or
                visuals:get_id(id)) then
                return nil,'theme image unavailable: '..slot..' ('..id..')'
            end
        end
        return effective
    end
    local effective,reason=prepare(theme or Theme.new(visuals.project_id))
    local warning=not theme and status~='absent' and status or nil
    if not effective and direct then
        warning=reason
        effective,reason=prepare(Theme.new(visuals.project_id))
    end
    if not effective then return nil,reason end
    return setmetatable({theme=effective,visuals=visuals,
        warning=warning,has_direct_glyphs=direct_glyphs,quads={},panel_quads={},
        counts={panel=0,icon=0,glyph=0,background=0,effect=0}},
        {__index=R}),direct and 'loaded' or status
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
    local direct_bindings=self.visuals.bindings or {}
    if not handle and not self.has_direct_glyphs then return false end
    local font=self.theme.font
    local id=self.theme.slots.bitmap_font
    local glyphs=handle and self.quads[id] or {}
    if handle and not glyphs then
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
        local binding=Slots.glyph_binding(c)
        local direct= binding and direct_bindings[binding] and
            self.visuals:resolve(binding) or nil
        if c=='\n' then x=start;y=y+font.line_height*scale
        elseif direct then
            love.graphics.setColor(1,1,1,1)
            draw_handle(direct,nil,x,y,0,font.glyph_width*scale/direct.width,
                font.glyph_height*scale/direct.height)
            x=x+(font.advance+font.spacing)*scale
            self.counts.glyph=self.counts.glyph+1
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
