local G=require('src.game')
local D=require('src.dice_trial')
local Pixel=require('src.pixel_ui')
local P=require('src.dice_presentation')
local V=require('src.dice_view')
local U={}
local host
local dirs={w={0,-1},up={0,-1},d={1,0},right={1,0},s={0,1},down={0,1},a={-1,0},left={-1,0},q={-1,-1},e={1,-1},z={-1,1},c={1,1}}
function U.attach(h)host=h;U.motion=nil end
local function motion()
 if not U.motion then U.motion=P.new(host.state().dice,U.reduced);P.finish(U.motion)end
 return U.motion
end
local function sound()
 local p=motion()
 if host.dice_sound then for _,kind in ipairs(p.sounds)do host.dice_sound(kind)end end
 p.sounds={}
end
function U.set_inspect(open,die)
 U.inspect=open;local p=motion();P.inspect(p,open,die or U.inspect_die or 1);U.inspect_die=p.net_die;sound()
end
function U.on_events(events)
 local p=motion();local by_id={}
 for _,e in ipairs(events)do by_id[e.id]=e end
 p.labels=p.labels or {}
 for _,e in ipairs(events)do
  if e.kind=='BoonActivated'and e.boon_id=='core:boon/rhythm_of_steel'then
   local trigger=by_id[e.trigger_event_id]
   p.labels[e.id]=trigger and trigger.kind=='Moved'and 'RHYTHM READY'or 'RHYTHM BOOST'
  end
 end
end
function U.active()return host and host.state()and host.state().dice~=nil end
function U.clear()U.selected=nil;U.aim=nil;U.preview=nil;U.forecast=nil;U.cache_key=nil;U.hover=nil;host.target(nil)end
function U.open(seed)U.seed=seed or 202;U.start()end
function U.start()
 host.start(U.seed,'bare','dice');U.clear();U.menu=false;U.inspect=false;U.await_update=false;U.message=nil;U.edit_die=1;U.edit_index=1;U.edit_type=1;U.seen=host.state().dice.revision;U.shown=G.snapshot(host.state()).dice.results
 U.motion=P.new(host.state().dice,U.reduced or host.effects and not host.effects());U.inspect_die=1;U.inspect_index=1
 sound()
end
local function intent(n,dx,dy)return {kind='dice_action',die=n,dx=dx,dy=dy,revision=host.state().dice.revision}end
local function aim(i)
 U.aim=i;U.preview=D.plan(host.state(),i,G);U.forecast=D.forecast(host.state(),i,G)
end
local function commit(i)
 if host.busy()or U.await_update then return end
 local p=motion();local face=host.state().dice.results[i.die].face
 local ok,why=host.submit(i)
 if ok then P.use(p,i.die,face,i.dx,i.dy);P.sync(p,host.state().dice);sound() end
 if ok then U.await_update=true;U.clear();U.seen=host.state().dice.revision else U.message=why end
 return ok
end
local function choose(n)
 U.clear();U.selected=n;P.select(motion(),n)
 if D.faces[host.state().dice.results[n].face].kind=='dice_guard'then commit(intent(n))end
end
function U.update(dt)
 if not U.active()then return end
 U.await_update=false
 local p=motion();p.reduced=U.reduced or host.effects and not host.effects() or false
 if P.sync(p,host.state().dice)then U.clear()end
 P.update(p,dt or 1/60,host.busy(),U.hover or U.selected)
 if host.playback_events and (not host.effects or host.effects())then P.events(p,host.playback_events(),host.state().player_id)end
 if host.state().card_trial.phase=='face_edit'then P.inspect(p,true,U.edit_die)end
 sound()
 if not host.busy()then
  if U.seen~=host.state().dice.revision then U.clear();U.seen=host.state().dice.revision end
  U.shown=host.state().dice.results
 end
end
local function edit(key)
 if key=='escape'then U.edit_type=math.ceil(U.edit_index/2);U.message='Edit cancelled; choose again or Skip.'
 elseif key=='left'then U.edit_index=(U.edit_index-2)%6+1
 elseif key=='right'then U.edit_index=U.edit_index%6+1
 elseif key=='up'then U.edit_type=(U.edit_type-2)%3+1
 elseif key=='down'then U.edit_type=U.edit_type%3+1
 elseif tonumber(key)and tonumber(key)>=1 and tonumber(key)<=3 then U.edit_die=tonumber(key);U.edit_type=math.ceil(U.edit_index/2)
 elseif key=='return' or key=='k'then
  local p=motion();local old=host.state().dice.dice[U.edit_die][U.edit_index]
  local ok,why=host.submit({kind='dice_edit',skip=key=='k',die=U.edit_die,index=U.edit_index,replacement=D.catalogue[U.edit_die][U.edit_type]})
  U.message=why;if ok then
   P.sync(p,host.state().dice)
   if key~='k'then P.edit(p,U.edit_die,U.edit_index,old,D.catalogue[U.edit_die][U.edit_type])else P.inspect(p,false)end
   U.clear();sound()
  end
 end
