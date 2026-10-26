-- Natural generated-floor probe. Reads the map, but changes gameplay only
-- through the same public Game.submit actions as a player. No engine/window.
local G=require('src.game')
local I=require('src.idea_interactions')
local C=require('src.content')
local P=require('src.persistence')
local Play=require('tests.legal_playthrough')
local wanted={'offering_font','ration_cache','cleanse_flask','escape_scroll',
    'return_token','charm_well','bell','falling_fixture','route_sign','camp',
    'attunement_forge','travelling_trader','memorial','archive_contract'}
local candidates={}
for _,kind in ipairs(wanted)do candidates[kind]={}end
for seed=1,36 do
    local s=G.new_stage(seed,{multifloor=true,class_id='core:actor/sentinel'})
    local p=G.player(s).position
    for _,n in ipairs(s.idea_rooms.nodes)do
        local list=candidates[n.kind]
        if list and n.floor_id==s.active_floor_id and (n.kind~='bell' or n.bell==1)then
            list[#list+1]={seed=seed,id=n.id,distance=math.abs(p.x-n.x)+math.abs(p.y-n.y)}
        end
    end
end
local function action(run,intent)
    local ok,why=Play.action(run,intent);assert(ok,why)
end
local function collect_kill(run)
    local e=assert(Play.nearest(run.state,function(v)
        return v.controller=='ai' and v.id~=run.state.stage.elite_id and not v.invulnerable and not v.reward_exempt
    end),'no ordinary enemy to earn supplies')
    local okay,why=Play.fight(run,e.id,160);assert(okay,why)
    okay,why=Play.navigate(run,function(at)return at.x==e.position.x and at.y==e.position.y end,160)
    assert(okay,why)
end
local function gain_boon(run)
    for _ in pairs(G.player(run.state).boons)do return end
    local chest=assert(Play.nearest(run.state,function(e)return e.controller=='chest' and not e.claimed end),'no chest')
    for _=1,8 do if run.state.currency>=chest.price then break end;collect_kill(run)end
    local okay,why=Play.navigate(run,function(at)return Play.cardinal(at,chest.position)end,240);assert(okay,why)
    action(run,{kind='claim_chest',chest_id=chest.id,offer_index=1})
end
local function operate(run,n)
    local s=run.state
    if n.kind=='route_sign'then
        local before=P.encode(G.snapshot(s));local view=assert(G.interaction_options(s,n.id))
        assert(#view.entries==0 and P.encode(G.snapshot(s))==before);return
    end
    if n.kind=='bell'then
        local group=s.idea_rooms.groups[n.group]
        for _,number in ipairs(group.sequence)do
            local bell
            for _,v in ipairs(s.idea_rooms.nodes)do if v.kind=='bell' and v.group==n.group and v.bell==number then bell=v end end
            local ok,why=Play.navigate(run,function(at)return math.abs(at.x-bell.x)+math.abs(at.y-bell.y)<=1 end,100)
            assert(ok,why);action(run,{kind='interact',node_id=bell.id,choice_id='ring'})
        end
        assert(group.completed);return
    end
    if n.kind=='attunement_forge' or n.kind=='memorial'then
        local view=assert(G.interaction_options(s,n.id));local entry=assert(view.entries[1],'no owned stack')
        if n.kind=='attunement_forge'then
            local alternatives=assert(G.interaction_options(s,n.id,entry.select));local chosen
            for _,e in ipairs(alternatives.entries)do if e.enabled then chosen=e;break end end
            assert(chosen,'no affordable compatible forge choice')
            action(run,{kind='interact',node_id=n.id,choice_id=chosen.id,selection=entry.select})
        else
            assert(entry.enabled,'memorial correctly disabled at full HP')
            action(run,{kind='interact',node_id=n.id,choice_id=entry.id})
        end
        assert(n.spent);return
    elseif n.kind=='travelling_trader'then
        local view=assert(G.interaction_options(s,n.id));local choice
        for _,entry in ipairs(view.entries)do if entry.enabled then choice=entry;break end end
        assert(choice,'no affordable trader service')
        action(run,{kind='interact',node_id=n.id,choice_id=choice.id,replace=choice.replace_required or nil})
        assert(n.spent);return
    elseif n.kind=='archive_contract'then
        action(run,{kind='interact',node_id=n.id,choice_id='accept'})
        for _,id in ipairs(n.targets)do local okay,why=Play.fight(run,id,180);assert(okay,why)end
        for _=1,12 do if n.status~='active'then break end;action(run,{kind='wait'})end
        assert(n.status=='completed','contract '..n.status..': '..(n.failure or 'targets unsettled'));return
    end
    local choice=n.kind=='offering_font' and '1' or (n.kind=='ration_cache' or n.kind=='camp') and 'ward'
        or n.kind=='charm_well' and 'focus' or n.kind=='falling_fixture' and 'trigger' or 'take'
    action(run,{kind='interact',node_id=n.id,choice_id=choice})
    assert(n.spent)
    if n.kind=='escape_scroll'then
        local used=false
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
            if G.preview(s,'use_utility',d[1],d[2],1).valid then
                action(run,{kind='use_utility',dx=d[1],dy=d[2],steps=1});used=true;break
            end
        end
        assert(used and not G.player(s).utility,'no visible clear escape step')
    elseif n.kind=='return_token'then
        action(run,{kind='use_utility'})
        local marker=G.player(s).utility.planted;local moved=false;local map=C.map_for(s)
        for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
            local x,y=marker.x+d[1],marker.y+d[2]
            local r=I.room_at(map,x,y);local view=G.preview(s,'move',d[1],d[2])
            if r and r.id==marker.room.id and view.valid and view.distance==1 then
                action(run,{kind='move',dx=d[1],dy=d[2]});moved=true;break
            end
        end
        assert(moved,'no clear same-room departure');action(run,{kind='use_utility'})
        assert(not G.player(s).utility and G.player(s).position.x==marker.x and G.player(s).position.y==marker.y)
    end
end
for _,kind in ipairs(wanted)do
    local list=candidates[kind]
    table.sort(list,function(a,b)if a.distance~=b.distance then return a.distance<b.distance end;return a.seed<b.seed end)
    local passed=false
    for attempt=1,math.min(6,#list)do
        local candidate=list[attempt]
        local run=Play.new(candidate.seed,{multifloor=true,class_id='core:actor/sentinel'})
        local initial=G.snapshot(run.state);local n=assert(I.node(run.state,candidate.id))
        local ok,why=pcall(function()
            if kind=='attunement_forge' or kind=='memorial'then gain_boon(run)end
            local arrived,reason=Play.navigate(run,function(at)
                if n.kind=='falling_fixture'then return at.x==n.x-1 and at.y==n.y end
                return math.abs(at.x-n.x)+math.abs(at.y-n.y)<=1
            end,240)
            assert(arrived,reason);operate(run,n)
            assert(not run.state.game_over,'died during service')
            -- Continue from an in-memory checked snapshot before half the trace,
            -- then compare every subsequent public action's exact event bytes.
            local replay=G.restore(initial);local offset=0
            for index,record in ipairs(run.trace)do
                if index==math.max(1,math.floor(#run.trace/2))then replay=G.restore(G.snapshot(replay))end
                local good,reason,events=G.submit(replay,record.intent)
                assert(good==record.ok and reason==record.reason,'replay result changed')
                for _,event in ipairs(events)do offset=offset+1;assert(P.encode(event)==P.encode(run.events[offset]),'replay event changed')end
            end
            assert(offset==#run.events and P.encode(G.snapshot(replay))==P.encode(G.snapshot(run.state)),'replay snapshot changed')
        end)
        local p=G.player(run.state)
        if ok then
            print(('PASS %s: seed=%d class=sentinel actions=%d time=%d HP=%d/%d gold=%d events=%d exact replay/Continue'):format(
                kind,candidate.seed,#run.trace,run.state.clock,p.hp,p.max_hp,run.state.currency,#run.events))
            passed=true;break
        end
        print(('ATTEMPT %s: seed=%d actions=%d HP=%d reason=%s'):format(kind,candidate.seed,#run.trace,p.hp,tostring(why)))
    end
    assert(passed,'no successful legal natural scenario for '..kind)
end
print('14 natural interaction scenarios passed; map-aware driver, no HP/position/reward assistance; no LÖVE initialized')
