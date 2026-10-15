#!/bin/sh
# Isolated E01/E03/E04 authored art -> atlas -> normal-game draw/fallback.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
out=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-studio-e05-XXXXXX")
run_love() {
    XDG_DATA_HOME="$2/data" TMPDIR="${TMPDIR:-/var/tmp}" \
        timeout 25s love "$2/app" "$3" >"$4" 2>&1
}
TMPDIR="${TMPDIR:-/var/tmp}" sh "$project_dir/tests/studio_e04_gui_smoke.sh" \
    >"$out/e04-fixture.log" 2>&1
e04=$(awk '{print $NF}' "$out/e04-fixture.log" | tail -1)
test -s "$e04/game-effect.tga"
save_dir=$(cat "$e04/save_dir.txt")
cp "$e04/app/main.lua" "$out/e04-wrapper.lua"
cp "$e04/good-theme.pack" "$e04/app/assets/studio/theme-a.pack"
cp "$e04/icon-source.rgba" "$out/icon-original.rgba"
# The upstream E04 smoke intentionally leaves an isolated active checkpoint.
# Each image comparison starts the same fresh title -> New Run sequence.
rm -f "$save_dir/active-a.roag" "$save_dir/active-b.roag"
cp "$e04/app/roag_main.lua" "$e04/app/main.lua"
run_love build "$e04" --atlas-build "$out/e04-build.log"
cp "$save_dir/studio/atlas-build/manifest.txt" "$out/first-candidate.pack"
mkdir -p "$out/first-pages"
cp "$save_dir"/studio/atlas-build/page-*.png "$out/first-pages/"
run_love build "$e04" --atlas-build "$out/e04-build-repeat.log"
cmp "$out/first-candidate.pack" "$save_dir/studio/atlas-build/manifest.txt"
for first_page in "$out"/first-pages/*.png; do
    cmp "$first_page" "$save_dir/studio/atlas-build/$(basename "$first_page")"
done
(cd "$e04/app" && luajit tools/studio_atlas_publish.lua --save-dir "$save_dir" \
    --project ui_fixture >"$out/e04-atlas-dry.log")
test ! -e "$e04/app/assets/studio/atlas-a.pack"
(cd "$e04/app" && luajit tools/studio_atlas_publish.lua --save-dir "$save_dir" \
    --project ui_fixture --apply >"$out/e04-atlas-apply.log")
cmp "$e04/icon-source.rgba" "$out/icon-original.rgba"
cp "$out/e04-wrapper.lua" "$e04/app/main.lua"
cat >>"$e04/app/main.lua" <<'LUA'
local e05_load=love.load
function love.load(args)
    e05_load(args)
    local phase=tonumber(os.getenv('ROAG_E04_PHASE'))
    if phase==3 or phase==4 then
        local expected=os.getenv('ROAG_E05_EXPECT') or 'atlas'
        for _,id in ipairs({'studio:ui/heart','studio:ui/panel',
            'studio:bg/title','studio:effect/hit'}) do
            assert(love.roagAssetBacking(id)==expected,
                id..' backing: '..tostring(love.roagAssetBacking(id)))
        end
        assert(love.roagAssetBacking('studio:font/hud')=='loose')
    end
end
LUA
mkdir -p "$out/e04-atlas" "$out/e04-loose" "$out/e04-corrupt" "$out/e04-stale"
cp "$out/icon-original.rgba" "$out/e04-atlas/icon-source.rgba"
ROAG_E04_PHASE=3 ROAG_E04_OUTPUT="$out/e04-atlas" \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" >"$out/e04-atlas-title.log" 2>&1
ROAG_E04_PHASE=4 ROAG_E04_OUTPUT="$out/e04-atlas" ROAG_GUI_AUTOSTART=1 \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" >"$out/e04-atlas-effect.log" 2>&1
cmp "$e04/game-title.tga" "$out/e04-atlas/game-title.tga"
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$e04/game-hud.tga" "$out/e04-atlas/game-hud.tga" ignore-notice \
    >"$out/hud-frame-comparison.log"
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$e04/game-effect.tga" "$out/e04-atlas/game-effect.tga" \
    >"$out/effect-frame-comparison.log"
# Editor reopen after compilation still reads the original layered source.
ROAG_E04_PHASE=2 ROAG_E04_OUTPUT="$out/e04-atlas" \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" --studio \
    >"$out/e04-atlas-reopen.log" 2>&1
(cd "$e04/app" && luajit tools/studio_atlas_publish.lua --disable --apply \
    >"$out/e04-disable.log")
rm -f "$save_dir/active-a.roag" "$save_dir/active-b.roag"
ROAG_E04_PHASE=3 ROAG_E04_OUTPUT="$out/e04-loose" ROAG_E05_EXPECT=loose \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" >"$out/e04-loose.log" 2>&1
cmp "$e04/game-title.tga" "$out/e04-loose/game-title.tga"
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$e04/game-hud.tga" "$out/e04-loose/game-hud.tga" ignore-notice \
    >"$out/loose-hud-comparison.log"
(cd "$e04/app" && luajit tools/studio_atlas_publish.lua --enable --apply \
    >"$out/e04-enable.log")
page=$(find "$e04/app/assets/studio/atlas" -name 'page-*.png' | head -1)
cp "$page" "$out/good-page.png"
printf 'truncated' > "$page"
rm -f "$save_dir/active-a.roag" "$save_dir/active-b.roag"
ROAG_E04_PHASE=3 ROAG_E04_OUTPUT="$out/e04-corrupt" ROAG_E05_EXPECT=loose \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" >"$out/e04-corrupt.log" 2>&1
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$e04/game-hud.tga" "$out/e04-corrupt/game-hud.tga" ignore-notice \
    >"$out/corrupt-hud-comparison.log"
cp "$out/good-page.png" "$page"
cp "$e04/app/assets/studio/manifest.txt" "$out/visual-manifest.txt"
sed 's/UI Fixture/UI Fixture Changed/' "$out/visual-manifest.txt" \
    > "$e04/app/assets/studio/manifest.txt"
rm -f "$save_dir/active-a.roag" "$save_dir/active-b.roag"
ROAG_E04_PHASE=3 ROAG_E04_OUTPUT="$out/e04-stale" ROAG_E05_EXPECT=loose \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" >"$out/e04-stale.log" 2>&1
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$e04/game-hud.tga" "$out/e04-stale/game-hud.tga" ignore-notice \
    >"$out/stale-hud-comparison.log"
cp "$out/visual-manifest.txt" "$e04/app/assets/studio/manifest.txt"
# Exercise real Save & Quit / Continue across two more LÖVE processes.
cp "$e04/app/roag_main.lua" "$e04/app/main.lua"
cat >"$e04/app/main.lua" <<'LUA'
local Game=require('src.game')
local Codec=require('src.persistence')
local tracked
local restore=Game.restore
Game.restore=function(...)
    tracked=restore(...);return tracked
end
assert(love.filesystem.load('roag_main.lua'))()
local old=love.load
function love.load(args)
    old(args)
    local mode=os.getenv('ROAG_E05_MODE')
    if mode=='save' then
        love.keypressed('c')
        assert(tracked,'atlas fixture Continue unavailable')
        assert(love.roagAssetBacking('studio:ui/heart')=='atlas')
        local f=assert(io.open(os.getenv('ROAG_E05_SNAPSHOT'),'wb'))
        f:write(Codec.encode(Game.snapshot(tracked)));f:close()
        love.keypressed('escape');love.keypressed('s')
    elseif mode=='continue' then
        love.keypressed('c')
        assert(tracked,'fresh-process Continue unavailable')
        local f=assert(io.open(os.getenv('ROAG_E05_SNAPSHOT'),'rb'))
        local expected=f:read('*a');f:close()
        assert(Codec.encode(Game.snapshot(tracked))==expected,
            'atlas changed restored authoritative snapshot')
        assert(love.roagAssetBacking('studio:ui/heart')=='atlas')
        love.event.quit()
    end
end
LUA
ROAG_E05_MODE=save ROAG_E05_SNAPSHOT="$out/saved.snapshot" \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" >"$out/save.log" 2>&1
ROAG_E05_MODE=continue ROAG_E05_SNAPSHOT="$out/saved.snapshot" \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" >"$out/continue.log" 2>&1
# A separate E03 fixture proves the generated room tile samples an atlas Quad.
TMPDIR="${TMPDIR:-/var/tmp}" sh "$project_dir/tests/studio_e03_gui_smoke.sh" \
    >"$out/e03-fixture.log" 2>&1
e03=$(awk '{print $NF}' "$out/e03-fixture.log" | tail -1)
test -s "$e03/phase3.tga"
cp "$e03/phase3.tga" "$out/e03-loose.tga"
cp "$e03/app/main.lua" "$out/e03-wrapper.lua"
cp "$e03/app/roag_main.lua" "$e03/app/main.lua"
run_love build "$e03" --atlas-build "$out/e03-build.log"
e03_save=$(cat "$e03/save_dir.txt")
(cd "$e03/app" && luajit tools/studio_atlas_publish.lua --save-dir "$e03_save" \
    --project world_fixture >"$out/e03-atlas-dry.log")
(cd "$e03/app" && luajit tools/studio_atlas_publish.lua --save-dir "$e03_save" \
    --project world_fixture --apply >"$out/e03-atlas-apply.log")
cp "$out/e03-wrapper.lua" "$e03/app/main.lua"
cat >>"$e03/app/main.lua" <<'LUA'
local e05_load=love.load
function love.load(args)
    e05_load(args)
    if tonumber(os.getenv('ROAG_E03_PHASE'))==3 then
        assert(love.roagAssetBacking('studio:tile/moss')=='atlas',
            'authored generated-room tile did not resolve from atlas')
    end
end
LUA
ROAG_E03_PHASE=3 ROAG_E03_OUTPUT="$e03" ROAG_GUI_AUTOSTART=1 \
    XDG_DATA_HOME="$e03/data" timeout 25s love "$e03/app" >"$out/e03-atlas-game.log" 2>&1
cmp "$out/e03-loose.tga" "$e03/phase3.tga"
# Re-edit the original E01 layer in Studio. The old immutable document remains
# intact; a new loose publish makes the old atlas stale until it is rebuilt.
cp "$e04/app/roag_main.lua" "$e04/app/main.lua"
cat >"$e04/app/main.lua" <<'LUA'
assert(love.filesystem.load('roag_main.lua'))()
local old=love.load
function love.load(args)
    old(args)
    love.keypressed('o');love.keypressed('return')
    local studio=assert(love.roagStudioState())
    assert(studio.mode=='project' and studio.project.id=='ui_fixture')
    local asset,selected_index
    for index,candidate in ipairs(studio.project.assets) do
        if candidate.id=='studio:ui/heart' then
            asset=candidate;selected_index=index
        end
    end
    assert(asset and asset.document~='')
    while love.roagStudioState().selected~=selected_index do
        love.keypressed(love.roagStudioState().selected<selected_index and
            'down' or 'up')
    end
    assert(love.roagStudioState().project.assets[
        love.roagStudioState().selected].id=='studio:ui/heart')
    local old_document=asset.document
    local old_path='studio/projects/ui_fixture/'..old_document
    local old_bytes=assert(love.filesystem.read(old_path))
    love.keypressed('e')
    local canvas=assert(love.roagStudioState().canvas)
    local ox,oy=canvas:origin()
    local x,y=ox+6.5*canvas.zoom,oy+6.5*canvas.zoom
    love.mousepressed(x,y,1);love.mousereleased(x,y,1)
    assert(canvas:dirty(),'edited E01 sprite has no pixel change')
    love.keypressed('s')
    local updated
    for _,candidate in ipairs(love.roagStudioState().project.assets) do
        if candidate.id=='studio:ui/heart' then updated=candidate end
    end
    assert(updated and updated.document~=old_document,
        'E01 edit did not create a new immutable source path: '..
            tostring(love.roagStudioState().message))
    assert(love.filesystem.read(old_path)==old_bytes,
        'atlas compilation or re-edit changed the old E01 source')
    love.keypressed('escape')
    assert(love.roagStudioState().mode=='project')
    love.keypressed('i') -- Import a separate optional enemy visual.
    local function replace(value)
        for _=1,120 do love.keypressed('backspace') end
        love.textinput(value);love.keypressed('return')
    end
    love.keypressed('3');replace('enemy')
    love.keypressed('2');replace('core:enemy/mossbound_guard')
    love.keypressed('s')
    assert(love.roagStudioState().project.assets[
        love.roagStudioState().selected].id=='core:enemy/mossbound_guard')
    love.event.quit()
end
LUA
XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" --studio \
    >"$out/e04-reedit.log" 2>&1
(cd "$e04/app" && luajit tools/studio_publish.lua --save-dir "$save_dir" \
    --project ui_fixture --apply --overwrite >"$out/e04-republish-loose.log")
cp "$out/e04-wrapper.lua" "$e04/app/main.lua"
cat >>"$e04/app/main.lua" <<'LUA'
local e05_load=love.load
function love.load(args)
    e05_load(args)
    if tonumber(os.getenv('ROAG_E04_PHASE'))==3 or
        tonumber(os.getenv('ROAG_E04_PHASE'))==4 then
        assert(love.roagAssetBacking('studio:ui/heart')==
            os.getenv('ROAG_E05_EXPECT'),'rebuilt stable ID backing differs')
        assert(love.roagAssetBacking('core:enemy/mossbound_guard')==
            os.getenv('ROAG_E05_EXPECT'),'enemy sprite backing differs')
    end
end
LUA
mkdir -p "$out/e04-rebuild-loose" "$out/e04-rebuild-atlas"
rm -f "$save_dir/active-a.roag" "$save_dir/active-b.roag"
ROAG_E04_PHASE=3 ROAG_E04_OUTPUT="$out/e04-rebuild-loose" ROAG_E05_EXPECT=loose \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" \
    >"$out/e04-rebuild-stale.log" 2>&1
ROAG_E04_PHASE=4 ROAG_E04_OUTPUT="$out/e04-rebuild-loose" ROAG_E05_EXPECT=loose \
    ROAG_GUI_AUTOSTART=1 XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" \
    >"$out/e04-rebuild-loose-effect.log" 2>&1
if cmp -s "$out/e04-atlas/game-hud.tga" \
    "$out/e04-rebuild-loose/game-hud.tga"; then
    echo 'E01 re-edit did not change the live HUD image' >&2;exit 1
fi
cp "$e04/app/roag_main.lua" "$e04/app/main.lua"
run_love build "$e04" --atlas-build "$out/e04-rebuild.log"
(cd "$e04/app" && luajit tools/studio_atlas_publish.lua --save-dir "$save_dir" \
    --project ui_fixture --apply >"$out/e04-rebuild-apply.log")
cp "$out/e04-wrapper.lua" "$e04/app/main.lua"
cat >>"$e04/app/main.lua" <<'LUA'
local e05_load=love.load
function love.load(args)
    e05_load(args)
    if tonumber(os.getenv('ROAG_E04_PHASE'))==3 or
        tonumber(os.getenv('ROAG_E04_PHASE'))==4 then
        assert(love.roagAssetBacking('studio:ui/heart')=='atlas',
            'edited E01 sprite did not enter the rebuilt atlas')
        assert(love.roagAssetBacking('core:enemy/mossbound_guard')=='atlas',
            'enemy sprite did not enter the rebuilt atlas')
    end
end
LUA
rm -f "$save_dir/active-a.roag" "$save_dir/active-b.roag"
ROAG_E04_PHASE=3 ROAG_E04_OUTPUT="$out/e04-rebuild-atlas" \
    XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" \
    >"$out/e04-rebuild-atlas-game.log" 2>&1
ROAG_E04_PHASE=4 ROAG_E04_OUTPUT="$out/e04-rebuild-atlas" \
    ROAG_GUI_AUTOSTART=1 XDG_DATA_HOME="$e04/data" timeout 25s love "$e04/app" \
    >"$out/e04-rebuild-atlas-effect.log" 2>&1
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$out/e04-rebuild-loose/game-hud.tga" \
    "$out/e04-rebuild-atlas/game-hud.tga" ignore-notice \
    >"$out/rebuild-hud-comparison.log"
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$out/e04-rebuild-loose/game-effect.tga" \
    "$out/e04-rebuild-atlas/game-effect.tga" \
    >"$out/rebuild-effect-comparison.log"
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$e04/game-effect.tga" "$out/e04-rebuild-atlas/game-effect.tga" \
    expect-world-change >"$out/enemy-visible-comparison.log"
# E01's independently painted Adventurer follows the same resolver at its
# interpolated player draw position, including the original fallback shape.
TMPDIR="${TMPDIR:-/var/tmp}" sh "$project_dir/tests/studio_e01_gui_smoke.sh" \
    >"$out/e01-fixture.log" 2>&1
e01=$(awk '{print $NF}' "$out/e01-fixture.log" | tail -1)
cp "$e01/phase5.tga" "$out/e01-loose-player.tga"
mv "$e01/app/assets/studio/manifest.disabled" \
    "$e01/app/assets/studio/manifest.txt"
cp "$e01/app/main.lua" "$out/e01-wrapper.lua"
cp "$e01/app/roag_main.lua" "$e01/app/main.lua"
run_love build "$e01" --atlas-build "$out/e01-build.log"
e01_save=$(cat "$e01/save_dir.txt")
(cd "$e01/app" && luajit tools/studio_atlas_publish.lua --save-dir "$e01_save" \
    --project paint_project >"$out/e01-atlas-dry.log")
(cd "$e01/app" && luajit tools/studio_atlas_publish.lua --save-dir "$e01_save" \
    --project paint_project --apply >"$out/e01-atlas-apply.log")
cp "$out/e01-wrapper.lua" "$e01/app/main.lua"
cat >>"$e01/app/main.lua" <<'LUA'
local e05_load=love.load
function love.load(args)
    e05_load(args)
    if tonumber(os.getenv('ROAG_E01_PHASE'))==5 then
        assert(love.roagAssetBacking('core:actor/adventurer')=='atlas',
            'painted player actor did not resolve from atlas')
    end
end
LUA
ROAG_E01_PHASE=5 ROAG_E01_OUTPUT="$e01" ROAG_GUI_AUTOSTART=1 \
    XDG_DATA_HOME="$e01/data" timeout 25s love "$e01/app" \
    >"$out/e01-atlas-game.log" 2>&1
luajit "$project_dir/tests/atlas_frame_compare.lua" \
    "$out/e01-loose-player.tga" "$e01/phase5.tga" ignore-notice \
    >"$out/e01-player-frame-comparison.log"
printf 'Studio E05 GUI smoke passed: E01 pixels, E03 tile, E04 HUD/font/background/effect, exact frames, fallback and fresh-process Continue in %s\n' "$out"
