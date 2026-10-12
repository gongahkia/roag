-- LÖVE 11.5 compositor for the pure deterministic atlas plan.
local Atlas=require('src.studio_atlas')
local Project=require('src.studio_project')
local PNG=require('src.studio_png')
local B={}
local workspace='studio/atlas-build/'
local function image(bytes,name)
    local file=love.filesystem.newFileData(bytes,name)
    local pixels=love.image.newImageData(file)
    assert(pixels:getFormat()=='rgba8','atlas requires rgba8 decoded pixels')
    return pixels
end
local function extrude(page,source,entry,p)
    local x,y,w,h=entry.x,entry.y,entry.w,entry.h
    page:paste(source,x,y,0,0,w,h)
    for delta=1,p do
        page:paste(source,x-delta,y,0,0,1,h)
        page:paste(source,x+w-1+delta,y,w-1,0,1,h)
        page:paste(source,x,y-delta,0,0,w,1)
        page:paste(source,x,y+h-1+delta,0,h-1,w,1)
    end
    for dx=1,p do for dy=1,p do
        page:paste(source,x-dx,y-dy,0,0,1,1)
        page:paste(source,x+w-1+dx,y-dy,w-1,0,1,1)
        page:paste(source,x-dx,y+h-1+dy,0,h-1,1,1)
        page:paste(source,x+w-1+dx,y+h-1+dy,w-1,h-1,1,1)
    end end
end
function B.compile(read,max_size)
    max_size=max_size or 2048
    local limits=love.graphics.getSystemLimits and love.graphics.getSystemLimits()
    if limits and limits.texturesize then
        assert(max_size<=limits.texturesize,
            'configured atlas page exceeds this renderer texture limit')
    end
    local manifest=assert(read('assets/studio/manifest.txt'),
        'publish loose images before building atlases')
    local project=Project.decode(manifest)
    local pack,items=Atlas.plan(project,read,max_size,2)
    local by_id={}
    for _,item in ipairs(items) do by_id[item.id]=item end
    local output={}
    for _,page in ipairs(pack.pages) do
        local pixels=love.image.newImageData(page.width,page.height)
        local originals={}
        for _,entry in ipairs(page.entries) do
            local item=by_id[entry.id]
            local source=image(item.bytes,item.source)
            assert(source:getWidth()==entry.w and source:getHeight()==entry.h,
                'decoded atlas source dimensions changed: '..entry.id)
            extrude(pixels,source,entry,pack.padding)
            originals[entry.id]=source:getString()
        end
        local raw=pixels:getString()
        for _,entry in ipairs(page.entries) do
            -- Verify original untrimmed pixels and edge extrusion independently
            -- of the PNG readback below, including alpha bytes.
            local original=originals[entry.id]
            for row=0,entry.h-1 do
                local start=((entry.y+row)*page.width+entry.x)*4+1
                local source_start=row*entry.w*4+1
                assert(raw:sub(start,start+entry.w*4-1)==
                    original:sub(source_start,source_start+entry.w*4-1),
                    'atlas source pixel mismatch: '..entry.id)
                for delta=1,pack.padding do
                    assert(raw:sub(start-delta*4,start-delta*4+3)==
                        original:sub(source_start,source_start+3),
                        'atlas left edge mismatch: '..entry.id)
                    local right=start+entry.w*4-4
                    assert(raw:sub(right+delta*4,right+delta*4+3)==
                        original:sub(source_start+entry.w*4-4,
                            source_start+entry.w*4-1),
                        'atlas right edge mismatch: '..entry.id)
                end
            end
            for column=0,entry.w-1 do
                local top=column*4+1
                local bottom=((entry.h-1)*entry.w+column)*4+1
                for delta=1,pack.padding do
                    local top_at=(((entry.y-delta)*page.width+
                        entry.x+column)*4)+1
                    local bottom_at=(((entry.y+entry.h-1+delta)*page.width+
                        entry.x+column)*4)+1
                    assert(raw:sub(top_at,top_at+3)==
                        original:sub(top,top+3),
                        'atlas top edge mismatch: '..entry.id)
                    assert(raw:sub(bottom_at,bottom_at+3)==
                        original:sub(bottom,bottom+3),
                        'atlas bottom edge mismatch: '..entry.id)
                end
            end
            for dx=1,pack.padding do for dy=1,pack.padding do
                for _,corner in ipairs({
                    {entry.x-dx,entry.y-dy,1},
                    {entry.x+entry.w-1+dx,entry.y-dy,entry.w*4-3},
                    {entry.x-dx,entry.y+entry.h-1+dy,
                        (entry.h-1)*entry.w*4+1},
                    {entry.x+entry.w-1+dx,entry.y+entry.h-1+dy,
                        entry.h*entry.w*4-3},
                }) do
                    local at=(corner[2]*page.width+corner[1])*4+1
                    assert(raw:sub(at,at+3)==
                        original:sub(corner[3],corner[3]+3),
                        'atlas corner extrusion mismatch: '..entry.id)
                end
            end end
        end
        local bytes=PNG.encode(page.width,page.height,raw,2048,
            function(data)return love.data.compress('string','zlib',data,9) end)
        local decoded=image(bytes,'atlas-page.png')
        assert(decoded:getString()==raw,
            'atlas PNG readback differs from source pixels')
        page.checksum=Atlas.checksum(bytes)
        page.filename='assets/studio/atlas/page-'..page.checksum..'.png'
        output[page.filename]=bytes
    end
    Atlas.validate(pack)
    return pack,output
end
function B.save_candidate(pack,pages)
    assert(love.filesystem.createDirectory(workspace),'cannot create atlas workspace')
    love.filesystem.remove(workspace..'manifest.txt')
    for _,page in ipairs(pack.pages) do
        local bytes=pages[page.filename]
        local path=workspace..page.filename:match('[^/]+$')
        assert(love.filesystem.write(path,bytes),'atlas candidate page write failed')
        assert(love.filesystem.read(path)==bytes,'atlas candidate page readback failed')
    end
    local metadata=Atlas.encode(pack)
    assert(love.filesystem.write(workspace..'manifest.txt',metadata),
        'atlas candidate metadata write failed')
    assert(love.filesystem.read(workspace..'manifest.txt')==metadata,
        'atlas candidate metadata readback failed')
    return workspace
end
return B
