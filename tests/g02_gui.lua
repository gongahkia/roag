-- COS-G02 presentation wiring under the disposable LÖVE mock.  This covers
-- player-reachable Rope/tool-bench/craft-tool affordances; it is not evidence
-- of native rendering or human interaction.
local directory=arg and arg[1] or '/tmp/cosmonauts-g02-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
require('tests.love_mock').install(directory)
local Campaign=require('src.campaign')
local History=require('src.campaign_history')
local S=require('src.structures')
local Visibility=require('src.visibility')
local HUD=require('src.ui.action_hud')
local main=require('main');local app=main.app
love.load()
local campaign=Campaign.newRegion(554433,{preset='frontier',mode='practice',width=128,height=80,layout='hybrid',climate='balanced',openness=.48,biomeScale=1,features='living',density=1,crew=3,logistics=true,travel=true,knowledge=true,education=true,body=true,visibility=true,equipment=true,safe_excavation=true})
app.history=History.new(campaign);app.campaign=true;app.siteId=1;app.help=nil;app.newRun=nil;app.paused=false;app.accumulator=0
local function has(actions,id) for _,action in ipairs(actions) do if action.id==id then return true end end end
local function frame(n) for _=1,n do love.update(1/60);love.draw() end end
local w=app.currentWorld();local gx,gy
for y=2,w.rows-1 do for x=2,w.cols-1 do
 local cx,cy=x*4-2,y*4-2
 if Visibility.currentlyVisible(w,cx,cy,{campaign=app.history.view,siteId=1}) and S.siteClear(w,x,y,'tool_bench') then gx,gy=x,y;break end
end if gx then break end end
assert(gx,'No visible tool-bench presentation block in mock fixture')
app.hud={cell={x=gx*4-2,y=gy*4-2}}
local model=assert(HUD.model(app,w));assert(has(model.actions,'rope'),'Visible G02 block omitted Rope action');assert(has(model.actions,'build:tool_bench'),'Visible G02 block omitted Tool bench action')
S.install(w,gx,gy,'tool_bench');app.hud={cell={x=gx*4-2,y=gy*4-2}}
model=assert(HUD.model(app,w));assert(has(model.actions,'fabricate:pickaxe') and has(model.actions,'fabricate:rope_coil'),'Tool bench omitted fabrication actions')
local start=app.history.live.tick;love.keypressed('h');frame(12);assert(app.history.live.tick>start and not app.paused,'G02 crew/stress panel paused the live campaign');love.keypressed('escape')
app.expedition={sourceSiteId=1,craftId=1,destinationSiteId=2,passengers={},cargo={food=1,metal=1},buttons={}}
main.getRenderer():drawExpedition(app)
local load=false;for _,button in ipairs(app.expedition.buttons) do if button.action=='loadTool' then load=true end end
assert(load,'Expedition panel omitted physical loose-tool loading controls')
love.quit()
print('PASS G02-Q MOCK UI: rope/tool-bench actions, live crew panel and physical craft-tool loading control.')
print('NOTE: mocked LÖVE graphics/input only; native rendering and human gameplay remain untested.')
