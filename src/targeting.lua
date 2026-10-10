local Targeting = {}

-- Clockwise compass order. Sweep emits counterclockwise, center, clockwise.
local directions = {
    { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 },
    { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 },
}

function Targeting.direction_index(dx, dy)
    for index, direction in ipairs(directions) do
        if dx == direction[1] and dy == direction[2] then return index end
    end
    return nil
end

function Targeting.is_cardinal(dx, dy)
    local index = Targeting.direction_index(dx, dy)
    return index ~= nil and index % 2 == 1
end

function Targeting.cells(kind, x, y, dx, dy)
    local index = Targeting.direction_index(dx, dy)
    if not index then return nil end
    if kind == "basic_attack" then
        return {{ x = x + dx, y = y + dy }}
    end
    if kind == "sweeping_slash" then
        local result = {}
        for _, offset in ipairs({ -1, 0, 1 }) do
            local direction = directions[(index + offset - 1) % 8 + 1]
            result[#result + 1] = { x = x + direction[1], y = y + direction[2] }
        end
        return result
    end
    return nil
end

return Targeting