end
function U.keyreleased(key)
 if U.downkeys then U.downkeys[key]=nil end
end
function U.keypressed(key,repeat_key,physical)
 if not U.active()then return false end
 if physical then
  U.downkeys=U.downkeys or {}
  if U.downkeys[key]then return true end
  U.downkeys[key]=true
 end
 if repeat_key or host.busy()or U.await_update then return true end
 local s=host.state();local phase=s.card_trial.phase
 if U.menu then
  if key=='escape' or key=='return'then U.menu=false elseif key=='m'then U.reduced=not U.reduced;motion().reduced=U.reduced;if U.reduced then P.finish(motion())end elseif key=='r'then U.start()elseif key=='t'then host.exit()end;return true
 end
 if phase=='death' or phase=='victory'then if key=='r' or key=='return'then U.start()elseif key=='escape' or key=='t'then host.exit()end;return true end
 if phase=='face_edit'then edit(key);if host.state().card_trial.phase=='face_edit'then P.inspect(motion(),true,U.edit_die)end;return true end
 if key=='i' or key=='f1'then U.set_inspect(not U.inspect);return true end
 if U.inspect then
  if key=='escape'then U.set_inspect(false)
  elseif tonumber(key)and tonumber(key)>=1 and tonumber(key)<=3 then U.set_inspect(true,tonumber(key))
  elseif key=='left'then U.inspect_index=(U.inspect_index-2)%6+1
  elseif key=='right'then U.inspect_index=U.inspect_index%6+1 end
  return true
 end
 local p=motion()
 if p.opening and p.rolling then P.finish(p);return true end
 if p.edit then p.edit=nil;p.net=0;p.net_target=false;P.finish(p);return true end
 if key=='escape'then if U.selected or U.aim then U.clear()else U.menu=true end
 elseif key=='1' or key=='2' or key=='3'then choose(tonumber(key))
 elseif key=='f'then choose(2)
 elseif dirs[key]then
  local d=dirs[key];local n=U.selected or 1
  if not U.selected then
   local p=G.player(s).position
   for _,e in ipairs(s.entities)do if e.faction=='enemy' and e.position.x==p.x+d[1]and e.position.y==p.y+d[2]then n=2;break end end
  end
  commit(intent(n,d[1],d[2]))
 elseif key=='return'and U.aim then commit(U.aim)end
 return true
end
local function at(x,y)
 local cx,cy=host.cell(x,y);if not cx then return end
 local s=host.state();local p=G.player(s).position;local dx,dy=cx-p.x,cy-p.y;local n=U.selected
 if not n then
  for _,e in ipairs(s.entities)do if e.faction=='enemy'and e.position.x==cx and e.position.y==cy then n=2;break end end
  n=n or 1
 end
 local face=s.dice.results[n].face
 if face=='dash' or face=='pierce'then
  if dx==0 and dy==0 or math.max(math.abs(dx),math.abs(dy))>2 or dx~=0 and dy~=0 and math.abs(dx)~=math.abs(dy)then return end
  dx=dx==0 and 0 or dx>0 and 1 or -1;dy=dy==0 and 0 or dy>0 and 1 or -1
 end
 return intent(n,dx,dy)
end
function U.mousemoved(x,y)
 if not U.active()then return false end
 if host.busy()or U.menu or U.inspect or host.state().card_trial.phase~='combat'then return true end
 if x>=672 and x<992 and y>=114 and y<354 then U.hover=math.floor((y-114)/80)+1;U.aim=nil;U.preview=nil;U.forecast=nil;U.cache_key=nil
  if D.faces[host.state().dice.results[U.hover].face].kind=='dice_guard'then aim(intent(U.hover))end
 else U.hover=nil;local i=at(x,y);if i then
  local key=table.concat({i.revision,i.die,i.dx or 0,i.dy or 0},':')
  if key~=U.cache_key then aim(i);U.cache_key=key end
 else U.aim=nil;U.preview=nil;U.forecast=nil;U.cache_key=nil end end
 return true
