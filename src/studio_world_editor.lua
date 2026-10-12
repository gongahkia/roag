-- E03 LÖVE tile-grid editor. The document and edit operations live in
-- studio_world.lua; this controller owns only viewport/input/history state.
local W=require('src.studio_world')
local Rooms=require('src.rooms')
local Biomes=require('src.biomes')
local Generation=require('src.generation')
local E={}
local tools={'floor','wall','water','void','tile','editable','protected',
    'socket','enemy','chest','secret_anchor','secret_wall','stair'}
local tool_labels={'Floor','Wall','Water','Void','Tile','Edit','Prot',
    'Sock','Enemy','Chest','S.An','S.Wl','Stair'}
local colors={['.']={0.21,0.29,0.25},['#']={0.42,0.45,0.40},
    ['~']={0.15,0.38,0.56},[' ']={0.08,0.10,0.12}}
local function same(a,b)
    if type(a)~=type(b) then return false end
    if type(a)~='table' then return a==b end
    for k,v in pairs(a) do if not same(v,b[k]) then return false end end
    for k in pairs(b) do if a[k]==nil then return false end end
    return true
end
local function point(list,x,y)
    for _,p in ipairs(list or {}) do if p.x==x and p.y==y then return true end end
end
function E.new(pack,assets)
    local self=setmetatable({pack=pack,assets=assets or {},selected=1,tool='floor',
        zoom=24,panx=0,pany=0,drag=false,panning=false,lastx=nil,lasty=nil,
        before=nil,undo_stack={},redo_stack={},saved=W.clone(pack),
        selected_tile=1,overlay='all',preview_turn=0,preview_mirror=false,
        generated=nil,seed=1,biome_index=1,message='N new room · S save · Esc assets'},
        {__index=E})
    return self
end
function E:room() return self.pack.rooms[self.selected] end
function E:dirty() return self.drag or not same(self.pack,self.saved) end
function E:saved_now() self.saved=W.clone(self.pack) end
function E:viewport()
    local width,height=love.graphics.getDimensions()
    return 192,134,math.max(360,width-410),math.max(250,height-184)
end
function E:origin()
    local x,y,w,h=self:viewport();local room=self:room()
    if not room then return x,y end
    return x+math.floor((w-room.width*self.zoom)/2)+self.panx,
        y+math.floor((h-room.height*self.zoom)/2)+self.pany
end
function E:cell_at(px,py)
    local room=self:room();if not room then return nil end
    local x,y,w,h=self:viewport()
    if px<x or px>=x+w or py<y or py>=y+h then return nil end
    local ox,oy=self:origin()
    local cx,cy=math.floor((px-ox)/self.zoom)+1,math.floor((py-oy)/self.zoom)+1
    if cx<1 or cy<1 or cx>room.width or cy>room.height then return nil end
    return cx,cy
end
function E:begin()
    if not self.before then self.before=W.clone(self.pack) end
