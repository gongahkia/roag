local Project=require('src.studio_project')
local PNG=require('src.studio_png')
local Atlas=require('src.studio_atlas')
local Store=require('src.studio_atlas_store')
local Game=require('src.game')
local Codec=require('src.persistence')
local function source(w,h,r)
    return PNG.encode(w,h,string.rep(string.char(r,17,33,255),w*h))
end
local function fixture(count)
    local project=Project.new('atlas_fixture','Atlas fixture')
    local files={}
    for i=1,count do
        local id=('studio:tile/t%03d'):format(i)
        local asset=Project.asset(id,id,'tile')
        asset.width=4+(i%5)*3;asset.height=5+(i%7)*2
        asset.format='png';asset.source=('sources/t%03d.png'):format(i)
        asset.filter=i%4==0 and 'linear' or 'nearest'
        project.assets[#project.assets+1]=asset
        files['assets/studio/'..asset.source]=source(asset.width,asset.height,i%255)
    end
    local function read(path)return files[path]end
    return project,files,read
end
local function complete(plan,files)
    for index,page in ipairs(plan.pages) do
        local bytes='page '..index..' '..page.width..'x'..page.height
        page.checksum=Atlas.checksum(bytes)
        page.filename='assets/studio/atlas/page-'..page.checksum..'.png'
        files[page.filename]=bytes
    end
    assert(Atlas.validate(plan))
    return plan
end
return function(test,equal)
    test('E05 deterministic multi-page packing and stable IDs',function()
        local project,files,read=fixture(60)
        local a=complete(Atlas.plan(project,read,64,2),files)
        local b=complete(Atlas.plan(project,read,64,2),files)
        assert(#a.pages>2 and #a.pages<=Atlas.max_pages)
        assert(Atlas.encode(a)==Atlas.encode(b))
        equal(a,Atlas.decode(Atlas.encode(a)))
        for _,asset in ipairs(project.assets) do
            local entry=a.assets[asset.id]
            assert(entry and entry.w==asset.width and entry.h==asset.height)
        end
    end)
    test('E05 filter groups, uneven sizes and standalone bitmap sheets',function()
        local project,files,read=fixture(5)
        project.assets[1].kind='bitmap_font'
        local pack=complete(Atlas.plan(project,read,64,2),files)
        assert(pack.excluded[project.assets[1].id])
        assert(not pack.assets[project.assets[1].id])
        for _,page in ipairs(pack.pages) do
            for _,entry in ipairs(page.entries) do
                assert(entry.filter==page.filter and entry.padding==2)
            end
        end
        assert(#pack.pages>=2)
    end)
    test('E05 empty, single odd image and unsupported page sizes are explicit',function()
        local empty=Project.new('empty_atlas','Empty atlas')
        local plan=complete(Atlas.plan(empty,function()return nil end,64,2),{})
        assert(#plan.pages==0 and next(plan.assets)==nil)
        local project,files,read=fixture(1)
        local single=complete(Atlas.plan(project,read,64,2),files)
        assert(#single.pages==1 and #single.pages[1].entries==1)
        assert(not pcall(Atlas.plan,project,read,63,2))
        assert(not pcall(Atlas.plan,project,read,32,0))
    end)
    test('E05 rejects invalid inputs, paths, overlap and truncated metadata',function()
        local project,files,read=fixture(2)
        files['assets/studio/'..project.assets[1].source]=nil
        assert(not pcall(Atlas.plan,project,read,64,2))
        files['assets/studio/'..project.assets[1].source]=source(
            project.assets[1].width,project.assets[1].height,1)
        local plan=complete(Atlas.plan(project,read,64,2),files)
        local bytes=Atlas.encode(plan)
        assert(not pcall(Atlas.decode,bytes:sub(1,-2)))
        local first=plan.pages[1]
        if #first.entries>1 then
            first.entries[2].x=first.entries[1].x
            assert(not pcall(Atlas.validate,plan))
        end
        project.assets[1].source='../unsafe.png'
        assert(not pcall(Atlas.plan,project,read,64,2))
    end)
    test('E05 checked activation falls back only to compatible complete generation',function()
        local project,files,read=fixture(4)
        local a=complete(Atlas.plan(project,read,64,2),files)
        files[Atlas.slots[1]]=Store.record(1,a)
        assert(Store.inspect(read,project))
        local b=complete(Atlas.plan(project,read,64,2),files)
        files[Atlas.slots[2]]=Store.record(2,b)
        files[b.pages[1].filename]=nil
        local restored,status=Store.inspect(read,project)
        assert(not restored and status:find('page')) -- same content-named page
        files[b.pages[1].filename]='page 1 '..b.pages[1].width..'x'..b.pages[1].height
        files[Atlas.slots[2]]=files[Atlas.slots[2]]:sub(1,-2)
        restored,status=Store.inspect(read,project)
        assert(restored and status:find('recovered'))
        files['assets/studio/'..project.assets[1].source]=source(
            project.assets[1].width,project.assets[1].height,99)
        assert(not Store.inspect(read,project))
    end)
    test('E05 torn newer page recovers prior complete layout without mixing pages',function()
        local project,files,read=fixture(40)
        local a=complete(Atlas.plan(project,read,64,2),files)
        files[Atlas.slots[1]]=Store.record(1,a)
        local b=complete(Atlas.plan(project,read,128,2),files)
        assert(b.pages[1].filename~=a.pages[1].filename or
            #b.pages~=#a.pages,'test layouts did not differ')
        local intended=Store.plan(b,read)
        assert(intended.path==Atlas.slots[2])
        -- A failed page write occurs before the final record activation.
        local backup=files[b.pages[1].filename]
        files[b.pages[1].filename]=nil
        local prior=assert(Store.inspect(read,project))
        equal(prior,a)
        files[intended.path]=intended.bytes -- injected torn publish record
        local recovered,status=Store.inspect(read,project)
        assert(recovered and status:find('recovered'))
        equal(recovered,a)
        files[b.pages[1].filename]=backup
        local newest=assert(Store.inspect(read,project))
        equal(newest,b)
    end)
    test('E05 deterministic mapping rejects missing or moved stable references',function()
        local project,files,read=fixture(3)
        local pack=complete(Atlas.plan(project,read,64,2),files)
        assert(Atlas.matches_inputs(pack,project,read))
        local first=project.assets[1]
        files['assets/studio/'..first.source]=source(first.width,first.height,98)
        assert(not Atlas.matches_inputs(pack,project,read))
        files['assets/studio/'..first.source]=source(first.width,first.height,1)
        pack.assets[first.id].x=pack.assets[first.id].x+1
        -- The page entry shares the same table; both agree, but the source
        -- planner still detects an illicit coordinate edit.
        local ok,matching=pcall(Atlas.matches_inputs,pack,project,read)
        assert(not ok or not matching)
    end)
    test('E05 dry plan and presentation lookup do not mutate simulation',function()
        local project,files,read=fixture(3)
        local game=Game.new_stage(3)
        local snapshot=Codec.encode(Game.snapshot(game))
        local before={}
        for k,v in pairs(files) do before[k]=v end
        Atlas.plan(project,read,64,2)
        equal(files,before)
        assert(Codec.encode(Game.snapshot(game))==snapshot)
    end)
end
