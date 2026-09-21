-- MOCK UI evidence only: no native LÖVE window or human input claim.
local directory=arg and arg[1] or '/tmp/cosmonauts-g04-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
require('tests.love_mock').install(directory)
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local W=require('src.world')
local M=require('src.materials')
local S=require('src.structures')
local Commands=require('src.campaign_commands')
local main=require('main');local app=main.app
love.load()
local c=Campaign.newRegion(10411,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true,industry=true})
local w=c.sites[1].world;local x1,y1,_,y2=W.rect(8,6);for y=3,y2 do for x=x1,x1+7 do W.put(w,x,y,M.AIR) end end;for x=x1,x1+7 do W.put(w,x,y2+1,M.ROCK) end
local fab=S.install(w,8,6,'fabricator')
app.history=History.new(c);app.campaign=true;app.siteId=1;app.help=nil;app.newRun=nil;app.crew=nil;app.fieldnotes=nil;app.school=nil;app.paused=false;app.accumulator=0
local ok,why=Commands.apply(c,{scope='site',siteId=1,payload={type='industry_config',structureId=fab.id,recipe='component'}})
assert(ok,why);assert(fab.recipe=='component','Fabricator recipe configuration did not bind by structure ID')
app.selectedCell={x=fab.gx*4-2,y=fab.gy*4-2};local start=app.history.live.tick
for _=1,12 do love.update(1/60);love.draw() end
assert(app.history.live.tick>start and not app.paused,'Industrial inspector/mock drawing paused live simulation')
local staleOk=Commands.apply(c,{scope='site',siteId=1,payload={type='industry_config',structureId=fab.id+999,recipe='component'}})
assert(not staleOk,'Stale industrial structure ID was accepted')
love.quit()
print('PASS G04-O MOCK UI: industrial build/inspect configuration remains live and stale IDs reject.')
print('NOTE: mocked LÖVE graphics/input only; native rendering and human gameplay remain untested.')
