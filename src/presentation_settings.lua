-- Player-facing presentation preferences. Never enters a run snapshot.
local Codec=require('src.persistence')
local S={format='roag-presentation-settings/1',file='presentation-settings.roag'}
function S.defaults()
    return {format=S.format,master=100,music=80,effects=90,
        shake=true,flash=true,combat_effects=true,damage_numbers=true,
        font='pixel',text_size='normal',high_contrast=false}
end
function S.validate(value)
    assert(type(value)=='table' and value.format==S.format,'invalid presentation format')
    for _,key in ipairs({'master','music','effects'}) do
        local number=value[key]
        assert(type(number)=='number' and number%1==0 and
            number>=0 and number<=100,'invalid '..key..' volume')
    end
    for _,key in ipairs({'shake','flash','combat_effects','damage_numbers','high_contrast'}) do
        assert(type(value[key])=='boolean','invalid '..key..' option')
    end
    assert(value.font=='pixel' or value.font=='sans','invalid font option')
    assert(value.text_size=='small' or value.text_size=='normal' or
        value.text_size=='large','invalid text size')
    for key in pairs(value) do
        assert(S.defaults()[key]~=nil,'unexpected presentation option')
    end
    return true
end
function S.load(storage)
    local okay,bytes=pcall(storage.read,storage,S.file)
    if not okay then return S.defaults(),'read error' end
    if bytes==nil then return S.defaults(),'default' end
    local valid,value=pcall(Codec.decode,bytes)
    if not valid or not pcall(S.validate,value) then
        return S.defaults(),'invalid'
    end
    return value,'loaded'
end
function S.save(storage,value)
    local valid,reason=pcall(S.validate,value)
    if not valid then return false,tostring(reason) end
    local bytes=Codec.encode(value)
    local wrote,result=pcall(storage.write,storage,S.file,bytes)
    if not wrote or not result then return false,'settings write failed' end
    local read_ok,readback=pcall(storage.read,storage,S.file)
    if not read_ok or readback~=bytes then return false,'settings verification failed' end
    return true
end
return S
