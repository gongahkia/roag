-- Trial-only presentation/input. All commitments and resource validation use Game.
local Game=require('src.game')
local Trial=require('src.card_trial')
local Cards=require('src.cards')
local Boons=require('src.boons')
local Catalog=require('src.boon_content')
local Classes=require('src.classes')
local Content=require('src.content')
local Terrain=require('src.terrain')
local Pixel=require('src.pixel_ui')
local C=Trial.content
local U={preset=1,seed=C.seed,reward_selection=1,focus=1}
local host
local dirs={w={0,-1},up={0,-1},d={1,0},right={1,0},s={0,1},down={0,1},
    a={-1,0},left={-1,0},q={-1,-1},e={1,-1},z={-1,1},c={1,1}}
local compass={{0,-1},{1,-1},{1,0},{1,1},{0,1},{-1,1},{-1,0},{-1,-1}}
function U.attach(value)host=value end
function U.active()local s=host and host.state();return s and s.card_trial~=nil end
local function clear()
    U.armed=nil;U.hover=nil;U.replacing=nil;U.aim=nil;U.preview=nil;U.forecast=nil;U.targets={};U.basic_aim=false
    host.target(nil)
end
function U.open(seed)
    U.setup=true;U.seed=seed or U.seed;U.menu=false;U.help=false;clear()
    U.message='Two fights. No save, Continue or achievement progress.';host.setup()
end
function U.start()
    host.start(U.seed,C.preset_order[U.preset]);U.setup=false
    clear();U.focus=1;U.menu=false;U.reward_hidden=false;U.last_phase=nil;U.last_boon=nil
    U.message='Step immediately with WASD/arrows. Click a foe to Strike.'
end
local function technique(id)return {kind='technique',card_id=id}end
local function set_preview(intent)
    U.aim=intent;U.preview=Game.card_preview(host.state(),intent)
    U.forecast=Game.card_forecast(host.state(),intent)
end
local function inspect_card(id,replace)
    U.hover=id;U.replacing=replace;U.aim=nil;U.forecast=nil;U.targets={}
    local s=host.state();local def=Cards.card(s.card_trial.cards,id)
    if not def then return end
    local intent=replace and {kind='replace_card',card_id=id} or technique(id)
    U.preview=Game.card_preview(s,intent)
    if not replace and def.action~='trial_heal'then U.targets=Game.card_targets(s,intent)end
end
local function commit(intent)
    if host.busy()then return end
    local result=Game.card_preview(host.state(),intent)
    if not result.valid then U.message=result.reason;U.preview=result;return false end
    local ok=host.submit(intent)
    if ok then clear();U.message='Step or Strike freely. Techniques retain the other cards.'end
    return ok
end
local function activate(slot)
    local s=host.state();local id=s.card_trial.cards.hand[slot]
    if not id then return end
    U.focus=slot;U.basic_aim=false;U.armed=id;inspect_card(id)
    if Cards.card(s.card_trial.cards,id).action=='trial_heal'then U.armed=nil;commit(technique(id))
    else U.message='Armed: click a highlighted target to play. Esc cancels.'end
end
local function basic_at(x,y)
    local s=host.state();local p=Game.player(s).position;local dx,dy=x-p.x,y-p.y
    local enemy=false
    for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp and e.hp>0 and e.position.x==x and e.position.y==y then enemy=true end end
    if enemy and math.max(math.abs(dx),math.abs(dy))==1 then
        return {kind='trial_basic',action='basic_attack',dx=dx,dy=dy}
    end
    return {kind='trial_basic',action='move',dx=dx,dy=dy}
end
local function aim_at(x,y)
    if U.armed then local intent=technique(U.armed);intent.x,intent.y=x,y;set_preview(intent)
    elseif U.basic_aim then
        local p=Game.player(host.state()).position
        set_preview({kind='trial_basic',action='basic_attack',dx=x-p.x,dy=y-p.y})
    else set_preview(basic_at(x,y))end
