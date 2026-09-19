local directory=arg and arg[1] or '/tmp/deepward-map-gui'
local mock=require('tests.love_mock').install(directory)
os.remove(directory..'/run.dat');os.remove(directory..'/run.tmp')
local main=require('main');love.load();love.keypressed('f1')
local app=main.app;local Map=require('src.mapfile');local Codec=require('src.codec')
local Store=require('src.storage');local MapStore=require('src.mapstore')
local initial=app.history;local hash=Codec.hash(initial.live)
love.keypressed('f7');love.draw();assert(app.view==4);love.keypressed('f7')
love.keypressed('n');love.draw();assert(app.newRun and app.newRun.preset=='frontier')
love.keypressed('space');love.draw();assert(app.newRun.preview,app.newRun.error)
assert(app.history==initial and Codec.hash(initial.live)==hash,'Preview changed live settlement')
love.keypressed('v');love.draw();assert(app.newRun.view==4)
love.keypressed('f2');assert(app.newRun.error:match('Exported'))
love.keypressed('up');assert(not app.newRun.preview,'Changing settings must invalidate the preview')
love.keypressed('b');love.keypressed('o');love.keypressed('g');love.keypressed('space');assert(app.newRun.preview,app.newRun.error)
local chosen=Map.fingerprint(app.newRun.preview.map)
-- Minimum size layout, material + biome preview, import browser rendering.
mock.width,mock.height=1040,720;love.resize();love.draw();love.keypressed('v');love.draw()
mock.width,mock.height=1340,840;love.resize();love.draw()
-- Simulated save failure must not discard the current colony or start a new one.
local oldsave=Store.save;Store.save=function() return false,'injected write failure' end
love.keypressed('return');assert(app.history==initial and app.newRun.error:match('retained'))
Store.save=oldsave;love.keypressed('return');assert(not app.newRun and app.history~=initial)
assert(Map.fingerprint(Map.fromWorld(app.history.initial))==chosen)
local live=app.history
love.keypressed('f2');local entries=MapStore.list();assert(#entries>=7,'Export or bundled maps missing')
love.keypressed('f3');love.draw();assert(app.mapBrowser and #app.mapBrowser.entries>=7)
love.keypressed('down');love.keypressed('return');love.draw();assert(app.newRun and app.newRun.imported)
assert(app.history==live,'Browser import must only preview')
love.keypressed('escape');assert(app.history==live)
local closed=false
local payload=Map.encode(Map.fromWorld(live.initial))
local function dropped(text,size)
 closed=false
 return {getSize=function() return size or #text end,open=function() return true end,
  read=function() return text end,close=function() closed=true end}
end
love.filedropped(dropped(payload));love.draw();assert(app.newRun and app.newRun.imported and closed)
love.keypressed('tab');assert(app.newRun.mode=='practice');love.keypressed('escape')
local before=Codec.hash(app.history.live)
love.filedropped(dropped('{bad}'));assert(not app.newRun and closed);assert(Codec.hash(app.history.live)==before)
love.filedropped(dropped('x',Map.maxBytes+1));assert(not app.newRun and not closed,'Oversize input should fail before open')
-- Confirming exact map import creates a separate colony, not a rollback of the old one.
love.filedropped(dropped(payload));love.keypressed('return');assert(app.history.live.tick==0 and app.history~=live)
assert(Codec.hash(app.history.live.mat)==Codec.hash(live.initial.mat))
love.keypressed('home');love.keypressed('f5');assert(Store.load())
love.draw();love.quit()
print('PASS map GUI: generation previews, every option, biome view, export/browser/drop import, failed-save rollback, fresh-colony confirmation, and small-window layout.')
print('NOTE: mocked LÖVE adapter only; no SDL, GPU or actual file-drop delivery.')
