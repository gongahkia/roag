-- Dry-run by default. Publish E01 images first, then this checked E04 pack.
package.path='./?.lua;./?/init.lua;'..package.path
local Project=require('src.studio_project')
local ProjectStore=require('src.studio_store')
local Theme=require('src.studio_theme')
local ThemeStore=require('src.studio_theme_store')
local opts={};local i=1
while i<=#arg do
    local a=arg[i]
    if a=='--save-dir' or a=='--project' then
        assert(arg[i+1] and arg[i+1]:sub(1,2)~='--','missing '..a)
        opts[a]=arg[i+1];i=i+2
    elseif a=='--apply' or a=='--overwrite' then opts[a]=true;i=i+1
    else error('unsupported option: '..tostring(a)) end
end
local id=opts['--project']
assert(opts['--save-dir'] and Project.valid_project_id(id),
    'usage: --save-dir <absolute LOVE save dir> --project <id> [--apply] [--overwrite]')
local root=opts['--save-dir']:gsub('\\','/')
assert((root:sub(1,1)=='/' or root:match('^[A-Za-z]:/')) and
    not root:find('%z'),'save directory must be absolute')
local function read(path)
    local f=io.open(path,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
assert(read('main.lua') and read('conf.lua') and read('src/studio_theme.lua'),
    'run publisher from the ROAG checkout')
local adapter={}
function adapter:read(path)
    assert(Project.valid_path(path),'unsafe Studio path')
    return read(root..'/'..path)
end
function adapter:write() error('publisher is read-only in Studio workspace') end
function adapter:mkdir() error('publisher is read-only in Studio workspace') end
function adapter:list() return {} end
local project,reason=ProjectStore.new(adapter):open(id)
assert(project,'project unavailable: '..tostring(reason))
local theme,why=ThemeStore.new(adapter):open(id,project.assets)
assert(theme,'theme unavailable: '..tostring(why))
local manifest=assert(read('assets/studio/manifest.txt'),
    'publish project images first with tools/studio_publish.lua')
local published=Project.decode(manifest)
assert(published.id==id,'published images belong to another project')
Theme.validate(theme,published.assets)
local by_id={};for _,asset in ipairs(published.assets) do by_id[asset.id]=asset end
for _,slot in ipairs(Theme.slots) do
    local asset_id=theme.slots[slot]
    if asset_id~='' then
        local asset=by_id[asset_id]
        local bytes=assert(read('assets/studio/'..asset.source),
            'missing published image for '..slot..': '..asset_id)
        local w,h=Project.png_dimensions(bytes)
        assert(w==asset.width and h==asset.height,
            'published dimensions mismatch for '..asset_id)
        print(slot..' -> '..asset_id)
    end
end
local plan=ThemeStore.plan_published(theme,read)
print(('%s %s (%d bytes, generation %d)'):format(
    plan.overwrite and 'overwrite' or 'create',plan.path,#plan.bytes,plan.seq))
if not opts['--apply'] then
    print('Dry run. Pass --apply; --overwrite is required for a changed occupied slot.')
    return
end
assert(not plan.overwrite or opts['--overwrite'],
    'occupied theme slot needs --overwrite')
local command=package.config:sub(1,1)=='\\' and 'mkdir assets\\studio' or
    'mkdir -p assets/studio'
assert(os.execute(command)==0 or package.config:sub(1,1)=='\\',
    'could not create assets/studio')
local f=assert(io.open(plan.path,'wb'))
assert(f:write(plan.bytes));assert(f:close())
assert(read(plan.path)==plan.bytes,'theme write verification failed')
local loaded,status,entry=ThemeStore.inspect_published(read)
assert(loaded and entry.seq==plan.seq,'theme verification failed: '..tostring(status))
print('Published verified theme generation '..plan.seq)
