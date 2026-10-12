#!/bin/sh
# Callback-driven presentation sample under the current display/renderer.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
result=$(sh "$project_dir/tests/studio_e04_gui_smoke.sh")
printf '%s\n' "$result"
output_dir=${result##* in }
app="$output_dir/app"
cp "$output_dir/good-theme.pack" "$app/assets/studio/theme-a.pack"
cat > "$app/main.lua" <<'LUA'
local Game=require('src.game')
local Playback=require('src.playback')
local original_new=Game.new_stage
Game.new_stage=function()
    return Game.new(123,'core:map/scrolling_room',
        {demo_enemies=true,demo_chests=true})
end
local queue
local old_add=Playback.add
Playback.add=function(p,...)
    queue=p;return old_add(p,...)
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw=love.load,love.draw
local times,intervals={},{}
local output=assert(os.getenv('ROEG_E04_PROFILE_OUTPUT'))
local mode=assert(os.getenv('ROEG_E04_PROFILE_MODE'))
local frame=0
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
    f:write(('draw_ms_p50=%.3f p95=%.3f max=%.3f\n'):format(
        percentile(times,.5),percentile(times,.95),times[#times]))
    f:write(('frame_interval_ms_p50=%.3f p95=%.3f p99=%.3f\n'):format(
        percentile(intervals,.5),percentile(intervals,.95),percentile(intervals,.99)))
    local w,h=love.graphics.getDimensions()
    f:write(('window=%dx%d dpi_scale=%s drawcalls=%s texturememory=%s lua_heap_kib=%.1f fps=%s\n'):format(
        w,h,tostring(love.window.getDPIScale()),tostring(stats.drawcalls),
        tostring(stats.texturememory),collectgarbage('count'),
        tostring(love.timer.getFPS())))
    f:close();love.event.quit()
end
LUA
for mode in themed fallback; do
    if [ "$mode" = fallback ]; then
        mv "$app/assets/studio/theme-a.pack" "$output_dir/profile-theme.off"
    fi
    ROEG_GUI_AUTOSTART=1 ROEG_E04_PROFILE_MODE="$mode" \
        ROEG_E04_PROFILE_OUTPUT="$output_dir/profile-$mode.txt" \
        XDG_DATA_HOME="$output_dir/data" timeout 25s love "$app" \
        >"$output_dir/profile-$mode.log" 2>&1
    test -s "$output_dir/profile-$mode.txt"
    cat "$output_dir/profile-$mode.txt"
done
printf 'E04 profile artifacts: %s\n' "$output_dir"
