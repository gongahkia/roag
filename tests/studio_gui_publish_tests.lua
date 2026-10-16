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

    test('one artwork slot replaces another project safely and keeps other art',function()
        local publisher,project,files,source=fixture()
        local PNG=require('src.studio_png')
        local old=Project.new('player_sprite','Earlier artwork')
        local hero=Project.asset('studio:actor/old_hero','Old hero','actor')
        hero.binding='core:actor/adventurer'
        hero.source='sources/old_hero.png';hero.format='png'
        hero.width=1;hero.height=1
        local coin=Project.asset('studio:ui/coin','Coin','ui')
        coin.binding='core:drop/currency'
        coin.source='sources/coin.png';coin.format='png'
        coin.width=1;coin.height=1
        old.assets={hero,coin}
        files['assets/studio/'..hero.source]=PNG.encode(1,1,string.char(0,0,0,255))
        files['assets/studio/'..coin.source]=PNG.encode(1,1,string.char(255,180,0,255))
        files['assets/studio/manifest.txt']=Project.encode(old)
        local old_manifest=files['assets/studio/manifest.txt']
        local old_coin=files['assets/studio/'..coin.source]
        local plan=publisher:plan_asset(project,project.assets[1].id,source)
        assert(not plan.blocked and plan.owner=='player_sprite')
        assert(plan.replaced_asset_id==hero.id)
        assert(plan.counts.create==1 and plan.counts.overwrite==1)
        assert(files['assets/studio/manifest.txt']==old_manifest,
            'preview must be read-only')
        local backed_up
        assert(publisher:apply_asset(project,project.assets[1].id,source,plan,
            function(bytes) backed_up=bytes;return '/isolated/backup' end))
        assert(backed_up==old_manifest)
        local merged=Project.decode(files['assets/studio/manifest.txt'])
        assert(merged.id=='player_sprite' and #merged.assets==2)
        local bindings={}
        for _,asset in ipairs(merged.assets) do bindings[asset.binding]=asset end
        assert(bindings['core:actor/adventurer'].id==project.assets[1].id)
        assert(bindings['core:drop/currency'].id==coin.id)
        assert(files['assets/studio/'..coin.source]==old_coin)
        assert(files['assets/studio/'..hero.source],
            'old source bytes remain available for the older project')
        local again=publisher:plan_asset(project,project.assets[1].id,source)
        assert(again.counts.unchanged==2 and again.counts.overwrite==0)
    end)

    test('one-slot publish rejects stale plans, invalid owners and ID collisions',function()
        local publisher,project,files,source=fixture()
        local first=publisher:plan_asset(project,project.assets[1].id,source)
        files['assets/studio/manifest.txt']=Project.encode(
            Project.new('external','External'))
        assert(not publisher:apply_asset(project,project.assets[1].id,source,
            first,function() error('stale plan must not back up') end))
        local other=Project.new('external','External')
        local occupied=Project.asset(project.assets[1].id,'Other slot','actor')
        occupied.binding='core:actor/ranger';occupied.source='sources/other.png'
        occupied.format='png';occupied.width=1;occupied.height=1
        other.assets[1]=occupied
        files['assets/studio/'..occupied.source]=require('src.studio_png').encode(
            1,1,string.char(9,8,7,255))
        files['assets/studio/manifest.txt']=Project.encode(other)
        local blocked=publisher:plan_asset(project,project.assets[1].id,source)
        assert(blocked.blocked and blocked.blocked:find('Asset ID',1,true))
        files['assets/studio/'..occupied.source]=nil
        local incomplete=publisher:plan_asset(project,project.assets[1].id,source)
        assert(incomplete.blocked and incomplete.blocked:find('incomplete',1,true))
        project.assets[1].binding=''
        local ok=pcall(function()
            publisher:plan_asset(project,project.assets[1].id,source)
        end)
        assert(not ok,'unbound art must not publish to a game slot')
    end)

    test('failed one-slot image write leaves the previous live manifest intact',function()
        local publisher,project,files,source=fixture()
        local first=publisher:plan(project,source)
        assert(publisher:apply(project,source,first,false))
        local before=files['assets/studio/manifest.txt']
        local changed=require('src.studio_png').encode(1,1,
            string.char(240,10,20,255))
        local function new_source(path)
            return path==project.assets[1].source and changed or nil
        end
        local preview=publisher:plan_asset(project,project.assets[1].id,new_source)
        assert(preview.counts.create==1 and preview.counts.overwrite==1)
        publisher.fs.write=function(_,path,bytes)
            if path:match('/sources/') then error('simulated image write failure') end
            files[path]=bytes;return true
        end
        local ok=publisher:apply_asset(project,project.assets[1].id,
            new_source,preview,function() return '/isolated/backup' end)
        assert(not ok and files['assets/studio/manifest.txt']==before)
    end)
end
