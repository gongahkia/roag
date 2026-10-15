-- Build candidates with `love . --atlas-build`, then dry-run this command.
-- `--apply` activates an entire verified generation; `--overwrite` is required
-- after both metadata slots have been used. Run from the ROAG checkout.
package.path='./?.lua;./?/init.lua;'..package.path
local Atlas=require('src.studio_atlas')
local Store=require('src.studio_atlas_store')
local Project=require('src.studio_project')
local opts={}
local i=1
while i<=#arg do
    local flag=arg[i]
    if flag=='--save-dir' or flag=='--project' then
        assert(arg[i+1] and arg[i+1]:sub(1,2)~='--','missing '..flag)
        opts[flag]=arg[i+1];i=i+2
    elseif flag=='--apply' or flag=='--overwrite' or flag=='--disable' or
        flag=='--enable' then opts[flag]=true;i=i+1
    else error('unknown atlas option: '..tostring(flag)) end
end
assert(not (opts['--disable'] and opts['--enable']),'choose disable or enable')
local function read(path)
    local f=io.open(path,'rb');if not f then return nil end
    local bytes=f:read('*a');f:close();return bytes
end
local function write(path,bytes)
    local f=assert(io.open(path,'wb'))
    assert(f:write(bytes));assert(f:close())
    assert(read(path)==bytes,'atlas write readback failed: '..path)
end
local windows=package.config:sub(1,1)=='\\'
local function safe_destination(path)
    assert(Project.valid_path(path) and path:match('^assets/studio/'),
        'unsafe atlas destination')
    if not windows then
        -- Paths below are fixed slots or checksum-derived ASCII filenames.
        for _,candidate in ipairs({'assets','assets/studio',
            'assets/studio/atlas',path}) do
            assert(os.execute('test ! -L '..candidate)==0,
                'refusing symlink destination: '..candidate)
        end
    end
end
assert(read('main.lua') and read('conf.lua') and read('src/studio_atlas.lua'),
    'run from the ROAG checkout')
if opts['--disable'] or opts['--enable'] then
    local action=opts['--disable'] and 'Disable' or 'Enable'
    print(action..' optional atlas lookup; loose visuals remain available.')
    if not opts['--apply'] then print('Dry run. Pass --apply to change lookup.');return end
    local flag=Store.disable_path
    safe_destination(flag)
    if opts['--disable'] then write(flag,'ROAG-ATLAS-DISABLED/1\n')
    else os.remove(flag);assert(not read(flag),'cannot enable atlas') end
    print(action..'d atlas lookup.')
    return
end
local save_dir=assert(opts['--save-dir'],'--save-dir is required')
save_dir=save_dir:gsub('\\','/')
assert((save_dir:sub(1,1)=='/' or save_dir:match('^[A-Za-z]:/')) and
    not save_dir:find('%z'),'save directory must be absolute')
assert(Project.valid_project_id(opts['--project']),'--project needs a safe ID')
local manifest=assert(read('assets/studio/manifest.txt'),
    'publish loose visuals before atlas compilation')
local project=Project.decode(manifest)
assert(project.id==opts['--project'],'published visual project differs')
local candidate_root=save_dir..'/studio/atlas-build/'
local metadata=assert(read(candidate_root..'manifest.txt'),
    'missing atlas candidate; run love . --atlas-build first')
local pack=Atlas.decode(metadata)
assert(pack.project_id==project.id,'candidate project mismatch')
local matches,why=Atlas.matches_inputs(pack,project,read)
assert(matches,'candidate is stale: '..tostring(why))
local total_source,total_atlas=0,0
for _,entry in pairs(pack.assets) do
    local bytes=assert(read('assets/studio/'..entry.source),'missing source: '..entry.id)
    assert(Atlas.checksum(bytes)==entry.checksum,'changed source: '..entry.id)
    total_source=total_source+#bytes
end
local page_bytes={}
for index,page in ipairs(pack.pages) do
    local name=page.filename:match('[^/]+$')
    local bytes=assert(read(candidate_root..name),'missing candidate page '..index)
    assert(#bytes<=32*1024*1024 and Atlas.checksum(bytes)==page.checksum,
        'corrupt candidate page '..index)
    page_bytes[page.filename]=bytes;total_atlas=total_atlas+#bytes
    local pixels=0
    for _,entry in ipairs(page.entries) do pixels=pixels+entry.w*entry.h end
    print(('page %d %s/%s %dx%d %s, %d assets, %.1f%% content occupancy, %d bytes'):
        format(index,page.group,page.filter,page.width,page.height,page.filename,
            #page.entries,100*pixels/(page.width*page.height),#bytes))
    for _,entry in ipairs(page.entries) do
        print(('  %s -> (%d,%d %dx%d)'):format(entry.id,
            entry.x,entry.y,entry.w,entry.h))
    end
end
local excluded={}
for id in pairs(pack.excluded) do excluded[#excluded+1]=id end
table.sort(excluded)
for _,id in ipairs(excluded) do print('standalone '..id..': '..pack.excluded[id]) end
local slot=Store.plan(pack,read)
print(('source PNG bytes %d; atlas PNG bytes %d; activate %s sequence %d'):
    format(total_source,total_atlas,slot.path,slot.seq))
if not opts['--apply'] then
    print('Dry run. Pass --apply to publish; --overwrite if the inactive slot is occupied.')
    return
end
assert(not slot.occupied or opts['--overwrite'],
    'inactive atlas record is occupied; inspect dry run then pass --overwrite')
safe_destination(slot.path)
local mkdir_command=windows and
    'if not exist assets\\studio\\atlas mkdir assets\\studio\\atlas' or
    'mkdir -p assets/studio/atlas'
assert(os.execute(mkdir_command)==0,
    'cannot create fixed atlas directory')
for _,page in ipairs(pack.pages) do
    local bytes=page_bytes[page.filename]
    safe_destination(page.filename)
    local existing=read(page.filename)
    assert(not existing or existing==bytes,
        'content-addressed page collision: '..page.filename)
    if not existing then write(page.filename,bytes) end
end
-- The metadata record is the only activation step, and is written last.
write(slot.path,slot.bytes)
if not read(Store.disable_path) then
    assert(Store.inspect(read,project),
        'published generation failed complete readback')
end
print('Activated complete atlas generation '..slot.seq)
