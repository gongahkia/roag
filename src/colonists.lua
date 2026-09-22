local W=require('src.world')
local U=require('src.util')
local N=require('src.nav')
local M=require('src.materials')
local J=require('src.jobs')
local C=require('config')
local B=require('src.body')
local Equipment=require('src.equipment')
local A={}

-- The cabin is deliberately the local ground-rest rule without terrain exposure,
-- navigation, a bed bonus, or health recovery.  Travel supplies the food callback
-- so the same hunger threshold and starvation ordering remain explicit.
function A.transitStep(a,rules,consumeFood)
 if not a.alive then return false end
 a.hunger=math.min(100,a.hunger+rules.hungerRate)
 a.fatigue=math.max(0,a.fatigue-0.035)
 a.breath=math.min(100,a.breath+1.6)
 if a.hunger>=100 then a.hp=a.hp-0.06 end
 if a.hp<=0 then return true,'starvation' end
 if a.hunger>=60 and consumeFood and consumeFood() then a.hunger=math.max(0,a.hunger-48) end
 return false
end

function A.kill(w,a,reason,context)
 if not a.alive then return end
 if context and context.campaign and context.campaign.features.equipment==1 then
 require('src.equipment').dropPerson(context.campaign,a.personId,context.siteId,a.x,a.y)
 end
 if context and context.campaign and context.campaign.features.relics==1 then
  require('src.relics').dropPerson(context.campaign,a.personId,context.siteId,a.x,a.y)
 end
 if context and context.campaign and context.campaign.features.security==1 and a.security and a.security.ammo>0 then
  W.stack(w,'ammunition',a.security.ammo,a.x,a.y);a.security.ammo=0
 end
 J.release(w,a,true);a.alive=false;a.hp=0;a.status='Dead';a.reason=reason;a.deathTick=w.tick
 W.event(w,'death',a.name..' died: '..reason..'. No replacement will arrive.',a.id)
 if context and context.campaign and context.campaign.features.psychology==1 then require('src.psychology').death(context.campaign,w,a,reason,context) end
end
function A.step(w,context)
 for _,a in ipairs(w.workers) do if a.alive then
 a.hunger=math.min(100,a.hunger+w.rules.hungerRate)
  local hpBefore=a.hp;local criticalBefore=a.criticalBreath==true
  local sleeping=a.task and a.task.kind=='rest' and not a.task.path[a.task.next]
  if sleeping then
   a.fatigue=math.max(0,a.fatigue-(a.task.slot and 0.12 or 0.035))
   -- Slow recovery only while fed and resting in a bed; death is never reversed.
   if a.task.slot and a.hunger<50 and a.hp>0 then a.hp=math.min(100,a.hp+0.003) end
  else a.fatigue=math.min(100,a.fatigue+w.rules.fatigueRate*(a.worked and 1.4 or 1)) end
  local eyeX,eyeY=B.eye(w,a)
  local headWet=W.get(w,eyeX,eyeY)==M.WATER or W.get(w,eyeX+B.width(w)-1,eyeY)==M.WATER
  local hot,steam=false,false
  B.occupied(w,a.x,a.y,function(x,y)
   local m=W.get(w,x,y); if m==M.LAVA then hot=true elseif m==M.STEAM then steam=true end
  end)
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
  if a.hp<=0 then A.kill(w,a,reason or 'injuries',context)
  else
   if not N.support(w,a.x,a.y) and N.occupy(w,a.x,a.y+1,false) then
    if a.task then J.release(w,a,true) end
    a.y=a.y+1;a.fall=a.fall+1;a.status='Falling';a.thinkAt=w.tick+1
   else
     if a.fall>0 then
     local damage
     if context and Equipment.safe(context.campaign) then
      local blocks=math.max(0,math.ceil((a.fall-4)/4));damage=math.min(90,math.ceil(blocks*15))
      if context.campaign.features.environments==1 then damage=require('src.environments').fallDamage(damage,w) end
     else damage=math.max(0,a.fall-5)*3 end
     if damage>0 then a.hp=a.hp-damage;W.event(w,'injury',a.name..' fell '..a.fall..' cells and took '..damage..' damage.',a.id) end
     a.fall=0
    end
    if context and Equipment.safe(context.campaign) then
     local critical=a.breath<=0
     if a.blastDangerTick==w.tick then Equipment.stress(context.campaign,a,25,w.tick,'blast_danger')
     elseif a.hp<hpBefore then Equipment.stress(context.campaign,a,20,w.tick,a.fall>4 and 'dangerous_fall' or 'seriously_injured')
     elseif critical and not criticalBefore then Equipment.stress(context.campaign,a,25,w.tick,'critical_breath') end
     a.criticalBreath=critical;Equipment.recover(context.campaign,a,w)
    end
    if a.hp<=0 then A.kill(w,a,'fall injuries',context)
    else
     local unsafe=not N.occupy(w,a.x,a.y,true)
     if a.evacuate and w.tick<=a.evacuate.untilTick and a.task and a.task.kind~='escape' then J.release(w,a,true);a.thinkAt=w.tick end
     local environmental=context and context.campaign.features.environments==1 and a.environment and a.environment.danger
     if a.task and ((unsafe and a.task.kind~='escape') or (environmental and a.task.kind~='escape') or (a.hunger>=82 and a.task.kind~='eat' and w.tick%100==a.id%100 and J.canEat(w,a))
        or (a.fatigue>=95 and a.task.kind~='rest' and a.task.kind~='eat')) then
      if unsafe and context and Equipment.safe(context.campaign) and a.task.kind=='work' then Equipment.stress(context.campaign,a,15,w.tick,'unsafe_abort') end
      J.release(w,a,true);a.thinkAt=w.tick
     end
     if environmental then require('src.environments').seekSafety(context.campaign,w,a)
     elseif not a.task and w.tick>=a.thinkAt then J.plan(w,a,context);a.thinkAt=w.tick+w.rules.planEvery end
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
