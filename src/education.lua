-- Field schools are physical local institutions.  Personal tuition travels with
-- a person; records, drafts, and sessions remain with the installed school.
local U=require('src.util')
local W=require('src.world')
local N=require('src.nav')
local Labor=require('src.labor')
local Random=require('src.campaign_random')
local Knowledge=require('src.knowledge')
local E={version=1,maxRecords=16,maxTuition=64,maxContributors=4,recordWork=120,tuitionWork=200}

local function exact(t,keys,label,optional)
 assert(type(t)=='table',label..' must be a table')
 for key in pairs(t) do assert(keys[key],'Unknown '..label..' key '..tostring(key)) end
 for key in pairs(keys) do if not (optional and optional[key]) then assert(t[key]~=nil,'Missing '..label..' key '..key) end end
end
local function dense(t,label,limit)
 assert(type(t)=='table',label..' must be an array')
 local count,max=0,0
 for key in pairs(t) do U.integer(key,label..' index',1,limit);count=count+1;if key>max then max=key end end
 assert(count==max and count<=limit,label..' has a hole or exceeds its limit')
 return max
end
local function key(id,version) return id..'/'..version end
local function personal(worker) return worker and worker.frontier and worker.frontier.education end
local function school(structure) return structure and structure.kind=='field_school' and structure.education end
local function bySchoolId(w,id)
 for slot=1,w.cols*w.rows do local s=w.structures[slot];if school(s) and s.education.id==id then return s,slot end end
end
local function workerByPerson(w,personId)
 for _,worker in ipairs(w.workers) do if worker.personId==personId then return worker end end
end
local function recordFor(s,id,version)
 for _,record in ipairs(s.education.records) do if record.topicId==id and record.topicVersion==version then return record end end
end
local function fact(worker,id)
 return Knowledge.fact(worker,id)
end
local function sourceFor(worker,id)
 local record=fact(worker,id);return record and record.subject
end
local function requiredIdentity(id)
 local spec=Knowledge.spec(id)
 if spec and spec.kind=='operational' then return Knowledge.identificationId(spec.category,spec.subject) end
end
local function supports(worker,id)
 local record=fact(worker,id);if not record then return false end
 local prerequisite=requiredIdentity(id)
 return not prerequisite or fact(worker,prerequisite)~=nil
end
local function knownEarlier(worker,id,tick)
 local record=fact(worker,id);return record and record.tick<tick and supports(worker,id)
end
local function fieldEligible(w,worker)
 if not worker.alive or worker.task or worker.directive or worker.hunger>=60 or worker.fatigue>=75 then return false end
 return Labor.score(w,worker,{kind='school'})~=nil
