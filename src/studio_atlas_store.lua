-- Checked two-generation atlas activation. Pages are immutable, content named
-- and validated before the inactive record is written.
local Atlas=require('src.studio_atlas')
local S={format='ROAG-STUDIO-ATLAS-RECORD/1',disable_path='assets/studio/atlas-off.flag'}
local function line_header(bytes)
    assert(type(bytes)=='string' and #bytes<=1024*1024+200,'invalid atlas record size')
    local format,seq,size,hash,payload=bytes:match('^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$')
    assert(format==S.format,'incompatible atlas record')
    seq,size=tonumber(seq),tonumber(size)
    assert(seq and seq%1==0 and seq>=1 and size and size%1==0 and
        size>=0 and size<=1024*1024 and #payload==size and
        hash==Atlas.checksum(payload),'corrupt atlas record')
    return {seq=seq,payload=payload}
end
function S.record(seq,pack)
    local payload=Atlas.encode(pack)
    return table.concat({S.format,seq,#payload,Atlas.checksum(payload)},'\n')..
        '\n'..payload
end
local function records(read)
    local out,invalid={},0
    for index,path in ipairs(Atlas.slots) do
        local ok,bytes=pcall(read,path)
        if ok and bytes then
            local valid,result=pcall(line_header,bytes)
            if valid then result.slot=index;out[#out+1]=result
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(out,function(a,b)return a.seq>b.seq end)
    return out,invalid
end
function S.inspect(read,project,verify_page)
    local off=read(S.disable_path)
    if off then return nil,'disabled' end
    local current,invalid=records(read)
    if #current==0 then return nil,invalid>0 and 'corrupt atlas records' or 'absent' end
    local reason
    for _,record in ipairs(current) do
        local valid,pack=pcall(Atlas.decode,record.payload)
        local compatible,why=false,nil
        if valid then
            local accepted,result,message=pcall(Atlas.matches_inputs,pack,project,read)
            compatible=accepted and result
            why=accepted and message or tostring(result)
        end
        if valid and compatible then
            local pages_ok=true
            for _,page in ipairs(pack.pages) do
                local bytes=read(page.filename)
                if type(bytes)~='string' or #bytes>32*1024*1024 or
                    Atlas.checksum(bytes)~=page.checksum or
                    (verify_page and not verify_page(bytes,page)) then
                    pages_ok=false;reason='missing or corrupt atlas page';break
                end
            end
            if pages_ok then
                return pack,record==current[1] and invalid==0 and 'loaded' or
                    'recovered previous atlas generation',record
            end
        else
            reason=valid and ('stale atlas for current loose images: '..
                tostring(why)) or 'incompatible atlas metadata'
        end
    end
    return nil,reason or 'no complete atlas generation'
end
function S.plan(pack,read)
    Atlas.validate(pack)
    local current=records(read)
    local newest=current[1]
    local slot=newest and (newest.slot==1 and 2 or 1) or 1
    local seq=newest and newest.seq+1 or 1
    return {path=Atlas.slots[slot],bytes=S.record(seq,pack),seq=seq,
        occupied=read(Atlas.slots[slot])~=nil}
end
return S
