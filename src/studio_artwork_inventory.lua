-- Read-only inventory of game artwork destinations. Slots are virtual: an
-- empty slot never consumes one of the project's bounded asset entries.
local Slots=require('src.visual_slots')
local Theme=require('src.studio_theme')
local Terrain=require('src.terrain')
local Document=require('src.studio_document')
local I={}
local group_order={Character=1,Enemies=2,Rewards=3,Boons=4,
    Backdrops=5,Terrain=6,['UI theme']=7,['Letters & text']=8,
    ['World tiles']=9}
local function words(value)
    return (value:gsub('_',' '):gsub('^%l',string.upper))
end
local function visual_entry(id)
    local group,kind,optional
    if id:match('^core:actor/') then group='Character';kind='actor'
    elseif id:match('^core:enemy/') then group='Enemies';kind='enemy'
    elseif id:match('^core:drop/') or id:match('^core:chest/') then
        group='Rewards';kind='ui'
    elseif id:match('^core:boon/') then group='Boons';kind='ui_icon'
    elseif id:match('^core:background/') then group='Backdrops';kind='background'
    else group='Terrain';kind='tile' end
    optional=id:match('/attack$') or id:match('/hurt$') or
        id:match('/detail$') or (group=='Terrain' and not id:match('/base$'))
    local path=id:match(':(.+)$') or id
    return {key=id,binding=id,label=words(path),group=group,kind=kind,
        optional=not not optional}
end
local function asset_for(project,binding)
    for index,asset in ipairs(project.assets or {}) do
        if asset.binding==binding then return asset,index end
    end
end
local function find_id(project,id)
    for index,asset in ipairs(project.assets or {}) do
        if asset.id==id then return asset,index end
    end
end
local function has_pixels(asset)
    return asset and (asset.source~='' or asset.derived~='')
end
local function safe_read(read,path)
    if not read then return nil end
    local ok,value=pcall(read,path)
    return ok and value or nil
end
local function document_has_visible_ink(bytes)
    local ok,doc=pcall(Document.decode,bytes)
    if not ok then return nil end
    for _,layer in ipairs(doc.layers) do
        if layer.visible then
            for _,row in ipairs(layer.rows) do
                for x=1,doc.width do
                    if row:byte(x*4)>0 then return true end
                end
            end
        end
    end
    return false
end
local function art_state(asset,source_read)
    if not asset then return 'broken reference' end
    if not has_pixels(asset) then return 'draft' end
    if not source_read then return 'drawn' end
    local path=asset.derived~='' and asset.derived or asset.source
    if not safe_read(source_read,path) then return 'broken source' end
    if asset.document~='' then
        local bytes=safe_read(source_read,asset.document)
        local ink=bytes and document_has_visible_ink(bytes)
        if ink==nil then return 'broken source' end
        if not ink then return 'blank canvas' end
    end
    return 'drawn'
end
local function published_status(asset,published,source_read,published_read)
    if not asset then return published and 'published elsewhere' or 'missing' end
    if not has_pixels(asset) then return 'draft' end
    local source_path=asset.derived~='' and asset.derived or asset.source
    local bytes=safe_read(source_read,source_path)
    local state=art_state(asset,source_read)
    if state~='drawn' then return state end
    if published and bytes and
        bytes==safe_read(published_read,'assets/studio/'..published.source) then
        return 'published'
    end
    return published and 'changed since publish' or 'drawn, unpublished'
end
local function glyph_label(byte)
    local char=string.char(byte)
    if byte==32 then return 'Space' end
    if char:match('%u') then return 'Uppercase '..char end
    if char:match('%l') then return 'Lowercase '..char end
    if char:match('%d') then return 'Digit '..char end
    return 'Punctuation '..char
