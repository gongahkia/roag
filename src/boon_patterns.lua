-- Bounded, deterministic geometry shared by conditional previews and effects.
-- No RNG, actor targeting, simulation writes or graphics dependencies.
local Content=require('src.content')
local P={names={lance=true,rook=true,bishop=true,knight=true,
    crown=true,crosswind=true,fan=true,backtrail=true}}
local cardinal={{0,-1},{1,0},{0,1},{-1,0}}
local diagonal={{-1,-1},{1,-1},{1,1},{-1,1}}
local function sign(n)return n<0 and -1 or n>0 and 1 or 0 end

function P.axis(event,axis,origin)
    local p=origin or event.pattern_origin or event.source_position
    if not p or not event.x or not event.y or p.floor_id~=event.floor_id then return false end
    local dx,dy=event.x-p.x,event.y-p.y
    if axis=='cardinal' then return (dx==0)~=(dy==0) end
    return dx~=0 and math.abs(dx)==math.abs(dy)
end

function P.cells(state,name,event)
    assert(P.names[name],'unknown boon pattern')
    local origin=event.pattern_origin or event.source_position
    local x,y=event.x or event.from_x,event.y or event.from_y
    if name=='backtrail' then x,y=event.from_x,event.from_y end
    local floor_id=event.floor_id or origin and origin.floor_id
    local map=Content.map_for(state)
    if not x or not y or not map or floor_id~=map.floor_id then return {} end
    local cells,seen={},{}
    local function pass(px,py)return Content.projectile_passable(map,px,py,state)end
    local function append(px,py)
        local key=px..':'..py
        if not seen[key] and pass(px,py)then
            seen[key]=true;cells[#cells+1]={floor_id=floor_id,x=px,y=py}
        end
    end
    -- One-cell diagonal steps cannot squeeze through a blocked stone corner.
    local function step_clear(ax,ay,bx,by)
        return pass(bx,by) and (ax==bx or ay==by or pass(ax,by) and pass(bx,ay))
    end
    local function segment_clear(ax,ay,bx,by)
        local nx,ny=math.abs(bx-ax),math.abs(by-ay)
        local sx,sy=sign(bx-ax),sign(by-ay)
        local ix,iy=0,0
        while ix<nx or iy<ny do
            local a,b=(1+2*ix)*ny,(1+2*iy)*nx
            local cx,cy=ax,ay
            if a==b then cx,cy=ax+sx,ay+sy;ix,iy=ix+1,iy+1
            elseif a<b then cx=ax+sx;ix=ix+1
            else cy=ay+sy;iy=iy+1 end
            if not step_clear(ax,ay,cx,cy)then return false end
            ax,ay=cx,cy
        end
        return true
    end
    local function ray(dx,dy,length)
        local ax,ay=x,y
        for _=1,length do
            local bx,by=ax+dx,ay+dy
            if not step_clear(ax,ay,bx,by)then break end
            append(bx,by);ax,ay=bx,by
        end
    end
    local dx,dy=origin and sign(x-origin.x) or 0,origin and sign(y-origin.y) or 0
    if name=='lance' then
        if dx~=0 or dy~=0 then ray(dx,dy,2)end
    elseif name=='rook' then
        if P.axis(event,'cardinal')then ray(-dy,dx,2);ray(dy,-dx,2)end
    elseif name=='bishop' then
        if P.axis(event,'diagonal')then
            for _,d in ipairs(diagonal)do ray(d[1],d[2],2)end
        end
    elseif name=='knight' then
        -- Two orthogonal legs, either order. No jumping through walls/doors.
        for _,d in ipairs({{-1,-2},{1,-2},{2,-1},{2,1},
            {1,2},{-1,2},{-2,1},{-2,-1}})do
            local function route(horizontal_first)
                local ax,ay=x,y
                for leg=1,2 do
                    local horizontal=(leg==1)==horizontal_first
                    local count=horizontal and math.abs(d[1]) or math.abs(d[2])
                    for _=1,count do
                        ax=ax+(horizontal and sign(d[1]) or 0)
                        ay=ay+(horizontal and 0 or sign(d[2]))
                        if not pass(ax,ay)then return false end
                    end
                end
                return true
            end
            if route(true)or route(false)then append(x+d[1],y+d[2])end
        end
    elseif name=='crown' then
        for oy=-2,2 do for ox=-2,2 do
            if math.max(math.abs(ox),math.abs(oy))==2 then
                -- A short supercover line from the victim to each ring cell.
                if segment_clear(x,y,x+ox,y+oy)then append(x+ox,y+oy)end
            end
        end end
    elseif name=='crosswind' then
        for _,d in ipairs(cardinal)do ray(d[1],d[2],2)end
    elseif name=='fan' then
        if dx~=0 or dy~=0 then
            for side=-1,1 do
                local bx,by=x+dx-dy*side,y+dy+dx*side
                if segment_clear(x,y,bx,by)then append(bx,by)end
            end
        end
    else
        append(x,y)
        for _,d in ipairs(cardinal)do ray(d[1],d[2],1)end
    end
    return cells
end
return P
