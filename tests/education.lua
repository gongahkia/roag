-- COS-P06 contracts: physical field schools, personal tuition, and local records.
local F=require('tests.fixtures')
local W=require('src.world')
local S=require('src.structures')
local J=require('src.jobs')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Codec=require('src.campaign_codec')
local Commands=require('src.campaign_commands')
local Knowledge=require('src.knowledge')
local Education=require('src.education')
local T={}

local identify='identify/flora/filter/v1'
local function source(id)
 return {siteId=1,category='flora',id=id or 9001,kind='filter',definitionVersion=1}
end
local function advance(history,n)
 for _=1,n do assert(history:advance()) end
end
local function world(history) return history.live.sites[1].world end
local function school(history,slot) return world(history).structures[slot] end
local function newCampaign(people)
 local w=F.world('practice')
 for i=1,people or 3 do F.worker(w,8+(i-1)*7,24,string.char(64+i)) end
 return Campaign.new(w,{knowledge=true,education=true})
end
local function grantIdentify(c,index)
 local w=c.sites[1].world
 assert(Knowledge.survey({campaign=c,siteId=1},w.workers[index or 1],source()))
end
local function installSchool(c)
 local w=c.sites[1].world;local slot=W.slot(w,8,6)
 local structure=S.install(w,8,6,'field_school')
 return slot,structure
end
local function policy(history,slot,data,enabled,mode,topic,version,priority)
 assert(history:queue({scope='site',siteId=1,payload={type='school_policy',slot=slot,schoolId=data.id,
  expectedPolicyRevision=data.policyRevision,enabled=enabled,mode=mode,topicId=topic or false,topicVersion=version or 0,priority=priority or 3}}))
