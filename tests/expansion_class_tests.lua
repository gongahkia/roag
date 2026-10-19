return function(test,equal)
    local Classes=require("src.classes")
    local Content=require("src.content")
    local Game=require("src.game")
    local Play=require("tests.legal_playthrough")
    local Scheduler=require("src.scheduler")
    local Visibility=require("src.visibility")
    local function hits(events,kind)
        local out={}
        for _,event in ipairs(events) do
            if event.kind==kind then out[#out+1]=event end
        end
        return out
    end
    local function new(class_id)
        return Game.new(7,"core:map/test_room",{class_id=class_id})
    end

    test("all registered classes have explicit stats, costs and snapshot identity",function()
        assert(Classes.validate() and #Classes.order==15)
        local ids={}
        for _,id in ipairs(Classes.order) do
            assert(not ids[id]);ids[id]=true
            local state=new(id)
            local class=Classes.get(id)
            local player=Game.player(state)
            assert(player.definition_id==id and player.hp==class.hp
                and player.damage==class.damage and player.speed==class.speed)
            assert(Game.preview(state,"basic_attack",1,0).cost==
                Game.effective_cost(class.costs.basic_attack,player.speed))
            equal(Game.snapshot(Game.restore(Game.snapshot(state))),Game.snapshot(state))
        end
        assert(Classes.get(Classes.order[1]).hp==24
            and Classes.get(Classes.order[1]).costs.dash==130)
    end)

    test("Ranger shot obeys range, diagonal corners and first-target stop",function()
        local state=new(Classes.order[2])
        local preview=Game.preview(state,"basic_attack",1,0)
        assert(#preview.cells==4 and preview.cells[4].x==6)
        local first=Game.spawn_enemy(state,"core:enemy/ruin_skitter",4,2,0)
        local second=Game.spawn_enemy(state,"core:enemy/mossbound_guard",6,2,0)
        local ok,_,events=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(ok and #hits(events,"DamageTaken")==1
            and hits(events,"DamageTaken")[1].target_id==first.id
            and second.hp==second.max_hp)
        local blocked=new(Classes.order[2])
        Game.spawn_enemy(blocked,"core:enemy/ruin_skitter",4,4,0)
        local diagonal=Game.preview(blocked,"basic_attack",1,1)
        assert(diagonal.cells[2].x==4 and diagonal.cells[2].y==3)
        local _,_,out=Game.submit(blocked,{kind="basic_attack",dx=1,dy=1})
        assert(#hits(out,"DamageTaken")==0,
            "bow shot crossed a blocked diagonal corner")
    end)

    test("Ranger volley pierces two hostiles in one primary batch and evades three cells",function()
        local state=new(Classes.order[2])
        local first=Game.spawn_enemy(state,"core:enemy/ruin_skitter",4,2,0)
        local second=Game.spawn_enemy(state,"core:enemy/mossbound_guard",6,2,0)
        local ok,_,events=Game.submit(state,{kind="sweeping_slash",dx=1,dy=0})
        assert(ok)
        local batches=hits(events,"DamageBatchResolved")
        assert(#batches>=1 and #batches[1].outcomes==2
            and batches[1].outcomes[1].target_id==first.id
            and batches[1].outcomes[2].target_id==second.id)
        local fresh=new(Classes.order[2])
        local dash=Game.preview(fresh,"dash",1,0)
        assert(dash.valid and dash.distance==3
            and dash.cost==Game.effective_cost(115,Game.player(fresh).speed))
        assert(Game.submit(fresh,{kind="dash",dx=1,dy=0}))
        assert(Game.player(fresh).position.x==5)
    end)

    test("Arcanist lightning triggers crystal and fire sigil batches visible targets",function()
        local state=new(Classes.order[3])
        local function copy(value)
            if type(value)~="table" then return value end
            local out={}
            for key,part in pairs(value) do out[key]=copy(part) end
            return out
        end
        state.generated_map=copy(Content.get_map("core:map/test_room"))
        local map=state.generated_map
        map.rows[3]=map.rows[3]:sub(1,3).."c"..map.rows[3]:sub(5)
        local first=Game.spawn_enemy(state,"core:enemy/mossbound_guard",4,2,0)
        local second=Game.spawn_enemy(state,"core:enemy/ruin_skitter",5,2,0)
        local ok,_,events=Game.submit(state,{kind="basic_attack",dx=1,dy=0})
        assert(ok and #hits(events,"TerrainTriggered")>=1)
        local first_hit,second_hit=false,false
        for _,event in ipairs(hits(events,"DamageTaken")) do
            if event.target_id==first.id and event.damage_type=="lightning" then
                first_hit=true
            elseif event.target_id==second.id and event.damage_type=="crystal_arc" then
                second_hit=true
            end
        end
        assert(first_hit and second_hit)
        local area=new(Classes.order[3])
        Game.spawn_enemy(area,"core:enemy/mossbound_guard",4,2,0)
        Game.spawn_enemy(area,"core:enemy/mossbound_guard",5,2,0)
        local _,_,out=Game.submit(area,{kind="sweeping_slash",dx=1,dy=0})
        local batch=hits(out,"DamageBatchResolved")[1]
        assert(batch and #batch.outcomes==2
            and batch.outcomes[1].damage_type=="fire")
    end)

    test("Berserker keeps deliberate slow costs, broad cleave and cardinal bump",function()
        local state=new(Classes.order[4])
        local p=Game.player(state)
        assert(p.hp==32 and p.defense==1 and p.speed==75)
        local move=Game.preview(state,"move",1,0)
        local slash=Game.preview(state,"sweeping_slash",1,0)
        assert(move.cost==Game.effective_cost(115,75)
            and slash.cost==Game.effective_cost(180,75)
            and #slash.cells==5)
        local before=Game.snapshot(state)
        local ok=Game.submit(state,{kind="move",dx=1,dy=1})
        assert(not ok);equal(before,Game.snapshot(state))
        Game.spawn_enemy(state,"core:enemy/mossbound_guard",3,2,0)
        local bump=Game.preview(state,"move",1,0)
        assert(bump.bump and bump.cost==Game.effective_cost(130,75))
        local committed,_,events=Game.submit(state,{kind="move",dx=1,dy=0})
        local struck=false
        for _,event in ipairs(hits(events,"DamageTaken")) do
            if event.target_id==2 and event.source_id==state.player_id then
                struck=true
            end
        end
        assert(committed and struck)
    end)

    test("every class survives an ordinary-action first encounter on paired floors",function()
        for _,id in ipairs(Classes.order) do
            local run=Play.new(3,{class_id=id,multifloor=true})
            local state=run.state
            local enemy=assert(Play.nearest(state,function(actor)
                return actor.controller=="ai" and
                    actor.position.floor_id==state.active_floor_id
                    and actor.id~=state.stage.elite_id
            end))
            local ok,reason=Play.fight(run,enemy.id,220)
            assert(ok,Classes.get(id).name..": "..tostring(reason))
            assert(Game.player(state).hp>0 and #run.trace>0
                and Game.player(state).definition_id==id)
        end
    end)

    test("stage route handoff carries class build through a zero-time choice",function()
        local state=Game.new_stage(4,{class_id=Classes.order[2]})
        local player=Game.player(state)
        local elite=Game.entity(state,state.stage.elite_id)
        for _,actor in ipairs(state.entities) do
            if actor.controller=="ai" then Scheduler.remove_for_actor(state,actor.id) end
        end
        local aim
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
            local x,y=elite.position.x+d[1],elite.position.y+d[2]
            if Content.walkable(state.generated_map,x,y) then
                player.position.x,player.position.y=x,y
                aim={-d[1],-d[2]};break
            end
        end
        assert(aim)
        player.damage=elite.hp+elite.defense+2 -- assisted route unit fixture
        Visibility.recompute(state)
        assert(Game.submit(state,{kind="basic_attack",dx=aim[1],dy=aim[2]}))
        assert(state.stage.phase=="extraction")
        local exit=Game.entity(state,state.stage.exit_id)
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}}) do
            local x,y=exit.position.x+d[1],exit.position.y+d[2]
            if Content.walkable(state.generated_map,x,y) then
                player.position.x,player.position.y=x,y;break
            end
        end
        Visibility.recompute(state)
        assert(Game.route_options(state,exit.id))
        assert(Game.submit(state,{kind="choose_route",exit_id=exit.id,
            route_index=1}))
        assert(Game.player(state).definition_id==Classes.order[2]
            and Game.player(state).damage==player.damage)
        assert(Game.restore(Game.snapshot(state)))
    end)
end
