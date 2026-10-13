-- Checked, independent source and published audio generations. Source bytes
-- are immutable by checksum; manifests are written only after every clip.
local Audio=require('src.studio_audio')
local Project=require('src.studio_project')
local S={paths={'assets/studio/audio-a.pack','assets/studio/audio-b.pack'}}
local function source_path(id,slot)
    assert(Project.valid_project_id(id),'invalid Studio audio project ID')
    return 'studio/projects/'..id..'/audio-'..slot..'.pack'
end
local function record(seq,pack,kind)
    local payload=Audio.encode(pack)
    return table.concat({'ROEG-STUDIO-'..kind..'/1',seq,#payload,
        Audio.checksum(payload)},'\n')..'\n'..payload
end
local function header(bytes,kind)
    assert(type(bytes)=='string' and #bytes<=270000,'invalid audio record size')
    local tag,seq,size,sum,payload=bytes:match('^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$')
    assert(tag=='ROEG-STUDIO-'..kind..'/1','incompatible audio record')
    seq,size=tonumber(seq),tonumber(size)
    assert(seq and seq%1==0 and seq>=1 and seq<9007199254740991 and
        size and size%1==0 and size<=262144 and #payload==size and
        Audio.checksum(payload)==sum,'corrupt audio record')
    return {seq=seq,payload=payload}
end
local function inspect(read,paths,kind,source_read,id)
    local records,invalid={},0
    for slot,path in ipairs(paths) do
        local ok,bytes=pcall(read,path)
        if ok and bytes then
            local valid,entry=pcall(header,bytes,kind)
            if valid then entry.slot=slot;records[#records+1]=entry
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(records,function(a,b)return a.seq>b.seq end)
    for index,entry in ipairs(records) do
        local ok,pack=pcall(Audio.decode,entry.payload)
        if not ok then
            if index==1 then return nil,'incompatible audio metadata: '..tostring(pack),entry end
        else
            local valid,why=pcall(Audio.validate,pack,source_read)
            if id and pack.project_id~=id then valid=false;why='wrong audio project ID' end
            if valid then
                return pack,(index>1 or invalid>0) and
                    'recovered previous audio generation' or 'loaded',entry
            end
            -- A complete but incompatible newest definition is never silently
            -- replaced with old content. Missing/corrupt bytes can recover.
            if index==1 and not tostring(why):match('missing/corrupt audio source') then
                return nil,'invalid audio metadata: '..tostring(why),entry
            end
            invalid=invalid+1
        end
    end
    return nil,invalid>0 and 'corrupt audio' or 'absent'
end
function S.new(io) return setmetatable({io=io},{__index=S}) end
function S:open(id)
    local paths={source_path(id,'a'),source_path(id,'b')}
    local pack,status,entry=inspect(function(path)return self.io:read(path)end,
        paths,'AUDIO-SOURCE',function(path)return self.io:read(path)end,id)
    if status=='absent' then status='missing audio document' end
    return pack,status,entry
end
function S:import(pack,id,name,bytes,provenance)
    local sound=Audio.make_sound(pack.project_id,id,name,bytes,provenance)
    local ok,why=pcall(function()
        self.io:mkdir('studio/projects/'..pack.project_id..'/audio')
        local old=self.io:read(sound.path)
        if old then assert(old==bytes,'immutable audio source path collision')
        else
            self.io:write(sound.path,bytes)
            assert(self.io:read(sound.path)==bytes,'audio source read-back failed')
        end
    end)
    if not ok then return nil,tostring(why) end
    return sound
end
function S:save(pack)
    local valid,why=pcall(Audio.validate,pack,function(path)return self.io:read(path)end)
    if not valid then return false,tostring(why) end
    local previous,status,entry=self:open(pack.project_id)
    if not previous and status~='missing audio document' and status~='corrupt audio' then
        return false,status
    end
    local slot=entry and (entry.slot==1 and 'b' or 'a') or 'a'
    local seq=entry and entry.seq+1 or 1
    local bytes=record(seq,pack,'AUDIO-SOURCE')
    local path=source_path(pack.project_id,slot)
    local ok,result=pcall(function()
        self.io:mkdir('studio/projects/'..pack.project_id)
        self.io:write(path,bytes)
        assert(self.io:read(path)==bytes,'audio manifest read-back failed')
    end)
    if not ok then return false,tostring(result) end
    local loaded,reason,verified=self:open(pack.project_id)
    if not loaded or not verified or verified.seq~=seq then
        return false,'audio verification failed: '..tostring(reason)
    end
    return true,seq
end
function S.inspect_published(read)
    return inspect(read,S.paths,'PUBLISHED-AUDIO',function(path)
        local basename=path:match('([^/]+)$')
        return basename and read('assets/studio/audio/'..basename)
    end)
end
function S.plan_published(pack,read)
    Audio.validate(pack)
    local current,status,entry=S.inspect_published(read)
    assert(current or status=='absent' or status=='corrupt audio',
        'refusing to overwrite incompatible published audio: '..tostring(status))
    local slot=entry and (entry.slot==1 and 2 or 1) or 1
    return {path=S.paths[slot],bytes=record(entry and entry.seq+1 or 1,
        pack,'PUBLISHED-AUDIO'),seq=entry and entry.seq+1 or 1,
        overwrite=read(S.paths[slot])~=nil}
end
return S
