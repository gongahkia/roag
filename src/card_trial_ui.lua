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
            if host.state().card_trial.knight_boots then U.last_boon='Knight Boots: all Dash cards are now Knight Hop' end
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
        clear();U.focus=U.focus%3+1;inspect_card(host.state().card_trial.cards.hand[U.focus]);U.message='Enter selects; R replaces for one Wait.'
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
local function cost_for(def)
    local s=host.state();local p=Game.player(s);local base=Classes.for_player(p).costs[def.action=='knight_hop' and 'dash' or def.action] or 100
    return Game.effective_cost(base,p.speed+Boons.speed_bonus(s,p))
end
function U.draw_sidebar()
    if not U.active()then return end
    local s=host.state();local t=s.card_trial;local body,menu,display=host.fonts();local g=love.graphics
    Pixel.frame('CARD TRIAL',656,22,352,514,display)
    text(C.presets[t.preset].name..' · seed '..t.seed,672,52,body)
    text('Fight '..t.fight..'/2 · '..C.fights[t.fight].name,672,72,body)
    text(t.healing_charges==0 and 'MEND EXHAUSTED · card retired' or 'Mend charges: '..t.healing_charges..'/2 · both fights',672,92,body)
    text('Step: WASD/arrows or adjacent click.\nStrike: click adjacent foe, or F to aim.',672,112,body,Pixel.colors.text,320)
    text('TECHNIQUES · hover to inspect · 1 / 2 / 3',672,148,body,Pixel.colors.muted)
    for slot,id in ipairs(t.cards.hand)do
        local def,c=Cards.card(t.cards,id);local x=672+(slot-1)*108
        local active=U.armed==id;local focused=U.hover==id or slot==U.focus
        g.setColor(active and Pixel.colors.selected or focused and Pixel.colors.text or Pixel.colors.edge)
        g.rectangle('fill',x,168,100,160);g.setColor(.065,.065,.065);g.rectangle('fill',x+2,170,96,156)
        text(slot..' '..def.name,x+4,176,body,active and Pixel.colors.selected or Pixel.colors.text,94)
        host.visual(def.icon,x+34,207,32,32)
        local summary=({dash='Straight\n2 tiles max',knight_hop='L jump\nSkip middle',sweep='3-cell arc\n3 base dmg',mend='4 HP\n1 charge'})[c.kind]
        if c.kind=='mend' and Game.player(s).hp==Game.player(s).max_hp then summary='4 HP\nFull health'end
        text(summary,x+5,246,body,Pixel.colors.text,90)
        g.setColor(U.replacing and U.hover==id and Pixel.colors.text or Pixel.colors.edge)
        g.rectangle('line',x+4,296,92,28)
        text('Replace',x+12,304,body,Pixel.colors.text)
        if active then text('ARMED',x+20,282,body,Pixel.colors.selected)end
    end
    local id=U.hover or U.armed;local def=id and Cards.card(t.cards,id)
    if def then
        text(U.replacing and 'Replace '..def.name or def.name,672,344,menu,Pixel.colors.selected)
        local description=U.replacing and 'Discard this card and draw one. Enemies and scheduled effects advance.' or def.description
        text(description,672,368,body,Pixel.colors.text,320)
        local cost=U.preview and U.preview.cost or cost_for(def)
        text('Time '..cost..(U.replacing and ' · one Wait' or def.heal and ' · 1 healing charge' or ' · enemies advance'),672,418,body)
        local detail
        if U.replacing then detail='Click Replace (or R) to pay and exchange.'
        elseif def.heal then
            detail=U.preview and not U.preview.valid and U.preview.reason or 'Recover '..(U.preview and U.preview.heal or 0)..' HP. Click card to Mend.'
        elseif U.preview and U.aim and not U.preview.valid then detail=U.preview.reason
        elseif U.preview and U.preview.landing then
            local l=U.preview.landing;local terrain=Terrain.at(Content.map_for(s),l.x,l.y)
            detail='Landing '..l.x..','..l.y..': '..(terrain and terrain.name or 'Floor')
            if terrain and terrain.contact_damage then detail=detail..' · contact damage '..terrain.contact_damage end
            detail=detail..'\nClick / Enter plays · Esc cancels'
        elseif U.armed then detail=def.action=='knight_hop' and 'Click a landing. Arrows cycle; Enter jumps.' or 'Click highlighted target. Directions aim; Enter plays.'
        else detail='Select card, then click a highlighted target.'end
        text(detail,672,442,body,Pixel.colors.muted,320)
    else
        text(U.basic_aim and 'Strike ready' or 'Step & Strike ready',672,344,menu,Pixel.colors.selected)
        text(U.message or 'Move now, or hover a technique.',672,370,body,Pixel.colors.text,320)
        if U.preview then
            text(U.preview.valid and ('Time '..U.preview.cost..' · keeps all technique cards') or U.preview.reason,672,418,body,Pixel.colors.muted,320)
        end
        if t.phase=='reward'then Pixel.option('REOPEN REWARD',672,448,320,true,body)end
    end
    text(('Draw %d · Discard %d · Retired %d'):format(#t.cards.draw,#t.cards.discard,#t.cards.retired),672,488,body)
    text('I  Boons / Stats',672,516,body);text('Esc  Trial menu',850,516,body)
end
function U.draw_hud()
    local s=host.state();local p=Game.player(s);local body,menu=host.fonts();local g=love.graphics
    g.setColor(.025,.025,.04);g.rectangle('fill',16,544,992,80)
    g.setColor(Pixel.colors.edge);g.rectangle('fill',16,544,992,2)
    text(('HP %d/%d'):format(p.hp,p.max_hp),28,550,menu)
    text('TIME '..s.clock,206,550,menu);text('FIGHT '..s.card_trial.fight..'/2',398,550,menu);text('SEED '..s.card_trial.seed,590,550,menu)
    g.setColor(Pixel.colors.edge);g.rectangle('line',28,570,154,7)
    g.setColor(Pixel.colors.text);g.rectangle('fill',29,571,152*p.hp/p.max_hp,5)
    local gain=U.preview and U.preview.heal or 0
    if gain>0 then
        local left=29+152*p.hp/p.max_hp;local width=152*gain/p.max_hp
        g.setColor(1,.85,.5);g.rectangle('fill',left,571,width,5)
        text('Mend +'..gain,206,569,body,Pixel.colors.selected)
        text('1 charge -> '..(s.card_trial.healing_charges-1)..' left',398,569,body)
    end
    local incoming={}
    for _,strike in ipairs(Game.pending_strikes(s))do incoming[#incoming+1]=Content.ability_name(strike.ability_id)..' in '..math.max(0,strike.due-s.clock)end
    local line=host.busy() and 'RESOLVING · input locked' or #incoming>0 and 'INCOMING: '..table.concat(incoming,' / ') or 'Step: WASD/arrows · click foe: Strike · cards 1-3 · Tab inspect · R Replace · Esc cancel'
    text(line,28,582,body)
    text(U.last_boon or 'Hollow circles: possible targets · inner outline: exact effect · ghost: landing',28,601,body,Pixel.colors.selected,960)
end
local function marker(c,mode)
    local x,y=host.screen(c.x,c.y);local g=love.graphics
    if mode=='possible'then
        g.setColor(.78,.78,.78,.8);g.circle('line',x+24,y+24,8)
    else
        g.setColor(mode=='invalid' and {1,.35,.28,.9} or {1,.9,.65,.95})
        g.rectangle('line',x+5,y+5,38,38)
    end
end
function U.draw_preview()
    if not U.active()then return false end
    if host.busy() or U.menu or U.help or host.inspection() or host.state().card_trial.phase~='combat'then return true end
    for _,t in ipairs(U.targets or {})do marker(t,'possible')end
    local p=U.preview
    if U.aim and p then
        if p.valid then
            local kind=p.intent.kind
            if p.landing then
                if not p.jump then
                    for i=1,p.distance do
                        local c=p.cells[i];local x,y=host.screen(c.x,c.y);local g=love.graphics
                        g.setColor(.5,.8,.65,.65);g.rectangle('line',x+12,y+12,24,24)
                    end
                end
                marker(p.landing,'exact');host.ghost(p.landing.x,p.landing.y)
            else for _,cell in ipairs(p.cells)do marker(cell,'exact')end end
            for _,cell in ipairs(p.boon_cells or {})do
                local x,y=host.screen(cell.x,cell.y);love.graphics.setColor(.7,.7,.7,.7);love.graphics.circle('line',x+24,y+24,16)
            end
            if kind=='basic_attack' or kind=='sweeping_slash' or p.bump then
                for _,hit in ipairs(U.forecast or {})do
                    local x,y=host.screen(hit.x,hit.y);local body=host.fonts()
                    text('HIT '..hit.loss,x+3,y+32,body,Pixel.colors.selected)
                end
                local body=host.fonts();text('Primary HP loss shown · boon chains / later ticks extra',72,524,body,Pixel.colors.text)
            end
        else
            local c=U.aim.x and {x=U.aim.x,y=U.aim.y}
            if c then marker(c,'invalid')end
        end
    end
    return true
end
local function pattern(cx,cy,kind,scale)
    local g=love.graphics;g.setColor(Pixel.colors.muted)
    for y=-2,2 do for x=-2,2 do g.rectangle('line',cx+x*scale,cy+y*scale,scale-2,scale-2)end end
    g.setColor(Pixel.colors.text);g.rectangle('fill',cx+2,cy+2,scale-6,scale-6)
    local points=kind=='knight_hop' and C.knight_offsets or {{0,-2},{2,0},{0,2},{-2,0}}
    for _,p in ipairs(points)do g.rectangle('fill',cx+p[1]*scale+3,cy+p[2]*scale+3,scale-8,scale-8)end
end
function U.draw_reward()
    if not U.active() or host.state().card_trial.phase~='reward' or U.reward_hidden then return end
    local s=host.state();local offers=C.rewards;local body,menu,display=host.fonts()
    Pixel.dim(.55);Pixel.frame('CARD TRIAL · CHOOSE ONE',80,120,472,360,display);Pixel.frame('DETAILS',568,120,376,360,display)
    for i,o in ipairs(offers)do
        local name=o.upgrade_id and C.upgrade.name or Catalog.get(o.boon_id).name
        local icon=o.upgrade_id and C.upgrade.icon or o.boon_id..'/icon'
        host.visual(icon,104,178+(i-1)*76,32,32)
        Pixel.option(i..'. '..name,146,178+(i-1)*76,376,U.reward_selection==i,menu)
        Pixel.center(o.upgrade_id and 'TECHNIQUE UPGRADE' or 'RARE',202+(i-1)*76,body,Pixel.colors.muted,146,376)
    end
    local o=offers[U.reward_selection]
    if o.upgrade_id then
        host.visual(C.upgrade.icon,724,146,48,48)
        Pixel.center(C.upgrade.name,206,menu,Pixel.colors.selected,584,344)
        text(C.upgrade.description..' Jump in an L; ignores intervening tiles. Free landing required.',592,238,body,Pixel.colors.text,328)
        pattern(660,354,'dash',16);pattern(844,354,'knight_hop',16)
        text('Dash',636,402,body);text('Knight Hop',804,402,body)
    else
        host.visual(o.boon_id..'/icon',724,152,64,64)
        Pixel.center(Catalog.get(o.boon_id).name,230,menu,Pixel.colors.selected,584,344)
        text(Catalog.offer_text(o.boon_id,o.rarity),592,266,menu,Pixel.colors.text,328)
    end
    Pixel.option('Enter claim',584,446,344,true,menu)
    Pixel.center('Free · 1/2/3 or click choose · Enter claim · Esc close',516,body,Pixel.colors.muted)
end
return U
