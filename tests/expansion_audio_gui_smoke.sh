#!/bin/sh
# Isolated Studio WAV import/picker -> publish -> actual LÖVE playback/F5 fallback.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roeg-m7-audio-XXXXXX")
app="$output_dir/app"
mkdir -p "$app" "$output_dir/data"
cp "$project_dir/main.lua" "$app/roeg_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cp -R "$project_dir/examples" "$app/examples"
cp -R "$project_dir/tools" "$app/tools"
cat > "$app/main.lua" <<'LUA'
local phase=assert(tonumber(os.getenv('ROEG_AUDIO_PHASE')))
local output=assert(os.getenv('ROEG_AUDIO_OUTPUT'))
local Game=require('src.game')
local Persistence=require('src.persistence')
local Samples=require('src.studio_audio_samples')
local tracked
local original_restore=Game.restore
if phase==4 then
    Game.restore=function(...)tracked=original_restore(...);return tracked end
end
if phase==3 then
    Game.new_stage=function()tracked=Game.new(7,'core:map/test_room');return tracked end
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local pending=false
local function state()return assert(love.roegStudioState())end
local function search(value)
    love.keypressed('f')
    for _=1,100 do love.keypressed('backspace') end
    love.textinput(value);love.keypressed('return')
end
local function shot(name,after)
    pending=true
    love.graphics.captureScreenshot(function(image)
        local file=assert(io.open(output..'/'..name..'.tga','wb'))
        file:write(image:encode('tga'):getString());file:close()
        pending=false
        if after then after() else love.event.quit() end
    end)
end
local function dropped(name,bytes)
    local file={}
    function file:getFilename()return name end
    function file:open()return true end
    function file:getSize()return #bytes end
    function file:read()return bytes end
    function file:close()return true end
    love.filedropped(file)
