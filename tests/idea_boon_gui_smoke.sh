#!/bin/sh
# Actual game renderer, native source-pixel checks and legal pickup callbacks.
# Assertions run on update, never across the screenshot callback's C boundary.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d /var/tmp/roag-idea-boon-gui-XXXXXX)}
mkdir -p "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets" "$output/app/assets"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/art_png.lua"
cat > "$output/app/main.lua" <<'LUA'
io.stdout:setvbuf('line')
-- Also catch parse/load errors before the detailed phase handler is installed.
function love.errorhandler(why)
    io.stderr:write(debug.traceback(tostring(why),2)..'\n')
    return function()return 1 end
end
local G=require('src.game')
local I=require('src.idea_boon_content')
local C=require('src.boon_content')
local R=require('src.rewards')
local S=require('src.kenney_symbols')
local O=require('src.onebit_visuals')
local Palette=require('src.visual_palette')
local P=require('src.persistence')
local PNG=require('tools.art_png')
local mode=os.getenv('ROAG_IDEA_BOON_CASE') or 'supply'
assert(mode=='supply' or mode=='vow' or mode=='dice')
local wanted_face=mode=='dice' and assert(tonumber(os.getenv('ROAG_DICE_FACE')))or nil
if wanted_face then assert(wanted_face%1==0 and wanted_face>=1 and wanted_face<=8)end
local s,p,events
function G.new_stage()
    if mode=='dice' then
        for seed=1,80 do
            s=G.new(seed)
            if require('src.rng').next_int({state=s.rng.state},8)==wanted_face then break end
        end
        p=G.player(s);p.hp=10
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,100000)
        e.hp=100;e.max_hp=100
        local other=G.spawn_enemy(s,'core:enemy/mossbound_guard',4,2,100000)
        other.hp=100;other.max_hp=100
        return s
    end
    s=G.new(37);p=G.player(s);p.hp=6
    if mode=='vow' then
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2,100000)
        e.hp=100;e.max_hp=100
        return s
    end
    R.spawn_currency(s,2,s.active_floor_id,3,2,function()end,p.id)
    R.spawn_supply(s,'food',s.active_floor_id,3,2,function()end,p.id)
    R.spawn_supply(s,'health',s.active_floor_id,4,2,function()end,p.id)
    R.spawn_supply(s,'health',s.active_floor_id,4,2,function()end,p.id)
    return s
