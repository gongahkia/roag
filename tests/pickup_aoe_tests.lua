return function(test,equal)
    local Game=require("src.game")
    local Classes=require("src.classes")
    local Rewards=require("src.rewards")
    local Catalog=require("src.boon_content")
    local Achievements=require("src.achievements")
    local Content=require("src.content")
    local function events_of(events,kind)
        local result={}
        for _,event in ipairs(events) do
            if event.kind==kind then result[#result+1]=event end
        end
        return result
    end
    local function new(id)
        return Game.new(9,"core:map/test_room",{class_id=id,
            available_boons=Achievements.pool({})})
    end

    test("Sentinel and Alchemist are selectable plain-data classes with distinct area kits",function()
        assert(Classes.validate() and #Classes.order==13)
        local sentinel=new("core:actor/sentinel")
        local s=Game.player(sentinel)
        assert(s.defense==2 and s.max_hp==29)
        local pulse=Game.preview(sentinel,"sweeping_slash",1,0)
        assert(pulse.valid and #pulse.cells==8)
        local a=Game.spawn_enemy(sentinel,"core:enemy/mossbound_guard",3,2,99999)
        local b=Game.spawn_enemy(sentinel,"core:enemy/mossbound_guard",2,3,99999)
        local _,_,events=Game.submit(sentinel,{kind="sweeping_slash",dx=1,dy=0})
        local batch=events_of(events,"DamageBatchResolved")[1]
        assert(batch and #batch.outcomes==2 and a.hp<a.max_hp and b.hp<b.max_hp)
        local alchemist=new("core:actor/alchemist")
        local flask=Game.preview(alchemist,"sweeping_slash",1,0)
        assert(flask.valid and #flask.cells>=3)
        local center=Game.spawn_enemy(alchemist,"core:enemy/mossbound_guard",5,2,99999)
        local flank=Game.spawn_enemy(alchemist,"core:enemy/mossbound_guard",6,2,99999)
        local _,_,blast=Game.submit(alchemist,{kind="sweeping_slash",dx=1,dy=0})
        local outcomes=events_of(blast,"DamageBatchResolved")[1].outcomes
        assert(#outcomes==2 and center.hp<center.max_hp and flank.hp<flank.max_hp)
        assert(Game.restore(Game.snapshot(alchemist)))
    end)

    test("food and health pickups wait for injury, heal once and cap at maximum",function()
        local state=Game.new(17)
        local emitted={}
        local function emit(kind,fields)
            emitted[#emitted+1]={kind=kind,fields=fields}
        end
        local food=Rewards.spawn_supply(state,"food",state.active_floor_id,3,2,emit)
        local health=Rewards.spawn_supply(state,"health",state.active_floor_id,4,2,emit)
        local player=Game.player(state)
        assert(food.amount==4 and health.amount==10)
        local _,_,full=Game.submit(state,{kind="move",dx=1,dy=0})
        assert(not food.collected and #events_of(full,"SupplyCollected")==0)
        player.hp=player.max_hp-7
        assert(Game.submit(state,{kind="move",dx=-1,dy=0}))
        local _,_,picked=Game.submit(state,{kind="move",dx=1,dy=0})
        assert(food.collected and player.hp==player.max_hp-3)
        assert(events_of(picked,"SupplyCollected")[1].actual_gain==4)
        local _,_,healed=Game.submit(state,{kind="move",dx=1,dy=0})
        assert(health.collected and player.hp==player.max_hp)
        assert(events_of(healed,"SupplyCollected")[1].actual_gain==3)
        equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
    end)

    test("supply rolls use an independent stable stream and elite drops health",function()
        local state=Game.new_stage(21)
        local before=state.rng.state
        local emitted={}
        local function emit(kind,fields) emitted[#emitted+1]={kind=kind,fields=fields} end
        local event={kind="EntityKilled",target_id=991,
            target_definition_id="core:enemy/thorn_warden",
            source_id=999,source_faction="enemy",
            floor_id=state.active_floor_id,x=Game.player(state).position.x,
            y=Game.player(state).position.y}
        Rewards.on_event(state,event,emit)
        assert(state.rng.state==before)
        local supply
        for _,drop in ipairs(state.drops) do
            if drop.killed_entity_id==991 and drop.kind=="health" then supply=drop end
        end
        assert(supply and supply.amount==10)
        local count=#state.drops
        Rewards.on_event(state,event,emit)
        assert(#state.drops==count)
    end)

    test("canonical elite death leaves a collectible health pickup",function()
        local state=Game.new_stage(4)
        local elite=assert(Game.entity(state,state.stage.elite_id))
        local player=Game.player(state)
        local aim
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
            local x,y=elite.position.x+d[1],elite.position.y+d[2]
            local clear=Content.walkable(state.generated_map,x,y)
            for _,actor in ipairs(state.entities) do
                if actor.id~=player.id and actor.position.floor_id==state.active_floor_id
                    and actor.position.x==x and actor.position.y==y then clear=false end
            end
            if clear then
                player.position.x,player.position.y=x,y
                aim={-d[1],-d[2]}
                break
            end
        end
        assert(aim,"elite has no attack approach")
        player.damage=elite.hp+elite.defense -- assisted fixture, ordinary attack pipeline
        require("src.visibility").recompute(state)
        local _,_,events=Game.submit(state,{kind="basic_attack",dx=aim[1],dy=aim[2]})
        local dropped=events_of(events,"SupplyDropped")
        assert(#dropped==1 and dropped[1].supply_kind=="health")
        local found=false
        for _,drop in ipairs(state.drops) do
            if drop.id==dropped[1].drop_id and drop.kind=="health" then found=true end
        end
        assert(found)
    end)

    test("new area boons batch secondary hits and are offered only to new kits",function()
        assert(Catalog.validate() and #Catalog.order==53)
        for _,id in ipairs({"core:boon/aftershock","core:boon/reprisal_nova"}) do
            local def=Catalog.get(id)
            for _,rarity in ipairs(Catalog.rarities) do
                assert(def.parameters[rarity].power>0)
            end
        end
        local state=new("core:actor/sentinel")
        Game.set_boon_stacks(state,state.player_id,"core:boon/aftershock","common",1)
        local first=Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2,99999)
        local neighbor=Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,3,99999)
        local _,_,events=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(#events_of(events,"BoonActivated")>=1
            and first.hp<first.max_hp and neighbor.hp<neighbor.max_hp)
        local old=new("core:actor/adventurer")
        local found_old,found_new=false,false
        for seed=1,300 do
            local rng=require("src.rng").new(seed)
            for _,offer in ipairs(Rewards.make_offers(old,
                Content.get_chest("core:chest/standard"),rng)) do
                if offer.boon_id=="core:boon/aftershock" then found_old=true end
            end
            local options=Rewards.make_offers(state,
                Content.get_chest("core:chest/standard"),require("src.rng").new(seed))
            for _,offer in ipairs(options) do
                if offer.boon_id=="core:boon/aftershock" then found_new=true end
            end
        end
        assert(not found_old and found_new)
    end)

    test("Reprisal Nova reacts to real incoming harm but never to a full block",function()
        local hurt=new("core:actor/sentinel")
        Game.set_boon_stacks(hurt,hurt.player_id,"core:boon/reprisal_nova","common",1)
        local foe=Game.spawn_enemy(hurt,"core:enemy/mossbound_guard",3,2,0)
        Game.submit(hurt,{kind="wait"})
        local _,_,events=Game.submit(hurt,{kind="wait"})
        assert(#events_of(events,"BoonActivated")==1
            and Game.player(hurt).hp<Game.player(hurt).max_hp
            and foe.hp<foe.max_hp)
        local blocked=new("core:actor/sentinel")
        Game.set_boon_stacks(blocked,blocked.player_id,
            "core:boon/reprisal_nova","common",1)
        Game.player(blocked).defense=20
        Game.spawn_enemy(blocked,"core:enemy/mossbound_guard",3,2,0)
        Game.submit(blocked,{kind="wait"})
        local _,_,no_proc=Game.submit(blocked,{kind="wait"})
        assert(#events_of(no_proc,"DamageBlocked")>=1
            and #events_of(no_proc,"BoonActivated")==0)
    end)
end
