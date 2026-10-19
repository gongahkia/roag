-- A prepared fixture, then ordinary actions through a real checked save store.
-- Run legacy/write from the pre-addition tree to prove old saves and events,
-- rather than merely asserting a new->new round trip.
local G=require('src.game')
local P=require('src.persistence')
local R=require('src.rewards')
local Catalog=require('src.boon_content')
local mode,dir,scenario=arg[1],arg[2],arg[3]
local form_name=scenario and scenario:match('^form_([a-z_]+)$')
local form=form_name and require('src.boon_patterns').weapon_forms[form_name]
local cadence=scenario and scenario:match('^cadence_([a-z_]+)$')
assert((mode=='write' or mode=='continue') and
    dir:match('^/var/tmp/roag%-boons%-disk%-[%w%-]+$'))
assert(scenario=='supply' or scenario=='legacy' or scenario=='vow' or scenario=='dice'
    or scenario=='handoff' or scenario=='patterns' or scenario=='controls' or form or cadence)
local names={['active-a.roag']=true,['active-b.roag']=true,
    ['expected.data']=true,['events.data']=true,['plan.data']=true}
local storage={}
function storage:read(name)
    assert(names[name]);local f=io.open(dir..'/'..name,'rb')
    if not f then return end
    local bytes=f:read('*a');f:close();return bytes
end
function storage:write(name,bytes)
    assert(names[name]);local f=assert(io.open(dir..'/'..name,'wb'))
    assert(f:write(bytes));assert(f:close());return true
end
local store=P.new(storage)
local function submit(s,intent)
    local ok,why,events=G.submit(s,intent);assert(ok,why);return events
