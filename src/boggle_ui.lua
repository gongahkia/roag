-- Input/presentation only. Ordered pointer samples edit a route immediately;
-- forecasts are coalesced once per frame and never replace live combat state.
local B=require('src.boggle')
local C=B.content
local G=require('src.game')
local Pixel=require('src.pixel_ui')
local Catalog=require('src.boon_content')
local U={seed=202,cursor=1,path={},generation=0,metrics={path={},forecast={},ghost={}}}
local host
local origin_x,origin_y,pitch,tile_size=672,112,53,49
local function now()return love and love.timer and love.timer.getTime()or os.clock()end
local function add_metric(key,v)
    local list=U.metrics[key];list[#list+1]=v;if #list>512 then table.remove(list,1)end
end
function U.attach(h)host=h end
function U.active()return host and host.state()and host.state().boggle~=nil end
local function changed()
    U.generation=U.generation+1;U.dirty=true;U.forecast=nil;U.focus_step=nil
    U.pending={version=U.generation,input=U.input_time or now()}
end
function U.clear()U.path={};U.drag=false;U.last_point=nil;U.message=nil;changed()end
function U.open(seed)
    U.seed=seed or U.seed;U.setup=true;U.menu=false;U.help=false
    require('src.card_trial_ui').setup=false;U.clear();host.setup()
end
function U.start()
    U.install_present_hook()
    host.start(U.seed,'bare','boggle');U.setup=false;U.menu=false;U.help=false;U.cursor=1;U.edit=nil;U.edit_ids={};U.cache={}
    U.clear();U.stamp=B.stamp(host.state());U.message='Trace four tiles. Release to inspect. Enter / Commit executes.'
end
function U.cell_at(x,y,logical)
    local col,row=math.floor((x-origin_x)/pitch),math.floor((y-origin_y)/pitch)
    if col<0 or col>=C.size or row<0 or row>=C.size then return end
    if not logical and ((x-origin_x)%pitch>=tile_size or (y-origin_y)%pitch>=tile_size)then return end
    return row*C.size+col+1
end
function U.center(cell)return origin_x+((cell-1)%C.size)*pitch+tile_size/2,origin_y+math.floor((cell-1)/C.size)*pitch+tile_size/2 end
function U.extend(cell)
    if not cell then return end
    local s=host.state();local t=s.boggle.board[cell];if not t then return end
    U.cursor=cell
    for i,id in ipairs(U.path)do if id==t.id then
        if i<#U.path then for j=#U.path,i+1,-1 do U.path[j]=nil end;changed()end
        return
    end end
    local at=U.path[#U.path]and B.cell(s,U.path[#U.path])
    if at and math.abs((cell-1)%C.size-(at-1)%C.size)+math.abs(math.floor((cell-1)/C.size)-math.floor((at-1)/C.size))~=1 then
        U.message='Connect one edge at a time.';return
    end
    if #U.path>=C.route_size then U.message='Four tiles selected. Backtrack, Clear, or Commit.';return end
    U.path[#U.path+1]=t.id;U.message=nil;changed()
end
-- Supercover grid traversal. At a simultaneous row/column crossing there is
-- no unique orthogonal route: stop, rather than inventing a helpful corner.
function U.segment(x0,y0,x1,y1)
    local first=U.cell_at(x0,y0,true);local last=U.cell_at(x1,y1,true)
    if not first or not last then U.drag=false;U.last_point=nil;return end
    local cx,cy=(first-1)%C.size,math.floor((first-1)/C.size)
    local ex,ey=(last-1)%C.size,math.floor((last-1)/C.size)
    local dx,dy=x1-x0,y1-y0
    local sx=dx>0 and 1 or -1;local sy=dy>0 and 1 or -1
    local tx=dx==0 and math.huge or (origin_x+(cx+(sx>0 and 1 or 0))*pitch-x0)/dx
    local ty=dy==0 and math.huge or (origin_y+(cy+(sy>0 and 1 or 0))*pitch-y0)/dy
    local stepx=dx==0 and math.huge or pitch/math.abs(dx)
    local stepy=dy==0 and math.huge or pitch/math.abs(dy)
    for _=1,C.size*2 do
        if cx==ex and cy==ey then return end
        if math.abs(tx-ty)<1e-8 then
            U.drag=false;U.last_point=nil;U.message='Ambiguous corner: cross one shared edge.';return
        elseif tx<ty then cx=cx+sx;tx=tx+stepx else cy=cy+sy;ty=ty+stepy end
        if cx<0 or cx>=C.size or cy<0 or cy>=C.size then U.drag=false;return end
        U.extend(cy*C.size+cx+1)
    end
end
local function submit(intent)
    if host.busy()then return false end
    intent.revision=B.stamp(host.state())
    local ok,why=host.submit(intent)
    if ok then U.clear();U.pending=nil;U.cache={};U.stamp=B.stamp(host.state())else U.message=why end
    return ok
end
function U.commit()
    if not U.forecast or U.dirty or not U.forecast.complete or not U.forecast.valid
        or U.forecast.stamp~=B.stamp(host.state())then U.message='Inspect a valid four-tile forecast before committing.';return false end
    -- Use the forecast's stamp, not a newer revision supplied at click time.
    local ok,why=host.submit({kind='boggle_route',ids=B.copy(U.path),revision=U.forecast.stamp})
    if ok then U.clear();U.pending=nil;U.cache={};U.stamp=B.stamp(host.state())else U.message=why;U.dirty=true;U.forecast=nil end
    return ok
end
function U.update()
    if not U.active()then return end
    local s=host.state()
    if host.busy()then return end
    if s.card_trial.phase=='intermission'then submit({kind='boggle_intermission'});U.edit=nil;U.edit_ids={};return end
    local stamp=B.stamp(s)
    if U.stamp~=stamp then U.stamp=stamp;U.path={};U.cache={};changed()end
end
function U.refresh_forecast()
    local s=host.state();local stamp=B.stamp(s)
    if U.stamp~=stamp then U.stamp=stamp;U.path={};U.cache={};changed()end
    if U.dirty then
        U.dirty=false
        if #U.path>0 then
            local cached=U.cache.stamp==stamp and U.cache.entries and U.cache.entries[table.concat(U.path,',')]~=nil
            local started=now();U.forecast=B.forecast(s,U.path,G,U.cache)
            U.forecast_cached=cached
            add_metric('forecast',{ms=(now()-started)*1000,tiles=#U.path,version=U.generation,stamp=stamp,cached=cached})
        end
        U.forecast_generation=U.generation
    end
end
local function edit_pick(cell)
    local tile=host.state().boggle.board[cell];if not tile then return end
    U.cursor=cell
    if not U.edit then U.message='R Rotate, V Swap, or K Skip.';return end
    if U.edit=='rotate'then U.edit_ids={tile.id}
    else
        if #U.edit_ids==2 then U.edit_ids={}end
        if U.edit_ids[1]~=tile.id then U.edit_ids[#U.edit_ids+1]=tile.id end
    end
end
function U.keypressed(key,isrepeat)
    if not U.setup and not U.active()then return false end
    if isrepeat then return true end
    if U.setup then
        if key=='escape'then U.setup=false;host.exit()
        elseif key=='left' or key=='right'then local seeds={101,202,303};local n=1;for i,x in ipairs(seeds)do if x==U.seed then n=i end end;U.seed=seeds[(n-1+(key=='right' and 1 or -1)+3)%3+1]
        elseif key=='return' or key=='kpenter'then U.start()end;return true
    end
    if host.busy()then return true end
    if key=='f1'then U.help=not U.help;return true end
    if U.help then if key=='escape' or key=='return'then U.help=false end;return true end
    local s=host.state();local phase=s.card_trial.phase
    if U.menu then
        if key=='escape' or key=='return'then U.menu=false
        elseif key=='e'then U.menu=false;submit({kind='boggle_reset'})
        elseif key=='r'then U.start()
        elseif key=='p'then U.open()
        elseif key=='t'then U.menu=false;host.exit()end;return true
    end
    if phase=='death' or phase=='victory'then
        if key=='return' or key=='r'then U.start()elseif key=='p'then U.open()elseif key=='escape' or key=='t'then host.exit()end;return true
    end
    if phase=='edit'then
        if key=='r'then U.edit='rotate';U.edit_ids={}
        elseif key=='v'then U.edit='swap';U.edit_ids={}
        elseif key=='k'then U.edit='skip';U.edit_ids={}
        elseif key=='escape'then
            if U.edit or #U.edit_ids>0 then U.edit=nil;U.edit_ids={}else U.menu=true end
        elseif key=='return'then
            if submit({kind='boggle_edit',edit=U.edit,first=U.edit_ids[1],second=U.edit_ids[2]})then U.edit=nil;U.edit_ids={}end
        end
    elseif key=='escape'then if #U.path>0 then U.clear()else U.menu=true end;return true
    elseif key=='return' or key=='kpenter'then U.commit();return true
    elseif key=='y'then submit({kind='boggle_shuffle'});return true
    elseif key=='c'then U.clear();return true
    elseif key=='backspace'then U.path[#U.path]=nil;changed();return true
    elseif tonumber(key) and tonumber(key)>=1 and tonumber(key)<=4 then U.focus_step=tonumber(key);return true
    elseif key=='tab'then U.focus_step=(U.focus_step or 0)%math.max(1,#U.path)+1;return true
    end
    local delta=({up=-C.size,w=-C.size,right=1,d=1,down=C.size,s=C.size,left=-1,a=-1})[key]
    if delta then
        local next=U.cursor+delta
        if next>=1 and next<=C.size*C.size and math.abs((next-1)%C.size-(U.cursor-1)%C.size)+math.abs(math.floor((next-1)/C.size)-math.floor((U.cursor-1)/C.size))==1 then U.cursor=next;U.message=nil end
    elseif key=='space'then if phase=='edit'then edit_pick(U.cursor)else U.extend(U.cursor)end end
    return true -- swallow every legacy movement, ability, utility and save input
end
function U.mousepressed(x,y,button)
    if not U.setup and not U.active()then return false end
    if button==2 then U.keypressed('escape');return true end
    if button~=1 then return true end
    if U.setup then if y>=482 and y<530 then U.start()elseif y>=430 and y<470 then U.keypressed(x<512 and 'left' or 'right')elseif y>=560 then U.keypressed('escape')end;return true end
    if host.busy()then return true end
    local s=host.state();local phase=s.card_trial.phase
    if U.help then U.help=false;return true end
    if U.menu then
        local keys={'return','e','r','p','t'};local row=math.floor((y-186)/48)+1
        if x>=272 and x<752 and row>=1 and row<=5 then U.keypressed(keys[row])end;return true
    end
    if phase=='death' or phase=='victory'then
        if y>=294 and y<334 then U.keypressed('r')elseif y>=342 and y<382 then U.keypressed('p')elseif y>=390 and y<430 then U.keypressed('t')end;return true
    end
    local cell=U.cell_at(x,y)
    if phase=='edit'then
        if cell then edit_pick(cell)
        elseif x>=80 and x<568 and y>=362 and y<402 then U.keypressed(x<240 and 'r' or x<400 and 'v' or 'k')
        elseif x>=672 and x<828 and y>=476 and y<506 then U.keypressed('return')
        elseif x>=836 and x<992 and y>=476 and y<506 then U.keypressed('escape')
        elseif x>=880 and y>=546 and y<578 then U.menu=true end;return true
    end
    if cell then U.clear();U.drag=true;U.last_point={x,y};U.extend(cell)
    elseif x>=672 and x<828 and y>=476 and y<506 then U.commit()
    elseif x>=836 and x<992 and y>=476 and y<506 then U.clear()
    elseif x>=672 and x<992 and y>=508 and y<536 then submit({kind='boggle_shuffle'})
    elseif x>=880 and y>=546 and y<578 then U.menu=true end
    return true
end
function U.mousemoved(x,y)
    if not U.active()or U.setup then return U.setup==true end
    if host.busy()or U.menu or U.help then return true end
    if U.drag and U.last_point then
        U.segment(U.last_point[1],U.last_point[2],x,y)
        if U.drag then U.last_point={x,y}end
    else local cell=U.cell_at(x,y);if cell then U.cursor=cell;U.message=nil end end
    return true
end
function U.mousereleased(x,y,button)
    if not U.active()then return false end
    if button==1 then
        if U.drag and U.last_point then U.segment(U.last_point[1],U.last_point[2],x,y)end
        U.drag=false;U.last_point=nil
    end
    return true
end
function U.gamepadpressed(button)
    if not U.setup and not U.active()then return false end
    local keys={a='space',b='escape',x='return',y='y',start='escape',dpup='up',dpdown='down',dpleft='left',dpright='right',leftshoulder='backspace',rightshoulder='tab'}
    if keys[button]then U.keypressed(keys[button])end;return true
end
local function text(t,x,y,font,color,width)
    local g=love.graphics;g.setFont(font);g.setColor(color or Pixel.colors.text)
    if width then g.printf(t,x,y,width)else g.print(t,x,y)end
end
function U.draw_setup()
    if not U.setup then return false end
    local body,menu,display=host.fonts()
    Pixel.frame('BOGGLE TRIAL',192,80,640,464,display)
    for i,line in ipairs({'Adventurer · two fights · no starting boons',
        'Trace four connected tiles on the action board.',
        'Arena previews include enemy responses.',
        'Release to inspect; Enter / Commit executes.',
        '! tiles hurt. Outlined powers are one-use.',
        'Two boon pickups build your combat engine.'})do Pixel.center(line,146+(i-1)*42,menu,Pixel.colors.text)end
    Pixel.center('<  SEED '..U.seed..'  >',442,menu,Pixel.colors.selected)
    Pixel.option('BEGIN BOGGLE TRIAL',272,490,480,true,display)
    Pixel.center('Left/right seed · Enter begin · Esc back',566,body,Pixel.colors.text)
    Pixel.center('Nonpersistent experiment. Normal saves and achievements stay untouched.',594,body,Pixel.colors.muted)
    return true
end
local function arrow(dir,x,y)
    local g=love.graphics;local d=C.directions[dir];local dx,dy=d[1],d[2]
    local px,py=-dy,dx
    g.line(x-dx*6,y-dy*6,x+dx*7,y+dy*7)
    g.line(x+dx*7,y+dy*7,x+dx*2+px*4,y+dy*2+py*4)
    g.line(x+dx*7,y+dy*7,x+dx*2-px*4,y+dy*2-py*4)
end
function U.draw_sidebar()
    if not U.active()then return end
    local s=host.state();local b=s.boggle;local body,menu,display=host.fonts();local g=love.graphics
    Pixel.frame('ACTION BOARD',656,22,352,514,display)
    text('Fight '..s.card_trial.fight..'/2 · Seed '..U.seed..' · '..#U.path..'/'..C.route_size,672,52,body)
    for slot,id in ipairs({'core:boon/rhythm_of_steel','core:boon/detonation_bloom'})do
        local x=672+(slot-1)*164;local owned=G.player(s).boons[id]
        local upcoming=U.forecast and U.forecast.valid and G.player(U.forecast.state).boons[id]
        if owned or upcoming then host.visual(id..'/icon',x,74,24,24)end
        text((owned or upcoming)and ((not owned and '+'or '')..(slot==1 and 'Rhythm' or 'Bloom'))or 'Empty slot',x+28,82,body,(owned or upcoming)and Pixel.colors.selected or Pixel.colors.muted)
    end
    local selected={};for n,id in ipairs(U.path)do selected[id]=n end
    for _,id in ipairs(U.edit_ids or {})do selected[id]='*'end
    for i=1,C.size*C.size do
        local tile=b.board[i];local x,y=U.center(i);x=x-tile_size/2;y=y-tile_size/2
        local d=tile and B.definition(s,tile)
        g.setColor(tile and selected[tile.id]and Pixel.colors.selected or d and d.harmful and {.8,.44,.4}or Pixel.colors.edge)
        g.rectangle('fill',x,y,tile_size,tile_size);g.setColor(Pixel.colors.background);g.rectangle('fill',x+2,y+2,tile_size-4,tile_size-4)
        if d then
            if d.finite then g.setColor(Pixel.colors.light);g.rectangle('line',x+4,y+4,tile_size-8,tile_size-8)end
            if d.boon_id then host.visual(d.boon_id..'/icon',x+15,y+7,20,20)
            elseif d.dir then g.setColor(Pixel.colors.text);arrow(d.dir,x+25,y+16)
            elseif d.kind=='guard'then g.setColor(Pixel.colors.text);g.polygon('line',x+18,y+7,x+32,y+7,x+32,y+19,x+25,y+25,x+18,y+19)
            elseif d.harmful then text('!',x+20,y+6,menu,Pixel.colors.selected)
            else text(d.kind=='mend' and '+'or '+1',x+19,y+7,menu)end
            text(d.short,x+3,y+33,body,Pixel.colors.text,45)
            if selected[tile.id]then text(tostring(selected[tile.id]),x+5,y+4,body,Pixel.colors.selected)end
        end
        if i==U.cursor then g.setColor(Pixel.colors.text);g.rectangle('line',x+5,y+5,tile_size-10,tile_size-10)end
    end
    local hover=b.board[U.cursor];local d=hover and B.definition(s,hover)
    text(U.message or (d and d.name..': '..d.description or ''),672,434,body,Pixel.colors.text,320)
    local ready=U.forecast and U.forecast.valid and U.forecast.complete and not U.dirty and not host.busy()
    Pixel.option(s.card_trial.phase=='edit' and 'Enter Apply edit' or 'Enter Commit',672,484,156,ready,body,not ready and s.card_trial.phase~='edit')
    Pixel.option('C Clear',836,484,156,false,body)
    Pixel.option('Y Shuffle · '..b.shuffle_left..' left · 1 Wait',672,516,320,false,body,b.shuffle_left==0 or s.card_trial.phase~='combat')
end
function U.draw_preview()
    if not U.active()then return false end
    if host.busy()or U.menu or U.help then return true end
    -- Coalesce the complete ordered input trajectory, including callbacks that
    -- arrived after update, immediately before drawing this frame's ghost.
    U.refresh_forecast()
    local s=host.state();local body,menu=host.fonts();local g=love.graphics
    if s.card_trial.phase=='edit'then
        Pixel.frame('ONE BOARD EDIT',80,308,488,222,menu)
        Pixel.option('R Rotate',96,374,144,U.edit=='rotate',body)
        Pixel.option('V Swap',256,374,144,U.edit=='swap',body)
        Pixel.option('K Skip',416,374,136,U.edit=='skip',body)
        local detail='Rotate: select an ordinary arrow tile. Swap: select two tiles. Enter applies once.'
        if U.edit=='rotate' and U.edit_ids[1]then
            local _,t=B.cell(s,U.edit_ids[1]);local d=C.types[t.type]
            detail=d.dir and not d.finite and (d.name..' -> '..C.types[d.kind..(d.dir%4+1)].name..'. Transfer one refill weight; future draws change too.')or 'Choose an ordinary directional tile.'
        elseif U.edit=='swap'then detail='Select two tiles ('..#U.edit_ids..'/2). Their positions swap; refill odds stay the same.'end
        text(detail,96,412,body,Pixel.colors.text,456)
        text('Finite supply, boons, HP, Guard and pending effects carry. No free boon reward.',96,482,body,Pixel.colors.muted,456)
        return true
    end
    local f=U.forecast;if not f or U.dirty then return true end
    local step=f.steps[U.focus_step or #f.steps];if not step then return true end
    for n,r in ipairs(f.steps)do
        for _,e in ipairs(r.events)do if e.kind=='Moved' and e.source_id==s.player_id then
            for _,cell in ipairs(e.path or {})do
                local x,y=host.screen(cell.x,cell.y);g.setColor(.6,.8,.65,.7);g.rectangle('line',x+15,y+15,18,18)
            end
            local x,y=host.screen(e.x,e.y);g.setColor(Pixel.colors.selected);g.rectangle('line',x+10,y+10,28,28);text(tostring(n),x+12,y+12,body)
        elseif e.kind=='AttackPerformed' then
            for _,cell in ipairs(e.target_cells)do
                local x,y=host.screen(cell.x,cell.y)
                g.setColor(e.source_id==s.player_id and {.8,.8,.6,.6}or {.9,.45,.45,.5});g.rectangle('line',x+9,y+9,30,30)
                if e.source_id==s.player_id then text(tostring(n),x+34,y+10,body,Pixel.colors.muted)end
            end
        end end
    end
    for _,e in ipairs(step.events)do
        if e.kind=='AttackPerformed' then for _,c in ipairs(e.target_cells)do
            local x,y=host.screen(c.x,c.y);g.setColor(e.source_id==s.player_id and {1,.9,.65,.9}or {1,.4,.4,.8});g.rectangle('line',x+5,y+5,38,38)
        end end
    end
    local shown=step.state;local p=G.player(shown);host.ghost(p.position.x,p.position.y)
    for _,e in ipairs(s.entities)do if e.faction=='enemy'then
        local after=G.entity(shown,e.id);local x,y=host.screen(after and after.position.x or e.position.x,after and after.position.y or e.position.y)
        g.setColor(.025,.025,.025,.9);g.rectangle('fill',x,y+31,48,16)
        text(after and (e.hp..'>'..after.hp)or 'DEAD',x+2,y+33,body,Pixel.colors.selected)
        if after and (after.position.x~=e.position.x or after.position.y~=e.position.y)then
            if host.entity_ghost then host.entity_ghost(after,after.position.x,after.position.y)end
            g.setColor(Pixel.colors.muted);g.rectangle('line',x+7,y+7,34,34)
        end
        local fury=shown.boggle.fury[e.id]or 0
        if fury>0 then text('F+'..fury,x+4,y+2,body,Pixel.colors.selected)end
    end end
    return true
end
function U.draw_hud()
    local s=host.state();local p=G.player(s);local b=s.boggle;local f=U.forecast;local body,menu=host.fonts();local g=love.graphics
    g.setColor(Pixel.colors.background);g.rectangle('fill',16,544,992,90)
    local endp=f and G.player(f.state)or p;local hp=endp.hp
    text(('HP %d -> %d/%d'):format(p.hp,hp,p.max_hp),28,550,menu,hp<=0 and {1,.4,.4}or Pixel.colors.text)
    text('Guard '..b.guard..(f and ' -> '..f.state.boggle.guard or ''),282,550,menu)
    text('Time '..s.clock..(f and ' -> '..f.state.clock or ''),500,550,menu)
    text('Esc Menu',880,550,body)
    local phase=s.card_trial.phase
    local line=b.defect and ('DEFECT: '..b.defect)or host.busy()and 'EXECUTING · input locked' or phase=='edit' and 'Choose one edit; Enter begins fight 2.'
        or U.dirty and 'Updating forecast · commit locked' or f and (not f.valid and ('INVALID: '..f.reason)or
            (hp<=0 and ('LETHAL: death at tile '..f.executed..' · remaining tiles stay unspent')or f.state.card_trial.phase~='combat' and 'ENCOUNTER ENDS · remaining tiles stay unspent' or
                (#U.path==4 and 'Four commands, with enemy responses. Enter / Commit executes.' or 'Partial forecast · choose four connected tiles.')))
        or 'Drag from any tile. Four connected tiles; release to inspect, Enter to commit.'
    text(line,28,571,body,hp<=0 and {1,.4,.4}or Pixel.colors.selected,960)
    local timeline={}
    for n,r in ipairs(f and f.steps or {})do
        local healing,cost,damage=0,0,0
        local mend=r.plan.definition and r.plan.definition.kind=='mend'
        for _,e in ipairs(r.events)do if e.target_id==s.player_id then
            if e.kind=='Healed'then healing=healing+(e.actual_gain or e.amount or 0)
            elseif e.kind=='HealthCostPaid'then cost=cost+e.hp_cost
            elseif e.kind=='DamageTaken'then damage=damage+e.actual_loss end
        end end
        local detail=(mend or healing>0)and (' +'..healing..' heal')or ''
        if cost>0 then detail=detail..' -'..cost..' cost'end
        if damage>0 then detail=detail..' -'..damage..' dmg'end
        timeline[#timeline+1]=(U.focus_step==n and '['or '')..n..': '..G.player(r.state).hp..' HP'..detail..(U.focus_step==n and ']'or '')
    end
    text(#timeline>0 and table.concat(timeline,'  >  ')or 'Tile arrows command the hero independently of drag direction.',28,586,body,Pixel.colors.text,960)
    local focused=f and U.focus_step and f.steps[U.focus_step]and f.steps[U.focus_step].state
    local pending=focused and focused.boggle or f and f.state.boggle or b;local fury=0
    for id,v in pairs(pending.fury)do if G.entity(focused or f and f.state or s,id)then fury=fury+v end end
    local detail_state=focused or f and f.state or s;local detail_owner=G.player(detail_state)
    local slots=0;for _ in pairs(detail_owner.boons)do slots=slots+1 end
    local ward=require('src.boons').ward_total(detail_state,detail_owner)
    local status={};for k in pairs(detail_owner.statuses or {})do status[#status+1]=k end;table.sort(status)
    text((focused and 'Step '..U.focus_step or 'After plan')..': Guard '..pending.guard..' · Focus '..pending.focus..' · Dull '..pending.dull..' · Fury '..fury..' · Ward '..ward..' · Boons '..slots..'/2'..
        (#status>0 and (' · '..table.concat(status,','))or '')..
        (U.focus_step and (' · Inspect step '..U.focus_step)or ''),28,601,body,Pixel.colors.text)
    text('Arrows/WASD cursor · Space add · Backspace undo · 1-4 inspect step · C clear · F1 help',28,616,body,Pixel.colors.muted)
end
function U.draw_overlay()
    if not U.active()then return end
    local s=host.state();local phase=s.card_trial.phase
    if host.busy()or not U.menu and not U.help and phase~='death' and phase~='victory'then return end
    local body,menu,display=host.fonts();Pixel.dim(.7)
    Pixel.frame(U.help and 'BOGGLE CONTROLS' or U.menu and 'TRIAL MENU' or phase=='death' and 'TRIAL DEATH' or 'TRIAL VICTORY',224,104,576,440,display)
    if U.help then
        for i,line in ipairs({'Start on any tile; connect four distinct neighbours.',
            'Tile arrows command the hero, not the drag direction.',
            'Drag back to truncate. Release inspects, never commits.',
            'Mouse: trace route, then Commit. Enter also commits.',
            'Keyboard: arrows cursor, Space add, Backspace undo.',
            '1-4 inspect a step; C clear; Escape cancel/menu.',
            'Enemy turns resolve between commands in the forecast.',
            '! tiles hurt; inner-framed powers are finite.',
            'Y Shuffle: once per trial; costs one real Wait.',
            'Emergency Reset is in the menu; costs two Waits.',
            'Esc closes help. Normal saves stay untouched.'})do Pixel.center(line,150+(i-1)*31,body,Pixel.colors.text)end
    elseif U.menu then
        for i,line in ipairs({'Enter Resume','E Emergency Reset · two Waits','R Replay same seed','P Choose seed','T Return to title'})do Pixel.option(line,272,194+(i-1)*48,480,i==1,menu)end
        Pixel.center('Reset preserves finite tiles and penalties; rebuilds ordinary tiles.',472,body,Pixel.colors.text,240,544)
        Pixel.center('Both Waits advance enemies. Stops if combat ends.',502,body,Pixel.colors.selected,240,544)
    else
        Pixel.center('Seed '..U.seed..' · Fight '..s.card_trial.fight..'/2 · HP '..G.player(s).hp,180,menu,Pixel.colors.text)
        Pixel.center('Trial progress is not saved.',232,body,Pixel.colors.muted)
        Pixel.option('Enter / R  Replay same seed',272,302,480,true,menu)
        Pixel.option('P  Choose seed',272,350,480,false,menu)
        Pixel.option('T  Return to title',272,398,480,false,menu)
    end
end
-- Remember exactly which selection this rendered frame contains. The timestamp
-- is taken after LÖVE's actual present call, including any present/vsync wait.
function U.frame_drawn()
    if not U.active()or host.busy()or U.menu or U.help then return end
    U.drawn_path_version=U.generation
    U.drawn_ghost_version=not U.dirty and U.forecast_generation or nil
end
function U.frame_submitted()
    local p=U.pending
    if not U.active()or not p or host.busy()then return end
    local ms=(now()-p.input)*1000
    if not p.path_done and U.drawn_path_version==p.version then add_metric('path',{ms=ms,version=p.version,tiles=#U.path});p.path_done=true end
    if U.drawn_ghost_version==p.version and not U.dirty then
        if #U.path>0 then add_metric('ghost',{ms=ms,version=p.version,tiles=#U.path,stamp=U.stamp,cached=U.forecast_cached})end
        U.pending=nil
    end
end
function U.install_present_hook()
    if U.present_hook_installed or not (love and love.graphics and love.graphics.present)then return end
    U.present_hook_installed=true
    local present=love.graphics.present
    love.graphics.present=function(...)
        local result=present(...)
        U.frame_submitted()
        return result
    end
end
for _,name in ipairs({'keypressed','mousepressed','mousemoved','mousereleased','gamepadpressed'})do
    local callback=U[name]
    U[name]=function(...)
        local previous=U.input_time;U.input_time=previous or now()
        local result=callback(...);U.input_time=previous;return result
    end
end
return U
