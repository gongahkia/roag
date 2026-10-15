#!/bin/sh
# Atlas versus loose presentation only, on one isolated E04 fixture.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
result=$(TMPDIR="${TMPDIR:-/var/tmp}" sh "$project_dir/tests/studio_e05_gui_smoke.sh")
printf '%s\n' "$result"
out=${result##* in }
e04=$(awk '{print $NF}' "$out/e04-fixture.log" | tail -1)
app="$e04/app"
cp "$e04/good-theme.pack" "$app/assets/studio/theme-a.pack"
cat >"$app/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
Game.new_stage=function()
    return Game.new(123,'core:map/scrolling_room',
        {demo_enemies=true,demo_chests=true})
end
local queue
local add=Playback.add
Playback.add=function(p,...)
    queue=p;return add(p,...)
end
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_draw=love.load,love.draw
local times,intervals={},{}
local frame=0
local output=assert(os.getenv('ROAG_E05_PROFILE_OUTPUT'))
local mode=assert(os.getenv('ROAG_E05_PROFILE_MODE'))
local function percentile(list,p)
    table.sort(list)
    return list[math.max(1,math.min(#list,math.ceil(#list*p)))]
end
function love.load(args)
    old_load(args)
    pcall(love.window.setVSync,0)
    love.keypressed('space')
    for _=1,30 do
        local event=queue and Playback.current(queue)
        if event and event.kind=='DamageTaken' then break end
        Playback.update(queue,.1)
    end
    assert(Playback.current(queue).kind=='DamageTaken')
    assert(love.roagAssetBacking('studio:ui/heart')==mode)
end
function love.update(dt)
    if frame>0 then intervals[#intervals+1]=dt*1000 end
end
function love.draw()
    local start=love.timer.getTime()
    old_draw()
    times[#times+1]=(love.timer.getTime()-start)*1000
    frame=frame+1
    if frame<180 then return end
    local a,b,c,d=love.graphics.getRendererInfo()
    local stats=love.graphics.getStats()
    local f=assert(io.open(output,'wb'))
    f:write(('mode=%s renderer=%s/%s/%s/%s frames=%d\n'):format(
        mode,tostring(a),tostring(b),tostring(c),tostring(d),frame))
    f:write(('draw_cpu_ms_p50=%.3f p95=%.3f max=%.3f\n'):format(
        percentile(times,.5),percentile(times,.95),times[#times]))
    f:write(('observed_frame_ms_p50=%.3f p95=%.3f p99=%.3f\n'):format(
        percentile(intervals,.5),percentile(intervals,.95),percentile(intervals,.99)))
    local w,h=love.graphics.getDimensions()
    f:write(('window=%dx%d dpi_scale=%s drawcalls=%s texturememory=%s lua_heap_kib=%.1f fps=%s\n'):format(
        w,h,tostring(love.window.getDPIScale()),tostring(stats.drawcalls),
        tostring(stats.texturememory),collectgarbage('count'),
        tostring(love.timer.getFPS())))
    f:close();love.event.quit()
end
LUA
for mode in atlas loose; do
    if [ "$mode" = loose ]; then
        (cd "$app" && luajit tools/studio_atlas_publish.lua --disable --apply \
            >"$out/profile-disable.log")
    fi
    ROAG_GUI_AUTOSTART=1 ROAG_E05_PROFILE_MODE="$mode" \
        ROAG_E05_PROFILE_OUTPUT="$out/profile-$mode.txt" \
        XDG_DATA_HOME="$e04/data" timeout 25s love "$app" \
        >"$out/profile-$mode.log" 2>&1
    test -s "$out/profile-$mode.txt"
    cat "$out/profile-$mode.txt"
done
printf 'E05 presentation profile artifacts: %s\n' "$out"
