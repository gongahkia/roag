-- Two distinct LuaJIT processes, real rotating run envelopes, no LÖVE/window.
local Game=require('src.game')
local P=require('src.persistence')
local mode,dir=arg[1],arg[2]
local scenario=arg[3] or 'rammer'
assert(scenario=='rammer' or scenario=='wraith' or scenario=='cantor' or scenario=='toad'
    or scenario=='cache' or scenario=='bones' or scenario=='alarm' or scenario=='web'
    or scenario=='oath' or scenario=='sentinel' or scenario=='mirror' or scenario=='silt' or scenario=='moth','unknown fixture scenario')
assert((mode=='write' or mode=='continue' or mode=='cross' and (scenario=='web' or scenario=='mirror' or scenario=='moth')) and type(dir)=='string'
    and dir:match('^/var/tmp/roag%-idea%-disk%-[%w%-]+$'),'isolated fixture directory required')
local allowed={['active-a.roag']=true,['active-b.roag']=true,
    ['expected.roag-data']=true,['expected-events.roag-data']=true}
local storage={}
function storage:read(name)
    assert(allowed[name]);local f=io.open(dir..'/'..name,'rb');if not f then return end
    local bytes=f:read('*a');f:close();return bytes
end
function storage:write(name,bytes)
    assert(allowed[name]);local f=assert(io.open(dir..'/'..name,'wb'))
    assert(f:write(bytes));assert(f:close());return true
