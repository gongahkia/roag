-- Focused on-screen sound library and contextual cue picker. Source files stay
-- immutable; all edits below change only versioned plain-data metadata.
local Audio=require('src.studio_audio')
local UI=require('src.studio_chrome')
local Cues=require('src.audio_cues')
local Samples=require('src.studio_audio_samples')
local E={}
local tabs={'Library','Cue picker','Mixer'}
local function copy(pack)return Audio.decode(Audio.encode(pack))end
local function slug(s)
    local v=s:lower():gsub('[^a-z0-9]+','_'):gsub('^_+',''):gsub('_+$','')
    if v=='' or not v:match('^[a-z]') then v='clip_'..v end
    return v:sub(1,48)
end
local function clamp(n,a,b)return math.max(a,math.min(b,n))end
function E.new(pack,store,io,world_pack,context)
    local self=setmetatable({pack=copy(pack),store=store,io=io,
        world_pack=world_pack,cues=Cues.list(world_pack),
        tab=2,selected_sound=1,selected_cue=1,selected_category=1,
        selected_variant=1,query='',sound_query='',category='all',input=nil,typed='',
        history={},future={},saved=Audio.encode(pack),audition=nil,
        audition_volume=.8,audition_mute=false,sample_index=1,
        message='Drop WAV/Ogg · Tab Library/Cues/Mixer · F search · F8 context'},
        {__index=E})
    if context then
        for index,cue in ipairs(self.cues) do
            if cue.context_id==context or cue.category==context then
                self.selected_cue=index;break
            end
        end
        self.message='Context: '..context..' · '..self.message
    end
    return self