end
function love.load(args)
    old_load(args)
    if phase==1 then
        love.keypressed('n');love.textinput('Audio Fixture');love.keypressed('return')
        love.keypressed('y');assert(state().mode=='audio')
        love.keypressed('tab');love.keypressed('tab') -- Library
        love.keypressed('i') -- original step, on-screen import action
        dropped('original music.wav',Samples.wav('music')) -- file-drop callback
        local editor=state().audio_editor
        assert(#editor.pack.sounds==2 and editor.pack.sounds[1].format=='wav')
        love.keypressed('a')
        for _=1,100 do love.keypressed('backspace') end
        love.textinput('ROEG Studio synthesized');love.keypressed('return')
        love.keypressed('l')
        for _=1,100 do love.keypressed('backspace') end
        love.textinput('Original project asset');love.keypressed('return')
        love.keypressed('up');love.keypressed('p')
        assert(editor.audition and editor.audition:isPlaying(),editor.message)
        love.keypressed('x')
        love.keypressed('tab') -- Cue picker
        search('player/move');love.keypressed('right')
        local cue=assert(editor:cue())
        local move=assert(editor:binding(cue.id))
        assert(move.variants[1].asset_id==editor.pack.sounds[1].id)
        search('music/title');love.keypressed('right');love.keypressed('right')
        local music=assert(editor:binding(assert(editor:cue()).id))
        assert(music.variants[1].asset_id==editor.pack.sounds[2].id)
        love.keypressed('t')
        for _=1,30 do love.keypressed('backspace') end
        love.textinput('fade');love.keypressed('return')
        assert(assert(editor:binding(assert(editor:cue()).id)).transition.mode=='fade')
        search('music/explore/overgrown_ruins')
        love.keypressed('right');love.keypressed('right')
        local explore=assert(editor:binding(assert(editor:cue()).id))
        assert(explore.variants[1].asset_id==editor.pack.sounds[2].id and
            explore.transition.mode=='crossfade')
        love.keypressed('s');assert(not state().dirty)
        local path=love.filesystem.getSaveDirectory()
        local file=assert(io.open(output..'/save_dir.txt','wb'))
        file:write(path);file:close()
        assert(not love.filesystem.getInfo('active-a.roeg'))
    elseif phase==2 then
        love.keypressed('o');love.keypressed('return');love.keypressed('y')
        local editor=state().audio_editor
        assert(state().mode=='audio' and #editor.pack.sounds==2)
        assert(#editor.pack.bindings==3)
        local first=editor.pack.sounds[1]
        assert(love.filesystem.read(first.path)==Samples.wav('step'))
        assert(first.author=='ROEG Studio synthesized' and
            first.license=='Original project asset')
        editor:audition_sound(first)
        assert(editor.audition and editor.audition:isPlaying())
        editor:stop_audition()
        love.keypressed('escape')
        love.keypressed('u');assert(state().mode=='theme')
        love.keypressed('f8');assert(state().mode=='audio')
        assert(assert(state().audio_editor:cue()).category=='ui',
            'theme F8 did not select UI audio context')
        love.keypressed('escape');assert(state().mode=='theme')
        love.keypressed('escape');assert(state().mode=='project')
        love.keypressed('w');assert(state().mode=='world')
        love.keypressed('n')
        for _=1,30 do love.keypressed('backspace') end
        love.textinput('9x7');love.keypressed('return')
        local room=assert(state().world_editor:room())
        love.keypressed('f8');assert(state().mode=='audio')
        assert(assert(state().audio_editor:cue()).context_id==room.id,
            'room F8 did not select local ambience/music context')
        love.keypressed('s');assert(not state().dirty)
    elseif phase==3 then
        assert(tracked and tracked.clock==0)
        local info=love.roegAudioDiagnostics()
        assert(info.binding_count==3 and info.sound_count==2,
            'published audio pack was not loaded: '..tostring(info.status))
        assert(info.scene=='core:cue/music/title' and info.music_playing,
            'published music did not play in the isolated movement fixture')
        local before=info.played
        love.keypressed('d')
        assert(tracked.clock==100)
        assert(love.roegAudioDiagnostics().played>before,
            'legal movement did not play the published cue')
        local snapshot=Persistence.encode(Game.snapshot(tracked))
        local file=assert(io.open(output..'/game.snapshot','wb'))
        file:write(snapshot);file:close()
        -- Corrupt optional audio while this exact LÖVE process is running.
        local damaged=assert(io.open(output..'/app/assets/studio/audio-a.pack','wb'))
        damaged:write('torn');damaged:close()
        love.keypressed('f5');old_update(0)
        assert(love.roegAudioDiagnostics().binding_count>=20,
            'F5 did not recover original built-in audio')
        assert(Persistence.encode(Game.snapshot(tracked))==snapshot,
            'audio F5 changed authoritative state')
    elseif phase==4 then
        assert(love.roegAudioDiagnostics().binding_count>=20,
            'corrupt optional pack did not use built-in samples')
        love.keypressed('c')
        assert(tracked,'Continue did not restore saved run')
        local file=assert(io.open(output..'/game.snapshot','rb'))
        local expected=file:read('*a');file:close()
        assert(Persistence.encode(Game.snapshot(tracked))==expected,
            'audio publish/fallback altered fresh-process Continue')
    elseif phase==5 then
        local before=love.roegAudioDiagnostics()
        assert(before.scene=='core:cue/music/title' and before.music_playing,
            'published title music did not play')
        love.keypressed('n')
        old_update(0) -- scene changes at the next safe presentation update
        local after=love.roegAudioDiagnostics()
        assert(after.scene=='core:cue/music/explore/overgrown_ruins' and
            after.music_playing and after.music_transitioning and
            after.played>before.played,
            ('title to exploration did not crossfade: before=%s/%s/%s after=%s/%s/%s/%s status=%s'):
                format(tostring(before.scene),tostring(before.music_playing),
                    tostring(before.played),tostring(after.scene),
                    tostring(after.music_playing),
                    tostring(after.music_transitioning),tostring(after.played),
                    tostring(after.status)))
    end
end
function love.update(dt)
    return old_update(dt)
end
function love.draw()
    old_draw()
    if pending then return end
    if phase==1 then shot('studio-audio')
    elseif phase==2 then shot('studio-reopened')
    elseif phase==3 then shot('game-audio-f5',function()
        love.keypressed('escape');love.keypressed('2') -- real Save & Quit
        assert(love.filesystem.getInfo('active-a.roeg') or
            love.filesystem.getInfo('active-b.roeg'))
    end)
    elseif phase==4 then shot('game-fallback')
    elseif phase==5 then shot('game-music-transition') end
end
LUA
run_phase() {
    number=$1;shift
    ROEG_AUDIO_PHASE="$number" ROEG_AUDIO_OUTPUT="$output_dir" \
        ROEG_GUI_AUTOSTART=$([ "$number" -eq 3 ] && echo 1 || echo 0) \
    XDG_DATA_HOME=$([ "$number" -eq 5 ] && echo "$output_dir/music-data" ||
        echo "$output_dir/data") timeout 25s love "$app" "$@" \
        >"$output_dir/phase$number.log" 2>&1 || {
            cat "$output_dir/phase$number.log" >&2
            echo "Audio GUI failure artifacts: $output_dir" >&2
            exit 1
        }
}
run_phase 1 --studio
run_phase 2 --studio
save_dir=$(cat "$output_dir/save_dir.txt")
(cd "$app" && luajit tools/studio_audio_publish.lua --save-dir "$save_dir" \
    --project audio_fixture) >"$output_dir/audio-dry-run.log"
test ! -e "$app/assets/studio/audio-a.pack"
(cd "$app" && luajit tools/studio_audio_publish.lua --save-dir "$save_dir" \
    --project audio_fixture --apply) >"$output_dir/audio-publish.log"
run_phase 5
run_phase 3
run_phase 4
for name in studio-audio studio-reopened game-audio-f5 game-fallback game-music-transition; do
    test -s "$output_dir/$name.tga"
done
printf 'M7 audio GUI smoke passed: %s\n' "$output_dir"
