#!/bin/sh
# Two fresh LÖVE processes per new biome, using isolated save directories.
# Stage selection is a prepared fixture; save/Continue and subsequent gameplay
# go through real UI callbacks and the production disk adapter.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roeg-biome-resume-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roeg_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Biomes=require('src.biomes')
local Persistence=require('src.persistence')
local id=assert(os.getenv('ROEG_RESUME_BIOME'))
local tracked
local old_new,old_restore,old_submit=Game.new_stage,Game.restore,Game.submit
function Game.new_stage(seed,options)
    tracked=old_new(seed,{root_seed=seed,stage_options={index=2,
        biome_id=id,route_history={(id:gsub('^core:biome/','core:route/'))}}})
    return tracked
end
function Game.restore(...)
    tracked=old_restore(...);return tracked
end
function Game.submit(state,...)
    tracked=state;return old_submit(state,...)
end
assert(love.filesystem.load('roeg_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local phase=assert(os.getenv('ROEG_RESUME_PHASE'))
local out=assert(os.getenv('ROEG_RESUME_OUTPUT'))
local name=id:match('[^/]+$')
local labels,done,pending,continued={},false,false,false
local old_print,old_printf=love.graphics.print,love.graphics.printf
love.graphics.print=function(v,...) labels[#labels+1]=tostring(v);return old_print(v,...) end
love.graphics.printf=function(v,...) labels[#labels+1]=tostring(v);return old_printf(v,...) end
local function has(fragment)
    for _,v in ipairs(labels) do if v:find(fragment,1,true) then return true end end
end
local function write(path,bytes)
    local f=assert(io.open(path,'wb'));f:write(bytes);f:close()
end
local function read(path)
    local f=assert(io.open(path,'rb'));local bytes=f:read('*a');f:close();return bytes
end
function love.load(...)
    old_load(...)
    if phase=='save' then
        love.keypressed('n')
        assert(tracked.stage.biome_id==id and tracked.stage.index==2)
        love.keypressed('space')
        assert(tracked.clock==100)
        write(out..'/'..name..'.expected',Persistence.encode(Game.snapshot(tracked)))
        local control=old_restore(Game.snapshot(tracked))
        assert(old_submit(control,{kind='wait'}))
        write(out..'/'..name..'.next',Persistence.encode(Game.snapshot(control)))
        old_update(10)
        love.keypressed('escape')
    end
end
function love.draw(...)
    labels={};old_draw(...)
    if done or pending then return end
    if phase=='load' and not continued then
        assert(has('C / Enter: Continue'))
        pending=true
        love.graphics.captureScreenshot(function(image)
            write(out..'/'..name..'-title.png',image:encode('png'):getString())
            love.keypressed('c')
            assert(tracked and tracked.stage.biome_id==id and
                Persistence.encode(Game.snapshot(tracked))==
                read(out..'/'..name..'.expected'))
            continued=true
            pending=false
        end)
        return
    end
    assert(tracked and tracked.stage.biome_id==id and has(Biomes.get(id).name),
        'missing biome HUD: '..table.concat(labels,' | '))
    pending=true
    love.graphics.captureScreenshot(function(image)
        write(out..'/'..name..'-'..phase..'.png',image:encode('png'):getString())
        if phase=='save' then love.keypressed('2')
        else
            love.keypressed('space')
            assert(Persistence.encode(Game.snapshot(tracked))==
                read(out..'/'..name..'.next'))
            love.event.quit()
        end
        done=true;pending=false
    end)
end
LUA
for name in fungal_hollows sunken_aqueduct ashen_bastion; do
    data_dir="$output_dir/data-$name"
    mkdir -p "$data_dir"
    ROEG_RESUME_BIOME="core:biome/$name" ROEG_RESUME_PHASE=save \
      ROEG_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$data_dir" \
      timeout 25s love "$output_dir"
    ROEG_RESUME_BIOME="core:biome/$name" ROEG_RESUME_PHASE=load \
      ROEG_RESUME_OUTPUT="$output_dir" XDG_DATA_HOME="$data_dir" \
      timeout 25s love "$output_dir"
    test -s "$output_dir/$name-save.png"
    test -s "$output_dir/$name-title.png"
    test -s "$output_dir/$name-load.png"
done
printf 'Biome disk resume passed: six LÖVE processes in %s\n' "$output_dir"
