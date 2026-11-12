-- Real LÖVE callbacks/rendering, ordinary new-run inputs, isolated disk saves.
-- The spies below observe returned state/events; they never replace a world,
-- inject inventory/enemies, or bypass the production input callback.
local Game=require('src.game')
local Codec=require('src.persistence')
local output=assert(os.getenv('ROAG_CLASSIC_GUI_OUTPUT'))
local mode=assert(os.getenv('ROAG_CLASSIC_GUI_MODE'))
local tracked,actions,created_seed
actions={}
local capture_continue=false
local new,restore,submit=Game.new_stage,Game.restore,Game.submit
function Game.new_stage(...)
    created_seed=select(1,...)
    tracked=new(...)
    return tracked
end
function Game.restore(...)
    local restored=restore(...)
    -- Checkpoint validation also restores a copy. Retain the actual new-run
    -- object; a fresh process initially observes the restored continuation.
    if not tracked or capture_continue then tracked=restored end
    return restored
end
function Game.submit(state,intent,...)
    local before=state.clock
    local ok,why,events=submit(state,intent,...)
    tracked=state
    actions[#actions+1]={intent=intent,committed=ok,reason=why,
        before=before,after=state.clock,events=events}
    return ok,why,events
end

assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,pending,failed,frames=1,false,false,0
local stable_bytes,expected_clock,move,attack,loaded
local screenshot_names=mode=='preview' and
    {'initial','biome','floor','class','seed','free-camera','camera-pan','god-play'} or mode=='save' and
    {'title','class-geomancer','new-run','idle','paid-move','wait','aim','attack','boons','pause'} or
    {'continue-title','continued','continued-idle','continued-wait'}

local function write(name,bytes)
    local f=assert(io.open(output..'/'..name,'wb'));f:write(bytes);f:close()
end
local function read(name)
    local f=assert(io.open(output..'/'..name,'rb'));local bytes=f:read('*a');f:close();return bytes
end
local function snapshot()return Codec.encode(Game.snapshot(assert(tracked)))end
local function unchanged(label)
    assert(snapshot()==stable_bytes,label..' changed authoritative state or RNG')
end
local function press(key)
    love.keypressed(key,key,false)
end
local function fail(why)
    if failed then return end
    failed=true
    io.stderr:write('CLASSIC GUI '..mode..' phase '..phase..' FAILED: '..tostring(why)..'\n')
    love.event.quit(1)
end
local function assert_catalog()
    assert(love.roagVisualPackMode()=='catalog','wrong active artwork pack')
    assert(love.roagAssetStatus()=='generated','catalog renderer unavailable')
    if tracked then
        assert(love.roagAssetBacking(Game.player(tracked).definition_id)=='catalog')
    end
end
local function idle_ready()
    if mode=='save' and phase==4 or mode=='load' and phase==3 then
        unchanged('idle update/draw');frames=frames+1
        return frames>=120
    end
    return true
end
local function record()
    write(mode..'-actions.snapshot',Codec.encode(actions))
    write(mode..'-result.txt',('PASS classic catalog GUI\nmode=%s\nseed=%d\nclock=%d\nclass=%s\nidleFrames=120\npack=%s\ncommittedInputs=%d\n'):
        format(mode,tracked.root_seed or tracked.run_seed or 0,tracked.clock,
            Game.player(tracked).definition_id,love.roagVisualPackMode(),#actions))
end

function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args));love.mouse.setPosition(8,8)
        assert(love.roagUIState().page==(mode=='preview' and 'game' or 'home'),'incorrect launch mode')
        if mode=='preview' then
            assert(os.getenv('ROAG_SPECTATOR')~='1','preview test must use the command-line flag')
            assert(tracked and Game.player(tracked).invulnerable and not tracked.meta.eligible)
        end
        assert_catalog()
    end)
    if not ok then fail(why)end
end
function love.update(dt)
    if failed then return end
    local ok,why=pcall(update,dt)
    if not ok then fail(why)end
