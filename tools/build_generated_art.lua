-- Offline compilation of the checked-in imagegen reference sheets. LÖVE 11.5
-- decodes source PNGs; the build quantizes, slices, aligns and packs exact pixels.
local PNG=require('tools.art_png')
local Atlas=require('src.studio_atlas')
local Classes=require('src.classes')
local Biomes=require('src.biomes')
local B={}
local enemy_order={
    'mossbound_guard','thornspitter','ruin_skitter','spore_herald','canal_lancer',
    'cinder_mender','cinder_bellkeeper','glass_charger','crystal_bombardier','vault_lurker',
    'mire_binder','pack_chorister','gloom_strider','forge_conjurer','mossbreaker',
    'bastion_bulwark','thorn_warden','mycelial_matriarch','canal_lockmaster','ash_marshal',
    'prism_curator','index_keeper','ember_crucible','extraction_pursuer','hollow_regent',
}
local legacy='assets/generated/builds/e6f7e490b979490e/'
local function read(path)
    local f=assert(io.open(path,'rb'));local bytes=f:read('*a');f:close();return bytes
end
local function hex(value)
    return {tonumber(value:sub(1,2),16)/255,tonumber(value:sub(3,4),16)/255,
        tonumber(value:sub(5,6),16)/255}
end
local source_palette={hex('101820'),hex('28383c'),hex('607466'),hex('b8c48a')}
local hero_palette={hex('101820'),hex('52664c'),hex('a3b66c'),hex('e4e6ae')}
-- Each raster has four opaque colors. Terrain brightness has a fixed hierarchy:
-- quiet floors, legible wall mass, and bright heroes above both.
local themes={
    {'101d19','1b3027','476143','879d60'},
    {'191521','2c2336','5c4a70','aa8cba'},
    {'101f24','20373d','496c72','96b8b2'},
    {'211915','392d23','725643','ba9564'},
    {'14232d','293f4c','668a98','bfd4cd'},
    {'211d27','37313c','75615b','bea879'},
    {'261b19','432c27','81523d','c99b65'},
}
local function nearest(r,g,b,palette)
    local best,distance=1,math.huge
    for index,c in ipairs(palette) do
        local d=(r-c[1])^2+(g-c[2])^2+(b-c[3])^2
        if d<distance then best,distance=index,d end
    end
    return best
end
local function load(root,path)
    return love.image.newImageData(love.filesystem.newFileData(read(root..'/'..path),path))
end
local function sample(image,x,y)
    local r,g,b,a=image:getPixel(math.min(image:getWidth()-1,math.max(0,math.floor(x))),
        math.min(image:getHeight()-1,math.max(0,math.floor(y))))
    return r,g,b,a
end
local function terrain_indices(source,column,row)
    local result={}
    local cw,ch=source:getWidth()/4,source:getHeight()/2
    for y=0,31 do
        result[y]={}
        for x=0,31 do
            -- Interior samples exclude the thin separating rules in the sheet.
            local votes={0,0,0,0}
            for _,dy in ipairs({.25,.5,.75}) do
                for _,dx in ipairs({.25,.5,.75}) do
                    local r,g,b=sample(source,column*cw+2+(x+dx)*(cw-4)/32,
                        row*ch+2+(y+dy)*(ch-4)/32)
                    local color=nearest(r,g,b,source_palette)
                    votes[color]=votes[color]+1
                end
            end
            local color=1
            for i=2,4 do if votes[i]>votes[color] then color=i end end
            result[y][x]=color
        end
    end
    -- An actual toroidal tile contract: every opposing edge pixel matches,
    -- including corners. Transparent atlas margins are never part of terrain.
    for i=0,31 do result[i][31]=result[i][0] end
    for i=0,31 do result[31][i]=result[0][i] end
    return result