end
function U.mousepressed(x,y,button,presses)
 if not U.active()then return false end
 if host.busy()or U.await_update or (presses or 1)>1 then return true end
 if button==2 then U.keypressed('escape');return true end
 if button~=1 then return true end
 if U.menu then U.keypressed('escape');return true end
 if U.inspect then
  if x>=672 and x<992 and y>=110 and y<140 then U.set_inspect(true,math.min(3,math.floor((x-672)/108)+1))
  else local j=V.hit(motion(),x,y);if j then U.inspect_index=j else U.set_inspect(false)end end
  return true
 end
 local phase=host.state().card_trial.phase
 if phase=='death'or phase=='victory'then U.keypressed('r');return true end
 if phase=='face_edit'then
  if x>=672 and x<992 then
   if y>=110 and y<140 then U.edit_die=math.min(3,math.floor((x-672)/108)+1);P.inspect(motion(),true,U.edit_die)
   elseif y>=422 and y<476 then U.edit_type=math.min(3,math.floor((y-422)/18)+1)
   elseif y>=480 and y<512 then U.keypressed('return')elseif y>=514 and y<542 then U.keypressed('k')
   else local j=V.hit(motion(),x,y);if j then U.edit_index=j end end
  end
 else
  local p=motion()
  if p.opening and p.rolling then P.finish(p);return true end
  if p.edit then p.edit=nil;p.net=0;p.net_target=false;P.finish(p);return true end
  if x>=672 and x<992 and y>=114 and y<354 then choose(math.floor((y-114)/80)+1)
  elseif x>=672 and y>=470 then U.set_inspect(true)
  else local i=at(x,y);if i then commit(i)end end
 end
 return true
end
function U.gamepadpressed(button)
 if not U.active()then return false end
 local keys={dpup='up',dpdown='down',dpleft='left',dpright='right',a='1',x='2',y='3',b='escape',start='escape'}
 if keys[button]then U.keypressed(keys[button])end;return true
end
local function text(t,x,y,font,color,width)
 love.graphics.setFont(font);love.graphics.setColor(color or Pixel.colors.text)
 if width then love.graphics.printf(t,x,y,width)else love.graphics.print(t,x,y)end
end
local function draw_net()
 local p=motion();local s=host.state();local editing=s.card_trial.phase=='face_edit' or p.edit~=nil
 local n=editing and (p.edit and p.edit.die or U.edit_die)or U.inspect_die or 1
 local focus=editing and (p.edit and p.edit.index or U.edit_index)or U.inspect_index or 1
 local body,menu=host.fonts()
 love.graphics.setColor(Pixel.colors.background);love.graphics.rectangle('fill',658,105,348,430)
 for j,role in ipairs(D.roles)do Pixel.option(j..' '..role,672+(j-1)*108,120,104,n==j,body)end
 local proposal=editing and not p.edit and D.catalogue[n][U.edit_type]or nil
 V.net(p,n,focus,proposal,host)
 if editing and not p.edit then
  local die=s.dice.dice[n]
  for j,face in ipairs(D.catalogue[n])do
   local count=0;for k,f in ipairs(die)do if (k==focus and proposal or f)==face then count=count+1 end end
   Pixel.option(D.faces[face].name..' '..count..'/6',672,426+(j-1)*18,320,j==U.edit_type,body)
  end
  local same=proposal==die[focus]
  Pixel.option(same and 'Choose a different face' or 'Enter Confirm replacement',672,486,320,not same,body,same)
  Pixel.option('K Skip',672,518,320,false,body)
 elseif p.edit then
  for j=1,3 do V.cube(p,j,704+(j-1)*112,488,28,host,false)end
 elseif not p.edit then
  local counts={};for _,face in ipairs(p.dice[n])do counts[face]=(counts[face]or 0)+1 end
  for j,face in ipairs(D.catalogue[n])do text(D.faces[face].name..' '..counts[face]..'/6',680,430+(j-1)*19,body)end
  text('f1-f6: physical faces · * current result',680,496,body)
  text('1/2/3 die · left/right face · I/Esc fold',680,516,body)
 end
end
function U.draw_sidebar()
 if not U.active()then return end
 local s=host.state();local body,menu,display=host.fonts();local p=motion()
 Pixel.frame('CUSTOM DICE',656,22,352,514,display)
 text('Fight '..s.card_trial.fight..'/2 · Seed '..U.seed,672,54,body)
 for n,id in ipairs({'rhythm_of_steel','detonation_bloom'})do
  host.visual('core:boon/'..id..'/icon',672+(n-1)*160,76,24,24)
  text(n==1 and 'Rhythm (U)'or 'Bloom (R)',700+(n-1)*160,82,body)
 end
 for n,r in ipairs(p.results)do
  local f=D.faces[r.face];local y=114+(n-1)*80
  V.cube(p,n,710,y+36,42,host,U.selected==n or U.hover==n)
  text(n..' '..D.roles[n],752,y,body,Pixel.colors.muted)
  text(f.name,752,y+18,menu,U.selected==n and Pixel.colors.selected or Pixel.colors.text)
  text(f.description,752,y+42,body,nil,240)
 end
 local n=U.hover or U.selected
 if n then local f=D.faces[p.results[n].face];text(f.name..' · base time '..f.cost,672,378,body);text('Aim at a cell, or press a direction.',672,398,body)end
 text('I Unfold dice & inspect build',672,480,menu)
 text('1 Footwork · F/2 Weapon · 3 Tactics',672,514,body)
 if p.net>0 or U.inspect or s.card_trial.phase=='face_edit'then draw_net()end
