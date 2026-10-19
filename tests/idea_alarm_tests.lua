return function(test,equal)
    local G=require('src.game')
    local AI=require('src.ai')
    local C=require('src.persistence')
    local I=require('src.idea_enemies')
    local function trial()
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        local e=G.spawn_enemy(s,'core:enemy/alarm_keeper',10,6,50)
        return s,p,e
    end
    local function called(s)
        local list={};for _,e in ipairs(s.entities)do if e.called_by then list[#list+1]=e end end;return list
    end
    local function resolve(s)
        local events={}
        while s.clock<400 do
            local ok,_,list=G.submit(s,{kind='wait'});assert(ok)
            for _,event in ipairs(list)do events[#events+1]=event end
        end
        return events
    end
    test('Alarm Keeper commits a long pure visible call, spends its only budget and locks two cells',function()
        local s,p,e=trial();local before=G.snapshot(s)
        local intent=AI.choose(s,e);equal(before,G.snapshot(s))
        assert(intent.support.kind=='alarm' and #intent.cells==2)
        assert(G.submit(s,{kind='wait'}))
        assert(e.ai_memory.alarm_spent and e.pending_attack.due==343 and #called(s)==0)
        local cells=C.decode(C.encode(e.pending_attack.target_cells));local events=resolve(s)
        local count=0
        for _,event in ipairs(events)do if event.kind=='AlarmReinforcement'then
            count=count+1;assert(event.x==cells[count].x and event.y==cells[count].y and event.time==343)
        end end
        assert(count==2 and #called(s)==2 and not e.pending_attack)
        for _,actor in ipairs(called(s))do assert(actor.called_by==e.id and actor.reward_exempt)end
        assert(not AI.choose(s,e).support,'a resolved alarm must never call again')
    end)
    test('stun or death interrupts the finite call without a delayed second attempt',function()
        local s,p,e=trial();assert(G.submit(s,{kind='wait'}));assert(G.stun(s,e.id,500))
        resolve(s);assert(e.ai_memory.alarm_spent and #called(s)==0 and not e.pending_attack)
        assert(not AI.choose(s,e).support)
        s,p,e=trial();p.position.x=9;e.hp=3
        assert(G.submit(s,{kind='wait'}));assert(e.pending_attack)
        assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        resolve(s);assert(not G.entity(s,e.id) and #called(s)==0)
    end)
    test('an occupied marked cell blocks that reinforcement without retargeting the other cell',function()
        local s,p,e=trial();assert(G.submit(s,{kind='wait'}))
        local cell=e.pending_attack.target_cells[1]
        G.spawn_enemy(s,'core:enemy/mossbound_guard',cell.x,cell.y,100000)
        local events=resolve(s);local spawned,blocked=0,0
        for _,event in ipairs(events)do
            if event.kind=='AlarmReinforcement'then spawned=spawned+1
            elseif event.kind=='AlarmBlocked'then
                blocked=blocked+1;assert(event.x==cell.x and event.y==cell.y)
            end
        end
        assert(spawned==1 and blocked==1 and #called(s)==1)
    end)
    test('hidden destinations, stairs, exits and map boundaries are never eligible alarm cells',function()
        local s,p,e=trial();local map=require('src.content').map_for(s)
        local hidden=I.choose(s,e,p,{map=map,free=function()return true end,
            visible=function(x,y)return x~=e.position.x or y~=e.position.y end,
            next_step=function()end,adjacent_goals=function()return {}end})
        assert(not hidden.support,'an unseen caller cannot begin a supposedly visible alarm')
        local free=function()return true end
        local cell={floor_id=s.active_floor_id,x=11,y=6}
        assert(not I.alarm_landing(s,e,cell,map,free,function()return false end))
        map=C.decode(C.encode(map));map.placements={stairs={{x=11,y=6}}}
        assert(not I.alarm_landing(s,e,cell,map,free,function()return true end))
        map.placements.stairs={}
        local exit={controller='exit',position=cell};s.entities[#s.entities+1]=exit
        assert(not I.alarm_landing(s,e,cell,map,free,function()return true end))
        table.remove(s.entities);e.position.x,e.position.y=1,1;cell.x,cell.y=1,0
        assert(not I.alarm_landing(s,e,cell,map,free,function()return true end))
    end)
    test('reinforcements have no repeated XP, currency, supply drops or revival records',function()
        local s,p,e=trial();resolve(s)
        local cast=called(s);assert(#cast==2)
        local xp,drops=s.progression.xp_total,#s.drops
        -- Prepared close-combat fixture, not a legal full-run claim.
        for _,actor in ipairs(cast)do
            p.position.x,p.position.y=actor.position.x-1,actor.position.y
            actor.hp=1
            assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
        end
        assert(#called(s)==0 and s.progression.xp_total==xp and #s.drops==drops)
        assert(not s.idea_corpses or #s.idea_corpses==0)
    end)
    test('pending alarm and called actors resume exact ordered events and reject forged budgets or claims',function()
        local s,p,e=trial();assert(G.submit(s,{kind='wait'}))
        local snap=G.snapshot(s)
        local b=G.restore(C.decode(C.encode(snap)))
        equal(resolve(s),resolve(b));equal(G.snapshot(s),G.snapshot(b))
        assert(G.restore(C.decode(C.encode(G.snapshot(s)))))
        for _,v in ipairs(snap.entities)do if v.id==e.id then v.ai_memory.alarm_spent=false end end
        assert(not pcall(G.restore,snap))
        snap=G.snapshot(s)
        for _,v in ipairs(snap.entities)do if v.called_by then v.reward_exempt=false end end
        assert(not pcall(G.restore,snap))
    end)
    test('real generated floors contain Alarm Keepers and the approved keeper and bell resolve in both packs',function()
        local found=false
        for seed=1,50 do
            local s=G.new_stage(seed,{multifloor=true})
            for _,e in ipairs(s.entities)do if e.definition_id=='core:enemy/alarm_keeper'then found=true end end
        end
        assert(found)
        local report=require('src.sprite_audit').collect()
        for _,pack in ipairs({'kenney','onebit'})do
            assert(#report.packs[pack].missing==0)
            local body,bell=false,false
            for _,h in ipairs(report.packs[pack].bindings)do
                if h.id=='core:enemy/alarm_keeper'then assert(h.index==173);body=true end
                if h.id=='core:ui/idea_alarm_effect'then assert(h.index==915);bell=true end
            end
            assert(body and bell)
        end
    end)
end
