-- Run from checkout: luajit tools/studio_world_publish.lua --save-dir <LOVE_SAVE_DIR>
--   --project <id> [--apply] [--overwrite]. Default is dry run.
package.path='./?.lua;./?/init.lua;'..package.path
local Project=require('src.studio_project')
local ProjectStore=require('src.studio_store')
local WorldStore=require('src.studio_world_store')
local Runtime=require('src.studio_world_runtime')
local opts={};local i=1
while i<=#arg do
    local a=arg[i]
    if a=='--save-dir' or a=='--project' then
        assert(arg[i+1] and arg[i+1]:sub(1,2)~='--','missing value for '..a)
        opts[a]=arg[i+1];i=i+2
    elseif a=='--apply' or a=='--overwrite' then opts[a]=true;i=i+1
    else error('unsupported world publish option: '..tostring(a)) end
end
local root=assert(opts['--save-dir'],'--save-dir required'):gsub('\\','/')
local id=assert(opts['--project'],'--project required')
assert(Project.valid_project_id(id),'unsafe project ID')
assert((root:sub(1,1)=='/' or root:match('^[A-Za-z]:/')) and
    not root:find('%z'),'save directory must be absolute')
local function read_file(path)
    local f=io.open(path,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
local function read(path)
    assert(Project.valid_path(path),'unsafe path')
    return read_file(path)
end
assert(read('main.lua') and read('conf.lua') and read('src/studio_world.lua'),
    'run from ROAG checkout')
local adapter={}
function adapter:read(path)
    assert(Project.valid_path(path),'unsafe workspace path')
    return read_file(root..'/'..path)
end
function adapter:list() return {} end
function adapter:write() error('publisher cannot write Studio sources') end
function adapter:mkdir() error('publisher cannot modify Studio workspace') end
local project,reason=ProjectStore.new(adapter):open(id)
assert(project,'project unavailable: '..tostring(reason))
local pack,why=WorldStore.new(adapter):open(id,project.assets)
assert(pack,'world source unavailable: '..tostring(why))
local plan=Runtime.plan(pack,read)
print(('project %s: %d rooms, %d tile types'):format(id,#pack.rooms,#pack.tiles))
for _,room in ipairs(pack.rooms) do
    print(('room %s weight %d in %s'):format(room.id,room.weight,
        table.concat(room.biomes,',')))
end
print(('%s %s (%d bytes, generation %d)'):format(
    plan.overwrite and 'overwrite' or 'create',plan.path,#plan.bytes,plan.seq))
if not opts['--apply'] then
    print('Dry run. Pass --apply to publish; --overwrite is needed for a changed occupied slot.')
    return
end
assert(not plan.overwrite or opts['--overwrite'],
    'occupied published slot needs --overwrite')
-- Fixed allowlisted directory, no shell interpolation from project data.
local command=package.config:sub(1,1)=='\\' and 'mkdir assets\\studio' or
    'mkdir -p assets/studio'
assert(os.execute(command)==0 or package.config:sub(1,1)=='\\',
    'could not create assets/studio')
local f=assert(io.open(plan.path,'wb'))
assert(f:write(plan.bytes));assert(f:close())
assert(read(plan.path)==plan.bytes,'published write verification failed')
local loaded,status,record=Runtime.inspect(read)
assert(loaded and record.seq==plan.seq,'published pack verification failed: '..tostring(status))
print('Published verified world generation '..record.seq)
