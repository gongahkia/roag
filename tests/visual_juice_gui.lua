-- Isolated callback-driven rendering acceptance. Prepared art/reward fixtures;
-- not physical input or a claimed legal full run. Screenshot callbacks only
-- hand ImageData to protected update code, where assertions and actions run.
io.stdout:setvbuf('line')
local G=require('src.game')
local Codec=require('src.persistence')
local PB=require('src.playback')
local J=require('src.visual_juice')
local UI=require('src.pixel_ui')
local O=require('src.onebit_visuals')
local Palette=require('src.visual_palette')
local Camera=require('src.camera')
local PNG=require('tools.art_png')
local Rewards=require('src.rewards')
local out=assert(os.getenv('ROAG_JUICE_OUTPUT'))
local mode=assert(os.getenv('ROAG_JUICE_MODE'))
local tracked,shadow,enemy,chest,pb,juice,native,camera
local before,source,drawn,kicks,flights,focus,offers,labels
local original_restore,original_submit=G.restore,G.submit
local submissions=0
local function snapshot(s)return Codec.encode(G.snapshot(s or assert(tracked)))end
function G.new_stage(seed,options)
    assert(mode~='resume','Continue unexpectedly generated a floor')
    local fixture_options={}
    for k,v in pairs(options or {})do fixture_options[k]=v end
    fixture_options.meta=require('src.achievements').new_run(false,'juice-fixture')
    if mode=='discovery'then
        tracked=G.new_generated(1,fixture_options)
        for _,e in ipairs(tracked.entities)do
            if e.controller=='ai'then require('src.scheduler').remove_for_actor(tracked,e.id)end
        end
        local s=tracked.secrets[1];local p=G.player(tracked)
        p.position.x,p.position.y=s.x,s.y
        require('src.visibility').recompute(tracked)
        shadow=original_restore(G.snapshot(tracked));return tracked
    end
    tracked=G.new(17,'core:map/scrolling_room',fixture_options)
    local p=G.player(tracked);p.position.x,p.position.y=3,3;p.hp=10
    enemy=G.spawn_enemy(tracked,'core:enemy/mossbound_guard',4,3,mode=='enemy' and 0 or 10000)
    enemy.hp,enemy.max_hp=200,200
    if mode=='impact'then enemy.hp=1;enemy.defense=0;tracked.progression.xp_total=4 end
    if mode=='blocked'then enemy.defense=100 end
    if mode=='flow'then
        tracked.currency=10
        Rewards.spawn_currency(tracked,3,tracked.active_floor_id,3,4,function()end)
        Rewards.spawn_supply(tracked,'food',tracked.active_floor_id,3,4,function()end)
        chest=G.spawn_chest(tracked,'core:chest/standard',4,4)
    end
    require('src.visibility').recompute(tracked)
    shadow=original_restore(G.snapshot(tracked));return tracked
end
function G.submit(s,intent)
    tracked=s;submissions=submissions+1
    if snapshot(s)~=snapshot(shadow)then
        local function difference(a,b,path)
            if type(a)~=type(b)then return path..' type '..type(a)..'/'..type(b)end
            if type(a)~='table'then if a~=b then return path..' '..tostring(a)..'/'..tostring(b)end;return end
            for k,v in pairs(a)do local d=difference(v,b[k],path..'.'..tostring(k));if d then return d end end
            for k in pairs(b)do if a[k]==nil then return path..'.'..tostring(k)..' missing' end end
        end
        error('state changed before committed action: '..(difference(G.snapshot(s),G.snapshot(shadow),'state')or '?'))
    end
    local a,b,c=original_submit(shadow,intent)
    local ok,why,events,preview=original_submit(s,intent)
    assert(ok==a and why==b and Codec.encode(events)==Codec.encode(c),'visuals changed committed events')
    assert(snapshot(s)==snapshot(shadow),'visuals changed authoritative continuation')
    before=snapshot();return ok,why,events,preview
