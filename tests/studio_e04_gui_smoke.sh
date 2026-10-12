#!/bin/sh
# Isolated E04 art -> Studio theme -> publish -> real game -> fallback.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roeg-studio-e04-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roeg_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/examples" "$app/examples"
cp -R "$project_dir/tools" "$app/tools"
cat > "$app/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
local Persistence=require('src.persistence')
local phase=assert(tonumber(os.getenv('ROEG_E04_PHASE')))
local output=assert(os.getenv('ROEG_E04_OUTPUT'))
local tracked,events,queue
local original_new_stage,original_submit,original_add=Game.new_stage,Game.submit,Playback.add
local original_restore=Game.restore
Game.restore=function(...) tracked=original_restore(...);return tracked end
if phase==4 then
    Game.new_stage=function() tracked=Game.new(123,'core:map/scrolling_room',
        {demo_enemies=true,demo_chests=true});return tracked end
else
    Game.new_stage=function(...) tracked=original_new_stage(...);return tracked end
end
Game.submit=function(s,...)
    tracked=s
    local ok,reason,result,preview=original_submit(s,...)
    events=result
    return ok,reason,result,preview
end
Playback.add=function(q,...)
    queue=q;return original_add(q,...)
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw=love.load,love.draw
local shot,pending,part=0,false,1
local effect_snapshot
local function state()return assert(love.roegStudioState())end
local function replace_text(value)
    for _=1,120 do love.keypressed('backspace') end
    love.textinput(value);love.keypressed('return')
end
local function screenshot(name,next_step)
    pending=true
    love.graphics.captureScreenshot(function(img)
        local file=assert(io.open(output..'/'..name..'.tga','wb'))
        file:write(img:encode('tga'):getString());file:close()
        shot=shot+1
        if next_step then next_step() else love.event.quit() end
        pending=false
    end)
end
local function paint_sprite(w,h,kind,id)
    love.keypressed('c');replace_text(w..'x'..h)
    local canvas=assert(state().canvas)
    local ox,oy=canvas:origin()
    local x,y=ox+canvas.zoom/2,oy+canvas.zoom/2
    love.mousepressed(x,y,1)
    love.mousemoved(x+2*canvas.zoom,y,2*canvas.zoom,0)
    love.mousereleased(x+2*canvas.zoom,y,1)
    if kind=='bitmap_font' then
        for _,column in ipairs({0,2}) do
            local sx=ox+(column+.5)*canvas.zoom
            love.mousepressed(sx,y,1)
            love.mousemoved(sx,y+6*canvas.zoom,0,6*canvas.zoom)
            love.mousereleased(sx,y+6*canvas.zoom,1)
        end
        local sy=oy+3.5*canvas.zoom
        love.mousepressed(x,sy,1)
        love.mousemoved(x+2*canvas.zoom,sy,2*canvas.zoom,0)
        love.mousereleased(x+2*canvas.zoom,sy,1)
    end
    assert(canvas:dirty())
    love.keypressed('s');love.keypressed('escape')
    love.keypressed('3');replace_text(kind)
    love.keypressed('2');replace_text(id)
    love.keypressed('s')
end
local function import_sample(kind,id)
    love.keypressed('i')
    love.keypressed('3');replace_text(kind)
    love.keypressed('2');replace_text(id)
    love.keypressed('s')
end
local function bind(slot)
    love.mousepressed(45,133+(slot-1)*34+5,1)
    love.keypressed('right')
    assert(state().theme_editor.theme.slots[require('src.studio_theme').slots[slot]]~='')
