local Packs=require('src.visual_packs')
local Project=require('src.studio_project')
local Game=require('src.game')
local Atlas=require('src.studio_atlas')

return function(test,equal)
    local function memory()
        local store={fail=false,truncate=false}
        function store:read(name) return self[name] end
        function store:write(name,bytes)
            if self.fail then return false end
            self[name]=self.truncate and bytes:sub(1,math.floor(#bytes/2)) or bytes
            return true
        end
        return store
    end
    test('sprite-pack preference survives reopen and corrupt newest recovery',function()
        local store=memory()
        assert(Packs.inspect(store).mode=='automatic')
        assert(Packs.save(store,'classic'))
        assert(Packs.save(store,'generated'))
        local found=Packs.inspect(store)
        assert(found.status=='loaded' and found.mode=='generated')
        store[Packs.files[2]]='truncated'
        found=Packs.inspect(store)
        assert(found.status=='recovered' and found.mode=='classic')
        store[Packs.files[1]]='bad'
        found=Packs.inspect(store)
        assert(found.status=='corrupt' and found.mode=='automatic')
    end)
    test('sprite-pack settings reject bad choices and failed writes',function()
        local store=memory()
        assert(Packs.save(store,'published'))
        assert(not Packs.save(store,'unsafe'))
        store.fail=true
        assert(not Packs.save(store,'generated'))
        assert(Packs.inspect(store).mode=='published')
        store.fail=false;store.truncate=true
        assert(not Packs.save(store,'classic'))
        assert(Packs.inspect(store).mode=='published')
        store.truncate=false
        -- A well-formed newer record with a future format must not roll back
        -- to the older valid preference or be overwritten.
        local future='roag-visual-pack/9\n2\nclassic\n'
        store[Packs.files[2]]=future..Atlas.checksum(future)..'\n'
        local found=Packs.inspect(store)
        assert(found.status=='incompatible' and found.mode=='automatic')
        assert(not Packs.save(store,'classic'))
        assert(store[Packs.files[2]]==future..Atlas.checksum(future)..'\n')
    end)
    test('sprite-pack choices select only the intended visual resolver',function()
        local path='assets/generated/builds/e6f7e490b979490e/icons.png'
        local file=assert(io.open(path,'rb'))
        local png=file:read('*a');file:close()
        local project=Project.new('fixture','Fixture')
        local asset=Project.asset('studio:art/hero','Hero','actor')
        asset.binding='core:actor/adventurer'
        asset.source='sources/hero.png';asset.format='png'
        asset.width=128;asset.height=128
        project.assets[1]=asset
        local manifest=Project.encode(project)
        local function read(name)
            if name=='assets/studio/manifest.txt' then return manifest end
            if name=='assets/studio/sources/hero.png' then return png end
        end
        local function image() return {getWidth=function()return 128 end,
            getHeight=function()return 128 end} end
        local generated={resolve=function(_,id)
            if id=='core:actor/adventurer' then return {source='generated'} end
        end}
        local calls=0
        local function load_generated()
            calls=calls+1
            return generated,'loaded'
        end
        local state=Game.new_stage(37)
        local before=Game.snapshot(state)
        local classic=Packs.build('classic',read,image,load_generated)
        assert(classic.status=='absent' and
            not classic:resolve('core:actor/adventurer'))
        local four=Packs.build('generated',read,image,load_generated)
        assert(four:resolve('core:actor/adventurer').source=='generated')
        local published=Packs.build('published',read,image,load_generated)
        assert(published:resolve('core:actor/adventurer').source=='loose')
        local automatic=Packs.build('automatic',read,image,load_generated)
        assert(automatic:resolve('core:actor/adventurer').source=='loose')
        assert(calls==2 and Packs.uses_published_theme('automatic') and
            not Packs.uses_published_theme('classic'))
        local missing_generated,status=Packs.build('generated',function()
            return nil end,image,function()return nil,'absent' end)
        assert(status=='absent' and missing_generated.status=='absent' and
            not missing_generated:resolve('core:actor/adventurer'))
        local missing_published=Packs.build('published',function()
            return nil end,image,load_generated)
        assert(missing_published.status=='absent' and
            not missing_published:resolve('core:actor/adventurer'))
        local bundled=Packs.build('automatic',function()return nil end,
            image,load_generated)
        assert(bundled:resolve('core:actor/adventurer').source=='generated',
            'fresh Automatic must select the bundled dungeon pack')
        equal(before,Game.snapshot(state))
    end)
end
