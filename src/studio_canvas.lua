-- Studio E01 canvas controller. Mouse and pen-as-mouse share this path.
local D=require('src.studio_document')
local C={}
local palette={{0,0,0,255},{255,255,255,255},{232,82,71,255},{243,173,55,255},
    {246,223,99,255},{85,174,107,255},{71,185,199,255},{64,109,187,255},
    {151,96,180,255},{231,130,180,255},{120,81,60,255},{127,139,145,255},
    {0,0,0,0},{255,255,255,128},{30,60,40,255},{230,220,180,255}}
local tools={'pencil','eraser','eyedropper','fill'}
local zooms={0.25,0.5,1,2,3,4,6,8,12,16,24,32}
local function hex(c) return string.format('%02X%02X%02X%02X',unpack(c)) end
local function fromhex(s)
    s=s:gsub('^#',''):upper()
    if not s:match('^%x%x%x%x%x%x%x%x$') then return nil end
    return {tonumber(s:sub(1,2),16),tonumber(s:sub(3,4),16),tonumber(s:sub(5,6),16),tonumber(s:sub(7,8),16)}
end
function C.new(doc)
    local self=setmetatable({doc=doc,tool='pencil',color={255,255,255,255},brush=1,zoom=8,panx=0,pany=0,
        dragging=false,panning=false,before=nil,undo_stack={},redo_stack={},bytes_history=0,
        texture=nil,texture_dirty=true,modal=nil,typed='',cursor_x=nil,cursor_y=nil,is_dirty=false,layer_offset=1}, {__index=C})
    self:fit();self.saved=D.encode(doc);return self
end
function C:fit()
    local vw=math.max(100,love.graphics.getWidth()-455)
    local vh=math.max(100,love.graphics.getHeight()-180)
    local target=math.min(vw/self.doc.width,vh/self.doc.height)
    self.zoom=0.25
    for _,z in ipairs(zooms) do if z<=target then self.zoom=z end end
    self.panx=0;self.pany=0
end
function C:rect()
    local w,h=love.graphics.getDimensions()
    return 210,100,math.max(100,w-450),math.max(100,h-160)
end
function C:origin()
    local x,y,w,h=self:rect()
    return x+math.floor((w-self.doc.width*self.zoom)/2)+self.panx,
        y+math.floor((h-self.doc.height*self.zoom)/2)+self.pany
end
function C:pixel_at(x,y)
    local rx,ry,rw,rh=self:rect()
    if x<rx or x>=rx+rw or y<ry or y>=ry+rh then return nil end
    local ox,oy=self:origin()
    local px,py=math.floor((x-ox)/self.zoom),math.floor((y-oy)/self.zoom)
    if px<0 or py<0 or px>=self.doc.width or py>=self.doc.height then return nil end
    return px,py
