-- Actual main.lua input/render callbacks with graphics stubs; no LÖVE window.
local Settings=require('src.presentation_settings')
local Game=require('src.game')
local Fonts=require('src.ui_fonts')
local function with_love(fake,fn)
    assert(_G.love==nil)
    _G.love=fake
    local okay,why=pcall(fn)
    _G.love=nil
    assert(okay,why)
end
local function fixture()
    local f={writes=0,font_allocations=0,audio_changes=0,text={},rects={}}
    local function font(points)
        return {points=points,getWidth=function(_,s)return #s*points*.6 end,
            getHeight=function()return points end,setFilter=function()end,
            setFallbacks=function()end}
    end
    local current=font(12)
    local fake={graphics=setmetatable({
        newFont=function(path,points)
            f.font_allocations=f.font_allocations+1
            return font(points or path)
        end,
        getFont=function()return current end,setFont=function(value)current=value end,
        getDimensions=function()return 1024,640 end,getWidth=function()return 1024 end,
        print=function(text,x,y)f.text[#f.text+1]={text=text,x=x,y=y,font=current}end,
        printf=function(text,x,y,width,align)
            f.text[#f.text+1]={text=text,x=x,y=y,width=width,align=align,font=current}
        end,
        rectangle=function(mode,x,y,w,h)
            f.rects[#f.rects+1]={mode=mode,x=x,y=y,w=w,h=h}
        end,
    },{__index=function()return function()end end}),
        audio={setVolume=function(value)f.master=value;f.audio_changes=f.audio_changes+1 end}}
    local chunk=assert(loadfile('main.lua'))
    setfenv(chunk,setmetatable({love=fake},{__index=_G}));chunk()
    local function walk(fn,callback,seen)
        if seen[fn]then return end;seen[fn]=true
        local children={}
        for i=1,200 do
            local name,value=debug.getupvalue(fn,i);if not name then break end
            callback(fn,i,name,value)
            if type(value)=='function'then children[#children+1]=value end
        end
        for _,child in ipairs(children)do walk(child,callback,seen)end
    end
    local function visit(callback)
        local seen={}
        for _,name in ipairs({'draw','update','keypressed','mousepressed',
            'mousemoved','mousereleased','focus','wheelmoved'})do
            walk(fake[name],callback,seen)
        end
    end
    function f.set(key,value)
        local found=false
        visit(function(fn,i,name)
            if name==key then debug.setupvalue(fn,i,value);found=true end
        end)
        assert(found,'missing fixture upvalue: '..key)
        if jit then jit.flush()end
    end
    function f.get(key)
        local result
        visit(function(_,_,name,value)if name==key then result=value end end)
        return result
    end
    local memory={}
    function memory:read(name)return self[name]end
    function memory:write(name,bytes)
        assert(name==Settings.file,'settings must not write saves or profiles')
        if f.fail_write then return false end
        self[name]=bytes;f.writes=f.writes+1;return true
    end
    f.state=Game.new(17);f.settings=Settings.defaults();f.disk=memory;f.love=fake
    f.set('state',f.state);f.set('title',true);f.set('title_view','settings')
    f.set('storage',memory);f.set('presentation_settings',f.settings)
    f.set('ui_body_font',font(12));f.set('ui_display_font',font(24));f.set('ui_menu_font',font(16))
    f.set('audio_runtime',{set_user_mix=function(_,music,effects)f.music,f.effects=music,effects end,
        emit=function()end})
    return f
end
return function(test,equal)
    test('volume percentages and all three text sizes persist with existing preference compatibility',function()
        local disk={read=function(self,k)return self[k]end,
            write=function(self,k,v)self[k]=v;return true end}
        for _,size in ipairs({'small','normal','large'})do
            for _,value in ipairs({0,1,37,99,100})do
                local prefs=Settings.defaults();prefs.text_size=size
                prefs.master,prefs.music,prefs.effects=value,value,value
                assert(Settings.save(disk,prefs))
                local loaded,status=Settings.load(disk)
                assert(status=='loaded');equal(loaded,prefs)
            end
        end
        -- Existing version-1 normal/large and multiples-of-ten files still load.
        for _,size in ipairs({'normal','large'})do
            local old=Settings.defaults();old.text_size=size
            disk[Settings.file]=require('src.persistence').encode(old)
            local loaded,status=Settings.load(disk)
            assert(status=='loaded');equal(loaded,old)
        end
        for _,value in ipairs({-1,100.1,101,55.5,'50',math.huge,0/0})do
            local prefs=Settings.defaults();prefs.music=value
            assert(not Settings.save(disk,prefs))
        end
        local bad=Settings.defaults();bad.text_size='tiny'
        assert(not Settings.save(disk,bad))
    end)
    test('small normal and large load distinct UI fonts with safe glyph fallback in both faces',function()
        with_love({graphics={}},function()
            local fallback={};local requests={}
            love.graphics.getFont=function()return fallback end
            love.graphics.newFont=function(path,points)
                local face={points=points or path}
                function face:setFilter(a,b)self.filter={a,b}end
                function face:setFallbacks(other)self.fallback=other end
                requests[#requests+1]={path=path,face=face};return face
            end
            for _,style in ipairs({'pixel','sans'})do
                for size,expected in pairs({small={10,20,14},normal={12,24,16},large={14,26,18}})do
                    local faces={Fonts.load(style,size)}
                    for i,face in ipairs(faces)do
                        assert(face.points==expected[i] and face.fallback==fallback)
                        if style=='pixel'then equal(face.filter,{'nearest','nearest'})
                        else assert(face.filter==nil)end
                    end
                end
            end
            assert(#requests==18)
        end)
    end)
    test('real volume sliders drag clamp render and save without font churn or authoritative changes',function()
        local f=fixture();local before=Game.snapshot(f.state)
        with_love(f.love,function()
            love.mousepressed(164,228,1) -- Label selects only.
            love.mousemoved(770,228);love.mousereleased(770,228,1)
            assert(f.writes==0 and f.settings.master==100)
            love.mousepressed(625,228,2);love.mousemoved(480,228)
            assert(f.settings.master==100) -- Right click cannot start a drag.
            for i,key in ipairs({'master','music','effects'})do
                local y=228+(i-1)*70
                local writes=f.writes
                love.mousepressed(625,y,1)
                assert(f.settings[key]==50 and f.writes==writes)
                local updates=f.audio_changes
                for _=1,200 do love.mousemoved(625.1,y)end
                assert(f.settings[key]==50 and f.audio_changes==updates)
                love.mousemoved(-100,y);assert(f.settings[key]==0)
                love.mousemoved(2000,y);assert(f.settings[key]==100)
                love.mousemoved(587.3,y);assert(f.settings[key]==37)
                assert(f.writes==writes) -- Preview audio, no per-motion disk writes.
                love.mousereleased(587.3,y,1)
                assert(f.writes==writes+1)
                local saved,status=Settings.load(f.disk)
                assert(status=='loaded' and saved[key]==37)
                love.mousemoved(480,y);assert(f.settings[key]==37)
            end
            assert(f.font_allocations==0 and f.master==.37 and f.music==.37 and f.effects==.37)
            -- Keyboard remains usable and saves once without recreating fonts.
            love.keypressed('right');assert(f.settings.effects==47 and f.font_allocations==0)
            f.get('draw_settings')()
            local values,thumbs=0,0
            for _,text in ipairs(f.text)do
                assert(text.text~='+' and text.text~='−')
                if tostring(text.text):match('^%d+%%$')then
                    values=values+1
                    assert(text.x==794 and text.width==65 and text.align=='right')
                end
                if text.text=='Music & ambience'then
                    assert(text.x+text.font:getWidth(text.text)<480)
                end
            end
            for _,rect in ipairs(f.rects)do
                if rect.w==10 and rect.h==22 then
                    thumbs=thumbs+1;assert(rect.x>=475 and rect.x+rect.w<794)
                end
            end
            assert(values==3 and thumbs==3)
        end)
        equal(Game.snapshot(f.state),before)
    end)
    test('settings navigation commits drags safely and small text cycles and reopens from title and pause',function()
        local f=fixture();local before=Game.snapshot(f.state)
        with_love(f.love,function()
            love.mousepressed(596,228,1);love.focus(false)
            assert(f.settings.master==40 and f.writes==1)
            love.mousemoved(770,228);assert(f.settings.master==40)
            love.mousepressed(625,298,1);love.keypressed('tab')
            assert(f.settings.music==50 and f.writes==2)
            love.mousemoved(770,298);assert(f.settings.music==50)
            love.keypressed('tab');love.keypressed('down') -- Accessibility / text size.
            love.keypressed('left');assert(f.settings.text_size=='small')
            assert(Settings.load(f.disk).text_size=='small')
            f.get('draw_settings')()
            local small=false
            for _,text in ipairs(f.text)do if text.text=='SMALL'then small=true end end
            assert(small)
            love.keypressed('return');assert(f.settings.text_size=='normal')
            love.keypressed('right');assert(f.settings.text_size=='large')
            love.mousepressed(600,298,1);assert(f.settings.text_size=='small')
            love.keypressed('escape');assert(love.roagUIState().page=='home')
            love.mousemoved(770,228);assert(f.settings.master==40)
            -- Same controls in a paused live run; failed storage is visible.
            f.set('title',false);f.set('pause_menu',true);f.set('pause_settings',true)
            f.set('settings_page',1);f.fail_write=true
            love.mousepressed(625,368,1);love.mousereleased(625,368,1)
            assert(f.settings.effects==50 and f.get('settings_message'):find('not saved',1,true))
            f.fail_write=false
            love.mousepressed(683,368,1);love.keypressed('escape')
            assert(f.settings.effects==70 and not f.get('pause_settings'))
            assert(Settings.load(f.disk).effects==70)
            love.mousemoved(770,368);assert(f.settings.effects==70)
        end)
        equal(Game.snapshot(f.state),before)
    end)
    test('inventory groups utility and close instructions in the boon footer with matching click target',function()
        local f=fixture()
        for i=1,7 do
            Game.set_boon_stacks(f.state,f.state.player_id,
                require('src.boon_content').order[i],'common',1)
        end
        local before=Game.snapshot(f.state)
        f.set('title',false);f.set('inventory_open',true)
        with_love(f.love,function()
            f.get('draw_inventory')()
            local utility,close,page=false,false,false
            for _,text in ipairs(f.text)do
                assert(not tostring(text.text):find('no world time',1,true))
                if text.text=='U · UTILITY / ITEMS'then
                    utility=true;assert(text.x==88 and text.y==492 and text.width==488)
                elseif text.text=='I / Esc close'then
                    close=true;assert(text.x==88 and text.y==518)
                elseif tostring(text.text):find('PAGE ',1,true)==1 then
                    page=true;assert(text.y==466)
                end
            end
            assert(utility and close and page)
            love.mousepressed(700,495,1) -- Former character-panel action is gone.
            assert(love.roagUIState().page=='inventory')
            love.mousepressed(300,500,1)
            assert(love.roagUIState().page=='interaction')
            love.keypressed('escape');love.keypressed('i');love.keypressed('u')
            assert(love.roagUIState().page=='interaction')
            f.get('draw_help')()
            for _,text in ipairs(f.text)do
                assert(not tostring(text.text):find('no world time',1,true))
            end
        end)
        equal(Game.snapshot(f.state),before)
    end)
end
