-- Standalone mouse-driven mapper for ROAG's Kenney 1-Bit sprite sheet.
-- Launch from the repository root with: love sprite_editor

local COLUMNS, ROWS, SOURCE_TILE_SIZE = 49, 22, 16
local JSON_FILE = "mappings.json"

local DEFAULT_MAPPINGS = {
  player={25,1}, target={38,3}, ammo={23,5}, torch={20,7}, door={22,1},
  bullet={34,3}, bomb={38,6}, flare={23,6}, necromancer={27,10}, wolf={31,9},
  bomber={20,9}, cultist={28,10}, boss={30,2},
}
local ROLES = {
  {key="player", label="Player"}, {key="target", label="Target"},
  {key="ammo", label="Ammo"}, {key="torch", label="Torch"},
  {key="door", label="Exit door"}, {key="bullet", label="Bullet"},
  {key="bomb", label="Bomb"}, {key="flare", label="Flare"},
  {key="wolf", label="Wolf"}, {key="bomber", label="Bomber"},
  {key="necromancer", label="Necromancer"}, {key="cultist", label="Cultist"},
  {key="boss", label="Boss"},
}

local mappings, selected_role, sheet, quads, dirty, status, json_path, fonts
local sheet_zoom, sheet_pan_x, sheet_pan_y, dragging

local function clone_mappings(source)
  local result={}
  for key,tile in pairs(source) do result[key]={tile[1],tile[2]} end
  return result
end

local function set_color(r,g,b,a) love.graphics.setColor(r,g,b,a or 1) end
local function clamp(value,minimum,maximum) return math.max(minimum,math.min(maximum,value)) end
local function font_for(scale)
  if scale>=1.75 then return fonts.title end
  if scale>=1.1 then return fonts.large end
  if scale>=.9 then return fonts.normal end
  if scale>=.75 then return fonts.small end
  return fonts.tiny
end
local function draw_text(value,x,y,size,r,g,b)
  love.graphics.setFont(font_for(size or 1))
  set_color(r or 1,g or 1,b or 1)
  love.graphics.print(value,x,y)
end

local function editor_layout()
  local width,height=love.graphics.getDimensions()
  local viewport_x,viewport_y=430,86
  local viewport_width,viewport_height=math.max(180,width-458),math.max(160,height-190)
  local base_scale=math.min(1,math.max(.5,math.min(viewport_width/(COLUMNS*SOURCE_TILE_SIZE),viewport_height/(ROWS*SOURCE_TILE_SIZE))))
  local base_tile_size=SOURCE_TILE_SIZE*base_scale
  local tile_size=base_tile_size*sheet_zoom
  local sheet_width,sheet_height=COLUMNS*tile_size,ROWS*tile_size
  local base_sheet_x=viewport_x+(viewport_width-COLUMNS*base_tile_size)/2
  local base_sheet_y=viewport_y+(viewport_height-ROWS*base_tile_size)/2
  return {
    width=width, height=height, tile_size=tile_size,
    base_sheet_x=base_sheet_x, base_sheet_y=base_sheet_y,
    sheet_x=base_sheet_x+sheet_pan_x, sheet_y=base_sheet_y+sheet_pan_y,
    sheet_width=sheet_width, sheet_height=sheet_height,
    viewport_x=viewport_x, viewport_y=viewport_y,
    viewport_width=viewport_width, viewport_height=viewport_height,
    role_x=24, role_y=154, role_width=380, role_height=30,
    button_y=height-76,
  }
end

local function selected()
  return ROLES[selected_role]
end

local function valid_tile(column,row)
  return column>=1 and column<=COLUMNS and row>=1 and row<=ROWS
end

