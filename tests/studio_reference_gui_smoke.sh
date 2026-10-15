#!/bin/sh
# Copy a Studio sprite, paint a pose, and reopen its presentation-only reference.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-reference-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/examples" "$app/examples"
cat > "$app/main.lua" <<'LUA'
assert(love.filesystem.load('roag_main.lua'))()
local original_load,original_draw,original_update=love.load,love.draw,love.update
local D=require('src.studio_document')
local phase=assert(tonumber(os.getenv('ROAG_REFERENCE_PHASE')))
local output=assert(os.getenv('ROAG_REFERENCE_OUTPUT'))
local function state() return assert(love.roagStudioState()) end
local function press(k) love.keypressed(k) end
local function replace_field(value)
    for _=1,100 do press('backspace') end
    love.textinput(value);press('return')
end
local function pixel(doc,x,y)
    return table.concat(D.get(doc,x,y),',')
end
function love.load(args)
    original_load(args)
    if phase==1 then
        press('n');love.textinput('Reference Test');press('return')
        press('i');press('b');press('s')
        local source=state().project.assets[1]
        assert(source.binding=='core:actor/adventurer')
        local original_bytes=assert(love.filesystem.read('studio/projects/reference_test/'..source.source))
        press('d')
        local s=state();local copy=s.project.assets[s.selected]
        assert(s.mode=='canvas' and copy.id~=source.id and copy.reference_id==source.id)
        assert(s.canvas.reference_visible and s.canvas.reference_name==source.name)
        assert(D.flatten(s.canvas.doc)==s.canvas.reference_raw)
        press('g');assert(not s.canvas.reference_visible)
        press('g');assert(s.canvas.reference_visible)
        press('h');replace_field('FF0000FF')
        local ox,oy=s.canvas:origin()
        local x,y=ox+s.canvas.zoom/2,oy+s.canvas.zoom/2
        love.mousepressed(x,y,1);love.mousereleased(x,y,1)
        assert(pixel(s.canvas.doc,0,0)=='255,0,0,255')
        press('s');assert(not state().dirty)
        press('escape');press('4');replace_field('core:actor/adventurer/hurt');press('s')
        assert(not state().dirty and copy.document~=source.document)
        assert(love.filesystem.read('studio/projects/reference_test/'..source.source)==original_bytes,
            'copy changed imported source PNG')
        press('p');press('d')
        assert(state().publish_preview and state().publish_preview.counts.create==3)
        press('a');press('return')
        assert(state().mode=='pipeline' and not state().publish_preview,
            'Studio did not publish the copied pose')
        local file=assert(io.open(love.filesystem.getSource()..'/assets/studio/manifest.txt','rb'))
        local published=require('src.studio_project').decode(file:read('*a'))
        file:close()
        local bindings={}
        for _,asset in ipairs(published.assets) do
            assert(asset.reference_id=='','editor reference leaked into published manifest')
            bindings[asset.binding]=true
        end
        assert(bindings['core:actor/adventurer'] and bindings['core:actor/adventurer/hurt'])
    else
        press('o');press('return')
        local s=state();assert(s.mode=='project' and #s.project.assets==2)
        for i,a in ipairs(s.project.assets) do
            if a.binding=='core:actor/adventurer/hurt' then s.selected=i end
        end
        press('e');s=state()
        assert(s.mode=='canvas' and s.canvas.reference_visible and s.canvas.reference_raw)
        assert(pixel(s.canvas.doc,0,0)=='255,0,0,255')
        assert(not s.canvas:dirty())
    end
end
local pending=false
local on_pixel
local function canvas_pixel(image)
    local canvas=state().canvas
    local ox,oy=canvas:origin()
    local scale_x=image:getWidth()/love.graphics.getWidth()
    local scale_y=image:getHeight()/love.graphics.getHeight()
    return {image:getPixel(math.floor((ox+8.5*canvas.zoom)*scale_x),
        math.floor((oy+6.5*canvas.zoom)*scale_y))}
end
function love.draw()
    original_draw()
    if phase~=2 or pending then return end
    pending=true
    local path=output..'/reference-on.tga'
    love.graphics.captureScreenshot(function(image)
        on_pixel=canvas_pixel(image)
        local file=assert(io.open(path,'wb'))
        assert(file:write(image:encode('tga'):getString()));assert(file:close())
    end)
end
local stage=0
function love.update(dt)
    original_update(dt)
    stage=stage+1
    if phase==1 and stage>=3 then love.event.quit() end
    if phase==2 and stage==4 then
        press('g');assert(not state().canvas.reference_visible)
        function love.draw()
            original_draw()
            love.graphics.captureScreenshot(function(image)
                local off_pixel=canvas_pixel(image)
                assert(on_pixel and (math.abs(on_pixel[1]-off_pixel[1])>0.01 or
                    math.abs(on_pixel[2]-off_pixel[2])>0.01 or
                    math.abs(on_pixel[3]-off_pixel[3])>0.01),
                    'reference overlay did not change a sprite pixel')
                local file=assert(io.open(output..'/reference-off.tga','wb'))
                assert(file:write(image:encode('tga'):getString()));assert(file:close())
                love.event.quit()
            end)
        end
    end
end
LUA
ROAG_REFERENCE_PHASE=1 ROAG_REFERENCE_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
    timeout 25s love "$app" --studio >"$output_dir/phase1.log" 2>&1
ROAG_REFERENCE_PHASE=2 ROAG_REFERENCE_OUTPUT="$output_dir" XDG_DATA_HOME="$output_dir/data" \
    timeout 25s love "$app" --studio >"$output_dir/phase2.log" 2>&1
test -s "$output_dir/reference-on.tga" && test -s "$output_dir/reference-off.tga"
if cmp -s "$output_dir/reference-on.tga" "$output_dir/reference-off.tga"; then
    echo 'Reference overlay did not change the canvas rendering' >&2;exit 1
fi
printf 'Studio reference GUI smoke passed: independent copy, saved overlay, fresh reopen, visual toggle (%s)\n' "$output_dir"
