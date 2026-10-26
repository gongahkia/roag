-- Published E03 packs are optional and rotate across two checked records.
local W=require('src.studio_world')
local R={paths={'assets/studio/world-a.pack','assets/studio/world-b.pack'}}
local function sum(bytes)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format('%08x',b*65536+a)
end
function R.record(seq,pack)
    assert(type(seq)=='number' and seq%1==0 and seq>=1,'invalid pack sequence')
    local payload=W.encode(pack)
    return table.concat({'ROAG-STUDIO-PUBLISHED-WORLD/1',seq,#payload,sum(payload)},'\n')..
        '\n'..payload
end
function R.header(bytes)
    assert(type(bytes)=='string' and #bytes<=1024*1024+100,'invalid published pack size')
    local format,seq,size,checksum,payload=bytes:match('^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n(.*)$')
    assert(format=='ROAG-STUDIO-PUBLISHED-WORLD/1','incompatible published pack format')
    seq,size=tonumber(seq),tonumber(size)
    assert(seq and seq%1==0 and seq>=1 and size and size%1==0 and
        size>=0 and size<=1024*1024 and #payload==size and checksum==sum(payload),
        'corrupt published pack')
    return {seq=seq,payload=payload}
end
function R.inspect(read)
    local records,invalid={},0
    for index,path in ipairs(R.paths) do
        local ok,bytes=pcall(read,path)
        if ok and bytes then
            local valid,record=pcall(R.header,bytes)
            if valid then record.slot=index;records[#records+1]=record
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(records,function(a,b)return a.seq>b.seq end)
    if #records==0 then return nil,invalid>0 and 'corrupt published world' or 'absent' end
    local newest=records[1]
    local ok,pack=pcall(W.decode,newest.payload)
    if not ok then return nil,'incompatible or invalid published world: '..tostring(pack),newest end
    return pack,invalid>0 and 'recovered previous published world' or 'loaded',newest
end
function R.plan(pack,read)
    W.validate(pack)
    local current,status,record=R.inspect(read)
    assert(current or status=='absent' or status=='corrupt published world',
        'refusing to overwrite incompatible published world: '..tostring(status))
    local slot=record and (record.slot==1 and 2 or 1) or 1
    local seq=record and record.seq+1 or 1
    local path=R.paths[slot]
    local bytes=R.record(seq,pack)
    return {path=path,bytes=bytes,seq=seq,status=status,
        overwrite=read(path)~=nil}
end
return R
