-- Optional, presentation-only four-tone atlases generated from Paint sources.
-- The manifest is activated last; every page is checked before any handle is
-- exposed. Bad/missing art leaves VisualAssets' loose and built-in fallbacks.
local Atlas=require('src.studio_atlas')
local Slots=require('src.visual_slots')
local G={}
local function fields(line)
    local result={}
    for part in (line..'\t'):gmatch('(.-)\t') do result[#result+1]=part end
    return result
end
local function integer(value,lo,hi)
    local number=tonumber(value)
    assert(number and number%1==0 and number>=lo and number<=hi,
        'invalid atlas integer')
    return number
end
function G.new(read,make_texture,make_quad)
    local bytes=read('assets/generated/manifest.txt')
    if not bytes then return nil,'absent' end
    local lines={}
    for line in bytes:gmatch('[^\r\n]+') do lines[#lines+1]=line end
    assert(lines[1]=='ROAG-GENERATED-ATLAS/1','incompatible generated atlas')
    local pages,assets={},{}
    for index=2,#lines do
        local record=fields(lines[index])
        if record[1]=='page' then
            assert(#record==6,'invalid generated page')
            local name,path=record[2],record[3]
            assert(name:match('^[a-z]+$') and not pages[name],
                'duplicate or unsafe generated page')
            assert(path:match('^assets/generated/builds/[0-9a-f]+/[a-z]+%.png$'),
                'unsafe generated page path')
            local width,height=integer(record[4],1,2048),integer(record[5],1,2048)
            local data=assert(read(path),'missing generated page: '..path)
            assert(Atlas.checksum(data)==record[6],
                'corrupt generated page: '..path)
            local texture=assert(make_texture(data,path),'unreadable generated page: '..path)
            assert(texture:getWidth()==width and texture:getHeight()==height,
                'generated page dimensions differ')
            pages[name]={texture=texture,width=width,height=height}
        elseif record[1]=='asset' then
            assert(#record==7,'invalid generated asset')
            local id,page_name=record[2],record[3]
            assert(Slots.bindings[id] and not assets[id],
                'duplicate or unknown generated binding: '..tostring(id))
            assets[id]={page=page_name,x=integer(record[4],0,2048),
                y=integer(record[5],0,2048),width=integer(record[6],1,2048),
                height=integer(record[7],1,2048)}
        else error('unknown generated atlas record') end
    end
    assert(next(pages) and next(assets),'empty generated atlas')
    make_quad=make_quad or function(x,y,w,h,pw,ph)
        return love.graphics.newQuad(x,y,w,h,pw,ph)
    end
    local handles={}
    for id,asset in pairs(assets) do
        local page=assert(pages[asset.page],'unknown generated page for '..id)
        assert(asset.x+asset.width<=page.width and
            asset.y+asset.height<=page.height,'out-of-bounds generated quad')
        handles[id]={texture=page.texture,
            quad=make_quad(asset.x,asset.y,asset.width,asset.height,
                page.width,page.height),
            width=asset.width,height=asset.height,source='generated'}
    end
    return setmetatable({handles=handles,pages=pages,
        signature=Atlas.checksum(bytes),status='loaded'},{__index=G}), 'loaded'
end
function G:resolve(id) return self.handles[id] end
return G
