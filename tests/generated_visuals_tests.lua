local Generated=require('src.generated_visuals')
local Visual=require('src.visual_assets')
local Classes=require('src.classes')
local Biomes=require('src.biomes')
local Boons=require('src.boon_content')
local Game=require('src.game')
local Project=require('src.studio_project')
local Slots=require('src.visual_slots')
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
    test('generated floor and backdrop art stay visually subordinate',function()
        assert(Slots.terrain_alpha('floor','generated',false)==0.18)
        assert(Slots.terrain_alpha('wall','generated',false)==0.72)
        assert(Slots.terrain_alpha('floor','generated',true)==0.18*0.38)
        assert(Slots.terrain_alpha('floor','loose',false)==1)
        assert(Slots.background_alpha('generated')==0.16)
        assert(Slots.background_alpha('loose')==1)
    end)
    test('generated terrain motifs are sparse while authored tiles remain literal',function()
        for _,kind in ipairs({'floor','wall','water'}) do
            local count=0
            for y=1,20 do
                for x=1,20 do
                    if Slots.terrain_draws_at(kind,'generated',x,y) then
                        count=count+1
                        assert(not Slots.terrain_draws_at(kind,'generated',x+1,y))
                        assert(not Slots.terrain_draws_at(kind,'generated',x,y+1))
                    end
                    assert(Slots.terrain_draws_at(kind,'loose',x,y))
                    assert(Slots.terrain_draws_at(kind,'atlas',x,y))
                end
            end
            assert(count>40 and count<140,kind..' motif density is unsafe')
        end
    end)
    test('tiled generated terrain covers every cell and omits the stretched backdrop',function()
        for _,kind in ipairs({'floor','wall','water'})do
            assert(Slots.terrain_alpha(kind,'generated',false,'tiled')==1)
            assert(Slots.terrain_alpha(kind,'generated',true,'tiled')==.38)
            for y=1,20 do for x=1,20 do
                assert(Slots.terrain_draws_at(kind,'generated',x,y,'tiled'))
            end end
        end
        assert(Slots.background_alpha('generated','tiled')==0)
        assert(Slots.background_alpha('loose','tiled')==1)
    end)
    test('connected wall borders use known neighbors without exposing hidden geometry',function()
        local map={rows={'...','.#B','...'}}
        local e=Slots.wall_edges(map,2,2)
        assert(e.n and e.s and e.w and not e.e)
        e=Slots.wall_edges(map,2,2,function(x,y)return y<2 end)
        assert(e.n and not e.s and not e.w and not e.e)
        assert(map.rows[2]=='.#B')
    end)
    test('default checkout has no hand-painted published art override',function()
        assert(read('assets/studio/manifest.txt')==nil)
        local visuals=Visual.new(read,function() error('unexpected image load') end)
        assert(visuals.status=='absent' and not visuals:resolve('core:actor/adventurer'))
    end)
    test('generated four-tone atlas covers current classes, biomes and boons',function()
        local atlas,status=Generated.new(read,image,quad)
        assert(status=='loaded' and atlas and atlas.terrain_mode=='tiled')
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
        for _,slot in ipairs({'hp_icon','xp_icon','currency_icon',
            'hit_effect','boon_effect'}) do
            assert(atlas:resolve('core:ui/'..slot))
        end
        for _,drop in ipairs({'currency','food','health'}) do
            assert(atlas:resolve('core:drop/'..drop))
        end
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
        local missing_contract=original:gsub('terrain_mode\ttiled\n','')
        assert(not pcall(Generated.new,function(path)
            return path=='assets/generated/manifest.txt' and missing_contract or read(path)
        end,image,quad))
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
