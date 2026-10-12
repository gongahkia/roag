return function(test,equal)
    local AI=require("src.ai")
    local Biomes=require("src.biomes")
    local Content=require("src.content")
    local Game=require("src.game")
    local RegularAI=require("src.regular_ai")
    local Scheduler=require("src.scheduler")
    local Visibility=require("src.visibility")

    local function events_of(events,kind)
        local out={}
        for _,event in ipairs(events or {}) do
            if event.kind==kind then out[#out+1]=event end
        end
        return out
    end
    local function trial(id,x,y,px,py)
        local state=Game.new(3,"core:map/test_room")
        local player=Game.player(state)
        player.position.x,player.position.y=px or 9,py or 6
        local enemy=Game.spawn_enemy(state,id,x or 9,y or 8,0)
        Visibility.recompute(state)
        return state,enemy
    end

    test("seven biome elites have unique IDs and two locked footprints",function()
        local distinct={}
        for index,id in ipairs(Biomes.order) do
            local biome=Biomes.get(id)
            assert(not distinct[biome.elite_id])
            distinct[biome.elite_id]=true
            local stage_options=index>1 and {index=2,biome_id=id,
                route_history={(id:gsub("^core:biome/","core:route/"))}} or nil
            local state=Game.new_stage(3,{stage_options=stage_options})
            local elite=assert(Game.entity(state,state.stage.elite_id))
            assert(elite.definition_id==biome.elite_id)
            equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
            if index>1 then
                local p=Game.player(state)
                local found=false
                for _,d in ipairs({{2,0},{-2,0},{0,2},{0,-2},
                    {1,0},{-1,0},{0,1},{0,-1}}) do
                    local x,y=elite.position.x+d[1],elite.position.y+d[2]
                    if Content.walkable(state.generated_map,x,y) then
                        p.position.x,p.position.y=x,y
                        elite.ai_memory.attacks=0
                        local first=AI.choose(state,elite)
                        elite.ai_memory.attacks=1
                        local second=AI.choose(state,elite)
                        if first.kind=="windup" and second.kind=="windup" then
                            assert(first.ability_id~=second.ability_id
                                and #first.cells>0 and #second.cells>0)
                            elite.phase_two=true
                            local phased=AI.choose(state,elite)
                            assert(phased.ability_id==first.ability_id)
                            found=true;break
                        end
                    end
                end
                assert(found,"elite lacked two observable patterns: "..id)
            end
        end
        assert(#Biomes.order==7)
    end)

    test("ten regular roles are distinct and occur in biome pools",function()
        local count,seen=0,{}
        for id,role in pairs(RegularAI.roles) do
            count=count+1
            assert(Content.get_enemy(id).ai=="expansion_regular")
            assert(not seen[role]);seen[role]=true
            local in_pool=false
            for _,biome_id in ipairs(Biomes.order) do
                for _,offered in ipairs(Biomes.get(biome_id).enemy_ids) do
                    if offered==id then in_pool=true end
                end
            end
            assert(in_pool,"unreachable enemy role: "..id)
        end
        assert(count==10)
    end)

    test("regular attack decisions expose positioning, coordination and retaliation",function()
        local state,enemy=trial("core:enemy/glass_charger",7,8,7,5)
        local intent=AI.choose(state,enemy)
        assert(intent.ability_id=="core:ability/glass_charge"
            and intent.advance.max_steps==3 and #intent.cells==3)
        state,enemy=trial("core:enemy/mire_binder")
        intent=AI.choose(state,enemy)
        assert(intent.ability_id=="core:ability/mire_ring")
        for _,cell in ipairs(intent.cells) do
            assert(cell.x~=9 or cell.y~=6,"denial ring must leave locked center safe")
        end
        state,enemy=trial("core:enemy/pack_chorister",9,8,9,7)
        Game.spawn_enemy(state,"core:enemy/mossbound_guard",10,8,0)
        intent=AI.choose(state,enemy)
        assert(intent.ability_id=="core:ability/pack_chorus"
            and intent.damage_bonus==1)
        state,enemy=trial("core:enemy/bastion_bulwark",9,8,9,7)
        enemy.ai_memory.retaliate=true
        intent=AI.choose(state,enemy)
        assert(intent.ability_id=="core:ability/bulwark_retort"
            and intent.clear_retaliate)
        state,enemy=trial("core:enemy/gloom_strider",9,8,9,7)
        assert(AI.choose(state,enemy).kind=="move")
        state,enemy=trial("core:enemy/vault_lurker",9,8,9,6)
        assert(AI.choose(state,enemy).ability_id=="core:ability/vault_ambush")
        assert(not enemy.ai_memory.alerted,"AI decision must not mutate runtime")
    end)

    test("summoner windup, pending save and canonical spawned enemy agree",function()
        local state,enemy=trial("core:enemy/forge_conjurer")
        local ok,_,events=Game.submit(state,{kind="wait"})
        assert(ok and #events_of(events,"WindupStarted")>=1)
        local snapshot=Game.snapshot(state)
        local restored=Game.restore(snapshot)
        local found=false
        for _=1,3 do
            local a,_,out=Game.submit(state,{kind="wait"})
            local b,_,out2=Game.submit(restored,{kind="wait"})
            assert(a and b);equal(out,out2)
            if #events_of(out,"EnemySummoned")>0 then found=true;break end
        end
        assert(found and enemy.ai_memory.summons==1)
        equal(Game.snapshot(state),Game.snapshot(restored))
        local summoned
        for _,entity in ipairs(state.entities) do
            if entity.summoned_by_id==enemy.id then summoned=entity end
        end
        assert(summoned and summoned.definition_id=="core:enemy/ruin_skitter"
            and summoned.faction==enemy.faction)
    end)

    test("support heals wounded allies at locked cells and charger traverses only open path",function()
        local state,healer=trial("core:enemy/cinder_mender")
        local ally=Game.spawn_enemy(state,"core:enemy/mossbound_guard",10,8,0)
        ally.hp=ally.max_hp-4
        Scheduler.remove_for_actor(state,ally.id)
        assert(AI.choose(state,healer).support.kind=="heal")
        local healed=false
        for _=1,4 do
            local ok,_,events=Game.submit(state,{kind="wait"})
            assert(ok)
            if #events_of(events,"Healed")>0 then healed=true;break end
        end
        assert(healed and ally.hp==ally.max_hp-1)
        state=trial("core:enemy/glass_charger",7,8,7,5)
        local moved=false
        for _=1,3 do
            local ok,_,events=Game.submit(state,{kind="wait"})
            assert(ok)
            for _,event in ipairs(events) do
                if event.kind=="Moved" and event.path and #event.path>0 then
                    moved=true
                    assert(event.x==7 and event.y==6)
                end
            end
            if moved then break end
        end
        assert(moved,"charger impact did not move along its locked line")
    end)

    test("artillery friendly fire uses one ordered canonical damage batch",function()
        local state,bombardier=trial("core:enemy/crystal_bombardier",9,8,9,6)
        local ally=Game.spawn_enemy(state,"core:enemy/mossbound_guard",11,6,0)
        Scheduler.remove_for_actor(state,ally.id)
        local batch,player_hit,ally_hit
        for _=1,4 do
            local ok,_,events=Game.submit(state,{kind="wait"})
            assert(ok)
            for index,event in ipairs(events) do
                if event.kind=="DamageBatchResolved"
                    and event.source_id==bombardier.id then
                    batch=event
                    for later=index+1,#events do
                        local hit=events[later]
                        if hit.kind=="DamageTaken" then
                            if hit.target_id==state.player_id then player_hit=true end
                            if hit.target_id==ally.id then ally_hit=true end
                        end
                    end
                end
            end
            if batch then break end
        end
        assert(batch and player_hit and ally_hit
            and #batch.outcomes==2,"artillery friendly fire did not batch")
    end)

    test("retaliator arms only after actual hostile HP loss",function()
        local state=Game.new(9,"core:map/test_room")
        local enemy=Game.spawn_enemy(state,"core:enemy/bastion_bulwark",3,2,0)
        local ok,_,events=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(ok and #events_of(events,"RetaliationReadied")==1
            and enemy.pending_attack
            and enemy.pending_attack.ability_id=="core:ability/bulwark_retort")
        local blocked=Game.new(9,"core:map/test_room")
        local shield=Game.spawn_enemy(blocked,"core:enemy/bastion_bulwark",3,2,0)
        Game.player(blocked).damage=1 -- defense 2, canonical DamageBlocked
        local _,_,out=Game.submit(blocked,{kind="basic_attack",dx=1,dy=0})
        assert(#events_of(out,"DamageBlocked")==1
            and #events_of(out,"RetaliationReadied")==0
            and not shield.ai_memory.retaliate)
    end)

    test("Mossbreaker destroys a real barricade before pursuing",function()
        local state=Game.new(9,"core:map/test_room")
        local function copy(value)
            if type(value)~="table" then return value end
            local out={}
            for key,part in pairs(value) do out[key]=copy(part) end
            return out
        end
        state.generated_map=copy(Content.get_map("core:map/test_room"))
        local map=state.generated_map
        map.rows[8]=map.rows[8]:sub(1,7).."B"..map.rows[8]:sub(9)
        local player=Game.player(state)
        player.position.x,player.position.y=7,8
        local breaker=Game.spawn_enemy(state,"core:enemy/mossbreaker",9,8,0)
        Visibility.recompute(state)
        assert(AI.choose(state,breaker).ability_id=="core:ability/mossbreaker_crack")
        local broken=false
        for _=1,4 do
            local ok,_,events=Game.submit(state,{kind="wait"})
            assert(ok)
            if #events_of(events,"TerrainBroken")>0 then broken=true;break end
        end
        assert(broken and map.rows[8]:sub(8,8)=="."
            and Content.walkable(map,8,8))
    end)

    test("new elite pending windup and phase survive authoritative continuation",function()
        local id=Biomes.order[2]
        local state=Game.new_stage(11,{stage_options={index=2,biome_id=id,
            route_history={"core:route/fungal_hollows"}}})
        local elite=Game.entity(state,state.stage.elite_id)
        for _,actor in ipairs(state.entities) do
            if actor.controller=="ai" then Scheduler.remove_for_actor(state,actor.id) end
        end
        local player=Game.player(state)
        local target,aim
        for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
            local x,y=elite.position.x+d[1],elite.position.y+d[2]
            local free=Content.walkable(state.generated_map,x,y)
            for _,actor in ipairs(state.entities) do
                if actor.id~=player.id and actor.blocks_movement
                    and actor.position.x==x and actor.position.y==y then free=false end
            end
            if free then target={x=x,y=y};aim={-d[1],-d[2]};break end
        end
        assert(target,"no elite approach")
        player.position.x,player.position.y=target.x,target.y
        Visibility.recompute(state)
        elite.dormant=nil
        state.stage.phase="elite_active"
        elite.hp=math.floor(elite.max_hp/2)+1
        player.damage=elite.defense+1
        local old_speed=elite.speed
        Scheduler.enqueue(state,{kind="actor_ready",due=state.clock,
            actor_id=elite.id,is_player=false})
        local ok,_,events=Game.submit(state,{kind="basic_attack",
            dx=aim[1],dy=aim[2]})
        assert(ok and #events_of(events,"ElitePhaseChanged")==1
            and elite.phase_two and elite.speed==old_speed+25)
        -- If a locked attack is now pending, its cells and due time remain
        -- byte-for-byte equal on restore and subsequent ordinary actions.
        if not elite.pending_attack then
            for _=1,2 do
                assert(Game.submit(state,{kind="wait"}))
                if elite.pending_attack then break end
            end
        end
        assert(elite.pending_attack and
            elite.pending_attack.ability_id:find("core:ability/elite_",1,true))
        local restored=Game.restore(Game.snapshot(state))
        for _=1,2 do
            local a,_,events_a=Game.submit(state,{kind="wait"})
            local b,_,events_b=Game.submit(restored,{kind="wait"})
            assert(a==b);equal(events_a,events_b)
            equal(Game.snapshot(state),Game.snapshot(restored))
        end
    end)
end
