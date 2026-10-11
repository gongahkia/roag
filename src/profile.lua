-- Independent, headless two-generation permanent profile store.
-- Storage has read(name) and write(name,bytes), as with active-run persistence.
local Codec=require("src.persistence")
local Achievements=require("src.achievement_content")
local Boons=require("src.boon_content")
local Profile={}
Profile.schema="roeg-profile/1"
Profile.format="roeg-profile-record/1"
Profile.content=Achievements.version.."|"..Boons.version
Profile.slots={"profile-a.roeg","profile-b.roeg"}
Profile.max_bytes=131072
local function checksum(bytes)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format("%08x",b*65536+a)
end
function Profile.fresh()
    return {schema=Profile.schema,content=Profile.content,next_run_serial=1,earned={}}
end
function Profile.validate(value)
    Achievements.validate()
    assert(type(value)=="table" and value.schema==Profile.schema
        and value.content==Profile.content,"incompatible profile content")
    assert(type(value.next_run_serial)=="number" and value.next_run_serial%1==0
        and value.next_run_serial>=1 and value.next_run_serial<9007199254740991,
        "invalid profile run serial")
    assert(type(value.earned)=="table","invalid profile achievements")
    for id,earned in pairs(value.earned) do
        assert(Achievements.get(id) and earned==true,"invalid profile achievement ID")
    end
    for key in pairs(value) do
        assert(key=="schema" or key=="content" or key=="next_run_serial"
            or key=="earned","invalid profile field")
    end
    return true
end
local function pack(sequence,profile)
    Profile.validate(profile)
    local payload=Codec.encode(profile)
    assert(#payload<=Profile.max_bytes,"profile size limit")
    return table.concat({Profile.format,tostring(sequence),Profile.schema,
        Profile.content,tostring(#payload),checksum(payload)},"\n").."\n"..payload
end
local function unpack(bytes)
    assert(type(bytes)=="string" and #bytes<=Profile.max_bytes+256,
        "invalid profile record size")
    local lines,pos={},1
    for i=1,6 do
        local stop=assert(bytes:find("\n",pos,true),"truncated profile header")
        lines[i]=bytes:sub(pos,stop-1);pos=stop+1
    end
    local sequence=tonumber(lines[2])
    local length=tonumber(lines[5])
    assert(sequence and sequence%1==0 and sequence>=1 and sequence<9007199254740991,
        "invalid profile generation")
    assert(length and length%1==0 and length>=0 and length<=Profile.max_bytes
        and #bytes-pos+1==length,"profile payload size mismatch")
    local payload=bytes:sub(pos)
    assert(checksum(payload)==lines[6],"profile checksum mismatch")
    return {format=lines[1],sequence=sequence,schema=lines[3],content=lines[4],
        profile=Codec.decode(payload)}
end
function Profile.new(storage)
    assert(type(storage)=="table" and type(storage.read)=="function"
        and type(storage.write)=="function","invalid profile storage")
    return setmetatable({storage=storage},{__index=Profile})
end
function Profile.inspect(self)
    local records,invalid={},0
    for slot,name in ipairs(Profile.slots) do
        local ok,bytes=pcall(self.storage.read,self.storage,name)
        if ok and bytes~=nil then
            local valid,record=pcall(unpack,bytes)
            if valid then record.slot=slot;records[#records+1]=record
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(records,function(a,b)
        if a.sequence~=b.sequence then return a.sequence>b.sequence end
        return a.slot>b.slot
    end)
    local top=records[1]
    if not top then return {status=invalid>0 and "corrupt" or "absent",invalid=invalid} end
    if top.format~=Profile.format or top.schema~=Profile.schema
        or top.content~=Profile.content then
        return {status="incompatible",reason="Incompatible Profile",record=top,invalid=invalid}
    end
    local valid,reason=pcall(Profile.validate,top.profile)
    if not valid then return {status="corrupt",reason=tostring(reason),record=top,
        invalid=invalid} end
    return {status=invalid>0 and "recovered" or "active",
        profile=top.profile,record=top,invalid=invalid}
end
function Profile.save(self,value)
    local found=self:inspect()
    if found.status=="incompatible" or found.status=="corrupt" then
        return false,"profile "..found.status.."; progress was not overwritten"
    end
    local old=found.record
    local slot=old and 3-old.slot or 1
    local sequence=old and old.sequence+1 or 1
    local ok,bytes=pcall(pack,sequence,value)
    if not ok then return false,tostring(bytes) end
    local name=Profile.slots[slot]
    local wrote,result=pcall(self.storage.write,self.storage,name,bytes)
    if not wrote or not result then return false,"profile write failed: "..tostring(result) end
    local read_ok,readback=pcall(self.storage.read,self.storage,name)
    if not read_ok or readback~=bytes then return false,"profile read-back failed" end
    local valid,record=pcall(unpack,readback)
    if not valid or record.sequence~=sequence then return false,"profile verification failed" end
    return true,sequence
end
function Profile.allocate_run(self)
    local found=self:inspect()
    if found.status=="corrupt" or found.status=="incompatible" then
        return nil,found.reason or found.status
    end
    local value=found.profile or Profile.fresh()
    local serial=value.next_run_serial
    local next_value=Codec.decode(Codec.encode(value))
    next_value.next_run_serial=serial+1
    local ok,reason=self:save(next_value)
    if not ok then return nil,reason end
    return ("local-run/%d"):format(serial),next_value
end
function Profile.commit_awards(self,pending)
    local found=self:inspect()
    if found.status=="corrupt" or found.status=="incompatible" then
        return false,found.reason or found.status
    end
    local value=found.profile or Profile.fresh()
    local next_value=Codec.decode(Codec.encode(value))
    local changed=false
    for id,flag in pairs(pending) do
        if not Achievements.get(id) or flag~=true then return false,"invalid pending award" end
        if not next_value.earned[id] then next_value.earned[id]=true;changed=true end
    end
    if changed then
        local ok,reason=self:save(next_value)
        if not ok then return false,reason end
    end
    return true,next_value
end
return Profile