end
local submit=G.submit
function G.submit(...)
    local ok,why,result,preview=submit(...);events=result
    return ok,why,result,preview
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_IDEA_BOON_OUTPUT'))
local stage,page,pending,captured='inventory',1,false,nil
local source,before,started
local pages=math.ceil(#I.order/7)
local function snapshot()return P.encode(G.snapshot(s))end
local function fail(why)
    io.stderr:write('IDEA BOON GUI FAILED ['..stage..']: '..tostring(why)..'\n')
    love.event.quit(1)
end
local function compare(image,id,line)
    local index=C.get(id).art.index
    local sx,sy=S.region(index)
    local checked=0
    for y=0,15 do for x=0,15 do
        local r,g,b,a=source:getPixel(sx+x,sy+y)
        if a==1 then
            local actual={image:getPixel(104+x*2+1,150+(line-1)*44+y*2+1)}
            local gray=r*O.ink[1]*.2126+g*O.ink[2]*.7152+b*O.ink[3]*.0722
            for channel=1,3 do
                assert(math.abs(actual[channel]-gray*Palette.gold[channel])<=2/255+1e-6,
                    'native icon pixels differ: '..id..' at '..x..','..y)
            end
            checked=checked+1
        end
    end end
    assert(checked>5,'empty boon icon '..id)
end
function love.load(...)
    local args={...}
    local ok,why=xpcall(function()
        love.mouse.getPosition=function()return 0,0 end
        love.keyboard.isDown=function()return false end
        load(unpack(args));source=love.image.newImageData(S.path)
        -- AUTOSTART applies the game's developer showcase after new_stage;
        -- install this fixture's inventory after that deliberate reset.
        for _,id in ipairs(I.order)do
            if mode=='supply' or mode=='vow' and id=='core:boon/pilgrims_vow'
                or mode=='dice' and id=='core:boon/fateful_dice' then
                G.set_boon_stacks(s,p.id,id,'rare',1)
            end
        end
        print('Renderer:',love.graphics.getRendererInfo())
        assert(love.roagAssetBacking(p.definition_id)=='onebit')
        if mode=='vow' then
            before=snapshot()
            love.gamepadpressed(nil,'x');love.gamepadpressed(nil,'leftshoulder')
            love.gamepadpressed(nil,'dpright')
            assert(snapshot()==before,'arming a vow changed the run')
            stage='vow-preview'
        elseif mode=='dice' then stage='dice-ready'
        else love.keypressed('i')end
        before=snapshot();started=love.timer.getTime()
    end,debug.traceback)
    if not ok then fail(why)end
end
local function check_and_advance(image)
    assert(snapshot()==before,'drawing or menu changed authoritative state')
    local label=stage=='inventory' and 'inventory-'..page or stage
    PNG.write(output..'/'..label..'.png',image)
    if stage=='dice-ready' then
        love.keypressed('f');love.keypressed('d');love.keypressed('return');update(10)
        local rolls=0
        for _,e in ipairs(events)do if e.kind=='FateRolled' then
            rolls=rolls+1;assert(e.face==wanted_face)
        end end
        assert(rolls==1 and p.boon_memory['core:boon/fateful_dice'].face==wanted_face)
        stage='dice-result'
    elseif stage=='dice-result' then
        local refreshes=tonumber(os.getenv('ROAG_DICE_REFRESHES') or '1')
        assert(refreshes%1==0 and refreshes>=1 and refreshes<=20)
        local baseline
        for n=1,refreshes do
            local expected=snapshot()
            love.keypressed('f5');update(0);collectgarbage('collect')
            assert(snapshot()==expected,'visual refresh changed saved roll, RNG or run')
            local stats=love.graphics.getStats()
            if n==2 then baseline=stats end
            if baseline and stats.images and baseline.images then
                assert(stats.images<=baseline.images+4,'visual refresh accumulated image handles')
            end
        end
        local stats=love.graphics.getStats()
        print('Dice F5 refreshes:',refreshes,'images:',stats.images,'texture bytes:',stats.texturememory)
        love.keypressed('i');stage='dice-status'
    elseif stage=='dice-status' then
        local statuses=require('src.idea_presentation').statuses(p)
        local found
        for n,status in ipairs(statuses)do
            if status.id==I.cues[wanted_face].id then found=n;break end
        end
        assert(found and found<=3,'rolled icon not shown in character panel')
        local sx,sy=S.region(I.cues[wanted_face].index)
        local checked=0
        for y=0,15 do for x=0,15 do
            local r,g,b,a=source:getPixel(sx+x,sy+y)
            if a==1 then
                local actual={image:getPixel(636+(found-1)*96+x,410+y)}
                local gray=r*O.ink[1]*.2126+g*O.ink[2]*.7152+b*O.ink[3]*.0722
                for c=1,3 do assert(math.abs(actual[c]-gray*Palette.gold[c])<=2/255+1e-6,
                    'rolled face pixels differ: '..wanted_face)end
                checked=checked+1
            end
        end end
        assert(checked>5);print('PASS real rolled face '..wanted_face..' native pixels and pure status view')
        love.event.quit();return
    elseif stage=='vow-preview' then
        love.keypressed('escape');assert(snapshot()==before,'canceling vow changed the run')
        love.keypressed('f');love.keypressed('p');love.keypressed('d');love.keypressed('return');update(10)
        assert(p.hp==4 and s.clock==100)
        local paid=0
        for _,e in ipairs(events)do
            if e.kind=='VowPaid' then paid=paid+1;assert(e.hp_cost==2 and e.bonus==6)end
            if e.kind=='AttackPerformed' then assert(e.raw_damage==p.damage+6)end
        end
        assert(paid==1);stage='vow-paid'
    elseif stage=='vow-paid' then
        love.keypressed('f');love.keypressed('d');love.keypressed('return');update(10)
        assert(p.hp==4 and s.clock==200)
        for _,e in ipairs(events)do
            assert(e.kind~='VowPaid')
            if e.kind=='AttackPerformed' then assert(e.raw_damage==p.damage)end
        end
        stage='vow-finished'
    elseif stage=='vow-finished' then
        print('PASS actual Pilgrim aim, cancel, one paid attack and ordinary follow-up')
        love.event.quit();return
    elseif stage=='inventory' then
        for line=1,7 do
            local id=I.order[(page-1)*7+line]
            if id then compare(image,id,line)end
        end
        if page<pages then page=page+1;love.keypressed('right')
        else
            love.keypressed('i');love.keypressed('d');update(10)
            assert(s.clock==100 and p.position.x==3 and p.hp==10 and s.currency==2)
            assert(p.boon_memory['core:boon/road_flask'].speed==20)
            assert(p.boon_memory['core:boon/ration_reserve'].charges==1)
            assert(p.boon_memory['core:boon/beggars_cap'].ward==4)
            stage='food-and-gold'
        end
    elseif stage=='food-and-gold' then
        local cost=G.preview(s,'move',1,0).cost
        love.keypressed('d');update(10)
        assert(p.position.x==4 and p.hp==24 and s.clock==100+cost)
        assert(p.boon_memory['core:boon/overflow_chalice'].ward==4)
        stage='health-and-ward'
    elseif stage=='health-and-ward' then
        love.keypressed('i');love.keypressed('return');stage='detail'
    else
        assert(stage=='detail');print('PASS native boon pixels, pickup actions and read-only details')
        love.event.quit();return
    end
    before=snapshot()
end
function love.update()
    local ok,why=xpcall(function()
        if started and love.timer.getTime()-started>20 then error('fixture timed out')end
        if captured then
            local image=captured;captured=nil;check_and_advance(image)
        end
    end,debug.traceback)
    if not ok then fail(why)end
end
function love.draw()
    local ok,why=xpcall(function()
        draw()
        if not pending and not captured then
            pending=true
            love.graphics.captureScreenshot(function(image)
                captured=image;pending=false
            end)
        end
    end,debug.traceback)
    if not ok then fail(why)end
end
function love.errorhandler(why)
    fail(debug.traceback(tostring(why),2));return function()return 1 end
end
LUA
ROAG_GUI_AUTOSTART=1 ROAG_IDEA_BOON_OUTPUT="$output" XDG_DATA_HOME="$output/data" \
    timeout -k 3s 30s love "$output/app" > "$output/love.log" 2>&1 || {
    cat "$output/love.log";printf 'Retained failed GUI artifacts: %s\n' "$output";exit 1;
}
if [ "${ROAG_IDEA_BOON_CASE:-supply}" = dice ]; then
    test -s "$output/dice-ready.png"
    test -s "$output/dice-result.png"
    test -s "$output/dice-status.png"
elif [ "${ROAG_IDEA_BOON_CASE:-supply}" = vow ]; then
    test -s "$output/vow-preview.png"
    test -s "$output/vow-paid.png"
    test -s "$output/vow-finished.png"
else
    test -s "$output/inventory-1.png"
    test -s "$output/food-and-gold.png"
    test -s "$output/health-and-ward.png"
    test -s "$output/detail.png"
fi
printf 'PASS actual boon renderer and pickups: %s\n' "$output"
