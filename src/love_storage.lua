-- LÖVE save-directory adapter. conf.lua supplies the stable identity.
local Storage={}
function Storage:read(name)
    if not love.filesystem.getInfo(name) then return nil end
    local bytes,err=love.filesystem.read(name)
    if not bytes then error(err or 'save read failed') end
    return bytes
end
function Storage:write(name,bytes)
    local ok,err=love.filesystem.write(name,bytes)
    if not ok then error(err or 'save write failed') end
    return true
end
return Storage