end
local function advance_save()
    if phase==1 then
        press('2');assert(love.roagUIState().page=='class')
        press('right');press('left');press('1')
        assert(love.roagUIState().class=='core:actor/adventurer')
        for _=1,15 do
            if love.roagUIState().class=='core:actor/geomancer' then break end
            press('right')
        end
        assert(love.roagUIState().class=='core:actor/geomancer','class carousel missed Geomancer')
    elseif phase==2 then
        press('1')
        press('return')
        assert(tracked and tracked.clock==0 and love.roagUIState().page=='game')
        assert(Game.player(tracked).definition_id=='core:actor/adventurer')
        assert_catalog();stable_bytes=snapshot();write('new-run.snapshot',stable_bytes)
    elseif phase==3 then
        unchanged('initial rendering')
        require('classic_catalog_pixels')(draw,output)
        unchanged('isolated source-pixel and presentation review');frames=0
    elseif phase==4 then
        unchanged('120 idle frames')
        for _,candidate in ipairs({{'d',1,0},{'s',0,1},{'a',-1,0},{'w',0,-1}})do
            local preview=Game.preview(tracked,'move',candidate[2],candidate[3])
            if preview.valid and preview.cells[1] and preview.cells[1].status=='open' then
                move=candidate;expected_clock=tracked.clock+preview.cost;break
            end
        end
        assert(move,'ordinary starting room has no legal adjacent move')
        unchanged('movement preview')
        local previous=Game.player(tracked).position
        local x,y=previous.x,previous.y
        press(move[1]);assert(tracked.clock==expected_clock and actions[#actions].committed)
        local position=Game.player(tracked).position
        assert(position.x==x+move[2] and position.y==y+move[3],'input did not move one tile')
    elseif phase==5 then
        local before=tracked.clock;press('space')
        assert(tracked.clock==before+100 and actions[#actions].intent.kind=='wait')
    elseif phase==6 then
        stable_bytes=snapshot()
        for _,candidate in ipairs({{'d',1,0},{'s',0,1},{'a',-1,0},{'w',0,-1}})do
            local preview=Game.preview(tracked,'basic_attack',candidate[2],candidate[3])
            if preview.valid then attack=candidate;expected_clock=tracked.clock+preview.cost;break end
        end
        assert(attack,'no legal aim in ordinary new run')
        press('f');press(attack[1]);unchanged('aiming')
    elseif phase==7 then
        unchanged('aim rendering');press('return')
        assert(tracked.clock==expected_clock and actions[#actions].intent.kind=='basic_attack')
        local performed=false
        for _,event in ipairs(actions[#actions].events)do
            if event.kind=='AttackPerformed' and event.source_id==tracked.player_id then performed=true end
        end
        assert(performed,'paid aimed attack did not emit player AttackPerformed')
    elseif phase==8 then
        stable_bytes=snapshot();press('i')
        assert(love.roagUIState().page=='inventory');unchanged('boon panel')
    elseif phase==9 then
        unchanged('boon panel rendering');press('i');press('escape')
        assert(love.roagUIState().page=='pause');unchanged('pause')
    else
        unchanged('pause rendering');write('expected.snapshot',stable_bytes)
        local control=restore(Game.snapshot(tracked))
        assert(submit(control,{kind='wait'}))
        write('expected-next.snapshot',Codec.encode(Game.snapshot(control)))
        record();press('2') -- production Save & Quit, not a direct save-service call
    end
end
local function advance_load()
    if phase==1 then
        capture_continue=true
        press('c');capture_continue=false
        assert(tracked and love.roagUIState().page=='game')
        assert(snapshot()==read('expected.snapshot'),'fresh process Continue differs')
        stable_bytes=snapshot();loaded=true;assert_catalog()
    elseif phase==2 then
        unchanged('continued rendering');frames=0
    elseif phase==3 then
        unchanged('120 continued idle frames');press('space')
        assert(snapshot()==read('expected-next.snapshot'),'paid continuation differs after disk reload')
    else
        assert(loaded);write('continued.snapshot',snapshot());record();love.event.quit()
    end
end
local function advance_preview()
    assert(Game.player(tracked).invulnerable and not tracked.meta.eligible)
    assert(tracked.clock==0,'preview control spent world time')
    if phase==1 then
        local before=snapshot()
        for _,key in ipairs({'f6','f7','f8','f9'})do press(key);assert(snapshot()==before)end
        press('j');assert(tracked.generated_map.biome_id==require('src.biomes').order[2])
    elseif phase==2 then
        press('k');assert(tracked.active_floor_index==2)
    elseif phase==3 then
        press('k');assert(tracked.active_floor_index==1)
        press('l');assert(Game.player(tracked).definition_id==require('src.classes').order[2])
    elseif phase==4 then
        local seed=created_seed;press('m');assert(created_seed==seed+1)
        stable_bytes=snapshot();press('n');unchanged('same-seed preview restart')
    elseif phase==5 then
        press('g');unchanged('free camera toggle')
    elseif phase==6 then
        for _,key in ipairs({'d','w','a','s','space'})do press(key);unchanged('free camera input')end
    elseif phase==7 then
        press('g');unchanged('god play toggle')
    else
        local before=#actions
        press('space')
        local action=actions[#actions]
        assert(#actions==before+1 and action.intent.kind=='wait' and action.committed and tracked.clock>0,
            'G did not restore paid character actions')
        write('preview-result.txt','PASS --spectator launch without environment flag; J/K/L/M/N/G; free camera; normal controls restored\n')
        love.event.quit()
    end
end
function love.draw(...)
    if failed then return end
    local args={...}
    local ok,why=pcall(function()
        draw(unpack(args))
        if pending or phase>#screenshot_names or love.roagPlaybackPending() or not idle_ready() then return end
        assert_catalog();pending=true
        love.graphics.captureScreenshot(function(image)
            local good,reason=pcall(function()
                write(mode..'-'..screenshot_names[phase]..'.png',image:encode('png'):getString())
                if mode=='preview' then advance_preview()
                elseif mode=='save' then advance_save()else advance_load()end
                phase=phase+1;pending=false
            end)
            if not good then fail(reason)end
        end)
    end)
    if not ok then fail(why)end
end
