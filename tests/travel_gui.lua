-- COS-P04-P mocked input/layout adapter.  It deliberately proves neither SDL
-- delivery nor the real Cozette bitmap layout in a visible LÖVE window.
local directory=arg and arg[1] or '/tmp/cosmonauts-p04-travel-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
local mock=require('tests.love_mock').install(directory)
local Codec=require('src.campaign_codec')
local Logistics=require('src.logistics')
local main=require('main');local app=main.app
love.load();love.keypressed('f1');love.keypressed('n');love.keypressed('c')
assert(app.newRun.action=='campaign_new');love.keypressed('return');love.keypressed('return')
assert(app.campaign and app.history.live.features.travel==1,'New frontier campaign did not explicitly opt into travel')
mock.held.lshift=true;love.keypressed('f7');mock.held.lshift=nil;love.draw()
local prepare;for _,button in ipairs(app.regionButtons) do if button.action=='expedition' then prepare=button;break end end
assert(prepare,'Region did not expose the docked craft')
love.mousepressed(prepare.x+2,prepare.y+2,1);love.draw();local passenger,prepareAction
for _,button in ipairs(app.expedition.buttons) do
 if button.action=='passenger' and not passenger then passenger=button end
 if button.action=='prepare' then prepareAction=button end
end
assert(passenger and prepareAction,'Travel-capable expedition panel omitted preparation controls')
local before=Codec.encode(app.history.live);love.mousepressed(10,150,1);assert(Codec.encode(app.history.live)==before,'Expedition modal leaked a world action')
love.mousepressed(passenger.x+2,passenger.y+2,1);love.mousepressed(prepareAction.x+2,prepareAction.y+2,1)
for _=1,480 do assert(app.history:advance()) end
love.draw();local assemble
for _,button in ipairs(app.expedition.buttons) do if button.action=='assemble' then assemble=button;break end end
assert(assemble,'Applied manifest omitted Assemble crew')
love.mousepressed(assemble.x+2,assemble.y+2,1);for _=1,180 do assert(app.history:advance()) end
love.draw();local launch
for _,button in ipairs(app.expedition.buttons) do if button.action=='launch' then launch=button;break end end
assert(launch,'Ready travel manifest omitted Launch')
love.mousepressed(launch.x+2,launch.y+2,1);assert(app.history:advance())
local vehicle=Logistics.craft(app.history.live,1);assert(vehicle.journey,'Launch control did not submit a travel command')
local canonical=Codec.encode(app.history.live);love.draw();love.draw();assert(Codec.encode(app.history.live)==canonical,'Repeated transit inspection mutated campaign state')
vehicle.journey.remainingTicks=1;assert(app.history:advance());love.draw()
assert(vehicle.dockedSiteId==2 and app.history.live.sites[2].ownerSocietyId==1,'Mocked launch did not land/found through the campaign state')
love.keypressed('escape');assert(not app.expedition and app.region,'Escape did not return from transit/docked craft panel to Region')
love.keypressed('escape');assert(not app.region,'Escape did not close Region')
love.quit()
print('PASS P04-P GUI adapter: Cozette-backed mocked prepare/assemble/launch/transit/landing flow, modal isolation, and pure inspection.')
print('NOTE: mocked LÖVE graphics/input/storage only; no real window, SDL, GPU, or bitmap-layout pass occurred.')
