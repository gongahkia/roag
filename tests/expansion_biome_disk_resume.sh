#!/bin/sh
# Three new biome identities, each saved/continued in separate LÖVE processes.
# The direct stage fixture is assisted; the wait and continuation are ordinary
# Game.submit actions and complete concrete maps are compared byte-for-byte.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/var/tmp}/roag-expansion-biome-resume-XXXXXX")
app="$output_dir/app"
mkdir -p "$app"
cp "$project_dir/main.lua" "$app/roag_main.lua"
cp "$project_dir/conf.lua" "$app/conf.lua"
cp -R "$project_dir/src" "$app/src"
cat > "$app/main.lua" <<'LUA'
local Game=require('src.game')
local Biomes=require('src.biomes')
local Persistence=require('src.persistence')
local id=assert(os.getenv('ROAG_EXPANSION_RESUME_ID'))
local phase=assert(os.getenv('ROAG_EXPANSION_RESUME_PHASE'))
local output=assert(os.getenv('ROAG_EXPANSION_RESUME_OUTPUT'))
local index,history
if id==Biomes.order[5] then
    index,history=2,{'core:route/glacial_observatory'}
elseif id==Biomes.order[6] then
    index,history=3,{'core:route/fungal_hollows','core:route/fractured_archive'}
else
    index,history=3,{'core:route/sunken_aqueduct','core:route/sundered_forge'}
end
local tracked
local old_new,old_restore=Game.new_stage,Game.restore
function Game.new_stage(seed,options)
    tracked=old_new(seed,{multifloor=true,root_seed=seed,
        stage_options={index=index,biome_id=id,route_history=history}})
    tracked.meta.eligible=false -- direct stage fixture
    return tracked
end
function Game.restore(...)
    local restored=old_restore(...)
    if phase=='load' then tracked=restored end
    return restored
end
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_draw=love.load,love.draw
local pending,done,continued=false,false,false
local function write(name,bytes)
    local file=assert(io.open(output..'/'..name,'wb'))
    file:write(bytes);file:close()
end
local function read(name)
    local file=assert(io.open(output..'/'..name,'rb'))
    local bytes=file:read('*a');file:close();return bytes
end
function love.load(...)
    old_load(...)
    if phase=='save' then
        love.keypressed('n')
        assert(tracked.stage.biome_id==id and #tracked.floor_order==2)
        assert(Game.submit(tracked,{kind='wait'}))
        write('expected',Persistence.encode(Game.snapshot(tracked)))
        local control=Game.restore(Game.snapshot(tracked))
        assert(Game.submit(control,{kind='wait'}))
        write('next',Persistence.encode(Game.snapshot(control)))
        love.keypressed('escape')
    end
end
function love.draw(...)
    old_draw(...)
    if pending or done then return end
    pending=true
    love.graphics.captureScreenshot(function(image)
        write(phase..'.png',image:encode('png'):getString())
        if phase=='save' then
            love.keypressed('2')
            done=true
        elseif not continued then
            love.keypressed('c')
            assert(tracked and tracked.stage.biome_id==id
                and Persistence.encode(Game.snapshot(tracked))==read('expected'),
                'fresh-process biome mismatch')
            continued=true
        else
            assert(Game.submit(tracked,{kind='wait'}))
            assert(Persistence.encode(Game.snapshot(tracked))==read('next'),
                'continued biome outcome differs')
            love.event.quit();done=true
        end
        pending=false
    end)
end
LUA
for biome_id in core:biome/glacial_observatory core:biome/fractured_archive core:biome/sundered_forge; do
    name=${biome_id##*/}
    mkdir -p "$output_dir/data-$name"
    for phase in save load; do
        ROAG_EXPANSION_RESUME_ID="$biome_id" \
            ROAG_EXPANSION_RESUME_PHASE="$phase" \
            ROAG_EXPANSION_RESUME_OUTPUT="$output_dir" \
            XDG_DATA_HOME="$output_dir/data-$name" \
            timeout 30s love "$app" > "$output_dir/$name-$phase.log" 2>&1 || {
                cat "$output_dir/$name-$phase.log"; exit 1;
            }
    done
done
test -s "$output_dir/expected"
test -s "$output_dir/next"
printf 'New-biome fresh-process disk resumes passed: %s\n' "$output_dir"
