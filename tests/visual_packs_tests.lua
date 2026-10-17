local Packs=require('src.visual_packs')
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
        assert(Packs.inspect(store).mode=='classic')
        assert(Packs.save(store,'classic'))
        assert(Packs.save(store,'kenney'))
        local found=Packs.inspect(store)
        assert(found.status=='loaded' and found.mode=='kenney')
        store[Packs.files[2]]='truncated'
        found=Packs.inspect(store)
        assert(found.status=='recovered' and found.mode=='classic')
        store[Packs.files[1]]='bad'
        found=Packs.inspect(store)
        assert(found.status=='corrupt' and found.mode=='classic')
    end)
    test('sprite-pack settings reject bad choices and failed writes',function()
        local store=memory()
        assert(Packs.save(store,'kenney'))
        assert(not Packs.save(store,'unsafe'))
        store.fail=true
        assert(not Packs.save(store,'kenney'))
        assert(Packs.inspect(store).mode=='kenney')
        store.fail=false;store.truncate=true
        assert(not Packs.save(store,'classic'))
        assert(Packs.inspect(store).mode=='kenney')
        store.truncate=false
        -- A well-formed newer record with a future format must not roll back
        -- to the older valid preference or be overwritten.
        local future='roag-visual-pack/9\n2\nclassic\n'
        store[Packs.files[2]]=future..Atlas.checksum(future)..'\n'
        local found=Packs.inspect(store)
        assert(found.status=='incompatible' and found.mode=='classic')
        assert(not Packs.save(store,'classic'))
        assert(store[Packs.files[2]]==future..Atlas.checksum(future)..'\n')
    end)
    test('Classic, Kenney mix and standalone 1-Bit select only their intended sources',function()
        assert(#Packs.order==3 and Packs.order[1]=='classic' and Packs.order[2]=='kenney'
            and Packs.order[3]=='onebit')
        for _,mode in ipairs({'automatic','generated','published','unsafe'})do
            assert(not Packs.valid(mode) and not Packs.save(memory(),mode))
            assert(not pcall(Packs.build,mode))
        end
        local native={resolve=function(_,id)
            if id=='core:actor/adventurer' then return {source='kenney'} end
        end}
        local calls=0
        local function load_native(read,mode)
            calls=calls+1;assert(mode=='kenney' or mode=='onebit');return native,'loaded'
        end
        local function read()error('retired visual sources must not be read')end
        local function image()error('unexpected loose image load')end
        local state=Game.new_stage(37);local before=Game.snapshot(state)
        local classic=Packs.build('classic',read,image,load_native)
        assert(classic.status=='absent' and not classic:resolve('core:actor/adventurer'))
        local kenney=Packs.build('kenney',read,image,load_native)
        assert(kenney:resolve('core:actor/adventurer').source=='kenney' and calls==1)
        local onebit=Packs.build('onebit',read,image,load_native)
        assert(onebit:resolve('core:actor/adventurer') and calls==2)
        assert(not Packs.uses_published_theme('classic') and not Packs.uses_published_theme('kenney'))
        local missing,status=Packs.build('kenney',read,image,function()return nil,'absent'end)
        assert(status=='absent' and not missing:resolve('core:actor/adventurer'))
        equal(before,Game.snapshot(state))
    end)
    test('retired checked sprite choices fall back without erasing run or settings data',function()
        for _,mode in ipairs({'automatic','generated','published'})do
            local store=memory()
            local body=Packs.format..'\n9\n'..mode..'\n'
            store[Packs.files[1]]=body..Atlas.checksum(body)..'\n'
            local found=Packs.inspect(store)
            assert(found.status=='retired' and found.mode=='classic')
            assert(store[Packs.files[1]]==body..Atlas.checksum(body)..'\n')
            assert(Packs.save(store,'kenney') and Packs.inspect(store).mode=='kenney')
        end
    end)
end
