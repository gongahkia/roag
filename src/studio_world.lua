-- E03 authored world source. This is inert, bounded data; runtime templates are
-- derived from it and never execute project-provided Lua.
local Project=require('src.studio_project')
local Rooms=require('src.rooms')
local Biomes=require('src.biomes')
local Terrain=require('src.terrain')
local W={schema='roag-studio-world/1',max_rooms=32,max_tiles=32}
local symbols={[' ']=true}
for symbol in pairs(Terrain.definitions) do symbols[symbol]=true end
local roles={combat=true,treasure=true,landmark=true,secret=true,start=true,elite=true}
local markers={enemy=true,chest=true,player=true,elite=true,landmark=true,
    secret_anchor=true,secret_wall=true,stair=true}
local function check(ok,msg) if not ok then error(msg,2) end end
local function integer(n,lo,hi) return type(n)=='number' and n%1==0 and n>=lo and n<=hi end
local function clone(value)
    if type(value)~='table' then return value end
    local out={};for k,v in pairs(value) do out[k]=clone(v) end;return out
end
W.clone=clone
local function key(x,y) return x..','..y end
local function sorted(list) local out={};for _,v in ipairs(list) do out[#out+1]=v end;table.sort(out);return out end
function W.new(project_id)
    check(Project.valid_project_id(project_id),'invalid world project ID')
    local pack={schema=W.schema,project_id=project_id,tiles={
        {id='studio:tile/floor',name='Floor',symbol='.',asset_id='',
            blocks_movement=false,blocks_sight=false,blocks_projectile=false},
        {id='studio:tile/wall',name='Wall',symbol='#',asset_id='',
            blocks_movement=true,blocks_sight=true,blocks_projectile=true},
        {id='studio:tile/water',name='Water',symbol='~',asset_id='',
            blocks_movement=true,blocks_sight=false,blocks_projectile=false}},rooms={}}
    return pack
end
function W.add_terrain_tiles(pack)
    for _,symbol in ipairs(Terrain.order) do
        local def=Terrain.get(symbol)
        local id='studio:tile/'..def.id:match('[^/]+$')
        if not W.tile(pack,id) then
            pack.tiles[#pack.tiles+1]={id=id,name=def.name,symbol=symbol,asset_id='',
                blocks_movement=not def.walkable,blocks_sight=def.blocks_vision,
                blocks_projectile=not def.projectile_passable}
        end
    end
end
function W.new_room(id,name,width,height)
    check(Project.valid_id(id),'invalid room ID')
    check(integer(width,7,23) and integer(height,7,17) and
        width%2==1 and height%2==1,'room size must be odd, up to 23×17 tiles')
    local rows={};for y=1,height do
        rows[y]=(y==1 or y==height) and string.rep('#',width) or
            '#'..string.rep('.',width-2)..'#'
    end
    local midx,midy=(width+1)/2,(height+1)/2
    local sockets={{x=midx,y=1,facing='N',width=1},
        {x=width,y=midy,facing='E',width=1},
        {x=midx,y=height,facing='S',width=1},
        {x=1,y=midy,facing='W',width=1}}
    for _,p in ipairs(sockets) do
        rows[p.y]=rows[p.y]:sub(1,p.x-1)..'.'..rows[p.y]:sub(p.x+1)
    end
    return {id=id,name=name or id,width=width,height=height,rows=rows,
        roles={'combat'},biomes={Biomes.order[1]},weight=1,sockets=sockets,
        markers={enemy={{x=midx,y=midy}},chest={{x=width-2,y=height-2}},
            player={},elite={},landmark={},stair={},secret_anchor={{x=2,y=2}},
            secret_wall={{x=1,y=2}}},editable={},protected={},
        visual_cells={},tile_cells={},rotations={0,1,2,3},mirror=true}
end
function W.tile(pack,id)
    for _,tile in ipairs(pack.tiles) do if tile.id==id then return tile end end
end
function W.set_cell(room,x,y,symbol)
    check(integer(x,1,room.width) and integer(y,1,room.height),'cell outside room')
    check(symbols[symbol],'invalid tile symbol')
    local row=room.rows[y]
    if row:sub(x,x)==symbol then return false end
    room.rows[y]=row:sub(1,x-1)..symbol..row:sub(x+1)
    return true
end
function W.set_mask(room,kind,x,y,value)
    check(kind=='editable' or kind=='protected','invalid mask kind')
    check(integer(x,1,room.width) and integer(y,1,room.height),'mask outside room')
    local list=room[kind]
    for i,p in ipairs(list) do if p.x==x and p.y==y then
        if value then return false end;table.remove(list,i);return true end end
    if not value then return false end
    list[#list+1]={x=x,y=y};return true
end
function W.set_visual(room,x,y,asset_id)
    check(integer(x,1,room.width) and integer(y,1,room.height),'visual outside room')
    check(asset_id=='' or Project.valid_id(asset_id),'invalid visual asset ID')
    local k=key(x,y)
    if room.visual_cells[k]==asset_id then return false end
    room.visual_cells[k]=asset_id=='' and nil or asset_id
    return true
end
function W.paint_tile(pack,room,x,y,tile_id)
    local tile=assert(W.tile(pack,tile_id),'unknown tile type')
    local changed=W.set_cell(room,x,y,tile.symbol)
    local k=key(x,y)
    if room.tile_cells[k]~=tile_id then room.tile_cells[k]=tile_id;changed=true end
    return changed
end
function W.add_marker(room,kind,x,y)
    check(markers[kind],'invalid marker kind')
    check(integer(x,1,room.width) and integer(y,1,room.height),'marker outside room')
    local points=room.markers[kind] or {}
    room.markers[kind]=points
    for i,p in ipairs(points) do if p.x==x and p.y==y then table.remove(points,i);return false end end
    if kind=='stair' then room.markers.stair={};points=room.markers.stair end
    points[#points+1]={x=x,y=y};return true
end
local function walkable(room,x,y)
    local def=x>=1 and y>=1 and x<=room.width and y<=room.height and
        Terrain.get(room.rows[y]:sub(x,x))
    return def and def.walkable or false
end
function W.validate(pack,assets)
    check(type(pack)=='table' and pack.schema==W.schema,'incompatible world schema')
    check(Project.valid_project_id(pack.project_id),'invalid world project ID')
    check(type(pack.tiles)=='table' and #pack.tiles>=3 and #pack.tiles<=W.max_tiles,
        'invalid tile catalogue')
    local tile_ids,asset_ids={},{}
    for _,a in ipairs(assets or {}) do asset_ids[a.id]=true end
    for _,tile in ipairs(pack.tiles) do
        check(type(tile)=='table' and Project.valid_id(tile.id) and not tile_ids[tile.id],
            'duplicate or invalid tile ID')
        tile_ids[tile.id]=true
        check(type(tile.name)=='string' and #tile.name>0 and #tile.name<=80
            and not tile.name:find('[%c]'),
            'invalid tile name: '..tile.id)
        check(symbols[tile.symbol] and tile.symbol~=' ','invalid tile symbol: '..tile.id)
        check(tile.asset_id=='' or Project.valid_id(tile.asset_id),
            'invalid tile visual ID: '..tile.id)
        if assets and tile.asset_id~='' then
            check(asset_ids[tile.asset_id],'missing tile visual asset: '..tile.asset_id)
        end
        for _,field in ipairs({'blocks_movement','blocks_sight','blocks_projectile'}) do
            check(type(tile[field])=='boolean','invalid '..field..': '..tile.id)
        end
        local semantic=Terrain.get(tile.symbol)
        local m,s,p=not semantic.walkable,semantic.blocks_vision,
            not semantic.projectile_passable
        check(tile.blocks_movement==m and tile.blocks_sight==s and
            tile.blocks_projectile==p,'tile properties disagree with runtime symbol: '..tile.id)
    end
    check(type(pack.rooms)=='table' and #pack.rooms<=W.max_rooms,'invalid room list')
    local room_ids={}
    for _,room in ipairs(pack.rooms) do
        local id=room.id
        check(Project.valid_id(id) and not room_ids[id] and not Rooms.definitions[id],
            'duplicate or invalid room ID: '..tostring(id))
        room_ids[id]=true
        check(type(room.name)=='string' and #room.name>0 and #room.name<=80
            and not room.name:find('[%c]'),
            'invalid room name: '..id)
        check(integer(room.width,7,23) and integer(room.height,7,17) and
            room.width%2==1 and room.height%2==1,'invalid dimensions: '..id)
        check(type(room.rows)=='table' and #room.rows==room.height,'invalid rows: '..id)
        for y,row in ipairs(room.rows) do
            check(type(row)=='string' and #row==room.width,
                'invalid row '..y..': '..id)
            for x=1,#row do
                check(symbols[row:sub(x,x)],'invalid row '..y..' cell '..x..': '..id)
            end
        end
        check(walkable(room,(room.width+1)/2,(room.height+1)/2),
            'room center must be walkable: '..id)
        check(type(room.roles)=='table' and #room.roles>0 and #room.roles<=6,
            'missing role: '..id)
        local seen={}
        for _,role in ipairs(room.roles) do
            check(roles[role] and not seen[role],'invalid role: '..id);seen[role]=true
        end
        check(type(room.biomes)=='table' and #room.biomes>0
            and #room.biomes<=#Biomes.order,
            'missing biome: '..id)
        local role_seen=seen
        local cx,cy=(room.width+1)/2,(room.height+1)/2
        local clearance=0
        for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
            if walkable(room,cx+d[1],cy+d[2]) then clearance=clearance+1 end
        end
        check(clearance>=((role_seen.start or role_seen.elite) and 4 or 2),
            'center combat clearance too narrow: '..id)
        seen={}
        for _,biome in ipairs(room.biomes) do
            check(Biomes.get(biome) and not seen[biome],'invalid biome: '..id);seen[biome]=true
        end
        check(integer(room.weight,1,20),'invalid weight: '..id)
        check(type(room.rotations)=='table' and #room.rotations>0 and #room.rotations<=4
            and type(room.mirror)=='boolean','invalid transforms: '..id)
        seen={};for _,turn in ipairs(room.rotations) do
            check(integer(turn,0,3) and not seen[turn],'invalid rotation: '..id);seen[turn]=true
        end
        check(type(room.sockets)=='table' and #room.sockets==4,'room needs four sockets: '..id)
        seen={};local required={}
        for _,s in ipairs(room.sockets) do
            check(type(s)=='table' and ({N=true,E=true,S=true,W=true})[s.facing]
                and not seen[s.facing] and s.width==1
                and integer(s.x,1,room.width) and integer(s.y,1,room.height),
                'invalid socket: '..id)
            seen[s.facing]=true;required[#required+1]={x=s.x,y=s.y}
            check((s.facing=='N' and s.y==1 or s.facing=='S' and s.y==room.height
                or s.facing=='E' and s.x==room.width or s.facing=='W' and s.x==1)
                and walkable(room,s.x,s.y),'socket must open at boundary: '..id)
            local dx=s.facing=='E' and -1 or s.facing=='W' and 1 or 0
            local dy=s.facing=='N' and 1 or s.facing=='S' and -1 or 0
            check(walkable(room,s.x+dx,s.y+dy),'socket lacks inner clearance: '..id)
        end
        check(type(room.markers)=='table','invalid markers: '..id)
        check(not room.markers.stair or #room.markers.stair<=1,
            'room may have only one stair marker: '..id)
        check(type(room.markers.secret_anchor)=='table' and
            #room.markers.secret_anchor>0,'room needs future secret anchor: '..id)
        for kind,points in pairs(room.markers) do
            check(markers[kind] and type(points)=='table','invalid marker kind: '..id)
            for _,p in ipairs(points) do
                check(type(p)=='table' and integer(p.x,1,room.width) and
                    integer(p.y,1,room.height) and
                    room.rows[p.y]:sub(p.x,p.x)==(kind=='secret_wall' and '#' or '.'),
                    'invalid '..kind..' marker at '..tostring(p.x)..','..tostring(p.y)..': '..id)
                if kind=='player' or kind=='elite' or kind=='chest' or kind=='stair' then
                    required[#required+1]=p
                end
            end
        end
        for role,kind in pairs({start='player',combat='enemy',treasure='chest',
            landmark='landmark',elite='elite'}) do
            if role_seen[role] then check(#(room.markers[kind] or {})>0,
                'role '..role..' needs '..kind..' marker: '..id) end
        end
        local blocked={};for _,list in ipairs({room.editable,room.protected}) do
            check(type(list)=='table','invalid masks: '..id)
            for _,p in ipairs(list) do
                check(integer(p.x,1,room.width) and integer(p.y,1,room.height),
                    'mask outside room: '..id)
                check(room.rows[p.y]:sub(p.x,p.x)~=' ',
                    'mask on void cell: '..id)
                local k=key(p.x,p.y);check(not blocked[k],'overlapping masks: '..id)
                blocked[k]=true
            end
        end
        for _,s in ipairs(room.sockets) do
            local dx=s.facing=='E' and -1 or s.facing=='W' and 1 or 0
            local dy=s.facing=='N' and 1 or s.facing=='S' and -1 or 0
            for _,p in ipairs(room.editable) do
                check(not (p.x==s.x and p.y==s.y) and
                    not (p.x==s.x+dx and p.y==s.y+dy),
                    'editable socket approach: '..id)
            end
        end
        for _,points in pairs(room.markers) do for _,marker in ipairs(points) do
            for _,p in ipairs(room.editable) do
                check(p.x~=marker.x or p.y~=marker.y,
                    'editable critical marker: '..id)
            end
        end end
        if role_seen.start or role_seen.elite then
            for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
                for _,p in ipairs(room.editable) do
                    check(p.x~=cx+d[1] or p.y~=cy+d[2],
                        'editable critical center clearance: '..id)
                end
            end
        end
        local first=required[1]
        local queue={{x=first.x,y=first.y}}
        local visited={[key(first.x,first.y)]=true}
        local qi=1
        while qi<=#queue do
            local p=queue[qi]
            for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}}) do
                local x,y=p.x+d[1],p.y+d[2];local k=key(x,y)
                if walkable(room,x,y) and not visited[k] then
                    visited[k]=true;queue[#queue+1]={x=x,y=y}
                end
            end
            qi=qi+1
        end
        for _,p in ipairs(required) do
            check(visited[key(p.x,p.y)],'disconnected socket or anchor at '..
                p.x..','..p.y..': '..id)
        end
        local def=W.runtime_room(room)
        local biome=Biomes.get(room.biomes[1])
        local rules={id=biome.id,floor_id=biome.floor_id,width=140,height=90,
            room_min=8,room_max=12,standard_chests=5,retry_limit=2,
            enemy_ids=biome.enemy_ids}
        local valid,reason=pcall(Rooms.validate,{[id]=def},{id},rules)
        check(valid,'generator room contract: '..tostring(reason))
        check(type(room.visual_cells)=='table','invalid visual cells: '..id)
        for k,asset_id in pairs(room.visual_cells) do
            local x,y=k:match('^(%d+),(%d+)$');x=tonumber(x);y=tonumber(y)
            check(x and y and integer(x,1,room.width) and integer(y,1,room.height)
                and room.rows[y]:sub(x,x)~=' '
                and Project.valid_id(asset_id),'invalid visual cell: '..id)
            if assets then check(asset_ids[asset_id],
                'missing room visual asset: '..asset_id) end
        end
        check(type(room.tile_cells)=='table','invalid tile cells: '..id)
        for k,tile_id in pairs(room.tile_cells) do
            local x,y=k:match('^(%d+),(%d+)$');x=tonumber(x);y=tonumber(y)
            local tile=W.tile(pack,tile_id)
            check(x and y and integer(x,1,room.width) and integer(y,1,room.height)
                and tile and room.rows[y]:sub(x,x)==tile.symbol,
                'invalid tile cell: '..id..' '..tostring(k))
        end
    end
    return true
end
function W.runtime_room(room,pack)
    local visuals=clone(room.visual_cells)
    if pack then for location,tile_id in pairs(room.tile_cells) do
        local tile=W.tile(pack,tile_id)
        if tile and tile.asset_id~='' and not visuals[location] then
            visuals[location]=tile.asset_id
        end
    end end
    local out={id=room.id,roles=clone(room.roles),rows=clone(room.rows),
        sockets=clone(room.sockets),markers=clone(room.markers),editable=clone(room.editable),
        rotations=clone(room.rotations),mirror=room.mirror,protected_default=true,
        studio_visual_cells=visuals,studio_protected=clone(room.protected),
        studio_weight=room.weight,
        studio_biomes=clone(room.biomes)}
    return out
end
-- Compact deterministic escaped text. Each line is one typed record. No load().
local function esc(s) return tostring(s):gsub('%%','%%25'):gsub('\t','%%09'):gsub('\n','%%0A'):gsub('\r','%%0D') end
local function unesc(s)
    check(not s:find('%%[^0-9A-F]') and not s:find('%%[0-9A-F][^0-9A-F]'),
        'invalid world escape')
    return (s:gsub('%%(%x%x)',function(v) return string.char(tonumber(v,16)) end))
end
local function fields(line)
    local f={};for part in (line..'\t'):gmatch('(.-)\t') do f[#f+1]=unesc(part) end;return f
end
local function add(out,...)
    local f={...};for i=1,#f do f[i]=esc(f[i]) end;out[#out+1]=table.concat(f,'\t')
end
local function points(list)
    local out={};for _,p in ipairs(list) do out[#out+1]=p.x..','..p.y end
    table.sort(out);return table.concat(out,';')
end
local function read_points(s)
    local out={};if s=='' then return out end
    for pair in s:gmatch('[^;]+') do
        local x,y=pair:match('^(%d+),(%d+)$');check(x,'invalid point')
        out[#out+1]={x=tonumber(x),y=tonumber(y)}
    end;return out
end
function W.encode(pack)
    W.validate(pack)
    local out={'ROAG-STUDIO-WORLD/1'};add(out,'project',pack.project_id)
    local tiles=clone(pack.tiles);table.sort(tiles,function(a,b)return a.id<b.id end)
    for _,t in ipairs(tiles) do add(out,'tile',t.id,t.name,t.symbol,t.asset_id,
        t.blocks_movement and 1 or 0,t.blocks_sight and 1 or 0,
        t.blocks_projectile and 1 or 0) end
    local rooms=clone(pack.rooms);table.sort(rooms,function(a,b)return a.id<b.id end)
    for _,r in ipairs(rooms) do
        add(out,'room',r.id,r.name,r.width,r.height,r.weight,
            table.concat(sorted(r.roles),','),table.concat(sorted(r.biomes),','),
            table.concat(r.rotations,','),r.mirror and 1 or 0)
        for y,row in ipairs(r.rows) do add(out,'row',r.id,y,row) end
        for _,s in ipairs(r.sockets) do add(out,'socket',r.id,s.facing,s.x,s.y,s.width) end
        local kinds={};for kind in pairs(r.markers) do kinds[#kinds+1]=kind end;table.sort(kinds)
        for _,kind in ipairs(kinds) do add(out,'marker',r.id,kind,points(r.markers[kind])) end
        add(out,'mask',r.id,'editable',points(r.editable))
        add(out,'mask',r.id,'protected',points(r.protected))
        local keys={};for k in pairs(r.visual_cells) do keys[#keys+1]=k end;table.sort(keys)
        for _,k in ipairs(keys) do add(out,'visual',r.id,k,r.visual_cells[k]) end
        keys={};for k in pairs(r.tile_cells) do keys[#keys+1]=k end;table.sort(keys)
        for _,k in ipairs(keys) do add(out,'tilecell',r.id,k,r.tile_cells[k]) end
    end
    return table.concat(out,'\n')..'\n'
end
function W.decode(bytes)
    check(type(bytes)=='string' and #bytes<=1024*1024,'world source too large')
    local lines={};for line in (bytes..'\n'):gmatch('(.-)\n') do
        if line~='' then lines[#lines+1]=line end end
    check(lines[1]=='ROAG-STUDIO-WORLD/1','incompatible world source')
    local project=fields(lines[2] or '');check(project[1]=='project' and #project==2,
        'invalid world project record')
    local pack={schema=W.schema,project_id=project[2],tiles={},rooms={}}
    local by_id={}
    for i=3,#lines do
        local f=fields(lines[i]);local kind=f[1]
        if kind=='tile' then
            check(#f==8,'invalid tile record')
            pack.tiles[#pack.tiles+1]={id=f[2],name=f[3],symbol=f[4],asset_id=f[5],
                blocks_movement=f[6]=='1',blocks_sight=f[7]=='1',
                blocks_projectile=f[8]=='1'}
            for j=6,8 do check(f[j]=='0' or f[j]=='1','invalid tile flag') end
        elseif kind=='room' then
            check(#f==10 and not by_id[f[2]],'invalid room record')
            local function csv(v) local a={};for item in v:gmatch('[^,]+') do a[#a+1]=item end;return a end
            local r={id=f[2],name=f[3],width=tonumber(f[4]),height=tonumber(f[5]),
                weight=tonumber(f[6]),roles=csv(f[7]),biomes=csv(f[8]),
                rotations={},mirror=f[10]=='1',rows={},sockets={},markers={},
                editable={},protected={},visual_cells={},tile_cells={}}
            check(f[10]=='0' or f[10]=='1','invalid mirror flag')
            for _,v in ipairs(csv(f[9])) do r.rotations[#r.rotations+1]=tonumber(v) end
            pack.rooms[#pack.rooms+1]=r;by_id[r.id]=r
        else
            local r=by_id[f[2]];check(r,'record before room')
            if kind=='row' then check(#f==4 and not r.rows[tonumber(f[3])],'invalid row record');r.rows[tonumber(f[3])]=f[4]
            elseif kind=='socket' then check(#f==6,'invalid socket record');r.sockets[#r.sockets+1]={facing=f[3],x=tonumber(f[4]),y=tonumber(f[5]),width=tonumber(f[6])}
            elseif kind=='marker' then check(#f==4 and not r.markers[f[3]],'invalid marker record');r.markers[f[3]]=read_points(f[4])
            elseif kind=='mask' then check(#f==4 and (f[3]=='editable' or f[3]=='protected'),'invalid mask record');r[f[3]]=read_points(f[4])
            elseif kind=='visual' then check(#f==4 and not r.visual_cells[f[3]],'invalid visual record');r.visual_cells[f[3]]=f[4]
            elseif kind=='tilecell' then check(#f==4 and not r.tile_cells[f[3]],'invalid tile cell record');r.tile_cells[f[3]]=f[4]
            else error('unknown world record: '..tostring(kind)) end
        end
    end
    W.validate(pack);return pack
end
return W
