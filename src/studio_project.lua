-- Portable, inert, deterministic Studio manifest. Tabs separate escaped fields.
local M={schema='roeg-studio-project/1',max_image_bytes=8*1024*1024}
local bit=require('bit')
local Document=require('src.studio_document')
local crc_table={}
for i=0,255 do
    local c=i
    for _=1,8 do
        c=bit.band(c,1)==1 and bit.bxor(bit.rshift(c,1),0xedb88320)
            or bit.rshift(c,1)
    end
    crc_table[i]=c
end
local kinds={actor=true,enemy=true,tile=true,effect=true,ui=true,background=true,
    glyph=true,shader=true,ui_icon=true,ui_panel=true,ui_background=true,
    bitmap_font=true,effect_sprite=true}
M.bindings={['core:actor/adventurer']=true}
local function check(ok,message) if not ok then error(message,2) end end
local function string_field(s,label,max)
    check(type(s)=='string' and #s<=max and not s:find('%z'),label..' must be text')
end
function M.valid_id(id)
    return type(id)=='string' and #id<=100 and
        id:match('^[a-z][a-z0-9_-]*:[a-z][a-z0-9_/-]*$')~=nil and
        id:sub(-1)~='/' and not id:find('//',1,true) and not id:find('/%.')
end
function M.valid_project_id(id)
    return type(id)=='string' and #id<=48 and id:match('^[a-z][a-z0-9_-]*$')~=nil
end
function M.valid_path(path)
    if type(path)~='string' or #path==0 or #path>180 or path:find('[\\:%z]') or
        path:sub(1,1)=='/' or path:find('//',1,true) then return false end
    for segment in path:gmatch('[^/]+') do
        if segment=='.' or segment=='..' or not segment:match('^[%w_.-]+$') then return false end
    end
    return true
end
function M.new(id,name)
    check(M.valid_project_id(id),'invalid project ID')
    string_field(name,'project name',80)
    check(#name>0,'project name required')
    return {schema=M.schema,id=id,name=name,assets={}}
end
function M.validate(project,read,decode)
    check(type(project)=='table' and project.schema==M.schema,'incompatible project schema')
    check(M.valid_project_id(project.id),'invalid project ID')
    string_field(project.name,'project name',80)
    check(#project.name>0,'project name required')
    check(type(project.assets)=='table' and #project.assets<=256,'invalid asset list')
    local ids,paths,bindings={},{},{}
    local total_source_bytes=0
    local total_project_bytes=0
    for _,a in ipairs(project.assets) do
        check(type(a)=='table' and M.valid_id(a.id),'invalid asset ID')
        check(not ids[a.id],'duplicate asset ID: '..a.id);ids[a.id]=true
        string_field(a.name,'asset name',80)
        check(#a.name>0,'asset name required')
        check(kinds[a.kind],'unsupported asset kind')
        check(a.binding=='' or M.bindings[a.binding],'unknown game binding')
        if a.binding~='' then
            check(not bindings[a.binding],'duplicate game binding: '..a.binding)
            bindings[a.binding]=true
        end
        check(a.filter=='nearest' or a.filter=='linear','invalid sampling filter')
        string_field(a.description or '','description',300)
        string_field(a.atlas_group or '','atlas group',60)
        string_field(a.frame_hint or '','frame hint',120)
        string_field(a.animation_hint or '','animation hint',120)
        check(type(a.tags)=='table' and #a.tags<=20,'invalid tags')
        for _,tag in ipairs(a.tags) do
            check(type(tag)=='string' and tag:match('^[a-z0-9_-]+$') and #tag<=32,'invalid tag')
        end
        a.document=a.document or '';a.derived=a.derived or ''
        a.outline_color=a.outline_color or ''
        check(a.outline_color=='' or (type(a.outline_color)=='string' and
            a.outline_color:match('^%x%x%x%x%x%x%x%x$')),'invalid outline color')
        check(a.outline_color=='' or a.document~='',
            'outline requires an editable document')
        check(a.source=='' or M.valid_path(a.source),'unsafe source path')
        check(a.document=='' or (M.valid_path(a.document) and a.document:match('%.rgba$')),'unsafe document path')
        check(a.derived=='' or (M.valid_path(a.derived) and a.derived:match('%.png$')),'unsafe derived path')
        check((a.document=='')==(a.derived==''),'document and derived image must be paired')
        if a.source=='' then
            if a.document=='' then check(a.format=='' and a.width==0 and a.height==0,'draft has image metadata') end
        else
            check(a.format=='png' and a.source:lower():match('%.png$'),'unsupported source format')
            check(not paths[a.source],'duplicate source path');paths[a.source]=true
        end
        if a.source~='' or a.document~='' then
            check(a.format=='png','unsupported source format')
            check(type(a.width)=='number' and type(a.height)=='number' and
                a.width%1==0 and a.height%1==0 and a.width>=1 and a.height>=1 and
                a.width<=1024 and a.height<=1024 and a.width*a.height<=1048576,
                'invalid image dimensions')
            check(type(a.transparent)=='boolean','invalid alpha intent')
            if read and a.source~='' then
                local bytes=read(a.source)
                check(type(bytes)=='string','missing source image: '..a.source)
                total_source_bytes=total_source_bytes+#bytes
                check(total_source_bytes<=64*1024*1024,'project source size limit')
                total_project_bytes=total_project_bytes+#bytes
                local w,h=M.png_dimensions(bytes)
                check(w==a.width and h==a.height,'source dimensions changed: '..a.source)
                if decode then decode(bytes,a.source,w,h) end
            end
        end
        if a.document~='' then
            check(not paths[a.document] and not paths[a.derived],'duplicate document path')
            paths[a.document]=true;paths[a.derived]=true
            check(a.width<=Document.max_dimension and a.height<=Document.max_dimension,'editable canvas too large')
            if read then
                local bytes=read(a.document)
                check(type(bytes)=='string','missing editable document: '..a.document)
                total_project_bytes=total_project_bytes+#bytes
                local doc=Document.decode(bytes)
                check(doc.id==a.id and doc.width==a.width and doc.height==a.height,'document identity or dimensions mismatch')
                local png=read(a.derived)
                check(type(png)=='string','missing derived PNG: '..a.derived)
                total_project_bytes=total_project_bytes+#png
                local w,h=M.png_dimensions(png)
                local dw,dh=M.output_dimensions(a)
                check(w==dw and h==dh,'derived dimensions mismatch')
                if decode then decode(png,a.derived,w,h) end
            end
        end
    end
    if read then check(total_project_bytes<=128*1024*1024,'project referenced-data size limit') end
    return true
end
function M.output_dimensions(asset)
    if asset.derived~='' and asset.outline_color~='' then
        return asset.width*2,asset.height*2
    end
    return asset.width,asset.height
end
function M.png_dimensions(bytes)
    check(type(bytes)=='string' and #bytes>=45 and #bytes<=M.max_image_bytes,
        'invalid or oversized PNG')
    check(bytes:sub(1,8)=='\137PNG\r\n\26\n' and bytes:sub(13,16)=='IHDR',
        'not a PNG image')
    local function u32(i)
        local a,b,c,d=bytes:byte(i,i+3)
        return ((a*256+b)*256+c)*256+d
    end
    check(u32(9)==13,'invalid PNG header')
    local w,h=u32(17),u32(21)
    check(w>=1 and h>=1 and w<=1024 and h<=1024 and w*h<=1048576,
        'PNG dimensions out of bounds')
    local pos,has_idat,ended=9,false,false
    while pos<=#bytes do
        check(pos+11<=#bytes,'truncated PNG chunk')
        local length=u32(pos)
        check(length<=M.max_image_bytes and pos+length+11<=#bytes,'invalid PNG chunk size')
        local kind=bytes:sub(pos+4,pos+7)
        check(kind:match('^[A-Za-z][A-Za-z][A-Za-z][A-Za-z]$'),'invalid PNG chunk type')
        local crc=0xffffffff
        for i=pos+4,pos+7+length do
            crc=bit.bxor(crc_table[bit.band(bit.bxor(crc,bytes:byte(i)),255)],
                bit.rshift(crc,8))
        end
        check(bit.bxor(crc,0xffffffff)%4294967296==u32(pos+8+length),
            'PNG checksum mismatch')
        if pos==9 then check(kind=='IHDR' and length==13,'invalid PNG header') end
        if kind=='IDAT' then has_idat=true end
        if kind=='IEND' then
            check(length==0 and has_idat and pos+length+12==#bytes+1,
                'invalid PNG ending')
            ended=true;break
        end
        pos=pos+length+12
    end
    check(ended,'truncated PNG')
    return w,h
end
local function escape(s)
    return tostring(s):gsub('\\','\\\\'):gsub('\t','\\t'):gsub('\r','\\r'):gsub('\n','\\n')
end
local function unescape(s)
    check(not s:find('\\[^\\trn]'),'invalid manifest escape')
    return (s:gsub('\\([\\trn])',function(c)
        return ({['\\']='\\',t='\t',r='\r',n='\n'})[c]
    end))
end
local function fields(line)
    local result={}
    for s in (line..'\t'):gmatch('(.-)\t') do result[#result+1]=unescape(s) end
    return result
end
function M.encode(project)
    M.validate(project)
    local out={'ROEG-STUDIO-MANIFEST/1','project\t'..escape(project.id)..'\t'..escape(project.name)}
    local assets={}
    for _,a in ipairs(project.assets) do assets[#assets+1]=a end
    table.sort(assets,function(a,b) return a.id<b.id end)
    for _,a in ipairs(assets) do
        local tags={}
        for _,t in ipairs(a.tags) do tags[#tags+1]=t end
        table.sort(tags)
        local f={'asset',a.id,a.name,a.kind,a.source,a.format,tostring(a.width),
            tostring(a.height),tostring(a.transparent),a.filter,a.binding,
            a.description or '',table.concat(tags,','),a.atlas_group or '',
            a.frame_hint or '',a.animation_hint or '',a.document or '',a.derived or ''}
        if a.outline_color~='' then f[#f+1]=a.outline_color end
        for i,v in ipairs(f) do f[i]=escape(v) end
        out[#out+1]=table.concat(f,'\t')
    end
    return table.concat(out,'\n')..'\n'
end
function M.decode(bytes)
    check(type(bytes)=='string' and #bytes<=1024*1024,'manifest too large')
    local lines={}
    for line in (bytes..'\n'):gmatch('(.-)\n') do
        if #line>0 then lines[#lines+1]=line end
    end
    check(lines[1]=='ROEG-STUDIO-MANIFEST/1','incompatible manifest format')
    local p=fields(lines[2] or '')
    check(#p==3 and p[1]=='project','invalid project record')
    local project=M.new(p[2],p[3])
    for i=3,#lines do
        local f=fields(lines[i])
        check((#f==16 or #f==18 or #f==19) and f[1]=='asset','invalid asset record')
        local tags={}
        if f[13]~='' then for t in f[13]:gmatch('[^,]+') do tags[#tags+1]=t end end
        project.assets[#project.assets+1]={id=f[2],name=f[3],kind=f[4],source=f[5],
            format=f[6],width=tonumber(f[7]),height=tonumber(f[8]),
            transparent=f[9]=='true',filter=f[10],binding=f[11],
            description=f[12],tags=tags,atlas_group=f[14],
            frame_hint=f[15],animation_hint=f[16],document=f[17] or '',
            derived=f[18] or '',outline_color=f[19] or ''}
        check(f[9]=='true' or f[9]=='false','invalid alpha intent')
    end
    M.validate(project)
    return project
end
function M.asset(id,name,kind)
    return {id=id,name=name,kind=kind,source='',format='',width=0,height=0,
        transparent=true,filter='nearest',binding='',description='',tags={},atlas_group='',
        frame_hint='',animation_hint='',document='',derived='',outline_color=''}
end
return M
