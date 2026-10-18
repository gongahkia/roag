-- Reviewed squads. No presentation dependencies or combat RNG draws. All state
-- is ordinary entity data; existing saved floors are never repopulated.
local RNG=require('src.rng')
local Terrain=require('src.terrain')
local S={ward_radius=4,guard_leash=6,rally_radius=5,snare_cap=2,snare_delay=40}
S.order={'warded_bodyguards','healer_rushwings','snares_and_siege',
    'shield_and_archer','captain_and_followers'}
-- id, name, HP, damage, defense, speed, windup, recovery, range
S.enemies={
    {'ward_weaver','Ward Weaver',6,1,0,80,140,140,4},
    {'bodyguard_brute','Bodyguard Brute',10,3,0,70,100,130,1},
    {'mend_acolyte','Mend Acolyte',6,1,0,85,120,150,4},
    {'rushwing','Rushwing',4,1,0,130,60,100,1},
    {'snare_scribe','Snare Scribe',6,1,0,80,125,140,4},
    {'siege_wisp','Siege Wisp',6,2,0,65,155,150,5},
    {'shield_bearer','Shield Bearer',9,2,0,70,100,135,1},
    {'pin_archer','Pin Archer',5,2,0,90,110,135,6},
    {'banner_captain','Banner Captain',8,2,0,75,110,140,1},
    {'banner_follower','Banner Follower',3,1,0,95,90,125,1},
}
S.roles={}
for _,row in ipairs(S.enemies) do S.roles['core:enemy/'..row[1]]=row[1] end
S.groups={
    warded_bodyguards={'ward_weaver','bodyguard_brute',2},
    healer_rushwings={'mend_acolyte','rushwing',2},
    snares_and_siege={'snare_scribe','siege_wisp',1},
    shield_and_archer={'shield_bearer','pin_archer',1},
    captain_and_followers={'banner_captain','banner_follower',2},
}
S.biomes={
    overgrown_ruins={'captain_and_followers'},
    fungal_hollows={'healer_rushwings'},
    sunken_aqueduct={'shield_and_archer'},
    ashen_bastion={'warded_bodyguards','shield_and_archer','captain_and_followers'},
    glacial_observatory={'snares_and_siege'},
    fractured_archive={'warded_bodyguards','snares_and_siege'},
    sundered_forge={'captain_and_followers','healer_rushwings'},
}
S.tiny={ward_weaver=84,bodyguard_brute=110,mend_acolyte=86,rushwing=120,
    snare_scribe=124,siege_wisp=108,shield_bearer=96,pin_archer=87,
    banner_captain=98,banner_follower=97}
S.onebit={ward_weaver=319,bodyguard_brute=270,mend_acolyte=123,rushwing=415,
    snare_scribe=221,siege_wisp=321,shield_bearer=126,pin_archer=78,
    banner_captain=172,banner_follower=174}
S.cues={squad_ward_effect=636,squad_heal_effect=535,squad_snare_effect=715,
    squad_shield_effect=239,squad_rally_effect=527}
local dirs={{0,-1},{1,0},{0,1},{-1,0}}
local function key(x,y) return x..','..y end
function S.distance(a,b) return math.abs(a.x-b.x)+math.abs(a.y-b.y) end
function S.entity(state,id)
    for _,e in ipairs(state.entities) do if e.id==id then return e end end
end
function S.leader(state,e)
    local leader=S.entity(state,e.squad_leader_id)
    if leader and leader.hp and leader.hp>0 and leader.position.floor_id==e.position.floor_id
        and leader.squad_id==e.squad_id then return leader end
