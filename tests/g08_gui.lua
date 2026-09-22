-- MOCK UI evidence only.  This verifies the ordinary G08 Region, cache,
-- Analyzer, and expedition surfaces without claiming native LÖVE rendering or
-- a human playtest.
local directory=arg and arg[1] or '/tmp/cosmonauts-g08-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
local mock=require('tests.love_mock').install(directory)
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Structures=require('src.structures')
local World=require('src.world')
local Labor=require('src.labor')
local F=require('tests.fixtures')
local M=require('src.materials')
local Relics=require('src.relics')
local main=require('main');local app=main.app

local function hasText(needle)
 for _,record in ipairs(mock.records) do if record.kind=='text' and record.text:find(needle,1,true) then return true end end
 return false
end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end
local function options()
 return {preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,
  logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true,relics=true}
end

love.load()
local c=Campaign.newRegion(10888,options());local remoteName=c.region.bodies[8].name
local w=c.sites[1].world
local frontier=Campaign.instantiate(c,4);c.sites[4].ownerSocietyId=c.society.id
local cache=Relics.cache(c,1)
local archaeologist=table.remove(w.workers,1);archaeologist.id=World.id(frontier);archaeologist.x,archaeologist.y=cache.gx*4-2,cache.gy*4-2;frontier.workers[#frontier.workers+1]=archaeologist
w.labor=Labor.default(w);w.laborAssignments=Labor.allocate(w);frontier.labor=Labor.default(frontier);frontier.laborAssignments=Labor.allocate(frontier)
clear(frontier,8,8,1);local analyzer=Structures.install(frontier,8,8,'relic_analyzer');analyzer._powerGranted=true
app.history=History.new(c);app.campaign=true;app.siteId=1;app.help=nil;app.newRun=nil;app.crew=nil;app.fieldnotes=nil;app.school=nil;app.expedition=nil;app.paused=false;app.accumulator=0;app.region=true

mock.record=true;mock.records={};love.draw()
assert(hasText('REGION / DEEP FRONTIER') and hasText('Deep-space systems: unknown'),'Hidden remote systems did not use the bounded pre-scan presentation')
assert(not hasText(remoteName),'Undetected remote body name leaked into Region UI')
local firstTick=app.history.live.tick;for _=1,12 do love.update(1/60);love.draw() end
assert(app.history.live.tick>firstTick and not app.paused,'Region UI paused live simulation')

-- The scan changes only reveal state.  It gives no local map/cache coordinate
-- and makes the existing Region surface show system grouping/coarse routes.
app.history.live.region.systems[2].revealed=true;app.history.view.region.systems[2].revealed=true
app.history.live.region.systems[3].revealed=true;app.history.view.region.systems[3].revealed=true
mock.records={};love.draw();assert(hasText(remoteName) and hasText('requires relic drive'),'Post-scan Region UI omitted remote grouping/deep-route requirement')
assert(not hasText('Ancient Cache'),'Region UI leaked hidden archaeology after scan')

-- Header switching is the normal selected-site path.  It must replace local
-- state rather than retaining Home's selected model, and still remain live.
app.region=nil;mock.records={};love.draw();local button
for _,candidate in ipairs(app.siteButtons or {}) do if candidate.siteId==4 then button=candidate end end
assert(button,'Owned frontier settlement was missing from normal site header')
love.mousepressed(button.x+1,button.y+1,1);assert(app.siteId==4 and app.currentWorld()==app.history.live.sites[4].world,'Normal header switch did not select owned ancient-site settlement')
app.selectedWorker=archaeologist.id;app.hud={x=40,y=110,cell={x=cache.gx*4-2,y=cache.gy*4-2}};mock.records={};love.draw();assert(hasText('Ancient Cache') and hasText('Excavate Ancient Cache'),'Visible cache omitted normal contextual excavation affordance')
for _,worker in ipairs(app.history.live.sites[4].world.workers) do if worker.personId==archaeologist.personId then worker.x,worker.y=analyzer.gx*4-2,analyzer.gy*4-2 end end
app.selectedCell={x=analyzer.gx*4-2,y=analyzer.gy*4-2};app.hud={x=40,y=110,cell={x=analyzer.gx*4-2,y=analyzer.gy*4-2}};mock.records={};love.draw();assert(hasText('Analyze loaded relic'),'Analyzer context omitted physical analysis action')
app.selectedWorker=nil;app.hud=nil;mock.records={};love.draw();assert(hasText('relic slots'),'Analyzer inspector omitted physical slot/power state')
local switchedTick=app.history.live.tick;for _=1,12 do love.update(1/60);love.draw() end
assert(app.history.live.tick>switchedTick and not app.paused,'Cache/Analyzer UI paused simulation')

-- The existing expedition UI owns the drive/compatibility warning; no second
-- spacecraft interface is created for G08.
app.siteId=1;local d=app.history.live.relics.drive;d.installed=true;d.craftId=1;d.siteId=1;d.sockets={1,2,3}
for _,id in ipairs({1,2,3}) do local relic=Relics.find(app.history.live,id);relic.state='drive';relic.cacheId=nil;relic.craftId=1;relic.socket=id;app.history.live.relics.caches[id].empty=true end
local person=w.workers[1].personId
app.expedition={sourceSiteId=1,craftId=1,destinationSiteId=8,passengers={person},cargo={food=1,metal=1,component=1},buttons={}}
mock.records={};love.draw();assert(hasText('Relic Drive Frame: fitted') and hasText('Deep-space route: needs') and hasText('Machine Component') and hasText('transit may be unstable'),'Expedition UI omitted G08 drive sockets/cost/uncertainty')
app.expedition=nil
love.quit()
print('PASS G08-U MOCK UI: hidden/revealed deep Region discipline, live selected-site switching, cache/analyzer contextual controls, and drive/instability expedition presentation.')
print('NOTE: mocked LÖVE graphics/input/storage only; native rendering and human gameplay remain untested.')