end
function love.load(args)
    old_load(args)
    if phase==1 then
        assert(state().mode=='home')
        love.keypressed('n');love.textinput('UI Fixture');love.keypressed('return')
        paint_sprite(16,16,'ui_icon','studio:ui/heart')
        import_sample('ui_panel','studio:ui/panel')
        import_sample('ui_background','studio:bg/title')
        import_sample('effect_sprite','studio:effect/hit')
        paint_sprite(64,8,'bitmap_font','studio:font/hud')
        assert(#state().project.assets==5)
        love.keypressed('u');assert(state().mode=='theme')
        bind(1);bind(2);bind(3);bind(4);bind(5);bind(7);bind(8);bind(9)
        love.keypressed('g');replace_text('HP0123456789:/LV')
        love.keypressed('i');replace_text('4,4,4,4')
        love.keypressed('b');love.keypressed('b') -- fit the authored title art
        love.keypressed('h');replace_text('HP 24/40 LV 3 GOLD 12')
        assert(state().theme_editor.preview_text=='HP 24/40 LV 3 GOLD 12')
        love.keypressed('p')
        love.keypressed('v')
        assert(state().theme_editor.viewport==2)
        love.keypressed('s')
        assert(not state().dirty)
        local icon
        for _,a in ipairs(state().project.assets) do
            if a.id=='studio:ui/heart' then icon=a end
        end
        local source=assert(love.filesystem.read('studio/projects/ui_fixture/'..icon.document))
        local written=assert(io.open(output..'/icon-source.rgba','wb'))
        written:write(source);written:close()
        assert(not love.filesystem.getInfo('active-a.roeg') and
            not love.filesystem.getInfo('profile-a.roeg'))
        local file=assert(io.open(output..'/save_dir.txt','wb'))
        file:write(love.filesystem.getSaveDirectory());file:close()
    elseif phase==2 then
        love.keypressed('o');love.keypressed('return');love.keypressed('u')
        local s=state();assert(s.mode=='theme' and s.project.id=='ui_fixture')
        assert(s.theme.slots.hud_panel=='studio:ui/panel')
        assert(s.theme.slots.bitmap_font=='studio:font/hud')
        assert(s.theme.font.characters=='HP0123456789:/LV')
        local asset
        for _,candidate in ipairs(s.project.assets) do
            if candidate.id=='studio:ui/heart' then asset=candidate end
        end
        assert(asset and asset.document~='','painted icon source missing')
        local doc=assert(require('src.studio_store').new(require('src.studio_love_storage')):
            document(s.project,asset))
        assert(require('src.studio_document').get(doc,0,0)[4]==255)
        local saved=assert(io.open(output..'/icon-source.rgba','rb'))
        local old_bytes=saved:read('*a');saved:close()
        assert(love.filesystem.read('studio/projects/ui_fixture/'..asset.document)==old_bytes,
            'E04 metadata edit changed E01 source pixels')
        love.keypressed('tab');love.keypressed('v');love.keypressed('p')
        assert(s.theme_editor.viewport==2 or s.theme_editor.viewport==1)
    elseif phase==3 then
        assert(love.roegThemeStatus()=='loaded',love.roegThemeStatus())
        assert(love.roegAssetIdResolved('studio:ui/heart'))
    elseif phase==4 then
        assert(love.roegThemeStatus()=='loaded')
        assert(tracked and tracked.clock==0)
        love.keypressed('space')
        assert(tracked.clock==100)
        local found=false
        for _,event in ipairs(events) do
            if event.kind=='DamageTaken' then found=true end
        end
        assert(found,'fixture produced no genuine DamageTaken event')
        for _=1,30 do
            local current=queue and Playback.current(queue)
            if current and current.kind=='DamageTaken' then break end
            Playback.update(queue,.1)
        end
        assert(Playback.current(queue).kind=='DamageTaken',
            'combat playback never reached hit effect')
        effect_snapshot=Persistence.encode(Game.snapshot(tracked))
    elseif phase==5 then
        assert(love.roegThemeStatus()=='absent')
    elseif phase==7 then
        assert(love.roegThemeStatus():find('incompatible',1,true),
            'valid incompatible optional theme did not fall back')
        love.keypressed('c')
        assert(tracked,'Continue failed with incompatible optional theme')
        local file=assert(io.open(output..'/saved.snapshot','rb'))
        local expected=file:read('*a');file:close()
        assert(Persistence.encode(Game.snapshot(tracked))==expected,
            'incompatible visual pack changed active-run continuation')
    elseif phase==8 then
        assert(love.roegThemeStatus():find('corrupt',1,true),
            'damaged optional theme did not fall back')
    elseif phase==6 then
        assert(love.roegThemeStatus()=='absent')
        love.keypressed('c')
        assert(tracked,'Continue failed without optional theme')
        local file=assert(io.open(output..'/saved.snapshot','rb'))
        local expected=file:read('*a');file:close()
        assert(Persistence.encode(Game.snapshot(tracked))==expected,
            'presentation pack changed authoritative continuation')
    end
end
function love.draw()
    old_draw()
    if pending then return end
    if phase==1 then
        if part==1 then
            screenshot('studio-theme',function()
                part=2;love.keypressed('v');love.keypressed('tab')
            end)
        elseif part==2 then
            screenshot('studio-preview',function()
                part=3;love.keypressed('tab');love.keypressed('tab')
            end)
        else screenshot('studio-chest-preview') end
    elseif phase==2 then screenshot('studio-reopened')
    elseif phase==3 then
        if part==1 then
            assert(love.roegThemeDrawCounts().background>0)
            screenshot('game-title',function()
                part=2;love.keypressed('n')
                assert(tracked and tracked.clock==0)
                local f=assert(io.open(output..'/saved.snapshot','wb'))
                f:write(Persistence.encode(Game.snapshot(tracked)));f:close()
            end)
        else
            local counts=love.roegThemeDrawCounts()
            assert(counts.panel>0 and counts.icon>0 and counts.glyph>0,
                'themed HUD/icon/bitmap text did not render')
            local f=assert(io.open(output..'/saved.snapshot','rb'))
            local expected=f:read('*a');f:close()
            assert(Persistence.encode(Game.snapshot(tracked))==expected,
                'themed HUD draw changed authoritative state')
            screenshot('game-hud')
        end
    elseif phase==4 then
        assert(love.roegThemeDrawCounts().effect>0,
            'genuine DamageTaken did not render authored effect')
        assert(Persistence.encode(Game.snapshot(tracked))==effect_snapshot,
            'authored effect draw changed authoritative state')
        screenshot('game-effect')
    elseif phase==5 then screenshot('fallback-title')
    elseif phase==7 then screenshot('incompatible-continued')
    elseif phase==8 then screenshot('corrupt-fallback-title')
    elseif phase==6 then screenshot('fallback-continued') end
end
LUA
run_phase() {
    number=$1
    shift
    ROEG_E04_PHASE="$number" ROEG_E04_OUTPUT="$output_dir" \
        XDG_DATA_HOME="$output_dir/data" timeout 25s love "$app" "$@" \
        >"$output_dir/phase$number.log" 2>&1
}
run_phase 1 --studio
run_phase 2 --studio
save_dir=$(cat "$output_dir/save_dir.txt")
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" \
    --project ui_fixture) >"$output_dir/visual-dry-run.log"
(cd "$app" && luajit tools/studio_publish.lua --save-dir "$save_dir" \
    --project ui_fixture --apply) >"$output_dir/visual-publish.log"
(cd "$app" && luajit tools/studio_theme_publish.lua --save-dir "$save_dir" \
    --project ui_fixture) >"$output_dir/theme-dry-run.log"
(cd "$app" && luajit tools/studio_theme_publish.lua --save-dir "$save_dir" \
    --project ui_fixture --apply) >"$output_dir/theme-publish.log"
run_phase 3
ROEG_GUI_AUTOSTART=1 run_phase 4
cp "$app/assets/studio/theme-a.pack" "$output_dir/good-theme.pack"
(cd "$app" && luajit - <<'LUA'
local Store=require('src.studio_theme_store')
local Codec=require('src.persistence')
local path='assets/studio/theme-a.pack'
local function read(p)
    local f=io.open(p,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
local theme=assert(Store.inspect_published(read))
theme.schema='future-theme/2'
local payload=Codec.encode(theme)
local a,b=1,0
for i=1,#payload do a=(a+payload:byte(i))%65521;b=(b+a)%65521 end
local f=assert(io.open(path,'wb'))
f:write(('ROEG-STUDIO-PUBLISHED-THEME/1\n2\n%d\n%08x\n'):format(
    #payload,b*65536+a)..payload);f:close()
LUA
)
run_phase 7
printf 'truncated' > "$app/assets/studio/theme-a.pack"
run_phase 8
rm "$app/assets/studio/theme-a.pack"
run_phase 5
run_phase 6
for name in studio-theme studio-preview studio-chest-preview studio-reopened game-title game-hud \
    game-effect incompatible-continued corrupt-fallback-title fallback-title fallback-continued; do
    test -s "$output_dir/$name.tga"
done
if cmp -s "$output_dir/game-title.tga" "$output_dir/fallback-title.tga"; then
    echo 'Themed and fallback title frames were identical' >&2;exit 1
fi
printf 'Studio E04 GUI smoke passed: source/reopen/publish/title/HUD/effect/fallback in %s\n' "$output_dir"
