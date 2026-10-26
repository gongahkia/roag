-- Explicit assisted prerequisite shared by headless and fresh-process tests.
-- Never used as an ordinary-action Victory or human-play claim.
local G=require('src.game')
local Q=require('src.scheduler')
local C=require('src.content')
local V=require('src.visibility')
local F={}
local dirs={{0,-1},{1,0},{0,1},{-1,0}}
function F.submit(s,intent)
    local ok,why,events=G.submit(s,intent);assert(ok,why);return events
end
local function quiet(s)
    for _,e in ipairs(s.entities)do if e.controller=='ai'then Q.remove_for_actor(s,e.id)end end
end
local function free(s,x,y)
    if not C.walkable(C.map_for(s),x,y)then return false end
    for _,e in ipairs(s.entities)do
        if e.blocks_movement and e.position.floor_id==s.active_floor_id
            and e.position.x==x and e.position.y==y then return false end
    end
    return true
end
local function near(s,e)
    local p=G.player(s)
    for _,d in ipairs(dirs)do
        local x,y=e.position.x+d[1],e.position.y+d[2]
        if free(s,x,y)then
            p.position.x,p.position.y=x,y;V.recompute(s);return -d[1],-d[2]
        end
    end
    error('no prepared approach')
end
function F.ready()
    local s=G.new_stage(8);quiet(s)
    local elite=G.entity(s,s.stage.elite_id);local p=G.player(s)
    p.damage=elite.hp+elite.defense+1 -- explicit assisted prerequisite
    local dx,dy=near(s,elite)
    F.submit(s,{kind='basic_attack',dx=dx,dy=dy})
    quiet(s)
    F.submit(s,{kind='wait'});F.submit(s,{kind='wait'});quiet(s)
    local exit=G.entity(s,s.stage.exit_id);near(s,exit)
    assert(G.route_options(s,exit.id))
    return s,p,exit.id
end
function F.route(s,id)
    return F.submit(s,{kind='choose_route',exit_id=id,route_index=1})
end
function F.burning()
    local s,p,exit=F.ready();local attacker
    for _,d in ipairs(dirs)do
        local x,y=p.position.x+d[1],p.position.y+d[2]
        if free(s,x,y)then attacker=G.spawn_enemy(s,'core:enemy/mossbound_guard',x,y,s.clock);break end
    end
    assert(attacker)
    G.set_boon_stacks(s,attacker.id,'core:boon/emberbrand','common',1)
    F.submit(s,{kind='wait'});F.submit(s,{kind='wait'})
    assert(p.statuses and p.statuses.burning)
    quiet(s)
    return s,p,exit
end
return F
