local Project=require('src.studio_project')
local Store=require('src.studio_store')
local IO=require('src.studio_love_storage')
local Document=require('src.studio_document')
local Canvas=require('src.studio_canvas')
local StudioWorld=require('src.studio_world')
local StudioWorldStore=require('src.studio_world_store')
local WorldEditor=require('src.studio_world_editor')
local StudioTheme=require('src.studio_theme')
local StudioThemeStore=require('src.studio_theme_store')
local ThemeEditor=require('src.studio_theme_editor')
local StudioAudio=require('src.studio_audio')
local StudioAudioStore=require('src.studio_audio_store')
local AudioEditor=require('src.studio_audio_editor')
local Pipeline=require('src.studio_pipeline_status')
local GuiPublish=require('src.studio_gui_publish')
local VisualAssets=require('src.visual_assets')
local E06Views=require('src.studio_e06_views')
local UI=require('src.studio_chrome')
local utf8=require('utf8')
local App={}
function App.requested(args)
    for _,value in ipairs(args or {}) do if value=='--studio' then return true end end
    return false
end
local function decode(bytes,name,w,h)
    local data=love.filesystem.newFileData(bytes,name)
    local pixels=love.image.newImageData(data)
    assert(pixels:getWidth()==w and pixels:getHeight()==h,'decoded image dimensions differ')
    return pixels
end
local function slug(name)
    local value=name:lower():gsub('[^a-z0-9]+','_'):gsub('^_+',''):gsub('_+$','')
    if value=='' or not value:match('^[a-z]') then value='project_'..value end
    return value:sub(1,48)
end
function App.new()
    return setmetatable({store=Store.new(IO),world_store=StudioWorldStore.new(IO),
        theme_store=StudioThemeStore.new(IO),audio_store=StudioAudioStore.new(IO),
        project=nil,selected=1,dirty=false,
        mode='home',message='Create a project (N) or open one (O).',
        preview=nil,preview_id=nil,field=nil,typed='',pending=nil,listing={},list_index=1,
        filter_kind='all',world_images={},world_image_data={},
        draft_preview=false,help_open=false,pipeline_report=nil},
        {__index=App})
end
function App:load()
    love.window.setTitle('ROAG Studio — authored assets')
    love.window.setMode(love.graphics.getWidth(),love.graphics.getHeight(),{resizable=true,minwidth=1024,minheight=640})
    love.graphics.setBackgroundColor(UI.color.background)
    self.workspace=love.filesystem.getSaveDirectory()..'/studio/projects'
end
function App:status(text)
    self.message=text
    if self.world_editor then self.world_editor.message=text end
    if self.theme_editor then self.theme_editor.message=text end
    if self.audio_editor then self.audio_editor.message=text end
end
function App:ask_discard(action)
    if self.dirty then self.pending=action;self.return_mode=self.mode;self.mode='confirm'
        self:status('Unsaved edits. Enter/Y discards them; Esc keeps this project.')
    else action() end
end
function App:new_project(name)
    local id=slug(name)
    for _,saved_id in ipairs(self.store:list()) do
        if saved_id==id then self:status('Project ID already exists: '..id);return end
    end
    self.project=Project.new(id,name)
    self.loose_plan=nil;self.loose_publisher=nil;self.publish_last_backup=nil
    self.world_pack=StudioWorld.new(id);self.world_editor=nil
    self.world_images={};self.world_image_data={}
    self.world_blocked=false
    self.theme_pack=StudioTheme.new(id);self.theme_editor=nil;self.theme_blocked=false
    self.audio_pack=StudioAudio.new(id);self.audio_editor=nil;self.audio_blocked=false
    self.selected=1;self.dirty=true;self.metadata_dirty=true;self.mode='project';self.preview=nil;self.canvas=nil
    self:status('Created '..id..' (unsaved). Drag a PNG or press I for the sample.')
end
function App:open_project(id)
    local project,reason=self.store:open(id,decode)
    if not project then self:status('Open failed: '..tostring(reason));return end
    self.project=project;self.selected=1;self.dirty=false;self.metadata_dirty=false;self.mode='project'
    self.loose_plan=nil;self.loose_publisher=nil;self.publish_last_backup=nil
    local pack,world_reason=self.world_store:open(id,project.assets)
    self.world_pack=pack or StudioWorld.new(id)
    self.world_editor=nil;self.world_images={};self.world_image_data={}
    self.world_reason=world_reason
    self.world_blocked=not pack and world_reason~='missing world document'
    local theme,theme_reason=self.theme_store:open(id,project.assets)
    self.theme_pack=theme or StudioTheme.new(id)
    self.theme_editor=nil;self.theme_reason=theme_reason
    self.theme_blocked=not theme and theme_reason~='missing theme document'
    local audio,audio_reason=self.audio_store:open(id)
    self.audio_pack=audio or StudioAudio.new(id)
    self.audio_editor=nil;self.audio_reason=audio_reason
    self.audio_blocked=not audio and audio_reason~='missing audio document'
    self.preview=nil;self.preview_id=nil;self.canvas=nil
    self:status(reason..' '..id..(self.world_blocked and
        ' · World document unavailable: '..tostring(world_reason) or
        world_reason=='recovered previous world generation' and
        ' · recovered previous world generation' or '')..
        (self.theme_blocked and
        ' · Theme document unavailable: '..tostring(theme_reason) or
        theme_reason=='recovered previous theme generation' and
        ' · recovered previous theme generation' or '')..
        (self.audio_blocked and
        ' · Audio document unavailable: '..tostring(audio_reason) or
        audio_reason=='recovered previous audio generation' and
        ' · recovered previous audio generation' or ''))
end
function App:save()
    if not self.project then self:status('No project to save');return end
    if self.canvas then self.canvas:finish() end
    local ok,reason
    if self.canvas and (self.canvas:dirty() or self.canvas_asset.document=='') then
        ok,reason=self.store:save_document(self.project,self.canvas_asset,self.canvas.doc,
            decode,self.canvas.outline_color)
    else ok,reason=self.store:save(self.project,decode) end
    if ok and self.world_editor then
        self.world_editor:finish()
        self.world_pack=self.world_editor.pack
        ok,reason=self.world_store:save(self.world_pack,self.project.assets)
    end
    if ok and self.theme_editor then
        self.theme_pack=self.theme_editor.theme
        ok,reason=self.theme_store:save(self.theme_pack,self.project.assets)
    end
    if ok and self.audio_editor then
        self.audio_pack=self.audio_editor.pack
        ok,reason=self.audio_store:save(self.audio_pack)
    end
    if ok then
        if self.canvas then self.canvas:saved_now() end
        if self.world_editor then self.world_editor:saved_now() end
        if self.theme_editor then self.theme_editor:saved_now() end
        if self.audio_editor then self.audio_editor:saved_now() end
        self.dirty=false;self.metadata_dirty=false;self.preview=nil
        self.world_images={};self.world_image_data={}
        self:status('Saved generation '..reason)
    else self:status('Save failed: '..tostring(reason)) end
