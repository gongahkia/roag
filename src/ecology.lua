-- Small, causal alien ecology. Physical matter and edible biomass are explicitly
-- accounted for. Unknown to the colony is not random or unknowable to the engine.
local W=require('src.world')
local M=require('src.materials')
local R=require('src.random')
local Cat=require('src.catalog')
local Signals=require('src.signals')
local Body=require('src.body')
local E={}
local adjacent={{-1,0},{1,0},{0,-1},{0,1}}
function E.clear(w,x,y,tx,ty)
 local steps=math.max(1,math.ceil(math.max(math.abs(tx-x),math.abs(ty-y))))
 for k=1,steps-1 do
  local xx=math.floor(x+(tx-x)*k/steps+0.5);local yy=math.floor(y+(ty-y)*k/steps+0.5)
  if W.solid(w,xx,yy) then return false end
 end
 return true
end
function E.warded(w,x,y)
 for _,s in pairs(w.structures) do
  if s.kind=='ward' and s.enabled and s.tank>0 and W.supportedStructure(w,s) and require('src.structures').wet(w,s)<4
   and math.abs(x-(s.gx*4-2))+math.abs(y-(s.gy*4-2))<=12 then return true end
 end
 return false
end
function E.observe(w,p,category,context)
 if require('src.knowledge').enabled(context) then return end
 local key=category..':'..p.kind
 if not w.content.observed[key] then
  w.content.observed[key]={tick=w.tick,category=category,kind=p.kind}
  W.event(w,'sighting','Unclassified '..(category=='fauna' and 'creature' or category=='flora' and 'growth' or 'artifact')..' sighted. Survey it to record field notes.',p.id)
 end
end
function E.floraDie(w,p,cause)
 if not p.alive then return end
 if p.food>0 then W.stack(w,'food',p.food,p.x,p.y) end
 if p.water>0 then W.stack(w,'water',p.water,p.x,p.y) end
 p.food=0;p.water=0;p.alive=false;p.deathTick=w.tick
 p.status=cause or 'Destroyed'
end
function E.kill(w,c,cause)
 if not c.alive then return end
 c.alive=false;c.hp=0;c.deathTick=w.tick;c.status=cause or 'Dead'
 if c.food>0 then W.stack(w,'food',c.food,c.x,c.y);c.food=0 end
 if c.observed then W.event(w,'creature','A '..Cat.fauna[c.kind].name..' died: '..c.status..'.',c.id) end
end
function E.hurt(w,c,amount,cause)
 if not c.alive then return end
 c.hp=math.max(0,c.hp-amount);c.awake=true
 if c.hp==0 then E.kill(w,c,cause) end
end
local function due(w,p,period) return (w.tick+math.floor(p.phase/20)*20)%period==0 end
local function plantRoom(w,x,y)
 if not W.inside(w,x,y) or x<=3 or x>=w.width-3 or y<5 or y>=w.height-3 then return false end
 if W.get(w,x,y)~=M.AIR or W.blocked(w,x,y) or not W.solid(w,x,y+1) then return false end
 for _,p in ipairs(w.content.flora) do if p.alive and math.abs(p.x-x)+math.abs(p.y-y)<3 then return false end end
 return true
