-- ROAG: LÖVE 11.x implementation of the original terminal roguelike.
-- One input consumes one turn: player action, hazards, then enemy actions.

-- The playfield is intentionally compact and the camera shows a wide area.
local WIDTH, HEIGHT = 80, 50
local VIEW_W, VIEW_H = 39, 25
local BOSS_WINDUP = 4

local STAGES = {
  { level=0, terrain="forest", targets=4, enemies=6, score=5, ammo=2, vision=6, torches=3, wilds=true },
  { level=1, terrain="cave", targets=3, enemies=0, score=4, ammo=1, vision=5, torches=3 },
  { level=2, terrain="dungeon", targets=1, enemies=2, score=5, ammo=2, vision=4, torches=3, cultists=true },
}
local CLASSES = {
  {name="VANGUARD", description="TOUGH AND WELL-ARMED", modifiers={health=2,bombs=1,flares=-1}},
  {name="GUNSLINGER", description="EXTRA AMMO, LESS HEALTH", modifiers={health=-1,ammo=3}},
  {name="SCOUT", description="BETTER SIGHT, FLARES, AND DASH", modifiers={vision=2,dash_cooldown=-1}},
}
local BOONS = {
  {name="IRON HEART", description="BEGIN EACH DESCENT WITH MORE HEALTH", modifiers={health=1}},
  {name="FULL QUIVER", description="BEGIN EACH DESCENT WITH EXTRA AMMO", modifiers={ammo=2}},
  {name="DEMOLITION KIT", description="BEGIN EACH DESCENT WITH AN EXTRA BOMB", modifiers={bombs=1}},
  {name="FLARE SATCHEL", description="BEGIN EACH DESCENT WITH TWO EXTRA FLARES", modifiers={flares=2}},
  {name="EAGLE EYE", description="SEE FURTHER THROUGH THE DARKNESS", modifiers={vision=2}},
  {name="WINDWALKER", description="YOUR DASH RECHARGES MORE QUICKLY", modifiers={dash_cooldown=-1}},
}
local CURSES = {
  {name="FRAIL BODY",description="BEGIN WITH ONE HEALTH",modifiers={health=1}},
  {name="DARKNESS",description="REDUCED NATURAL VISION",modifiers={vision=-3}},
  {name="HUNTED",description="ONE EXTRA NECROMANCER",modifiers={enemies=1}},
  {name="RELENTLESS",description="TWO EXTRA NECROMANCERS",modifiers={enemies=2}},
  {name="EMPTY CHAMBER",description="START WITH LESS AMMO",modifiers={ammo=-1}},
  {name="SPENT BOMBS",description="START WITHOUT BOMBS",modifiers={bombs=0}},
  {name="LONG HUNT",description="MORE TARGETS MUST FALL",modifiers={score=2}},
  {name="BLACKOUT",description="NO TORCHES OR NATURAL LIGHT",modifiers={vision=-99,torches=-99}},
  {name="GUTTERING TORCHES",description="TORCHES BARELY REACH",modifiers={torch_radius=1}},
  {name="SLOW DASH",description="DASH RECHARGES SLOWLY",modifiers={dash_cooldown=6}},
  {name="SPENT FLARES",description="START WITHOUT FLARES",modifiers={flares=0}},
  {name="SMALL BLAST",description="BOMBS HAVE LESS RANGE",modifiers={bomb_radius=1}},
  {name="SHORT FUSE",description="BOMBS DETONATE QUICKLY",modifiers={bomb_fuse=1}},
  {name="RUSTED BARREL",description="BULLETS FADE EARLY",modifiers={bullet_range=3}},
  {name="DRY RELOAD",description="KILLS RESTORE LESS AMMO",modifiers={reload_penalty=1}},
}
-- sprite_map.lua is the safe fallback. The standalone editor owns the JSON
-- mapping that ROAG reads whenever it starts or regains window focus.
local SPRITE = require("sprite_map")
local DEFAULT_SPRITE = {}
for kind,tile in pairs(SPRITE) do DEFAULT_SPRITE[kind]={tile[1],tile[2]} end
local function sprite_mapping_contents()
  local source=love.filesystem.getSource()
  if source:sub(-5):lower()==".love" then
    local path=love.filesystem.getSourceBaseDirectory().."/sprite_editor/mappings.json"
    local file=io.open(path,"rb")
    if file then
      local contents=file:read("*a")
      file:close()
      return contents
    end
  end
  return love.filesystem.read("sprite_editor/mappings.json")
end
local function load_sprite_mappings()
  local contents=sprite_mapping_contents()
  if not contents then return end
  for kind,column,row in contents:gmatch('\"([%w_]+)\"%s*:%s*{%s*\"column\"%s*:%s*(%d+)%s*,%s*\"row\"%s*:%s*(%d+)%s*}') do
    column,row=tonumber(column),tonumber(row)
    if DEFAULT_SPRITE[kind] and column>=1 and column<=49 and row>=1 and row<=22 then
      SPRITE[kind]={column,row}
    end
  end
end
local SPRITE_ORDER = {
  {kind="player", label="PLAYER"}, {kind="target", label="TARGET"},
  {kind="ammo", label="AMMO"}, {kind="torch", label="TORCH"},
  {kind="door", label="EXIT DOOR"}, {kind="bullet", label="BULLET"},
  {kind="bomb", label="BOMB"}, {kind="flare", label="FLARE"},
  {kind="wolf", label="WOLF"}, {kind="bomber", label="BOMBER"},
  {kind="necromancer", label="NECROMANCER"}, {kind="cultist", label="CULTIST"},
  {kind="boss", label="BOSS"},
}
local DIR = { w={0,1,"N"}, a={-1,0,"W"}, s={0,-1,"S"}, d={1,0,"E"} }
local MOVE_KEY = {w="w",up="w",a="a",left="a",s="s",down="s",d="d",right="d"}
local HOLD_INITIAL_DELAY, HOLD_REPEAT_DELAY = .28, .11

local G = { screen="title", menu=1, stage=1, score=0, log={}, curse_bag={}, effects={} }

