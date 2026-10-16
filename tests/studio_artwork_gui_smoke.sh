#!/bin/sh
# Current-checkout Studio landing, slot creation and fresh-process reopen.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-artwork-studio-XXXXXX")
app="$output/app"
mkdir -p "$app" "$output/data"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
mkdir -p "$app/assets"
cp -R "$project_dir/assets/fonts" "$app/assets/fonts"
cp -R "$project_dir/examples" "$app/examples"
cat > "$app/main.lua" <<'LUA'
assert(love.filesystem.load('roag_main.lua'))()
local original_load,original_draw=love.load,love.draw
local output=assert(os.getenv('ROAG_ARTWORK_OUTPUT'))
local process=assert(os.getenv('ROAG_ARTWORK_PROCESS'))
local frame=0
local function entry(state,id)
    for _,item in ipairs(state.artwork) do if item.key==id then return item end end
end
function love.load(args)
    original_load(args)
    local state=love.roagStudioState()
    assert(state.mode=='slots' and state.project.id=='roag',
        'current checkout did not open its ROAG project checklist')
    assert(#state.artwork>500 and entry(state,'theme:hp_icon') and
        entry(state,'world:studio:tile/floor'))
    if process=='reopen' then
        local slot=assert(entry(state,'core:actor/adventurer'))
        assert(slot.status=='drawn, unpublished' and slot.asset.document~='')
        local bytes=assert(love.filesystem.read('studio/projects/roag/'..slot.asset.derived))
        local image=love.image.newImageData(love.filesystem.newFileData(bytes,'painted.png'))
        local r,g,b,a=image:getPixel(0,0)
        assert(r>0.99 and g>0.99 and b>0.99 and a>0.99,
            'painted pixel lost across LÖVE processes')
    else
        assert(entry(state,'core:actor/adventurer').status=='missing')
    end
end
function love.draw()
    original_draw()
    frame=frame+1
    if frame==1 then
        love.graphics.captureScreenshot(function(image)
            local file=assert(io.open(output..'/'..process..'.tga','wb'))
            file:write(image:encode('tga'):getString());file:close()
        end)
        if process=='reopen' then love.event.quit();return end
        love.keypressed('slash')
        love.textinput('core:actor/adventurer')
        love.keypressed('return')
        assert(#love.roagStudioState().visible_artwork==1)
        love.keypressed('tab') -- All includes the optional attack/hurt poses.
        assert(#love.roagStudioState().visible_artwork==3)
        love.keypressed('return')
        assert(love.roagStudioState().mode=='size')
        for _=1,5 do love.keypressed('backspace') end
        love.textinput('16x16')
        love.keypressed('return')
        local state=love.roagStudioState()
        assert(state.mode=='canvas' and state.canvas.doc.width==16)
        local x,y=state.canvas:origin()
        love.mousepressed(x+3,y+3,1)
        love.mousereleased(x+3,y+3,1)
        love.keypressed('s')
        love.keypressed('escape')
        state=love.roagStudioState()
        assert(state.mode=='slots' and not state.dirty)
        assert(entry(state,'core:actor/adventurer').status=='drawn, unpublished')
    elseif frame>=2 then love.event.quit() end
end
LUA
ROAG_ARTWORK_OUTPUT="$output" ROAG_ARTWORK_PROCESS=create \
    XDG_DATA_HOME="$output/data" timeout 25s love "$app" --studio --current-project \
    >"$output/create.log" 2>&1
ROAG_ARTWORK_OUTPUT="$output" ROAG_ARTWORK_PROCESS=reopen \
    XDG_DATA_HOME="$output/data" timeout 25s love "$app" --studio --current-project \
    >"$output/reopen.log" 2>&1
test -s "$output/create.tga"
test -s "$output/reopen.tga"
printf 'Artwork checklist GUI passed: %s\n' "$output"
