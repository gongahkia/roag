-- COS-P06-P mocked UI adapter; this does not establish real-window behaviour.
local directory=arg and arg[1] or '/tmp/cosmonauts-p06-education-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
require('tests.love_mock').install(directory)
local F=require('tests.fixtures')
local W=require('src.world')
local S=require('src.structures')
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local Knowledge=require('src.knowledge')
local main=require('main');local app=main.app

love.load()
local localWorld=F.world('practice');F.worker(localWorld,12,24,'A');F.worker(localWorld,19,24,'B')
local campaign=Campaign.new(localWorld,{knowledge=true,education=true});local world=campaign.sites[1].world
assert(Knowledge.survey({campaign=campaign,siteId=1},world.workers[1],{siteId=1,category='flora',id=9001,kind='filter',definitionVersion=1}))
local slot=W.slot(world,8,6);local structure=S.install(world,8,6,'field_school')
app.history=History.new(campaign);app.campaign=true;app.siteId=1;app.paused=true;app.help=nil;app.newRun=nil;app.fieldnotes=nil;app.school=nil
app.selectedCell={x=30,y=24};app.selectedWorker=nil
love.draw()
local build
for _,button in ipairs(app.buttons) do if button.kind=='field_school' then build=button;break end end
assert(build,'Education frontier did not expose Field school build selection')
assert(app.schoolButton and app.schoolButton.schoolId==structure.education.id,'Selected school did not expose a school-policy control')
love.mousepressed(app.schoolButton.x+2,app.schoolButton.y+2,1);assert(app.school,'School policy control did not open the inspector')
love.draw();local topic,enabled,mode,apply
for _,button in ipairs(app.school.buttons) do
 if button.action=='topic' then topic=button elseif button.action=='enabled' then enabled=button elseif button.action=='mode' then mode=button elseif button.action=='apply' then apply=button end
end
assert(topic and enabled and mode and apply,'School inspector omitted policy controls')
love.mousepressed(topic.x+2,topic.y+2,1);love.mousepressed(enabled.x+2,enabled.y+2,1);love.mousepressed(mode.x+2,mode.y+2,1)
love.mousepressed(apply.x+2,apply.y+2,1);assert(not app.school,'Apply did not close the school policy draft')
assert(app.history.commands[app.history.live.tick+1] and app.history.commands[app.history.live.tick+1][1].payload.type=='school_policy','School UI did not queue a site-bound policy command')
assert(main.getRenderer().small.size==13,'School UI did not retain the shared 13px bitmap font strike')
love.quit()
print('PASS P06-P GUI adapter: field-school build control, School inspector policy draft/apply, and shared-font mock wiring.')
print('NOTE: mocked LÖVE graphics/input/storage only; no real window, SDL, GPU, click-target, or bitmap-layout evidence.')
