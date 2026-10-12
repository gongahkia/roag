local Project=require('src.studio_project')
local Publisher=require('src.studio_gui_publish')
local function fixture()
    local files={['main.lua']='game',['conf.lua']='identity',
        ['src/studio_project.lua']='project module'}
    local fs={}
    function fs:read(path) return files[path] end
    function fs:mkdir(path) assert(path=='assets/studio/sources' or
        path=='assets/studio');return true end
    function fs:write(path,bytes) files[path]=bytes;return true end
    local project=Project.new('gui_publish','GUI publish')
    local asset=Project.asset('studio:actor/player','Player','actor')
    asset.binding='core:actor/adventurer';asset.source='sources/player.png'
    asset.width=1;asset.height=1;asset.format='png'
    project.assets[1]=asset
    local png=require('src.studio_png').encode(1,1,string.char(20,40,60,255))
    return Publisher.new('/isolated-checkout',fs),project,files,function(path)
        return path==asset.source and png or nil
    end
end
return function(test)
    test('Studio GUI publish previews, backs up and confirms manifest replacement',function()
        local publisher,project,files,source=fixture()
        local first=publisher:plan(project,source)
        assert(first.counts.create==2 and first.counts.overwrite==0)
        assert(files['assets/studio/manifest.txt']==nil,'preview wrote a manifest')
        assert(publisher:apply(project,source,first,false))
        local original=files['assets/studio/manifest.txt']
        project.name='Revised title'
        local second=publisher:plan(project,source)
        assert(second.counts.overwrite==1 and second.counts.unchanged==1)
        assert(not publisher:apply(project,source,second,false),
            'manifest replacement skipped confirmation')
        local saved
        assert(publisher:apply(project,source,second,true,function(bytes)
            saved=bytes;return '/isolated-save/backup.txt'
        end))
        assert(saved==original and files['assets/studio/manifest.txt']~=original)
    end)
    test('Studio GUI publish refuses changed plans and unrelated published projects',function()
        local publisher,project,files,source=fixture()
        local preview=publisher:plan(project,source)
        files['assets/studio/manifest.txt']='external change'
        assert(not publisher:apply(project,source,preview,false),
            'stale preview was applied')
        local other=Project.new('another_project','Another')
        files['assets/studio/manifest.txt']=Project.encode(other)
        local blocked=publisher:plan(project,source)
        assert(blocked.blocked and blocked.blocked:find('another_project',1,true))
        local before=files['assets/studio/manifest.txt']
        assert(not publisher:apply(project,source,blocked,true,function()
            error('must not back up or write another project')
        end))
        assert(files['assets/studio/manifest.txt']==before)
    end)
end
