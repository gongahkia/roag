-- Validated, explicit loose-source export. No filesystem access in this module.
local Project=require('src.studio_project')
local M={}
local function checksum(bytes)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format('%08x',b*65536+a)
end
function M.plan(project,source_read,dest_read)
    assert(type(source_read)=='function' and type(dest_read)=='function')
    Project.validate(project,source_read)
    local files={}
    local published={schema=project.schema,id=project.id,name=project.name,assets={}}
    local ordinal=0
    local ordered={}
    for _,asset in ipairs(project.assets) do ordered[#ordered+1]=asset end
    table.sort(ordered,function(a,b)return a.id<b.id end)
    for _,asset in ipairs(ordered) do
        local copy={}
        for k,v in pairs(asset) do copy[k]=v end
        local publish_source=asset.derived~='' and asset.derived or asset.source
        if publish_source~='' then
            ordinal=ordinal+1
            local bytes=assert(source_read(publish_source))
            copy.source=string.format('sources/%s-%04d-%s.png',
                project.id,ordinal,checksum(bytes))
            if asset.derived~='' then
                copy.width,copy.height=Project.output_dimensions(asset)
            end
            copy.document='';copy.derived=''
            copy.outline_color=''
            files[#files+1]={path='assets/studio/'..copy.source,bytes=bytes}
            published.assets[#published.assets+1]=copy
        elseif asset.binding~='' then
            error('bound draft has no image: '..asset.id)
        end
    end
    table.sort(files,function(a,b) return a.path<b.path end)
    files[#files+1]={path='assets/studio/manifest.txt',bytes=Project.encode(published)}
    for _,file in ipairs(files) do
        local existing=dest_read(file.path)
        file.status=existing==file.bytes and 'unchanged' or
            existing and 'overwrite' or 'create'
    end
    return files
end
function M.apply(plan,write,mkdir,overwrite)
    assert(type(write)=='function' and type(mkdir)=='function')
    for _,file in ipairs(plan) do
        assert(type(file.path)=='string' and file.path:match('^assets/studio/') and
            Project.valid_path(file.path),'unsafe publish path')
        if file.status=='overwrite' and not overwrite then
            return false,'existing published file needs --overwrite: '..file.path
        end
    end
    for _,file in ipairs(plan) do
        if file.status~='unchanged' then
            local dir=file.path:match('^(.*)/[^/]+$')
            local ok,err=pcall(mkdir,dir)
            if not ok then return false,tostring(err) end
            ok,err=pcall(write,file.path,file.bytes)
            if not ok or not err then return false,tostring(err or 'publish write failed') end
        end
    end
    return true
end
return M
