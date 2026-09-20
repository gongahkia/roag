-- Mocked UI wiring only.  This is intentionally separate from a real LÖVE
-- window test and uses a disposable save directory.
local directory=arg and arg[1] or '/tmp/cosmonauts-p02-region-gui'
local mock=require('tests.love_mock').install(directory)
os.remove(directory..'/run.dat');os.remove(directory..'/run.tmp');os.remove(directory..'/campaign.run.dat');os.remove(directory..'/campaign.run.tmp')
local C=require('config');C.width,C.height,C.preset=128,80,'frontier'
local Codec=require('src.campaign_codec')
local Campaign=require('src.campaign')
local main=require('main');love.load();local app=main.app
love.keypressed('f1')
local localHistory=app.history
love.keypressed('n');assert(app.newRun and app.newRun.action=='local_run','N must retain local-run action by default')
love.keypressed('escape');assert(app.history==localHistory and not app.campaign,'N/Escape changed local session')
assert(love.filesystem.write('run.dat','legacy sentinel'))
love.keypressed('n');love.keypressed('c');love.keypressed('c');assert(app.newRun.action=='campaign_continue');love.keypressed('return')
assert(app.history==localHistory and app.newRun.error=='No campaign save.','Missing campaign continuation replaced active session');love.keypressed('escape')
love.keypressed('n');love.keypressed('c');assert(app.newRun.action=='campaign_new');app.newRun.seed='0';love.keypressed('return')
assert(app.history==localHistory and app.newRun.error:match('Campaign seed'),'Failed campaign creation replaced active session');app.newRun.seed='12345'
love.keypressed('return');assert(app.newRun.campaignCandidate and not app.campaign,'Campaign candidate replaced active session before confirmation')
love.keypressed('return');assert(app.campaign and app.history.live.features.region==1 and app.siteId==1,'Explicit campaign action did not create a region campaign')
assert(assert(love.filesystem.read('run.dat'))=='legacy sentinel','Campaign creation touched local save namespace')
assert(love.filesystem.getInfo('campaign.run.dat'),'Campaign action did not save separate slot')
local canonical=Codec.encode(app.history.live);love.draw();assert(app.regionButton and #app.siteButtons==1,'Campaign selector was not drawn')
mock.held.lshift=true;love.keypressed('f7');mock.held.lshift=nil;assert(app.region,'Shift+F7 did not open campaign overlay')
assert(Codec.encode(app.history.live)==canonical,'Opening overlay changed campaign state')
local paused=app.paused;love.keypressed('space');assert(app.paused~=paused,'Overlay Space did not control global clock')
love.draw();local moon
for _,b in ipairs(app.regionButtons) do if b.action=='unvisited' then moon=b;break end end
assert(moon,'Unvisited moon exposed no summary action')
love.mousepressed(moon.x+2,moon.y+2,1);assert(app.siteId==1,'Unvisited moon changed active settlement')
love.keypressed('escape');assert(not app.region)
-- Explicit fixture ownership exercises an empty selectable settlement before P04.
assert(app.history:advance())
app.history.live.sites[2].ownerSocietyId=1;app.history.view.sites[2].ownerSocietyId=1;love.draw()
local site2;for _,b in ipairs(app.siteButtons) do if b.siteId==2 then site2=b end end;assert(site2,'Empty owned settlement missing from selector')
love.mousepressed(site2.x+2,site2.y+2,1);assert(app.siteId==2 and #app.currentWorld().workers==0,'Owned-site selection was not view-only')
love.draw();assert(main.getRenderer().lastWorld==app.currentWorld(),'Same-tick site switch reused another world image')
app.history:seek(0);while app.history.seekTarget do app.history:updateSeek(20) end;love.draw()
for _,b in ipairs(app.siteButtons) do assert(b.siteId~=2,'Archive selector inspected future ownership') end
-- Corrupt continuation is explicit and leaves the active session alone.
app.history:seek(app.history.frontier);assert(love.filesystem.write('campaign.run.dat','{'))
local active=app.history;love.keypressed('n');love.keypressed('c');love.keypressed('c');assert(app.newRun.action=='campaign_continue');love.keypressed('return')
assert(app.history==active and app.newRun and app.newRun.error:match('Campaign save left unchanged'),'Corrupt campaign continue replaced active session')
love.keypressed('escape')
local U=require('src.util');local bad=U.deep(active.live);bad.features.region=2
assert(love.filesystem.write('campaign.run.dat',Codec.encode({format='cosmonauts-campaign',version=1,initial=bad,live=bad,commands={}})))
love.keypressed('n');love.keypressed('c');love.keypressed('c');love.keypressed('return')
assert(app.history==active and app.newRun and app.newRun.error:match('Unsupported campaign region feature version'),'Unsupported campaign continue replaced active session')
love.keypressed('escape');love.quit()
print('PASS P02-I GUI adapter: explicit lab actions, isolated campaign slot, region modal, global Space, owned selector, archive visibility and corrupt continuation.')
print('NOTE: mocked input/draw/storage only; this is not a real LÖVE, SDL, or GPU pass.')