end
local function keyboard_aim(key)
    local s=host.state();local d=dirs[key];local p=Game.player(s).position
    if U.armed and Cards.card(s.card_trial.cards,U.armed).action=='knight_hop'then
        if #U.targets==0 then U.message='No free L-shaped landing';return end
        local backwards=key=='left' or key=='up' or key=='a' or key=='w'
        U.target_index=((U.target_index or (backwards and 1 or 0))-1+(backwards and -1 or 1)+#U.targets)%#U.targets+1
        local at=U.targets[U.target_index];aim_at(at.x,at.y)
    elseif U.armed and Cards.card(s.card_trial.cards,U.armed).action=='dash'then
        local preview=Game.preview(s,'dash',d[1],d[2]);local at=preview.valid and preview.cells[preview.distance]
        aim_at(at and at.x or p.x+d[1],at and at.y or p.y+d[2])
    else aim_at(p.x+d[1],p.y+d[2])end
end
function U.on_events(events)
    local reasons={rhythm_of_steel='primary hit after moving',ring_of_reversal='hit the attacker you escaped',
        emberbrand='direct weapon hit',shared_agony='damage beside another Burning foe',
        pulse_step='entered beside at least two foes',detonation_bloom='credited kill',grave_dividend='credited kill'}
    local kinds,messages={},{}
    for _,e in ipairs(events)do kinds[e.id]=e.kind end
    local function show(message)
        messages[#messages+1]=message
        if #messages>2 then table.remove(messages,1)end
    end
    for _,e in ipairs(events)do
        if e.kind=='BoonActivated'then
            local reason=reasons[e.boon_id:match('/([^/]+)$')] or 'triggered'
            if e.boon_id=='core:boon/rhythm_of_steel' and kinds[e.trigger_event_id]=='Moved'then reason='movement primed the next primary hit'end
            show(Catalog.get(e.boon_id).name..': '..reason)
        elseif e.kind=='RingReversalPrimed'then show('Ring of Reversal: escaped locked attack; next hit on that foe gains damage')
        elseif e.kind=='RingReversalSpent'then show('Ring of Reversal: bonus damage on the attacker you escaped')
        end
    end
    if #messages>0 then U.last_boon=table.concat(messages,' / ')end
end
function U.update()
    if not U.active()then return end
    local t=host.state().card_trial;local busy=host.busy()
    if U.last_phase~=t.phase then
        U.last_phase=t.phase;U.reward_hidden=false
        if t.phase~='combat'then clear()end
    end
    if t.phase=='reward' and not U.reward_hidden and not busy then host.reward_view(U.reward_selection)end
    if U.was_busy and not busy and U.mouse_x then U.mousemoved(U.mouse_x,U.mouse_y)end
    U.was_busy=busy
end
local function reward_key(key)
    if U.reward_hidden then
        if key=='return' or key=='r'then U.reward_hidden=false;host.reward_view(U.reward_selection)
        elseif key=='escape'then U.menu=true end
    elseif key=='escape'then U.reward_hidden=true;host.hide_reward()
    elseif tonumber(key) and tonumber(key)>=1 and tonumber(key)<=3 then
        U.reward_selection=tonumber(key);host.reward_view(U.reward_selection)
    elseif key=='up' or key=='left'then
        U.reward_selection=(U.reward_selection+1)%3+1;host.reward_view(U.reward_selection)
    elseif key=='down' or key=='right'then
        U.reward_selection=U.reward_selection%3+1;host.reward_view(U.reward_selection)
    elseif key=='return' or key=='kpenter'then
        if host.submit({kind='trial_reward',offer_index=U.reward_selection})then
            host.hide_reward();clear();U.last_boon=nil
            U.message='Fight 2: enemies enter from new flanks. HP, hand and charges carried.'
        end
    end
end
function U.keypressed(key,isrepeat)
    if not host then return false end
    if U.setup then
        if isrepeat then return true end
        if key=='escape'then U.setup=false;host.exit()
        elseif tonumber(key) and tonumber(key)>=1 and tonumber(key)<=3 then U.preset=tonumber(key)
        elseif key=='down' or key=='s'then U.preset=U.preset%3+1
        elseif key=='up' or key=='w'then U.preset=(U.preset+1)%3+1
        elseif key=='left' or key=='right'then
            local i=1;for n,v in ipairs(C.seeds)do if v==U.seed then i=n end end
            U.seed=C.seeds[(i-1+(key=='right' and 1 or -1)+#C.seeds)%#C.seeds+1]
        elseif key=='return' or key=='kpenter'then Pixel.press();U.start()end
        return true
    end
    if not U.active()then return false end
    if isrepeat then return true end
    if host.inspection()then if key=='u'then return true end;return false end
    if key=='f3' or key=='f5'then return false end
    if key=='f1'then U.help=not U.help;return true end
    if U.help then if key=='escape' or key=='return'then U.help=false end;return true end
    if key=='i' or key=='v'then host.inspect();return true end
    if host.busy()then return true end
    if U.menu then
        if key=='escape' or key=='return'then U.menu=false
        elseif key=='r'then U.start()
        elseif key=='p'then host.hide_reward();U.open()
        elseif key=='t'then U.menu=false;host.exit()end
        return true
    end
    local phase=host.state().card_trial.phase
    if phase=='victory' or phase=='death'then
        if key=='return' or key=='r'then U.start()
        elseif key=='p'then U.open()
        elseif key=='t' or key=='escape'then host.exit()end
        return true
    end
    if phase=='reward'then reward_key(key);return true end
    if tonumber(key) and tonumber(key)>=1 and tonumber(key)<=3 then activate(tonumber(key))
    elseif key=='tab'then
        U.focus=U.focus%3+1;inspect_card(host.state().card_trial.cards.hand[U.focus]);U.message='Enter selects; R replaces for one Wait.'
    elseif key=='r'then
        local id=U.armed or host.state().card_trial.cards.hand[U.focus]
        commit({kind='replace_card',card_id=id})
    elseif key=='f'then clear();U.basic_aim=true;U.message='Strike: click adjacent target, or direction then Enter.'
    elseif key=='escape' or key=='backspace'then
        if U.armed or U.basic_aim or U.hover or U.aim then clear();U.message='Cancelled. Step and Strike ready.'else U.menu=true end
    elseif key=='return' or key=='kpenter'then
        if U.aim then commit(U.aim)else activate(U.focus)end
    elseif dirs[key]then
        if U.armed or U.basic_aim then keyboard_aim(key)
        else commit(basic_at(Game.player(host.state()).position.x+dirs[key][1],Game.player(host.state()).position.y+dirs[key][2]))end
    end
    return true -- no classic utility, reset or B loadout escape routes
end
-- Fixed hit regions. The Replace subregion is deliberately disjoint from activation.
function U.card_at(x,y)
    if x<672 or x>=996 then return end
    local slot=math.floor((x-672)/108)+1;local left=672+(slot-1)*108
    if slot>3 or x>=left+100 then return end
    if y>=296 and y<324 then return slot,'replace'end
    if y>=168 and y<290 then return slot,'card'end
end
function U.mousemoved(x,y)
    U.mouse_x,U.mouse_y=x,y
    if not U.active() or U.setup or host.busy() or U.menu or U.help or host.inspection() or host.state().card_trial.phase~='combat'then return false end
    local slot,region=U.card_at(x,y)
    if slot then
        U.focus=slot;inspect_card(host.state().card_trial.cards.hand[slot],region=='replace')
    else
        U.hover=nil;U.replacing=nil
        if U.armed then U.targets=Game.card_targets(host.state(),technique(U.armed))else U.targets={}end
        local cx,cy=host.cell(x,y)
        if cx then aim_at(cx,cy)
        else U.aim=nil;U.preview=nil;U.forecast=nil end
    end
    return true
end
function U.mousepressed(x,y,button)
    if not U.setup and not U.active()then return false end
    if button==2 then U.keypressed('escape');return true end
    if button~=1 then return true end
    if host.inspection()then
        if x>=88 and x<576 and y>=490 and y<514 then return true end
        return false
    end
    if U.setup then
        local row=Pixel.row_at(232,182,560,56,3,78,x,y)
        if row then U.preset=row
        elseif y>=434 and y<470 then U.keypressed(x<512 and 'left' or 'right')
        elseif y>=482 and y<526 then U.start()
        elseif y>=556 then U.keypressed('escape')end
        return true
    end
    if host.busy()then return true end
    if U.help then U.help=false;return true end
    local phase=host.state().card_trial.phase
    if U.menu or phase=='death' or phase=='victory'then
        if y>=292 and y<332 then U.keypressed(U.menu and 'return' or 'r')
        elseif y>=340 and y<380 then U.keypressed('r')
        elseif y>=388 and y<428 then U.keypressed('p')
        elseif y>=436 and y<476 then U.keypressed('t')end
        return true
    end
    if phase=='reward'then
        if U.reward_hidden then
            if y>=436 and y<476 and x>=672 then reward_key('return')end
        else
            local row=Pixel.chest_at(x,y)
            if row then reward_key(tostring(row))
            elseif x>=568 and x<=944 and y>=438 and y<=480 then reward_key('return')
            elseif y>=500 then reward_key('escape')end
        end
        return true
    end
    local slot,region=U.card_at(x,y)
    if slot then
        if region=='replace'then commit({kind='replace_card',card_id=host.state().card_trial.cards.hand[slot]})else activate(slot)end
    elseif y>=506 and x>=672 then U.keypressed(x<834 and 'i' or 'escape')
    else
        local cx,cy=host.cell(x,y)
        if cx then U.mousemoved(x,y);if U.aim then commit(U.aim)end end
    end
    return true
end
function U.gamepadpressed(button)
    if not U.setup and not U.active()then return false end
    if host.inspection()then return false end
    if U.setup or U.menu or U.help or host.state().card_trial.phase~='combat'then
        local key=({a='return',b='escape',dpup='up',dpdown='down',dpleft='left',dpright='right',x='r',y='p',back='t',start='escape'})[button]
        if key then U.keypressed(key)end;return true
    end
    if host.busy()then return true end
    if button=='leftshoulder' or button=='rightshoulder'then
        U.focus=(U.focus-1+(button=='rightshoulder' and 1 or -1)+3)%3+1
        inspect_card(host.state().card_trial.cards.hand[U.focus])
    else
        local key=({a='return',b='escape',x='f',y='r',back='i',start='escape',dpup='up',dpdown='down',dpleft='left',dpright='right'})[button]
        if key then U.keypressed(key)end
    end
    return true
end
function U.draw_setup()
    if not U.setup then return false end
    local body,menu,display=host.fonts()
    Pixel.frame('CARD TRIAL',192,80,640,464,display)
    Pixel.center('Adventurer · two fights · nonpersistent',122,menu,Pixel.colors.text)
    Pixel.center('Choose a starting boon preset',150,body,Pixel.colors.muted)
    for i,id in ipairs(C.preset_order)do
        local d=C.presets[id];local y=182+(i-1)*78
        Pixel.option(i..' '..d.name:upper(),232,y,560,U.preset==i,menu)
        Pixel.center(d.description,y+26,body,Pixel.colors.text,244,536)
        for j,b in ipairs(d.boons)do host.visual('core:boon/'..b[1]..'/icon',204+(j-1)*26,y,24,24)end
    end
    Pixel.center('<  SEED '..U.seed..'  >',442,menu,Pixel.colors.selected)
    Pixel.option('BEGIN CARD TRIAL',272,490,480,true,display)
    Pixel.center('1-3 preset · Left/right seed · Enter begin · Esc back',566,body,Pixel.colors.text)
    Pixel.center('Normal run and achievements stay untouched. Trial progress is not saved.',594,body,Pixel.colors.muted)
    return true
end
local function text(line,x,y,font,color,width)
    local g=love.graphics;g.setFont(font);g.setColor(color or Pixel.colors.text)
    if width then g.printf(line,x,y,width)else g.print(line,x,y)end
end
function U.draw_overlay()
    if not U.active() or host.busy() or host.inspection()then return end
    local t=host.state().card_trial
    if not U.menu and not U.help and t.phase~='victory' and t.phase~='death'then return end
    local body,menu,display=host.fonts();Pixel.dim(.65)
    Pixel.frame(U.help and 'CARD TRIAL CONTROLS' or U.menu and 'TRIAL MENU' or
        t.phase=='victory' and 'TRIAL VICTORY' or 'TRIAL DEATH',224,112,576,408,display)
    if U.help then
        for i,line in ipairs({'WASD/arrows: Step now. Click adjacent foe: Strike.',
            '1-3 / click: select that named technique.', 'Hover card: possibilities. Hover board: exact result.',
            'Click target: play. Mend card click: heal self.',
            'Replace button / R: one card, one paid Wait.',
            'Armed: directions aim; Enter plays; Esc cancels.',
            'Knight Hop: arrows cycle landings; Enter jumps.',
            'Tab inspects cards. F aims Strike. I inspects boons.',
            'Mend: 4 HP, 1 charge. Two charges across both fights.',
            'Gamepad: shoulders focus, A select/play, Y replace.',
            'F1 / Esc closes help.'})do Pixel.center(line,162+(i-1)*28,body,Pixel.colors.text)end
        return
    end
    Pixel.center(('Seed %d · %s · Fight %d/2 · HP %d/%d'):format(t.seed,C.presets[t.preset].name,t.fight,
        Game.player(host.state()).hp,Game.player(host.state()).max_hp),176,menu,Pixel.colors.text)
    Pixel.center('Trial progress is not saved. Normal run stays untouched.',220,body,Pixel.colors.muted)
    if U.menu then Pixel.option('ENTER  Resume',272,300,480,true,menu)
    else Pixel.option('ENTER  Replay same preset + seed',272,300,480,true,menu)end
    Pixel.option('R  Replay from fight 1',272,348,480,false,menu)
    Pixel.option('P  Choose another preset',272,396,480,false,menu)
    Pixel.option('T  Return to title',272,444,480,false,menu)
end
