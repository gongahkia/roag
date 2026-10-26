return function(test,equal)
    local Catalog=require("src.boon_content")
    local Achievements=require("src.achievements")
    local Boons=require("src.boons")
    local Content=require("src.content")
    local Game=require("src.game")
    local Rewards=require("src.rewards")
    local Scheduler=require("src.scheduler")
    local Classes=require("src.classes")
    local function count(events,kind,id)
        local n=0
        for _,event in ipairs(events) do
            if event.kind==kind and (not id or event.boon_id=="core:boon/"..id) then
                n=n+1
            end
        end
        return n
    end
    local function copy(value)
        if type(value)~="table" then return value end
        local result={}
        for k,v in pairs(value) do result[k]=copy(v) end
        return result
    end
    local function fixture(boon,class_id)
        local state=Game.new(7,"core:map/test_room",{class_id=class_id})
        if boon then Game.set_boon_stacks(state,state.player_id,
            "core:boon/"..boon,"rare",1) end
        return state
    end
    local function tile(state,symbol,x,y)
        if not state.generated_map then
            state.generated_map=copy(Content.get_map("core:map/test_room"))
        end
        local map=state.generated_map
        map.rows[y]=map.rows[y]:sub(1,x-1)..symbol..map.rows[y]:sub(x+1)
    end
    local function quiet(state,x,y,hp)
        local enemy=Game.spawn_enemy(state,"core:enemy/mossbound_guard",x,y)
        Scheduler.remove_for_actor(state,enemy.id)
        if hp then enemy.hp=hp;enemy.max_hp=math.max(enemy.max_hp,hp) end
        return enemy
    end
    local function action(state,kind,dx,dy)
        local ok,reason,events=Game.submit(state,{kind=kind,dx=dx,dy=dy})
        assert(ok,reason)
        return events
    end

    test("additive boon identities retain 20 originals, four unlocks and class-aware normal offers",function()
        assert(Catalog.validate() and #Catalog.order==53+#require('src.idea_boon_content').order)
        local baseline=Catalog.starter_order()
        local normal=Achievements.pool({})
        assert(#baseline==20 and #normal==49+#require('src.idea_boon_content').order)
        for i=1,20 do assert(normal[i]==baseline[i]) end
        local expansion=require("src.boon_expansion_content")
        assert(#expansion.order==26)
        local seen={}
        for _,class_id in ipairs(Classes.order) do
            local found={}
            for seed=1,950 do
                local state=Game.new(seed,"core:map/test_room",{
                    class_id=class_id,available_boons=normal})
                local offers=Rewards.make_offers(state,
                    Content.get_chest("core:chest/standard"))
                for _,offer in ipairs(offers) do
                    found[offer.boon_id]=true
                    local eligible=Catalog.get(offer.boon_id).eligible_classes
                    if eligible then
                        local allowed=false
                        for _,id in ipairs(eligible) do
                            if id==class_id then allowed=true end
                        end
                        assert(allowed,"offered unusable class boon")
                    end
                end
            end
            for _,id in ipairs(expansion.order) do
                local eligible=Catalog.get(id).eligible_classes
                if not eligible or eligible[1]==class_id then
                    assert(found[id],"new boon never offered: "..id)
                    seen[id]=true
                else assert(not found[id],"class boon leaked: "..id) end
            end
        end
        for _,id in ipairs(expansion.order) do assert(seen[id]) end
        local generated_seen={}
        for _,class_id in ipairs(Classes.order) do
            for seed=1,100 do
                local state=Game.new_stage(seed,{multifloor=true,
                    class_id=class_id,available_boons=normal})
                for _,actor in ipairs(state.entities) do
                    if actor.controller=="chest" then
                        for _,offer in ipairs(actor.offers) do
                            generated_seen[offer.boon_id]=true
                        end
                    end
                end
            end
        end
        for _,id in ipairs(expansion.order) do
            assert(generated_seen[id],"new boon absent from generated chests: "..id)
        end
        for i=21,24 do
            for _,id in ipairs(normal) do assert(id~=Catalog.order[i]) end
        end
        local state=fixture("stonewake")
        Game.set_boon_stacks(state,state.player_id,"core:boon/stonewake","common",9)
        Game.set_boon_stacks(state,state.player_id,"core:boon/stonewake","legendary",7)
        assert(Boons.aggregate("core:boon/stonewake",
            Game.player(state).boons["core:boon/stonewake"],"power")==40)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)

    test("terrain boons activate through actual bog, bramble, ice, break, mist and crystal",function()
        local s=fixture("stonewake")
        tile(s,"g",2,2)
        local e=quiet(s,4,2,20)
        assert(count(action(s,"move",1,0),"BoonActivated","stonewake")>=1)
        local before=e.hp
        local events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","stonewake")>=1 and e.hp<=before-5)

        s=fixture("bramble_compact");tile(s,"b",3,2);e=quiet(s,4,2,20)
        before=e.hp;events=action(s,"move",1,0)
        assert(count(events,"TerrainTriggered")>=1
            and count(events,"BoonActivated","bramble_compact")>=1
            and e.hp<before)

        s=fixture("froststep_catalyst");tile(s,"i",3,2)
        e=quiet(s,6,2,20);before=e.hp
        events=action(s,"dash",1,0)
        assert(count(events,"BoonActivated","froststep_catalyst")>=1
            and e.hp<before)

        s=fixture("fracture_dividend");tile(s,"B",3,2)
        e=quiet(s,4,2,20);before=e.hp
        events=action(s,"basic_attack",1,0)
        assert(count(events,"TerrainBroken")>=1
            and count(events,"BoonActivated","fracture_dividend")>=1
            and e.hp<before)

        s=fixture("mist_veil");tile(s,"m",2,2)
        e=quiet(s,3,2,20);before=e.hp
        events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","mist_veil")>=1 and e.hp<=before-5)

        s=fixture("crystal_harmony",Classes.order[3]);tile(s,"c",3,3)
        quiet(s,3,2,20);e=quiet(s,4,2,20);before=e.hp
        events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","crystal_harmony")>=1
            and e.hp<before)
    end)

    test("conditional offense uses marks, wounded health, Burning and three-target batches",function()
        local s=fixture("sunderers_mark")
        local e=quiet(s,3,2,30)
        local first=action(s,"basic_attack",1,0)
        local after=e.hp
        local second=action(s,"basic_attack",1,0)
        assert(count(first,"BoonActivated","sunderers_mark")==1
            and count(second,"BoonActivated","sunderers_mark")==1
            and after-e.hp>4)
        local memory=Game.player(s).boon_memory["core:boon/sunderers_mark"]
        assert(memory.mark_target_id==e.id and memory.mark_count==2)
        equal(Game.snapshot(s),Game.snapshot(Game.restore(Game.snapshot(s))))

        s=fixture("executioners_thread");e=quiet(s,3,2,20);e.hp=8
        local before=e.hp;local events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","executioners_thread")>=1
            and before-e.hp>4)

        s=fixture("shared_agony")
        local first_enemy=quiet(s,3,2,20)
        local second_enemy=quiet(s,3,3,20)
        first_enemy.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        second_enemy.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        before=second_enemy.hp;events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","shared_agony")>=1
            and second_enemy.hp<before)

        s=fixture("cluster_rupture",Classes.order[3])
        quiet(s,3,2,20);quiet(s,4,2,20);e=quiet(s,5,2,20)
        events=action(s,"sweeping_slash",1,0)
        assert(count(events,"BoonActivated","cluster_rupture")>=1)
    end)

    test("block, harm, credited kill and alternating motion provide bounded stateful boons",function()
        local s=fixture("counterweight")
        local enemy=Game.spawn_enemy(s,"core:enemy/mossbound_guard",3,2,0)
        Game.player(s).defense=20
        action(s,"wait")
        local events=action(s,"wait")
        assert(count(events,"DamageBlocked")>=1
            and count(events,"BoonActivated","counterweight")>=1)
        local before=enemy.hp
        events=action(s,"sweeping_slash",1,0)
        assert(count(events,"BoonActivated","counterweight")>=1
            and before-enemy.hp>Game.player(s).slash_damage)
        assert(Game.player(s).boon_memory["core:boon/counterweight"].charges==0)

        s=fixture("pain_dividend")
        enemy=Game.spawn_enemy(s,"core:enemy/mossbound_guard",3,2,0)
        action(s,"wait");events=action(s,"wait")
        assert(count(events,"DamageTaken")>=1
            and count(events,"BoonActivated","pain_dividend")>=1)
        before=enemy.hp
        events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","pain_dividend")>=1
            and before-enemy.hp>Game.player(s).damage)

        s=fixture("bloodrush")
        enemy=quiet(s,3,2,1)
        local old=Game.preview(s,"move",0,1).cost
        events=action(s,"basic_attack",1,0)
        assert(count(events,"EntityKilled")>=1
            and count(events,"BoonActivated","bloodrush")>=1)
        assert(Game.preview(s,"move",0,1).cost<old)
        equal(Game.snapshot(s),Game.snapshot(Game.restore(Game.snapshot(s))))

        s=fixture("rhythm_of_steel")
        enemy=quiet(s,4,2,20)
        events=action(s,"move",1,0)
        assert(count(events,"BoonActivated","rhythm_of_steel")>=1)
        before=enemy.hp;events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","rhythm_of_steel")>=1
            and before-enemy.hp>Game.player(s).damage)
    end)

    test("enemy-originated windups, approach and friendly fire drive reactions",function()
        local s=fixture("windup_hunter")
        local enemy=Game.spawn_enemy(s,"core:enemy/mossbound_guard",3,2,0)
        local before=enemy.hp
        local events=action(s,"wait")
        assert(count(events,"WindupStarted")>=1
            and count(events,"BoonActivated","windup_hunter")>=1
            and enemy.hp<before and enemy.pending_attack)

        s=fixture("crossfire_omen")
        Game.spawn_enemy(s,"core:enemy/mossbound_guard",3,2,0)
        Game.spawn_enemy(s,"core:enemy/mossbound_guard",2,3,0)
        events=action(s,"wait")
        assert(count(events,"WindupStarted")>=2
            and count(events,"BoonActivated","crossfire_omen")>=1)

        s=fixture("pursuit_spark")
        enemy=Game.spawn_enemy(s,"core:enemy/mossbound_guard",5,2,0)
        before=enemy.hp;events=action(s,"wait")
        assert(count(events,"Moved")>=1
            and count(events,"BoonActivated","pursuit_spark")>=1
            and enemy.hp<before)

        for _,name in ipairs({"far_witness","betrayal_ward"}) do
            s=fixture(name)
            Game.spawn_enemy(s,"core:enemy/thornspitter",9,2,0)
            enemy=Game.spawn_enemy(s,"core:enemy/mossbound_guard",7,2,0)
            events=action(s,"wait")
            assert(count(events,"BoonActivated",name)>=1,
                name.." did not observe real hostile friendly fire")
            if name=="far_witness" then
                local control=fixture(nil)
                Game.spawn_enemy(control,"core:enemy/thornspitter",9,2,0)
                local control_target=Game.spawn_enemy(control,
                    "core:enemy/mossbound_guard",7,2,0)
                action(control,"wait")
                assert(enemy.hp<control_target.hp,
                    "distant witness dealt no bonus damage")
            else
                assert(Game.player(s).boon_memory["core:boon/betrayal_ward"].ward==3)
            end
        end
    end)

    test("formation attack and status-sharing bonuses produce ordered secondary damage",function()
        local s=fixture("ordered_ranks")
        local first=quiet(s,3,2,20)
        quiet(s,4,2,20);quiet(s,5,2,20)
        local before=first.hp
        local events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","ordered_ranks")>=1
            and before-first.hp>Game.player(s).damage)
        local first_batch
        for _,event in ipairs(events) do
            if event.kind=="DamageBatchResolved" then first_batch=event;break end
        end
        assert(first_batch and first_batch.outcomes[1].target_id==first.id)
    end)

    test("ten mixed old/new boon interactions retain one causal action and real effects",function()
        local function add(state,name,rarity,amount)
            Game.set_boon_stacks(state,state.player_id,"core:boon/"..name,
                rarity or "rare",amount or 1)
        end
        local function both(events,a,b)
            assert(count(events,"BoonActivated",a)>=1 and
                count(events,"BoonActivated",b)>=1,
                a.." + "..b.." failed to activate")
        end
        local s=fixture("mist_veil")
        add(s,"opening_cut");tile(s,"m",2,2);quiet(s,3,2,20)
        both(action(s,"basic_attack",1,0),"mist_veil","opening_cut")

        s=fixture("crystal_harmony",Classes.order[3])
        add(s,"storm_conductor","legendary",2)
        tile(s,"c",3,3);quiet(s,3,2,20);quiet(s,4,2,20)
        both(action(s,"basic_attack",1,0),"crystal_harmony","storm_conductor")

        s=fixture("bloodrush");add(s,"grave_dividend")
        Game.player(s).hp=20;quiet(s,3,2,1)
        both(action(s,"basic_attack",1,0),"bloodrush","grave_dividend")
        assert(Game.player(s).hp>20)

        s=fixture("windup_hunter");add(s,"harriers_response","legendary",2)
        Game.spawn_enemy(s,"core:enemy/mossbound_guard",3,2,0)
        both(action(s,"wait"),"windup_hunter","harriers_response")

        s=fixture("betrayal_ward");add(s,"turncoat_spark")
        local spitter=Game.spawn_enemy(s,"core:enemy/thornspitter",9,2,0)
        Game.spawn_enemy(s,"core:enemy/mossbound_guard",7,2,0)
        both(action(s,"wait"),"betrayal_ward","turncoat_spark")
        assert(spitter.hp<spitter.max_hp)

        s=fixture("rhythm_of_steel");add(s,"pulse_step")
        quiet(s,4,2,20);quiet(s,3,3,20)
        both(action(s,"move",1,0),"rhythm_of_steel","pulse_step")

        s=fixture("cluster_rupture",Classes.order[3]);add(s,"resonant_wounds")
        quiet(s,3,2,20);quiet(s,4,2,20);quiet(s,5,2,20)
        both(action(s,"sweeping_slash",1,0),"cluster_rupture","resonant_wounds")

        s=fixture("shared_agony");add(s,"flashpoint")
        local first=quiet(s,3,2,20);local second=quiet(s,3,3,20)
        first.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        second.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        both(action(s,"basic_attack",1,0),"shared_agony","flashpoint")

        s=fixture("froststep_catalyst");add(s,"phantom_lunge")
        tile(s,"i",3,2);quiet(s,6,2,20)
        both(action(s,"dash",1,0),"froststep_catalyst","phantom_lunge")

        s=fixture("sunderers_mark");add(s,"executioners_thread")
        first=quiet(s,3,2,20);first.max_hp=60
        action(s,"basic_attack",1,0)
        both(action(s,"basic_attack",1,0),"sunderers_mark","executioners_thread")

        s=fixture("splitflight",Classes.order[2]);add(s,"opening_cut")
        quiet(s,3,2,20);quiet(s,3,3,20)
        local events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","opening_cut")>=2,
            "Ranger branch lost multi-target pre-hit HP facts")
    end)

    test("all six class transformations change normal ability geometry or follow-up",function()
        local s=fixture("splitflight",Classes.order[2])
        local first=quiet(s,3,2,20)
        local branch=quiet(s,3,3,20)
        local events=action(s,"basic_attack",1,0)
        assert(first.hp<20 and branch.hp<20
            and count(events,"DamageBatchResolved")>=1)

        s=fixture("recurve",Classes.order[2]);local far=quiet(s,8,2,20)
        assert(#Game.preview(s,"sweeping_slash",1,0).cells==6)
        action(s,"sweeping_slash",1,0)
        assert(far.hp<20)

        s=fixture("expanding_sigil",Classes.order[3]);far=quiet(s,6,2,20)
        action(s,"sweeping_slash",1,0)
        assert(far.hp<20)

        s=fixture("alchemical_flip",Classes.order[3])
        first=quiet(s,3,2,20);branch=quiet(s,3,3,20)
        first.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        branch.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        action(s,"basic_attack",1,0)
        assert(first.hp<20 and branch.hp<20)

        s=fixture("earthshaker",Classes.order[4])
        first=quiet(s,3,2,20);branch=quiet(s,3,3,20)
        action(s,"basic_attack",1,0)
        assert(first.hp<20 and branch.hp<20)

        s=fixture("battle_march",Classes.order[4]);far=quiet(s,5,2,30)
        events=action(s,"dash",1,0)
        assert(count(events,"BoonActivated","battle_march")>=1)
        local before=far.hp
        events=action(s,"basic_attack",1,0)
        assert(count(events,"BoonActivated","battle_march")>=1
            and before-far.hp>Game.player(s).damage)
        equal(Game.snapshot(s),Game.snapshot(Game.restore(Game.snapshot(s))))
    end)

    test("new class transformations preserve blocked terrain and rejected Dash rules",function()
        local s=fixture("splitflight",Classes.order[2])
        tile(s,"#",3,3)
        quiet(s,3,2,20)
        local side=quiet(s,2,3,20)
        action(s,"basic_attack",1,0)
        assert(side.hp==20,"Splitflight crossed a blocked diagonal corner")

        s=fixture("recurve",Classes.order[2]);tile(s,"B",8,2)
        local preview=Game.preview(s,"sweeping_slash",1,0)
        for _,cell in ipairs(preview.cells) do
            assert(cell.x~=8,"Recurve crossed barricade")
        end

        s=fixture("expanding_sigil",Classes.order[3]);tile(s,"#",4,2)
        preview=Game.preview(s,"sweeping_slash",1,0)
        for _,cell in ipairs(preview.cells) do
            assert(cell.x~=6,"Sigil expanded through an unreachable center")
        end

        s=fixture("alchemical_flip",Classes.order[3]);tile(s,"#",3,3)
        local first=quiet(s,3,2,20);side=quiet(s,2,3,20)
        first.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        side.statuses={burning={power=1,remaining=3,interval=100,next_due=100}}
        action(s,"basic_attack",1,0)
        assert(side.hp==20,"Alchemical Flip crossed a blocked corner")

        s=fixture("earthshaker",Classes.order[4]);tile(s,"B",3,3)
        preview=Game.preview(s,"basic_attack",1,0)
        for _,cell in ipairs(preview.cells) do
            assert(not (cell.x==3 and cell.y==3),"shockwave crossed barricade")
        end

        s=fixture("battle_march",Classes.order[4]);tile(s,"B",3,2)
        local before=Game.snapshot(s)
        local ok=Game.submit(s,{kind="dash",dx=1,dy=0})
        assert(not ok and not Game.player(s).boon_memory)
        equal(before,Game.snapshot(s))
    end)

    test("ordinary paired-floor New Run earns and activates a purchased new boon",function()
        local run,result=require("tests.expansion_boon_acceptance").run()
        assert(result.seed==50 and result.actions>0 and result.hp>0
            and result.clock>0 and result.rarity=="common"
            and result.movement_activations>=1 and result.hit_activations>=1)
        assert(run.state.floor_order and #run.state.floor_order==2)
        equal(Game.snapshot(run.state),Game.snapshot(Game.restore(Game.snapshot(run.state))))
    end)

    test("movement boon on a stair commits on the destination without waking old-floor effects",function()
        local Visibility=require("src.visibility")
        local state=Game.new_stage(3,{multifloor=true})
        Game.set_boon_stacks(state,state.player_id,
            "core:boon/rhythm_of_steel","common",1)
        local stair=state.generated_map.placements.stairs[1]
        local player=Game.player(state)
        local dx,dy
        for _,step in ipairs({{1,0},{0,1},{-1,0},{0,-1}}) do
            local x,y=stair.x-step[1],stair.y-step[2]
            local free=Content.walkable(state.generated_map,x,y)
            for _,actor in ipairs(state.entities) do
                if actor.id~=player.id and actor.blocks_movement
                    and actor.position.floor_id==state.active_floor_id
                    and actor.position.x==x and actor.position.y==y then free=false end
            end
            if free then
                player.position.x,player.position.y=x,y
                dx,dy=step[1],step[2]
                break
            end
        end
        assert(dx,"no legal stair approach")
        Visibility.recompute(state)
        local old_floor=state.active_floor_id
        local events=action(state,"move",dx,dy)
        assert(count(events,"FloorChanged")==1 and
            count(events,"BoonActivated","rhythm_of_steel")>=1)
        assert(state.active_floor_id~=old_floor
            and player.boon_memory["core:boon/rhythm_of_steel"].charges==1)
        equal(Game.snapshot(state),Game.snapshot(Game.restore(Game.snapshot(state))))
    end)
end