local function key(x,y) return x..":"..y end
local function cell(x,y) return {x=x,y=y} end
local function copy(t) local r={} for k,v in pairs(t) do r[k]=v end return r end
local function clamp(n,a,b) return math.max(a,math.min(b,n)) end
local function dist(a,b) return math.abs(a.x-b.x)+math.abs(a.y-b.y) end
local function open(x,y) return G.space[key(x,y)] end
local function in_world(x,y) return x>=0 and x<WIDTH and y>=0 and y<HEIGHT end
local function push(t,v) t[#t+1]=v end
local function remove(t,i) local v=t[i]; table.remove(t,i); return v end
local function shuffled(t)
  local r={} for _,v in ipairs(t) do push(r,v) end
  for i=#r,2,-1 do local j=love.math.random(i);r[i],r[j]=r[j],r[i] end
  return r
end
local function log(message) push(G.log,1,message); while #G.log>5 do table.remove(G.log) end end
local function sound(name) if G.sounds[name] then G.sounds[name]:stop();G.sounds[name]:play() end end
local function color(c) love.graphics.setColor(c[1],c[2],c[3],c[4] or 1) end
local function text(v,x,y,s,c) if type(s)=="table" then c,s=s,1 end;color(c or {1,1,1});love.graphics.print(v,x,y,0,s or 1);love.graphics.setColor(1,1,1) end
local function entity(kind,x,y,values) local e={kind=kind,x=x,y=y,render_x=x,render_y=y};for k,v in pairs(values or {}) do e[k]=v end;return e end
local function move_entity(e,x,y)
  e.render_x,e.render_y=e.render_x or e.x,e.render_y or e.y
  e.x,e.y=x,y
end
local function slide_axis(current,target,dt,speed)
  -- Simulation remains grid-snapped; render positions move independently.
  local delta=target-current
  local step=dt*(speed or 12)
  if math.abs(delta)<=step then return target end
  return current+(delta>0 and step or -step)
end
local function follow_camera_axis(current,target,dt)
  -- Preserve a subtle follow lag, but close a large gap before the player can leave view.
  local speed=math.abs(target-current)>1.5 and 18 or 6
  return slide_axis(current,target,dt,speed)
end

-- Map generation uses the same cellular forest/cave generation and linked dungeon rooms.
local function carve(space,x,y,w,h) for px=x,x+w-1 do for py=y,y+h-1 do if px>0 and px<WIDTH-1 and py>0 and py<HEIGHT-1 then space[key(px,py)]=true end end end end
local function corridor(space,a,b)
  local x,y=a.x,a.y
  local function horizontal() while x~=b.x do space[key(x,y)]=true;x=x+(b.x>x and 1 or -1) end end
  local function vertical() while y~=b.y do space[key(x,y)]=true;y=y+(b.y>y and 1 or -1) end end
  if love.math.random()<.5 then horizontal();vertical() else vertical();horizontal() end;space[key(x,y)]=true
end
local function walls_near(space,x,y)
  local n=0 for dx=-1,1 do for dy=-1,1 do if (dx~=0 or dy~=0) and not space[key(x+dx,y+dy)] then n=n+1 end end end;return n
end
local function generate(terrain,start,arena)
  local space={}
  if arena then carve(space,2,2,37,15);return space end
  if terrain=="dungeon" then
    local rooms={}
    for _=1,14 do
      local w,h=love.math.random(5,11),love.math.random(4,8);local x,y=love.math.random(1,WIDTH-w-2),love.math.random(1,HEIGHT-h-2);carve(space,x,y,w,h)
      local center=cell(x+math.floor(w/2),y+math.floor(h/2));if #rooms>0 then corridor(space,rooms[#rooms],center) end;push(rooms,center)
    end
    carve(space,start.x-3,start.y-3,7,7);corridor(space,start,rooms[1]);return space
  end
  local chance=terrain=="forest" and .25 or .43
  for x=1,WIDTH-2 do for y=1,HEIGHT-2 do if love.math.random()>chance then space[key(x,y)]=true end end end
  for _=1,(terrain=="forest" and 2 or 5) do
    local next_space={};for x=1,WIDTH-2 do for y=1,HEIGHT-2 do if walls_near(space,x,y)<5 then next_space[key(x,y)]=true end end end;space=next_space
  end
  carve(space,start.x-3,start.y-3,7,7);return space
end
local function neighbours(p) return {cell(p.x,p.y+1),cell(p.x-1,p.y),cell(p.x,p.y-1),cell(p.x+1,p.y)} end
local function path(start,finish,blocked)
  blocked=blocked or {};local queue,head={cell(start.x,start.y)},1;local previous={[key(start.x,start.y)]=false};blocked[key(start.x,start.y)]=nil;blocked[key(finish.x,finish.y)]=nil
  while queue[head] do
    local p=queue[head];head=head+1
    if p.x==finish.x and p.y==finish.y then local r={};while p do table.insert(r,1,p);p=previous[key(p.x,p.y)] end;return r end
    for _,n in ipairs(neighbours(p)) do local k=key(n.x,n.y);if open(n.x,n.y) and not blocked[k] and previous[k]==nil then previous[k]=p;push(queue,n) end end
  end
  return {}
end
local function blast(origin,radius)
  local result,queue,head={}, {cell(origin.x,origin.y)},1;local steps={[key(origin.x,origin.y)]=0}
  while queue[head] do local p=queue[head];head=head+1;local k=key(p.x,p.y);local d=steps[k]
    if d<=radius and open(p.x,p.y) and not result[k] then result[k]=true;for _,n in ipairs(neighbours(p)) do local nk=key(n.x,n.y);if in_world(n.x,n.y) and not steps[nk] then steps[nk]=d+1;push(queue,n) end end end
  end;return result
end

local function apply(settings,mods)
  settings.health=settings.health+(mods.health or 0);for k,v in pairs(mods) do if k~="health" then settings[k]=(settings[k] or 0)+v end end
  settings.health=clamp(settings.health,1,5);settings.ammo=math.max(0,settings.ammo);settings.bombs=math.max(0,settings.bombs);settings.flares=math.max(0,settings.flares);settings.vision=math.max(1,settings.vision);settings.enemies=math.max(0,settings.enemies);settings.torches=math.max(0,settings.torches);settings.score=math.max(1,settings.score);settings.dash_cooldown=math.max(1,settings.dash_cooldown)
end
local function settings_for_stage()
  local s=copy(STAGES[G.stage]);s.health,s.bombs,s.flares=2,1,1;s.torch_radius,s.dash_cooldown=4,3;s.bomb_radius,s.bomb_fuse=2,3;s.bullet_range,s.reload_penalty=nil,0
  if G.curse then
    local m=G.curse.modifiers;if m.health then s.health=m.health end;if m.bombs~=nil then s.bombs=m.bombs end;if m.flares~=nil then s.flares=m.flares end
    for _,name in ipairs({"vision","enemies","ammo","score","torches","torch_radius","dash_cooldown","bomb_radius","bomb_fuse","reload_penalty"}) do if m[name]~=nil then s[name]=(s[name] or 0)+m[name] end end
    if m.bullet_range then s.bullet_range=m.bullet_range end
  end
  apply(s,G.class.modifiers);apply(s,G.boon.modifiers);return s
end
local function occupied(include_boss)
  local r={[key(G.player.x,G.player.y)]=true};for _,list in ipairs({G.targets,G.enemies,G.bullets,G.bombs,G.flares,G.torches}) do for _,e in ipairs(list) do r[key(e.x,e.y)]=true end end
  if G.ammo then r[key(G.ammo.x,G.ammo.y)]=true end;if G.exit then r[key(G.exit.x,G.exit.y)]=true end;if include_boss then for x=16,24 do r[key(x,1)]=true end end;return r
end
local function open_location(space,used,minimum)
  local options={};for k in pairs(space) do local x,y=k:match("(%d+):(%d+)");local p=cell(tonumber(x),tonumber(y));if not used[k] and (not minimum or dist(p,G.player)>=minimum) then push(options,p) end end
  assert(#options>0,"No spawn location available");return options[love.math.random(#options)]
end
local function enemy_type(index) if G.settings.wilds then return index%3==0 and "bomber" or "wolf" end;return G.settings.cultists and "cultist" or "necromancer" end
local function make_enemy(kind,p)
  return entity(kind,p.x,p.y,{health=1,attack=0,attack_kind=nil,attack_windup=0,attack_x=nil,attack_y=nil,radius=1,stun=0})
end
local function clear_enemy_attack(e)
  e.attack,e.attack_kind,e.attack_windup,e.attack_x,e.attack_y=0,nil,0,nil,nil
end
local function begin_enemy_attack(e,kind,target,radius,windup)
  e.attack,e.attack_kind,e.attack_windup=1,kind,windup
  e.attack_x,e.attack_y,e.radius=target.x,target.y,radius
end
local function spawn_entities()
  G.targets,G.enemies,G.bullets,G.bombs,G.flares,G.torches={},{},{},{},{},{}
  for _=1,G.settings.torches do local p=open_location(G.space,occupied());push(G.torches,entity("torch",p.x,p.y,{light=G.settings.torch_radius})) end
  for _=1,G.settings.targets do local p=open_location(G.space,occupied());push(G.targets,entity("target",p.x,p.y)) end
  for i=1,G.settings.enemies do local p=open_location(G.space,occupied());push(G.enemies,make_enemy(enemy_type(i),p)) end
  local p=open_location(G.space,occupied());G.ammo=entity("ammo",p.x,p.y)
end
local function refill_entities()
  while #G.targets<G.settings.targets do local p=open_location(G.space,occupied());push(G.targets,entity("target",p.x,p.y)) end
  while #G.enemies<G.settings.enemies do local p=open_location(G.space,occupied());push(G.enemies,make_enemy(enemy_type(#G.enemies+1),p)) end
  if not G.ammo then local p=open_location(G.space,occupied());G.ammo=entity("ammo",p.x,p.y) end
end
local function start_stage()
  G.settings=settings_for_stage();G.player=entity("player",math.floor(WIDTH/2),math.floor(HEIGHT/2),{direction="w",health=G.settings.health,ammo=G.settings.ammo,bombs=G.settings.bombs,flares=G.settings.flares,score=0,dash=0,dash_base=G.settings.dash_cooldown,bomb_radius=G.settings.bomb_radius,bomb_fuse=G.settings.bomb_fuse,bullet_range=G.settings.bullet_range,reload_penalty=G.settings.reload_penalty,impact=0})
  G.camera_x,G.camera_y=G.player.x,G.player.y;G.explored={};G.held_direction=nil
  G.space=generate(G.settings.terrain,G.player);G.effects={};G.exit=nil;G.boss=nil;G.log={};G.phase="combat";G.screen="game";spawn_entities();log("Descend into the "..G.settings.terrain..".")
end

local function reload(amount,cursed) G.player.ammo=G.player.ammo+math.max(0,amount-(cursed and G.player.reload_penalty or 0)) end
local function hurt(message)
  G.player.health=math.max(0,G.player.health-1);G.player.impact=2
  G.hit_flash,G.hit_shake=.32,.24
  sound("hurt");log(message)
  if G.player.health==0 then G.screen="gameover" end
end
local function destroy_target(i) remove(G.targets,i);G.player.score=G.player.score+1;reload(1,true);sound("hit");log("Destroyed a target.") end
local function destroy_enemy(i) remove(G.enemies,i);G.player.score=G.player.score+1;reload(2,true);sound("hit");log("Defeated an enemy.") end
local function boss_hitbox() local r={};for x=16,24 do r[key(x,1)]=true end;return r end
local function damage_boss(amount) G.boss.health=G.boss.health-amount;sound("hit") end
local function destroy_terrain(origin,radius) for x=origin.x-radius,origin.x+radius do for y=origin.y-radius,origin.y+radius do if x>0 and x<WIDTH-1 and y>0 and y<HEIGHT-1 and math.abs(x-origin.x)+math.abs(y-origin.y)<=radius then G.space[key(x,y)]=true end end end end
local function update_bullets()
  local remaining={}
  for _,b in ipairs(G.bullets) do
    if b.active then if b.max and b.travel>=b.max then b.expired=true else local d=DIR[b.direction];move_entity(b,b.x+d[1],b.y+d[2]);b.travel=b.travel+1 end else b.active=true end
    local hit=b.expired or not open(b.x,b.y)
    for i=#G.targets,1,-1 do local t=G.targets[i];if t.x==b.x and t.y==b.y then destroy_target(i);hit=true;break end end
    if not hit then for i=#G.enemies,1,-1 do local e=G.enemies[i];if e.x==b.x and e.y==b.y then e.health=e.health-1;if e.health<=0 then destroy_enemy(i) end;hit=true;break end end end
    if not hit and G.boss and boss_hitbox()[key(b.x,b.y)] then damage_boss(1);hit=true end;if not hit then push(remaining,b) end
  end;G.bullets=remaining
end
local function update_bombs()
  local remaining={};for _,b in ipairs(G.bombs) do b.fuse=b.fuse-1;if b.fuse>0 then push(remaining,b) else
    destroy_terrain(b,b.radius);local cells=blast(b,b.radius);for k in pairs(cells) do G.effects[k]=true end;sound("boom")
    if cells[key(G.player.x,G.player.y)] then hurt("You were caught in the blast.") end
    for i=#G.targets,1,-1 do local e=G.targets[i];if cells[key(e.x,e.y)] then destroy_target(i) end end
    for i=#G.enemies,1,-1 do local e=G.enemies[i];if cells[key(e.x,e.y)] then e.health=e.health-2;if e.health<=0 then destroy_enemy(i) end end end
    if G.boss then for k in pairs(cells) do if boss_hitbox()[k] then damage_boss(2);break end end end
  end end;G.bombs=remaining
end
local function update_flares()
  local remaining={};for _,f in ipairs(G.flares) do f.fuse=f.fuse-1;if f.fuse>0 then push(remaining,f) else local cells=blast(f,f.radius);for k in pairs(cells) do G.effects[k]=true end;sound("flare");for _,e in ipairs(G.enemies) do if cells[key(e.x,e.y)] then e.stun=math.max(e.stun,f.stun);clear_enemy_attack(e) end end end end;G.flares=remaining
end
local function attack_cells(e)
  local r={};if e.attack==0 then return r end;for x=e.attack_x-e.radius,e.attack_x+e.radius do for y=e.attack_y-e.radius,e.attack_y+e.radius do if in_world(x,y) then r[key(x,y)]=true end end end;return r
end
local function nearest_wolf() local leader,near=nil,math.huge;for _,e in ipairs(G.enemies) do if e.kind=="wolf" and dist(e,G.player)<near then leader,near=e,dist(e,G.player) end end;return leader end
local function resolve_enemy_attack(e,i)
  if attack_cells(e)[key(G.player.x,G.player.y)] then
    local message={pounce="A forest wolf tore into you.",detonate="A bomber detonated beside you.",spell="A "..e.kind.." spell struck you."}
    hurt(message[e.attack_kind] or "An enemy attack struck you.")
  end
  if e.attack_kind=="detonate" then
    remove(G.enemies,i)
    log("A bomber exploded nearby!")
  else
    clear_enemy_attack(e)
  end
end
local function enemy_turn()
  local leader=nearest_wolf()
  for i=#G.enemies,1,-1 do local e=G.enemies[i]
    if e.stun>0 then e.stun=e.stun-1
    elseif e.attack==0 then
      local blocked={};for _,o in ipairs(G.enemies) do if o~=e then blocked[key(o.x,o.y)]=true end end;local hunt=G.player;if e.kind=="wolf" and e~=leader and leader and dist(e,leader)<=10 then hunt=leader end;local route=path(e,hunt,blocked)
      if e.kind=="bomber" and #route<=2 then begin_enemy_attack(e,"detonate",G.player,0,1)
      elseif e.kind=="wolf" and dist(e,G.player)<=1 then begin_enemy_attack(e,"pounce",G.player,0,1)
      elseif e.kind=="wolf" and #route>2 then move_entity(e,route[2].x,route[2].y)
      elseif #route>0 and #route-1<=4 then begin_enemy_attack(e,"spell",G.player,1,3)
      elseif #route>1 then move_entity(e,route[2].x,route[2].y) end
    else
      e.attack=e.attack+1
      if e.attack>e.attack_windup then resolve_enemy_attack(e,i) end
    end
  end
end
local function boss_cells()
  local b,r=G.boss,{};if not b.type then return r end
  if b.type=="crossfire" then for x=0,WIDTH-1 do r[key(x,b.y1)]=true;r[key(x,b.y2)]=true end;for y=0,HEIGHT-1 do r[key(b.x1,y)]=true;r[key(b.x2,y)]=true end
  elseif b.type=="diagonal" then for y=0,HEIGHT-1 do local a,c=b.x1+(y-b.y1),b.x1-(y-b.y1);if in_world(a,y) then r[key(a,y)]=true end;if in_world(c,y) then r[key(c,y)]=true end end
  else for x=b.x1-b.radius,b.x1+b.radius do for y=b.y1-b.radius,b.y1+b.radius do if in_world(x,y) and math.abs(x-b.x1)+math.abs(y-b.y1)==b.radius then r[key(x,y)]=true end end end end;return r
end
local function new_boss_attack()
  local b=G.boss;local options={"crossfire"};if b.health<8 then push(options,"diagonal") end;if b.health<4 then push(options,"pulse") end;b.type=options[love.math.random(#options)];b.name=({crossfire="CROSSFIRE",diagonal="DIAGONAL SWEEP",pulse="ARCANE PULSE"})[b.type];b.radius=love.math.random(2,4)
  local points={};for k in pairs(G.space) do local x,y=k:match("(%d+):(%d+)");push(points,cell(tonumber(x),tonumber(y))) end;local a,c=points[love.math.random(#points)],points[love.math.random(#points)];b.x1,b.y1,b.x2,b.y2=a.x,a.y,c.x,c.y
end
local function boss_turn()
  local b=G.boss;b.attack=b.attack+1;if b.attack>BOSS_WINDUP then b.attack=0;new_boss_attack() elseif b.attack==BOSS_WINDUP and boss_cells()[key(G.player.x,G.player.y)] then hurt("The boss attack struck you.") end
  local limit=b.health<4 and 3 or(b.health<8 and 2 or 1);if b.attack==0 and #G.enemies<limit then local p=open_location(G.space,occupied(true),6);push(G.enemies,make_enemy("necromancer",p));log("The boss summoned a necromancer.") end
end
local function collect_ammo() if G.ammo and G.player.x==G.ammo.x and G.player.y==G.ammo.y then reload(1,false);G.ammo=nil;sound("pickup");log("Collected ammo.") end end
local function move_player(direction)
  local p,d=G.player,DIR[direction];local moved=false;if p.direction==direction and open(p.x+d[1],p.y+d[2]) then move_entity(p,p.x+d[1],p.y+d[2]);moved=true elseif p.direction==direction then log("A wall blocks your path.") end;p.direction=direction;log(moved and "Moved "..d[3].."." or "Faced "..d[3]..".")
end
local function dash()
  local p=G.player;if p.dash>0 then log("Dash is recharging.");return end;local d=DIR[p.direction];local ox,oy=p.x,p.y;for _=1,2 do if open(p.x+d[1],p.y+d[2]) then move_entity(p,p.x+d[1],p.y+d[2]) else break end end;if p.x==ox and p.y==oy then log("Dash blocked.");return end;p.dash=p.dash_base;sound("step");log("Dashed forward.")
end
local function action(input)
  local p=G.player;if DIR[input] then move_player(input);sound("step");return end;if p.impact>0 then log("You are recovering from the hit.");return end
  if input=="q" then dash()
  elseif input=="e" then if p.ammo<=0 then log("No more ammo, find more to shoot.") else p.ammo=p.ammo-1;push(G.bullets,entity("bullet",p.x,p.y,{direction=p.direction,active=false,travel=1,max=p.bullet_range,light=2}));sound("shoot");log("Fired a shot.") end
  elseif input=="b" then if p.bombs<=0 then log("No bombs left. Buy bombs in the shop.") else p.bombs=p.bombs-1;push(G.bombs,entity("bomb",p.x,p.y,{fuse=p.bomb_fuse,radius=p.bomb_radius,light=3}));sound("select");log("Bomb armed. Move away before it explodes.") end
  elseif input=="f" then if p.flares<=0 then log("No flares left. Buy flares in the shop.") else p.flares=p.flares-1;push(G.flares,entity("flare",p.x,p.y,{fuse=2,radius=1,stun=2,light=3}));sound("flare");log("Flare lit. Necromancers will be stunned.") end end
end
local function begin_exit() local p=open_location(G.space,occupied(),6);G.exit=entity("door",p.x,p.y);G.phase="exit";log("All targets are down. Find the exit.") end
local function draw_curses() if #G.curse_bag<3 then G.curse_bag=shuffled(CURSES) end;G.curse_options={table.remove(G.curse_bag,1),table.remove(G.curse_bag,1),table.remove(G.curse_bag,1)} end
local function complete_stage() G.score=G.score+G.player.score;if G.stage<#STAGES then G.stage=G.stage+1;draw_curses();G.screen="curse";G.menu=1 else G.screen="shop";G.menu=1 end end
local function turn(input)
  G.effects={};G.player.dash=math.max(0,G.player.dash-1);G.player.impact=math.max(0,G.player.impact-1)
  if G.phase=="exit" then if DIR[input] then move_player(input) elseif input=="q" then dash() end;if G.player.x==G.exit.x and G.player.y==G.exit.y then complete_stage() end;return end
  action(input);collect_ammo();update_bullets();update_bombs();update_flares();if G.screen~="game" then return end
  if G.phase=="boss" then if G.boss.health<=0 then G.screen="victory";sound("door") else boss_turn();if G.boss.health>0 then enemy_turn() end end
  elseif G.player.score>=G.settings.score then begin_exit() else enemy_turn();refill_entities() end
end
local function start_boss()
  local p=G.player;p.x,p.y,p.render_x,p.render_y,p.direction,p.score=20,9,20,9,"w",0;G.camera_x,G.camera_y=p.x,p.y;G.explored={};G.held_direction=nil;p.bombs,p.flares=math.max(1,p.bombs),math.max(1,p.flares);p.dash=0;p.dash_base=math.max(1,3+(G.class.modifiers.dash_cooldown or 0)+(G.boon.modifiers.dash_cooldown or 0));p.bomb_radius,p.bomb_fuse,p.bullet_range,p.reload_penalty=2,3,nil,0
  G.settings={terrain="arena",vision=99,score=10};G.space=generate("arena",p,true);G.targets,G.enemies,G.bullets,G.bombs,G.flares,G.torches={},{},{},{},{},{};G.effects={};G.exit=nil;G.boss={kind="boss",x=16,y=1,health=10,attack=0,type="crossfire",name="CROSSFIRE",radius=3,line="PuNy MoRtAl, yoU dArE cHalLenGE mE?"};new_boss_attack();local a=open_location(G.space,occupied(true));G.ammo=entity("ammo",a.x,a.y);G.phase="boss";G.screen="game";log("The boss awaits.")
end

-- Rendering and menus
local function quad(kind) local s=SPRITE[kind] or SPRITE.target;return G.quads[s[1]..":"..s[2]] end
local function draw_sprite(kind,x,y,size,tint) if tint then color(tint) else love.graphics.setColor(1,1,1) end;love.graphics.draw(G.sheet,quad(kind),x,y,0,size/16,size/16);love.graphics.setColor(1,1,1) end
local function layout() local w,h=love.graphics.getDimensions();local margin,sidebar,gap=20,270,20;local size=math.max(10,math.min(24,math.floor(math.min((w-sidebar-gap-margin*2)/VIEW_W,(h-130)/VIEW_H))));return size,margin,margin,margin+VIEW_W*size+gap end
local function camera_position() return G.camera_x or G.player.render_x or G.player.x,G.camera_y or G.player.render_y or G.player.y end
local function screen_position(x,y,size,ox,oy) local camera_x,camera_y=camera_position();local sx=x-camera_x+math.floor((VIEW_W-1)/2);local sy=y-camera_y+math.floor((VIEW_H-1)/2);if sx<-1 or sx>=VIEW_W+1 or sy<-1 or sy>=VIEW_H+1 then return nil end;return ox+sx*size,oy+(VIEW_H-1-sy)*size end
local function has_line_of_sight(x0,y0,x1,y1)
  local dx,dy=math.abs(x1-x0),math.abs(y1-y0)
  local sx,sy=x0<x1 and 1 or -1,y0<y1 and 1 or -1
  local err=dx-dy
  while x0~=x1 or y0~=y1 do
    local twice=err*2
    if twice>-dy then err=err-dy;x0=x0+sx end
    if twice<dx then err=err+dx;y0=y0+sy end
    if x0~=x1 or y0~=y1 then
      if not open(x0,y0) then return false end
    end
  end
  return true
end
local function light_area(source,radius,visible)
  if radius>=math.max(WIDTH,HEIGHT) then
    for x=0,WIDTH-1 do for y=0,HEIGHT-1 do visible[key(x,y)]=true end end
    return
  end
  for x=source.x-radius,source.x+radius do
    for y=source.y-radius,source.y+radius do
      local dx,dy=x-source.x,y-source.y
      if in_world(x,y) and dx*dx+dy*dy<=radius*radius and has_line_of_sight(source.x,source.y,x,y) then visible[key(x,y)]=true end
    end
  end
end
local function visibility()
  local visible={};G.explored=G.explored or {}
  light_area(G.player,G.settings.vision,visible)
  for _,list in ipairs({G.torches,G.bombs,G.flares,G.bullets}) do
    for _,e in ipairs(list) do if e.light then light_area(e,e.light,visible) end end
  end
  for k in pairs(G.effects) do visible[k]=true end
  for k in pairs(visible) do G.explored[k]=true end
  return visible,G.explored
end
local function telegraphs()
  local r={};for _,e in ipairs(G.enemies) do if e.attack>0 then for k in pairs(attack_cells(e)) do r[k]=e.attack>=e.attack_windup and "danger" or "warn" end end end;if G.boss then for k in pairs(boss_cells()) do r[k]=G.boss.attack>=BOSS_WINDUP-1 and "danger" or "warn" end end;return r
end
local function enemy_intent(e)
  if e.stun>0 then return "STUNNED" end
  if e.attack>0 then return string.upper(e.attack_kind).." IN "..math.max(1,e.attack_windup-e.attack+1) end
  return e.kind=="wolf" and "POUNCE" or e.kind=="bomber" and "DETONATE" or "ADVANCE"
end
local function draw_game()
  local size,ox,oy,hud=layout();love.graphics.clear(.025,.035,.055);local shake=(G.hit_shake or 0)/.24;local time=love.timer.getTime();ox,oy=ox+math.sin(time*78)*6*shake,oy+math.cos(time*93)*4*shake;color({.08,.1,.14});love.graphics.rectangle("fill",ox-4,oy-4,VIEW_W*size+8,VIEW_H*size+8);local floor=({forest={.09,.19,.13},cave={.12,.14,.18},dungeon={.16,.12,.18},arena={.14,.12,.17}})[G.settings.terrain]
  local visible,explored=visibility()
  local camera_x,camera_y=camera_position();local base_x,base_y=math.floor(camera_x)-math.floor((VIEW_W-1)/2),math.floor(camera_y)-math.floor((VIEW_H-1)/2);local offset_x,offset_y=camera_x-math.floor(camera_x),camera_y-math.floor(camera_y)
  for vx=-1,VIEW_W do for vy=-1,VIEW_H do local x,y=base_x+vx,base_y+vy;local px,py=ox+(vx-offset_x)*size,oy+(VIEW_H-1-vy+offset_y)*size;local tile=key(x,y);if in_world(x,y) and visible[tile] then if open(x,y) then color(floor);love.graphics.rectangle("fill",px,py,size,size);if (x*7+y*11)%5==0 then color({floor[1]*1.55,floor[2]*1.55,floor[3]*1.55});love.graphics.rectangle("fill",px+size*.35,py+size*.35,math.max(1,size*.12),math.max(1,size*.12)) end else color({.11,.075,.13});love.graphics.rectangle("fill",px,py,size,size);color({.22,.13,.22});love.graphics.rectangle("line",px,py,size,size) end elseif in_world(x,y) and explored[tile] then color(open(x,y) and {floor[1]*.28,floor[2]*.28,floor[3]*.28} or {.035,.025,.04});love.graphics.rectangle("fill",px,py,size,size) else color({.008,.011,.017});love.graphics.rectangle("fill",px,py,size,size) end end end
  for k,style in pairs(telegraphs()) do local x,y=k:match("(%d+):(%d+)");local px,py=screen_position(tonumber(x),tonumber(y),size,ox,oy);if px then color(style=="danger" and {1,.2,.15,.4} or {1,.75,.15,.28});love.graphics.rectangle("fill",px,py,size,size) end end
  local function actor(e,always_visible)
    if not e or (not always_visible and not visible[key(e.x,e.y)]) then return end
    local px,py=screen_position(e.render_x or e.x,e.render_y or e.y,size,ox,oy)
    if not px then return end
    local tint=e.stun and e.stun>0 and {1,.85,.2} or nil
    if e==G.player and (G.hit_flash or 0)>0 then tint={1,.35,.35} end
    draw_sprite(e.kind,px,py,size,tint)
  end
  for _,list in ipairs({G.torches,G.targets,G.enemies,G.bullets,G.bombs,G.flares}) do for _,e in ipairs(list) do actor(e) end end;actor(G.ammo);actor(G.exit);for k in pairs(G.effects) do local x,y=k:match("(%d+):(%d+)");actor(entity("flare",tonumber(x),tonumber(y))) end;if G.boss then for x=16,24 do actor(entity("boss",x,1)) end end;actor(G.player,true);local px,py=screen_position(G.player.render_x,G.player.render_y,size,ox,oy);if px then color({.15,.9,1});love.graphics.rectangle("line",px,py,size,size);if (G.hit_flash or 0)>0 then color({1,.2,.2,.75});love.graphics.setLineWidth(2);love.graphics.rectangle("line",px-size*.12,py-size*.12,size*1.24,size*1.24);love.graphics.setLineWidth(1) end end
  text("ROAG",hud,oy,2,{.7,.9,1});text("HP    "..string.rep("♥",G.player.health),hud,oy+42,1+(G.hit_flash or 0)*.8,{1,.35,.35});text("AMMO  "..G.player.ammo,hud,oy+64);text("BOMBS "..G.player.bombs.."  ARMED "..#G.bombs,hud,oy+84);text("FLARES "..G.player.flares.."  LIT "..#G.flares,hud,oy+104);text("DASH  "..(G.player.dash==0 and "READY" or "RECHARGING"),hud,oy+124);text(G.boss and "BOSS "..G.boss.health.." / 10" or "SCORE "..G.player.score.." / "..G.settings.score,hud,oy+150,1,{.95,.85,.25});if G.curse then text("CURSE "..G.curse.name,hud,oy+174,1,{.9,.4,.8}) end
  if G.boss then text("BOSS "..G.boss.name.." IN "..math.max(0,BOSS_WINDUP-G.boss.attack),hud,oy+198,.8,{1,.6,.35}) end;text("CONTROLS",hud,oy+246,1,{.6,.8,1});text("WASD / ARROWS: FACE, MOVE, HOLD",hud,oy+266,.7);text("E shoot  Q dash",hud,oy+284,.85);text("B bomb   F flare",hud,oy+302,.85);text("INTENTS",hud,oy+338,1,{.9,.7,.4});for i,e in ipairs(G.enemies) do if i>5 then break end;text(string.upper(e.kind)..": "..enemy_intent(e),hud,oy+356+i*17,.75) end;for i,message in ipairs(G.log) do text(message,20,oy+VIEW_H*size+16+(i-1)*17,.78,{.8,.85,.9}) end
  if (G.hit_flash or 0)>0 then local w,h=love.graphics.getDimensions();color({1,.04,.04,(G.hit_flash/.32)*.16});love.graphics.rectangle("fill",0,0,w,h) end
end

local function sprite_lab_item() return SPRITE_ORDER[G.sprite_slot] end
local function select_sprite_slot(delta)
  G.sprite_slot=clamp(G.sprite_slot+delta,1,#SPRITE_ORDER)
  local tile=SPRITE[sprite_lab_item().kind]
  G.sprite_x,G.sprite_y=tile[1],tile[2]
  sound("select")
end
local function reset_sprite(kind)
  local tile=DEFAULT_SPRITE[kind]
  SPRITE[kind]={tile[1],tile[2]}
end
local function open_sprite_lab()
  G.screen="sprite_lab"
  G.sprite_slot=G.sprite_slot or 1
  local tile=SPRITE[sprite_lab_item().kind]
  G.sprite_x,G.sprite_y=tile[1],tile[2]
end
local function draw_sprite_lab()
  love.graphics.clear(.025,.035,.055)
  local w,h=love.graphics.getDimensions()
  local scale=math.min(1,math.max(.5,math.min((w-460)/(49*16),(h-130)/(22*16))))
  local tile_size=16*scale
  local sheet_w,sheet_h=49*tile_size,22*tile_size
  local sheet_x,sheet_y=w-sheet_w-28,88
  local current=sprite_lab_item()

  text("SPRITE LAB",28,26,2,{.7,.9,1})
  text("EDITING "..current.label,28,72,1.15,{.95,.85,.3})
  text("HIGHLIGHTED TILE: COLUMN "..G.sprite_x.."  ROW "..G.sprite_y,28,98,.8,{.8,.85,.9})
  love.graphics.draw(G.sheet,G.quads[G.sprite_x..":"..G.sprite_y],28,122,0,4,4)

  for i,item in ipairs(SPRITE_ORDER) do
    local y=198+(i-1)*29
    if i==G.sprite_slot then color({.13,.22,.3});love.graphics.rectangle("fill",24,y-3,390,26) end
    draw_sprite(item.kind,30,y,24)
    local tile=SPRITE[item.kind]
    text(item.label.."  ["..tile[1]..", "..tile[2].."]",62,y+2,.82,i==G.sprite_slot and {.95,.85,.3} or {.78,.83,.9})
  end

  love.graphics.draw(G.sheet,sheet_x,sheet_y,0,scale,scale)
  local function outline(column,row,tint,width)
    color(tint);love.graphics.setLineWidth(width)
    love.graphics.rectangle("line",sheet_x+(column-1)*tile_size,sheet_y+(row-1)*tile_size,tile_size,tile_size)
  end
  local assigned=SPRITE[current.kind]
  outline(assigned[1],assigned[2],{.15,.9,1},2)
  outline(G.sprite_x,G.sprite_y,{1,.85,.2},3)
  love.graphics.setLineWidth(1)

  text("WASD / ARROWS: BROWSE TILE",28,h-104,.78,{.75,.82,.92})
  text("Q / E: CHANGE CHARACTER     ENTER: ASSIGN LIVE",28,h-82,.78,{.75,.82,.92})
  text("R: RESET CHARACTER     X: RESET ALL     P / ESC: RETURN",28,h-60,.78,{.75,.82,.92})
  text("PREVIEW ONLY — SAVE MAPPINGS IN THE STANDALONE SPRITE EDITOR",28,h-34,.72,{.95,.65,.45})
end
local function menu(title,items,footer)
  love.graphics.clear(.025,.035,.055);local w,h=love.graphics.getDimensions();text(title,w/2-#title*8,70,2,{.7,.9,1});for i,item in ipairs(items) do local y=150+(i-1)*84;local selected=i==G.menu;color(selected and {.13,.22,.3} or {.06,.08,.12});love.graphics.rectangle("fill",w*.18,y,w*.64,68);text((selected and "> " or "  ")..item.name,w*.21,y+9,1.25,selected and {.95,.85,.3} or {1,1,1});text(item.description or "",w*.21,y+37,.85,{.72,.76,.84}) end;text(footer or "W/S SELECT     ENTER CONFIRM",w/2-150,h-52,1,{.65,.75,.9})
end
local SHOP={{name="HEALTH",description="BUY [B] / SELL [V] — MAX 5",key="health",minimum=1},{name="AMMO",description="BUY [B] / SELL [V] — MAX 5",key="ammo",minimum=1},{name="BOMBS",description="BUY [B] / SELL [V] — MAX 5",key="bombs",minimum=0},{name="FLARES",description="BUY [B] / SELL [V] — MAX 5",key="flares",minimum=0}}
local function draw_shop() menu("SHOP — POINTS "..G.score,SHOP,"W/S SELECT     B BUY     V SELL     ENTER FIGHT BOSS") end

function love.update(dt)
  G.hit_flash=math.max(0,(G.hit_flash or 0)-dt);G.hit_shake=math.max(0,(G.hit_shake or 0)-dt)
  if G.screen~="game" or not G.player then return end
  if G.held_direction then
    G.hold_timer=(G.hold_timer or HOLD_INITIAL_DELAY)-dt
    if G.hold_timer<=0 then
      G.hold_timer=HOLD_REPEAT_DELAY
      local d=DIR[G.held_direction]
      if G.player.direction==G.held_direction and open(G.player.x+d[1],G.player.y+d[2]) then turn(G.held_direction) end
    end
  end
  local function animate(e)
    e.render_x=slide_axis(e.render_x or e.x,e.x,dt)
    e.render_y=slide_axis(e.render_y or e.y,e.y,dt)
  end
  animate(G.player)
  G.camera_x=follow_camera_axis(G.camera_x or G.player.render_x,G.player.render_x,dt)
  G.camera_y=follow_camera_axis(G.camera_y or G.player.render_y,G.player.render_y,dt)
  for _,list in ipairs({G.enemies,G.bullets}) do
    for _,e in ipairs(list) do animate(e) end
  end
end

function love.load()
  love.graphics.setDefaultFilter("nearest","nearest")
  G.font=love.graphics.newFont("assets/fonts/BigBlueTermPlusNerdFontMono-Regular.ttf",16)
  love.graphics.setFont(G.font)
  G.sheet=love.graphics.newImage("assets/kenney/Tilesheet/colored-transparent_packed.png")
  G.quads={}
  for x=1,49 do
    for y=1,22 do
      G.quads[x..":"..y]=love.graphics.newQuad((x-1)*16,(y-1)*16,16,16,G.sheet)
    end
  end
  load_sprite_mappings()
  G.sounds={}
  for name,file in pairs({step="footstep00",shoot="drawKnife1",hit="chop",boom="metalPot3",flare="metalClick",hurt="knifeSlice",door="doorOpen_1",pickup="handleCoins",select="bookFlip2"}) do
    local ok,source=pcall(love.audio.newSource,"assets/sounds/OGG/"..file..".ogg","static")
    if ok then G.sounds[name]=source end
  end
end
function love.focus(focused) if focused then load_sprite_mappings() end end
function love.draw() if G.screen=="game" then draw_game() elseif G.screen=="title" then love.graphics.clear(.025,.035,.055);local w,h=love.graphics.getDimensions();text("ROAG",w/2-104,h/2-100,4,{.7,.9,1});text("A ONE-BIT DESCENT",w/2-110,h/2-34,1.2,{.7,.75,.85});text("PRESS ENTER TO BEGIN",w/2-115,h/2+48,1,{.95,.85,.3});text("P: SPRITE LAB",w/2-62,h/2+78,.82,{.75,.82,.92}) elseif G.screen=="sprite_lab" then draw_sprite_lab() elseif G.screen=="class" then menu("CHOOSE YOUR CLASS",CLASSES) elseif G.screen=="boon" then menu("CHOOSE A BOON",G.boon_options) elseif G.screen=="curse" then menu("CHOOSE A CURSE",G.curse_options,"W/S SELECT     ENTER ACCEPT BURDEN") elseif G.screen=="shop" then draw_shop() elseif G.screen=="gameover" then menu("YOU DIED",{{name="RETURN TO TITLE",description="Press Enter to begin a new descent."}},"") elseif G.screen=="victory" then menu("YOU HAVE WON",{{name="THE DESCENT IS OVER",description="Press Enter to return to the title."}},"") end end
local function move_menu(n,limit) G.menu=clamp(G.menu+n,1,limit);sound("select") end
function love.keypressed(k,scancode,isrepeat)
  if G.screen=="sprite_lab" then
    if k=="escape" or k=="p" then G.screen="title";return end
    if k=="q" then select_sprite_slot(-1)
    elseif k=="e" then select_sprite_slot(1)
    elseif k=="a" or k=="left" then G.sprite_x=clamp(G.sprite_x-1,1,49)
    elseif k=="d" or k=="right" then G.sprite_x=clamp(G.sprite_x+1,1,49)
    elseif k=="w" or k=="up" then G.sprite_y=clamp(G.sprite_y-1,1,22)
    elseif k=="s" or k=="down" then G.sprite_y=clamp(G.sprite_y+1,1,22)
    elseif k=="return" or k=="space" then SPRITE[sprite_lab_item().kind]={G.sprite_x,G.sprite_y};sound("select")
    elseif k=="r" then reset_sprite(sprite_lab_item().kind);local tile=SPRITE[sprite_lab_item().kind];G.sprite_x,G.sprite_y=tile[1],tile[2];sound("select")
    elseif k=="x" then for kind in pairs(SPRITE) do reset_sprite(kind) end;local tile=SPRITE[sprite_lab_item().kind];G.sprite_x,G.sprite_y=tile[1],tile[2];sound("select") end
    return
  end
  if k=="escape" then love.event.quit();return end
  if G.screen=="title" then if k=="p" then open_sprite_lab() elseif k=="return" or k=="space" then G.screen="class";G.menu=1;sound("select") end;return end
  if G.screen=="class" then if k=="w" or k=="up" then move_menu(-1,#CLASSES) elseif k=="s" or k=="down" then move_menu(1,#CLASSES) elseif k=="return" or k=="e" then G.class=CLASSES[G.menu];G.boon_options={};for i,v in ipairs(shuffled(BOONS)) do if i<=3 then push(G.boon_options,v) end end;G.menu=1;G.screen="boon" end;return end
  if G.screen=="boon" then if k=="w" or k=="up" then move_menu(-1,#G.boon_options) elseif k=="s" or k=="down" then move_menu(1,#G.boon_options) elseif k=="return" or k=="e" then G.boon=G.boon_options[G.menu];G.stage=1;G.score=0;G.curse=nil;start_stage() end;return end
  if G.screen=="curse" then if k=="w" or k=="up" then move_menu(-1,#G.curse_options) elseif k=="s" or k=="down" then move_menu(1,#G.curse_options) elseif k=="return" or k=="e" then G.curse=G.curse_options[G.menu];start_stage() end;return end
  if G.screen=="shop" then if k=="w" or k=="up" then move_menu(-1,#SHOP) elseif k=="s" or k=="down" then move_menu(1,#SHOP) elseif k=="b" then local i=SHOP[G.menu];if G.score>0 and G.player[i.key]<5 then G.player[i.key]=G.player[i.key]+1;G.score=G.score-1;sound("pickup") else log("Cannot buy that item.") end elseif k=="v" then local i=SHOP[G.menu];if G.player[i.key]>i.minimum then G.player[i.key]=G.player[i.key]-1;G.score=G.score+1;sound("select") else log("That item cannot be sold.") end elseif k=="return" or k=="e" then start_boss() end;return end
  if G.screen=="gameover" or G.screen=="victory" then if k=="return" then G.screen="title" end;return end
  if G.screen=="game" then
    local direction=MOVE_KEY[k]
    if direction then
      if not isrepeat then G.held_direction,G.hold_timer=direction,HOLD_INITIAL_DELAY;turn(direction) end
    elseif k=="q" or k=="e" or k=="b" or k=="f" then turn(k) end
  end
end
function love.keyreleased(k)
  if MOVE_KEY[k] and G.held_direction==MOVE_KEY[k] then G.held_direction=nil;G.hold_timer=nil end
end
