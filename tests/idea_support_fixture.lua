-- Assisted prerequisites only. The selected interaction is a real paid action.
local G=require('src.game')
local B=require('src.boons')
local C=require('src.content')
local Q=require('src.scheduler')
local V=require('src.visibility')
local F={order={'doorward_lamp','secret_apple','spoils_aegis','mist_lullaby',
    'rime_disk','vent_mantle','patient_rebuke','beacon_afterglow',
    'sentry_quartermaster','spirit_shelter','grounding_rod','mirror_dividend'}}
local classes={beacon_afterglow='wayfinder',sentry_quartermaster='tinkerer',
    spirit_shelter='spiritbinder',grounding_rod='stormcaller',mirror_dividend='ranger'}
local function submit(s,intent)
    local ok,why,events=G.submit(s,intent);assert(ok,why);return events
end
local function quiet(s)
    for i=#s.entities,1,-1 do local e=s.entities[i]
        if e.controller=='ai'then Q.remove_for_actor(s,e.id);table.remove(s.entities,i)end
    end
end
local function blocked(s,x,y)
    for _,e in ipairs(s.entities)do if e.blocks_movement and e.id~=s.player_id
        and e.position.x==x and e.position.y==y then return true end end
    return false
end
local function terrain(symbol)
    for seed=1,20 do
        local s=G.new(seed,symbol=='m' and 'core:map/fractured_archive' or 'core:map/glacial_observatory')
        quiet(s);local map=C.map_for(s)
        for y=2,map.height-1 do for x=2,map.width-1 do
            if map.rows[y]:sub(x,x)==symbol and not blocked(s,x,y)then
                for _,d in ipairs({{0,-1},{1,0},{0,1},{-1,0}})do
                    local nx,ny=x+d[1],y+d[2]
                    if map.rows[ny]:sub(nx,nx)=='.' and not blocked(s,nx,ny)then
                        local p=G.player(s);p.position.x,p.position.y=x,y
                        return s,p,d
                    end
                end
            end
        end end
    end
    error('no natural '..symbol..' edge in fixed terrain corpus')
end
function F.prepare(slug)
    local found;for _,id in ipairs(F.order)do if id==slug then found=true end end;assert(found)
    local s=G.new(37,nil,{class_id=classes[slug] and 'core:actor/'..classes[slug]})
    local p=G.player(s);p.position.x,p.position.y=7,6;p.hp=10
    local f={slug=slug,intent={kind='wait'},boons={slug}}
    local function enemy(x,y,due,definition)
        local e=G.spawn_enemy(s,definition or 'core:enemy/mossbound_guard',x,y,due or 100000)
        e.hp=100;e.max_hp=100;f.enemy=f.enemy or e;return e
    end
    if slug=='doorward_lamp'then
        f.door=G.spawn_door(s,8,6);f.intent={kind='move',dx=1,dy=0}
    elseif slug=='secret_apple'then
        s=G.new_generated(1);quiet(s);p=G.player(s);p.hp=10
        f.secret=s.secrets[1];p.position.x,p.position.y=f.secret.x,f.secret.y
        f.intent={kind='basic_attack',dx=f.secret.wall_x-f.secret.x,dy=f.secret.wall_y-f.secret.y}
    elseif slug=='spoils_aegis'then
        f.chest=G.spawn_chest(s,'core:chest/standard',8,6);s.currency=f.chest.price
        f.chest.offers={{boon_id='core:boon/opening_cut',rarity='common'},
            {boon_id='core:boon/thorn_mirror',rarity='common'},
            {boon_id='core:boon/longblade_geometry',rarity='common'}}
        f.intent={kind='claim_chest',chest_id=f.chest.id,offer_index=1}
    elseif slug=='mist_lullaby' or slug=='rime_disk'then
        local d;s,p,d=terrain(slug=='mist_lullaby' and 'm' or 'i')
        p.hp=10
        if slug=='rime_disk'then f.intent={kind='move',dx=d[1],dy=d[2]}end
    elseif slug=='vent_mantle'then
        f.vent=require('src.idea_terrain').place(s,C.map_for(s),'heat_vent',8,6)
        submit(s,{kind='wait'})
        f.intent={kind='move',dx=1,dy=0}
    elseif slug=='patient_rebuke'then
        p.defense=100;local e=enemy(8,6,0)
        B.set_stacks(p,'core:boon/'..slug,'rare',1)
        for _=1,3 do
            submit(s,{kind='wait'})
            local m=p.boon_memory and p.boon_memory['core:boon/'..slug]
            if m and m.charges==1 then break end
        end
        assert(p.boon_memory['core:boon/'..slug].charges==1)
        Q.remove_for_actor(s,e.id)
    elseif slug=='beacon_afterglow'then
        submit(s,{kind='sweeping_slash',dx=-1,dy=0})
        submit(s,{kind='move',dx=1,dy=0})
        enemy(6,5);f.intent={kind='dash',dx=1,dy=0}
    elseif slug=='sentry_quartermaster' or slug=='spirit_shelter'then
        f.intent={kind='sweeping_slash',dx=1,dy=0}
    elseif slug=='grounding_rod'then
        enemy(9,6);submit(s,{kind='basic_attack',dx=1,dy=0})
        assert(p.storm_charge==1);f.intent={kind='dash',dx=-1,dy=0}
    else
        f.hexer=enemy(10,6,50,'core:enemy/mirror_hexer')
        for _=1,3 do submit(s,{kind='wait'})end
        assert(#s.idea_mirrors==1)
        Q.remove_for_actor(s,f.hexer.id)
        f.enemy=nil;enemy(9,7);f.intent={kind='basic_attack',dx=1,dy=0}
    end
    f.state=s;f.player=p
    B.set_stacks(p,'core:boon/'..slug,'rare',1);V.recompute(s)
    f.hp_before=p.hp;f.clock_before=s.clock
    return f
end
function F.install(f)
    f.player.boons={}
    for _,id in ipairs(f.boons)do B.set_stacks(f.player,'core:boon/'..id,'rare',1)end
end
function F.check(f,events)
    local n=0
    for _,e in ipairs(events)do if e.kind=='BoonActivated' and e.boon_id=='core:boon/'..f.slug then n=n+1 end end
    assert(n==1,'expected one '..f.slug..' activation, got '..n)
    local p=f.player;local m=p.boon_memory and p.boon_memory['core:boon/'..f.slug]
    if f.slug=='secret_apple'then assert(f.secret.revealed and p.hp==f.hp_before+7)
    elseif f.slug=='mist_lullaby'then assert(p.hp==f.hp_before+3 and m.expires_at==f.clock_before+400)
    elseif f.slug=='mirror_dividend'then assert(#f.state.idea_mirrors==0 and p.hp==f.hp_before+4)
    elseif f.slug=='beacon_afterglow'then assert(p.position.x==6 and f.enemy.hp==98)
    elseif f.slug=='patient_rebuke'then assert(m.charges==0 and f.enemy.hp==97)
    elseif f.slug=='sentry_quartermaster'then assert(m.speed==20)
    elseif f.slug=='vent_mantle'then assert(p.hp==f.hp_before and m.ward==0)
    else assert(m and m.ward and m.ward>0)end
end
return F
