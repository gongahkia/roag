-- Presentation-only sprite-pack choice. This record is separate from runs and
-- profiles; bad settings fall back to the automatic resolver.
local Atlas=require('src.studio_atlas')
local VisualAssets=require('src.visual_assets')
local P={format='roag-visual-pack/1',files={
    'visual-pack-a.roag','visual-pack-b.roag'},default='automatic',
    order={'automatic','classic','generated','published','kenney'},
    labels={automatic='Automatic',classic='Classic built-in',
        generated='Dungeon pixel art',published='Published Paint',
        kenney='Kenney Micro Roguelike'},
    descriptions={
        automatic='Published Paint art, then bundled Dungeon pixel art',
        classic='Original game tokens and terrain',
        generated='Connected terrain, heroes and enemies',
        published='Paint art and atlas; built-in fallback',
        kenney='Original 8×8 terrain, characters and items · CC0 trial',
    }}

function P.valid(mode) return P.labels[mode]~=nil end
function P.index(mode)
    for index,value in ipairs(P.order) do if value==mode then return index end end
    return 1
end

local function encode(sequence,mode)
    assert(P.valid(mode),'invalid sprite pack')
    local body=table.concat({P.format,tostring(sequence),mode},'\n')..'\n'
    return body..Atlas.checksum(body)..'\n'
end

local function decode(bytes)
    assert(type(bytes)=='string' and #bytes<=128,'invalid sprite-pack record size')
    local format,number,mode,hash=bytes:match('^([^\n]+)\n([^\n]+)\n([^\n]+)\n([^\n]+)\n$')
    assert(format and hash,'truncated sprite-pack record')
    local sequence=tonumber(number)
    assert(sequence and sequence%1==0 and sequence>=1 and
        sequence<9007199254740991,'invalid sprite-pack generation')
    assert(P.valid(mode),'invalid sprite-pack choice')
    assert(Atlas.checksum(table.concat({format,number,mode},'\n')..'\n')==hash,
        'sprite-pack checksum mismatch')
    return {format=format,sequence=sequence,mode=mode}
end

function P.inspect(storage)
    assert(type(storage)=='table' and type(storage.read)=='function',
        'invalid sprite-pack storage')
    local records,invalid={},0
    for slot,name in ipairs(P.files) do
        local ok,bytes=pcall(storage.read,storage,name)
        if not ok then invalid=invalid+1
        elseif bytes~=nil then
            local valid,record=pcall(decode,bytes)
            if valid then record.slot=slot;records[#records+1]=record
            else invalid=invalid+1 end
        end
    end
    table.sort(records,function(a,b)
        if a.sequence~=b.sequence then return a.sequence>b.sequence end
        return a.slot>b.slot
    end)
    local latest=records[1]
    if not latest then
        return {mode=P.default,status=invalid>0 and 'corrupt' or 'absent'}
    end
    if latest.format~=P.format then
        return {mode=P.default,status='incompatible',record=latest}
    end
    return {mode=latest.mode,status=invalid>0 and 'recovered' or 'loaded',
        record=latest}
end

function P.save(storage,mode)
    if not P.valid(mode) then return false,'invalid sprite pack' end
    local found=P.inspect(storage)
    if found.status=='incompatible' then
        return false,'newer sprite-pack settings preserved'
    end
    local previous=found.record
    local slot=previous and 3-previous.slot or 1
    local sequence=previous and previous.sequence+1 or 1
    local bytes=encode(sequence,mode)
    local wrote,result=pcall(storage.write,storage,P.files[slot],bytes)
    if not wrote or not result then return false,'settings write failed: '..tostring(result) end
    local ok,readback=pcall(storage.read,storage,P.files[slot])
    if not ok or readback~=bytes then return false,'settings read-back failed' end
    local valid,record=pcall(decode,readback)
    if not valid or record.sequence~=sequence or record.format~=P.format then
        return false,'settings verification failed'
    end
    return true
end

-- Only the selected renderer is handed a source. Missing assets still reach
-- the established built-in drawing path; no simulation data is involved.
function P.build(mode,read,make_image,load_generated)
    assert(P.valid(mode),'invalid sprite pack')
    local generated,status=nil,'disabled'
    if mode=='generated' or mode=='automatic' or mode=='kenney' then
        generated,status=load_generated(read,mode)
    end
    local visual_read=read
    if mode=='classic' or mode=='generated' or mode=='kenney' then
        visual_read=function(path)
            if path=='assets/studio/manifest.txt' then return nil end
            return read(path)
        end
    end
    return VisualAssets.new(visual_read,make_image,generated),status
end

function P.uses_published_theme(mode)
    return mode=='automatic' or mode=='published'
end

return P
