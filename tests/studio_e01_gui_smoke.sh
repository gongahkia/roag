#!/bin/sh
# Callback-driven E01 drawing, fresh-process reopen, lossless publish and fallback.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roeg-studio-e01-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roeg_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/examples" "$app/examples"
cp -R "$project_dir/tools" "$app/tools"
cat > "$app/main.lua" <<'LUA'
assert(love.filesystem.load('roeg_main.lua'))()
local original_load,original_draw,original_update=love.load,love.draw,love.update
local original_mousemoved=love.mousemoved
-- A scripted press is not an OS press. Ignore unrelated WSLg cursor motion
-- until the matching scripted release, while still testing the real canvas
-- mouse callback for every intended drag segment.
local scripted_stroke=false
function love.mousemoved(x,y,dx,dy)
    if scripted_stroke then return end
    return original_mousemoved(x,y,dx,dy)
end
local function scripted_move(x,y,dx,dy)
    return original_mousemoved(x,y,dx,dy)
end
local phase=assert(tonumber(os.getenv('ROEG_E01_PHASE')))
local output=assert(os.getenv('ROEG_E01_OUTPUT'))
-- Screenshot callbacks only persist frames; editor inputs run in the next update.
-- LÖVE/driver rendering may still own the captured image inside the callback.
local shot,pending,queued_action=0,false,nil
local function mark(label)
    local file=assert(io.open(output..'/trace-phase'..phase..'.log','ab'))
    file:write(label..'\n');file:close()
end
local function state() return assert(love.roegStudioState()) end
local function pixel_xy(px,py)
    local c=assert(state().canvas)
    local x,y=c:origin();return x+px*c.zoom+math.floor(c.zoom/2),y+py*c.zoom+math.floor(c.zoom/2)
end
local function paint(px,py)
    local x,y=pixel_xy(px,py)
    love.mousepressed(x,y,1);love.mousereleased(x,y,1)
end
local function color(hex)
    love.keypressed('h');for _=1,8 do love.keypressed('backspace') end
    love.textinput(hex);love.keypressed('return')
end
local function check_png(path,expected)
    local bytes=assert(love.filesystem.read(path))
    local data=love.image.newImageData(love.filesystem.newFileData(bytes,'painted.png'))
    assert(data:getWidth()==16 and data:getHeight()==16)
    local r,g,b,a=data:getPixel(0,0)
    local function b8(v)return math.floor(v*255+0.5)end
    assert(b8(r)==expected[1] and b8(g)==expected[2] and b8(b)==expected[3] and b8(a)==expected[4],
        ('PNG pixel mismatch: %d,%d,%d,%d'):format(b8(r),b8(g),b8(b),b8(a)))
    local _,_,_,edge_alpha=data:getPixel(15,15)
    assert(b8(edge_alpha)==0,'transparent opposite edge changed')
    return data