end
local store=P.new(storage)
local function advance(s)
    local events={}
    for _=1,scenario=='alarm' and 3 or (scenario=='cache' or scenario=='web' or scenario=='sentinel' or scenario=='mirror' or scenario=='silt' or scenario=='moth') and 2 or 1 do
        local ok,_,list=Game.submit(s,{kind='wait'});assert(ok)
        for _,event in ipairs(list)do events[#events+1]=event end
    end
    return events
end
if mode=='write' then
    local s=Game.new(37,nil,{class_id=scenario=='mirror' and 'core:actor/ranger' or nil});local p=Game.player(s)
    local enemy
    if scenario=='moth'then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/ember_moth',10,6,50)
    elseif scenario=='silt'then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/silt_worm',10,6,50)
    elseif scenario=='mirror'then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/mirror_hexer',10,6,50)
        Game.spawn_enemy(s,'core:enemy/mossbound_guard',9,7,100000)
    elseif scenario=='sentinel'then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/rotating_sentinel',8,6,50)
    elseif scenario=='oath' then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/oath_twin_a',8,6,100000)
        local other=Game.spawn_enemy(s,'core:enemy/oath_twin_b',9,6,100000)
        enemy.oath_partner_id=other.id;other.oath_partner_id=enemy.id
        assert(Game.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(enemy.hp==6 and other.hp==6)
    elseif scenario=='web' then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/web_weaver',10,6,50)
    elseif scenario=='alarm' then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/alarm_keeper',10,6,50)
    elseif scenario=='bones' then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/bone_splitter',8,6,100000);enemy.hp=3
        assert(Game.submit(s,{kind='basic_attack',dx=1,dy=0}))
    elseif scenario=='cache' then
        p.position.x,p.position.y=8,6
        enemy=Game.spawn_enemy(s,'core:enemy/haunted_cache',10,6,50)
        assert(Game.submit(s,{kind='wait'}))
    elseif scenario=='toad' then
        p.position.x,p.position.y=8,6
        enemy=Game.spawn_enemy(s,'core:enemy/gulp_toad',10,6,50);enemy.hp=3
        require('src.rewards').spawn_currency(s,7,s.active_floor_id,9,6,function()end)
    elseif scenario=='cantor' then
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/grave_cantor',10,6,200)
        local victim=Game.spawn_enemy(s,'core:enemy/mossbound_guard',8,6,100000)
        victim.hp=3;victim.max_hp=6
        assert(Game.submit(s,{kind='basic_attack',dx=1,dy=0}))
        assert(Game.submit(s,{kind='wait'}))
    elseif scenario=='wraith' then
        p.position.x,p.position.y=8,7
        enemy=Game.spawn_enemy(s,'core:enemy/hollow_wraith',5,7,0)
    else
        p.position.x,p.position.y=7,6
        enemy=Game.spawn_enemy(s,'core:enemy/heavy_rammer',6,6,0)
    end
    if scenario~='bones' and scenario~='oath' then
        assert(Game.submit(s,scenario=='cache' and {kind='move',dx=1,dy=0} or {kind='wait'}))
    end
    assert(s.clock==(scenario=='cache' and 200 or scenario=='cantor' and 300 or scenario=='mirror' and 87 or 100)
        and (scenario=='bones' or scenario=='oath' or enemy.pending_attack.due==(scenario=='moth' and 230 or scenario=='silt' and 215 or scenario=='mirror' and 226 or scenario=='sentinel' and 250 or scenario=='web' and 206 or scenario=='alarm' and 343 or scenario=='cache' and 370 or scenario=='cantor' and 388 or scenario=='wraith' and 111
            or scenario=='toad' and 175 or 157)))
    assert(store:save(s));assert(store:inspect().record.seq==1)
    local expected=Game.restore(Game.snapshot(s))
    local events=advance(expected)
    storage:write('expected.roag-data',P.encode(Game.snapshot(expected)))
    storage:write('expected-events.roag-data',P.encode(events))
    print('PASS disk write: pending '..scenario..' saved in generation 1')
elseif mode=='continue' then
    local s=assert(store:load());assert(s.clock==(scenario=='cache' and 200 or scenario=='cantor' and 300 or scenario=='mirror' and 87 or 100))
    local events=advance(s)
    assert(P.encode(events)==storage:read('expected-events.roag-data'))
    assert(P.encode(Game.snapshot(s))==storage:read('expected.roag-data'))
    assert(Game.player(s).position.x==(scenario=='cache' and 9 or
        (scenario=='cantor' or scenario=='bones' or scenario=='alarm' or scenario=='web' or scenario=='oath' or scenario=='sentinel' or scenario=='mirror' or scenario=='silt' or scenario=='moth') and 7 or 8)
        and s.clock==(scenario=='mirror' and 261 or (scenario=='web' or scenario=='sentinel' or scenario=='silt' or scenario=='moth') and 300 or (scenario=='cantor' or scenario=='cache' or scenario=='alarm') and 400 or 200))
    if scenario=='moth'then
        assert(#s.idea_embers==1 and s.idea_embers[1].next_due==330 and Game.player(s).hp==22)
    elseif scenario=='silt'then
        assert(#s.idea_silt==2 and s.idea_silt[1].expires==515
            and s.idea_silt[1].x==10 and s.idea_silt[2].x==9)
    elseif scenario=='mirror'then
        assert(#s.idea_mirrors==1 and s.idea_mirrors[1].expires==626)
    elseif scenario=='sentinel'then
        local e
        for _,v in ipairs(s.entities)do if v.definition_id=='core:enemy/rotating_sentinel'then e=v end end
        assert(e and e.ai_memory.sentinel_facing==4 and e.ai_memory.sentinel_strikes==1
            and not e.pending_attack and Game.player(s).hp==22)
    elseif scenario=='oath' then
        local count=0
        for _,e in ipairs(s.entities)do if require('src.idea_oaths').is_twin(e)then
            assert(require('src.idea_oaths').partner(s,e) and e.hp==6);count=count+1
        end end
        assert(count==2)
    elseif scenario=='web' then
        assert(#s.idea_webs==1 and s.idea_webs[1].expires==606)
    elseif scenario=='alarm' then
        local count=0
        for _,e in ipairs(s.entities)do if e.called_by then
            assert(e.reward_exempt and e.definition_id=='core:enemy/ruin_skitter');count=count+1
        end end
        assert(count==2 and s.progression.xp_total==0)
    elseif scenario=='bones' then
        local count=0
        for _,e in ipairs(s.entities)do if e.fragment_of then
            assert(e.reward_exempt and e.hp==2 and e.max_hp==2);count=count+1
        end end
        assert(count==2 and s.progression.xp_total==4)
    elseif scenario=='cache' then
        assert(Game.player(s).hp==22,'ordinary announced bite must resolve after Continue')
    elseif scenario=='cantor' then
        local found=false
        for _,e in ipairs(s.entities)do if e.revival_of then
            assert(e.reward_exempt and e.hp==3);found=true
        end end
        assert(found and #s.idea_corpses==0 and s.progression.xp_total==5)
    elseif scenario=='toad' then
        local found=false
        for _,e in ipairs(s.entities)do if e.carried_drop_id then
            assert(e.definition_id=='core:enemy/gulp_toad' and e.carried_drop_id==s.drops[1].id)
            found=true
        end end
        assert(found and not s.drops[1].collected and s.currency==0)
    end
    if scenario=='wraith' then
        local found=false
        for _,e in ipairs(events)do if e.kind=='Displaced' and e.reason=='phase' then found=true end end
        assert(found and Game.player(s).hp==24)
    end
    assert(store:save(s));assert(store:inspect().record.seq==2)
    assert(storage:read('active-a.roag') and storage:read('active-b.roag'))
    print('PASS fresh-process Continue: exact events, RNG/state and generation 2')
    if scenario=='web' or scenario=='mirror' or scenario=='moth'then
        local expected=Game.restore(Game.snapshot(s))
        local ok,_,ev=Game.submit(expected,scenario=='moth' and {kind='wait'}
            or {kind=scenario=='mirror' and 'basic_attack' or 'move',dx=1,dy=0});assert(ok)
        storage:write('expected.roag-data',P.encode(Game.snapshot(expected)))
        storage:write('expected-events.roag-data',P.encode(ev))
    end
else
    local s=assert(store:load())
    assert(scenario=='mirror' and s.clock==261 and #s.idea_mirrors==1
        or scenario=='web' and s.clock==300 and #s.idea_webs==1
        or scenario=='moth' and s.clock==300 and #s.idea_embers==1)
    local ok,_,ev=Game.submit(s,scenario=='moth' and {kind='wait'}
        or {kind=scenario=='mirror' and 'basic_attack' or 'move',dx=1,dy=0});assert(ok)
    if scenario=='moth'then
        assert(s.clock==400 and Game.player(s).hp==21 and #s.idea_embers==1
            and s.idea_embers[1].remaining==1 and s.idea_embers[1].next_due==430)
    elseif scenario=='mirror'then
        assert(s.clock==348 and #s.idea_mirrors==0)
        local hit=false
        for _,v in ipairs(ev)do if v.kind=='DamageTaken'then
            assert(v.source_id==s.player_id and v.x==9 and v.y==7 and v.actual_loss==2);hit=true
        end end
        assert(hit)
    else assert(s.clock==450 and #s.idea_webs==0)end
    assert(P.encode(ev)==storage:read('expected-events.roag-data'))
    assert(P.encode(Game.snapshot(s))==storage:read('expected.roag-data'))
    assert(store:save(s));assert(store:inspect().record.seq==3)
    print(scenario=='moth' and 'PASS third-process ember pulse: exact fire damage, RNG and checked generation 3'
        or scenario=='mirror' and 'PASS third-process reflected shot: exact original damage, RNG and checked generation 3'
        or 'PASS third-process web crossing: exact +50 recovery and checked generation 3')
end