end
local function write_terrain(source)
    local page=love.image.newImageData(224,96,'rgba8')
    local textures={terrain_indices(source,0,0),terrain_indices(source,2,0),
        terrain_indices(source,0,1)}
    for biome=1,7 do
        local p={}
        for _,color in ipairs(themes[biome])do p[#p+1]=hex(color)end
        for kind=1,3 do
            local palette=p
            if kind==1 then
                -- All floor pattern values remain below the wall's main face.
                palette={p[1],p[2],{p[2][1]*1.12,p[2][2]*1.12,p[2][3]*1.12},p[3]}
            elseif kind==3 then
                palette={hex('0d2631'),hex('194451'),hex('347184'),hex('86b4bb')}
            end
            for y=0,31 do for x=0,31 do
                local c=palette[textures[kind][y][x]]
                page:setPixel((biome-1)*32+x,(kind-1)*32+y,c[1],c[2],c[3],1)
            end end
        end
    end
    return page
end
local function sprite(source,index,columns,rows,label)
    local column,row=(index-1)%columns,math.floor((index-1)/columns)
    local ox=math.floor(column*source:getWidth()/columns)
    local oy=math.floor(row*source:getHeight()/rows)
    local cw=math.floor((column+1)*source:getWidth()/columns)-ox
    local ch=math.floor((row+1)*source:getHeight()/rows)-oy
    local xmin,ymin,xmax,ymax=cw,ch,0,0
    local alpha_min,alpha_max=1,0
    for y=0,ch-1 do for x=0,cw-1 do
        local r,g,b,a=source:getPixel(ox+x,oy+y)
        alpha_min=math.min(alpha_min,a);alpha_max=math.max(alpha_max,a)
        if a>=.7 and (r*.2126+g*.7152+b*.0722)>.19 then
            xmin=math.min(xmin,x);xmax=math.max(xmax,x)
            ymin=math.min(ymin,y);ymax=math.max(ymax,y)
        end
    end end
    assert(xmax>xmin and ymax>ymin,'empty generated actor cell '..label)
    local width,height=xmax-xmin+1,ymax-ymin+1
    local scale=math.min(27/width,28/height)
    local w,h=math.max(1,math.floor(width*scale+.5)),math.max(1,math.floor(height*scale+.5))
    local left,top=math.floor((32-w)/2),30-h
    local colors,mask={},{}
    for y=0,31 do colors[y]={};mask[y]={} end
    for y=0,h-1 do for x=0,w-1 do
        local r,g,b,a=sample(source,ox+xmin+(x+.5)*width/w,
            oy+ymin+(y+.5)*height/h)
        local xx,yy=left+x,top+y
        if a>=.5 and r*.2126+g*.7152+b*.0722>.13 then
            colors[yy][xx]=nearest(r,g,b,hero_palette);mask[yy][xx]=true
        end
    end end
    -- Restore enclosed dark eyes and belts. Exterior dark/glowing source
    -- backgrounds are discarded; a precise one-pixel outline is rebuilt.
    local outside,queue,head={},{},1
    for y=0,31 do outside[y]={} end
    local function visit(x,y)
        if x<0 or y<0 or x>31 or y>31 or outside[y][x] or mask[y][x]then return end
        outside[y][x]=true;queue[#queue+1]={x,y}
    end
    for i=0,31 do visit(i,0);visit(i,31);visit(0,i);visit(31,i)end
    while head<=#queue do
        local p=queue[head];head=head+1
        visit(p[1]-1,p[2]);visit(p[1]+1,p[2]);visit(p[1],p[2]-1);visit(p[1],p[2]+1)
    end
    for y=0,31 do for x=0,31 do
        if not outside[y][x] and not mask[y][x]then colors[y][x]=1;mask[y][x]=true end
    end end
    local image=love.image.newImageData(32,32,'rgba8')
    for y=0,31 do for x=0,31 do
        local color=colors[y][x]
        if not color then
            for dy=-1,1 do for dx=-1,1 do
                if mask[y+dy] and mask[y+dy][x+dx]then color=1 end
            end end
        end
        if color then local c=hero_palette[color];image:setPixel(x,y,c[1],c[2],c[3],1)end
    end end
    print(('Actor %s: source %dx%d, normalized %dx%d; alpha %.2f..%.2f'):
        format(label,width,height,w,h,alpha_min,alpha_max))
    return image
end

function B.run(root)
    local terrain=write_terrain(load(root,'assets/generated/sources/terrain-v2.png'))
    local actors=load(root,legacy..'actors.png')
    local source=load(root,'assets/generated/sources/classes-v2.png')
    local manifest=read(root..'/assets/generated/manifest.txt')
    local placements={}
    for line in manifest:gmatch('[^\r\n]+')do
        local id,x,y=line:match('^asset\t([^\t]+)\tactors\t(%d+)\t(%d+)\t32\t32$')
        if id and not id:match('/attack$') and not id:match('/hurt$')then
            placements[id]={tonumber(x),tonumber(y)}
        end
    end
    local function paste(id,sprite)
        local colors={}
        for y=0,31 do for x=0,31 do
            local r,g,b,a=sprite:getPixel(x,y)
            assert(a==0 or a==1,'fractional hero alpha')
            if x==0 or y==0 or x==31 or y==31 then assert(a==0,'hero touches cell edge')end
            if a==1 then colors[('%f:%f:%f'):format(r,g,b)]=true end
        end end
        local count=0;for _ in pairs(colors)do count=count+1 end
        assert(count<=4,'hero palette exceeds four colors')
        local p=assert(placements[id],'missing actor atlas binding '..id)
        actors:paste(sprite,p[1],p[2])
        placements[id]=nil
    end
    for index,id in ipairs(Classes.order)do
        paste(id,sprite(source,index,3,2,id))
    end
    local enemy_source=load(root,'assets/generated/sources/enemies-v2.png')
    for index,slug in ipairs(enemy_order)do
        local id='core:enemy/'..slug
        paste(id,sprite(enemy_source,index,5,5,id))
    end
    assert(not next(placements),'actor atlas has an uncompiled identity')
    local pages={actors=PNG.encode(actors),terrain=PNG.encode(terrain),
        icons=read(root..'/'..legacy..'icons.png'),
        backgrounds=read(root..'/'..legacy..'backgrounds.png')}
    local biome_columns={}
    for i,id in ipairs(Biomes.order)do biome_columns[id:match('/([^/]+)$')]=(i-1)*32 end
    local asset_lines={}
    for line in manifest:gmatch('[^\r\n]+')do
        if line:match('^asset\t')then
            local id,slug,kind=line:match('^asset\t(core:terrain/([^/]+)/([^/]+)/base)\t')
            if id then
                local row=assert(({floor=0,wall=32,water=64})[kind])
                line=table.concat({'asset',id,'terrain',assert(biome_columns[slug]),row,32,32},'\t')
            end
            asset_lines[#asset_lines+1]=line
        end
    end
    table.sort(asset_lines)
    local signature=Atlas.checksum(pages.actors..pages.terrain..pages.icons..
        pages.backgrounds..table.concat(asset_lines,'\n'))
    local lines={'ROAG-GENERATED-ATLAS/2','terrain_mode\ttiled'}
    for _,name in ipairs({'actors','backgrounds','icons','terrain'})do
        local image=love.image.newImageData(love.filesystem.newFileData(pages[name],name..'.png'))
        lines[#lines+1]=table.concat({'page',name,
            'assets/generated/builds/'..signature..'/'..name..'.png',
            image:getWidth(),image:getHeight(),Atlas.checksum(pages[name])},'\t')
        assert(love.filesystem.write(name..'.png',pages[name]))
        assert(love.filesystem.read(name..'.png')==pages[name],'PNG readback differs')
    end
    for _,line in ipairs(asset_lines)do lines[#lines+1]=line end
    -- Validate the actual decoded raster, not file extensions or just headers.
    local decoded=love.image.newImageData(love.filesystem.newFileData(pages.terrain,'terrain.png'))
    assert(decoded:getString()==terrain:getString(),'PNG terrain pixel roundtrip failed')
    for biome=0,6 do for kind=0,2 do
        local colors={}
        for yy=0,31 do for xx=0,31 do
            local r,g,b,a=decoded:getPixel(biome*32+xx,kind*32+yy)
            assert(a==1,'transparent terrain cell')
            colors[('%f:%f:%f'):format(r,g,b)]=true
        end end
        local count=0;for _ in pairs(colors)do count=count+1 end
        assert(count<=4,'terrain palette exceeds four colors')
        for i=0,31 do
            local x,y=biome*32,kind*32
            local function pixel(px,py)return string.char(math.floor(select(1,decoded:getPixel(px,py))*255+.5),
                math.floor(select(2,decoded:getPixel(px,py))*255+.5),
                math.floor(select(3,decoded:getPixel(px,py))*255+.5),
                math.floor(select(4,decoded:getPixel(px,py))*255+.5))end
            assert(pixel(x,y+i)==pixel(x+31,y+i),'horizontal texture seam')
            assert(pixel(x+i,y)==pixel(x+i,y+31),'vertical texture seam')
            assert(select(4,decoded:getPixel(x+i,y))==1,'transparent terrain edge')
        end
    end end
    assert(love.filesystem.write('manifest.txt',table.concat(lines,'\n')..'\n'))
    assert(love.filesystem.write('fingerprint.txt',signature..'\n'))
    local result=assert(io.open(assert(os.getenv('ROAG_ART_RESULT')),'w'))
    result:write(love.filesystem.getSaveDirectory(),'\n',signature,'\n');result:close()
    print('Art build validated: '..signature..' (21 opaque edge-matched tiles, 31 aligned actors)')
end
return B