end
local function recordedSchool()
 local c=newCampaign(3);grantIdentify(c,1);local slot=installSchool(c);local h=History.new(c)
 policy(h,slot,school(h,slot).education,true,'record',identify,1,3);advance(h,420)
 local structure=school(h,slot);assert(#structure.education.records==1,'Recording did not complete through worker actions')
 return h,slot
end

function T.run()
 local report={groups=0,assertions=0}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'P06 assertion failed') end
 local function eq(a,b,why) check(a==b,(why or 'P06 mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function reject(fn,why) check(not pcall(fn),why or 'Expected rejection') end
 local function group(name,fn) local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err));report.groups=report.groups+1;print('PASS  '..name) end

 group('P06-A normal construction consumes stone and metal before installing an empty school',function()
  local c=newCampaign(1);local h=History.new(c);local w=world(h);W.stack(w,'stone',4,11,24);W.stack(w,'metal',2,12,24)
  assert(h:queue({scope='site',siteId=1,payload={type='order',kind='build',build='field_school',gx=8,gy=6,priority=3,worker=0}}))
  advance(h,800);local structure=school(h,W.slot(w,8,6));check(structure and structure.kind=='field_school','Build did not install a field school')
  eq(W.totalResource(world(h),'stone'),0,'School construction stone');eq(W.totalResource(world(h),'metal'),0,'School construction metal')
  check(not structure.education.enabled and #structure.education.records==0 and not structure.education.session,'New school was not empty and disabled')
  local old=Campaign.new(F.world('practice'),{knowledge=true})
  check(not Commands.valid(old,{scope='site',siteId=1,payload={type='order',kind='build',build='field_school',gx=8,gy=6,priority=2,worker=0}}),'Feature-off campaign accepted field school construction')
 end)

 group('P06-B policy needs a living local source and Field work remains ordinary work',function()
  local c=newCampaign(2);local slot,structure=installSchool(c);local h=History.new(c)
  check(not h:queue({scope='site',siteId=1,payload={type='school_policy',slot=slot,schoolId=structure.education.id,expectedPolicyRevision=1,enabled=true,mode='teach',topicId=identify,topicVersion=1,priority=3}}),'School accepted an unavailable catalog fact')
  grantIdentify(c,1);h=History.new(c);structure=school(h,slot);policy(h,slot,structure.education,true,'teach',identify,1,3)
  advance(h,1);structure=school(h,slot);check(structure.education.session~=nil,'Available teacher and learner did not form a pair')
  J.release(world(h),world(h).workers[1],true);check(not school(h,slot).education.session,'Teacher release did not release the pair')
 end)

 group('P06-C recording takes 120 attendance actions and leaves a local immutable record',function()
  local h,slot=recordedSchool();local structure=school(h,slot);local record=structure.education.records[1]
  eq(record.topicId,identify);check(record.tick>=120,'Recording completed before 120 work actions');eq(#record.contributors,1);check(not structure.education.draft and not structure.education.session,'Completed record left active recording state')
  advance(h,120);eq(#school(h,slot).education.records,1,'Duplicate record policy created another record')
 end)

 group('P06-D attended lessons reserve both people and release both on interruption',function()
  local c=newCampaign(2);grantIdentify(c,1);local slot=installSchool(c);local h=History.new(c);policy(h,slot,school(h,slot).education,true,'teach',identify,1,3)
  advance(h,25);local structure=school(h,slot);local session=assert(structure.education.session)
  local a,b=world(h).workers[1],world(h).workers[2];check(a.task and b.task and a.task.sessionId==session.id and b.task.sessionId==session.id,'Lesson did not reserve both participants')
  J.release(world(h),b,true);check(not a.task and not b.task and not school(h,slot).education.session,'One-side interruption left a reservation behind')
 end)

 group('P06-E pre-action XP arithmetic, ten-tick evaluation, and feature-off analysis remain distinct',function()
  local c=newCampaign(2);grantIdentify(c,1);local w=c.sites[1].world;w.workers[1].frontier.education.teachingXP=99;w.workers[2].frontier.education.fieldworkXP=99
  local slot=installSchool(c);local h=History.new(c);policy(h,slot,school(h,slot).education,true,'teach',identify,1,3)
  local tuition
  repeat advance(h,1);tuition=world(h).workers[2].frontier.education.tuition[1] until tuition
  w=world(h);eq(tuition.progress,2);eq(w.workers[1].frontier.education.teachingXP,100);eq(w.workers[2].frontier.education.fieldworkXP,100)
  local first=tuition.progress
  repeat advance(h,1);tuition=world(h).workers[2].frontier.education.tuition[1] until tuition.progress>first
  eq(tuition.progress,6,'Level gain changed the same action instead of the next action')
  local old=Campaign.new(F.world('practice'),{knowledge=true});eq(old.features.education,nil)
 end)

 group('P06-F record study works without a teacher and loss stops but does not erase earned tuition',function()
  local h,slot=recordedSchool();local structure=school(h,slot);policy(h,slot,structure.education,true,'study',identify,1,3)
  advance(h,50);local w=world(h);local learner=w.workers[2];local progress=assert(learner.frontier.education.tuition[1]).progress;check(progress>0,'Record study did not create personal progress')
  require('src.education').destroy(w,school(h,slot));w.structures[slot]=nil;advance(h,30)
  eq(learner.frontier.education.tuition[1].progress,progress,'Destroyed source advanced or erased tuition')
 end)

 group('P06-G communicated facts have their own method and do not copy eyewitness observations',function()
  local c=newCampaign(2);grantIdentify(c,1);local slot=installSchool(c);local h=History.new(c);policy(h,slot,school(h,slot).education,true,'teach',identify,1,3)
  advance(h,1300);local learner=world(h).workers[2];local learned=Knowledge.fact(learner,identify)
  check(learned and learned.method=='taught','Lesson did not create taught provenance');eq(#learner.frontier.knowledge.observations,0,'Lesson copied teacher eyewitness observations')
 end)

 group('P06-H portable education state survives campaign cloning without local school references',function()
  local c=newCampaign(2);grantIdentify(c,1);local slot=installSchool(c);local h=History.new(c);policy(h,slot,school(h,slot).education,true,'teach',identify,1,3)
  advance(h,60);local learner=world(h).workers[2];local progress=assert(learner.frontier.education.tuition[1]).progress
  local copied=Campaign.clone(h.live);local cloneLearner=copied.sites[1].world.workers[2]
  eq(cloneLearner.frontier.education.tuition[1].progress,progress);cloneLearner.frontier.education.tuition[1].progress=1
  eq(learner.frontier.education.tuition[1].progress,progress,'Portable education clone aliases live personal state')
 end)

 group('P06-I save, replay, seek, and branch retain partial school work',function()
  local h,slot=recordedSchool();local structure=school(h,slot);policy(h,slot,structure.education,true,'study',identify,1,3);advance(h,35)
  local saved=h:saveText();local restored=History.fromText(saved);advance(h,40);advance(restored,40);eq(Codec.encode(h.live),Codec.encode(restored.live),'School continuation diverged after save/load')
  local verified,why=History.fromText(saved):verifyReplay(2000);check(verified,why)
  restored:seek(restored.frontier-10);while restored.seekTarget do restored:updateSeek(20) end
  check(restored:queue({scope='site',siteId=1,payload={type='paint',x=8,y=8,material=0}}),'Practice branch rejected school history')
 end)

 group('P06-J education has no rendered-state dependency',function()
  local h,slot=recordedSchool();local structure=school(h,slot);policy(h,slot,structure.education,true,'study',identify,1,3)
  check(world(h).workers[2].frontier.education.tuition[1] == nil,'Selection state changed before simulation')
  for _=1,300 do advance(h,1);if world(h).workers[2].frontier.education.tuition[1] then break end end
  check(world(h).workers[2].frontier.education.tuition[1].progress>0,'Unviewed school did not progress')
 end)

 group('P06-K a missing pupil never creates a partial teacher reservation',function()
  local c=newCampaign(1);grantIdentify(c,1);local slot=installSchool(c);local h=History.new(c);policy(h,slot,school(h,slot).education,true,'teach',identify,1,3)
  advance(h,20);local a=world(h).workers[1];check(not a.task and not school(h,slot).education.session,'Teacher was reserved without an eligible pupil')
 end)

 group('P06-L stale policy revisions cannot control a rebuilt school',function()
  local c=newCampaign(2);grantIdentify(c,1);local slot,structure=installSchool(c);local h=History.new(c)
  policy(h,slot,structure.education,true,'teach',identify,1,3);policy(h,slot,structure.education,true,'record',identify,1,3);advance(h,1)
  local current=school(h,slot).education;eq(current.policyRevision,2,'Duplicate stale policy changed the school twice')
  local stale={scope='site',siteId=1,payload={type='school_policy',slot=slot,schoolId=current.id,expectedPolicyRevision=1,enabled=false,mode='record',topicId=false,topicVersion=0,priority=3}}
  check(not Commands.valid(h.live,stale),'Stale policy command revalidated as current')
 end)

 group('P06-M learner tuition follows that learner while school drafts remain local',function()
  local h,slot=recordedSchool();local structure=school(h,slot);policy(h,slot,structure.education,true,'study',identify,1,3);advance(h,50)
  local w=world(h);local b,c=w.workers[2],w.workers[3];local earned=assert(b.frontier.education.tuition[1]).progress
  J.release(w,b,true);b.directive={x=b.x,y=b.y};advance(h,20)
  local replacement=c.frontier.education.tuition[1]
  check(replacement and replacement~=b.frontier.education.tuition[1] and replacement.firstTick>b.frontier.education.tuition[1].firstTick,'Replacement learner inherited tuition')
  eq(b.frontier.education.tuition[1].progress,earned,'Interrupted learner lost earned tuition')
 end)

 group('P06-N same-tick guards prevent double education progress',function()
  local h,slot=recordedSchool();local structure=school(h,slot);policy(h,slot,structure.education,true,'study',identify,1,3)
  for _=1,300 do advance(h,1);if world(h).workers[2].frontier.education.tuition[1] then break end end
  local learner=world(h).workers[2];local progress=learner.frontier.education.tuition[1].progress
  require('src.education').finalize(world(h),{campaign=h.live,siteId=1,_educationAttendance={}})
  eq(learner.frontier.education.tuition[1].progress,progress,'Repeated finalizer granted same-tick tuition')
 end)

 group('P06-O strict bounds reject malformed expertise and duplicate records',function()
  local h,slot=recordedSchool();local bad=Campaign.clone(h.live);bad.sites[1].world.workers[1].frontier.education.fieldworkXP=401
  reject(function() Campaign.validate(bad) end,'Out-of-range expertise was accepted')
  bad=Campaign.clone(h.live);local data=bad.sites[1].world.structures[slot].education;data.records[2]=require('src.util').deep(data.records[1]);data.records[2].id=2;data.nextRecordId=3
  reject(function() Campaign.validate(bad) end,'Duplicate record topic was accepted')
 end)

 group('P06-P school UI is represented by a revision-bound local policy command',function()
  local c=newCampaign(2);grantIdentify(c,1);local slot,structure=installSchool(c)
  local command={scope='site',siteId=1,payload={type='school_policy',slot=slot,schoolId=structure.education.id,expectedPolicyRevision=1,enabled=true,mode='teach',topicId=identify,topicVersion=1,priority=3}}
  check(Commands.valid(c,command),'Inspector policy shape was not accepted')
 end)

 group('P06-Q a physical school records then teaches a real local fact',function()
  local h,slot=recordedSchool();local structure=school(h,slot);policy(h,slot,structure.education,true,'teach',identify,1,3);advance(h,1300)
  local a,b=world(h).workers[1],world(h).workers[2];check(Knowledge.fact(a,identify) and Knowledge.fact(b,identify),'Institution trace did not retain teacher and learner facts')
  eq(Knowledge.fact(b,identify).method,'taught')
 end)

 group('P06-R deterministic XP is per person and does not run for feature-off campaigns',function()
  local c=Campaign.newRegion(12345,{mode='practice',logistics=true,travel=true,knowledge=true,education=true})
  local people=c.sites[1].world.workers;eq(people[1].frontier.education.fieldworkXP,43);eq(people[1].frontier.education.teachingXP,98);eq(people[2].frontier.education.fieldworkXP,63);eq(people[2].frontier.education.teachingXP,126);eq(people[3].frontier.education.fieldworkXP,83);eq(people[3].frontier.education.teachingXP,101)
  local legacy=F.world('practice');F.worker(legacy,10,24);local old=Campaign.new(legacy,{knowledge=true});check(old.sites[1].world.workers[1].frontier.education==nil,'Feature-off campaign initialized expertise')
 end)

 group('P06-S record/session allocations remain bounded through repeated policy changes',function()
  local h,slot=recordedSchool();local structure=school(h,slot)
  for _=1,40 do
   policy(h,slot,school(h,slot).education,false,'record',identify,1,2);advance(h,1)
   policy(h,slot,school(h,slot).education,true,'study',identify,1,2);advance(h,1)
  end
  structure=school(h,slot);check(#structure.education.records<=Education.maxRecords and (not structure.education.session or structure.education.session.id<world(h).education.nextSessionId),'School policy churn exceeded a bounded allocation')
  Campaign.validate(h.live)
 end)

 print(report.groups..' education groups; '..report.assertions..' assertions passed.')
 return report
end
return T
