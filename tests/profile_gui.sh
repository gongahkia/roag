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
local Biomes=require('src.biomes')
local mode=assert(os.getenv('ROEG_PROFILE_SCENE'))
local tracked,boon_activations
boon_activations=0
if mode=='fungal' or mode=='aqueduct' or mode=='bastion' then
    local id=mode=='fungal' and Biomes.order[2]
        or mode=='aqueduct' and Biomes.order[3] or Biomes.order[4]
    local original=Game.new_stage
    function Game.new_stage(seed)
        local index=mode=='bastion' and 3 or 2
        tracked=original(seed,{stage_options={index=index,biome_id=id,
            route_history=index==3 and {"core:route/fungal_hollows",
                "core:route/ashen_bastion"}
                or {(id:gsub('^core:biome/','core:route/'))}}})
        return tracked
    end
end
if mode=='finale' then
    function Game.new_stage(seed)
        tracked=Game.new_finale(seed,{root_seed=seed,route_history={
            'core:route/fungal_hollows','core:route/ashen_bastion',
            'core:route/gateway'}})
        local boss=Game.entity(tracked,tracked.finale.boss_id)
        local player=Game.player(tracked)
        player.position.x,player.position.y=boss.position.x,boss.position.y+1
        require('src.visibility').recompute(tracked)
        return tracked
    end
end
if mode=='boon' or mode=='crowd' then
    function Game.new_stage(seed)
        tracked=Game.new(seed,'core:map/scrolling_room',
            mode=='boon' and {demo_enemies=true,demo_chests=true} or nil)
        if mode=='crowd' then
            local Content=require('src.content')
            local map=Content.map_for(tracked)
            local count=0
            for y=2,10 do
                for x=2,14 do
                    if count<40 and not (x==3 and y==3) and
                        Content.walkable(map,x,y) then
                        Game.spawn_enemy(tracked,'core:enemy/mossbound_guard',x,y)
                        count=count+1
                    end
                end
            end
            assert(count==40,'crowd fixture is too small')
        end
        return tracked
    end
end
local actual_submit=Game.submit
function Game.submit(...)
    local ok,reason,events,preview=actual_submit(...)
    for _,event in ipairs(events or {}) do
        if event.kind=='BoonActivated' then boon_activations=boon_activations+1 end
    end
    return ok,reason,events,preview
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local frames=0
local output=assert(os.getenv('ROEG_PROFILE_OUTPUT'))
function love.load(...)
    old_load(...)
    if mode=='finale' then love.keypressed('space') end
    if mode=='crowd' then
        local before=tracked.clock
        love.keypressed('space') -- real committed wait; 40 actors move/attack
        assert(tracked.clock>before,'crowd action did not commit')
    end
    if mode=='boon' then
        love.keypressed('b');love.keypressed('b')
        local function settle()
            local ticks=0
            while love.roegPlaybackPending() do
                old_update(0.25)
                ticks=ticks+1
                assert(ticks<400,'boon scene playback did not drain')
            end
        end
        for _=1,2 do love.keypressed('d');settle() end
        local p=Game.player(tracked).position
        local buttons={['-1,-1']='q',['0,-1']='w',['1,-1']='e',
            ['-1,0']='a',['1,0']='d',['-1,1']='z',
            ['0,1']='s',['1,1']='c'}
        local target
        for _,entity in ipairs(tracked.entities) do
            local dx,dy=entity.position.x-p.x,entity.position.y-p.y
            if entity.controller=='ai' and buttons[dx..','..dy] then
                target=buttons[dx..','..dy];break
            end
        end
        assert(target,'no adjacent target in boon profile scene')
        love.keypressed('f');love.keypressed(target);love.keypressed('return')
        assert(boon_activations>0,'prepared hit did not activate boon')
    end
end
function love.draw(...)
    old_draw(...)
    frames=frames+1
    if mode=='generated' or mode=='fungal' or mode=='aqueduct'
        or mode=='bastion' then
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
        file:write('boon_activations\t',boon_activations,'\n')
        local width,height=love.graphics.getDimensions()
        local desktop_width,desktop_height=love.window.getDesktopDimensions()
        file:write('window_logical\t',width,'x',height,'\n')
        file:write('desktop_logical\t',desktop_width,'x',desktop_height,'\n')
        file:write('dpi_scale\t',love.window.getDPIScale(),'\n')
        file:write('lua_heap_kib\t',collectgarbage('count'),'\n')
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
for scene in generated boon crowd fungal aqueduct bastion finale; do
    ROEG_PROFILE_SCENE="$scene" ROEG_PROFILE_OUTPUT="$output_dir" \
      ROEG_GUI_AUTOSTART=1 ROEG_DIAGNOSTICS=1 XDG_DATA_HOME="$output_dir/data" \
      timeout 30s love "$output_dir"
    test -s "$output_dir/$scene.tsv"
    test -s "$output_dir/$scene.png"
done
printf 'GUI profile results: %s\n' "$output_dir"
