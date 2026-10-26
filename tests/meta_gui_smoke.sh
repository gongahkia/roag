#!/bin/sh
# Callback-driven two-process title/profile smoke. All saves use isolated XDG data.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-meta-gui-XXXXXX")
mkdir -p "$output_dir/data" "$output_dir/tests"
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
mkdir -p "$output_dir/assets"
cp -R "$project_dir/assets/fonts" "$output_dir/assets/fonts"
cp "$project_dir/tests/legal_playthrough.lua" "$output_dir/tests/legal_playthrough.lua"
cp "$project_dir/tests/expansion_legal_playthrough.lua" "$output_dir/tests/expansion_legal_playthrough.lua"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Profile=require('src.profile')
local Persistence=require('src.persistence')
local RunSeed=require('src.run_seed')
local tracked
local original_new,original_restore,original_submit=Game.new_stage,Game.restore,Game.submit
function Game.new_stage(...)
    local result=original_new(...)
    tracked=result
    return result
end
function Game.restore(...)
    local result=original_restore(...)
    tracked=result
    return result
end
assert(love.filesystem.load('roag_main.lua'))()
local original_load,original_draw,original_update=love.load,love.draw,love.update
local phase=assert(os.getenv('ROAG_META_PHASE'))
local output=assert(os.getenv('ROAG_META_OUTPUT'))
local function mark(text) io.stderr:write(('Meta %s %.3fs: %s\n'):format(phase,love.timer.getTime(),text))end
local labels={}
local old_print,old_printf=love.graphics.print,love.graphics.printf
love.graphics.print=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_print(value,...)
end
love.graphics.printf=function(value,...)
    labels[#labels+1]=tostring(value)
    return old_printf(value,...)
end
local function has(fragment)
    for _,value in ipairs(labels) do
        if value:find(fragment,1,true) then return true end
    end
end
local function capture(name,then_do)
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/'..name..'.png','wb'))
        file:write(image:encode('png'):getString());file:close()
        local ok,reason=xpcall(then_do,debug.traceback)
        if not ok then
            local failure=assert(io.open(output..'/failure.txt','wb'))
            failure:write(reason);failure:close()
            love.event.quit(2)
        end
    end)
end
local function main_state()
    for index=1,100 do
        local name,value=debug.getupvalue(love.keypressed,index)
        if name=='state' then return value end
    end
end
local step=0
function love.load(...)
    original_load(...)
    if phase=='earn' then
        love.keypressed('u')
    else
        local profile=Profile.new(require('src.love_storage')):inspect()
        assert(profile.status=='active' or profile.status=='recovered')
        assert(profile.profile.earned['core:achievement/first_guardian'])
        love.keypressed('u')
    end
end
function love.draw(...)
    labels={}
    original_draw(...)
    if step==0 then
        assert(has('ACHIEVEMENTS'))
        assert(has(phase=='earn' and 'LOCKED — First Guardian'
            or 'UNLOCKED — First Guardian'))
        step=1
        capture(phase..'-unlocks',function()
            love.keypressed('escape')
            if phase=='earn' then
                love.keypressed('n')
                local live=assert(main_state())
                tracked=live
                assert(live.meta.eligible and
                    live.run_seed==RunSeed.for_serial(1) and
                    live.floor_order and #live.floor_order==2)
                assert(main_state()==live)
                mark('begin legal stage driver')
                local Driver=require('tests.expansion_legal_playthrough')
                local guided,reason=Driver.attempt(live.run_seed,{stop_after_stage=1})
                assert(guided,reason) -- generate ordinary paired-floor commands
                mark('driver complete: '..#guided.trace..' actions')
                tracked=live
                local defeated=false
                for _,record in ipairs(guided.trace) do
                    local ok,reason,events=original_submit(live,record.intent)
                    assert(ok==record.ok and reason==record.reason)
                    for _,event in ipairs(events) do
                        if event.kind=='EliteDefeated' then defeated=true end
                    end
                    if defeated then break end
                end
                mark('replay complete')
                assert(defeated and live.meta.pending['core:achievement/first_guardian'])
                assert(main_state()==live and live.clock>0)
                original_update(10)
                love.keypressed('escape') -- pause; no world time
                local clock=live.clock
                mark('before Save & Quit; UI '..love.roagUIState().page)
                love.keypressed('2') -- Save & Quit commits profile
                mark('after Save & Quit')
                assert(live.clock==clock)
                local profile=Profile.new(require('src.love_storage')):inspect()
                assert(profile.profile and profile.profile.earned['core:achievement/first_guardian'],
                    'Save & Quit failed to commit earned profile')
            else
                love.keypressed('n') -- existing active save requires confirmation
                love.keypressed('return')
                tracked=assert(main_state())
                assert(tracked.run_seed==RunSeed.for_serial(2) and
                    tracked.run_seed~=RunSeed.for_serial(1))
                local unlocked=false
                for _,id in ipairs(tracked.available_boons) do
                    if id=='core:boon/guardians_vow' then unlocked=true end
                end
                assert(unlocked and #tracked.available_boons>=47,
                    'future run did not freeze the expanded pool plus reward')
                step=2
            end
        end)
    elseif step==2 then
        assert(has('STAGE 1') or has('OVERGROWN RUINS'))
        step=3
        capture('future-run',function()
            local clock=tracked.clock
            love.keypressed('escape')
            love.keypressed('2')
            assert(tracked.clock==clock)
        end)
    end
end
local checked_draw=love.draw
function love.draw(...)
    local ok,why=pcall(checked_draw,...)
    if not ok then
        io.stderr:write('Meta draw assertion: '..tostring(why)..'\n')
        love.event.quit(2)
    end
end

LUA
ROAG_META_PHASE=earn ROAG_META_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" timeout -k 3s 35s love "$output_dir"
ROAG_META_PHASE=future ROAG_META_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" timeout -k 3s 35s love "$output_dir"
test -s "$output_dir/earn-unlocks.png"
test -s "$output_dir/future-unlocks.png"
test -s "$output_dir/future-run.png"
printf 'Meta GUI smoke passed: legal elite trace, disk profile, future-run pool in %s\n' "$output_dir"
