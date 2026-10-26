local Settings=require('src.presentation_settings')
return function(test,equal)
    test('presentation preferences persist independently and reject invalid values',function()
        local disk={}
        function disk:read(name) return self[name] end
        function disk:write(name,bytes) self[name]=bytes;return true end
        local defaults,status=Settings.load(disk)
        assert(status=='default' and defaults.music==80 and defaults.shake)
        defaults.music=30;defaults.font='sans';defaults.shake=false
        assert(Settings.save(disk,defaults))
        local loaded,source=Settings.load(disk)
        assert(source=='loaded');equal(defaults,loaded)
        loaded.music=55.5
        assert(not Settings.save(disk,loaded))
        local intact=Settings.load(disk)
        assert(intact.music==30)
        disk[Settings.file]='corrupt'
        local fallback,reason=Settings.load(disk)
        assert(reason=='invalid' and fallback.music==80)
    end)
end
