local W=require('src.world')
local U=require('src.util')
local N=require('src.nav')
local M=require('src.materials')
local J=require('src.jobs')
local C=require('config')
local A={}
function A.kill(w,a,reason)
 if not a.alive then return end
 J.release(w,a,true);a.alive=false;a.hp=0;a.status='Dead';a.reason=reason;a.deathTick=w.tick
 W.event(w,'death',a.name..' died: '..reason..'. No replacement will arrive.',a.id)
end
function A.step(w,context)
 for _,a in ipairs(w.workers) do if a.alive then
  a.hunger=math.min(100,a.hunger+w.rules.hungerRate)
  local sleeping=a.task and a.task.kind=='rest' and not a.task.path[a.task.next]
  if sleeping then
   a.fatigue=math.max(0,a.fatigue-(a.task.slot and 0.12 or 0.035))
   -- Slow recovery only while fed and resting in a bed; death is never reversed.
   if a.task.slot and a.hunger<50 and a.hp>0 then a.hp=math.min(100,a.hp+0.003) end
  else a.fatigue=math.min(100,a.fatigue+w.rules.fatigueRate*(a.worked and 1.4 or 1)) end
  local headWet=W.get(w,a.x,a.y-2)==M.WATER or W.get(w,a.x+1,a.y-2)==M.WATER
  local hot,steam=false,false
  for y=a.y-2,a.y do for x=a.x,a.x+1 do
   local m=W.get(w,x,y); if m==M.LAVA then hot=true elseif m==M.STEAM then steam=true end
  end end
  local reason=a.injuryCause
  local function hurt(amount,cause)
   local before=a.hp;a.hp=a.hp-amount
   if before>0 and a.hp<=0 then reason=cause end
  end
  if hot then hurt(2,'lava burns') end
  if steam then hurt(0.10,'steam scalding') end
  if headWet then
   a.breath=math.max(0,a.breath-0.8)
   if a.breath==0 then hurt(0.6,'drowning') end
  else a.breath=math.min(100,a.breath+1.6) end
  if not N.occupy(w,a.x,a.y,false) then hurt(0.45,'buried in solid material') end
  if a.hunger>=100 then hurt(0.06,'starvation') end
  if a.hp<=0 then A.kill(w,a,reason or 'injuries')
  else
   if not N.support(w,a.x,a.y) and N.occupy(w,a.x,a.y+1,false) then
    if a.task then J.release(w,a,true) end
    a.y=a.y+1;a.fall=a.fall+1;a.status='Falling';a.thinkAt=w.tick+1
   else
    if a.fall>0 then
     local damage=math.max(0,a.fall-5)*3
     if damage>0 then a.hp=a.hp-damage;W.event(w,'injury',a.name..' fell '..a.fall..' cells and took '..damage..' damage.',a.id) end
     a.fall=0
    end
    if a.hp<=0 then A.kill(w,a,'fall injuries')
    else
     local unsafe=not N.occupy(w,a.x,a.y,true)
     if a.evacuate and w.tick<=a.evacuate.untilTick and a.task and a.task.kind~='escape' then J.release(w,a,true);a.thinkAt=w.tick end
     if a.task and ((unsafe and a.task.kind~='escape') or (a.hunger>=82 and a.task.kind~='eat' and w.tick%100==a.id%100 and J.canEat(w,a))
        or (a.fatigue>=95 and a.task.kind~='rest' and a.task.kind~='eat')) then
      J.release(w,a,true);a.thinkAt=w.tick
     end
     if not a.task and w.tick>=a.thinkAt then J.plan(w,a,context);a.thinkAt=w.tick+w.rules.planEvery end
     local interval=w.rules.moveEvery+(a.hp<45 and 1 or 0)+(a.fatigue>85 and 1 or 0)
     if w.tick%interval==0 then J.act(w,a,context) end
    end
   end
  end
 end end
 if not w.extinct and W.alive(w)==0 then
  w.extinct=true;w.extinctionTick=w.tick
  W.event(w,'extinction','The settlement is extinct. Its material world continues; its losses stand.')
 end
end
return A
