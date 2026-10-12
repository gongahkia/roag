local W=require('src.studio_world')
local Store=require('src.studio_world_store')
local Runtime=require('src.studio_world_runtime')
local Generation=require('src.generation')
local Game=require('src.game')
local Rooms=require('src.rooms')
local VisualAssets=require('src.visual_assets')
local Project=require('src.studio_project')
local Biomes=require('src.biomes')
local Editor=require('src.studio_world_editor')

local function fixture(role)
    local pack=W.new('e03_fixture')
    local room=W.new_room('studio:room/test_gallery','Test gallery',9,7)
    if role=='start' then
        room.roles={'start'};room.weight=20
        room.markers.enemy={};room.markers.player={{x=5,y=4}}
    end
    pack.rooms[1]=room
    return pack,room
end
local function memory()
    local files={};local fail=false
    local io={}
    function io:read(path) return files[path] end
    function io:write(path,bytes)
        if fail then files[path]=bytes:sub(1,math.floor(#bytes/2));return true end
        files[path]=bytes;return true
    end
    function io:mkdir() return true end
    function io:list() return {} end
    return io,files,function(v) fail=v end
end
return function(test,equal)
    test('E03 inert room source, masks, semantic tiles and deterministic encoding',function()
        local pack,room=fixture()
        W.paint_tile(pack,room,3,3,'studio:tile/wall')
        assert(room.rows[3]:sub(3,3)=='#')
        W.set_mask(room,'editable',4,3,true)
        W.set_mask(room,'protected',6,3,true)
        local encoded=W.encode(pack)
        local decoded=W.decode(encoded)
        assert(encoded==W.encode(decoded))
        equal(pack,decoded)
        assert(pack.tiles[2].blocks_movement and pack.tiles[2].blocks_sight and
            pack.tiles[2].blocks_projectile)
        assert(pack.tiles[3].blocks_movement and not pack.tiles[3].blocks_sight and
            not pack.tiles[3].blocks_projectile)
        pack.tiles[3].blocks_projectile=true
        assert(not pcall(W.validate,pack))
    end)
    test('E03 room validation reports disconnected sockets and missing assets',function()
        local pack,room=fixture()
        room.rows[3]='#########'
        local ok,err=pcall(W.validate,pack)
        assert(not ok and tostring(err):find('disconnected socket',1,true))
        room.rows[3]='#.......#'
        pack.tiles[1].asset_id='studio:tile/moss'
        ok,err=pcall(W.validate,pack,{})
        assert(not ok and tostring(err):find('missing tile visual asset',1,true))
        local asset=Project.asset('studio:tile/moss','Moss','tile')
        assert(W.validate(pack,{asset}))
    end)
    test('E03 rotations mirror sockets, markers, masks and authored visuals',function()
        local pack,room=fixture()
        W.set_mask(room,'protected',3,3,true)
        W.paint_tile(pack,room,4,3,'studio:tile/floor')
        pack.tiles[1].asset_id='studio:tile/moss'
        assert(W.validate(pack))
        local def=W.runtime_room(room,pack)
        local moved=Rooms.transform(def,1,true)
        assert(moved.width==7 and moved.height==9)
        assert(#moved.sockets==4 and #moved.markers.enemy==1)
        assert(#moved.editable==0)
        assert(#moved.studio_protected==1)
        local found=false
        for _,id in pairs(moved.studio_visual_cells) do
            if id=='studio:tile/moss' then found=true end
        end
        assert(found)
    end)
    test('E03 void footprint and decoration remain independent of collision',function()
        local pack,room=fixture('start')
        W.set_cell(room,2,3,' ')
        W.set_visual(room,3,3,'studio:tile/decor')
        local asset=Project.asset('studio:tile/decor','Decor','tile')
        assert(W.validate(pack,{asset}))
        local map=Generation.generate(1,{studio_pack=pack})
        assert(map.rooms[1].template_id==room.id)
        local placed=map.rooms[1]
        local transformed=Rooms.transform(map.studio_templates[room.id],
            placed.turns,placed.mirror)
        local voidx,voidy
        for y,row in ipairs(transformed.rows) do
            local x=row:find(' ',1,true)
            if x then voidx,voidy=placed.x+x-1,placed.y+y-1 end
        end
        assert(voidx and voidy)
        local protected=false
        for _,p in ipairs(map.protected_cells) do
            if p.x==voidx and p.y==voidy then protected=true end
        end
        assert(not protected,'void cell became protected room geometry')
        assert(map.rows[voidy]:sub(voidx,voidx)=='#')
        local visual=false
        for _,id in pairs(map.studio_visuals or {}) do
            if id=='studio:tile/decor' then visual=true end
        end
        assert(visual)
    end)
    test('E03 one authored room can enter each existing biome pool',function()
        local pack,room=fixture('start')
        room.biomes={unpack(Biomes.order)}
        local chosen=0
        for _,biome_id in ipairs(Biomes.order) do
            local found=false
            for seed=1,20 do
                local map=Generation.generate(seed,{biome_id=biome_id,
                    studio_pack=pack})
                assert(Generation.validate(map))
                if map.rooms[1].template_id==room.id then found=true;break end
            end
            assert(found,'authored room absent from '..biome_id)
            chosen=chosen+1
        end
        assert(chosen==4)
    end)
    test('E03 room history groups fast pointer drag and survives zoom/pan mapping',function()
        local old_love=_G.love
        _G.love={graphics={getDimensions=function()return 1024,640 end},
            keyboard={isDown=function()return false end}}
        local ok,err=pcall(function()
            local pack=fixture()
            local editor=Editor.new(pack,{})
            local ox,oy=editor:origin()
            local function center(x,y)
                return ox+(x-0.5)*editor.zoom,oy+(y-0.5)*editor.zoom
            end
            editor.tool='wall'
            local x1,y1=center(3,3);local x2,y2=center(5,3)
            assert(editor:mousepressed(x1,y1,1))
            editor:mousemoved(x2,y2,x2-x1,y2-y1);editor:mousereleased()
            for x=3,5 do assert(editor:room().rows[3]:sub(x,x)=='#') end
            assert(#editor.undo_stack==1 and editor:dirty())
            assert(editor:undo() and editor:room().rows[3]:sub(4,4)=='.')
            assert(editor:redo() and editor:room().rows[3]:sub(4,4)=='#')
            editor:saved_now();assert(not editor:dirty())
            editor:wheelmoved(0,1)
            local nx,ny=editor:origin()
            assert(select(1,editor:cell_at(nx+editor.zoom*2.5,
                ny+editor.zoom*2.5))==3)
            editor:mousepressed(x1,y1,3);editor:mousemoved(x1+20,y1+5,20,5)
            editor:mousereleased();assert(editor.panx==20 and editor.pany==5)
        end)
        _G.love=old_love;assert(ok,err)
    end)
    test('E03 world source two generations recover torn newest write',function()
        local io,files,failing=memory()
        local store=Store.new(io)
        local pack=fixture()
        assert(store:save(pack,{}))
        pack.rooms[1].name='Second'
        assert(store:save(pack,{}))
        pack.rooms[1].name='Third'
        failing(true)
        local ok=store:save(pack,{})
        assert(not ok)
        failing(false)
        local recovered,reason=store:open(pack.project_id,{})
        assert(recovered and recovered.rooms[1].name=='Second')
        assert(reason:find('recovered',1,true))
        files['studio/projects/e03_fixture/world-a.txt']='truncated'
        files['studio/projects/e03_fixture/world-b.txt']='truncated'
        assert(not store:open(pack.project_id,{}))
    end)
    test('E03 valid newer incompatible world source never rolls back silently',function()
        local io,files=memory();local store=Store.new(io)
        local pack=fixture();assert(store:save(pack,{}))
        local payload=W.encode(pack):gsub('ROEG%-STUDIO%-WORLD/1',
            'ROEG-STUDIO-WORLD/2')
        local a,b=1,0
        for i=1,#payload do a=(a+payload:byte(i))%65521;b=(b+a)%65521 end
        files['studio/projects/e03_fixture/world-b.txt']=table.concat({
            'ROEG-STUDIO-WORLD-RECORD/1',2,#payload,
            string.format('%08x',b*65536+a)},'\n')..'\n'..payload
        local loaded,reason=store:open('e03_fixture',{})
        assert(not loaded and reason:find('incompatible',1,true))
    end)
    test('E03 published generations recover corruption and reject incompatible newest',function()
        local pack=fixture();local files={}
        local function read(path)return files[path] end
        files[Runtime.paths[1]]=Runtime.record(1,pack)
        pack.rooms[1].name='Newer'
        files[Runtime.paths[2]]=Runtime.record(2,pack)
        assert(Runtime.inspect(read).rooms[1].name=='Newer')
        files[Runtime.paths[2]]=files[Runtime.paths[2]]:sub(1,-3)
        local loaded,status=Runtime.inspect(read)
        assert(loaded and status:find('recovered',1,true))
        -- Repack a valid envelope with a newer inert but incompatible payload.
        local payload=W.encode(pack):gsub('ROEG%-STUDIO%-WORLD/1','ROEG-STUDIO-WORLD/2')
        local function checksum(bytes)
            local a,b=1,0;for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
            return string.format('%08x',b*65536+a)
        end
        files[Runtime.paths[2]]=table.concat({'ROEG-STUDIO-PUBLISHED-WORLD/1',3,
            #payload,checksum(payload)},'\n')..'\n'..payload
        assert(not Runtime.inspect(read))
    end)
    test('E03 authored start room enters real floor and 100 seeds stay valid',function()
        local pack,room=fixture('start')
        W.paint_tile(pack,room,3,3,'studio:tile/floor')
        pack.tiles[1].asset_id='studio:tile/moss'
        local hits,retries,repairs,fallbacks=0,0,0,0
        for seed=1,100 do
            local map=Generation.generate(seed,{studio_pack=pack})
            local valid,errors=Generation.validate(map)
            assert(valid,errors[1] and errors[1].kind)
            assert(#map.rooms>=8 and #map.rooms<=12)
            assert(#map.placements.chests==5 and #map.placements.secrets==2)
            if next(map.studio_templates) then hits=hits+1 end
            retries=retries+#map.diagnostics.attempts-1
            repairs=repairs+#map.repairs
            fallbacks=fallbacks+(map.diagnostics.fallback and 1 or 0)
        end
        assert(hits>=90 and fallbacks==0,'authored start room selection failed')
        print(('E03 CORPUS overgrown: 100 valid, %d authored, %d retries, %d repairs, %d fallbacks'):
            format(hits,retries,repairs,fallbacks))
    end)
    test('E03 concrete authored floor restores only with matching published pack',function()
        local pack=fixture('start')
        local ok,err=pcall(function()
            Generation.install_studio_pack(pack)
            local state=Game.new_stage(1)
            assert(state.generated_map.rooms[1].template_id=='studio:room/test_gallery')
            local snapshot=Game.snapshot(state)
            equal(snapshot,Game.snapshot(Game.restore(snapshot)))
            Generation.install_studio_pack(nil)
            local restored,reason=pcall(Game.restore,snapshot)
            assert(not restored and tostring(reason):find('published world pack',1,true))
        end)
        Generation.install_studio_pack(nil)
        assert(ok,err)
    end)
    test('E03 same seed and saved authored-floor continuation replay identically',function()
        local pack=fixture('start')
        local map_a=Generation.generate(17,{studio_pack=pack})
        local map_b=Generation.generate(17,{studio_pack=pack})
        equal(map_a,map_b)
        local ok,err=pcall(function()
            Generation.install_studio_pack(pack)
            local original=Game.new_stage(1)
            local restored=Game.restore(Game.snapshot(original))
            for _=1,3 do
                local a,_,events_a=Game.submit(original,{kind='wait'})
                local b,_,events_b=Game.submit(restored,{kind='wait'})
                assert(a==b)
                equal(events_a,events_b)
            end
            equal(Game.snapshot(original),Game.snapshot(restored))
        end)
        Generation.install_studio_pack(nil)
        assert(ok,err)
    end)
    test('E03 changing optional pack rejects active save and leaves combat RNG isolated',function()
        local Persistence=require('src.persistence')
        local pack=fixture('start')
        local ok,err=pcall(function()
            Generation.install_studio_pack(nil)
            local plain=Game.new_stage(7)
            Generation.install_studio_pack(pack)
            local authored=Game.new_stage(7)
            equal(plain.rng,authored.rng)
            local io=memory()
            local service=Persistence.new(io)
            assert(service:save(authored))
            Generation.install_studio_pack(nil)
            local inspection=service:inspect()
            assert(inspection.status=='incompatible' and
                inspection.reason:find('published world pack',1,true))
        end)
        Generation.install_studio_pack(nil)
        assert(ok,err)
    end)
    test('E03 custom tile IDs and semantic changes update painted source cells',function()
        local pack,room=fixture()
        local tile=W.clone(pack.tiles[1]);tile.id='studio:tile/custom_moss'
        tile.name='Custom Moss';pack.tiles[#pack.tiles+1]=tile
        W.paint_tile(pack,room,3,3,tile.id)
        assert(room.tile_cells['3,3']==tile.id and room.rows[3]:sub(3,3)=='.')
        tile.symbol='~';tile.blocks_movement=true
        W.set_cell(room,3,3,'~')
        assert(W.validate(pack))
        local encoded=W.encode(pack)
        assert(W.decode(encoded).rooms[1].tile_cells['3,3']==tile.id)
    end)
    test('E03 optional visual asset ID resolves and missing binding falls back',function()
        local project=Project.new('e03_fixture','E03 Fixture')
        local asset=Project.asset('studio:tile/moss','Moss','tile')
        asset.source='sources/moss.png';asset.format='png';asset.width=16;asset.height=16
        project.assets[1]=asset
        local manifest=Project.encode(project)
        local f=assert(io.open('examples/studio/adventurer.png','rb'))
        local fake_png=f:read('*a');f:close()
        local resolver=VisualAssets.new(function(path)
            if path=='assets/studio/manifest.txt' then return manifest end
            if path=='assets/studio/sources/moss.png' then return fake_png end
        end,function() return {mock=true} end)
        assert(resolver:get_id('studio:tile/moss').mock)
        assert(resolver:get_id('studio:tile/missing')==nil)
    end)
end
