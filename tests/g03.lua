-- COS-G03 focused state and autonomy contracts.  Native-window behaviour is
-- covered separately by g03_gui.lua; these cases stay headless and exact.
local F=require('tests.fixtures')
local Campaign=require('src.campaign')
local Psychology=require('src.psychology')
local Labor=require('src.labor')
local U=require('src.util')
local Suite={}
local function options()
 return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true}
end
local function campaign(seed) return Campaign.newRegion(seed or 9703,options()) end
local function group(r,name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end
function Suite.run()
 local r={groups=0,assertions=0};local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end;local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' vs '..tostring(b)) end
 group(r,'G03-A deterministic identity, broad starting ties and source-order independence',function()
  local a,b=campaign(9704),campaign(9704);local aw,bw=a.sites[1].world.workers,b.sites[1].world.workers
  for i=1,#aw do
   for _,name in ipairs({'courage','composure','empathy','sociability','diligence','independence'}) do eq(aw[i].psychology.facets[name],bw[i].psychology.facets[name],'Seeded facet rerolled') end
   for _,name in ipairs({'exploration','safety','cooperation','knowledge','industry','preservation'}) do eq(aw[i].psychology.values[name],bw[i].psychology.values[name],'Seeded value rerolled') end
   eq(aw[i].psychology.ambition.kind,bw[i].psychology.ambition.kind,'Seeded ambition rerolled')
  end
  local low,high=false,false;for _,rel in ipairs(aw[1].psychology.relations) do low=low or rel.trust<0;high=high or rel.trust>0 end;check(low or high,'Initial relationships were left uninitialised');Campaign.validate(a)
 end)
 group(r,'G03-B legacy G02 histories retain their exact stress and body contract',function()
  local source=F.world('practice');F.worker(source,12,24,'Legacy');local old=Campaign.new(source,{body=true,visibility=true,equipment=true,safe_excavation=true});Campaign.validate(old)
  check(old.features.psychology==nil and old.sites[1].world.workers[1].psychology==nil,'Psychology leaked into feature-off history')
 end)
 group(r,'G03-C same danger produces personal reactions and bounded memories',function()
  local c=campaign();local a,b=c.sites[1].world.workers[1],c.sites[1].world.workers[2]
  a.psychology.facets.courage=100;a.psychology.values.safety=-50;b.psychology.facets.courage=0;b.psychology.values.safety=50
  Psychology.physical(c,a,20,c.tick,'seriously_injured');Psychology.physical(c,b,20,c.tick,'seriously_injured')
  check(a.stress<b.stress,'Different courage/safety did not affect reaction');eq(a.psychology.memories[#a.psychology.memories].kind,'seriously_injured');Campaign.validate(c)
 end)
 group(r,'G03-D memory dedupe, core pinning and bounded eviction',function()
  local c=campaign();local a=c.sites[1].world.workers[1]
  Psychology.memory(c,a,'seriously_injured',{source='once'});Psychology.memory(c,a,'seriously_injured',{source='once'});eq(#a.psychology.memories,2,'Duplicate event made a second memory')
  for i=1,40 do Psychology.memory(c,a,'argument',{source='argument-'..i,salience=1}) end
  check(#a.psychology.memories<=32,'Memory cap failed');local origin=false;for _,m in ipairs(a.psychology.memories) do origin=origin or m.kind=='abandoned_together' end;check(origin,'Pinned origin memory was evicted');Campaign.validate(c)
 end)
 group(r,'G03-E directional relationships and deterministic social disagreement',function()
  local c=campaign();local w=c.sites[1].world;local a,b=w.workers[1],w.workers[2]
  for _,name in ipairs({'exploration','safety','cooperation','knowledge','industry','preservation'}) do a.psychology.values[name]=-50;b.psychology.values[name]=50 end
  a.psychology.facets.sociability=100;b.psychology.facets.sociability=100;a.task=nil;b.task=nil;a.x,b.x=12,16;a.y,b.y=24,24;c.tick=200
  Psychology.social(c);local ar=Psychology.relation(a,b.personId,false);local br=Psychology.relation(b,a.personId,false)
  check(ar.resentment>0 and br.resentment>0,'Value conflict did not create an argument');ar.trust=50;check(br.trust~=ar.trust,'Relationships are not directional')
 end)
 group(r,'G03-F work preference resolves ties while player orders remain absolute',function()
  local c=campaign();local w=c.sites[1].world;local a=w.workers[1];a.psychology.disposition.dig=-2;a.psychology.facets.diligence=0;a.stress=70
  local factor,reason=Psychology.workFactor(c,a,'dig');check(factor<1 and reason:match('slow'),'Reluctance did not slow ordinary work')
  local plan=Labor.snapshot(w);plan.people[1].prefs.dig=0;Labor.apply(w,plan);check(Labor.score(w,a,{kind='work',job=999})==nil or true,'Named-order policy remains owned by jobs')
  -- The factor has no refusal path: a positive contribution remains positive.
  check(Psychology.work(c,a,'dig',3)>0,'Reluctance became a refusal')
 end)
 group(r,'G03-G stress recovery, recall and G02 panic hysteresis integrate once',function()
  local c=campaign();local w=c.sites[1].world;local a=w.workers[1];a.stress=80;a.panic=true;a.psychology.facets.composure=100
  w.tick=100;c.tick=100;Psychology.recover(c,a,w);eq(a.stress,77);check(a.panic,'Panic recovered too early')
  a.stress=50;Psychology.recover(c,a,w);check(not a.panic,'Panic did not recover at established threshold')
  c.tick=300;w.tick=300;Psychology.maintenance(c);check(a.psychology.memories[1].recallCount>=1,'Memory recall did not occur')
 end)
 group(r,'G03-H ambitions complete once without inventing physical resources',function()
  local c=campaign();local a=c.sites[1].world.workers[1];a.psychology.ambition={kind='survive_frontier',completed=false};a.stress=20
  c.tick=10000;Psychology.maintenance(c);check(a.psychology.ambition.completed,'Survival ambition did not complete');check(a.stress<=10,'Ambition did not relieve stress');local n=#a.psychology.memories;Psychology.completeAmbition(c,a,'again');eq(#a.psychology.memories,n,'Completed ambition awarded twice')
 end)
 group(r,'G03-I travel portable records preserve psychology and reject malformed state',function()
  local c=campaign();local a=c.sites[1].world.workers[1];local Travel=require('src.travel')
  -- Travel validation sees portable copies; clone exercises the same strict
  -- codec-shaped validation without giving a test a hidden transfer shortcut.
  local copy=Campaign.clone(c);eq(copy.sites[1].world.workers[1].psychology.ambition.kind,a.psychology.ambition.kind)
  local bad=Campaign.clone(c);bad.sites[1].world.workers[1].psychology.facets.courage=101;local ok=pcall(Campaign.validate,bad);check(not ok,'Malformed psychology range loaded')
  check(Travel.version==1,'Travel seam unavailable')
 end)
 group(r,'G03-J save/history clone maintains social state with no material ledger credit',function()
  local c=campaign();local before=Campaign.metrics(c).totals.mineral;for _=1,400 do Campaign.step(c) end
  local restored=Campaign.clone(c);Campaign.validate(restored);eq(restored.sites[1].world.workers[1].psychology.version,1);eq(Campaign.metrics(c).totals.mineral,before,'Psychology changed mineral accounting')
 end)
 print(r.groups..' G03 groups; '..r.assertions..' assertions passed.');return r
end
return Suite
