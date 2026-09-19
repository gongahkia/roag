-- Mock-contract input/render test, NOT real SDL, LÖVE, GPU or LuaJIT validation.
local directory=arg and arg[1] or '/tmp/cosmonauts-expansion-gui'
local mock=require('tests.love_mock').install(directory)
os.remove(directory..'/run.dat');os.remove(directory..'/run.tmp')
local C=require('config');C.width,C.height,C.crew=192,112,9
local main=require('main');love.load();love.keypressed('f1')
local app=main.app;local W=require('src.world');local U=require('src.util');local Codec=require('src.codec')
local w=app.history.live;assert(#w.workers==9)
local function key(k)love.keypressed(k);love.draw()end
local function click(x,y)
 love.draw();local r=main.getRenderer().rect
 local sx,sy=r.x+(x-0.5)*r.scale,r.y+(y-0.5)*r.scale
 love.mousemoved(sx,sy,0,0);love.mousepressed(sx,sy,1);love.mousereleased(sx,sy,1)
end
local function snapshot(name)
 if not arg or not arg[2] then return end
 mock.records={};mock.record=true;love.draw();mock.record=false
 local f=assert(io.open(arg[2]..'/'..name..'.dw','wb'))
 f:write(Codec.encode({width=mock.width,height=mock.height,records=mock.records}));f:close()
end
key('h');assert(app.crew);local before=Codec.encode(w.labor)
key('right');key('0');assert(app.crew.draft.people[1].prefs.dig==0);assert(Codec.encode(w.labor)==before)
mock.width,mock.height=1040,720;love.resize();love.draw();snapshot('crew-individual')
key('tab');key('2');assert(app.crew.draft.weights.dig==50);snapshot('crew-quotas')
key('return');assert(not app.crew);assert(Codec.encode(w.labor)==before,'Policy should be queued, not applied out of tick')
key('right');love.update(0);love.draw();assert(w.labor.quotas);assert(w.labor.people[1].prefs.dig==0)
-- Individual order target survives clicking on the world.
app.selectedWorker=w.workers[2].id;key('y');assert(app.orderWorker==w.workers[2].id)
key('d');click(w.home.left+4,w.home.floor+3)
local pending=app.history.commands[w.tick+1];assert(pending[#pending].worker==w.workers[2].id)
key('y');assert(not app.orderWorker)
-- A named rally, then release, are tick-stamped.
app.selectedWorker=w.workers[3].id;key('m');click(w.home.left+35,w.home.floor-1)
pending=app.history.commands[w.tick+1];assert(pending[#pending].type=='rally' and pending[#pending].worker==w.workers[3].id)
app.selectedWorker=w.workers[3].id;key('j');pending=app.history.commands[w.tick+1];assert(pending[#pending].type=='releaserally')
-- UI arming doesn't start a fuse: a worker must later execute the field job.
local gx,gy=W.tile(w,w.home.left+26,w.home.floor-1)
local s=require('src.structures').install(w,gx,gy,'charge')
app.selectedCell={x=s.gx*4-2,y=s.gy*4-1};app.selectedWorker=nil
key('t');assert(not s.fuseAt);pending=app.history.commands[w.tick+1];assert(pending[#pending].type=='arm')
key('a');assert(app.tool=='charge');key('f9');assert(app.tool=='ward')
-- Inspect/survey an encounter through pointer input.
local site=w.content.sites[1];assert(site);key('u');click(site.x,site.y)
pending=app.history.commands[w.tick+1];assert(pending[#pending].type=='field' and pending[#pending].kind=='survey')
key('f4');assert(app.fieldnotes);snapshot('field-notes');key('escape')
key('home');love.update(0);key('h');assert(app.crew.readonly);key('return');assert(app.crew.error);key('escape');key('end')
-- Map-lab variants don't overwrite the colony until explicit adoption.
key('n');assert(app.newRun);app.newRun.seed='456';key('k');assert(app.newRun.crew==3)
key('f');assert(app.newRun.features=='ruins');key('x');key('space')
assert(app.newRun.preview,app.newRun.error);assert(app.newRun.preview.map.features.crew==3)
key('v');snapshot('world-lab');key('escape');assert(app.history.live==w)
-- 9-person gameplay sidebar, overlays and labels fit the minimum window.
key('q');app.selectedWorker=w.workers[1].id;app.selectedCell=nil;snapshot('nine-crew-gameplay')
love.draw();assert(#app.crewButtons==9)
print('PASS expansion GUI: nine-person layout, quota draft/apply, assignment targets, rally/release, charge controls, survey, notes, archive lock and lab content/crew settings.')
print('NOTE: mocked input and draw commands, not actual LÖVE rendering.')
