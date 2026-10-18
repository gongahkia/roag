return function(test,equal)
    local Driver=require('tests.expansion_legal_playthrough')
    local Play=require('tests.legal_playthrough')
    local Game=require('src.game')
    local Achievements=require('src.achievements')
    local Classes=require('src.classes')
    test('seed 9 completes six procedural floors and final boss with legal actions',function()
        local run,reason,failed=Driver.attempt(9)
        assert(run,tostring(reason)..' after '..(failed and #failed.trace or 0)..' actions')
        local state=run.state
        assert(state.victory and state.game_over and not Game.entity(state,
            state.victory.boss_id))
        assert(#run.stages==3 and #run.purchases>=1 and #run.trace>500)
        local floors,stages,victories={},{},0
        for _,record in ipairs(run.trace) do
            for _,kind in ipairs(record.event_kinds) do
                if kind=='FloorChanged' then floors[#floors+1]=record.clock end
                if kind=='StageEntered' then stages[#stages+1]=record.clock end
                if kind=='Victory' then victories=victories+1 end
            end
        end
        assert(#floors==3 and #stages==2 and victories==1)
        assert(state.victory.hp>0 and state.victory.world_time>0)
        local replay={state=Game.new_stage(9,{multifloor=true,
            class_id=Classes.order[1],available_boons=Achievements.pool({})}),
            trace={},events={}}
        for index,record in ipairs(run.trace) do
            local ok,why,events=Play.action(replay,record.intent)
            assert(ok==record.ok and why==record.reason and
                replay.state.clock==record.clock and
                #events==#record.event_kinds,'replay action '..index)
            for i,event in ipairs(events) do
                assert(event.kind==record.event_kinds[i],
                    'replay ordered event '..index..':'..i)
            end
        end
        equal(Game.snapshot(replay.state),Game.snapshot(state))
    end)
    test('all four classes can win through six floors with ordinary submitted actions',function()
        for _,case in ipairs({
            {Classes.order[1],9}, {Classes.order[2],9},
            {Classes.order[3],45}, {Classes.order[4],28},
        }) do
            local run,reason,failed=Driver.attempt(case[2],{class_id=case[1]})
            assert(run,('%s seed %d failed after %d actions: %s'):format(
                case[1],case[2],failed and #failed.trace or 0,tostring(reason)))
            assert(run.state.victory and Game.player(run.state).definition_id==case[1])
            assert(#run.stages==3 and #run.purchases>=1 and #run.trace>500)
            assert(run.state.victory.hp>0)
        end
    end)
    test('Sentinel and Alchemist each reach Victory through ordinary submitted actions',function()
        for _,case in ipairs({{Classes.order[5],28},{Classes.order[6],19}}) do
            local run,reason,failed=Driver.attempt(case[2],{class_id=case[1]})
            assert(run,('%s seed %d failed after %d actions: %s'):format(
                case[1],case[2],failed and #failed.trace or 0,tostring(reason)))
            assert(run.state.victory and Game.player(run.state).definition_id==case[1]
                and #run.stages==3 and #run.purchases>=1 and #run.trace>500)
        end
    end)
    test('ordinary legal routes defeat all seven unique biome minibosses',function()
        local cases={
            {'core:biome/overgrown_ruins',9,1,{1,2,1}},
            {'core:biome/fungal_hollows',9,2,{1}},
            {'core:biome/sunken_aqueduct',21,2,{2}},
            {'core:biome/glacial_observatory',21,2,{3}},
            {'core:biome/ashen_bastion',9,3,{1,2}},
            {'core:biome/fractured_archive',9,3,{1,3}},
            {'core:biome/sundered_forge',25,3,{2,3}},
        }
        for _,case in ipairs(cases) do
            local run,reason,failed=Driver.attempt(case[2],{
                routes=case[4],stop_after_stage=case[3]})
            assert(run,('%s seed %d failed after %d actions: %s'):format(
                case[1],case[2],failed and #failed.trace or 0,tostring(reason)))
            local stage=run.stages[#run.stages]
            assert(stage and stage.biome_id==case[1] and stage.hp>0)
            assert(run.state.stage.elite_reward_id and
                not Game.entity(run.state,run.state.stage.elite_id),
                'miniboss still alive after legal elite route: '..case[1])
        end
    end)
end
