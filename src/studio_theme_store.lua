-- Two independent checked generations for E04 source and optional runtime pack.
local Theme=require('src.studio_theme')
local Project=require('src.studio_project')
local S={paths={'assets/studio/theme-a.pack','assets/studio/theme-b.pack'}}
local function hash(bytes)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format('%08x',b*65536+a)
end
local function path(id,slot)
    assert(Project.valid_project_id(id),'invalid project ID')
    return 'studio/projects/'..id..'/theme-'..slot..'.txt'
end
local function record(seq,theme,kind)
    assert(type(seq)=='number' and seq%1==0 and seq>=1 and seq<9007199254740991)
    local payload=Theme.encode(theme)
    return table.concat({'ROAG-STUDIO-'..kind..'/1',seq,#payload,hash(payload)},'\n')..
        '\n'..payload
end
local function header(bytes,kind)
    assert(type(bytes)=='string' and #bytes<=66000,'invalid theme record size')
    local format,seq,size,sum,payload=bytes:match('^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$')
    assert(format=='ROAG-STUDIO-'..kind..'/1','incompatible theme record')
    seq,size=tonumber(seq),tonumber(size)
    assert(seq and seq%1==0 and seq>=1 and seq<9007199254740991 and
        size and size%1==0 and size>=0 and size<=65536 and
        #payload==size and hash(payload)==sum,'corrupt theme record')
    return {seq=seq,payload=payload}
end
local function inspect(read,paths,kind,assets,id)
    local records,invalid={},0
    for slot,p in ipairs(paths) do
        local ok,bytes=pcall(read,p)
        if ok and bytes then
            local valid,entry=pcall(header,bytes,kind)
            if valid then entry.slot=slot;records[#records+1]=entry
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(records,function(a,b)return a.seq>b.seq end)
    for index,entry in ipairs(records) do
        local ok,theme=pcall(Theme.decode,entry.payload)
        if not ok and index==1 then
            return nil,'incompatible or invalid theme: '..tostring(theme),entry
        end
        if ok then
            local valid,why=pcall(Theme.validate,theme,assets)
            if id and theme.project_id~=id then valid=false;why='wrong project ID' end
            if not valid and index==1 then return nil,'invalid theme: '..tostring(why),entry end
            if valid then return theme,(index>1 or invalid>0) and
                'recovered previous theme generation' or 'loaded',entry end
        end
    end
    return nil,invalid>0 and 'corrupt theme' or 'absent'
end
function S.new(io) return setmetatable({io=io},{__index=S}) end
function S:open(id,assets)
    local paths={path(id,'a'),path(id,'b')}
    local theme,status,entry=inspect(function(p) return self.io:read(p) end,
        paths,'THEME-SOURCE',assets,id)
    if status=='absent' then status='missing theme document' end
    return theme,status,entry
end
function S:save(theme,assets)
    local valid,why=pcall(Theme.validate,theme,assets)
    if not valid then return false,tostring(why) end
    local previous,status,entry=self:open(theme.project_id,assets)
    if not previous and status~='missing theme document' and status~='corrupt theme' then
        return false,status
    end
    local slot=entry and (entry.slot==1 and 'b' or 'a') or 'a'
    local seq=entry and entry.seq+1 or 1
    local bytes=record(seq,theme,'THEME-SOURCE')
    local filename=path(theme.project_id,slot)
    local made,merr=pcall(self.io.mkdir,self.io,'studio/projects/'..theme.project_id)
    if not made then return false,tostring(merr) end
    local wrote,result=pcall(self.io.write,self.io,filename,bytes)
    if not wrote or not result then return false,tostring(result or 'theme write failed') end
    local readok,again=pcall(self.io.read,self.io,filename)
    if not readok or again~=bytes then return false,'theme read-back failed' end
    local reopened,reason,verified=self:open(theme.project_id,assets)
    if not reopened or verified.seq~=seq then return false,'theme verification failed: '..tostring(reason) end
    return true,seq
end
function S.inspect_published(read)
    return inspect(read,S.paths,'PUBLISHED-THEME')
end
function S.plan_published(theme,read)
    Theme.validate(theme)
    local current,status,entry=S.inspect_published(read)
    assert(current or status=='absent' or status=='corrupt theme',
        'refusing to overwrite incompatible published theme: '..tostring(status))
    local slot=entry and (entry.slot==1 and 2 or 1) or 1
    local seq=entry and entry.seq+1 or 1
    local p=S.paths[slot]
    return {path=p,bytes=record(seq,theme,'PUBLISHED-THEME'),seq=seq,
        overwrite=read(p)~=nil}
end
return S
