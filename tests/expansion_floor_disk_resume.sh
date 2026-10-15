#!/bin/sh
# Assisted stair setup; production Save & Quit/Continue in two isolated LÖVE processes.
set -eu
project_dir=$(CDPATH= cd "$(dirname "$0")/.." && pwd)
output_dir=$(mktemp -d "${TMPDIR:-/tmp}/roag-floor-resume-XXXXXX")
cp "$project_dir/main.lua" "$output_dir/roag_main.lua"
cp "$project_dir/conf.lua" "$output_dir/conf.lua"
cp -R "$project_dir/src" "$output_dir/src"
cat > "$output_dir/main.lua" <<'LUA'
local Game=require('src.game')
local Content=require('src.content')
local Persistence=require('src.persistence')
local Visibility=require('src.visibility')
local tracked
local original_new,original_restore=Game.new_stage,Game.restore
function Game.new_stage(seed,options)
    assert(options and options.multifloor==true,
        'normal New Run must request connected floors')
    tracked=original_new(seed,options)
    return tracked
end
function Game.restore(...)
    local restored=original_restore(...)
    if os.getenv('ROAG_FLOOR_RESUME_PHASE')=='load' then tracked=restored end
    return restored
end
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_draw,old_update=love.load,love.draw,love.update
local phase=assert(os.getenv('ROAG_FLOOR_RESUME_PHASE'))
local output=assert(os.getenv('ROAG_FLOOR_RESUME_OUTPUT'))
local pending,done,continued=false,false,false
local function write(name,bytes)
    local file=assert(io.open(output..'/'..name,'wb'))
    file:write(bytes);file:close()
end
local function read(name)
    local file=assert(io.open(output..'/'..name,'rb'))
    local bytes=file:read('*a');file:close();return bytes
end
local function stair_direction()
    local stair=tracked.generated_map.placements.stairs[1]
    local p=Game.player(tracked)
    for _,d in ipairs({{1,0,'d'},{-1,0,'a'},{0,1,'s'},{0,-1,'w'}}) do
        local x,y=stair.x-d[1],stair.y-d[2]
        local free=Content.walkable(tracked.generated_map,x,y)
        for _,entity in ipairs(tracked.entities) do
            if entity.id~=p.id and entity.blocks_movement
                and entity.position.floor_id==tracked.active_floor_id
                and entity.position.x==x and entity.position.y==y then free=false end
        end
        if free then
            p.position.x,p.position.y=x,y
            tracked.meta.eligible=false
            Visibility.recompute(tracked)
            return d[3]
        end
    end
    error('no stair approach')
end
function love.load(...)
    old_load(...)
    if phase=='save' then
        love.keypressed('n')
        assert(tracked.floor_order and tracked.active_floor_index==1)
        old_update(10)
        local direction=stair_direction()
        local offsets={d={1,0},a={-1,0},s={0,1},w={0,-1}}
        local offset=offsets[direction]
        local preview=Game.preview(tracked,'move',offset[1],offset[2])
        assert(preview.valid and preview.stair,
            'stair preview: '..tostring(preview.reason))
        love.keypressed(direction)
        assert(tracked.active_floor_index==2,
            'stair key ignored; clock '..tostring(tracked.clock))
        write('expected',Persistence.encode(Game.snapshot(tracked)))
        local control=Game.restore(Game.snapshot(tracked))
        assert(Game.submit(control,{kind='wait'}))
        write('next',Persistence.encode(Game.snapshot(control)))
        old_update(10)
        love.keypressed('escape')
    end
end
function love.update() old_update(0) end
function love.draw(...)
    old_draw(...)
    if done or pending then return end
    pending=true
    love.graphics.captureScreenshot(function(image)
        write(phase..'.png',image:encode('png'):getString())
        if phase=='save' then
            love.keypressed('2')
        elseif not continued then
            love.keypressed('c')
            assert(tracked and tracked.active_floor_index==2)
            assert(Persistence.encode(Game.snapshot(tracked))==read('expected'),
                'fresh-process connected floor mismatch')
            continued=true;pending=false;return
        else
            love.keypressed('space')
            assert(Persistence.encode(Game.snapshot(tracked))==read('next'),
                'continued connected floor outcome differs')
            love.event.quit()
        end
        done=true;pending=false
    end)
end
LUA
mkdir -p "$output_dir/data"
ROAG_FLOOR_RESUME_PHASE=save ROAG_FLOOR_RESUME_OUTPUT="$output_dir" \
    XDG_DATA_HOME="$output_dir/data" timeout 30s love "$output_dir"
ROAG_FLOOR_RESUME_PHASE=load ROAG_FLOOR_RESUME_OUTPUT="$output_dir" \
    XDG_DATA_HOME="$output_dir/data" timeout 30s love "$output_dir"
test -s "$output_dir/save.png"
test -s "$output_dir/load.png"
printf 'Connected-floor fresh-process resume passed: %s\n' "$output_dir"
