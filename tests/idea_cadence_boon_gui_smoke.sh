#!/bin/sh
# Real game callbacks/rendering, assisted inventory/enemy prerequisites.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d /var/tmp/roag-cadence-boon-gui-XXXXXX)}
mkdir -p "$output/app/tools" "$output/app/tests" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets" "$output/app/assets"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/art_png.lua"
cp "$project_dir/tests/idea_cadence_fixture.lua" "$output/app/tests/idea_cadence_fixture.lua"
cat > "$output/app/main.lua" <<'LUA'
io.stdout:setvbuf('line')
function love.errorhandler(why)
    io.stderr:write(debug.traceback(tostring(why),2)..'\n');return function()return 1 end
end
local G=require('src.game')
local P=require('src.persistence')
local Camera=require('src.camera')
local R=require('src.rewards')
local S=require('src.kenney_symbols')
local O=require('src.onebit_visuals')
local Palette=require('src.visual_palette')
local Playback=require('src.playback')
local PNG=require('tools.art_png')
local F=require('tests.idea_cadence_fixture')
local slug=os.getenv('ROAG_CADENCE_BOON_CASE') or 'threefold_remedy'
local s,p,events,pb,f
local playback_new=Playback.new
function Playback.new(...)pb=playback_new(...);return pb end
function G.new_stage()
    f=F.prepare(slug);s=f.state;p=f.player;return s
end
local submit=G.submit
function G.submit(...)
    local ok,why,result,preview=submit(...);events=result;return ok,why,result,preview
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_CADENCE_BOON_OUTPUT'))
local stage,before,baseline,started='before'
local pending,captured,failed=false,nil,false
local function snapshot()return P.encode(G.snapshot(s))end
local function fail(why)
    failed=true;io.stderr:write('CADENCE BOON GUI FAILED ['..stage..']: '..tostring(why)..'\n')
    love.event.quit(1)
end
function love.load(...)
    local args={...}
    local ok,why=xpcall(function()
        love.mouse.getPosition=function()return 0,0 end
        love.keyboard.isDown=function()return false end
        load(unpack(args));source=love.image.newImageData(S.path)
        F.install(f)
        before=snapshot();started=love.timer.getTime()
        print('Renderer:',love.graphics.getRendererInfo())
    end,debug.traceback)
    if not ok then fail(why)end
end
local function advance(image)
    assert(snapshot()==before,'rendering changed authoritative state')
    PNG.write(output..'/'..stage..'.png',image)
    if stage=='before'then
        baseline=image
        if f.intent.kind=='move'then love.keypressed('d')
        else
            love.keypressed(f.intent.kind=='sweeping_slash' and 'r' or 'f')
            love.keypressed('d');love.keypressed('return')
        end
        assert(events and #events>0);F.check(f,events)
        local shown=false
        for _=1,400 do
            local event=Playback.current(pb)
            for _,v in ipairs(Playback.current_group(pb)or (event and {event}or {}))do
                if v.kind=='BoonActivated' and v.boon_id=='core:boon/'..slug then shown=true end
            end
            if shown then break end
            update(.01)
        end
        assert(shown,'real activation cue never appeared in playback')
        stage='after';before=snapshot()
    else
        local changes=0
        for y=16,623,3 do for x=16,1007,3 do
            local a={baseline:getPixel(x,y)};local b={image:getPixel(x,y)}
            if math.abs(a[1]-b[1])+math.abs(a[2]-b[2])+math.abs(a[3]-b[3])>.05 then changes=changes+1 end
        end end
        assert(changes>8,'real rendered result did not change')
        local expected=snapshot();love.keypressed('f5');update(0)
        assert(snapshot()==expected,'refresh changed boon state')
        print('PASS '..slug..': real key action, one activation, rendered result, pure frames/F5')
        love.event.quit()
    end
end
function love.update()
    if failed then return end
    local ok,why=xpcall(function()
        if started and love.timer.getTime()-started>20 then error('fixture timed out')end
        if captured then local image=captured;captured=nil;advance(image)end
    end,debug.traceback)
    if not ok then fail(why)end
end
function love.draw()
    if failed then return end
    local ok,why=xpcall(function()
        draw()
        if not pending and not captured then
            pending=true;love.graphics.captureScreenshot(function(image)captured=image;pending=false end)
        end
    end,debug.traceback)
    if not ok then fail(why)end
end
LUA
ROAG_GUI_AUTOSTART=1 ROAG_CADENCE_BOON_OUTPUT="$output" XDG_DATA_HOME="$output/data" \
    timeout -k 3s 35s love "$output/app" > "$output/love.log" 2>&1 || {
    cat "$output/love.log"
    printf 'Retained failed cadence boon artifacts: %s\n' "$output"
    exit 1
}
test -s "$output/before.png"
test -s "$output/after.png"
cat "$output/love.log"
printf 'Retained cadence boon artifacts: %s\n' "$output"
