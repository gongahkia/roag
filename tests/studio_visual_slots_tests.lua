local Slots=require('src.visual_slots')
local Project=require('src.studio_project')
local PNG=require('src.studio_png')
local Publish=require('src.studio_publish')
local VisualAssets=require('src.visual_assets')
return function(test)
    test('Studio visual bindings accept actor states, boon art and terrain slots',function()
        local p=Project.new('visuals','Visuals')
        local asset=Project.asset('studio:actor/attack','Attack','actor')
        asset.binding='core:actor/adventurer/attack'
        asset.source='sources/attack.png';asset.format='png'
        asset.width=2;asset.height=2
        p.assets[1]=asset
        assert(Project.validate(p))
        asset.binding='core:enemy/mossbound_guard/hurt';assert(Project.validate(p))
        asset.binding='core:boon/storm_conductor/detail';assert(Project.validate(p))
        asset.binding='core:terrain/overgrown_ruins/wall/corner_nw';assert(Project.validate(p))
        asset.binding='core:background/fungal_hollows';assert(Project.validate(p))
        asset.binding='core:enemy/nonexistent/hurt'
        assert(not pcall(Project.validate,p))
    end)
    test('Presentation poses use current event and damage wins a shared beat',function()
        local id='enemy_7'
        assert(Slots.pose(id,{kind='WindupStarted',source_id=id})=='attack')
        assert(Slots.pose(id,{kind='AttackPerformed',source_id=id})=='attack')
        assert(Slots.pose(id,{kind='DamageTaken',target_id=id,actual_loss=2})=='hurt')
        assert(Slots.pose(id,{kind='DamageTaken',target_id=id,actual_loss=0})==nil)
        assert(Slots.pose(id,nil,{{kind='AttackPerformed',source_id=id},
            {kind='DamageTaken',target_id=id,actual_loss=1}})=='hurt')
    end)
    test('Terrain classifier distinguishes centers, edges and both corner types',function()
        local function shape(rows,x,y)
            return Slots.terrain_shape({rows=rows},x,y,'wall')
        end
        assert(shape({'###','###','###'},2,2)=='center')
        assert(shape({'...','###','###'},2,2)=='edge_n')
        assert(shape({'....','..##','..##'},3,2)=='corner_nw')
        assert(shape({'.##','###','###'},2,2)=='inner_corner_nw')
        assert(shape({'...','###','...'},2,2)=='horizontal')
        local bindings=Slots.terrain_bindings('core:biome/overgrown_ruins',
            'wall','corner_nw')
        assert(bindings[1]=='core:terrain/overgrown_ruins/wall/corner_nw')
        assert(bindings[2]=='core:terrain/overgrown_ruins/wall/corner')
        assert(bindings[#bindings]=='core:terrain/overgrown_ruins/wall/base')
    end)
    test('Published state sprite resolves by binding without simulation changes',function()
        local p=Project.new('visuals','Visuals')
        local a=Project.asset('studio:actor/attack','Attack','actor')
        a.binding='core:actor/adventurer/attack'
        a.source='sources/attack.png';a.format='png';a.width=2;a.height=2
        p.assets[1]=a
        local png=PNG.encode(2,2,string.rep(string.char(10,20,30,255),4))
        local plan=Publish.plan(p,function(path)
            return path==a.source and png or nil
        end,function()return nil end)
        local files={}
        for _,file in ipairs(plan) do files[file.path]=file.bytes end
        local visuals=VisualAssets.new(function(path)return files[path]end,
            function(_,asset)
                return {getWidth=function()return asset.width end,
                    getHeight=function()return asset.height end}
            end)
        assert(visuals.status=='loaded')
        assert(visuals:resolve('core:actor/adventurer/attack'))
        assert(not visuals:resolve('core:actor/adventurer/hurt'))
    end)
end
