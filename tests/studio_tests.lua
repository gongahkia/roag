local Project=require('src.studio_project')
local Store=require('src.studio_store')
local Publish=require('src.studio_publish')
local Visual=require('src.visual_assets')
local function sample()
    local f=assert(io.open('examples/studio/adventurer.png','rb'))
    local bytes=f:read('*a');f:close();return bytes
end
local function memory()
    local files={};local io={fail=false,partial=false}
    function io:read(path) return files[path] end
    function io:write(path,bytes)
        if self.fail then error('injected write failure') end
        if self.partial then files[path]=bytes:sub(1,20);return true end
        files[path]=bytes;return true
    end
    function io:mkdir() return true end
    function io:list() return {'demo'} end
    return io,files
end
return function(test,equal)
    test('Studio manifest is human-readable, deterministic and stable by ID',function()
        local p=Project.new('demo','Demo')
        local a=Project.asset('core:actor/adventurer','Hero','actor')
        local b=Project.asset('studio:ui/heart','Heart','ui')
        p.assets={b,a}
        local encoded=Project.encode(p)
        assert(encoded:find('core:actor/adventurer',1,true)<encoded:find('studio:ui/heart',1,true))
        local copy=Project.decode(encoded)
        assert(Project.encode(copy)==encoded)
        copy.assets[1].name='New Hero'
        assert(copy.assets[1].id=='core:actor/adventurer')
        assert(Project.encode(copy)~=encoded)
        assert(Project.encode(Project.decode(Project.encode(copy)))==Project.encode(copy))
    end)
    test('Studio validates IDs, categories, duplicate IDs and safe relative paths',function()
        local p=Project.new('demo','Demo')
        local a=Project.asset('core:actor/adventurer','Hero','actor')
        p.assets={a};assert(Project.validate(p))
        for _,path in ipairs({'../x.png','a/../x.png','/tmp/x.png','C:\\x.png',
            'a//x.png','a/./x.png','a\\x.png'}) do
            assert(not Project.valid_path(path),path)
        end
        assert(Project.valid_path('sources/image-0001.png'))
        p.assets[2]=Project.asset(a.id,'Duplicate','actor')
        assert(not pcall(Project.validate,p));p.assets[2]=nil
        a.kind='invalid';assert(not pcall(Project.validate,p));a.kind='actor'
        a.id='evil:../escape';assert(not pcall(Project.validate,p))
        a.id='core:actor/adventurer';a.binding='bad binding'
        assert(not pcall(Project.validate,p))
        a.binding='';a.source='sources/photo.jpg';a.format='jpg'
        a.width=16;a.height=16
        assert(not pcall(Project.validate,p))
    end)
    test('Studio import validates PNG and keeps original bytes',function()
        local io,files=memory();local store=Store.new(io)
        local p=Project.new('demo','Demo')
        local a=Project.asset('core:actor/adventurer','Hero','actor')
        a.binding='core:actor/adventurer'
        assert(not store:import(p,a,'not png'))
        local ok,asset=store:import(p,a,sample())
        assert(ok and asset.width==16 and asset.height==16)
        assert(files['studio/projects/demo/'..asset.source]==sample())
        assert(not store:import(p,a,sample()))
        assert(not pcall(Project.png_dimensions,sample():sub(1,30)))
        assert(store:save(p))
        local reopened=assert(Store.new(io):open('demo'))
        equal(p,reopened)
        assert(Project.encode(p)==Project.encode(reopened))
    end)
    test('Studio draft accepts source import without changing stable ID',function()
        local io=memory();local store=Store.new(io)
        local p=Project.new('demo','Demo')
        local draft=Project.asset('studio:actor/custom','Custom','actor')
        p.assets[1]=draft
        assert(store:import(p,draft,sample()))
        assert(#p.assets==1 and p.assets[1]==draft)
        assert(draft.id=='studio:actor/custom' and draft.width==16)
        assert(store:save(p))
        local reopened=assert(store:open('demo'))
        assert(reopened.assets[1].id==draft.id)
    end)
    test('Studio rotating manifest survives failed and truncated writes',function()
        local io,files=memory();local store=Store.new(io)
        local p=Project.new('demo','First')
        assert(store:save(p))
        local first=files['studio/projects/demo/manifest-a.txt']
        p.name='Second';assert(store:save(p))
        p.name='Third';io.fail=true
        assert(not store:save(p));io.fail=false
        local loaded=assert(store:open('demo'));assert(loaded.name=='Second')
        io.partial=true
        assert(not store:save(p));io.partial=false
        loaded=assert(store:open('demo'));assert(loaded.name=='Second')
        files['studio/projects/demo/manifest-a.txt']=first
        files['studio/projects/demo/manifest-b.txt']='truncated'
        local recovered,status=store:open('demo')
        assert(recovered.name=='First' and status=='recovered previous manifest')
        files['studio/projects/demo/manifest-a.txt']='bad'
        local missing,reason=store:open('demo')
        assert(not missing and reason=='corrupt manifest')
    end)
    test('Studio missing source and corrupt manifest fail without partial project install',function()
        local io,files=memory();local store=Store.new(io)
        local p=Project.new('demo','Demo')
        local a=Project.asset('core:actor/adventurer','Hero','actor')
        assert(store:import(p,a,sample()))
        assert(store:save(p))
        files['studio/projects/demo/'..p.assets[1].source]=nil
        local loaded,reason=store:open('demo')
        assert(not loaded and reason:find('broken project',1,true))
    end)
    test('Studio reopens previous complete project if newest source is torn',function()
        local io,files=memory();local store=Store.new(io)
        local p=Project.new('demo','Before import');assert(store:save(p))
        p.name='After import'
        local a=Project.asset('core:actor/adventurer','Hero','actor')
        assert(store:import(p,a,sample()))
        assert(store:save(p))
        files['studio/projects/demo/'..p.assets[1].source]=sample():sub(1,20)
        local prior,status=store:open('demo')
        assert(prior and prior.name=='Before import' and #prior.assets==0)
        assert(status:find('recovered prior project',1,true))
    end)
    test('Studio valid newer incompatible manifest is visible, not rolled back',function()
        local io,files=memory();local store=Store.new(io)
        local p=Project.new('demo','First');assert(store:save(p))
        local payload=Project.encode(p):gsub('ROEG%-STUDIO%-MANIFEST/1',
            'ROEG-STUDIO-MANIFEST/9',1)
        local a,b=1,0
        for i=1,#payload do a=(a+payload:byte(i))%65521;b=(b+a)%65521 end
        files['studio/projects/demo/manifest-b.txt']=
            table.concat({'ROEG-STUDIO-RECORD/1','2',#payload,
                string.format('%08x',b*65536+a)},'\n')..'\n'..payload
        local loaded,reason=store:open('demo')
        assert(not loaded and reason:find('incompatible manifest format',1,true))
        assert(not store:save(p))
    end)
    test('Studio explicit publish is dry-plannable and protects existing files',function()
        local io=memory();local store=Store.new(io)
        local p=Project.new('demo','Demo')
        local a=Project.asset('core:actor/adventurer','Hero','actor')
        a.binding='core:actor/adventurer'
        assert(store:import(p,a,sample()))
        local output={}
        local source=function(path)return io:read('studio/projects/demo/'..path) end
        local plan=Publish.plan(p,source,function(path)return output[path] end)
        assert(next(output)==nil,'dry-run planning wrote files')
        assert(#plan==2 and plan[1].path:find('assets/studio/sources/',1,true)==1)
        assert(plan[2].path=='assets/studio/manifest.txt')
        assert(Publish.apply(plan,function(path,bytes) output[path]=bytes;return true end,
            function()return true end))
        local registry=Visual.new(function(path)return output[path] end,
            function(bytes)return {bytes=bytes} end)
        assert(registry.status=='loaded' and registry:get('core:actor/adventurer').bytes==sample())
        output[plan[1].path]='different'
        local collision=Publish.plan(p,source,function(path)return output[path] end)
        assert(not Publish.apply(collision,function() error('must not write') end,
            function()return true end,false))
        assert(output[plan[1].path]=='different')
        assert(Visual.new(function()return nil end,function()error('unused')end):get(
            'core:actor/adventurer')==nil)
        local reads=0
        local broken=Visual.new(function(path)
            if path=='assets/studio/manifest.txt' then return output[path] end
            reads=reads+1;return nil
        end,function()error('must not decode')end)
        assert(broken:get('core:actor/adventurer')==nil)
        assert(broken:get('core:actor/adventurer')==nil and reads==1,
            'missing image retried filesystem read every frame')
    end)
end