end
local pb_new=PB.new
function PB.new(...)pb=pb_new(...);return pb end
local juice_new=J.new
function J.new(...)juice=juice_new(...);return juice end
local native_new=O.new
function O.new(...)local status;native,status=native_new(...);return native,status end
local camera_new=Camera.new
function Camera.new(...)camera=camera_new(...);return camera end
local attack=J.attack_offset
function J.attack_offset(p,id,...)
    local x,y=attack(p,id,...);if kicks then kicks[id]={x,y}end;return x,y
end
local reward_pose=J.reward_pose
function J.reward_pose(e,...)
    local x,y,size,a=reward_pose(e,...)
    if flights then flights[#flights+1]={binding=e.binding,x=x-size/2,y=y-size/2,sx=size/16,sy=size/16,alpha=a}end
    return x,y,size,a
end
local option=UI.option_feedback
function UI.option_feedback(label,x,y,w,...)
    local dy,gap,pulse=option(label,x,y,w,...)
    if focus then focus[label]={dy=dy,gap=gap,pulse=pulse}end
    return dy,gap,pulse
end
local offer_pose=UI.offer_pose
function UI.offer_pose(i)
    local y,scale=offer_pose(i);if offers then offers[i]={y=y,scale=scale}end;return y,scale
end
assert(love.filesystem.load('roag_main.lua'))()
local load,update,draw=love.load,love.update,love.draw
local keypressed,mousepressed,mousereleased=love.keypressed,love.mousepressed,love.mousereleased
local scripted=false
function love.keypressed(...)if scripted then return keypressed(...)end end
function love.mousepressed(...)if scripted then return mousepressed(...)end end
function love.mousereleased(...)if scripted then return mousereleased(...)end end
function love.mousemoved()end
function love.gamepadpressed()end
local draw_image=love.graphics.draw
function love.graphics.draw(texture,quad,...)
    if drawn and type(quad)=='userdata' and quad.typeOf and quad:typeOf('Quad')then
        local a={...};drawn[#drawn+1]={texture=texture,quad=quad,x=a[1],y=a[2],sx=a[4],sy=a[5],clip={love.graphics.getScissor()}}
    end
    return draw_image(texture,quad,...)
end
local print_text=love.graphics.print
function love.graphics.print(text,...)
    if labels then labels[tostring(text)]=true end
    return print_text(text,...)
end
local function runtime_state()
    for i=1,100 do
        local name,value=debug.getupvalue(keypressed,i)
        if name=='state'then return value end
    end
end
local function stable()
    local current=runtime_state()
    if current and before then assert(snapshot(current)==before,'presentation changed snapshot/RNG/schedule')end
end
local function step(dt)update(dt or 0);stable()end
local function key(k)
    local state=runtime_state()
    local old,count=state and snapshot(state),submissions
    scripted=true;keypressed(k);scripted=false
    local current=runtime_state()
    if current and current==state and submissions==count then assert(snapshot(current)==old,'menu/aim key changed the run')end
    if current then
        tracked=current
        if not shadow then shadow=original_restore(G.snapshot(current))end
        before=snapshot()
    end
end
local function aim()key('f');key('right');key('return');step(0)end
local function click(x,y)
    scripted=true;mousepressed(x,y,1);mousereleased(x,y,1);scripted=false;step(0)
end
local function rect_pixels(image,x,y,w,h)
    local bytes={}
    for yy=y,y+h-1 do for xx=x,x+w-1 do
        local r,g,b=image:getPixel(xx,yy)
        bytes[#bytes+1]=string.char(math.floor(r*255+.5),math.floor(g*255+.5),math.floor(b*255+.5))
    end end
    return table.concat(bytes)
end
local function actor_pixels(image,actor)
    local handle=assert(native.handles[actor.definition_id])
    local x,y=Camera.tile_top_left(camera,actor.position.x,actor.position.y)
    local kick=assert(kicks[actor.id]);local found
    for _,r in ipairs(drawn)do
        if r.texture==handle.texture and r.quad==handle.quad and math.abs(r.sx)==2.5 and r.sy==2.5 then
            local left=math.min(r.x,r.x+r.sx*16)
            if math.abs(left-(x+kick[1]))<.01 and math.abs(r.y-(y+kick[2]))<.01 then found=r;break end
        end
    end
    assert(found,'native actor was not drawn at the recoil position')
    local sx,sy=handle.quad:getViewport();local ink=actor.controller=='ai' and Palette.enemy or {1,1,1}
    local samples=0
    for yy=0,(actor.controller=='ai' and 9 or 15)do for xx=0,15 do
        local r,g,b,a=source:getPixel(sx+xx,sy+yy)
        if a>.99 and r>.8 then
            local px,py=math.floor(found.x+(xx+.5)*found.sx),math.floor(found.y+(yy+.5)*found.sy)
            local actual={image:getPixel(px,py)}
            for c,v in ipairs({r,g,b})do
                assert(math.abs(actual[c]-v*(handle.tint and handle.tint[c] or 1)*ink[c])<3/255,
                    'native recoil pixels differ from source/ink')
            end
            samples=samples+1
        end
    end end
    assert(samples>5,'empty actor source')
end
local function flight_pixels(image)
    assert(#flights>0,'collection arc was not rendered')
    for _,flight in ipairs(flights)do
        local h=assert(native.handles[flight.binding]);local found
        for _,r in ipairs(drawn)do
            if r.texture==h.texture and r.quad==h.quad and math.abs(r.x-flight.x)<.01 and
                math.abs(r.y-flight.y)<.01 and math.abs(r.sx-flight.sx)<.01 then found=r;break end
        end
        assert(found,'reward pose was not used by real draw')
        local sx,sy=h.quad:getViewport();local samples=0;local ink=assert(Palette.binding(flight.binding))
        for yy=0,15 do for xx=0,15 do
            local r,g,b,a=source:getPixel(sx+xx,sy+yy)
            if a>.99 and r>.8 then
                local px,py=math.floor(found.x+(xx+.5)*found.sx),math.floor(found.y+(yy+.5)*found.sy)
                local actual={image:getPixel(px,py)}
                for c,v in ipairs({r,g,b})do
                    assert(math.abs(actual[c]-v*(h.tint and h.tint[c] or 1)*ink[c])<4/255,
                        'collection sprite pixels/ink do not match source')
                end
                samples=samples+1
            end
        end end
        assert(samples>3,'empty reward source')
    end
end
local function pixel_ink(image,x,y,color)
    local actual={image:getPixel(math.floor(x),math.floor(y))}
    for i=1,3 do assert(math.abs(actual[i]-color[i])<3/255,
        ('accent pixel %d,%d channel %d: %.3f / %.3f'):format(x,y,i,actual[i],color[i]))end
end
local function impact_pixels(image,blocked)
    assert(#juice.impacts==1 and juice.impacts[1].age==0)
    local e=juice.impacts[1];local x,y=Camera.tile_top_left(camera,e.x,e.y)
    x,y=x+20,y+20
    if blocked then
        local radius=J.ring_pose(e,40);pixel_ink(image,x+radius,y+radius,Palette.enemy)
    else
        for i=1,5 do
            local dx,dy=J.impact_pose(e,i,40);pixel_ink(image,x+dx,y+dy,Palette.friendly)
        end
    end
end
local function crumble_pixels(image,initial)
    assert(#juice.crumbles==1)
    local e=juice.crumbles[1];local h=assert(native.handles[e.binding])
    local x,y=Camera.tile_top_left(camera,e.x,e.y)
    local samples=0;local sx,sy=h.quad:getViewport()
    for i=1,9 do
        local left,top,w,height,dx,dy=J.crumble_piece(e,i,40)
        local found
        for _,r in ipairs(drawn)do
            if r.texture==h.texture and r.quad==h.quad and r.x==x+dx and r.y==y+dy and
                r.clip[1]==x+left+dx and r.clip[2]==y+top+dy and
                r.clip[3]==w and r.clip[4]==height then found=r;break end
        end
        assert(found,'crumble did not crop the native sprite at its fragment pose')
        -- The top of the sprite is clear of the existing central hit cue.
        if initial then for yy=0,4 do for xx=0,15 do
            local r,g,b,a=source:getPixel(sx+xx,sy+yy)
            local px,py=math.floor(x+(xx+.5)*2.5),math.floor(y+(yy+.5)*2.5)
            if a>.99 and r>.8 and px>=found.clip[1] and px<found.clip[1]+w and
                py>=found.clip[2] and py<found.clip[2]+height then
                local color={}
                for c,v in ipairs({r,g,b})do color[c]=v*(h.tint and h.tint[c]or 1)*Palette.enemy[c]end
                pixel_ink(image,px,py,color);samples=samples+1
            end
        end end end
    end
    if initial then assert(samples>2,'no original native pixels survive the initial crumble')end
end
local phases,phase={},1
local function add(name,enter,check)phases[#phases+1]={name=name,enter=enter,check=check}end
local menu_pop,menu_settled,press_settled,offer_initial
if mode=='flow'then
    add('menu-focus-pop',function()
        while love.roagUIState().selected~=3 do key('down')end
    end,function(image)
        assert(focus.ACHIEVEMENTS and focus.ACHIEVEMENTS.pulse==1,'initial focus accent is disabled or missing')
        menu_pop=rect_pixels(image,260,305,510,58)
    end)
    add('menu-focus-settled',function()step(.2)end,function(image)
        menu_settled=rect_pixels(image,260,305,510,58);assert(menu_pop~=menu_settled,'focus pop has no rendered change')
    end)
    add('class-open',function()key('up');key('return');step(0)end,function()assert(love.roagUIState().page=='class')end)
    add('class-settled',function()step(.2)end,function(image)
        assert(love.roagUIState().page=='class');press_settled=rect_pixels(image,180,142,302,40)
    end)
    add('class-press',function()click(300,160)end,function(image)
        assert(rect_pixels(image,180,142,302,40)~=press_settled,'mouse press has no rendered offset')
    end)
    add('player-neutral',function()key('return');step(.3);aim()end,function(image)
        assert(kicks[tracked.player_id][1]==0);actor_pixels(image,G.player(tracked))
    end)
    add('player-recoil',function()step(.04)end,function(image)
        assert(kicks[tracked.player_id][1]==3);actor_pixels(image,G.player(tracked))
    end)
    add('pickup-flight',function()step(.3);key('s');step(.12)end,function(image)
        assert(#juice.items==2 and tracked.currency==13 and G.player(tracked).hp==14)
        flight_pixels(image)
    end)
    add('pickup-free-action',function()
        step(.1);assert(not love.roagPlaybackPending() and #juice.items==2)
        local clock=tracked.clock;key('space');step(0)
        assert(tracked.clock==clock+100 and #juice.items==2,'pickup accent delayed a legal action')
    end,function(image)flight_pixels(image)end)
    add('pickup-settled',function()step(.6)end,function()
        assert(#juice.items==0 and #flights==0 and not love.roagPlaybackPending())
    end)
    add('chest-offers-arrive',function()key('d');step(0)end,function(image)
        assert(love.roagUIState().page=='chest' and offers[1].y==6)
        offer_initial=rect_pixels(image,96,168,48,204)
    end)
    add('chest-offers-settled',function()step(.3)end,function(image)
        assert(offers[1].y==0 and offers[3].y==0)
        assert(offer_initial~=rect_pixels(image,96,168,48,204),'offer icons did not visibly arrive')
    end)
    add('claimed-boon-flight',function()key('return');step(.11)end,function(image)
        assert(chest.claimed and #juice.items==1 and J.chest_offset(juice,chest.id,true)==-4)
        flight_pixels(image)
        local h=assert(native.handles['core:object/chest_open'])
        local x,y=Camera.tile_top_left(camera,chest.position.x,chest.position.y)
        local found=false
        for _,r in ipairs(drawn)do
            if r.quad==h.quad and r.texture==h.texture and r.x==x and r.y==y-4 and r.sx==2.5 then found=true end
        end
        assert(found,'opened chest was not drawn at its hop position')
    end)
    add('refresh-in-flight',function()key('f5');step(0)end,function(image)
        assert(#juice.items==1);flight_pixels(image)
    end)
    add('effects-off',function()
        step(.6);key('escape');key('3');key('tab');key('down');key('down');key('return')
        key('escape');key('escape');aim();step(.04)
    end,function(image)
        assert(kicks[tracked.player_id][1]==0 and #juice.items==0)
        actor_pixels(image,G.player(tracked))
    end)
    add('save-while-effect-pending',function()
        step(.3)
        -- Restore accents through the same visible Settings control.
        key('escape');key('3');key('tab');key('down');key('down');key('return');key('escape');key('escape')
        aim();step(.04)
        local f=assert(io.open(out..'/expected-save.txt','wb'));f:write(snapshot());f:close()
    end,function()
        assert(kicks[tracked.player_id][1]==3)
        key('escape');key('2')
        local found=Codec.new(require('src.love_storage')):inspect()
        assert(found.status=='active' or found.status=='recovered','Save & Quit did not persist')
        assert(snapshot(found.state)==before,'Save & Quit changed the run')
    end)
elseif mode=='impact'then
    add('hit-level-crumble',function()aim()end,function(image)
        assert(not G.entity(tracked,enemy.id) and tracked.progression.level==2)
        impact_pixels(image,false);crumble_pixels(image,true)
        assert(labels['LEVEL 2'],'level badge was not rendered')
    end)
    add('crumble-spread',function()step(.08)end,function(image)
        crumble_pixels(image,false)
        assert(juice.crumbles[1].age==.08 and labels['LEVEL 2'])
    end)
    add('crumble-refresh',function()key('f5');step(0)end,function(image)
        crumble_pixels(image,false)
    end)
    add('crumble-finished',function()step(.17)end,function()
        assert(#juice.crumbles==0 and #juice.impacts==0 and #juice.celebrations==1)
    end)
    add('level-free-action',function()
        while PB.busy(pb)do step(pb.remaining)end
        assert(#juice.celebrations==1)
        local clock=tracked.clock;key('space');step(0)
        assert(tracked.clock>clock,'celebration gated a legal action')
    end,function()assert(labels['LEVEL 2'])end)
    add('all-settled',function()step(1)end,function()
        assert(#juice.celebrations==0 and #juice.crumbles==0 and not labels['LEVEL 2'])
    end)
elseif mode=='blocked'then
    add('blocked-ring',function()aim()end,function(image)
        assert(enemy.hp==200 and #juice.crumbles==0 and #juice.celebrations==0)
        impact_pixels(image,true)
    end)
    add('blocked-finished',function()step(.2)end,function()assert(#juice.impacts==0)end)
    add('blocked-off',function()
        key('escape');key('3');key('tab');key('down');key('down');key('return')
        key('escape');key('escape');aim()
    end,function()assert(#juice.impacts==0 and #juice.crumbles==0 and #juice.celebrations==0)end)
    add('repeat-hit-stress',function()
        step(.3)
        key('escape');key('3');key('tab');key('down');key('down');key('return')
        key('escape');key('escape')
        local textures=love.graphics.getStats().texturememory
        for _=1,80 do
            aim();assert(#juice.impacts==1)
            step(.25);assert(#juice.impacts==0 and #juice.crumbles==0 and #juice.celebrations==0)
        end
        collectgarbage('collect');assert(love.graphics.getStats().texturememory==textures,
            'repeated impacts allocated textures')
        key('f5');step(0)
    end,function()assert(enemy.hp==200 and #juice.impacts==0)end)
elseif mode=='discovery'then
    add('secret-found',function()
        local s=tracked.secrets[1];local dx,dy=s.wall_x-s.x,s.wall_y-s.y
        key('f');key(dx==1 and 'right' or dx==-1 and 'left' or dy==1 and 'down' or 'up');key('return');step(0)
        while PB.current(pb)and PB.current(pb).kind~='SecretDiscovered'do step(pb.remaining)end
    end,function(image)
        assert(tracked.secrets[1].revealed and #juice.celebrations==1 and labels['SECRET FOUND'])
        local e=juice.celebrations[1];local x,y=Camera.tile_top_left(camera,e.x,e.y)
        local radius=J.celebration_pose(e,40)
        -- Leftmost sparkle avoids the pre-existing secret clue/weapon icon.
        pixel_ink(image,math.floor(x+20-radius),math.floor(y+20),Palette.gold)
    end)
    add('secret-refresh',function()key('f5');step(.2)end,function()assert(labels['SECRET FOUND'])end)
    add('secret-settled',function()step(1)end,function()
        assert(#juice.celebrations==0 and not labels['SECRET FOUND'])
    end)
elseif mode=='enemy'then
    add('enemy-neutral',function()
        key('space');step(2);key('space');step(0)
        while PB.current(pb)and PB.current(pb).kind~='AttackPerformed'do step(pb.remaining)end
        assert(PB.current(pb)and PB.current(pb).source_id==enemy.id,'no committed enemy impact')
    end,function(image)actor_pixels(image,enemy)end)
    add('enemy-recoil',function()step(.06)end,function(image)
        assert(kicks[enemy.id][1]==-3);actor_pixels(image,enemy)
    end)
elseif mode=='resume'then
    add('continued',function()
        key('c');step(0)
        local f=assert(io.open(out..'/expected-save.txt','rb'));assert(snapshot()==f:read('*a'));f:close()
        assert(#juice.items==0 and not love.roagPlaybackPending())
    end,function(image)actor_pixels(image,G.player(tracked))end)
    add('menu-stress',function()
        local textures=love.graphics.getStats().texturememory
        for _=1,200 do key('i');step(.01);key('escape');key('escape');step(.01);key('escape')end
        collectgarbage('collect');assert(love.graphics.getStats().texturememory==textures)
        key('f5');step(0)
    end,function()assert(#juice.items==0 and not love.roagPlaybackPending())end)
end
local pending,captured,failed,started=false,nil,false,false
local function fail(why)
    if captured then pcall(PNG.write,out..'/'..mode..'-failure.png',captured)end
    failed=true;io.stderr:write('JUICE '..mode..'/'..phase..': '..tostring(why)..'\n');love.event.quit(1)
end
function love.errorhandler(why)
    io.stderr:write(debug.traceback(tostring(why),2)..'\n');return function()return 1 end
end
function love.load(...)
    local args={...};local ok,why=xpcall(function()
        load(unpack(args));love.mouse.setPosition(8,8)
        print('JUICE_SAVE_DIR '..love.filesystem.getSaveDirectory())
        source=love.image.newImageData(O.path)
        tracked=runtime_state()or tracked
        if tracked then before=snapshot()end
    end,debug.traceback);if not ok then fail(why)end
end
function love.update()
    if failed then return end
    local ok,why=xpcall(function()
        if not started then started=true;phases[phase].enter();if tracked then before=snapshot()end;return end
        if not captured then return end
        stable();phases[phase].check(captured)
        PNG.write(out..'/'..mode..'-'..phases[phase].name..'.png',captured)
        captured:release();captured=nil;pending=false
        phase=phase+1
        if not phases[phase]then print('PASS visual juice '..mode);love.event.quit();return end
        phases[phase].enter();if tracked then before=snapshot()end
    end,debug.traceback);if not ok then fail(why)end
end
function love.draw()
    if failed or not started then return end
    local ok,why=xpcall(function()
        drawn,kicks,flights,focus,offers,labels={},{},{},{},{},{}
        draw();stable()
        if not pending then
            pending=true
            love.graphics.captureScreenshot(function(image)captured=image end)
        end
    end,debug.traceback);if not ok then fail(why)end
end