end
-- Supercover line: protection never reaches through a wall, closed door,
-- pillar or obscuring mist. Actors themselves do not occlude a support beam.
function S.clear(state,a,b)
    if a.floor_id~=b.floor_id then return false end
    local map=a.floor_id==state.active_floor_id and state.generated_map or
        state.inactive_floor_maps and state.inactive_floor_maps[a.floor_id]
    map=map or require('src.content').get_map(state.map_id)
    if not map then return false end
    local function open(x,y)
        local mark=map.rows[y] and map.rows[y]:sub(x,x)
        local tile=Terrain.definitions[mark]
        if not tile or not tile.projectile_passable or tile.blocks_vision then return false end
        for _,e in ipairs(state.entities) do
            if e.position and e.position.floor_id==a.floor_id and
                e.position.x==x and e.position.y==y and
                (e.blocks_projectiles or e.blocks_vision) then return false end
        end
        return true
    end
    local x,y=a.x,a.y
    local dx,dy=math.abs(b.x-x),math.abs(b.y-y)
    local sx,sy=b.x>x and 1 or -1,b.y>y and 1 or -1
    local err=dx-dy
    while x~=b.x or y~=b.y do
        local ox,oy=x,y;local twice=err*2
        if twice>-dy then x=x+sx;err=err-dy end
        if twice<dx then y=y+sy;err=err+dx end
        if x~=ox and y~=oy and (not open(x,oy) or not open(ox,y)) then return false end
        if not open(x,y) then return false end
    end
    return true
end
function S.ward_source(state,e)
    if S.roles[e.definition_id]~='bodyguard_brute' then return nil end
    local leader=S.leader(state,e)
    if leader and leader.ai_memory.ward_ready and
        (leader.stunned_until or 0)<=state.clock and
        S.distance(leader.position,e.position)<=S.ward_radius and
        S.clear(state,leader.position,e.position) then return leader end
end
function S.rally_source(state,e)
    if S.roles[e.definition_id]~='banner_follower' then return nil end
    local leader=S.leader(state,e)
    if leader and (leader.stunned_until or 0)<=state.clock and
        S.distance(leader.position,e.position)<=S.rally_radius and
        S.clear(state,leader.position,e.position) then return leader end
end
function S.block(state,target,source,direct)
    local ward=S.ward_source(state,target)
    if ward then return 'forcefield',ward.id end
    if direct and S.roles[target.definition_id]=='shield_bearer' and source.position and
        source.position.floor_id==target.position.floor_id and
        (target.stunned_until or 0)<=state.clock then
        local m,p=target.ai_memory,target.position
        local dx,dy=source.position.x-p.x,source.position.y-p.y
        local fx,fy=m.shield_dx or 0,m.shield_dy or 1
        -- Front 90-degree wedge; side and rear hits, DOT and secondary effects bypass.
        local forward=dx*fx+dy*fy
        if forward>0 and forward>=math.abs(dx*fy-dy*fx) then
            return 'directional shield',target.id
        end
    end
end

