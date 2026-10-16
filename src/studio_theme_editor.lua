-- E04 game-specific theme chooser and realistic, presentation-only mock preview.
local Theme=require('src.studio_theme')
local UI=require('src.studio_chrome')
local Runtime=require('src.studio_theme_runtime')
local E={}
local views={'hud','title','chest','effect'}
local function copy(value)
    if type(value)~='table' then return value end
    local out={};for k,v in pairs(value) do out[k]=copy(v) end;return out
end
function E.new(theme,assets,image_for,image_data)
    local self=setmetatable({theme=copy(theme),assets=assets,image_for=image_for,
        selected=1,view=1,viewport=1,history={},future={},saved=Theme.encode(theme),
        message='Up/Down slot · Left/Right choose image · Tab preview · S save',
        input=nil,effect_started=0,preview_text='HP 24/40 LV 3 GOLD 12'}, {__index=E})
    self.renderer=Runtime.preview(self.theme,image_for,image_data)
    return self
end
function E:dirty() return Theme.encode(self.theme)~=self.saved end
function E:saved_now() self.saved=Theme.encode(self.theme) end
function E:change(fn)
    local before=Theme.encode(self.theme)
    local next_theme=copy(self.theme)
    local ok,err=pcall(fn,next_theme)
    if ok then ok,err=pcall(Theme.validate,next_theme,self.assets) end
    if not ok then self.message='Edit rejected: '..tostring(err);return false end
    local after=Theme.encode(next_theme)
    if after~=before then
        self.history[#self.history+1]=before
        if #self.history>32 then table.remove(self.history,1) end
        self.future={};self.theme=next_theme
        self.renderer.theme=self.theme;self.renderer.quads={};self.renderer.panel_quads={}
    end
    self.message='Theme updated' ;return true
end
function E:undo()
    local before=table.remove(self.history)
    if not before then self.message='Nothing to undo';return end
    self.future[#self.future+1]=Theme.encode(self.theme)
    self.theme=Theme.decode(before);self.renderer.theme=self.theme
    self.renderer.quads={};self.renderer.panel_quads={}
end
function E:redo()
    local after=table.remove(self.future)
    if not after then self.message='Nothing to redo';return end
    self.history[#self.history+1]=Theme.encode(self.theme)
    self.theme=Theme.decode(after);self.renderer.theme=self.theme
    self.renderer.quads={};self.renderer.panel_quads={}
end
function E:cycle_asset(delta)
    local slot=Theme.slots[self.selected]
    local ids={''}
    for _,asset in ipairs(self.assets) do
        if Theme.accepted[slot][asset.kind] and
            (asset.source~='' or asset.derived~='') then ids[#ids+1]=asset.id end
    end
    table.sort(ids)
    local current=self.theme.slots[slot];local at=1
    for i,id in ipairs(ids) do if id==current then at=i end end
    local selected=ids[(at-1+delta+#ids)%#ids+1]
    self:change(function(theme) theme.slots[slot]=selected end)
end
function E:start_input(field)
    self.input=field
    if field=='name' then self.typed=self.theme.name
    elseif field=='preview' then self.typed=self.preview_text
    elseif field=='glyphs' then self.typed=self.theme.font.characters
    elseif field=='metrics' then
        local f=self.theme.font
        self.typed=table.concat({f.glyph_width,f.glyph_height,f.advance,
            f.line_height,f.spacing},',')
    elseif field=='insets' then
        local p=self.theme.panel
        self.typed=table.concat({p.left,p.right,p.top,p.bottom},',')
    elseif field=='duration' then
        local key=Theme.slots[self.selected]=='boon_effect' and 'boon' or 'hit'
        self.typed=tostring(self.theme.effects[key].duration_ms)
    end
    self.message='Edit '..field..' · Enter accepts · Esc cancels'
end
function E:commit_input()
    local field,value=self.input,self.typed
    if field=='preview' then
        if #value<1 or #value>80 or value:find('[^ -~]') then
            self.message='Preview text must be 1–80 printable ASCII characters'
            return
        end
        self.preview_text=value;self.input=nil;self.typed=nil
        self.message='Preview text changed; source theme is unchanged'
        return
    end
    local numbers={}
    for number in value:gmatch('%d+') do numbers[#numbers+1]=tonumber(number) end
    local changed=self:change(function(theme)
        if field=='name' then theme.name=value
        elseif field=='glyphs' then theme.font.characters=value
        elseif field=='metrics' then
            assert(value:match('^%d+,%d+,%d+,%d+,%d+$') and #numbers==5,
                'enter width,height,advance,line height,spacing')
            local f=theme.font
            f.glyph_width,f.glyph_height,f.advance,f.line_height,f.spacing=
                numbers[1],numbers[2],numbers[3],numbers[4],numbers[5]
        elseif field=='insets' then
            assert(value:match('^%d+,%d+,%d+,%d+$') and #numbers==4,
                'enter left,right,top,bottom')
            local p=theme.panel;p.left,p.right,p.top,p.bottom=
                numbers[1],numbers[2],numbers[3],numbers[4]
        elseif field=='duration' then
            assert(value:match('^%d+$') and #numbers==1,
                'enter duration in milliseconds')
            local key=Theme.slots[self.selected]=='boon_effect' and 'boon' or 'hit'
            theme.effects[key].duration_ms=numbers[1]
        end
    end)
    if changed then self.input=nil;self.typed=nil end
end
function E:keypressed(key,control)
    if self.revert_confirm then
        if key=='return' or key=='y' then
            self.theme=Theme.decode(self.saved);self.renderer.theme=self.theme
            self.renderer.quads={};self.renderer.panel_quads={}
            self.history={};self.future={};self.revert_confirm=false
            self.message='Reverted theme metadata to the last save'
        elseif key=='escape' then self.revert_confirm=false
            self.message='Revert cancelled' end
        return true
    end
    if self.input then
        if key=='escape' then self.input=nil;self.typed=nil
        elseif key=='backspace' then self.typed=self.typed:sub(1,-2)
        elseif key=='return' then self:commit_input() end
        return true
    end
    if control and key=='z' then self:undo();return true end
    if control and key=='y' then self:redo();return true end
    if key=='up' then self.selected=math.max(1,self.selected-1)
    elseif key=='down' then self.selected=math.min(#Theme.slots,self.selected+1)
    elseif key=='left' then self:cycle_asset(-1)
    elseif key=='right' then self:cycle_asset(1)
    elseif key=='tab' then self.view=self.view%#views+1
    elseif key=='v' then self.viewport=self.viewport==1 and 2 or 1
    elseif key=='n' then self:start_input('name')
    elseif key=='r' then self.revert_confirm=true
        self.message='Discard theme edits since last save? Enter/Y confirms · Esc cancels'
    elseif key=='i' then self:start_input('insets')
    elseif key=='g' then self:start_input('glyphs')
    elseif key=='h' then self:start_input('preview')
    elseif key=='m' then self:start_input('metrics')
    elseif key=='d' then
        local slot=Theme.slots[self.selected]
        if slot=='hit_effect' or slot=='boon_effect' then
            self:start_input('duration')
        else self.message='Select a hit or boon effect slot first' end
    elseif key=='b' then self:change(function(theme)
        local modes={'center','fit','cover'};local at=1
        for i,mode in ipairs(modes) do if mode==theme.background.mode then at=i end end
        theme.background.mode=modes[at%3+1]
    end)
    elseif key=='p' then self.effect_started=love.timer.getTime()
        self.view=4;self.message='Cosmetic effect preview; game time remains untouched'
    elseif key=='[' or key==']' then
        local slot=Theme.slots[self.selected]
        if slot=='hit_effect' or slot=='boon_effect' then
            local name=slot=='hit_effect' and 'hit' or 'boon'
            self:change(function(theme)
                local effect=theme.effects[name]
                effect.scale=math.max(.25,math.min(4,
                    math.floor((effect.scale+(key==']' and .25 or -.25))*4+.5)/4))
            end)
        end
    else return false end
    return true
end
function E:textinput(text)
    if self.input and #self.typed+#text<=100 then self.typed=self.typed..text end
end
function E:mousepressed(x,y,button)
    if button~=1 then return end
    if x<300 and y>=133 and y<133+#Theme.slots*34 then
        self.selected=math.floor((y-133)/34)+1
    elseif y>=72 and y<=105 then
        if x<100 then self:keypressed('tab')
        elseif x<200 then self:keypressed('v')
        elseif x<300 then self:keypressed('p') end
    elseif x<300 and y>=485 and y<520 then self:cycle_asset(1) end
end
function E:draw()
    local g=love.graphics
    local width,height=g.getDimensions()
    UI.begin('ROAG  /  UI THEME','S save  ·  Esc assets  ·  Ctrl+Z/Y history  ·  R revert  ·  N name')
    UI.button(12,72,88,34,'Tab view',false,true)
    UI.button(100,72,100,34,'V size',false,true)
    UI.button(200,72,97,34,'P effect',false,true)
    g.setColor(UI.color.text)
    g.print(self.theme.name..(self:dirty() and ' * UNSAVED' or ''),18,111)
    UI.panel(12,131,285,308)
    for i,slot in ipairs(Theme.slots) do
        local y=133+(i-1)*34
        if i==self.selected then UI.selected(12,y,285,32) end
        g.setColor(UI.color.text)
        g.print(slot:gsub('_',' '),20,y+2)
        g.setColor(UI.color.muted)
        local id=self.theme.slots[slot]
        g.print(id=='' and '(default)' or id:sub(1,35),22,y+17)
    end
    g.setColor(UI.color.muted)
    g.print('←/→ bind art · click slot',16,449)
    g.print('I insets  G glyph map  H sample text',16,467)
    g.print('M metrics  B bg mode  D effect ms',16,483)
    g.print('[ / ] scale · font fallback',16,501)
    g.setColor(UI.color.warning)
    g.printf(self.message,18,height-76,width-36)
    if self.input then
        UI.modal(310,205,width-330,145)
        g.setColor(UI.color.text);g.print('EDIT '..self.input:upper(),333,225)
        g.print(self.typed,333,268)
        return
    end
    local vx,vy,vw,vh=316,94,math.max(320,width-337),math.max(320,height-155)
    UI.inset(vx,vy,vw,vh)
    local logical_w,logical_h=self.viewport==1 and 800 or 1024,
        self.viewport==1 and 600 or 640
    local factor=math.min(vw/logical_w,vh/logical_h)
    local px,py=vx+(vw-logical_w*factor)/2,vy+(vh-logical_h*factor)/2
    g.setScissor(vx,vy,vw,vh)
    g.push();g.translate(px,py);g.scale(factor)
    local renderer=self.renderer
    if views[self.view]=='title' then
        g.setColor(.08,.12,.11);g.rectangle('fill',0,0,logical_w,logical_h)
        local bg=renderer:image('title_background')
        if bg then
            local ox,oy,w,h=Theme.background_rect(self.theme.background.mode,
                bg:getWidth(),bg:getHeight(),logical_w,logical_h)
            g.setColor(1,1,1);g.draw(bg,ox,oy,0,w/bg:getWidth(),h/bg:getHeight())
        end
        g.setColor(.95,.96,.83);g.printf('ROAG — Four Biomes',0,120,logical_w,'center')
        g.printf('Continue  ·  New Run',0,220,logical_w,'center')
    elseif views[self.view]=='chest' then
        g.setColor(.16,.21,.19);g.rectangle('fill',0,0,logical_w,logical_h)
        if renderer:image('hud_panel') and renderer:draw_panel(70,100,640,390) then
            g.setColor(.02,.04,.04,.72)
            g.rectangle('fill',74,104,632,382)
        else
            g.setColor(.04,.06,.05,.9);g.rectangle('fill',70,100,640,390)
        end
        g.setColor(.98,.88,.52);g.print('CHEST · choose one of three',90,120)
        g.print('1. Storm Conductor [Rare]',90,170)
        g.print('2. Emberbrand [Uncommon]',90,240)
    else
        g.setColor(.12,.17,.16);g.rectangle('fill',0,0,logical_w,logical_h)
        local hud_x=logical_w-294
        g.setColor(.21,.27,.24);g.rectangle('fill',16,16,hud_x-24,logical_h-32)
        if renderer:image('hud_panel') then
            if renderer:draw_panel(hud_x,16,280,logical_h-32) then
                g.setColor(.015,.025,.025,.62)
                g.rectangle('fill',hud_x+3,19,274,logical_h-38)
            end
        end
        local text_x=hud_x+12
        g.setColor(.93,.95,.87);g.print('Overgrown Ruins · Stage 2',text_x,26)
        renderer:draw_icon('hp_icon',hud_x+250,58,18)
        renderer:draw_icon('xp_icon',hud_x+250,178,18)
        renderer:draw_icon('currency_icon',hud_x+250,195,18)
        if not renderer:draw_text(self.preview_text,text_x,58,2) then
            g.setColor(.93,.95,.87);g.print(self.preview_text,text_x,58) end
        g.setColor(.93,.95,.87);g.print('Level 3 · XP 18/50',text_x,178)
        g.print('Currency 12',text_x,195)
        g.print('Boons: Emberbrand · Flashpoint',text_x,350)
        if views[self.view]=='effect' then
            local effect=renderer:image('hit_effect') or renderer:image('boon_effect')
            if effect then
                local elapsed=love.timer.getTime()-self.effect_started
                local alpha=math.max(.25,1-elapsed)
                g.setColor(1,1,1,alpha)
                g.draw(effect,300,275,0,2,2)
            end
        end
    end
    g.pop();g.setScissor()
    UI.statusbar(height-55,55)
    g.setColor(UI.color.muted)
    g.print(('Preview: %s · %d×%d · %s'):format(views[self.view],logical_w,
        logical_h,self.theme.background.mode),vx,height-46)
end
return E
