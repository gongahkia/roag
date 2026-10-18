-- Optional reviewed corridor shapes and quest anchors. The primary graph edge
-- remains intact; every extra passage is a side path outside authored rooms.
local RNG=require('src.rng')
local Branches={}

Branches.kinds={'spur','braid','switchback','hub','false_end'}
local function key(x,y) return x..','..y end

local function design(kind,at)
    local cells,nodes={},{}
    local function carve(x1,y1,x2,y2)
        assert(x1==x2 or y1==y2)
        local dx=x1==x2 and 0 or (x2>x1 and 1 or -1)
        local dy=y1==y2 and 0 or (y2>y1 and 1 or -1)
        local x,y=x1,y1
        while true do
            if y~=0 then cells[key(x,y)]=at(x,y) end
            if x==x2 and y==y2 then break end
            x,y=x+dx,y+dy
        end
    end
    local function node(name,x,y)
        local point=at(x,y)
        nodes[#nodes+1]={kind=name,x=point.x,y=point.y}
    end
    if kind=='spur' then
        carve(3,0,3,3);carve(3,3,5,3)
        node('clue',3,2);node('reward',5,3)
    elseif kind=='braid' then
        carve(1,0,1,2);carve(1,2,6,2);carve(6,2,6,0)
        carve(4,2,4,4)
        node('key',2,2);node('gate',4,3);node('reward',4,4)
    elseif kind=='switchback' then
        carve(2,0,2,3);carve(5,0,5,2);carve(5,2,6,2)
        carve(6,2,6,3)
        node('plate',2,3);node('gate',6,2);node('reward',6,3)
    elseif kind=='hub' then
        carve(3,0,3,3);carve(5,0,5,-3);carve(3,1,5,1)
        node('brazier',3,3);node('brazier',5,-3);node('reward',4,1)
    elseif kind=='false_end' then
        carve(2,0,2,3);carve(2,3,6,3);carve(6,3,6,0)
        carve(5,2,5,3)
        node('clue',3,3);node('gate',4,3)
        node('enemy',5,3);node('reward',5,2)
    else error('unknown corridor branch: '..tostring(kind)) end
    local ordered={}
    for _,p in pairs(cells) do ordered[#ordered+1]=p end
    table.sort(ordered,function(a,b)
        return a.y==b.y and a.x<b.x or a.y<b.y
    end)
    return ordered,nodes
end

function Branches.attach(map,terrain,art,footprint,rng,biome)
    map.corridor_quests={}
    if RNG.next_int(rng,100)>70 then return end
    local candidates={}
    for _,corridor in ipairs(map.corridors) do
        local cells=corridor.cells
        for first=3,#cells-9 do
            local p,q=cells[first],cells[first+1]
            local dx,dy=q.x-p.x,q.y-p.y
            local straight=true
            for index=first+2,first+7 do
                if cells[index].x-cells[index-1].x~=dx or
                    cells[index].y-cells[index-1].y~=dy then straight=false;break end
            end
            if straight then
                local clear=true
                for index=first,first+7 do
                    local step=cells[index]
                    if footprint[key(step.x,step.y)] then clear=false;break end
                end
                if clear then
                    for _,side in ipairs({-1,1}) do
                        candidates[#candidates+1]={corridor=corridor,
                            first=first,dx=dx,dy=dy,side=side}
                    end
                end
            end
        end
    end
    if #candidates==0 then return end
    local first_kind=RNG.next_int(rng,#Branches.kinds)
    local first_candidate=RNG.next_int(rng,#candidates)
    local primary={}
    for _,corridor in ipairs(map.corridors) do
        for _,p in ipairs(corridor.cells) do primary[key(p.x,p.y)]=true end
    end
    for offset=0,#candidates-1 do
        local candidate=candidates[(first_candidate+offset-1)%#candidates+1]
        local kind=Branches.kinds[(first_kind+offset-1)%#Branches.kinds+1]
        local p=candidate.corridor.cells[candidate.first]
        local function at(u,v)
            local x=p.x+u*candidate.dx-v*candidate.dy*candidate.side
            local y=p.y+u*candidate.dy+v*candidate.dx*candidate.side
            return {x=x,y=y}
        end
        local cells,nodes=design(kind,at)
        local safe=true
        for _,point in ipairs(cells) do
            local x,y=point.x,point.y
            if x<=1 or y<=1 or x>=map.width or y>=map.height
                or footprint[key(x,y)] or primary[key(x,y)] then
                safe=false;break
            end
        end
        if safe then
            for _,node in ipairs(nodes) do
                if node.kind=='enemy' and
                    math.abs(node.x-map.start_x)
                        +math.abs(node.y-map.start_y)<5 then
                    safe=false;break
                end
            end
        end
        if safe then
            local quest={id=map.floor_id..':branch:'..candidate.corridor.edge_id,
                kind=kind,edge_id=candidate.corridor.edge_id,
                cells=cells,nodes=nodes}
            for _,point in ipairs(cells) do
                terrain[point.y][point.x]='.'
                art[point.y][point.x]=biome.corridor_visual
                map.editable_cells[#map.editable_cells+1]={
                    x=point.x,y=point.y,kind='quest_branch'}
            end
            candidate.corridor.branch_kind=kind
            candidate.corridor.branch_cells=cells
            map.corridor_quests[1]=quest
            return
        end
    end
end

return Branches
