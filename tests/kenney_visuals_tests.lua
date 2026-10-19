local Kenney=require('src.kenney_visuals')
local Packs=require('src.visual_packs')
local Game=require('src.game')
local Slots=require('src.visual_slots')
local function read(path)
    local file=io.open(path,'rb');if not file then return nil end
    local bytes=file:read('*a');file:close();return bytes
end
local function image(bytes,path,transparent)
    assert(bytes:sub(1,8)=='\137PNG\r\n\26\n')
    local function number(at)
        local a,b,c,d=bytes:byte(at,at+3);return ((a*256+b)*256+c)*256+d
    end
    return {transparent=transparent,getWidth=function()return number(17)end,
        getHeight=function()return number(21)end}
end
local function quad(x,y,w,h,pw,ph)return {x=x,y=y,w=w,h=h,pw=pw,ph=ph}end
return function(test,equal)
    test('Kenney native packed regions cover classes, enemies, props and all biomes',function()
        local art=assert(Kenney.new(read,image,quad))
        for id,index in pairs(Kenney.actors)do
            assert(Slots.bindings[id])
            local h=assert(art:resolve(id));assert(h.width==8 and h.height==8)
            assert(h.quad.x==index%16*8 and h.quad.y==math.floor(index/16)*8)
            assert(h.texture.transparent and art:resolve(id..'/attack')==h)
        end
        for id in pairs(Kenney.objects)do assert(Slots.bindings[id] and art:resolve(id))end
        local x,y,w,h=Kenney.region(159)
        assert(x==120 and y==72 and w==8 and h==8)
        assert(not pcall(Kenney.region,160) and not pcall(Kenney.region,-1))
        assert(not pcall(Kenney.region,1.5))
    end)
    test('Kenney reads exact checked source and fails closed on missing or corrupt sheet',function()
        assert(Kenney.new(function()end,image,quad)==nil)
        assert(not pcall(Kenney.new,function()return 'corrupt'end,image,quad))
        assert(not pcall(Kenney.new,read,function()
            return {getWidth=function()return 129 end,getHeight=function()return 80 end}
        end,quad))
        assert(not pcall(Kenney.new,function(path)
            if path=='assets/kenney/approved/tiny-dungeon.png' then return nil end
            return read(path)
        end,image,quad))
        assert(not pcall(Kenney.new,function(path)
            if path=='assets/kenney/approved/roguelike-rpg.png' then return 'changed' end
            return read(path)
        end,image,quad))
        local calls=0
        local art=Kenney.new(read,function(bytes,path,transparent)
            calls=calls+1
            local terrain=require('src.kenney_terrain')
            local paths={Kenney.path,require('src.kenney_symbols').path,terrain.path,terrain.path,
                'assets/kenney/approved/tiny-dungeon.png',
                'assets/kenney/approved/roguelike-rpg.png',
                'assets/kenney/approved/roguelike-rpg.png',terrain.path}
            assert(path==paths[calls],'unexpected uncached native texture')
            if calls<=6 then assert(transparent==({true,false,false,'caves-surfaces',true,true})[calls])
            else assert(type(transparent)=='table' and transparent.environment and transparent.style=='kenney'
                and transparent.sheet==(calls==7 and 'rpg'or'caves'))end
            return image(bytes,path,transparent)
        end,quad)
        assert(calls==8 and not next(art.environment.failures),'six native pages plus two cached room-material pages')
        for _=1,20 do
            for id in pairs(art.handles)do assert(art:resolve(id))end
            art:terrain({rows={'###','###','###'}},2,2,'wall')
        end
        assert(calls==8,'resolution must never allocate another texture')
    end)
    test('Kenney connected walls and water select native edges without revealing unknown cells',function()
        local art=assert(Kenney.new(read,image,quad))
        assert(#Kenney.wall_masks==47)
        local seen={}
        for _,mask in ipairs(Kenney.wall_masks)do
            assert(Kenney.canonical(mask)==mask and not seen[mask])
            seen[mask]=true
            local h=assert(art.wall_regions[mask])
            assert(h.quad.x>=0 and h.quad.x+16<=492 and h.quad.y+16<=305)
        end
        for mask=0,255 do assert(seen[Kenney.canonical(mask)])end
        local map={rows={'###','###','###'}}
        local center=art:terrain(map,2,2,'wall')
        assert(center.wall_mask==255)
        map.rows[1]='#.#'
        assert(art:terrain(map,2,2,'wall').wall_mask==110)
        assert(art:terrain(map,2,2,'wall',function(x,y)return y~=1 end)==center)
        map.rows={'##.','###','###'}
        assert(art:terrain(map,2,2,'wall').wall_mask==239)
        assert(art:terrain(map,2,2,'wall',function(x,y)return x~=3 or y~=1 end)==center,
            'hidden diagonal changed the visible inner-corner art')
        map.rows={'~~~','~~~','~~~'}
        assert(art:terrain(map,2,2,'water').water_mask==255)
        map.rows[1]='...';map.rows[2]='.~~'
        assert(art:terrain(map,2,2,'water').water_mask==38)
        assert(art:terrain(map,2,2,'floor')==art:terrain(map,2,2,'floor'))
    end)
    test('Caves floors use only native ground, never door frames or wall props',function()
        local Terrain=require('src.kenney_terrain')
        local art=assert(Kenney.new(read,image,quad))
        for slug,style in pairs(Terrain.styles)do
            local allowed={};for _,index in ipairs(style.floor)do allowed[index]=true end
            local map={rows={},biome_id='core:biome/'..slug}
            for y=1,20 do for x=1,20 do
                local h=art:terrain(map,x,y,'floor')
                assert(allowed[h.tile_index] and h.terrain_pack=='caves' and h.width==16)
                assert(h.quad.x==h.tile_index%29*17 and h.quad.y==math.floor(h.tile_index/29)*17)
                assert(h==art:terrain(map,x,y,'floor'))
            end end
            assert(allowed[art:resolve('core:terrain/'..slug..'/floor/base').tile_index])
        end
        assert(art:resolve('core:object/door_closed').tile_index==36)
        assert(art:resolve('core:object/stairs_down').tile_index==37)
    end)
    test('all boons have deliberate native icons and matching details',function()
        local Symbols=require('src.kenney_symbols')
        local Boons=require('src.boon_content')
        local art=assert(Kenney.new(read,image,quad))
        local used,count={},0
        for _,id in ipairs(Boons.order)do
            local definition=assert(Symbols.boons[id],'unmapped boon '..id)
            local h=assert(art:resolve(id..'/icon'))
            assert(art:resolve(id..'/detail')==h and h.source=='kenney')
            if id=='core:boon/ring_of_reversal' then
                assert(h.width==8 and h.tile_index==89 and h.quad.pw==128)
            else
                assert(h.width==16 and h.height==16 and h.symbol_index==definition[1])
                assert(not used[h.symbol_index],'unintentionally shared boon icon '..id)
                used[h.symbol_index]=true
                assert(h.quad.x==h.symbol_index%49*16 and h.quad.y==math.floor(h.symbol_index/49)*16)
                assert(h.quad.pw==784 and h.quad.ph==352 and h.motif~='')
                assert(not h.tile_inset and not h.ground)
                for _,c in ipairs(h.tint)do assert(c>=0 and c<=1)end
            end
            count=count+1
        end
        assert(count==53)
        for id in pairs(Symbols.boons)do assert(Boons.definitions[id],'orphan boon mapping')end
        assert(art:resolve('core:boon/unknown/icon')==nil)
    end)
    test('all six mechanical terrains have legible native art with exact integer inset scaling',function()
        local Terrain=require('src.terrain')
        local Symbols=require('src.kenney_symbols')
        local art=assert(Kenney.new(read,image,quad))
        local used={}
        for _,symbol in ipairs(Terrain.order)do
            local id=Terrain.get(symbol).id
            local h=assert(art:resolve(id),'unmapped terrain '..id)
            assert(h.symbol_index==Symbols.terrain[id][1] and h.width==16)
            assert((40-h.tile_inset*2)/h.width==2 and not used[h.symbol_index])
            used[h.symbol_index]=true
            for _,c in ipairs(h.ground)do assert(c>=0 and c<1)end
        end
        assert(art:resolve('core:terrain/obscuring_mist').motif=='layered wisps')
        assert(art:resolve('core:terrain/barricade').motif=='cross-braced crate')
        assert(art:resolve('core:terrain/glazed_ice').motif=='ice star')
    end)
    test('Kenney supplement rejects missing, corrupt, wrong-sized and out-of-range inputs',function()
        local Symbols=require('src.kenney_symbols')
        for _,bad in ipairs({'missing','corrupt'})do
            assert(not pcall(Kenney.new,function(path)
                if path==Symbols.path then return bad=='corrupt' and 'broken' or nil end
                return read(path)
            end,image,quad))
        end
        assert(not pcall(Symbols.new,read,function()
            return {getWidth=function()return 783 end,getHeight=function()return 352 end}
        end,quad))
        local x,y,w,h=Symbols.region(1077)
        assert(x==768 and y==336 and w==16 and h==16)
        for _,bad in ipairs({-1,1078,1.5,'1'})do assert(not pcall(Symbols.region,bad))end
    end)
    test('Kenney selection, persistence and refresh lookup leave simulation unchanged',function()
        local state=Game.new_stage(7);local before=Game.snapshot(state)
        local art=assert(Kenney.new(read,image,quad))
        local visuals,status=Packs.build('kenney',function(path)
            assert(path~='assets/studio/manifest.txt','Kenney must bypass published overrides')
            return read(path)
        end,image,function(_,mode)assert(mode=='kenney');return art,'loaded'end)
        assert(status=='loaded' and visuals:resolve('core:actor/adventurer').source=='kenney')
        assert(not Packs.uses_published_theme('kenney'))
        local memory={}
        function memory:read(path)return self[path]end
        function memory:write(path,bytes)self[path]=bytes;return true end
        assert(Packs.save(memory,'kenney') and Packs.inspect(memory).mode=='kenney')
        equal(before,Game.snapshot(state))
    end)
end
