-- Optional loose-source development visuals, keyed by stable game binding.
-- Game simulation and snapshots never depend on this module.
local Project=require('src.studio_project')
local AtlasRuntime=require('src.studio_atlas_runtime')
local V={}
function V.new(read,make_image,generated)
    local self=setmetatable({bindings={},by_id={},cache={},image_data={},has_terrain=false,
        failed={},status=generated and 'generated' or 'absent',atlas_status='absent',
        generated=generated},{__index=V})
    if generated then self.has_terrain=true end
    local ok,bytes=pcall(read,'assets/studio/manifest.txt')
    if not ok or not bytes then return self end
    local decoded,project=pcall(Project.decode,bytes)
    if not decoded then self.status='invalid manifest: '..tostring(project);return self end
    self.project_id=project.id
    for _,asset in ipairs(project.assets) do
        if asset.source~='' then self.by_id[asset.id]=asset end
        if asset.binding~='' and asset.source~='' then
            if asset.binding:match('^core:terrain/') then self.has_terrain=true end
            if self.bindings[asset.binding] then
                self.status='duplicate binding: '..asset.binding
                self.bindings={}
                return self
            end
            self.bindings[asset.binding]=asset
        end
    end
    self.read,self.make_image=read,make_image
    self.status='loaded'
    self.atlas_project=project
    self.atlas_status='uninspected'
    return self
end
function V:check_atlas()
    if self.atlas_status=='uninspected' then
        self.atlas,self.atlas_status=AtlasRuntime.new(
            self.read,self.make_image,self.atlas_project)
    end
end
function V:resolve_id(id)
    if not self.by_id[id] then
        return self.generated and self.generated:resolve(id) or nil
    end
    self:check_atlas()
    if self.atlas then
        local packed=self.atlas:resolve(id)
        if packed then return packed end
    end
    local image=self:get_id(id)
    if not image then return self.generated and self.generated:resolve(id) or nil end
    return {texture=image,width=image:getWidth(),height=image:getHeight(),
        source='loose',offset_x=0,offset_y=0}
end
function V:resolve(binding)
    local asset=self.bindings[binding]
    return asset and self:resolve_id(asset.id) or
        self.generated and self.generated:resolve(binding) or nil
end
function V:get(binding)
    local asset=self.bindings[binding]
    if not asset then return nil end
    if self.cache[binding] then return self.cache[binding] end
    if self.failed[binding] then return nil end
    local ok,bytes=pcall(self.read,'assets/studio/'..asset.source)
    if not ok or not bytes then self.failed[binding]=true;return nil end
    local valid,w,h=pcall(Project.png_dimensions,bytes)
    if not valid or w~=asset.width or h~=asset.height then
        self.failed[binding]=true;return nil
    end
    local loaded,image,pixels=pcall(self.make_image,bytes,asset)
    if not loaded or not image then self.failed[binding]=true;return nil end
    self.cache[binding]=image
    return image
end
function V:get_id(id)
    local asset=self.by_id[id]
    if not asset then return nil end
    local cache_key='id:'..id
    if self.cache[cache_key] then return self.cache[cache_key] end
    if self.failed[cache_key] then return nil end
    local ok,bytes=pcall(self.read,'assets/studio/'..asset.source)
    if not ok or not bytes then self.failed[cache_key]=true;return nil end
    local valid,w,h=pcall(Project.png_dimensions,bytes)
    if not valid or w~=asset.width or h~=asset.height then
        self.failed[cache_key]=true;return nil end
    local loaded,image,pixels=pcall(self.make_image,bytes,asset)
    if not loaded or not image then self.failed[cache_key]=true;return nil end
    self.cache[cache_key]=image
    if asset.kind=='bitmap_font' or asset.kind=='glyph' then
        self.image_data[cache_key]=pixels
    end
    return image
end
return V
