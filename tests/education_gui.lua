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
love.draw();assert(#app.buttons==0,'Fixed action bar remained after the contextual HUD refactor')
local renderer=main.getRenderer();local function clickCell(x,y,button)
 local rect=renderer.rect;love.mousepressed(rect.x+(x-.5)*rect.scale,rect.y+(y-.5)*rect.scale,button)
end
local empty
for gy=2,world.rows-1 do for gx=2,world.cols-1 do
 if not world.structures[W.slot(world,gx,gy)] then empty={x=(gx-1)*4+1,y=(gy-1)*4+1};break end
end;if empty then break end end
assert(empty,'Fixture has no empty block for contextual build selection')
clickCell(empty.x,empty.y,2);love.draw();local build
for _,button in ipairs(app.hud.buttons) do if button.action=='build:field_school' then build=button;break end end
assert(build,'Education block HUD did not expose Field school construction')
love.mousepressed(build.x+2,build.y+2,1)
local buildCommands=assert(app.history.commands[app.history.live.tick+1],'Field-school delegation did not queue a command')
assert(buildCommands[#buildCommands].payload.type=='order' and buildCommands[#buildCommands].payload.build=='field_school','Contextual field-school delegation queued the wrong command')
clickCell(30,24,2);love.draw();local schoolAction
for _,button in ipairs(app.hud.buttons) do if button.action=='school' then schoolAction=button;break end end
assert(schoolAction,'Installed school HUD omitted the School policy action')
love.mousepressed(schoolAction.x+2,schoolAction.y+2,1);assert(app.school,'School policy action did not open the inspector')
love.draw();local topic,enabled,mode,apply
for _,button in ipairs(app.school.buttons) do
 if button.action=='topic' then topic=button elseif button.action=='enabled' then enabled=button elseif button.action=='mode' then mode=button elseif button.action=='apply' then apply=button end
end
assert(topic and enabled and mode and apply,'School inspector omitted policy controls')
love.mousepressed(topic.x+2,topic.y+2,1);love.mousepressed(enabled.x+2,enabled.y+2,1);love.mousepressed(mode.x+2,mode.y+2,1)
love.mousepressed(apply.x+2,apply.y+2,1);assert(not app.school,'Apply did not close the school policy draft')
local pending=assert(app.history.commands[app.history.live.tick+1],'School UI did not queue a site-bound policy command')
assert(pending[#pending].payload.type=='school_policy','School UI did not queue a site-bound policy command')
assert(main.getRenderer().small.size==13,'School UI did not retain the shared 13px bitmap font strike')
love.quit()
print('PASS P06-P GUI adapter: contextual field-school build/delegate controls, School inspector policy draft/apply, and shared-font mock wiring.')
print('NOTE: mocked LÖVE graphics/input/storage only; no real window, SDL, GPU, click-target, or bitmap-layout evidence.')