-- Entire squad must fit an open 5x5 patch inside a non-start/non-elite room.
-- The access flood treats guards as walls and still reaches a side of the
-- caster. This rejects chokepoints rather than promising reaction time.
function S.plan(state,map)
    local pool=S.biomes[(map.biome_id or ''):match('/([^/]+)$')]
    if not pool then return nil end
    local rng=RNG.new(RNG.derive(state.run_seed,'enemy-squad:'..map.floor_id))
    local kind=pool[RNG.next_int(rng,#pool)]
    local roster=S.groups[kind]
    local count=kind=='captain_and_followers' and 1+RNG.next_int(rng,4) or roster[3]
    local blocked={}
    for _,e in ipairs(state.entities) do
        if e.blocks_movement and e.position.floor_id==map.floor_id then
            blocked[key(e.position.x,e.position.y)]=true
        end
    end
    for _,stair in ipairs(map.placements.stairs or {}) do blocked[key(stair.x,stair.y)]=true end
    for _,p in ipairs(map.placements.chests or {}) do blocked[key(p.x,p.y)]=true end
    local offsets={{0,0},{-1,1},{1,1},{-1,0},{1,0},{0,1}}
    for _,room in ipairs(map.rooms) do
        if room.role~='start' and room.role~='elite' then
            -- Use actual room footprint bounds; no assumptions about rotation.
            local marker=(room.markers.enemy or {})[1]
            if marker then
                for oy=-2,2 do for ox=-2,2 do
                    local cx,cy=marker.x+ox,marker.y+oy
                    local fits=S.distance({x=cx,y=cy},{x=map.start_x,y=map.start_y})>=10
                    for yy=cy-2,cy+2 do for xx=cx-2,cx+2 do
                        if not map.rows[yy] or map.rows[yy]:sub(xx,xx)~='.' or
                            blocked[key(xx,yy)] then fits=false end
                    end end
                    if fits then
                        local points={}
                        for i=1,count+1 do
                            points[i]={x=cx+offsets[i][1],y=cy+offsets[i][2]}
                        end
                        return {kind=kind,points=points,room_id=room.id}
                    end
                end end
            end
        end
    end
end
function S.populate(state,map,spawn)
    local plan=S.plan(state,map)
    if not plan then return end -- small/occupied rooms keep their original encounters
    local roster=S.groups[plan.kind]
    local leader
    for i,p in ipairs(plan.points) do
        local e=spawn(state,'core:enemy/'..(i==1 and roster[1] or roster[2]),p.x,p.y)
        leader=leader or e
        e.squad_id=map.floor_id..':squad:'..plan.room_id
        e.squad_kind=plan.kind;e.squad_leader_id=leader.id
        e.squad_home={floor_id=map.floor_id,x=plan.points[1].x,y=plan.points[1].y}
    end
end
function S.validate(state,maps)
    for _,e in ipairs(state.entities) do
        if e.squad_id then
            assert(S.roles[e.definition_id] and type(e.squad_id)=='string' and
                S.groups[e.squad_kind] and type(e.squad_leader_id)=='number' and
                e.squad_leader_id%1==0 and e.squad_leader_id>0 and
                e.squad_leader_id<state.next_entity_id,'invalid saved squad')
            local h=e.squad_home
            local map=h and maps[h.floor_id]
            assert(map and h.floor_id==e.position.floor_id and type(h.x)=='number' and
                h.x%1==0 and type(h.y)=='number' and h.y%1==0 and
                map.rows[h.y] and Terrain.definitions[map.rows[h.y]:sub(h.x,h.x)] and
                Terrain.definitions[map.rows[h.y]:sub(h.x,h.x)].walkable,'invalid squad home')
        end
        if e.snare_delay then assert(e.snare_delay==S.snare_delay,'invalid snare delay') end
        if e.ai_memory and e.ai_memory.ward_ready~=nil then
            assert(S.roles[e.definition_id]=='ward_weaver' and
                type(e.ai_memory.ward_ready)=='boolean','invalid ward state')
        end
        if e.ai_memory and e.ai_memory.shield_dx then
            assert(S.roles[e.definition_id]=='shield_bearer' and
                math.abs(e.ai_memory.shield_dx)+math.abs(e.ai_memory.shield_dy)==1,
                'invalid shield facing')
        end
    end
    local counts,seen={},{}
    for _,trap in ipairs(state.enemy_snares or {}) do
        local map=maps[trap.floor_id]
        assert(map and type(trap.x)=='number' and trap.x%1==0 and
            type(trap.y)=='number' and trap.y%1==0 and map.rows[trap.y] and
            Terrain.definitions[map.rows[trap.y]:sub(trap.x,trap.x)] and
            Terrain.definitions[map.rows[trap.y]:sub(trap.x,trap.x)].walkable and
            type(trap.source_id)=='number','invalid saved enemy snare')
        local source=S.entity(state,trap.source_id)
        assert(source and S.roles[source.definition_id]=='snare_scribe' and
            source.position.floor_id==trap.floor_id,'orphan saved snare')
        local k=trap.floor_id..':'..key(trap.x,trap.y)
        assert(not seen[k],'duplicate saved snare');seen[k]=true
        counts[trap.source_id]=(counts[trap.source_id] or 0)+1
        assert(counts[trap.source_id]<=S.snare_cap,'too many saved snares')
    end
end
return S
