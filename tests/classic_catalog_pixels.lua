-- Isolated presentation evidence from the real production texture handles.
-- This contact sheet is not a generated-floor or gameplay-journey claim.
return function(production_draw,output)
    local g=love.graphics
    local visited,renderer,draw_visual={}
    local function inspect(fn)
        if visited[fn]then return end;visited[fn]=true
        for i=1,200 do
            local name,value=debug.getupvalue(fn,i);if not name then break end
            if name=='visual_assets' and value then renderer=value.generated end
            if name=='draw_visual' then draw_visual=value end
            if type(value)=='function'then inspect(value)end
        end
    end
    inspect(production_draw);assert(renderer and renderer.style=='catalog' and draw_visual)
    local data=renderer.data
    local function write(name,bytes)
        local f=assert(io.open(output..'/'..name,'wb'));f:write(bytes);f:close()
    end
    local function pixels(handle)
        local canvas=g.newCanvas(handle.width,handle.height)
        g.push('all');g.setCanvas(canvas);g.clear(0,0,0,0);g.setShader()
        g.setColor(1,1,1,1);g.setBlendMode('replace','premultiplied')
        if handle.quad then g.draw(handle.texture,handle.quad,0,0)else g.draw(handle.texture,0,0)end
        g.pop();local result=canvas:newImageData();canvas:release();return result
    end
    local source_cache,checks,examples={},0,{}
    local function source(path)
        if not source_cache[path]then source_cache[path]=love.image.newImageData(path)end
        return source_cache[path]
    end
    local function near(a,b)assert(math.abs(a-b)<=2/255,'source pixel transform differs');checks=checks+1 end
    for _,role in ipairs({'core:actor/geomancer','core:enemy/mossbound_guard','core:enemy/bastion_bulwark',
        'core:object/door_closed','core:object/door_open','core:object/review_pillar_top','core:object/review_vault_top'})do
        local entry=assert(data.bindings[role]);local handle=assert(renderer:resolve(role));local actual=pixels(handle)
        local original=source(entry.source);assert(data.sources[entry.source].background=='black_to_transparent')
        local foreground,empty=0,0
        for y=0,entry.height-1 do for x=0,entry.width-1 do
            local r,b,c,a=original:getPixel(entry.x+x,entry.y+y)
            local rr,gg,bb,aa=actual:getPixel(x,y)
            if math.max(r,b,c)<=8/255 then near(aa,0);empty=empty+1
            else near(aa,a);near(rr,1);near(gg,1);near(bb,1);foreground=foreground+1 end
        end end
        assert(foreground>0 and empty>0,'crop is blank or lost transparent silhouette')
        actual:release();examples[#examples+1]={label=role:gsub('core:',''),handle=handle}
    end
    -- Exercise the same draw function used by class portraits, moving actors and
    -- attack/hurt playback, against independently selected source pixel columns.
    local corrected={berserker=true,alchemist=true,hookrunner=true,trapper=true,
        duelist=true,geomancer=true,spiritbinder=true,tinkerer=true,stormcaller=true,
        wayfinder=true,shade=true}
    local facing_comparisons=0
    for _,role in ipairs(require('src.classes').order)do
        local flip=corrected[role:match('/([^/]+)$')]==true
        if role=='core:actor/shade' then
            assert(data.bindings[role].id=='urizen-onebit-r05-c108','Shade replacement missing')
        end
        for _,suffix in ipairs({'','/attack','/hurt'})do
            local entry=data.bindings[role..suffix];local handle=renderer:resolve(role..suffix)
            assert(handle.flip_x==flip,'unexpected class orientation: '..role..suffix)
            for _,left in ipairs({false,true})do
                local canvas=g.newCanvas(entry.width,entry.height)
                g.push('all');g.setCanvas(canvas);g.clear(0,0,0,0);g.setShader()
                g.setBlendMode('replace','premultiplied')
                draw_visual(handle,0,0,entry.width,entry.height,1,left)
                g.pop();local actual=canvas:newImageData();canvas:release()
                local original=source(entry.source)
                for y=0,entry.height-1 do for x=0,entry.width-1 do
                    local column=(flip~=left) and entry.width-1-x or x
                    local r,b,c,a=original:getPixel(entry.x+column,entry.y+y)
                    local rr,gg,bb,aa=actual:getPixel(x,y)
                    if math.max(r,b,c)<=8/255 then near(aa,0)
                    else near(aa,a);near(rr,1);near(gg,1);near(bb,1)end
                    facing_comparisons=facing_comparisons+1
                end end
                actual:release()
            end
        end
    end
    local actors=g.newCanvas(1080,884)
    g.push('all');g.setCanvas(actors);g.clear(.035,.035,.035,1);g.setShader();g.setColor(1,1,1,1)
    g.print('CLASS FACING REVIEW — production portrait / actor / pose draw function',20,12)
    g.print('Source crop | corrected right | corrected left. Shade uses a new masked-looking figure.',20,34)
    for i,role in ipairs(require('src.classes').order)do
        local x,y=20+((i-1)%3)*354,70+math.floor((i-1)/3)*160
        local h=renderer:resolve(role)
        g.setColor(1,1,1,1);g.print(role:match('/([^/]+)$'),x,y)
        g.draw(h.texture,h.quad,x+8,y+26,0,4,4)
        draw_visual(h,x+108,y+26,48,48,1,false)
        draw_visual(h,x+208,y+26,48,48,1,true)
        g.setColor(1,1,1,1);g.print('source        right         left',x+4,y+88)
        g.print(h.catalog_id,x+4,y+108)
    end
    g.pop();local actor_capture=actors:newImageData()
    write('class-facing-review.png',actor_capture:encode('png'):getString())
    write('class-facing-checks.txt',('PASS production class facing\nclasses=15\nposesPerClass=3\ndirections=2\npixelComparisons=%d\nShade=urizen-onebit-r05-c108\n'):format(facing_comparisons))
    actor_capture:release();actors:release()
    local around={{0,-1,1},{1,0,2},{0,1,4},{-1,0,8},{1,-1,16},{1,1,32},{-1,1,64},{-1,-1,128}}
    local surfaces={}
    for _,case in ipairs({{'floor',255},{'wall',255},{'wall',0},{'wall',127},{'water',255},{'water',0}})do
        local kind,mask=case[1],case[2];local mark=kind=='water'and'~'or'#'
        -- Far outside real room coordinates: exercise the biome's fallback crop
        -- without replacing the live world's room plan or any authoritative data.
        local map={rows={},biome_id='core:biome/sunken_aqueduct'}
        for y=599,601 do map.rows[y]=string.rep('.',601)end
        for _,d in ipairs(around)do if math.floor(mask/d[3])%2==1 then
            local x,y=600+d[1],600+d[2];local row=map.rows[y]
            map.rows[y]=row:sub(1,x-1)..mark..row:sub(x+1)
        end end
        local handle=renderer:terrain(map,600,600,kind);local actual=pixels(handle)
        local entry=data.bindings['core:terrain/sunken_aqueduct/'..kind..'/base']
        local original=source(entry.source)
        for y=0,handle.height-1 do for x=0,handle.width-1 do local _,_,_,a=actual:getPixel(x,y);near(a,1)end end
        -- Interior source samples remain dimmed source artwork, not white masks.
        local r,b,c,a=original:getPixel(entry.x+5,entry.y+5)
        local rr,gg,bb=actual:getPixel(5,5)
        local base=kind=='water'and .065 or .045
        local light=kind=='floor'and .22 or kind=='water'and .48 or .58
        near(rr,base+r*a*light);near(gg,base+b*a*light);near(bb,base+c*a*light)
        surfaces[kind..mask]=actual
        examples[#examples+1]={label=kind..' / neighbor mask '..mask,handle=handle}
    end
    -- A real concave corner is visibly distinct while the center is unchanged.
    local full,corner=surfaces.wall255,surfaces.wall127
    local fr,fg,fb=full:getPixel(0,0);local cr,cg,cb=corner:getPixel(0,0)
    assert(cr>=.37 and cg>=.37 and cb>=.37 and cr+cg+cb>fr+fg+fb)
    local sheet=g.newCanvas(1024,640)
    g.push('all');g.setCanvas(sheet);g.clear(.025,.03,.04,1);g.setShader();g.setColor(1,1,1,1)
    g.print('ISOLATED CATALOGUE PRESENTATION REVIEW — production handles',20,12)
    g.print('Original source shapes; foreground alpha masks; derived opaque terrain. No gameplay fixture injected.',20,36)
    for i,example in ipairs(examples)do
        local x,y=20+((i-1)%4)*250,76+math.floor((i-1)/4)*138
        g.setColor(.11,.12,.14,1);g.rectangle('fill',x,y,240,125);g.setColor(1,1,1,1)
        local h=example.handle
        if h.quad then g.draw(h.texture,h.quad,x+8,y+8,0,4,4)else g.draw(h.texture,x+8,y+8,0,4,4)end
        g.printf(example.label,x+8,y+66,224)
    end
    g.pop();local capture=sheet:newImageData()
    write('catalogue-presentation-review.png',capture:encode('png'):getString())
    write('catalogue-pixel-checks.txt',('PASS production catalogue pixels\nchannelComparisons=%d\nsourceCrops=7\nderivedTerrainCases=6\n'):format(checks))
    capture:release();sheet:release()
    for _,value in pairs(surfaces)do value:release()end
    for _,value in pairs(source_cache)do value:release()end
end
