local Camera=require("src.camera")
local Content=require("src.content")
local Game=require("src.game")
local Playback=require("src.playback")
local CombatNumbers=require("src.combat_numbers")
local Generation=require("src.generation")

return function(test,equal)
    test("floating hit numbers use resolved loss once per displayed beat",function()
        local numbers=CombatNumbers.new()
        local first={events={
            {kind='DamageTaken',target_id=7,source_id=1,floor_id='floor',
                x=3,y=4,actual_loss=2,critical=false},
            {kind='DamageTaken',target_id=7,source_id=1,floor_id='floor',
                x=3,y=4,actual_loss=3,critical=true},
            {kind='Died',target_id=7,floor_id='floor',x=3,y=4},
        }}
        local playback={items={first}}
        CombatNumbers.update(numbers,playback,0,1,'floor')
        assert(#numbers.items==1 and numbers.items[1].amount==5 and
            numbers.items[1].critical and numbers.items[1].from_player)
        CombatNumbers.update(numbers,playback,0.1,1,'floor')
        assert(#numbers.items==1 and numbers.items[1].age==0.1)
        playback.items={{event={kind='DamageBlocked',target_id=8,
            floor_id='floor',x=2,y=4}}}
        CombatNumbers.update(numbers,playback,0,1,'floor')
        assert(#numbers.items==2 and numbers.items[2].blocked)
        CombatNumbers.update(numbers,playback,0,1,'other-floor')
        assert(#numbers.items==0,'old floor kept floating damage')
    end)
    local function near(a,b)
        assert(math.abs(a-b)<0.000001,("%.8f ~= %.8f"):format(a,b))
    end
    local function moved(id,from_x,from_y,x,y,time,path)
        return {id=id,kind="Moved",source_id=id,time=time,floor_id="floor",
            from_x=from_x,from_y=from_y,x=x,y=y,path=path}
    end

    test("board token starts, eases through midpoint and snaps to final tile",function()
        local playback=Playback.new()
        Playback.add(playback,{moved(3,2,4,3,4,0)},1)
        near(select(1,Playback.position(playback,3,"floor",3,4)),2)
        Playback.update(playback,Playback.tile_seconds/2)
        near(select(1,Playback.position(playback,3,"floor",3,4)),2.5)
        Playback.update(playback,Playback.tile_seconds/2)
        assert(not Playback.busy(playback))
        near(select(1,Playback.position(playback,3,"floor",3,4)),3)
        Playback.update(playback,10)
        near(select(1,Playback.position(playback,3,"floor",3,4)),3)
    end)

    test("Dash follows both legal legs and partial Dash stops before wall",function()
        local full=Playback.new()
        Playback.add(full,{moved(1,2,2,4,2,0,{{x=3,y=2},{x=4,y=2}})},1)
        Playback.update(full,Playback.tile_seconds)
        near(select(1,Playback.position(full,1,"floor",4,2)),3)
        Playback.update(full,Playback.tile_seconds/2)
        near(select(1,Playback.position(full,1,"floor",4,2)),3.5)
        Playback.update(full,Playback.tile_seconds/2)
        near(select(1,Playback.position(full,1,"floor",4,2)),4)
        local state=Game.new(5)
        local ok,_,events=Game.submit(state,{kind="dash",dx=1,dy=0})
        assert(ok and events[1].kind=="Moved" and #events[1].path==2)
        local partial=Playback.new()
        Playback.add(partial,{moved(1,2,2,3,2,0,{{x=3,y=2}})},1)
        Playback.update(partial,1)
        near(select(1,Playback.position(partial,1,"floor",3,2)),3)
    end)

    test("simultaneous actors retain their own origins and bounded beat",function()
        local events={}
        for id=1,60 do
            events[#events+1]=moved(id,id,3,id+1,3,id)
        end
        local playback=Playback.new()
        Playback.add(playback,events,1)
        assert(#playback.items==1 and playback.items[1].duration==Playback.tile_seconds)
        for id=1,60 do near(select(1,Playback.position(playback,id,"floor",id+1,3)),id) end
        Playback.update(playback,Playback.tile_seconds/2)
        for id=1,60 do near(select(1,Playback.position(playback,id,"floor",id+1,3)),id+0.5) end
        Playback.update(playback,1)
        assert(not Playback.busy(playback))
    end)

    test("one actor's repeated enemy turns retain intermediate event-time tiles",function()
        local playback=Playback.new()
        Playback.add(playback,{moved(8,7,3,6,3,0),moved(8,6,3,5,3,70)},1)
        assert(#playback.items==1 and playback.items[1].duration==0.16)
        near(select(1,Playback.position(playback,8,"floor",5,3)),7)
        Playback.update(playback,0.08)
        near(select(1,Playback.position(playback,8,"floor",5,3)),6)
        Playback.update(playback,0.08)
        near(select(1,Playback.position(playback,8,"floor",5,3)),5)
    end)

    test("movement beats precede telegraph and impact without changing world state",function()
        local state=Game.new(28,"core:map/scrolling_room",{demo_enemies=true})
        local before=Game.snapshot(state)
        local ok,_,events=Game.submit(state,{kind="move",dx=1,dy=0})
        assert(ok)
        local committed=Game.snapshot(state)
        local a,b=Playback.new(),Playback.new()
        Playback.add(a,events,state.player_id)
        Playback.add(b,events,state.player_id)
        for _,dt in ipairs({0.01,0.03,0.03,0.5,1,1,1}) do Playback.update(a,dt) end
        Playback.update(b,10)
        assert(not Playback.busy(a) and not Playback.busy(b))
        equal(committed,Game.snapshot(state))
        assert(before.clock~=state.clock)
        local ordered=Playback.new()
        Playback.add(ordered,{moved(2,3,3,4,3,0),
            {id=3,kind="WindupStarted",source_id=2,time=50},
            {id=4,kind="AttackPerformed",source_id=2,time=80}},1)
        assert(Playback.current(ordered).kind=="Moved")
        Playback.update(ordered,0.08)
        assert(Playback.current(ordered).kind=="WindupStarted")
        Playback.update(ordered,0.55)
        assert(Playback.current(ordered).kind=="AttackPerformed")
    end)

    test("displacement without a route snaps safely and restored presentation is clean",function()
        local playback=Playback.new()
        Playback.add(playback,{{id=4,kind="Displaced",source_id=1,target_id=7,
            floor_id="floor",from_x=2,from_y=2,x=9,y=2}},1)
        near(select(1,Playback.position(playback,7,"floor",9,2)),2)
        Playback.update(playback,0.04)
        near(select(1,Playback.position(playback,7,"floor",9,2)),2)
        Playback.update(playback,0.04)
        near(select(1,Playback.position(playback,7,"floor",9,2)),9)
        local state=Game.new(12)
        local clone=Game.restore(Game.snapshot(state))
        equal(Game.snapshot(state),Game.snapshot(clone))
        local clean=Playback.new()
        local p=Game.player(clone).position
        near(select(1,Playback.position(clean,clone.player_id,p.floor_id,p.x,p.y)),p.x)
    end)

    test("fog-hidden actor motion never exposes an unseen tile",function()
        local playback=Playback.new()
        Playback.add(playback,{moved(9,20,3,21,3,0)},1)
        local function visible(x) return x<10 end
        local _,_,shown=Playback.visible_position(playback,9,"floor",21,3,visible)
        assert(not shown)
        Playback.update(playback,1)
        local entering=Playback.new()
        Playback.add(entering,{moved(9,9,3,10,3,0)},1)
        local _,_,before=Playback.visible_position(entering,9,"floor",10,3,visible)
        assert(not before) -- final tile is hidden even if the origin was seen
    end)

    test("camera follows visual coordinates with aligned rounded tile offsets",function()
        local camera=Camera.new(Content.get_map("core:map/scrolling_room"),
            {x=16,y=16,width=704,height=608},32,3,3)
        local initial_x=camera.x
        Camera.update(camera,0.08,18.5,3)
        assert(camera.x>initial_x and camera.x<initial_x+400)
        local x1=Camera.tile_top_left(camera,4,3)
        local x2=Camera.tile_top_left(camera,5,3)
        assert(x2-x1==32 and x1%1==0)
        Camera.update(camera,5,35,25)
        assert(camera.x<=768 and camera.y<=496)
    end)

    test("long corridor ranged lane stays blocked by walls and strict timing is legible",function()
        local map=Generation.generate(1)
        local long,alcoves=0,0
        for _,corridor in ipairs(map.corridors) do
            if corridor.length>=24 then
                long=long+1
                alcoves=alcoves+#corridor.alcoves
            end
        end
        assert(long>=1 and alcoves>=1)
        local state=Game.new(10,"core:map/test_room")
        local spitter=Game.spawn_enemy(state,"core:enemy/thornspitter",9,2)
        local ok,_,events=Game.submit(state,{kind="wait"})
        assert(ok and state.clock==100)
        local windup,impact
        for i,event in ipairs(events) do
            if event.kind=="WindupStarted" and event.source_id==spitter.id then
                windup={index=i,event=event}
            elseif event.kind=="AttackPerformed" and event.source_id==spitter.id then
                impact={index=i,event=event}
            end
        end
        assert(windup and impact and windup.index<impact.index)
        assert(windup.event.due==impact.event.time and impact.event.time==100)
        assert(#windup.event.target_cells==7)
        -- This is a strict-scheduler hazard: the locked strike resolved before
        -- the next player decision. Playback still shows the windup first.
        local playback=Playback.new()
        Playback.add(playback,events,state.player_id)
        assert(Playback.current(playback).kind=="WindupStarted")
        Playback.update(playback,0.55)
        assert(Playback.current(playback).kind=="AttackPerformed")
    end)

    test("consecutive locked windups share one visible beat without reordering impacts",function()
        local events={}
        for id=2,4 do
            events[#events+1]={id=id,kind="WindupStarted",source_id=id,
                floor_id="floor",time=100,due=200,
                target_cells={{x=id,y=3}}}
        end
        events[#events+1]={id=5,kind="AttackPerformed",source_id=2,
            floor_id="floor",time=200,target_cells={{x=2,y=3}}}
        local playback=Playback.new()
        Playback.add(playback,events,1)
        assert(#playback.items==2 and playback.items[1].duration==0.55)
        local group=Playback.current_group(playback)
        assert(#group==3 and group[1]==events[1] and group[2]==events[2]
            and group[3]==events[3])
        Playback.update(playback,0.55)
        assert(Playback.current(playback)==events[4]
            and Playback.current_group(playback)==nil)
        Playback.update(playback,0.26)
        assert(not Playback.busy(playback))
    end)

    test("windup grouping respects intervening events and action boundaries",function()
        local function windup(id,time)
            return {id=id,kind="WindupStarted",source_id=id,
                floor_id="floor",time=time,due=time+100}
        end
        local playback=Playback.new()
        Playback.add(playback,{windup(2,0),windup(3,40),
            moved(4,5,3,6,3,40),windup(5,40)},1)
        assert(#playback.items==3 and #playback.items[1].events==2)
        Playback.add(playback,{windup(6,40)},1)
        assert(#playback.items==4) -- a later submitted action is distinct
    end)

    test("dense same-kind boon cues share a bounded beat, with hits and deaths ordered",function()
        local events={{id=1,kind="DamageTaken",time=100,floor_id="floor",x=3,y=2}}
        for id=2,41 do
            events[#events+1]={id=id,kind="BoonActivated",time=100,
                floor_id="floor",x=3,y=2,boon_id="core:boon/storm_conductor"}
        end
        events[#events+1]={id=42,kind="Died",time=100,floor_id="floor",x=3,y=2}
        local playback=Playback.new()
        Playback.add(playback,events,1)
        assert(#playback.items==3)
        assert(playback.items[1].event==events[1])
        assert(playback.items[2].duration==Playback.max_cascade_seconds)
        assert(#playback.items[2].events==40)
        for index,event in ipairs(playback.items[2].events) do
            assert(event==events[index+1])
        end
        Playback.update(playback,0.20)
        assert(Playback.current(playback)==events[2])
        Playback.update(playback,Playback.max_cascade_seconds)
        assert(Playback.current(playback)==events[#events])
    end)

    test("one lethal target's hit and death share a beat without mixing other deaths",function()
        local hit={id=1,kind="DamageTaken",target_id=7,time=100,
            floor_id="floor",x=3,y=2}
        local death={id=2,kind="Died",target_id=7,time=100,
            floor_id="floor",x=3,y=2}
        local other={id=3,kind="Died",target_id=8,time=100,
            floor_id="floor",x=4,y=2}
        local playback=Playback.new()
        Playback.add(playback,{hit,death,other},1)
        assert(#playback.items==2 and playback.items[1].duration==0.30)
        local group=Playback.current_group(playback)
        assert(#group==2 and group[1]==hit and group[2]==death)
        Playback.update(playback,0.30)
        assert(Playback.current(playback)==other)
    end)

    test("generated ranged lanes retain lateral geometry across representative seeds",function()
        local Biomes=require("src.biomes")
        local lanes=0
        for _,biome_id in ipairs({Biomes.order[1],Biomes.order[3]}) do
            for seed=1,20 do
                local map=Generation.generate(seed,{biome_id=biome_id})
                for _,p in ipairs(map.placements.enemies) do
                    if p.definition_id=="core:enemy/thornspitter" or
                        p.definition_id=="core:enemy/canal_lancer" then
                        for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
                            local length,lateral=0,false
                            for step=1,7 do
                                local x,y=p.x+step*d[1],p.y+step*d[2]
                                if not Content.projectile_passable(map,x,y) then break end
                                length=step
                                lateral=lateral or
                                    Content.walkable(map,x-d[2],y+d[1]) or
                                    Content.walkable(map,x+d[2],y-d[1])
                            end
                            if length>=5 then
                                lanes=lanes+1
                                assert(lateral,('no lateral exit %s seed %d enemy %s'):
                                    format(biome_id,seed,p.id))
                            end
                        end
                    end
                end
            end
        end
        assert(lanes>=100,'corpus did not exercise enough ranged lanes')
    end)
end
