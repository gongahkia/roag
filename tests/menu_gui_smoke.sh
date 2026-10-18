#!/bin/sh
# Microcraft-inspired ROAG menus: actual callback/render and fresh-process saves.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d "${TMPDIR:-/var/tmp}/roag-menus-gui-XXXXXX")}
mkdir -p "$output/app/assets" "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets/fonts" "$project_dir/assets/kenney" "$output/app/assets/"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/"
cat > "$output/app/main.lua" <<'LUA'
local Game=require('src.game')
local Camera=require('src.camera')
local Codec=require('src.persistence')
local PNG=require('tools.art_png')
local Boons=require('src.boon_content')
local UI=require('src.pixel_ui')
local mode=assert(os.getenv('ROAG_MENU_MODE'))
local output=assert(os.getenv('ROAG_MENU_OUTPUT'))
local tracked,chest,camera
local new,restore=Game.new_stage,Game.restore
function Game.new_stage(...)
    if mode=='fixture'then
        tracked=Game.new(17,'core:map/scrolling_room')
        tracked.currency=10
        chest=Game.spawn_chest(tracked,'core:chest/standard',4,3)
        return tracked
    end
    tracked=new(...);return tracked
end
function Game.restore(...)tracked=restore(...);return tracked end
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,pending,failed=1,false,false
local before
local stats_text={}
local print_text=love.graphics.print
love.graphics.print=function(text,x,y,...)
    if x==636 and type(y)=='number' and y>=286 and y<460 then
        stats_text[#stats_text+1]=tostring(text)
    end
    return print_text(text,x,y,...)
end
local sequences={
 normal={'title','help','class','class-ranger','settings','settings-kenney','hud','inventory','help-in-game','pause','pause-settings'},
 resume={'continue-summary','continued-hud','continued-stats','replace-confirmation'},
 fixture={'chest','chest-selection','boons','boon-detail','defeat'},
}
local function snapshot()return Codec.encode(Game.snapshot(tracked))end
local function stable()assert(snapshot()==before,'menu action changed simulation, RNG, or save snapshot')end
local function click(x,y,presses)love.mousepressed(x,y,1,false,presses or 1);update(0)end
local function fail(why)
    failed=true;io.stderr:write('MENU '..mode..' FAILED: '..tostring(why)..'\n');love.event.quit(1)
end
local function choose_pack()
    local row=UI.row(145,202,734,54,2,70)
    click(row.x+row.w/2,row.y+10)
    assert(love.roagVisualPackMode()=='kenney')
end
function love.load(...)
    local args={...}
    local ok,why=pcall(function()
        load(unpack(args));love.mouse.setPosition(8,8)
        if mode=='resume'then
            love.keypressed('return') -- open Continue summary, not auto-load
            assert(love.roagUIState().page=="continue")
        elseif mode=='fixture'then
            Game.set_boon_stacks(tracked,tracked.player_id,Boons.order[1],'common',1)
            before=snapshot();love.keypressed('d')
            assert(love.roagMenuState().kind=='chest');stable()
        end
    end)
    if not ok then fail(why)end
end
function love.update()end
local function advance()
    if mode=='normal'then
        if phase==1 then love.keypressed('f1')
        elseif phase==2 then love.keypressed('space');love.keypressed('escape');click(500,286)
        elseif phase==3 then click(300,228)
        elseif phase==4 then love.keypressed('escape');click(500,375)
        elseif phase==5 then choose_pack()
        elseif phase==6 then
            love.keypressed('escape');click(500,286)
            local row=UI.row(190,150,290,32,5,36)
            click(row.x+10,row.y+10);love.keypressed('return');update(0)
            assert(Game.player(tracked).definition_id=='core:actor/sentinel')
            assert(camera.viewport.width==992 and camera.viewport.height==520)
            before=snapshot()
        elseif phase==7 then click(730,611);stable()
        elseif phase==8 then
            love.keypressed('space');love.keypressed('f');stable()
            love.keypressed('i');love.keypressed('f1');stable()
        elseif phase==9 then love.keypressed('escape');click(960,611);stable()
        elseif phase==10 then
            love.keypressed('down');love.keypressed('down');love.keypressed('return');stable()
        else
            love.keypressed('escape');love.keypressed('escape');stable()
            love.keypressed('f5');update(0);stable()
            local f=assert(io.open(output..'/expected-run.txt','wb'));f:write(snapshot());f:close()
            love.keypressed('escape');love.keypressed('2');return
        end
    elseif mode=='resume'then
        if phase==1 then
            love.keypressed('return');update(0)
            local f=assert(io.open(output..'/expected-run.txt','rb'))
            assert(snapshot()==f:read('*a'),'fresh Continue changed the saved run');f:close()
            before=snapshot()
            assert(love.roagVisualPackMode()=='kenney')
            -- Repeated open/close uses the same resources and no gameplay calls.
            local texture_memory=love.graphics.getStats().texturememory
            for _=1,100 do
                love.keypressed('i');love.keypressed('space');love.keypressed('escape')
                love.keypressed('f1');love.keypressed('space');love.keypressed('escape')
            end
            stable()
            assert(love.graphics.getStats().texturememory==texture_memory,
                'menu toggles allocated new textures')
        elseif phase==2 then love.keypressed('i');stable()
        elseif phase==3 then
            love.keypressed('escape');love.keypressed('n');stable()
        else
            love.keypressed('escape');stable();assert(love.roagUIState().page=='game')
            love.event.quit();return end
    else
        if phase==1 then
            click(300,178+76+10)
            assert(love.roagMenuState().selected==2);stable()
        elseif phase==2 then
            love.keypressed('escape');stable();love.keypressed('i');stable()
        elseif phase==3 then love.keypressed('return');stable()
        elseif phase==4 then
            love.keypressed('escape');love.keypressed('i');stable()
            -- Explicit terminal UI fixture; not a legal death/Victory claim.
            tracked.game_over=true;Game.player(tracked).hp=0
        else love.event.quit();return end
    end
    phase=phase+1
end
function love.draw()
    if failed then return end
    local ok,why=pcall(function()
        stats_text={}
        draw()
        if love.roagUIState().page=='inventory'then
            assert(#stats_text==5,'unexpected character stat rows')
            local text=table.concat(stats_text,' ')
            assert(not text:find('Seed') and not text:find('Danger') and
                not text:find('Next threat'),'removed character details still rendered')
        end
        if not pending then
            pending=true
            love.graphics.captureScreenshot(function(image)
                local good,err=pcall(function()
                    local pages={normal={'home','help','class','class','settings','settings','game','inventory','help','pause','settings'},
                        resume={'continue','game','inventory','confirm'},fixture={'chest','chest','inventory','boon-detail','game'}}
                    assert(love.roagUIState().page==pages[mode][phase],
                        'wrong visible menu: '..love.roagUIState().page)
                    PNG.write(output..'/'..mode..'-'..sequences[mode][phase]..'.png',image)
                    advance();pending=false
                end)
                if not good then fail(err)end
            end)
        end
    end)
    if not ok then fail(why)end
end
LUA
for mode in normal resume fixture;do
    if [ "$mode" = fixture ];then autostart=1;else autostart=0;fi
    ROAG_MENU_OUTPUT="$output" ROAG_MENU_MODE="$mode" ROAG_GUI_AUTOSTART="$autostart" \
    XDG_DATA_HOME="$output/data" timeout -k 3s 35s love "$output/app" > "$output/$mode.log" 2>&1 || {
        cat "$output/$mode.log";exit 1;
    }
    printf 'PASS menu %s\n' "$mode"
done
printf 'ROAG menu rendered checks: %s\n' "$output"
