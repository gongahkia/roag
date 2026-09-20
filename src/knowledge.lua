-- Personal field knowledge is deliberately small, versioned data.  It records
-- what a person saw and supported; it is not a colony-wide unlock table.
local U=require('src.util')
local Cat=require('src.catalog')
local K={version=1,maxObservations=64,maxFacts=64,maxStudies=64,maxSamples=4,maxHistory=128,sightRange=8,studyWork=120}

local categories={'flora','fauna','sites'}
local effectSpecs={
 filter_steam_to_water={category='flora',kind='filter',fact='operational/flora/filter/steam-to-water/v1'},
 thorn_sand_to_rock={category='flora',kind='thorn',fact='operational/flora/thorn/sand-to-rock/v1'},
}
local registry={}
for _,category in ipairs(categories) do
 local source=category=='flora' and Cat.flora or category=='fauna' and Cat.fauna or Cat.sites
 for _,kind in ipairs(U.keys(source)) do
  local id='identify/'..category..'/'..kind..'/v1'
  registry[id]={id=id,version=1,kind='identification',category=category,subject=kind,label=source[kind].name,note=source[kind].note}
 end
end
for effect,spec in pairs(effectSpecs) do
 local source=Cat.flora[spec.kind]
 registry[spec.fact]={id=spec.fact,version=1,kind='operational',category=spec.category,subject=spec.kind,effect=effect,
  label=source.name,note=effect=='filter_steam_to_water' and 'Supported field finding: this growth can change adjacent steam into water.' or 'Supported field finding: this growth can change adjacent sand into rock.'}
end

local function exact(t,keys,label)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do assert(keys[key],'Unknown '..label..' key '..tostring(key)) end
 for key in pairs(keys) do assert(t[key]~=nil,'Missing '..label..' key '..key) end
end
local function dense(t,label,limit)
 assert(type(t)=='table',label..' must be an array')
 local count,max=0,0
 for key in pairs(t) do U.integer(key,label..' index',1,limit);count=count+1;if key>max then max=key end end
 assert(count==max and count<=limit,label..' has a hole or exceeds its limit')
 return max
end
local function sourceRegistry(category)
 return category=='flora' and Cat.flora or category=='fauna' and Cat.fauna or category=='sites' and Cat.sites
end
local function validateSource(source,label)
 exact(source,{siteId=true,category=true,id=true,kind=true,definitionVersion=true},label)
 U.integer(source.siteId,label..' site ID',1,3);U.integer(source.id,label..' entity ID',1,100000000)
 assert(type(source.category)=='string' and type(source.kind)=='string' and source.definitionVersion==1,'Invalid '..label)
 assert(sourceRegistry(source.category) and sourceRegistry(source.category)[source.kind],'Unknown '..label..' definition')
end
local function sameSource(a,b)
 return a.siteId==b.siteId and a.category==b.category and a.id==b.id and a.kind==b.kind and a.definitionVersion==b.definitionVersion
end
local function sampleKey(sample)
 return sample.siteId..':'..sample.tick..':'..sample.ordinal
end
local function validateSample(sample,label,tick)
 exact(sample,{siteId=true,tick=true,ordinal=true,effect=true,x=true,y=true,before=true,after=true,quantity=true,source=true},label)
 U.integer(sample.siteId,label..' site ID',1,3);U.integer(sample.tick,label..' tick',0,tick);U.integer(sample.ordinal,label..' ordinal',1,100000000)
 assert(effectSpecs[sample.effect],'Unknown '..label..' effect');U.integer(sample.x,label..' x',1,512);U.integer(sample.y,label..' y',1,256)
 U.integer(sample.before,label..' before material',0,100);U.integer(sample.after,label..' after material',0,100);U.integer(sample.quantity,label..' quantity',1,1000000)
 validateSource(sample.source,label..' source')
