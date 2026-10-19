-- Loaded only by the isolated shell harness. Existing disk fixtures provide
-- independently computed expected events/state; actual actions use game keys.
io.stdout:setvbuf('line')
function love.errorhandler(why)
    io.stderr:write(debug.traceback(tostring(why),2)..'\n');return function()return 1 end
end
local G=require('src.game');local P=require('src.persistence')
local Menu=require('src.interaction_menu');local PNG=require('tools.art_png')
local dir=assert(os.getenv('ROAG_IDEA_ACCEPTANCE_DIR'))
local output=assert(os.getenv('ROAG_IDEA_ACCEPTANCE_OUTPUT'))
local group=assert(os.getenv('ROAG_IDEA_ACCEPTANCE_GROUP'))
local case=assert(os.getenv('ROAG_IDEA_ACCEPTANCE_CASE'))
local resuming=os.getenv('ROAG_IDEA_ACCEPTANCE_RESUME')=='1'
local function read(name)
    local f=io.open(dir..'/'..name,'rb');if not f then return end
    local bytes=f:read('*a');f:close();return bytes
end
local expected=assert(read('expected.data')or read('expected.roag-data'))
local expected_events=assert(read('events.data')or read('events.roag-data')or read('expected-events.roag-data'))
local store=P.new({read=function(_,name)return read(name)end,
    write=function()error('reference fixture is read-only')end})
