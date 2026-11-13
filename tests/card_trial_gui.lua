-- Actual LÖVE rendering + production keyboard/mouse/gamepad callbacks. The
-- policy observes snapshots and proposes legal inputs; it never injects state.
local G=require('src.game')
local T=require('src.card_trial')
local Codec=require('src.persistence')
local Policy=require('tests.card_trial_policy')
local original_submit=G.submit
local out=assert(os.getenv('ROAG_CARD_GUI_OUTPUT'))
local actions,flags,results,procs={}, {}, {}, {}
local classic_state
local ordinary_restore=G.restore
function G.restore(...)
    local s=ordinary_restore(...);classic_state=s;return s
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,case,step,frames='title',1,0,0
local pending_capture,delay,started=nil,0,0
local presets={'movement','burning','bare'}
local function same(a,b)assert(Codec.encode(a)==Codec.encode(b),'GUI and authoritative action differ')end
local function capture(name)pending_capture=name end
local function snapshot()return assert(love.roagCardTrialState(),'trial is not active')end
local function key(k)love.keypressed(k)end
local function prefix(s)
    local h=s.card_trial.cards.hand
    local function i(a,slots,dx,dy)local ids={};for _,slot in ipairs(slots)do ids[#ids+1]=h[slot]end
        return {kind='card_action',action=a,cards=ids,dx=dx,dy=dy}end
    if case==1 then
        if step==0 then return i('move',{1},-1,0)
        elseif step==1 then return i('move',{3},0,-1)
        elseif step==2 then return i('redraw',{4})
        elseif step==3 then return i('move',{5},0,1)
        elseif step==4 then return i('attack',{3},-1,-1)end
    elseif case==2 then
        if step==0 then return i('move',{1},-1,0)
        elseif step==1 then return i('move',{3},0,-1)
        elseif step==2 then return i('attack',{1,2},-1,-1)end
    end
end
local direction_keys={['0,-1']='w',['1,-1']='e',['1,0']='d',['1,1']='c',
    ['0,1']='s',['-1,1']='z',['-1,0']='a',['-1,-1']='q'}
local function perform(intent,mouse)
    local before=snapshot();local expected=G.snapshot(before)
    local ok,why,events=original_submit(expected,intent);assert(ok,why)
    key('backspace')
    for _,id in ipairs(intent.cards)do for slot,held in ipairs(before.card_trial.cards.hand)do
        if held==id then
            if mouse then love.mousepressed(690+(slot-1)*66,122,1)else key(tostring(slot))end
        end
    end end
    local name=intent.action..(#intent.cards==2 and '_pair' or '_single')
    if mouse then
        local index=({move=1,attack=2,heal=3,redraw=4})[intent.action]
        love.mousepressed(820,268+(index-1)*40,1)
    else key(({move='m',attack='f',heal='h',redraw='space'})[intent.action])end
    if intent.dx then
        if mouse then
            local p=G.player(before).position
            love.mousepressed(64+(p.x+intent.dx-1)*48+24,16+(p.y+intent.dy-1)*48+24,1)
        else key(direction_keys[intent.dx..','..intent.dy])end
    end
    same(before,snapshot()) -- selecting/aiming is pure
    if not flags[name]then capture(name..'-preview')end
    if mouse then love.mousepressed(820,450,1)else key('return')end
    same(expected,snapshot())
    -- Repeat key, click, classic shortcut and controller input during playback.
    if love.roagPlaybackPending()then
        key('return');love.keypressed('return',nil,true)
        key('b');key('w');key('x');key('u');love.gamepadpressed(nil,'a')
        love.mousepressed(820,450,1);same(expected,snapshot());flags.input_lock=true
    end
    flags[name]=true
    local kept=0
    for _,id in ipairs(before.card_trial.cards.hand)do
        local used=false;for _,v in ipairs(intent.cards)do if v==id then used=true end end
        if not used then kept=kept+1 end
    end
    if kept>0 then flags.retention=true end
    for _,e in ipairs(events)do
        if e.kind=='BoonActivated'then procs[e.boon_id]=(procs[e.boon_id]or 0)+1 end
        if e.kind=='RingReversalPrimed' or e.kind=='RingReversalSpent'then flags[e.kind]=true end
        if e.kind=='BoonActivated' and (e.boon_id=='core:boon/shared_agony' or e.boon_id=='core:boon/rhythm_of_steel')then
            capture(presets[case]..'-synergy')
        end
    end
    actions[#actions+1]={preset=presets[case],fight=before.card_trial.fight,intent=intent,
        hp_before=G.player(before).hp,hp_after=G.player(expected).hp,time=expected.clock,events=events}
    delay=2
end
function love.load(args)
    started=love.timer.getTime();load(args)
    local body=love.graphics.getFont();assert(body:hasGlyphs('ASHCD234567890JQK'),'ASCII card labels unavailable')
end
local function advance()
    if phase=='title'then
        assert(love.roagUIState().page=='home');capture('title-entry');key('t')
        key('right') -- default 101 -> 202; held opening pair is earned by the first step
        key('2');key('return');phase='fight';delay=3
        local s=snapshot();assert(s.card_trial.seed==202 and s.card_trial.preset=='movement')
        local before=G.snapshot(s)
        for _,k in ipairs({'w','a','s','d','f','r','x','space','b','n','m','u','e','return','escape','backspace'})do key(k)end
        for _,b in ipairs({'x','y','rightshoulder','leftshoulder','a','rightstick','leftstick','b'})do love.gamepadpressed(nil,b)end
        key('escape');key('backspace');same(before,snapshot());flags.bypass_guard=true
        return
    end
    if phase=='fight'then
        local s=snapshot();local t=s.card_trial
        if t.phase=='reward'then
            capture(presets[case]..'-reward');phase='reward';delay=4;return
        elseif t.phase=='victory' or t.phase=='death'then
            assert(t.phase=='victory',presets[case]..' policy died')
            capture(presets[case]..'-victory')
            results[#results+1]={preset=presets[case],seed=t.seed,actions=t.commits,hp=G.player(s).hp,
                charges=t.healing_charges,time=s.clock};phase='result';delay=4;return
        end
        assert(step<100,'trial policy exceeded action budget')
        local intent=prefix(s)or Policy.choose(s,flags)
        perform(intent,step%3==1);step=step+1
        if t.fight==2 and not flags['later_'..presets[case]]then
            flags['later_'..presets[case]]=true;capture(presets[case]..'-fight-2')
        end
    elseif phase=='reward'then
        local before=snapshot()
        key('escape');key('w');key('h');key('space');key('return') -- reopen only
        same(before,snapshot());flags.reward_lock=true
        key('2');key('return')
        local after=snapshot();assert(after.card_trial.fight==2)
        same(before.card_trial.cards,after.card_trial.cards)
        assert(before.card_trial.healing_charges==after.card_trial.healing_charges)
        assert(G.player(before).hp==G.player(after).hp and before.clock==after.clock)
        phase='fight';delay=3
    elseif phase=='result'then
        if case<3 then
            case=case+1;step=0;key('p');key(tostring(({movement=2,burning=3,bare=1})[presets[case]]));key('return')
            phase='fight';delay=3
        else
            key('return');same(T.new(G,202,'bare'),snapshot());flags.replay=true
            key('p');key('left');key('left') -- 202 -> 101 -> 303
            -- Select seed 2 through the ordinary launch argument in a separate
            -- exercise below? Instead use the exposed seed carousel's 303 hand.
            key('1');key('return');phase='healing';step=0;delay=2
        end
    elseif phase=='healing'then
        local s=snapshot();local p=G.player(s)
        if s.game_over then
            -- A deliberate stationary redraw exercise also verifies death.
            assert(s.card_trial.phase=='death');capture('death');flags.death=true
            key('return');same(T.new(G,s.card_trial.seed,'bare'),snapshot())
            key('p');key('escape');phase='classic';delay=3;return
        end
        local h=s.card_trial.cards.hand;local pair
        for i=1,4 do for j=i+1,5 do if require('src.cards').card(h[i]).rank==require('src.cards').card(h[j]).rank then pair={h[i],h[j]}end end end
        local intent
        if p.hp<=16 and pair and not flags.heal_pair then
            intent={kind='card_action',action='heal',cards=pair}
        elseif p.hp<=20 and s.card_trial.healing_charges>0 and flags.heal_pair and not flags.heal_single_exercise then
            intent={kind='card_action',action='heal',cards={h[1]}};flags.heal_single_exercise=true
        else
            local ids={};for _,id in ipairs(h)do if not pair or id~=pair[1]and id~=pair[2]then ids[#ids+1]=id end end
            intent={kind='card_action',action='redraw',cards=ids}
        end
        perform(intent,step%2==0);step=step+1
        if snapshot().card_trial.healing_charges==0 then flags.charges_exhausted=true end
        assert(step<100,'death exercise exceeded budget')
    elseif phase=='classic'then
        assert(love.roagUIState().page=='home');key('c');phase='classic-action';delay=3
    elseif phase=='classic-action'then
        key('d');phase='classic-wait';delay=3
    elseif phase=='classic-wait'then
        key('space');phase='classic-attack';delay=3
    elseif phase=='classic-attack'then
        key('f');key('d');key('return');phase='finish';delay=4
    elseif phase=='finish'then
        assert(classic_state and classic_state.clock==300,'classic callbacks failed')
        capture('classic-controls');flags.classic=true
        assert(flags.move_single and flags.attack_single and flags.move_pair and flags.attack_pair)
        assert(flags.heal_single and flags.heal_pair and flags.charges_exhausted and flags.redraw_single)
        assert(flags.RingReversalPrimed and flags.RingReversalSpent and procs['core:boon/shared_agony'])
        local f=assert(io.open(out..'/gui-results.txt','wb'))
        for _,r in ipairs(results)do f:write(('%s seed %d: VICTORY, %d actions, HP %d, charges %d, time %d\n'):format(r.preset,r.seed,r.actions,r.hp,r.charges,r.time))end
        local names={};for k in pairs(flags)do names[#names+1]=k end;table.sort(names)
        f:write('Verified: '..table.concat(names,', ')..'\n');f:close()
        local f=assert(io.open(out..'/gui-actions.roag','wb'));f:write(Codec.encode(actions));f:close()
        print('PASS real Card Trial GUI: three victories, curated synergies, healing, death/replay, classic controls')
        phase='quit';delay=4
    elseif phase=='quit'then love.event.quit()end
end
function love.update(dt)
    local ok,why=pcall(function()
        update(dt);frames=frames+1
        assert(love.timer.getTime()-started<240,'GUI timeout')
        if love.roagPlaybackPending()then return end
        if delay>0 then delay=delay-1;return end
        advance()
    end)
    if not ok then io.stderr:write('FAIL Card Trial GUI: '..tostring(why)..'\n');love.event.quit(1)end
end
function love.draw()
    draw()
    if pending_capture then
        local name=pending_capture;pending_capture=nil
        love.graphics.captureScreenshot(function(data)
            local bytes=data:encode('png'):getString();local f=assert(io.open(out..'/'..name..'.png','wb'));f:write(bytes);f:close()
        end)
    end
end
