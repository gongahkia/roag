#!/bin/sh
# Opt-in callback-driven rendering samples under the current display/renderer.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=${1:-$(mktemp -d "${TMPDIR:-/tmp}/roeg-gui-profile-XXXXXX")}
mkdir -p "$output_dir/data"
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local mode=assert(os.getenv('ROEG_PROFILE_SCENE'))
if mode=='boon' then
    function Game.new_stage(seed)
        return Game.new(seed,'core:map/scrolling_room',
            {demo_enemies=true,demo_chests=true})
    end
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw=love.load,love.draw
local frames=0
local output=assert(os.getenv('ROEG_PROFILE_OUTPUT'))
function love.load(...)
    old_load(...)
    if mode=='boon' then
        love.keypressed('b');love.keypressed('b')
        for _=1,3 do love.keypressed('d') end
        love.keypressed('f');love.keypressed('d');love.keypressed('return')
    end
end
function love.draw(...)
    old_draw(...)
    frames=frames+1
    if mode=='generated' then
        if frames==50 then love.keypressed('tab') end
        if frames==100 then love.keypressed('tab') end
        if frames>120 and frames<165 and frames%4==0 then
            love.keypressed('d')
        end
    end
    if frames==180 then
        local data=assert(love.roegDiagnostics())
        local file=assert(io.open(output..'/'..mode..'.tsv','w'))
        for _,field in ipairs({'samples','frame_p50_ms','frame_p95_ms',
            'frame_p99_ms','update_p95_ms','draw_p95_ms',
            'visible_actors','draw_calls','texture_bytes'}) do
            file:write(field,'\t',tostring(data[field]),'\n')
        end
        file:write('fps\t',love.timer.getFPS(),'\n')
        file:write('average_delta_s\t',love.timer.getAverageDelta(),'\n')
        if love.graphics.getRendererInfo then
            local name,version,vendor,device=love.graphics.getRendererInfo()
            file:write('renderer\t',tostring(name),' ',tostring(version),' ',
                tostring(vendor),' ',tostring(device),'\n')
        end
        file:close()
        love.graphics.captureScreenshot(function(image)
            local screenshot=assert(io.open(output..'/'..mode..'.png','wb'))
            screenshot:write(image:encode('png'):getString())
            screenshot:close()
            love.event.quit()
        end)
    end
end
LUA
for scene in generated boon; do
    ROEG_PROFILE_SCENE="$scene" ROEG_PROFILE_OUTPUT="$output_dir" \
      ROEG_GUI_AUTOSTART=1 ROEG_DIAGNOSTICS=1 XDG_DATA_HOME="$output_dir/data" \
      timeout 30s love "$output_dir"
    test -s "$output_dir/$scene.tsv"
    test -s "$output_dir/$scene.png"
done
printf 'GUI profile results: %s\n' "$output_dir"
