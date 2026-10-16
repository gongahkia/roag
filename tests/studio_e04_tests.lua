local Theme=require('src.studio_theme')
local Store=require('src.studio_theme_store')
local Runtime=require('src.studio_theme_runtime')
local Project=require('src.studio_project')
local Editor=require('src.studio_theme_editor')
local Game=require('src.game')
local function asset(id,kind,w,h)
    local a=Project.asset(id,id,kind)
    a.source='sources/example.png';a.format='png';a.width=w;a.height=h
    return a
end
local function fixture()
    local theme=Theme.new('e04_fixture')
    local assets={
        asset('studio:ui/panel','ui_panel',16,16),
        asset('studio:ui/heart','ui_icon',16,16),
        asset('studio:ui/xp','ui_icon',16,16),
        asset('studio:ui/coin','ui_icon',16,16),
        asset('studio:bg/title','ui_background',32,16),
        asset('studio:font/hud','bitmap_font',64,8),
        asset('studio:effect/hit','effect_sprite',16,16),
        asset('studio:effect/boon','effect_sprite',16,16),
    }
    theme.slots.hud_panel=assets[1].id;theme.slots.hp_icon=assets[2].id
    theme.slots.xp_icon=assets[3].id;theme.slots.currency_icon=assets[4].id
    theme.slots.title_background=assets[5].id
    theme.slots.bitmap_font=assets[6].id
    theme.slots.hit_effect=assets[7].id;theme.slots.boon_effect=assets[8].id
    return theme,assets
