-- Independent two-generation E03 document store in the E00 project workspace.
local World=require('src.studio_world')
local Project=require('src.studio_project')
local S={}
local function hash(bytes)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format('%08x',b*65536+a)
end
local function path(id,slot)
    assert(Project.valid_project_id(id),'invalid project ID')
    return 'studio/projects/'..id..'/world-'..slot..'.txt'
end
local function unpack_record(bytes)
    assert(type(bytes)=='string' and #bytes<=1024*1024+100,'invalid world record')
    local header,seq,size,sum,payload=bytes:match('^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$')
    assert(header=='ROAG-STUDIO-WORLD-RECORD/1','incompatible world record')
    seq,size=tonumber(seq),tonumber(size)
    assert(seq and seq%1==0 and seq>=1 and size and size%1==0 and
        size>=0 and size<=1024*1024 and #payload==size and hash(payload)==sum,
        'corrupt world record')
    return {seq=seq,payload=payload}
end
function S.new(io) return setmetatable({io=io},{__index=S}) end
function S:open(id,assets)
    local records,invalid={},0
    for _,slot in ipairs({'a','b'}) do
        local ok,bytes=pcall(self.io.read,self.io,path(id,slot))
        if ok and bytes then
            local valid,record=pcall(unpack_record,bytes)
            if valid then
                record.slot=slot;records[#records+1]=record
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(records,function(a,b)return a.seq>b.seq end)
    for i,record in ipairs(records) do
        local decoded,pack=pcall(World.decode,record.payload)
        if not decoded and i==1 then
            return nil,'incompatible or invalid world source: '..tostring(pack)
        end
        local ok=decoded and pack.project_id==id and
            pcall(World.validate,pack,assets)
        if ok then return pack,(i>1 or invalid>0) and
            'recovered previous world generation' or 'opened',record end
    end
    if #records>0 or invalid>0 then return nil,'corrupt world document' end
    return nil,'missing world document'
end
function S:save(pack,assets)
    local ok,err=pcall(World.validate,pack,assets)
    if not ok then return false,tostring(err) end
    local old,reason,record=self:open(pack.project_id,assets)
    if not old and reason~='missing world document' then return false,reason end
    local seq=record and record.seq+1 or 1
    local slot=record and (record.slot=='a' and 'b' or 'a') or 'a'
    local payload=World.encode(pack)
    local bytes=table.concat({'ROAG-STUDIO-WORLD-RECORD/1',seq,#payload,hash(payload)},'\n')..'\n'..payload
    local filename=path(pack.project_id,slot)
    local made,merr=pcall(self.io.mkdir,self.io,'studio/projects/'..pack.project_id)
    if not made then return false,tostring(merr) end
    local wrote,werr=pcall(self.io.write,self.io,filename,bytes)
    if not wrote or not werr then return false,tostring(werr or 'world write failed') end
    local readok,again=pcall(self.io.read,self.io,filename)
    if not readok or again~=bytes then return false,'world read-back failed' end
    local valid,new=pcall(unpack_record,again)
    if not valid or new.seq~=seq then return false,'world verification failed' end
    return true,seq
end
return S
