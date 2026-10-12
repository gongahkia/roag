local Game=require('src.game')
local Codec=require('src.persistence')
local Build=require('src.studio_atlas_build')
local Store=require('src.studio_atlas_store')
local tracked
local old_new=Game.new_stage
Game.new_stage=function(...)
    tracked=old_new(...);return tracked
end
assert(love.filesystem.load('roeg_main.lua'))()
local original_load,original_draw=love.load,love.draw
local root=assert(os.getenv('ROEG_E06_APP'))
local output=assert(os.getenv('ROEG_E06_OUTPUT'))
local function read(path)
    local f=io.open(root..'/'..path,'rb')
    if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
local function write(path,bytes)
    local f=assert(io.open(root..'/'..path,'wb'))
    assert(f:write(bytes));assert(f:close())
    assert(read(path)==bytes)
end
local images={}
local phase=1
local capture_pending=false
local original_snapshot
local before_clock
local page_bytes={}
local refresh_ms={}
local initial_heap,initial_texture
local actor_changed,hud_changed
local function changed(a,b,x0,y0,x1,y1)
    local count=0
    for y=y0,y1 do for x=x0,x1 do
        local ar,ag,ab,aa=a:getPixel(x,y)
        local br,bg,bb,ba=b:getPixel(x,y)
        if ar~=br or ag~=bg or ab~=bb or aa~=ba then count=count+1 end
    end end
    return count
end
local function assert_unchanged()
    assert(Codec.encode(Game.snapshot(tracked))==original_snapshot,
        'F5 changed authoritative state')
    assert(tracked.clock==before_clock,'F5 advanced world time')
end
local function f5(expected)
    local started=love.timer.getTime()
    love.keypressed('f5')
    love.update(0)
    refresh_ms[#refresh_ms+1]=(love.timer.getTime()-started)*1000
    assert_unchanged()
    assert(love.roegAssetBacking('core:actor/adventurer')==expected,
        'actor backing '..love.roegAssetBacking('core:actor/adventurer'))
    assert(love.roegAssetBacking('studio:ui/heart')==expected,
        'HUD backing '..love.roegAssetBacking('studio:ui/heart'))
end
function love.load(args)
    original_load(args)
    assert(tracked,'expected isolated normal game startup')
    original_snapshot=Codec.encode(Game.snapshot(tracked))
    before_clock=tracked.clock
    assert(love.roegAssetBacking('core:actor/adventurer')=='atlas')
    assert(love.roegAssetBacking('studio:ui/heart')=='atlas')
    initial_heap=collectgarbage('count')
    initial_texture=love.graphics.getStats().texturememory or 0
end
function love.draw()
    original_draw()
    if capture_pending then return end
    capture_pending=true
    love.graphics.captureScreenshot(function(img)
        images[phase]=img
        local bytes=img:encode('tga'):getString()
        local f=assert(io.open(output..'/frame-'..phase..'.tga','wb'))
        assert(f:write(bytes));assert(f:close())
        if phase==1 then
            write('assets/studio/sources/actor.png',assert(read('alternate-actor.png')))
            write('assets/studio/sources/heart.png',assert(read('alternate-icon.png')))
            f5('loose')
            assert(love.roegAtlasStatus():match('stale'))
        elseif phase==2 then
            actor_changed=changed(images[1],images[2],16,16,720,590)
            hud_changed=changed(images[1],images[2],980,55,1008,88)
            assert(actor_changed>20,
                'actor did not change visibly after F5')
            assert(hud_changed>20,
                'HUD icon did not change visibly after F5')
            local pack,pages=Build.compile(read,64)
            for path,page in pairs(pages) do write(path,page);page_bytes[path]=page end
            local plan=Store.plan(pack,read)
            write(plan.path,plan.bytes)
            f5('atlas')
        elseif phase==3 then
            assert(changed(images[2],images[3],16,16,720,590)==0,
                'atlas actor differs from loose actor')
            assert(changed(images[2],images[3],980,55,1008,88)==0,
                'atlas HUD differs from loose HUD')
            local path=next(page_bytes)
            assert(path)
            write(path,'corrupt')
            f5('loose')
        elseif phase==4 then
            local path=next(page_bytes)
            write(path,page_bytes[path])
            f5('atlas')
            collectgarbage('collect')
            local before_repeat=collectgarbage('count')
            for _=1,50 do f5('atlas') end
            collectgarbage('collect')
            print(('E06 repeated unchanged F5 heap %.0f→%.0fKiB'):
                format(before_repeat,collectgarbage('count')))
            assert(love.roegVisualRefreshStatus():match('checked'),
                'repeat F5 should report unchanged')
        elseif phase==5 then
            assert(changed(images[3],images[5],16,16,720,590)==0,
                'recovered atlas pixels differ')
            assert_unchanged()
            local theme_path='assets/studio/theme-a.pack'
            local valid_theme=assert(read(theme_path))
            write(theme_path,'corrupt theme generation')
            love.keypressed('f5');love.update(0)
            assert(love.roegVisualRefreshStatus():match('rejected'),
                'bad theme should reject candidate')
            assert(love.roegAssetBacking('studio:ui/heart')=='atlas')
            assert_unchanged()
            write(theme_path,valid_theme)
            f5('atlas')
            local actor_path='assets/studio/sources/actor.png'
            local valid_actor=assert(read(actor_path))
            write(actor_path,'not a PNG')
            love.keypressed('f5');love.update(0)
            assert(love.roegVisualRefreshStatus():match('rejected'),
                'bad loose PNG should reject candidate')
            assert(love.roegAssetBacking('core:actor/adventurer')=='atlas')
            assert_unchanged()
            write(actor_path,valid_actor)
            f5('atlas')
            local World=require('src.studio_world')
            local WorldRuntime=require('src.studio_world_runtime')
            local world=World.new('e06_visuals')
            world.tiles[1].asset_id='studio:tile/missing_visual'
            local world_path='assets/studio/world-a.pack'
            write(world_path,WorldRuntime.record(1,world))
            love.keypressed('f5');love.update(0)
            assert(love.roegVisualRefreshStatus():match('rejected'),
                'world reference mismatch should reject candidate')
            assert_unchanged()
            assert(os.remove(root..'/'..world_path))
            f5('atlas')
            table.sort(refresh_ms)
            local p50=refresh_ms[math.ceil(#refresh_ms*.50)]
            local p95=refresh_ms[math.ceil(#refresh_ms*.95)]
            local vendor,renderer,version,device=love.graphics.getRendererInfo()
            collectgarbage('collect')
            local final_texture=love.graphics.getStats().texturememory or 0
            print(('E06 refresh n=%d p50=%.3fms p95=%.3fms max=%.3fms LuaHeap=%.0f→%.0fKiB Texture=%.0f→%.0fB renderer=%s %s %s %s'):
                format(#refresh_ms,p50,p95,refresh_ms[#refresh_ms],
                    initial_heap,collectgarbage('count'),initial_texture,
                    final_texture,vendor,renderer,version,device))
            print('E06 live actor/HUD F5 atlas → stale loose → rebuilt atlas → corrupt loose → recovered atlas; invalid theme/PNG/world rejected; snapshots exact')
            print(('E06 changed pixels actor=%d HUD=%d'):format(actor_changed,hud_changed))
            love.event.quit()
        end
        phase=phase+1;capture_pending=false
    end)
end
