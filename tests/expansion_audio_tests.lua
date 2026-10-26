return function(test,equal)
    local Audio=require('src.studio_audio')
    local Store=require('src.studio_audio_store')
    local Samples=require('src.studio_audio_samples')
    local Cues=require('src.audio_cues')
    local Runtime=require('src.studio_audio_runtime')
    local Project=require('src.studio_project')
    local Persistence=require('src.persistence')
    local function memory()
        local io={files={}}
        function io:read(path)return self.files[path]end
        function io:write(path,bytes)
            if self.fail then error('injected write failure') end
            self.files[path]=bytes;return true
        end
        function io:mkdir()return true end
        return io
    end
    local function fixture()
        local io=memory();local store=Store.new(io)
        local pack,source=Samples.showcase('audio_test')
        for _,sound in ipairs(pack.sounds) do
            local made=assert(store:import(pack,sound.id,sound.name,
                source[sound.path],{author=sound.author,license=sound.license,
                    tags=sound.tags}))
            equal(made,sound)
        end
        assert(store:save(pack))
        return io,store,pack,source
    end
    local function fake_source()
        local source={playing=false,volume=0,play_count=0}
        function source:clone()return fake_source()end
        function source:setVolume(v)self.volume=v end
        function source:setPitch(v)self.pitch=v end
        function source:setLooping(v)self.loop=v end
        function source:play()self.playing=true;self.play_count=self.play_count+1 end
        function source:stop()self.playing=false end
        function source:pause()self.playing=false end
        function source:isPlaying()return self.playing end
        return source
    end
    test('audio cue registry covers class, enemy, elite, terrain, boon, room and music contexts',function()
        local cues=Cues.list();assert(#cues>=260)
        local seen={}
        for _,cue in ipairs(cues) do
            assert(Project.valid_id(cue.id) and not seen[cue.id]);seen[cue.id]=true
        end
        for _,id in ipairs({'core:cue/player/move','core:cue/ability/ranger_shot',
            'core:cue/enemy/mycelial_matriarch/telegraph',
            'core:cue/elite/mycelial_matriarch/phase','core:cue/terrain/sticky_bog',
            'core:cue/boon/storm_conductor','core:cue/ui/achievement',
            'core:cue/music/explore/sundered_forge'}) do
            assert(Cues.get(id),'missing '..id)
        end
        local room={rooms={{id='studio:room/test',name='Test room'}}}
        local id=Cues.room_cue(room.rooms[1].id,'music')
        assert(Cues.get(id,room) and Cues.get(id))
    end)
    test('original synthesized WAV samples parse and source IDs remain stable',function()
        local pack,source=Samples.showcase('audio_test')
        assert(#pack.sounds==8 and #pack.bindings>=27)
        Audio.validate(pack,function(path)return source[path]end)
        for _,sound in ipairs(pack.sounds) do
            local bytes=source[sound.path]
            local format,duration=Audio.format(bytes)
            assert(format=='wav' and duration>=.08 and duration<=2.01)
            assert(sound.checksum==Audio.checksum(bytes))
            assert(Audio.published_path(sound):match('^assets/studio/audio/'))
            assert(not sound.path:find('%.%./',1,true))
        end
        local bad=Samples.wav('step'):sub(1,-3)
        assert(not pcall(Audio.format,bad))
        assert(not pcall(Audio.format,'RIFFnot an audio file'))
    end)
    test('audio source save/reopen, immutable replace and torn newest recovery',function()
        local io,store,pack=fixture()
        local loaded,reason=store:open('audio_test')
        assert(reason=='loaded' and Audio.encode(loaded)==Audio.encode(pack))
        local old=pack.sounds[1]
        local replacement=assert(store:import(pack,old.id,old.name,
            Samples.wav('hit'),{author=old.author,license=old.license,tags=old.tags}))
        assert(replacement.path~=old.path and io.files[old.path])
        pack.sounds[1]=replacement
        assert(store:save(pack))
        local newest,status,entry=store:open('audio_test')
        assert(status=='loaded' and newest.sounds[1].path==replacement.path)
        io.files['studio/projects/audio_test/audio-b.pack']='truncated'
        local recovered,why=store:open('audio_test')
        assert(why=='recovered previous audio generation' and
            recovered.sounds[1].path==old.path)
        io.files['studio/projects/audio_test/audio-b.pack']=nil
        io.fail=true
        local okay=store:save(pack)
        assert(not okay)
        local safe=assert(store:open('audio_test'))
        assert(safe.sounds[1].path==old.path)
    end)
    test('audio metadata validates references, settings, provenance and paths',function()
        local _,_,pack,source=fixture()
        local bytes=Audio.encode(pack)
        equal(Audio.decode(bytes),pack)
        local function invalid(change)
            local value=Audio.decode(bytes);change(value)
            assert(not pcall(Audio.validate,value,function(path)return source[path]end))
        end
        invalid(function(value)value.sounds[1].path='../escape.wav'end)
        invalid(function(value)value.sounds[1].license=string.rep('x',513)end)
        invalid(function(value)value.sounds[2].id=value.sounds[1].id end)
        invalid(function(value)value.bindings[1].variants[1].asset_id='studio:audio/missing'end)
        invalid(function(value)value.bindings[1].max_voices=0 end)
        invalid(function(value)value.bindings[1].transition.mode='unknown'end)
        invalid(function(value)value.bindings[2].cue_id=value.bindings[1].cue_id end)
        assert(not pcall(Audio.decode,bytes:sub(1,#bytes-6)))
        local mismatch=Audio.decode(bytes)
        source[mismatch.sounds[1].path]='corrupt'
        assert(not pcall(Audio.validate,mismatch,function(path)return source[path]end))
    end)
    test('audio publishing refuses unverified source permissions without changing drafts',function()
        local pack=Samples.showcase('audio_test')
        assert(Audio.validate_publish_permissions(pack))
        local before=Audio.encode(pack)
        pack.sounds[1].license='User supplied; verify rights'
        assert(not pcall(Audio.validate_publish_permissions,pack))
        pack.sounds[1].license='Original project asset'
        pack.sounds[1].author=''
        assert(not pcall(Audio.validate_publish_permissions,pack))
        pack.sounds[1].author='ROAG Studio synthesized'
        assert(Audio.encode(pack)==before,
            'permission rejection altered editable source metadata')
    end)
    test('audio publish plan is read-only and complete generations recover safely',function()
        local io,_,pack,source=fixture()
        local published={}
        local function read(path)return published[path]end
        local plan=Store.plan_published(pack,read)
        assert(plan.seq==1 and published[plan.path]==nil)
        for _,sound in ipairs(pack.sounds) do
            published[Audio.published_path(sound)]=source[sound.path]
        end
        published[plan.path]=plan.bytes
        local loaded,status,entry=Store.inspect_published(read)
        assert(status=='loaded' and entry.seq==1 and
            Audio.encode(loaded)==Audio.encode(pack))
        local second=Store.plan_published(pack,read)
        assert(second.seq==2 and second.path~=plan.path)
        published[second.path]='torn'
        local fallback,reason=Store.inspect_published(read)
        assert(fallback and reason=='recovered previous audio generation')
        published[second.path]=second.bytes
        published[Audio.published_path(pack.sounds[1])]=nil
        local none,why=Store.inspect_published(read)
        assert(not none and why=='corrupt audio')
        assert(io.files[pack.sounds[1].path],
            'published damage affected editable original')
    end)
    test('audio event routing and wall-time playback do not mutate game snapshots',function()
        local pack,source=Samples.showcase('audio_test')
        local published={}
        for _,sound in ipairs(pack.sounds) do
            published[Audio.published_path(sound)]=source[sound.path]
        end
        local plan=Store.plan_published(pack,function(path)return published[path]end)
        published[plan.path]=plan.bytes
        local count=0
        local runtime=Runtime.new(function(path)return published[path]end,
            function()count=count+1;return fake_source()end)
        assert(runtime.pack and runtime.status:match('refreshed') and count==8)
        local Game=require('src.game')
        local state=Game.new(7,'core:map/test_room')
        local before=Persistence.encode(Game.snapshot(state))
        local event={kind='Moved',source_id=state.player_id,
            x=3,y=2,source_position={x=3,y=2}}
        local routed=runtime:event(event,state)
        assert(#routed==1 and routed[1].cue_id=='core:cue/player/move')
        local again=runtime:event(event,state)
        assert(#again==0,'audio cooldown ignored')
        runtime:update(.1)
        assert(#runtime:event(event,state)==1)
        assert(runtime:set_scene('core:cue/music/title'))
        runtime:update(.3)
        local music=runtime.music.source
        assert(music.volume>0 and music.loop)
        assert(not runtime:set_scene('core:cue/music/title'))
        runtime:set_scene('core:cue/music/explore/overgrown_ruins')
        assert(runtime.retiring_music)
        for _=1,4 do runtime:update(.25) end
        assert(runtime.retiring_music==nil)
        local okay,status=runtime:refresh()
        assert(okay and status:match('unchanged'))
        equal(Persistence.encode(Game.snapshot(state)),before)
    end)
    test('audio voices, variants, mixer and pause obey presentation-only limits',function()
        local pack,source=Samples.showcase('audio_test')
        local move
        for _,binding in ipairs(pack.bindings) do
            if binding.cue_id=='core:cue/player/move' then move=binding;break end
        end
        assert(move)
        move.cooldown_ms=0;move.max_voices=1
        move.variants[2]={asset_id=pack.sounds[2].id,weight=2}
        pack.mix.player.volume=.5
        local published={}
        for _,sound in ipairs(pack.sounds) do
            published[Audio.published_path(sound)]=source[sound.path]
        end
        local plan=Store.plan_published(pack,function(path)return published[path]end)
        published[plan.path]=plan.bytes
        local runtime=Runtime.new(function(path)return published[path]end,
            function()return fake_source()end)
        local played,id=runtime:emit('core:cue/player/move')
        assert(played and id==pack.sounds[1].id and runtime.voices[1].source.volume==.5)
        local blocked,reason=runtime:emit('core:cue/player/move')
        assert(not blocked and reason=='voice cap')
        runtime:set_paused(true)
        assert(not runtime.voices[1].source:isPlaying())
        runtime:set_paused(false)
        assert(runtime.voices[1].source:isPlaying())
        runtime.voices[1].source:stop()
        local second,next_id=runtime:emit('core:cue/player/move')
        assert(second and next_id==pack.sounds[2].id)
        runtime.pack.mix.player.mute=true
        assert(not runtime:emit('core:cue/player/move'))
        assert(runtime:set_ambience('core:cue/ambient/overgrown_ruins'))
        assert(runtime.ambience.source.loop)
        runtime:set_ambience(nil)
        assert(runtime.ambience==nil)
    end)
    test('partial published audio overrides one cue and retains original unbound sounds',function()
        local pack,source=Samples.showcase('audio_test')
        local move=pack.bindings[1]
        pack.bindings={move}
        local published={}
        for _,sound in ipairs(pack.sounds) do
            published[Audio.published_path(sound)]=source[sound.path]
        end
        local plan=Store.plan_published(pack,function(path)return published[path]end)
        published[plan.path]=plan.bytes
        local runtime=Runtime.new(function(path)return published[path]end,
            function()return fake_source()end)
        assert(runtime.custom_binding_count==1 and
            runtime.fallback_binding_count>=20)
        assert(runtime:emit('core:cue/player/move'))
        assert(runtime:emit('core:cue/enemy/telegraph'),
            'an unbound enemy cue became silent under a partial custom pack')
        assert(runtime:set_scene('core:cue/music/explore/fungal_hollows'))
        assert(runtime.music and runtime.music.source:isPlaying())
    end)
    test('audio refresh rejects incompatible newer data and falls back on corruption',function()
        local pack,source=Samples.showcase('audio_test')
        local published={}
        for _,sound in ipairs(pack.sounds) do
            published[Audio.published_path(sound)]=source[sound.path]
        end
        local plan=Store.plan_published(pack,function(path)return published[path]end)
        published[plan.path]=plan.bytes
        local runtime=Runtime.new(function(path)return published[path]end,
            function()return fake_source()end)
        assert(#runtime.pack.sounds==8)
        local bad=Audio.decode(Audio.encode(pack));bad.schema='future-audio/2'
        local payload=Persistence.encode(bad)
        local newer=Store.paths[2]
        published[newer]=('ROAG-STUDIO-PUBLISHED-AUDIO/1\n2\n%d\n%s\n%s'):
            format(#payload,Audio.checksum(payload),payload)
        local ok,status=runtime:refresh()
        assert(not ok and status:match('rejected') and #runtime.pack.sounds==8)
        local fresh=Runtime.new(function(path)return published[path]end,
            function()return fake_source()end)
        assert(fresh.status:match('built%-in') and #fresh.pack.bindings>=27)
        published[newer]='torn'
        local recovered=Runtime.new(function(path)return published[path]end,
            function()return fake_source()end)
        assert(recovered.status:match('refreshed') and
            #recovered.pack.bindings==#pack.bindings)
        published[plan.path]='torn'
        local fallback=Runtime.new(function(path)return published[path]end,
            function()return fake_source()end)
        assert(fallback.status:match('corrupt') and
            #fallback.pack.bindings>=27)
    end)
    test('player music and effects volumes update active sounds independently',function()
        local runtime=Runtime.new(function()return nil end,
            function()return fake_source()end)
        assert(runtime:emit('core:cue/player/move'))
        assert(runtime:set_scene('core:cue/music/explore/overgrown_ruins'))
        local voice=runtime.voices[1]
        local original_effect=voice.source.volume
        local original_music=runtime.music.target
        runtime:set_user_mix(.25,.5)
        assert(math.abs(voice.source.volume-original_effect*.5)<.000001)
        assert(math.abs(runtime.music.target-original_music*.25)<.000001)
        for _=1,20 do runtime:update(.25) end
        assert(math.abs(runtime.music.source.volume-original_music*.25)<.000001)
    end)
end
