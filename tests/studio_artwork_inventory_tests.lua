local Inventory=require('src.studio_artwork_inventory')
local Project=require('src.studio_project')
local World=require('src.studio_world')
local Theme=require('src.studio_theme')
local Document=require('src.studio_document')
return function(test)
    local function row(entries,key)
        for _,entry in ipairs(entries) do if entry.key==key then return entry end end
        error('missing catalogue entry: '..key)
    end
    test('Studio artwork checklist includes uncreated game, theme and world destinations',function()
        local project=Project.new('roag','ROAG')
        local entries=Inventory.build(project,World.new('roag'),Theme.new('roag'))
        assert(#entries>500 and #project.assets==0,
            'virtual slots must not consume the 256 saved-asset limit')
        assert(row(entries,'core:actor/adventurer').status=='missing')
        assert(Inventory.progress(row(entries,'core:actor/adventurer'))=='empty')
        assert(row(entries,'core:actor/ranger').status=='missing')
        assert(row(entries,'core:actor/arcanist/attack').optional)
        assert(row(entries,'core:enemy/mossbound_guard/attack').optional)
        assert(row(entries,'core:enemy/mycelial_matriarch').group=='Enemies')
        assert(row(entries,'core:boon/storm_conductor/icon').group=='Boons')
        assert(row(entries,'core:terrain/overgrown_ruins/wall/base').group=='Terrain')
        assert(row(entries,'theme:hp_icon').status=='missing')
        assert(row(entries,'theme:hp_icon').binding=='core:ui/hp_icon')
        assert(row(entries,'core:glyph/ascii_065').label=='Uppercase A')
        assert(row(entries,'core:glyph/ascii_097').label=='Lowercase a')
        assert(row(entries,'core:glyph/ascii_065').default_size=='4x8')
        assert(#Inventory.filter(entries,'uppercase','Letters & text',true)==26)
        assert(row(entries,'world:studio:tile/floor').status=='unmapped')
        assert(row(entries,'world:studio:tile/bramble_bed').status=='not added')
        assert(#Inventory.filter(entries,'storm conductor','Boons',true)==2)
        assert(#Inventory.filter(entries,'adventurer','Enemies',false)==0)
    end)
    test('Studio artwork UI and letter slots use normal one-image publishing',function()
        local project=Project.new('roag','ROAG')
        local icon=Project.asset('studio:art/ui/hp_icon','HP','ui_icon')
        icon.binding='core:ui/hp_icon';icon.source='sources/hp.png'
        icon.format='png';icon.width=16;icon.height=16
        local upper=Project.asset('studio:art/glyph/a','A','glyph')
        upper.binding='core:glyph/ascii_065';upper.source='sources/a.png'
        upper.format='png';upper.width=4;upper.height=8
        project.assets={icon,upper}
        assert(Project.validate(project))
        local source={['sources/hp.png']='hp pixels',['sources/a.png']='A pixels'}
        local entries=Inventory.build(project,nil,Theme.new('roag'),
            function(path)return source[path] end)
        assert(row(entries,'theme:hp_icon').status=='drawn, unpublished')
        assert(row(entries,'core:glyph/ascii_065').status=='drawn, unpublished')
        assert(row(entries,'core:glyph/ascii_097').status=='missing')
        assert(row(entries,'theme:hp_icon').asset_index==1)
    end)
    test('Studio artwork status distinguishes draft, drawn, changed and published pixels',function()
        local project=Project.new('roag','ROAG')
        local asset=Project.asset('studio:art/actor/adventurer','Hero','actor')
        asset.binding='core:actor/adventurer'
        project.assets[1]=asset
        local function build(source,published)
            return Inventory.build(project,nil,nil,
                function(path)return source and source[path] end,
                function(path)return published and published[path] end)
        end
        assert(row(build(),'core:actor/adventurer').status=='draft')
        assert(Inventory.progress(row(build(),'core:actor/adventurer'))=='work')
        asset.source='sources/hero.png';asset.format='png';asset.width=2;asset.height=2
        local source={['sources/hero.png']='painted pixels'}
        assert(row(build(source),'core:actor/adventurer').status=='drawn, unpublished')
        local published={['assets/studio/manifest.txt']=Project.encode(project),
            ['assets/studio/sources/hero.png']='painted pixels'}
        assert(row(build(source,published),'core:actor/adventurer').status=='published')
        assert(Inventory.progress(row(build(source,published),'core:actor/adventurer'))=='done')
        source['sources/hero.png']='changed pixels'
        assert(row(build(source,published),'core:actor/adventurer').status==
            'changed since publish')
        source['sources/hero.png']=nil
        assert(row(build(source,published),'core:actor/adventurer').status=='broken source')
        project.assets={}
        local elsewhere=row(build(source,published),'core:actor/adventurer')
        assert(elsewhere.status=='published elsewhere' and
            elsewhere.published_asset.source=='sources/hero.png' and
            Inventory.progress(elsewhere)=='done')
    end)
    test('Studio artwork checklist follows theme and world asset assignments',function()
        local project=Project.new('roag','ROAG')
        local icon=Project.asset('studio:ui/hp','HP icon','ui_icon')
        icon.source='sources/hp.png';icon.format='png';icon.width=16;icon.height=16
        local tile=Project.asset('studio:tile/floor_art','Floor art','tile')
        tile.source='sources/floor.png';tile.format='png';tile.width=16;tile.height=16
        project.assets={icon,tile}
        local world,theme=World.new('roag'),Theme.new('roag')
        world.tiles[1].asset_id=tile.id
        theme.slots.hp_icon=icon.id
        local entries=Inventory.build(project,world,theme)
        assert(row(entries,'theme:hp_icon').status=='drawn, assigned')
        assert(row(entries,'world:studio:tile/floor').status=='drawn, assigned')
        assert(row(entries,'theme:hp_icon').asset_index==1)
        assert(row(entries,'world:studio:tile/floor').asset_index==2)
    end)
    test('Studio artwork checklist keeps a saved empty canvas marked unfinished',function()
        local project=Project.new('roag','ROAG')
        local asset=Project.asset('studio:art/actor/adventurer','Hero','actor')
        asset.binding='core:actor/adventurer';asset.format='png'
        asset.width=2;asset.height=2
        asset.document='sources/hero.rgba';asset.derived='sources/hero.png'
        project.assets[1]=asset
        local doc=Document.new(asset.id,2,2)
        local function status()
            local files={[asset.document]=Document.encode(doc),
                [asset.derived]='derived image bytes'}
            local entries=Inventory.build(project,nil,nil,
                function(path)return files[path] end)
            return row(entries,asset.binding).status
        end
        assert(status()=='blank canvas')
        Document.set(doc,0,0,{200,10,30,255},'replace')
        assert(status()=='drawn, unpublished')
    end)
end
