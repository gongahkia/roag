local Document=require('src.studio_document')
assert(love.filesystem.load('roag_main.lua'))()
local old_load,old_draw=love.load,love.draw
local mode=assert(os.getenv('ROAG_E06_STUDIO_MODE'))
local output=assert(os.getenv('ROAG_E06_OUTPUT'))
local phase=1
local pending=false
local before_image
local function state() return assert(love.roagStudioState()) end
local function input(value)
    for _=1,100 do love.keypressed('backspace') end
    love.textinput(value);love.keypressed('return')
end
local function difference(a,b,x0,y0,x1,y1)
    local changed=0
    for y=y0,y1 do for x=x0,x1 do
        local ar,ag,ab,aa=a:getPixel(x,y)
        local br,bg,bb,ba=b:getPixel(x,y)
        if ar~=br or ag~=bg or ab~=bb or aa~=ba then changed=changed+1 end
    end end
    return changed
end
function love.load(args)
    old_load(args)
    if mode=='create' then
        love.keypressed('n');love.textinput('E06 Draft');love.keypressed('return')
        love.keypressed('c');input('16x16')
        assert(state().mode=='canvas')
        love.keypressed('tab')
        assert(state().draft_preview)
    else
        love.keypressed('o')
        love.keypressed('return')
        assert(state().project and state().project.id=='e06_draft')
        love.keypressed('e')
        assert(state().mode=='canvas')
        local pixel=Document.composite_pixel(state().canvas.doc,2,2)
        assert(pixel[4]>0,'painted source pixel lost after restart')
        love.keypressed('tab')
        assert(state().draft_preview)
    end
end
function love.draw()
    old_draw()
    if pending then return end
    pending=true
    love.graphics.captureScreenshot(function(img)
        local name=mode..'-'..phase..'.tga'
        local f=assert(io.open(output..'/'..name,'wb'))
        assert(f:write(img:encode('tga'):getString()));assert(f:close())
        if mode=='create' and phase==1 then
            before_image=img
            love.keypressed('tab')
            local canvas=state().canvas
            local ox,oy=canvas:origin()
            local x=ox+2.5*canvas.zoom;local y=oy+2.5*canvas.zoom
            love.mousepressed(x,y,1)
            love.mousemoved(x+6*canvas.zoom,y,6*canvas.zoom,0)
            love.mousereleased(x+6*canvas.zoom,y,1)
            assert(canvas:dirty())
            love.keypressed('tab')
        elseif mode=='create' and phase==2 then
            assert(difference(before_image,img,400,190,580,390)>15,
                'unsaved draft pixels not visible in preview')
            love.keypressed('tab')
            love.keypressed('s')
            assert(not state().dirty,'save did not clear dirty state')
            love.keypressed('escape')
            love.keypressed('p')
            assert(state().mode=='pipeline' and state().pipeline_report,
                'publish readiness not available')
        elseif mode=='create' and phase==3 then
            print('E06 Studio unsaved draft preview, saved source and publish readiness PASS')
            love.event.quit()
        else
            print('E06 Studio fresh-process painted source reopen PASS')
            love.event.quit()
        end
        phase=phase+1;pending=false
    end)
end
