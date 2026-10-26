-- Bounded, deterministic geometry shared by conditional previews and effects.
-- No RNG, simulation writes or graphics dependencies.
local Content=require('src.content')
local Targeting=require('src.targeting')
local Visibility=require('src.visibility')
local P={names={lance=true,rook=true,bishop=true,knight=true,
    crown=true,crosswind=true,fan=true,backtrail=true}}
P.weapon_forms={
    pommel_return={class_id='core:actor/adventurer',ability_id='core:ability/basic_attack',hint='clear rear cell; aimed F only'},
    needle_flight={class_id='core:actor/ranger',ability_id='core:ability/ranger_shot',hint='piercing line; aimed F only',route_first='piercing_line',explicit_only=true},
    hollow_sigil={class_id='core:actor/arcanist',ability_id='core:ability/arcanist_sigil',hint='hollow sigil; center spared'},
    backswing={class_id='core:actor/berserker',ability_id='core:ability/berserker_cleave',hint='cleave includes clear rear cells'},
    distant_bastion={class_id='core:actor/sentinel',ability_id='core:ability/sentinel_pulse',hint='two-step pulse; near cardinals spared'},
    volatile_vial={class_id='core:actor/alchemist',ability_id='core:ability/alchemist_vial',hint='cross splash on first contact',route_first=true},
    split_thrust={class_id='core:actor/duelist',ability_id='core:ability/duelist_thrust',hint='forked second step; aimed F only'},
}
local cardinal={{0,-1},{1,0},{0,1},{-1,0}}
local diagonal={{-1,-1},{1,-1},{1,1},{-1,1}}
local function sign(n)return n<0 and -1 or n>0 and 1 or 0 end

function P.clear_segment(state,floor_id,ax,ay,bx,by)
    local map=Content.map_for(state)
    if not map or map.floor_id~=floor_id then return false end
    local function pass(x,y)return Content.projectile_passable(map,x,y,state)end
    local nx,ny=math.abs(bx-ax),math.abs(by-ay)
    local sx,sy=sign(bx-ax),sign(by-ay)
    local ix,iy=0,0
    while ix<nx or iy<ny do
        local a,b=(1+2*ix)*ny,(1+2*iy)*nx
        local cx,cy=ax,ay
        if a==b then cx,cy=ax+sx,ay+sy;ix,iy=ix+1,iy+1
        elseif a<b then cx=ax+sx;ix=ix+1
        else cy=ay+sy;iy=iy+1 end
        if not pass(cx,cy)or (cx~=ax and cy~=ay and (not pass(ax,cy)or not pass(cx,ay)))then
            return false
        end
        ax,ay=cx,cy
    end
    return true
end

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
        return P.clear_segment(state,floor_id,ax,ay,bx,by)
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

function P.weapon_cells(state,form,owner,current,base,dx,dy,ranged,explicit,known_only)
    assert(P.weapon_forms[form],'unknown weapon form')
    local p=owner.position
    if explicit==false and (form=='pommel_return' or form=='needle_flight' or form=='split_thrust')then
        return current,ranged
    end
    local result,seen={},{}
    local map=Content.map_for(state)
    local function pass(x,y)return Content.projectile_passable(map,x,y,state)end
    local function append(x,y,allow_blocked)
        local key=x..':'..y
        if not seen[key] and (x~=p.x or y~=p.y)and (allow_blocked or pass(x,y))then
            seen[key]=true;result[#result+1]={floor_id=p.floor_id,x=x,y=y}
        end
    end
    local function copy_current()
        for _,c in ipairs(current)do append(c.x,c.y,true)end
    end
    local function clear(ax,ay,bx,by)return P.clear_segment(state,p.floor_id,ax,ay,bx,by)end
    if form=='pommel_return' or form=='backswing'then
        copy_current()
        local extra=form=='pommel_return' and {{x=p.x-dx,y=p.y-dy}}
            or Targeting.cells('sweeping_slash',p.x,p.y,-dx,-dy)
        for _,c in ipairs(extra)do if clear(p.x,p.y,c.x,c.y)then append(c.x,c.y)end end
    elseif form=='needle_flight'then
        local blocked
        for _,c in ipairs(base)do
            if not pass(c.x,c.y)then blocked=c;break end
            append(c.x,c.y)
        end
        -- Keep valid Splitflight branches. A blocked terminal cell stays last
        -- so it can still receive normal barricade/web contact handling.
        for _,c in ipairs(current)do if pass(c.x,c.y)then append(c.x,c.y)end end
        if blocked then append(blocked.x,blocked.y,true)end
        ranged='piercing_line'
    elseif form=='hollow_sigil'then
        local cx,cy=p.x+dx*2,p.y+dy*2
        local reached=false
        for _,c in ipairs(base)do if c.x==cx and c.y==cy and pass(cx,cy)then reached=true end end
        if not reached then return current,ranged end
        for _,c in ipairs(current)do if c.x~=cx or c.y~=cy then append(c.x,c.y,true)end end
    elseif form=='distant_bastion'then
        for oy=-2,2 do for ox=-2,2 do
            if math.abs(ox)+math.abs(oy)==2 and clear(p.x,p.y,p.x+ox,p.y+oy)then
                append(p.x+ox,p.y+oy)
            end
        end end
        ranged='area'
    elseif form=='volatile_vial'then
        local center,index
        for n,c in ipairs(base)do
            if not pass(c.x,c.y)then break end
            for _,actor in ipairs(state.entities)do
                local q=actor.position
                if actor.id~=owner.id and actor.hp and actor.hp>0 and q
                    and q.floor_id==p.floor_id and q.x==c.x and q.y==c.y then
                    -- Preview may not reveal a concealed body's location by
                    -- changing a visible splash footprint around it.
                    if known_only and not Visibility.is_visible(state,p.floor_id,q.x,q.y)then
                        return current,ranged,P.weapon_forms[form].hint
                    end
                    center,index=c,n;break
                end
            end
            if center then break end
        end
        if not center then return current,ranged,P.weapon_forms[form].hint end
        for n=1,index do append(base[n].x,base[n].y)end
        for _,d in ipairs(cardinal)do
            local x,y=center.x+d[1],center.y+d[2]
            if clear(center.x,center.y,x,y)then append(x,y)end
        end
        ranged='area'
    elseif form=='split_thrust'then
        local stem=base[1]
        if not stem then return current,ranged end
        append(stem.x,stem.y,true)
        if pass(stem.x,stem.y)then
            for _,side in ipairs({-1,1})do
                local x,y=p.x+dx*2-dy*side,p.y+dy*2+dx*side
                if clear(stem.x,stem.y,x,y)then append(x,y)end
            end
        end
        ranged='piercing_line'
    end
    return result,ranged,P.weapon_forms[form].hint
end
return P
