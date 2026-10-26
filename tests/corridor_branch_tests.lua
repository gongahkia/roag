local Game=require('src.game')
local Review=require('src.review_features')
local Rooms=require('src.rooms')
local Biomes=require('src.biomes')
local Content=require('src.content')

return function(test,equal)
    test('approved room pool and joined breakable cloister are live',function()
        assert(#Rooms.order==21)
        local count=0
        for _,id in ipairs(Biomes.order) do
            local biome=Biomes.get(id)
            for _,base in ipairs(Rooms.order) do
                local room=Rooms.definitions[biome.room_prefix..
                    base:match('[^/]+$')]
                assert(room,'missing approved biome room')
                if room.structures then
                    assert(#room.structures==3)
                    count=count+1
                end
            end
        end
        assert(count==105)
        local exits={['5,8']=true,['17,8']=true,['11,12']=true,
            ['7,4']=true,['15,4']=true}
        for _,id in ipairs(Biomes.order) do
            local biome=Biomes.get(id)
            local cloister=Rooms.definitions[biome.room_prefix..'broken_cloister']
            for y=4,12 do
                for x=5,17 do
                    if y==4 or y==12 or x==5 or x==17 then
                        local tile=cloister.rows[y]:sub(x,x)
                        local position=x..','..y
                        assert(tile==(position=='11,4' and '.' or
                            exits[position] and 'B' or '#'),
                            'cloister ring breached in '..id..' at '..position)
                    end
                end
            end
        end
    end)

    test('all five sidequests progress, reward once, and survive restore',function()
        local found,found_count={},0
        for seed=1,80 do
            if found_count==5 then break end
            local state=Game.new(seed,Biomes.get(Biomes.order[1]).map_id)
            local quest=state.generated_map.corridor_quests[1]
            if quest and not found[quest.kind] then
                found[quest.kind]=state;found_count=found_count+1
            end
        end
        assert(found_count==5,'a reviewed branch type never appeared')
        for _,kind in ipairs({'spur','braid','switchback','hub','false_end'}) do
            local state=assert(found[kind])
            local quest=state.generated_map.corridor_quests[1]
            local events={}
            local function emit(name,fields)
                events[#events+1]={kind=name,fields=fields}
            end
            local reward,gate
            for _,f in ipairs(state.review_features) do
                if f.quest_id==quest.id and f.kind=='quest_reward' then
                    reward=f
                end
            end
            for _,entity in ipairs(state.entities) do
                if entity.quest_id==quest.id then gate=entity end
            end
            assert(reward and not reward.unlocked and not reward.collected)
            if gate then assert(not gate.open and gate.blocks_movement) end
            for _,f in ipairs(state.review_features) do
                if f.quest_id==quest.id and f.kind~='quest_reward' then
                    Review.on_event(state,{kind='TileEntered',
                        floor_id=f.floor_id,source_id=state.player_id,
                        x=f.x,y=f.y},emit,function() end)
                end
            end
            if kind=='false_end' and reward.target_id then
                assert(not reward.unlocked)
                Review.on_event(state,{kind='EntityKilled',
                    floor_id=reward.floor_id,target_id=reward.target_id},
                    emit,function() end)
            end
            assert(reward.unlocked,kind..' reward stayed locked')
            if gate then assert(gate.open and not gate.blocks_movement) end
            local before=state.currency
            local enter={kind='TileEntered',floor_id=reward.floor_id,
                source_id=state.player_id,x=reward.x,y=reward.y}
            Review.on_event(state,enter,emit,function() end)
            Review.on_event(state,enter,emit,function() end)
            assert(reward.collected and state.currency==before+5)
            assert(Game.restore(Game.snapshot(state)))
        end
    end)

    test('quest branch cells and nodes stay walkable across all biomes',function()
        local Generation=require('src.generation')
        local seen={}
        for _,id in ipairs(Biomes.order) do
            for seed=1,30 do
                local maps=Generation.generate_pair(seed,{biome_id=id})
                assert(Generation.validate_pair(maps))
                for _,map in ipairs(maps) do
                    for _,quest in ipairs(map.corridor_quests or {}) do
                        seen[id]=true
                        for _,node in ipairs(quest.nodes) do
                            assert(Content.walkable(map,node.x,node.y))
                        end
                    end
                end
            end
        end
        for _,id in ipairs(Biomes.order) do
            assert(seen[id],'biome lost all sidequest branches')
        end
    end)

    test('an ordinary move discovers a sidequest clue',function()
        local state,clue
        for seed=1,40 do
            local candidate=Game.new(seed,Biomes.get(Biomes.order[1]).map_id)
            local quest=candidate.generated_map.corridor_quests[1]
            if quest and quest.kind=='spur' then
                state=candidate
                for _,feature in ipairs(state.review_features) do
                    if feature.quest_id==quest.id and
                        feature.kind=='quest_clue' then clue=feature end
                end
                break
            end
        end
        assert(state and clue)
        local moved=false
        for _,direction in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
            local x,y=clue.x-direction[1],clue.y-direction[2]
            local occupied=false
            for _,entity in ipairs(state.entities) do
                if entity.id~=state.player_id and entity.blocks_movement
                    and entity.position.floor_id==state.active_floor_id
                    and entity.position.x==x and entity.position.y==y then
                    occupied=true;break
                end
            end
            if Content.walkable(state.generated_map,x,y) and not occupied then
                local player=Game.player(state)
                player.position.x,player.position.y=x,y
                local ok,_,events=Game.submit(state,{kind='move',
                    dx=direction[1],dy=direction[2]})
                if ok then
                    assert(clue.discovered)
                    local signaled=false
                    for _,event in ipairs(events) do
                        if event.kind=='SidequestClueFound' then
                            signaled=true;break
                        end
                    end
                    assert(signaled,'movement did not emit quest discovery')
                    moved=true;break
                end
            end
        end
        assert(moved,'could not move onto the sidequest clue')
    end)
end