end
function E.spawnFlora(w,kind,x,y,food,water)
 if #w.content.flora>=128 or not plantRoom(w,x,y) then return nil end
 local p={id=W.id(w),kind=kind,x=x,y=y,food=food or 0,water=water or 0,alive=true,phase=math.floor(R.hash(w.seed+775,x,y)*3600)}
 w.content.flora[#w.content.flora+1]=p;return p
end
function E.occupy(w,c,x,y)
 local d=Cat.fauna[c.kind]
 if x<3 or x+d.width-1>w.width-2 or y<4 or y-d.height+1<3 or y>w.height-3 then return false end
 for yy=y-d.height+1,y do for xx=x,x+d.width-1 do
  local m=W.get(w,xx,yy)
  if W.solid(w,xx,yy) or m==M.LAVA or m==M.STEAM then return false end
  if c.kind=='leech' then if m~=M.WATER then return false end
  elseif m==M.WATER then return false end
 end end
 return true
end
local function support(w,c,x,y)
 local d=Cat.fauna[c.kind]
 if c.kind=='leech' then return true end
 for xx=x,x+d.width-1 do if W.solid(w,xx,y+1) then return true end end
 return false
end
function E.spawn(w,kind,x,y,food)
 if #w.content.fauna>=64 then return nil end
 local c={id=w.nextId,kind=kind,x=x,y=y,homeX=x,homeY=y,food=food or 8,hp=Cat.fauna[kind].hp,
  alive=true,awake=kind~='sentinel',phase=math.floor(R.hash(w.seed+912,x,y)*3600),status='Wandering'}
 if not E.occupy(w,c,x,y) then return nil end
 c.id=W.id(w);w.content.fauna[#w.content.fauna+1]=c;return c
end
local function plants(w,context)
 local count=#w.content.flora
 for n=1,count do local p=w.content.flora[n]
  if p.alive then
   local m=W.get(w,p.x,p.y)
   if W.solid(w,p.x,p.y) or not W.solid(w,p.x,p.y+1) or m==M.LAVA then E.floraDie(w,p,'Buried, uprooted or burned')
   else
    if p.kind=='filter' then
     for _,d in ipairs(adjacent) do local x,y=p.x+d[1],p.y+d[2]
      if W.get(w,x,y)==M.STEAM and not W.blocked(w,x,y) then
       local Knowledge=require('src.knowledge');local source=Knowledge.enabled(context) and Knowledge.source(context.siteId,'flora',p) or nil
       local witnesses=source and Knowledge.witnesses(w,context,p,'flora',x,y) or nil
       if W.put(w,x,y,M.WATER) and source then Knowledge.creditEffect(context,witnesses,source,'filter_steam_to_water',x,y,M.STEAM,M.WATER) end
      end
     end
    elseif p.kind=='thorn' and due(w,p,240) then
     for _,d in ipairs(adjacent) do local x,y=p.x+d[1],p.y+d[2]
      if W.get(w,x,y)==M.SAND then
       local Knowledge=require('src.knowledge');local source=Knowledge.enabled(context) and Knowledge.source(context.siteId,'flora',p) or nil
       local witnesses=source and Knowledge.witnesses(w,context,p,'flora',x,y) or nil
       if W.put(w,x,y,M.ROCK) and source then Knowledge.creditEffect(context,witnesses,source,'thorn_sand_to_rock',x,y,M.SAND,M.ROCK) end
       break
      end
     end
    end
    if p.water<6 and due(w,p,120) then
     for _,d in ipairs(adjacent) do local x,y=p.x+d[1],p.y+d[2]
      if W.get(w,x,y)==M.WATER and not W.blocked(w,x,y) then W.put(w,x,y,M.AIR);p.water=p.water+1;break end
     end
    end
    if p.water>0 and p.food<12 and due(w,p,240) then
     p.water=p.water-1;p.food=p.food+1;w.ledger.waterUsed=w.ledger.waterUsed+1;w.ledger.foodGrown=w.ledger.foodGrown+1
    end
    if p.kind=='veil' and p.food>=4 and due(w,p,Cat.flora.veil.period) then
     p.pulseUntil=w.tick+30;Signals.emit(w,'bloom',p.x,p.y,8)
    end
    if p.food>=7 and p.water>0 and #w.content.flora<128 and due(w,p,960) then
     local dir=R.hash(w.seed+p.id,w.tick,1)<0.5 and -1 or 1
     for dy=-1,1 do
      local child=E.spawnFlora(w,p.kind,p.x+dir*3,p.y+dy,2,1)
      if child then p.food=p.food-2;p.water=p.water-1;break end
     end
    end
    p.status=p.water==0 and 'Dry' or p.food>=7 and 'Established' or 'Growing'
   end
  end
 end
end
local function harmWorker(w,a,amount,cause)
 a.hp=math.max(0,a.hp-amount);a.injuryCause=cause
end
local function exposure(w)
 for _,p in ipairs(w.content.flora) do if p.alive then
  local pulse=p.pulseUntil and p.pulseUntil>=w.tick
  if pulse or p.kind=='thorn' then
   for _,a in ipairs(w.workers) do if a.alive then
    local d=math.abs(p.x-a.x)+math.abs(p.y-(a.y-1))
    if ((pulse and d<=4) or (p.kind=='thorn' and d<=1)) and E.clear(w,p.x,p.y,a.x,a.y-1) and not E.warded(w,a.x,a.y) then
     harmWorker(w,a,pulse and 0.12 or 0.2,pulse and 'veil-bloom exposure' or 'iron-thorn contact')
     if pulse then a.breath=math.max(0,a.breath-0.4) end
    end
   end end
  end
 end end
end
local function target(w,c)
 local best,score
 local function offer(x,y,base)
  local dist=math.abs(x-c.x)+math.abs(y-c.y)
  if dist>45 then return end
  local v=base-dist
  if not score or v>score then best,score={x=x,y=y},v end
 end
 if c.kind=='grazer' then
  for _,p in ipairs(w.content.flora) do if p.alive and p.food>0 then offer(p.x,p.y,30) end end
  for slot=1,w.cols*w.rows do local s=w.structures[slot]
   if s and s.kind=='farm' and s.growth>=w.rules.cropTicks then offer(s.gx*4-2,s.gy*4,35) end
  end
 elseif c.kind=='stalker' then
  for _,other in ipairs(w.content.fauna) do if other.alive and other.kind=='grazer' then offer(other.x,other.y,36) end end
  for _,a in ipairs(w.workers) do if a.alive and math.abs(a.x-c.x)+math.abs(a.y-c.y)<=14 then offer(a.x,a.y,28) end end
  for _,signal in ipairs(w.content.signals) do if w.tick-signal.tick<=200 then offer(signal.x,signal.y,signal.strength+10) end end
 elseif c.kind=='sentinel' then
  for _,a in ipairs(w.workers) do if a.alive and math.abs(a.x-c.homeX)+math.abs(a.y-c.homeY)<=22 then offer(a.x,a.y,30) end end
 end
 return best
end
local function move(w,c)
 if not E.occupy(w,c,c.x,c.y) then
  E.hurt(w,c,1,'Habitat loss');return
 end
 if c.kind~='leech' and not support(w,c,c.x,c.y) then
  if E.occupy(w,c,c.x,c.y+1) then c.y=c.y+1;c.status='Falling' end
  return
 end
 if not c.awake then return end
 local t=target(w,c);local options={}
 local dirs=R.hash(w.seed+c.id,w.tick,8)<0.5 and {-1,1} or {1,-1}
 local function offer(x,y)
  if not E.occupy(w,c,x,y) then return end
  if c.kind=='sentinel' and math.abs(x-c.homeX)+math.abs(y-c.homeY)>24 then return end
  local score=t and -(math.abs(x-t.x)+math.abs(y-t.y)) or R.hash(w.seed+c.id,x,y+math.floor(w.tick/40))*4
  if c.kind~='grazer' and E.warded(w,x,y) then score=score-40 end
  options[#options+1]={x=x,y=y,score=score}
 end
 offer(c.x,c.y)
 if c.kind=='leech' then
  for _,d in ipairs(adjacent) do offer(c.x+d[1],c.y+d[2]) end
 else
  for _,dx in ipairs(dirs) do
   if E.occupy(w,c,c.x+dx,c.y) then
    if support(w,c,c.x+dx,c.y) then offer(c.x+dx,c.y)
    elseif E.occupy(w,c,c.x+dx,c.y+1) and support(w,c,c.x+dx,c.y+1) then offer(c.x+dx,c.y+1) end
   elseif E.occupy(w,c,c.x,c.y-1) and support(w,c,c.x+dx,c.y-1) then offer(c.x+dx,c.y-1) end
  end
 end
 local best
 for _,v in ipairs(options) do if not best or v.score>best.score then best=v end end
 if best then c.x,c.y=best.x,best.y;c.status=t and 'Following a stimulus' or 'Wandering' end
end
local function feedAndFight(w,c)
 if c.kind=='grazer' then
  for _,p in ipairs(w.content.flora) do
   if c.food<24 and p.alive and p.food>0 and math.abs(p.x-c.x)+math.abs(p.y-c.y)<=2 and E.clear(w,c.x,c.y,p.x,p.y) then p.food=p.food-1;c.food=c.food+1;c.status='Grazing';return end
  end
  for slot=1,w.cols*w.rows do local s=w.structures[slot]
   if c.food<24 and s and s.kind=='farm' and s.growth>=w.rules.cropTicks and math.abs(c.x-(s.gx*4-2))+math.abs(c.y-s.gy*4)<=3 then
    s.growth=0;c.food=math.min(24,c.food+1);w.ledger.foodGrown=w.ledger.foodGrown+1
    W.event(w,'ecology','A grazer consumed a mature crop.',c.id);return
   end
  end
 elseif c.kind=='leech' then
  if c.food<16 and w.tick%100==0 then
   for _,d in ipairs(adjacent) do local x,y=c.x+d[1],c.y+d[2]
    if W.get(w,x,y)==M.WATER then W.put(w,x,y,M.AIR);c.food=c.food+1;w.ledger.waterUsed=w.ledger.waterUsed+1;w.ledger.foodGrown=w.ledger.foodGrown+1;break end
   end
  end
 elseif c.kind=='stalker' then
  for _,prey in ipairs(w.content.fauna) do
   if prey.alive and prey.kind=='grazer' and math.abs(prey.x-c.x)+math.abs(prey.y-c.y)<=3 and E.clear(w,c.x,c.y,prey.x,prey.y) then
    if prey.hp<=8 then
     local n=math.min(prey.food,24-c.food);prey.food=prey.food-n;c.food=c.food+n
    end
    E.hurt(w,prey,8,'Predation');c.status='Hunting';return
   end
  end
 end
 if c.kind~='grazer' and c.awake and not E.warded(w,c.x,c.y) then
  for _,a in ipairs(w.workers) do if a.alive and math.abs(a.x-c.x)+math.abs((a.y-1)-c.y)<=3 and E.clear(w,c.x,c.y,a.x,a.y-1) then
   harmWorker(w,a,c.kind=='sentinel' and 8 or c.kind=='stalker' and 4 or 2,Cat.fauna[c.kind].name..' attack')
   E.hurt(w,c,5,'Settler self-defence');c.status='Attacking'
   if (w.tick+c.id)%200==0 then W.event(w,'attack',Cat.fauna[c.kind].name..' attacked '..a.name..'.',a.id) end
   break
  end end
 end
end
local function fauna(w,context)
 local count=#w.content.fauna
 for i=1,count do local c=w.content.fauna[i]
  if c.alive then
   for _,a in ipairs(w.workers) do if a.alive and math.abs(c.x-a.x)+math.abs(c.y-a.y)<=11 and E.clear(w,c.x,c.y,a.x,a.y-1) then
    c.observed=true;E.observe(w,c,'fauna',context);break
   end end
   if c.kind=='sentinel' and not c.awake then
    for _,s in ipairs(w.content.signals) do
     if math.abs(s.x-c.x)+math.abs(s.y-c.y)<=s.strength*2 and w.tick-s.tick<160 then c.awake=true;c.status='Awakened';break end
    end
   end
   if c.kind~='sentinel' and (w.tick+c.phase)%300==0 then
    if c.food>0 then c.food=c.food-1;w.ledger.foodEaten=w.ledger.foodEaten+1 else E.hurt(w,c,6,'Starvation') end
   end
   if c.alive and w.tick%20==0 then feedAndFight(w,c) end
   if c.alive and (w.tick+c.phase)%Cat.fauna[c.kind].period==0 then move(w,c) end
   if c.alive and c.kind~='sentinel' and c.food>=18 and w.tick%600==0 then
    local child=E.spawn(w,c.kind,c.x+2,c.y,8)
    if child then c.food=c.food-8 end
   end
  end
 end
end
local function sites(w,context)
 for _,s in ipairs(w.content.sites) do if s.alive then
  if s.kind=='resonator' then
   if not s.active then
    for _,v in ipairs(w.content.signals) do if v.kind~='resonance' and math.abs(v.x-s.x)+math.abs(v.y-s.y)<=v.strength*2 then s.active=true;break end end
   end
   if s.active and due(w,s,240) then Signals.emit(w,'resonance',s.x,s.y,18);s.pulseUntil=w.tick+30 end
  elseif s.kind=='vent' and due(w,s,180) then
   for yy=s.y-2,s.y+2 do for xx=s.x-2,s.x+2 do
    if W.get(w,xx,yy)==M.WATER and E.clear(w,s.x,s.y,xx,yy) then W.put(w,xx,yy,M.STEAM) end
   end end
   s.pulseUntil=w.tick+20
  end
  for _,a in ipairs(w.workers) do if a.alive and math.abs(s.x-a.x)+math.abs(s.y-a.y)<=10 and E.clear(w,s.x,s.y,a.x,a.y-1) then E.observe(w,s,'sites',context);break end end
 end end
end
function E.salvage(w,p,category)
 if category=='flora' then E.floraDie(w,p,'Harvested')
 elseif category=='sites' then
  if p.kind=='nursery' and (p.stock.food or 0)>=8 then
   local child=E.spawn(w,'grazer',p.x+2,p.y,6)
   if child then p.stock.food=p.stock.food-6 end
   local plant=E.spawnFlora(w,'veil',p.x-3,p.y,2,0)
   if plant then p.stock.food=p.stock.food-2 end
  end
  for _,kind in ipairs({'stone','soil','metal','food','water'}) do if (p.stock[kind] or 0)>0 then W.stack(w,kind,p.stock[kind],p.x,p.y);p.stock[kind]=0 end end
  p.alive=false;p.status='Salvaged';p.deathTick=w.tick
 end
end
local function passiveSightings(w,context)
 if not require('src.knowledge').enabled(context) then return end
 local Knowledge=require('src.knowledge');local people={}
 for _,worker in ipairs(w.workers) do if worker.alive then people[#people+1]=worker end end
 table.sort(people,function(a,b) return a.personId<b.personId end)
 for _,category in ipairs({'flora','fauna','sites'}) do
  local records={};for _,record in ipairs(w.content[category]) do if record.alive then records[#records+1]=record end end
  table.sort(records,function(a,b) return a.id<b.id end)
  for _,worker in ipairs(people) do for _,record in ipairs(records) do
  local eyeX,eyeY=Body.eye(w,worker)
   if (require('src.visibility').enabled(w) and require('src.visibility').visible(w,worker,record.x,record.y,context)) or (not require('src.visibility').enabled(w) and math.abs(record.x-worker.x)+math.abs(record.y-eyeY)<=Knowledge.sightRange and E.clear(w,worker.x,eyeY,record.x,record.y)) then Knowledge.sighting(context,worker,Knowledge.source(context.siteId,category,record)) end
  end end
 end
end
function E.step(w,context)
 if not w.content then return end
 passiveSightings(w,context)
 if w.tick%20==0 then
  Signals.prune(w);plants(w,context);sites(w,context)
  for _,p in ipairs(w.content.flora) do if p.alive then
   for _,a in ipairs(w.workers) do if a.alive and math.abs(p.x-a.x)+math.abs(p.y-a.y)<=10 and E.clear(w,p.x,p.y,a.x,a.y-1) then E.observe(w,p,'flora',context);break end end
  end end
 end
 exposure(w);fauna(w,context)
 if w.tick%400==0 then
  for _,key in ipairs({'flora','fauna'}) do
   local keep={};for _,p in ipairs(w.content[key]) do if p.alive or w.tick-(p.deathTick or w.tick)<200 then keep[#keep+1]=p end end
   w.content[key]=keep
  end
 end
end
return E