end
function C:dirty() return self.dragging or self.is_dirty end
function C:saved_now() self.saved=D.encode(self.doc);self.is_dirty=false end
function C:begin_change() self:finish();self.before=D.encode(self.doc) end
function C:commit()
    if not self.before then return end
    local after=D.encode(self.doc)
    if after~=self.before then
        self.undo_stack[#self.undo_stack+1]={before=self.before,after=after}
        self.bytes_history=self.bytes_history+#self.before+#after
        self.redo_stack={}
        while #self.undo_stack>32 or self.bytes_history>32*1024*1024 do
            local old=table.remove(self.undo_stack,1)
            self.bytes_history=self.bytes_history-#old.before-#old.after
        end
        self.texture_dirty=true
    end
    self.is_dirty=after~=self.saved
    self.before=nil
end
function C:finish()
    if self.dragging then self.dragging=false;self:commit() end
    self.panning=false
end
function C:undo()
    self:finish();local entry=table.remove(self.undo_stack)
    if not entry then return false end
    self.bytes_history=self.bytes_history-#entry.before-#entry.after
    self.redo_stack[#self.redo_stack+1]=entry
    self.doc=D.decode(entry.before);self.texture_dirty=true;self.is_dirty=entry.before~=self.saved;return true
end
function C:redo()
    self:finish();local entry=table.remove(self.redo_stack)
    if not entry then return false end
    self.undo_stack[#self.undo_stack+1]=entry
    self.bytes_history=self.bytes_history+#entry.before+#entry.after
    self.doc=D.decode(entry.after);self.texture_dirty=true;self.is_dirty=entry.after~=self.saved;return true
end
function C:change(fn)
    self:begin_change()
    local ok,err=pcall(function() fn();self:commit() end)
    if not ok then self.doc=D.decode(self.before);self.before=nil;return false,tostring(err) end
    return true
end
function C:set_tool(tool)
    self:finish();self.tool=tool
end
function C:zoom_at(direction,x,y)
    self:finish()
    local ox,oy=self:origin()
    local ix,iy=(x-ox)/self.zoom,(y-oy)/self.zoom
    local index=1;for i,z in ipairs(zooms) do if z==self.zoom then index=i end end
    index=math.max(1,math.min(#zooms,index+direction))
    self.zoom=zooms[index]
    local nx,ny=self:origin()
    self.panx=self.panx+math.floor(x-(nx+ix*self.zoom)+0.5)
    self.pany=self.pany+math.floor(y-(ny+iy*self.zoom)+0.5)
end
function C:mousepressed(x,y,button)
    if self.modal then return false end
    local rw=love.graphics.getWidth();local right=rw-230
    if button==2 or button==3 or (button==1 and love.keyboard.isDown('space')) then
        if button==3 or (button==1 and love.keyboard.isDown('space')) then
            self:finish();self.panning=true;self.lastx=x;self.lasty=y;return true
        end
        if button==2 then
            local px,py=self:pixel_at(x,y)
            if px then self.color=D.composite_pixel(self.doc,px,py);return true end
        end
    end
    if button~=1 then return false end
    local height=love.graphics.getHeight()
    if x>=right and y>=62 and y<92 then
        if x<right+105 then self:undo() else self:redo() end
        return true
    end
    if x>=right and y>=height-140 and y<height-80 then
        local row=math.floor((y-(height-140))/30)
        local col=math.floor((x-right)/54)+1
        local commands=row==0 and {'l','delete','pageup','pagedown'} or {'v','k','r'}
        if commands[col] then local handled,reason=self:keypressed(commands[col]);return true,reason end
    end
    if x>=right and y>=100 and y<230 then
        local row=math.floor((y-100)/30)+1
        if tools[row] then self:set_tool(tools[row]);return true end
    end
    if x>=right and y>=236 and y<304 then
        local col=math.floor((x-right)/26)+1;local row=math.floor((y-236)/30)
        local idx=row*8+col
        if palette[idx] then self.color={unpack(palette[idx])};return true end
    end
    if x>=right and y>=320 and y<350 then
        self.modal='color';self.typed=hex(self.color);return true
    end
    if x>=right and y>=375 and y<math.min(height-145,375+5*24) then
        local i=self.layer_offset+math.floor((y-375)/24)
        if self.doc.layers[i] then self:finish();self.doc.selected=self.doc.layers[i].id;self.is_dirty=D.encode(self.doc)~=self.saved;self.texture_dirty=true;return true end
    end
    local px,py=self:pixel_at(x,y)
    if not px then return false end
    self.cursor_x,self.cursor_y=px,py
    if self.tool=='eyedropper' then self.color=D.composite_pixel(self.doc,px,py);return true end
    if self.tool=='fill' then
        local ok,err=self:change(function() local changed,reason=D.fill(self.doc,px,py,self.color);if reason then error(reason) end end)
        return true,ok and nil or err
    end
    local l=D.active(self.doc)
    if l.locked or not l.visible then return true,'layer is locked or hidden' end
    self:begin_change();self.dragging=true;self.lastpx=px;self.lastpy=py
    D.line(self.doc,px,py,px,py,self.color,self.brush,self.tool=='eraser' and 'erase' or 'paint')
    self.texture_dirty=true;return true
end
function C:mousemoved(x,y,dx,dy)
    if self.panning then
        self.panx=math.floor(self.panx+(x-self.lastx)+0.5)
        self.pany=math.floor(self.pany+(y-self.lasty)+0.5)
        self.lastx=x;self.lasty=y;return
    end
    local px,py=self:pixel_at(x,y);self.cursor_x,self.cursor_y=px,py
    if not self.dragging then return end
    if not px then self:finish();return end
    if px~=self.lastpx or py~=self.lastpy then
        D.line(self.doc,self.lastpx,self.lastpy,px,py,self.color,self.brush,self.tool=='eraser' and 'erase' or 'paint')
        self.lastpx,self.lastpy=px,py;self.texture_dirty=true
    end
end
function C:mousereleased() self:finish() end
function C:wheelmoved(_,dy)
    if dy==0 then return end
    local x,y=love.mouse.getPosition()
    if x>=love.graphics.getWidth()-230 and y>=375 then
        self.layer_offset=math.max(1,math.min(math.max(1,#self.doc.layers-4),self.layer_offset-(dy>0 and 1 or -1)))
    else self:zoom_at(dy>0 and 1 or -1,x,y) end
end
function C:keypressed(key)
    if self.modal then
        if key=='escape' then self.modal=nil
        elseif key=='backspace' then self.typed=self.typed:sub(1,-2)
        elseif key=='return' then
            if self.modal=='color' then
                local c=fromhex(self.typed);if not c then return 'Enter 8 hex digits: RRGGBBAA' end
                self.color=c
            elseif self.modal=='rename' then
                local name=self.typed
                local ok,err=self:change(function() assert(#name>=1 and #name<=64,'layer name must be 1–64 characters');D.active(self.doc).name=name end)
                if not ok then return err end
            end
            self.modal=nil
        end
        return true
    end
    local ctrl=love.keyboard.isDown('lctrl','rctrl')
    if ctrl and key=='z' then if love.keyboard.isDown('lshift','rshift') then self:redo() else self:undo() end;return true end
    if ctrl and key=='y' then self:redo();return true end
    if key=='p' then self:set_tool('pencil')
    elseif key=='e' then self:set_tool('eraser')
    elseif key=='i' then self:set_tool('eyedropper')
    elseif key=='f' then self:set_tool('fill')
    elseif key=='[' or key=='leftbracket' then self.brush=math.max(1,self.brush/2)
    elseif key==']' or key=='rightbracket' then self.brush=math.min(8,self.brush*2)
    elseif key=='l' then return self:change(function() D.add_layer(self.doc) end)
    elseif key=='delete' then return self:change(function() D.delete_layer(self.doc) end)
    elseif key=='pageup' then return self:change(function() D.move_layer(self.doc,1) end)
    elseif key=='pagedown' then return self:change(function() D.move_layer(self.doc,-1) end)
    elseif key=='v' then return self:change(function() local l=D.active(self.doc);l.visible=not l.visible end)
    elseif key=='k' then return self:change(function() local l=D.active(self.doc);l.locked=not l.locked end)
    elseif key=='r' then self:finish();self.modal='rename';self.typed=D.active(self.doc).name
    elseif key=='h' then self:finish();self.modal='color';self.typed=hex(self.color)
    elseif key=='0' then self:fit()
    elseif key=='+' or key=='=' or key=='kp+' then self:zoom_at(1,love.graphics.getWidth()/2,love.graphics.getHeight()/2)
    elseif key=='-' then self:zoom_at(-1,love.graphics.getWidth()/2,love.graphics.getHeight()/2)
    else return false end
    return true
end
function C:textinput(t) if self.modal and #self.typed+#t<=64 then self.typed=self.typed..t end end
function C:refresh_texture()
    if not self.texture_dirty then return end
    local raw=D.flatten(self.doc);local data=love.image.newImageData(self.doc.width,self.doc.height)
    local i=1
    for y=0,self.doc.height-1 do for x=0,self.doc.width-1 do
        local r,g,b,a=raw:byte(i,i+3);i=i+4
        data:setPixel(x,y,r/255,g/255,b/255,a/255)
    end end
    if self.texture and self.texture.replacePixels then self.texture:replacePixels(data)
    else self.texture=love.graphics.newImage(data) end
    self.texture:setFilter('nearest','nearest');self.texture_dirty=false
end
function C:draw()
    local g=love.graphics;local w,h=g.getDimensions();local rx,ry,rw,rh=self:rect()
    g.setColor(0.93,0.94,0.86);g.print('PIXEL EDITOR · '..self.doc.id,18,16)
    g.setColor(0.69,0.83,0.78)
    local active=D.active(self.doc)
    g.print(('Tool %s · RGBA #%s · Brush %d · %d×%d · %d%% · Layer %s'):format(
        self.tool,hex(self.color),self.brush,self.doc.width,self.doc.height,self.zoom*100,
        active.name:sub(1,18)),18,42)
    g.print(('Cursor %s · %s · Undo %d / Redo %d'):format(
        self.cursor_x and (self.cursor_x..','..self.cursor_y) or 'outside',self:dirty() and 'UNSAVED' or 'Saved',
        #self.undo_stack,#self.redo_stack),18,65)
    local side=w-230
    g.setColor(0.28,0.4,0.37);g.rectangle('fill',side+1,62,103,28)
    g.rectangle('fill',side+107,62,103,28)
    g.setColor(1,1,1);g.print('Undo',side+24,68);g.print('Redo',side+130,68)
    g.setColor(0.12,0.16,0.17);g.rectangle('fill',rx,ry,rw,rh)
    local ox,oy=self:origin()
    g.setScissor(rx,ry,rw,rh)
    local step=math.max(8,self.zoom*2)
    for yy=ry,ry+rh,step do for xx=rx,rx+rw,step do
        local even=(math.floor((xx-ox)/step)+math.floor((yy-oy)/step))%2==0
        g.setColor(even and 0.28 or 0.38,even and 0.30 or 0.40,even and 0.31 or 0.41)
        g.rectangle('fill',xx,yy,step,step)
    end end
    self:refresh_texture()
    g.setColor(1,1,1);g.draw(self.texture,ox,oy,0,self.zoom,self.zoom)
    if self.zoom>=12 then
        g.setColor(0.15,0.17,0.18,0.24)
        for x=0,self.doc.width do g.line(ox+x*self.zoom,oy,ox+x*self.zoom,oy+self.doc.height*self.zoom) end
        for y=0,self.doc.height do g.line(ox,oy+y*self.zoom,ox+self.doc.width*self.zoom,oy+y*self.zoom) end
    end
    if self.cursor_x then
        g.setColor(1,1,0.4,0.9);g.rectangle('line',ox+self.cursor_x*self.zoom,oy+self.cursor_y*self.zoom,self.zoom,self.zoom)
    end
    g.setScissor()
    local sx=w-230
    g.setColor(0.15,0.22,0.23);g.rectangle('fill',sx,100,220,h-160)
    for i,t in ipairs(tools) do
        g.setColor(self.tool==t and 0.3 or 0.22,self.tool==t and 0.47 or 0.31,0.34)
        g.rectangle('fill',sx+4,100+(i-1)*30,210,28)
        g.setColor(1,1,1);g.print(('%s  %s'):format(({'P','E','I','F'})[i],t),sx+14,106+(i-1)*30)
    end
    for i,c in ipairs(palette) do
        local col=(i-1)%8;local row=math.floor((i-1)/8)
        g.setColor(0.65,0.65,0.65);g.rectangle('fill',sx+col*26+4,236+row*30,11,11)
        g.rectangle('fill',sx+col*26+15,247+row*30,11,11)
        g.setColor(0.3,0.3,0.3);g.rectangle('fill',sx+col*26+15,236+row*30,11,11)
        g.rectangle('fill',sx+col*26+4,247+row*30,11,11)
        g.setColor(c[1]/255,c[2]/255,c[3]/255,c[4]/255);g.rectangle('fill',sx+col*26+4,236+row*30,22,22)
        g.setColor(0.7,0.8,0.8);g.rectangle('line',sx+col*26+4,236+row*30,22,22)
    end
    g.setColor(0.88,0.9,0.82);g.print('H / click: RGBA hex #'..hex(self.color),sx+4,322)
    g.print('LAYERS  bottom to top',sx+4,350)
    local selected_index=1;for i,l in ipairs(self.doc.layers) do if l.id==self.doc.selected then selected_index=i end end
    self.layer_offset=math.max(1,math.min(self.layer_offset,math.max(1,#self.doc.layers-4)))
    if selected_index<self.layer_offset then self.layer_offset=selected_index end
    if selected_index>self.layer_offset+4 then self.layer_offset=selected_index-4 end
    for i=self.layer_offset,math.min(#self.doc.layers,self.layer_offset+4) do
        local l=self.doc.layers[i]
        local y=375+(i-self.layer_offset)*24
        g.setColor(l.id==self.doc.selected and 0.34 or 0.2,0.34,0.32)
        g.rectangle('fill',sx+4,y,210,22)
        g.setColor(1,1,1)
        g.print((l.visible and '[V]' or '[ ]')..(l.locked and '[L] ' or '[ ] ')..l.name,sx+7,y+3)
    end
    local buttons={{'Add','Del','Up','Down'},{'Show','Lock','Rename'}}
    for row=1,2 do for col,label in ipairs(buttons[row]) do
        local bx,by=sx+(col-1)*54,h-140+(row-1)*30
        g.setColor(0.26,0.38,0.36);g.rectangle('fill',bx+1,by+1,51,27)
        g.setColor(1,1,1);g.print(label,bx+5,by+6)
    end end
    g.setColor(0.72,0.84,0.81)
    g.print('L add · Del remove · R rename',18,h-51)
    g.print('V show · K lock · PgUp/PgDn order · Ctrl+Z/Y undo/redo',18,h-31)
    g.print('S save · Esc metadata · wheel zoom · middle/Space drag pan',18,h-12)
    if self.modal then
        g.setColor(0.02,0.04,0.05,0.94);g.rectangle('fill',rx+40,ry+100,rw-80,110)
        g.setColor(1,1,1);g.printf(self.modal=='color' and 'RGBA hex: RRGGBBAA' or 'Layer name',rx+50,ry+112,rw-100,'center')
        g.printf(self.typed,rx+50,ry+151,rw-100,'center')
    end
end
return C
