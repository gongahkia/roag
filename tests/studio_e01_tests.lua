local D=require('src.studio_document')
local PNG=require('src.studio_png')
local Project=require('src.studio_project')
local Store=require('src.studio_store')
local Publish=require('src.studio_publish')
local Outline=require('src.studio_outline')
local function rgba(doc,x,y,id) return table.concat(D.get(doc,x,y,id),',') end
local function mem()
    local files={};local io={fail=false,partial=false}
    function io:read(p) return files[p] end
    function io:write(p,b)
        if self.fail then error('injected failure') end
        files[p]=self.partial and b:sub(1,20) or b
        return true
    end
    function io:mkdir() return true end
    function io:list() return {'e01'} end
    return io,files
end
return function(test,equal)
    test('E01 blank RGBA canvas, dimensions and exact binary roundtrip',function()
        local d=D.new('core:actor/adventurer',16,16)
        assert(rgba(d,0,0)=='0,0,0,0')
        assert(not pcall(D.new,'bad id',16,16))
        assert(not pcall(D.new,'core:actor/adventurer',513,16))
        D.set(d,0,0,{2,23,254,128},'replace')
        local copy=D.decode(D.encode(d));equal(d,copy)
        assert(rgba(copy,0,0)=='2,23,254,128')
        assert(not pcall(D.decode,D.encode(d):sub(1,-8)))
        copy.layers[1].rows[1]='short'
        assert(not pcall(D.validate,copy))
    end)
    test('E01 editable paths and paired derived images fail closed',function()
        local p=Project.new('e01','E01')
        local a=Project.asset('core:actor/adventurer','Hero','actor')
        a.width=16;a.height=16;a.format='png';p.assets[1]=a
        a.document='../escape.rgba';a.derived='derived/painted.png'
        assert(not pcall(Project.validate,p))
        a.document='documents/layers.rgba';a.derived=''
        assert(not pcall(Project.validate,p))
        a.derived='derived/painted.png';assert(Project.validate(p))
    end)
    test('E01 pencil rapid drag, square brush clipping, erase and picker facts',function()
        local d=D.new('core:actor/adventurer',16,16)
        assert(D.line(d,0,0,15,11,{255,0,0,255},1,'paint'))
        for x=0,15 do
            local seen=false;for y=0,11 do if D.get(d,x,y)[4]>0 then seen=true end end
            assert(seen,'gap in Bresenham stroke')
        end
        D.line(d,0,0,0,0,{0,255,0,255},4,'paint')
        assert(rgba(d,0,0)=='0,255,0,255')
        assert(D.get(d,15,15)[4]==0)
        D.set(d,0,0,{0,0,0,0},'erase')
        assert(rgba(d,0,0)=='0,0,0,0')
        equal(D.composite_pixel(d,1,0),D.get(d,1,0))
    end)
    test('E01 four-connected fill and independent layer compositing',function()
        local d=D.new('core:actor/adventurer',4,4)
        for y=0,3 do D.set(d,1,y,{255,0,0,255},'replace') end
        assert(D.fill(d,0,0,{0,0,255,128}))
        assert(rgba(d,0,3)=='0,0,255,128')
        assert(rgba(d,3,3)=='0,0,0,0')
        local base=d.layers[1].id;local lid=D.add_layer(d,'Lights')
        D.set(d,0,0,{255,0,0,128},'replace')
        assert(rgba(d,0,0,base)=='0,0,255,128')
        local before=D.composite_pixel(d,0,0)
        assert(before[1]>0 and before[3]>0 and before[4]>128)
        d.layers[2].visible=false
        assert(rgba(d,0,0,base)=='0,0,255,128')
        assert(D.composite_pixel(d,0,0)[1]==0)
        d.layers[2].visible=true;d.layers[2].locked=true
        assert(not D.set(d,2,2,{1,1,1,255},'paint'))
        d.layers[2].locked=false
        assert(D.move_layer(d,-1))
        local shifted=D.composite_pixel(d,0,0)
        assert(shifted[3]>before[3],'layer order did not change blend')
        D.delete_layer(d);assert(#d.layers==1 and d.selected==base)
    end)
    test('E01 PNG has valid dimensions and preserves alpha bytes for LÖVE decode',function()
        local d=D.new('core:actor/adventurer',16,16)
        D.set(d,0,0,{9,120,201,255},'replace')
        D.set(d,15,15,{99,22,11,77},'replace')
        local png=PNG.encode(16,16,D.flatten(d))
        local w,h=Project.png_dimensions(png)
        assert(w==16 and h==16 and png:sub(1,8)=='\137PNG\r\n\26\n')
        assert(not pcall(Project.png_dimensions,png:sub(1,-10)))
    end)
    test('E01 layered source and PNG survive save/reopen; torn save retains previous',function()
        local io,files=mem();local store=Store.new(io)
        local p=Project.new('e01','E01');local a=Project.asset('core:actor/adventurer','Hero','actor')
        a.binding='core:actor/adventurer';p.assets[1]=a
        local d=D.new(a.id,16,16);D.set(d,1,1,{250,2,3,255},'replace')
        assert(store:save_document(p,a,d))
        assert(a.source=='' and a.document~='' and a.derived~='')
        local previous=a.document
        local reopened=assert(store:open('e01'))
        equal(d,store:document(reopened,reopened.assets[1]))
        D.add_layer(d,'Ink');D.set(d,2,2,{4,5,6,255},'replace')
        d.layers[2].visible=false;d.layers[2].locked=true
        assert(D.move_layer(d,-1))
        assert(store:save_document(p,a,d))
        assert(a.document~=previous)
        local newer=assert(store:open('e01'))
        equal(d,store:document(newer,newer.assets[1]))
        local newest_document=a.document
        local newest_bytes=files['studio/projects/e01/'..newest_document]
        files['studio/projects/e01/'..newest_document]='torn'
        local recovered,status=store:open('e01')
        assert(recovered and recovered.assets[1].document==previous)
        assert(status:find('recovered prior project',1,true))
        files['studio/projects/e01/'..newest_document]=newest_bytes
        files['studio/projects/e01/'..a.derived]='truncated PNG'
        assert(assert(store:open('e01')).assets[1].document==previous)
        io.fail=true;assert(not store:save_document(p,a,d));io.fail=false
        assert(assert(store:open('e01')).assets[1].document==previous)
        io.partial=true;assert(not store:save_document(p,a,d));io.partial=false
        assert(assert(store:open('e01')).assets[1].document==previous)
    end)
    test('E01 publishing chooses painted PNG and preserves stable game binding',function()
        local io=mem();local store=Store.new(io)
        local p=Project.new('e01','E01');local a=Project.asset('core:actor/adventurer','Hero','actor')
        a.binding='core:actor/adventurer';p.assets[1]=a
        local d=D.new(a.id,16,16);D.set(d,0,0,{23,45,67,255},'replace')
        assert(store:save_document(p,a,d))
        local files=Publish.plan(p,function(path)return io:read('studio/projects/e01/'..path) end,function()return nil end)
        assert(#files==2 and files[1].bytes==io:read('studio/projects/e01/'..a.derived))
        local derived=Project.decode(files[2].bytes)
        assert(derived.assets[1].id==a.id and derived.assets[1].binding==a.binding)
        assert(derived.assets[1].document=='' and derived.assets[1].derived=='')
    end)
    test('E01 drag history, clean save checkpoint and redo-branch invalidation',function()
        local old_love=_G.love
        _G.love={graphics={getWidth=function()return 1024 end,
            getHeight=function()return 640 end,getDimensions=function()return 1024,640 end}}
        local ok,err=pcall(function()
            local C=require('src.studio_canvas')
            local c=C.new(D.new('core:actor/adventurer',16,16))
            c:begin_change();c.dragging=true
            D.line(c.doc,0,0,7,0,{255,0,0,255},1,'paint')
            c:finish()
            assert(#c.undo_stack==1 and c:dirty())
            assert(c:undo() and not c:dirty())
            assert(D.get(c.doc,4,0)[4]==0)
            assert(c:redo() and c:dirty())
            c:saved_now();assert(not c:dirty())
            assert(c:undo() and c:dirty())
            assert(c:redo() and not c:dirty())
            c:undo()
            assert(c:change(function() D.add_layer(c.doc,'Second') end))
            assert(#c.doc.layers==2 and #c.redo_stack==0)
            assert(c:undo() and #c.doc.layers==1)
            local large=C.new(D.new('core:actor/adventurer',512,512))
            assert(large.zoom==0.5,'fit must show a full 512-pixel sprite')
            local ox,oy=large:origin()
            assert(select(1,large:pixel_at(ox,oy))==0)
            assert(select(1,large:pixel_at(ox+255.5,oy+255.5))==511)
        end)
        _G.love=old_love
        assert(ok,err)
    end)
    test('E01 rectangular selection recolors opaque pixels and clears as one undo step',function()
        local old_love=_G.love
        local down={}
        _G.love={graphics={getWidth=function()return 1024 end,
            getHeight=function()return 640 end,getDimensions=function()return 1024,640 end},
            keyboard={isDown=function(...)
                for i=1,select('#',...) do if down[select(i,...)] then return true end end
                return false
            end}}
        local ok,err=pcall(function()
            local C=require('src.studio_canvas')
            local d=D.new('core:actor/adventurer',4,4)
            D.set(d,1,1,{10,20,30,128},'replace')
            D.set(d,2,2,{40,50,60,255},'replace')
            local c=C.new(d);c.color={200,100,50,255}
            c:keypressed('m')
            local ox,oy=c:origin()
            local function pos(x,y)return ox+(x+0.5)*c.zoom,oy+(y+0.5)*c.zoom end
            local x0,y0=pos(1,1);local x1,y1=pos(2,2)
            c:mousepressed(x0,y0,1);c:mousemoved(x1,y1,x1-x0,y1-y0)
            c:mousereleased()
            assert(c.selection.x0==1 and c.selection.y1==2 and not c:dirty())
            assert(c:keypressed('c'))
            assert(rgba(c.doc,1,1)=='200,100,50,128')
            assert(rgba(c.doc,2,2)=='200,100,50,255')
            assert(rgba(c.doc,1,2)=='0,0,0,0')
            assert(#c.undo_stack==1)
            down.lgui=true;c:keypressed('z');down.lgui=nil
            assert(rgba(c.doc,1,1)=='10,20,30,128')
            down.rgui=true;down.lshift=true;c:keypressed('z')
            down.rgui=nil;down.lshift=nil
            assert(rgba(c.doc,1,1)=='200,100,50,128')
            assert(c:keypressed('delete'))
            assert(rgba(c.doc,1,1)=='0,0,0,0' and rgba(c.doc,2,2)=='0,0,0,0')
            down.lctrl=true;c:keypressed('z');down.lctrl=nil
            assert(rgba(c.doc,1,1)=='200,100,50,128')
            c.doc.layers[1].locked=true
            assert(not c:apply_selection('erase'))
            assert(rgba(c.doc,1,1)=='200,100,50,128')
            c:keypressed('o')
            for _=1,8 do c:keypressed('backspace') end
            c:textinput('#ABCDEF80');c:keypressed('return')
            assert(c.outline_color=='ABCDEF80')
            assert(c:undo() and c.outline_color=='')
            assert(c:redo() and c.outline_color=='ABCDEF80')
        end)
        _G.love=old_love
        assert(ok,err)
    end)
    test('E01 one-output-pixel outline preserves 2x core and requested RGBA',function()
        local d=D.new('core:actor/adventurer',3,3)
        D.set(d,1,1,{10,20,30,128},'replace')
        local w,h,raw=Outline.render(D.flatten(d),3,3,'A1B2C3D4')
        assert(w==6 and h==6 and #raw==144)
        local function pixel(x,y)
            local p=(y*w+x)*4+1
            return table.concat({raw:byte(p,p+3)},',')
        end
        assert(pixel(2,2)=='10,20,30,128' and pixel(3,3)=='10,20,30,128')
        assert(pixel(1,1)=='161,178,195,212' and pixel(4,4)=='161,178,195,212')
        assert(pixel(0,0)=='0,0,0,0')
        assert(not pcall(Outline.render,D.flatten(d),3,3,'bad'))
    end)
    test('E01 outlined source reopens and publishes as a 2x bound visual',function()
        local io=mem();local store=Store.new(io)
        local p=Project.new('e01','E01')
        local a=Project.asset('studio:actor/hero','Hero','actor')
        a.binding='core:actor/adventurer';p.assets[1]=a
        local d=D.new(a.id,3,3)
        D.set(d,1,1,{80,90,100,255},'replace')
        assert(store:save_document(p,a,d,nil,'FF0000FF'))
        assert(a.width==3 and a.height==3 and a.outline_color=='FF0000FF')
        local reopened=assert(store:open('e01'))
        assert(reopened.assets[1].outline_color=='FF0000FF')
        assert(D.get(store:document(reopened,reopened.assets[1]),1,1)[1]==80)
        local files=Publish.plan(reopened,function(path)
            return io:read('studio/projects/e01/'..path)
        end,function()return nil end)
        local output=Project.decode(files[2].bytes)
        local published=output.assets[1]
        assert(published.width==6 and published.height==6)
        assert(published.binding=='core:actor/adventurer' and published.outline_color=='')
        local w,h=Project.png_dimensions(files[1].bytes)
        assert(w==6 and h==6)
        local atlas=require('src.studio_atlas').plan(output,function(path)
            return path==files[1].path and files[1].bytes or nil
        end,32,2)
        assert(atlas.assets[a.id].w==6 and atlas.assets[a.id].h==6)
    end)
    test('E01 copied pose preserves source layers and persistent reference',function()
        local io,files=mem();local store=Store.new(io)
        local p=Project.new('e01','E01')
        local original=Project.asset('studio:actor/hero','Hero','actor')
        original.binding='core:actor/adventurer';p.assets[1]=original
        local source=D.new(original.id,4,4)
        D.set(source,1,1,{80,90,100,255},'replace')
        assert(store:save_document(p,original,source))
        local original_bytes=files['studio/projects/e01/'..original.document]
        local copy=Project.asset('studio:actor/copy_001','Hero hurt','actor')
        copy.binding='core:actor/adventurer/hurt'
        copy.reference_id=original.id;p.assets[2]=copy
        local pose=D.decode(D.encode(source));pose.id=copy.id
        D.set(pose,1,1,{240,20,30,255},'replace')
        assert(store:save_document(p,copy,pose))
        assert(files['studio/projects/e01/'..original.document]==original_bytes)
        local reopened=assert(store:open('e01'))
        local saved_original,saved_copy
        for _,asset in ipairs(reopened.assets) do
            if asset.id==original.id then saved_original=asset else saved_copy=asset end
        end
        assert(saved_copy.reference_id==original.id)
        assert(rgba(store:document(reopened,saved_original),1,1)=='80,90,100,255')
        assert(rgba(store:document(reopened,saved_copy),1,1)=='240,20,30,255')
        local plan=Publish.plan(reopened,function(path)
            return files['studio/projects/e01/'..path]
        end,function()return nil end)
        local published=Project.decode(plan[#plan].bytes)
        assert(published.assets[1].reference_id=='' and published.assets[2].reference_id=='')
        saved_copy.reference_id='studio:actor/missing'
        assert(not pcall(Project.validate,reopened))
        saved_copy.reference_id=original.id
        saved_copy.width=5
        assert(not pcall(Project.validate,reopened))
    end)
end