local function serialize_json()
  local lines={"{", "  \"version\": 1,", "  \"sprites\": {"}
  for index,role in ipairs(ROLES) do
    local tile=mappings[role.key]
    local suffix=index==#ROLES and "" or ","
    lines[#lines+1]=string.format("    \"%s\": {\"column\": %d, \"row\": %d}%s",role.key,tile[1],tile[2],suffix)
  end
  lines[#lines+1]="  }"
  lines[#lines+1]="}"
  return table.concat(lines,"\n").."\n"
end

local function deserialize_json(contents)
  local loaded,count={},0
  for key,column,row in contents:gmatch('\"([%w_]+)\"%s*:%s*{%s*\"column\"%s*:%s*(%d+)%s*,%s*\"row\"%s*:%s*(%d+)%s*}') do
    column,row=tonumber(column),tonumber(row)
    if DEFAULT_MAPPINGS[key] and valid_tile(column,row) then
      loaded[key]={column,row}
      count=count+1
    end
  end
  if count==0 then return nil,"No valid ROAG mappings were found in "..JSON_FILE.."." end
  for key,default in pairs(DEFAULT_MAPPINGS) do
    mappings[key]=loaded[key] or {default[1],default[2]}
  end
  return true
end

local function read_local_json()
  local file,err=io.open(json_path,"rb")
  if not file then return nil,err end
  local contents=file:read("*a")
  file:close()
  return contents
end

local function write_local_json(contents)
  local file,err=io.open(json_path,"wb")
  if not file then return nil,err end
  local ok,write_error=file:write(contents)
  file:close()
  if not ok then return nil,write_error end
  return true
end

local function save_json()
  local ok,err=write_local_json(serialize_json())
  if ok then
    dirty=false
    status="Saved "..json_path
  else
    status="Could not save: "..tostring(err)
  end
end

local function load_json()
  local contents,err=read_local_json()
  if not contents then
    status="No "..JSON_FILE.." yet. Choose tiles, then click SAVE JSON."
    return
  end
  local ok,message=deserialize_json(contents)
  if ok then
    dirty=false
    status="Loaded "..json_path
  else
    status=message or tostring(err)
  end
end

local function button_layout(layout)
  local width,gap=116,8
  return {
    {label="SAVE JSON", x=24, y=layout.button_y, width=width, action=save_json},
    {label="LOAD JSON", x=24+(width+gap), y=layout.button_y, width=width, action=load_json},
    {label="CLOSE", x=24+(width+gap)*2, y=layout.button_y, width=width, action=love.event.quit},
  }
end

local function point_in_rect(x,y,rect)
  return x>=rect.x and x<=rect.x+rect.width and y>=rect.y and y<=rect.y+(rect.height or 34)
end

local function point_in_viewport(x,y,layout)
  return x>=layout.viewport_x and x<layout.viewport_x+layout.viewport_width and y>=layout.viewport_y and y<layout.viewport_y+layout.viewport_height
end

local function tile_at(x,y,layout)
  if not point_in_viewport(x,y,layout) then return nil end
  local column=math.floor((x-layout.sheet_x)/layout.tile_size)+1
  local row=math.floor((y-layout.sheet_y)/layout.tile_size)+1
  if valid_tile(column,row) then return column,row end
end

function love.load()
  love.graphics.setDefaultFilter("nearest","nearest")
  fonts={
    tiny=love.graphics.newFont(11), small=love.graphics.newFont(12),
    normal=love.graphics.newFont(14), large=love.graphics.newFont(20),
    title=love.graphics.newFont(32),
  }
  json_path=love.filesystem.getSource().."/"..JSON_FILE

  sheet=love.graphics.newImage("colored-transparent_packed.png")
  quads={}
  for column=1,COLUMNS do
    for row=1,ROWS do
      quads[column..":"..row]=love.graphics.newQuad((column-1)*SOURCE_TILE_SIZE,(row-1)*SOURCE_TILE_SIZE,SOURCE_TILE_SIZE,SOURCE_TILE_SIZE,sheet)
    end
  end

  mappings=clone_mappings(DEFAULT_MAPPINGS)
  selected_role=1
  sheet_zoom,sheet_pan_x,sheet_pan_y,dragging=1,0,0,false
  dirty=false
  status="Click a role, then click a tile. SAVE JSON updates ROAG's mapping."
  if read_local_json() then load_json() end
end

function love.draw()
  local layout=editor_layout()
  love.graphics.clear(.025,.035,.055)

  draw_text("ROAG SPRITE EDITOR",24,24,2,.7,.9,1)
  draw_text("Click a role, then click a sprite-sheet tile.",24,70,1,.78,.84,.94)
  draw_text("JSON FILE: "..json_path,24,94,.7,.65,.72,.84)
  draw_text("Selected: "..selected().label.."  →  ["..mappings[selected().key][1]..", "..mappings[selected().key][2].."]"..(dirty and "  UNSAVED" or ""),24,120,1,.95,.85,.3)

  for index,role in ipairs(ROLES) do
    local y=layout.role_y+(index-1)*layout.role_height
    if index==selected_role then set_color(.13,.22,.3);love.graphics.rectangle("fill",layout.role_x,y,layout.role_width,layout.role_height-3) end
    local tile=mappings[role.key]
    love.graphics.draw(sheet,quads[tile[1]..":"..tile[2]],layout.role_x+6,y+3,0,1.5,1.5)
    draw_text(role.label.."  ["..tile[1]..", "..tile[2].."]",layout.role_x+36,y+6,.9,index==selected_role and .95 or .78,index==selected_role and .85 or .83,index==selected_role and .3 or .9)
  end

  set_color(.08,.1,.14);love.graphics.rectangle("fill",layout.viewport_x-4,layout.viewport_y-4,layout.viewport_width+8,layout.viewport_height+8)
  set_color(.025,.035,.055);love.graphics.rectangle("fill",layout.viewport_x,layout.viewport_y,layout.viewport_width,layout.viewport_height)
  love.graphics.setScissor(layout.viewport_x,layout.viewport_y,layout.viewport_width,layout.viewport_height)
  love.graphics.setColor(1,1,1,1)
  love.graphics.draw(sheet,layout.sheet_x,layout.sheet_y,0,layout.tile_size/SOURCE_TILE_SIZE,layout.tile_size/SOURCE_TILE_SIZE)

  local function outline(column,row,r,g,b,width)
    set_color(r,g,b);love.graphics.setLineWidth(width)
    love.graphics.rectangle("line",layout.sheet_x+(column-1)*layout.tile_size,layout.sheet_y+(row-1)*layout.tile_size,layout.tile_size,layout.tile_size)
  end
  local assigned=mappings[selected().key]
  outline(assigned[1],assigned[2],.15,.9,1,2)
  local mouse_x,mouse_y=love.mouse.getPosition()
  local column,row=tile_at(mouse_x,mouse_y,layout)
  if column then
    outline(column,row,1,.85,.2,3)
  end
  love.graphics.setScissor()
  love.graphics.setLineWidth(1)
  if column then draw_text("HOVER: ["..column..", "..row.."]",layout.viewport_x,layout.viewport_y+layout.viewport_height+12,.75,.95,.85,.3) end

  for _,button in ipairs(button_layout(layout)) do
    set_color(.13,.22,.3);love.graphics.rectangle("fill",button.x,button.y,button.width,34)
    draw_text(button.label,button.x+9,button.y+10,.72,.9,.93,1)
  end
  draw_text("WHEEL: ZOOM SHEET   RIGHT/MIDDLE DRAG: PAN",24,layout.height-104,.68,.75,.82,.92)
  draw_text(status,24,layout.height-30,.68,.78,.84,.94)
end

function love.mousepressed(x,y,button)
  local layout=editor_layout()
  if (button==2 or button==3) and point_in_viewport(x,y,layout) then dragging=true;return end
  if button~=1 then return end
  for _,control in ipairs(button_layout(layout)) do
    if point_in_rect(x,y,control) then control.action();return end
  end
  for index in ipairs(ROLES) do
    local row={x=layout.role_x,y=layout.role_y+(index-1)*layout.role_height,width=layout.role_width,height=layout.role_height-3}
    if point_in_rect(x,y,row) then selected_role=index;return end
  end
  local column,row=tile_at(x,y,layout)
  if column then
    mappings[selected().key]={column,row}
    dirty=true
    status=selected().label.." is now ["..column..", "..row.."]. Click SAVE JSON to keep it."
  end
end

function love.mousereleased(_,_,button)
  if button==2 or button==3 then dragging=false end
end

function love.mousemoved(_,_,delta_x,delta_y)
  if dragging then
    sheet_pan_x=sheet_pan_x+delta_x
    sheet_pan_y=sheet_pan_y+delta_y
  end
end

function love.wheelmoved(_,delta_y)
  if delta_y==0 then return end
  local mouse_x,mouse_y=love.mouse.getPosition()
  local old_layout=editor_layout()
  if not point_in_viewport(mouse_x,mouse_y,old_layout) then return end
  local sheet_x=(mouse_x-old_layout.sheet_x)/old_layout.tile_size
  local sheet_y=(mouse_y-old_layout.sheet_y)/old_layout.tile_size
  sheet_zoom=clamp(sheet_zoom*(1.15^delta_y),.5,6)
  local new_layout=editor_layout()
  sheet_pan_x=mouse_x-new_layout.base_sheet_x-sheet_x*new_layout.tile_size
  sheet_pan_y=mouse_y-new_layout.base_sheet_y-sheet_y*new_layout.tile_size
  status=string.format("Sprite sheet zoom: %d%%",math.floor(sheet_zoom*100+.5))
end

function love.keypressed(key)
  if key=="escape" then love.event.quit()
  elseif key=="s" and (love.keyboard.isDown("lctrl") or love.keyboard.isDown("rctrl")) then save_json()
  elseif key=="l" and (love.keyboard.isDown("lctrl") or love.keyboard.isDown("rctrl")) then load_json()
  end
end
