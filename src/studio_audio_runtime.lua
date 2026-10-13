-- Cosmetic sound routing. Never writes a Game state or draws simulation RNG.
local Audio=require('src.studio_audio')
local Store=require('src.studio_audio_store')
local Samples=require('src.studio_audio_samples')
local Cues=require('src.audio_cues')
local Terrain=require('src.terrain')
local R={}
local function tail(id) return id and id:match('([^/]+)$') end
local function add(out,id) if id and Cues.get(id) then out[#out+1]=id end end
local function source_enemy(event)
    return event.source_tags and event.source_tags[1]
end
local function target_enemy(event)
    return event.target_definition_id
end
function R.cues_for_event(event,state)
    local out={}
    if not event or not event.kind then return out end
    local player=state and state.player_id
    local actor=source_enemy(event)
    local target=target_enemy(event)
    if event.kind=='Moved' then
        if event.source_id==player then
            add(out,'core:cue/player/'..(event.path and #event.path>1 and 'dash' or 'move'))
        else
            add(out,'core:cue/enemy/move')
            add(out,'core:cue/enemy/'..tostring(tail(actor))..'/move')
        end
    elseif event.kind=='AttackPerformed' then
        if event.source_id==player then
            add(out,'core:cue/player/attack')
            add(out,'core:cue/ability/'..tostring(tail(event.ability_id)))
        else
            add(out,'core:cue/enemy/attack')
            add(out,'core:cue/enemy/'..tostring(tail(actor))..'/attack')
        end
    elseif event.kind=='WindupStarted' then
        add(out,'core:cue/enemy/telegraph')
        add(out,'core:cue/enemy/'..tostring(tail(actor))..'/telegraph')
    elseif event.kind=='DamageTaken' then
        if event.target_id==player then add(out,'core:cue/player/damage')
        else
            add(out,'core:cue/enemy/hit')
            add(out,'core:cue/enemy/'..tostring(tail(target))..'/hit')
        end
    elseif event.kind=='EntityKilled' then
        if event.target_id==player then add(out,'core:cue/player/death')
        else
            add(out,'core:cue/enemy/death')
            add(out,'core:cue/enemy/'..tostring(tail(target))..'/death')
        end
    elseif event.kind=='BoonActivated' then
        add(out,'core:cue/boon/activate')
        add(out,'core:cue/boon/'..tostring(tail(event.boon_id)))
    elseif event.kind=='StatusTick' and event.status=='burning' then
        add(out,'core:cue/status/burning')
    elseif event.kind=='TileEntered' and event.source_id==player then
        add(out,'core:cue/terrain/step')
        if state and state.generated_map and
            event.floor_id==state.active_floor_id then
            local def=Terrain.at(state.generated_map,event.x,event.y)
            if def then add(out,'core:cue/terrain/'..tostring(tail(def.id))) end
        end
    elseif event.kind=='TerrainTriggered' then
        add(out,'core:cue/terrain/hazard')
        add(out,'core:cue/terrain/'..tostring(tail(event.terrain_id)))
    elseif event.kind=='TerrainBroken' then
        add(out,'core:cue/terrain/'..tostring(tail(event.terrain_id)))
    elseif event.kind=='FloorChanged' then add(out,'core:cue/terrain/stairs')
    elseif event.kind=='DoorOpened' then add(out,'core:cue/world/door')
    elseif event.kind=='ChestClaimed' then add(out,'core:cue/world/chest')
    elseif event.kind=='SecretDiscovered' then add(out,'core:cue/world/secret')
    elseif event.kind=='CurrencyCollected' then add(out,'core:cue/world/currency')
    elseif event.kind=='StatIncreased' then add(out,'core:cue/ui/level_up')
    elseif event.kind=='AchievementEarned' then add(out,'core:cue/ui/achievement')
    elseif event.kind=='EliteEngaged' then
        add(out,'core:cue/boss/arrival')
        add(out,'core:cue/elite/'..tostring(tail(actor))..'/arrival')
    elseif event.kind=='FinalSanctumEntered' then
        add(out,'core:cue/boss/arrival')
        add(out,'core:cue/elite/hollow_regent/arrival')
    elseif event.kind=='ElitePhaseChanged' or event.kind=='RegentPhaseChanged' then
        add(out,'core:cue/boss/phase')
        add(out,'core:cue/elite/'..tostring(tail(actor))..'/phase')
    elseif event.kind=='EliteDefeated' or event.kind=='RegentDefeated' then
        add(out,'core:cue/boss/defeat')
        local elite=event.kind=='RegentDefeated' and 'core:enemy/hollow_regent'
            or state and state.stage and state.stage.elite_definition_id or actor
        add(out,'core:cue/elite/'..tostring(tail(elite))..'/defeat')
    elseif event.kind=='Victory' then add(out,'core:cue/ui/victory')
    elseif event.kind=='Defeat' then add(out,'core:cue/ui/defeat') end
    return out
end
local function binding_map(pack)
    local map={};for _,binding in ipairs(pack.bindings) do map[binding.cue_id]=binding end
    return map
end
local function category_for(cue_id)
    if cue_id:find('/music/',1,true) or cue_id:match('/music$') then
        return 'music'
    end
    local cue=Cues.get(cue_id)
    return cue and cue.category or 'world'
end
local function candidate(read,make_source)
    local pack,status=Store.inspect_published(read)
    local bytes_by_path
    local published=pack~=nil
    if not pack then
        if status~='absent' and status~='corrupt audio' then
            return nil,'rejected: '..tostring(status)
        end
        pack,bytes_by_path=Samples.showcase('builtin_audio')
        status=status=='absent' and 'original built-in samples' or
            'original built-in samples (published audio corrupt)'
    end
    local templates={}
    for _,sound in ipairs(pack.sounds) do
        local bytes=bytes_by_path and bytes_by_path[sound.path] or
            read(Audio.published_path(sound))
        if type(bytes)~='string' or #bytes~=sound.bytes or
            Audio.checksum(bytes)~=sound.checksum then
            return nil,'missing/corrupt audio: '..sound.id
        end
        local format,why=pcall(Audio.format,bytes)
        if not format then return nil,'invalid audio '..sound.id..': '..tostring(why) end
        local music=false
        for _,binding in ipairs(pack.bindings) do
            if binding.cue_id:find('/music/',1,true) or
                binding.cue_id:find('/ambient/',1,true) then
                for _,v in ipairs(binding.variants) do
                    if v.asset_id==sound.id then music=true;break end
                end
            end
            if music then break end
        end
        local okay,source=pcall(make_source,bytes,sound,music and 'stream' or 'static')
        if not okay or not source then
            return nil,'audio decode failed '..sound.id..': '..tostring(source)
        end
        templates[sound.id]=source
    end
    local bindings=binding_map(pack)
    local custom_count=#pack.bindings
    local fallback_count=0
    if published then
        -- A valid project pack may intentionally override only a few cues.
        -- Keep the original placeholder coverage for every unbound context.
        local original,original_bytes=Samples.showcase('builtin_audio')
        local needed={}
        for _,binding in ipairs(original.bindings) do
            if not bindings[binding.cue_id] then
                bindings[binding.cue_id]=binding
                needed[binding.variants[1].asset_id]=true
                fallback_count=fallback_count+1
            end
        end
        for _,sound in ipairs(original.sounds) do
            if needed[sound.id] and not templates[sound.id] then
                local music=sound.id:match('original_music$') or
                    sound.id:match('original_ambience$')
                local okay,source=pcall(make_source,original_bytes[sound.path],
                    sound,music and 'stream' or 'static')
                if not okay or not source then
                    return nil,'original audio decode failed '..sound.id..': '..
                        tostring(source)
                end
                templates[sound.id]=source
            end
        end
    end
    return {pack=pack,status=status,templates=templates,
        bindings=bindings,custom_binding_count=published and custom_count or 0,
        fallback_binding_count=fallback_count,
        signature=Audio.checksum(Audio.encode(pack))}
end
function R.new(read,make_source)
    local self=setmetatable({read=read,make_source=make_source,
        elapsed=0,voices={},music=nil,retiring_music=nil,
        scene_id=nil,ambient_id=nil,ambience=nil,retiring_ambience=nil,
        variant_counter=0,last_played={},status='absent',
        paused=false,max_total_voices=24,played_count=0}, {__index=R})
    self:refresh()
    return self
end
function R:refresh()
    local loaded,reason=candidate(self.read,self.make_source)
    if not loaded then
        if not self.pack then
            local fallback=select(1,candidate(function()return nil end,self.make_source))
            if fallback then
                loaded=fallback
                loaded.status='original built-in samples (published audio rejected: '..
                    tostring(reason)..')'
            end
        end
    end
    if not loaded then
        self.status='rejected; kept previous audio: '..tostring(reason)
        return false,self.status
    end
    if self.signature==loaded.signature then
        self.status='unchanged · '..loaded.status
        return true,self.status
    end
    self:stop_all()
    local was_paused=self.paused
    self.paused=false
    self.pack,self.templates,self.bindings,self.signature=loaded.pack,
        loaded.templates,loaded.bindings,loaded.signature
    self.custom_binding_count=loaded.custom_binding_count
    self.fallback_binding_count=loaded.fallback_binding_count
    self.status='refreshed · '..loaded.status
    local scene,ambient=self.scene_id,self.ambient_id
    self.scene_id,self.ambient_id=nil,nil
    if scene then self:set_scene(scene) end
    if ambient then self:set_ambience(ambient) end
    if was_paused then self:set_paused(true) end
    return true,self.status
end
function R:stop_all()
    for _,voice in ipairs(self.voices) do voice.source:stop() end
    self.voices={}
    if self.music then self.music.source:stop() end
    if self.retiring_music then self.retiring_music.source:stop() end
    self.music,self.retiring_music=nil,nil
    if self.ambience then self.ambience.source:stop() end
    if self.retiring_ambience then self.retiring_ambience.source:stop() end
    self.ambience,self.retiring_ambience=nil,nil
end
function R:select_variant(binding)
    self.variant_counter=self.variant_counter+1
    local weight=0
    for _,v in ipairs(binding.variants) do weight=weight+v.weight end
    local position=(self.variant_counter-1)%weight+1
    for _,v in ipairs(binding.variants) do
        position=position-v.weight
        if position<=0 then return v.asset_id end
    end
end
local function clone(template)
    return template:clone()
end
function R:emit(cue_id,position,listener)
    local binding=self.bindings and self.bindings[cue_id]
    if not binding then return false,'unbound' end
    local mix=self.pack.mix[category_for(cue_id)]
    if not mix or mix.mute or mix.volume==0 then return false,'muted' end
    local last=self.last_played[cue_id] or -math.huge
    if self.elapsed-last<binding.cooldown_ms/1000 then return false,'cooldown' end
    local active=0
    for i=#self.voices,1,-1 do
        if not self.voices[i].source:isPlaying() then table.remove(self.voices,i)
        elseif self.voices[i].cue_id==cue_id then active=active+1 end
    end
    if active>=binding.max_voices then return false,'voice cap' end
    if #self.voices>=self.max_total_voices then
        local at,priority
        for i,voice in ipairs(self.voices) do
            if not priority or voice.priority<priority then at=i;priority=voice.priority end
        end
        if priority and priority<=binding.priority then
            self.voices[at].source:stop();table.remove(self.voices,at)
        else return false,'global voice cap' end
    end
    local distance=1
    if binding.distance>0 and position and listener then
        local dx,dy=position.x-listener.x,position.y-listener.y
        local delta=math.sqrt(dx*dx+dy*dy)
        if delta>binding.distance then return false,'out of range' end
        distance=1-delta/(binding.distance+1)
    end
    local asset_id=self:select_variant(binding)
    local okay,source=pcall(clone,self.templates[asset_id])
    if not okay then self.status='audio clone failed: '..tostring(source);return false,self.status end
    source:setVolume(binding.volume*mix.volume*distance)
    source:setPitch(binding.pitch)
    source:setLooping(binding.loop)
    local played,err=pcall(source.play,source)
    if not played then self.status='audio play failed: '..tostring(err);return false,self.status end
    self.last_played[cue_id]=self.elapsed
    self.voices[#self.voices+1]={source=source,cue_id=cue_id,priority=binding.priority}
    self.played_count=self.played_count+1
    return true,asset_id
end
function R:event(event,state)
    local player=state and state.player_id
    local listener
    if state and player then
        for _,entity in ipairs(state.entities or {}) do
            if entity.id==player then listener=entity.position;break end
        end
    end
    local position=event.x and {x=event.x,y=event.y} or event.source_position
    local result={}
    for _,cue_id in ipairs(R.cues_for_event(event,state)) do
        local played,detail=self:emit(cue_id,position,listener)
        if played then result[#result+1]={cue_id=cue_id,asset_id=detail} end
    end
    return result
end
function R:set_scene(cue_id)
    if self.scene_id==cue_id then return false,'already playing scene' end
    self.scene_id=cue_id
    local binding=self.bindings and self.bindings[cue_id]
    if not binding then
        if self.music then self.music.source:stop() end
        self.music=nil
        return false,'scene unbound'
    end
    local asset_id=self:select_variant(binding)
    local okay,source=pcall(clone,self.templates[asset_id])
    if not okay then self.status='music clone failed: '..tostring(source);return false,self.status end
    local mix=self.pack.mix[category_for(cue_id)] or {volume=1,mute=false}
    local target=mix.mute and 0 or binding.volume*mix.volume
    local transition=binding.transition
    local duration=transition.mode=='instant' and 0 or transition.duration_ms/1000
    local fade_time=transition.mode=='fade' and duration/2 or duration
    local delay=transition.mode=='fade' and self.music and duration/2 or 0
    source:setLooping(binding.loop)
    source:setPitch(binding.pitch)
    source:setVolume(fade_time>0 and 0 or target)
    local played,why=pcall(source.play,source)
    if not played then self.status='music play failed: '..tostring(why);return false,self.status end
    if self.retiring_music then self.retiring_music.source:stop() end
    self.retiring_music=self.music
    if self.retiring_music then self.retiring_music.retire_duration=fade_time end
    if self.retiring_music and transition.mode=='instant' then
        self.retiring_music.source:stop();self.retiring_music=nil
    end
    self.music={source=source,target=target,duration=fade_time,delay=delay,elapsed=0}
    self.played_count=self.played_count+1
    return true,asset_id
end
function R:set_ambience(cue_id)
    if self.ambient_id==cue_id then return false,'already playing ambience' end
    self.ambient_id=cue_id
    local binding=self.bindings and self.bindings[cue_id]
    if not binding then
        if self.ambience then self.ambience.source:stop() end
        self.ambience=nil
        return false,'ambience unbound'
    end
    local asset_id=self:select_variant(binding)
    local okay,source=pcall(clone,self.templates[asset_id])
    if not okay then self.status='ambience clone failed: '..tostring(source)
        return false,self.status end
    local mix=self.pack.mix[category_for(cue_id)] or {volume=1,mute=false}
    local target=mix.mute and 0 or binding.volume*mix.volume
    local transition=binding.transition
    local duration=transition.mode=='instant' and 0 or transition.duration_ms/1000
    local fade_time=transition.mode=='fade' and duration/2 or duration
    local delay=transition.mode=='fade' and self.ambience and duration/2 or 0
    source:setLooping(binding.loop)
    source:setPitch(binding.pitch)
    source:setVolume(fade_time>0 and 0 or target)
    local played,why=pcall(source.play,source)
    if not played then self.status='ambience play failed: '..tostring(why)
        return false,self.status end
    if self.retiring_ambience then self.retiring_ambience.source:stop() end
    self.retiring_ambience=self.ambience
    if self.retiring_ambience then self.retiring_ambience.retire_duration=fade_time end
    if self.retiring_ambience and duration==0 then
        self.retiring_ambience.source:stop();self.retiring_ambience=nil
    end
    self.ambience={source=source,target=target,duration=fade_time,delay=delay,elapsed=0}
    self.played_count=self.played_count+1
    return true,asset_id
end
function R:update(dt)
    if self.paused then return end
    dt=math.max(0,math.min(dt or 0,.25))
    self.elapsed=self.elapsed+dt
    if self.music and self.music.duration>0 then
        local m=self.music
        if m.delay and m.delay>0 then m.delay=math.max(0,m.delay-dt)
        else
            m.elapsed=math.min(m.duration,m.elapsed+dt)
            m.source:setVolume(m.target*m.elapsed/m.duration)
        end
    end
    if self.retiring_music then
        local m=self.retiring_music
        local duration=m.retire_duration or 0
        m.elapsed=(m.elapsed or 0)+dt
        if duration<=0 or m.elapsed>=duration then
            m.source:stop();self.retiring_music=nil
        else m.source:setVolume(m.target*(1-m.elapsed/duration)) end
    end
    if self.ambience and self.ambience.duration>0 then
        local a=self.ambience
        if a.delay and a.delay>0 then a.delay=math.max(0,a.delay-dt)
        else
            a.elapsed=math.min(a.duration,a.elapsed+dt)
            a.source:setVolume(a.target*a.elapsed/a.duration)
        end
    end
    if self.retiring_ambience then
        local a=self.retiring_ambience
        local duration=a.retire_duration or 0
        a.elapsed=(a.elapsed or 0)+dt
        if duration<=0 or a.elapsed>=duration then
            a.source:stop();self.retiring_ambience=nil
        else a.source:setVolume(a.target*(1-a.elapsed/duration)) end
    end
end
function R:set_paused(value)
    if self.paused==value then return end
    self.paused=value
    local function act(entry)
        if not entry then return end
        if value then
            entry.resume=entry.source:isPlaying()
            if entry.resume then entry.source:pause() end
        elseif entry.resume then
            entry.source:play();entry.resume=nil
        end
    end
    act(self.music);act(self.retiring_music)
    act(self.ambience);act(self.retiring_ambience)
    for _,voice in ipairs(self.voices) do act(voice) end
end
return R
