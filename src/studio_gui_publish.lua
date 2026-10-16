-- Loose-image publisher for Studio's explicit preview/confirm flow.
-- Source artwork stays in LÖVE's save directory; output is the launched checkout.
local Project=require('src.studio_project')
local Publish=require('src.studio_publish')
local ffi=require('ffi')
local M={}
local next_temp=0

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
        -- Build beside the destination, then replace the finished file. The
        -- manifest is last in Publish.plan, so a failed image write cannot
        -- activate an incomplete new pack.
        local destination=full(path)
        local temporary
        repeat
            next_temp=next_temp+1
            temporary=destination..('.roag-publish-%d-%d.tmp'):format(
                os.time(),next_temp)
            local existing=io.open(temporary,'rb')
            if existing then existing:close() else break end
        until false
        local file=assert(io.open(temporary,'wb'))
        assert(file:write(bytes))
        assert(file:close())
        local staged=assert(io.open(temporary,'rb'))
        local staged_bytes=assert(staged:read('*a'))
        assert(staged:close())
        assert(staged_bytes==bytes,'publish staging read-back mismatch: '..path)
        local renamed,reason=os.rename(temporary,destination)
        if not renamed then os.remove(temporary);error(reason or 'publish rename failed') end
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
            result.blocked='Published art belongs to '..published.id..
                '; use Pixels > Use in Game for one slot, or open its project for a full publish.'
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

-- Publish one selected game binding without taking over another project's
-- manifest. This keeps every unrelated published asset and source PNG intact.
function M:plan_asset(project,asset_id,source_read)
    Project.validate(project,source_read)
    local selected
    for _,asset in ipairs(project.assets) do
        if asset.id==asset_id then selected=asset;break end
    end
    assert(selected,'selected artwork is missing')
    assert(selected.binding~='','choose a game artwork slot first')
    assert(selected.source~='' or selected.derived~='',
        'selected artwork has no saved image')

    local copy={}
    for key,value in pairs(selected) do copy[key]=value end
    copy.reference_id='' -- editor-only reference is already flattened
    local single=Project.new(project.id,project.name)
    single.assets[1]=copy
    local files=Publish.plan(single,source_read,function(path)
        return self.fs:read(path)
    end)
    local replacement=Project.decode(files[#files].bytes).assets[1]
    local old_manifest=self.fs:read('assets/studio/manifest.txt')
    local result={files=files,old_manifest=old_manifest,asset_id=asset_id,
        binding=selected.binding,counts={create=0,overwrite=0,unchanged=0}}
    local published
    if old_manifest then
        local ok,value=pcall(Project.decode,old_manifest)
        if not ok then
            result.blocked='Existing published manifest is invalid; inspect it before replacing.'
            return result
        end
        published=value
        local valid,reason=pcall(Project.validate,published,function(path)
            return self.fs:read('assets/studio/'..path)
        end)
        if not valid then
            result.blocked='Existing published art is incomplete: '..tostring(reason)
            return result
        end
    end
    local merged=Project.new(published and published.id or project.id,
        published and published.name or project.name)
    result.owner=merged.id
    for _,asset in ipairs(published and published.assets or {}) do
        if asset.binding==selected.binding then
            result.replaced_asset_id=asset.id
        elseif asset.id==replacement.id then
            result.blocked='Asset ID already belongs to another game slot: '..asset.id
            return result
        else
            merged.assets[#merged.assets+1]=asset
        end
    end
    merged.assets[#merged.assets+1]=replacement
    Project.validate(merged)
    files[#files].bytes=Project.encode(merged)
    for _,file in ipairs(files) do
        local existing=self.fs:read(file.path)
        file.status=existing==file.bytes and 'unchanged' or
            existing and 'overwrite' or 'create'
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

function M:apply_asset(project,asset_id,source_read,preview,backup_manifest)
    local fresh=self:plan_asset(project,asset_id,source_read)
    if not same_plan(preview,fresh) then
        return false,'Artwork or published destination changed; review again.'
    end
    if fresh.blocked then return false,fresh.blocked end
    local backup_path
    if fresh.counts.overwrite>0 then
        assert(type(backup_manifest)=='function','manifest backup required')
        backup_path=backup_manifest(fresh.old_manifest)
        assert(backup_path,'manifest backup failed')
    end
    local ok,reason=Publish.apply(fresh.files,function(path,bytes)
        return self.fs:write(path,bytes)
    end,function(path) return self.fs:mkdir(path) end,true)
    if not ok then return false,reason,backup_path end
    return true,nil,backup_path
end
return M
