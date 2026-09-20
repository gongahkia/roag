-- COS-P03-N: mocked modal/input/render wiring only; not a real LÖVE window.
local directory=arg and arg[1] or '/tmp/cosmonauts-p03-logistics-gui'
assert(directory:match('^[%w%_/%-%.]+$'),'Unsafe test directory')
local mock=require('tests.love_mock').install(directory)
local Codec=require('src.campaign_codec')
local Campaign=require('src.campaign')
local main=require('main');local app=main.app
love.load()
love.keypressed('f1')
love.keypressed('n');love.keypressed('c');assert(app.newRun.action=='campaign_new')
love.keypressed('return');assert(app.newRun.campaignCandidate and not app.campaign)
love.keypressed('return');assert(app.campaign and app.history.live.features.logistics==1,'New frontier campaign did not opt into logistics explicitly')
-- This fixture exercises the authorised practice-only archive branch; production
-- challenge archives remain inspection-only.
for _,state in ipairs({app.history.live,app.history.initial}) do
 state.mode='practice'
 for _,site in ipairs(Campaign.sites(state)) do site.world.mode='practice' end
end
mock.held.lshift=true;love.keypressed('f7');mock.held.lshift=nil;assert(app.region,'Region overlay did not open')
love.draw();local prepare
for _,button in ipairs(app.regionButtons) do if button.action=='expedition' then prepare=button;break end end
assert(prepare,'Owned docked craft did not expose a Prepare craft action')
love.mousepressed(prepare.x+2,prepare.y+2,1);assert(app.expedition and app.region,'Craft action did not open its modal over Region')
local before=Codec.encode(app.history.live);love.draw();love.draw();assert(Codec.encode(app.history.live)==before,'Repeated expedition inspection/draw mutated canonical state')
local passenger,plus,prepareButton
for _,button in ipairs(app.expedition.buttons) do
 if button.action=='passenger' and not passenger then passenger=button end
 if button.action=='cargo' and button.resource=='food' and button.delta==1 then plus=button end
 if button.action=='prepare' then prepareButton=button end
end
assert(passenger and plus and prepareButton,'Expedition modal omitted required draft controls')
love.mousepressed(passenger.x+2,passenger.y+2,1);love.mousepressed(plus.x+2,plus.y+2,1)
local draftBefore=Codec.encode(app.history.live);love.mousepressed(20,150,1);assert(Codec.encode(app.history.live)==draftBefore,'Expedition modal click leaked into a world designation')
love.mousepressed(prepareButton.x+2,prepareButton.y+2,1)
local queued=app.history.commands[app.history.live.tick+1];assert(queued and #queued==1 and queued[1].scope=='campaign' and queued[1].sourceSiteId==1,'Modal prepare did not submit one source-bound campaign command')
local wrong={scope='campaign',type='prepare_expedition',sourceSiteId=2,craftId=1,destinationSiteId=1,passengers={},cargo={food=1}}
local state=Codec.encode(app.history.live);assert(not app.history:queue(wrong));assert(Codec.encode(app.history.live)==state,'Wrong-source craft command mutated state')
love.keypressed('escape');assert(not app.expedition and app.region,'Escape did not return from expedition panel to Region')
love.keypressed('escape');assert(not app.region,'Escape did not close Region')
assert(app.history:advance());app.history:seek(0);assert(not app.history:atPresent(),'Seek did not create an archive view')
mock.held.lshift=true;love.keypressed('f7');mock.held.lshift=nil;love.draw();prepare=nil
for _,button in ipairs(app.regionButtons) do if button.action=='expedition' then prepare=button;break end end
assert(prepare);love.mousepressed(prepare.x+2,prepare.y+2,1);love.draw();passenger=nil;prepareButton=nil
for _,button in ipairs(app.expedition.buttons) do if button.action=='passenger' and not passenger then passenger=button elseif button.action=='prepare' then prepareButton=button end end
love.mousepressed(passenger.x+2,passenger.y+2,1);love.mousepressed(prepareButton.x+2,prepareButton.y+2,1)
assert(app.history:atPresent() and app.history.frontier==0 and not app.expedition,'Practice archive action did not clear stale expedition draft while branching')
love.quit()
print('PASS P03-N GUI adapter: source-bound expedition drafts, modal isolation, pure inspection and Region return path.')