end
function App:open_audio(context)
    if not self.project then self:status('Open a project first');return end
    if self.audio_blocked then
        self:status('Audio editor blocked: '..tostring(self.audio_reason));return
    end
    if self.audio_editor and self.audio_editor:dirty() then
        self:status('Save audio edits before switching context');return
    end
    self.audio_return_mode=self.mode
    self.audio_editor=AudioEditor.new(self.audio_pack,self.audio_store,IO,
        self.world_editor and self.world_editor.pack or self.world_pack,context)
    self.mode='audio'
    self:status('Sound Library / Cue Picker · drop WAV/Ogg · S save · F search')
end
function App:open_theme()
    if not self.project then self:status('Open a project first');return end
    if self.theme_blocked then
        self:status('Theme editor blocked: '..tostring(self.theme_reason));return end
    self.theme_editor=ThemeEditor.new(self.theme_pack,self.project.assets,
        function(id) return self:world_asset_image(id) end,self.world_image_data)
    self.mode='theme'
    self:status('UI / Presentation: choose asset slots, Tab preview, S save.')
end
function App:open_world()
    if not self.project then self:status('Open a project first');return end
    if self.world_blocked then
        self:status('World editor blocked: '..tostring(self.world_reason));return end
    self.world_editor=WorldEditor.new(self.world_pack,self.project.assets)
    self.mode='world';self:status('World editor: N create room, paint with mouse, V validate, G preview.')
