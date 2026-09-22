-- MOCK UI evidence only.  This checks G07's normal Region, crew, expedition,
-- and regulator presentation without claiming native LÖVE or human play.
local directory=arg and arg[1] or '/tmp/cosmonauts-g07-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
local mock=require('tests.love_mock').install(directory)
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Structures=require('src.structures')
local World=require('src.world')
local Labor=require('src.labor')
local M=require('src.materials')
local F=require('tests.fixtures')
local main=require('main');local app=main.app
local function hasText(needle)
 for _,record in ipairs(mock.records) do if record.kind=='text' and record.text:find(needle,1,true) then return true end end
 return false
end
local function clear(w,gx,gy,width)
 local x1,y1,_,y2=World.rect(gx,gy);local x2=(gx+(width or 1)-1)*4
 F.fill(w,x1,3,x2,y1-1,M.AIR);F.fill(w,x1,y1,x2,y2,M.AIR);F.fill(w,x1,y2+1,x2,y2+1,M.ROCK)
end

love.load()
local c=Campaign.newRegion(10777,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,
 logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true,factions=true,security=true,environments=true})
local w=c.sites[1].world;clear(w,13,9,1);local regulator=Structures.install(w,13,9,'environmental_regulator');w.workers[1].x,w.workers[1].y=46,36;regulator._powerGranted=true;regulator.status='Protecting thermal/radiation radius 24'
-- Controlled UI fixture: an owned, instantiated frontier settlement has a
-- different environment, a real moved campaign person, and a different local
-- security policy.  This makes a header click prove the UI reads the selected
-- site rather than carrying Home's panel model into a fourth destination.
local frontier=Campaign.instantiate(c,4);c.sites[4].ownerSocietyId=1
local traveller=table.remove(w.workers,1);w.labor=Labor.default(w);w.laborAssignments=Labor.allocate(w)
traveller.id=World.id(frontier);traveller.x,traveller.y=frontier.home.x,frontier.home.y;traveller.environment.atmosphere=42;frontier.workers[#frontier.workers+1]=traveller;frontier.labor=Labor.default(frontier);frontier.laborAssignments=Labor.allocate(frontier)
w.security.posture='alert';frontier.security.posture='lockdown'
app.history=History.new(c);app.campaign=true;app.siteId=1;app.help=nil;app.newRun=nil;app.crew=nil;app.fieldnotes=nil;app.school=nil;app.paused=false;app.accumulator=0;app.region=true
mock.record=true;mock.records={};love.draw()
assert(hasText('REGION / LOCAL FRONTIER') and hasText('Home Planet'),'Region did not render environment frontier')
assert(hasText('gravity') and hasText('vacuum'),'Region omitted coarse environment summaries')
local leaked=false;for _,record in ipairs(mock.records) do if record.kind=='text' and (record.text:find('ore ',1,true) or record.text:find('water pocket',1,true)) then leaked=true end end
assert(not leaked,'Region leaked fine-cell resource information')
local tick=app.history.live.tick;for _=1,12 do love.update(1/60);love.draw() end;assert(app.history.live.tick>tick and not app.paused,'Region overlay paused live simulation')

app.region=nil;love.keypressed('h');love.keypressed('tab');love.keypressed('tab');love.keypressed('tab');assert(app.crew and app.crew.section=='security','Environment-capable Security crew page was not reachable')
mock.records={};love.draw();assert(hasText('exposure') and hasText('suit off'),'Crew page omitted bounded exposure/suit state')

-- Header-site selection is a normal live UI action, not a test-only setter.
-- It must switch all local data to the owned frontier world without pausing.
app.crew=nil;mock.records={};love.draw();local button;for _,candidate in ipairs(app.siteButtons) do if candidate.siteId==4 then button=candidate end end;assert(button,'Seven-site header omitted owned frontier settlement')
local tickBeforeSwitch=app.history.live.tick;love.mousepressed(button.x+1,button.y+1,1);assert(app.siteId==4 and app.currentWorld()==app.history.live.sites[4].world,'Header site switch did not select the frontier world');assert(not app.paused,'Site switching paused live simulation')
love.keypressed('h');love.keypressed('tab');love.keypressed('tab');love.keypressed('tab');mock.records={};love.draw();assert(hasText('exposure 42'),'Selected frontier Crew/Security panel leaked Home person data');assert(app.history.live.sites[4].world.security.posture=='lockdown' and app.currentWorld().security.posture=='lockdown','Selected frontier Security policy leaked another site state')
for _=1,12 do love.update(1/60);love.draw() end;assert(app.history.live.tick>tickBeforeSwitch and not app.paused,'Site-switch Security UI paused live simulation')
app.crew=nil;app.siteId=1

local person=app.history.live.sites[1].world.workers[1].personId
app.expedition={sourceSiteId=1,craftId=1,destinationSiteId=4,passengers={person},cargo={food=1,metal=1},buttons={}}
mock.records={};love.draw();assert(hasText('Conditions:') and hasText('Hazard: vacuum'),'Expedition preparation omitted non-breathable destination warning')
app.expedition=nil;app.selectedCell={x=regulator.gx*4-2,y=regulator.gy*4-2};mock.records={};love.draw();assert(hasText('protection radius 24'),'Regulator inspector omitted operational radius/status')
love.quit()
print('PASS G07-O MOCK UI: seven-body Region summaries, fog-safe information discipline, live simulation, selected-site Security/Crew state, suit/exposure crew state, expedition hazard warning, and regulator status.')
print('NOTE: mocked LÖVE graphics/input/storage only; native rendering and human gameplay remain untested.')