end
function I.build(project,world,theme,source_read,published_read)
    assert(project and project.assets,'Studio project required')
    local published_by_binding={}
    local manifest=safe_read(published_read,'assets/studio/manifest.txt')
    if manifest then
        local Project=require('src.studio_project')
        local ok,published=pcall(Project.decode,manifest)
        if ok then
            for _,asset in ipairs(published.assets) do
                if asset.binding~='' then published_by_binding[asset.binding]=asset end
            end
        end
    end
    local result={}
    for id in pairs(Slots.bindings) do
        if not id:match('^core:ui/') and not id:match('^core:glyph/') then
        local entry=visual_entry(id)
        local asset,index=asset_for(project,id)
        local published=published_by_binding[id]
        entry.asset=asset;entry.asset_index=index;entry.published_asset=published
        entry.status=published_status(asset,published,source_read,published_read)
        result[#result+1]=entry
        end
    end
    for _,slot in ipairs(Theme.slots) do
        local binding='core:ui/'..slot
        local direct,index=asset_for(project,binding)
        local published=published_by_binding[binding]
        local id=theme and theme.slots[slot] or ''
        local assigned,assigned_index
        if id~='' then assigned,assigned_index=find_id(project,id) end
        local asset=direct or assigned
        index=index or assigned_index
        local status=direct and published_status(direct,published,source_read,published_read)
            or published and 'published elsewhere' or
            id=='' and 'missing' or
            art_state(assigned,source_read):gsub('^drawn$','drawn, assigned')
        result[#result+1]={key='theme:'..slot,label=words(slot),group='UI theme',
            kind=slot=='hud_panel' and 'ui_panel' or
                (slot=='title_background' or slot=='pause_background') and
                    'ui_background' or slot=='bitmap_font' and 'bitmap_font' or
                (slot=='hit_effect' or slot=='boon_effect') and 'effect_sprite' or
                    'ui_icon',binding=binding,asset=asset,asset_index=index,
            published_asset=published,optional=false,status=status}
    end
    for byte=32,126 do
        local char=string.char(byte)
        local binding=Slots.glyph_binding(char)
        local asset,index=asset_for(project,binding)
        local published=published_by_binding[binding]
        result[#result+1]={key=binding,binding=binding,
            label=glyph_label(byte),group='Letters & text',kind='glyph',
            asset=asset,asset_index=index,published_asset=published,
            default_size='4x8',optional=false,
            status=published_status(asset,published,source_read,published_read)}
    end
    local world_tiles={}
    local known_tiles={}
    for _,tile in ipairs(world and world.tiles or {}) do
        world_tiles[#world_tiles+1]=tile;known_tiles[tile.id]=true
    end
    for _,symbol in ipairs(Terrain.order) do
        local def=Terrain.get(symbol)
        local id='studio:tile/'..def.id:match('[^/]+$')
        if not known_tiles[id] then
            world_tiles[#world_tiles+1]={id=id,name=def.name,symbol=symbol,
                asset_id='',not_added=true}
        end
    end
    for _,tile in ipairs(world_tiles) do
        local id=tile.asset_id or ''
        local asset,index
        if id~='' then asset,index=find_id(project,id) end
        result[#result+1]={key='world:'..tile.id,label=tile.name..' ('..tile.symbol..')',
            group='World tiles',kind='world',asset=asset,asset_index=index,
            optional=false,not_added=tile.not_added,
            status=tile.not_added and 'not added' or id=='' and 'unmapped' or
                art_state(asset,source_read):gsub('^drawn$','drawn, assigned')}
    end
    table.sort(result,function(a,b)
        local ga,gb=group_order[a.group],group_order[b.group]
        if ga~=gb then return ga<gb end
        if a.optional~=b.optional then return not a.optional end
        return a.key<b.key
    end)
    return result
end
function I.progress(entry)
    local status=entry.status
    if status=='published' or status=='published elsewhere' then
        return 'done','LIVE'
    elseif status=='drawn, assigned' or status=='drawn, unpublished' or
        status=='changed since publish' then
        return 'work',status=='changed since publish' and 'CHANGED' or 'READY'
    elseif status=='draft' or status=='blank canvas' then
        return 'work','DRAFT'
    elseif status=='broken source' or status=='broken reference' then
        return 'repair','REPAIR'
    end
    return 'empty','EMPTY'
end
function I.filter(entries,query,group,missing_only)
    query=(query or ''):lower()
    local result={}
    for _,entry in ipairs(entries) do
        local absent=entry.status=='missing' or entry.status=='unmapped' or
            entry.status=='draft' or entry.status=='broken source' or
            entry.status=='broken reference' or entry.status=='not added' or
            entry.status=='blank canvas'
        if (not group or group=='All' or
            (group=='Core' and not entry.optional) or entry.group==group) and
            (not missing_only or absent) and
            (query=='' or entry.key:lower():find(query,1,true) or
                entry.label:lower():find(query,1,true)) then
            result[#result+1]=entry
        end
    end
    return result
end
I.groups={'Core','All','Character','Enemies','Rewards','Boons','Backdrops',
    'Terrain','UI theme','Letters & text','World tiles'}
return I