end
function App:create_room(w,h)
    local editor=self.world_editor
    if not editor or #editor.pack.rooms>=StudioWorld.max_rooms then
        self:status('Room limit reached');return end
    local n=1;local id
    repeat
        id=string.format('studio:room/room_%03d',n);n=n+1
        local found=false
        for _,r in ipairs(editor.pack.rooms) do if r.id==id then found=true end end
        if not found then break end
    until n>1000
    local room=StudioWorld.new_room(id,'Room '..(n-1),w,h)
    editor:change(function() editor.pack.rooms[#editor.pack.rooms+1]=room end)
    editor.selected=#editor.pack.rooms;self.world_pack=editor.pack
    self.dirty=true;self.mode='world';self:status('Created '..id..'. Paint or edit, then S to save.')
end
function App:world_asset_image(id)
    if self.canvas and self.canvas_asset and self.canvas_asset.id==id and
        self.canvas:dirty() then
        self.canvas:refresh_texture()
        return self.canvas.texture
    end
    if self.world_images[id] then return self.world_images[id] end
    for _,asset in ipairs(self.project.assets) do if asset.id==id then
        local path=asset.derived~='' and asset.derived or asset.source
        if path=='' then return nil end
        local bytes=IO:read('studio/projects/'..self.project.id..'/'..path)
        if not bytes then return nil end
        local ok,image=pcall(function()
            local width,height=Project.output_dimensions(asset)
            local pixels=decode(bytes,path,width,height)
            local value=love.graphics.newImage(pixels)
            value:setFilter(asset.filter,asset.filter)
            self.world_image_data[id]=pixels
            return value
        end)
        if ok then self.world_images[id]=image;return image end
    end end
end
function App:inspect_pipeline()
    if not self.project then self:status('Open a project first');return end
    local base='studio/projects/'..self.project.id..'/'
    local source_read=function(path) return IO:read(base..path) end
    local published_read=function(path) return love.filesystem.read(path) end
    local verify_page=function(bytes,page)
        local ok,pixels=pcall(function()
            return love.image.newImageData(love.filesystem.newFileData(bytes,page.filename))
        end)
        return ok and pixels and pixels:getWidth()==page.width and
            pixels:getHeight()==page.height
    end
    local ok,report=pcall(Pipeline.inspect,self.project,source_read,
        published_read,self.world_pack,self.theme_pack,verify_page)
    if not ok then self:status('Publish readiness failed: '..tostring(report));return end
    self.pipeline_report=report;self.mode='pipeline'
    self.loose_plan=nil;self.loose_publisher=nil
    self:status('Publish readiness · D previews loose files · Esc returns')
end
function App:preview_loose_publish()
    if not self.project then self:status('Open a project first');return end
    if self.dirty or self.canvas and self.canvas:dirty() or
        self.world_editor and self.world_editor:dirty() or
        self.theme_editor and self.theme_editor:dirty() or
        self.audio_editor and self.audio_editor:dirty() then
        self:status('Save all Studio edits (S) before publishing.');return
    end
    local saved,reason=self.store:open(self.project.id,decode)
    if not saved then self:status('Saved project unavailable: '..tostring(reason));return end
    if Project.encode(saved)~=Project.encode(self.project) then
        self:status('Saved project changed outside Studio; reopen it before publishing.')
        return
    end
    local ok,publisher,plan=pcall(function()
        local target=GuiPublish.new(love.filesystem.getSource())
        local base='studio/projects/'..saved.id..'/'
        local preview=target:plan(saved,function(path)
            return IO:read(base..path)
        end)
        return target,preview
    end)
    if not ok then self:status('Loose publish preview failed: '..tostring(publisher));return end
    self.loose_publisher,self.loose_plan,self.loose_project=publisher,plan,saved
    self:status(plan.blocked or 'Dry run only. Review files, then press A to apply.')
end
function App:confirm_loose_publish()
    local plan=self.loose_plan
    if not plan then self:status('Press D to preview the exact files first.');return end
    if plan.blocked then self:status(plan.blocked);return end
    if plan.counts.create+plan.counts.overwrite==0 then
        self:status('Published loose visuals are already current.');return
    end
    self.typed='';self.mode='publish_confirm'
    self:status(plan.counts.overwrite>0 and
        'Type OVERWRITE and press Enter to replace the published manifest.' or
        'Press Enter to publish the listed new files; Esc cancels.')
end
function App:backup_published_manifest(bytes)
    local base='studio/publish-backups/'..self.project.id
    IO:mkdir(base)
    for number=1,9999 do
        local path=base..('/manifest-%04d.txt'):format(number)
        if not love.filesystem.getInfo(path) then
            IO:write(path,bytes)
            assert(IO:read(path)==bytes,'published manifest backup read-back mismatch')
            return love.filesystem.getSaveDirectory()..'/'..path
        end
    end
    error('published manifest backup directory is full')
end
function App:apply_loose_publish()
    local plan=self.loose_plan
    if not plan or not self.loose_publisher then
        self.mode='pipeline';self:status('Preview the files again.');return
    end
    if self.dirty then
        self.mode='pipeline';self.loose_plan=nil
        self:status('Studio edits changed; save and preview again.');return
    end
    local saved,reason=self.store:open(self.project.id,decode)
    if not saved or Project.encode(saved)~=Project.encode(self.loose_project) then
        self.mode='pipeline';self.loose_plan=nil
        self:status('Saved project changed; reopen or preview again: '..tostring(reason))
        return
    end
    local base='studio/projects/'..saved.id..'/'
    local made_backup
    local ok,applied,why,backup=pcall(function()
        return self.loose_publisher:apply(saved,function(path)
            return IO:read(base..path)
        end,plan,plan.counts.overwrite>0,function(bytes)
            made_backup=self:backup_published_manifest(bytes)
            return made_backup
        end)
    end)
    if not ok then why=applied;applied=false end
    self.mode='pipeline';self.loose_plan=nil;self.loose_publisher=nil
    self.publish_last_backup=backup or made_backup or self.publish_last_backup
    if not applied then
        self:inspect_pipeline()
        self:status('Publish stopped: '..tostring(why)..' Press P, then D to preview again.')
        return
    end
    self:inspect_pipeline()
    self:status('Published loose art to this checkout. Game: F5 refreshes visuals.'..
        (backup and ' Previous manifest backed up.' or ''))
end
function App:create_sprite(w,h)
    if not self.project then self:status('Create or open a project first');return end
    if #self.project.assets>=256 then self:status('Project asset limit reached');return end
    local n=1;local id
    repeat
        id=string.format('studio:actor/sprite_%03d',n);n=n+1
        local found=false;for _,a in ipairs(self.project.assets) do if a.id==id then found=true end end
        if not found then break end
    until n>1000
    local doc=Document.new(id,w,h)
    local a=Project.asset(id,'New sprite','actor')
    a.width=w;a.height=h;a.format='png'
    self.project.assets[#self.project.assets+1]=a
    self.selected=#self.project.assets;self.filter_kind='all';self.canvas=Canvas.new(doc)
    self.canvas_asset=a;self.mode='canvas';self.dirty=true;self.metadata_dirty=true
    self:status('Transparent sprite created. Draw, then press S to save.')
end
function App:asset_document(a)
    if a.document~='' then return self.store:document(self.project,a) end
    assert(a.source~='','Draft has no pixels; press C to create a sprite')
    assert(a.width<=Document.max_dimension and a.height<=Document.max_dimension,
        'image exceeds 512-pixel editing limit')
    local bytes=assert(IO:read('studio/projects/'..self.project.id..'/'..a.source))
    local pixels=decode(bytes,a.source,a.width,a.height)
    local raw={}
    for y=0,a.height-1 do for x=0,a.width-1 do
        local r,g,b,alpha=pixels:getPixel(x,y)
        raw[#raw+1]=string.char(math.floor(r*255+0.5),math.floor(g*255+0.5),
            math.floor(b*255+0.5),math.floor(alpha*255+0.5))
    end end
    return Document.from_rgba(a.id,a.width,a.height,table.concat(raw))
end
function App:attach_reference(canvas,a)
    local reference_id=a.reference_id
    if not reference_id or reference_id=='' then
        local base=a.binding and a.binding:match('^(.-)/attack$') or
            (a.binding and a.binding:match('^(.-)/hurt$'))
        if base then
            for _,candidate in ipairs(self.project.assets) do
                if candidate.binding==base then reference_id=candidate.id;break end
            end
        end
    end
    if not reference_id or reference_id=='' then return end
    for _,candidate in ipairs(self.project.assets) do
        if candidate.id==reference_id then
            local ok,doc=pcall(self.asset_document,self,candidate)
            if ok and doc.width==canvas.doc.width and doc.height==canvas.doc.height then
                canvas:set_reference(doc,candidate.name)
            end
            return
        end
    end
end
function App:edit_pixels()
    local a=self:selected_asset()
    if not a then self:status('Select an asset first');return end
    local ok,doc=pcall(self.asset_document,self,a)
    if not ok then self:status('Pixel editor unavailable: '..tostring(doc));return end
    self.canvas=Canvas.new(doc,a.outline_color);self.canvas_asset=a;self.mode='canvas'
    self:attach_reference(self.canvas,a)
    self:status('Pixel editor: mouse or pen-as-mouse draws on the selected layer.')
end
function App:duplicate_sprite()
    local original=self:selected_asset()
    if not original then self:status('Select an asset first');return end
    if #self.project.assets>=256 then self:status('Project asset limit reached');return end
    local ok,doc=pcall(self.asset_document,self,original)
    if not ok then self:status('Copy unavailable: '..tostring(doc));return end
    local n,id=1,nil
    repeat
        id=string.format('studio:%s/copy_%03d',original.kind,n);n=n+1
        local found=false
        for _,asset in ipairs(self.project.assets) do if asset.id==id then found=true;break end end
        if not found then break end
    until n>1000
    if n>1001 then self:status('No free copy asset ID');return end
    local copy=Project.asset(id,(original.name..' copy'):sub(1,80),original.kind)
    copy.filter=original.filter
    copy.reference_id=original.id
    local reference_doc=doc
    doc=Document.decode(Document.encode(doc))
    doc.id=id
    self.project.assets[#self.project.assets+1]=copy
    self.selected=#self.project.assets;self.filter_kind='all'
    self.canvas=Canvas.new(doc,original.outline_color)
    self.canvas:set_reference(reference_doc,original.name)
    self.canvas_asset=copy;self.mode='canvas';self.dirty=true;self.metadata_dirty=true
    self:status('Copied layers. G toggles the original overlay; S saves this separate sprite.')
end
function App:selected_asset()
    local asset=self.project and self.project.assets[self.selected]
    if asset and self.filter_kind~='all' and asset.kind~=self.filter_kind then return nil end
    return asset
end
function App:visible_assets()
    local result={}
    for i,a in ipairs(self.project and self.project.assets or {}) do
        if self.filter_kind=='all' or a.kind==self.filter_kind then result[#result+1]=i end
    end
    table.sort(result,function(i,j)
        local a,b=self.project.assets[i],self.project.assets[j]
        if a.kind~=b.kind then return a.kind<b.kind end
        return a.id<b.id
    end)
    return result
end
function App:add_draft()
    if not self.project then self:status('Create or open a project first');return end
    if #self.project.assets>=256 then self:status('Project asset limit reached');return end
    local n=1;local id
    repeat
        id=string.format('studio:actor/asset_%03d',n);n=n+1
        local found=false
        for _,a in ipairs(self.project.assets) do if a.id==id then found=true end end
        if not found then break end
    until n>1000
    local a=Project.asset(id,'Untitled asset','actor')
    self.project.assets[#self.project.assets+1]=a
    self.selected=#self.project.assets;self.filter_kind='all';self.dirty=true;self.metadata_dirty=true
    self:status('Draft entry added. Drag a PNG to import source pixels.')
end
function App:import_bytes(bytes,name)
    if not self.project then self:status('Create or open a project first');return end
    local selected=self:selected_asset()
    if #self.project.assets>=256 and not (selected and selected.source=='' and selected.document=='') then
        self:status('Project asset limit reached');return
    end
    local n=1;local id
    repeat
        id=string.format('studio:actor/image_%03d',n);n=n+1
        local found=false
        for _,a in ipairs(self.project.assets) do if a.id==id then found=true end end
        if not found then break end
    until n>1000
    local a=selected and selected.source=='' and selected.document=='' and selected or
        Project.asset(id,name or 'Imported PNG','actor')
    local ok,result=self.store:import(self.project,a,bytes,decode)
    if not ok then self:status('Import rejected: '..tostring(result));return end
    if a~=selected then self.selected=#self.project.assets end
    self.filter_kind='all'
    self.dirty=true;self.metadata_dirty=true;self.preview=nil
    self:status(('Imported %s (%d×%d). Save the project.'):format(result.id,result.width,result.height))
end
function App:import_sample()
    local bytes=love.filesystem.read('examples/studio/adventurer.png')
    if not bytes then self:status('Bundled sample unavailable');return end
    self:import_bytes(bytes,'Adventurer sample')
end
function App:filedropped(file)
    if self.mode=='audio' then
        self.audio_editor:import_file(file)
        self.audio_pack=self.audio_editor.pack
        self.dirty=self.metadata_dirty or self.audio_editor:dirty() or
            self.world_editor and self.world_editor:dirty() or
            self.theme_editor and self.theme_editor:dirty() or false
        return
    end
    if self.mode=='canvas' then
        self:status('Save this sprite and return to asset metadata before importing another PNG.')
        return
    end
    local name=file:getFilename() or 'dropped.png'
    if not name:lower():match('%.png$') then self:status('Only PNG import is supported');return end
    local ok,err=pcall(function()
        assert(file:open('r'))
        local size=file:getSize()
        assert(size and size<=Project.max_image_bytes,'image exceeds 8 MiB')
        local bytes=file:read()
        file:close()
        self:import_bytes(bytes,name:match('[^/\\]+$'))
    end)
    if not ok then self:status('Import rejected: '..tostring(err)) end
end
function App:edit(field)
    local asset=self:selected_asset()
    if not asset then self:status('Select an asset first');return end
    self.field=field;self.typed=tostring(asset[field] or '')
    self.mode='edit';self:status('Editing '..field..'. Enter validates; Esc cancels.')
end
function App:commit_edit()
    local a=self:selected_asset()
    local old=a[self.field]
    local value=self.typed
    a[self.field]=value
    local ok,err=pcall(function()
        if self.field=='id' and old~=value and self.world_pack then
            for _,tile in ipairs(self.world_pack.tiles) do
                assert(tile.asset_id~=old,'asset ID is used by Studio tile '..tile.id)
            end
            if self.theme_pack then
                for slot,id in pairs(self.theme_pack.slots) do
                    assert(id~=old,'asset ID is used by theme slot '..slot)
                end
            end
            for _,room in ipairs(self.world_pack.rooms) do
                for _,asset_id in pairs(room.visual_cells) do
                    assert(asset_id~=old,'asset ID is used by room '..room.id)
                end
            end
        end
        Project.validate(self.project)
        if self.theme_pack then StudioTheme.validate(self.theme_pack,self.project.assets) end
    end)
    if not ok then a[self.field]=old;self:status('Edit rejected: '..tostring(err))
    else
        if self.field=='id' and self.canvas and self.canvas_asset==a then
            self.canvas.doc.id=value;self.canvas.is_dirty=true;self.canvas.texture_dirty=true
        end
        self.dirty=true;self.metadata_dirty=true;self.preview=nil;self:status('Updated '..self.field)
    end
    self.mode='project';self.field=nil
end
function App:keypressed(key)
    if key=='f10' then self.help_open=not self.help_open;return true end
    if self.help_open then
        if key=='escape' then self.help_open=false end
        return true
    end
    if key=='f8' and (self.mode=='project' or self.mode=='world' or
        self.mode=='theme') then
        local context
        if self.mode=='world' then
            local room=self.world_editor and self.world_editor:room()
            context=room and room.id or 'room'
        elseif self.mode=='theme' then context='ui' end
        self:open_audio(context)
        return true
    end
    if self.mode=='audio' then
        if key=='s' and not self.audio_editor.input and
            not self.audio_editor.delete_confirm then self:save();return true end
        if key=='escape' and not self.audio_editor.input and
            not self.audio_editor.delete_confirm then
            self.audio_editor:stop_audition()
            if self.audio_editor:dirty() then
                self:status('Save audio edits (S) before returning; original clips remain safe.')
            else
                self.mode=self.audio_return_mode or 'project'
                self:status('Returned to '..self.mode..'.')
            end
            return true
        end
        self.audio_editor:keypressed(key,love.keyboard.isDown('lctrl','rctrl'))
        self.audio_pack=self.audio_editor.pack
        self.dirty=self.metadata_dirty or self.audio_editor:dirty() or
            self.world_editor and self.world_editor:dirty() or
            self.theme_editor and self.theme_editor:dirty() or false
        return true
    end
    if self.mode=='publish_confirm' then
        if key=='escape' then self.mode='pipeline';self.typed=''
        elseif key=='backspace' then self.typed=self.typed:sub(1,-2)
        elseif key=='return' or key=='kpenter' then
            if self.loose_plan.counts.overwrite==0 or self.typed=='OVERWRITE' then
                self:apply_loose_publish()
            else self:status('Type OVERWRITE exactly, or Esc to cancel.') end
        end
        return true
    end
    if self.mode=='pipeline' then
        if key=='escape' then
            if self.loose_plan then self.loose_plan=nil;self.loose_publisher=nil
            else self.mode='project' end
        elseif key=='p' then self:inspect_pipeline()
        elseif key=='d' then self:preview_loose_publish()
        elseif key=='a' then self:confirm_loose_publish() end
        return true
    end
    if self.mode=='theme' then
        if key=='s' and not self.theme_editor.input and
            not self.theme_editor.revert_confirm then self:save();return end
        if key=='escape' and not self.theme_editor.input and
            not self.theme_editor.revert_confirm then
            if self.theme_editor:dirty() then
                self:status('Save theme changes (S), or close and confirm discard.')
            else self.mode='project';self:status('Asset metadata ready.') end
            return
        end
        self.theme_editor:keypressed(key,love.keyboard.isDown('lctrl','rctrl'))
        self.theme_pack=self.theme_editor.theme
        self.dirty=self.metadata_dirty or
            (self.world_editor and self.world_editor:dirty() or false) or
            self.theme_editor:dirty()
        return
    end
    if self.mode=='canvas' then
        if key=='tab' then
            self.canvas:finish()
            self.draft_preview=not self.draft_preview
            self.published_visuals=nil
            return true
        end
        if self.draft_preview then
            if key=='escape' then self.draft_preview=false end
            return true
        end
        if key=='s' and not self.canvas.modal then self:save();return end
        if key=='escape' and not self.canvas.modal then
            self.canvas:finish()
            if self.canvas.selection then
                self.canvas.selection=nil
                self:status('Selection cleared.')
                return
            end
            if self.canvas:dirty() or self.canvas_asset.document=='' then
                self:status('Save this sprite (S) before returning to asset metadata.')
            else self.mode='project';self:status('Asset metadata ready.') end
            return
        end
        local handled,reason=self.canvas:keypressed(key)
        if reason then self:status(reason) end
        if handled then self.dirty=self.metadata_dirty or self.canvas:dirty() end
        return
    end
    if self.mode=='world_size' then
        if key=='escape' then self.mode='world'
        elseif key=='backspace' then self.typed=self.typed:sub(1,-2)
        elseif key=='return' then
            local w,h=self.typed:match('^(%d+)%s*[xX]%s*(%d+)$')
            local ok,err=pcall(function() self:create_room(tonumber(w),tonumber(h)) end)
            if not ok then self:status('Invalid room size: '..tostring(err)) end
        end
        return
    end
    if self.mode=='world_edit' then
        if key=='escape' then self.mode='world'
        elseif key=='backspace' then self.typed=self.typed:sub(1,-2)
        elseif key=='return' then
            local editor=self.world_editor
            local target=self.world_edit_target=='tile' and
                editor.pack.tiles[editor.selected_tile] or editor:room()
            local value=self.typed;local field=self.field
            local ok,err=editor:change(function()
                assert(#value>0 and #value<=80,'empty or long field')
                if field=='id' then
                    assert(Project.valid_id(value),'invalid stable room ID')
                    local list=self.world_edit_target=='tile' and
                        editor.pack.tiles or editor.pack.rooms
                    for _,other in ipairs(list) do
                        assert(other==target or other.id~=value,'duplicate content ID')
                    end
                    if self.world_edit_target=='tile' then
                        for _,room in ipairs(editor.pack.rooms) do
                            for location,id in pairs(room.tile_cells) do
                                if id==target.id then room.tile_cells[location]=value end
                            end
                        end
                    end
                end
                target[field]=value
            end)
            if ok then self.mode='world';self.dirty=true
                self:status('Updated room '..field)
            else self:status('Room edit rejected: '..tostring(err)) end
        end
        return
    end
    if self.mode=='world_delete_confirm' then
        if key=='escape' then self.mode='world'
        elseif key=='return' or key=='y' then
            local editor=self.world_editor
            editor:change(function() table.remove(editor.pack.rooms,editor.selected) end)
            editor.selected=math.min(editor.selected,#editor.pack.rooms)
            self.world_pack=editor.pack;self.dirty=true;self.mode='world'
            self:status('Room deleted. Save to persist, or Undo to restore.')
        end
        return
    end
    if self.mode=='world' then
        local editor=self.world_editor
        if key=='s' then self:save();return end
        if key=='escape' then
            if editor.generated then editor.generated=nil;return end
            editor:finish()
            if editor:dirty() then
                self:status('Save world changes (S), or close the window and confirm discard.')
            else self.mode='project';self:status('Asset metadata ready.') end
            return
        end
        if key=='n' then self.mode='world_size';self.typed='9x7'
            self:status('Enter room dimensions: 9x7, 15x11, 23x17 or odd custom up to 23x17.');return end
        if key=='d' and editor:room() then
            local source=editor:room();local n=1;local id
            repeat id=source.id..'_copy'..n;n=n+1
                local used=false;for _,r in ipairs(editor.pack.rooms) do if r.id==id then used=true end end
                if not used then break end
            until n>1000
            local ok,err=editor:change(function()
                assert(#editor.pack.rooms<StudioWorld.max_rooms,'room limit reached')
                local copy=StudioWorld.clone(source);copy.id=id;copy.name=source.name..' copy'
                editor.pack.rooms[#editor.pack.rooms+1]=copy
            end)
            if ok then editor.selected=#editor.pack.rooms;self.dirty=true;self:status('Duplicated '..id)
            else self:status(tostring(err)) end
            return
        end
        if key=='x' and editor:room() then self.mode='world_delete_confirm'
            self:status('Delete selected room? Enter/Y confirms; Esc cancels.');return end
        if (key=='f1' or key=='f2') and editor:room() then
            self.field=key=='f1' and 'name' or 'id';self.typed=editor:room()[self.field]
            self.world_edit_target='room'
            self.mode='world_edit';self:status('Edit room '..self.field..' · Enter accepts, Esc cancels.');return
        end
        if key=='k' then
            local n=1;local id
            repeat id='studio:tile/custom_'..n;n=n+1
                local used=false;for _,tile in ipairs(editor.pack.tiles) do
                    if tile.id==id then used=true end end
                if not used then break end
            until n>1000
            local source=editor.pack.tiles[editor.selected_tile]
            local ok,err=editor:change(function()
                assert(#editor.pack.tiles<StudioWorld.max_tiles,'tile limit reached')
                local tile=StudioWorld.clone(source)
                tile.id=id;tile.name='Custom tile '..(n-1);tile.asset_id=''
                editor.pack.tiles[#editor.pack.tiles+1]=tile
            end)
            if ok then editor.selected_tile=#editor.pack.tiles;self.dirty=true
                self:status('Created '..id..' · F3/F4 edit name/ID, Z changes terrain type.')
            else self:status(tostring(err)) end
            return
        end
        if key=='f3' or key=='f4' then
            local tile=editor.pack.tiles[editor.selected_tile]
            self.field=key=='f3' and 'name' or 'id';self.typed=tile[self.field]
            self.world_edit_target='tile';self.mode='world_edit'
            self:status('Edit tile '..self.field..' · Enter accepts, Esc cancels.');return
        end
        if key=='e' then
            local a=self:selected_asset()
            if not a or a.kind~='tile' or (a.source=='' and a.derived=='') then
                self:status('Select a saved tile image in Assets first.');return end
            editor.decor_asset=a.id;editor.tool='decoration'
            self:status('Decoration paints '..a.id..' without changing collision.');return
        end
        if key=='backspace' then editor.tool='clear_visual'
            self:status('Visual eraser active; terrain stays unchanged.');return end
        if key=='i' then
            local a=self:selected_asset()
            if not a or a.kind~='tile' or (a.source=='' and a.derived=='') then
                self:status('Select a saved tile image in Assets before opening World Editor.');return
            end
            local tile=editor.pack.tiles[editor.selected_tile]
            editor:change(function() tile.asset_id=a.id end)
            self.dirty=true;self:status('Tile '..tile.id..' uses '..a.id);return
        end
        if key=='u' then
            local tile=editor.pack.tiles[editor.selected_tile]
            editor:change(function() tile.asset_id='' end)
            self.dirty=true;self:status('Cleared visual on '..tile.id);return
        end
        if editor:keypressed(key,love.keyboard.isDown('lctrl','rctrl')) then
            self.world_pack=editor.pack;self.dirty=self.metadata_dirty or editor:dirty()
            return
        end
        return
    end
    if self.mode=='quit_confirm' then
        if key=='escape' then self.mode=self.return_mode or (self.project and 'project' or 'home')
            self.return_mode=nil
        elseif key=='return' or key=='y' then
            self.allow_quit=true;love.event.quit()
        end
        return
    end
    if self.mode=='confirm' then
        if key=='escape' then self.mode=self.return_mode or (self.project and 'project' or 'home');self.pending=nil
        elseif key=='return' or key=='y' then local action=self.pending;self.pending=nil;action() end
        return
    end
    if self.mode=='size' then
        if key=='escape' then self.mode='project'
        elseif key=='backspace' then self.typed=self.typed:sub(1,-2)
        elseif key=='return' then
            local w,h=self.typed:match('^(%d+)%s*[xX]%s*(%d+)$')
            local ok,err=pcall(function() self:create_sprite(tonumber(w),tonumber(h)) end)
            if not ok then self:status('Invalid sprite size: '..tostring(err)) end
        end
        return
    end
    if self.mode=='name' or self.mode=='edit' then
        if key=='escape' then self.mode=self.project and 'project' or 'home'
        elseif key=='backspace' then
            local offset=utf8.offset(self.typed,-1)
            if offset then self.typed=self.typed:sub(1,offset-1) end
        elseif key=='return' then
            if self.mode=='name' then
                local name=self.typed;self.mode='home'
                local ok,err=pcall(function() self:new_project(name) end)
                if not ok then self:status('New project rejected: '..tostring(err)) end
            else self:commit_edit() end
        end
        return
    end
    if self.mode=='open' then
        if key=='escape' then self.mode=self.project and 'project' or 'home'
        elseif key=='up' then self.list_index=math.max(1,self.list_index-1)
        elseif key=='down' then self.list_index=math.min(#self.listing,self.list_index+1)
        elseif key=='return' and self.listing[self.list_index] then
            self:open_project(self.listing[self.list_index])
        end
        return
    end
    if key=='n' then self:ask_discard(function()
        self.mode='name';self.typed='';self:status('Type a project name, then Enter.') end)
    elseif key=='o' then self:ask_discard(function()
        self.listing=self.store:list();self.list_index=1;self.mode='open'
        self:status(#self.listing>0 and 'Select project and press Enter.' or 'No saved projects yet.') end)
    elseif key=='s' then self:save()
    elseif key=='a' then self:add_draft()
    elseif key=='i' then self:import_sample()
    elseif key=='d' then self:duplicate_sprite()
    elseif key=='c' then
        if self.project then self.mode='size';self.typed='32x32'
            self:status('Enter width x height (1–512). Presets: 16x16, 32x32, 64x64, 128x128.') end
    elseif key=='e' then self:edit_pixels()
    elseif key=='w' then self:open_world()
    elseif key=='u' then self:open_theme()
    elseif key=='y' then self:open_audio()
    elseif key=='p' then self:inspect_pipeline()
    elseif key=='tab' then
        local kinds={'all','actor','enemy','tile','effect','ui','background','glyph',
            'shader','ui_icon','ui_panel','ui_background','bitmap_font','effect_sprite'}
        local at=1;for i,v in ipairs(kinds) do if v==self.filter_kind then at=i end end
        self.filter_kind=kinds[at%#kinds+1]
        local visible=self:visible_assets()
        if #visible>0 then self.selected=visible[1] end
        self:status('Showing '..self.filter_kind..' assets')
    elseif key=='up' or key=='down' then
        local visible=self:visible_assets();local at=1
        for i,v in ipairs(visible) do if v==self.selected then at=i end end
        at=math.max(1,math.min(#visible,at+(key=='up' and -1 or 1)))
        if visible[at] then self.selected=visible[at];self.preview=nil end
    elseif key=='1' then self:edit('name')
    elseif key=='2' then self:edit('id')
    elseif key=='3' then self:edit('kind')
    elseif key=='4' then self:edit('binding')
    elseif key=='5' then self:edit('filter')
    elseif key=='6' then self:edit('reference_id')
    elseif key=='b' then
        local a=self:selected_asset()
        if a then
            local old=a.binding;a.binding='core:actor/adventurer'
            local ok,err=pcall(Project.validate,self.project)
            if ok then self.dirty=true;self.metadata_dirty=true;self:status('Bound to core:actor/adventurer')
            else a.binding=old;self:status(tostring(err)) end
        end
    elseif key=='escape' then self:status('Studio stays open; use the window close control.')
    end
end
function App:textinput(text)
    if self.mode=='publish_confirm' then
        if #self.typed+#text<=32 then self.typed=self.typed..text end
        return
    end
    if self.mode=='canvas' then self.canvas:textinput(text);return end
    if self.mode=='theme' then self.theme_editor:textinput(text);return end
    if self.mode=='audio' then self.audio_editor:textinput(text);return end
    if self.mode=='name' or self.mode=='edit' or self.mode=='size' or
        self.mode=='world_size' or self.mode=='world_edit' then
        if #self.typed+#text<=100 then self.typed=self.typed..text end
    end
end
function App:mousepressed(x,y,button)
    if self.help_open then return true end
    if button==1 and self.mode=='pipeline' then
        if y>=558 and y<=596 then
            if x>=24 and x<224 then self:keypressed('d')
            elseif x>=240 and x<440 then self:keypressed('a') end
        end
        return true
    end
    if button==1 and self.mode=='publish_confirm' then
        if y>=380 and y<=420 then
            if x>=360 and x<500 then self:keypressed('return')
            elseif x>=520 and x<660 then self:keypressed('escape') end
        end
        return true
    end
    if self.mode=='canvas' then
        if self.draft_preview then self.draft_preview=false;return true end
        local handled,reason=self.canvas:mousepressed(x,y,button)
        if reason then self:status(reason) end
        self.dirty=self.metadata_dirty or self.canvas:dirty()
        return handled
    end
    if self.mode=='world' then
        self.world_editor:mousepressed(x,y,button)
        self.world_pack=self.world_editor.pack
        self.dirty=self.metadata_dirty or self.world_editor:dirty()
        return true
    end
    if self.mode=='theme' then
        self.theme_editor:mousepressed(x,y,button)
        self.theme_pack=self.theme_editor.theme
        self.dirty=self.metadata_dirty or self.theme_editor:dirty()
        return true
    end
    if self.mode=='audio' then
        self.audio_editor:mousepressed(x,y,button)
        self.audio_pack=self.audio_editor.pack
        self.dirty=self.metadata_dirty or self.audio_editor:dirty() or
            self.world_editor and self.world_editor:dirty() or
            self.theme_editor and self.theme_editor:dirty() or false
        return true
    end
    if self.mode~='home' and self.mode~='project' then return end
    if button~=1 then return end
    if y>=72 and y<=100 then
        if x<100 then self:keypressed('n')
        elseif x<200 then self:keypressed('o')
        elseif x<300 then self:keypressed('s')
        elseif x<400 then self:keypressed('a')
        elseif x<500 then self:keypressed('i')
        elseif x<600 then self:keypressed('c')
        elseif x<700 then self:keypressed('e')
        elseif x<800 then self:keypressed('w')
        elseif x<900 then self:keypressed('u')
        elseif x<960 then self:keypressed('p')
        else self:keypressed('y') end
    elseif self.mode=='project' and x<430 and y>=166 and y<536 then
        local visible=self:visible_assets()
        local at=1;for i,v in ipairs(visible) do if v==self.selected then at=i end end
        local first=math.max(1,at-13)
        local index=visible[first+math.floor((y-166)/25)]
        if index then self.selected=index;self.preview=nil end
    elseif self.mode=='project' and x>=450 and y>=386 and y<=530 then
        local index=math.floor((y-386)/24)+1
        local f=({'name','id','kind','binding','filter','reference_id'})[index]
        if f then self:edit(f) end
    end
end
function App:mousemoved(x,y,dx,dy)
    if self.mode=='canvas' then self.canvas:mousemoved(x,y,dx,dy)
        self.dirty=self.metadata_dirty or self.canvas:dirty() end
    if self.mode=='world' then
        self.world_editor:mousemoved(x,y,dx,dy)
        self.dirty=self.metadata_dirty or self.world_editor:dirty()
    end
end
function App:mousereleased()
    if self.mode=='canvas' then self.canvas:mousereleased()
        self.dirty=self.metadata_dirty or self.canvas:dirty() end
    if self.mode=='world' then self.world_editor:mousereleased()
        self.dirty=self.metadata_dirty or self.world_editor:dirty() end
end
function App:wheelmoved(dx,dy)
    if self.mode=='canvas' then self.canvas:wheelmoved(dx,dy) end
    if self.mode=='world' then self.world_editor:wheelmoved(dx,dy) end
end
function App:focus(focused)
    if not focused and self.canvas then self.canvas:finish() end
    if not focused and self.world_editor then self.world_editor:finish() end
end
function App:preview_image(a)
    if not a or (a.source=='' and a.derived=='') then return nil end
    if self.preview_id==a.id and self.preview then return self.preview end
    local path=a.derived~='' and a.derived or a.source
    local readok,bytes=pcall(IO.read,IO,'studio/projects/'..self.project.id..'/'..path)
    if not readok or not bytes then self:status('Missing image: '..path);return nil end
    local ok,image=pcall(function()
        local width,height=Project.output_dimensions(a)
        local pixels=decode(bytes,path,width,height)
        local result=love.graphics.newImage(pixels)
        result:setFilter(a.filter,a.filter)
        return result
    end)
    if not ok then self:status('Preview decode failed: '..tostring(image));return nil end
    self.preview=image;self.preview_id=a.id
    return image
end
function App:draw()
    if self.mode=='audio' then
        self.audio_editor:draw()
        if self.help_open then E06Views.help(self.workspace) end
        return
    end
    if self.mode=='theme' then
        self.theme_editor:draw()
        love.graphics.setColor(.95,.83,.53)
        love.graphics.print('DRAFT THEME · F8 contextual sound/music · F10 help',18,60)
        if self.help_open then E06Views.help(self.workspace) end
        return
    end
    if self.mode=='canvas' then
        self.canvas:draw()
        love.graphics.setColor(UI.color.warning)
        love.graphics.printf(self.message,24,love.graphics.getHeight()-133,172,'left')
        if self.draft_preview then
            if not self.published_visuals then
                self.published_visuals=VisualAssets.new(function(path)
                    return love.filesystem.read(path)
                end,function(bytes,asset)
                    local pixels=decode(bytes,asset.source,asset.width,asset.height)
                    local texture=love.graphics.newImage(pixels)
                    texture:setFilter(asset.filter,asset.filter)
                    return texture,pixels
                end)
            end
            E06Views.draft(self.canvas,
                self.published_visuals:resolve_id(self.canvas.doc.id))
        end
        if self.help_open then E06Views.help(self.workspace) end
        return
    end
    if self.world_editor and (self.mode=='world' or self.mode=='world_size' or
        self.mode=='world_edit' or self.mode=='world_delete_confirm') then
        self.world_editor:draw(function(id) return self:world_asset_image(id) end)
        if self.mode~='world' then
            local g=love.graphics
            UI.modal(170,205,680,200)
            g.setColor(UI.color.text)
            local title=self.mode=='world_size' and 'NEW ROOM DIMENSIONS (odd, ≤23×17)' or
                self.mode=='world_edit' and ('EDIT ROOM '..self.field:upper()) or
                'DELETE SELECTED ROOM?'
            g.printf(title,190,225,640,'center')
            g.printf(self.mode=='world_delete_confirm' and
                'Enter / Y to delete · Esc to cancel' or self.typed,
                190,285,640,'center')
        end
        love.graphics.setColor(UI.color.warning)
        love.graphics.print('DRAFT ROOM · F8 room sound/music · F10 help',18,60)
        if self.help_open then E06Views.help(self.workspace) end
        return
    end
    if self.mode=='pipeline' or self.mode=='publish_confirm' then
        E06Views.pipeline(self.pipeline_report,self.workspace,self.dirty,
            (self:selected_asset() or {}).id,
            self.world_editor and self.world_editor:room() and
                self.world_editor:room().id or 'none',
            self.theme_pack and self.theme_pack.name or 'none',
            self.loose_plan,self.loose_publisher and self.loose_publisher.root,
            self.publish_last_backup,self.message)
        if self.mode=='publish_confirm' then
            E06Views.publish_confirm(self.loose_plan,self.typed,self.message)
        end
        if self.help_open then E06Views.help(self.workspace) end
        return
    end
    local g=love.graphics
    local w,h=g.getDimensions()
    UI.begin('ROAG  /  STUDIO','ASSETS  ·  Static art, rooms, themes and audio')
    local actions={{'N New',18,82},{'O Open',100,98},{'S Save',200,98},
        {'A Draft',300,98},{'I Sample',400,98},{'C Sprite',500,98},
        {'E Pixels',600,98},{'W World',700,98},{'U UI',800,98},
        {'P Pub',900,58},{'Y Audio',960,math.max(45,w-978)}}
    for _,action in ipairs(actions) do
        UI.button(action[2],72,action[3],29,action[1],false,true)
    end
    g.setColor(UI.color.muted)
    g.print('WORKSPACE  '..self.workspace,24,112)
    local p=self.project
    g.setColor(p and UI.color.text or UI.color.muted)
    g.print(p and (p.name..'  ['..p.id..']'..(self.dirty and '  * UNSAVED' or '')) or
        'No project open',24,139)
    UI.panel(20,166,410,370)
    g.setColor(UI.color.muted)
    if p and #p.assets==0 then g.print('Empty project. Press A or drop a PNG.',30,195) end
    if p then
        local visible=self:visible_assets();local at=1
        for i,v in ipairs(visible) do if v==self.selected then at=i end end
        local first=math.max(1,at-13)
        for row=1,math.min(14,#visible-first+1) do
            local i=visible[first+row-1];local a=p.assets[i]
            if i==self.selected then UI.selected(24,166+(row-1)*25,400,24) end
            g.setColor((a.source=='' and a.derived=='') and UI.color.muted or UI.color.text)
            g.print(('%s  %s  %s'):format(a.kind,a.id,(a.source=='' and a.derived=='') and '[draft]' or ''),
                31,171+(row-1)*25)
        end
    end
    local a=self:selected_asset()
    UI.panel(446,139,555,397)
    g.setColor(UI.color.text)
    g.print(a and ('SELECTED · '..a.name) or 'No asset selected',458,148)
    if a then
        UI.inset(459,182,258,178)
        for yy=183,342,16 do
            for xx=460,700,16 do
                local dark=((xx+yy)/16)%2<1
                g.setColor(dark and UI.color.checker_dark or UI.color.checker_light)
                g.rectangle('fill',xx,yy,16,16)
            end
        end
        local image=self:preview_image(a)
        if image then
            local width,height=image:getWidth(),image:getHeight()
            local scale=math.min(240/width,160/height,8)
            g.setColor(1,1,1)
            g.draw(image,580-width*scale/2,264-height*scale/2,0,scale,scale)
        else g.setColor(UI.color.muted);g.print('No image source',510,258) end
        g.setColor(UI.color.muted)
        local width,height=Project.output_dimensions(a)
        g.print(('PNG %d×%d · %s'):format(width,height,a.filter),458,355)
        g.print('Source: '..(a.source=='' and (a.derived=='' and '[draft]' or '[painted layers]') or a.source),458,370)
        local values={a.name,a.id,a.kind,a.binding,a.filter,a.reference_id or ''}
        for i,v in ipairs(values) do
            if i==6 and #v>48 then v=v:sub(1,45)..'...' end
            g.print(('%d %s: %s'):format(i,({'Name','Asset ID','Kind','Binding','Filter','Reference'})[i],v),
                458,386+(i-1)*24)
        end
    end
    UI.statusbar(543,math.max(97,h-543))
    g.setColor(UI.color.muted)
    g.print('Tab filter: '..self.filter_kind..' · 1–6 edit · B bind · C sprite · D copy · E pixels',24,550)
    g.print('Click asset or field · Enter accept · Esc cancel · Save before close',24,573)
    g.print('F10 help · Canvas Tab preview · P readiness',600,550)
    g.setColor(UI.color.warning);g.printf(self.message,24,601,976,'left')
    if self.mode=='name' or self.mode=='edit' or self.mode=='size' or self.mode=='confirm' or
        self.mode=='quit_confirm' or self.mode=='open' or
        self.mode=='world_size' or self.mode=='world_edit' or
        self.mode=='world_delete_confirm' then
        UI.modal(170,205,680,200)
        g.setColor(UI.color.text)
        local title=self.mode=='name' and 'NEW PROJECT NAME' or
            self.mode=='edit' and ('EDIT '..self.field:upper()) or
            self.mode=='size' and 'NEW SPRITE DIMENSIONS (1–512)' or
            self.mode=='world_size' and 'NEW ROOM DIMENSIONS (odd, ≤23×17)' or
            self.mode=='world_edit' and ('EDIT ROOM '..self.field:upper()) or
            self.mode=='world_delete_confirm' and 'DELETE ROOM?' or
            self.mode=='confirm' and 'DISCARD UNSAVED EDITS?' or 'OPEN PROJECT'
        if self.mode=='quit_confirm' then title='CLOSE WITHOUT SAVING?' end
        g.printf(title,190,225,640,'center')
        if self.mode=='open' then
            local first=math.max(1,self.list_index-5)
            for row=1,math.min(6,#self.listing-first+1) do
                local i=first+row-1;local id=self.listing[i]
                g.setColor(i==self.list_index and 1 or 0.7,0.88,0.78)
                g.printf(id,190,260+(row-1)*23,640,'center')
            end
        else g.printf((self.mode=='confirm' or self.mode=='quit_confirm' or
            self.mode=='world_delete_confirm') and
            'Enter / Y to discard · Esc to cancel' or
            self.typed,190,285,640,'center') end
    end
    if self.help_open then E06Views.help(self.workspace) end
end
return App
