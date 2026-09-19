local U=require('src.util')
local W=require('src.world')
local M=require('src.materials')
local G=require('src.generate')
local Sim=require('src.sim')
local J=require('src.jobs')
local Content=require('src.content')
local Codec=require('src.codec')
local Campaign=require('src.campaign')
local Command=require('src.campaign_commands')
local History=require('src.campaign_history')
local CampaignCodec=require('src.campaign_codec')
local Store=require('src.storage')
local F=require('tests.fixtures')
local T={}

local function campaignWorld(mode)
 local world=F.world(mode or 'challenge')
 F.worker(world,10,24,'Campaign tester')
 F.baseline(world)
 return world
end

local function envelope(payload)
 return {scope='site',siteId=1,payload=payload}
end

local function compareLocal(world)
 local copy=U.deep(world)
 copy.frontier=nil
 for _,worker in ipairs(copy.workers) do worker.personId=nil end
 return copy
end

function T.run()
 local report={groups=0,assertions=0}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'Campaign assertion failed') end
 local function eq(a,b,why) check(a==b,(why or 'Campaign mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function reject(fn,why) check(not pcall(fn),why or 'Expected campaign rejection') end
 local function group(name,fn)
  local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err))
  report.groups=report.groups+1;print('PASS  '..name)
 end

 group('P01-A legacy local histories and fixtures retain their compatibility boundary',function()
  local f=assert(io.open('tests/data/legacy-v020.dw','rb'));local text=f:read('*a');f:close()
  local legacy=require('src.history').fromText(text)
  check(legacy.live.frontier==nil,'Campaign metadata leaked into legacy fixture')
  check(legacy.live.workers[1].personId==nil,'Campaign person ID leaked into legacy fixture')
  check(W.validate(legacy.live),'Legacy world validator changed')
  local expected=assert(io.open('tests/data/legacy-v020-expected.txt','rb'));local hash=expected:read('*a'):gsub('%s+$','');expected:close()
  for _=1,80 do legacy:advance() end
  eq(Codec.hash(legacy.live),hash,'Legacy canonical trace changed')
 end)

 group('P01-B equivalent one-site and local traces match after only campaign projection removal',function()
  local source=campaignWorld('practice');local localWorld=U.deep(source);local campaign=Campaign.new(source)
  check(source.frontier==nil and source.workers[1].personId==nil,'Campaign initialization mutated caller world')
  local scripted={
   [1]={{type='paint',x=12,y=12,material=M.WATER}},
   [400]={{type='paint',x=12,y=12,material=M.AIR}},
   [900]={{type='paint',x=16,y=12,material=M.SAND}},
   [1500]={{type='paint',x=16,y=12,material=M.AIR}},
  }
  local history=History.new(campaign)
  for tick=1,2000 do
   local localCommands=scripted[tick]
   if localCommands then check(history:queue(envelope(localCommands[1]))) end
   Sim.step(localWorld,localCommands)
   check(history:advance())
  end
  eq(Codec.encode(localWorld),Codec.encode(compareLocal(history.live.sites[1].world)),'One-site campaign changed local simulation')
 end)

 group('P01-C campaign save, load, checkpoint seek and explicit replay verification agree',function()
  local history=History.new(Campaign.new(campaignWorld('practice')))
  local snapshots={}
  for tick=1,315 do
   if tick==1 then check(history:queue(envelope({type='paint',x=12,y=12,material=M.WATER}))) end
   if tick==121 then check(history:queue(envelope({type='paint',x=12,y=12,material=M.AIR}))) end
   check(history:advance())
   if tick==37 or tick==211 then snapshots[tick]=CampaignCodec.encode(history.live) end
  end
  check(history:queue(envelope({type='paint',x=16,y=12,material=M.SAND})))
  local restored=History.fromText(history:saveText())
  eq(CampaignCodec.encode(restored.live),CampaignCodec.encode(history.live),'Campaign load changed live state')
  for _,tick in ipairs({37,211}) do
   restored:seek(tick);while restored.seekTarget do restored:updateSeek(13) end
   eq(CampaignCodec.encode(restored.view),snapshots[tick],'Campaign checkpoint reconstruction')
   restored:seek(restored.frontier)
  end
  local valid,why=restored:verifyReplay(1000);eq(valid,true,why)
  for _=1,120 do check(history:advance());check(restored:advance()) end
  eq(CampaignCodec.encode(restored.live),CampaignCodec.encode(history.live),'Campaign continuation changed')
 end)

 group('P01-D site envelopes execute once and invalid routing is inert',function()
  local history=History.new(Campaign.new(campaignWorld('practice')))
  local before=CampaignCodec.encode(history.live)
  check(history:queue(envelope({type='paint',x=12,y=12,material=M.WATER})))
  check(history:advance());eq(history.live.tick,1);eq(history.live.sites[1].world.ledger.waterMade,1)
  check(history:advance());eq(history.live.tick,2);eq(history.live.sites[1].world.ledger.waterMade,1,'Command ran twice')
  local after=CampaignCodec.encode(history.live)
  local ok=history:queue({scope='site',siteId=2,payload={type='paint',x=12,y=12,material=M.AIR}});check(not ok)
  ok=history:queue({scope='site',siteId=1,payload={type='unknown'}});check(not ok)
  eq(CampaignCodec.encode(history.live),after,'Rejected routing mutated live campaign')
  local unsupported=Campaign.clone(history.live);unsupported.features.core=2
  reject(function() Campaign.validate(unsupported) end,'Unknown campaign feature accepted')
  unsupported=Campaign.clone(history.live);unsupported.version=2
  reject(function() Campaign.validate(unsupported) end,'Unknown campaign version accepted')
  check(before~=after,'Expected executed command to alter baseline comparison')
 end)

 group('P01-E challenge archives lock and practice branches as one timeline',function()
  local challenge=History.new(Campaign.new(campaignWorld('challenge')))
  for _=1,30 do challenge:advance() end
  local live=CampaignCodec.encode(challenge.live);challenge:seek(10);while challenge.seekTarget do challenge:updateSeek(9) end
  check(not challenge:queue(envelope({type='order',kind='dig',gx=6,gy=6})))
  eq(CampaignCodec.encode(challenge.live),live,'Challenge archive mutated live campaign')

  local practice=History.new(Campaign.new(campaignWorld('practice')))
  check(practice:queue(envelope({type='order',kind='dig',gx=6,gy=6})))
  for _=1,20 do practice:advance() end
  check(practice:queue(envelope({type='order',kind='dig',gx=7,gy=6})))
  for _=1,10 do practice:advance() end
  local futureId=practice.live.sites[1].world.nextId
  practice:seek(10);while practice.seekTarget do practice:updateSeek(9) end
  local branchId=practice.view.sites[1].world.nextId
  check(practice:queue(envelope({type='order',kind='dig',gx=8,gy=6})))
  eq(practice.frontier,10);check(practice.commands[21]==nil,'Practice future commands survived branch')
  eq(practice.live.sites[1].world.nextId,branchId,'Future IDs leaked into branch')
  check(branchId<=futureId)
 end)

 group('P01-F campaign clones do not alias worker, job, item or content state',function()
  local world=campaignWorld('practice');W.stack(world,'stone',4,12,24)
  local job=J.add(world,'build',6,6,'ladder',2)
  Content.install(world,{version=1,crew=3,flora={{kind='veil',x=20,y=24,food=3,water=1,phase=0}},fauna={},sites={},ruins={}})
  local campaign=Campaign.new(world);local clone=Campaign.clone(campaign);local original=campaign.sites[1].world
  clone.sites[1].world.workers[1].hunger=77
  clone.sites[1].world.jobs[1].priority=3
  clone.sites[1].world.items[1].n=99
  clone.sites[1].world.content.flora[1].food=12
  check(original.workers[1].hunger~=77 and original.jobs[1].priority~=3)
  check(original.items[1].n~=99 and original.content.flora[1].food~=12)
  check(job.id==original.jobs[1].id)
 end)

 group('P01-G campaign validation rejects duplicate IDs, holes, nonfinite values and tick mismatch',function()
  local source=campaignWorld();F.worker(source,13,24,'Second campaign tester');local campaign=Campaign.new(source)
  local duplicatePerson=Campaign.clone(campaign);duplicatePerson.sites[1].world.workers[1].personId=duplicatePerson.sites[1].world.workers[2].personId
  reject(function() Campaign.validate(duplicatePerson) end)
  local duplicateSite=Campaign.clone(campaign);duplicateSite.sites[2]=U.deep(duplicateSite.sites[1]);duplicateSite.sites[2].id=2
  reject(function() Campaign.validate(duplicateSite) end)
  local hole=Campaign.clone(campaign);hole.sites[2]=hole.sites[1];hole.sites[1]=nil
  reject(function() Campaign.validate(hole) end)
  local nonfinite=Campaign.clone(campaign);nonfinite.tick=0/0
  reject(function() Campaign.validate(nonfinite) end)
  local mismatch=Campaign.clone(campaign);mismatch.sites[1].world.tick=1
  reject(function() Campaign.validate(mismatch) end)
  local counter=Campaign.clone(campaign);counter.sites[1].world.nextId=1
  reject(function() Campaign.validate(counter) end,'Allocated local ID counter accepted')
  local resumed=campaignWorld('practice');Sim.step(resumed);local resumedHistory=History.new(Campaign.new(resumed));local badHistory=resumedHistory:bundle()
  badHistory.commands[1]={envelope({type='paint',x=12,y=12,material=M.WATER})}
  reject(function() History.restore(badHistory) end,'Command before campaign initial state accepted')
 end)

 group('P01-H validation and derived campaign inspection are non-mutating',function()
  local campaign=Campaign.new(campaignWorld());local before=CampaignCodec.encode(campaign)
  for _=1,10 do check(Campaign.validate(campaign));check(Campaign.site(campaign,1));check(not Campaign.extinct(campaign)) end
  eq(CampaignCodec.encode(campaign),before,'Campaign inspection mutated canonical state')
 end)

 group('P01-I campaign save replacement is isolated and failure preserves prior bytes',function()
  local directory='/tmp/cosmonauts-p01-campaign-storage'
  os.remove(directory..'/campaign.run.dat');os.remove(directory..'/campaign.run.tmp');os.remove(directory..'/run.dat')
  require('tests.love_mock').install(directory)
  assert(love.filesystem.write('run.dat','legacy sentinel'))
  local history=History.new(Campaign.new(campaignWorld()))
  local saved,why=Store.saveCampaign(history);check(saved,why)
  local original=assert(love.filesystem.read('campaign.run.dat'));local legacy=assert(love.filesystem.read('run.dat'))
  local write=love.filesystem.write;love.filesystem.write=function() return false,'injected campaign write failure' end
  saved=Store.saveCampaign(history);check(not saved,'Injected campaign write failure passed')
  love.filesystem.write=write
  eq(assert(love.filesystem.read('campaign.run.dat')),original,'Failed campaign save replaced prior bytes')
  eq(assert(love.filesystem.read('run.dat')),legacy,'Campaign save touched legacy save')
  saved=Store.saveCampaign({saveText=function() return string.rep('x',CampaignCodec.limit+1) end})
  check(not saved,'Oversize campaign save passed')
  eq(assert(love.filesystem.read('campaign.run.dat')),original,'Oversize campaign save replaced prior bytes')
  local rename=os.rename;os.rename=function() return nil,'injected campaign rename failure' end
  saved=Store.saveCampaign(history);os.rename=rename
  check(not saved,'Injected campaign rename failure passed')
  eq(assert(love.filesystem.read('campaign.run.dat')),original,'Rename failure replaced prior bytes')
  check(history:advance())
  saved=Store.saveCampaign(history);check(saved,'Campaign replacement save failed')
  check(assert(love.filesystem.read('campaign.run.dat'))~=original,'Successful campaign replacement kept old bytes')
  eq(assert(love.filesystem.read('run.dat')),legacy,'Campaign replacement touched legacy save')
  check(Store.loadCampaign()~=nil,'Saved campaign did not load')
 end)

 group('P01-J checkpoints remain bounded and old seeks rebuild from the campaign initial state',function()
  local history=History.new(Campaign.new(campaignWorld('practice')));local reference={}
  for tick=1,2200 do
   check(history:advance())
   if tick==37 then reference[tick]=CampaignCodec.encode(history.live) end
  end
  check(#history.checkpoints<=8,'Campaign checkpoint cap exceeded')
  history:seek(37);while history.seekTarget do history:updateSeek(17) end
  eq(CampaignCodec.encode(history.view),reference[37],'Campaign seek before retained checkpoints diverged')
 end)

 print(report.groups..' campaign groups; '..report.assertions..' assertions passed.')
 return report
end

return T
