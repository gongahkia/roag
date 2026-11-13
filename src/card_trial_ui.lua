-- Disposable selection/aiming. The authoritative trial owns budgets and cards.
local Game=require('src.game')
local Trial=require('src.card_trial')
local Cards=require('src.cards')
local Catalog=require('src.boon_content')
local Content=require('src.content')
local Terrain=require('src.terrain')
local Classes=require('src.classes')
local Boons=require('src.boons')
local Pixel=require('src.pixel_ui')
local C=Trial.content
local U={preset=1,seed=C.seed,mode='poker',focus=1,reward_selection=1,deck_choice=1,selected={}}
local host
local dirs={w={0,-1},up={0,-1},d={1,0},right={1,0},s={0,1},down={0,1},a={-1,0},left={-1,0},q={-1,-1},e={1,-1},z={-1,1},c={1,1}}
function U.attach(h)host=h end
function U.active()local s=host and host.state();return s and s.card_trial~=nil end
local function clear()
    U.armed=nil;U.hover_action=nil;U.aim=nil;U.preview=nil;U.forecast=nil;U.targets={};U.target_index=nil
    host.target(nil)
end
function U.open(seed,mode)
    U.setup=true;U.seed=seed or U.seed;U.mode=mode or U.mode;U.menu=false;U.help=false
    U.selected={};clear();host.setup()
end
function U.start()
    host.start(U.seed,C.preset_order[U.preset],U.mode);U.setup=false;clear()
    U.selected={};U.focus=1;U.last_phase='combat';U.last_cycle=host.state().card_trial.cycle;U.last_boon=nil;U.menu=false;U.reward_hidden=false;U.deck_choice=1
    U.message=U.mode=='fixed' and ('Spend stamina on actions. Recover restores '..C.fixed_stamina..'; enemies advance.')
        or 'Cash a hand for stamina. Spend on actions. Recover draws again; enemies advance.'
