-- Presentation coordinates only. Never changes game state or world time.
local Camera = {}

local function target(camera, x, y)
    local world_width = camera.map_width * camera.tile
    local world_height = camera.map_height * camera.tile
    local half_width, half_height = camera.viewport.width / 2, camera.viewport.height / 2
    local center_x, center_y = (x - 0.5) * camera.tile, (y - 0.5) * camera.tile
    if world_width <= camera.viewport.width then
        center_x = world_width / 2
    else
        center_x = math.max(half_width, math.min(world_width - half_width, center_x))
    end
    if world_height <= camera.viewport.height then
        center_y = world_height / 2
    else
        center_y = math.max(half_height, math.min(world_height - half_height, center_y))
    end
    return center_x, center_y
end

function Camera.new(map, viewport, tile, player_x, player_y)
    local camera = {
        map_width = map.width,
        map_height = map.height,
        viewport = viewport,
        tile = tile,
        time_constant = 0.12,
        offset_x = 0,
        offset_y = 0,
    }
    camera.x, camera.y = target(camera, player_x, player_y)
    return camera
end

function Camera.set_presentation_offset(camera, x, y)
    camera.offset_x, camera.offset_y = x, y
end

function Camera.set_tile(camera, tile, player_x, player_y)
    assert(type(tile)=='number' and tile>0 and tile%1==0,
        'invalid presentation tile size')
    camera.tile=tile
    camera.x,camera.y=target(camera,player_x,player_y)
end

function Camera.update(camera, dt, player_x, player_y)
    local goal_x, goal_y = target(camera, player_x, player_y)
    local alpha = 1 - math.exp(-math.max(dt, 0) / camera.time_constant)
    camera.x = camera.x + (goal_x - camera.x) * alpha
    camera.y = camera.y + (goal_y - camera.y) * alpha
end

function Camera.tile_top_left(camera, x, y)
    local viewport = camera.viewport
    -- Round the shared world-to-screen offset for tiles and sprites alike.
    -- Both stay crisp while the camera itself retains subpixel easing.
    return math.floor(viewport.x + viewport.width / 2 + (x - 1) * camera.tile - camera.x + 0.5)
            + camera.offset_x,
        math.floor(viewport.y + viewport.height / 2 + (y - 1) * camera.tile - camera.y + 0.5)
            + camera.offset_y
end

return Camera