end
local function participants(session)
 local out={}
 if session.teacherPersonId>0 then out[#out+1]={role='teacher',personId=session.teacherPersonId} end
 if session.learnerPersonId>0 then out[#out+1]={role='learner',personId=session.learnerPersonId} end
 if session.recorderPersonId>0 then out[#out+1]={role='recorder',personId=session.recorderPersonId} end
 return out
end
local function contributor(list,entry)
 local marker=entry.kind..':'..entry.id
 for _,value in ipairs(list) do
  if value.kind..':'..value.id==marker then value.lastTick=entry.lastTick;return end
 end
 if #list>=E.maxContributors then table.remove(list,2) end
 list[#list+1]=entry
end
local function copyProvenance(record)
 local out={}
 for i=1,math.min(4,#(record.provenance or {})) do out[#out+1]=U.deep(record.provenance[i]) end
 return out
end
local function validateContributors(values,label,tick)
 dense(values,label,E.maxContributors)
 local prior={}
 for _,entry in ipairs(values) do
  exact(entry,{kind=true,id=true,firstTick=true,lastTick=true},label..' entry')
  assert(entry.kind=='person' or entry.kind=='record','Invalid '..label..' contributor kind')
  U.integer(entry.id,label..' contributor ID',1,100000000)
  U.integer(entry.firstTick,label..' contributor first tick',0,tick)
  U.integer(entry.lastTick,label..' contributor last tick',entry.firstTick,tick)
  local marker=entry.kind..':'..entry.id;assert(not prior[marker],'Duplicate '..label..' contributor');prior[marker]=true
 end
end

function E.enabled(context)
 return context and context.campaign and context.campaign.features and context.campaign.features.education==1
end
function E.newCampaign()
 return {version=1,rulesVersion=1}
end
function E.newWorld()
 return {version=1,nextSchoolId=1,nextSessionId=1}
end
function E.attach(worker,seed)
 local field=Random.new(Random.derive(seed,'person/'..worker.personId..'/expertise/fieldworkXP/v1'))
 local teaching=Random.new(Random.derive(seed,'person/'..worker.personId..'/expertise/teachingXP/v1'))
 worker.frontier.education={version=1,fieldworkXP=Random.uniform(field,150)-1,teachingXP=Random.uniform(teaching,150)-1,tuition={},lastLearningActionTick=0}
end
function E.validatePersonal(state,tick)
 exact(state,{version=true,fieldworkXP=true,teachingXP=true,tuition=true,lastLearningActionTick=true},'Personal education')
 assert(state.version==E.version,'Unsupported personal education version')
 U.integer(state.fieldworkXP,'Fieldwork XP',0,400);U.integer(state.teachingXP,'Teaching XP',0,400);U.integer(state.lastLearningActionTick,'Last education action tick',0,tick)
 dense(state.tuition,'Personal tuition',E.maxTuition);local seen={}
 for _,topic in ipairs(state.tuition) do
  exact(topic,{id=true,version=true,subject=true,progress=true,firstTick=true,lastTick=true,teachUnits=true,recordUnits=true,contributors=true},'Personal tuition topic')
  local spec=Knowledge.spec(topic.id);assert(spec and spec.version==topic.version,'Unknown tuition topic')
  assert(not seen[key(topic.id,topic.version)],'Duplicate personal tuition topic');seen[key(topic.id,topic.version)]=true
  U.integer(topic.progress,'Tuition progress',1,E.tuitionWork-1);U.integer(topic.firstTick,'Tuition first tick',0,tick);U.integer(topic.lastTick,'Tuition last tick',topic.firstTick,tick)
  U.integer(topic.teachUnits,'Tuition teaching units',0,E.tuitionWork);U.integer(topic.recordUnits,'Tuition record units',0,E.tuitionWork);assert(topic.progress==topic.teachUnits+topic.recordUnits,'Tuition method totals disagree')
  Knowledge.validateSource(topic.subject,'Tuition subject');assert(topic.subject.category==spec.category and topic.subject.kind==spec.subject,'Tuition subject mismatch')
  validateContributors(topic.contributors,'Tuition contributors',tick)
 end
 return true
end
function E.validateWorld(world,tick)
 exact(world.education,{version=true,nextSchoolId=true,nextSessionId=true},'World education')
 assert(world.education.version==E.version,'Unsupported world education version');U.integer(world.education.nextSchoolId,'Next school ID',1,100000000);U.integer(world.education.nextSessionId,'Next school session ID',1,100000000)
 local schools,maxSchool,maxSession={},0,0
 for slot=1,world.cols*world.rows do
  local s=world.structures[slot]
  if s and s.kind=='field_school' then
   local data=school(s);assert(data,'Field school state missing')
   exact(data,{id=true,policyRevision=true,enabled=true,mode=true,topicId=true,topicVersion=true,priority=true,nextRecordId=true,records=true,draft=true,session=true},'Field school',{draft=true,session=true})
   U.integer(data.id,'Field school ID',1,world.education.nextSchoolId-1);assert(not schools[data.id],'Duplicate field school ID');schools[data.id]=true;maxSchool=math.max(maxSchool,data.id)
   U.integer(data.policyRevision,'School policy revision',1,100000000);assert(type(data.enabled)=='boolean' and (data.mode=='record' or data.mode=='teach' or data.mode=='study'),'Invalid school policy');U.integer(data.priority,'School priority',1,3);U.integer(data.nextRecordId,'Next school record ID',1,100000000)
   if data.topicId then local spec=Knowledge.spec(data.topicId);assert(spec and data.topicVersion==spec.version,'Invalid school policy topic') else assert(data.topicVersion==0,'Empty school policy must use version zero') end
   dense(data.records,'School records',E.maxRecords);local records,topics,maxRecord={}, {},0
   for _,record in ipairs(data.records) do
    exact(record,{id=true,topicId=true,topicVersion=true,subject=true,tick=true,contributors=true,provenance=true},'School record')
    U.integer(record.id,'School record ID',1,data.nextRecordId-1);assert(not records[record.id],'Duplicate school record ID');records[record.id]=true;maxRecord=math.max(maxRecord,record.id)
    local spec=Knowledge.spec(record.topicId);assert(spec and record.topicVersion==spec.version,'Invalid school record topic');assert(not topics[key(record.topicId,record.topicVersion)],'Duplicate school record topic');topics[key(record.topicId,record.topicVersion)]=true;Knowledge.validateSource(record.subject,'School record subject');assert(record.subject.category==spec.category and record.subject.kind==spec.subject,'School record subject mismatch');U.integer(record.tick,'School record tick',0,tick);validateContributors(record.contributors,'School record contributors',tick);Knowledge.validateProvenance(record.provenance,'School record provenance',tick)
   end
   assert(data.nextRecordId>maxRecord,'Next school record ID was already allocated')
   if data.draft then
    local draft=data.draft;exact(draft,{topicId=true,topicVersion=true,subject=true,progress=true,firstTick=true,lastTick=true,contributors=true,provenance=true},'School recording draft')
    local spec=Knowledge.spec(draft.topicId);assert(spec and draft.topicVersion==spec.version and not recordFor(s,draft.topicId,draft.topicVersion),'Invalid or duplicate school recording draft');Knowledge.validateSource(draft.subject,'School draft subject');assert(draft.subject.category==spec.category and draft.subject.kind==spec.subject,'School draft subject mismatch');U.integer(draft.progress,'School recording progress',0,E.recordWork-1);U.integer(draft.firstTick,'School draft first tick',0,tick);U.integer(draft.lastTick,'School draft last tick',draft.firstTick,tick);validateContributors(draft.contributors,'School draft contributors',tick);Knowledge.validateProvenance(draft.provenance,'School draft provenance',tick)
   end
   if data.session then
    local session=data.session;exact(session,{id=true,policyRevision=true,mode=true,topicId=true,topicVersion=true,teacherPersonId=true,learnerPersonId=true,recorderPersonId=true,poses=true,lastEvaluationTick=true},'School session')
    U.integer(session.id,'School session ID',1,world.education.nextSessionId-1);maxSession=math.max(maxSession,session.id);assert(session.policyRevision==data.policyRevision and session.mode==data.mode and session.topicId==data.topicId and session.topicVersion==data.topicVersion,'Stale school session');U.integer(session.lastEvaluationTick,'School session evaluation tick',0,tick);dense(session.poses,'School session poses',2)
    U.integer(session.teacherPersonId,'School teacher ID',0,100000000);U.integer(session.learnerPersonId,'School learner ID',0,100000000);U.integer(session.recorderPersonId,'School recorder ID',0,100000000)
    if session.mode=='record' then assert(session.recorderPersonId>0 and session.teacherPersonId==0 and session.learnerPersonId==0,'Invalid recording participants')
    elseif session.mode=='teach' then assert(session.teacherPersonId>0 and session.learnerPersonId>0 and session.teacherPersonId~=session.learnerPersonId and session.recorderPersonId==0,'Invalid lesson participants')
    else assert(session.learnerPersonId>0 and session.teacherPersonId==0 and session.recorderPersonId==0,'Invalid record-study participants') end
   end
  elseif s and s.education then error('Non-school structure has education state') end
 end
 assert(world.education.nextSchoolId>maxSchool and world.education.nextSessionId>maxSession,'Education allocator was already allocated')
 return true
end
function E.validateCampaign(campaign)
 exact(campaign.education,{version=true,rulesVersion=true},'Campaign education')
 assert(campaign.education.version==E.version and campaign.education.rulesVersion==1,'Unsupported campaign education version')
 for _,site in ipairs(campaign.sites) do E.validateWorld(site.world,campaign.tick) end
 return true
end
function E.install(world,structure)
 assert(world.education,'Education world state is missing')
 local id=world.education.nextSchoolId;world.education.nextSchoolId=id+1
 -- `false` is an explicit serialized "no topic" marker.  `nil` would be
 -- dropped by the codec and violate the strict school schema on reload.
 structure.education={id=id,policyRevision=1,enabled=false,mode='record',topicId=false,topicVersion=0,priority=2,nextRecordId=1,records={},draft=nil,session=nil}
 return structure.education
end
function E.destroy(world,structure)
 if school(structure) and structure.education.session then E.releaseSession(world,structure) end
end
function E.releaseSession(world,structure)
 local data=school(structure);if not data or not data.session then return end
 local sessionId=data.session.id
 for _,worker in ipairs(world.workers) do
  local task=worker.task
  if task and task.kind=='school' and task.schoolId==data.id and task.sessionId==sessionId then
   if task.node and world.workClaims and world.workClaims[task.node]==worker.id then world.workClaims[task.node]=nil end
   worker.task=nil;worker.thinkAt=world.tick+1;worker.status='School session released';worker.reason='School policy or attendance changed'
  end
 end
 data.session=nil
end
function E.releaseTask(world,task)
 if task and task.kind=='school' then local structure=bySchoolId(world,task.schoolId);if structure then E.releaseSession(world,structure) end end
end
function E.sourceTopics(world,structure)
 local data=school(structure);if not data then return {} end
 local topics={}
 for _,worker in ipairs(world.workers) do if worker.alive then
  for _,record in ipairs(worker.frontier.education and worker.frontier.knowledge.facts or {}) do topics[key(record.id,record.version)]={id=record.id,version=record.version} end
 end end
 for _,record in ipairs(data.records) do topics[key(record.topicId,record.topicVersion)]={id=record.topicId,version=record.topicVersion} end
 local out={};for _,value in pairs(topics) do out[#out+1]=value end;table.sort(out,function(a,b)return a.id<b.id end);return out
end
function E.policyValid(campaign,site,payload)
 local ok,result=pcall(function()
  assert(campaign.features.education==1 and site.world.frontier.education==1,'Education unavailable in this older campaign')
  U.integer(payload.slot,'Field school slot',1,site.world.cols*site.world.rows);U.integer(payload.schoolId,'Field school ID',1,100000000);U.integer(payload.expectedPolicyRevision,'Field school policy revision',1,100000000)
  local structure=site.world.structures[payload.slot];local data=school(structure);assert(data and data.id==payload.schoolId,'School no longer exists at that site')
  assert(data.policyRevision==payload.expectedPolicyRevision,'School policy changed before this command')
  assert(type(payload.enabled)=='boolean' and (payload.mode=='record' or payload.mode=='teach' or payload.mode=='study'),'Invalid school policy')
  U.integer(payload.priority,'School policy priority',1,3)
  if payload.topicId~=nil and payload.topicId~=false then assert(type(payload.topicId)=='string','Invalid school topic') end
  if payload.topicVersion~=nil then U.integer(payload.topicVersion,'School topic version',0,100000000) end
  if not payload.enabled then return {structure=structure,data=data} end
  local spec=Knowledge.spec(payload.topicId);assert(spec and payload.topicVersion==spec.version,'Unknown school topic')
  local source=false
  for _,worker in ipairs(site.world.workers) do if worker.alive and fact(worker,payload.topicId) then source=true;break end end
  if not source and recordFor(structure,payload.topicId,payload.topicVersion) then source=true end
  assert(source,'No current local source for this school topic')
  return {structure=structure,data=data,spec=spec}
 end)
 return ok,ok and result or tostring(result)
end
function E.applyPolicy(campaign,site,payload)
 local ok,plan=E.policyValid(campaign,site,payload);if not ok then return false,plan end
 local data=plan.data
 local topicId=payload.topicId or false
 local topicVersion=payload.topicVersion or 0
 if data.enabled==payload.enabled and data.mode==payload.mode and data.topicId==topicId and data.topicVersion==topicVersion and data.priority==payload.priority then return true,'School policy already applied' end
 if data.policyRevision>=100000000 then return false,'Field school policy revision capacity reached' end
 if data.session then E.releaseSession(site.world,plan.structure) end
 if data.draft and (data.mode~=payload.mode or data.topicId~=topicId or data.topicVersion~=topicVersion) then data.draft=nil end
 data.enabled=payload.enabled;data.mode=payload.mode;data.topicId=topicId;data.topicVersion=topicVersion;data.priority=payload.priority;data.policyRevision=data.policyRevision+1
 W.event(site.world,'school','Field school policy updated.',plan.structure.id)
 return true,'School policy updated'
end

local function poses(world,structure)
 local cx,cy=structure.gx*4-2,structure.gy*4;local out={}
 for y=math.max(1,cy-4),math.min(world.height,cy+4) do for x=math.max(1,cx-4),math.min(world.width,cx+4) do
  local distance=math.abs(x-cx)+math.abs(y-cy)
  if distance<=4 and N.stand(world,x,y,true) and (not world.workClaims or not world.workClaims[W.index(world,x,y)]) then out[#out+1]={x=x,y=y,distance=distance} end
 end end
 table.sort(out,function(a,b)if a.distance~=b.distance then return a.distance<b.distance end;if a.y~=b.y then return a.y<b.y end;return a.x<b.x end)
 return out
end
local function pathTo(world,worker,pose)
 local flood=N.flood(world,worker.x,worker.y)
 return N.closest(world,flood,function(x,y)return x==pose.x and y==pose.y end)
end
local function assign(world,worker,structure,session,role,pose,path,node)
 world.workClaims=world.workClaims or {};world.workClaims[node]=worker.id
 worker.task={kind='school',schoolId=structure.education.id,sessionId=session.id,policyRevision=session.policyRevision,role=role,x=pose.x,y=pose.y,path=path,node=node,next=1,progress=0,label='Field school '..role}
 worker.status='Going to field school';worker.reason='';worker.worked=false
end
local function createSession(world,structure,fields)
 local id=world.education.nextSessionId;world.education.nextSessionId=id+1
 local data=structure.education;local session={id=id,policyRevision=data.policyRevision,mode=data.mode,topicId=data.topicId,topicVersion=data.topicVersion,teacherPersonId=fields.teacher and fields.teacher.personId or 0,learnerPersonId=fields.learner and fields.learner.personId or 0,recorderPersonId=fields.recorder and fields.recorder.personId or 0,poses=fields.poses,lastEvaluationTick=0}
 data.session=session
 for _,entry in ipairs(fields.assignments) do assign(world,entry.worker,structure,session,entry.role,entry.pose,entry.path,entry.node) end
 return session
end
local function sourcePresent(world,id,tick)
 for _,worker in ipairs(world.workers) do if worker.alive and knownEarlier(worker,id,tick) then return true end end
 return false
end
local function validLearner(worker,id)
 return worker.alive and not fact(worker,id) and (not requiredIdentity(id) or fact(worker,requiredIdentity(id))~=nil)
end
local function candidates(world,id)
 local teachers,learners,recorders={},{},{}
 for _,worker in ipairs(world.workers) do if fieldEligible(world,worker) then
  if knownEarlier(worker,id,world.tick) then teachers[#teachers+1]=worker;recorders[#recorders+1]=worker end
  if validLearner(worker,id) then learners[#learners+1]=worker end
 end end
 table.sort(teachers,function(a,b)local aa,bb=personal(a).teachingXP,personal(b).teachingXP;if aa~=bb then return aa>bb end;return a.personId<b.personId end)
 table.sort(learners,function(a,b)local aa,bb=personal(a).fieldworkXP,personal(b).fieldworkXP;if aa~=bb then return aa<bb end;return a.personId<b.personId end)
 table.sort(recorders,function(a,b)return a.personId<b.personId end)
 return teachers,learners,recorders
end
local function offer(world,structure)
 local data=structure.education
 if not data.enabled or not data.topicId or data.session or not W.supportedStructure(world,structure) then return end
 local topic=data.topicId;local hasRecord=recordFor(structure,topic,data.topicVersion)
 if data.mode=='record' then
  if hasRecord or #data.records>=E.maxRecords or not sourcePresent(world,topic,world.tick) then return end
  local _,_,recorders=candidates(world,topic);local available=poses(world,structure)
  for _,worker in ipairs(recorders) do for _,pose in ipairs(available) do local path,node=pathTo(world,worker,pose);if path then
   createSession(world,structure,{recorder=worker,poses={{role='recorder',personId=worker.personId,x=pose.x,y=pose.y}},assignments={{worker=worker,role='recorder',pose=pose,path=path,node=node}}});return
  end end end
 elseif data.mode=='teach' then
  if not sourcePresent(world,topic,world.tick) then return end
  local teachers,learners=candidates(world,topic);local available=poses(world,structure)
  for _,teacher in ipairs(teachers) do for _,learner in ipairs(learners) do if teacher~=learner then
   for i,teacherPose in ipairs(available) do local teacherPath,teacherNode=pathTo(world,teacher,teacherPose);if teacherPath then for j,learnerPose in ipairs(available) do if i~=j then
    local learnerPath,learnerNode=pathTo(world,learner,learnerPose);if learnerPath and teacherNode~=learnerNode then
     createSession(world,structure,{teacher=teacher,learner=learner,poses={{role='teacher',personId=teacher.personId,x=teacherPose.x,y=teacherPose.y},{role='learner',personId=learner.personId,x=learnerPose.x,y=learnerPose.y}},assignments={{worker=teacher,role='teacher',pose=teacherPose,path=teacherPath,node=teacherNode},{worker=learner,role='learner',pose=learnerPose,path=learnerPath,node=learnerNode}}});return
    end
   end end end end
  end end end
 elseif data.mode=='study' and hasRecord and hasRecord.tick<world.tick then
  local _,learners=candidates(world,topic);local available=poses(world,structure)
  for _,learner in ipairs(learners) do for _,pose in ipairs(available) do local path,node=pathTo(world,learner,pose);if path then
   createSession(world,structure,{learner=learner,poses={{role='learner',personId=learner.personId,x=pose.x,y=pose.y}},assignments={{worker=learner,role='learner',pose=pose,path=path,node=node}}});return
  end end end
 end
end
local function sessionCurrent(world,structure,session)
 local data=structure.education
 if data.session~=session or data.policyRevision~=session.policyRevision or data.mode~=session.mode then return false end
 for _,entry in ipairs(participants(session)) do
  local worker=workerByPerson(world,entry.personId);if not worker or not worker.alive then return false end
  local task=worker.task;if not task or task.kind~='school' or task.schoolId~=data.id or task.sessionId~=session.id or task.role~=entry.role then return false end
 end
 return true
end
function E.begin(world,context)
 if not E.enabled(context) then return end
 context._educationAttendance={}
 local schools={};for slot=1,world.cols*world.rows do local s=world.structures[slot];if school(s) then schools[#schools+1]=s end end
 table.sort(schools,function(a,b)if a.education.priority~=b.education.priority then return a.education.priority>b.education.priority end;return a.education.id<b.education.id end)
 for _,structure in ipairs(schools) do
  if structure.education.session and not sessionCurrent(world,structure,structure.education.session) then E.releaseSession(world,structure) end
  offer(world,structure)
 end
end
function E.act(world,worker,task,context)
 local structure=bySchoolId(world,task.schoolId);local data=school(structure);local session=data and data.session
 if not structure or not session or session.id~=task.sessionId or task.policyRevision~=session.policyRevision or worker.x~=task.x or worker.y~=task.y then return false,'School session changed or pose is unavailable' end
 local attendance=context and context._educationAttendance;if not attendance then return false,'School attendance context is unavailable' end
 local marker=task.schoolId..':'..task.sessionId;attendance[marker]=attendance[marker] or {};attendance[marker][task.role]=worker.personId
 worker.status='At field school';worker.reason='';worker.worked=true
 return true
end
local function actionAvailable(worker,tick) return personal(worker).lastLearningActionTick~=tick end
local function markAction(worker,tick) personal(worker).lastLearningActionTick=tick end
local function tuition(worker,id,version,subject)
 for _,record in ipairs(personal(worker).tuition) do if record.id==id and record.version==version then return record end end
 return nil
end
local function addTuition(context,worker,id,subject,mode,contribution)
 local state=personal(worker);local record=tuition(worker,id,Knowledge.spec(id).version,subject)
 if not record then
  if #state.tuition>=E.maxTuition then return false,'Personal tuition limit reached' end
  record={id=id,version=Knowledge.spec(id).version,subject=U.deep(subject),progress=0,firstTick=context.campaign.tick,lastTick=context.campaign.tick,teachUnits=0,recordUnits=0,contributors={}}
  state.tuition[#state.tuition+1]=record;table.sort(state.tuition,function(a,b)return a.id<b.id end)
 end
 local added=math.min(E.tuitionWork-record.progress,contribution);if added<=0 then return false,'Tuition is already complete' end
 record.progress=record.progress+added;record.lastTick=context.campaign.tick
 if mode=='teach' then record.teachUnits=record.teachUnits+added else record.recordUnits=record.recordUnits+added end
 return true,record,added
end
local function finishTuition(context,worker,record,source,mode)
 if record.progress<E.tuitionWork then return true end
 local method=record.teachUnits>0 and record.recordUnits>0 and 'mixed' or record.teachUnits>0 and 'taught' or 'record'
 local ok,why=Knowledge.learn(context,worker,record.id,method,record.subject,source and copyProvenance(source) or {})
 if not ok then return false,why end
 for i,value in ipairs(personal(worker).tuition) do if value==record then table.remove(personal(worker).tuition,i);break end end
 return true
end
function E.finalize(world,context)
 if not E.enabled(context) then return end
 local attendance=context._educationAttendance or {};local schools={}
 for slot=1,world.cols*world.rows do local s=world.structures[slot];if school(s) and s.education.session then schools[#schools+1]=s end end
 table.sort(schools,function(a,b)return a.education.session.id<b.education.session.id end)
 for _,structure in ipairs(schools) do
  local data,session=structure.education,structure.education.session;local marker=data.id..':'..session.id;local present=attendance[marker] or {}
  if sessionCurrent(world,structure,session) then
   local teacher=session.teacherPersonId>0 and workerByPerson(world,session.teacherPersonId) or nil
   local learner=session.learnerPersonId>0 and workerByPerson(world,session.learnerPersonId) or nil
   local recorder=session.recorderPersonId>0 and workerByPerson(world,session.recorderPersonId) or nil
   if session.mode=='record' and recorder and present.recorder==recorder.personId and actionAvailable(recorder,world.tick) and knownEarlier(recorder,session.topicId,world.tick) and not recordFor(structure,session.topicId,session.topicVersion) then
    local draft=data.draft
    if not draft then draft={topicId=session.topicId,topicVersion=session.topicVersion,subject=U.deep(sourceFor(recorder,session.topicId)),progress=0,firstTick=world.tick,lastTick=world.tick,contributors={},provenance=copyProvenance(fact(recorder,session.topicId))};data.draft=draft end
    if draft.topicId==session.topicId and draft.topicVersion==session.topicVersion then
     markAction(recorder,world.tick);draft.progress=draft.progress+1;draft.lastTick=world.tick;contributor(draft.contributors,{kind='person',id=recorder.personId,firstTick=world.tick,lastTick=world.tick})
     if draft.progress>=E.recordWork then
      local id=data.nextRecordId;data.nextRecordId=id+1;data.records[#data.records+1]={id=id,topicId=draft.topicId,topicVersion=draft.topicVersion,subject=U.deep(draft.subject),tick=world.tick,contributors=U.deep(draft.contributors),provenance=U.deep(draft.provenance)};data.draft=nil;W.event(world,'school_record',recorder.name..' completed a field school record.',structure.id);E.releaseSession(world,structure)
     end
    end
   elseif (session.mode=='teach' or session.mode=='study') and world.tick>0 and world.tick%10==0 and session.lastEvaluationTick~=world.tick then
    local required=session.mode=='teach' and teacher and learner and present.teacher==teacher.personId and present.learner==learner.personId or learner and present.learner==learner.personId
    if required and actionAvailable(learner,world.tick) and (not teacher or actionAvailable(teacher,world.tick)) and validLearner(learner,session.topicId) then
     local source=session.mode=='teach' and fact(teacher,session.topicId) or recordFor(structure,session.topicId,session.topicVersion)
     if source and (session.mode~='teach' or knownEarlier(teacher,session.topicId,world.tick)) then
      local rate=session.mode=='teach' and 2*(1+math.floor(personal(teacher).teachingXP/100)) or 1+math.floor(personal(learner).fieldworkXP/100)
      local existing=tuition(learner,session.topicId,session.topicVersion,source.subject)
      local current=existing and existing.progress or 0
      if current+rate>=E.tuitionWork and #learner.frontier.knowledge.facts>=Knowledge.maxFacts then
       learner.reason='Personal fact limit reached'
      else
      local ok,record,added=addTuition(context,learner,session.topicId,source.subject,session.mode,rate)
      if ok then
       markAction(learner,world.tick);personal(learner).fieldworkXP=math.min(400,personal(learner).fieldworkXP+1)
       if teacher then markAction(teacher,world.tick);personal(teacher).teachingXP=math.min(400,personal(teacher).teachingXP+1) end
       contributor(record.contributors,{kind=session.mode=='teach' and 'person' or 'record',id=session.mode=='teach' and teacher.personId or source.id,firstTick=world.tick,lastTick=world.tick})
       session.lastEvaluationTick=world.tick
       if record.progress>=E.tuitionWork then
        local complete,why=finishTuition(context,learner,record,source,session.mode);if complete then W.event(world,'school_learning',learner.name..' completed field school learning.',structure.id);E.releaseSession(world,structure) else learner.reason=why end
       end
      end
      end
     end
    end
   end
  else E.releaseSession(world,structure) end
 end
 context._educationAttendance=nil
end
function E.analysis(context,worker)
 if not E.enabled(context) then return 1 end
 local state=personal(worker);if state.lastLearningActionTick==context.campaign.tick then return nil,'Already performed learning work this tick' end
 return 1+math.floor(state.fieldworkXP/100)
end
function E.finishAnalysis(context,worker)
 if not E.enabled(context) then return end
 local state=personal(worker);state.lastLearningActionTick=context.campaign.tick;state.fieldworkXP=math.min(400,state.fieldworkXP+1)
end
return E
