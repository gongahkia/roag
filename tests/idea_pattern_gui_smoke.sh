#!/bin/sh
# Assisted inventory/enemies; real main.lua, key callbacks and captured pixels.
# Screenshot callbacks only hand images to a protected update phase.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output=${1:-$(mktemp -d /var/tmp/roag-pattern-gui-XXXXXX)}
mkdir -p "$output/app/tools" "$output/data"
cp "$project_dir/main.lua" "$output/app/roag_main.lua"
cp "$project_dir/conf.lua" "$output/app/conf.lua"
cp -R "$project_dir/src" "$output/app/src"
cp -R "$project_dir/assets" "$output/app/assets"
cp "$project_dir/tools/art_png.lua" "$output/app/tools/art_png.lua"
cat > "$output/app/main.lua" <<'LUA'
io.stdout:setvbuf('line')
function love.errorhandler(why)
    io.stderr:write(debug.traceback(tostring(why),2)..'\n')
    return function()return 1 end
end
local G=require('src.game')
local Camera=require('src.camera')
local Codec=require('src.persistence')
local Outline=require('src.attack_outline')
local Palette=require('src.visual_palette')
local Playback=require('src.playback')
local PNG=require('tools.art_png')
local V=require('src.visibility')
local slug=os.getenv('ROAG_PATTERN_CASE') or 'linebreaker_pawn'
local resuming=os.getenv('ROAG_PATTERN_RESUME')=='1'
local id='core:boon/'..slug
local def=assert(require('src.boon_content').get(id))
local form=require('src.boon_patterns').weapon_forms[slug]
assert(def.pattern_preview or form,'choose a pattern or weapon-form boon')
assert(not resuming or form,'only weapon forms use the resume phase')
local kind
if form then
    kind=form.ability_id==require('src.classes').get(form.class_id).special_id
        and 'sweeping_slash' or 'basic_attack'
else
    kind=def.pattern_preview.trigger=='dash' and 'dash'
        or def.pattern_preview.trigger=='wait' and 'wait'
        or def.pattern_preview.special and 'sweeping_slash' or 'basic_attack'
