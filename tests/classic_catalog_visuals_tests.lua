-- Active-pack contract coverage. Legacy pixel coordinates remain in historical
-- tests; these checks exercise the catalogue actually loaded by classic main.lua.
local Catalog=require('src.catalog_visuals')
local Data=require('src.classic_sprite_data')
local Packs=require('src.visual_packs')
local Game=require('src.game')
local Codec=require('src.persistence')
local Slots=require('src.visual_slots')
local function read(path)
    assert(not path:lower():find('kenney',1,true),'retired Kenney image was read')
    local f=io.open(path,'rb');if not f then return end
    local bytes=f:read('*a');f:close();return bytes
end
local function image(raw,path,meta)
    assert(raw:sub(1,8)=='\137PNG\r\n\26\n','invalid PNG')
    local function n(i)local a,b,c,d=raw:byte(i,i+3);assert(d,'truncated PNG');return ((a*256+b)*256+c)*256+d end
    local w,h=n(17),n(21)
    if meta and meta.catalog_surface then w,h=meta.catalog_surface.width,meta.catalog_surface.height end
    return {path=path,meta=meta,getWidth=function()return w end,getHeight=function()return h end}
end
local function quad(x,y,w,h,pw,ph)
    assert(x>=0 and y>=0 and w>0 and h>0 and x+w<=pw and y+h<=ph,'crop escapes texture')
    return {x=x,y=y,w=w,h=h,pw=pw,ph=ph}
end
local function build(reader,factory)return Catalog.new(reader or read,factory or image,quad)end
local function rejected(reader,factory)
    local okay,value=pcall(build,reader,factory)
    assert(not okay or value==nil,'invalid required artwork was accepted')
end
local function changed_data(mutation)
    return function(path)
        if path=='src/classic_sprite_data.lua' then
            return 'local data=(function()\n'..read(path)..'\nend)()\n'..mutation..'\nreturn data'
        end
        return read(path)
    end
