-- COS-G03 keeps personal state compact and deterministic.  It deliberately
-- models reasons for behaviour, not a second job planner or a dialogue system.
local U=require('src.util')
local Random=require('src.campaign_random')
local P={version=1,maxMemories=32,maxCore=8,maxRelations=64}

local facets={'courage','composure','empathy','sociability','diligence','independence'}
local values={'exploration','safety','cooperation','knowledge','industry','preservation'}
local duties={'dig','build','haul','farm','pump','field'}
local roles={'miner','builder','grower','hauler','researcher','teacher','mechanic'}
local ambitions={'reach_another_world','make_discovery','teach_another','become_expert','build_a_home','survive_frontier'}
local memoryKinds={abandoned_together=true,seriously_injured=true,nearly_starved=true,panic_episode=true,dangerous_fall=true,survived_stranding=true,witnessed_injury=true,witnessed_death=true,lost_trusted_person=true,made_discovery=true,learned_from_teacher=true,learned_from_record=true,taught_someone=true,completed_record=true,first_moon_landing=true,returned_from_expedition=true,completed_ambition=true,good_conversation=true,argument=true,came_under_attack=true,was_injured_in_combat=true,witnessed_combat_death=true,defended_settlement=true,killed_hostile=true,protested_conditions=true,sabotaged_colony=true,witnessed_sabotage=true,insurgency_began=true,recovered_relic=true,relic_discharge=true,unstable_deep_transit=true,reached_star_system=true}
local interpretations={danger=true,grief=true,relief=true,pride=true,curiosity=true,trust=true,disagreement=true,origin=true,learning=true,teaching=true}
local function enabled(c) return c and c.features and c.features.psychology==1 end
P.enabled=enabled
local function randint(seed,key,n)
 local stream=Random.new(Random.derive(seed,key));return Random.uniform(stream,n)
