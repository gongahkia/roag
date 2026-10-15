-- E07 source audio metadata. Plain data: playback and authored bytes live elsewhere.
local Project=require('src.studio_project')
local Codec=require('src.persistence')
local Cues=require('src.audio_cues')
local A={schema='roag-studio-audio/1',max_sounds=256,max_bindings=512,
    max_source_bytes=32*1024*1024,max_project_bytes=128*1024*1024}
A.categories={'player','enemy','boss','elite','class','ability','boon',
    'terrain','world','room','biome','ui','music'}
local category={};for _,v in ipairs(A.categories) do category[v]=true end
local function check(ok,why) if not ok then error(why,2) end end
local function integer(n,a,b) return type(n)=='number' and n%1==0 and n>=a and n<=b end
local function number(n,a,b) return type(n)=='number' and n==n and n>=a and n<=b end
function A.checksum(bytes)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format('%08x',b*65536+a)
end
function A.format(bytes)
    check(type(bytes)=='string' and #bytes>=32 and #bytes<=A.max_source_bytes,
        'audio source must be 32 bytes to 32 MiB')
    if bytes:sub(1,4)=='RIFF' and bytes:sub(9,12)=='WAVE' then
        local function u16(i) local a,b=bytes:byte(i,i+1);return a+b*256 end
        local function u32(i) local a,b,c,d=bytes:byte(i,i+3)
            return a+b*256+c*65536+d*16777216 end
        check(u32(5)+8==#bytes,'WAV RIFF length mismatch')
        local at,channels,rate,bits,data=13
        while at+7<=#bytes do
            local kind=bytes:sub(at,at+3);local size=u32(at+4)
            check(size<=#bytes-at-7,'truncated WAV chunk')
            if kind=='fmt ' then
                check(size>=16 and u16(at+8)==1,'only PCM WAV is supported')
                channels,rate,bits=u16(at+10),u32(at+12),u16(at+22)
            elseif kind=='data' then data=size end
            at=at+8+size+(size%2)
        end
        check((channels==1 or channels==2) and integer(rate,8000,96000) and
            (bits==8 or bits==16) and data and data>0,
            'unsupported WAV channels, rate, depth or empty data')
        local duration=data/(rate*channels*(bits/8))
        check(duration>=.01 and duration<=1800,'WAV duration outside 0.01–1800 seconds')
        return 'wav',duration
    end
    if bytes:sub(1,4)=='OggS' and bytes:find('vorbis',1,true) then
        return 'ogg',nil -- LÖVE decoder performs the definitive stream check.
    end
    error('only PCM WAV or Ogg Vorbis is supported')
end
function A.source_path(project_id,id,checksum,format)
    check(Project.valid_project_id(project_id) and Project.valid_id(id),
        'invalid project or audio ID')
    check(type(checksum)=='string' and checksum:match('^%x%x%x%x%x%x%x%x$') and
        (format=='wav' or format=='ogg'),'invalid audio checksum or format')
    local slug=id:gsub('[^%w_%-]','_')
    local path='studio/projects/'..project_id..'/audio/'..slug..'-'..checksum..'.'..format
    check(Project.valid_path(path),'unsafe audio source path')
    return path
end
function A.published_path(sound)
    local basename=sound.path:match('([^/]+)$')
    check(basename and not basename:find('%.%.'),'invalid published audio basename')
    return 'assets/studio/audio/'..basename
end
function A.validate_publish_permissions(pack)
    for _,sound in ipairs(pack.sounds) do
        check(sound.author~='' and sound.license~='' and
            sound.license~='User supplied; verify rights',
            'complete author/license permission for '..sound.id..' before publishing')
    end
    return true
end
function A.make_sound(project_id,id,name,bytes,provenance)
    check(Project.valid_project_id(project_id) and Project.valid_id(id),
        'invalid audio identity')
    check(type(name)=='string' and #name>0 and #name<=80 and
        not name:find('[%c]'),'invalid audio name')
    local format,duration=A.format(bytes)
    local checksum=A.checksum(bytes)
    provenance=provenance or {}
    return {id=id,name=name,tags=provenance.tags or '',
        author=provenance.author or '',source_url=provenance.source_url or '',
        license=provenance.license or '',format=format,bytes=#bytes,
        checksum=checksum,duration=duration,
        path=A.source_path(project_id,id,checksum,format)}
end
function A.new(project_id)
    check(Project.valid_project_id(project_id),'invalid audio project ID')
    local mix={}
    for _,name in ipairs(A.categories) do mix[name]={volume=1,mute=false} end
    return {schema=A.schema,project_id=project_id,sounds={},bindings={},mix=mix}
end
function A.binding(cue_id,asset_id)
    check(Cues.get(cue_id),'unknown audio cue')
    local music=cue_id:find('/music/',1,true)~=nil or
        cue_id:find('/ambient/',1,true)~=nil
    return {cue_id=cue_id,variants={{asset_id=asset_id,weight=1}},
        volume=1,pitch=1,loop=music,priority=music and 10 or 5,
        max_voices=music and 1 or 4,cooldown_ms=music and 0 or 60,
        distance=0,transition={mode=music and 'crossfade' or 'instant',duration_ms=music and 800 or 0}}
end
function A.validate(pack,read,world_pack)
    check(type(pack)=='table' and pack.schema==A.schema,'incompatible audio schema')
    check(Project.valid_project_id(pack.project_id),'invalid audio project ID')
    check(type(pack.sounds)=='table' and #pack.sounds<=A.max_sounds,'invalid audio library')
    local ids,total_bytes={},0
    for _,sound in ipairs(pack.sounds) do
        check(type(sound)=='table' and Project.valid_id(sound.id) and not ids[sound.id],
            'duplicate or invalid audio ID')
        ids[sound.id]=sound
        for _,key in ipairs({'name','tags','author','source_url','license'}) do
            local value=sound[key]
            check(type(value)=='string' and #value<=512 and not value:find('[%c]'),
                'invalid audio '..key..' for '..sound.id)
        end
        check(#sound.name>0,'audio name required')
        check(sound.format=='wav' or sound.format=='ogg','unsupported audio format')
        check(integer(sound.bytes,32,A.max_source_bytes) and
            type(sound.checksum)=='string' and sound.checksum:match('^%x%x%x%x%x%x%x%x$'),
            'invalid audio length/checksum')
        total_bytes=total_bytes+sound.bytes
        check(total_bytes<=A.max_project_bytes,'audio project exceeds 128 MiB')
        check(type(sound.path)=='string' and Project.valid_path(sound.path) and
            sound.path==A.source_path(pack.project_id,sound.id,sound.checksum,sound.format),
            'unsafe or mismatched audio source path')
        check(sound.duration==nil or number(sound.duration,.01,1800),
            'invalid audio duration')
        if read then
            local bytes=read(sound.path)
            check(type(bytes)=='string' and #bytes==sound.bytes and
                A.checksum(bytes)==sound.checksum,'missing/corrupt audio source: '..sound.id)
            local format=A.format(bytes)
            check(format==sound.format,'audio source format mismatch: '..sound.id)
        end
    end
    check(type(pack.bindings)=='table' and #pack.bindings<=A.max_bindings,
        'invalid audio bindings')
    local bound={}
    for _,binding in ipairs(pack.bindings) do
        check(type(binding)=='table' and type(binding.cue_id)=='string' and
            Cues.get(binding.cue_id,world_pack) and not bound[binding.cue_id],
            'duplicate or unknown audio cue')
        bound[binding.cue_id]=true
        check(type(binding.variants)=='table' and #binding.variants>=1 and
            #binding.variants<=8,'audio binding requires 1–8 variants')
        local variants={}
        for _,variant in ipairs(binding.variants) do
            check(type(variant)=='table' and ids[variant.asset_id] and
                not variants[variant.asset_id] and integer(variant.weight,1,100),
                'missing/duplicate audio variant or invalid weight')
            variants[variant.asset_id]=true
        end
        check(number(binding.volume,0,2) and number(binding.pitch,.5,2) and
            type(binding.loop)=='boolean' and integer(binding.priority,0,10) and
            integer(binding.max_voices,1,16) and integer(binding.cooldown_ms,0,5000) and
            integer(binding.distance,0,40),'invalid audio playback settings')
        local transition=binding.transition
        check(type(transition)=='table' and
            ({instant=true,fade=true,crossfade=true})[transition.mode] and
            integer(transition.duration_ms,0,5000),'invalid audio transition')
    end
    check(type(pack.mix)=='table','missing audio category mixer')
    for name,setting in pairs(pack.mix) do
        check(category[name] and type(setting)=='table' and
            number(setting.volume,0,2) and type(setting.mute)=='boolean',
            'invalid audio category mixer')
    end
    for _,name in ipairs(A.categories) do check(pack.mix[name],'missing '..name..' mixer') end
    return true
end
function A.encode(pack)
    A.validate(pack)
    local bytes=Codec.encode(pack)
    check(#bytes<=262144,'audio metadata exceeds 256 KiB')
    return bytes
end
function A.decode(bytes)
    check(type(bytes)=='string' and #bytes<=262144,'invalid audio metadata length')
    local pack=Codec.decode(bytes)
    A.validate(pack)
    return pack
end
return A
