local P=require('src.visual_palette')
local G=require('src.game')
local Shader=require('src.onebit_shader')
local function graphics()
    local g={records={},color={1,1,1,1}}
    function g.newShader(source)
        assert(source:find('vec3(gray) * ink',1,true) and source:find('pixel.a',1,true))
        return {send=function(self,key,value)assert(key=='ink');self.ink=value end}
    end
    function g.getShader()return g.shader end
    function g.setShader(value)g.shader=value end
    function g.setColor(r,b,c,a)g.color=type(r)=='table' and r or {r,b,c,a or 1}end
    local function record(kind,...)
        local rgb={g.color[1],g.color[2],g.color[3],g.color[4]or 1}
        if g.shader then
            local gray=rgb[1]*.2126+rgb[2]*.7152+rgb[3]*.0722
            for i=1,3 do rgb[i]=gray*g.shader.ink[i]end
        end
        g.records[#g.records+1]={kind=kind,color=rgb,args={...}}
    end
    function g.draw(...)record('sprite',...)end
    function g.rectangle(...)record('rect',...)end
    function g.print(...)record('text',...)end
    local font={getWidth=function(_,s)return #s*8 end,getHeight=function()return 16 end}
    function g.getFont()return font end
    for _,key in ipairs({'setFont','push','pop','translate','scale','ellipse','circle','line','polygon'})do
        g[key]=function()end
    end
    return g,font
end
local function callbacks()
    assert(not _G.love)
    local g,font=graphics();local fake={graphics=g}
    local chunk=assert(loadfile('main.lua'))
    setfenv(chunk,setmetatable({love=fake},{__index=_G}));chunk()
    local function visit(callback)
        local seen={}
        local function walk(fn)
            if seen[fn]then return end;seen[fn]=true
            local children={}
            for i=1,200 do
                local name,value=debug.getupvalue(fn,i);if not name then break end
                callback(fn,i,name,value)
                if type(value)=='function'then children[#children+1]=value end
            end
            for _,child in ipairs(children)do walk(child)end
        end
        for _,fn in ipairs({fake.draw,fake.keypressed})do walk(fn)end
    end
    local function set(key,value)
        local found=false
        visit(function(fn,i,name)if name==key then debug.setupvalue(fn,i,value);found=true end end)
        assert(found,'fixture upvalue missing: '..key);if jit then jit.flush()end
    end
    local function get(key)
        local result;visit(function(_,_,name,value)if name==key then result=value end end)
        return assert(result,'fixture value missing: '..key)
    end
    local state=G.new(17)
    set('state',state);set('title',false)
    set('camera',require('src.camera').new(require('src.content').map_for(state),
        {x=16,y=16,width=992,height=520},40,2,2))
    set('ui_body_font',font);set('ui_display_font',font)
    return {g=g,state=state,set=set,get=get}
end
local function near(a,b)assert(math.abs(a-b)<1e-6,a..' ~= '..b)end
local function rgb(record,ink,scale)
    for i=1,3 do near(record.color[i],ink[i]*(scale or 1))end
end
return function(test,equal)
    test('semantic reward and enemy inks preserve shared handles, atlas geometry and generation ownership',function()
        local base={texture={},quad={},width=16,height=16,tint={.9,.9,.9},offset_x=17,offset_y=32}
        local assets={resolve=function()return base end}
        local actor=P.resolve(assets,'core:actor/adventurer')
        local enemy=P.resolve(assets,'core:enemy/mossbound_guard/attack')
        local boon=P.resolve(assets,'core:boon/emberbrand/icon')
        assert(actor==base and enemy~=boon and base.ink==nil)
        assert(enemy.ink==P.enemy and boon.ink==P.gold)
        for _,h in ipairs({enemy,boon})do
            assert(h.texture==base.texture and h.quad==base.quad and h.offset_x==17 and
                h.offset_y==32 and h.width==16 and h.height==16)
        end
        for _=1,1000 do assert(P.resolve(assets,'core:boon/emberbrand/icon')==boon)end
        assert(P.binding('core:object/idea_cache_dormant')==P.chest,'dormant mimic must resemble loot')
        assert(P.binding('core:object/chest_open')==P.spent)
        for _,id in ipairs(require('src.boon_content').order)do assert(P.binding(id..'/icon')==P.gold)end
        for _,kind in ipairs({'health','currency','food'})do assert(P.binding('core:drop/'..kind))end
        assert(not P.binding('core:terrain/bramble_bed') and not P.binding(nil))
        local fresh={resolve=assets.resolve}
        assert(P.resolve(fresh,'core:boon/emberbrand/icon')~=boon,'cache crossed resource generations')
    end)
    test('palette shader scopes ink and restores neutral drawing; failed uniform initialization falls back once',function()
        local g=graphics();local prior={};g.shader=prior
        local filter=Shader.new();local old,applied=filter:begin(g)
        assert(applied and old==prior)
        local neutral=filter:set_ink(g,P.hostile);equal(g.shader.ink,P.hostile)
        local red=filter:set_ink(g,P.friendly);equal(g.shader.ink,P.friendly)
        filter:set_ink(g,red);equal(g.shader.ink,P.hostile)
        filter:set_ink(g,neutral);equal(g.shader.ink,{1,1,1})
        filter:finish(g,old,applied);assert(g.shader==prior)
        local calls,warnings=0,0
        local bad={newShader=function()
            calls=calls+1;return {send=function()error('uniform unavailable')end}
        end,getShader=function()error('failed candidate must not activate')end}
        local failed=Shader.new()
        for _=1,30 do local _,active=failed:begin(bad,function()warnings=warnings+1 end);assert(not active)end
        assert(calls==1 and warnings==1)
    end)
    test('rainbow damage ink is deterministic, gently pulses, spans hues and leaves blocked zeros calm',function()
        local state=G.new(17);local before=G.snapshot(state)
        local hit={x=3,y=2,amount=4,age=0};local dominant={};local brightness={}
        for step=0,77 do
            hit.age=step/100
            local first=P.damage(hit);equal(first,P.damage(hit))
            local best=1
            for i,v in ipairs(first)do assert(v>=.159 and v<=1);if v>first[best]then best=i end end
            dominant[best]=true;brightness[#brightness+1]=first[best]
        end
        assert(dominant[1] and dominant[2] and dominant[3])
        local low,high=1,0
        for _,v in ipairs(brightness)do low=math.min(low,v);high=math.max(high,v)end
        assert(low>=.8-1e-6 and high-low>.1,'pulse must be visible without blinking')
        assert(P.damage({amount=0,age=999})==P.enemy)
        equal(G.snapshot(state),before)
    end)
    test('real sprite and tactical draw callbacks keep neutral terrain and separate enemy red from player green',function()
        local f=callbacks();local s=f.state;local before=G.snapshot(s)
        local base={texture={},quad={},width=16,height=16,tint={.9,.9,.9}}
        local assets={resolve=function()return base end}
        f.set('visual_assets',assets);f.set('visible_at',function()return true end)
        local filter=f.get('onebit_filter');local old,applied=filter:begin(f.g)
        for _,case in ipairs({{'core:drop/currency',P.gold},{'core:chest/standard',P.chest},
            {'core:enemy/mossbound_guard',P.enemy},{'core:actor/adventurer',{1,1,1}},
            {'core:terrain/bramble_bed',{1,1,1}}})do
            f.get('draw_visual')(f.get('bound_visual')(case[1]),10,20,40,40,.6,true)
            local r=f.g.records[#f.g.records];rgb(r,case[2],.9)
            assert(r.args[3]==50 and r.args[6]==-2.5 and r.args[7]==2.5)
            near(r.color[4],.6);equal(filter.ink,{1,1,1})
        end
        for _,source in ipairs({s.player_id,999})do
            f.g.records={}
            f.get('draw_history_event')({kind='WindupStarted',source_id=source,
                target_cells={{x=3,y=2}},floor_id=s.active_floor_id})
            local ink=source==s.player_id and P.friendly or P.hostile
            assert(#f.g.records>0)
            for i,r in ipairs(f.g.records)do if i%2==0 then rgb(r,ink)end end
        end
        for _,kind in ipairs({'basic_attack','sweeping_slash','dash'})do
            f.set('targeting',{kind=kind,dx=1,dy=0})
            f.g.records={};f.get('draw_preview')();assert(#f.g.records>0)
            for i,r in ipairs(f.g.records)do if i%2==0 then rgb(r,P.friendly)end end
        end
        assert(P.preview('dash','wall')==P.blocked and P.preview('use_utility','unreachable')==P.blocked)
        assert(P.attack(s.player_id,{source_id=999},{owner_id=s.player_id})==P.friendly)
        f.set('visible_at',function()return false end);f.g.records={}
        f.get('draw_cell')({x=3,y=2},1,1,1,.7,true,P.hostile)
        assert(#f.g.records==0)
        filter:finish(f.g,old,applied)
        filter.shader=nil -- Unsupported-shader native-sprite fallback.
        f.get('draw_visual')(f.get('bound_visual')('core:drop/health'),10,20,40,40)
        rgb(f.g.records[#f.g.records],P.gold,.9)
        equal(G.snapshot(s),before)
    end)
    test('real resolved damage numbers animate their ink while preserving labels, fog and exact run snapshot',function()
        local f=callbacks();local s=f.state
        G.spawn_enemy(s,'core:enemy/mossbound_guard',3,2)
        local okay,why,events=G.submit(s,{kind='basic_attack',dx=1,dy=0});assert(okay,why)
        local damage
        for _,e in ipairs(events)do if e.kind=='DamageTaken' and e.source_id==s.player_id then damage=e end end
        assert(damage and damage.actual_loss>0)
        local before=G.snapshot(s);local N=require('src.combat_numbers');local numbers=N.new()
        N.update(numbers,{items={{event=damage}}},0,s.player_id,s.active_floor_id)
        assert(#numbers.items==1);numbers.items[1].critical=true
        f.set('combat_numbers',numbers);f.set('visible_at',function()return true end)
        local filter=f.get('onebit_filter');local old,applied=filter:begin(f.g)
        local first
        for _,age in ipairs({0,.18,.39})do
            numbers.items[1].age=age;f.g.records={};f.get('draw_combat_numbers')()
            assert(#f.g.records==5)
            for _,r in ipairs(f.g.records)do assert(r.args[1]==damage.actual_loss..'!')end
            local face=f.g.records[5];rgb(face,P.damage(numbers.items[1]))
            for i=1,4 do rgb(f.g.records[i],{1,1,1},.025)end
            if first then assert(math.abs(first[1]-face.color[1])+math.abs(first[2]-face.color[2])>.1)
            else first=face.color end
            equal(filter.ink,{1,1,1})
        end
        f.set('visible_at',function()return false end);f.g.records={}
        f.get('draw_combat_numbers')();assert(#f.g.records==0)
        filter:finish(f.g,old,applied);equal(G.snapshot(s),before)
    end)
end
