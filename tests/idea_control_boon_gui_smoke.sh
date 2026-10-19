#!/bin/sh
# Real game callbacks/rendering, assisted inventory/enemy prerequisites.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d /var/tmp/roag-control-boon-gui-XXXXXX)}
mkdir -p "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets" "$output/app/assets"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/art_png.lua"
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
local slug=os.getenv('ROAG_CONTROL_BOON_CASE') or 'anchor_maul'
local id='core:boon/'..slug
assert(({grasping_hand=true,anchor_maul=true,hourglass_clasp=true,
    ward_ledger=true,cinder_stopper=true,scavengers_hook=true})[slug])
local s,p,e,events,camera,source,pb
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
local playback_new=Playback.new
function Playback.new(...)pb=playback_new(...);return pb end
function G.new_stage()
    s=G.new(37,nil,{class_id=slug=='grasping_hand' and 'core:actor/ranger' or nil})
    p=G.player(s)
    if slug=='ward_ledger'then p.hp=10
    elseif slug=='scavengers_hook'then
        p.hp=6
        R.spawn_currency(s,2,s.active_floor_id,3,2,function()end,p.id)
        R.spawn_supply(s,'food',s.active_floor_id,3,2,function()end,p.id)
        R.spawn_supply(s,'health',s.active_floor_id,2,3,function()end,p.id)
    else
        e=G.spawn_enemy(s,'core:enemy/mossbound_guard',slug=='grasping_hand' and 5 or 3,2,100000)
        e.hp=100;e.max_hp=100
        if slug=='cinder_stopper'then
            G.set_boon_stacks(s,p.id,'core:boon/emberbrand','common',1)
            assert(G.submit(s,{kind='basic_attack',dx=1,dy=0}))
            G.set_boon_stacks(s,p.id,'core:boon/emberbrand','common',0)
            assert(e.statuses.burning)
        end
    end
    return s
end
local submit=G.submit
function G.submit(...)
    local ok,why,result,preview=submit(...);events=result;return ok,why,result,preview
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_CONTROL_BOON_OUTPUT'))
local stage,before,baseline,started='before'
local pending,captured,failed=false,nil,false
local function snapshot()return P.encode(G.snapshot(s))end
local function fail(why)
    failed=true;io.stderr:write('CONTROL BOON GUI FAILED ['..stage..']: '..tostring(why)..'\n')
    love.event.quit(1)
end
function love.load(...)
    local args={...}
    local ok,why=xpcall(function()
        love.mouse.getPosition=function()return 0,0 end
        love.keyboard.isDown=function()return false end
        load(unpack(args));source=love.image.newImageData(S.path)
        G.set_boon_stacks(s,p.id,id,'rare',1)
        if slug=='ward_ledger'then
            G.set_boon_stacks(s,p.id,'core:boon/beggars_cap','rare',1)
            p.boon_memory={['core:boon/beggars_cap']={ward=6,expires_at=500}}
        end
        before=snapshot();started=love.timer.getTime()
        print('Renderer:',love.graphics.getRendererInfo())
    end,debug.traceback)
    if not ok then fail(why)end
end
local function compare_actor(image)
    local x,y=Camera.tile_top_left(camera,e.position.x,e.position.y)
    local sx,sy=S.region(O.actors[e.definition_id]);local checked=0
    for yy=0,12 do for xx=0,15 do
        local r,g,b,a=source:getPixel(sx+xx,sy+yy)
        if a==1 then
            local pixel={image:getPixel(math.floor(x+(xx+.5)*camera.tile/16),
                math.floor(y+(yy+.5)*camera.tile/16))}
            local gray=r*O.ink[1]*.2126+g*O.ink[2]*.7152+b*O.ink[3]*.0722
            for c=1,3 do assert(math.abs(pixel[c]-gray*Palette.enemy[c])<=2/255+1e-6,
                'moved actor native pixels differ')end
            checked=checked+1
        end
    end end
    assert(checked>5)
end
local function advance(image)
    assert(snapshot()==before,'rendering changed authoritative state')
    PNG.write(output..'/'..stage..'.png',image)
    if stage=='before'then
        baseline=image
        if slug=='ward_ledger' or slug=='scavengers_hook'then love.keypressed('space')
        else
            love.keypressed(slug=='grasping_hand' and 'r' or 'f')
            love.keypressed('d');love.keypressed('return')
        end
        assert(events and #events>0)
        local activated=0
        for _,event in ipairs(events)do
            if event.kind=='BoonActivated' and event.boon_id==id then activated=activated+1 end
        end
        assert(activated==1,'one real activation required')
        if slug=='hourglass_clasp' or slug=='cinder_stopper'then
            local shown=false
            for _=1,400 do
                local event=Playback.current(pb)
                for _,e in ipairs(Playback.current_group(pb)or (event and {event}or {}))do
                    if e.kind=='BoonActivated' and e.boon_id==id then shown=true end
                end
                if shown then break end
                update(.01)
            end
            assert(shown,'real activation cue never appeared in playback')
        else update(10)end
        if slug=='grasping_hand' or slug=='anchor_maul'then assert(e.position.x==4)
        elseif slug=='hourglass_clasp'then assert(e.stunned_until==30 and p.boon_memory[id].expires_at==400)
        elseif slug=='ward_ledger'then assert(p.hp==13 and p.boon_memory['core:boon/beggars_cap'].ward==0)
        elseif slug=='cinder_stopper'then assert(not e.statuses.burning)
        else
            assert(p.hp==20 and s.currency==2,'Hook must retain normal food=4 / health=10 healing')
            for _,drop in ipairs(s.drops)do assert(drop.collected)end
        end
        stage='after';before=snapshot()
    else
        if slug=='grasping_hand' or slug=='anchor_maul'then compare_actor(image)end
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
ROAG_GUI_AUTOSTART=1 ROAG_CONTROL_BOON_OUTPUT="$output" XDG_DATA_HOME="$output/data" \
    timeout -k 3s 35s love "$output/app" > "$output/love.log" 2>&1 || {
    cat "$output/love.log"
    printf 'Retained failed control boon artifacts: %s\n' "$output"
    exit 1
}
test -s "$output/before.png"
test -s "$output/after.png"
cat "$output/love.log"
printf 'Retained control boon artifacts: %s\n' "$output"
