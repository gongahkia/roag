local O=require('src.onebit_visuals')
local Slots=require('src.visual_slots')
local Boons=require('src.boon_content')
local Terrain=require('src.terrain')
local Packs=require('src.visual_packs')
local Game=require('src.game')
local function read(path)
    assert(path==O.path,'standalone 1-Bit read another art pack')
    local f=assert(io.open(path,'rb'));local b=f:read('*a');f:close();return b
end
local function image()return {getWidth=function()return 784 end,getHeight=function()return 352 end}end
local function quad(x,y,w,h,pw,ph)return {x=x,y=y,w=w,h=h,pw=pw,ph=ph}end
return function(test,equal)
    test('1-Bit sheet ink and composed surface background are neutral',function()
        assert(O.ink[1]==O.ink[2] and O.ink[2]==O.ink[3])
        assert(O.background[1]==O.background[2] and
            O.background[2]==O.background[3])
    end)
    test('standalone 1-Bit covers every actor, prop, boon and terrain sprite using only its own PNG',function()
        local art,status=O.new(read,image,quad)
        assert(status=='loaded' and art.style=='onebit')
        local used,count={},0
        for id in pairs(Slots.bindings)do
            if id:match('^core:actor/') or id:match('^core:enemy/')then
                assert(art:resolve(id),'missing native 1-Bit actor/pose '..id)
            end
        end
        for id,index in pairs(O.actors)do
            local h=art:resolve(id)
            local prior=used[index]
            if prior then
                local aliases={['core:enemy/bone_fragment']='core:enemy/bone_splitter',
                    ['core:enemy/bone_splitter']='core:enemy/bone_fragment'}
                assert(aliases[id]==prior,'unexpected shared actor silhouette '..id)
            end
            used[index]=id;count=count+1
            assert(h.source=='onebit' and h.width==16 and h.quad.x==index%49*16)
            assert(art:resolve(id..'/attack')==h and art:resolve(id..'/hurt')==h)
        end
        assert(count==52)
        for id in pairs(O.objects)do assert(art:resolve(id) and Slots.bindings[id])end
        for _,id in ipairs(Boons.order)do
            local h=assert(art:resolve(id..'/icon'));assert(art:resolve(id..'/detail')==h)
            assert(h.source=='onebit' and h.tint[1]==O.ink[1])
        end
        for _,mark in ipairs(Terrain.order)do assert(art:resolve(Terrain.get(mark).id))end
    end)
    test('standalone 1-Bit neighbor lookup, opacity and source composition remain deterministic',function()
        local art=O.new(read,image,quad)
        local state=Game.new_stage(13);local before=Game.snapshot(state)
        local neighbors={{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},
            {1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}}
        for _,kind in ipairs({'wall','water'})do
            local mark=kind=='wall' and '#' or '~'
            for mask=0,255 do
                local rows={{'.','.','.'},{'.',mark,'.'},{'.','.','.'}}
                for _,n in ipairs(neighbors)do
                    if math.floor(mask/n[3])%2==1 then rows[n[2]+2][n[1]+2]=mark end
                end
                local map={rows={}}
                for i,row in ipairs(rows)do map.rows[i]=table.concat(row)end
                local h=art:terrain(map,2,2,kind)
                assert((h.wall_mask or h.water_mask)==O.canonical(mask))
                assert(art:terrain(map,2,2,kind,function()return false end)==
                    (kind=='wall' and art.walls[255] or art.waters[255]))
            end
        end
        equal(before,Game.snapshot(state))
        local written={};local source={}
        function source:getPixel(x,y)assert(x>=0 and y>=0 and x<784 and y<352);return 1,1,1,(x+y)%2 end
        function source:setPixel()error('1-Bit source was changed')end
        function source:clone()return {setPixel=function(_,x,y,r,g,b,a)
            assert(not written[y*784+x] and a==1);written[y*784+x]=true
            assert(r>=0 and r<=1 and g>=0 and g<=1 and b>=0 and b<=1)
        end}end
        O.surface_sheet(source)
        local count=0;for _ in pairs(written)do count=count+1 end
        assert(count==95*16*16)
    end)
    test('standalone 1-Bit fails closed on absent, corrupt or wrongly sized art',function()
        assert(O.new(function()end,image,quad)==nil)
        assert(not pcall(O.new,function()return 'broken'end,image,quad))
        assert(not pcall(O.new,read,function()
            return {getWidth=function()return 783 end,getHeight=function()return 352 end}
        end,quad))
    end)
    test('third sprite-pack setting persists and rejects torn replacement without touching the run',function()
        local store={}
        function store:read(path)return self[path]end
        function store:write(path,bytes)self[path]=self.torn and bytes:sub(1,7) or bytes;return true end
        assert(Packs.save(store,'onebit'))
        assert(Packs.inspect(store).mode=='onebit')
        store.torn=true;assert(not Packs.save(store,'kenney'))
        assert(Packs.inspect(store).mode=='onebit')
    end)
end
