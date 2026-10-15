-- LuaJIT publisher. Dry-run is read-only; --apply copies immutable clips before
-- activating one checked metadata generation. Run from the ROAG checkout.
package.path='./?.lua;./?/init.lua;'..package.path
local Project=require('src.studio_project')
local ProjectStore=require('src.studio_store')
local Audio=require('src.studio_audio')
local AudioStore=require('src.studio_audio_store')
local opts={};local i=1
while i<=#arg do
    local flag=arg[i]
    if flag=='--save-dir' or flag=='--project' then
        assert(arg[i+1] and arg[i+1]:sub(1,2)~='--','missing '..flag)
        opts[flag]=arg[i+1];i=i+2
    elseif flag=='--apply' or flag=='--overwrite' then opts[flag]=true;i=i+1
    else error('unsupported option: '..tostring(flag)) end
end
local id=opts['--project']
assert(opts['--save-dir'] and Project.valid_project_id(id),
    'usage: --save-dir <absolute LOVE save dir> --project <id> [--apply] [--overwrite]')
local root=opts['--save-dir']:gsub('\\','/')
assert((root:sub(1,1)=='/' or root:match('^[A-Za-z]:/')) and
    not root:find('%z') and not root:find('/%.%./'),
    'save directory must be an absolute safe path')
local function read(path)
    local file=io.open(path,'rb');if not file then return nil end
    local bytes=file:read('*a');file:close();return bytes
end
local function write(path,bytes)
    local file=assert(io.open(path,'wb'))
    assert(file:write(bytes));assert(file:close())
    assert(read(path)==bytes,'audio publish read-back failed: '..path)
end
assert(read('main.lua') and read('conf.lua') and read('src/studio_audio.lua'),
    'run audio publisher from the ROAG checkout')
local adapter={}
function adapter:read(path)
    assert(Project.valid_path(path),'unsafe Studio audio source path')
    return read(root..'/'..path)
end
function adapter:write()error('source workspace is read-only during publish')end
function adapter:mkdir()error('source workspace is read-only during publish')end
function adapter:list()return {}end
local project,reason=ProjectStore.new(adapter):open(id)
assert(project,'Studio project unavailable: '..tostring(reason))
local pack,status=AudioStore.new(adapter):open(id)
assert(pack,'Studio audio unavailable: '..tostring(status))
local files={}
Audio.validate(pack,function(path)return adapter:read(path)end)
Audio.validate_publish_permissions(pack)
for _,sound in ipairs(pack.sounds) do
    local from=assert(adapter:read(sound.path),'missing source '..sound.id)
    local path=Audio.published_path(sound)
    local prior=read(path)
    assert(not prior or prior==from,
        'published audio filename collision; refusing to alter old generation: '..path)
    files[#files+1]={path=path,bytes=from,already=prior~=nil,id=sound.id}
end
local plan=AudioStore.plan_published(pack,read)
print(('Audio project %s: %d source(s), %d cue binding(s)'):format(
    id,#pack.sounds,#pack.bindings))
for _,entry in ipairs(files) do
    print(('%s %s (%d bytes)'):format(entry.already and 'keep' or 'create',
        entry.path,#entry.bytes))
end
print(('%s %s (%d bytes, generation %d)'):format(
    plan.overwrite and 'overwrite' or 'create',plan.path,#plan.bytes,plan.seq))
if not opts['--apply'] then
    print('Dry run. Add --apply; use --overwrite when the destination manifest slot is occupied.')
    return
end
assert(not plan.overwrite or opts['--overwrite'],
    'occupied audio manifest slot needs --overwrite')
local command=package.config:sub(1,1)=='\\' and
    'mkdir assets\\studio\\audio' or 'mkdir -p assets/studio/audio'
assert(os.execute(command)==0 or package.config:sub(1,1)=='\\',
    'could not create audio output directory')
for _,entry in ipairs(files) do if not entry.already then write(entry.path,entry.bytes) end end
-- The final record is activated only after every referenced clip was verified.
write(plan.path,plan.bytes)
local loaded,why,entry=AudioStore.inspect_published(read)
assert(loaded and entry and entry.seq==plan.seq,
    'audio publication verification failed: '..tostring(why))
print('Published verified audio generation '..plan.seq)