end
local function memory()
    local files,fail={},false
    local io={}
    function io:read(path) return files[path] end
    function io:write(path,bytes)
        files[path]=fail and bytes:sub(1,math.floor(#bytes/2)) or bytes
        return true
    end
    function io:mkdir() return true end
    return io,files,function(v)fail=v end
end
return function(test,equal)
    test('E04 theme IDs, slots and kinds are validated without touching pixels',function()
        local theme,assets=fixture()
        assert(Theme.validate(theme,assets))
        local wrong=Theme.decode(Theme.encode(theme))
        wrong.slots.hp_icon='studio:font/hud'
        assert(not pcall(Theme.validate,wrong,assets))
        wrong.slots.hp_icon='studio:ui/missing'
        assert(not pcall(Theme.validate,wrong,assets))
        wrong.slots.hp_icon='../escape'
        assert(not pcall(Theme.validate,wrong))
        local bad=asset('studio:ui/heart','ui_icon',16,16)
        assets[#assets+1]=bad
        assert(Project.valid_id(theme.slots.hp_icon))
    end)
    test('E04 nine-slice preserves corners at normal and tiny destinations',function()
        local panel={left=4,right=4,top=4,bottom=4}
        local pieces=Theme.nine_slice(16,16,panel,280,608)
        assert(#pieces==9 and pieces[1].sw==4 and pieces[1].w==4)
        assert(pieces[9].x+pieces[9].w==280 and pieces[9].y+pieces[9].h==608)
        local tiny=Theme.nine_slice(16,16,panel,5,5)
        for _,piece in ipairs(tiny) do
            assert(piece.x>=0 and piece.y>=0 and piece.x+piece.w<=5.00001 and
                piece.y+piece.h<=5.00001)
        end
        assert(not pcall(Theme.nine_slice,8,8,{left=5,right=5,top=0,bottom=0},20,20))
    end)
    test('E04 bitmap mapping rejects duplicates and falls back for missing glyphs',function()
        local theme,assets=fixture()
        local glyphs=Theme.glyphs(theme.font,64,8)
        assert(glyphs.H.x==0 and glyphs.P.x==4 and glyphs['/'].x==52)
        assert(glyphs['?']==nil)
        theme.font.characters='HHP'
        assert(not pcall(Theme.validate,theme,assets))
        theme.font.characters='HP0123456789:/'
        assets[6].width=16
        assert(not pcall(Theme.validate,theme,assets))
    end)
    test('E04 background fit, cover and center preserve aspect ratio',function()
        local x,y,w,h=Theme.background_rect('fit',32,16,800,600)
        assert(x==0 and y==100 and w==800 and h==400)
        x,y,w,h=Theme.background_rect('cover',32,16,800,600)
        assert(x==-200 and y==0 and w==1200 and h==600)
        x,y,w,h=Theme.background_rect('center',32,16,800,600)
        assert(x==384 and y==292 and w==32 and h==16)
    end)
    test('E04 effect presets accept only visible canonical playback events',function()
        local theme,assets=fixture()
        assert(Theme.validate(theme,assets))
        local preset,id=Theme.effect_for(theme,{kind='DamageTaken',x=3,y=4},true)
        assert(preset.event=='DamageTaken' and id=='studio:effect/hit')
        assert(Theme.effect_for(theme,{kind='DamageTaken',x=3,y=4},false)==nil)
        assert(Theme.effect_for(theme,{kind='Moved',x=3,y=4},true)==nil)
        assert(Theme.effect_for(theme,{kind='BoonActivated',x=3,y=4},true))
        theme.effects.hit.duration_ms=2000
        assert(not pcall(Theme.validate,theme,assets))
    end)
    test('E04 source codec is deterministic and two generations recover tears',function()
        local theme,assets=fixture()
        local bytes=Theme.encode(theme)
        equal(theme,Theme.decode(bytes))
        assert(bytes==Theme.encode(Theme.decode(bytes)))
        local io,files,fail=memory();local store=Store.new(io)
        assert(store:save(theme,assets))
        theme.name='Second theme';assert(store:save(theme,assets))
        local restored=assert(store:open('e04_fixture',assets))
        assert(restored.name=='Second theme')
        theme.name='Third theme';fail(true)
        assert(not store:save(theme,assets))
        local recovered,status=store:open('e04_fixture',assets)
        assert(recovered.name=='Second theme' and status:find('recovered'))
        assert(files['studio/projects/e04_fixture/theme-a.txt'])
    end)
    test('E04 valid incompatible newest is rejected without stale rollback',function()
        local theme,assets=fixture()
        local io,files=memory();local store=Store.new(io)
        assert(store:save(theme,assets));assert(store:save(theme,assets))
        local newest=files['studio/projects/e04_fixture/theme-b.txt']
        -- A valid checksum with a future source schema must not load the old slot.
        local future=Theme.decode(Theme.encode(theme));future.schema='future-theme/2'
        local Codec=require('src.persistence')
        local payload=Codec.encode(future)
        local a,b=1,0
        for i=1,#payload do a=(a+payload:byte(i))%65521;b=(b+a)%65521 end
        files['studio/projects/e04_fixture/theme-b.txt']=
            ('ROAG-STUDIO-THEME-SOURCE/1\n2\n%d\n%08x\n'):format(
                #payload,b*65536+a)..payload
        local opened,reason=store:open('e04_fixture',assets)
        assert(not opened and reason:find('incompatible'))
        files['studio/projects/e04_fixture/theme-b.txt']=newest
    end)
    test('E04 published pack validates references and corrupt fallback',function()
        local theme,assets=fixture()
        local files={}
        local read=function(path)return files[path] end
        local plan=Store.plan_published(theme,read)
        files[plan.path]=plan.bytes
        local visuals={status='loaded',project_id='e04_fixture',by_id={},
            get_id=function()return true end}
        for _,a in ipairs(assets) do visuals.by_id[a.id]=a end
        local loaded,status=Runtime.inspect(read,visuals)
        assert(loaded and status=='loaded')
        local next_plan=Store.plan_published(theme,read)
        files[next_plan.path]=next_plan.bytes:sub(1,10)
        loaded,status=Runtime.inspect(read,visuals)
        assert(loaded and status:find('recovered'))
        visuals.by_id['studio:ui/heart']=nil
        assert(not Runtime.inspect(read,visuals))
    end)
    test('E04 valid incompatible published newest refuses older theme',function()
        local theme,assets=fixture()
        local files={};local read=function(path)return files[path] end
        local first=Store.plan_published(theme,read);files[first.path]=first.bytes
        local future=Theme.decode(Theme.encode(theme));future.schema='future-theme/2'
        local payload=require('src.persistence').encode(future)
        local a,b=1,0
        for i=1,#payload do a=(a+payload:byte(i))%65521;b=(b+a)%65521 end
        files['assets/studio/theme-b.pack']=
            ('ROAG-STUDIO-PUBLISHED-THEME/1\n2\n%d\n%08x\n'):format(
                #payload,b*65536+a)..payload
        local loaded,reason=Store.inspect_published(read)
        assert(not loaded and reason:find('incompatible'))
    end)
    test('E04 metadata edits and cosmetics leave game time and RNG untouched',function()
        local theme,assets=fixture()
        local editor=Editor.new(theme,assets,function()return nil end)
        editor.selected=2;editor:cycle_asset(1)
        editor:start_input('name');editor.typed='Quiet theme';editor:commit_input()
        assert(editor:dirty());editor:undo();editor:redo()
        editor:keypressed('r');editor:keypressed('escape')
        assert(editor:dirty())
        editor:keypressed('r');editor:keypressed('return')
        assert(not editor:dirty())
        editor:keypressed('h');editor.typed='HP 7/20 LV 2 GOLD 5'
        editor:keypressed('return')
        assert(editor.preview_text=='HP 7/20 LV 2 GOLD 5' and not editor:dirty())
        local state=Game.new(81)
        local before=Game.snapshot(state)
        Theme.nine_slice(16,16,theme.panel,280,608)
        Theme.glyphs(theme.font,64,8)
        Theme.effect_for(theme,{kind='DamageTaken',x=2,y=2},true)
        equal(before,Game.snapshot(state))
    end)
    test('individual Artwork UI and uppercase/lowercase glyphs render without a theme pack',function()
        local Project=require('src.studio_project')
        local Slots=require('src.visual_slots')
        local assets={}
        local function asset(id,kind,binding,w,h)
            local a=Project.asset(id,id,kind)
            a.binding=binding;a.source='sources/'..id:match('/([^/]+)$')..'.png'
            a.format='png';a.width=w;a.height=h
            assets[id]=a
        end
        asset('studio:art/hp','ui_icon','core:ui/hp_icon',16,16)
        asset('studio:art/upper','glyph',Slots.glyph_binding('A'),4,8)
        asset('studio:art/lower','glyph',Slots.glyph_binding('a'),4,8)
        local visuals={status='loaded',project_id='roag',by_id=assets,bindings={}}
        for _,a in pairs(assets) do visuals.bindings[a.binding]=a end
        function visuals:resolve_id(id)
            local a=self.by_id[id]
            return a and {texture=id,width=a.width,height=a.height}
        end
        function visuals:resolve(binding)
            local a=self.bindings[binding]
            return a and self:resolve_id(a.id)
        end
        local runtime,status=Runtime.inspect(function()return nil end,visuals)
        assert(runtime and status=='loaded')
        assert(runtime.theme.slots.hp_icon=='studio:art/hp')
        local stale=Theme.new('roag')
        stale.slots.xp_icon='studio:ui/missing'
        local plan=Store.plan_published(stale,function()return nil end)
        local recovered=Runtime.inspect(function(path)
            return path==plan.path and plan.bytes or nil
        end,visuals)
        assert(recovered and recovered.theme.slots.hp_icon=='studio:art/hp'
            and recovered.theme.slots.xp_icon=='' and recovered.warning)
        local previous=_G.love
        local draws,printed={},{}
        _G.love={graphics={setColor=function()end,
            draw=function(texture,_,x)draws[#draws+1]={texture,x} end,
            print=function(char)printed[#printed+1]=char end,
            getFont=function()return {getWidth=function()return 4 end} end}}
        local ok,reason=pcall(function()
            assert(runtime:draw_text('Aa?',0,0,2))
        end)
        _G.love=previous
        assert(ok,reason)
        assert(#draws==2 and draws[1][1]=='studio:art/upper' and
            draws[2][1]=='studio:art/lower')
        assert(#printed==1 and printed[1]=='?')
    end)
end
