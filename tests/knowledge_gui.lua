-- COS-P05-P mocked modal and shared-font wiring; it is not a real LÖVE window.
local directory=arg and arg[1] or '/tmp/cosmonauts-p05-knowledge-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
local mock=require('tests.love_mock').install(directory)
local Codec=require('src.campaign_codec')
local main=require('main');local app=main.app
love.load();love.keypressed('f1');love.keypressed('n');love.keypressed('c')
assert(app.newRun.action=='campaign_new');love.keypressed('return');love.keypressed('return')
assert(app.campaign and app.history.live.features.knowledge==1,'New frontier campaign did not opt into personal knowledge')
love.draw();assert(#app.buttons==0,'Fixed action bar remained after the contextual HUD refactor')
local world=app.currentWorld();local plant=assert(world.content.flora[1],'Frontier fixture did not provide a growth for the Study HUD path')
local rect=main.getRenderer().rect;local sx=rect.x+(plant.x-.5)*rect.scale;local sy=rect.y+(plant.y-.5)*rect.scale
love.mousepressed(sx,sy,2);assert(app.hud,'Right-click did not open the contextual delegate HUD')
love.draw();local study
for _,button in ipairs(app.hud.buttons) do if button.action=='field:study' then study=button;break end end
assert(study,'Growth delegate HUD omitted Study in a knowledge campaign')
love.mousepressed(study.x+2,study.y+2,1)
local pending=app.history.commands[app.history.live.tick+1]
assert(pending and pending[#pending].payload.type=='field' and pending[#pending].payload.kind=='study','Study HUD action did not queue the generic field command')
local before=Codec.encode(app.history.live);love.keypressed('f4');assert(app.fieldnotes,'F4 did not open personal field notes')
love.draw();love.keypressed('right');love.draw();assert(Codec.encode(app.history.live)==before,'Observer selection or field-note rendering mutated campaign state')
love.mousepressed(20,150,1);assert(Codec.encode(app.history.live)==before,'Field-note modal leaked a world command')
love.keypressed('escape');assert(not app.fieldnotes,'Escape did not dismiss field notes')
love.quit()
print('PASS P05-P GUI adapter: contextual Study delegation, shared-font personal field notes, observer selection, and modal isolation.')
print('NOTE: mocked LÖVE graphics/input/storage only; no real window, SDL, GPU, or bitmap-layout pass occurred.')