end
function love.load(args)
    original_load(args)
    if phase==1 then
        assert(state().mode=='home')
        love.keypressed('n');love.textinput('Paint Project');love.keypressed('return')
        love.mousepressed(550,86,1);for _=1,5 do love.keypressed('backspace') end
        love.textinput('513x16');love.keypressed('return')
        assert(state().mode=='size' and state().message:find('Invalid sprite size',1,true))
        for _=1,6 do love.keypressed('backspace') end
        love.textinput('16x16');love.keypressed('return')
        assert(state().mode=='canvas' and state().canvas.doc.width==16)
        color('FF0000FF')
    elseif phase==2 then
        mark('open-start')
        love.keypressed('o');love.keypressed('return');love.keypressed('e')
        mark('open-done')
        local s=state();assert(s.mode=='canvas' and s.canvas and #s.canvas.doc.layers==2,
            ('E01 reopen: mode=%s message=%s assets=%s'):format(tostring(s.mode),
                tostring(s.message),tostring(s.project and #s.project.assets)))
        local D=require('src.studio_document')
        assert(D.get(s.canvas.doc,0,0,'layer_1')[1]==255)
        assert(D.get(s.canvas.doc,0,0,'layer_2')[3]==255)
        check_png('studio/projects/paint_project/'..s.project.assets[1].derived,{127,0,128,255})
        mark('png-checked')
        -- Select the lower layer, erase one pixel, save a new complete generation.
        love.mousepressed(love.graphics.getWidth()-220,380,1)
        love.keypressed('e');paint(5,0)
        assert(D.get(s.canvas.doc,5,0,'layer_1')[4]==0)
        love.mousepressed(love.graphics.getWidth()-210,72,1)
        assert(D.get(s.canvas.doc,5,0,'layer_1')[4]==255,'clickable Undo failed')
        love.mousepressed(love.graphics.getWidth()-105,72,1)
        mark('erase-undo-redo')
        assert(D.get(s.canvas.doc,5,0,'layer_1')[4]==0,'clickable Redo failed')
        love.keypressed('i');paint(1,0)
        assert(s.canvas.color[1]==255 and s.canvas.color[4]==255,'eyedropper failed')
        love.keypressed('f');paint(10,10)
        mark('fill-done')
        assert(D.get(s.canvas.doc,10,10,'layer_1')[1]==255,'GUI flood fill failed')
        love.mousepressed(love.graphics.getWidth()-210,72,1)
        assert(D.get(s.canvas.doc,10,10,'layer_1')[4]==0)
        local cx,cy=pixel_xy(2,2)
        local before=s.canvas.zoom
        -- This callback-driven harness supplies the pointer fact that the
        -- real wheel callback reads. WSLg cannot reliably warp the OS cursor.
        local read_cursor=love.mouse.getPosition
        love.mouse.getPosition=function()return cx,cy end
        local wheel_ok,wheel_error=pcall(love.wheelmoved,0,1)
        love.mouse.getPosition=read_cursor
        assert(wheel_ok,wheel_error)
        assert(s.canvas.zoom>before and select(1,s.canvas:pixel_at(cx,cy))==2,
            ('zoom anchor drifted: zoom=%s old=%s cursor=%s,%s'):format(
                s.canvas.zoom,before,cx,cy))
        local oldx=s.canvas:origin()
        love.mousepressed(cx,cy,3);love.mousemoved(cx+24,cy+12,24,12)
        love.mousereleased(cx+24,cy+12,3)
        assert(s.canvas:origin()==oldx+24,'middle-mouse pan failed')
        love.window.setMode(1120,680,{resizable=true})
        mark('resize-done')
        local rx,ry=s.canvas:rect();assert(rx==210 and ry==100)
        love.keypressed('p')
        local stroke_x,stroke_y=pixel_xy(4,4)
        love.mousepressed(stroke_x,stroke_y,1)
        love.keypressed('s');assert(not state().dirty)
        mark('save-done')
        assert(D.get(s.canvas.doc,4,4,'layer_1')[4]==255,
            'save during active stroke did not commit pixels')
        love.mousereleased(stroke_x,stroke_y,1)
        local painted=check_png('studio/projects/paint_project/'..s.project.assets[1].derived,{127,0,128,255})
        local _,_,_,erased=painted:getPixel(5,0)
        assert(math.floor(erased*255+0.5)==0,'eraser did not reach exported PNG')
        local f=assert(io.open(output..'/save_dir.txt','wb'))
        f:write(love.filesystem.getSaveDirectory());f:close()
    elseif phase==3 then
        love.keypressed('o');love.keypressed('return');love.keypressed('e')
        local s=state();assert(s.project.id=='paint_project' and #s.canvas.doc.layers==2)
        local D=require('src.studio_document')
        assert(D.get(s.canvas.doc,5,0,'layer_1')[4]==0)
        assert(D.get(s.canvas.doc,4,4,'layer_1')[4]==255)
        -- Import a separate PNG, initialize editable pixels, paint, then save.
        love.keypressed('escape');love.keypressed('n')
        love.textinput('Import Project');love.keypressed('return')
        love.keypressed('i');love.mousepressed(650,86,1)
        assert(state().mode=='canvas')
        color('00FF00FF');paint(0,0);love.keypressed('s')
        local a=state().project.assets[1]
        assert(a.source~='' and a.document~='' and a.derived~='')
        assert(love.filesystem.read('studio/projects/import_project/'..a.source)==
            love.filesystem.read('examples/studio/adventurer.png'),'original import changed')
    elseif phase==4 then
        love.keypressed('o');love.keypressed('return');love.keypressed('e')
        local s=state();assert(s.project.id=='import_project' and s.canvas.doc.width==16)
        local D=require('src.studio_document')
        assert(D.get(s.canvas.doc,0,0)[2]==255)
        local a=s.project.assets[1]
        local data=love.image.newImageData(love.filesystem.newFileData(
            assert(love.filesystem.read('studio/projects/import_project/'..a.derived)),'import-painted.png'))
        assert(math.floor(select(2,data:getPixel(0,0))*255+0.5)==255)
        color('1122CCFF')
        love.keypressed('m')
        local x0,y0=pixel_xy(0,0);local x1,y1=pixel_xy(2,2)
        scripted_stroke=true
        love.mousepressed(x0,y0,1);scripted_move(x1,y1,x1-x0,y1-y0)
        love.mousereleased(x1,y1,1);scripted_stroke=false
        assert(s.canvas.selection.x0==0 and s.canvas.selection.x1==2)
        love.keypressed('c')
        assert(D.get(s.canvas.doc,0,0)[3]==204,'GUI selection recolor failed')
        love.mousepressed(love.graphics.getWidth()-210,72,1)
        assert(D.get(s.canvas.doc,0,0)[2]==255,'GUI selection undo failed')
        love.mousepressed(love.graphics.getWidth()-105,72,1)
        assert(D.get(s.canvas.doc,0,0)[3]==204,'GUI selection redo failed')
        love.keypressed('delete')
        assert(D.get(s.canvas.doc,0,0)[4]==0,'GUI selection clear failed')
        love.mousepressed(love.graphics.getWidth()-210,72,1)
        assert(D.get(s.canvas.doc,0,0)[4]==255,'GUI selection clear undo failed')
        love.keypressed('o');for _=1,8 do love.keypressed('backspace') end
        love.textinput('AABBCCFF');love.keypressed('return')
        assert(s.canvas.outline_color=='AABBCCFF')
        love.keypressed('s')
        local outlined=assert(love.filesystem.read('studio/projects/import_project/'..a.derived))
        local ow,oh=require('src.studio_project').png_dimensions(outlined)
        assert(ow==32 and oh==32,'outlined export is not 2x')
        assert(not love.filesystem.getInfo('active-a.roeg') and
            not love.filesystem.getInfo('profile-a.roeg'),
            'Studio touched gameplay persistence')
    elseif phase==7 then
        love.keypressed('o');love.keypressed('return');love.keypressed('e')
        local s=state();assert(s.project.id=='import_project')
        assert(s.canvas.outline_color=='AABBCCFF' and s.canvas.doc.width==16)
        local a=s.project.assets[1]
        local bytes=assert(love.filesystem.read('studio/projects/import_project/'..a.derived))
        local image=love.image.newImageData(love.filesystem.newFileData(bytes,'outlined.png'))
        assert(image:getWidth()==32 and image:getHeight()==32)
    else
        assert(love.roegStudioState()==nil)
        if phase==5 then assert(love.roegAssetStatus()=='loaded' and love.roegAssetResolved('core:actor/adventurer'))
        else assert(love.roegAssetStatus()=='absent' and not love.roegAssetResolved('core:actor/adventurer')) end
    end
end
function love.update(dt)
    original_update(dt)
    if queued_action then
        local action=queued_action;queued_action=nil
        action();pending=false
    end
end
function love.draw()
    original_draw()
    if pending then return end
    if phase==1 and shot<3 then
        pending=true
        local label=({'before','mid','after'})[shot+1]
        love.graphics.captureScreenshot(function(img)
            local bytes=img:encode('tga'):getString()
            local f=assert(io.open(output..'/'..label..'.tga','wb'));f:write(bytes);f:close()
            shot=shot+1
            if shot==1 then
                queued_action=function()
                    local x,y=pixel_xy(0,0);local tx,ty=pixel_xy(3,0)
                    local c=state().canvas
                    assert(select(1,c:pixel_at(x,y))==0)
                    scripted_stroke=true
                    love.mousepressed(x,y,1)
                    local D=require('src.studio_document')
                    assert(D.get(c.doc,0,0)[4]==255,'source pixel did not paint on press')
                    scripted_move(tx,ty,tx-x,ty-y)
                    assert(D.get(c.doc,3,0)[4]==255,'drag interpolation did not paint')
                end
            elseif shot==2 then
                mark('mid-callback')
                queued_action=function()
                    local tx,ty=pixel_xy(5,0)
                    scripted_move(tx,ty,0,0)
                    love.mousereleased(tx,ty,1)
                    scripted_stroke=false
                    local D=require('src.studio_document')
                    assert(D.get(state().canvas.doc,5,0,'layer_1')[4]==255,
                        'stroke did not reach fifth pixel')
                    mark('stroke-complete')
                    assert(love.quit()==true and state().mode=='quit_confirm')
                    love.keypressed('escape')
                    assert(state().mode=='canvas' and state().dirty,
                        'canceling quit lost the unsaved canvas')
                    mark('quit-confirm')
                    love.keypressed('l');color('0000FF80');paint(0,0)
                    local s=state();assert(#s.canvas.doc.layers==2,
                        ('layer add failed: mode=%s layers=%d message=%s'):format(
                            tostring(s.mode),#s.canvas.doc.layers,tostring(s.message)))
                    love.keypressed('s');assert(not state().dirty)
                    mark('save-complete')
                    local a=s.project.assets[1]
                    check_png('studio/projects/paint_project/'..a.derived,{127,0,128,255})
                    love.keypressed('escape');love.keypressed('b')
                    love.keypressed('2');for _=1,100 do love.keypressed('backspace') end
                    love.textinput('core:actor/adventurer');love.keypressed('return')
                    love.keypressed('s')
                    assert(state().project.assets[1].binding=='core:actor/adventurer')
                    assert(state().project.assets[1].id=='core:actor/adventurer')
                    mark('metadata-complete')
                end
            else queued_action=function() love.event.quit() end end
        end)
    elseif shot==0 then
        shot=1
        love.graphics.captureScreenshot(function(img)
            local bytes=img:encode('tga'):getString()
            local f=assert(io.open(output..'/phase'..phase..'.tga','wb'));f:write(bytes);f:close()
            love.event.quit()
        end)
    end
end
LUA
run_phase() {
    ROEG_E01_PHASE="$1" ROEG_E01_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/data" ROEG_GUI_AUTOSTART="${2:-0}" \
        timeout 25s love "$app" ${3:-} > "$output_dir/phase$1.log" 2>&1
}
run_phase 1 0 --studio
run_phase 2 0 --studio
run_phase 3 0 --studio
run_phase 4 0 --studio
run_phase 7 0 --studio
for frame in before mid after; do test -s "$output_dir/$frame.tga"; done
if cmp -s "$output_dir/before.tga" "$output_dir/mid.tga" || cmp -s "$output_dir/mid.tga" "$output_dir/after.tga"; then
    echo 'Canvas did not change across actual rendered drawing frames' >&2;exit 1
fi
save_dir=$(cat "$output_dir/save_dir.txt")
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" --project paint_project \
    > "$output_dir/publish-dry-run.log")
test ! -e "$app/assets/studio/manifest.txt"
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" --project paint_project \
    --apply > "$output_dir/publish.log")
run_phase 5 1
mv "$app/assets/studio/manifest.txt" "$app/assets/studio/manifest.disabled"
run_phase 6 1
test -s "$output_dir/phase5.tga" && test -s "$output_dir/phase6.tga"
if cmp -s "$output_dir/phase5.tga" "$output_dir/phase6.tga"; then
    echo 'Painted game sprite and fallback rendered identical frames' >&2;exit 1
fi
printf 'Studio E01 GUI smoke passed: paint, selection, outline, fresh reopen, PNG, publish and fallback in %s\n' "$output_dir"
