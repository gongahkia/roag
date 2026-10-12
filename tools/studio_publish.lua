-- Usage: luajit tools/studio_publish.lua --save-dir <LOVE_SAVE_DIR> --project <id>
--        [--apply] [--overwrite]. Default is dry-run. Run from the game checkout.
package.path='./?.lua;./?/init.lua;'..package.path
local Project=require('src.studio_project')
local Store=require('src.studio_store')
local Publish=require('src.studio_publish')
local opts={}
local i=1
while i<=#arg do
    local a=arg[i]
    if a=='--save-dir' or a=='--project' then
        assert(arg[i+1] and arg[i+1]:sub(1,2)~='--','missing value for '..a)
        opts[a]=arg[i+1];i=i+2
    elseif a=='--apply' or a=='--overwrite' then opts[a]=true;i=i+1
    else error('unsupported publish option: '..tostring(a)) end
end
assert(opts['--save-dir'] and opts['--project'] and
    Project.valid_project_id(opts['--project']),
    'usage: --save-dir <LOVE_SAVE_DIR> --project <safe-id> [--apply] [--overwrite]')
local root=opts['--save-dir']
root=root:gsub('\\','/')
assert((root:sub(1,1)=='/' or root:match('^[A-Za-z]:/')) and
    not root:find('%z'),'save directory must be absolute')
local function read_file(path)
    local f=io.open(path,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
assert(read_file('main.lua') and read_file('conf.lua') and
    read_file('src/studio_project.lua'),'run publisher from the ROEG checkout')
local io_adapter={}
function io_adapter:read(path)
    assert(Project.valid_path(path),'unsafe workspace path')
    return read_file(root..'/'..path)
end
function io_adapter:write() error('read-only workspace') end
function io_adapter:mkdir() error('read-only workspace') end
function io_adapter:list() return {} end
local project,reason=Store.new(io_adapter):open(opts['--project'])
assert(project,reason)
local base='studio/projects/'..project.id..'/'
local plan=Publish.plan(project,function(path)
    return assert(io_adapter:read(base..path),'missing source: '..path)
end,function(path) return read_file(path) end)
for _,file in ipairs(plan) do
    print(('%s %s (%d bytes)'):format(file.status,file.path,#file.bytes))
end
if not opts['--apply'] then
    print('Dry run. Pass --apply to publish; --overwrite is required for changed existing files.')
    return
end
local function mkdir(path)
    assert(path=='assets/studio' or path=='assets/studio/sources','unsafe destination')
    -- This is a fixed allowlisted directory, never project-provided shell input.
    local windows=package.config:sub(1,1)=='\\'
    local command=windows and 'mkdir assets\\studio\\sources' or
        'mkdir -p assets/studio/sources'
    os.execute(command)
    return true
end
local function write(path,bytes)
    assert(Project.valid_path(path) and path:match('^assets/studio/'),'unsafe output path')
    local f=assert(io.open(path,'wb'))
    assert(f:write(bytes));assert(f:close())
    assert(read_file(path)==bytes,'publish read-back mismatch')
    return true
end
local ok,err=Publish.apply(plan,write,mkdir,opts['--overwrite'])
assert(ok,err)
print('Published project '..project.id)
