-- Presentation/input adapter. Selections and aiming are disposable; Game owns
-- every card/resource payment. Existing main.lua still draws the board/effects.
local Game=require('src.game')
local Trial=require('src.card_trial')
local Cards=require('src.cards')
local Boons=require('src.boons')
local Catalog=require('src.boon_content')
local Classes=require('src.classes')
local Content=require('src.content')
local Pixel=require('src.pixel_ui')
local C=Trial.content
local U={selected={},preset=1,seed=C.seed,reward_selection=1,focus=1}
local host
local dirs={w={0,-1},up={0,-1},d={1,0},right={1,0},s={0,1},down={0,1},
    a={-1,0},left={-1,0},q={-1,-1},e={1,-1},z={-1,1},c={1,1}}
function U.attach(value)host=value end
function U.active()local s=host and host.state();return s and s.card_trial~=nil end
function U.open(seed)
    U.setup=true;U.seed=seed or U.seed;U.menu=false;U.help=false
    U.selected={};U.action=nil;U.message='Two fights. No save, Continue or achievement progress.'
    if host then host.setup()end
end
local function reset_selection()
    U.selected={};U.action=nil;U.dx=nil;U.dy=nil
    host.target(nil)
end
function U.start()
    host.start(U.seed,C.preset_order[U.preset]);U.setup=false
    reset_selection();U.menu=false;U.reward_hidden=false;U.last_phase=nil;U.last_boon=nil
    U.message='Select cards 1-5, then M / F / H / Space.'
