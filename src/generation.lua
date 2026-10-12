-- Concrete floors built from biome-specific authored rooms and graph geometry.
-- No LÖVE state, callbacks, or runtime combatants are stored here.
local Content = require("src.content")
local RNG = require("src.rng")
local Rooms = require("src.rooms")
local Biomes = require("src.biomes")
local World = require("src.world")
local StudioWorld = require("src.studio_world")

local Generation = {}
Generation.map_id = "core:map/overgrown_ruins"
Generation.floor_id = Rooms.biome.floor_id
Generation.room_content_version = Rooms.version
Generation.width, Generation.height = Rooms.biome.width, Rooms.biome.height
Generation.room_min, Generation.room_max = Rooms.biome.room_min, Rooms.biome.room_max
Generation.chest_target = Rooms.biome.standard_chests
function Generation.biome_for_map(map_id) return Biomes.for_map(map_id) end
local active_studio_pack
local function pack_fingerprint(pack)
    if not pack then return nil end
    local bytes=StudioWorld.encode(pack)
    local a,b=1,0
    for i=1,#bytes do a=(a+bytes:byte(i))%65521;b=(b+a)%65521 end
    return string.format('%08x:%d',b*65536+a,#bytes)
end
function Generation.install_studio_pack(pack)
    if pack then StudioWorld.validate(pack) end
    active_studio_pack=pack
end
function Generation.studio_pack_fingerprint() return pack_fingerprint(active_studio_pack) end

local dirs = {{-1,0},{1,0},{0,-1},{0,1}}
local function key(x,y) return x .. "," .. y end
local function slot(c,r) return (r-1)*4+c end
local function col(id) return (id-1)%4+1 end
local function row(id) return math.floor((id-1)/4)+1 end
local function neighbors(id)
    local result={}
    for _,d in ipairs(dirs) do
        local c,r=col(id)+d[1],row(id)+d[2]
        if c>=1 and c<=4 and r>=1 and r<=3 then result[#result+1]=slot(c,r) end
    end
    table.sort(result)
    return result
end
local function edge_key(a,b)
    if a>b then a,b=b,a end
    return a .. ":" .. b
end
local function add_edge(graph, seen, a,b,kind)
    local id=edge_key(a,b)
    if seen[id] then return false end
    seen[id]=true
    graph.edges[#graph.edges+1]={a=a,b=b,kind=kind,id=id}
    return true
end

function Generation.plan(rng,biome)
    local target=Generation.room_min+RNG.next_int(rng,
        Generation.room_max-Generation.room_min+1)-1
    local graph={nodes={},edges={},critical={1}}
    local c,r=1,1
    if biome and biome.strategy=="courtyards" then
        -- The central courtyard is always on the main route; side wings still
        -- come from the same branching/loop planner below.
        c,r=2,2
        graph.critical[2]=slot(2,1)
        graph.critical[3]=slot(2,2)
    end
    while c<4 or r<3 do
        if c==4 then r=r+1
        elseif r==3 then c=c+1
        elseif RNG.next_int(rng,2)==1 then c=c+1
        else r=r+1 end
        local id=slot(c,r)
        graph.critical[#graph.critical+1]=id
    end
    if target>=10 and RNG.next_int(rng,2)==1 then
        local base_seen={}
        for _,id in ipairs(graph.critical) do base_seen[id]=true end
        local detours={}
        for index=1,#graph.critical-1 do
            local a,b=graph.critical[index],graph.critical[index+1]
            for _,side in ipairs({-1,1}) do
                local p,q
                if row(a)==row(b) then
                    local rr=row(a)+side
                    if rr>=1 and rr<=3 then
                        p,q=slot(col(a),rr),slot(col(b),rr)
                    end
                else
                    local cc=col(a)+side
                    if cc>=1 and cc<=4 then
                        p,q=slot(cc,row(a)),slot(cc,row(b))
                    end
                end
                if p and not base_seen[p] and not base_seen[q] then
                    detours[#detours+1]={index=index,p=p,q=q}
                end
            end
        end
        if #detours>0 then
            local detour=detours[RNG.next_int(rng,#detours)]
            table.insert(graph.critical,detour.index+1,detour.p)
            table.insert(graph.critical,detour.index+2,detour.q)
        end
    end
    local chosen,edge_seen={},{}
    for index,id in ipairs(graph.critical) do
        chosen[id]=true
        if index>1 then
            add_edge(graph,edge_seen,graph.critical[index-1],id,"route")
        end
    end
    local selected=#graph.critical
    while selected<target do
        local candidates={}
        for id=1,12 do
            if not chosen[id] then
                for _,near in ipairs(neighbors(id)) do
                    if chosen[near] then candidates[#candidates+1]=id; break end
                end
            end
        end
        local id=candidates[RNG.next_int(rng,#candidates)]
        local parents={}
        for _,near in ipairs(neighbors(id)) do
            if chosen[near] then parents[#parents+1]=near end
        end
        chosen[id]=true
        selected=selected+1
        add_edge(graph,edge_seen,id,parents[RNG.next_int(rng,#parents)],"branch")
    end
    local loops={}
    for id=1,12 do
        if chosen[id] then for _,near in ipairs(neighbors(id)) do
            if chosen[near] and id<near and not edge_seen[edge_key(id,near)] then
                loops[#loops+1]={id,near}
            end
        end end
    end
    assert(#loops>0,"room plan has no loop candidate")
    local first=table.remove(loops,RNG.next_int(rng,#loops))
    add_edge(graph,edge_seen,first[1],first[2],"loop")
    if #loops>0 and (biome and biome.strategy=="canals" or RNG.next_int(rng,3)==1) then
        local next_loop=loops[RNG.next_int(rng,#loops)]
        add_edge(graph,edge_seen,next_loop[1],next_loop[2],"loop")
    end
    for id=1,12 do
        if chosen[id] then
            local role="combat"
            if id==1 then role="start"
            elseif id==12 then role="elite"
            elseif id%4==0 or id%5==0 then role="treasure"
            elseif id%3==0 then role="landmark" end
            graph.nodes[#graph.nodes+1]={id=id,col=col(id),row=row(id),role=role}
        end
    end
    return graph
end

local function blank_grid(w,h,char)
    local result={}
    for y=1,h do
        result[y]={}
        for x=1,w do result[y][x]=char end
    end
    return result
end
local function serialize_grid(grid)
    local rows={}
    for y=1,#grid do rows[y]=table.concat(grid[y]) end
    return rows
end
local function set_tile(map,x,y,value,visual)
    local row=map.rows[y]
    map.rows[y]=row:sub(1,x-1)..value..row:sub(x+1)
    if visual then
        local art=map.visual_rows[y]
        map.visual_rows[y]=art:sub(1,x-1)..visual..art:sub(x+1)
    end
end
Generation.set_tile = set_tile -- used by focused fault-injection tests

local function world_points(points, ox, oy)
    local result={}
    for _,p in ipairs(points or {}) do
        result[#result+1]={x=ox+p.x-1,y=oy+p.y-1}
    end
    return result
end
local function socket_for(room, facing)
    for _,s in ipairs(room.sockets) do if s.facing==facing then return s end end
    error("missing room socket "..facing)
end

local function stamp_room(map, terrain, art, footprint, rng, node, transformed,biome)
    local cx=20+(node.col-1)*32+RNG.next_int(rng,7)-4
    local cy=16+(node.row-1)*26+RNG.next_int(rng,7)-4
    local ox=cx-math.floor(transformed.width/2)
    local oy=cy-math.floor(transformed.height/2)
    local editable={}
    for _,p in ipairs(transformed.editable) do editable[key(p.x,p.y)]=true end
    local room={id=node.id,slot=node.id,role=node.role,template_id=transformed.id,
        turns=transformed.turns,mirror=transformed.mirror,
        x=ox,y=oy,width=transformed.width,height=transformed.height,
        center={x=cx,y=cy},sockets={},markers={}}
    for kind,points in pairs(transformed.markers) do
        room.markers[kind]=world_points(points,ox,oy)
    end
    for _,s in ipairs(transformed.sockets) do
        room.sockets[#room.sockets+1]={x=ox+s.x-1,y=oy+s.y-1,
            facing=s.facing,width=s.width}
    end
    for y=1,transformed.height do for x=1,transformed.width do
        local wx,wy=ox+x-1,oy+y-1
        assert(wx>1 and wx<map.width and wy>1 and wy<map.height,
            "room outside generated floor")
        local base=transformed.rows[y]:sub(x,x)
        if base~=" " then
        assert(not footprint[key(wx,wy)],"room footprints overlap")
        footprint[key(wx,wy)]=room.id
        local varied=base
        if editable[key(x,y)] then
            map.editable_cells[#map.editable_cells+1]={x=wx,y=wy,kind="room"}
            if RNG.next_int(rng,100)<=35 then
                if biome.strategy=="canals" then
                    varied=base=="~" and "." or "~"
                else varied=base=="#" and "." or "#" end
            end
        else
            map.protected_cells[#map.protected_cells+1]={x=wx,y=wy,tile=base}
        end
        terrain[wy][wx]=varied
        local visual_id=transformed.studio_visual_cells and
            transformed.studio_visual_cells[key(x,y)]
        if visual_id then
            map.studio_visuals=map.studio_visuals or {}
            map.studio_visuals[key(wx,wy)]=visual_id
        end
        if varied=="#" then art[wy][wx]=biome.wall_visual
        elseif varied=="~" then art[wy][wx]="q"
        elseif node.role=="elite" then art[wy][wx]="a"
        else
            local roll=RNG.next_int(rng,12)
            if biome.strategy=="ruins" then
                art[wy][wx]=roll==1 and "v" or roll==2 and "r"
                    or roll<=5 and "m" or "s"
            else
                art[wy][wx]=roll<=3 and biome.floor_visual
                    or roll==4 and "r" or biome.floor_visual
            end
        end
        end
    end end
    return room
end

local function corridor_path(a,b,rng,biome)
    local cells={}
    local function line(x1,y1,x2,y2)
        local dx=x1==x2 and 0 or (x2>x1 and 1 or -1)
        local dy=y1==y2 and 0 or (y2>y1 and 1 or -1)
        local x,y=x1,y1
        while true do
            if #cells==0 or cells[#cells].x~=x or cells[#cells].y~=y then
                cells[#cells+1]={x=x,y=y}
            end
            if x==x2 and y==y2 then break end
            x,y=x+dx,y+dy
        end
    end
    if a.facing=="E" or a.facing=="W" then
        local left,right=a,b
        if a.x>b.x then left,right=b,a end
        local gap=right.x-left.x
        local offset=math.floor(gap/2)+RNG.next_int(rng,3)-2
        local mid=math.max(left.x+2,math.min(right.x-2,left.x+offset))
        line(left.x,left.y,mid,left.y)
        line(mid,left.y,mid,right.y)
        line(mid,right.y,right.x,right.y)
    else
        local top,bottom=a,b
        if a.y>b.y then top,bottom=b,a end
        local gap=bottom.y-top.y
        local offset=math.floor(gap/2)+RNG.next_int(rng,3)-2
        local mid=math.max(top.y+2,math.min(bottom.y-2,top.y+offset))
        line(top.x,top.y,top.x,mid)
        line(top.x,mid,bottom.x,mid)
        line(bottom.x,mid,bottom.x,bottom.y)
    end
    if biome.strategy=="warrens" and #cells>=12 then
        -- Replace one straight interior run with a short, repeatable zigzag.
        -- The detour remains in the empty inter-room band.
        local first=math.floor(#cells/3)
        local last=math.floor(#cells*2/3)
        local p,q=cells[first],cells[last]
        if p and q and (p.x==q.x or p.y==q.y) and last-first>=5 then
            local bend={}
            for i=1,first do bend[#bend+1]=cells[i] end
            local offset=RNG.next_int(rng,2)==1 and 2 or -2
            local function append(x,y)
                local prev=bend[#bend]
                while prev.x~=x or prev.y~=y do
                    local nx=prev.x+(prev.x==x and 0 or (x>prev.x and 1 or -1))
                    local ny=prev.y+(prev.x~=x and 0 or (y>prev.y and 1 or -1))
                    bend[#bend+1]={x=nx,y=ny};prev=bend[#bend]
                end
            end
            if p.y==q.y then
                append(p.x,p.y+offset);append(q.x,p.y+offset)
            else append(p.x+offset,p.y);append(p.x+offset,q.y) end
            append(q.x,q.y)
            for i=last+1,#cells do bend[#bend+1]=cells[i] end
            cells=bend
        end
    end
    return cells
end

local function assign_placements(map,biome)
    local occupied={}
    local chests={}
    for _,room in ipairs(map.rooms) do
        if room.role~="start" and room.role~="elite" then
            local point=room.markers.chest[1]
            if point then chests[#chests+1]={room=room,point=point} end
        end
    end
    assert(#chests>=Generation.chest_target,"too few chest markers")
    -- Spread five chests through the ordered room slots, including branches.
    for index=1,Generation.chest_target do
        local chosen=chests[math.floor((index-1)*(#chests-1)/(Generation.chest_target-1))+1]
        local p=chosen.point
        local id="chest:"..chosen.room.id
        map.placements.chests[#map.placements.chests+1]={id=id,
            definition_id="core:chest/standard",x=p.x,y=p.y,room_id=chosen.room.id}
        occupied[key(p.x,p.y)]=true
    end
    local enemy_types=biome.enemy_ids
    local count=0
    for _,room in ipairs(map.rooms) do
        if room.role~="start" and room.role~="elite" then
            local markers=room.markers.enemy or {}
            local selected=#markers>0 and markers[(room.id-1)%#markers+1] or nil
            for _,p in ipairs(selected and {selected} or {}) do
                if not occupied[key(p.x,p.y)] then
                    count=count+1
                    local id="enemy:"..room.id..":"..count
                    map.placements.enemies[#map.placements.enemies+1]={id=id,
                        definition_id=enemy_types[(count-1)%3+1],x=p.x,y=p.y,
                        room_id=room.id}
                    occupied[key(p.x,p.y)]=true
                end
            end
        end
    end
    local corridor_encounters=0
    for _,corridor in ipairs(map.corridors) do
        if corridor.length>=24 and #corridor.alcoves>0 and corridor_encounters<2 then
            local p=corridor.alcoves[math.floor((#corridor.alcoves+1)/2)]
            if not occupied[key(p.x,p.y)]
                and math.abs(p.x-map.start_x)+math.abs(p.y-map.start_y)>=8 then
                count=count+1
                corridor_encounters=corridor_encounters+1
                map.placements.enemies[#map.placements.enemies+1]={
                    id="corridor_enemy:"..corridor.edge_id,
                    definition_id=enemy_types[(count-1)%3+1],x=p.x,y=p.y,
                    corridor_id=corridor.edge_id}
                occupied[key(p.x,p.y)]=true
            end
        end
    end
end

local function place_exploration(map, terrain, art, footprint, seed, attempt)
    local secret_rng=RNG.new(RNG.derive(seed,
        map.floor_id..":secrets:"..attempt))
    local critical={}
    for _,id in ipairs(map.graph.critical) do critical[id]=true end
    local branches={}
    for _,room in ipairs(map.rooms) do
        if not critical[room.id] and room.markers.secret_anchor
            and #room.markers.secret_anchor>0
            and room.markers.secret_wall and #room.markers.secret_wall>0 then
            branches[#branches+1]=room
        end
    end
    assert(#branches>=2,"secret placement needs two optional rooms")
    local attack_candidates={}
    for _,room in ipairs(branches) do
        local wall=room.markers.secret_wall[1]
        local dx=wall.x==room.x and -1 or
            (wall.x==room.x+room.width-1 and 1 or 0)
        local dy=wall.y==room.y and -1 or
            (wall.y==room.y+room.height-1 and 1 or 0)
        local cache_x,cache_y=wall.x+dx,wall.y+dy
        if cache_x>1 and cache_x<map.width and cache_y>1 and cache_y<map.height
            and terrain[wall.y][wall.x]=="#" and terrain[cache_y][cache_x]=="#"
            and not footprint[key(cache_x,cache_y)] then
            attack_candidates[#attack_candidates+1]={room=room,
                wall=wall,cache_x=cache_x,cache_y=cache_y}
        end
    end
    assert(#attack_candidates>0,"no protected attack-secret alcove")
    local attack=attack_candidates[RNG.next_int(secret_rng,#attack_candidates)]
    local movement_candidates={}
    for _,room in ipairs(branches) do
        if room.id~=attack.room.id then
            movement_candidates[#movement_candidates+1]=room
        end
    end
    local movement=movement_candidates[RNG.next_int(secret_rng,#movement_candidates)]
    local anchor=attack.room.markers.secret_anchor[1]
    map.placements.secrets[1]={id="secret:cracked:"..attack.room.id,
        definition_id="core:secret/cracked_cache",room_id=attack.room.id,
        x=anchor.x,y=anchor.y,wall_x=attack.wall.x,wall_y=attack.wall.y,
        cache_x=attack.cache_x,cache_y=attack.cache_y}
    local step=movement.markers.secret_anchor[1]
    map.placements.secrets[2]={id="secret:sigil:"..movement.id,
        definition_id="core:secret/moss_sigil",room_id=movement.id,
        x=step.x,y=step.y}
    art[attack.wall.y][attack.wall.x]="k" -- subtle recurring cracked moss
    art[step.y][step.x]="p" -- subtle recurring spiral floor pattern

    local degree={}
    for _,edge in ipairs(map.graph.edges) do
        degree[edge.a]=(degree[edge.a] or 0)+1
        degree[edge.b]=(degree[edge.b] or 0)+1
    end
    local door_room
    for _,room in ipairs(branches) do
        if degree[room.id]==1 and room.id~=attack.room.id then
            door_room=room; break
        end
    end
    door_room=door_room or movement
    local door_socket
    for _,corridor in ipairs(map.corridors) do
        if corridor.from_room==door_room.id then door_socket=corridor.from_socket; break end
        if corridor.to_room==door_room.id then door_socket=corridor.to_socket; break end
    end
    assert(door_socket,"optional room lacks door connection")
    map.placements.doors[1]={id="door:"..door_room.id,
        definition_id="core:door/ruin",room_id=door_room.id,
        x=door_socket.x,y=door_socket.y}
end

local function build(seed,attempt,derived_seed,biome,pack)
    local rng=RNG.new(derived_seed)
    local graph=Generation.plan(rng,biome)
    local map={id=biome.map_id,floor_id=biome.floor_id,biome_id=biome.id,
        strategy=biome.strategy,
        width=Generation.width,height=Generation.height,
        seed=seed,attempt=attempt,derived_seed=derived_seed,
        graph=graph,rooms={},corridors={},protected_cells={},editable_cells={},
        placements={enemies={},chests={},secrets={},doors={}},repairs={}}
    if pack then
        map.studio_pack_fingerprint=pack_fingerprint(pack)
        map.studio_templates={}
    end
    local terrain=blank_grid(map.width,map.height,"#")
    local art=blank_grid(map.width,map.height,"w")
    local footprint={}
    local by_id={}
    for _,node in ipairs(graph.nodes) do
        local pool=Rooms.for_role(node.role,biome.id)
        if pack then for _,source in ipairs(pack.rooms) do
            local allowed=false
            for _,biome_id in ipairs(source.biomes) do
                if biome_id==biome.id then allowed=true;break end
            end
            if allowed and Rooms.has_role(source,node.role) then
                local definition=StudioWorld.runtime_room(source,pack)
                for _=1,source.weight do pool[#pool+1]=definition end
            end
        end end
        local def=biome.hub_template_id and node.id==6
            and Rooms.definitions[biome.hub_template_id]
            or pool[RNG.next_int(rng,#pool)]
        if pack and def.studio_weight then
            map.studio_templates[def.id]=def
        end
        local turn=def.rotations[RNG.next_int(rng,#def.rotations)]
        local transformed=Rooms.transform(def,turn,def.mirror and RNG.next_int(rng,2)==1)
        local room=stamp_room(map,terrain,art,footprint,rng,node,transformed,biome)
        map.rooms[#map.rooms+1]=room
        by_id[node.id]=room
    end
    for _,edge in ipairs(graph.edges) do
        local a,b=by_id[edge.a],by_id[edge.b]
        local facing_a,facing_b
        if a.center.x<b.center.x and a.center.y-b.center.y<20 and a.center.y-b.center.y>-20 then
            facing_a,facing_b="E","W"
        elseif a.center.x>b.center.x and a.center.y-b.center.y<20 and a.center.y-b.center.y>-20 then
            facing_a,facing_b="W","E"
        elseif a.center.y<b.center.y then facing_a,facing_b="S","N"
        else facing_a,facing_b="N","S" end
        local from,to=socket_for(a,facing_a),socket_for(b,facing_b)
        local cells=corridor_path(from,to,rng,biome)
        local corridor={edge_id=edge.id,from_room=a.id,to_room=b.id,
            from_socket={x=from.x,y=from.y,facing=from.facing},
            to_socket={x=to.x,y=to.y,facing=to.facing},
            cells=cells,alcoves={},length=#cells-1,
            form=(from.x==to.x or from.y==to.y) and "straight" or "dogleg"}
        for index,p in ipairs(cells) do
            local owner=footprint[key(p.x,p.y)]
            assert(not owner or (index==1 or index==#cells)
                and (owner==a.id or owner==b.id),
                "corridor intersects authored room")
            terrain[p.y][p.x]="."
            art[p.y][p.x]=biome.corridor_visual
            if not owner then
                map.editable_cells[#map.editable_cells+1]={x=p.x,y=p.y,kind="corridor"}
            end
        end
        -- Side alcoves break up long lanes without modifying any room footprint.
        for index=7,#cells-6,8 do
            local p=cells[index]
            local previous,next_cell=cells[index-1],cells[index+1]
            local horizontal=previous.y==p.y and next_cell.y==p.y
            local ax=p.x+(horizontal and 0 or (index%2==0 and 1 or -1))
            local ay=p.y+(horizontal and (index%2==0 and 1 or -1) or 0)
            if ax>1 and ax<map.width and ay>1 and ay<map.height
                and not footprint[key(ax,ay)] then
                terrain[ay][ax]="."
                art[ay][ax]=biome.floor_visual
                corridor.alcoves[#corridor.alcoves+1]={x=ax,y=ay}
                map.editable_cells[#map.editable_cells+1]={x=ax,y=ay,kind="alcove"}
            end
        end
        if biome.strategy=="canals" then
            -- A second bank parallels one long straight gallery segment.
            -- Both ends reconnect, so it is a usable flank, not decoration.
            local best_start,best_end,best_length=nil,nil,0
            local run_start=1
            for i=3,#cells do
                local a0,a1,a2=cells[i-2],cells[i-1],cells[i]
                local same=(a1.x-a0.x==a2.x-a1.x and
                    a1.y-a0.y==a2.y-a1.y)
                if not same or i==#cells then
                    local finish=same and i or i-1
                    if finish-run_start+1>best_length then
                        best_start,best_end,best_length=run_start,finish,
                            finish-run_start+1
                    end
                    run_start=i-1
                end
            end
            if best_length>=9 then
                local first,last=cells[best_start+2],cells[best_end-2]
                local horizontal=first.y==last.y
                for _,side in ipairs({2,-2}) do
                    local extra={}
                    local function push(x,y) extra[#extra+1]={x=x,y=y} end
                    for step=1,2 do
                        push(first.x+(horizontal and 0 or side/2*step),
                            first.y+(horizontal and side/2*step or 0))
                    end
                    local steps=math.abs(last.x-first.x)+math.abs(last.y-first.y)
                    for step=1,steps do
                        push(first.x+(horizontal and (last.x>first.x and step or -step) or side),
                            first.y+(horizontal and side or (last.y>first.y and step or -step)))
                    end
                    for step=1,2 do
                        push(last.x+(horizontal and 0 or side-side/2*step),
                            last.y+(horizontal and side-side/2*step or 0))
                    end
                    local safe=true
                    for _,p in ipairs(extra) do
                        if p.x<=1 or p.x>=map.width or p.y<=1 or p.y>=map.height
                            or footprint[key(p.x,p.y)] then safe=false;break end
                    end
                    if safe then
                        for _,p in ipairs(extra) do
                            terrain[p.y][p.x]="."
                            art[p.y][p.x]="b"
                            map.editable_cells[#map.editable_cells+1]={x=p.x,y=p.y,kind="parallel_bank"}
                        end
                        corridor.parallel=extra
                        break
                    end
                end
            end
        elseif biome.strategy=="courtyards" then
            -- Wider passage nodes around wings create little defensible hubs.
            for i=6,#cells-5,10 do
                local p=cells[i]
                local widened={}
                for dy=-1,1 do for dx=-1,1 do
                    local x,y=p.x+dx,p.y+dy
                    if x>1 and x<map.width and y>1 and y<map.height
                        and not footprint[key(x,y)] then
                        terrain[y][x]="."
                        art[y][x]="t"
                        map.editable_cells[#map.editable_cells+1]={x=x,y=y,kind="courtyard_node"}
                        widened[#widened+1]={x=x,y=y}
                    end
                end end
                corridor.widened=corridor.widened or {}
                corridor.widened[#corridor.widened+1]=widened
            end
        end
        map.corridors[#map.corridors+1]=corridor
    end
    local start=by_id[1].markers.player[1]
    local elite=by_id[12].markers.elite[1]
    map.start_x,map.start_y=start.x,start.y
    map.elite={floor_id=map.floor_id,x=elite.x,y=elite.y,room_id=12,
        kind="reserved_elite_arena"}
    assign_placements(map,biome)
    place_exploration(map,terrain,art,footprint,seed,attempt)
    map.rows=serialize_grid(terrain)
    map.visual_rows=serialize_grid(art)
    return map
end

local function walkable(map,x,y)
    return Content.walkable(map,x,y)
end
local function reachable(map,chest_blockers)
    local found={[key(map.start_x,map.start_y)]=true}
    local queue={{x=map.start_x,y=map.start_y}}
    local head=1
    while head<=#queue do
        local current=queue[head]; head=head+1
        for _,d in ipairs(dirs) do
            local x,y=current.x+d[1],current.y+d[2]
            local k=key(x,y)
            if not found[k] and walkable(map,x,y) and not chest_blockers[k] then
                found[k]=true
                queue[#queue+1]={x=x,y=y}
            end
        end
    end
    return found
end

function Generation.validate(map,runtime_secrets)
    local errors={}
    local function issue(kind,x,y,detail)
        errors[#errors+1]={kind=kind,x=x,y=y,detail=detail}
    end
    local ok,reason=pcall(Content.validate_map,map)
    if not ok then issue("map_invalid",0,0,tostring(reason)); return false,errors end
    local biome=Biomes.get(map.biome_id or Biomes.order[1])
    if not biome or map.id~=biome.map_id or
        (map.floor_id~=biome.floor_id and not
            map.floor_id:match("^"..biome.floor_id.."/stage/[2-9]$"))
        or type(map.graph)~="table" or type(map.rooms)~="table"
        or type(map.corridors)~="table" or type(map.placements)~="table" then
        issue("metadata_invalid",0,0,"missing generated floor records")
        return false,errors
    end
    if #map.rooms<Generation.room_min or #map.rooms>Generation.room_max
        or #map.graph.edges<#map.rooms then
        issue("graph_invalid",0,0,"room count or loop missing")
    end
    local graph_adj={}
    local room_by_id={}
    for _,room in ipairs(map.rooms) do
        graph_adj[room.id]={}
        room_by_id[room.id]=room
    end
    local edge_seen={}
    for _,edge in ipairs(map.graph.edges) do
        if not graph_adj[edge.a] or not graph_adj[edge.b] or edge_seen[edge.id] then
            issue("graph_invalid",0,0,"bad or duplicate edge")
        else
            graph_adj[edge.a][#graph_adj[edge.a]+1]=edge.b
            graph_adj[edge.b][#graph_adj[edge.b]+1]=edge.a
            edge_seen[edge.id]=true
        end
    end
    local graph_reached={[1]=true}
    local graph_queue={1}
    for head=1,#map.rooms do
        local id=graph_queue[head]
        if not id then break end
        for _,near in ipairs(graph_adj[id] or {}) do
            if not graph_reached[near] then
                graph_reached[near]=true
                graph_queue[#graph_queue+1]=near
            end
        end
    end
    if #graph_queue~=#map.rooms or not graph_reached[12] then
        issue("graph_disconnected",0,0,"entrance cannot reach elite room in graph")
    end
    local secret_states={}
    for _,secret in ipairs(runtime_secrets or {}) do secret_states[secret.id]=secret end
    local mutable_walls={}
    for _,place in ipairs(map.placements.secrets or {}) do
        if place.wall_x then
            mutable_walls[key(place.wall_x,place.wall_y)]=secret_states[place.id]
                and secret_states[place.id].revealed
        end
    end
    local protected={}
    for _,cell in ipairs(map.protected_cells or {}) do
        local k=key(cell.x,cell.y)
        if protected[k] and protected[k]~=cell.tile then
            issue("protected_conflict",cell.x,cell.y,"conflicting authored cell")
        end
        protected[k]=cell.tile
        if map.rows[cell.y]:sub(cell.x,cell.x)~=cell.tile
            and not (mutable_walls[k] and cell.tile=="#"
                and map.rows[cell.y]:sub(cell.x,cell.x)==".") then
            issue("protected_changed",cell.x,cell.y,"authored geometry changed")
        end
    end
    local editable={}
    for _,cell in ipairs(map.editable_cells or {}) do
        local k=key(cell.x,cell.y)
        editable[k]=true
        if protected[k] then
            issue("editable_protected_overlap",cell.x,cell.y,"mask conflict")
        end
    end
    local footprints={}
    for _,room in ipairs(map.rooms) do
        local definition=Rooms.definitions[room.template_id] or
            map.studio_templates and map.studio_templates[room.template_id]
        local transformed
        if definition then
            local success,result=pcall(Rooms.transform,definition,room.turns,room.mirror)
            if success then transformed=result end
        end
        if not transformed or room.width~=transformed.width
            or room.height~=transformed.height then
            issue("room_invalid",room.x,room.y,"template or transform mismatch")
        else
            local editable={}
            for _,p in ipairs(transformed.editable) do editable[key(p.x,p.y)]=true end
            for y=1,room.height do for x=1,room.width do
                local wx,wy=room.x+x-1,room.y+y-1
                local k=key(wx,wy)
                local expected=transformed.rows[y]:sub(x,x)
                if expected~=" " then
                if footprints[k] then
                    issue("room_overlap",wx,wy,room.id)
                end
                footprints[k]=room.id
                if not editable[key(x,y)] and protected[k]~=expected then
                    issue("protected_mask_missing",wx,wy,room.template_id)
                end
                end
            end end
            for _,s in ipairs(transformed.sockets) do
                local wx,wy=room.x+s.x-1,room.y+s.y-1
                local matched=false
                for _,actual in ipairs(room.sockets) do
                    if actual.x==wx and actual.y==wy and actual.facing==s.facing
                        and actual.width==s.width then matched=true; break end
                end
                if not matched then issue("socket_invalid",wx,wy,room.id) end
            end
            for kind,points in pairs(transformed.markers) do
                for index,p in ipairs(points) do
                    local actual=room.markers[kind] and room.markers[kind][index]
                    if not actual or actual.x~=room.x+p.x-1
                        or actual.y~=room.y+p.y-1 then
                        issue("marker_invalid",room.x+p.x-1,room.y+p.y-1,kind)
                    end
                end
            end
        end
    end
    if #map.corridors~=#map.graph.edges then
        issue("corridor_count",0,0,"graph edge lacks concrete corridor")
    end
    local opposite={N="S",S="N",E="W",W="E"}
    local function matches_socket(room,socket)
        for _,candidate in ipairs(room.sockets) do
            if candidate.x==socket.x and candidate.y==socket.y
                and candidate.facing==socket.facing then return true end
        end
        return false
    end
    for _,corridor in ipairs(map.corridors) do
        if not edge_seen[corridor.edge_id] or
            not room_by_id[corridor.from_room] or
            not room_by_id[corridor.to_room] then
            issue("corridor_invalid",0,0,"unknown graph connection")
        elseif not matches_socket(room_by_id[corridor.from_room],corridor.from_socket)
            or not matches_socket(room_by_id[corridor.to_room],corridor.to_socket)
            or opposite[corridor.from_socket.facing]~=corridor.to_socket.facing then
            issue("socket_mismatch",corridor.from_socket.x,
                corridor.from_socket.y,corridor.edge_id)
        end
        local first,last=corridor.cells[1],corridor.cells[#corridor.cells]
        if not first or not last or
            not ((first.x==corridor.from_socket.x and first.y==corridor.from_socket.y
                and last.x==corridor.to_socket.x and last.y==corridor.to_socket.y)
                or (last.x==corridor.from_socket.x and last.y==corridor.from_socket.y
                and first.x==corridor.to_socket.x and first.y==corridor.to_socket.y)) then
            issue("corridor_endpoint",0,0,corridor.edge_id)
        end
        for index,cell in ipairs(corridor.cells) do
            if not walkable(map,cell.x,cell.y) then
                issue("corridor_blocked",cell.x,cell.y,corridor.edge_id)
            end
            local owner=footprints[key(cell.x,cell.y)]
            if owner and index~=1 and index~=#corridor.cells then
                issue("corridor_room_crossing",cell.x,cell.y,corridor.edge_id)
            end
            if index>1 then
                local previous=corridor.cells[index-1]
                if math.abs(cell.x-previous.x)+math.abs(cell.y-previous.y)~=1 then
                    issue("corridor_gap",cell.x,cell.y,corridor.edge_id)
                end
            end
        end
        local a,b=corridor.from_socket,corridor.to_socket
        if not walkable(map,a.x,a.y) or not walkable(map,b.x,b.y) then
            issue("socket_blocked",a.x,a.y,corridor.edge_id)
        end
    end
    local blockers={}
    local placement_ids={}
    local used={}
    used[key(map.start_x,map.start_y)]=true
    for _,kind in ipairs({"chests","enemies","doors"}) do
        for _,place in ipairs(map.placements[kind] or {}) do
            local k=key(place.x,place.y)
            if placement_ids[place.id] or used[k] or not walkable(map,place.x,place.y) then
                issue("placement_invalid",place.x,place.y,place.id)
            end
            placement_ids[place.id]=true; used[k]=true
            if kind=="chests" then blockers[k]=true end
            if kind=="enemies" and
                math.abs(place.x-map.start_x)+math.abs(place.y-map.start_y)<5 then
                issue("start_unsafe",place.x,place.y,place.id)
            end
        end
    end
    if #(map.placements.chests or {})~=Generation.chest_target then
        issue("chest_count",0,0,"expected five paid chests")
    end
    local seen=reachable(map,blockers)
    if not seen[key(map.elite.x,map.elite.y)] then
        issue("elite_unreachable",map.elite.x,map.elite.y,"no cardinal route")
    end
    for _,room in ipairs(map.rooms) do
        if not seen[key(room.center.x,room.center.y)] then
            issue("room_unreachable",room.center.x,room.center.y,room.id)
        end
        local clearance=0
        for _,d in ipairs(dirs) do
            if walkable(map,room.center.x+d[1],room.center.y+d[2]) then
                clearance=clearance+1
            end
        end
        if clearance<2 or ((room.role=="start" or room.role=="elite")
            and clearance<4) then
            issue("combat_clearance",room.center.x,room.center.y,room.id)
        end
        if not room.markers.secret_anchor or #room.markers.secret_anchor==0 then
            issue("future_anchor_missing",room.center.x,room.center.y,room.id)
        end
    end
    for _,place in ipairs(map.placements.chests or {}) do
        local approach=false
        for _,d in ipairs(dirs) do
            if seen[key(place.x+d[1],place.y+d[2])] then approach=true end
        end
        if not approach then issue("chest_unreachable",place.x,place.y,place.id) end
    end
    local closed_blockers={}
    for k,value in pairs(blockers) do closed_blockers[k]=value end
    for _,door in ipairs(map.placements.doors or {}) do
        closed_blockers[key(door.x,door.y)]=true
        local room=room_by_id[door.room_id]
        local socket=false
        for _,s in ipairs(room and room.sockets or {}) do
            if s.x==door.x and s.y==door.y then socket=true; break end
        end
        if not socket then issue("door_not_socket",door.x,door.y,door.id) end
        local critical=false
        for _,id in ipairs(map.graph.critical or {}) do
            if id==door.room_id then critical=true end
        end
        if critical then issue("door_on_critical_route",door.x,door.y,door.id) end
    end
    if #(map.placements.doors or {})~=1 then
        issue("door_count",0,0,"expected one optional unlocked door")
    end
    local closed_seen=reachable(map,closed_blockers)
    if not closed_seen[key(map.elite.x,map.elite.y)] then
        issue("closed_door_blocks_route",map.elite.x,map.elite.y,"elite route blocked")
    end
    for _,door in ipairs(map.placements.doors or {}) do
        local approach=false
        for _,d in ipairs(dirs) do
            if closed_seen[key(door.x+d[1],door.y+d[2])] then approach=true end
        end
        if not approach then issue("door_unreachable",door.x,door.y,door.id) end
    end
    local secret_ids={}
    for _,secret in ipairs(map.placements.secrets or {}) do
        local room=room_by_id[secret.room_id]
        if placement_ids[secret.id] or not room or secret_ids[secret.definition_id] then
            issue("secret_invalid",secret.x,secret.y,secret.id)
        end
        placement_ids[secret.id]=true
        secret_ids[secret.definition_id]=true
        if not seen[key(secret.x,secret.y)] then
            issue("secret_approach_unreachable",secret.x,secret.y,secret.id)
        end
        local anchor=false
        for _,p in ipairs(room and room.markers.secret_anchor or {}) do
            if p.x==secret.x and p.y==secret.y then anchor=true end
        end
        if not anchor then issue("secret_anchor_invalid",secret.x,secret.y,secret.id) end
        if secret.wall_x then
            local wall=false
            for _,p in ipairs(room.markers.secret_wall or {}) do
                if p.x==secret.wall_x and p.y==secret.wall_y then wall=true end
            end
            if not wall or math.abs(secret.x-secret.wall_x)
                +math.abs(secret.y-secret.wall_y)~=1
                or not protected[key(secret.wall_x,secret.wall_y)]
                or footprints[key(secret.cache_x,secret.cache_y)] then
                issue("secret_wall_invalid",secret.wall_x,secret.wall_y,secret.id)
            end
            local revealed=secret_states[secret.id] and secret_states[secret.id].revealed
            local expected=revealed and "." or "#"
            if map.rows[secret.wall_y]:sub(secret.wall_x,secret.wall_x)~=expected
                or map.rows[secret.cache_y]:sub(secret.cache_x,secret.cache_x)~=expected then
                issue("secret_geometry_invalid",secret.wall_x,secret.wall_y,secret.id)
            end
            if not revealed and map.visual_rows[secret.wall_y]:sub(secret.wall_x,secret.wall_x)~="k" then
                issue("secret_clue_invalid",secret.wall_x,secret.wall_y,secret.id)
            end
        else
            local revealed=secret_states[secret.id] and secret_states[secret.id].revealed
            if map.visual_rows[secret.y]:sub(secret.x,secret.x)~=
                (revealed and "m" or "p") then
                issue("secret_clue_invalid",secret.x,secret.y,secret.id)
            end
        end
    end
    if #(map.placements.secrets or {})~=2
        or not secret_ids["core:secret/cracked_cache"]
        or not secret_ids["core:secret/moss_sigil"] then
        issue("secret_count",0,0,"expected two optional discovery mechanisms")
    end
    local art=map.visual_rows
    if type(art)~="table" or #art~=map.height then
        issue("visual_invalid",0,0,"missing visual layer")
    else
        for y,row_text in ipairs(art) do
            if #row_text~=map.width or row_text:find("[^wsmcavrkpFfWbTtq]") then
                issue("visual_invalid",0,y,"invalid decoration layer")
            end
        end
    end
    return #errors==0,errors
end

function Generation.repair(map,limit)
    limit=limit or 8
    local allowed={}
    local protected={}
    for _,p in ipairs(map.editable_cells or {}) do allowed[key(p.x,p.y)]=true end
    for _,p in ipairs(map.protected_cells or {}) do protected[key(p.x,p.y)]=true end
    local log={}
    local valid,errors=Generation.validate(map)
    for _=1,limit do
        if valid then break end
        local candidate
        for _,error_record in ipairs(errors) do
            local k=key(error_record.x,error_record.y)
            if (error_record.kind=="corridor_blocked" and allowed[k]
                and not protected[k]) or error_record.kind=="secret_clue_invalid" then
                candidate=error_record; break
            end
        end
        if not candidate then break end
        if candidate.kind=="corridor_blocked" then
            local biome=Biomes.get(map.biome_id or Biomes.order[1])
            set_tile(map,candidate.x,candidate.y,".",biome.corridor_visual)
        else
            local secret
            for _,place in ipairs(map.placements.secrets) do
                if (place.wall_x==candidate.x and place.wall_y==candidate.y)
                    or (not place.wall_x and place.x==candidate.x
                        and place.y==candidate.y) then secret=place; break end
            end
            assert(secret,"missing secret clue repair target")
            local tile=map.rows[candidate.y]:sub(candidate.x,candidate.x)
            set_tile(map,candidate.x,candidate.y,tile,
                secret.wall_x and "k" or "p")
        end
        valid,errors=Generation.validate(map)
        log[#log+1]={kind=candidate.kind=="corridor_blocked" and
            "restore_corridor" or "restore_secret_clue",x=candidate.x,y=candidate.y,
            valid_after=valid}
    end
    return valid,errors,log
end

local function inject_fault(map,kind)
    if kind=="editable" then
        for _,corridor in ipairs(map.corridors) do
            for index=2,#corridor.cells-1 do
                local p=corridor.cells[index]
                set_tile(map,p.x,p.y,"#","w")
                return p
            end
        end
    elseif kind=="protected" then
        local p=map.corridors[1].from_socket
        set_tile(map,p.x,p.y,"#","w")
        return p
    elseif kind=="secret_clue" then
        local p=map.placements.secrets[1]
        local row=map.rows[p.wall_y]
        set_tile(map,p.wall_x,p.wall_y,row:sub(p.wall_x,p.wall_x),"w")
        return p
    end
end

function Generation.generate(seed,options)
    options=options or {}
    local pack=options.studio_pack or active_studio_pack
    if pack then StudioWorld.validate(pack) end
    local biome=assert(Biomes.get(options.biome_id or Biomes.order[1]),
        "unknown generation biome")
    Biomes.validate()
    local order={}
    for _,base_id in ipairs(Rooms.order) do
        order[#order+1]=biome.room_prefix..base_id:match("[^/]+$")
    end
    local biome_rules={id=biome.id,floor_id=biome.floor_id,
        width=Generation.width,height=Generation.height,
        room_min=Generation.room_min,room_max=Generation.room_max,
        standard_chests=Generation.chest_target,retry_limit=Rooms.biome.retry_limit,
        enemy_ids=biome.enemy_ids}
    Rooms.validate(Rooms.definitions,order,biome_rules)
    World.validate()
    RNG.new(seed)
    local retry_limit=options.retry_limit or Rooms.biome.retry_limit
    assert(type(retry_limit)=="number" and retry_limit%1==0
        and retry_limit>=0 and retry_limit<=8,"invalid generation retry budget")
    local attempts={}
    for attempt=0,retry_limit do
        local derived=RNG.derive(seed,biome.floor_id..":"..attempt)
        local ok,map_or_error=pcall(build,seed,attempt,derived,biome,pack)
        local map=ok and map_or_error or nil
        if map then
            if options.fault then inject_fault(map,options.fault) end
            local valid,errors,repairs=Generation.repair(map,8)
            attempts[#attempts+1]={attempt=attempt,derived_seed=derived,
                valid=valid,error_count=#errors,first_error=errors[1],repairs=repairs}
            if valid then
                map.repairs=repairs
                map.diagnostics={attempts=attempts,fallback=false}
                return map
            end
        else
            attempts[#attempts+1]={attempt=attempt,derived_seed=derived,
                valid=false,error_count=1,error=tostring(map_or_error),repairs={}}
        end
    end
    -- Known safe deterministic reference candidate, independently validated on every use.
    local fallback_seed=RNG.derive(314159,biome.floor_id..":fallback")
    local fallback_ok,fallback=pcall(build,seed,-1,fallback_seed,biome,pack)
    local valid,errors=false,{}
    if fallback_ok then valid,errors=Generation.validate(fallback) end
    local authored_excluded=false
    if not valid and pack then
        fallback=build(seed,-1,fallback_seed,biome,nil)
        valid,errors=Generation.validate(fallback)
        authored_excluded=true
        fallback.studio_pack_fingerprint=pack_fingerprint(pack)
        fallback.studio_templates={}
    end
    assert(valid,"prevalidated fallback failed: "..(errors[1] and errors[1].kind or "unknown"))
    fallback.diagnostics={attempts=attempts,fallback=true,
        fallback_reference_seed=fallback_seed,authored_excluded=authored_excluded}
    return fallback
end

return Generation