end
function U.draw_combat()
 if not U.active()or host.effects and not host.effects()then return end
 local hero=host.hero_screen_position and host.hero_screen_position()or G.player(host.state()).position
 V.accents(motion(),host,hero)
end
function U.draw_preview()
 if not U.active()then return false end
 if host.busy()or U.inspect or U.menu then return true end
 local body=host.fonts();local p=U.preview
 local cells=p and p.cells or {}
 if host.state().card_trial.phase~='combat'then return true end
 if not U.aim and (U.hover or U.selected)then
  cells={};for _,dir in pairs(dirs)do
   local q=D.plan(host.state(),intent(U.hover or U.selected,dir[1],dir[2]),G)
   if q.valid then for j,c in ipairs(q.cells)do if not q.distance or j<=q.distance then cells[#cells+1]=c end end end
  end
 end
 for j,c in ipairs(cells)do
  local x,y=host.screen(c.x,c.y)
  local legal=not p or p.valid and (not p.distance or j<=p.distance)
  love.graphics.setColor(legal and {.8,.8,.55,.8}or {.5,.5,.5,.5})
  love.graphics.rectangle('line',x+5,y+5,38,38)
 end
 local f=U.forecast
 if f and f.valid then
  local p=G.player(f.state);host.ghost(p.position.x,p.position.y)
  for _,e in ipairs(host.state().entities)do if e.faction=='enemy'then
   local after=G.entity(f.state,e.id)
   if after and host.entity_ghost and (after.position.x~=e.position.x or after.position.y~=e.position.y)then host.entity_ghost(after,after.position.x,after.position.y)end
   local x,y=host.screen(after and after.position.x or e.position.x,after and after.position.y or e.position.y)
   text(after and e.hp..' > '..after.hp or 'DEAD',x,y+30,body,Pixel.colors.selected)
  end end
  for _,e in ipairs(f.events)do if e.kind=='AttackPerformed'and e.source_id~=host.state().player_id then
   for _,c in ipairs(e.target_cells)do local x,y=host.screen(c.x,c.y);love.graphics.setColor(1,.3,.3,.7);love.graphics.rectangle('line',x+10,y+10,28,28)end
  end end
 end
 return true
end
function U.draw_hud()
 local s=host.state();local p=G.player(s);local body,menu=host.fonts();local f=U.forecast;local g=love.graphics
 g.setColor(Pixel.colors.background);g.rectangle('fill',16,544,992,90)
 text('HP '..p.hp..'/'..p.max_hp..' · Guard '..D.guard(s)..' · Time '..s.clock,28,550,menu)
 local line=host.busy()and 'Resolving · input locked'or s.card_trial.phase=='face_edit'and 'Replace one physical face. Left/right index; up/down replacement; 1/2/3 die. Esc cancel.'or 'Move: arrows/WASD · Q/E/Z/C aim diagonally · F Weapon · 1/2/3 select · Esc cancel/menu'
 text(line,28,580,body,nil,960)
 if f and f.valid then text('Forecast: HP '..G.player(f.state).hp..' · Guard '..D.guard(f.state)..' · Time +'..f.plan.cost..' · gold = legal, red = enemy response',28,602,body,Pixel.colors.selected)
 elseif U.message then text(U.message,28,602,body,Pixel.colors.selected,960)end
end
function U.draw_overlay()
 if not U.active()then return end
 local s=host.state();local phase=s.card_trial.phase
 if not U.menu and phase~='death'and phase~='victory'then return end
 local body,menu,display=host.fonts();Pixel.dim(.8);Pixel.frame(U.inspect and 'DICE & BUILD' or U.menu and 'TRIAL MENU'or 'TRIAL '..phase:upper(),96,74,832,536,display)
 text('Seed '..U.seed..' · Fight '..s.card_trial.fight..'/2',116,166,menu)
 text('R Replay same seed · T Title'..(U.menu and ' · Enter Resume'or ''),116,230,menu)
 text('M Reduced motion: '..(U.reduced and 'ON'or 'OFF'),116,288,menu)
 text('Trial progress is not saved.',116,340,body)
end

return U
