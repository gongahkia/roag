-- G01 presentation/input wiring under the disposable LÖVE mock. This is not a
-- native window or human playtest result.
local directory=arg and arg[1] or '/tmp/cosmonauts-g01-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
require('tests.love_mock').install(directory)
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Visibility=require('src.visibility')
local main=require('main');local app=main.app
love.load()
local campaign=Campaign.newRegion(443322,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true})
app.history=History.new(campaign);app.campaign=true;app.siteId=1;app.help=nil;app.newRun=nil;app.paused=false;app.accumulator=0
local function frame(n) for _=1,n do love.update(1/60);love.draw() end end
love.draw();local renderer=main.getRenderer();local w=app.currentWorld();local worker=w.workers[1]
-- The top row belongs to a 2x4 worker, while a cell just above is no hit.
local function clickCell(x,y,button)
 local r=renderer.rect;local sx,sy=r.x+(x-.5)*r.scale,r.y+(y-.5)*r.scale
 love.mousepressed(sx,sy,button or 1);love.mousereleased(sx,sy,button or 1)
end
clickCell(worker.x,worker.y-3);assert(app.selectedWorker==worker.id,'2x4 top-row hitbox did not select worker')
app.selectedWorker=nil;clickCell(worker.x,worker.y-4);assert(app.selectedWorker==nil,'Obsolete 2x3/oversized hitbox selected worker')
local start=app.history.live.tick;love.keypressed('h');assert(app.crew,'Crew panel did not open');frame(12);assert(app.history.live.tick>start and not app.paused,'Crew panel paused the running campaign')
love.keypressed('escape');love.keypressed('f4');assert(app.fieldnotes,'Field notes did not open');start=app.history.live.tick;frame(12);assert(app.history.live.tick>start and not app.paused,'Field notes paused the running campaign')
love.keypressed('escape');love.keypressed('f1');start=app.history.live.tick;frame(12);assert(app.history.live.tick>start and not app.paused,'Help panel paused the running campaign')
love.keypressed('space');assert(app.paused,'Space did not explicitly pause while help was open');love.keypressed('f1')
love.draw();local farX,farY=w.width-5,5;assert(not Visibility.currentlyVisible(w,farX,farY,{campaign=app.history.view,siteId=1}),'Fixture unexpectedly revealed the full map')
local r=renderer.rect;local px,py=r.x+(farX-.5)*r.scale,r.y+(farY-.5)*r.scale
love.mousepressed(px,py,2);love.draw();assert(not app.hud,'Unseen right-click opened a detailed action HUD')
love.quit()
print('PASS G01-P MOCK UI: 2x4 hitboxes, live Crew/F4/Help panels, explicit Space pause, and unseen action suppression.')
print('NOTE: mocked LÖVE graphics/input only; native rendering and human gameplay remain untested.')
