-- Shared assisted prerequisites for actual-key GUI and separate-process saves.
local G=require('src.game')
local B=require('src.boons')
local P=require('src.persistence')
local Content=require('src.content')
local R=require('src.rewards')
local V=require('src.visibility')
local Q=require('src.scheduler')
local F={order={'threefold_remedy','twin_seals','fourfold_ledger','stone_backstop',
    'borrowed_guard','long_wick','passing_ember','purging_seal'}}
local function grant(s,slug,tier)B.set_stacks(G.player(s),'core:boon/'..slug,tier or 'rare',1)end
local function submit(s,intent)
    local ok,why,events=G.submit(s,intent);assert(ok,why);return events
end
function F.prepare(slug)
    local found;for _,v in ipairs(F.order)do if v==slug then found=true end end;assert(found)
    local class=(slug=='threefold_remedy' or slug=='twin_seals')and 'core:actor/berserker' or nil
    local s=G.new(37,'core:map/scrolling_room',{class_id=class});local p=G.player(s)
    p.position.x,p.position.y=15,16;p.hp=10
    local f={state=s,player=p,slug=slug,boons={slug},intent={kind='basic_attack',dx=1,dy=0}}
    local function add(slug,tier)f.boons[#f.boons+1]={slug,tier};grant(s,slug,tier)end
    local function enemy(dx,dy,hp)
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',15+dx,16+dy,100000)
        e.hp=hp or 100;e.max_hp=e.hp;f.enemy=f.enemy or e;return e
    end
    if slug=='threefold_remedy' or slug=='twin_seals'then
        enemy(1,0,slug=='twin_seals' and 1 or 100)
        enemy(1,1,slug=='twin_seals' and 1 or 100)
        if slug=='threefold_remedy'then enemy(1,-1)end
        f.intent.kind='sweeping_slash'
    elseif slug=='fourfold_ledger'then
        p.hp=1;p.max_hp=100
        for _,v in ipairs({'beggars_cap','road_flask','ration_reserve','field_dressing'})do add(v)end
        R.spawn_currency(s,1,s.active_floor_id,16,16,function()end,p.id)
        R.spawn_supply(s,'food',s.active_floor_id,16,16,function()end,p.id)
        R.spawn_supply(s,'health',s.active_floor_id,16,16,function()end,p.id)
        f.intent.kind='move'
    elseif slug=='stone_backstop'then
        enemy(1,0)
        G.spawn_door(s,17,16)
    elseif slug=='borrowed_guard'then enemy(1,0).defense=100
    elseif slug=='long_wick'then enemy(1,0);add('emberbrand','common')
    elseif slug=='passing_ember'then
        local e=enemy(1,0);grant(s,'emberbrand','common')
        submit(s,f.intent);B.set_stacks(p,'core:boon/emberbrand','common',0);e.hp=1
        f.next_enemy=enemy(1,1)
    else
        local e=enemy(1,0);e.defense=0
        G.set_boon_stacks(s,e.id,'core:boon/emberbrand','common',1)
        Q.remove_for_actor(s,e.id)
        Q.enqueue(s,{kind='actor_ready',due=0,actor_id=e.id,is_player=false})
        submit(s,{kind='wait'});submit(s,{kind='wait'});assert(p.statuses.burning)
        Q.remove_for_actor(s,e.id)
        for n,a in ipairs(s.entities)do if a.id==e.id then table.remove(s.entities,n);break end end
        f.enemy=nil;enemy(1,-1);add('field_dressing')
        R.spawn_supply(s,'health',s.active_floor_id,16,16,function()end,p.id)
        f.intent.kind='move'
    end
    grant(s,slug);V.recompute(s);return f
end
function F.install(f)
    -- GUI autostart deliberately installs its showcase after new_stage.
    -- Reinstall only the chosen fixture inventory once that has happened.
    f.player.boons={}
    for _,v in ipairs(f.boons)do
        if type(v)=='table'then grant(f.state,v[1],v[2])else grant(f.state,v)end
    end
end
function F.check(f,events)
    local n=0
    for _,e in ipairs(events)do if e.kind=='BoonActivated' and e.boon_id=='core:boon/'..f.slug then n=n+1 end end
    assert(n==1,'expected one '..f.slug..' activation, got '..n)
    local p=f.player;local memory=p.boon_memory and p.boon_memory['core:boon/'..f.slug]
    if f.slug=='threefold_remedy'then assert(p.hp==13)
    elseif f.slug=='twin_seals'then assert(memory.ward==4)
    elseif f.slug=='fourfold_ledger'then assert(p.hp==22)
    elseif f.slug=='stone_backstop'then assert(f.enemy.hp==94)
    elseif f.slug=='borrowed_guard'then assert(memory.ward==3)
    elseif f.slug=='long_wick'then assert(f.enemy.statuses.burning.remaining==5)
    elseif f.slug=='passing_ember'then assert(f.next_enemy.statuses.burning)
    else assert(not p.statuses.burning and f.enemy.hp==98)end
end
return F