end
local function intent(action)return {kind='trial_action',action=action}end
function U.ids()
    local out={};local d=host.state().card_trial.cards
    if d then for _,id in ipairs(d.hand)do if U.selected[id]then out[#out+1]=id end end end
    return out
end
function U.cash_preview()
    local ids=U.ids();return Trial.cash_preview(host.state(),#ids>0 and ids or nil)
end
local function free(i)
    if host.busy()then return false end
    local ok,why=host.submit(i);if not ok then U.message=why;return false end
    clear();U.selected={};U.last_phase=host.state().card_trial.phase;U.last_cycle=host.state().card_trial.cycle;return true
end
local function cash()
    local result,why=U.cash_preview()
    if not result then U.message=why;return end
    local ids=U.ids()
    if free({kind='cash_hand',ids=#ids>0 and ids or nil})then U.message='Cashed '..result.name..' for '..result.stamina..' stamina. Act directly on the board.'end
end
local function exchange()
    if free({kind='exchange_cards',ids=U.ids()})then U.message='Exchange used. Gold outlines show the new recommendation.'end
end
local function retain(slot)
    local t=host.state().card_trial;if not t.cards then return end
    local id=t.cards.hand[slot];if not id then return end
    local ids={};for _,v in ipairs(t.cards.hand)do if t.retained[v] and v~=id then ids[#ids+1]=v end end
    if not t.retained[id]then ids[#ids+1]=id end
    -- Retention does not cancel the candidate subset being inspected.
    local selection=U.selected
    if free({kind='retain_cards',ids=ids})then U.selected=selection;U.message='Keep marks apply to unused cards at Recover. Gold-outlined cards are cashed.'end
end
local function set_preview(i)
    U.aim=i;U.preview=Game.card_preview(host.state(),i);U.forecast=Game.card_forecast(host.state(),i)
end
local function commit(i)
    if host.busy()then return false end
    local p=Game.card_preview(host.state(),i)
    if not p.valid then U.message=p.reason;U.preview=p;return false end
    local ok,why=host.submit(i)
    if ok then
        clear();U.last_phase=host.state().card_trial.phase;U.last_cycle=host.state().card_trial.cycle
        if i.action=='recover'then U.selected={};U.focus=1 end
        U.message='Actions spend stamina and advance enemies. G: Recover.'
    else U.message=why end
    return ok
end
local function choose(action)
    clear();U.armed=action;U.hover_action=action
    U.preview=Game.card_preview(host.state(),intent(action))
    if action=='dash' or action=='sweep'then U.targets=Game.card_targets(host.state(),intent(action))end
    U.message='Click a target to act; directions aim, Enter commits, Esc cancels.'
    if action=='mend' or action=='recover'then commit(intent(action))end
end
local function basic_at(x,y)
    local s=host.state();local p=Game.player(s).position;local dx,dy=x-p.x,y-p.y
    if math.max(math.abs(dx),math.abs(dy))~=1 then return end
    for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.hp and e.hp>0 and e.position.x==x and e.position.y==y then
        return {kind='trial_action',action='strike',dx=dx,dy=dy}
    end end
    if dx==0 or dy==0 then return {kind='trial_action',action='move',dx=dx,dy=dy}end
end
local function aim_at(x,y)
    if U.armed=='dash' or U.armed=='sweep'then local i=intent(U.armed);i.x,i.y=x,y;set_preview(i)
    elseif U.armed=='strike' or U.armed=='move'then
        local p=Game.player(host.state()).position;local i=intent(U.armed);i.dx,i.dy=x-p.x,y-p.y;set_preview(i)
    else local i=basic_at(x,y);if i then set_preview(i)else U.aim=nil;U.preview=nil;U.forecast=nil end end
end
local function keyboard_aim(key)
    local s=host.state();local p=Game.player(s).position;local d=dirs[key]
    if U.armed=='dash' and s.card_trial.knight_boots then
        if #U.targets==0 then U.message='No legal L-shaped landing';return end
        local back=key=='left' or key=='up' or key=='a' or key=='w'
        U.target_index=((U.target_index or (back and 1 or 0))-1+(back and -1 or 1)+#U.targets)%#U.targets+1
        local at=U.targets[U.target_index];aim_at(at.x,at.y)
    elseif U.armed=='dash'then
        local r=Game.preview(s,'dash',d[1],d[2]);local at=r.valid and r.cells[r.distance]
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
        U.last_phase=t.phase;U.reward_hidden=false;clear();U.selected={}
        if t.phase~='reward'then host.hide_reward()end
    end
    if U.last_cycle~=t.cycle then U.last_cycle=t.cycle;U.selected={};clear()end
    if t.phase=='reward' and not U.reward_hidden and not busy then host.reward_view(U.reward_selection)end
    if U.was_busy and not busy and U.mouse_x then U.mousemoved(U.mouse_x,U.mouse_y)end
    U.was_busy=busy
end
local function reward_key(key)
    local t=host.state().card_trial
    if t.phase=='deck_reward'then
        if tonumber(key) and tonumber(key)>=1 and tonumber(key)<=3 then U.deck_choice=tonumber(key)
        elseif key=='left' or key=='up'then U.deck_choice=(U.deck_choice+1)%3+1
        elseif key=='right' or key=='down'then U.deck_choice=U.deck_choice%3+1
        elseif key=='return' or key=='kpenter'then
            if free({kind='trial_deck_reward',choice=U.deck_choice==3 and 0 or U.deck_choice})then
                U.message='Fight 2: fresh opening hand. HP, Mend charges, boons and time preserved.'
            end
        elseif key=='escape'then U.menu=true end
        return
    end
    if U.reward_hidden then
        if key=='return'then U.reward_hidden=false;host.reward_view(U.reward_selection)
        elseif key=='escape'then U.menu=true end
    elseif key=='escape'then U.reward_hidden=true;host.hide_reward()
    elseif tonumber(key) and tonumber(key)>=1 and tonumber(key)<=3 then U.reward_selection=tonumber(key);host.reward_view(U.reward_selection)
    elseif key=='left' or key=='up'then U.reward_selection=(U.reward_selection+1)%3+1;host.reward_view(U.reward_selection)
    elseif key=='right' or key=='down'then U.reward_selection=U.reward_selection%3+1;host.reward_view(U.reward_selection)
    elseif key=='return' or key=='kpenter'then
        if host.submit({kind='trial_reward',offer_index=U.reward_selection})then
            host.hide_reward();clear();U.selected={};U.last_boon=t.knight_boots and 'Knight Boots: Dash is now Knight Hop' or nil
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
        elseif key=='tab'then U.mode=U.mode=='poker' and 'fixed' or 'poker'
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
        elseif key=='p'then U.open()
        elseif key=='t'then U.menu=false;host.exit()end;return true
    end
    local t=host.state().card_trial
    if t.phase=='death' or t.phase=='victory'then
        if key=='return' or key=='r'then U.start()
        elseif key=='p'then U.open()
        elseif key=='t' or key=='escape'then host.exit()end;return true
    end
    if t.phase=='reward' or t.phase=='deck_reward'then reward_key(key);return true end
    if key=='g'then choose('recover')
    elseif key=='y'then exchange()
    elseif key=='t'then retain(U.focus)
    elseif key=='tab'then if t.cards and #t.cards.hand>0 then U.focus=U.focus%#t.cards.hand+1 end
    elseif key=='backspace'then U.selected={};clear()
    elseif tonumber(key) and tonumber(key)>=1 and tonumber(key)<=8 and t.cards then
        local n=tonumber(key);local id=t.cards.hand[n]
        if id then U.focus=n;if t.resource_phase=='prepare'then U.selected[id]=not U.selected[id]end end
    elseif key=='f'then choose('strike')
    elseif key=='x'then choose('dash')
    elseif key=='r'then choose('sweep')
    elseif key=='h'then choose('mend')
    elseif key=='escape'then
        if U.armed or U.aim or next(U.selected)then clear();U.selected={}else U.menu=true end
    elseif key=='return' or key=='kpenter'then
        if U.aim then commit(U.aim)elseif t.resource_phase=='prepare'then cash()end
    elseif dirs[key]then
        if U.armed then keyboard_aim(key)
        elseif dirs[key][1]==0 or dirs[key][2]==0 then commit({kind='trial_action',action='move',dx=dirs[key][1],dy=dirs[key][2]})
        else local p=Game.player(host.state()).position;local i=basic_at(p.x+dirs[key][1],p.y+dirs[key][2]);if i then commit(i)end end
    end
    return true -- classic B, utilities, reset and every other combat route stay intercepted
end
function U.card_at(x,y)
    if x<672 or x>=992 then return end
    local col=math.floor((x-672)/82);if col>3 or (x-672)%82>=74 then return end
    local row=y>=210 and 1 or 0;local top=136+74*row
    if y<top or y>=top+64 then return end
    return row*4+col+1,y>=top+42 and 'keep' or 'face'
end
local order={'move','strike','dash','sweep','mend','recover'}
function U.action_at(x,y)
    if x<672 or x>=992 or y<370 or y>=466 then return end
    local col=x>=836 and 1 or 0;local left=672+col*164
    local row=math.floor((y-370)/32)
    if x>=left+156 or (y-370)%32>=28 then return end
    return order[row*2+col+1]
end
function U.mousemoved(x,y)
    U.mouse_x,U.mouse_y=x,y
    if not U.active() or U.setup or host.busy() or U.menu or U.help or host.inspection() or host.state().card_trial.phase~='combat'then return false end
    local slot=U.card_at(x,y);local a=U.action_at(x,y)
    U.hover_action=nil
    if a then
        U.hover_action=a;U.aim=nil;U.preview=Game.card_preview(host.state(),intent(a));U.forecast=nil;U.targets={}
        if a=='dash' or a=='sweep'then U.targets=Game.card_targets(host.state(),intent(a))end
    elseif slot then U.focus=slot;U.aim=nil;U.preview=nil;U.forecast=nil;U.targets={}
    elseif x>=672 and x<992 and y>=282 and y<310 and host.state().card_trial.resource_phase=='spend'then
        U.hover_action='recover';U.preview=Game.card_preview(host.state(),intent('recover'));U.aim=nil;U.targets={}
    else
        U.targets=(U.armed=='dash' or U.armed=='sweep') and Game.card_targets(host.state(),intent(U.armed)) or {}
        local cx,cy=host.cell(x,y);if cx then aim_at(cx,cy)else U.aim=nil;U.preview=nil;U.forecast=nil end
    end
    return true
end
function U.mousepressed(x,y,button)
    if not U.setup and not U.active()then return false end
    if button==2 then U.keypressed('escape');return true end
    if button~=1 then return true end
    if host.inspection()then if x>=88 and x<576 and y>=490 and y<514 then return true end;return false end
    if U.setup then
        local row=Pixel.row_at(232,182,560,56,3,78,x,y)
        if row then U.preset=row
        elseif y>=408 and y<434 then U.keypressed('tab')
        elseif y>=434 and y<470 then U.keypressed(x<512 and 'left' or 'right')
        elseif y>=482 and y<526 then U.start()
        elseif y>=556 then U.keypressed('escape')end;return true
    end
    if host.busy()then return true end
    if U.help then U.help=false;return true end
    local t=host.state().card_trial
    if U.menu or t.phase=='death' or t.phase=='victory'then
        if y>=292 and y<332 then U.keypressed(U.menu and 'return' or 'r')
        elseif y>=340 and y<380 then U.keypressed('r')
        elseif y>=388 and y<428 then U.keypressed('p')
        elseif y>=436 and y<476 then U.keypressed('t')end;return true
    end
    if t.phase=='reward' or t.phase=='deck_reward'then
        if t.phase=='reward' and U.reward_hidden then if y>=440 and y<476 and x>=672 then reward_key('return')end;return true end
        local row=Pixel.chest_at(x,y)
        if row then reward_key(tostring(row))elseif x>=568 and x<=944 and y>=438 and y<=480 then reward_key('return')elseif y>=500 then reward_key('escape')end
        return true
    end
    local slot,region=U.card_at(x,y);local a=U.action_at(x,y)
    if slot and t.cards and t.cards.hand[slot]then
        U.focus=slot
        if region=='keep'then retain(slot)
        elseif t.resource_phase=='prepare'then local id=t.cards.hand[slot];U.selected[id]=not U.selected[id]end
    elseif x>=672 and x<992 and y>=282 and y<310 then if t.resource_phase=='prepare'then cash()else choose('recover')end
    elseif x>=672 and x<828 and y>=316 and y<344 then exchange()
    elseif x>=836 and x<992 and y>=316 and y<344 then U.selected={};clear()
    elseif a then choose(a)
    elseif x>=672 and y>=506 then U.keypressed(x<834 and 'i' or 'escape')
    else local cx,cy=host.cell(x,y);if cx then U.mousemoved(x,y);if U.aim then commit(U.aim)end end end
    return true
end
function U.gamepadpressed(b)
    if not U.setup and not U.active()then return false end
    if host.inspection()then return false end
    if host.busy()then return true end
    if b=='leftshoulder' or b=='rightshoulder'then U.keypressed('tab');return true end
    local key=({a='return',b='escape',x='f',y='r',leftstick='x',rightstick='h',back='g',start='escape',dpup='up',dpdown='down',dpleft='left',dpright='right'})[b]
    if key then U.keypressed(key)end;return true
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
    Pixel.center('Tab: '..(U.mode=='fixed' and 'FIXED STAMINA (comparison)' or 'POKER STAMINA'),416,menu,Pixel.colors.selected)
    Pixel.center('<  SEED '..U.seed..'  >',442,menu,Pixel.colors.selected)
    Pixel.option('BEGIN CARD TRIAL',272,490,480,true,display)
    Pixel.center('1-3 preset · Tab comparison · Left/right seed · Enter begin · Esc back',566,body,Pixel.colors.text)
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
    if t.phase=='deck_reward' and not U.menu and not U.help then U.draw_deck_reward();return end
    if not U.menu and not U.help and t.phase~='victory' and t.phase~='death'then return end
    local body,menu,display=host.fonts();Pixel.dim(.65)
    Pixel.frame(U.help and 'CARD TRIAL CONTROLS' or U.menu and 'TRIAL MENU' or
        t.phase=='victory' and 'TRIAL VICTORY' or 'TRIAL DEATH',224,112,576,408,display)
    if U.help then
        local lines=t.mode=='fixed' and {
            'Spend a fixed budget on direct actions.',
            'Each fight starts with '..C.fixed_stamina..' stamina; there are no cards.',
            'G Recover: lose leftover stamina and Wait once.',
            'Enemies and scheduled effects advance during Recover.',
            'If alive in combat, Recover restores '..C.fixed_stamina..' stamina.',
        } or {'Cash a hand for stamina. Spend on direct actions.',
            'Enter cashes gold-outlined contributors; 1-8 selects a pool.',
            'Y exchanges 1-3 selected cards, once before cashing.',
            'Keep buttons / T: retain up to two unused cards.',
            'G Recover: lose stamina, one Wait, then draw again.'}
        for _,line in ipairs({
            'WASD/arrows Step; click adjacent tile/foe to act.',
            'F Strike, X Dash, R Sweep; direction/target then play.',
            'H Mend: 4 HP, 2 stamina and 1 charge; two charges total.',
            'Target click commits. Keyboard: directions then Enter.',
            t.cards and 'Tab focuses cards; Backspace auto hand; Esc cancels.' or 'Esc cancels; I inspects boons.',
            'Trial progress is not saved.'})do lines[#lines+1]=line end
        for i,line in ipairs(lines)do Pixel.center(line,162+(i-1)*28,body,Pixel.colors.text)end
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
local function action_cost(a)
    local s=host.state();local p=Game.player(s);local def=Trial.action(s.card_trial,a)
    return Game.effective_cost(Classes.for_player(p).costs[def.kind=='knight_hop' and 'dash' or def.kind] or 100,p.speed+Boons.speed_bonus(s,p))
end
local function suit(n,x,y)
    local g=love.graphics
    if n==2 then g.circle('fill',x-3,y-2,4);g.circle('fill',x+3,y-2,4);g.polygon('fill',x-7,y-1,x+7,y-1,x,y+8)
    elseif n==4 then g.polygon('fill',x,y-8,x+6,y,x,y+8,x-6,y)
    else
        if n==1 then g.polygon('fill',x,y-8,x+7,y+1,x-7,y+1);g.circle('fill',x-3,y+1,4);g.circle('fill',x+3,y+1,4)
        else g.circle('fill',x,y-4,4);g.circle('fill',x-4,y+2,4);g.circle('fill',x+4,y+2,4)end
        g.polygon('fill',x,y,x+3,y+8,x-3,y+8)
    end
end
function U.draw_sidebar()
    if not U.active()then return end
    local s=host.state();local t=s.card_trial;local body,menu,display=host.fonts();local g=love.graphics
    Pixel.frame(t.mode=='fixed' and 'FIXED STAMINA' or 'POKER STAMINA',656,22,352,514,display)
    text(C.presets[t.preset].name..' · seed '..t.seed..(t.cards and ' · deck '..#t.cards.instances or ''),672,52,body)
    text('Fight '..t.fight..'/2 · '..C.fights[t.fight].name,672,72,body)
    text('STAMINA '..t.stamina,672,92,menu,Pixel.colors.selected)
    text(t.phase~='combat' and 'ENCOUNTER ENDED' or t.resource_phase:upper(),850,96,body)
    local kept=0;for _ in pairs(t.retained)do kept=kept+1 end
    text('Mend '..t.healing_charges..'/'..C.healing_charges..(t.cards and (' · Keep '..kept..'/'..C.retain_limit..' · unused '..#t.cards.hand) or ' · no cards'),672,116,body)
    local cash_result=t.cards and U.cash_preview();local contributing={}
    for _,id in ipairs(cash_result and cash_result.cards or {})do contributing[id]=true end
    if t.cards then
        for slot,id in ipairs(t.cards.hand)do
            local c=Cards.card(t.cards,id);local x=672+((slot-1)%4)*82;local y=136+math.floor((slot-1)/4)*74
            g.setColor(contributing[id] and {1,.9,.65} or U.selected[id] and Pixel.colors.text or Pixel.colors.edge)
            g.rectangle('fill',x,y,74,64);g.setColor(.065,.065,.065);g.rectangle('fill',x+2,y+2,70,60)
            text(c.label,x+6,y+5,menu,Pixel.colors.text);text(({'S','H','C','D'})[c.suit],x+42,y+6,body)
            g.setColor(Pixel.colors.text);suit(c.suit,x+58,y+21)
            text(tostring(slot)..(U.selected[id] and '*' or ''),x+5,y+26,body)
            if contributing[id]then text('CASH',x+26,y+27,body,{1,.9,.65})end
            g.setColor(t.retained[id] and Pixel.colors.text or Pixel.colors.edge);g.rectangle('line',x+3,y+42,68,19)
            text(t.retained[id] and 'KEEP *' or 'Keep',x+13,y+46,body)
            if slot==U.focus then g.setColor(Pixel.colors.muted);g.rectangle('line',x+5,y+3,64,36)end
        end
    else
        text(C.fixed_stamina..' stamina to start each fight.\nRecover restores '..C.fixed_stamina..' after one Wait.\n\nSame combat, costs and enemy timing.\nNo hand building or deck rewards.',672,150,body,Pixel.colors.text,320)
    end
    local prepare=t.phase=='combat' and t.resource_phase=='prepare'
    local cash_label=prepare and cash_result and ('Enter  Cash '..cash_result.name..' -> '..cash_result.stamina..' stamina') or 'G  Recover · lose leftover stamina'
    Pixel.option(cash_label,672,288,320,prepare or t.stamina==0,body,t.phase~='combat')
    if t.cards then
        Pixel.option(t.exchange_used and 'Exchange used' or 'Y Exchange (1 left)',672,324,156,false,body,not prepare or t.exchange_used)
        Pixel.option('Backspace: Auto',836,324,156,false,body)
    end
    text(prepare and '1-8 subset · Y swaps 1-3 · T Keep' or t.mode=='fixed' and ('G Recover: Wait, then '..C.fixed_stamina..' stamina') or 'G Recover: enemies advance; keeps marks',672,350,body,Pixel.colors.muted)
    local bindings={move='Step',strike='F Strike',dash='X '..Trial.action(t,'dash').name,sweep='R Sweep',mend='H Mend',recover='G Recover'}
    for i,a in ipairs(order)do
        local x=672+((i-1)%2)*164;local y=376+math.floor((i-1)/2)*32
        local disabled=t.phase~='combat' or a~='recover' and (t.resource_phase~='spend' or t.stamina<C.stamina_costs[a]) or a=='mend' and (t.healing_charges==0 or Game.player(s).hp==Game.player(s).max_hp)
        Pixel.option(bindings[a]..' ['..C.stamina_costs[a]..']',x,y,156,U.armed==a,body,disabled)
    end
    local hint=U.preview and not U.preview.valid and U.preview.reason or U.message
    local focused_action=U.hover_action or U.armed
    if focused_action and focused_action~='recover' and focused_action~='mend' and t.phase=='combat'then
        if t.resource_phase=='prepare'then hint='Cash a hand for stamina before acting.'
        elseif t.stamina<C.stamina_costs[focused_action]then hint='Need '..C.stamina_costs[focused_action]..' stamina. G: Recover.'end
    end
    text(hint or '',672,476,body,Pixel.colors.muted,320)
    text('I  Boons / Stats',672,516,body);text('Esc  Trial menu',850,516,body)
end
function U.draw_hud()
    local s=host.state();local p=Game.player(s);local t=s.card_trial;local body,menu=host.fonts();local g=love.graphics
    g.setColor(.025,.025,.04);g.rectangle('fill',16,544,992,86);g.setColor(Pixel.colors.edge);g.rectangle('fill',16,544,992,2)
    text(('HP %d/%d'):format(p.hp,p.max_hp),28,550,menu);text('TIME '..s.clock,206,550,menu)
    text('STAMINA '..t.stamina,398,550,menu);text('SEED '..t.seed,640,550,menu)
    g.setColor(Pixel.colors.edge);g.rectangle('line',28,570,154,7);g.setColor(Pixel.colors.text);g.rectangle('fill',29,571,152*p.hp/p.max_hp,5)
    local gain=U.preview and U.preview.heal or 0
    if gain>0 then g.setColor(1,.85,.5);g.rectangle('fill',29+152*p.hp/p.max_hp,571,152*gain/p.max_hp,5)end
    local a=U.hover_action or U.armed or U.aim and U.aim.action
    local def=a and Trial.action(t,a);local preview=U.preview
    if host.busy()then text('RESOLVING · input locked',206,570,body)
    elseif def then
        local detail=def.name..': '..def.description
        if a=='recover' and t.mode=='fixed'then detail='Recover: lose leftover stamina. Wait once, then restore '..C.fixed_stamina..' if alive.'end
        if gain>0 then detail='Mend: recover '..gain..' HP ('..p.hp..' -> '..(p.hp+gain)..'), 1 charge'end
        text(detail,206,570,body,Pixel.colors.text,786)
    else text(t.mode=='fixed' and ('Fixed budget: Recover restores '..C.fixed_stamina..' after one Wait.') or 'Cash a hand once; several actions share its stamina budget.',206,570,body)end
    local incoming={};for _,v in ipairs(Game.pending_strikes(s))do incoming[#incoming+1]=Content.ability_name(v.ability_id)..' in '..math.max(0,v.due-s.clock)end
    local line
    if a=='recover'then
        local n=0;for _ in pairs(t.retained)do n=n+1 end
        line='Recover: lose '..t.stamina..' stamina; keep '..n..' marked cards; Time '..action_cost('recover')..' (enemies/scheduled effects advance).'
        if t.mode=='fixed'then line='Lose '..t.stamina..' stamina; Time '..action_cost('recover')..' (enemies/effects advance); restore '..C.fixed_stamina..' if combat remains active.'end
    elseif def then
        line='Cost '..C.stamina_costs[a]..' stamina'..(preview and preview.valid and (' ('..t.stamina..' -> '..preview.stamina_after..')') or '')..'; Time '..(preview and preview.cost or action_cost(a))..' (enemies advance).'
        if preview and preview.landing then local l=preview.landing;local terrain=Terrain.at(Content.map_for(s),l.x,l.y);line=line..' Landing '..l.x..','..l.y..': '..(terrain and terrain.name or 'floor')..'.'end
    elseif #incoming>0 then line='INCOMING: '..table.concat(incoming,' / ')
    elseif t.cards and t.resource_phase=='prepare'then
        local result=U.cash_preview();local selected=U.ids()
        line=(#selected>0 and 'Selected pool '..#selected or 'Auto: all unused cards')..'; cash '..(result and #result.cards or 0)..' contributors. Other cards stay. Cash clears Keep on contributors.'
    else line='WASD Step · click foe Strike · F aim · X Dash · R Sweep · H Mend · G Recover'..(t.cards and ' · T Keep' or '')..' · Esc cancel'end
    text(line,28,586,body,Pixel.colors.text,960)
    text(U.last_boon or 'Hollow circles: possible targets · inner outline: footprint · ghost: landing. Legal does not mean safe.',28,604,body,Pixel.colors.selected,960)
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
        if p.target_valid or p.valid then
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
                    love.graphics.setColor(.025,.025,.025,.9);love.graphics.rectangle('fill',x+2,y+31,44,15)
                    text('HIT '..hit.loss,x+3,y+32,body,Pixel.colors.selected)
                end
                love.graphics.setColor(.025,.025,.025,.9);love.graphics.rectangle('fill',68,520,518,20)
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
    love.graphics.setColor(Pixel.colors.panel);love.graphics.rectangle('fill',16,502,992,34)
    Pixel.center('Free · 1/2/3 or click choose · Enter claim · Esc close',516,body,Pixel.colors.muted)
end
function U.draw_deck_reward()
    local t=host.state().card_trial;local body,menu,display=host.fonts();local d=t.cards
    Pixel.dim(.55);Pixel.frame('INTERMISSION · DECK EDIT',80,120,472,360,display);Pixel.frame('FRESH OPENING HAND',568,120,376,360,display)
    for i,face in ipairs(t.deck_offers)do
        local c=Cards.face(face);local y=182+(i-1)*92
        Pixel.option(i..'. Add '..C.deck_add_count..' copies',100,y,432,U.deck_choice==i,menu)
        Pixel.center(c.name,y+26,menu,Pixel.colors.selected,100,432)
    end
    Pixel.option('3. Skip deck edit',100,378,432,U.deck_choice==3,menu)
    local face=t.deck_offers[U.deck_choice];local extra=face and C.deck_add_count or 0
    text('Deck '..#d.instances..' -> '..(#d.instances+extra)..' cards',592,176,menu)
    if face then
        local c=Cards.face(face);local ranks,suits=0,0
        for _,v in ipairs(d.instances)do local f=Cards.face(v.face);if f.rank==c.rank then ranks=ranks+1 end;if f.suit==c.suit then suits=suits+1 end end
        text(c.name..': '..Cards.face_count(d,face)..' -> '..(Cards.face_count(d,face)+extra)..' copies\nRank '..c.label..': '..ranks..' -> '..(ranks+extra)..'\n'..c.suit_name..': '..suits..' -> '..(suits+extra),592,210,body,Pixel.colors.text,328)
    end
    text('After this choice, consolidate and shuffle the deck; deal a fresh eight. Stamina and Keep marks reset. One exchange is available.\n\nThis opening reset costs no time. HP, Mend charges, boons and ongoing effects carry.',592,280,body,Pixel.colors.text,328)
    Pixel.option('Enter: begin fight 2',584,446,344,true,menu)
    love.graphics.setColor(Pixel.colors.panel);love.graphics.rectangle('fill',16,502,992,34)
    Pixel.center('1/2 add three copies · 3 Skip · Enter confirm · no repeated deals',516,body,Pixel.colors.muted)
end
return U