end
local function allPeople(c,includeDead)
 local out={}
 for _,site in ipairs(c.sites or {}) do for _,worker in ipairs(site.world and site.world.workers or {}) do
  if includeDead or worker.alive then out[#out+1]={worker=worker,site=site} end
 end end
 table.sort(out,function(a,b) return a.worker.personId<b.worker.personId end)
 return out
end
local function state(worker) return worker.psychology end
function P.band(worker)
 local stress=(worker and worker.stress) or 0
 if stress>=80 then return 'Panicked' elseif stress>=70 then return 'Fraying' elseif stress>=50 then return 'Distressed' elseif stress>=25 then return 'Strained' end
 return 'Steady'
end
local facetWords={courage={'cautious','bold'},composure={'easily unsettled','composed'},empathy={'reserved','empathetic'},sociability={'private','social'},diligence={'easily distracted','persistent'},independence={'group-minded','independent'}}
local function describeFacet(name,v)
 local low,high=facetWords[name][1],facetWords[name][2]
 return v<34 and low or v>66 and high or 'balanced'
end
function P.describe(worker)
 local s=state(worker);if not s then return nil end
 local traits={};for _,name in ipairs(facets) do traits[#traits+1]=describeFacet(name,s.facets[name]) end
 return {mind=P.band(worker),traits=traits,ambition=s.ambition.kind,complete=s.ambition.completed}
end
function P.attach(worker,seed)
 local personal={version=P.version,facets={},values={},disposition={},background={},ambition={},memories={},nextMemoryId=1,relations={},adaptation={},lastRecoveryTick=0}
 for _,name in ipairs(facets) do personal.facets[name]=randint(seed,'person/'..worker.personId..'/psychology/facet/'..name..'/v1',101)-1 end
 for _,name in ipairs(values) do
  -- The sum of two independent centred draws gives variety without making all
  -- people hard extremes.  Individual relationships below can still be strong.
  personal.values[name]=math.floor(((randint(seed,'person/'..worker.personId..'/psychology/value/'..name..'/a/v1',51)-1)+(randint(seed,'person/'..worker.personId..'/psychology/value/'..name..'/b/v1',51)-1))/2)-25
  personal.adaptation[name]=0
 end
 for _,role in ipairs(duties) do personal.disposition[role]=randint(seed,'person/'..worker.personId..'/psychology/disposition/'..role..'/v1',5)-3 end
 local role=roles[randint(seed,'person/'..worker.personId..'/psychology/role/v1',#roles)]
 local exile=({'smuggling','sabotage','trespass','forbidden salvage','desertion','fraud','political noncompliance'})[randint(seed,'person/'..worker.personId..'/psychology/exile/v1',7)]
 personal.background={origin='abandoned_convict',role=role,exile=exile}
 personal.ambition={kind=ambitions[randint(seed,'person/'..worker.personId..'/psychology/ambition/v1',#ambitions)],completed=false}
 worker.psychology=personal
end
local function relation(worker,otherId,create)
 local s=state(worker);if not s or worker.personId==otherId then return nil end
 for _,record in ipairs(s.relations) do if record.personId==otherId then return record end end
 if not create or #s.relations>=P.maxRelations then return nil end
 local record={personId=otherId,trust=0,respect=0,affection=0,resentment=0,lastInteractionTick=0,interactionCount=0}
 s.relations[#s.relations+1]=record;table.sort(s.relations,function(a,b)return a.personId<b.personId end)
 return record
end
P.relation=relation
local function clamp(v,lo,hi) return math.max(lo,math.min(hi,v)) end
local function adjustRelation(worker,otherId,delta,tick)
 local r=relation(worker,otherId,true);if not r then return end
 r.trust=clamp(r.trust+(delta.trust or 0),-100,100)
 r.respect=clamp(r.respect+(delta.respect or 0),-100,100)
 r.affection=clamp(r.affection+(delta.affection or 0),-100,100)
 r.resentment=clamp(r.resentment+(delta.resentment or 0),0,100)
 r.lastInteractionTick=tick or r.lastInteractionTick;r.interactionCount=math.min(1000000,r.interactionCount+1)
 return r
end
function P.initialise(c)
 if not enabled(c) then return end
 local people=allPeople(c,true)
 for _,entry in ipairs(people) do if not entry.worker.psychology then P.attach(entry.worker,c.seed) end end
 for _,from in ipairs(people) do for _,to in ipairs(people) do if from.worker.personId~=to.worker.personId then
  local a,b=state(from.worker),state(to.worker);local difference=0
  for _,name in ipairs(values) do difference=difference+math.abs(a.values[name]-b.values[name]) end
  local jitter=randint(c.seed,'relationship/'..from.worker.personId..'/'..to.worker.personId..'/initial/v1',131)-66
  -- A frontier crew can begin as close friends, guarded acquaintances, or
  -- rivals.  These are attitudes from a shared abandonment, not invented events.
  local trust=clamp(jitter-math.floor(difference/18),-80,80)
  local affection=clamp(math.floor(jitter*.7)-math.floor(difference/24),-70,70)
  relation(from.worker,to.worker.personId,true)
  local r=relation(from.worker,to.worker.personId);r.trust=trust;r.affection=affection;r.respect=clamp(math.floor(jitter*.5),-60,60);r.resentment=math.max(0,-trust)
 end end end
 for _,entry in ipairs(people) do P.memory(c,entry.worker,'abandoned_together',{participants=(function() local out={};for _,other in ipairs(people) do if other.worker.personId~=entry.worker.personId then out[#out+1]=other.worker.personId end end;return out end)(),valence=-4,intensity=35,salience=85,interpretation='origin',source='origin:'..entry.worker.personId}) end
end
local function effectiveSalience(memory,tick) return math.max(1,memory.salience-math.floor((tick-memory.createdTick)/3000)) end
local function evict(s,tick)
 while #s.memories>P.maxMemories do
  local choice
  for i,m in ipairs(s.memories) do if not m.core then
   if not choice or effectiveSalience(m,tick)<effectiveSalience(choice.m,tick) or (effectiveSalience(m,tick)==effectiveSalience(choice.m,tick) and (m.lastRecalledTick<choice.m.lastRecalledTick or (m.lastRecalledTick==choice.m.lastRecalledTick and m.id<choice.m.id))) then choice={i=i,m=m} end
  end end
  if not choice then break end
  table.remove(s.memories,choice.i)
 end
 local cores={};for _,m in ipairs(s.memories) do if m.core then cores[#cores+1]=m end end
 table.sort(cores,function(a,b) if a.salience~=b.salience then return a.salience<b.salience end;if a.createdTick~=b.createdTick then return a.createdTick<b.createdTick end;return a.id<b.id end)
 while #cores>P.maxCore do
  local victim
  for i,m in ipairs(cores) do if not m.pinned then victim=i;break end end
  if not victim then break end
  cores[victim].core=false;table.remove(cores,victim)
 end
end
local defaults={
 seriously_injured={valence=-45,intensity=60,salience=65,interpretation='danger'},nearly_starved={valence=-45,intensity=65,salience=70,interpretation='danger'},panic_episode={valence=-35,intensity=60,salience=70,interpretation='danger'},dangerous_fall={valence=-40,intensity=65,salience=75,interpretation='danger'},
 witnessed_injury={valence=-25,intensity=45,salience=45,interpretation='grief'},witnessed_death={valence=-55,intensity=85,salience=90,interpretation='grief'},lost_trusted_person={valence=-65,intensity=90,salience=95,interpretation='grief'},made_discovery={valence=30,intensity=55,salience=60,interpretation='curiosity'},learned_from_teacher={valence=20,intensity=45,salience=45,interpretation='learning'},learned_from_record={valence=12,intensity=30,salience=30,interpretation='learning'},taught_someone={valence=24,intensity=45,salience=50,interpretation='teaching'},completed_record={valence=20,intensity=42,salience=45,interpretation='pride'},first_moon_landing={valence=35,intensity=70,salience=85,interpretation='pride'},returned_from_expedition={valence=18,intensity=40,salience=40,interpretation='relief'},completed_ambition={valence=40,intensity=70,salience=85,interpretation='pride'},good_conversation={valence=12,intensity=30,salience=30,interpretation='trust'},argument={valence=-18,intensity=45,salience=40,interpretation='disagreement'},abandoned_together={valence=-4,intensity=35,salience=85,interpretation='origin'},came_under_attack={valence=-30,intensity=65,salience=70,interpretation='danger'},was_injured_in_combat={valence=-45,intensity=70,salience=80,interpretation='danger'},witnessed_combat_death={valence=-55,intensity=85,salience=90,interpretation='grief'},defended_settlement={valence=15,intensity=50,salience=65,interpretation='pride'},killed_hostile={valence=-15,intensity=55,salience=65,interpretation='danger'},protested_conditions={valence=-15,intensity=45,salience=50,interpretation='disagreement'},sabotaged_colony={valence=-40,intensity=70,salience=80,interpretation='disagreement'},witnessed_sabotage={valence=-30,intensity=60,salience=70,interpretation='danger'},insurgency_began={valence=-50,intensity=80,salience=90,interpretation='danger'},recovered_relic={valence=25,intensity=55,salience=65,interpretation='curiosity'},relic_discharge={valence=-38,intensity=65,salience=75,interpretation='danger'},unstable_deep_transit={valence=-20,intensity=50,salience=55,interpretation='danger'},reached_star_system={valence=35,intensity=70,salience=85,interpretation='pride'}
}
local function memoryHasSource(s,source)
 if not source then return false end
 for _,m in ipairs(s.memories) do if m.source==source then return true end end
end
function P.changeStress(c,worker,amount,tick)
 if not enabled(c) or not worker.alive then return 0 end
 worker.stress=clamp((worker.stress or 0)+amount,0,100);worker.lastStressTick=tick
 if worker.stress>=80 then worker.panic=true end
 if worker.panic and worker.stress<=50 and not worker.evacuate then worker.panic=false end
 return amount
end
function P.memory(c,worker,kind,data)
 if not enabled(c) or not worker or not worker.alive then return end
 assert(memoryKinds[kind],'Unknown psychology memory kind')
 local s=state(worker);if not s then return end;data=data or {}
 if memoryHasSource(s,data.source) then return end
 local d=defaults[kind];local valence=clamp(data.valence==nil and d.valence or data.valence,-100,100);local intensity=clamp(data.intensity==nil and d.intensity or data.intensity,1,100);local salience=clamp(data.salience==nil and d.salience or data.salience,1,100)
 local participants={};local seen={}
 for _,id in ipairs(data.participants or {}) do if not seen[id] and #participants<8 then participants[#participants+1]=id;seen[id]=true end end
 table.sort(participants)
 local id=s.nextMemoryId;s.nextMemoryId=id+1
 local m={id=id,kind=kind,createdTick=c.tick,lastRecalledTick=c.tick,siteId=data.siteId or 0,participants=participants,valence=valence,intensity=intensity,salience=salience,interpretation=data.interpretation or d.interpretation,source=data.source or ('memory:'..worker.personId..':'..id),recallCount=0,core=salience>=80 or kind=='abandoned_together',pinned=kind=='abandoned_together'}
 s.memories[#s.memories+1]=m;evict(s,c.tick)
 if data.stress then P.changeStress(c,worker,data.stress,c.tick) end
 if c.features and c.features.security==1 and worker.security then
  local Security=require('src.security');local source=data.source or ('memory:'..worker.personId..':'..id)
  if kind=='nearly_starved' then Security.grieve(c,worker,'severe_hunger',6,source)
  elseif kind=='seriously_injured' and worker.task and worker.task.kind=='work' then Security.grieve(c,worker,'ordinary_job_injury',8,source)
  elseif kind=='panic_episode' then Security.grieve(c,worker,'panic',3,source)
  elseif kind=='completed_ambition' then Security.grieve(c,worker,'completed_ambition',-10,source)
  elseif kind=='good_conversation' then Security.grieve(c,worker,'positive_social',-2,source)
  elseif kind=='argument' then
   local other=(data.participants or {})[1];local r=other and relation(worker,other,false)
   if r and r.resentment>=60 then Security.grieve(c,worker,'severe_argument',4,source) end
  end
 end
 local adaptation=data.adaptation
 if adaptation then for name,amount in pairs(adaptation) do
   s.adaptation[name]=clamp((s.adaptation[name] or 0)+amount,-12,12)
   if s.adaptation[name]>=6 then s.values[name]=clamp(s.values[name]+1,-50,50);s.adaptation[name]=s.adaptation[name]-6
   elseif s.adaptation[name]<=-6 then s.values[name]=clamp(s.values[name]-1,-50,50);s.adaptation[name]=s.adaptation[name]+6 end
  end end
 return m
end
function P.physical(c,worker,amount,tick,kind)
 if not enabled(c) then return false end
 local s=state(worker);local danger=(100-s.facets.courage)/200+s.values.safety/250
 local delta=math.max(1,math.floor(amount*(1+danger)+.5));local source=(kind or 'danger')..':'..tostring(tick)
 P.memory(c,worker,kind=='critical_breath' and 'nearly_starved' or kind=='dangerous_fall' and 'dangerous_fall' or 'seriously_injured',{stress=delta,source=source,siteId=0,adaptation=(kind=='dangerous_fall' or kind=='seriously_injured') and {safety=2,exploration=-1} or nil})
 if worker.stress>=80 then P.memory(c,worker,'panic_episode',{source='panic:'..tostring(tick),stress=0}) end
 return true
end
function P.recover(c,worker,w)
 if not enabled(c) or not worker.alive then return false end
 if w.tick%100==0 and not worker.evacuate and worker.hunger<82 and worker.fatigue<95 and worker.breath>0 then
  local n=1+math.floor(state(worker).facets.composure/34);P.changeStress(c,worker,-n,w.tick);state(worker).lastRecoveryTick=w.tick
 end
 if worker.panic and worker.stress<=50 and not worker.evacuate then worker.panic=false end
 return true
end
function P.maintenance(c)
 if not enabled(c) then return end
 for _,entry in ipairs(allPeople(c,false)) do
  local worker,w=entry.worker,entry.site.world;P.recover(c,worker,w)
  if c.tick%300==0 then
   local choice
   for _,m in ipairs(state(worker).memories) do if m.createdTick<c.tick then
    local score=effectiveSalience(m,c.tick)*(1+m.intensity)+((worker.personId*37+m.id*17+c.tick)%11)
    if not choice or score>choice.score or (score==choice.score and m.id<choice.m.id) then choice={m=m,score=score} end
   end end
   if choice then local m=choice.m;m.lastRecalledTick=c.tick;m.recallCount=math.min(1000000,m.recallCount+1);P.changeStress(c,worker,math.floor(m.valence/4),c.tick);if m.recallCount>=3 and m.intensity>=50 then m.core=true;evict(state(worker),c.tick) end end
  end
  local ambition=state(worker).ambition
  if not ambition.completed and ambition.kind=='survive_frontier' and c.tick>=10000 then P.completeAmbition(c,worker,'survived') end
 end
end
function P.completeAmbition(c,worker,source)
 if not enabled(c) then return end
 local ambition=state(worker).ambition;if ambition.completed then return end
 ambition.completed=true;ambition.completedTick=c.tick;ambition.source=source or 'event';P.memory(c,worker,'completed_ambition',{stress=-10,source='ambition:'..ambition.kind,siteId=0})
end
function P.discovery(c,worker,record)
 if not enabled(c) or not record or record.method~='study' then return end
 P.memory(c,worker,'made_discovery',{stress=-math.max(1,math.floor((state(worker).values.knowledge+50)/20)),source='fact:'..record.id..':'..record.tick,siteId=record.subject.siteId,adaptation={knowledge=2,exploration=1}})
 if state(worker).ambition.kind=='make_discovery' then P.completeAmbition(c,worker,'fact:'..record.id) end
end
function P.teaching(c,teacher,learner,topic,complete)
 if not enabled(c) or not teacher or not learner then return end
 local tick=c.tick;local prior=relation(learner,teacher.personId,true)
 if prior.interactionCount==0 or tick-prior.lastInteractionTick>=100 then adjustRelation(learner,teacher.personId,{respect=1},tick) end
 if complete then
  adjustRelation(learner,teacher.personId,{trust=3},tick);adjustRelation(teacher,learner.personId,{affection=1,respect=1},tick)
  P.memory(c,learner,'learned_from_teacher',{stress=-2,source='taught:'..teacher.personId..':'..learner.personId..':'..topic,participants={teacher.personId},siteId=0,adaptation={cooperation=1,knowledge=1}})
  P.memory(c,teacher,'taught_someone',{stress=-1,source='taught-by:'..teacher.personId..':'..learner.personId..':'..topic,participants={learner.personId},siteId=0,adaptation={cooperation=1,knowledge=1}})
  if state(teacher).ambition.kind=='teach_another' then P.completeAmbition(c,teacher,'teach:'..topic) end
 end
end
function P.death(c,w,victim,reason,context)
 if not enabled(c) then return end
 local V=require('src.visibility')
 for _,observer in ipairs(w.workers) do if observer.alive and observer.personId~=victim.personId then
  local near=(observer.x-victim.x)^2+(observer.y-victim.y)^2<=400
  if near and V.visible(w,observer,victim.x,victim.y,context) then
   local r=relation(observer,victim.personId,false);local closeness=r and math.max(0,r.trust+r.affection) or 0
   local grief=math.min(70,20+math.floor(state(observer).facets.empathy/4)+math.floor(closeness/4))
   P.memory(c,observer,'witnessed_death',{stress=grief,source='death:'..victim.personId..':'..w.tick,participants={victim.personId},siteId=w.frontier.siteId})
   if closeness>=80 then P.memory(c,observer,'lost_trusted_person',{stress=15,source='lost:'..victim.personId..':'..w.tick,participants={victim.personId},siteId=w.frontier.siteId}) end
   if c.features and c.features.security==1 and observer.security and r and r.affection>=50 then require('src.security').grieve(c,observer,'close_death',10,'death-grievance:'..victim.personId..':'..w.tick) end
  end
 end end
end
local function socialOutcome(a,b,tick)
 local sa,sb=state(a),state(b);local difference=0;local shared=0
 for _,name in ipairs(values) do difference=difference+math.abs(sa.values[name]-sb.values[name]);shared=shared+math.min(sa.values[name],sb.values[name]) end
 local ar,br=relation(a,b.personId,false),relation(b,a.personId,false);local resentment=(ar and ar.resentment or 0)+(br and br.resentment or 0)
 local mood=(a.stress or 0)+(b.stress or 0);local noise=(a.personId*19+b.personId*31+tick)%19
 if difference+resentment+mood/2+noise>190 then return 'argument' end
 if shared+sa.facets.empathy+sb.facets.empathy-resentment+noise>35 then return 'good_conversation' end
 return 'neutral'
end
function P.social(c)
 if not enabled(c) or c.tick%200~=0 then return end
 for _,site in ipairs(c.sites) do
  if site.world then
  local people={};for _,a in ipairs(site.world.workers) do if a.alive and not a.panic and not a.task and a.hunger<60 and a.fatigue<75 then people[#people+1]=a end end;table.sort(people,function(a,b)return a.personId<b.personId end)
  local used={}
  for i=1,#people do for j=i+1,#people do local a,b=people[i],people[j]
   local clear=require('src.visibility').fov(site.world,a.x,a.y,8)[require('src.world').index(site.world,b.x,b.y)]==true
   if not used[a.personId] and not used[b.personId] and (a.x-b.x)^2+(a.y-b.y)^2<=64 and clear then
    local drive=state(a).facets.sociability+state(b).facets.sociability;local threshold=randint(c.seed,'social/'..a.personId..'/'..b.personId..'/'..math.floor(c.tick/200)..'/v1',201)-1
    if drive>=threshold then
     local kind=socialOutcome(a,b,c.tick);local topic='cooperation';local highest=-1;for _,name in ipairs(values) do local d=math.abs(state(a).values[name]-state(b).values[name]);if d>highest then highest=d;topic=name end end
     if kind=='argument' then
      adjustRelation(a,b.personId,{trust=-3,respect=-2,affection=-1,resentment=4},c.tick);adjustRelation(b,a.personId,{trust=-3,respect=-2,affection=-1,resentment=4},c.tick)
      P.memory(c,a,'argument',{stress=4,source='argument:'..a.personId..':'..b.personId..':'..c.tick,participants={b.personId},siteId=site.id,adaptation={cooperation=-1}});P.memory(c,b,'argument',{stress=4,source='argument:'..b.personId..':'..a.personId..':'..c.tick,participants={a.personId},siteId=site.id,adaptation={cooperation=-1}})
     elseif kind=='good_conversation' then
      adjustRelation(a,b.personId,{trust=2,affection=1,resentment=-1},c.tick);adjustRelation(b,a.personId,{trust=2,affection=1,resentment=-1},c.tick)
      P.memory(c,a,'good_conversation',{stress=-2,source='talk:'..a.personId..':'..b.personId..':'..c.tick,participants={b.personId},siteId=site.id,adaptation={cooperation=1}});P.memory(c,b,'good_conversation',{stress=-2,source='talk:'..b.personId..':'..a.personId..':'..c.tick,participants={a.personId},siteId=site.id,adaptation={cooperation=1}})
     end
     if kind~='neutral' then used[a.personId]=true;used[b.personId]=true end
    end
  end
 end end
 end
end
end
function P.autoBias(worker,role)
 local s=state(worker);if not s then return 0 end
 return s.disposition[role] or 0
end
function P.workFactor(c,worker,role)
 if not enabled(c) or not worker or not worker.psychology then return 1,'Normal pace' end
 local s=state(worker);local preference=s.disposition[role] or 0
 local reluctance=math.max(0,-preference)+math.max(0,(worker.stress or 0)-25)/35+math.max(0,45-s.facets.diligence)/45
 if reluctance<1 then return 1,'Working steadily' end
 -- Player authority remains absolute: the job is never refused.  A reluctant
 -- worker is simply slower and sometimes loses a work beat to hesitation.
 local factor=clamp(1-reluctance*.12,.55,.9)
 return factor,preference<=-1 and 'Reluctant — working carefully and slowly' or 'Under strain — working slowly'
end
function P.work(c,worker,role,amount)
 local factor,reason=P.workFactor(c,worker,role)
 -- A small, repeatable lapse represents an ordinary mistake or rework beat.
 -- It never consumes extra material, changes a target, or weakens G02 safety.
 if factor<.78 and (c.tick+worker.personId*13+#role*7)%23==0 then return 0,'Reluctant — correcting a small mistake' end
 return amount*factor,reason
end
function P.validatePersonal(s,tick,selfId)
 assert(type(s)=='table','Missing psychology state')
 for key in pairs(s) do assert(({version=true,facets=true,values=true,disposition=true,background=true,ambition=true,memories=true,nextMemoryId=true,relations=true,adaptation=true,lastRecoveryTick=true})[key],'Unknown psychology state key') end
 assert(s.version==P.version,'Unsupported psychology version');assert(type(s.facets)=='table' and type(s.values)=='table' and type(s.adaptation)=='table' and type(s.disposition)=='table','Malformed psychology dimensions')
 for key in pairs(s.facets) do local known=false;for _,name in ipairs(facets) do known=known or key==name end;assert(known,'Unknown psychology facet') end
 for key in pairs(s.values) do local known=false;for _,name in ipairs(values) do known=known or key==name end;assert(known,'Unknown psychology value') end
 for key in pairs(s.adaptation) do local known=false;for _,name in ipairs(values) do known=known or key==name end;assert(known,'Unknown psychology adaptation') end
 for key in pairs(s.disposition) do local known=false;for _,role in ipairs(duties) do known=known or key==role end;assert(known,'Unknown psychology disposition') end
 for _,name in ipairs(facets) do U.integer(s.facets[name],'Psychology facet '..name,0,100) end;for _,name in ipairs(values) do U.integer(s.values[name],'Psychology value '..name,-50,50);U.integer(s.adaptation[name],'Psychology adaptation '..name,-12,12) end;for _,role in ipairs(duties) do U.integer(s.disposition[role],'Psychology disposition '..role,-2,2) end
 assert(type(s.background)=='table' and s.background.origin=='abandoned_convict' and type(s.background.role)=='string' and type(s.background.exile)=='string','Invalid psychology background')
 assert(type(s.ambition)=='table' and type(s.ambition.kind)=='string' and type(s.ambition.completed)=='boolean','Invalid psychology ambition');local valid=false;for _,kind in ipairs(ambitions) do valid=valid or s.ambition.kind==kind end;assert(valid,'Unknown psychology ambition');if s.ambition.completed then U.integer(s.ambition.completedTick,'Psychology ambition completion',0,tick);assert(type(s.ambition.source)=='string') end
 U.integer(s.nextMemoryId,'Psychology memory allocator',1,100000000);U.integer(s.lastRecoveryTick,'Psychology recovery tick',0,tick);assert(type(s.memories)=='table' and #s.memories<=P.maxMemories,'Psychology memory limit');assert(type(s.relations)=='table' and #s.relations<=P.maxRelations,'Psychology relation limit')
 local ids,max,cores={},0,0
 for _,m in ipairs(s.memories) do
  for key in pairs(m) do assert(({id=true,kind=true,createdTick=true,lastRecalledTick=true,siteId=true,participants=true,valence=true,intensity=true,salience=true,interpretation=true,source=true,recallCount=true,core=true,pinned=true})[key],'Unknown psychology memory key') end
  U.integer(m.id,'Psychology memory ID',1,s.nextMemoryId-1);assert(not ids[m.id],'Duplicate psychology memory ID');ids[m.id]=true;max=math.max(max,m.id);assert(memoryKinds[m.kind] and interpretations[m.interpretation],'Unknown psychology memory enum');U.integer(m.createdTick,'Psychology memory tick',0,tick);U.integer(m.lastRecalledTick,'Psychology recall tick',m.createdTick,tick);U.integer(m.siteId,'Psychology memory site',0,100000000);assert(type(m.participants)=='table' and #m.participants<=8,'Invalid psychology participants');local seen={};for _,id in ipairs(m.participants) do U.integer(id,'Psychology participant',1,100000000);assert(not seen[id],'Duplicate psychology participant');seen[id]=true end;U.integer(m.valence,'Psychology memory valence',-100,100);U.integer(m.intensity,'Psychology memory intensity',1,100);U.integer(m.salience,'Psychology memory salience',1,100);assert(type(m.source)=='string' and #m.source<=160,'Invalid psychology memory source');U.integer(m.recallCount,'Psychology recall count',0,1000000);assert(type(m.core)=='boolean' and type(m.pinned)=='boolean','Invalid psychology core marker');if m.core then cores=cores+1 end
 end
 assert(s.nextMemoryId>max and cores<=P.maxCore,'Psychology memory allocator/core limit')
 local seen={};for _,r in ipairs(s.relations) do for key in pairs(r) do assert(({personId=true,trust=true,respect=true,affection=true,resentment=true,lastInteractionTick=true,interactionCount=true})[key],'Unknown psychology relation key') end;U.integer(r.personId,'Psychology relation person',1,100000000);assert(not seen[r.personId] and r.personId~=selfId,'Duplicate/self psychology relation');seen[r.personId]=true;U.integer(r.trust,'Psychology trust',-100,100);U.integer(r.respect,'Psychology respect',-100,100);U.integer(r.affection,'Psychology affection',-100,100);U.integer(r.resentment,'Psychology resentment',0,100);U.integer(r.lastInteractionTick,'Psychology interaction tick',0,tick);U.integer(r.interactionCount,'Psychology interaction count',0,1000000) end
 return true
end
return P
