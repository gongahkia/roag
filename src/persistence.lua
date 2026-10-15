-- Inert, deterministic plain-data codec and two-generation active-run store.
-- Storage is injected; this module has no LÖVE dependency.
local Game=require('src.game')
local Content=require('src.content')
local BoonContent=require('src.boon_content')
local Generation=require('src.generation')
local World=require('src.world')

local P={}
P.format='roag-save/1'
P.slots={'active-a.roag','active-b.roag'}
P.max_bytes=32*1024*1024
P.compatibility=table.concat({Content.version,BoonContent.version,
    Generation.room_content_version,World.version},'|')

local function sorted_keys(value)
    local keys={}
    for key in pairs(value) do
        assert(type(key)=='string' or (type(key)=='number' and key%1==0),
            'unsupported table key')
        keys[#keys+1]=key
    end
    table.sort(keys,function(a,b)
        if type(a)~=type(b) then return type(a)=='number' end
        return a<b
    end)
    return keys
end
local function encode_value(value,out,seen,depth)
    assert(depth<=100,'save nesting limit')
    local kind=type(value)
    if kind=='nil' then out[#out+1]='Z'
    elseif kind=='boolean' then out[#out+1]=value and 'Y' or 'N'
    elseif kind=='number' then
        assert(value==value and value~=math.huge and value~=-math.huge,'nonfinite save number')
        local s=string.format('%.17g',value)
        out[#out+1]='D'..#s..':'..s
    elseif kind=='string' then out[#out+1]='S'..#value..':'..value
    elseif kind=='table' then
        assert(not seen[value],'cyclic save table')
        seen[value]=true
        local keys=sorted_keys(value)
        out[#out+1]='T'..#keys..':'
        for _,key in ipairs(keys) do
            encode_value(key,out,seen,depth+1)
            encode_value(value[key],out,seen,depth+1)
        end
        seen[value]=nil
    else error('unsupported save value: '..kind) end
end
function P.encode(value)
    local out={}
    encode_value(value,out,{},0)
    local bytes=table.concat(out)
    assert(#bytes<=P.max_bytes,'save size limit')
    return bytes
end
function P.decode(bytes)
    assert(type(bytes)=='string' and #bytes<=P.max_bytes,'invalid save payload size')
    local pos,nodes=1,0
    local function count()
        local s,e,num=bytes:find('(%d+):',pos)
        assert(s==pos and num and #num<=10,'invalid save length')
        pos=e+1
        local value=tonumber(num)
        assert(value and value<=P.max_bytes,'save length limit')
        return value
    end
    local parse
    parse=function(depth)
        assert(depth<=100,'save nesting limit')
        nodes=nodes+1
        assert(nodes<=2000000,'save node limit')
        local tag=bytes:sub(pos,pos);pos=pos+1
        if tag=='Z' then return nil
        elseif tag=='Y' then return true
        elseif tag=='N' then return false
        elseif tag=='S' or tag=='D' then
            local length=count()
            assert(pos+length-1<=#bytes,'truncated save value')
            local raw=bytes:sub(pos,pos+length-1);pos=pos+length
            if tag=='S' then return raw end
            assert(raw:match('^[%+%-%d%.eE]+$'),'invalid save number')
            local value=tonumber(raw)
            assert(value and value==value and value~=math.huge and value~=-math.huge,
                'nonfinite save number')
            return value
        elseif tag=='T' then
            local count_value=count()
            assert(count_value<=1000000,'save table limit')
            local result={}
            for _=1,count_value do
                local key=parse(depth+1)
                assert(type(key)=='string' or
                    (type(key)=='number' and key%1==0),'invalid save key')
                assert(result[key]==nil,'duplicate save key')
                local value=parse(depth+1)
                assert(value~=nil,'nil save table value')
                result[key]=value
            end
            return result
        end
        error('invalid save tag')
    end
    local result=parse(0)
    assert(pos==#bytes+1,'trailing save data')
    return result
end
local function checksum(bytes)
    local a,b=1,0
    for i=1,#bytes do
        a=(a+bytes:byte(i))%65521
        b=(b+a)%65521
    end
    return string.format('%08x',b*65536+a)
end
local function envelope(seq,kind,snapshot)
    local payload=P.encode(snapshot or {})
    return table.concat({P.format,tostring(seq),kind,
        snapshot and snapshot.schema or 'none',P.compatibility,
        tostring(#payload),checksum(payload)},'\n')..'\n'..payload
end
local function parse_record(bytes)
    assert(type(bytes)=='string' and #bytes<=P.max_bytes+512,'invalid save record size')
    local lines={}
    local pos=1
    for i=1,7 do
        local endpos=assert(bytes:find('\n',pos,true),'truncated save header')
        lines[i]=bytes:sub(pos,endpos-1)
        pos=endpos+1
    end
    local seq=tonumber(lines[2])
    assert(seq and seq%1==0 and seq>=1 and seq<9007199254740991,'invalid generation')
    assert(lines[3]=='active' or lines[3]=='terminal' or lines[3]=='pending_terminal',
        'invalid save kind')
    local size=tonumber(lines[6])
    assert(size and size%1==0 and size>=0 and size<=P.max_bytes
        and #bytes-pos+1==size,'save payload size mismatch')
    local payload=bytes:sub(pos)
    assert(checksum(payload)==lines[7],'save checksum mismatch')
    return {format=lines[1],seq=seq,kind=lines[3],schema=lines[4],compatibility=lines[5],
        snapshot=P.decode(payload)}
end
local function validated(record)
    if record.format~=P.format then return false,'incompatible envelope format' end
    if record.compatibility~=P.compatibility then
        return false,'incompatible content'
    end
    if record.kind=='terminal' then return true end
    if record.schema~=Game.snapshot_schema then
        return false,'incompatible snapshot schema'
    end
    local ok,result=pcall(Game.restore,record.snapshot)
    if not ok then return false,'invalid save state: '..tostring(result) end
    return true,result
end
function P.new(storage)
    assert(type(storage)=='table' and type(storage.read)=='function'
        and type(storage.write)=='function','invalid storage adapter')
    return setmetatable({storage=storage},{__index=P})
end
function P.inspect(self)
    local records,invalid={},0
    for i,name in ipairs(P.slots) do
        local ok,bytes=pcall(self.storage.read,self.storage,name)
        if ok and bytes~=nil then
            local decoded,record=pcall(parse_record,bytes)
            if decoded then record.slot=i;records[#records+1]=record
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(records,function(a,b)
        if a.seq~=b.seq then return a.seq>b.seq end
        return a.slot>b.slot
    end)
    local record=records[1]
    if not record then
        return {status=invalid>0 and 'corrupt' or 'absent',invalid=invalid}
    end
    local compatible,loaded=validated(record)
    if not compatible then
        return {status=loaded:find('incompatible',1,true) and 'incompatible' or 'corrupt',
            reason=loaded,record=record,invalid=invalid}
    end
    local recovered=invalid>0
    return {status=record.kind=='terminal' and 'terminal' or
        record.kind=='pending_terminal' and 'pending_terminal' or
        (recovered and 'recovered' or 'active'),record=record,state=loaded,
        invalid=invalid}
end
function P.load(self)
    local found=P.inspect(self)
    if found.status=='active' or found.status=='recovered' then
        return found.state,found
    end
    return nil,found
end
local function write_record(self,kind,snapshot)
    local found=P.inspect(self)
    if found.status=='incompatible' then return false,'incompatible save requires New Run confirmation' end
    local previous=found.record
    local slot=previous and (3-previous.slot) or 1
    local seq=previous and previous.seq+1 or 1
    local bytes=envelope(seq,kind,snapshot)
    local name=P.slots[slot]
    local ok,result=pcall(self.storage.write,self.storage,name,bytes)
    if not ok or not result then return false,'save write failed: '..tostring(result) end
    local read_ok,readback=pcall(self.storage.read,self.storage,name)
    if not read_ok or readback~=bytes then return false,'save read-back failed' end
    local valid,record=pcall(parse_record,readback)
    if not valid or record.seq~=seq then return false,'save verification failed' end
    return true,seq
end
function P.save(self,state)
    if state.game_over then
        return P.tombstone(self)
    end
    local snapshot=Game.snapshot(state)
    local ok,reason=pcall(Game.restore,snapshot)
    if not ok then return false,'unstable save boundary: '..tostring(reason) end
    return write_record(self,'active',snapshot)
end
-- Terminal award outbox. This record is never offered as live Continue.
-- It can be replayed idempotently into the separate profile before tombstoning.
function P.pending_terminal(self,state)
    assert(state.game_over,'pending terminal requires finished run')
    local snapshot=Game.snapshot(state)
    local ok,reason=pcall(Game.restore,snapshot)
    if not ok then return false,'unstable terminal boundary: '..tostring(reason) end
    local first,why=write_record(self,'pending_terminal',snapshot)
    if not first then return false,why end
    local second,final=write_record(self,'pending_terminal',snapshot)
    if not second then return false,'terminal outbox backup failed: '..tostring(final) end
    return true,final
end
function P.tombstone(self)
    local ok,reason=write_record(self,'terminal',nil)
    if not ok then return false,reason end
    local second,final=write_record(self,'terminal',nil)
    if not second then return false,'terminal backup failed: '..tostring(final) end
    return true,final
end
-- Explicit replacement after the user confirms New Run. Invalid/incompatible slots
-- are overwritten with a new generation in both positions so stale data cannot revive.
function P.replace(self,state)
    local snapshot=Game.snapshot(state)
    local ok,reason=pcall(Game.restore,snapshot)
    if not ok then return false,'unstable new run: '..tostring(reason) end
    local previous=P.inspect(self).record
    local first=previous and 3-previous.slot or 1
    local second=3-first
    local sequence=previous and previous.seq+1 or 1
    local function verified_write(slot,number)
        local name=P.slots[slot]
        local bytes=envelope(number,'active',snapshot)
        local wrote,result=pcall(self.storage.write,self.storage,name,bytes)
        if not wrote or not result then return false end
        local read_ok,readback=pcall(self.storage.read,self.storage,name)
        if not read_ok or readback~=bytes then return false end
        local valid,record=pcall(parse_record,readback)
        return valid and record.seq==number
    end
    if not verified_write(first,sequence) then
        return false,'new run save failed; prior generation retained'
    end
    if not verified_write(second,sequence+1) then
        -- The new active generation is already verified. A later checkpoint
        -- replaces the stale backup; do not report an unsaved run as failure.
        return true,'new run saved with one verified generation; backup write failed'
    end
    local check=P.inspect(self)
    if check.status~='active' or check.record.seq~=sequence+1 then
        return false,'new run verification failed'
    end
    return true
end
return P
