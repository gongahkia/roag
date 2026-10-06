-- Focused Studio surface for real Expedition chamber/encounter content. The
-- model owns safety; this file is deliberately a compact immediate-mode UI.
local Model = require("src.expedition.workbench_model")
local Assets = require("src.rendering.loveable_rogue_assets")

local Editor = {}; Editor.__index = Editor
local C = { bg={.025,.035,.055}, panel={.055,.075,.11}, panel2={.075,.105,.15}, border={.18,.25,.34}, text={.84,.89,.96}, muted={.52,.61,.72}, cyan={.42,.84,1}, gold={.95,.82,.24}, mint={.45,.9,.68}, coral={1,.44,.34} }
local function color(v) love.graphics.setColor(v[1],v[2],v[3],v[4] or 1) end
local function inside(x,y,r) return x>=r.x and y>=r.y and x<=r.x+r.width and y<=r.y+r.height end
local function box(r, fill, outline) if fill then color(fill); love.graphics.rectangle("fill",r.x,r.y,r.width,r.height,4,4) end; color(outline or C.border); love.graphics.rectangle("line",r.x+.5,r.y+.5,r.width-1,r.height-1,4,4) end
local TILE_SPRITES = { floor="terrain.floor", wall="terrain.wall_center", water="effect.liquid", gas="effect.gas", fire="effect.fire", spikes="effect.spikes", breakable="object.crate", volatile="object.barrier", ["door.closed"]="object.door_closed" }
local MARKER_COLOR = { player_spawn=C.mint, enemy_spawns=C.coral, exits=C.gold, caches=C.cyan, reinforcements={.85,.5,1}, boss_spawns={1,.35,.55} }

function Editor.new()
  local model = assert(Model.new())
  local self = setmetatable({model=model, fonts={normal=love.graphics.newFont(14),small=love.graphics.newFont(12),title=love.graphics.newFont(26)}, tab="chambers", tool=".", controls={}, rows={}, selected_chamber="chamber.open_basic", selected_encounter="expedition.encounter.swarm", preview=nil, preview_seed=1, preview_stage=1, preview_character="expedition.gunner", play_preview=false, search="", filter_tag=nil, active_text=nil, role_to_add="rusher"},Editor)
  self.assets=Assets.new(); self.assets:load(); model:open("chambers",self.selected_chamber); return self
end
function Editor:text(value,x,y,scale,tint,limit)
  love.graphics.setFont(scale>=1.5 and self.fonts.title or scale<.8 and self.fonts.small or self.fonts.normal); color(tint or C.text)
  if limit then love.graphics.printf(value,x,y,limit) else love.graphics.print(value,x,y) end