end
local function validateFact(record,label,tick)
 exact(record,{id=true,version=true,tick=true,method=true,subject=true,provenance=true},label)
 local spec=registry[record.id];assert(spec and record.version==spec.version,'Unknown '..label..' definition')
 U.integer(record.tick,label..' tick',0,tick);assert(record.method=='survey' or record.method=='study' or record.method=='taught' or record.method=='record' or record.method=='mixed','Invalid '..label..' method')
 validateSource(record.subject,label..' subject');assert(record.subject.category==spec.category and record.subject.kind==spec.subject and record.subject.definitionVersion==spec.version,'Mismatched '..label..' subject')
 dense(record.provenance,label..' provenance',K.maxSamples)
 if spec.kind=='identification' then assert((record.method=='survey' or record.method=='taught' or record.method=='record' or record.method=='mixed') and #record.provenance==0,'Invalid identification provenance')
 else
  assert((record.method=='study' and #record.provenance>=2) or ((record.method=='taught' or record.method=='record' or record.method=='mixed') and #record.provenance<=K.maxSamples),'Invalid operational provenance')
  local prior={};for _,sample in ipairs(record.provenance) do validateSample(sample,label..' provenance sample',tick);assert(sample.effect==spec.effect,'Mismatched study effect');assert(not prior[sampleKey(sample)],'Duplicate study provenance');prior[sampleKey(sample)]=true end
 end
end
local function validateEffect(effect,label,tick,source)
 exact(effect,{kind=true,distinctTicks=true,lastEffectTick=true,ticks=true,samples=true},label)
 local spec=effectSpecs[effect.kind];assert(spec and source.category==spec.category and source.kind==spec.kind,'Invalid '..label..' kind')
 U.integer(effect.distinctTicks,label..' distinct tick count',0,100000000);U.integer(effect.lastEffectTick,label..' last tick',0,tick)
 assert(effect.distinctTicks>=#effect.ticks,'Invalid '..label..' count');dense(effect.ticks,label..' ticks',K.maxSamples);dense(effect.samples,label..' samples',K.maxSamples);assert(#effect.ticks==#effect.samples,'Mismatched '..label..' samples')
 local prior=0
 for i,value in ipairs(effect.ticks) do
  U.integer(value,label..' tick',0,tick);assert(value>prior,'Unordered '..label..' ticks');prior=value
  local sample=effect.samples[i];validateSample(sample,label..' sample',tick);assert(sample.tick==value and sample.effect==effect.kind and sameSource(sample.source,source),'Mismatched '..label..' sample')
 end
end
local function validateObservation(record,label,tick)
 exact(record,{source=true,firstSeenTick=true,lastSeenTick=true,sightingTicks=true,effects=true},label)
 validateSource(record.source,label..' source');U.integer(record.firstSeenTick,label..' first sighting',0,tick);U.integer(record.lastSeenTick,label..' last sighting',record.firstSeenTick,tick);U.integer(record.sightingTicks,label..' sighting count',1,100000000)
 dense(record.effects,label..' effects',2);local seen={}
 for _,effect in ipairs(record.effects) do assert(not seen[effect.kind],'Duplicate '..label..' effect');seen[effect.kind]=true;validateEffect(effect,label..' effect',tick,record.source) end
end
local function validateStudy(record,label,tick)
 exact(record,{id=true,version=true,firstTick=true,lastTick=true,progress=true,source=true,provenance=true},label)
 local spec=registry[record.id];assert(spec and spec.kind=='operational' and record.version==spec.version,'Invalid '..label..' topic')
 U.integer(record.firstTick,label..' first tick',0,tick);U.integer(record.lastTick,label..' last tick',record.firstTick,tick);U.integer(record.progress,label..' progress',0,K.studyWork-1)
 validateSource(record.source,label..' source');assert(record.source.category==spec.category and record.source.kind==spec.subject,'Mismatched '..label..' source')
 dense(record.provenance,label..' provenance',K.maxSamples);assert(#record.provenance>=2,'Study lacks qualifying evidence')
 local prior={};for _,sample in ipairs(record.provenance) do validateSample(sample,label..' provenance sample',tick);assert(sample.effect==spec.effect,'Mismatched '..label..' effect');assert(not prior[sampleKey(sample)],'Duplicate '..label..' provenance');prior[sampleKey(sample)]=true end
end

function K.enabled(context)
 return context and context.campaign and context.campaign.features and context.campaign.features.knowledge==1
end
function K.newPersonal()
 return {version=1,observations={},facts={},studies={},lastStudyActionTick=0}
end
function K.attach(worker)
 worker.frontier={version=1,knowledge=K.newPersonal()}
end
function K.validatePersonal(frontier,tick,education)
 local fields={version=true,knowledge=true};if education then fields.education=true end
 exact(frontier,fields,'Personal frontier')
 assert(frontier.version==1,'Unsupported personal frontier version')
 local knowledge=frontier.knowledge;exact(knowledge,{version=true,observations=true,facts=true,studies=true,lastStudyActionTick=true},'Personal knowledge')
 assert(knowledge.version==K.version,'Unsupported personal knowledge version');U.integer(knowledge.lastStudyActionTick,'Last study action tick',0,tick)
 dense(knowledge.observations,'Personal observations',K.maxObservations);dense(knowledge.facts,'Personal facts',K.maxFacts);dense(knowledge.studies,'Personal studies',K.maxStudies)
 local sources,facts,studies={},{},{}
 for _,record in ipairs(knowledge.observations) do validateObservation(record,'Personal observation',tick);local key=record.source.siteId..':'..record.source.category..':'..record.source.id;assert(not sources[key],'Duplicate personal observation');sources[key]=true end
 for _,record in ipairs(knowledge.facts) do validateFact(record,'Personal fact',tick);assert(not facts[record.id],'Duplicate personal fact');facts[record.id]=true end
 for _,record in ipairs(knowledge.studies) do validateStudy(record,'Personal study',tick);assert(not studies[record.id] and not facts[record.id],'Duplicate/completed personal study');studies[record.id]=true end
 if education then require('src.education').validatePersonal(frontier.education,tick) end
 return true
end
function K.newCampaign()
 return {version=1,rulesVersion=1,registryVersion=1,nextHistoryId=1,history={}}
end
function K.validateCampaign(state,tick)
 exact(state,{version=true,rulesVersion=true,registryVersion=true,nextHistoryId=true,history=true},'Campaign knowledge')
 assert(state.version==K.version and state.rulesVersion==1 and state.registryVersion==1,'Unsupported campaign knowledge version');U.integer(state.nextHistoryId,'Next knowledge history ID',1,100000000)
 dense(state.history,'Knowledge history',K.maxHistory);local prior,max=0,0
 for _,entry in ipairs(state.history) do
  exact(entry,{id=true,tick=true,siteId=true,personId=true,name=true,fact=true,version=true,method=true,subject=true},'Knowledge history entry')
  U.integer(entry.id,'Knowledge history ID',1,state.nextHistoryId-1);assert(entry.id>prior,'Knowledge history is unordered');prior=entry.id;max=entry.id
  U.integer(entry.tick,'Knowledge history tick',0,tick);U.integer(entry.siteId,'Knowledge history site ID',1,3);U.integer(entry.personId,'Knowledge history person ID',1,100000000)
  assert(type(entry.name)=='string' and #entry.name<=80 and registry[entry.fact] and entry.version==registry[entry.fact].version and (entry.method=='survey' or entry.method=='study' or entry.method=='taught' or entry.method=='record' or entry.method=='mixed'),'Malformed knowledge history')
  validateSource(entry.subject,'Knowledge history subject')
 end
 assert(state.nextHistoryId>max,'Next knowledge history ID was already allocated')
 return true
end
function K.source(siteId,category,record)
 return {siteId=siteId,category=category,id=record.id,kind=record.kind,definitionVersion=1}
end
function K.sameSource(a,b) return sameSource(a,b) end
function K.spec(id) return registry[id] end
function K.validateSource(source,label)
 validateSource(source,label or 'Knowledge source')
 return true
end
function K.validateProvenance(samples,label,tick)
 dense(samples,label or 'Knowledge provenance',K.maxSamples)
 for _,sample in ipairs(samples) do validateSample(sample,label or 'Knowledge provenance sample',tick) end
 return true
end
function K.identificationId(category,kind) return 'identify/'..category..'/'..kind..'/v1' end
function K.operationalId(category,kind)
 for _,spec in pairs(effectSpecs) do if spec.category==category and spec.kind==kind then return spec.fact end end
end
local function personal(worker)
 return worker and worker.frontier and worker.frontier.knowledge
end
local function fact(worker,id)
 for _,record in ipairs((personal(worker) or {}).facts or {}) do if record.id==id then return record end end
end
function K.fact(worker,id) return fact(worker,id) end
function K.identified(worker,category,kind) return fact(worker,K.identificationId(category,kind))~=nil end
function K.supports(worker,category,kind)
 local id=K.operationalId(category,kind);return id and fact(worker,id) or nil
end
local function findObservation(knowledge,source)
 for _,record in ipairs(knowledge.observations) do if sameSource(record.source,source) then return record end end
end
local function observationFor(knowledge,source)
 local record=findObservation(knowledge,source);if record then return record end
 if #knowledge.observations>=K.maxObservations then
  local remove=1
  for i=2,#knowledge.observations do
   local a,b=knowledge.observations[i],knowledge.observations[remove]
   local ak=a.lastSeenTick..':'..a.source.siteId..':'..a.source.category..':'..a.source.id
   local bk=b.lastSeenTick..':'..b.source.siteId..':'..b.source.category..':'..b.source.id
   if ak<bk then remove=i end
  end
  table.remove(knowledge.observations,remove)
 end
 record={source=U.deep(source),firstSeenTick=0,lastSeenTick=0,sightingTicks=0,effects={}}
 knowledge.observations[#knowledge.observations+1]=record
 return record
end
function K.sighting(context,worker,source)
 if not K.enabled(context) or not worker.alive then return false end
 local knowledge=personal(worker);assert(knowledge,'Knowledge-enabled worker lacks personal record')
 local record=observationFor(knowledge,source)
 if record.firstSeenTick==0 then record.firstSeenTick=context.campaign.tick end
 if record.lastSeenTick~=context.campaign.tick then record.sightingTicks=math.min(100000000,record.sightingTicks+1);record.lastSeenTick=context.campaign.tick end
 return true
end
local function effectRecord(observation,kind)
 for _,record in ipairs(observation.effects) do if record.kind==kind then return record end end
 local record={kind=kind,distinctTicks=0,lastEffectTick=0,ticks={},samples={}}
 observation.effects[#observation.effects+1]=record
 table.sort(observation.effects,function(a,b) return a.kind<b.kind end)
 return record
end
local function recordEffect(effect,sample)
 if effect.lastEffectTick==sample.tick then return end
 effect.lastEffectTick=sample.tick;effect.distinctTicks=math.min(100000000,effect.distinctTicks+1)
 if #effect.ticks<4 then effect.ticks[#effect.ticks+1]=sample.tick;effect.samples[#effect.samples+1]=sample
 else effect.ticks[3]=effect.ticks[4];effect.samples[3]=effect.samples[4];effect.ticks[4]=sample.tick;effect.samples[4]=sample end
end
function K.effect(context,worker,source,effect,x,y,before,after,ordinal)
 if not K.enabled(context) or not worker.alive then return false end
 local spec=effectSpecs[effect];assert(spec and source.category==spec.category and source.kind==spec.kind,'Unsupported knowledge effect')
 K.sighting(context,worker,source)
 local observation=findObservation(personal(worker),source);local effectState=effectRecord(observation,effect)
 recordEffect(effectState,{siteId=source.siteId,tick=context.campaign.tick,ordinal=ordinal,effect=effect,x=x,y=y,before=before,after=after,quantity=1,source=U.deep(source)})
 return true
end
local function clear(w,x,y,tx,ty)
 local World=require('src.world');local steps=math.max(1,math.ceil(math.max(math.abs(tx-x),math.abs(ty-y))))
 for index=1,steps-1 do
  local xx=math.floor(x+(tx-x)*index/steps+0.5);local yy=math.floor(y+(ty-y)*index/steps+0.5)
  if World.solid(w,xx,yy) then return false end
 end
 return true
end
function K.witnesses(w,context,record,category,x,y)
 local out={};if not K.enabled(context) then return out end
 for _,worker in ipairs(w.workers) do if worker.alive then out[#out+1]=worker end end
 table.sort(out,function(a,b) return a.personId<b.personId end)
 local eligible={}
 for _,worker in ipairs(out) do
  local headY=worker.y-1
  if math.abs(record.x-worker.x)+math.abs(record.y-headY)<=K.sightRange
   and math.abs(x-worker.x)+math.abs(y-headY)<=K.sightRange
   and clear(w,worker.x,headY,record.x,record.y) and clear(w,worker.x,headY,x,y) then eligible[#eligible+1]=worker end
 end
 return eligible
end
function K.creditEffect(context,witnesses,source,effect,x,y,before,after)
 if not K.enabled(context) then return end
 context._knowledgeEffectOrdinal=(context._knowledgeEffectOrdinal or 0)+1
 for _,worker in ipairs(witnesses) do K.effect(context,worker,source,effect,x,y,before,after,context._knowledgeEffectOrdinal) end
end
local function qualifying(worker,source,spec)
 local observation=findObservation(personal(worker),source);if not observation then return end
 for _,effect in ipairs(observation.effects) do
  if effect.kind==spec.effect and effect.distinctTicks>=2 and #effect.samples>=2 then return effect end
 end
end
local function addHistory(context,worker,record,siteId)
 local state=context.campaign.knowledge;local id=state.nextHistoryId;state.nextHistoryId=id+1
 state.history[#state.history+1]={id=id,tick=context.campaign.tick,siteId=siteId,personId=worker.personId,name=worker.name,fact=record.id,version=record.version,method=record.method,subject=U.deep(record.subject)}
 while #state.history>K.maxHistory do table.remove(state.history,1) end
end
local function addFact(context,worker,id,method,source,provenance)
 local knowledge=personal(worker);if fact(worker,id) then return true end
 if #knowledge.facts>=K.maxFacts then return false,'Personal fact limit reached' end
 local spec=registry[id];local record={id=id,version=spec.version,tick=context.campaign.tick,method=method,subject=U.deep(source),provenance=U.deep(provenance or {})}
 knowledge.facts[#knowledge.facts+1]=record;table.sort(knowledge.facts,function(a,b) return a.id<b.id end);addHistory(context,worker,record,source.siteId)
 return true,record
end
function K.learn(context,worker,id,method,source,provenance)
 if not K.enabled(context) then return false,'Knowledge is unavailable' end
 local spec=registry[id];if not spec then return false,'Unknown supported fact' end
 return addFact(context,worker,id,method,source,provenance)
end
function K.survey(context,worker,source)
 if not K.enabled(context) then return false,'Knowledge is unavailable' end
 local id=K.identificationId(source.category,source.kind);return addFact(context,worker,id,'survey',source,{})
end
local function copyQualifying(source,effect)
 local out={}
 for i=1,math.min(2,#effect.samples) do out[#out+1]=U.deep(effect.samples[i]) end
 return out
end
local function mergeProvenance(old,current)
 local out,seen={},{}
 for _,sample in ipairs(old or {}) do local key=sampleKey(sample);if not seen[key] and #out<K.maxSamples then out[#out+1]=U.deep(sample);seen[key]=true end end
 for _,sample in ipairs(current) do local key=sampleKey(sample);if not seen[key] then
  if #out>=K.maxSamples then table.remove(out,#out) end
  out[#out+1]=U.deep(sample);seen[key]=true
 end end
 return out
end
function K.canStudy(context,worker,source)
 if not K.enabled(context) or not worker.alive then return false,'Study is unavailable' end
 local id=K.operationalId(source.category,source.kind);if not id then return false,'This encounter has no supported field study' end
 local spec=registry[id]
 if fact(worker,id) then return false,'This person already supports that finding' end
 if not K.identified(worker,source.category,source.kind) then return false,'Needs personal identification of this target' end
 local effect=qualifying(worker,source,spec);if not effect then return false,'Needs personal observations of this target changing its surroundings' end
 return true,effect,id
end
function K.studyAction(context,worker,source)
 local ok,effect,id=K.canStudy(context,worker,source);if not ok then return false,effect end
 local knowledge=personal(worker)
 if knowledge.lastStudyActionTick==context.campaign.tick then return false,'Already performed field study this tick' end
 local added=1
 if context.campaign.features.education==1 then
  local value,why=require('src.education').analysis(context,worker);if not value then return false,why end;added=value
 end
 local study
 for _,record in ipairs(knowledge.studies) do if record.id==id then study=record;break end end
 local previous=study and study.progress or 0
 if previous+added>=K.studyWork and #knowledge.facts>=K.maxFacts then return false,'Personal fact limit reached' end
 local provenance=copyQualifying(source,effect)
 if not study then
  if #knowledge.studies>=K.maxStudies then return false,'Personal study limit reached' end
  study={id=id,version=registry[id].version,firstTick=context.campaign.tick,lastTick=context.campaign.tick,progress=0,source=U.deep(source),provenance=provenance}
  knowledge.studies[#knowledge.studies+1]=study;table.sort(knowledge.studies,function(a,b) return a.id<b.id end)
 else
  study.lastTick=context.campaign.tick
  if not sameSource(study.source,source) then study.source=U.deep(source) end
  study.provenance=mergeProvenance(study.provenance,provenance)
 end
 knowledge.lastStudyActionTick=context.campaign.tick;study.progress=math.min(K.studyWork,study.progress+added);study.lastTick=context.campaign.tick
 if context.campaign.features.education==1 then require('src.education').finishAnalysis(context,worker) end
 if study.progress<K.studyWork then return true,{complete=false,progress=study.progress} end
 local learned,record=addFact(context,worker,id,'study',source,study.provenance);assert(learned,record)
 for index,value in ipairs(knowledge.studies) do if value==study then table.remove(knowledge.studies,index);break end end
 return true,{complete=true,progress=K.studyWork,record=record}
end
function K.label(worker,category,kind)
 local source=sourceRegistry(category);if not source or not source[kind] then return 'Unknown encounter' end
 return K.identified(worker,category,kind) and source[kind].name or ('Unidentified '..(category=='flora' and 'growth' or category=='fauna' and 'creature' or 'site'))
end
function K.note(worker,category,kind)
 local source=sourceRegistry(category);if not source or not source[kind] then return 'No readable field note.' end
 local operational=K.supports(worker,category,kind)
 if operational then return registry[operational.id].note end
 if K.identified(worker,category,kind) then return source[kind].note end
 return 'Sighted but not personally identified. Survey it through real fieldwork.'
end
function K.history(c)
 return c and c.knowledge and c.knowledge.history or {}
end
function K.localExperts(world,category,kind)
 local out={};for _,worker in ipairs(world.workers) do if worker.alive and K.supports(worker,category,kind) then out[#out+1]=worker end end
 table.sort(out,function(a,b) return a.personId<b.personId end);return out
end
return K
