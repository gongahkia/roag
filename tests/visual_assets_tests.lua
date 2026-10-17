local Visual=require('src.visual_assets')
local Slots=require('src.visual_slots')
local function read(path)
    local f=io.open(path,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
return function(test)
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
end
