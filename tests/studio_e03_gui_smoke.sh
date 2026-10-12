#!/bin/sh
# Isolated callback-driven E03 author -> reopen -> publish -> game render/fallback.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roeg-studio-e03-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roeg_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/examples" "$app/examples"
cp -R "$project_dir/tools" "$app/tools"
cat > "$app/main.lua" <<'LUA'
local Game=require('src.game')
local Persistence=require('src.persistence')
local tracked
local old_new,old_restore,old_submit=Game.new_stage,Game.restore,Game.submit
Game.new_stage=function(...) tracked=old_new(...);return tracked end
Game.restore=function(...) tracked=old_restore(...);return tracked end
Game.submit=function(s,...) tracked=s;return old_submit(s,...) end
assert(love.filesystem.load('roeg_main.lua'))()
local original_load,original_update,original_draw=love.load,love.update,love.draw
local phase=assert(tonumber(os.getenv('ROEG_E03_PHASE')))
local output=assert(os.getenv('ROEG_E03_OUTPUT'))
local shot,pending,queued=0,false,nil
local function state() return assert(love.roegStudioState()) end
local function replace_text(value)
    for _=1,120 do love.keypressed('backspace') end
    love.textinput(value);love.keypressed('return')
end
local function cell(x,y)
    local e=assert(state().world_editor)
    local ox,oy=e:origin()
    return ox+(x-0.5)*e.zoom,oy+(y-0.5)*e.zoom
end
local function click_cell(x,y)
    local px,py=cell(x,y)
    love.mouse.setPosition(px,py)
    love.mousepressed(px,py,1);love.mousereleased(px,py,1)
end
local function capture(label,next_action)
    pending=true
    love.graphics.captureScreenshot(function(img)
        local f=assert(io.open(output..'/'..label..'.tga','wb'))
        f:write(img:encode('tga'):getString());f:close()
        shot=shot+1;queued=next_action
    end)
end
local function snapshot_file(name,snapshot)
    local f=assert(io.open(output..'/'..name,'wb'))
    f:write(Persistence.encode(snapshot));f:close()
end
local function read_file(name)
    local f=assert(io.open(output..'/'..name,'rb'))
    local bytes=f:read('*a');f:close();return bytes
