-- E01 editable RGBA8 layers. Bottom-to-top ordering; coordinates are zero-based.
local D={max_dimension=512,max_layers=16,max_bytes=16*1024*1024}
local function valid_id(id) return require('src.studio_project').valid_id(id) end
local function check(v,m) if not v then error(m,2) end end
local function u32(n) return string.char(math.floor(n/16777216)%256,math.floor(n/65536)%256,math.floor(n/256)%256,n%256) end
local function number(s,p) local a,b,c,d=s:byte(p,p+3);check(d,'truncated document');return ((a*256+b)*256+c)*256+d end
local function rgba(c) check(type(c)=='table' and #c==4,'invalid RGBA');for i=1,4 do check(type(c[i])=='number' and c[i]%1==0 and c[i]>=0 and c[i]<=255,'invalid channel') end end
local function blank(w,h) local row=string.rep('\0',w*4);local rows={};for y=1,h do rows[y]=row end;return rows end
local function valid_size(w,h) return type(w)=='number' and type(h)=='number' and w%1==0 and h%1==0 and w>=1 and h>=1 and w<=D.max_dimension and h<=D.max_dimension end
function D.new(id,w,h)
    check(valid_id(id),'invalid document asset ID');check(valid_size(w,h),'invalid canvas dimensions')
    return {id=id,width=w,height=h,next_layer=2,selected='layer_1',layers={{id='layer_1',name='Layer 1',visible=true,locked=false,rows=blank(w,h)}}}
end
local function layer(doc,id) for i,l in ipairs(doc.layers) do if l.id==id then return l,i end end end
function D.active(doc) return layer(doc,doc.selected) end
function D.validate(doc)
    check(type(doc)=='table' and valid_id(doc.id),'invalid document ID')
    check(valid_size(doc.width,doc.height),'invalid canvas dimensions')
    check(type(doc.layers)=='table' and #doc.layers>=1 and #doc.layers<=D.max_layers,'invalid layer count')
    check(type(doc.next_layer)=='number' and doc.next_layer%1==0 and doc.next_layer>=2 and doc.next_layer<=100000,'invalid layer sequence')
    local ids,max_layer_number={},0
    for _,l in ipairs(doc.layers) do
        check(type(l.id)=='string' and l.id:match('^layer_%d+$') and not ids[l.id],'invalid layer ID');ids[l.id]=true
        local n=tonumber(l.id:match('^layer_(%d+)$'))
        check(n and n>=1 and n<100000,'invalid layer sequence')
        max_layer_number=math.max(max_layer_number,n)
        check(type(l.name)=='string' and #l.name>=1 and #l.name<=64 and not l.name:find('%z'),'invalid layer name')
        check(type(l.visible)=='boolean' and type(l.locked)=='boolean','invalid layer flags')
        check(type(l.rows)=='table' and #l.rows==doc.height,'invalid raster rows')
        for _,row in ipairs(l.rows) do check(type(row)=='string' and #row==doc.width*4,'invalid raster length') end
    end
    check(doc.next_layer>max_layer_number,'layer sequence would reuse an ID')
    check(ids[doc.selected],'invalid selected layer')
    return true
end
function D.from_rgba(id,w,h,bytes)
    check(type(bytes)=='string' and valid_size(w,h) and #bytes==w*h*4,'invalid imported RGBA')
    local doc=D.new(id,w,h);local rows=doc.layers[1].rows
    for y=1,h do rows[y]=bytes:sub((y-1)*w*4+1,y*w*4) end
    return doc
end
local function pixel(row,x) local p=x*4+1;return {row:byte(p,p+3)} end
function D.get(doc,x,y,id)
    if x<0 or y<0 or x>=doc.width or y>=doc.height then return nil end
    local l=layer(doc,id or doc.selected);return l and pixel(l.rows[y+1],x) or nil
end
local function over(dst,src)
    local sa,da=src[4],dst[4]
    if sa==0 then return dst end
    if sa==255 then return {src[1],src[2],src[3],255} end
    local outA=sa+math.floor((da*(255-sa)+127)/255)
    if outA==0 then return {0,0,0,0} end
    local out={}
    for i=1,3 do out[i]=math.floor((src[i]*sa*255+dst[i]*da*(255-sa))/(outA*255)+0.5) end
    out[4]=outA;return out
end
function D.composite_pixel(doc,x,y)
    local c={0,0,0,0}
    for _,l in ipairs(doc.layers) do if l.visible then c=over(c,D.get(doc,x,y,l.id)) end end
    return c
end
function D.flatten(doc)
    D.validate(doc)
    local visible={}
    for _,l in ipairs(doc.layers) do if l.visible then visible[#visible+1]=l end end
    if #visible==0 then return string.rep('\0',doc.width*doc.height*4) end
    if #visible==1 then return table.concat(visible[1].rows) end
    local result={}
    for y=1,doc.height do
        local rows={};for i,l in ipairs(visible) do rows[i]=l.rows[y] end
        local out={}
        for x=0,doc.width-1 do
            local p=x*4+1;local r,g,b,a=0,0,0,0
            for i=1,#rows do
                local sr,sg,sb,sa=rows[i]:byte(p,p+3)
                if sa==255 then r,g,b,a=sr,sg,sb,255
                elseif sa>0 then
                    local outA=sa+math.floor((a*(255-sa)+127)/255)
                    r=math.floor((sr*sa*255+r*a*(255-sa))/(outA*255)+0.5)
                    g=math.floor((sg*sa*255+g*a*(255-sa))/(outA*255)+0.5)
                    b=math.floor((sb*sa*255+b*a*(255-sa))/(outA*255)+0.5)
                    a=outA
                end
            end
            out[#out+1]=string.char(r,g,b,a)
        end
        result[y]=table.concat(out)
    end
    return table.concat(result)
end
function D.set(doc,x,y,color,mode)
    local l=D.active(doc)
    if not l or l.locked or not l.visible then return false,'layer is locked or hidden' end
    if x<0 or y<0 or x>=doc.width or y>=doc.height then return false end
    rgba(color)
    local row=l.rows[y+1];local old=pixel(row,x)
    local c=mode=='erase' and {0,0,0,0} or mode=='replace' and color or over(old,color)
    local bytes=string.char(unpack(c));local p=x*4+1
    if row:sub(p,p+3)==bytes then return false end
    l.rows[y+1]=row:sub(1,p-1)..bytes..row:sub(p+4)
    return true
end
function D.line(doc,x0,y0,x1,y1,color,size,mode)
    check(type(size)=='number' and size%1==0 and size>=1 and size<=64,'invalid brush size')
    local dx,dy=math.abs(x1-x0),math.abs(y1-y0);local sx=x0<x1 and 1 or -1;local sy=y0<y1 and 1 or -1
    local err=dx-dy;local changed=false
    while true do
        for yy=y0-math.floor((size-1)/2),y0+math.floor(size/2) do
            for xx=x0-math.floor((size-1)/2),x0+math.floor(size/2) do
                local result=D.set(doc,xx,yy,color,mode);changed=result or changed
            end
        end
        if x0==x1 and y0==y1 then break end
        local e=2*err
        if e>-dy then err=err-dy;x0=x0+sx end
        if e<dx then err=err+dx;y0=y0+sy end
    end
    return changed
end
function D.fill(doc,x,y,color)
    rgba(color)
    local l=D.active(doc);if not l or l.locked or not l.visible then return false,'layer is locked or hidden' end
    local old=D.get(doc,x,y);if not old then return false end
    local target=string.char(unpack(old));local replacement=string.char(unpack(color))
    if target==replacement then return false end
    local function matches(row,px)
        return row:sub(px*4+1,px*4+4)==target
    end
    local stack={{x,y}}
    while #stack>0 do
        local point=table.remove(stack)
        local px,py=point[1],point[2]
        if px>=0 and py>=0 and px<doc.width and py<doc.height then
            local row=l.rows[py+1]
            if matches(row,px) then
                local left,right=px,px
                while left>0 and matches(row,left-1) do left=left-1 end
                while right+1<doc.width and matches(row,right+1) do right=right+1 end
                l.rows[py+1]=row:sub(1,left*4)..string.rep(replacement,right-left+1)..row:sub((right+1)*4+1)
                for _,ny in ipairs({py-1,py+1}) do
                    if ny>=0 and ny<doc.height then
                        local neighbor=l.rows[ny+1]
                        local scan=left
                        while scan<=right do
                            if matches(neighbor,scan) then
                                stack[#stack+1]={scan,ny}
                                repeat scan=scan+1 until scan>right or not matches(neighbor,scan)
                            else scan=scan+1 end
                        end
                    end
                end
            end
        end
    end
    return true
end
function D.add_layer(doc,name)
    check(#doc.layers<D.max_layers,'layer limit reached')
    local id='layer_'..doc.next_layer;doc.next_layer=doc.next_layer+1
    doc.layers[#doc.layers+1]={id=id,name=name or ('Layer '..#doc.layers+1),visible=true,locked=false,rows=blank(doc.width,doc.height)}
    doc.selected=id;return id
end
function D.delete_layer(doc)
    check(#doc.layers>1,'cannot delete last layer')
    local _,i=D.active(doc);table.remove(doc.layers,i)
    doc.selected=doc.layers[math.min(i,#doc.layers)].id
end
function D.move_layer(doc,direction)
    local _,i=D.active(doc);local j=i+direction
    if j<1 or j>#doc.layers then return false end
    doc.layers[i],doc.layers[j]=doc.layers[j],doc.layers[i];return true
end
function D.encode(doc)
    D.validate(doc)
    local out={'ROEG-LAYERS/1\n',u32(#doc.id),doc.id,u32(doc.width),u32(doc.height),u32(doc.next_layer),u32(#doc.layers),u32(#doc.selected),doc.selected}
    for _,l in ipairs(doc.layers) do
        out[#out+1]=u32(#l.id);out[#out+1]=l.id;out[#out+1]=u32(#l.name);out[#out+1]=l.name
        out[#out+1]=string.char(l.visible and 1 or 0,l.locked and 1 or 0)
        out[#out+1]=table.concat(l.rows)
    end
    local bytes=table.concat(out);check(#bytes<=D.max_bytes,'document too large')
    local a,b=1,0;for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return bytes..u32(b*65536+a)
end
function D.decode(bytes)
    check(type(bytes)=='string' and #bytes<=D.max_bytes+4 and bytes:sub(1,14)=='ROEG-LAYERS/1\n','invalid document header')
    local a,b=1,0;for i=1,#bytes-4 do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    check(number(bytes,#bytes-3)==b*65536+a,'document checksum mismatch')
    local p=15;local function str()
        local len=number(bytes,p);p=p+4;check(len<=1024 and p+len-1<=#bytes-4,'invalid document string')
        local s=bytes:sub(p,p+len-1);p=p+len;return s
    end
    local id=str();local w,h=number(bytes,p),number(bytes,p+4);p=p+8
    check(valid_size(w,h),'invalid canvas dimensions')
    local next_layer,count=number(bytes,p),number(bytes,p+4);p=p+8
    check(count>=1 and count<=D.max_layers,'invalid layer count')
    local selected=str();local doc={id=id,width=w,height=h,next_layer=next_layer,selected=selected,layers={}}
    for _=1,count do
        local lid,name=str(),str();local visible,locked=bytes:byte(p,p+1);p=p+2
        check((visible==0 or visible==1) and (locked==0 or locked==1),'invalid layer flags')
        local rows={};for y=1,h do check(p+w*4-1<=#bytes-4,'truncated raster');rows[y]=bytes:sub(p,p+w*4-1);p=p+w*4 end
        doc.layers[#doc.layers+1]={id=lid,name=name,visible=visible==1,locked=locked==1,rows=rows}
    end
    check(p==#bytes-3,'trailing raster data');D.validate(doc);return doc
end
return D