local initial=P.encode(G.snapshot(assert(store:load())))
local plan
if read('plan.data')then plan=P.decode(read('plan.data'))
elseif group=='enemy'then
    local n=case=='alarm' and 3 or ({cache=true,web=true,sentinel=true,mirror=true,silt=true,moth=true})[case]and 2 or 1
    plan={};for _=1,n do plan[#plan+1]={kind='wait'}end
elseif group=='duelist'then plan={{kind='basic_attack',dx=1,dy=0}}
else
    plan={};for i=1,4 do
        plan[i]=i==1 and case=='spiritbinder' and {kind='sweeping_slash',dx=1,dy=0}
            or i==1 and case=='wayfinder' and {kind='dash',dx=1,dy=0}or {kind='wait'}
    end
end
local tracked,menu,actual,events=nil,nil,{},{}
local restore=G.restore
function G.restore(...)local s=restore(...);tracked=s;return s end
local submit=G.submit
function G.submit(...)
    local ok,why,list,preview=submit(...)
    if ok then actual[#actual+1]=list;for _,e in ipairs(list)do events[#events+1]=e end end
    return ok,why,list,preview
end
local new_menu=Menu.new
function Menu.new(...)menu=new_menu(...);return menu end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local phase,step,pending,captured,failed='before',1,false,nil,false
local baseline,before,started
local function snapshot()return P.encode(G.snapshot(assert(tracked)))end
local function fail(why)
    failed=true;io.stderr:write('IDEA ACCEPTANCE ['..group..'/'..case..'/'..phase..']: '..tostring(why)..'\n')
    love.event.quit(1)
end
local function select_entry(matches)
    assert(menu,'interaction menu not opened')
    local index
    for i,e in ipairs(menu.options.entries)do if matches(e)then index=i;break end end
    assert(index,'required interaction option missing')
    love.keypressed('up')
    while menu.selected>1 do love.keypressed('up')end
    for _=2,index do love.keypressed('down')end
    love.keypressed('return')
end
local function key_action(intent)
    if intent.kind=='wait'then love.keypressed('space');return end
    if intent.kind=='interact'then
        love.keypressed('e')
        if menu.context.nearby then select_entry(function(e)return e.node_id==intent.node_id end)end
        assert(menu.context.node_id==intent.node_id)
        if intent.selection then select_entry(function(e)return e.select and
            e.select.id==intent.selection.id and e.select.rarity==intent.selection.rarity end)end
        select_entry(function(e)return e.id==intent.choice_id end)
        assert(menu.confirm);love.keypressed('return');return
    end
    local dx,dy=intent.dx or 1,intent.dy or 0
    local direction=dx<0 and (dy<0 and 'q' or dy>0 and 'z' or 'a')
        or dx>0 and (dy<0 and 'e' or dy>0 and 'c' or 'd')or dy<0 and 'w' or 's'
    if intent.kind=='move'then love.keypressed(direction);return end
    if intent.kind=='use_utility'then
        love.keypressed('u');love.keypressed('return');love.keypressed('return')
        if G.player(tracked).utility and G.player(tracked).utility.kind=='escape_scroll'then
            if intent.steps then love.keypressed(tostring(intent.steps))end
            love.keypressed(direction);love.keypressed('return')
        end
        return
    end
    love.keypressed(intent.kind=='basic_attack' and 'f' or intent.kind=='sweeping_slash' and 'r' or 'x')
    love.keypressed(direction);love.keypressed('return')
end
function love.load(...)
    local args={...};local ok,why=xpcall(function()
        love.mouse.getPosition=function()return 0,0 end
        love.keyboard.isDown=function()return false end
        if not resuming then assert(love.filesystem.write('active-a.roag',assert(read('active-a.roag'))))end
        load(unpack(args))
        if os.getenv('ROAG_GUI_WIDE')=='1'then love.window.setMode(1280,800,{resizable=false})end
        love.keypressed('c');update(10)
        assert(love.roagUIState().page=='game','Continue did not enter game')
        assert(snapshot()==(resuming and expected or initial),'Continue changed saved state')
        if case=='route_sign' then
            love.keypressed('e');assert(menu and #menu.options.entries==0)
            assert(snapshot()==(resuming and expected or initial),'reading sign changed the run')
            love.keypressed('escape')
        end
        before=snapshot();started=love.timer.getTime()
        print('Renderer:',love.graphics.getRendererInfo())
        if resuming then phase='resumed'end
    end,debug.traceback)
    if not ok then fail(why)end
end
local function advance(image)
    assert(snapshot()==before,'frame/update changed authoritative state')
    PNG.write(output..'/'..phase..'-'..step..'.png',image)
    if phase=='resumed'then
        love.keypressed('f5');update(0);assert(snapshot()==expected)
        print('PASS '..group..'/'..case..': actual fresh GUI Continue and pure F5')
        love.event.quit();return
    end
    if phase=='before'then baseline=image end
    if step<=#plan then
        local count=#actual;key_action(plan[step]);assert(#actual==count+1,'key action was lost or repeated')
        update(10);step=step+1;phase='after';before=snapshot();return
    end
    assert(snapshot()==expected,'GUI authoritative continuation differs from independent process')
    local encoded
    if group=='enemy' or group=='duelist'then encoded=P.encode(events)else encoded=P.encode(actual)end
    assert(encoded==expected_events,'GUI ordered events differ from independent process')
    local changed=0
    for y=16,623,3 do for x=16,1007,3 do
        local a={baseline:getPixel(x,y)};local b={image:getPixel(x,y)}
        if math.abs(a[1]-b[1])+math.abs(a[2]-b[2])+math.abs(a[3]-b[3])>.05 then changed=changed+1 end
    end end
    -- Oath/fragment fixtures may have no new movement after the saved mechanic;
    -- their exact events/state, visible art and fresh Continue still must pass.
    assert(changed>0 or case=='oath' or case=='bones','no visible result')
    love.keypressed('f5');update(0);assert(snapshot()==expected,'F5 changed continuation')
    print('PASS '..group..'/'..case..': actual keys, expected events/state, changed pixels='..changed..', Save & Quit')
    love.keypressed('escape');love.keypressed('s')
end
function love.update()
    if failed then return end
    local ok,why=xpcall(function()
        if started and love.timer.getTime()-started>25 then error('fixture timeout')end
        if captured then local im=captured;captured=nil;advance(im)end
    end,debug.traceback)
    if not ok then fail(why)end
end
function love.draw()
    if failed then return end
    local ok,why=xpcall(function()
        draw()
        if not pending and not captured then pending=true
            love.graphics.captureScreenshot(function(im)captured=im;pending=false end)
        end
    end,debug.traceback)
    if not ok then fail(why)end
end