end
function E:commit()
    if not self.before then return end
    if not same(self.before,self.pack) then
        self.undo_stack[#self.undo_stack+1]=self.before
        if #self.undo_stack>32 then table.remove(self.undo_stack,1) end
        self.redo_stack={}
    end
    self.before=nil
end
function E:finish() self.drag=false;self.panning=false;self:commit() end
function E:undo()
    self:finish();local prior=table.remove(self.undo_stack)
    if not prior then return false end
    self.redo_stack[#self.redo_stack+1]=W.clone(self.pack)
    self.pack=prior;self.selected=math.min(self.selected,#self.pack.rooms)
    self.generated=nil;return true
end
function E:redo()
    self:finish();local next_value=table.remove(self.redo_stack)
    if not next_value then return false end
    self.undo_stack[#self.undo_stack+1]=W.clone(self.pack)
    self.pack=next_value;self.selected=math.min(self.selected,#self.pack.rooms)
    self.generated=nil;return true
end
function E:change(fn)
    self:finish();self:begin()
    local ok,err=pcall(fn)
    if not ok then self.pack=self.before;self.before=nil
        self.message=tostring(err);return false,tostring(err) end
    self:commit();self.generated=nil;return true
end
function E:paint(x,y)
    local room=self:room();if not room then return end
    local tool=self.tool
    if tool=='floor' then W.set_cell(room,x,y,'.');room.tile_cells[x..','..y]=nil
    elseif tool=='wall' then W.set_cell(room,x,y,'#');room.tile_cells[x..','..y]=nil
    elseif tool=='water' then W.set_cell(room,x,y,'~');room.tile_cells[x..','..y]=nil
    elseif tool=='void' then
        W.set_cell(room,x,y,' ');room.tile_cells[x..','..y]=nil
        room.visual_cells[x..','..y]=nil
        W.set_mask(room,'editable',x,y,false)
        W.set_mask(room,'protected',x,y,false)
    elseif tool=='tile' then
        local tile=self.pack.tiles[self.selected_tile]
        if tile then W.paint_tile(self.pack,room,x,y,tile.id) end
    elseif tool=='decoration' then
        if self.decor_asset then W.set_visual(room,x,y,self.decor_asset) end
    elseif tool=='clear_visual' then W.set_visual(room,x,y,'')
    elseif tool=='editable' or tool=='protected' then
        W.set_mask(room,tool,x,y,true)
    elseif tool=='socket' then
        local facing=y==1 and 'N' or y==room.height and 'S' or
            x==1 and 'W' or x==room.width and 'E'
        if not facing then self.message='Socket must be on a boundary';return end
        for _,socket in ipairs(room.sockets) do if socket.facing==facing then
            socket.x,socket.y=x,y
            W.set_cell(room,x,y,'.')
            local dx=facing=='W' and 1 or facing=='E' and -1 or 0
            local dy=facing=='N' and 1 or facing=='S' and -1 or 0
            W.set_cell(room,x+dx,y+dy,'.')
        end end
    elseif tool=='stair' or room.markers[tool] then W.add_marker(room,tool,x,y) end
end
local function line(x0,y0,x1,y1,visit)
    local dx,dy=math.abs(x1-x0),math.abs(y1-y0)
    local sx,sy=x0<x1 and 1 or -1,y0<y1 and 1 or -1
    local err=dx-dy
    while true do
        visit(x0,y0)
        if x0==x1 and y0==y1 then break end
        local e=2*err
        if e>-dy then err=err-dy;x0=x0+sx end
        if e<dx then err=err+dx;y0=y0+sy end
    end
end
function E:mousepressed(px,py,button)
    local room=self:room()
    if button==2 or button==3 or love.keyboard.isDown('space') then
        self.panning=true;return true
    end
    if button~=1 then return false end
    if py>=78 and py<124 and px>=195 and px<800 then
        local index=math.floor((px-195)/50)+1+
            (py>=101 and 12 or 0)
        if tools[index] then self.tool=tools[index];self.message='Tool: '..self.tool;return true end
    end
    if px<185 then
        if py>=145 and py<145+#self.pack.rooms*24 then
            self:finish();self.selected=math.floor((py-145)/24)+1;self.generated=nil;return true
        end
        return false
    end
    if not room then return false end
    if self.generated or self.preview_turn~=0 or self.preview_mirror then
        self.message='Preview is read-only. Press P/H to return to source orientation.'
        return true
    end
    local x,y=self:cell_at(px,py)
    if not x then return false end
    self:begin();self:paint(x,y)
    self.lastx,self.lasty=x,y
    self.drag=(self.tool=='floor' or self.tool=='wall' or self.tool=='water'
        or self.tool=='void' or self.tool=='tile' or self.tool=='editable'
        or self.tool=='protected' or self.tool=='decoration' or
        self.tool=='clear_visual')
    if not self.drag then self:commit() end
    self.generated=nil
    return true
end
function E:mousemoved(px,py,dx,dy)
    if self.panning then self.panx=self.panx+dx;self.pany=self.pany+dy;return end
    if not self.drag then return end
    local x,y=self:cell_at(px,py)
    if not x then self:finish();return end
    if x~=self.lastx or y~=self.lasty then
        line(self.lastx,self.lasty,x,y,function(a,b) self:paint(a,b) end)
        self.lastx,self.lasty=x,y
    end
end
function E:mousereleased() self:finish() end
function E:wheelmoved(_,dy)
    local before=self.zoom
    local px,py=-1,-1
    if love.mouse and love.mouse.getPosition then px,py=love.mouse.getPosition() end
    local cx,cy=self:cell_at(px,py)
    local ox,oy=self:origin()
    local fx,fy=(px-ox)/before,(py-oy)/before
    self.zoom=math.max(5,math.min(56,self.zoom+(dy>0 and 2 or -2)))
    if cx then
        local nx,ny=self:origin()
        self.panx=self.panx+math.floor(px-(nx+fx*self.zoom)+0.5)
        self.pany=self.pany+math.floor(py-(ny+fy*self.zoom)+0.5)
    end
    return before~=self.zoom
end
function E:validate()
    local ok,err=pcall(W.validate,self.pack,self.assets)
    self.message=ok and 'VALID: generator template and sockets connected' or
        'INVALID: '..tostring(err)
    return ok
end
function E:generate()
    if not self:validate() then return false end
    local biome=Biomes.order[self.biome_index]
    local ok,map=pcall(Generation.generate,self.seed,
        {biome_id=biome,studio_pack=self.pack})
    if not ok then self.message='Preview failed: '..tostring(map);return false end
    self.generated=map
    local placed={}
    for _,r in ipairs(map.rooms) do if map.studio_templates[r.template_id] then
        placed[#placed+1]=r.template_id..' ('..r.turns..(r.mirror and 'M' or '')..')' end end
    self.message=('Seed %d %s: %d authored rooms; retry %d%s'):format(
        self.seed,Biomes.get(biome).name,#placed,#map.diagnostics.attempts-1,
        map.diagnostics.fallback and ' fallback' or '')
    return true,placed
end
function E:keypressed(key,control)
    if control and key=='z' then self:undo();return true end
    if control and key=='y' then self:redo();return true end
    if self.generated and key=='escape' then self.generated=nil;return true end
    if key=='1' then self.tool='floor'
    elseif key=='2' then self.tool='wall'
    elseif key=='3' then self.tool='water'
    elseif key=='4' then self.tool='void'
    elseif key=='5' then self.tool='tile'
    elseif key=='6' then self.tool='editable'
    elseif key=='7' then self.tool='protected'
    elseif key=='8' then self.tool='socket'
    elseif key=='9' then self.tool='enemy'
    elseif key=='0' then self.tool='chest'
    elseif key=='y' then self.tool='stair'
    elseif key=='tab' then self.selected_tile=self.selected_tile%#self.pack.tiles+1
    elseif key=='z' then
        local tile=self.pack.tiles[self.selected_tile]
        self:change(function()
            local next_symbol=tile.symbol=='.' and '#' or tile.symbol=='#' and '~' or '.'
            tile.symbol=next_symbol
            tile.blocks_movement=next_symbol~='.'
            tile.blocks_sight=next_symbol=='#'
            tile.blocks_projectile=next_symbol=='#'
            for _,room in ipairs(self.pack.rooms) do
                for location,id in pairs(room.tile_cells) do if id==tile.id then
                    local x,y=location:match('^(%d+),(%d+)$')
                    W.set_cell(room,tonumber(x),tonumber(y),next_symbol)
                end end
            end
        end)
    elseif key=='o' then
        local overlays={'all','movement','sight','projectile','none'}
        local at=1;for i,v in ipairs(overlays) do if v==self.overlay then at=i end end
        self.overlay=overlays[at%#overlays+1]
    elseif key=='up' then self:finish();self.selected=math.max(1,self.selected-1);self.generated=nil
    elseif key=='down' then self:finish();self.selected=math.min(#self.pack.rooms,self.selected+1);self.generated=nil
    elseif key=='r' then
        local room=self:room();if room then self:change(function()
            local cycle={'combat','treasure','landmark','secret','start','elite'}
            local at=1;for i,v in ipairs(cycle) do if v==room.roles[1] then at=i end end
            room.roles={cycle[at%#cycle+1]}
            if room.roles[1]~='start' then room.markers.player={} end
            if room.roles[1]~='elite' then room.markers.elite={} end
            if room.roles[1]=='start' or room.roles[1]=='elite' then
                room.markers.enemy={}
            elseif room.roles[1]=='combat' and #room.markers.enemy==0 then
                room.markers.enemy={{x=(room.width+1)/2,y=(room.height+1)/2}}
            end
            local kind=({start='player',combat='enemy',treasure='chest',
                landmark='landmark',elite='elite'})[room.roles[1]]
            if kind and #room.markers[kind]==0 then
                room.markers[kind][1]={x=(room.width+1)/2,y=(room.height+1)/2}
            end
        end) end
    elseif key=='b' then
        local room=self:room();if room then self:change(function()
            local at=1;for i,v in ipairs(Biomes.order) do if v==room.biomes[1] then at=i end end
            room.biomes={Biomes.order[at%#Biomes.order+1]}
        end) end
    elseif key=='j' then
        local room=self:room();if room then self:change(function()
            local id=Biomes.order[self.biome_index]
            local found
            for i,v in ipairs(room.biomes) do if v==id then found=i end end
            if found then assert(#room.biomes>1,'room needs at least one biome')
                table.remove(room.biomes,found)
            else room.biomes[#room.biomes+1]=id;table.sort(room.biomes) end
        end) end
    elseif key=='c' or key=='l' or key=='q' then
        local room=self:room();if room then self:change(function()
            local role=key=='c' and 'treasure' or key=='l' and 'landmark' or 'secret'
            local found
            for i,v in ipairs(room.roles) do if v==role then found=i end end
            if found then assert(#room.roles>1,'room needs at least one role')
                table.remove(room.roles,found)
            else
                room.roles[#room.roles+1]=role;table.sort(room.roles)
                if role=='landmark' and #room.markers.landmark==0 then
                    room.markers.landmark={{x=(room.width+1)/2,y=(room.height+1)/2}}
                end
            end
        end) end
    elseif key=='m' then local room=self:room();if room then self:change(function() room.mirror=not room.mirror end) end
    elseif key=='t' then local room=self:room();if room then self:change(function()
        local n=#room.rotations;room.rotations=n==4 and {0} or n==1 and {0,1} or
            n==2 and {0,1,2,3} or {0}
    end) end
    elseif key=='kp+' or key=='=' then local room=self:room();if room then self:change(function() room.weight=math.min(20,room.weight+1) end) end
    elseif key=='kp-' or key=='-' then local room=self:room();if room then self:change(function() room.weight=math.max(1,room.weight-1) end) end
    elseif key=='p' then
        local room=self:room();if room then
            local at=1
            for i,turn in ipairs(room.rotations) do
                if turn==self.preview_turn then at=i end
            end
            self.preview_turn=room.rotations[at%#room.rotations+1]
            self.preview_mirror=room.mirror and self.preview_mirror or false
        end
    elseif key=='h' then
        local room=self:room()
        if room and room.mirror then self.preview_mirror=not self.preview_mirror
        else self.message='Mirror is disabled for this room' end
    elseif key=='v' then self:validate()
    elseif key=='g' then self:generate()
    elseif key=='[' then self.seed=math.max(1,self.seed-1)
    elseif key==']' then self.seed=self.seed+1
    elseif key=='f' then self.biome_index=self.biome_index%#Biomes.order+1
    else return false end
    return true
end
local function tile_color(symbol)
    local color=colors[symbol] or colors[' ']
    love.graphics.setColor(color)
end
function E:draw(image_for)
    local g=love.graphics
    local room=self:room()
    local width,height=g.getDimensions()
    g.setColor(0.92,0.95,0.88);g.print('ROEG STUDIO · WORLD / ROOM EDITOR',20,14)
    g.setColor(0.6,0.8,0.8)
    g.print('N new  D duplicate  X delete  S save  Esc assets  Ctrl+Z/Y undo/redo',20,39)
    g.setColor(0.14,0.20,0.21);g.rectangle('fill',6,76,177,height-130)
    g.setColor(0.87,0.91,0.84);g.print('ROOMS',15,87)
    for i,r in ipairs(self.pack.rooms) do
        if i==self.selected then g.setColor(0.24,0.39,0.38)
            g.rectangle('fill',9,141+(i-1)*24,170,23) end
        g.setColor(0.9,0.91,0.84);g.print(r.name:sub(1,20),14,145+(i-1)*24)
    end
    g.setColor(0.17,0.23,0.25);g.rectangle('fill',194,76,math.min(610,width-410),50)
    for i,tool in ipairs(tools) do
        local col=(i-1)%12;local x=195+col*50
        local y=77+math.floor((i-1)/12)*24
        if tool==self.tool then g.setColor(0.28,0.47,0.44);g.rectangle('fill',x,y,49,22) end
        g.setColor(0.88,0.94,0.87)
        g.print(tool_labels[i],x+2,y+4)
    end
    local vx,vy,vw,vh=self:viewport()
    g.setColor(0.08,0.12,0.14);g.rectangle('fill',vx,vy,vw,vh)
    if self.generated then
        local map=self.generated
        local step=math.min(vw/map.width,vh/map.height)
        local ox,oy=vx+(vw-map.width*step)/2,vy+(vh-map.height*step)/2
        g.setScissor(vx,vy,vw,vh)
        for y=1,map.height do for x=1,map.width do
            local c=map.rows[y]:sub(x,x)
            if c~='#' then tile_color(c);g.rectangle('fill',ox+(x-1)*step,oy+(y-1)*step,step,step) end
        end end
        for _,r in ipairs(map.rooms) do if map.studio_templates[r.template_id] then
            g.setColor(1,0.76,0.22)
            g.rectangle('line',ox+(r.x-1)*step,oy+(r.y-1)*step,r.width*step,r.height*step)
        end end
        g.setScissor()
    elseif room then
        local def=W.runtime_room(room,self.pack)
        local turns=self.preview_turn
        local permitted=false;for _,t in ipairs(room.rotations) do if t==turns then permitted=true end end
        if not permitted then turns=room.rotations[1] end
        local ok,transformed=pcall(Rooms.transform,def,turns,
            self.preview_mirror and room.mirror)
        if not ok then transformed=def end
        local ox,oy=self:origin()
        if transformed.width~=room.width then
            ox=vx+math.floor((vw-transformed.width*self.zoom)/2)+self.panx
            oy=vy+math.floor((vh-transformed.height*self.zoom)/2)+self.pany
        end
        g.setScissor(vx,vy,vw,vh)
        for y=1,transformed.height do for x=1,transformed.width do
            local symbol=transformed.rows[y]:sub(x,x)
            local sx,sy=ox+(x-1)*self.zoom,oy+(y-1)*self.zoom
            tile_color(symbol);g.rectangle('fill',sx,sy,self.zoom-1,self.zoom-1)
            if self.overlay=='movement' or self.overlay=='sight' or
                self.overlay=='projectile' then
                local blocked=self.overlay=='movement' and symbol~='.' or
                    (self.overlay=='sight' or self.overlay=='projectile') and symbol=='#'
                if blocked then
                    g.setColor(self.overlay=='movement' and 0.85 or 0.55,
                        0.19,self.overlay=='sight' and 0.85 or 0.28,0.42)
                    g.rectangle('fill',sx,sy,self.zoom-1,self.zoom-1)
                end
            end
            local asset_id=transformed.studio_visual_cells and
                transformed.studio_visual_cells[x..','..y]
            local image=asset_id and image_for and image_for(asset_id)
            if image then g.setColor(1,1,1);g.draw(image,sx,sy,0,
                self.zoom/image:getWidth(),self.zoom/image:getHeight()) end
            if self.zoom>=14 then g.setColor(0.06,0.10,0.11,0.3)
                g.rectangle('line',sx,sy,self.zoom,self.zoom) end
        end end
        for _,s in ipairs(transformed.sockets) do
            g.setColor(0.98,0.82,0.23);g.rectangle('line',ox+(s.x-1)*self.zoom+2,
                oy+(s.y-1)*self.zoom+2,self.zoom-4,self.zoom-4)
        end
        for kind,points in pairs(transformed.markers) do for _,p in ipairs(points) do
            g.setColor(kind=='enemy' and 0.95 or 0.32,kind=='enemy' and 0.34 or 0.93,0.54)
            g.circle('fill',ox+(p.x-0.5)*self.zoom,oy+(p.y-0.5)*self.zoom,
                math.max(2,self.zoom/6))
        end end
        if self.overlay~='none' then for _,p in ipairs(transformed.editable or {}) do
            g.setColor(0.26,0.75,0.92,0.35)
            g.rectangle('fill',ox+(p.x-1)*self.zoom,oy+(p.y-1)*self.zoom,self.zoom,self.zoom)
        end end
        if self.overlay=='all' then for _,p in ipairs(transformed.studio_protected or {}) do
            g.setColor(0.98,0.45,0.19,0.4)
            g.rectangle('fill',ox+(p.x-1)*self.zoom,oy+(p.y-1)*self.zoom,self.zoom,self.zoom)
        end end
        g.setScissor()
    else
        g.setColor(0.8,0.87,0.82);g.printf('No room. Press N to create one.',vx,vy+vh/2,vw,'center')
    end
    local px=width-203
    g.setColor(0.14,0.19,0.21);g.rectangle('fill',px,76,197,height-130)
    g.setColor(0.91,0.94,0.87)
    local lines={'WORLD SOURCE',room and room.id or '[no room]',
        room and (room.width..'×'..room.height..' · '..table.concat(room.roles,',')) or '',
        room and table.concat(room.biomes,','):gsub('core:biome/','') or '',
        room and ('Weight '..room.weight..'  Mirror '..tostring(room.mirror)) or '',
        room and ('Rotations '..table.concat(room.rotations,',')) or '',
        'Tool: '..self.tool,'Tile: '..self.pack.tiles[self.selected_tile].id,
        'Zoom: '..self.zoom..' px/tile','F1 name · F2 ID · I art',
        'U clear art · Tab tile · K new',
        'F3/F4 tile name/ID · Z type',
        'E deco · Y stair · Backspace erase',
        'R role · C/L/Q extra',
        'B biome · J toggle','F preview biome · O overlay',
        'T rotations · +/- weight','M mirror · P/H transform',
        'V validate · G generate','[ / ] seed '..self.seed,
        'Mouse paint · wheel zoom','Middle/space drag pan'}
    for i,text in ipairs(lines) do g.print(text:sub(1,30),px+8,88+(i-1)*22) end
    g.setColor(1,0.77,0.44);g.printf(self.message,194,height-42,width-206,'left')
end
return E
