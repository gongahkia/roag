-- COS-G04 focused deterministic industry contracts. These tests use compact
-- prepared terrain only; production campaigns still construct every machine
-- through ordinary Build jobs.
local Campaign=require('src.campaign')
local W=require('src.world')
local M=require('src.materials')
local S=require('src.structures')
local I=require('src.industry')
local V=require('src.visibility')
local Codec=require('src.campaign_codec')
local F=require('tests.fixtures')

local T={}
local function options(industry)
 return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=industry==true}
end
local function campaign(seed) return Campaign.newRegion(seed or 10401,options(true)) end
local function group(r,name,fn) local ok,e=pcall(fn);assert(ok,name..' FAILED: '..tostring(e));r.groups=r.groups+1;print('PASS  '..name) end
local function site(c) return c.sites[1].world end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=W.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,y1,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
 F.fill(w,x1,3,x2,y1-1,M.AIR)
end
local function install(w,gx,gy,kind) clear(w,gx,gy,S.width(kind));return S.install(w,gx,gy,kind) end
local function step(c,n)
 local w=site(c);for _=1,n do c.tick=c.tick+1;w.tick=c.tick;I.step(w,{campaign=c,siteId=1}) end
end
function T.run()
 local r={groups=0,assertions=0};local function check(v,s) r.assertions=r.assertions+1;assert(v,s) end;local function eq(a,b,s) check(a==b,(s or 'Mismatch')..': '..tostring(a)..' vs '..tostring(b)) end
 group(r,'G04-A feature boundary and legacy compatibility',function()
  local c=campaign();check(c.features.industry==1 and site(c).frontier.industry==1,'New frontier lacks industry feature')
  local free=false;for _,s in pairs(site(c).structures) do free=free or S.def[s.kind].industry end
  check(site(c).industry and not free and W.totalResource(site(c),'component')==0,'New frontier received free industry')
  local old=Campaign.new(F.world('practice'),{body=true,visibility=true,equipment=true,safe_excavation=true})
  check(old.features.industry==nil and old.sites[1].world.industry==nil,'Industry leaked into feature-off campaign')
 end)
 group(r,'G04-B manual Tool Bench bootstrap uses real worker actions',function()
  local raw=F.world('practice');F.worker(raw,12,24,'Bootstrapper')
  local c=Campaign.new(raw,{body=true,visibility=true,equipment=true,safe_excavation=true,industry=true});local w=site(c);local gx,gy=5,6
  clear(w,gx,gy,1);local bench=S.install(w,gx,gy,'tool_bench');W.stack(w,'metal',2,12,24)
  local Commands=require('src.campaign_commands');local ok,why=Commands.apply(c,{scope='site',siteId=1,payload={type='fabricate',slot=W.slot(w,gx,gy),kind='component',priority=1}});check(ok,why)
  for _=1,800 do Campaign.step(c) end
  eq(W.totalResource(w,'component'),1,'Manual recipe did not create one physical component');eq(W.totalResource(w,'metal'),0,'Manual recipe did not consume exactly two metal');check(bench.fabrication==nil,'Completed manual recipe retained live fabrication state')
 end)
 group(r,'G04-B/C/D/E deterministic topology, exposure, battery and allocation',function()
  local c=campaign();local w=site(c)
  local solar=install(w,6,6,'solar_array');install(w,8,6,'power_pole');local battery=install(w,9,6,'battery');local lamp=install(w,10,6,'electric_lamp')
  step(c,1);local top=I.topology(w);check(top.byStructure[solar.id]==top.byStructure[lamp.id],'Covered structures did not share a pole network')
  eq(top.networks[1].generation,6,'Two exposed solar halves must generate six')
  check(lamp._powerGranted,'Priority-one lamp did not receive all-or-nothing power');check(battery.charge>0 and battery.charge<=6,'Battery did not take bounded leftover solar')
  F.fill(w,(solar.gx-1)*4+2,3,(solar.gx-1)*4+2,3,M.ROCK);step(c,1);eq(I.topology(w).networks[1].generation,3,'Roofing one panel half did not reduce solar generation')
 end)
 group(r,'G04-F Fabricator consumes once, pauses without power, and produces physical output',function()
  local c=campaign();local w=site(c);local solar=install(w,6,6,'solar_array');install(w,8,6,'power_pole');local fab=install(w,9,6,'fabricator')
  I.configure(w,fab,{recipe='component'});check(I.addCargo(fab.input,{kind='metal',n=2},16),'Could not load physical fabricator input')
  step(c,20);eq(fab.progress,20,'Fabricator did not take one productive powered tick each campaign tick')
  F.fill(w,(solar.gx-1)*4+2,3,solar.gx*4+2,3,M.ROCK);step(c,3);eq(fab.progress,20,'Unpowered fabricator progressed')
  F.fill(w,(solar.gx-1)*4+2,3,solar.gx*4+2,3,M.AIR);step(c,20);eq(I.countCargo(fab.output,'component'),1,'Fabricator did not commit one component output');eq(I.countCargo(fab.input,'metal'),0,'Fabricator duplicated input')
 end)
 group(r,'G04-G/H/I rig designation and one-segment deterministic logistics',function()
  local c=campaign();local w=site(c);install(w,6,6,'solar_array');install(w,8,6,'power_pole');local rig=install(w,9,6,'mining_rig')
  local _,_,x2,y2=S.footprint(rig);local x,y=x2+1,y2+1;W.put(w,x,y,M.ROCK);local j={id=W.id(w),kind='dig',gx=math.floor((x-1)/4)+1,gy=math.floor((y-1)/4)+1,state='open',priority=1,delivered=0,progress=0,reason=''};w.jobs[#w.jobs+1]=j
  step(c,12);check(I.countCargo(rig.output,'stone')>=1,'Rig did not turn its real designated face into physical output')
  local belt=install(w,11,6,'conveyor');belt.direction='east';local bin=install(w,12,6,'industrial_bin');bin.mode='receive'
  I.addCargo(rig.output,{kind='metal',n=1},16);if c.tick%2~=0 then step(c,1) end;step(c,1);check(I.capacity(belt.cargo)<=8 and I.capacity(bin.cargo)<=32,'Conveyor/bin capacity exceeded')
  I.addCargo(belt.cargo,{kind='metal',n=1},8);local before=I.countCargo(bin.cargo,'metal');step(c,2);check(I.countCargo(bin.cargo,'metal')>=before,'Conveyor failed to commit compatible cargo')
 end)
 group(r,'G04-J/K wear, maintenance boundary, powered lamp and visibility source',function()
  local c=campaign();local w=site(c);install(w,6,6,'solar_array');install(w,8,6,'power_pole');local fab=install(w,9,6,'fabricator');install(w,11,6,'electric_lamp')
  I.configure(w,fab,{recipe='component'});I.addCargo(fab.input,{kind='metal',n=2},16);fab.wear=599;step(c,1);eq(fab.wear,600,'600th productive tick did not complete before maintenance block');check(I.needsMaintenance(fab),'Machine did not require maintenance after wear 600')
  I.completeMaintenance(w,fab);eq(fab.wear,0,'Maintenance did not reset wear')
  local sources=I.lightSources(w);check(#sources==1 and sources[1].radius==18,'Powered lamp did not expose G01-compatible light source')
  check(not V.currentlyVisible(w,1,1,{campaign=c,siteId=1}),'Lamp invented observer visibility')
 end)
 group(r,'G04-L/M/P/Q physical cargo schema, history clone and validation',function()
  local c=campaign();local w=site(c);local fab=install(w,6,6,'fabricator');I.configure(w,fab,{recipe='component'});I.addCargo(fab.input,{kind='metal',n=2},16);fab.progress=17
  local text=Codec.encode(c);local restored=Codec.decode(text);Campaign.validate(restored);eq(restored.sites[1].world.structures[W.slot(w,6,6)].progress,17,'Partial industrial cycle did not persist')
  check(require('src.logistics').resources()[3]=='component','Machine components are not shuttle cargo resources')
  local bad=Campaign.clone(c);bad.sites[1].world.structures[W.slot(w,6,6)].input[1].n=17;local ok=pcall(Campaign.validate,bad);check(not ok,'Oversized industrial buffer loaded')
 end)
 print(r.groups..' G04 groups; '..r.assertions..' assertions passed.');return r
end
return T
