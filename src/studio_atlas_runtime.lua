local Atlas=require('src.studio_atlas')
local Store=require('src.studio_atlas_store')
local R={}
function R.new(read,make_image,project)
    local function verify(bytes,page)
        local ok,pixels=pcall(function()
            return love.image.newImageData(
                love.filesystem.newFileData(bytes,page.filename))
        end)
        return ok and pixels and pixels:getWidth()==page.width and
            pixels:getHeight()==page.height
    end
    local ok,pack,status=pcall(Store.inspect,read,project,verify)
    if not ok or not pack then return nil,ok and status or tostring(pack) end
    local images,by_filename={},{}
    for index,page in ipairs(pack.pages) do
        local cache_key=page.filename..':'..page.filter
        local image=by_filename[cache_key]
        if not image then
            image=assert(make_image(read(page.filename),{source=page.filename,
                width=page.width,height=page.height,filter=page.filter}))
            image:setFilter(page.filter,page.filter)
            by_filename[cache_key]=image
        end
        images[index]=image
    end
    local self=setmetatable({pack=pack,images=images,handles={},status=status},
        {__index=R})
    return self,status
end
function R:resolve(id)
    local entry=self.pack.assets[id]
    if not entry then return nil end
    local handle=self.handles[id]
    if handle then return handle end
    local page=self.pack.pages[entry.page]
    local image=self.images[entry.page]
    local quad=love.graphics.newQuad(entry.x,entry.y,entry.w,entry.h,
        page.width,page.height)
    handle={texture=image,quad=quad,width=entry.w,height=entry.h,
        offset_x=entry.x,offset_y=entry.y,source='atlas',page=entry.page}
    self.handles[id]=handle
    return handle
end
return R
