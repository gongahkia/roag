-- Deterministic static-image packing. This module has no LÖVE dependency.
local Project=require('src.studio_project')
local Codec=require('src.persistence')
local bit=require('bit')
local A={schema='roeg-studio-atlas/1',max_pages=32,
    slots={'assets/studio/atlas-a.pack','assets/studio/atlas-b.pack'}}
local crc_table={}
for i=0,255 do
    local c=i
    for _=1,8 do
        c=bit.band(c,1)==1 and bit.bxor(bit.rshift(c,1),0xedb88320)
            or bit.rshift(c,1)
    end
    crc_table[i]=c
end
function A.checksum(bytes)
    local a,b,crc=1,0,0xffffffff
    for i=1,#bytes do
        local byte=bytes:byte(i)
        a=(a+byte)%65521;b=(b+a)%65521
        crc=bit.bxor(crc_table[bit.band(bit.bxor(crc,byte),255)],
            bit.rshift(crc,8))
    end
    return string.format('%08x%08x',b*65536+a,
        bit.bxor(crc,0xffffffff)%4294967296)
end
local function hash(value)
    return type(value)=='string' and #value==16 and
        value:match('^%x+$')~=nil
end
local function integer(n,lo,hi)
    return type(n)=='number' and n%1==0 and n>=lo and n<=hi
end
local function power2(n)
    local value=8
    while value<n do value=value*2 end
    return value
end
local function exact_power2(n)
    return power2(n)==n
