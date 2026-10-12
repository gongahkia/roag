-- Two-generation project manifest store; image sources are immutable by path.
local Project=require('src.studio_project')
local Document=require('src.studio_document')
local PNG=require('src.studio_png')
local Outline=require('src.studio_outline')
local S={}
local function checksum(bytes)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format('%08x',b*65536+a)
end
local function pack(seq,payload)
    return table.concat({'ROEG-STUDIO-RECORD/1',seq,#payload,checksum(payload)},'\n')..'\n'..payload
end
local function unpack(bytes)
    assert(type(bytes)=='string' and #bytes<=1024*1024+100,'invalid record size')
    local h={};local pos=1
    for i=1,4 do
        local e=assert(bytes:find('\n',pos,true),'truncated record')
        h[i]=bytes:sub(pos,e-1);pos=e+1
    end
    assert(h[1]=='ROEG-STUDIO-RECORD/1','incompatible record format')
    local seq,size=tonumber(h[2]),tonumber(h[3])
    assert(seq and seq%1==0 and seq>=1 and size and size%1==0 and
        size>=0 and size<=1024*1024 and #bytes-pos+1==size,'invalid record header')
    local payload=bytes:sub(pos)
    assert(checksum(payload)==h[4],'record checksum mismatch')
    return {seq=seq,payload=payload}
end
function S.new(io)
    assert(type(io)=='table' and io.read and io.write and io.mkdir and io.list)
    return setmetatable({io=io},{__index=S})
end
local function prefix(id)
    assert(Project.valid_project_id(id),'invalid project ID')
    return 'studio/projects/'..id..'/'
end
function S:list()
    local ok,items=pcall(self.io.list,self.io,'studio/projects')
    if not ok or type(items)~='table' then return {} end
    local result={}
    for _,id in ipairs(items) do
        if Project.valid_project_id(id) then result[#result+1]=id end
    end
    table.sort(result)
    return result
end
function S:open(id,decode)
    local root=prefix(id);local records,invalid={},0
    for n,suffix in ipairs({'a','b'}) do
        local ok,bytes=pcall(self.io.read,self.io,root..'manifest-'..suffix..'.txt')
        if ok and bytes then
            local valid,record=pcall(unpack,bytes)
            if valid then
                record.slot=n;records[#records+1]=record
            else invalid=invalid+1 end
        elseif not ok then invalid=invalid+1 end
    end
    table.sort(records,function(a,b)
        if a.seq~=b.seq then return a.seq>b.seq end
        return a.slot>b.slot
    end)
    local newest=records[1]
    if not newest then return nil,invalid>0 and 'corrupt manifest' or 'missing project' end
    local parsed,project=pcall(Project.decode,newest.payload)
    if not parsed then return nil,'invalid project manifest: '..tostring(project) end
    if project.id~=id then return nil,'project ID mismatch' end
    local ok,err=pcall(Project.validate,project,function(path)
        return self.io:read(root..path)
    end,decode)
    if not ok then
        -- A source image is written before its new manifest. If storage later
        -- loses that source, the prior generation remains a usable project.
        for i=2,#records do
            local older=records[i]
            local decoded,candidate=pcall(Project.decode,older.payload)
            if decoded and candidate.id==id then
                local intact=pcall(Project.validate,candidate,function(path)
                    return self.io:read(root..path)
                end,decode)
                if intact then
                    return candidate,'recovered prior project; newest has broken source: '..
                        tostring(err),older
                end
            end
        end
        return nil,'broken project: '..tostring(err)
    end
    return project,invalid>0 and 'recovered previous manifest' or 'opened',newest
end
function S:save(project,decode)
    local root=prefix(project.id)
    local ok,err=pcall(Project.validate,project,function(path)
        return self.io:read(root..path)
    end,decode)
    if not ok then return false,tostring(err) end
    local current,reason,record=self:open(project.id,decode)
    if not current and reason~='missing project' then return false,reason end
    local seq=record and record.seq+1 or 1
    local suffix=record and (record.slot==1 and 'b' or 'a') or 'a'
    local payload=Project.encode(project)
    local bytes=pack(seq,payload)
    local name=root..'manifest-'..suffix..'.txt'
    local made,merr=pcall(self.io.mkdir,self.io,root)
    if not made then return false,tostring(merr) end
    local wrote,werr=pcall(self.io.write,self.io,name,bytes)
    if not wrote or not werr then return false,tostring(werr or 'write failed') end
    local readok,readback=pcall(self.io.read,self.io,name)
    if not readok or readback~=bytes then return false,'manifest read-back failed' end
    local valid,again=pcall(unpack,readback)
    if not valid or again.seq~=seq then return false,'manifest verification failed' end
    local decoded,reopened=pcall(Project.decode,again.payload)
    if not decoded or reopened.id~=project.id then return false,'manifest decode failed' end
    return true,seq
end
function S:import(project,asset,bytes,decode)
    if asset.document and asset.document~='' then
        return false,'cannot replace an editable document by importing over it'
    end
    local ok,w,h=pcall(Project.png_dimensions,bytes)
    if not ok then return false,tostring(w) end
    if decode then
        local decoded,err=pcall(decode,bytes,'import.png',w,h)
        if not decoded then return false,tostring(err) end
    end
    local seen,existing_index={},nil
    for i,a in ipairs(project.assets) do
        seen[a.id]=true
        if a==asset and a.source=='' then existing_index=i end
    end
    if not Project.valid_id(asset.id) or (seen[asset.id] and not existing_index) then
        return false,'invalid or duplicate asset ID'
    end
    local index=1;local root=prefix(project.id)
    local path
    local exists
    repeat
        path=string.format('sources/image-%04d.png',index)
        index=index+1
        local readok,bytes=pcall(self.io.read,self.io,root..path)
        if not readok then return false,'source lookup failed: '..tostring(bytes) end
        exists=bytes~=nil
    until not exists
    local copy={}
    for k,v in pairs(asset) do copy[k]=v end
    copy.source=path;copy.format='png';copy.width=w;copy.height=h
    copy.transparent=true
    local candidate={schema=project.schema,id=project.id,name=project.name,assets={}}
    for i,a in ipairs(project.assets) do
        candidate.assets[#candidate.assets+1]=i==existing_index and copy or a
    end
    if not existing_index then candidate.assets[#candidate.assets+1]=copy end
    local valid,err=pcall(Project.validate,candidate)
    if not valid then return false,tostring(err) end
    local made,merr=pcall(self.io.mkdir,self.io,root..'sources')
    if not made then return false,tostring(merr) end
    local wrote,werr=pcall(self.io.write,self.io,root..path,bytes)
    local readok,readback=pcall(self.io.read,self.io,root..path)
    if not wrote or not werr or not readok or readback~=bytes then
        return false,'image write/read-back failed: '..tostring(werr)
    end
    if existing_index then
        for k,v in pairs(copy) do asset[k]=v end
        copy=asset
    else project.assets[#project.assets+1]=copy end
    return true,copy
end
function S:document(project,asset)
    assert(asset.document~='','asset has no editable document')
    local bytes=assert(self.io:read(prefix(project.id)..asset.document),'missing editable document')
    local doc=Document.decode(bytes)
    assert(doc.id==asset.id and doc.width==asset.width and doc.height==asset.height,
        'editable document does not match asset')
    return doc
end
function S:_save_document(project,asset,doc,decode,outline_color)
    local valid,err=pcall(Document.validate,doc)
    if not valid then return false,tostring(err) end
    if doc.id~=asset.id then return false,'document asset ID mismatch' end
    local found=false
    for _,a in ipairs(project.assets) do if a==asset then found=true end end
    if not found then return false,'asset is not in project' end
    local root=prefix(project.id)
    local encoded=Document.encode(doc)
    outline_color=outline_color or asset.outline_color or ''
    local width,height,raw=doc.width,doc.height,Document.flatten(doc)
    if outline_color~='' then
        width,height,raw=Outline.render(raw,width,height,outline_color)
    end
    local png=PNG.encode(width,height,raw,outline_color~='' and 2048 or nil)
    if decode then
        local ok,reason=pcall(decode,png,'painted.png',width,height)
        if not ok then return false,'derived PNG decode failed: '..tostring(reason) end
    end
    local n=1;local document,derived
    repeat
        document=string.format('documents/layers-%04d.rgba',n)
        derived=string.format('derived/painted-%04d.png',n)
        n=n+1
    until self.io:read(root..document)==nil and self.io:read(root..derived)==nil
    local ok,reason=pcall(function()
        self.io:mkdir(root..'documents');self.io:mkdir(root..'derived')
        self.io:write(root..document,encoded)
        assert(self.io:read(root..document)==encoded,'document read-back failed')
        self.io:write(root..derived,png)
        assert(self.io:read(root..derived)==png,'derived PNG read-back failed')
    end)
    if not ok then return false,tostring(reason) end
    local copy={};for k,v in pairs(asset) do copy[k]=v end
    copy.document=document;copy.derived=derived;copy.width=doc.width;copy.height=doc.height
    copy.format='png';copy.transparent=true;copy.outline_color=outline_color
    local candidate={schema=project.schema,id=project.id,name=project.name,assets={}}
    for i,a in ipairs(project.assets) do candidate.assets[i]=a==asset and copy or a end
    local saved,seq=self:save(candidate,decode)
    if not saved then return false,seq end
    for k,v in pairs(copy) do asset[k]=v end
    return true,seq
end
function S:save_document(project,asset,doc,decode,outline_color)
    local ok,saved,reason=pcall(self._save_document,self,project,asset,doc,decode,outline_color)
    if not ok then return false,tostring(saved) end
    return saved,reason
end
return S