end
function Editor:button(label,x,y,width,action)
  local r={x=x,y=y,width=width,height=28}; box(r,C.panel2); self:text(label,x+6,y+7,.63,C.text,width-12); self.controls[#self.controls+1]={rect=r,action=action}; return r
end
function Editor:lists(w,h)
  local r={x=14,y=78,width=272,height=h-138}; box(r,C.panel); self:text("CHAMBERS"..(self.filter_tag and " / "..self.filter_tag:upper() or ""),24,88,.72,C.gold)
  local search={x=20,y=108,width=260,height=22}; box(search,self.active_text=="search" and {.1,.24,.34} or C.panel2,self.active_text=="search" and C.cyan or C.border); self:text("SEARCH "..(self.active_text=="search" and self.text_draft or self.search),search.x+5,search.y+4,.58,C.muted); self.search_rect=search
  local y=136; for _,entry in ipairs(self.model:list("chambers",self.search,self.filter_tag)) do local row={x=20,y=y,width=260,height=24}; box(row,entry.id==self.selected_chamber and {.1,.24,.34} or nil,entry.id==self.selected_chamber and C.cyan or {.1,.14,.2}); self:text(entry.name,row.x+5,row.y+5,.66,C.text); self.rows[#self.rows+1]={rect=row,kind="chambers",id=entry.id}; y=y+26 end
  y=y+11; self:text("ENCOUNTERS",24,y,.72,C.gold); y=y+24
  for _,entry in ipairs(self.model:list("encounters",self.search,self.filter_tag)) do local row={x=20,y=y,width=260,height=24}; box(row,entry.id==self.selected_encounter and {.1,.24,.34} or nil,entry.id==self.selected_encounter and C.cyan or {.1,.14,.2}); self:text(entry.name,row.x+5,row.y+5,.66,C.text); self.rows[#self.rows+1]={rect=row,kind="encounters",id=entry.id}; y=y+26 end
  return r
end
function Editor:draw_board(definition,x,y,available_w,available_h,preview)
  local cell=math.max(16,math.floor(math.min(available_w/definition.width,available_h/definition.height))); local bw,bh=cell*definition.width,cell*definition.height
  local bx,by=x+math.floor((available_w-bw)/2),y+math.floor((available_h-bh)/2); color({.01,.015,.025}); love.graphics.rectangle("fill",bx-5,by-5,bw+10,bh+10)
  for row_index,row in ipairs(definition.tiles) do for col=1,#row do
    local semantic=definition.tile_legend[row:sub(col,col)]; self.assets:draw_sprite(TILE_SPRITES[semantic] or "terrain.floor",bx+(col-1)*cell,by+(row_index-1)*cell,cell)
    color({.1,.14,.19,.42}); love.graphics.rectangle("line",bx+(col-1)*cell+.5,by+(row_index-1)*cell+.5,cell-1,cell-1)
  end end
  for kind,points in pairs(definition.markers or {}) do for _,point in ipairs(points) do
    local c=MARKER_COLOR[kind] or C.text; color({c[1],c[2],c[3],.85}); love.graphics.rectangle("line",bx+(point.x-1)*cell+3,by+(point.y-1)*cell+3,cell-6,cell-6); self:text(kind=="player_spawn" and "P" or kind=="enemy_spawns" and "E" or kind=="exits" and "X" or kind=="caches" and "$" or kind=="reinforcements" and "R" or "B",bx+(point.x-1)*cell+cell*.36,by+(point.y-1)*cell+cell*.27,.63,c)
  end end
  if preview then
    local bounds=preview.run.session.state.expedition.chamber.bounds; local state=preview.run.session.state
    for _,actor in ipairs(state.enemies) do self.assets:draw_actor(actor,state,bx+(actor.x-bounds.min_x)*cell,by+(actor.y-bounds.min_y)*cell,cell) end
    self.assets:draw_actor(state.player,state,bx+(state.player.x-bounds.min_x)*cell,by+(state.player.y-bounds.min_y)*cell,cell)
  end
  return {x=bx,y=by,cell=cell,width=bw,height=bh}
end
function Editor:draw_chamber(w,h)
  local d=self.model:current_definition(); if not d then return end
  self:text("CHAMBER / "..d.name,304,84,1.02,C.cyan); self:text(d.id.."   "..d.width.." x "..d.height.."   TAGS: "..table.concat(d.topology_tags,", "),304,109,.67,C.muted)
  self.board=self:draw_board(d,304,140,w-560,h-225)
  local px=w-244; box({x=px,y=78,width=228,height=h-138},C.panel); self:text("PALETTE",px+10,89,.72,C.gold)
  local semantic_by_symbol={ ["."]="floor",["#"]="wall",["~"]="water",["^"]="spikes",f="fire",g="gas",b="breakable",v="volatile",d="door.closed" }
  local sy=116; self.palette={}; for _,symbol in ipairs({".","#","~","^","f","g","b","v","d"}) do local semantic=semantic_by_symbol[symbol]; local r={x=px+9,y=sy,width=210,height=24}; box(r,self.tool==symbol and {.1,.24,.34} or nil,self.tool==symbol and C.cyan or C.border); self:text(symbol.."  "..semantic,r.x+6,r.y+5,.64,C.text); self.palette[#self.palette+1]={rect=r,tool=symbol}; sy=sy+26 end
  sy=sy+8; for _,kind in ipairs({"player_spawn","enemy_spawns","exits","caches","reinforcements","boss_spawns"}) do local r={x=px+9,y=sy,width=210,height=23}; box(r,self.tool==kind and {.1,.24,.34} or nil,self.tool==kind and C.cyan or C.border); self:text("MARK "..kind:upper(),r.x+6,r.y+5,.58,MARKER_COLOR[kind]); self.palette[#self.palette+1]={rect=r,tool=kind}; sy=sy+25 end
  self:button("W-",px+9,h-128,38,"width_down"); self:button("W+",px+50,h-128,38,"width_up"); self:button("H-",px+96,h-128,38,"height_down"); self:button("H+",px+137,h-128,38,"height_up")
end
function Editor:draw_encounter(w,h)
  local d=self.model:current_definition(); if not d then return end
  local x=304; box({x=x,y=78,width=w-320,height=h-138},C.panel); self:text("ENCOUNTER / "..d.name,x+12,90,1.02,C.cyan); self:text(d.id,x+12,119,.68,C.muted)
  self:text("ARCHETYPE  "..d.archetype:upper().."     STAGE "..d.stage.."     THREAT BASE "..d.threat_budget.base,x+12,151,.77,C.gold)
  self:text("SPAWN "..d.spawn_intent:upper().."     CLEAR "..d.clear_condition:upper().."     REWARD "..d.reward_intent:upper(),x+12,177,.7,C.text)
  self:text("ROLE ROWS",x+12,214,.72,C.gold); local y=240; for index,row in ipairs(d.roles) do self:text(string.format("%d. %-12s  MIN %d  MAX %d  WEIGHT %s",index,row.role,row.min,row.max,tostring(row.weight or 1)),x+20,y,.74,C.text); self:button("MIN-",x+300,y-3,42,"role_min_down:"..index); self:button("MIN+",x+346,y-3,42,"role_min_up:"..index); self:button("MAX-",x+392,y-3,42,"role_max_down:"..index); self:button("MAX+",x+438,y-3,42,"role_max_up:"..index); y=y+30 end
  self:text("TOPOLOGY COMPATIBILITY",x+12,y+20,.72,C.gold); self:text(table.concat(d.compatible_topology_tags or {},", "),x+20,y+44,.72,C.text); self:text("ELITE "..(d.elite.allowed and "ALLOWED" or "OFF").."     REINFORCEMENT "..(d.reinforcement.enabled and "ON" or "OFF"),x+12,y+79,.72,C.mint)
  self:text("Use the role controls in the lower bar to add/remove rows. Numeric metadata preserves through save/reload.",x+12,h-95,.66,C.muted)
  self:button("TYPE "..self.role_to_add:upper(),x+12,h-128,92,"role_type"); self:button("ROLE+",x+108,h-128,56,"role_add"); self:button("ROLE-",x+168,h-128,56,"role_remove"); self:button("THREAT-",x+228,h-128,68,"threat_down"); self:button("THREAT+",x+300,h-128,68,"threat_up"); self:button("STAGE",x+372,h-128,56,"stage_next"); self:button("ELITE",x+432,h-128,56,"elite_toggle"); self:button("REINF",x+492,h-128,56,"reinforce_toggle")
end
function Editor:draw_preview(w,h)
  local chamber=self.model.registry.chamber_by_id[self.selected_chamber]; local encounter=self.model.registry.encounter_by_id[self.selected_encounter]
  self:text("PREVIEW / REAL EXPEDITION RUNTIME",304,84,1.02,C.cyan); self:text("CHAMBER "..chamber.name.."   +   "..encounter.name.."   SEED "..self.preview_seed.."   STAGE "..self.preview_stage.."   "..self.preview_character:upper(),304,109,.68,C.muted)
  local data=self.preview
  if data then self.board=self:draw_board(chamber,304,140,w-320,h-250,data); self:text(self.play_preview and "PLAY PREVIEW — WASD MOVE, F ATTACK, Q ABILITY, R RESET" or "STATIC PREVIEW — Press PLAY PREVIEW to use this isolated real runtime.",304,h-80,.69,self.play_preview and C.mint or C.gold)
  else self:text("Select a chamber and encounter, then press PREVIEW. This creates an isolated real ExpeditionRun.",304,165,.78,C.text) end
  self:button("RESET",304,h-128,56,"preview"); self:button("NEW SEED",364,h-128,73,"new_seed"); self:button("STAGE",441,h-128,55,"preview_stage"); self:button("CHAR",500,h-128,50,"preview_character")
end
function Editor:draw()
  local w,h=love.graphics.getDimensions(); self.controls={}; self.rows={}; love.graphics.clear(C.bg); self:text("ROAG STUDIO / EXPEDITION WORKBENCH",14,16,1.55,C.cyan); self:text("AUTHORED CHAMBERS + ENCOUNTERS — runtime validation and preview share production code.",14,48,.69,C.muted)
  self:lists(w,h)
  if self.tab=="chambers" then self:draw_chamber(w,h) elseif self.tab=="encounters" then self:draw_encounter(w,h) else self:draw_preview(w,h) end
  local labels={{"CHAMBERS","tab_chambers",14},{"ENCOUNTERS","tab_encounters",108},{"PREVIEW","tab_preview",220},{"FILTER","filter",320},{"NEW","new",380},{"DUP","dup",430},{"RENAME","rename",480},{"DELETE","delete",545},{"SAVE","save",610},{"VALIDATE","validate",665},{"UNDO","undo",750},{"REDO","redo",805},{"PREVIEW","preview",860},{"PLAY","play",950},{"BACK","back",1005}}
  for _,v in ipairs(labels) do self:button(v[1],v[3],h-43,v[1]=="VALIDATE" and 80 or v[1]=="ENCOUNTERS" and 105 or 54,v[2]) end
  self:text((self.model.dirty and "UNSAVED — " or "")..(self.model.message or ""),900,h-33,.66,self.model.dirty and C.gold or C.muted,w-910)
end
function Editor:select(kind,id)
  local ok,err=self.model:open(kind,id); if not ok then self.model.message=(err.reason or "Unsaved work").." — Ctrl+S saves; Ctrl+D discards."; return end
  if kind=="chambers" then self.selected_chamber=id; self.tab="chambers" else self.selected_encounter=id; self.tab="encounters" end
end
function Editor:run_preview(play)
  local data,err=self.model:preview({chamber_id=self.selected_chamber,encounter_id=self.selected_encounter,seed=self.preview_seed,stage=self.preview_stage,character_id=self.preview_character}); if not data then self.model.message=err.reason; return end
  self.preview,self.play_preview,self.tab=data,play or false,"preview"; self.model.message="Preview is isolated: no save/profile state is used."
end
function Editor:mousepressed(x,y,button)
  if button~=1 then return end
  for _,control in ipairs(self.controls) do if inside(x,y,control.rect) then local a=control.action
    if a=="tab_chambers" then self.tab="chambers"; self.filter_tag=nil; self:select("chambers",self.selected_chamber)
    elseif a=="tab_encounters" then self.tab="encounters"; self.filter_tag=nil; self:select("encounters",self.selected_encounter)
    elseif a=="filter" then local values=self.tab=="encounters" and {"","swarm","crossfire","pincer","duel","hazard","breach","encirclement","hunter_kite","elite_hunt","reinforcement_pressure","volatile_arena","breakpoint"} or {"","open","lane","cross","choke","pinball","pockets","conductive","volatile","breakable","boss"}; local index=1; for i,v in ipairs(values) do if v==(self.filter_tag or "") then index=i end end; local next_value=values[index%#values+1]; self.filter_tag=next_value=="" and nil or next_value
    elseif a=="tab_preview" or a=="preview" then self:run_preview(false)
    elseif a=="new_seed" then self.preview_seed=self.preview_seed+1; self:run_preview(false)
    elseif a=="preview_stage" then self.preview_stage=self.preview_stage%3+1; self:run_preview(false)
    elseif a=="preview_character" then local values={"expedition.gunner","expedition.bruiser","expedition.conductor","expedition.demolitionist"}; local index=1; for i,v in ipairs(values) do if v==self.preview_character then index=i end end; self.preview_character=values[index%#values+1]; self:run_preview(false)
    elseif a=="play" then self:run_preview(true)
    elseif a=="new" then local kind=self.tab=="encounters" and "encounters" or "chambers"; local count=#self.model:list(kind)+1; self.model:new_definition(kind,kind=="chambers" and "chamber.new_board_"..count or "expedition.encounter.new_board_"..count); self.tab=kind
    elseif a=="dup" then local kind=self.model.current_kind; local id=self.model.current_definition() and self.model.current_definition().id; if id then self.model:duplicate(kind,id,id.."_copy") end
    elseif a=="rename" then local d=self.model:current_definition(); if d then self.active_text="rename"; self.text_draft=d.name end
    elseif a=="delete" then if self.model.pending and self.model.pending.action=="delete" then local ok,err=self.model:confirm_delete(true); self.model.message=ok and "Deleted definition and manifest entry." or (err.message or err.reason) else local pending,err=self.model:request_delete(); self.model.message=pending and ("DELETE AGAIN TO CONFIRM: "..table.concat(pending.references,", ")) or (err.message or err.reason) end
    elseif a=="save" then local ok,err=self.model:save(); self.model.message=ok and "Saved canonical JSON atomically." or (err.message or err.reason)
    elseif a=="validate" then local ok,err=self.model:validate(); self.model.message=ok and "Definition is valid." or (err.message or err.reason)
    elseif a=="undo" then self.model:undo() elseif a=="redo" then self.model:redo()
    elseif a=="width_up" then local d=self.model:current_definition(); self.model:resize(d.width+1,d.height,true)
    elseif a=="width_down" then local d=self.model:current_definition(); local key="width:"..d.width..":"..d.height; if self.pending_resize==key then self.model:resize(d.width-1,d.height,true); self.pending_resize=nil else local ok,err=self.model:resize(d.width-1,d.height,false); if not ok then self.pending_resize=key; self.model.message=(err.reason or "Click W- again to confirm shrink") end end
    elseif a=="height_up" then local d=self.model:current_definition(); self.model:resize(d.width,d.height+1,true)
    elseif a=="height_down" then local d=self.model:current_definition(); local key="height:"..d.width..":"..d.height; if self.pending_resize==key then self.model:resize(d.width,d.height-1,true); self.pending_resize=nil else local ok,err=self.model:resize(d.width,d.height-1,false); if not ok then self.pending_resize=key; self.model.message=(err.reason or "Click H- again to confirm shrink") end end
    elseif a=="role_type" then local values={"rusher","flanker","ranged","controller","heavy"}; local index=1; for i,v in ipairs(values) do if v==self.role_to_add then index=i end end; self.role_to_add=values[index%#values+1] elseif a=="role_add" then self.model:add_role(self.role_to_add) elseif a=="role_remove" then local d=self.model:current_definition(); if #d.roles>1 then self.model:remove_role(#d.roles) end
    elseif a=="threat_down" then local d=self.model:current_definition(); self.model:set_threat_base(math.max(1,d.threat_budget.base-1)) elseif a=="threat_up" then local d=self.model:current_definition(); self.model:set_threat_base(d.threat_budget.base+1)
    elseif a:match("^role_") then local field,delta,index=a:match("^role_(min|max)_(down|up):(%d+)$"); local d=self.model:current_definition(); index=tonumber(index); local row=d.roles[index]; if field and row then local value=field=="min" and math.min(row.max,math.max(0,row.min+(delta=="up" and 1 or -1))) or math.max(row.min,row.max+(delta=="up" and 1 or -1)); self.model:set_role(index,field,value) end
    elseif a=="stage_next" then local d=self.model:current_definition(); self.model:set_metadata("stage",d.stage%3+1) elseif a=="elite_toggle" then local d=self.model:current_definition(); self.model:set_flag("elite",not d.elite.allowed) elseif a=="reinforce_toggle" then local d=self.model:current_definition(); self.model:set_flag("reinforcement",not d.reinforcement.enabled)
    elseif a=="back" then return "back" end; return end end
  if self.search_rect and inside(x,y,self.search_rect) then self.active_text="search"; self.text_draft=self.search; return end
  for _,row in ipairs(self.rows) do if inside(x,y,row.rect) then self:select(row.kind,row.id); return end end
  for _,entry in ipairs(self.palette or {}) do if inside(x,y,entry.rect) then self.tool=entry.tool; return end end
  if self.tab=="chambers" and self.board and inside(x,y,{x=self.board.x,y=self.board.y,width=self.board.width,height=self.board.height}) then local tx=math.floor((x-self.board.x)/self.board.cell)+1; local ty=math.floor((y-self.board.y)/self.board.cell)+1; if self.tool:sub(1,1)=="." or #self.tool==1 then self.model:paint(tx,ty,self.tool) else self.model:place_marker(self.tool,tx,ty,self.tool=="enemy_spawns" and "ring" or nil) end end
end
function Editor:keypressed(key)
  if self.active_text then
    if key=="return" then if self.active_text=="search" then self.search=self.text_draft else self.model:set_metadata("name",self.text_draft) end; self.active_text,self.text_draft=nil,nil
    elseif key=="escape" then self.active_text,self.text_draft=nil,nil elseif key=="backspace" then self.text_draft=self.text_draft:sub(1,-2) end
    return
  end
  if key=="escape" then return "back" end
  if key=="s" and love.keyboard.isDown("lctrl","rctrl") then local ok,err=self.model:save(); self.model.message=ok and "Saved." or (err.message or err.reason)
  elseif key=="z" and love.keyboard.isDown("lctrl","rctrl") then self.model:undo()
  elseif key=="y" and love.keyboard.isDown("lctrl","rctrl") then self.model:redo()
  elseif key=="d" and love.keyboard.isDown("lctrl","rctrl") then local opened=self.model:confirm_discard(); if opened then if self.model.current_kind=="chambers" then self.selected_chamber=self.model.current.id else self.selected_encounter=self.model.current.id end end
  elseif key=="f5" then self:run_preview(true)
  elseif self.play_preview and self.preview then local mapping={w="up",a="left",s="down",d="right",f="attack",q="ability"}; if mapping[key] then self.preview.run:turn(mapping[key]) elseif key=="r" then self:run_preview(true) end end
end
function Editor:textinput(value) if self.active_text then self.text_draft=(self.text_draft or "")..value end end
return Editor