end
function A.inputs(project,read)
    Project.validate(project)
    local items,excluded,signature={},{},{}
    local ordered={}
    for _,asset in ipairs(project.assets) do ordered[#ordered+1]=asset end
    table.sort(ordered,function(a,b)return a.id<b.id end)
    for _,asset in ipairs(ordered) do
        if asset.source~='' then
            local bytes=assert(read('assets/studio/'..asset.source),
                'missing published image: '..asset.id)
            local w,h=Project.png_dimensions(bytes)
            assert(w==asset.width and h==asset.height,
                'published image dimensions changed: '..asset.id)
            local hash=A.checksum(bytes)
            signature[#signature+1]=table.concat({asset.id,asset.source,#bytes,hash},':')
            if asset.kind=='glyph' or asset.kind=='bitmap_font' then
                excluded[asset.id]='bitmap font uses standalone ImageData for glyph fallback'
            elseif asset.kind=='shader' then
                excluded[asset.id]='shader references are not static atlas sprites'
            elseif (asset.kind=='background' or asset.kind=='ui_background') and
                (w>512 or h>512) then
                excluded[asset.id]='large background kept standalone'
            else
                local group=asset.atlas_group
                if group=='' then
                    if asset.kind=='actor' or asset.kind=='enemy' then group='actors'
                    elseif asset.kind=='tile' then group='world'
                    elseif asset.kind=='effect' or asset.kind=='effect_sprite' then
                        group='effects'
                    elseif asset.kind=='background' or asset.kind=='ui_background' then
                        group='backgrounds'
                    else group='ui' end
                end
                assert(group:match('^[a-z0-9_-]+$'),
                    'invalid atlas group: '..asset.id)
                items[#items+1]={id=asset.id,w=w,h=h,filter=asset.filter,
                    group=group,checksum=hash,source=asset.source,bytes=bytes}
            end
        end
    end
    return items,excluded,A.checksum(Project.encode(project)..'\n'..
        table.concat(signature,'\n'))
end
function A.plan(project,read,max_size,padding)
    max_size=max_size or 2048;padding=padding or 2
    assert(integer(max_size,32,2048) and exact_power2(max_size) and
        integer(padding,1,8),
        'invalid atlas size or padding')
    local items,excluded,fingerprint=A.inputs(project,read)
    local groups={}
    for _,item in ipairs(items) do
        assert(item.w+padding*2<=max_size and item.h+padding*2<=max_size,
            'asset exceeds atlas page: '..item.id)
        local key=item.filter..':'..item.group
        groups[key]=groups[key] or {}
        groups[key][#groups[key]+1]=item
    end
    local pages,mappings={},{}
    local keys={};for key in pairs(groups) do keys[#keys+1]=key end
    table.sort(keys)
    for _,key in ipairs(keys) do
        local filter,group=key:match('^([^:]+):(.+)$')
        table.sort(groups[key],function(a,b)
            if a.h~=b.h then return a.h>b.h end
            if a.w~=b.w then return a.w>b.w end
            return a.id<b.id
        end)
        local page,cursor_x,cursor_y,row_h
        for _,item in ipairs(groups[key]) do
            local outer_w,outer_h=item.w+padding*2,item.h+padding*2
            if not page or cursor_y+outer_h>max_size then
                page={filter=filter,group=group,used_w=0,used_h=0,entries={}}
                pages[#pages+1]=page
                assert(#pages<=A.max_pages,'atlas page count limit')
                cursor_x,cursor_y,row_h=0,0,0
            end
            if cursor_x+outer_w>max_size then
                cursor_x=0;cursor_y=cursor_y+row_h;row_h=0
                if cursor_y+outer_h>max_size then
                    page={filter=filter,group=group,used_w=0,used_h=0,entries={}}
                    pages[#pages+1]=page
                    assert(#pages<=A.max_pages,'atlas page count limit')
                    cursor_y=0
                end
            end
            local entry={id=item.id,x=cursor_x+padding,y=cursor_y+padding,
                w=item.w,h=item.h,padding=padding,checksum=item.checksum,
                source=item.source,filter=filter,group=group,page=#pages}
            mappings[item.id]=entry
            page.entries[#page.entries+1]=entry
            page.used_w=math.max(page.used_w,cursor_x+outer_w)
            page.used_h=math.max(page.used_h,cursor_y+outer_h)
            cursor_x=cursor_x+outer_w;row_h=math.max(row_h,outer_h)
        end
    end
    for _,page in ipairs(pages) do
        page.width=power2(page.used_w);page.height=power2(page.used_h)
    end
    return {schema=A.schema,project_id=project.id,fingerprint=fingerprint,
        max_size=max_size,padding=padding,pages=pages,assets=mappings,
        excluded=excluded},items
end
function A.validate(pack)
    assert(type(pack)=='table' and pack.schema==A.schema,
        'incompatible atlas schema')
    assert(Project.valid_project_id(pack.project_id) and
        hash(pack.fingerprint),
        'invalid atlas identity')
    assert(integer(pack.max_size,32,2048) and exact_power2(pack.max_size) and
        integer(pack.padding,1,8),
        'invalid atlas configuration')
    assert(type(pack.pages)=='table' and #pack.pages<=A.max_pages and
        type(pack.assets)=='table' and type(pack.excluded)=='table',
        'invalid atlas tables')
    local seen={}
    for index,page in ipairs(pack.pages) do
        assert(page.filter=='nearest' or page.filter=='linear','invalid atlas filter')
        assert(type(page.group)=='string' and
            page.group:match('^[a-z0-9_-]+$'),'invalid atlas group')
        assert(integer(page.width,8,pack.max_size) and
            integer(page.height,8,pack.max_size) and
            type(page.entries)=='table','invalid atlas page')
        assert(hash(page.checksum) and
            page.filename=='assets/studio/atlas/page-'..page.checksum..'.png',
            'invalid atlas page filename')
        for _,entry in ipairs(page.entries) do
            assert(Project.valid_id(entry.id) and not seen[entry.id],
                'duplicate or invalid atlas asset ID')
            seen[entry.id]=true
            assert(pack.assets[entry.id] and
                Codec.encode(pack.assets[entry.id])==Codec.encode(entry),
                'atlas mapping differs from page')
            assert(entry.page==index and entry.filter==page.filter and
                entry.group==page.group and
                integer(entry.x,pack.padding,page.width) and
                integer(entry.y,pack.padding,page.height) and
                integer(entry.w,1,1024) and integer(entry.h,1,1024) and
                entry.padding==pack.padding and
                entry.x+entry.w+pack.padding<=page.width and
                entry.y+entry.h+pack.padding<=page.height and
                Project.valid_path(entry.source) and
                hash(entry.checksum),
                'atlas rectangle out of bounds')
        end
        for i=1,#page.entries do
            local a=page.entries[i]
            for j=i+1,#page.entries do
                local b=page.entries[j]
                assert(a.x+a.w+pack.padding<=b.x-pack.padding or
                    b.x+b.w+pack.padding<=a.x-pack.padding or
                    a.y+a.h+pack.padding<=b.y-pack.padding or
                    b.y+b.h+pack.padding<=a.y-pack.padding,
                    'overlapping atlas rectangles')
            end
        end
    end
    for id in pairs(pack.assets) do assert(seen[id],'orphan atlas mapping') end
    for id,reason in pairs(pack.excluded) do
        assert(Project.valid_id(id) and not seen[id] and type(reason)=='string' and
            #reason>0 and #reason<=120,'invalid atlas exclusion')
    end
    return true
end
function A.matches_inputs(pack,project,read)
    A.validate(pack)
    local expected=A.plan(project,read,pack.max_size,pack.padding)
    if pack.project_id~=expected.project_id or
        pack.fingerprint~=expected.fingerprint or
        Codec.encode(pack.assets)~=Codec.encode(expected.assets) or
        Codec.encode(pack.excluded)~=Codec.encode(expected.excluded) or
        #pack.pages~=#expected.pages then
        return false,'atlas does not cover the active published visuals'
    end
    for index,page in ipairs(pack.pages) do
        local candidate=expected.pages[index]
        if page.width~=candidate.width or page.height~=candidate.height or
            page.filter~=candidate.filter or page.group~=candidate.group or
            Codec.encode(page.entries)~=Codec.encode(candidate.entries) then
            return false,'atlas layout differs from deterministic source plan'
        end
    end
    return true
end
function A.encode(pack)
    A.validate(pack)
    local bytes=Codec.encode(pack)
    assert(#bytes<=1024*1024,'atlas metadata too large')
    return bytes
end
function A.decode(bytes)
    assert(type(bytes)=='string' and #bytes<=1024*1024,'invalid atlas metadata size')
    local pack=Codec.decode(bytes)
    A.validate(pack)
    return pack
end
return A