end
local dx,dy=1,slug=='bishops_wake' and 1 or 0
local s,p,camera,pb,events,foes,spared
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
local playback_new=Playback.new
function Playback.new(...)pb=playback_new(...);return pb end
function G.new_stage()
    s=G.new(37,form and 'core:map/scrolling_room' or nil,{class_id=form and form.class_id})
    p=G.player(s);p.position.x,p.position.y=form and 15 or 7,form and 16 or 6;foes={};spared={}
    local function foe(x,y,hp)
        local e=G.spawn_enemy(s,'core:enemy/mossbound_guard',x,y,100000)
        e.hp=hp or 100;e.max_hp=100;foes[#foes+1]=e
    end
    if form then
        if slug=='pommel_return' or slug=='backswing'then foe(16,16);foe(14,16)
        elseif slug=='needle_flight'then foe(16,16);foe(18,16)
        elseif slug=='hollow_sigil'then
            foe(17,16);foe(17,17);spared={{x=17,y=16}}
        elseif slug=='distant_bastion'then
            foe(16,16);foe(17,16);foe(16,17);spared={{x=16,y=16}}
        elseif slug=='volatile_vial'then foe(17,16);foe(17,17)
        else
            foe(16,16);foe(17,16);foe(17,15);foe(17,17);spared={{x=17,y=16}}
        end
    elseif slug=='backtrail_mantle'then foe(7,5)
    elseif slug=='crosswind'then foe(8,6);foe(9,6)
    else
        foe(8,6+dy,slug=='hollow_crown' and 1 or 100)
        if slug=='rooks_wake'then foe(8,8)
        elseif slug=='bishops_wake'then foe(9,8)
        elseif slug=='knights_gambit'then foe(10,7)
        elseif slug=='fan_of_blades'then foe(9,7)
        else foe(10,6)end
    end
    V.recompute(s);return s
end
local submit=G.submit
function G.submit(...)
    local ok,why,result,preview=submit(...);events=result
    return ok,why,result,preview
end
local restore=G.restore
function G.restore(...)
    local loaded=restore(...)
    if resuming then
        s=loaded;p=G.player(s);foes={};spared={}
        for _,e in ipairs(s.entities)do if e.controller=='ai'then foes[#foes+1]=e end end
    end
    return loaded
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local output=assert(os.getenv('ROAG_PATTERN_OUTPUT'))
local stage,before,baseline,preview,started='baseline'
local captured,pending,failed=nil,false,false
local function snapshot()return Codec.encode(G.snapshot(s))end
local function fail(why)
    failed=true;io.stderr:write('PATTERN GUI FAILED ['..stage..']: '..tostring(why)..'\n')
    love.event.quit(1)
end
function love.load(...)
    local args={...}
    local ok,why=xpcall(function()
        love.mouse.getPosition=function()return 0,0 end
        love.keyboard.isDown=function()return false end
        load(unpack(args))
        local wide=os.getenv('ROAG_PATTERN_WIDE')=='1'
        if wide then love.window.setMode(1280,800,{resizable=false})end
        if resuming then
            love.keypressed('c');update(10)
            local f=assert(io.open(output..'/expected-form-run.txt','rb'))
            local expected=f:read('*a');f:close()
            assert(s and snapshot()==expected,'fresh-process Continue changed the saved form/class/run')
            assert(p.definition_id==form.class_id and p.boons[id])
            stage='form-resume'
        else G.set_boon_stacks(s,p.id,id,'rare',1);update(10)end
        before=snapshot();started=love.timer.getTime()
        print('Renderer:',love.graphics.getRendererInfo())
        preview=G.preview(s,kind,dx,dy)
        local cells=form and preview.cells or preview.boon_cells
        assert(preview.valid and cells and #cells>0)
        if form then assert(preview.weapon_hint)end
    end,debug.traceback)
    if not ok then fail(why)end
end
local function border(image,cells,clear_interiors)
    local checked=0
    for _,cell in ipairs(cells)do
        local x,y=Camera.tile_top_left(camera,cell.x,cell.y)
        if x>0 and y>145 and x+camera.tile<image:getWidth() and y+camera.tile<image:getHeight()-110 then
            local rect=Outline.rectangles(camera.tile)[1]
            local pixel={image:getPixel(math.floor(x+rect[1]+1),math.floor(y+rect[2]))}
            for c=1,3 do assert(math.abs(pixel[c]-Palette.friendly[c])<.04,
                'green conditional/locked outline missing at '..cell.x..','..cell.y)end
            if clear_interiors then
                for yy=8,camera.tile-9 do for xx=8,camera.tile-9 do
                    local a={baseline:getPixel(math.floor(x+xx),math.floor(y+yy))}
                    local b={image:getPixel(math.floor(x+xx),math.floor(y+yy))}
                    for c=1,4 do assert(math.abs(a[c]-b[c])<=1/255,
                        'conditional outline covered the tile interior')end
                end end
            end
            checked=checked+1
        end
    end
    assert(checked>0,'no unobscured world cells checked')
end
local function seek_effect()
    for _=1,400 do
        local event=Playback.current(pb)
        if event and (event.kind=='EffectApplied' or event.kind=='DelayedEffectResolved')
            and event.boon_id==id then return end
        update(.01)
    end
    error('real playback never displayed the boon effect')
end
local function commit()
    if kind=='wait'then love.keypressed('space')else love.keypressed('return')end
    assert(events and #events>0,'key callback did not commit')
    if form then
        update(10);stage='form-result'
    elseif kind=='dash'then
        update(10);assert(#G.pending_boon_effects(s)==1)
        stage='locked-wake'
    else seek_effect();stage='effect'end
    before=snapshot()
end
local function advance(image)
    assert(snapshot()==before,'draw/menu/refresh changed authoritative state')
    PNG.write(output..'/'..stage..'.png',image)
    if stage=='baseline'then
        baseline=image
        if kind=='wait'then commit();return end
        love.keypressed(kind=='dash' and 'x' or kind=='sweeping_slash' and 'r' or 'f')
        love.keypressed(dy==1 and 'c' or 'd');stage='conditional-preview'
    elseif stage=='conditional-preview'then
        border(image,form and preview.cells or preview.boon_cells,true)
        for _,cell in ipairs(spared)do
            local x,y=Camera.tile_top_left(camera,cell.x,cell.y)
            for yy=0,camera.tile-1 do for xx=0,camera.tile-1 do
                local a={baseline:getPixel(math.floor(x+xx),math.floor(y+yy))}
                local b={image:getPixel(math.floor(x+xx),math.floor(y+yy))}
                for c=1,4 do assert(math.abs(a[c]-b[c])<=1/255,'spared cell was outlined or filled')end
            end end
        end
        love.keypressed('escape');assert(snapshot()==before,'cancel changed state')
        love.keypressed(kind=='dash' and 'x' or kind=='sweeping_slash' and 'r' or 'f')
        love.keypressed(dy==1 and 'c' or 'd');commit();return
    elseif stage=='locked-wake'then
        border(image,G.pending_boon_effects(s)[1].target_cells,false)
        local expected=snapshot();love.keypressed('f5');update(0)
        assert(snapshot()==expected,'refresh changed locked wake')
        love.keypressed('space');seek_effect();stage='effect'
    elseif stage=='form-result'then
        local performed
        for _,e in ipairs(events)do if e.kind=='AttackPerformed'then performed=e end end
        assert(performed and #performed.target_cells==#preview.cells)
        for n,c in ipairs(preview.cells)do
            assert(performed.target_cells[n].x==c.x and performed.target_cells[n].y==c.y,
                'visible form preview disagreed with actual primary cells')
        end
        local damaged=0
        for _,e in ipairs(foes)do
            local is_spared=false
            for _,c in ipairs(spared)do if c.x==e.position.x and c.y==e.position.y then is_spared=true end end
            assert(is_spared and e.hp==100 or not is_spared and e.hp<100,'wrong primary victim')
            if e.hp<100 then damaged=damaged+1 end
        end
        assert(damaged>0)
        local expected=snapshot();love.keypressed('f5');update(0)
        assert(snapshot()==expected,'refresh changed transformed attack or run state')
        local f=assert(io.open(output..'/expected-form-run.txt','wb'));assert(f:write(expected));f:close()
        love.keypressed('escape');love.keypressed('2') -- real Save & Quit callback
        print('PASS '..slug..': actual class, green primary pixels, clear/spared interiors, cancel, exact primary cells/damage/F5 and Save & Quit')
        love.event.quit();return
    elseif stage=='form-resume'then
        local f=assert(io.open(output..'/form-result.png','rb'));local bytes=f:read('*a');f:close()
        local expected=love.image.newImageData(love.filesystem.newFileData(bytes,'expected.png'))
        for y=16,535 do for x=16,1007 do
            local a={expected:getPixel(x,y)};local b={image:getPixel(x,y)}
            for c=1,4 do assert(math.floor(a[c]*255+.5)==math.floor(b[c]*255+.5),
                'continued world pixels differ at '..x..','..y)end
        end end
        assert(preview.weapon_hint,'saved form did not reconstruct its preview')
        local expected_run=snapshot();love.keypressed('f5');update(0)
        assert(snapshot()==expected_run,'resumed F5 changed the authoritative run')
        print('PASS '..slug..': new-process Continue, same class/form, pixel-identical world, reconstructed preview and pure F5')
        love.event.quit();return
    else
        assert(stage=='effect')
        local actual=Playback.current(pb)
        assert(actual and #actual.target_cells>0)
        local damaged=false
        for _,e in ipairs(foes)do if e.hp<100 then damaged=true end end
        assert(damaged,'no real damage committed')
        if kind=='dash'then assert(#G.pending_boon_effects(s)==0)end
        print('PASS '..slug..': real conditional/locked pixels, cancel, effect playback, damage, pure frames')
        love.event.quit();return
    end
    before=snapshot()
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
            pending=true
            love.graphics.captureScreenshot(function(image)captured=image;pending=false end)
        end
    end,debug.traceback)
    if not ok then fail(why)end
end
LUA
ROAG_GUI_AUTOSTART=1 ROAG_PATTERN_OUTPUT="$output" XDG_DATA_HOME="$output/data" \
    timeout -k 3s 35s love "$output/app" > "$output/love.log" 2>&1 || {
    cat "$output/love.log"
    printf 'Retained failed pattern GUI artifacts: %s\n' "$output"
    exit 1
}
test -s "$output/baseline.png"
if [ -s "$output/form-result.png" ];then
    ROAG_GUI_AUTOSTART=0 ROAG_PATTERN_RESUME=1 ROAG_PATTERN_OUTPUT="$output" XDG_DATA_HOME="$output/data" \
        timeout -k 3s 35s love "$output/app" > "$output/resume.log" 2>&1 || {
        cat "$output/resume.log"
        printf 'Retained failed form Continue artifacts: %s\n' "$output"
        exit 1
    }
    test -s "$output/form-resume.png"
    cat "$output/resume.log"
else test -s "$output/effect.png";fi
cat "$output/love.log"
printf 'Retained pattern GUI artifacts: %s\n' "$output"
