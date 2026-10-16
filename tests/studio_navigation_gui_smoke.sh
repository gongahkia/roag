#!/bin/sh
# Studio navigation, clickable search, artwork status thumbnails, and shared history.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=$(mktemp -d "${TMPDIR:-/tmp}/roag-studio-navigation-XXXXXX")
app="$output/app"
mkdir -p "$app/assets" "$output/data"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/assets/fonts" "$app/assets/fonts"
cp -R "$project_dir/assets/studio" "$app/assets/studio"
cat > "$app/main.lua" <<'LUA'
local D=require('src.studio_document')
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_draw=love.load,love.draw
local real_draw=love.graphics.draw
local output=assert(os.getenv('ROAG_STUDIO_NAV_OUTPUT'))
local phase,pending=1,false
local row_previews=0
love.graphics.draw=function(image,x,y,...)
    if type(x)=='number' and x>=30 and x<52 and
        type(y)=='number' and y>=182 and y<542 then
        row_previews=row_previews+1
    end
    return real_draw(image,x,y,...)
end
local function state() return love.roagStudioState() end
local function click(x,y) love.mousepressed(x,y,1) end
local function slot()
    for _,entry in ipairs(state().artwork) do
        if entry.key=='core:actor/adventurer' then return entry end
    end
end
function love.load(args)
    old_load(args)
    assert(state().mode=='slots' and slot().status=='published elsewhere')
end
function love.draw()
    row_previews=0
    old_draw()
    if pending then return end
    assert(row_previews>0,'mapped artwork thumbnail did not render')
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/phase'..phase..'.png','wb'))
        file:write(image:encode('png'):getString());file:close()
        if phase==1 then
            click(550,90) -- click directly into the search field
            love.textinput('core:actor/adventurer')
            assert(#state().visible_artwork==1)
            click(25,120) -- leave search focus without clearing the query
            love.keypressed('a')
            assert(state().mode=='project')
            click(750,46) -- Back
            assert(state().mode=='slots')
            click(820,46) -- Forward
            assert(state().mode=='project')
            click(645,17) -- Artwork tab
            click(780,17) -- World tab
            assert(state().mode=='world')
            click(750,46) -- Back to Artwork
            assert(state().mode=='slots')
            love.keypressed('return') -- mapped destination: create local replacement
            assert(state().mode=='size')
            for _=1,5 do love.keypressed('backspace') end
            love.textinput('16x16')
            love.keypressed('return')
            assert(state().mode=='canvas')
            local canvas=state().canvas
            local x,y=canvas:origin()
            click(x+3,y+3);love.mousereleased(x+3,y+3,1)
            assert(D.get(canvas.doc,0,0)[4]>0)
            click(900,46) -- shared Undo
            assert(D.get(canvas.doc,0,0)[4]==0)
            click(970,46) -- shared Redo
            assert(D.get(canvas.doc,0,0)[4]>0)
            love.keypressed('s')
            click(750,46) -- Back to Artwork after saving
            assert(state().mode=='slots' and slot().asset)
        else love.event.quit() end
        phase=phase+1;pending=false
    end)
end
LUA
ROAG_STUDIO_NAV_OUTPUT="$output" XDG_DATA_HOME="$output/data" \
    timeout 25s love "$app" --studio --current-project
test -s "$output/phase1.png"
test -s "$output/phase2.png"
printf 'Studio navigation GUI passed: %s\n' "$output"
