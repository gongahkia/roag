local IO={}
function IO:read(path)
    if not love.filesystem.getInfo(path) then return nil end
    local bytes,err=love.filesystem.read(path)
    if not bytes then error(err or 'read failed') end
    return bytes
end
function IO:write(path,bytes)
    local ok,err=love.filesystem.write(path,bytes)
    if not ok then error(err or 'write failed') end
    return true
end
function IO:mkdir(path)
    local ok,err=love.filesystem.createDirectory(path)
    if not ok then error(err or 'directory creation failed') end
    return true
end
function IO:list(path)
    if not love.filesystem.getInfo(path) then return {} end
    return love.filesystem.getDirectoryItems(path)
end
return IO