end
function E:dirty()return Audio.encode(self.pack)~=self.saved end
function E:saved_now()self.saved=Audio.encode(self.pack)end
function E:change(fn)
    local before=Audio.encode(self.pack)
    local next_pack=copy(self.pack)
    local ok,err=pcall(fn,next_pack)
    if ok then ok,err=pcall(Audio.validate,next_pack,
        function(path)return self.io:read(path)end,self.world_pack) end
    if not ok then self.message='Audio edit rejected: '..tostring(err);return false end
    local after=Audio.encode(next_pack)
    if after~=before then
        self.history[#self.history+1]=before
        if #self.history>32 then table.remove(self.history,1) end
        self.future={};self.pack=next_pack
    end
    self.message='Audio metadata updated · save before publishing'
    return true
end
function E:undo()
    local previous=table.remove(self.history)
    if not previous then self.message='Nothing to undo';return end
    self.future[#self.future+1]=Audio.encode(self.pack)
    self.pack=Audio.decode(previous)
    self.message='Undo audio metadata'
end
function E:redo()
    local next_pack=table.remove(self.future)
    if not next_pack then self.message='Nothing to redo';return end
    self.history[#self.history+1]=Audio.encode(self.pack)
    self.pack=Audio.decode(next_pack)
    self.message='Redo audio metadata'
end
function E:sound()
    if self.sound_query~='' then
        local found=false
        for _,i in ipairs(self:visible_sounds()) do
            if i==self.selected_sound then found=true;break end
        end
        if not found then return nil end
    end
    return self.pack.sounds[self.selected_sound]
end
function E:visible_sounds()
    local out={};local query=self.sound_query:lower()
    for i,sound in ipairs(self.pack.sounds) do
        if query=='' or sound.name:lower():find(query,1,true) or
            sound.id:lower():find(query,1,true) or
            sound.tags:lower():find(query,1,true) or
            sound.author:lower():find(query,1,true) then
            out[#out+1]=i
        end
    end
    return out
end
function E:visible_cues()
    local out={};local query=self.query:lower()
    for i,cue in ipairs(self.cues) do
        if (self.category=='all' or cue.category==self.category) and
            (query=='' or cue.id:lower():find(query,1,true) or
                cue.name:lower():find(query,1,true) or
                cue.context_id:lower():find(query,1,true)) then
            out[#out+1]=i
        end
    end
    return out
end
function E:cue()
    local visible=self:visible_cues()
    local at=clamp(self.selected_cue,1,#visible)
    return self.cues[visible[at]],at,#visible
end
function E:binding(cue_id)
    for i,b in ipairs(self.pack.bindings) do
        if b.cue_id==cue_id then return b,i end
    end
end
function E:import_bytes(bytes,name,replace_id,supplied_provenance)
    local format,duration
    local valid,why=pcall(function()
        format,duration=Audio.format(bytes)
        local data=love.filesystem.newFileData(bytes,'import.'..format)
        local source=love.audio.newSource(data,
            duration and duration>20 and 'stream' or 'static')
        assert(source:getDuration()>0,'audio decoder returned no duration')
    end)
    if not valid then self.message='Import rejected: '..tostring(why);return false end
    local id=replace_id or 'studio:audio/'..slug(name)
    if not replace_id then
        local base=id;local index=2
        local taken=true
        while taken do
            taken=false
            for _,sound in ipairs(self.pack.sounds) do
                if sound.id==id then taken=true;break end
            end
            if taken then id=base..'_'..index;index=index+1 end
        end
    end
    local prior
    for _,sound in ipairs(self.pack.sounds) do if sound.id==id then prior=sound;break end end
    local provenance=prior and {tags=prior.tags,author=prior.author,
        source_url=prior.source_url,license=prior.license} or
        supplied_provenance or
        {tags='imported',author='User import',license='User supplied; verify rights'}
    local sound,err=self.store:import(self.pack,id,
        prior and prior.name or name,bytes,provenance)
    if not sound then self.message='Import write failed: '..tostring(err);return false end
    sound.duration=duration or sound.duration
    local changed=self:change(function(pack)
        local index
        for i,old in ipairs(pack.sounds) do if old.id==id then index=i;break end end
        if index then pack.sounds[index]=sound else pack.sounds[#pack.sounds+1]=sound end
    end)
    if changed then
        self.sound_query=''
        for i,item in ipairs(self.pack.sounds) do
            if item.id==id then self.selected_sound=i;break end
        end
        self.message=(prior and 'Replaced' or 'Imported')..' '..id..
            ' · original bytes preserved · Save to retain reference'
    end
    return changed
end
function E:import_file(file)
    local name=(file:getFilename() or 'clip.wav'):match('[^/\\]+$')
    if not name:lower():match('%.wav$') and not name:lower():match('%.ogg$') then
        self.message='Drop a WAV or Ogg Vorbis file';return false
    end
    local ok,bytes=pcall(function()
        assert(file:open('r'))
        local size=file:getSize()
        assert(size and size<=Audio.max_source_bytes,'audio file exceeds 32 MiB')
        local data=file:read();file:close();return data
    end)
    if not ok then self.message='Audio read failed: '..tostring(bytes);return false end
    local replace=self.replace_id
    self.replace_id=nil
    return self:import_bytes(bytes,name:gsub('%.%w+$',''),replace)
end
function E:import_sample()
    local kinds={'step','hit','boon','menu','enemy','terrain','music','ambience'}
    local kind=kinds[self.sample_index]
    self.sample_index=self.sample_index%#kinds+1
    local added=self:import_bytes(Samples.wav(kind),'Original sample '..kind,nil,
        {tags='placeholder',author='ROAG Studio synthesized',
            license='Original project asset'})
    if added then self.message=self.message..' · next sample: '..kinds[self.sample_index] end
    return added
end
function E:audition_sound(sound)
    sound=sound or self:sound()
    if not sound then self.message='Select a sound first';return end
    self:stop_audition()
    local ok,result=pcall(function()
        local bytes=assert(self.io:read(sound.path),'audio source missing')
        assert(#bytes==sound.bytes and Audio.checksum(bytes)==sound.checksum,
            'audio source checksum mismatch')
        local data=love.filesystem.newFileData(bytes,sound.id..'.'..sound.format)
        local source=love.audio.newSource(data,
            (sound.duration or 0)>20 and 'stream' or 'static')
        source:setVolume(self.audition_mute and 0 or self.audition_volume)
        source:play();return source
    end)
    if ok then self.audition=result;self.message='Audition '..sound.name
    else self.message='Audition failed: '..tostring(result) end
end
function E:stop_audition()
    if self.audition then self.audition:stop();self.audition=nil end
end
function E:cycle_sound(delta)
    local cue=self:cue()
    if not cue then self.message='No matching cue';return end
    local ids={''}
    for _,sound in ipairs(self.pack.sounds) do ids[#ids+1]=sound.id end
    local binding=self:binding(cue.id)
    if binding then self.selected_variant=clamp(self.selected_variant,1,#binding.variants) end
    local current=binding and binding.variants[self.selected_variant]
    local current_id=current and current.asset_id or ''
    local at=1;for i,id in ipairs(ids) do if id==current_id then at=i end end
    local selected=ids[(at-1+delta+#ids)%#ids+1]
    self:change(function(pack)
        local index,entry
        for i,v in ipairs(pack.bindings) do
            if v.cue_id==cue.id then index=i;entry=v;break end
        end
        if selected=='' then
            if entry then
                table.remove(entry.variants,self.selected_variant)
                if #entry.variants==0 then table.remove(pack.bindings,index) end
            end
            self.selected_variant=1
        elseif not entry then
            pack.bindings[#pack.bindings+1]=Audio.binding(cue.id,selected)
            self.selected_variant=1
        else entry.variants[self.selected_variant].asset_id=selected end
    end)
end
function E:add_variant()
    local cue=self:cue();local sound=self:sound()
    if not cue or not sound then self.message='Select cue and sound first';return end
    self:change(function(pack)
        for _,binding in ipairs(pack.bindings) do
            if binding.cue_id==cue.id then
                assert(#binding.variants<8,'maximum eight variants')
                for _,v in ipairs(binding.variants) do
                    assert(v.asset_id~=sound.id,'sound is already a variant')
                end
                binding.variants[#binding.variants+1]={asset_id=sound.id,weight=1}
                self.selected_variant=#binding.variants
                return
            end
        end
        pack.bindings[#pack.bindings+1]=Audio.binding(cue.id,sound.id)
        self.selected_variant=1
    end)
end
local function edit_value(self,field)
    local cue=self:cue();local binding=cue and self:binding(cue.id)
    local sound=self:sound()
    if field=='search' then return self.query end
    if field=='sound_search' then return self.sound_query end
    if field=='sound_name' then return sound and sound.name end
    for _,name in ipairs({'tags','author','license','source_url'}) do
        if field==name then return sound and sound[name] end
    end
    if not binding then return nil end
    if field=='transition_mode' then return binding.transition.mode end
    if field=='transition_ms' then return binding.transition.duration_ms end
    if field=='variant_weight' then
        return binding.variants[self.selected_variant] and
            binding.variants[self.selected_variant].weight
    end
    return binding[field]
end
function E:start_input(field)
    local value=edit_value(self,field)
    if value==nil then self.message='Select a sound or bind a cue first';return end
    self.input=field;self.typed=tostring(value)
    self.message='Edit '..field..' · Enter accepts · Esc cancels'
end
function E:commit_input()
    local field,text=self.input,self.typed
    if field=='search' then
        self.query=text;self.selected_cue=1;self.input=nil;self.typed=''
        self.message='Filter: '..(text=='' and 'all cues' or text)
        return
    end
    if field=='sound_search' then
        self.sound_query=text;self.input=nil;self.typed=''
        local visible=self:visible_sounds()
        if visible[1] then self.selected_sound=visible[1] end
        self.message='Library filter: '..(text=='' and 'all sounds' or text)
        return
    end
    local changed=self:change(function(pack)
        if field=='sound_name' or field=='tags' or field=='author' or
            field=='license' or field=='source_url' then
            pack.sounds[self.selected_sound][field=='sound_name' and 'name' or field]=text
            return
        end
        local cue=self:cue()
        local binding
        for _,v in ipairs(pack.bindings) do
            if v.cue_id==cue.id then binding=v;break end
        end
        assert(binding,'bind a cue first')
        if field=='transition_mode' then binding.transition.mode=text
        elseif field=='transition_ms' then binding.transition.duration_ms=tonumber(text)
        elseif field=='variant_weight' then
            binding.variants[self.selected_variant].weight=tonumber(text)
        elseif field=='loop' then
            assert(text=='true' or text=='false' or text=='1' or text=='0',
                'loop must be true/false')
            binding.loop=text=='true' or text=='1'
        else binding[field]=tonumber(text) end
    end)
    if changed then self.input=nil;self.typed='' end
end
function E:keypressed(key,control)
    if self.delete_confirm then
        if key=='escape' then self.delete_confirm=false
        elseif key=='return' or key=='y' then
            local sound=self:sound()
            local changed=self:change(function(pack)
                for _,b in ipairs(pack.bindings) do
                    for _,v in ipairs(b.variants) do
                        assert(v.asset_id~=sound.id,
                            'sound is bound; unbind it before deleting')
                    end
                end
                table.remove(pack.sounds,self.selected_sound)
            end)
            if changed then self.selected_sound=clamp(self.selected_sound,1,#self.pack.sounds) end
            self.delete_confirm=false
        end
        return true
    end
    if self.input then
        if key=='escape' then self.input=nil;self.typed=''
        elseif key=='backspace' then self.typed=self.typed:sub(1,-2)
        elseif key=='return' then self:commit_input() end
        return true
    end
    if control and key=='z' then self:undo();return true end
    if control and key=='y' then self:redo();return true end
    if key=='tab' then self.tab=self.tab%3+1
    elseif key=='f' then self:start_input(self.tab==1 and 'sound_search' or 'search')
    elseif key=='up' then
        if self.tab==1 then
            local visible=self:visible_sounds();local at=1
            for i,v in ipairs(visible) do if v==self.selected_sound then at=i;break end end
            self.selected_sound=visible[clamp(at-1,1,#visible)] or self.selected_sound
        elseif self.tab==2 then self.selected_cue=clamp(self.selected_cue-1,1,#self:visible_cues())
        else self.selected_category=clamp(self.selected_category-1,1,#Audio.categories) end
    elseif key=='down' then
        if self.tab==1 then
            local visible=self:visible_sounds();local at=1
            for i,v in ipairs(visible) do if v==self.selected_sound then at=i;break end end
            self.selected_sound=visible[clamp(at+1,1,#visible)] or self.selected_sound
        elseif self.tab==2 then self.selected_cue=clamp(self.selected_cue+1,1,#self:visible_cues())
        else self.selected_category=clamp(self.selected_category+1,1,#Audio.categories) end
    elseif key=='p' then
        if self.tab==2 then
            local cue=self:cue();local binding=cue and self:binding(cue.id)
            local variant=binding and binding.variants[self.selected_variant]
            if variant then
                for _,sound in ipairs(self.pack.sounds) do
                    if sound.id==variant.asset_id then self:audition_sound(sound);break end
                end
            else self.message='Bind an audio clip first' end
        else self:audition_sound() end
    elseif key=='x' then self:stop_audition();self.message='Audition stopped'
    elseif self.tab==1 then
        if key=='i' then self:import_sample()
        elseif key=='[' or key==']' then
            local next_volume=self.audition_volume+(key==']' and .1 or -.1)
            self.audition_volume=math.floor(clamp(next_volume,0,1)*10+.5)/10
            if self.audition then
                self.audition:setVolume(self.audition_mute and 0 or self.audition_volume)
            end
            self.message=('Audition level %.1f'):format(self.audition_volume)
        elseif key=='m' then
            self.audition_mute=not self.audition_mute
            if self.audition then
                self.audition:setVolume(self.audition_mute and 0 or self.audition_volume)
            end
            self.message=self.audition_mute and 'Audition muted' or 'Audition unmuted'
        elseif key=='r' then
            local sound=self:sound()
            if sound then self.replace_id=sound.id
                self.message='Drop a replacement WAV/Ogg; stable ID stays '..sound.id end
        elseif key=='delete' then self.delete_confirm=true
            self.message='Delete selected audio metadata? Enter/Y confirms; source bytes remain.'
        elseif key=='n' then self:start_input('sound_name')
        elseif key=='t' then self:start_input('tags')
        elseif key=='a' then self:start_input('author')
        elseif key=='l' then self:start_input('license')
        elseif key=='u' then self:start_input('source_url') end
    elseif self.tab==2 then
        if key=='left' then self:cycle_sound(-1)
        elseif key=='right' then self:cycle_sound(1)
        elseif key=='a' then self:add_variant()
        elseif key=='v' then
            local cue=self:cue();local binding=cue and self:binding(cue.id)
            self.selected_variant=clamp(self.selected_variant+1,1,
                binding and #binding.variants or 1)
        elseif key=='c' then
            local choices={'all'}
            for _,name in ipairs(Audio.categories) do choices[#choices+1]=name end
            local index=1;for i,name in ipairs(choices) do
                if name==self.category then index=i;break end end
            self.category=choices[index%#choices+1];self.selected_cue=1
        elseif key=='e' then self:start_input('volume')
        elseif key=='g' then self:start_input('pitch')
        elseif key=='l' then self:start_input('loop')
        elseif key=='w' then self:start_input('variant_weight')
        elseif key=='j' then self:start_input('priority')
        elseif key=='k' then self:start_input('max_voices')
        elseif key=='d' then self:start_input('cooldown_ms')
        elseif key=='h' then self:start_input('distance')
        elseif key=='t' then self:start_input('transition_mode')
        elseif key=='o' then self:start_input('transition_ms') end
    else
        local name=Audio.categories[self.selected_category]
        if key=='m' then self:change(function(pack)
            pack.mix[name].mute=not pack.mix[name].mute end)
        elseif key=='left' or key=='right' then self:change(function(pack)
            local v=pack.mix[name].volume+(key=='right' and .1 or -.1)
            pack.mix[name].volume=math.floor(clamp(v,0,2)*10+.5)/10
        end) end
    end
    return true
end
function E:textinput(value)
    if self.input and #self.typed+#value<=512 then self.typed=self.typed..value end
end
function E:mousepressed(x,y,button)
    if button~=1 then return end
    if y>=68 and y<105 then
        self.tab=clamp(math.floor((x-20)/150)+1,1,3);return
    end
    if y>=122 and y<490 then
        local row=math.floor((y-122)/24)
        if self.tab==1 then
            local visible=self:visible_sounds()
            self.selected_sound=visible[clamp(row+1,1,#visible)] or self.selected_sound
        elseif self.tab==2 then self.selected_cue=clamp(row+1,1,#self:visible_cues())
        else self.selected_category=clamp(row+1,1,#Audio.categories) end
    elseif y>=520 and y<555 then
        if x<180 then self:keypressed('p')
        elseif x<350 then self:keypressed('x')
        elseif x<530 then self:keypressed('f')
        elseif x<710 then self:keypressed('i') end
    end
end
function E:draw()
    local g=love.graphics
    local width,height=g.getDimensions()
    UI.begin('ROAG  /  AUDIO','S save  ·  Esc assets  ·  F8 context  ·  Tab view  ·  Ctrl+Z/Y history')
    for index,name in ipairs(tabs) do
        UI.button(20+(index-1)*150,68,145,34,name,index==self.tab,true)
    end
    UI.panel(18,116,430,382)
    UI.panel(465,116,width-483,382)
    if self.tab==1 then
        local visible=self:visible_sounds()
        local first=1
        for i,v in ipairs(visible) do if v==self.selected_sound then
            first=clamp(i-7,1,math.max(1,#visible-14));break end end
        for row=1,math.min(15,#visible-first+1) do
                local i=visible[first+row-1]
                local sound=self.pack.sounds[i]
                local y=122+(row-1)*24
                if i==self.selected_sound then UI.selected(22,y,422,23) end
                g.setColor(UI.color.text)
                g.print(sound.name:sub(1,37),30,y+3)
        end
        local sound=self:sound()
        g.setColor(UI.color.text)
        if sound then
            local lines={'ID  '..sound.id,'FORMAT  '..sound.format..' · '..sound.bytes..' bytes',
                'DURATION  '..(sound.duration and string.format('%.2f s',sound.duration) or 'decoder'),
                'TAGS  '..sound.tags,'AUTHOR  '..sound.author,
                'LICENSE  '..sound.license,'SOURCE  '..sound.source_url,
                'CHECKSUM  '..sound.checksum}
            for i,line in ipairs(lines) do g.print(line:sub(1,70),476,125+(i-1)*32) end
        else g.print('No sound clips yet. Drop WAV/Ogg or press I for an original sample.',476,145) end
        g.print('I next original sample · drop WAV/Ogg · R replace · Delete',476,405)
        g.print('N name · T tags · A author · L license · U URL',476,430)
        g.print('F search name, tag, author or ID: '..self.sound_query,476,460)
        g.print(('[ / ] level %.1f · M %s'):format(self.audition_volume,
            self.audition_mute and 'unmute' or 'mute'),476,482)
    elseif self.tab==2 then
        local visible=self:visible_cues()
        local first=clamp(self.selected_cue-7,1,math.max(1,#visible-14))
        for row=1,math.min(15,#visible-first+1) do
            local index=visible[first+row-1]
            local cue=self.cues[index]
            local y=122+(row-1)*24
            if first+row-1==self.selected_cue then
                UI.selected(22,y,422,23) end
            g.setColor(UI.color.text)
            g.print(cue.name:sub(1,35),30,y+3)
        end
        local cue,index,total=self:cue()
        g.setColor(UI.color.text)
        if cue then
            local binding=self:binding(cue.id)
            g.print('CUE '..index..'/'..total..' · '..cue.name,476,125)
            g.print(cue.id,476,153)
            g.print('Context: '..cue.context_id..' · category '..cue.category,476,181)
            if binding then
                local variant=binding.variants[self.selected_variant]
                g.print('Variant '..self.selected_variant..'/'..#binding.variants..': '..(variant and variant.asset_id or ''),476,215)
                g.print(('weight %d · volume %.2f · pitch %.2f · loop %s'):format(
                    variant and variant.weight or 0,binding.volume,binding.pitch,
                    tostring(binding.loop)),476,243)
                g.print(('priority %d · voices %d · cooldown %d ms · range %d'):format(
                    binding.priority,binding.max_voices,binding.cooldown_ms,binding.distance),476,271)
                g.print(('transition %s · %d ms'):format(binding.transition.mode,
                    binding.transition.duration_ms),476,299)
            else g.print('Unbound · Left/Right chooses a sound',476,215) end
        else g.print('No matching cues. Press F to clear search.',476,145) end
        g.print('←/→ bind · A add variant · V next variant · C category',476,355)
        g.print('E vol · G pitch · L loop · W weight · J priority',476,381)
        g.print('K voices · D cooldown · H range · T transition · O ms',476,407)
        g.print('F search by cue, class, enemy, boon, room or biome',476,433)
        g.print('Category: '..self.category..' · Search: '..self.query,476,462)
    else
        for i,name in ipairs(Audio.categories) do
            local y=122+(i-1)*24
            if i==self.selected_category then
                UI.selected(22,y,422,23) end
            local item=self.pack.mix[name]
            g.setColor(UI.color.text)
            g.print(('%s  %.1f  %s'):format(name,item.volume,item.mute and 'MUTED' or ''),30,y+3)
        end
        g.setColor(UI.color.muted)
        g.print('Category volume and mute affect playback only.',476,142)
        g.print('Left/Right: change volume · M: mute/unmute',476,172)
        g.print('No game clock or RNG is used for audio.',476,216)
    end
    UI.panel(20,520,680,33)
    g.setColor(UI.color.text);g.print('P audition          X stop          F search          I original sample',28,527)
    UI.statusbar(560,height-560)
    g.setColor(UI.color.muted)
    g.print((self:dirty() and '* UNSAVED · ' or 'Saved · ')..#self.pack.sounds..
        ' sound(s) · '..#self.pack.bindings..' cue binding(s)',20,568)
    g.setColor(UI.color.warning);g.printf(self.message,20,height-55,width-40,'left')
    if self.delete_confirm or self.input then
        UI.modal(190,207,width-380,190)
        g.setColor(UI.color.text)
        g.printf(self.delete_confirm and 'DELETE SOUND METADATA?' or
            ('EDIT '..self.input:upper()),205,226,width-410,'center')
        g.printf(self.delete_confirm and 'Enter/Y confirms · Esc cancels' or
            self.typed,205,286,width-410,'center')
    end
end
return E
