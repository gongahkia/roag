-- Loose-image publisher for Studio's explicit preview/confirm flow.
-- Source artwork stays in LÖVE's save directory; output is the launched checkout.
local Project=require('src.studio_project')
local Publish=require('src.studio_publish')
local ffi=require('ffi')
local M={}

if ffi.os=='Windows' then
    ffi.cdef('int _mkdir(const char *path);')
else
    ffi.cdef('int mkdir(const char *path, unsigned int mode);')
end

local function disk(root)
    local fs={}
    local function full(path)
        assert(Project.valid_path(path),'unsafe checkout path')
        local last=root:sub(-1)
        return root..((last=='/' or last=='\\') and '' or '/')..path
    end
    function fs:read(path)
        local file=io.open(full(path),'rb')
        if not file then return nil end
        local bytes=assert(file:read('*a'))
        assert(file:close())
        return bytes
    end
    function fs:mkdir(path)
        assert(path=='assets/studio' or path=='assets/studio/sources',
            'unsafe publish directory')
        local current=root
        for part in path:gmatch('[^/]+') do
            local last=current:sub(-1)
            current=current..((last=='/' or last=='\\') and '' or '/')..part
            local result=ffi.os=='Windows' and ffi.C._mkdir(current)
                or ffi.C.mkdir(current,493) -- 0755
            assert(result==0 or ffi.errno()==17,
                'cannot create publish directory: '..current)
        end
        return true
    end
    function fs:write(path,bytes)
        assert(path=='assets/studio/manifest.txt' or
            path:match('^assets/studio/sources/[a-z0-9_%-]+%.png$'),
            'unsafe publish file')
        local file=assert(io.open(full(path),'wb'))
        assert(file:write(bytes))
        assert(file:close())
        assert(self:read(path)==bytes,'publish read-back mismatch: '..path)
        return true
    end
    return fs
end

function M.new(root,fs)
    assert(type(root)=='string' and #root>1 and not root:find('%z') and
        (root:sub(1,1)=='/' or root:match('^[A-Za-z]:[/\\]')),
        'Studio must run from an absolute checkout directory')
    fs=fs or disk(root)
    assert(fs:read('main.lua') and fs:read('conf.lua') and
        fs:read('src/studio_project.lua'),
        'GUI publish requires a writable source checkout, not a .love archive')
    return setmetatable({root=root,fs=fs},{__index=M})
end

function M:plan(project,source_read)
    local files=Publish.plan(project,source_read,function(path)
        return self.fs:read(path)
    end)
    local old_manifest=self.fs:read('assets/studio/manifest.txt')
    local result={files=files,old_manifest=old_manifest,
        counts={create=0,overwrite=0,unchanged=0},blocked=nil}
    if old_manifest then
        local valid,published=pcall(Project.decode,old_manifest)
        if not valid then
            result.blocked='Existing published manifest is invalid; inspect it before replacing.'
        elseif published.id~=project.id then
            result.blocked='Published art belongs to '..published.id..'; use its project or the CLI.'
        end
    end
    for _,file in ipairs(files) do
        result.counts[file.status]=result.counts[file.status]+1
        if file.status=='overwrite' and file.path~='assets/studio/manifest.txt' then
            result.blocked='Published source collision: '..file.path
        end
    end
    return result
end

local function same_plan(a,b)
    if not a or not b or a.old_manifest~=b.old_manifest or
        #a.files~=#b.files then return false end
    for index,file in ipairs(a.files) do
        local other=b.files[index]
        if file.path~=other.path or file.status~=other.status or
            file.bytes~=other.bytes then return false end
    end
    return true
end

function M:apply(project,source_read,preview,allow_overwrite,backup_manifest)
    local fresh=self:plan(project,source_read)
    if not same_plan(preview,fresh) then
        return false,'Source or destination changed; preview again before publishing.'
    end
    if fresh.blocked then return false,fresh.blocked end
    if fresh.counts.overwrite>0 and not allow_overwrite then
        return false,'Type OVERWRITE in Studio to replace the published manifest.'
    end
    local backup_path
    if fresh.counts.overwrite>0 then
        assert(type(backup_manifest)=='function','manifest backup required')
        backup_path=backup_manifest(fresh.old_manifest)
        assert(backup_path,'manifest backup failed')
    end
    local ok,reason=Publish.apply(fresh.files,function(path,bytes)
        return self.fs:write(path,bytes)
    end,function(path) return self.fs:mkdir(path) end,allow_overwrite)
    if not ok then return false,reason,backup_path end
    return true,nil,backup_path
end
return M
