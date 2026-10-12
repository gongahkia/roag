-- Read-only Studio E06 publication report. Source documents and published
-- generations remain independent; this module never writes or repairs either.
local Publish=require('src.studio_publish')
local World=require('src.studio_world')
local WorldRuntime=require('src.studio_world_runtime')
local Theme=require('src.studio_theme')
local ThemeStore=require('src.studio_theme_store')
local AtlasStore=require('src.studio_atlas_store')
local Project=require('src.studio_project')
local S={}

function S.inspect(project,source_read,published_read,world,theme,verify_page)
    assert(project and project.id and type(source_read)=='function' and
        type(published_read)=='function','project and readers required')
    local report={project=project.id,assets={},warnings={}}
    local ok,plan=pcall(Publish.plan,project,source_read,published_read)
    if ok then
        local changed=0
        for _,file in ipairs(plan) do
            if file.status~='unchanged' then changed=changed+1 end
        end
        report.loose=changed==0 and 'current' or 'stale ('..changed..' file changes)'
    else
        report.loose='invalid source: '..tostring(plan)
        report.warnings[#report.warnings+1]=report.loose
    end
    local published_bytes=published_read('assets/studio/manifest.txt')
    local published
    if published_bytes then
        local valid,value=pcall(Project.decode,published_bytes)
        if valid then published=value else
            report.warnings[#report.warnings+1]='published visual manifest: '..tostring(value)
        end
    end
    if published and published.id~=project.id then
        report.loose='other project ('..published.id..')'
        report.warnings[#report.warnings+1]=
            'active published visuals belong to '..published.id
    end
    local by_id={}
    if published then for _,asset in ipairs(published.assets) do by_id[asset.id]=asset end end
    for _,asset in ipairs(project.assets) do
        local path=asset.derived~='' and asset.derived or asset.source
        local bytes
        if path~='' then
            local fetched,value=pcall(source_read,path)
            if fetched then bytes=value end
        end
        local decoded,w,h=false,nil,nil
        if bytes then decoded,w,h=pcall(Project.png_dimensions,bytes) end
        local source_ok=decoded and w==asset.width and h==asset.height
        local published_asset=by_id[asset.id]
        local item={id=asset.id,source=source_ok and 'valid' or 'missing/draft',
            resolver='built-in fallback'}
        if published_asset and published_asset.source~='' then
            item.resolver='published loose'
        end
        if not source_ok then report.warnings[#report.warnings+1]=
            'missing source for '..asset.id end
        report.assets[#report.assets+1]=item
    end
    table.sort(report.assets,function(a,b) return a.id<b.id end)
    local published_world,world_status=WorldRuntime.inspect(published_read)
    if published_world and published then
        local valid,why=pcall(World.validate,published_world,published.assets)
        if not valid then
            world_status='invalid published references: '..tostring(why)
            report.warnings[#report.warnings+1]=world_status
        end
    end
    report.world=world_status
    if world then
        local valid,why=pcall(World.validate,world,project.assets)
        if not valid then report.world='invalid source: '..tostring(why)
        elseif published_world and published_world.project_id~=project.id then
            report.world='other project ('..published_world.project_id..')'
        elseif published_world and not world_status:match('^invalid') and
            World.encode(world)==World.encode(published_world) then
            report.world='current'
        elseif world_status=='absent' and #world.rooms==0 then report.world='none'
        else report.world='stale ('..tostring(world_status)..')' end
    end
    local published_theme,theme_status=ThemeStore.inspect_published(published_read)
    if published_theme and published then
        local valid,why=pcall(Theme.validate,published_theme,published.assets)
        if not valid then
            theme_status='invalid published references: '..tostring(why)
            report.warnings[#report.warnings+1]=theme_status
        end
    end
    report.theme=theme_status
    if theme then
        local valid,why=pcall(Theme.validate,theme,project.assets)
        if not valid then report.theme='invalid source: '..tostring(why)
        elseif published_theme and published_theme.project_id~=project.id then
            report.theme='other project ('..published_theme.project_id..')'
        elseif published_theme and not theme_status:match('^invalid') and
            Theme.encode(theme)==Theme.encode(published_theme) then
            report.theme='current'
        elseif theme_status=='absent' then report.theme='not published'
        else report.theme='stale ('..tostring(theme_status)..')' end
    end
    if published then
        local pack,status=AtlasStore.inspect(published_read,published,verify_page)
        report.atlas=status
        if pack then
            for _,item in ipairs(report.assets) do
                if pack.assets[item.id] then item.resolver='atlas' end
            end
        elseif status~='absent' and status~='disabled' then
            report.warnings[#report.warnings+1]='atlas: '..tostring(status)
        end
    else report.atlas='unavailable without published visuals' end
    return report
end
return S