end
local function requirements()
    local list={}
    for id in pairs(Slots.bindings)do
        if id:match('^core:actor/') or id:match('^core:enemy/') or
            id:match('^core:object/') or id:match('^core:chest/') or
            id:match('^core:boon/') or
            (id:match('^core:drop/') and not id:match('/detail$')) or
            id:match('^core:terrain/[^/]+$') or id:match('^core:terrain/.+/base$') or
            id:match('^core:ui/.+_icon$') or id:match('^core:ui/.+_effect$')then
            list[#list+1]=id
        end
    end
    table.sort(list);return list
end
return function(test,equal)
    test('classic catalogue has one fixed active pack and cannot load a retired renderer or preference',function()
        assert(Packs.default=='catalog' and Packs.label=='Urizen catalog')
        for _,key in ipairs({'order','labels','valid','index','save','inspect','files','format'})do assert(Packs[key]==nil,key)end
        for _,style in ipairs({'classic','kenney','onebit','automatic','generated','published','unsafe'})do
            assert(not pcall(Packs.build,function()error('preference read')end,function()end,
                function()return {style=style},'loaded'end))
        end
        local calls=0
        local value,status=Packs.build(function()error('retired published override read')end,function()end,function()
            calls=calls+1;return {style='catalog',resolve=function()return {source='catalog'}end},'loaded'
        end)
        assert(calls==1 and status=='loaded' and value:resolve('core:actor/adventurer').source=='catalog')
    end)
    test('all 599 live semantic roles resolve checked catalogue crops with original identification evidence',function()
        local list=requirements();assert(#list==599 and Data.requirement_count==599 and Data.schema_version==1)
        local count=0;for _ in pairs(Data.bindings)do count=count+1 end;assert(count==#list)
        local renderer,status=build();assert(renderer and status=='loaded' and renderer.style=='catalog')
        for _,id in ipairs(list)do
            local row=assert(Data.bindings[id],id);local source=assert(Data.sources[row.source],row.source)
            assert(type(row.id)=='string' and row.id~='' and row.x>=0 and row.y>=0)
            assert(row.width>0 and row.height>0 and row.x+row.width<=source.width and row.y+row.height<=source.height)
            assert(type(row.rgba_sha256)=='string' and #row.rgba_sha256==64)
            assert(row.identity_confidence and row.identity_basis and row.composition and type(row.subjects)=='table')
            assert(type(row.notes)=='string' and not row.source:lower():find('kenney',1,true))
            local h=assert(renderer:resolve(id),id)
            assert(h.source=='catalog' and h.texture and h.quad and h.width>0 and h.height>0)
        end
        assert(renderer:resolve('unknown:missing')==nil)
    end)
    test('catalogue sources load once and source crops remain static rather than invented animation frames',function()
        local reads,loads={},0
        local renderer=assert(build(function(path)reads[path]=(reads[path]or 0)+1;return read(path)end,function(...)
            loads=loads+1;return image(...)
        end))
        for path in pairs(Data.sources)do assert(reads[path]==1,path)end
        local initial=loads
        for id,row in pairs(Data.bindings)do
            if id:match('/attack$') or id:match('/hurt$')then
                local base=assert(Data.bindings[id:gsub('/[^/]+$','')])
                assert(row.id==base.id and row.source==base.source and row.x==base.x and row.y==base.y,
                    'static pose alias became an inferred animation: '..id)
            end
        end
        for _=1,12 do for id in pairs(Data.bindings)do assert(renderer:resolve(id))end end
        assert(loads==initial,'lookup decoded or allocated another texture')
    end)
    test('missing corrupt and wrong-sized required catalogue sources fail closed without old-pack fallback',function()
        local paths={};for path in pairs(Data.sources)do paths[#paths+1]=path end;table.sort(paths)
        local target=paths[1]
        rejected(function(path)if path==target then return nil end;return read(path)end)
        rejected(function(path)if path==target then return 'not a PNG' end;return read(path)end)
        rejected(read,function(raw,path,meta)
            local result=image(raw,path,meta)
            if path==target and not(meta and meta.catalog_surface)then result.getWidth=function()return 1 end end
            return result
        end)
    end)
    test('catalogue constructor rejects missing roles counterfeit registries and out-of-bounds crops',function()
        rejected(changed_data("data.bindings['core:actor/adventurer']=nil"))
        rejected(changed_data("data.bindings['core:actor/adventurer'].x=100000"))
        rejected(changed_data("data.bindings['core:actor/adventurer'].source='assets/unknown.png'"))
        local at
        for i,id in ipairs(Data.requirements)do if id=='core:actor/adventurer' then at=i end end
        assert(at)
        rejected(changed_data(([[local old='core:actor/adventurer'
data.bindings['core:actor/counterfeit']=data.bindings[old];data.bindings[old]=nil
data.requirements[%d]='core:actor/counterfeit']]):format(at)))
    end)
    test('fresh catalogue construction observes mapping changes without mutating live old resources',function()
        local first=assert(build());local original=first:resolve('core:actor/adventurer')
        local next_renderer=assert(build(changed_data([[data.bindings['core:actor/adventurer']=data.bindings['core:actor/geomancer']
data.bindings_sha256='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa']])) )
        assert(next_renderer.signature~=first.signature)
        assert(next_renderer:resolve('core:actor/adventurer').catalog_id==Data.bindings['core:actor/geomancer'].id)
        assert(first:resolve('core:actor/adventurer')==original)
        assert(original.catalog_id==Data.bindings['core:actor/adventurer'].id)
    end)
    test('catalogue terrain hides unknown neighbors and caches all 256 local neighborhood cases',function()
        local loads=0;local renderer=assert(build(nil,function(...)loads=loads+1;return image(...)end))
        local around={{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},{1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}}
        local solid={rows={'###','###','###'},rooms={},seed=1,floor_id='fixture'}
        local concealed=assert(renderer:terrain(solid,2,2,'wall',function()return true end))
        for mask=0,255 do
            local cells={{'.','.','.'},{'.','#','.'},{'.','.','.'}}
            for _,d in ipairs(around)do if math.floor(mask/d[3])%2==1 then cells[d[2]+2][d[1]+2]='#'end end
            local map={rows={},rooms={},seed=1,floor_id='fixture'}
            for i,row in ipairs(cells)do map.rows[i]=table.concat(row)end
            assert(renderer:terrain(map,2,2,'wall',function()return true end))
            local hidden=assert(renderer:terrain(map,2,2,'wall',function()return false end))
            equal(hidden.quad,concealed.quad);assert(hidden.texture==concealed.texture)
        end
        local warm=loads
        for _=1,25 do assert(renderer:terrain(solid,2,2,'wall'))end
        assert(loads==warm,'warm terrain lookup created another texture')
    end)
    test('catalogue inspection refresh and floor dressing preserve exact gameplay snapshot and RNG',function()
        local world=Game.new_stage(19,{multifloor=true})
        local before=Codec.encode(Game.snapshot(world));local renderer=assert(build())
        for _=1,8 do
            renderer:set_world(world)
            for id in pairs(Data.bindings)do renderer:resolve(id)end
            assert(Codec.encode(Game.snapshot(world))==before)
        end
        assert(renderer.environment and renderer.environment.plan and renderer.environment.plan.props)
        local second=assert(build());second:set_world(world)
        assert(second.signature==renderer.signature and Codec.encode(Game.snapshot(world))==before)
        equal(renderer.environment.plan,second.environment.plan)
    end)
    test('catalogue effects preserve actual hit playback and all fifteen class attack bindings',function()
        local renderer=assert(build());local Presentation=require('src.idea_presentation')
        for _,id in ipairs(require('src.classes').order)do
            for _,kind in ipairs({'basic_attack','sweeping_slash','dash'})do assert(renderer:resolve(Presentation.weapon(id,kind)))end
        end
        local world=Game.new(17);Game.spawn_enemy(world,'core:enemy/mossbound_guard',3,2,100000)
        local ok,why,events=Game.submit(world,{kind='basic_attack',dx=1,dy=0});assert(ok,why)
        local before=Codec.encode(Game.snapshot(world));local hit=false
        for _,event in ipairs(events)do if event.kind=='DamageTaken' then
            assert(renderer:resolve(Presentation.effect(event)));hit=true
        end end
        assert(hit);local Playback=require('src.playback');local playback=Playback.new();Playback.add(playback,events,world.player_id)
        for _=1,60 do Playback.update(playback,.05)end
        assert(Codec.encode(Game.snapshot(world))==before)
    end)
end
