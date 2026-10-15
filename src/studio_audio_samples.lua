-- Original, deterministic PCM16 placeholder cues. No third-party asset or
-- runtime game RNG is involved. Replace by importing WAV/Ogg in Studio.
local S={}
local function le16(n) return string.char(n%256,math.floor(n/256)%256) end
local function le32(n) return le16(n%65536)..le16(math.floor(n/65536)) end
function S.wav(kind)
    local rate=22050
    local specs={step={.085,150,0.2},hit={.14,85,0.36},
        boon={.28,660,0.26},menu={.09,480,0.18},
        enemy={.17,110,0.26},terrain={.12,190,0.24},
        music={2.0,220,0.13},ambience={2.0,96,0.055}}
    local spec=assert(specs[kind],'unknown original audio sample')
    local count=math.floor(rate*spec[1]+.5)
    local data={}
    for i=0,count-1 do
        local t=i/rate
        local envelope=(kind=='music' or kind=='ambience') and
            .5-.5*math.cos(2*math.pi*i/count)
            or (1-i/count)^2
        local wave=math.sin(2*math.pi*spec[2]*t)
        if kind=='hit' or kind=='enemy' then
            wave=wave*.7+math.sin(2*math.pi*(spec[2]*2.03)*t)*.3
        elseif kind=='boon' or kind=='music' or kind=='ambience' then
            wave=wave*.65+math.sin(2*math.pi*(spec[2]*1.5)*t)*.35
        end
        local sample=math.floor(math.max(-1,math.min(1,wave*envelope*spec[3]))*32767+.5)
        if sample<0 then sample=sample+65536 end
        data[#data+1]=le16(sample)
    end
    local pcm=table.concat(data)
    local fmt=le16(1)..le16(1)..le32(rate)..le32(rate*2)..le16(2)..le16(16)
    return 'RIFF'..le32(4+8+#fmt+8+#pcm)..'WAVE'..
        'fmt '..le32(#fmt)..fmt..'data'..le32(#pcm)..pcm
end
function S.showcase(project_id)
    local Audio=require('src.studio_audio')
    local pack=Audio.new(project_id)
    local source={}
    local bindings={
        {'step','core:cue/player/move'},
        {'hit','core:cue/player/damage'},
        {'enemy','core:cue/enemy/telegraph'},
        {'terrain','core:cue/terrain/step'},
        {'boon','core:cue/boon/activate'},
        {'menu','core:cue/ui/confirm'},
        {'music','core:cue/music/title'},
    }
    local Biomes=require('src.biomes')
    for _,biome_id in ipairs(Biomes.order) do
        local name=biome_id:match('([^/]+)$')
        bindings[#bindings+1]={'music','core:cue/music/explore/'..name}
        bindings[#bindings+1]={'music','core:cue/music/elite/'..name}
        bindings[#bindings+1]={'ambience','core:cue/ambient/'..name}
    end
    for _,id in ipairs({'final_boss','victory','defeat'}) do
        bindings[#bindings+1]={'music','core:cue/music/'..id}
    end
    local sound_by_kind={}
    for _,pair in ipairs(bindings) do
        local kind,cue=pair[1],pair[2]
        local sound=sound_by_kind[kind]
        if not sound then
            local bytes=S.wav(kind)
            local id='studio:audio/original_'..kind
            sound=Audio.make_sound(project_id,id,'Original '..kind..' placeholder',
                bytes,{author='ROAG Studio synthesized',license='Original project asset',
                    tags='placeholder'})
            pack.sounds[#pack.sounds+1]=sound
            source[sound.path]=bytes
            sound_by_kind[kind]=sound
        end
        pack.bindings[#pack.bindings+1]=Audio.binding(cue,sound.id)
    end
    return pack,source
end
return S
