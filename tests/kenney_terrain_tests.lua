local T=require('src.kenney_terrain')
local Biomes=require('src.biomes')
local function read(path)
    local f=io.open(path,'rb');if not f then return nil end
    local b=f:read('*a');f:close();return b
end
local function image()return {getWidth=function()return 492 end,getHeight=function()return 305 end}end
local function quad(x,y,w,h,pw,ph)return {x=x,y=y,w=w,h=h,pw=pw,ph=ph}end
return function(test)
    test('Caves native source validates 16px regions with 1px gutters and rejects damaged inputs',function()
        assert(T.new(read,image,quad))
        for _,index in ipairs({0,38,67,183,320,521})do
            local x,y,w,h=T.region(index)
            assert(x==index%29*17 and y==math.floor(index/29)*17 and w==16 and h==16)
            assert(x+w<=492 and y+h<=305)
        end
        for _,bad in ipairs({-1,522,1.5,'38'})do assert(not pcall(T.region,bad))end
        assert(not pcall(T.new,function()end,image,quad))
        assert(not pcall(T.new,function()return 'broken'end,image,quad))
        assert(not pcall(T.new,read,function()
            return {getWidth=function()return 491 end,getHeight=function()return 305 end}
        end,quad))
    end)
    test('all biomes cover every wall/water neighborhood without disclosing unknown geometry',function()
        local art=T.new(read,image,quad)
        local neighbors={{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},
            {1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}}
        for _,biome in ipairs(Biomes.order)do
            assert(T.styles[biome:match('/([^/]+)$')])
            for _,kind in ipairs({'wall','water'})do
                local mark=kind=='wall' and '#' or '~'
                for mask=0,255 do
                    local cells={{'.','.','.'},{'.',mark,'.'},{'.','.','.'}}
                    for _,n in ipairs(neighbors)do
                        if math.floor(mask/n[3])%2==1 then cells[2+n[2]][2+n[1]]=mark end
                    end
                    local map={biome_id=biome,rows={}}
                    for i,row in ipairs(cells)do map.rows[i]=table.concat(row)end
                    local h=art:resolve(map,2,2,kind)
                    assert((h.wall_mask or h.water_mask)==T.canonical(mask))
                    assert(h.terrain_pack=='caves' and h.width==16 and h.height==16)
                    assert(h.quad.x>=0 and h.quad.y>=0 and h.quad.x+16<=492 and h.quad.y+16<=305)
                    assert(art:resolve(map,2,2,kind,function()return false end)==
                        (kind=='wall' and art.walls[T.styles[biome:match('/([^/]+)$')].wall][255] or
                            art.waters[T.styles[biome:match('/([^/]+)$')].water][255]))
                end
            end
        end
    end)
    test('Caves surface assembly preserves native quarter pixels and leaves source untouched',function()
        local pixels,writes={},0
        local source={}
        function source:getPixel(x,y)
            assert(x>=0 and y>=0 and x<492 and y<305)
            return x/512,y/512,0,1
        end
        function source:setPixel()error('native source was mutated')end
        function source:clone()
            return {setPixel=function(_,x,y,r,g,b,a)
                assert(not pixels[y*492+x],'overlapping compiled regions')
                pixels[y*492+x]={r,g,b,a};writes=writes+1
            end}
        end
        T.surface_sheet(source)
        assert(writes==188*16*16 and #T.masks==47)
        local function pixel(family,mask,x,y)
            for i,s in ipairs(T.surfaces)do
                if s.family==family and s.mask==mask then
                    local ox,oy=(i-1)%29*16,math.floor((i-1)/29)*16
                    return assert(pixels[(oy+y)*492+ox+x])
                end
            end
            error('missing surface')
        end
        local function matches(p,index,x,y)
            local ox,oy=T.region(index)
            assert(p[1]==(ox+x)/512 and p[2]==(oy+y)/512 and p[4]==1)
        end
        for _,family in ipairs({'blue','green'})do
            local m=T.water_materials[family]
            for y=0,15 do for x=0,15 do matches(pixel(family,255,x,y),m[5],x,y)end end
            matches(pixel(family,0,0,0),m[1],0,0)
            matches(pixel(family,127,0,0),m[10],0,0)
        end
        for _,family in ipairs({'stone','earth'})do
            for y=0,15 do for x=0,15 do
                matches(pixel(family,255,x,y),T.wall_materials[family].center,8,8)
            end end
        end
    end)
end
