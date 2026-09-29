local Grid = {
  width = 80,
  height = 50,
}

function Grid.key(x, y)
  return x .. ":" .. y
end

function Grid.cell(x, y)
  return { x = x, y = y }
end

function Grid.copy(values)
  local result = {}
  for key, value in pairs(values) do
    result[key] = value
  end
  return result
end

function Grid.distance(a, b)
  return math.abs(a.x - b.x) + math.abs(a.y - b.y)
end

function Grid.in_bounds(x, y)
  return x >= 0 and x < Grid.width and y >= 0 and y < Grid.height
end

function Grid.neighbours(point)
  return {
    Grid.cell(point.x, point.y + 1),
    Grid.cell(point.x - 1, point.y),
    Grid.cell(point.x, point.y - 1),
    Grid.cell(point.x + 1, point.y),
  }
end

return Grid