end
function love.load(args)
    original_load(args)
    if phase==1 then
        assert(state().mode=='home')
        love.keypressed('n');love.textinput('World Fixture');love.keypressed('return')
        love.keypressed('c');replace_text('16x16')
        local c=assert(state().canvas)
        local ox,oy=c:origin();local x=ox+c.zoom/2;local y=oy+c.zoom/2
        love.mouse.setPosition(x,y);love.mousepressed(x,y,1)
        love.mousemoved(x+c.zoom*2,y,c.zoom*2,0)
        love.mousereleased(x+c.zoom*2,y,1)
        local D=require('src.studio_document')
        assert(D.get(c.doc,0,0)[4]==255 and D.get(c.doc,2,0)[4]==255)
        love.keypressed('s');love.keypressed('escape')
        love.keypressed('3');replace_text('tile')
        love.keypressed('2');replace_text('studio:tile/moss')
        love.keypressed('s')
        assert(state().project.assets[1].kind=='tile')
        love.keypressed('w');assert(state().mode=='world')
        love.keypressed('n');replace_text('9x7')
        local e=assert(state().world_editor)
        assert(e:room().width==9)
        love.keypressed('i')
        assert(e.pack.tiles[1].asset_id=='studio:tile/moss')
        love.keypressed('5');click_cell(3,3)
        assert(e:room().tile_cells['3,3']=='studio:tile/floor')
        for _=1,4 do love.keypressed('r') end
        for _=1,19 do love.keypressed('=') end
        assert(e:room().roles[1]=='start' and e:room().weight==20)
        love.keypressed('2')
        local x1,y1=cell(3,4);local x2,y2=cell(3,5)
        love.mouse.setPosition(x1,y1);love.mousepressed(x1,y1,1)
        love.mousemoved(x2,y2,x2-x1,y2-y1);love.mousereleased(x2,y2,1)
        love.keypressed('8');click_cell(3,1)
        love.keypressed('9');click_cell(4,5)
        love.keypressed('0');click_cell(6,5)
        love.keypressed('v')
        assert(e.message:find('VALID',1,true),e.message)
        love.keypressed('s');assert(not state().dirty)
        assert(not love.filesystem.getInfo('active-a.roeg') and
            not love.filesystem.getInfo('profile-a.roeg'),
            'Studio world editing touched player persistence')
        local f=assert(io.open(output..'/save_dir.txt','wb'))
        f:write(love.filesystem.getSaveDirectory());f:close()
    elseif phase==2 then
        love.keypressed('o');love.keypressed('return');love.keypressed('w')
        local s=state();local e=assert(s.world_editor)
        assert(s.project.id=='world_fixture' and #e.pack.rooms==1)
        assert(e:room().roles[1]=='start' and e:room().sockets[1].x==3)
        assert(e:room().rows[4]:sub(3,3)=='#')
        assert(e:room().tile_cells['3,3']=='studio:tile/floor')
        local a=s.project.assets[1]
        local doc=assert(require('src.studio_store').new(require('src.studio_love_storage')):
            document(s.project,a))
        assert(require('src.studio_document').get(doc,0,0)[4]==255)
        love.keypressed('g')
        assert(e.generated and e.generated.rooms[1].template_id=='studio:room/room_001',
            e.message)
        local visual=false
        for _,id in pairs(e.generated.studio_visuals or {}) do
            if id=='studio:tile/moss' then visual=true end
        end
        assert(visual,'generator lost stable tile asset ID')
        love.keypressed('escape');love.keypressed('escape')
        assert(state().mode=='project')
        love.keypressed('2');replace_text('studio:tile/renamed')
        assert(state().project.assets[1].id=='studio:tile/moss' and
            state().message:find('used by Studio tile',1,true),
            'asset ID edit broke authored room reference')
        love.keypressed('w');assert(state().mode=='world')
    elseif phase==5 then
        assert(love.roegWorldPackStatus()=='loaded')
        love.keypressed('n')
        assert(tracked and tracked.generated_map.rooms[1].template_id==
            'studio:room/room_001')
        love.keypressed('space')
        assert(tracked.clock==100,'clock after wait: '..tostring(tracked.clock))
        local snapshot=Game.snapshot(tracked)
        snapshot_file('authored-save.snapshot',snapshot)
        local control=old_restore(snapshot)
        assert(Game.submit(control,{kind='wait'}))
        snapshot_file('authored-next.snapshot',Game.snapshot(control))
        love.keypressed('escape')
    elseif phase==6 then
        assert(love.roegWorldPackStatus()=='loaded')
        love.keypressed('c')
        assert(tracked and Persistence.encode(Game.snapshot(tracked))==
            read_file('authored-save.snapshot'),
            'fresh-process authored Continue changed authoritative state')
        love.keypressed('space')
        assert(Persistence.encode(Game.snapshot(tracked))==
            read_file('authored-next.snapshot'),
            'authored floor continuation differs after disk restore')
    else
        assert(love.roegStudioState()==nil)
        local G=require('src.generation')
        local map=G.generate(1)
        if phase==3 then
            assert(love.roegWorldPackStatus()=='loaded')
            assert(love.roegAssetIdResolved('studio:tile/moss'))
            assert(map.rooms[1].template_id=='studio:room/room_001')
            local visual=false
            for _,id in pairs(map.studio_visuals or {}) do
                if id=='studio:tile/moss' then visual=true end
            end
            assert(visual,'normal game floor lacks authored tile visual')
        else
            assert(love.roegWorldPackStatus()=='absent')
            assert(map.rooms[1].template_id~='studio:room/room_001')
        end
    end
end
function love.update(dt)
    original_update(dt)
    if queued then local action=queued;queued=nil;action();pending=false end
end
function love.draw()
    original_draw()
    if pending then return end
    if phase==1 and shot==0 then
        capture('editor',function()
            local e=state().world_editor
            local px,py=cell(3,3);love.mouse.setPosition(px,py)
            local before=e.zoom;love.wheelmoved(0,1)
            assert(e.zoom>before and select(1,e:cell_at(px,py))==3)
            local ox=e:origin()
            love.mousepressed(px,py,3);love.mousemoved(px+20,py+10,20,10)
            love.mousereleased(px+20,py+10,3)
            assert(e:origin()==ox+20,'panning changed cell mapping')
            love.window.setMode(1120,680,{resizable=true})
            love.keypressed('o')
        end)
    elseif phase==1 and shot==1 then
        capture('overlay',function() love.keypressed('p') end)
    elseif phase==1 and shot==2 then
        capture('transform',function()
            for _=1,3 do love.keypressed('p') end
            love.keypressed('g')
            assert(state().world_editor.generated,
                'real biome preview failed: '..tostring(state().world_editor.message))
        end)
    elseif phase==1 and shot==3 then
        capture('generated',function() love.event.quit() end)
    elseif shot==0 then
        if phase==5 then capture('phase5',function() love.keypressed('2') end);return end
        capture('phase'..phase,function() love.event.quit() end)
    end
end
LUA
run_phase() {
    ROEG_E03_PHASE="$1" ROEG_E03_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/data" ROEG_GUI_AUTOSTART="${2:-0}" \
        timeout 25s love "$app" ${3:-} > "$output_dir/phase$1.log" 2>&1
}
run_phase 1 0 --studio
run_phase 2 0 --studio
save_dir=$(cat "$output_dir/save_dir.txt")
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" \
    --project world_fixture > "$output_dir/visual-dry-run.log")
(cd "$app" && luajit tools/studio_world_publish.lua --save-dir "$save_dir" \
    --project world_fixture > "$output_dir/world-dry-run.log")
test ! -e "$app/assets/studio/manifest.txt"
test ! -e "$app/assets/studio/world-a.pack"
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" \
    --project world_fixture --apply > "$output_dir/visual-publish.log")
(cd "$app" && luajit tools/studio_world_publish.lua --save-dir "$save_dir" \
    --project world_fixture --apply > "$output_dir/world-publish.log")
run_phase 3 1
mv "$app/assets/studio/world-a.pack" "$app/assets/studio/world-disabled.pack"
run_phase 4 1
mv "$app/assets/studio/world-disabled.pack" "$app/assets/studio/world-a.pack"
run_phase 5 0
run_phase 6 0
for frame in editor overlay transform generated phase2 phase3 phase4 phase5 phase6; do test -s "$output_dir/$frame.tga"; done
if cmp -s "$output_dir/editor.tga" "$output_dir/overlay.tga" ||
    cmp -s "$output_dir/overlay.tga" "$output_dir/transform.tga"; then
    echo 'Room overlay or transform did not change rendered frames' >&2;exit 1
fi
if cmp -s "$output_dir/phase3.tga" "$output_dir/phase4.tga"; then
    echo 'Published room and built-in fallback rendered identical frames' >&2;exit 1
fi
printf 'Studio E03 GUI smoke passed: edit/reopen/publish/generated/game/fallback in %s\n' "$output_dir"
