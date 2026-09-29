-- LÖVE 11.x port of the original terminal game. Coordinates use the original
-- world orientation: W is north and increases y in game space.
local W,H,TILE=160,80,16
local stages={{terrain="forest",targets=4,enemies=6,score=5,ammo=2,vision=6,torches=3,wilds=true},{terrain="cave",targets=3,enemies=0,score=4,ammo=1,vision=5,torches=3},{terrain="dungeon",targets=1,enemies=2,score=5,ammo=2,vision=4,torches=3,cultists=true}}
local classes={{id="vanguard",name="VANGUARD",desc="TOUGH AND WELL-ARMED",m={health=2,bombs=1,flares=-1}},{id="gunslinger",name="GUNSLINGER",desc="EXTRA AMMO, LESS HEALTH",m={health=-1,ammo=3}},{id="scout",name="SCOUT",desc="BETTER SIGHT, FLARES, AND DASH",m={vision=2,flares=1,dash=-1}}}
local boons={{id="iron",name="IRON HEART",desc="BEGIN EACH DESCENT WITH MORE HEALTH",m={health=1}},{id="quiver",name="FULL QUIVER",desc="BEGIN EACH DESCENT WITH EXTRA AMMO",m={ammo=2}},{id="demo",name="DEMOLITION KIT",desc="BEGIN EACH DESCENT WITH AN EXTRA BOMB",m={bombs=1}},{id="flare",name="FLARE SATCHEL",desc="BEGIN EACH DESCENT WITH TWO EXTRA FLARES",m={flares=2}},{id="eye",name="EAGLE EYE",desc="SEE FURTHER THROUGH THE DARKNESS",m={vision=2}},{id="wind",name="WINDWALKER",desc="YOUR DASH RECHARGES MORE QUICKLY",m={dash=-1}}}
local curses={{name="FRAIL BODY",desc="BEGIN WITH ONE HEALTH",m={health=1}},{name="DARKNESS",desc="REDUCED NATURAL VISION",m={vision=-3}},{name="HUNTED",desc="ONE EXTRA NECROMANCER",m={enemies=1}},{name="RELENTLESS",desc="TWO EXTRA NECROMANCERS",m={enemies=2}},{name="EMPTY CHAMBER",desc="START WITH LESS AMMO",m={ammo=-1}},{name="SPENT BOMBS",desc="START WITHOUT BOMBS",m={bombs=-99}},{name="LONG HUNT",desc="MORE TARGETS MUST FALL",m={score=2}},{name="BLACKOUT",desc="NO TORCHES OR NATURAL LIGHT",m={vision=-99,torches=-99}},{name="GUTTERING TORCHES",desc="TORCHES BARELY REACH",m={torchRadius=1}},{name="SLOW DASH",desc="DASH RECHARGES SLOWLY",m={dash=6}},{name="SPENT FLARES",desc="START WITHOUT FLARES",m={flares=-99}},{name="SMALL BLAST",desc="BOMBS HAVE LESS RANGE",m={bombRadius=1}},{name="SHORT FUSE",desc="BOMBS DETONATE QUICKLY",m={bombFuse=1}},{name="RUSTED BARREL",desc="BULLETS FADE EARLY",m={bulletRange=3}},{name="DRY RELOAD",desc="KILLS RESTORE LESS AMMO",m={reloadPenalty=1}}}
local G={screen="title",choice=1,stage=1,total=0,log={},explored={},effects={},shopChoice=1}
local function key(x,y)return x..":"..y end
local function copy(t)local n={} for k,v in pairs(t)do n[k]=v end return n end
local function clamp(n,a,b)return math.max(a,math.min(b,n))end
local function play(s)if G.sounds[s]then G.sounds[s]:stop();G.sounds[s]:play()end end
local function msg(s)table.insert(G.log,1,s)while #G.log>5 do table.remove(G.log)end end
local function mget(m,k,d)return m[k] or d or 0 end
local function dist(a,b)return math.abs(a.x-b.x)+math.abs(a.y-b.y)end
local function pos(x,y)return{x=x,y=y}end
local function inSpace(x,y)return G.space[key(x,y)]end
local function shuffled(t)local n={}for _,v in ipairs(t)do n[#n+1]=v end for i=#n,2,-1 do local j=love.math.random(i);n[i],n[j]=n[j],n[i]end return n end
local function modify(s,m)
 for k,v in pairs(m)do s[k]=(s[k] or 0)+v end
 s.health=clamp(s.health,1,5);s.ammo=math.max(0,s.ammo);s.bombs=math.max(0,s.bombs);s.flares=math.max(0,s.flares);s.vision=math.max(1,s.vision);s.enemies=math.max(0,s.enemies);s.torches=math.max(0,s.torches);s.score=math.max(1,s.score);s.dash=math.max(1,s.dash)
end
local function carve(space,x,y,w,h)for a=x,x+w-1 do for b=y,y+h-1 do if a>0 and a<W-1 and b>0 and b<H-1 then space[key(a,b)]=true end end end end
local function corridor(space,a,b)local x,y=a.x,a.y while x~=b.x do space[key(x,y)]=true;x=x+(b.x>x and 1 or -1)end while y~=b.y do space[key(x,y)]=true;y=y+(b.y>y and 1 or -1)end end
local function generate(terrain,start,arena)
 local s={};G.landmarks={};if arena then carve(s,2,2,37,15);return s end
 if terrain=="dungeon"then local rooms={}for i=1,14 do local rw,rh=love.math.random(5,11),love.math.random(4,8);local r=pos(love.math.random(1,W-rw-2),love.math.random(1,H-rh-2));carve(s,r.x,r.y,rw,rh);local c=pos(r.x+math.floor(rw/2),r.y+math.floor(rh/2));if #rooms>0 then corridor(s,rooms[#rooms],c)end;rooms[#rooms+1]=c end;carve(s,start.x-3,start.y-3,7,7);corridor(s,start,rooms[1]);return s end
 local chance=terrain=="forest" and .25 or .43
 for x=1,W-2 do for y=1,H-2 do if love.math.random()>chance then s[key(x,y)]=true end end end
 -- Cellular smoothing retains the original forest/cave character.
 for z=1,(terrain=="forest" and 2 or 5)do local n={}for x=1,W-2 do for y=1,H-2 do local c=0 for dx=-1,1 do for dy=-1,1 do if(dx~=0 or dy~=0)and not s[key(x+dx,y+dy)]then c=c+1 end end end;if c<5 then n[key(x,y)]=true end end end;s=n end
 carve(s,start.x-3,start.y-3,7,7);return s
end
local function open(away)
 local cells={}for k in pairs(G.space)do local x,y=k:match("(%d+):(%d+)");x,y=tonumber(x),tonumber(y);local p=pos(x,y);if(not away or dist(p,G.player)>=away)and not G.occupied[key(x,y)]then cells[#cells+1]=p end end
 return cells[love.math.random(#cells)]
end
local function occupy()G.occupied={[key(G.player.x,G.player.y)]=true};for _,list in ipairs{G.targets,G.enemies,G.bullets,G.bombs,G.flares,G.torches}do for _,e in ipairs(list)do G.occupied[key(e.x,e.y)]=true end end;if G.ammo then G.occupied[key(G.ammo.x,G.ammo.y)]=true end end
local function entity(kind,p,extra)local e={kind=kind,x=p.x,y=p.y};if extra then for k,v in pairs(extra)do e[k]=v end end;return e end
local function populate()
 G.targets,G.enemies,G.bullets,G.bombs,G.flares,G.torches={},{},{},{},{},{},{};occupy()
 for i=1,G.settings.torches do local p=open();G.torches[#G.torches+1]=entity("torch",p,{radius=G.settings.torchRadius});occupy()end
 for i=1,G.settings.targets do local p=open(G.settings.terrain=="forest" and 0 or nil);G.targets[#G.targets+1]=entity("target",p);occupy()end
 for i=1,G.settings.enemies do local kind=G.settings.wilds and(i%3==0 and"bomber"or"wolf")or(G.settings.cultists and"cultist"or"necromancer");G.enemies[#G.enemies+1]=entity(kind,open(),{health=1,stun=0,attack=0});occupy()end
 G.ammo=entity("ammo",open())
end
local function stageStart()
 local base=copy(stages[G.stage]);base.health=2;base.bombs=1;base.flares=1;base.dash=3;base.bombRadius=2;base.bombFuse=3;base.torchRadius=4;base.reloadPenalty=0
 if G.curse then
  -- The original curse rules set a frail body's starting health, whereas
  -- class and boon health modifiers are additive afterwards.
  if G.curse.m.health then base.health=G.curse.m.health end
  local cm=copy(G.curse.m);cm.health=nil;modify(base,cm)
 end;modify(base,G.class.m);modify(base,G.boon.m);G.settings=base
 G.player={x=80,y=40,dir="w",health=base.health,ammo=base.ammo,bombs=base.bombs,flares=base.flares,dashCD=0,score=0,dead=false};G.space=generate(base.terrain,G.player);G.explored={};G.effects={};G.log={};populate();G.exit=nil;G.screen="game";msg("Descend into the "..base.terrain..".")
end
local function destroy(list,i,amount)local e=list[i];table.remove(list,i);G.player.score=G.player.score+1;G.player.ammo=G.player.ammo+math.max(0,(amount or 1)-G.settings.reloadPenalty);play("hit")end
local function hitPlayer(reason)G.player.health=G.player.health-1;play("hurt");msg(reason or "You were hit.");if G.player.health<=0 then G.player.dead=true;G.screen="gameover"end end
local function blast(b)
 local cells={}for x=b.x-b.radius,b.x+b.radius do for y=b.y-b.radius,b.y+b.radius do if inSpace(x,y)then cells[key(x,y)]=true end end end;return cells
end
local function updateProjectiles()
 local keep={}for _,b in ipairs(G.bullets)do
  if b.first then b.first=false else local dx,dy=({w=0,a=-1,s=0,d=1})[b.dir],({w=1,a=0,s=-1,d=0})[b.dir];b.x,b.y=b.x+dx,b.y+dy;b.travel=b.travel+1 end
  local hit=false;if not inSpace(b.x,b.y)or(b.max and b.travel>=b.max)then hit=true end
  for i=#G.targets,1,-1 do if G.targets[i].x==b.x and G.targets[i].y==b.y then destroy(G.targets,i,1);hit=true end end
  for i=#G.enemies,1,-1 do local e=G.enemies[i];if e.x==b.x and e.y==b.y then e.health=e.health-1;if e.health<=0 then destroy(G.enemies,i,2)end;hit=true end end
  if G.boss and b.x>=G.boss.x and b.x<G.boss.x+6 and b.y==G.boss.y then G.boss.health=G.boss.health-1;play("hit");hit=true end
  if not hit then keep[#keep+1]=b end
 end;G.bullets=keep
 for i=#G.bombs,1,-1 do local b=G.bombs[i];b.fuse=b.fuse-1;if b.fuse<=0 then local cells=blast(b);G.effects=cells;table.remove(G.bombs,i);play("boom");if cells[key(G.player.x,G.player.y)]then hitPlayer("You were caught in the blast.")end;for n=#G.targets,1,-1 do if cells[key(G.targets[n].x,G.targets[n].y)]then destroy(G.targets,n,1)end end;for n=#G.enemies,1,-1 do if cells[key(G.enemies[n].x,G.enemies[n].y)]then table.remove(G.enemies,n);G.player.score=G.player.score+1 end end;if G.boss and cells[key(G.boss.x,G.boss.y)]then G.boss.health=G.boss.health-2 end end end
 for i=#G.flares,1,-1 do local f=G.flares[i];f.fuse=f.fuse-1;if f.fuse<=0 then local cells=blast(f);G.effects=cells;table.remove(G.flares,i);play("flare");for _,e in ipairs(G.enemies)do if cells[key(e.x,e.y)]then e.stun=2;e.attack=0 end end end end
end
local function enemyTurn()
 for i=#G.enemies,1,-1 do local e=G.enemies[i];if e.stun>0 then e.stun=e.stun-1 else
  if e.kind=="wolf" and dist(e,G.player)<=1 then hitPlayer("A wolf pounced.")
  elseif e.kind=="bomber" and dist(e,G.player)<=1 then hitPlayer("A bomber detonated.");table.remove(G.enemies,i)
  elseif e.attack==4 then if math.abs(G.player.x-e.tx)<=1 and math.abs(G.player.y-e.ty)<=1 then hitPlayer("A spell struck.")end;e.attack=0
  elseif e.attack>0 then e.attack=e.attack+1
  else local dx=G.player.x-e.x;local dy=G.player.y-e.y;if math.abs(dx)+math.abs(dy)<8 then e.tx,e.ty=G.player.x,G.player.y;e.attack=1 else local sx=dx==0 and 0 or(dx>0 and 1 or-1);local sy=dy==0 and 0 or(dy>0 and 1 or-1);if math.abs(dx)>math.abs(dy)then if inSpace(e.x+sx,e.y)then e.x=e.x+sx end else if inSpace(e.x,e.y+sy)then e.y=e.y+sy end end end end
 end end
end
local function bossTurn()
 local b=G.boss;b.attack=b.attack+1;if b.attack>4 then b.attack=0;b.mode=shuffled({"cross","diag","pulse"})[1];b.ax=love.math.random(3,35);b.ay=love.math.random(3,15)end
 local danger=false;if b.attack>0 then if b.mode=="cross"then danger=G.player.x==b.ax or G.player.y==b.ay elseif b.mode=="diag"then danger=math.abs(G.player.x-b.ax)==math.abs(G.player.y-b.ay) else danger=dist(G.player,{x=b.ax,y=b.ay})<=3 end;if b.attack==4 and danger then hitPlayer("The boss's "..b.mode.." attack hit.")end end
 if b.attack==0 and #G.enemies<(b.health<4 and 3 or b.health<8 and 2 or 1)then occupy();G.enemies[#G.enemies+1]=entity("necromancer",open(6),{health=1,attack=0,stun=0});msg("The boss summoned a necromancer.")end
end
local function finishTurn()
 updateProjectiles();if G.screen~="game"then return end
 if G.boss then bossTurn();if G.boss.health<=0 then G.screen="victory";play("door");return end else enemyTurn()end
 if #G.targets<G.settings.targets then occupy();while #G.targets<G.settings.targets do G.targets[#G.targets+1]=entity("target",open())end;while #G.enemies<G.settings.enemies do G.enemies[#G.enemies+1]=entity(G.settings.wilds and"wolf"or"necromancer",open(),{health=1,attack=0,stun=0})end end
 if G.player.score>=G.settings.score and not G.exit then occupy();G.exit=entity("door",open(6));msg("All targets are down. Find the exit.")end
 if G.exit and G.player.x==G.exit.x and G.player.y==G.exit.y then G.total=G.total+G.player.score;if G.stage<3 then G.stage=G.stage+1;G.curseOptions={};for i,c in ipairs(shuffled(curses))do if i<=3 then G.curseOptions[#G.curseOptions+1]=c end end;G.screen="curse";G.choice=1 else G.screen="shop";G.shopChoice=1 end end
end
local function act(k)
 local p=G.player;G.effects={};p.dashCD=math.max(0,p.dashCD-1)
 if k=="q" then if p.dashCD>0 then msg("Dash is recharging.")else local dx,dy=({w=0,a=-1,s=0,d=1})[p.dir],({w=1,a=0,s=-1,d=0})[p.dir];for z=1,2 do if inSpace(p.x+dx,p.y+dy)then p.x,p.y=p.x+dx,p.y+dy end end;p.dashCD=G.settings.dash;play("step")end
 elseif k=="e"then if p.ammo>0 then p.ammo=p.ammo-1;G.bullets[#G.bullets+1]=entity("bullet",p,{dir=p.dir,first=true,travel=1,max=G.settings.bulletRange});play("shoot")else msg("No more ammo.")end
 elseif k=="b"then if p.bombs>0 then p.bombs=p.bombs-1;G.bombs[#G.bombs+1]=entity("bomb",p,{fuse=G.settings.bombFuse,radius=G.settings.bombRadius});play("select")else msg("No bombs left.")end
 elseif k=="f"then if p.flares>0 then p.flares=p.flares-1;G.flares[#G.flares+1]=entity("flare",p,{fuse=2,radius=1});play("flare")else msg("No flares left.")end
 else local dx,dy=({w=0,a=-1,s=0,d=1})[k],({w=1,a=0,s=-1,d=0})[k];p.dir=k;if inSpace(p.x+dx,p.y+dy)then p.x,p.y=p.x+dx,p.y+dy;play("step")else msg("A wall blocks your path.")end end
 if G.ammo and p.x==G.ammo.x and p.y==G.ammo.y then p.ammo=p.ammo+1;G.ammo=nil;play("pickup");msg("Ammo collected.")end;finishTurn()
end

-- Verified against a labelled extraction of Kenney's 1-Bit Pack sheet.
-- Each role maps to a real 16×16 Kenney tile rather than an approximate glyph.
local sprite={player={25,1},target={38,3},ammo={23,5},torch={20,7},door={22,1},bullet={34,3},bomb={38,6},flare={23,6},necromancer={27,10},wolf={31,9},bomber={20,9},cultist={28,10},boss={30,2}}
local function quad(kind)return G.quads[(sprite[kind]or sprite.target)[1]..":"..(sprite[kind]or sprite.target)[2]]end
local function visible(x,y)
 if G.boss then return true end
 if dist(G.player,{x=x,y=y})<=G.settings.vision then return true end
 for _,t in ipairs(G.torches)do if math.abs(t.x-x)+math.abs(t.y-y)<=t.radius then return true end end
 for _,f in ipairs(G.flares)do if math.abs(f.x-x)+math.abs(f.y-y)<=3 then return true end end
 return G.effects[key(x,y)]~=nil
end
local function color(c)love.graphics.setColor(c[1],c[2],c[3],c[4]or 1)end
local function drawSprite(kind,x,y,scale,tint)
 if tint then color(tint)else love.graphics.setColor(1,1,1)end
 love.graphics.draw(G.sheet,quad(kind),x,y,0,scale,scale);love.graphics.setColor(1,1,1)
end
local function drawActor(e,x,y,size)
 drawSprite(e.kind,x,y,size/16,e.stun and e.stun>0 and {1,.85,.2}or nil)
 if e.kind=="player" then
  color({.15,.9,1,.9});love.graphics.rectangle("line",x,y,size,size);love.graphics.setColor(1,1,1)
 end
end
local function camera()
 -- Reserve a fixed right sidebar and bottom event log at every window size.
 -- This prevents a narrow display from clipping the HUD or the world board.
 local sw,sh=love.graphics.getDimensions();local margin,sidebar,gap=20,270,20
 local size=math.max(12,math.min(32,math.floor(math.min((sw-sidebar-gap-margin*2)/27,(sh-130)/19))))
 return size,margin,margin,13,9
end
local function text(s,x,y,scale,tint)
 -- Allow the convenient text(message, x, y, colour) form as well as an
 -- explicit scale; LÖVE's print expects its scale argument to be numeric.
 if type(scale)=="table" then tint,scale=scale,1 end
 color(tint or {1,1,1});love.graphics.print(s,x,y,0,scale or 1);love.graphics.setColor(1,1,1)
end
local function drawGame()
 local size,ox,oy,rx,ry=camera();local p=G.player
 love.graphics.clear(.025,.035,.055);love.graphics.setColor(.08,.1,.14);love.graphics.rectangle("fill",ox-4,oy-4,27*size+8,19*size+8)
 for sx=0,26 do for sy=0,18 do
  local x,y=p.x+sx-rx,p.y+sy-ry;local seen=visible(x,y);if seen then G.explored[key(x,y)]=true end
  if G.explored[key(x,y)]then
   local px,py=ox+sx*size,oy+(18-sy)*size
   local floor=inSpace(x,y)
   if floor then
    local palette={forest={.09,.19,.13},cave={.12,.14,.18},dungeon={.16,.12,.18},arena={.14,.12,.17}}
    local c=palette[G.settings.terrain] or palette.arena;color(seen and c or {.035,.045,.06});love.graphics.rectangle("fill",px,py,size,size)
    if seen and (x*7+y*11)%5==0 then color({c[1]*1.55,c[2]*1.55,c[3]*1.55});love.graphics.rectangle("fill",px+size*.35,py+size*.35,math.max(1,size*.12),math.max(1,size*.12))end
   else
    color(seen and {.11,.075,.13}or {.025,.028,.04});love.graphics.rectangle("fill",px,py,size,size);color(seen and {.22,.13,.22}or {.045,.05,.07});love.graphics.rectangle("line",px,py,size,size)
   end
  end
 end end
 local function ent(e)if e and visible(e.x,e.y)then local sx,sy=e.x-p.x+rx,e.y-p.y+ry;if sx>=0 and sx<=26 and sy>=0 and sy<=18 then drawActor(e,ox+sx*size,oy+(18-sy)*size,size)end end end
 if G.boss then for x=G.boss.x,G.boss.x+5 do ent(entity("boss",{x=x,y=G.boss.y}))end end
 for _,list in ipairs{G.torches,G.targets,G.enemies,G.bullets,G.bombs,G.flares}do for _,e in ipairs(list)do ent(e)end end;ent(G.ammo);ent(G.exit)
 for k in pairs(G.effects)do local x,y=k:match("(%d+):(%d+)");ent(entity("flare",{x=tonumber(x),y=tonumber(y)}))end
 ent(entity("player",p));local px=ox+13*size;local py=oy+9*size;love.graphics.setColor(.2,.9,1,.25);love.graphics.rectangle("line",px,py,size,size);love.graphics.setColor(1,1,1)
 local sx=ox+27*size+24;text("ROAG",sx,oy,2,{.7,.9,1});text("HP  "..string.rep("♥",p.health),sx,oy+45,1.25,{1,.35,.35});text("AMMO  "..p.ammo,sx,oy+72);text("BOMBS "..p.bombs,sx,oy+94);text("FLARES "..p.flares,sx,oy+116);text("DASH  "..(p.dashCD==0 and "READY"or p.dashCD),sx,oy+138);text("SCORE "..p.score.." / "..(G.boss and "BOSS"or G.settings.score),sx,oy+170,{.9,.85,.3})
 if G.curse then text("CURSE: "..G.curse.name,sx,oy+200,1,{.9,.4,.8})end
 text("CONTROLS",sx,oy+250,1,{.6,.8,1});text("WASD  move / face",sx,oy+272);text("E shoot   Q dash",sx,oy+292);text("B bomb    F flare",sx,oy+312)
 if #G.enemies>0 then text("INTENTS",sx,oy+350,1,{.9,.7,.4});for i,e in ipairs(G.enemies)do if i>5 then break end;local intent=e.stun>0 and"STUNNED"or e.attack>0 and("SPELL "..(5-e.attack))or(e.kind=="wolf"and"POUNCE"or e.kind=="bomber"and"DETONATE"or"ADVANCE");text(string.upper(e.kind)..": "..intent,sx,oy+370+i*18,.8)end end
 for i,s in ipairs(G.log)do text(s,ox,oy+19*size+12+(i-1)*18,.85,{.8,.85,.9})end
end
local function menu(title,items,footer)
 love.graphics.clear(.025,.035,.055);local sw,sh=love.graphics.getDimensions();text(title,sw/2-#title*8,100,2,{.7,.9,1});for i,it in ipairs(items)do local y=190+(i-1)*70;local sel=i==G.choice;color(sel and {.13,.22,.3}or {.06,.08,.12});love.graphics.rectangle("fill",sw*.2,y,sw*.6,56);text((sel and "> "or"  ")..it.name,sw*.23,y+7,1.3,sel and {.95,.85,.3}or {1,1,1});text(it.desc or "",sw*.23,y+31,.85,{.7,.75,.8})end;text(footer or "W/S SELECT     ENTER CONFIRM",sw*.5-145,sh-60,1,{.65,.75,.9})
end
local function shop()
 local items={{name="HEALTH +1",desc="Cost: 2 score",key="health",cost=2},{name="AMMO +2",desc="Cost: 1 score",key="ammo",cost=1},{name="BOMB +1",desc="Cost: 2 score",key="bombs",cost=2},{name="FLARE +1",desc="Cost: 1 score",key="flares",cost=1},{name="CHALLENGE THE BOSS",desc="Continue to final arena",key="go",cost=0}};G.choice=G.shopChoice;menu("PRE-BOSS SHOP  •  SCORE "..G.total,items,"W/S SELECT     ENTER BUY / CONTINUE");G.choice=G.shopChoice
end
local function startBoss()
 G.player.x,G.player.y=20,9;G.player.dir="w";G.player.health=math.max(1,G.player.health);G.player.bombs=math.max(1,G.player.bombs);G.player.flares=math.max(1,G.player.flares);G.space=generate("arena",G.player,true);G.targets,G.enemies,G.bullets,G.bombs,G.flares,G.torches={},{},{},{},{},{},{};G.ammo=entity("ammo",pos(10,9));G.boss={x=14,y=1,health=10,attack=0,mode="cross",ax=20,ay=9};G.settings.vision=99;G.explored={};G.screen="game";msg("The boss awaits.")
end
function love.load()
 love.graphics.setDefaultFilter("nearest","nearest");G.font=love.graphics.newFont("assets/fonts/BigBlueTermPlusNerdFontMono-Regular.ttf",16);love.graphics.setFont(G.font);G.sheet=love.graphics.newImage("assets/kenney/Tilesheet/colored_packed.png");G.quads={};for x=1,49 do for y=1,22 do G.quads[x..":"..y]=love.graphics.newQuad((x-1)*16,(y-1)*16,16,16,G.sheet)end end
 G.sounds={};local sounds={step="footstep00",shoot="drawKnife1",hit="chop",boom="metalPot3",flare="metalClick",hurt="knifeSlice",door="doorOpen_1",pickup="handleCoins",select="bookFlip2"};for n,file in pairs(sounds)do local ok,s=pcall(love.audio.newSource,"assets/sounds/OGG/"..file..".ogg","static");if ok then G.sounds[n]=s end end
end
function love.draw()if G.screen=="game"then drawGame()elseif G.screen=="title"then love.graphics.clear(.025,.035,.055);local sw,sh=love.graphics.getDimensions();text("ROAG",sw/2-100,sh/2-100,4,{.7,.9,1});text("A ONE-BIT DESCENT",sw/2-110,sh/2-35,1.2,{.7,.75,.85});text("Press ENTER to begin",sw/2-105,sh/2+55,1,{.95,.85,.3})elseif G.screen=="class"then menu("CHOOSE YOUR CLASS",classes)elseif G.screen=="boon"then menu("CHOOSE A BOON",boons)elseif G.screen=="curse"then menu("CHOOSE A CURSE",G.curseOptions,"W/S SELECT     ENTER ACCEPT BURDEN")elseif G.screen=="shop"then shop()elseif G.screen=="gameover"then menu("YOU DIED",{{name="RETURN TO TITLE",desc="Press Enter to start a new descent."}},"")elseif G.screen=="victory"then menu("YOU HAVE WON",{{name="THE DESCENT IS OVER",desc="Press Enter to return to the title."}},"")end end
function love.keypressed(k)
 if k=="escape"then love.event.quit()end
 if G.screen=="title"and(k=="return"or k=="space")then G.screen="class";G.choice=1;play("select");return end
 if G.screen=="class"or G.screen=="boon"or G.screen=="curse"then local options=G.screen=="class"and classes or G.screen=="boon"and boons or G.curseOptions;if k=="w"or k=="up"then G.choice=math.max(1,G.choice-1);play("select")elseif k=="s"or k=="down"then G.choice=math.min(#options,G.choice+1);play("select")elseif k=="return"or k=="e"then if G.screen=="class"then G.class=options[G.choice];G.screen="boon"elseif G.screen=="boon"then G.boon=options[G.choice];G.stage=1;G.curse=nil;stageStart()else G.curse=options[G.choice];stageStart()end;play("select")end;return end
 if G.screen=="shop"then local items=5;if k=="w"or k=="up"then G.shopChoice=math.max(1,G.shopChoice-1)elseif k=="s"or k=="down"then G.shopChoice=math.min(items,G.shopChoice+1)elseif k=="return"or k=="e"then local u=({{key="health",cost=2},{key="ammo",cost=1},{key="bombs",cost=2},{key="flares",cost=1},{key="go"}})[G.shopChoice];if u.key=="go"then startBoss()elseif G.total>=u.cost then G.total=G.total-u.cost;G.player[u.key]=G.player[u.key]+(u.key=="ammo"and 2 or 1);play("pickup")else msg("Not enough score.")end end;return end
 if G.screen=="gameover"or G.screen=="victory"then if k=="return"then G.screen="title"end;return end
 if G.screen=="game"and(k=="w"or k=="a"or k=="s"or k=="d"or k=="e"or k=="b"or k=="f"or k=="q")then act(k)end
end
