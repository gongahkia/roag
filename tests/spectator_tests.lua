local Biomes=require('src.biomes')
local Classes=require('src.classes')
local Game=require('src.game')
local Spectator=require('src.spectator')

return function(test,equal)
    test('spectator creates every biome as an ineligible disposable run',function()
        for _,biome_id in ipairs(Biomes.order) do
            local state=Spectator.new(17,biome_id,Classes.order[1])
            assert(state.meta.eligible==false)
            assert(Game.player(state).invulnerable==true)
            assert(state.generated_map.biome_id==biome_id)
            assert(#state.floor_order==2 and state.clock==0)
        end
    end)
    test('spectator camera pan is bounded and never changes the run',function()
        local state=Spectator.new(17,Biomes.order[1],Classes.order[1])
        local before=Game.snapshot(state)
        local point=Spectator.camera_step(state,{x=1,y=1},-1,-1,true)
        assert(point.x==1 and point.y==1)
        point=Spectator.camera_step(state,point,1,0,true)
        assert(point.x==9 and point.y==1)
        equal(before,Game.snapshot(state))
    end)
    test('spectator god play blocks real enemy attacks without awarding progress',function()
        local state=Spectator.new(17,Biomes.order[1],Classes.order[1])
        local player=Game.player(state)
        Game.spawn_enemy(state,'core:enemy/mossbound_guard',
            player.position.x+1,player.position.y,0)
        local blocked=false
        for _=1,30 do
            local committed,reason,events=Game.submit(state,{kind='wait'})
            assert(committed,reason)
            for _,event in ipairs(events) do
                if event.kind=='DamageBlocked' and event.target_id==player.id then
                    blocked=true
                end
            end
        end
        assert(blocked and player.hp==player.max_hp)
        assert(state.meta.eligible==false and next(state.meta.pending)==nil)
    end)
    test('spectator floor inspection keeps world time and invulnerability',function()
        local state=Spectator.new(17,Biomes.order[1],Classes.order[2])
        local first=state.active_floor_id
        assert(Spectator.switch_floor(state))
        assert(state.active_floor_id~=first and state.active_floor_index==2)
        assert(state.clock==0 and Game.player(state).invulnerable)
        assert(Spectator.switch_floor(state))
        assert(state.active_floor_id==first and state.active_floor_index==1)
        assert(state.clock==0 and state.meta.eligible==false)
        Game.restore(Game.snapshot(state))
    end)
    test('spectator selections wrap without changing class or biome registries',function()
        assert(Spectator.next_index(Biomes.order,#Biomes.order)==1)
        assert(Spectator.next_index(Classes.order,#Classes.order)==1)
    end)
end
