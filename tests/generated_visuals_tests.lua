local Generated=require('src.generated_visuals')
local Visual=require('src.visual_assets')
local Classes=require('src.classes')
local Biomes=require('src.biomes')
local Boons=require('src.boon_content')
local Game=require('src.game')
local Project=require('src.studio_project')
local function read(path)
    local file=io.open(path,'rb')
    if not file then return nil end
    local data=file:read('*a');file:close();return data
end
local function image(bytes)
    assert(bytes:sub(1,8)=='\137PNG\r\n\26\n')
    local function number(at)
        local a,b,c,d=bytes:byte(at,at+3)
        return a*16777216+b*65536+c*256+d
    end
    local width,height=number(17),number(21)
    return {getWidth=function() return width end,
        getHeight=function() return height end}
end
local function quad(x,y,w,h,pw,ph)
    return {x=x,y=y,w=w,h=h,pw=pw,ph=ph}
end
return function(test,equal)
    test('default checkout has no hand-painted published art override',function()
        assert(read('assets/studio/manifest.txt')==nil)
        local visuals=Visual.new(read,function() error('unexpected image load') end)
        assert(visuals.status=='absent' and not visuals:resolve('core:actor/adventurer'))
    end)
    test('generated four-tone atlas covers current classes, biomes and boons',function()
        local atlas,status=Generated.new(read,image,quad)
        assert(status=='loaded' and atlas)
        for _,id in ipairs(Classes.order) do
            assert(atlas:resolve(id) and atlas:resolve(id..'/attack') and
                atlas:resolve(id..'/hurt'))
        end
        for _,biome_id in ipairs(Biomes.order) do
            local biome=Biomes.get(biome_id)
            local slug=biome_id:match('/([^/]+)$')
            assert(atlas:resolve('core:background/'..slug))
            for _,tile in ipairs({'floor','wall','water'}) do
                assert(atlas:resolve('core:terrain/'..slug..'/'..tile..'/base'))
            end
            assert(atlas:resolve(biome.elite_id))
            for _,enemy in ipairs(biome.enemy_ids) do assert(atlas:resolve(enemy)) end
        end
        for _,id in ipairs(Boons.order) do
            assert(atlas:resolve(id..'/icon') and atlas:resolve(id..'/detail'))
        end
        assert(atlas:resolve('core:enemy/hollow_regent') and
            atlas:resolve('core:enemy/extraction_pursuer'))
        local count=0
        for _ in pairs(atlas.handles) do count=count+1 end
        assert(count==235 and #Boons.order==52)
    end)
    test('generated atlas rejects incomplete and corrupted pages as one pack',function()
        local original=read('assets/generated/manifest.txt')
        assert(original)
        local page=original:match('page\tactors\t([^\t]+)')
        assert(page)
        local missing=function(path)
            if path==page then return nil end
            return read(path)
        end
        assert(not pcall(Generated.new,missing,image,quad))
        local corrupt=function(path)
            if path==page then return 'bad PNG' end
            return read(path)
        end
        assert(not pcall(Generated.new,corrupt,image,quad))
        assert(Generated.new(function()return nil end,image,quad)==nil)
    end)
    test('generated art fallback stays presentation-only',function()
        local atlas=assert(Generated.new(read,image,quad))
        local visuals=Visual.new(function()return nil end,function()error('loose used')end,atlas)
        assert(visuals.status=='generated' and visuals.has_terrain)
        local handle=assert(visuals:resolve('core:actor/adventurer'))
        assert(handle.source=='generated' and handle.quad and handle.width==32)
        local before=Game.snapshot(Game.new_stage(29))
        visuals:resolve('core:terrain/overgrown_ruins/floor/base')
        visuals:resolve('core:boon/storm_conductor/icon')
        equal(before,Game.snapshot(Game.restore(before)))
    end)
    test('published loose art takes precedence over optional generated art',function()
        local generated=assert(Generated.new(read,image,quad))
        local project=Project.new('fixture','Fixture')
        local asset=Project.asset('studio:art/adventurer','Hero','actor')
        asset.binding='core:actor/adventurer'
        asset.source='sources/hero.png';asset.format='png'
        asset.width=192;asset.height=192
        project.assets[1]=asset
        local page=assert(read('assets/generated/manifest.txt'):match(
            'page\tactors\t([^\t]+)'))
        local png=assert(read(page))
        local function source(path)
            if path=='assets/studio/manifest.txt' then return Project.encode(project) end
            if path=='assets/studio/sources/hero.png' then return png end
            return nil
        end
        local visuals=Visual.new(source,function(bytes) return image(bytes) end,
            generated)
        local handle=assert(visuals:resolve(asset.binding))
        assert(handle.source=='loose' and not handle.quad)
        local broken=Visual.new(function(path)
            if path=='assets/studio/sources/hero.png' then return 'corrupt' end
            return source(path)
        end,function(bytes) return image(bytes) end,generated)
        assert(broken:resolve(asset.binding).source=='generated')
    end)
end
