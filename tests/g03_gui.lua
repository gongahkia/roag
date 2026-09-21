-- MOCK UI evidence only.  It verifies the live Crew Mind page and explicitly
-- does not claim native LÖVE input, rendering, or human playtest coverage.
local directory=arg and arg[1] or '/tmp/cosmonauts-g03-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
require('tests.love_mock').install(directory)
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local main=require('main');local app=main.app
love.load()
local c=Campaign.newRegion(97031,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true,psychology=true})
app.history=History.new(c);app.campaign=true;app.siteId=1;app.help=nil;app.newRun=nil;app.crew=nil;app.fieldnotes=nil;app.school=nil;app.paused=false;app.accumulator=0
love.keypressed('h');assert(app.crew,'Crew panel did not open');love.keypressed('tab');love.keypressed('tab');assert(app.crew.section=='mind','Crew Mind page is not reachable')
local start=app.history.live.tick;for _=1,12 do love.update(1/60);love.draw() end
assert(app.history.live.tick>start and not app.paused,'Mind page paused live simulation')
love.quit()
print('PASS G03-P MOCK UI: live plain-language Mind/relationships Crew page.')
print('NOTE: mocked LÖVE graphics/input only; native rendering and human gameplay remain untested.')
