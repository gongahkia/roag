-- Kenney Roguelike Caves & Dungeons, CC0. Native 16px tiles, 1px gutters.
-- Connectivity describes presentation only; it never changes map cells/RNG.
local Atlas=require('src.studio_atlas')
local Biomes=require('src.biomes')
local T={path='assets/kenney/caves-dungeons/terrain.png',checksum='974b7d14df86b1d7',
    width=492,height=305,tile=16,columns=29,stride=17}
local function has(mask,bit)return math.floor(mask/bit)%2==1 end
local corners={{1,2,16},{2,4,32},{4,8,64},{8,1,128}}
function T.canonical(mask)
    local result=mask%16
    for _,c in ipairs(corners)do
        if has(mask,c[1]) and has(mask,c[2]) and has(mask,c[3])then result=result+c[3]end
    end
    return result
end
T.masks={}
for mask=0,255 do
    if T.canonical(mask)==mask then T.masks[#T.masks+1]=mask end
end
T.styles={
    overgrown_ruins={wall='stone',water='blue',floor={67}},
    fungal_hollows={wall='earth',water='green',floor={212}},
    sunken_aqueduct={wall='stone',water='blue',floor={335,336,337,338}},
    ashen_bastion={wall='stone',water='blue',floor={364,365,366,367}},
    glacial_observatory={wall='stone',water='blue',floor={306,307,308,309}},
    fractured_archive={wall='stone',water='blue',floor={422,423,424,425}},
    sundered_forge={wall='earth',water='green',floor={393,394,395,396}},
}
T.wall_materials={stone={frame=38,center=39},earth={frame=183,center=184}}
-- Outer NW/N/NE/W/C/E/SW/S/SE plus native concave corners NW/NE/SE/SW.
T.water_materials={
    blue={290,291,292,319,320,321,348,349,350,322,323,294,293},
    green={295,296,297,324,325,326,353,354,355,327,328,299,298},
}
function T.region(index)
    assert(type(index)=='number' and index%1==0 and index>=0 and index<522,
        'invalid Caves & Dungeons tile index')
    return index%29*17,math.floor(index/29)*17,16,16
end
local surfaces={}
for _,family in ipairs({'stone','earth','blue','green'})do
    for _,mask in ipairs(T.masks)do
        surfaces[#surfaces+1]={family=family,mask=mask}
    end
end
T.surfaces=surfaces
function T.surface_sheet(source)
    local out=source:clone()
    local function sample(index,x,y)
        local ox,oy=T.region(index)
        return source:getPixel(ox+x,oy+y)
    end
    local quadrants={
        {1,8,128,0,0,1,2,4,10},{1,2,16,8,0,3,2,6,11},
        {4,2,32,8,8,9,8,6,12},{4,8,64,0,8,7,8,4,13},
    }
    for i,surface in ipairs(surfaces)do
        local ox,oy=(i-1)%29*16,math.floor((i-1)/29)*16
        local mask,family=surface.mask,surface.family
        local wall=T.wall_materials[family]
        if wall then
            for y=0,15 do for x=0,15 do
                local r,g,b,a=sample(wall.center,8,8)
                if not has(mask,1) and y<4 then r,g,b,a=sample(wall.frame,8,y)end
                if not has(mask,4) and y>=12 then r,g,b,a=sample(wall.frame,8,y)end
                if not has(mask,8) and x<4 then r,g,b,a=sample(wall.frame,x,8)end
                if not has(mask,2) and x>=12 then r,g,b,a=sample(wall.frame,x,8)end
                for _,q in ipairs(quadrants)do
                    local dx,dy=q[4]==0 and x or 15-x,q[5]==0 and y or 15-y
                    if dx<4 and dy<4 then
                        local sx=q[4]==0 and dx or 15-dx
                        local sy=q[5]==0 and dy or 15-dy
                        if not has(mask,q[1]) and not has(mask,q[2])then
                            r,g,b,a=sample(wall.frame,sx,sy)
                        elseif has(mask,q[1]) and has(mask,q[2]) and not has(mask,q[3])then
                            r,g,b,a=sample(wall.frame,q[4]==0 and 3-dx or 12+dx,
                                q[5]==0 and 3-dy or 12+dy)
                        end
                    end
                end
                -- Keep the wall mass opaque even at a native clipped corner.
                if a==0 then r,g,b,a=sample(wall.center,8,8)end
                out:setPixel(ox+x,oy+y,r,g,b,a)
            end end
        else
            local water=T.water_materials[family]
            for _,q in ipairs(quadrants)do
                local role
                if not has(mask,q[1]) and not has(mask,q[2])then role=q[6]
                elseif not has(mask,q[1])then role=q[7]
                elseif not has(mask,q[2])then role=q[8]
                elseif not has(mask,q[3])then role=q[9]
                else role=5 end
                for y=q[5],q[5]+7 do for x=q[4],q[4]+7 do
                    out:setPixel(ox+x,oy+y,sample(water[role],x,y))
                end end
            end
        end
    end
    return out
end
function T.new(read,make_texture,make_quad)
    local bytes=assert(read(T.path),'missing Caves & Dungeons sheet')
    assert(Atlas.checksum(bytes)==T.checksum,'corrupt Caves & Dungeons sheet')
    local native=assert(make_texture(bytes,T.path,false),'unreadable Caves & Dungeons sheet')
    local surface=assert(make_texture(bytes,T.path,'caves-surfaces'),'unreadable Caves surfaces')
    for _,image in ipairs({native,surface})do
        assert(image:getWidth()==T.width and image:getHeight()==T.height,
            'Caves & Dungeons dimensions differ')
    end
    local walls,waters,floors,bindings={},{},{},{}
    for i,entry in ipairs(surfaces)do
        local x,y=(i-1)%29*16,math.floor((i-1)/29)*16
        local handle={texture=surface,quad=make_quad(x,y,16,16,T.width,T.height),
            width=16,height=16,offset_x=x,offset_y=y,source='kenney',terrain_pack='caves',
            surface_family=entry.family,wall_mask=T.wall_materials[entry.family] and entry.mask or nil,
            water_mask=T.water_materials[entry.family] and entry.mask or nil}
        local groups=T.wall_materials[entry.family] and walls or waters
        groups[entry.family]=groups[entry.family] or {}
        groups[entry.family][entry.mask]=handle
    end
    local regions={}
    for _,biome in ipairs(Biomes.order)do
        local slug=biome:match('/([^/]+)$');local style=assert(T.styles[slug])
        floors[slug]={}
        for _,index in ipairs(style.floor)do
            if not regions[index]then
                local x,y=T.region(index)
                regions[index]={texture=native,quad=make_quad(x,y,16,16,T.width,T.height),
                    width=16,height=16,offset_x=x,offset_y=y,source='kenney',
                    terrain_pack='caves',tile_index=index}
            end
            floors[slug][#floors[slug]+1]=regions[index]
        end
        local root='core:terrain/'..slug..'/'
        bindings[root..'floor/base']=floors[slug][1]
        bindings[root..'wall/base']=walls[style.wall][255]
        bindings[root..'water/base']=waters[style.water][255]
    end
    return setmetatable({native=native,surface=surface,bindings=bindings,
        walls=walls,waters=waters,floors=floors,signature=T.checksum..':surfaces-1'},{__index=T})
end
function T:resolve(map,x,y,kind,known)
    local slug=(map.biome_id or ''):match('/([^/]+)$') or 'overgrown_ruins'
    if not T.styles[slug]then slug='overgrown_ruins'end
    local style=T.styles[slug]
    if kind=='floor'then
        local choices=self.floors[slug]
        return choices[(x*3+y*5)%#choices+1]
    end
    local function same(dx,dy)
        local xx,yy=x+dx,y+dy;local row=map.rows[yy]
        if xx<1 or not row or xx>#row then return false end
        if known and not known(xx,yy)then return true end
        return row:sub(xx,xx)==(kind=='wall' and '#' or '~')
    end
    local mask=0
    for _,neighbor in ipairs({{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},
        {1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}})do
        if same(neighbor[1],neighbor[2])then mask=mask+neighbor[3]end
    end
    mask=T.canonical(mask)
    return kind=='wall' and self.walls[style.wall][mask] or self.waters[style.water][mask]
end
return T
