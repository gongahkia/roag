#!/bin/sh
# E00 callback/render smoke: two Studio processes, explicit publish, two game processes.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roeg-studio-gui-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roeg_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/examples" "$app/examples"
cp -R "$project_dir/tools" "$app/tools"
cat > "$app/main.lua" <<'LUA'
assert(love.filesystem.load('roeg_main.lua'))()
local original_load,original_draw=love.load,love.draw
local phase=assert(tonumber(os.getenv('ROEG_STUDIO_SMOKE_PHASE')))
local output=assert(os.getenv('ROEG_STUDIO_SMOKE_OUTPUT'))
function love.load(args)
    original_load(args)
    if phase<=2 then
        local s=assert(love.roegStudioState(),'--studio did not select Studio')
        assert(love.window.getTitle():find('Studio',1,true),'Studio window title missing')
        if phase==1 then
            love.keypressed('n');love.textinput('Smoke Project');love.keypressed('return')
            s=love.roegStudioState()
            assert(s.project.id=='smoke_project' and s.dirty)
            love.keypressed('a')
            local sample=assert(love.filesystem.read('examples/studio/adventurer.png'))
            local dropped={getFilename=function()return 'adventurer.png' end,
                open=function()return true end,getSize=function()return #sample end,
                read=function()return sample end,close=function()end}
            love.filedropped(dropped)
            assert(#love.roegStudioState().project.assets==1)
            love.mousepressed(50,175,1)
            assert(love.roegStudioState().selected==1)
            love.keypressed('b')
            love.keypressed('1')
            for _=1,100 do love.keypressed('backspace') end
            love.textinput('Hero Token');love.keypressed('return')
            love.keypressed('2')
            for _=1,100 do love.keypressed('backspace') end
            love.textinput('core:actor/adventurer');love.keypressed('return')
            love.keypressed('5')
            for _=1,100 do love.keypressed('backspace') end
            love.textinput('linear');love.keypressed('return')
            love.keypressed('3')
            for _=1,100 do love.keypressed('backspace') end
            love.textinput('bogus');love.keypressed('return')
            assert(love.roegStudioState().message:find('Edit rejected',1,true))
            assert(love.quit()==true,'unsaved Studio quit was not intercepted')
            assert(love.roegStudioState().mode=='quit_confirm')
            love.keypressed('escape')
            assert(love.roegStudioState().dirty)
            love.keypressed('s')
            s=love.roegStudioState()
            assert(not s.dirty and #s.project.assets==1)
            assert(s.project.assets[1].binding=='core:actor/adventurer')
            assert(s.project.assets[1].id=='core:actor/adventurer' and
                s.project.assets[1].name=='Hero Token' and s.project.assets[1].filter=='linear')
            assert(not love.filesystem.getInfo('active-a.roeg') and
                not love.filesystem.getInfo('profile-a.roeg'),
                'Studio changed gameplay save/profile storage')
            local source='studio/projects/smoke_project/'..s.project.assets[1].source
            assert(love.filesystem.read(source)==love.filesystem.read('examples/studio/adventurer.png'))
            local f=assert(io.open(output..'/save_dir.txt','wb'))
            f:write(love.filesystem.getSaveDirectory());f:close()
            -- Malformed dropped image is rejected, while the verified project remains.
            local bad={getFilename=function()return 'bad.png' end,
                open=function()return true end,getSize=function()return 7 end,
                read=function()return 'bad png' end,close=function()end}
            love.filedropped(bad)
            s=love.roegStudioState()
            assert(s.message:find('Import rejected',1,true) and not s.dirty)
        else
            love.keypressed('o');love.keypressed('return')
            s=love.roegStudioState()
            assert(s.project.id=='smoke_project' and not s.dirty)
            assert(s.project.assets[1].width==16 and s.project.assets[1].height==16)
            assert(s.project.assets[1].binding=='core:actor/adventurer')
            assert(s.project.assets[1].name=='Hero Token' and s.project.assets[1].filter=='linear')
            local source='studio/projects/smoke_project/'..s.project.assets[1].source
            assert(love.filesystem.read(source)==love.filesystem.read('examples/studio/adventurer.png'))
        end
    else
        assert(love.roegStudioState()==nil,'normal launch opened Studio')
        assert(not love.window.getTitle():find('Studio',1,true),'game retained Studio title')
        if phase==3 then
            assert(love.roegAssetStatus()=='loaded' and
                love.roegAssetResolved('core:actor/adventurer'),
                'published visual was not resolved')
        else
            assert(love.roegAssetStatus()=='absent' and
                not love.roegAssetResolved('core:actor/adventurer'),
                'missing visual did not fall back')
        end
    end
end
local captured=false
function love.draw()
    original_draw()
    if captured then return end
    captured=true
    love.graphics.captureScreenshot(function(image)
        local bytes=image:encode('png'):getString()
        local f=assert(io.open(output..'/phase'..phase..'.png','wb'))
        f:write(bytes);f:close()
        love.event.quit()
    end)
end
LUA
run_phase() {
    ROEG_STUDIO_SMOKE_PHASE="$1" ROEG_STUDIO_SMOKE_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/data" ROEG_GUI_AUTOSTART="${2:-0}" \
        timeout 20s love "$app" ${3:-} > "$output_dir/phase$1.log" 2>&1
    test -s "$output_dir/phase$1.png"
}
run_phase 1 0 --studio
run_phase 2 0 --studio
save_dir=$(cat "$output_dir/save_dir.txt")
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" --project smoke_project \
    > "$output_dir/publish-dry-run.log")
test ! -e "$app/assets/studio/manifest.txt"
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" --project smoke_project \
    --apply > "$output_dir/publish.log")
test -s "$app/assets/studio/manifest.txt"
run_phase 3 1
mv "$app/assets/studio/manifest.txt" "$app/assets/studio/manifest.disabled"
run_phase 4 1
if cmp -s "$output_dir/phase3.png" "$output_dir/phase4.png"; then
    echo 'Published sprite and fallback rendered identical frames' >&2
    exit 1
fi
printf 'Studio GUI smoke passed: fresh-process reopen, import, publish and fallback in %s\n' "$output_dir"
