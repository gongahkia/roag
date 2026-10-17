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
    return {transparent=transparent,getWidth=function()return 128 end,
        getHeight=function()return 80 end}
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
        local calls=0
        Kenney.new(read,function(bytes,path,transparent)
            calls=calls+1;assert(path==Kenney.path)
            assert(transparent==({false,true,'walls'})[calls]);return image(bytes,path,transparent)
        end,quad)
        assert(calls==3,'textures are created per pack, not per sprite')
    end)
    test('Kenney connected walls and water select native edges without revealing unknown cells',function()
        local art=assert(Kenney.new(read,image,quad))
        assert(#Kenney.wall_masks==47)
        local seen={}
        for _,mask in ipairs(Kenney.wall_masks)do
            assert(Kenney.canonical(mask)==mask and not seen[mask])
            seen[mask]=true
            local h=assert(art.wall_regions[mask])
            assert(h.quad.x>=0 and h.quad.x+8<=128 and h.quad.y+8<=80)
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
        assert(art:terrain(map,2,2,'water').tile_index==140)
        map.rows[1]='...';map.rows[2]='.~~'
        assert(art:terrain(map,2,2,'water').tile_index==125)
        assert(art:terrain(map,2,2,'floor')==art:terrain(map,2,2,'floor'))
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
