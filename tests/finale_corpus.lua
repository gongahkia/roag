-- Assisted handoffs validate 100 complete three-biome route combinations.
-- Boss and arena geometry are real; elite kills are prepared fixtures.
local Game=require('src.game')
local Content=require('src.content')
local Generation=require('src.generation')
local Finale=require('src.finale')
local Scheduler=require('src.scheduler')
local Visibility=require('src.visibility')
local directions={{0,-1},{1,0},{0,1},{-1,0}}
local failures,retries,repairs,fallbacks=0,0,0,0
local start=os.clock()
local function approach(state,object)
    local p=Game.player(state)
    local map=Content.map_for(state)
    for _,d in ipairs(directions) do
        local x,y=object.position.x+d[1],object.position.y+d[2]
        local free=Content.walkable(map,x,y)
        for _,actor in ipairs(state.entities) do
            if actor.id~=p.id and actor.blocks_movement
                and actor.position.x==x and actor.position.y==y then free=false end
        end
        if free then
            p.position.x,p.position.y=x,y
            Visibility.recompute(state)
            return -d[1],-d[2]
        end
    end
    error('no cardinal approach')
end
for seed=1,100 do
    local ok,reason=pcall(function()
        local state=Game.new_stage(seed)
        local first=seed%2+1
        local second=math.floor(seed/2)%2+1
        for index=1,3 do
            for _,actor in ipairs(state.entities) do
                if actor.controller=='ai' then Scheduler.remove_for_actor(state,actor.id) end
            end
            local elite=assert(Game.entity(state,state.stage.elite_id))
            local player=Game.player(state)
            player.max_hp,player.hp,player.damage=200,200,100
            local dx,dy=approach(state,elite)
            assert(Game.submit(state,{kind='basic_attack',dx=dx,dy=dy}))
            local exit=assert(Game.entity(state,state.stage.exit_id))
            approach(state,exit)
            assert(Game.submit(state,{kind='choose_route',exit_id=exit.id,
                route_index=index==1 and first or index==2 and second or 1}))
        end
        Finale.validate(state)
        local visited={}
        for _,frozen in ipairs(state.retired_stages) do
            local map=frozen.generated_map
            assert(not visited[map.biome_id]);visited[map.biome_id]=true
            local valid,errors=Generation.validate(map,frozen.secrets)
            assert(valid,errors[1] and errors[1].kind or 'invalid generated floor')
            retries=retries+#map.diagnostics.attempts-1
            repairs=repairs+#(map.repairs or {})
            if map.diagnostics.fallback then fallbacks=fallbacks+1 end
        end
        assert(#state.retired_stages==3 and state.final_map
            and Content.walkable(state.final_map,Finale.map.start_x,Finale.map.start_y)
            and Content.walkable(state.final_map,Finale.map.boss_x,Finale.map.boss_y))
        local boss=Game.entity(state,state.finale.boss_id)
        assert(boss and boss.position.floor_id==state.active_floor_id)
        Game.restore(Game.snapshot(state))
    end)
    if not ok then
        failures=failures+1
        io.stderr:write(('seed %d: %s\n'):format(seed,tostring(reason)))
    end
end
print(('FINALE CORPUS seeds=100 failures=%d retries=%d repairs=%d fallbacks=%d cpu_s=%.3f'):format(
    failures,retries,repairs,fallbacks,os.clock()-start))
if failures>0 then os.exit(1) end
