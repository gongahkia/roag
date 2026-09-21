-- COS-G01 focused contracts: versioned bodies, live visibility state and torch
-- accounting. Existing P01-P06 suites continue to cover their feature-off runs.
local W=require('src.world')
local M=require('src.materials')
local F=require('tests.fixtures')
local Body=require('src.body')
local Nav=require('src.nav')
local Structures=require('src.structures')
local Jobs=require('src.jobs')
local Sim=require('src.sim')
local Campaign=require('src.campaign')
local Command=require('src.campaign_commands')
local Visibility=require('src.visibility')
local Content=require('src.content')
local Ecology=require('src.ecology')
local T={}

local function modern()
 local w=F.world('practice');w.body=1;local a=F.worker(w,16,24,'A')
 Content.install(w,{version=1,crew=3,flora={},fauna={},sites={},ruins={}})
 local c=Campaign.new(w,{knowledge=true,body=true,visibility=true})
 return c,c.sites[1].world.workers[1]
end
local function step(c,n)
 for _=1,n do Campaign.step(c,{}) end
end
function T.run()
 local report={groups=0,assertions=0}
 local function check(ok,why) report.assertions=report.assertions+1;assert(ok,why or 'G01 assertion failed') end
 local function eq(a,b,why) check(a==b,(why or 'G01 mismatch')..': '..tostring(a)..' ~= '..tostring(b)) end
 local function reject(fn,why) check(not pcall(fn),why or 'Expected rejection') end
 local function group(name,fn) local ok,err=pcall(fn);assert(ok,name..' FAILED: '..tostring(err));report.groups=report.groups+1;print('PASS  '..name) end

 group('G01-A versioned 2x4 body geometry leaves 2x3 histories intact',function()
  local legacy=F.world('practice');local modernWorld=F.world('practice');modernWorld.body=1
  eq(Body.height(legacy),3);eq(Body.height(modernWorld),4);eq(Body.width(modernWorld),2)
  local x1,y1,x2,y2=Body.rect(modernWorld,20,24);eq(x1,20);eq(y1,21);eq(x2,21);eq(y2,24)
  F.fill(modernWorld,30,3,31,24,M.ROCK);F.fill(modernWorld,30,22,31,24,M.AIR)
  check(not Nav.occupy(modernWorld,30,24,true),'Three-high passage accepted a 2x4 settler')
  F.fill(modernWorld,30,21,31,24,M.AIR);check(Nav.occupy(modernWorld,30,24,true),'Four-high passage rejected a 2x4 settler')
  check(Body.contains(modernWorld,20,24,21,21));check(not Body.contains(modernWorld,20,24,21,20))
 end)

 group('G01-F/G/H FOV, light, remembered terrain and save validation are bounded',function()
  local c,a=modern();local w=c.sites[1].world;local context={campaign=c,siteId=1}
  local mask=Visibility.fov(w,16,21,8);check(mask[W.index(w,16,21)],'FOV omitted its origin')
  F.fill(w,19,18,19,24,M.ROCK);local blocked=Visibility.fov(w,16,21,8)
  check(blocked[W.index(w,19,21)],'Opaque boundary was not visible');check(not blocked[W.index(w,20,21)],'FOV leaked through an opaque wall')
  F.fill(w,19,18,19,24,M.AIR);local torch=Structures.install(w,6,6,'torch');Visibility.update(w,context)
  local tx,ty=torch.gx*4-2,torch.gy*4-2;check(Visibility.lightAt(w,tx+15,ty,context)>0,'Torch did not light its radius');check(Visibility.lightAt(w,tx+16,ty,context)>0,'Torch boundary lost light')
  check(Visibility.currentlyVisible(w,tx+10,ty,context),'Living observer did not gain torch-lit visibility')
  W.put(w,tx+10,ty,M.SOIL);Visibility.update(w,context);eq(Visibility.memoryAt(w,tx+10,ty),M.SOIL)
  w.structures[W.slot(w,torch.gx,torch.gy)]=nil;a.x,a.y=4,24;W.put(w,tx+10,ty,M.WATER);Visibility.update(w,context)
  eq(Visibility.memoryAt(w,tx+10,ty),M.SOIL,'Remembered terrain leaked an off-screen mutation')
  a.x,a.y=tx+10,24;Visibility.update(w,context);eq(Visibility.memoryAt(w,tx+10,ty),M.WATER,'Re-observation did not refresh memory')
  Campaign.validate(c);local copy=Campaign.clone(c);Campaign.validate(copy)
 end)

 group('G01-J/L spatial command gate and real torch material cost',function()
  local c,a=modern();local w=c.sites[1].world;local hidden={scope='site',siteId=1,payload={type='order',kind='build',build='torch',gx=14,gy=6,priority=2,worker=a.id}}
  local ok,why=Command.valid(c,hidden);check(not ok and tostring(why):match('not currently visible'),'Hidden spatial command leaked or passed')
  W.stack(w,'metal',1,18,24);local job=Jobs.add(w,'build',6,6,'torch',3)
  for _=1,240 do Sim.step(w) end
  check(job.state=='done' and Structures.torchCount(w)==1,'Torch did not finish through ordinary construction')
  eq(W.totalResource(w,'metal'),0,'Torch did not consume exactly one physical metal')
  check(not require('src.commands').valid(F.world('practice'),{type='order',kind='build',build='torch',gx=6,gy=6,priority=2}),'Legacy campaign accepted torch build')
 end)

 group('G01-K a dark ecological effect gives no P05 evidence; light enables later evidence',function()
  local c,a=modern();local w=c.sites[1].world;local plant={id=W.id(w),kind='filter',x=22,y=24,food=0,water=0,alive=true,phase=0}
  w.content.flora={plant};local context={campaign=c,siteId=1}
  c.tick=20;w.tick=20;W.put(w,23,24,M.STEAM);Visibility.update(w,context);Ecology.step(w,context)
  eq(#a.frontier.knowledge.observations,0,'Dark event created personal evidence')
  Structures.install(w,6,6,'torch');c.tick=40;w.tick=40;W.put(w,23,24,M.STEAM);Visibility.update(w,context);Ecology.step(w,context)
  check(#a.frontier.knowledge.observations==1,'Lit event did not create personal evidence')
  eq(a.frontier.knowledge.observations[1].effects[1].distinctTicks,1)
 end)

 group('G01-C explicit rules reject incompatible visibility/body combinations',function()
  local source=F.world('practice');F.worker(source,16,24)
  reject(function() Campaign.new(source,{visibility=true}) end,'Visibility silently enabled without body')
  local c=Campaign.new(source,{body=true,visibility=true});check(c.features.body==1 and c.features.visibility==1)
  c.features.body=nil;reject(function() Campaign.validate(c) end,'Visibility/body mismatch validated')
 end)
 print(report.groups..' G01 groups; '..report.assertions..' assertions passed.')
 return report
end
return T
