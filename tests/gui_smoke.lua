local directory=arg and arg[1] or '/tmp/cosmonauts-gui-test'
local mock=require('tests.love_mock').install(directory)
os.remove(directory..'/run.dat');os.remove(directory..'/run.tmp')
local C=require('config');C.width,C.height,C.preset=192,112,'cistern'
local main=require('main');love.load();local app=main.app
love.draw();assert(app.help);love.keypressed('f1')
local function frame(n) for _=1,n do love.update(1/60);love.draw() end end
local function cellClick(x,y,dragToX,dragToY)
 love.draw();local r=main.getRenderer().rect
 local sx,sy=r.x+(x-0.5)*r.scale,r.y+(y-0.5)*r.scale
 love.mousemoved(sx,sy,0,0);love.mousepressed(sx,sy,1)
 local ex,ey=r.x+((dragToX or x)-0.5)*r.scale,r.y+((dragToY or y)-0.5)*r.scale
 love.mousemoved(ex,ey,ex-sx,ey-sy);love.mousereleased(ex,ey,1)
end
local row=(app.history.live.home.floor-1)/4
love.keypressed('c');cellClick(34,row*4-1)
love.keypressed('b');cellClick(26,row*4-1)
assert(#app.history.commands[1]==2)
love.keypressed('space');frame(420);love.keypressed('space')
assert(app.history.live.stats.jobsDone>=2,'Mouse orders should complete')
for _=1,3 do love.keypressed('tab');love.draw() end
love.keypressed('g');love.wheelmoved(0,1);love.draw();love.keypressed('r')
love.keypressed('home');frame(1);assert(app.history.view.tick==0)
local before=app.history.frontier
love.keypressed('d');cellClick(50,row*4+2);assert(app.toast:match('read%-only'))
love.keypressed('end');assert(app.history:atPresent())
love.keypressed('f5');assert(love.filesystem.getInfo('run.dat'))
local restored=require('src.storage').load();assert(restored.live.tick==before)
love.keypressed('f6');assert(app.toast:match('Exported'))
love.keypressed('f12');assert(mock.screenshotPath)
-- Selecting a settler and pump port wiring.
love.keypressed('q');cellClick(app.history.live.workers[1].x,app.history.live.workers[1].y-1)
frame(3)
-- Re-layout at minimum window size; font checks execute real format strings.
mock.width,mock.height=1040,720;love.resize();love.draw()
mock.width,mock.height=1340,840;love.resize();love.draw()
-- New-run modal does not discard a run on Escape.
love.keypressed('n');assert(app.newRun);love.keypressed('tab');love.keypressed('escape');assert(app.history.frontier==before)
-- Save always takes the live frontier, even while browsing the past.
love.keypressed('home');love.keypressed('f5');restored=require('src.storage').load();assert(restored.live.tick==before)
love.keypressed('end');app.tool='inspect';app.selectedWorker=nil;app.selectedCell=nil;app.hover=nil;app.toast=nil
mock.record=true;mock.records={};love.draw()
local f=assert(io.open(directory..'/draw.dw','wb'));f:write(require('src.codec').encode(mock.records));f:close()
love.quit()
print('PASS GUI adapter: 420 gameplay frames, mouse construction, layers, resize, timeline, save/load, exports and modal input.')
print('NOTE: this is a mock-contract test, NOT real LÖVE, SDL, LuaJIT, or GPU validation.')
