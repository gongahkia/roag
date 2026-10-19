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
local G=require('src.game')
local I=require('src.idea_boon_content')
local C=require('src.boon_content')
local R=require('src.rewards')
local S=require('src.kenney_symbols')
local O=require('src.onebit_visuals')
local Palette=require('src.visual_palette')
local P=require('src.persistence')
local PNG=require('tools.art_png')
local s,p,events
function G.new_stage()
    s=G.new(37);p=G.player(s);p.hp=6
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
        for _,id in ipairs(I.order)do G.set_boon_stacks(s,p.id,id,'rare',1)end
        print('Renderer:',love.graphics.getRendererInfo())
        assert(love.roagAssetBacking(p.definition_id)=='onebit')
        love.keypressed('i');before=snapshot();started=love.timer.getTime()
    end,debug.traceback)
    if not ok then fail(why)end
end
local function check_and_advance(image)
    assert(snapshot()==before,'drawing or menu changed authoritative state')
    local label=stage=='inventory' and 'inventory-'..page or stage
    PNG.write(output..'/'..label..'.png',image)
    if stage=='inventory' then
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
test -s "$output/inventory-1.png"
test -s "$output/food-and-gold.png"
test -s "$output/health-and-ward.png"
test -s "$output/detail.png"
printf 'PASS actual boon renderer and pickups: %s\n' "$output"