end
function U.ids()
    local result={};local s=host.state()
    for _,id in ipairs(s.card_trial.cards.hand)do if U.selected[id]then result[#result+1]=id end end
    return result
end
function U.intent()
    return {kind='card_action',action=U.action,cards=U.ids(),dx=U.dx,dy=U.dy}
end
local function sync_target()
    local intent=U.intent()
    if (U.action=='move' or U.action=='attack') and U.dx then
        local p=Game.card_preview(host.state(),intent)
        host.target({kind=p.intent and p.intent.kind or 'move',dx=U.dx,dy=U.dy,card_intent=intent})
    else host.target(nil)end
end
local function toggle(slot)
    local id=host.state().card_trial.cards.hand[slot]
    if id then U.selected[id]=not U.selected[id];U.message='';sync_target()end
end
local function choose(action)
    U.action=action;U.dx=nil;U.dy=nil;host.target(nil);U.message=''
    if action=='move' or action=='attack'then U.message='Choose a direction or click a board tile; Enter confirms.'end
end
local function commit()
    if host.busy()then return end
    local intent=U.intent();local p=Game.card_preview(host.state(),intent)
    if not p.valid then U.message=p.reason;return end
    local ok=host.submit(intent)
    if ok then reset_selection();U.message='Unplayed cards retained. Choose your next technique.'end
end
function U.on_events(events)
    local reasons={rhythm_of_steel='primary hit after moving',ring_of_reversal='hit the attacker you escaped',
        emberbrand='direct weapon hit',shared_agony='damage beside another Burning foe',
        pulse_step='entered beside at least two foes',detonation_bloom='credited kill',grave_dividend='credited kill'}
    for _,e in ipairs(events)do
        if e.kind=='BoonActivated'then
            U.last_boon=Catalog.get(e.boon_id).name..': '..(reasons[e.boon_id:match('/([^/]+)$')] or 'triggered')
        elseif e.kind=='RingReversalPrimed'then U.last_boon='Ring of Reversal: escaped a locked attack; next hit on that foe gains damage'
        elseif e.kind=='RingReversalSpent'then U.last_boon='Ring of Reversal: bonus damage on the attacker you escaped'
        end
    end
end
function U.update()
    if not U.active()then return end
    local t=host.state().card_trial
    if U.last_phase~=t.phase then
        U.last_phase=t.phase;U.reward_hidden=false
        if t.phase~='combat'then U.action=nil;host.target(nil)end
    end
    if t.phase=='reward' and not U.reward_hidden and not host.busy()then host.reward_view(U.reward_selection)end
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
            host.hide_reward();reset_selection();U.last_boon=nil
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
        elseif key=='return' or key=='kpenter'then U.start()end
        return true
    end
    if not U.active()then return false end
    if isrepeat then return true end
    if host.inspection()then
        if key=='u'then return true end -- no utility/service route from the inventory
        return false -- existing inspection panel consumes the remaining keys
    end
    if key=='f3' or key=='f5'then return false end
    if key=='f1'then U.help=not U.help;return true end
    if U.help then if key=='escape' or key=='return'then U.help=false end;return true end
    if key=='i' or key=='v'then host.inspect();return true end
    if host.busy()then U.message='Resolving: press again when playback finishes.';return true end
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
    if tonumber(key) and tonumber(key)>=1 and tonumber(key)<=5 then toggle(tonumber(key))
    elseif key=='m' or key=='x'then choose('move')
    elseif key=='f' or key=='r'then choose('attack')
    elseif key=='h'then choose('heal')
    elseif key=='space' or key=='.'then choose('redraw')
    elseif key=='escape'then
        if U.action then U.action=nil;host.target(nil);U.message='Cancelled; cards retained.'else U.menu=true end
    elseif key=='backspace'then reset_selection()
    elseif key=='return' or key=='kpenter'then commit()
    elseif dirs[key] and (U.action=='move' or U.action=='attack')then
        U.dx,U.dy=unpack(dirs[key]);sync_target()
    else U.message='Select cards, then M Move / F Attack / H Heal / Space Redraw.'end
    return true -- includes classic bump/skills, utility, developer B and reset keys
end
function U.mousepressed(x,y,button)
    if not U.setup and not U.active()then return false end
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
    local phase=host.state().card_trial.phase
    if U.menu or phase=='death' or phase=='victory' then
        if y>=292 and y<332 then U.keypressed(U.menu and 'return' or 'r')
        elseif y>=340 and y<380 then U.keypressed('r')
        elseif y>=388 and y<428 then U.keypressed('p')
        elseif y>=436 and y<476 then U.keypressed('t')end
        return true
    end
    if phase=='reward' and not U.reward_hidden then
        local row=Pixel.chest_at(x,y)
        if row then reward_key(tostring(row))
        elseif x>=568 and x<=944 and y>=438 and y<=480 then reward_key('return')
        elseif y>=500 then reward_key('escape')end
        return true
    end
    if x>=672 and x<996 and y>=100 and y<180 then
        local slot=math.floor((x-672)/66)+1
        if (x-672)%66<60 then toggle(slot)end
    elseif x>=672 and x<992 and y>=248 and y<408 then
        choose(({'move','attack','heal','redraw'})[math.floor((y-248)/40)+1])
    elseif x>=672 and x<992 and y>=440 and y<476 then
        if phase=='reward'then reward_key('return')else commit()end
    elseif y>=506 and x>=672 then U.keypressed(x<834 and 'i' or 'escape')
    elseif U.action=='move' or U.action=='attack'then
        local cx,cy=host.cell(x,y)
        if cx then
            local p=Game.player(host.state()).position
            local dx,dy=cx-p.x,cy-p.y
            if dx~=0 or dy~=0 then
                U.dx=dx==0 and 0 or dx>0 and 1 or -1
                U.dy=dy==0 and 0 or dy>0 and 1 or -1;sync_target()
            end
        end
    end
    return true
end
function U.gamepadpressed(button)
    if not U.setup and not U.active()then return false end
    if host.inspection()then return false end
    if U.setup or U.menu or U.help or host.inspection() or host.state().card_trial.phase~='combat'then
        local key=({a='return',b='escape',dpup='up',dpdown='down',dpleft='left',dpright='right',
            x='r',y='p',back='t',start='escape'})[button]
        if key then U.keypressed(key)end;return true
    end
    if host.busy()then return true end
    if button=='leftshoulder' or button=='rightshoulder'then
        U.focus=(U.focus-1+(button=='rightshoulder' and 1 or -1)+5)%5+1
    elseif button=='a'then if U.action then commit()else toggle(U.focus)end
    else
        local key=({b='escape',x='f',y='m',rightstick='h',leftstick='space',start='escape',back='i',
            dpup='w',dpdown='s',dpleft='a',dpright='d'})[button]
        if key then U.keypressed(key)end
    end
    return true
end

-- Suits use simple explicit geometry, plus ASCII S/H/C/D labels. No reliance
-- on a font's optional Unicode playing-card glyphs.
local function suit(s,x,y)
    local g=love.graphics
    if s==2 then
        g.circle('fill',x-3,y-2,4);g.circle('fill',x+3,y-2,4)
        g.polygon('fill',x-7,y-1,x+7,y-1,x,y+8)
    elseif s==4 then g.polygon('fill',x,y-8,x+6,y,x,y+8,x-6,y)
    else
        if s==1 then
            g.polygon('fill',x,y-8,x+7,y+1,x-7,y+1)
            g.circle('fill',x-3,y+1,4);g.circle('fill',x+3,y+1,4)
        else g.circle('fill',x,y-4,4);g.circle('fill',x-4,y+2,4);g.circle('fill',x+4,y+2,4)end
        g.polygon('fill',x,y,x+3,y+8,x-3,y+8)
    end
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
function U.draw_sidebar()
    if not U.active()then return end
    local s=host.state();local t=s.card_trial;local body,menu,display=host.fonts()
    Pixel.frame('CARD TRIAL',656,22,352,514,display)
    text(C.presets[t.preset].name..' · seed '..t.seed,672,52,body)
    text('Fight '..t.fight..'/2 · '..C.fights[t.fight].name,672,72,body)
    local g=love.graphics
    for i,id in ipairs(t.cards.hand)do
        local c=Cards.card(id);local x=672+(i-1)*66
        g.setColor(U.selected[id] and Pixel.colors.selected or Pixel.colors.edge)
        g.rectangle('fill',x,100,60,80)
        g.setColor(.065,.065,.065);g.rectangle('fill',x+2,102,56,76)
        text(c.label,x+6,106,menu,U.selected[id] and Pixel.colors.selected or Pixel.colors.text)
        text(({'S','H','C','D'})[c.suit],x+42,108,body)
        g.setColor(U.selected[id] and Pixel.colors.selected or Pixel.colors.text);suit(c.suit,x+30,143)
        text(tostring(i)..(U.selected[id] and ' *' or ''),x+8,163,body)
    end
    text(('Draw %d · Discard %d · retain unplayed'):format(#t.cards.draw,#t.cards.discard),672,190,body)
    local sel=Cards.selection(t.cards,U.ids())
    local recipe=sel and (sel.recipe or 'invalid for techniques') or 'none'
    text('Selected '..(sel and sel.count or 0)..' · '..recipe:upper(),672,212,menu,Pixel.colors.selected)
    local p=Game.player(s);local class=Classes.for_player(p);local speed=p.speed+Boons.speed_bonus(s,p)
    for i,a in ipairs({'move','attack','heal','redraw'})do
        local recognized=sel and (sel.recipe or a=='redraw')
        local kind=recognized and a~='redraw' and C.equipment[a][sel.recipe] or 'wait'
        local cost=Game.effective_cost(class.costs[kind] or 100,speed)
        local label=({move='M  Move',attack='F  Attack',heal='H  Heal',redraw='Space  Wait & Redraw'})[a]
        if recognized then
            if a=='heal'then label=label..(' %d HP'):format(C.equipment.heal[sel.recipe])
            elseif a=='move'then label=label..(sel.recipe=='pair' and ' / Dash' or ' / Step')
            elseif a=='attack'then label=label..(sel.recipe=='pair' and ' / Sweep' or ' / Sword')end
        end
        local disabled=not recognized or a=='heal' and (t.healing_charges==0 or p.hp==p.max_hp) or t.phase~='combat'
        Pixel.option(label..'  +'..cost,672,254+(i-1)*40,320,U.action==a,menu,disabled)
    end
    text('Flask: '..t.healing_charges..'/2 charges (both fights)',672,414,body)
    local preview=U.action and Game.card_preview(s,U.intent())
    local valid=preview and preview.valid
    Pixel.option(t.phase=='reward' and 'REOPEN REWARD' or valid and 'ENTER / CLICK TO CONFIRM' or 'CHOOSE CARDS / TARGET',
        672,448,320,valid,body,not valid and t.phase~='reward')
    local hint=U.message~='' and U.message or preview and (preview.valid and
        (preview.heal and ('Recover '..preview.heal..' HP; one charge.') or 'Footprint shown on board.') or preview.reason) or
        'One card = Single. Two equal ranks = Pair.'
    text(hint,672,476,body,Pixel.colors.muted,320)
    text('I  Boons / Stats',672,516,body);text('Esc  Trial menu',850,516,body)
end
function U.draw_hud()
    local s=host.state();local p=Game.player(s);local body,menu=host.fonts()
    local g=love.graphics;g.setColor(.025,.025,.04);g.rectangle('fill',16,544,992,80)
    g.setColor(Pixel.colors.edge);g.rectangle('fill',16,544,992,2)
    text(('HP %d/%d     TIME %d     FIGHT %d/2     SEED %d'):format(p.hp,p.max_hp,s.clock,s.card_trial.fight,s.card_trial.seed),28,552,menu)
    local incoming={}
    for _,strike in ipairs(Game.pending_strikes(s))do
        incoming[#incoming+1]=Content.ability_name(strike.ability_id)..' +'..math.max(0,strike.due-s.clock)
    end
    text(host.busy() and 'RESOLVING · card input locked' or #incoming>0 and 'INCOMING: '..table.concat(incoming,' / ') or
        '1-5 cards · M Move · F Attack · H Heal · Space Redraw · direction · Enter',28,578,body)
    text(U.last_boon or 'Boons react to real movement, weapon hits and kills.',28,602,body,Pixel.colors.selected)
end
function U.draw_overlay()
    if not U.active() or host.busy() or host.inspection()then return end
    local t=host.state().card_trial
    if not U.menu and not U.help and t.phase~='victory' and t.phase~='death'then return end
    local body,menu,display=host.fonts();Pixel.dim(.65)
    Pixel.frame(U.help and 'CARD TRIAL CONTROLS' or U.menu and 'TRIAL MENU' or
        t.phase=='victory' and 'TRIAL VICTORY' or 'TRIAL DEATH',224,112,576,408,display)
    if U.help then
        for i,line in ipairs({'1-5 / click: select cards. Unplayed cards stay.',
            'M Move: Single step / Pair dash.', 'F Attack: Single sword / Pair sweep.',
            'H Heal: Single 4 / Pair 8 HP; two charges total.',
            'Space: paid Wait & Redraw, any 1-5 cards.',
            'WASD/arrows aim; Q/E/Z/C attack diagonals.',
            'Click a board tile to aim; Enter/click confirm.',
            'Esc cancels free. I inspects boons. No trial saves.',
            'Gamepad: shoulders focus card; A select/confirm.',
            'X attack, Y move, stick clicks heal/redraw.',
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
return U