end
local function advance(s,plan)
    local events={}
    for _,intent in ipairs(plan)do events[#events+1]=submit(s,intent)end
    return P.encode(events)
end
local function save_expected(s,plan,label)
    assert(store:save(s));storage:write('plan.data',P.encode(plan))
    local restored=G.restore(G.snapshot(s))
    storage:write('events.data',advance(restored,plan))
    storage:write('expected.data',P.encode(G.snapshot(restored)))
    print('PASS '..scenario..': '..label..', generation 1')
end
if mode=='write' then
    if cadence then
        local F=require('tests.idea_cadence_fixture');local f=F.prepare(cadence)
        F.check(f,submit(f.state,f.intent))
        save_expected(f.state,{{kind='wait'},{kind='basic_attack',dx=1,dy=0},
            {kind='wait'},{kind='move',dx=0,dy=-1},{kind='wait'}},
            'saved real cadence proc, status/cooldown memory and exact later actions')
        return
    end
    if form then
        local s=G.new(37,'core:map/scrolling_room',{class_id=form.class_id})
        local p=G.player(s);p.position.x,p.position.y=15,16
        G.set_boon_stacks(s,p.id,'core:boon/'..form_name,'rare',1)
        G.set_boon_stacks(s,p.id,'core:boon/temporal_echo','common',1)
        G.set_boon_stacks(s,p.id,'core:boon/emberbrand','common',1)
        local foe=G.spawn_enemy(s,'core:enemy/mossbound_guard',16,16,100000)
        foe.hp=1000;foe.max_hp=1000
        submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(next(s.delayed_effects)and foe.statuses.burning)
        local kind=form.ability_id==require('src.classes').get(form.class_id).special_id
            and 'sweeping_slash' or 'basic_attack'
        save_expected(s,{{kind='wait'},{kind=kind,dx=1,dy=0},
            {kind='wait'},{kind='move',dx=0,dy=-1},{kind='basic_attack',dx=1,dy=0}},
            'saved class weapon form with pending Echo/Burning and later real transformed attack')
        return
    end
    if scenario=='controls'then
        local s=G.new(37);local p=G.player(s);p.hp=10
        for _,slug in ipairs({'hourglass_clasp','ward_ledger','scavengers_hook','cinder_stopper','emberbrand'})do
            G.set_boon_stacks(s,p.id,'core:boon/'..slug,'rare',1)
        end
        G.set_boon_stacks(s,p.id,'core:boon/beggars_cap','rare',1)
        p.boon_memory={['core:boon/beggars_cap']={ward=4,expires_at=500}}
        local foe=G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,100000)
        foe.hp=100;foe.max_hp=100
        R.spawn_currency(s,2,s.active_floor_id,2,3,function()end,p.id)
        submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(p.boon_memory['core:boon/hourglass_clasp'].expires_at==400
            and foe.statuses.burning)
        save_expected(s,{{kind='wait'},{kind='basic_attack',dx=1,dy=0},
            {kind='wait'},{kind='basic_attack',dx=1,dy=0},{kind='wait'}},
            'saved cooldown, one-hit ward, Burning, adjacent pickup and real later control')
        return
    end
    if scenario=='patterns'then
        local s=G.new(37);local p=G.player(s);p.position.x,p.position.y=7,6
        for _,slug in ipairs({'backtrail_mantle','linebreaker_pawn','crosswind','temporal_echo'})do
            G.set_boon_stacks(s,p.id,'core:boon/'..slug,'rare',1)
        end
        local foe=G.spawn_enemy(s,'core:enemy/mossbound_guard',7,5,100000)
        foe.hp=100;foe.max_hp=100
        submit(s,{kind='dash',dx=1,dy=0})
        assert(#G.pending_boon_effects(s)==1 and s.clock<150)
        save_expected(s,{{kind='wait'},{kind='basic_attack',dx=-1,dy=0},
            {kind='wait'},{kind='move',dx=-1,dy=0},{kind='wait'}},
            'saved locked Dash wake, paid Wait rays and pending continuation')
        return
    end
    if scenario=='handoff' then
        local F=require('tests.idea_boon_handoff_fixture')
        local s,p,exit=F.burning()
        G.set_boon_stacks(s,p.id,'core:boon/road_flask','rare',1)
        p.boon_memory={['core:boon/road_flask']={speed=20,expires_at=s.clock+120}}
        F.route(s,exit);p=G.player(s)
        assert(s.clock==0 and p.statuses.burning and
            p.boon_memory['core:boon/road_flask'].expires_at==120)
        save_expected(s,{{kind='wait'},{kind='wait'},{kind='wait'}},
            'saved actual traveling Burning with rebased timer/schedule and frozen retired floor')
        return
    end
    local pool={}
    for n,id in ipairs(Catalog.normal_order())do
        if scenario~='legacy' or n<=49 then pool[#pool+1]=id end
    end
    local s=G.new(scenario=='dice' and 4 or 37,nil,{available_boons=pool});local p=G.player(s)
    local function grant(slug)G.set_boon_stacks(s,p.id,'core:boon/'..slug,'rare',1)end
    grant('temporal_echo');grant('emberbrand')
    local foe=G.spawn_enemy(s,'core:enemy/mossbound_guard',4,2,100000)
    foe.hp=100;foe.max_hp=100
    p.hp=20
    if scenario=='legacy' then
        grant('bloodrush');grant('betrayal_ward')
        p.boon_memory={['core:boon/bloodrush']={speed=20,expires_at=600},
            ['core:boon/betrayal_ward']={ward=3,expires_at=600}}
        R.spawn_supply(s,'food',s.active_floor_id,3,2,function()end,p.id)
        submit(s,{kind='move',dx=1,dy=0})
    elseif scenario=='supply' then
        for _,id in ipairs(require('src.idea_boon_content').order)do
            G.set_boon_stacks(s,p.id,id,'rare',1)
        end
        R.spawn_supply(s,'health',s.active_floor_id,3,2,function()end,p.id)
        submit(s,{kind='move',dx=1,dy=0})
        p.hp=20
        R.spawn_supply(s,'food',s.active_floor_id,2,2,function()end,p.id)
        submit(s,{kind='move',dx=-1,dy=0})
        submit(s,{kind='move',dx=1,dy=0})
        assert(p.boon_memory['core:boon/ration_reserve'].charges==1)
        assert(p.boon_memory['core:boon/road_flask'].speed==20)
    else
        if scenario=='dice' then grant('fateful_dice')else grant('pilgrims_vow')end
        R.spawn_supply(s,'food',s.active_floor_id,3,2,function()end,p.id)
        submit(s,{kind='move',dx=1,dy=0})
    end
    submit(s,{kind='basic_attack',dx=1,dy=0,vow=scenario=='vow' or nil})
    if scenario=='vow' then assert(p.hp==22)end
    if scenario=='dice' then
        assert(p.boon_memory['core:boon/fateful_dice'].face==4
            and p.boon_memory['core:boon/fateful_dice'].ward==3)
    end
    assert(next(s.delayed_effects) and foe.statuses.burning)
    local plan={{kind='wait'},{kind='basic_attack',dx=1,dy=0,vow=scenario=='vow' or nil},
        {kind='move',dx=-1,dy=0},{kind='wait'},{kind='move',dx=1,dy=0}}
    save_expected(s,plan,'saved pending Echo/Burning and finite boon state')
else
    local s=assert(store:load())
    local plan=P.decode(assert(storage:read('plan.data')))
    assert(advance(s,plan)==storage:read('events.data'),'events or timing changed after restart')
    assert(P.encode(G.snapshot(s))==storage:read('expected.data'),'authoritative continuation changed')
    assert(store:save(s));assert(store:inspect().record.seq==2)
    print('PASS '..scenario..': fresh process exact continuation, generation 2')
end
